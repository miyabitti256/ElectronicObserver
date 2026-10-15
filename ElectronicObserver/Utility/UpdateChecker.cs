using System;
using System.Threading.Tasks;
using ElectronicObserver.Update;

namespace ElectronicObserver.Utility
{
	/// <summary>更新確認の結果の種類。</summary>
	public enum UpdateCheckStatus
	{
		/// <summary>お使いのバージョンが最新です。</summary>
		UpToDate,

		/// <summary>新しいバージョンが公開されています。</summary>
		UpdateAvailable,

		/// <summary>確認に失敗しました。</summary>
		Failed,
	}

	/// <summary>更新確認の結果。</summary>
	public class UpdateCheckResult
	{
		public UpdateCheckStatus Status { get; private set; }

		/// <summary>取得した最新リリース (失敗時は null)。</summary>
		public ReleaseInfo Release { get; private set; }

		/// <summary>インストール済みバージョンの日時。</summary>
		public DateTime InstalledVersionTime { get; private set; }

		/// <summary>最新バージョンの日時。</summary>
		public DateTime LatestVersionTime { get; private set; }

		/// <summary>失敗した理由 (成功時は null)。</summary>
		public Exception Error { get; private set; }

		/// <summary>最新バージョンの文字列 (失敗時は空文字)。</summary>
		public string LatestVersion => Release?.Version ?? string.Empty;

		internal static UpdateCheckResult CreateUpToDate(ReleaseInfo release, DateTime installedVersionTime, DateTime latestVersionTime)
		{
			return new UpdateCheckResult
			{
				Status = UpdateCheckStatus.UpToDate,
				Release = release,
				InstalledVersionTime = installedVersionTime,
				LatestVersionTime = latestVersionTime,
			};
		}

		internal static UpdateCheckResult CreateUpdateAvailable(ReleaseInfo release, DateTime installedVersionTime, DateTime latestVersionTime)
		{
			return new UpdateCheckResult
			{
				Status = UpdateCheckStatus.UpdateAvailable,
				Release = release,
				InstalledVersionTime = installedVersionTime,
				LatestVersionTime = latestVersionTime,
			};
		}

		internal static UpdateCheckResult CreateFailed(Exception error, DateTime installedVersionTime)
		{
			return new UpdateCheckResult
			{
				Status = UpdateCheckStatus.Failed,
				Error = error,
				InstalledVersionTime = installedVersionTime,
			};
		}
	}

	/// <summary>
	/// 配布元 (このフォーク) の最新リリースを確認します。
	/// バージョンの比較部分は <see cref="Evaluate"/> に分けてあり、テストから直接呼べます。
	/// </summary>
	public static class UpdateChecker
	{
		/// <summary>このフォークの最新リリースを確認します。</summary>
		public static Task<UpdateCheckResult> CheckAsync()
		{
			var source = new GitHubReleaseSource(SoftwareInformation.RepositoryOwner, SoftwareInformation.RepositoryName);
			return CheckAsync(source, SoftwareInformation.InstalledVersionTime);
		}

		/// <summary>指定した取得元で最新リリースを確認します。</summary>
		public static async Task<UpdateCheckResult> CheckAsync(IReleaseSource source, DateTime installedVersionTime)
		{
			try
			{
				ReleaseInfo release = await source.GetLatestReleaseAsync();
				return Evaluate(release, installedVersionTime);
			}
			catch (Exception ex)
			{
				return UpdateCheckResult.CreateFailed(ex, installedVersionTime);
			}
		}

		/// <summary>
		/// 取得済みのリリース情報とインストール済みバージョンを比較します。
		/// リリースの日時はタグ (例: v4.7.1-20261016) を優先し、無ければ公開日時を使います。
		/// </summary>
		public static UpdateCheckResult Evaluate(ReleaseInfo release, DateTime installedVersionTime)
		{
			if (release == null || string.IsNullOrEmpty(release.TagName))
				return UpdateCheckResult.CreateFailed(new InvalidOperationException("リリース情報を取得できませんでした。"), installedVersionTime);

			DateTime latestVersionTime = ReleaseVersion.ResolveDate(release.TagName, release.PublishedAt);

			if (latestVersionTime > installedVersionTime)
				return UpdateCheckResult.CreateUpdateAvailable(release, installedVersionTime, latestVersionTime);

			return UpdateCheckResult.CreateUpToDate(release, installedVersionTime, latestVersionTime);
		}
	}
}
