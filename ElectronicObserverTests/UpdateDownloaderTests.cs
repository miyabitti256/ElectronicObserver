using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ElectronicObserver.Update;
using ElectronicObserver.Utility;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the background download of the update package (#23): the application must be able to
	/// download and verify the package while it keeps running, so that the restart only happens
	/// when the user asks for it.
	/// </summary>
	public class UpdateDownloaderTests : IDisposable
	{
		private readonly string _directory;

		public UpdateDownloaderTests()
		{
			_directory = Path.Combine(Path.GetTempPath(), "EOUpdateDownloaderTests-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_directory);
		}

		public void Dispose()
		{
			try
			{
				Directory.Delete(_directory, true);
			}
			catch (Exception)
			{
				// 後始末に失敗してもテスト結果には影響しない
			}
		}

		[Fact]
		public async Task DownloadAsync_DownloadsAndVerifiesThePackage()
		{
			Fixture fixture = CreateFixture("4.7.3-20261018");
			var source = new StubReleaseSource(fixture);
			var downloader = new UpdateDownloader(source, _directory);

			bool result = await downloader.DownloadAsync(fixture.Release);

			Assert.True(result);
			Assert.Equal(UpdateDownloadStatus.Ready, downloader.Status);
			Assert.True(downloader.IsReady);
			Assert.Equal("4.7.3-20261018", downloader.LatestVersion);
			Assert.Equal(fixture.ZipContent, File.ReadAllBytes(downloader.ZipPath));
			Assert.Equal(1.0, downloader.Progress);

			Assert.Contains(fixture.ZipName, source.DownloadedFileNames);
			Assert.Contains(UpdateLogic.ChecksumsAssetName, source.DownloadedFileNames);
		}

		[Fact]
		public async Task DownloadAsync_ReusesACompleteDownload()
		{
			Fixture fixture = CreateFixture("4.7.3-20261018");

			// 前回の起動でダウンロード済みの状態を作る
			File.WriteAllBytes(Path.Combine(_directory, fixture.ZipName), fixture.ZipContent);
			File.WriteAllBytes(Path.Combine(_directory, UpdateLogic.ChecksumsAssetName), Encoding.UTF8.GetBytes(fixture.Checksums));

			var source = new StubReleaseSource(fixture);
			var downloader = new UpdateDownloader(source, _directory);

			bool result = await downloader.DownloadAsync(fixture.Release);

			Assert.True(result);
			Assert.Empty(source.DownloadedFileNames);
		}

		[Fact]
		public async Task DownloadAsync_DownloadsAgainWhenTheCachedFileIsIncomplete()
		{
			Fixture fixture = CreateFixture("4.7.3-20261018");

			// 途中までしか無いファイル
			File.WriteAllBytes(Path.Combine(_directory, fixture.ZipName), new byte[16]);

			var source = new StubReleaseSource(fixture);
			var downloader = new UpdateDownloader(source, _directory);

			bool result = await downloader.DownloadAsync(fixture.Release);

			Assert.True(result);
			Assert.Equal(fixture.ZipContent, File.ReadAllBytes(downloader.ZipPath));
		}

		[Fact]
		public async Task DownloadAsync_FailsWhenTheChecksumDoesNotMatch()
		{
			Fixture fixture = CreateFixture("4.7.3-20261018", mismatch: true);

			var source = new StubReleaseSource(fixture);
			var downloader = new UpdateDownloader(source, _directory);

			bool result = await downloader.DownloadAsync(fixture.Release);

			Assert.False(result);
			Assert.Equal(UpdateDownloadStatus.Failed, downloader.Status);
			Assert.False(string.IsNullOrWhiteSpace(downloader.Error));

			// 壊れたファイルは残さない
			Assert.False(File.Exists(Path.Combine(_directory, fixture.ZipName)));
		}

		[Fact]
		public async Task DownloadAsync_FailsWhenTheReleaseHasNoPackage()
		{
			Fixture fixture = CreateFixture("4.7.3-20261018");
			fixture.Release.Assets.Clear();

			var downloader = new UpdateDownloader(new StubReleaseSource(fixture), _directory);

			bool result = await downloader.DownloadAsync(fixture.Release);

			Assert.False(result);
			Assert.Equal(UpdateDownloadStatus.Failed, downloader.Status);
		}

		[Fact]
		public async Task DownloadAsync_RemovesPackagesOfOlderVersions()
		{
			Fixture fixture = CreateFixture("4.7.3-20261018");
			string oldZip = Path.Combine(_directory, "ElectronicObserver-4.7.2-20261010.zip");
			File.WriteAllBytes(oldZip, new byte[32]);

			var downloader = new UpdateDownloader(new StubReleaseSource(fixture), _directory);

			bool result = await downloader.DownloadAsync(fixture.Release);

			Assert.True(result);
			Assert.False(File.Exists(oldZip));
			Assert.True(File.Exists(downloader.ZipPath));
		}

		private static string ComputeSha256(byte[] data)
		{
			using (var sha256 = SHA256.Create())
			{
				return string.Concat(sha256.ComputeHash(data).Select(value => value.ToString("x2")));
			}
		}

		private static Fixture CreateFixture(string version, bool mismatch = false)
		{
			string zipName = UpdateLogic.ZipAssetPrefix + version + UpdateLogic.ZipAssetExtension;
			byte[] zipContent = Encoding.UTF8.GetBytes("dummy update package: " + version);

			// mismatch の場合は checksums.txt が別の内容のハッシュを指すようにする
			byte[] checksumTarget = mismatch ? Encoding.UTF8.GetBytes("broken: " + version) : zipContent;
			string checksums = ComputeSha256(checksumTarget) + "  " + zipName + "\n";

			var release = new ReleaseInfo
			{
				TagName = "v" + version,
				PublishedAtRaw = "2026-10-18T00:00:00Z",
				Body = "変更点",
				Assets = new List<ReleaseAsset>
				{
					new ReleaseAsset
					{
						Name = UpdateLogic.ChecksumsAssetName,
						DownloadUrl = "checksums-url",
						Size = Encoding.UTF8.GetByteCount(checksums),
					},
					new ReleaseAsset
					{
						Name = zipName,
						DownloadUrl = "zip-url",
						Size = zipContent.Length,
					},
				},
			};

			return new Fixture
			{
				Release = release,
				ZipName = zipName,
				ZipContent = zipContent,
				Checksums = checksums,
			};
		}

		private class Fixture
		{
			public ReleaseInfo Release { get; set; }
			public string ZipName { get; set; }
			public byte[] ZipContent { get; set; }
			public string Checksums { get; set; }
		}

		private class StubReleaseSource : IReleaseSource
		{
			private readonly Fixture _fixture;

			public StubReleaseSource(Fixture fixture)
			{
				_fixture = fixture;
			}

			/// <summary>ダウンロードしたファイル名 (ダウンロードの有無を確かめるため)</summary>
			public List<string> DownloadedFileNames { get; } = new List<string>();

			public Task<ReleaseInfo> GetLatestReleaseAsync() => Task.FromResult(_fixture.Release);

			public Task DownloadFileAsync(string url, string destinationPath)
			{
				string name = Path.GetFileName(destinationPath);
				DownloadedFileNames.Add(name);

				switch (name)
				{
					case UpdateLogic.ChecksumsAssetName:
						File.WriteAllText(destinationPath, _fixture.Checksums);
						break;

					default:
						File.WriteAllBytes(destinationPath, _fixture.ZipContent);
						break;
				}

				return Task.CompletedTask;
			}
		}
	}
}
