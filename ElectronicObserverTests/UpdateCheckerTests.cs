using System;
using System.Threading.Tasks;
using ElectronicObserver.Update;
using ElectronicObserver.Utility;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the update check of the application (#13): the installed version is
	/// compared with the release tag (falling back to the publish date), and network
	/// failures must be reported instead of throwing.
	/// </summary>
	public class UpdateCheckerTests
	{
		private static ReleaseInfo CreateRelease(string tagName, string publishedAt = null)
			=> new ReleaseInfo { TagName = tagName, PublishedAtRaw = publishedAt, Body = "変更点" };

		[Fact]
		public void Evaluate_ReportsUpdateAvailableWhenTagIsNewer()
		{
			UpdateCheckResult result = UpdateChecker.Evaluate(CreateRelease("v4.7.2-20261016"), new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
			Assert.Equal(new DateTime(2026, 10, 16), result.LatestVersionTime);
			Assert.Equal("4.7.2-20261016", result.LatestVersion);
			Assert.Equal(new DateTime(2026, 10, 4), result.InstalledVersionTime);
		}

		[Fact]
		public void Evaluate_ReportsUpToDateForSameVersion()
		{
			UpdateCheckResult result = UpdateChecker.Evaluate(CreateRelease("v4.7.1-20261004"), new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
		}

		[Fact]
		public void Evaluate_ReportsUpToDateForOlderVersion()
		{
			UpdateCheckResult result = UpdateChecker.Evaluate(CreateRelease("v4.7.0-20260901"), new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
		}

		[Fact]
		public void Evaluate_UsesPublishedAtWhenTagHasNoDate()
		{
			UpdateCheckResult result = UpdateChecker.Evaluate(CreateRelease("v4.7.2", "2026-10-16T00:00:00Z"), new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
			Assert.Equal(new DateTime(2026, 10, 16), result.LatestVersionTime);
		}

		[Fact]
		public void Evaluate_ReportsFailureWhenReleaseIsMissing()
		{
			UpdateCheckResult result = UpdateChecker.Evaluate(null, new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.Failed, result.Status);
			Assert.NotNull(result.Error);
		}

		[Fact]
		public void Evaluate_ReportsFailureWhenTagIsEmpty()
		{
			UpdateCheckResult result = UpdateChecker.Evaluate(CreateRelease(""), new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.Failed, result.Status);
		}

		[Fact]
		public async Task CheckAsync_ReturnsUpdateAvailableFromSource()
		{
			var source = new StubReleaseSource(CreateRelease("v4.7.2-20261016"));

			UpdateCheckResult result = await UpdateChecker.CheckAsync(source, new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
		}

		[Fact]
		public async Task CheckAsync_ReportsFailureWhenSourceThrows()
		{
			var source = new StubReleaseSource(new InvalidOperationException("network"));

			UpdateCheckResult result = await UpdateChecker.CheckAsync(source, new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.Failed, result.Status);
			Assert.IsType<InvalidOperationException>(result.Error);
		}

		private class StubReleaseSource : IReleaseSource
		{
			private readonly ReleaseInfo _release;
			private readonly Exception _error;

			public StubReleaseSource(ReleaseInfo release) => _release = release;

			public StubReleaseSource(Exception error) => _error = error;

			public Task<ReleaseInfo> GetLatestReleaseAsync()
				=> _error != null ? Task.FromException<ReleaseInfo>(_error) : Task.FromResult(_release);

			public Task DownloadFileAsync(string url, string destinationPath) => Task.CompletedTask;
		}
	}
}
