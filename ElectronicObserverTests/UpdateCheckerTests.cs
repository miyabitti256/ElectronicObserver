using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
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

		[Fact]
		public async Task CheckAsync_ReportsFailureWhenSourceThrowsHttp500()
		{
			// What: Verify HTTP 500 error from remote server is safely captured without crashing the caller.
			var source = new StubReleaseSource(new HttpRequestException("Response status code does not indicate success: 500 (Internal Server Error)."));

			UpdateCheckResult result = await UpdateChecker.CheckAsync(source, new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.Failed, result.Status);
			Assert.IsType<HttpRequestException>(result.Error);
			Assert.Contains("500", result.Error.Message);
		}

		[Fact]
		public async Task CheckAsync_ReportsFailureWhenSourceThrowsHttp404()
		{
			// What: Verify HTTP 404 (release not found or repository moved) is safely captured as failure.
			var source = new StubReleaseSource(new HttpRequestException("Response status code does not indicate success: 404 (Not Found)."));

			UpdateCheckResult result = await UpdateChecker.CheckAsync(source, new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.Failed, result.Status);
			Assert.IsType<HttpRequestException>(result.Error);
			Assert.Contains("404", result.Error.Message);
		}

		[Fact]
		public async Task CheckAsync_ReportsFailureWhenSourceThrowsSerializationException()
		{
			// What: Verify broken or unexpected JSON payload resulting in deserialization failure is safely caught.
			var source = new StubReleaseSource(new SerializationException("Unexpected end of JSON stream while deserializing ReleaseInfo."));

			UpdateCheckResult result = await UpdateChecker.CheckAsync(source, new DateTime(2026, 10, 4));

			Assert.Equal(UpdateCheckStatus.Failed, result.Status);
			Assert.IsType<SerializationException>(result.Error);
		}

		[Fact]
		public void Evaluate_ResilientToCorruptedReleaseNotesAndBody()
		{
			// What: Verify Evaluate handles null body, null name, and special characters in release note without throwing.
			var releaseWithNullBody = new ReleaseInfo
			{
				TagName = "v4.7.2-20261016",
				Name = null,
				Body = null,
				PublishedAtRaw = "2026-10-16T00:00:00Z"
			};
			var result1 = UpdateChecker.Evaluate(releaseWithNullBody, new DateTime(2026, 10, 4));
			Assert.Equal(UpdateCheckStatus.UpdateAvailable, result1.Status);
			Assert.Null(result1.Release.Body);

			var releaseWithCorruptedString = new ReleaseInfo
			{
				TagName = "v4.7.2-20261016",
				Name = "Release \0\r\n\t<script>alert('xss')</script>",
				Body = "Body with binary zero \0 and unicode surrogates \uD800\uDC00",
				PublishedAtRaw = "2026-10-16T00:00:00Z"
			};
			var result2 = UpdateChecker.Evaluate(releaseWithCorruptedString, new DateTime(2026, 10, 4));
			Assert.Equal(UpdateCheckStatus.UpdateAvailable, result2.Status);
			Assert.Contains("<script>", result2.Release.Name);
		}

		[Fact]
		public void Evaluate_ResilientToMalformedDateStrings()
		{
			// What: Verify malformed ISO string or unparseable tag date does not throw and falls back gracefully.
			var malformedRelease = new ReleaseInfo
			{
				TagName = "v-custom-build-without-date",
				PublishedAtRaw = "totally-invalid-timestamp-string"
			};

			var result = UpdateChecker.Evaluate(malformedRelease, new DateTime(2026, 10, 4));
			// Tag has no date and PublishedAt resolves to DateTime.MinValue (<= installedVersionTime)
			Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
			Assert.Equal(DateTime.MinValue, result.Release.PublishedAt);
		}

		[Fact]
		public void ReleaseInfo_DataContractJsonSerializer_ThrowsOnMalformedJson()
		{
			// What: Verify DataContractJsonSerializer throws a predictable exception on incomplete or corrupted JSON.
			const string truncatedJson = @"{ ""tag_name"": ""v4.7.2"", ""body"": ";
			var serializer = new DataContractJsonSerializer(typeof(ReleaseInfo));

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(truncatedJson)))
			{
				Assert.ThrowsAny<Exception>(() => serializer.ReadObject(stream));
			}
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
