using ElectronicObserver.Update;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ElectronicObserver.Utility
{
	/// <summary>更新プログラムのダウンロード状態。</summary>
	public enum UpdateDownloadStatus
	{
		/// <summary>ダウンロードしていない</summary>
		None,

		/// <summary>バックグラウンドでダウンロード中</summary>
		Downloading,

		/// <summary>準備完了 (再起動すると適用される)</summary>
		Ready,

		/// <summary>失敗</summary>
		Failed,
	}

	/// <summary>
	/// 配布 zip をバックグラウンドでダウンロードして保持します。
	///
	/// ダウンロード先は更新プログラム (EOUpdater) と共通のキャッシュディレクトリなので、
	/// 本体を終了してからダウンロードし直す必要はありません
	/// (再起動すると更新プログラムがダウンロード済みのファイルを使って適用します)。
	/// </summary>
	public class UpdateDownloader
	{
		private readonly IReleaseSource _source;
		private readonly string _downloadDirectory;

		public UpdateDownloader(IReleaseSource source = null, string downloadDirectory = null)
		{
			_source = source ?? new GitHubReleaseSource(SoftwareInformation.RepositoryOwner, SoftwareInformation.RepositoryName);
			_downloadDirectory = downloadDirectory ?? UpdateLogic.GetDownloadDirectory();
		}

		/// <summary>現在の状態</summary>
		public UpdateDownloadStatus Status { get; private set; } = UpdateDownloadStatus.None;

		/// <summary>対象のバージョン (タグから "v" を除いたもの)</summary>
		public string LatestVersion { get; private set; }

		/// <summary>ダウンロードする配布 zip のパス (ダウンロード開始後に設定されます)</summary>
		public string ZipPath { get; private set; }

		/// <summary>配布 zip のサイズ (進捗表示用)</summary>
		public long ZipSize { get; private set; }

		/// <summary>失敗した理由</summary>
		public string Error { get; private set; }

		/// <summary>ダウンロード済みのサイズ (進捗表示用)</summary>
		public long DownloadedSize
		{
			get
			{
				if (string.IsNullOrEmpty(ZipPath)) return 0;

				try
				{
					var info = new FileInfo(ZipPath);
					return info.Exists ? Math.Min(info.Length, ZipSize) : 0;
				}
				catch (Exception)
				{
					return 0;
				}
			}
		}

		/// <summary>進捗 (0～1)。サイズが不明な場合は 0。</summary>
		public double Progress => ZipSize > 0 ? Math.Min(1.0, (double)DownloadedSize / ZipSize) : 0.0;

		/// <summary>準備できているか (再起動で適用できる状態か)</summary>
		public bool IsReady => Status == UpdateDownloadStatus.Ready;

		/// <summary>
		/// 配布 zip をダウンロードして SHA-256 を検証します。
		/// ダウンロード済みで検証できる場合はダウンロードし直しません。
		/// </summary>
		public async Task<bool> DownloadAsync(ReleaseInfo release)
		{
			if (release == null)
			{
				Fail("リリース情報を取得できませんでした。");
				return false;
			}

			ReleaseAsset zipAsset = UpdateLogic.SelectZipAsset(release.Assets);
			ReleaseAsset checksumsAsset = UpdateLogic.SelectChecksumsAsset(release.Assets);

			if (zipAsset == null || checksumsAsset == null)
			{
				Fail($"配布ファイル ({UpdateLogic.ZipAssetPrefix}*.zip) が見つかりません。");
				return false;
			}

			LatestVersion = release.Version;
			ZipSize = zipAsset.Size;
			ZipPath = Path.Combine(_downloadDirectory, zipAsset.Name);
			Error = null;
			Status = UpdateDownloadStatus.Downloading;

			try
			{
				Directory.CreateDirectory(_downloadDirectory);

				string checksumsPath = Path.Combine(_downloadDirectory, UpdateLogic.ChecksumsAssetName);

				// 前のバージョンのチェックサムファイルが残っていることがあるので、
				// 中身が今回の配布 zip のものでなければ取り直す
				if (!UpdateLogic.CanReuseChecksumsFile(checksumsPath, zipAsset.Name, checksumsAsset.Size))
				{
					if (File.Exists(checksumsPath)) File.Delete(checksumsPath);
					await _source.DownloadFileAsync(checksumsAsset.DownloadUrl, checksumsPath).ConfigureAwait(false);
				}

				string expectedHash = UpdateLogic.ParseChecksum(File.ReadAllText(checksumsPath), zipAsset.Name);
				if (expectedHash == null)
					throw new InvalidOperationException($"{UpdateLogic.ChecksumsAssetName} に {zipAsset.Name} のハッシュがありません。");

				// ダウンロード済みならそれを検証して使う (SHA-256 の計算は重いので別スレッドで行う)
				bool usable = await Task.Run(() =>
					UpdateLogic.IsDownloadedFileComplete(ZipPath, zipAsset.Size) &&
					UpdateLogic.VerifyChecksum(ZipPath, expectedHash)).ConfigureAwait(false);

				if (!usable)
				{
					if (File.Exists(ZipPath)) File.Delete(ZipPath);

					await _source.DownloadFileAsync(zipAsset.DownloadUrl, ZipPath).ConfigureAwait(false);

					bool verified = await Task.Run(() => UpdateLogic.VerifyChecksum(ZipPath, expectedHash)).ConfigureAwait(false);
					if (!verified)
					{
						// 壊れているので残しておかない (次回もう一度ダウンロードする)
						try
						{
							File.Delete(ZipPath);
						}
						catch (Exception)
						{
							// 消せなくても更新は失敗させる
						}

						throw new InvalidOperationException("ダウンロードしたファイルの SHA-256 が一致しません。更新を中止しました。");
					}
				}

				DeleteObsoleteZips();

				Status = UpdateDownloadStatus.Ready;
				Utility.Logger.Add(2, $"新しいバージョン {LatestVersion} のダウンロードが完了しました。再起動すると更新が適用されます。");

				return true;
			}
			catch (Exception ex)
			{
				Fail(ex is AggregateException aggregate && aggregate.InnerException != null
					? aggregate.InnerException.Message
					: ex.Message);
				return false;
			}
		}

		/// <summary>古いバージョンの配布 zip を消してキャッシュを掃除します。</summary>
		private void DeleteObsoleteZips()
		{
			try
			{
				foreach (string path in UpdateLogic.GetObsoleteZipPaths(_downloadDirectory, ZipPath))
				{
					try
					{
						File.Delete(path);
					}
					catch (Exception ex)
					{
						Utility.Logger.Add(2, $"古い更新ファイルを削除できませんでした: {Path.GetFileName(path)} ({ex.Message})");
					}
				}
			}
			catch (Exception ex)
			{
				Utility.Logger.Add(2, "古い更新ファイルの削除に失敗しました: " + ex.Message);
			}
		}

		private void Fail(string message)
		{
			Status = UpdateDownloadStatus.Failed;
			Error = message;
		}
	}
}
