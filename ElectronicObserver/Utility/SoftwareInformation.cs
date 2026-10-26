using ElectronicObserver.Utility.Mathematics;
using System;
using System.Diagnostics;
using System.IO;

namespace ElectronicObserver.Utility
{

	/// <summary>
	/// ソフトウェアの情報を保持します。
	/// </summary>
	public static class SoftwareInformation
	{
		/// <summary>
		/// ソフトウェア名(日本語)
		/// </summary>
		public static string SoftwareNameJapanese => "七四式電子観測儀";

		/// <summary>
		/// ソフトウェア名(英語)
		/// </summary>
		public static string SoftwareNameEnglish => "ElectronicObserver";

		/// <summary>
		/// バージョン(日本語, ソフトウェア名を含みます)
		/// </summary>
		public static string VersionJapanese => SoftwareNameJapanese + "四七型改";

		/// <summary>
		/// バージョン(英語) - 本家ベースのバージョン
		/// 本家との upstream 同期でコンフリクトしないよう、このプロパティと UpdateTime は本家の定義を維持します。
		/// </summary>
		public static string VersionEnglish => "4.7.1 20261026";

		/// <summary>
		/// 更新日時 - 本家ベースの更新日時
		/// </summary>
		public static DateTime UpdateTime => DateTimeHelper.CSVStringToTime("2026/10/26 06:00:00");


		private static System.Net.WebClient client;
		private static readonly Uri uri = new("https://raw.githubusercontent.com/dais-k/ElectronicObserver/develop/ElectronicObserver/version.txt");

		public static void CheckUpdate()
		{
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

			if (!Utility.Configuration.Config.Life.CheckUpdateInformation)
				return;
			else
			{
				Utility.Configuration.Config.Life.CheckUpdateInformation = false;
				return;
			}

			if (client == null)
			{
				client = new System.Net.WebClient
				{
					Encoding = new System.Text.UTF8Encoding(false)
				};
				client.DownloadStringCompleted += DownloadStringCompleted;
			}

			if (!client.IsBusy)
				client.DownloadStringAsync(uri, "version.txt.tmp");
		}

		private static void DownloadStringCompleted(object sender, System.Net.DownloadStringCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				Utility.ErrorReporter.SendErrorReport(e.Error, "アップデート情報の取得に失敗しました。");
				return;
			}

			if (e.Result.StartsWith("<!DOCTYPE html>"))
			{
				Utility.Logger.Add(3, "アップデート情報の URI が無効です。");
				return;
			}

			try
			{
				using (var sr = new System.IO.StringReader(e.Result))
				{
					DateTime date = DateTimeHelper.CSVStringToTime(sr.ReadLine());
					string version = sr.ReadLine();
					string description = sr.ReadToEnd();

					if (UpdateTime < date)
					{
						Utility.Logger.Add(3, "新しいバージョンがリリースされています！ : " + version);

						var result = System.Windows.Forms.MessageBox.Show(
							string.Format("新しいバージョンがリリースされています！ : {0}\r\n更新内容 : \r\n{1}\r\nダウンロードページを開きますか？\r\n(キャンセルすると以降表示しません)",
							version, description),
							"アップデート情報", System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Information,
							System.Windows.Forms.MessageBoxDefaultButton.Button1);

						if (result == System.Windows.Forms.DialogResult.Yes)
						{
							System.Diagnostics.Process.Start("https://github.com/dais-k/ElectronicObserver/releases");
						}
						else if (result == System.Windows.Forms.DialogResult.Cancel)
						{
							Utility.Configuration.Config.Life.CheckUpdateInformation = false;
						}
					}
					else
					{
						Utility.Logger.Add(1, "お使いのバージョンは最新です。");
					}
				}
			}
			catch (Exception ex)
			{
				Utility.ErrorReporter.SendErrorReport(ex, "アップデート情報の処理に失敗しました。");
			}
		}


		// --- 以下、フォーク (miyabitti256 版) 独自の拡張情報 ---

		/// <summary>
		/// 配布元 (このフォーク) の GitHub オーナー名
		/// </summary>
		public const string RepositoryOwner = "miyabitti256";

		/// <summary>
		/// 配布元 (このフォーク) の GitHub リポジトリ名
		/// </summary>
		public const string RepositoryName = "ElectronicObserver";

		/// <summary>
		/// 配布元の URL
		/// </summary>
		public static string RepositoryUrl => $"https://github.com/{RepositoryOwner}/{RepositoryName}";

		/// <summary>
		/// この Fork の改変者名
		/// </summary>
		public const string ForkAuthorName = RepositoryOwner;

		/// <summary>
		/// この Fork の改変者の URL
		/// </summary>
		public static string ForkAuthorUrl => $"https://github.com/{RepositoryOwner}";

		/// <summary>
		/// ベースにしている本家 (dais-k 版) のバージョン。
		/// 本家の VersionEnglish をそのまま参照します（本家更新時に手動メンテ不要で追従させるため）。
		/// </summary>
		public static string BaseVersionEnglish => VersionEnglish;

		/// <summary>
		/// ベースにしている本家 (dais-k 版) の更新日時。
		/// 本家の UpdateTime をそのまま参照します（本家更新時に手動メンテ不要で追従させるため）。
		/// </summary>
		public static DateTime BaseUpdateTime => UpdateTime;

		/// <summary>
		/// ベースにしている本家 (dais-k 版) のリポジトリ URL
		/// </summary>
		public static string BaseRepositoryUrl => "https://github.com/dais-k/ElectronicObserver";

		/// <summary>
		/// 更新プログラムのファイル名
		/// </summary>
		public const string UpdaterFileName = "EOUpdater.exe";

		/// <summary>
		/// 実行ファイルのあるディレクトリ (末尾に区切り文字を含みます)
		/// </summary>
		public static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;

		/// <summary>
		/// フォーク版の組み込み既定バージョン
		/// </summary>
		public const string DefaultForkVersion = "4.7.6-20261026";

		/// <summary>
		/// インストール済みバージョン
		/// 更新プログラムが書き換える version.txt の 2 行目を優先し、無ければ組み込みの値を使います。
		/// </summary>
		public static string InstalledVersion
		{
			get
			{
				string[] lines = ReadVersionFileLines();

				if (lines != null && lines.Length >= 2 && !string.IsNullOrWhiteSpace(lines[1]))
					return lines[1].Trim();

				return DefaultForkVersion;
			}
		}

		/// <summary>
		/// インストール済みバージョンの日時
		/// version.txt の 1 行目と組み込みの値のうち、新しい方を使います。
		/// </summary>
		public static DateTime InstalledVersionTime
		{
			get
			{
				DateTime versionTime = UpdateTime;
				string[] lines = ReadVersionFileLines();

				if (lines != null && lines.Length >= 1)
				{
					try
					{
						DateTime fileTime = DateTimeHelper.CSVStringToTime(lines[0]);
						if (fileTime > versionTime)
							return fileTime;
					}
					catch (Exception)
					{
						// 解釈できない場合は組み込みの値を使う
					}
				}

				return versionTime;
			}
		}

		/// <summary>
		/// 更新プログラム (EOUpdater.exe) が存在するか
		/// </summary>
		public static bool IsUpdaterAvailable => File.Exists(Path.Combine(ApplicationDirectory, UpdaterFileName));

		/// <summary>
		/// 更新プログラムを起動します (更新後に本体を再起動させます)。
		/// </summary>
		/// <returns>起動できたら true</returns>
		public static bool LaunchUpdater()
		{
			string path = Path.Combine(ApplicationDirectory, UpdaterFileName);

			if (!File.Exists(path))
				return false;

			try
			{
				Process.Start(new ProcessStartInfo(path, "--restart")
				{
					WorkingDirectory = ApplicationDirectory,
					UseShellExecute = true,
				});

				Utility.Logger.Add(2, "更新プログラムを起動しました。終了後に更新されます…");
				return true;
			}
			catch (Exception ex)
			{
				Utility.ErrorReporter.SendErrorReport(ex, "更新プログラムの起動に失敗しました。");
				return false;
			}
		}

		/// <summary>
		/// version.txt を読み込みます。存在しない・読めない場合は null。
		/// </summary>
		private static string[] ReadVersionFileLines()
		{
			try
			{
				string path = Path.Combine(ApplicationDirectory, "version.txt");
				return File.Exists(path) ? File.ReadAllLines(path) : null;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
