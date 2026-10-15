using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ElectronicObserver.Update;

namespace EOUpdater
{
	/// <summary>
	/// 配布 zip をダウンロードして ElectronicObserver のインストール先へ適用する更新プログラム。
	///
	/// 引数:
	///   --nowait   実行中プロセスの終了を待たずに適用する (既に終了している場合など)
	///   --restart  更新後に ElectronicObserver.exe を起動する
	/// </summary>
	public static class Program
	{
		private const string RepositoryOwner = "miyabitti256";
		private const string RepositoryName = "ElectronicObserver";

		private const string ApplicationFileName = "ElectronicObserver.exe";
		private const string ApplicationProcessName = "ElectronicObserver";
		private const string BrowserProcessName = "EOBrowser";

		private static readonly string[] WaitProcessNames = { ApplicationProcessName, BrowserProcessName };

		public static async Task<int> Main(string[] args)
		{
			Console.Title = "七四式アップデータ";

			bool wait = true;
			bool restart = false;

			foreach (string argument in args)
			{
				switch (argument.ToLowerInvariant())
				{
					case "--nowait":
						wait = false;
						break;

					case "--restart":
						restart = true;
						break;

					default:
						Console.WriteLine($"不明な引数: {argument}");
						break;
				}
			}

			// GitHub API (api.github.com) は TLS 1.2 以上が必要
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

			string applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string selfPath = GetSelfPath();

			try
			{
				var source = new GitHubReleaseSource(RepositoryOwner, RepositoryName);

				Console.WriteLine("最新のリリースを確認しています...");
				ReleaseInfo release = await source.GetLatestReleaseAsync();

				if (release == null || string.IsNullOrEmpty(release.TagName))
					throw new InvalidOperationException("リリース情報を取得できませんでした。");

				Console.WriteLine($"最新バージョン: {release.Version} ({ReleaseVersion.ResolveDate(release.TagName, release.PublishedAt):yyyy/MM/dd})");

				if (!string.IsNullOrWhiteSpace(release.Body))
					Console.WriteLine(release.Body.Trim());

				ReleaseAsset zipAsset = UpdateLogic.SelectZipAsset(release.Assets);
				ReleaseAsset checksumsAsset = UpdateLogic.SelectChecksumsAsset(release.Assets);

				if (zipAsset == null)
					throw new InvalidOperationException($"配布ファイル ({UpdateLogic.ZipAssetPrefix}*.zip) が見つかりません。");

				if (checksumsAsset == null)
					throw new InvalidOperationException($"{UpdateLogic.ChecksumsAssetName} が見つかりません。");

				string downloadDirectory = PrepareDownloadDirectory();
				string zipPath = Path.Combine(downloadDirectory, zipAsset.Name);
				string checksumsPath = Path.Combine(downloadDirectory, UpdateLogic.ChecksumsAssetName);

				await DownloadIfNeededAsync(source, checksumsAsset, checksumsPath);
				await DownloadIfNeededAsync(source, zipAsset, zipPath);

				// ダウンロードした zip を checksums.txt の SHA-256 で検証する
				string expectedHash = UpdateLogic.ParseChecksum(File.ReadAllText(checksumsPath), zipAsset.Name);
				if (expectedHash == null)
					throw new InvalidOperationException($"{UpdateLogic.ChecksumsAssetName} に {zipAsset.Name} のハッシュがありません。");

				if (!UpdateLogic.VerifyChecksum(zipPath, expectedHash))
					throw new InvalidOperationException("ダウンロードしたファイルの SHA-256 が一致しません。更新を中止しました。");

				Console.WriteLine($"SHA-256 検証 OK: {expectedHash}");

				if (wait)
					WaitForProcessesExit();

				ApplyUpdate(zipPath, applicationDirectory, selfPath);
				WriteVersionFile(applicationDirectory, release);

				Console.WriteLine("アップデートが完了しました。");
			}
			catch (Exception ex)
			{
				Console.WriteLine("アップデートに失敗しました: " + ex.Message);
				Console.WriteLine("このウィンドウを閉じて、手動で最新版をダウンロードしてください。");

				if (!restart)
					WaitForKey();

				return 1;
			}

			if (restart)
			{
				string applicationPath = Path.Combine(applicationDirectory, ApplicationFileName);

				if (File.Exists(applicationPath))
					Process.Start(new ProcessStartInfo(applicationPath) { WorkingDirectory = applicationDirectory, UseShellExecute = true });
				else
					Console.WriteLine($"{ApplicationFileName} が見つからないため再起動できませんでした。");

				return 0;
			}

			Console.WriteLine("何かキーを押すと終了します。");
			WaitForKey();

			return 0;
		}

		/// <summary>実行中の自分のパス (削除・上書きの対象から除外する)。</summary>
		private static string GetSelfPath()
		{
			try
			{
				return Process.GetCurrentProcess().MainModule?.FileName
					?? Assembly.GetEntryAssembly()?.Location;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static string PrepareDownloadDirectory()
		{
			string directory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ElectronicObserver",
				"Update");

			Directory.CreateDirectory(directory);
			return directory;
		}

		private static async Task DownloadIfNeededAsync(IReleaseSource source, ReleaseAsset asset, string destinationPath)
		{
			// 前回のダウンロードが残っていれば再利用する
			if (File.Exists(destinationPath))
			{
				try
				{
					if (new FileInfo(destinationPath).Length == asset.Size)
					{
						Console.WriteLine($"ダウンロード済みのファイルを使用します: {asset.Name}");
						return;
					}
				}
				catch (Exception)
				{
					// 判定できなければダウンロードし直す
				}

				File.Delete(destinationPath);
			}

			Console.WriteLine($"ダウンロード中: {asset.Name} ({asset.Size / 1024 / 1024} MB)");
			await source.DownloadFileAsync(asset.DownloadUrl, destinationPath);
			Console.WriteLine("ダウンロード完了。");
		}

		private static void WaitForProcessesExit()
		{
			foreach (string name in WaitProcessNames)
			{
				foreach (Process process in Process.GetProcessesByName(name))
				{
					using (process)
					{
						Console.WriteLine($"{process.ProcessName} の終了を待っています... (終了すると更新が始まります)");

						try
						{
							process.WaitForExit();
						}
						catch (Exception ex)
						{
							Console.WriteLine($"プロセスの終了待機に失敗しました: {ex.Message}");
						}
					}
				}
			}
		}

		/// <summary>古いファイルを削除して zip を上書き展開する。</summary>
		private static void ApplyUpdate(string zipPath, string destinationDirectory, string selfPath)
		{
			foreach (string file in UpdateLogic.GetFilesToDelete(destinationDirectory, selfPath))
			{
				try
				{
					File.Delete(file);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"削除できませんでした: {Path.GetFileName(file)} ({ex.Message})");
				}
			}

			string selfFileName = string.IsNullOrEmpty(selfPath) ? null : Path.GetFileName(selfPath);

			using (var archive = ZipFile.OpenRead(zipPath))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					// ディレクトリエントリ
					if (string.IsNullOrEmpty(entry.Name)) continue;

					// 実行中の自分自身は上書きできない
					if (selfFileName != null && string.Equals(entry.Name, selfFileName, StringComparison.OrdinalIgnoreCase)) continue;

					// zip slip 対策
					if (!UpdateLogic.IsEntryInsideDirectory(destinationDirectory, entry.FullName)) continue;

					string destination = Path.Combine(destinationDirectory, entry.FullName.Replace('/', Path.DirectorySeparatorChar));

					try
					{
						string parent = Path.GetDirectoryName(destination);
						if (!string.IsNullOrEmpty(parent))
							Directory.CreateDirectory(parent);

						entry.ExtractToFile(destination, true);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"更新できませんでした: {entry.FullName} ({ex.Message})");
					}
				}
			}
		}

		private static void WriteVersionFile(string applicationDirectory, ReleaseInfo release)
		{
			string content = UpdateLogic.BuildVersionFileContent(release);
			if (content == null) return;

			try
			{
				File.WriteAllText(Path.Combine(applicationDirectory, "version.txt"), content);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"version.txt を書き込めませんでした: {ex.Message}");
			}
		}

		private static void WaitForKey()
		{
			try
			{
				if (!Console.IsInputRedirected)
					Console.ReadKey(true);
			}
			catch (Exception)
			{
				// コンソールが無い場合は何もしない
			}
		}
	}
}
