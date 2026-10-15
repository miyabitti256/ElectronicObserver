using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ElectronicObserver.Update
{
	/// <summary>
	/// 更新処理のうち環境に依存しないロジック。
	/// テストから直接呼べるように副作用の少ない形にしている。
	/// </summary>
	public static class UpdateLogic
	{
		/// <summary>配布 zip のファイル名の先頭。</summary>
		public const string ZipAssetPrefix = "ElectronicObserver-";

		/// <summary>配布 zip のファイル名の末尾。</summary>
		public const string ZipAssetExtension = ".zip";

		/// <summary>チェックサムファイルの名前。</summary>
		public const string ChecksumsAssetName = "checksums.txt";

		/// <summary>配布 zip (ElectronicObserver-&lt;version&gt;.zip) のアセットを選ぶ。</summary>
		public static ReleaseAsset SelectZipAsset(IEnumerable<ReleaseAsset> assets)
		{
			if (assets == null) return null;

			return assets.FirstOrDefault(asset =>
				asset != null &&
				!string.IsNullOrEmpty(asset.Name) &&
				asset.Name.StartsWith(ZipAssetPrefix, StringComparison.OrdinalIgnoreCase) &&
				asset.Name.EndsWith(ZipAssetExtension, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>checksums.txt のアセットを選ぶ。</summary>
		public static ReleaseAsset SelectChecksumsAsset(IEnumerable<ReleaseAsset> assets)
		{
			if (assets == null) return null;

			return assets.FirstOrDefault(asset =>
				asset != null &&
				string.Equals(asset.Name, ChecksumsAssetName, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// sha256sum 形式 (<c>&lt;hash&gt;  &lt;file&gt;</c> / <c>&lt;hash&gt; *&lt;file&gt;</c>) のテキストから
		/// 指定ファイルのハッシュ (小文字16進) を取り出す。見つからなければ null。
		/// </summary>
		public static string ParseChecksum(string checksumsText, string fileName)
		{
			if (string.IsNullOrEmpty(checksumsText) || string.IsNullOrEmpty(fileName)) return null;

			foreach (string rawLine in checksumsText.Split('\n'))
			{
				string line = rawLine.Trim();
				if (line.Length == 0) continue;

				string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 2) continue;

				string hash = tokens[0];
				string name = string.Join(" ", tokens.Skip(1)).TrimStart('*').Trim();

				if (string.Equals(Path.GetFileName(name), fileName, StringComparison.OrdinalIgnoreCase) && IsSha256(hash))
					return hash.ToLowerInvariant();
			}

			return null;
		}

		/// <summary>SHA-256 のハッシュ文字列 (16 進 64 桁) かどうか。</summary>
		public static bool IsSha256(string value)
			=> !string.IsNullOrEmpty(value) && value.Length == 64 && value.All(Uri.IsHexDigit);

		/// <summary>ファイルの SHA-256 (小文字16進) を計算する。</summary>
		public static string ComputeSha256(string path)
		{
			using (var sha256 = SHA256.Create())
			using (var stream = File.OpenRead(path))
			{
				byte[] hash = sha256.ComputeHash(stream);
				var builder = new StringBuilder(hash.Length * 2);

				foreach (byte value in hash)
					builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));

				return builder.ToString();
			}
		}

		/// <summary>ハッシュが一致するか。</summary>
		public static bool VerifyChecksum(string path, string expectedHash)
			=> IsSha256(expectedHash) && string.Equals(ComputeSha256(path), expectedHash, StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// 更新時に削除するファイル (トップレベルの *.exe / *.dll)。
		/// 実行中の自分自身は削除・上書きできないため除外する。
		/// </summary>
		public static IReadOnlyList<string> GetFilesToDelete(string directory, string selfPath)
		{
			string self = string.IsNullOrEmpty(selfPath) ? null : Path.GetFullPath(selfPath);
			var targets = new List<string>();

			foreach (string pattern in new[] { "*.exe", "*.dll" })
			{
				foreach (string file in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
				{
					if (self != null && string.Equals(Path.GetFullPath(file), self, StringComparison.OrdinalIgnoreCase))
						continue;

					targets.Add(file);
				}
			}

			return targets;
		}

		/// <summary>zip のエントリが展開先ディレクトリの外を指していないか (zip slip 対策)。</summary>
		public static bool IsEntryInsideDirectory(string baseDirectory, string entryName)
		{
			if (string.IsNullOrEmpty(baseDirectory) || string.IsNullOrEmpty(entryName)) return false;

			string baseFull = Path.GetFullPath(baseDirectory);
			if (!baseFull.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				baseFull += Path.DirectorySeparatorChar;

			string target;
			try
			{
				target = Path.GetFullPath(Path.Combine(baseFull, entryName));
			}
			catch (Exception)
			{
				return false;
			}

			return target.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>更新後に書き込む version.txt の内容を作る。</summary>
		public static string BuildVersionFileContent(ReleaseInfo release)
		{
			if (release == null) return null;

			var builder = new StringBuilder();
			DateTime date = ReleaseVersion.ResolveDate(release.TagName, release.PublishedAt);

			builder.AppendLine(date.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture));
			builder.AppendLine(release.Version);

			if (!string.IsNullOrWhiteSpace(release.Body))
				builder.AppendLine(release.Body.Trim());

			return builder.ToString();
		}
	}
}
