using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElectronicObserver.Update;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the pure parts of the update program (#13): asset selection, checksum
	/// verification, the file list that may be replaced, zip-slip protection and the
	/// interpretation of release tags.
	/// </summary>
	public class UpdateLogicTests
	{
		private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

		private static string CreateTempDirectory()
		{
			string path = Path.Combine(Path.GetTempPath(), "EOUpdaterTests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		// --- アセットの選択 ---

		[Fact]
		public void SelectZipAsset_PicksReleaseZip()
		{
			var assets = new List<ReleaseAsset>
			{
				new ReleaseAsset { Name = "checksums.txt", DownloadUrl = "checksums" },
				new ReleaseAsset { Name = "ElectronicObserver-20261016.zip", DownloadUrl = "zip" },
			};

			ReleaseAsset selected = UpdateLogic.SelectZipAsset(assets);

			Assert.NotNull(selected);
			Assert.Equal("ElectronicObserver-20261016.zip", selected.Name);
			Assert.Equal("zip", selected.DownloadUrl);
		}

		[Fact]
		public void SelectZipAsset_IgnoresUnrelatedArchives()
		{
			var assets = new List<ReleaseAsset>
			{
				new ReleaseAsset { Name = "Assets.zip", DownloadUrl = "assets" },
				new ReleaseAsset { Name = "ElectronicObserver-20261016.7z", DownloadUrl = "7z" },
			};

			Assert.Null(UpdateLogic.SelectZipAsset(assets));
		}

		[Fact]
		public void SelectChecksumsAsset_MatchesNameCaseInsensitively()
		{
			var assets = new List<ReleaseAsset>
			{
				new ReleaseAsset { Name = "ElectronicObserver-20261016.zip", DownloadUrl = "zip" },
				new ReleaseAsset { Name = "Checksums.TXT", DownloadUrl = "checksums" },
			};

			ReleaseAsset selected = UpdateLogic.SelectChecksumsAsset(assets);

			Assert.NotNull(selected);
			Assert.Equal("checksums", selected.DownloadUrl);
		}

		// --- チェックサム ---

		[Fact]
		public void ParseChecksum_ReadsSha256SumFormat()
		{
			string text = $"{new string('b', 64)}  checksums.txt\r\n{Hash}  ElectronicObserver-20261016.zip\r\n";

			Assert.Equal(Hash, UpdateLogic.ParseChecksum(text, "ElectronicObserver-20261016.zip"));
		}

		[Fact]
		public void ParseChecksum_SupportsBinaryMarkerAndUpperCase()
		{
			string text = $"{Hash.ToUpperInvariant()} *ELECTRONICOBSERVER-20261016.ZIP\n";

			Assert.Equal(Hash, UpdateLogic.ParseChecksum(text, "ElectronicObserver-20261016.zip"));
		}

		[Fact]
		public void ParseChecksum_ReturnsNullWhenEntryIsMissing()
		{
			Assert.Null(UpdateLogic.ParseChecksum($"{Hash}  other.zip\n", "ElectronicObserver-20261016.zip"));
		}

		[Fact]
		public void ParseChecksum_RejectsMalformedHash()
		{
			Assert.Null(UpdateLogic.ParseChecksum($"notahash  sample.zip\n", "sample.zip"));
			Assert.Null(UpdateLogic.ParseChecksum($"{new string('a', 63)}  sample.zip\n", "sample.zip"));
			Assert.Null(UpdateLogic.ParseChecksum("", "sample.zip"));
		}

		[Fact]
		public void VerifyChecksum_ComparesAgainstComputedHash()
		{
			string directory = CreateTempDirectory();

			try
			{
				string file = Path.Combine(directory, "sample.bin");
				File.WriteAllText(file, "ElectronicObserver");

				string hash = UpdateLogic.ComputeSha256(file);

				Assert.True(UpdateLogic.IsSha256(hash));
				Assert.True(UpdateLogic.VerifyChecksum(file, hash));
				Assert.True(UpdateLogic.VerifyChecksum(file, hash.ToUpperInvariant()));
				Assert.False(UpdateLogic.VerifyChecksum(file, new string('0', 64)));
				Assert.False(UpdateLogic.VerifyChecksum(file, null));
			}
			finally
			{
				Directory.Delete(directory, true);
			}
		}

		// --- 置き換えるファイル ---

		[Fact]
		public void GetFilesToDelete_TargetsTopLevelExeAndDllExceptSelf()
		{
			string directory = CreateTempDirectory();

			try
			{
				foreach (string name in new[]
				{
					"ElectronicObserver.exe",
					"EOUpdater.exe",
					"EOBrowser.exe",
					"WeifenLuo.WinFormsUI.Docking.dll",
					"EOUpdate.dll",
					"version.txt",
					"ElectronicObserver.exe.config",
					"ElectronicObserver.pdb",
				})
				{
					File.WriteAllText(Path.Combine(directory, name), "");
				}

				Directory.CreateDirectory(Path.Combine(directory, "x86"));
				File.WriteAllText(Path.Combine(directory, "x86", "libcef.dll"), "");

				string self = Path.Combine(directory, "EOUpdater.exe");

				string[] targets = UpdateLogic.GetFilesToDelete(directory, self)
					.Select(Path.GetFileName)
					.OrderBy(name => name, StringComparer.Ordinal)
					.ToArray();

				Assert.Equal(
					new[] { "EOBrowser.exe", "EOUpdate.dll", "ElectronicObserver.exe", "WeifenLuo.WinFormsUI.Docking.dll" },
					targets);
			}
			finally
			{
				Directory.Delete(directory, true);
			}
		}

		// --- zip slip 対策 ---

		[Theory]
		[InlineData("ElectronicObserver.exe", true)]
		[InlineData("x64/libcef.dll", true)]
		[InlineData("x64\\libcef.dll", true)]
		[InlineData("../evil.exe", false)]
		[InlineData("..\\evil.exe", false)]
		[InlineData("x64/../../evil.exe", false)]
		[InlineData("", false)]
		public void IsEntryInsideDirectory_RejectsEscapingEntries(string entryName, bool expected)
		{
			string baseDirectory = Path.Combine(Path.GetTempPath(), "eoupdater-base");

			Assert.Equal(expected, UpdateLogic.IsEntryInsideDirectory(baseDirectory, entryName));
		}

		[Fact]
		public void IsEntryInsideDirectory_RejectsAbsolutePath()
		{
			string baseDirectory = Path.Combine(Path.GetTempPath(), "eoupdater-base");

			Assert.False(UpdateLogic.IsEntryInsideDirectory(baseDirectory, @"C:\Windows\System32\evil.exe"));
		}

		// --- バージョン / リリース情報 ---

		[Fact]
		public void Normalize_RemovesLeadingV()
		{
			Assert.Equal("4.7.1-20261016", ReleaseVersion.Normalize("v4.7.1-20261016"));
			Assert.Equal("4.7.1-20261016", ReleaseVersion.Normalize("V4.7.1-20261016"));
			Assert.Equal("", ReleaseVersion.Normalize(null));
		}

		[Fact]
		public void ParseDate_ReadsDateSuffixFromTag()
		{
			Assert.Equal(new DateTime(2026, 10, 16), ReleaseVersion.ParseDate("v4.7.1-20261016"));
			Assert.Equal(new DateTime(2026, 10, 16), ReleaseVersion.ParseDate("20261016"));
		}

		[Fact]
		public void ParseDate_ReturnsNullWhenNoValidDate()
		{
			Assert.Null(ReleaseVersion.ParseDate("v4.7.1"));
			Assert.Null(ReleaseVersion.ParseDate("v4.7.1-20261332"));
			Assert.Null(ReleaseVersion.ParseDate(null));
		}

		[Fact]
		public void ResolveDate_PrefersTagDateAndFallsBackToPublishedAt()
		{
			var published = new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

			Assert.Equal(new DateTime(2026, 10, 16), ReleaseVersion.ResolveDate("v4.7.1-20261016", published));
			Assert.Equal(published, ReleaseVersion.ResolveDate("v4.7.1", published));
		}

		[Fact]
		public void ReleaseInfo_ParsesPublishedAtFromIso8601()
		{
			var release = new ReleaseInfo { TagName = "v4.7.1-20261016", PublishedAtRaw = "2026-10-16T12:34:56Z" };

			Assert.Equal(new DateTime(2026, 10, 16, 12, 34, 56), release.PublishedAt);
			Assert.Equal("4.7.1-20261016", release.Version);
		}

		[Fact]
		public void ReleaseInfo_UsesMinValueWhenPublishedAtIsMissing()
		{
			var release = new ReleaseInfo { TagName = "v4.7.1-20261016" };

			Assert.Equal(DateTime.MinValue, release.PublishedAt);
		}

		[Fact]
		public void BuildVersionFileContent_WritesDateAndVersion()
		{
			var release = new ReleaseInfo { TagName = "v4.7.1-20261016", Body = "変更点" };

			string[] lines = UpdateLogic.BuildVersionFileContent(release)
				.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

			Assert.Equal("2026/10/16 00:00:00", lines[0]);
			Assert.Equal("4.7.1-20261016", lines[1]);
			Assert.Equal("変更点", lines[2]);
		}

		// --- バックグラウンドダウンロード (#23) ---

		[Fact]
		public void GetDownloadDirectory_PointsToTheSharedCache()
		{
			string directory = UpdateLogic.GetDownloadDirectory();

			Assert.True(Path.IsPathRooted(directory));
			Assert.EndsWith("Update", directory.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal);
			Assert.Contains("ElectronicObserver", directory, StringComparison.Ordinal);
		}

		[Fact]
		public void IsDownloadedFileComplete_ComparesTheSize()
		{
			string directory = CreateTempDirectory();

			try
			{
				string path = Path.Combine(directory, "ElectronicObserver-4.7.3-20261018.zip");

				Assert.False(UpdateLogic.IsDownloadedFileComplete(path, 8));

				File.WriteAllBytes(path, new byte[8]);

				Assert.True(UpdateLogic.IsDownloadedFileComplete(path, 8));
				Assert.False(UpdateLogic.IsDownloadedFileComplete(path, 9));

				// サイズが分からない場合は存在すれば使えるとみなす
				Assert.True(UpdateLogic.IsDownloadedFileComplete(path, 0));
			}
			finally
			{
				Directory.Delete(directory, true);
			}
		}

		[Fact]
		public void CanReuseChecksumsFile_RequiresTheEntryForThePackage()
		{
			string directory = CreateTempDirectory();

			try
			{
				string fileName = "ElectronicObserver-4.7.3-20261018.zip";
				string path = Path.Combine(directory, UpdateLogic.ChecksumsAssetName);
				string text = Hash + "  ElectronicObserver-4.7.2-20261017.zip\n";

				File.WriteAllText(path, text);
				long size = new FileInfo(path).Length;

				// 別のバージョンのハッシュしか載っていないので使えない
				Assert.False(UpdateLogic.CanReuseChecksumsFile(path, fileName, size));

				File.WriteAllText(path, text + Hash + "  " + fileName + "\n");

				// サイズが合っていても中身が違えば使わないので、サイズを指定して確認する
				Assert.False(UpdateLogic.CanReuseChecksumsFile(path, fileName, size));
				Assert.True(UpdateLogic.CanReuseChecksumsFile(path, fileName, new FileInfo(path).Length));

				// 壊れている / 無い場合は使わない
				File.WriteAllText(path, "こわれています\n");
				Assert.False(UpdateLogic.CanReuseChecksumsFile(path, fileName, 0));
				Assert.False(UpdateLogic.CanReuseChecksumsFile(Path.Combine(directory, "missing.txt"), fileName, 0));
			}
			finally
			{
				Directory.Delete(directory, true);
			}
		}

		[Fact]
		public void GetObsoleteZipPaths_ReturnsOnlyOtherPackages()
		{
			string directory = CreateTempDirectory();

			try
			{
				string current = Path.Combine(directory, "ElectronicObserver-4.7.3-20261018.zip");
				string older = Path.Combine(directory, "ElectronicObserver-4.7.2-20261010.zip");

				File.WriteAllBytes(current, new byte[1]);
				File.WriteAllBytes(older, new byte[1]);
				File.WriteAllText(Path.Combine(directory, UpdateLogic.ChecksumsAssetName), "");

				string[] targets = UpdateLogic.GetObsoleteZipPaths(directory, current)
					.Select(Path.GetFileName)
					.ToArray();

				Assert.Equal(new[] { Path.GetFileName(older) }, targets);
			}
			finally
			{
				Directory.Delete(directory, true);
			}
		}
	}
}
