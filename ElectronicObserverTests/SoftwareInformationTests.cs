using System;
using System.IO;
using ElectronicObserver.Utility;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the installed version that the update check (#13) compares against.
	/// The installed version must never be older than the compiled version, otherwise
	/// the application would repeatedly offer an update to the version it already runs.
	/// </summary>
	public class SoftwareInformationTests
	{
		[Fact]
		public void InstalledVersion_IsNeverEmpty()
		{
			Assert.False(string.IsNullOrWhiteSpace(SoftwareInformation.InstalledVersion));
		}

		[Fact]
		public void InstalledVersionTime_IsNotOlderThanTheCompiledVersion()
		{
			Assert.True(SoftwareInformation.InstalledVersionTime >= SoftwareInformation.UpdateTime);
		}

		[Fact]
		public void InstalledVersionTime_IsNotInTheFuture()
		{
			// 開発中のビルドで「未来のバージョン」になり、更新が永久に来なくなるのを防ぐ
			Assert.True(SoftwareInformation.InstalledVersionTime <= DateTime.Now.AddDays(1));
		}

		[Fact]
		public void UpdateTimeMatchesVersionEnglish()
		{
			// version.txt と同様に、バージョン文字列と更新日時は同じ日付を指している必要がある
			string[] parts = SoftwareInformation.VersionEnglish.Split(' ');

			Assert.Equal(2, parts.Length);
			Assert.Equal(SoftwareInformation.UpdateTime.ToString("yyyyMMdd"), parts[1]);
		}

		[Fact]
		public void InstalledVersion_PrefersTheVersionFileWrittenByTheUpdater()
		{
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
			bool existed = File.Exists(path);
			string backup = existed ? File.ReadAllText(path) : null;

			try
			{
				File.WriteAllText(path, "2030/01/02 0:00:00\r\n9.9.9-20300102\r\n");

				Assert.Equal("9.9.9-20300102", SoftwareInformation.InstalledVersion);
				Assert.Equal(new DateTime(2030, 1, 2), SoftwareInformation.InstalledVersionTime);
			}
			finally
			{
				if (existed)
					File.WriteAllText(path, backup);
				else
					File.Delete(path);
			}
		}

		[Fact]
		public void BaseVersion_IsNotNewerThanTheCompiledVersion()
		{
			// ベースにしている本家のバージョンが本体より新しくなっていたら、取り込み漏れか書き忘れ
			Assert.True(SoftwareInformation.BaseUpdateTime <= SoftwareInformation.UpdateTime);
		}

		[Fact]
		public void BaseVersionEnglish_MatchesItsUpdateTime()
		{
			string[] parts = SoftwareInformation.BaseVersionEnglish.Split(' ');

			Assert.Equal(2, parts.Length);
			Assert.Equal(SoftwareInformation.BaseUpdateTime.ToString("yyyyMMdd"), parts[1]);
		}

		[Fact]
		public void ForkAuthor_IsShownWithItsGitHubUrl()
		{
			// バージョン情報に改変者名と GitHub URL を出すための前提
			Assert.False(string.IsNullOrWhiteSpace(SoftwareInformation.ForkAuthorName));

			Assert.Contains(SoftwareInformation.ForkAuthorName, SoftwareInformation.RepositoryUrl);
			Assert.Equal($"https://github.com/{SoftwareInformation.ForkAuthorName}", SoftwareInformation.ForkAuthorUrl);
		}

		[Fact]
		public void InstalledVersionNotes_IsEmptyWithoutAHistory()
		{
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
			bool existed = File.Exists(path);
			string backup = existed ? File.ReadAllText(path) : null;

			try
			{
				File.WriteAllText(path, "2026/10/16 0:00:00\r\n4.7.1-20261016\r\n");

				Assert.Equal(string.Empty, SoftwareInformation.InstalledVersionNotes);
			}
			finally
			{
				if (existed)
					File.WriteAllText(path, backup);
				else
					File.Delete(path);
			}
		}

		[Fact]
		public void InstalledVersionNotes_ReturnsTheLinesAfterTheVersion()
		{
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
			bool existed = File.Exists(path);
			string backup = existed ? File.ReadAllText(path) : null;

			try
			{
				File.WriteAllText(path, "2026/10/16 0:00:00\r\n4.7.1-20261016\r\n* 変更点1\r\n* 変更点2\r\n");

				Assert.Equal("* 変更点1\r\n* 変更点2", SoftwareInformation.InstalledVersionNotes);
			}
			finally
			{
				if (existed)
					File.WriteAllText(path, backup);
				else
					File.Delete(path);
			}
		}
	}
}
