using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElectronicObserver.Update
{
	/// <summary>GitHub Release に添付されたアセット。</summary>
	[DataContract]
	public class ReleaseAsset
	{
		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "browser_download_url")]
		public string DownloadUrl { get; set; }

		[DataMember(Name = "size")]
		public long Size { get; set; }
	}

	/// <summary>GitHub Release の情報。</summary>
	[DataContract]
	public class ReleaseInfo
	{
		[DataMember(Name = "tag_name")]
		public string TagName { get; set; }

		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "body")]
		public string Body { get; set; }

		/// <summary>ISO 8601 形式の公開日時。DateTime の JSON 解釈に依存しないよう文字列で受ける。</summary>
		[DataMember(Name = "published_at")]
		public string PublishedAtRaw { get; set; }

		[DataMember(Name = "assets")]
		public List<ReleaseAsset> Assets { get; set; }

		/// <summary>タグから先頭の "v" を除いたバージョン文字列。</summary>
		public string Version => ReleaseVersion.Normalize(TagName);

		/// <summary>公開日時 (UTC)。取得できない場合は DateTime.MinValue。</summary>
		public DateTime PublishedAt
		{
			get
			{
				if (string.IsNullOrEmpty(PublishedAtRaw)) return DateTime.MinValue;

				if (DateTime.TryParse(PublishedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value))
					return value;

				return DateTime.MinValue;
			}
		}
	}

	/// <summary>GitHub Release の取得とダウンロード (テストで差し替えられるように抽象化)。</summary>
	public interface IReleaseSource
	{
		Task<ReleaseInfo> GetLatestReleaseAsync();

		Task DownloadFileAsync(string url, string destinationPath);
	}

	/// <summary>GitHub Releases API から最新リリースを取得する。</summary>
	public class GitHubReleaseSource : IReleaseSource
	{
		private const string ApiBaseUrl = "https://api.github.com";

		private readonly string _owner;
		private readonly string _repository;
		private readonly HttpClient _client;

		public GitHubReleaseSource(string owner, string repository)
		{
			_owner = owner;
			_repository = repository;

			_client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
			_client.DefaultRequestHeaders.UserAgent.ParseAdd("ElectronicObserver-Updater");
			_client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		}

		public async Task<ReleaseInfo> GetLatestReleaseAsync()
		{
			string url = $"{ApiBaseUrl}/repos/{_owner}/{_repository}/releases/latest";

			using (var response = await _client.GetAsync(url).ConfigureAwait(false))
			{
				response.EnsureSuccessStatusCode();

				using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
				{
					var serializer = new DataContractJsonSerializer(typeof(ReleaseInfo));
					return (ReleaseInfo)serializer.ReadObject(stream);
				}
			}
		}

		public async Task DownloadFileAsync(string url, string destinationPath)
		{
			using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
			{
				response.EnsureSuccessStatusCode();

				using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
				using (var destination = File.Create(destinationPath))
				{
					await source.CopyToAsync(destination).ConfigureAwait(false);
				}
			}
		}
	}

	/// <summary>バージョン文字列 (リリースタグ) の解釈。</summary>
	public static class ReleaseVersion
	{
		/// <summary>タグから先頭の "v" を除く。</summary>
		public static string Normalize(string tagName)
		{
			if (string.IsNullOrEmpty(tagName)) return string.Empty;
			return tagName.TrimStart('v', 'V');
		}

		/// <summary>バージョン文字列に含まれる日付 (YYYYMMDD) を取り出す。無ければ null。</summary>
		public static DateTime? ParseDate(string version)
		{
			if (string.IsNullOrEmpty(version)) return null;

			var match = Regex.Match(version, @"(\d{8})(?!.*\d)");
			if (!match.Success) return null;

			if (DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
				return date;

			return null;
		}

		/// <summary>リリースの日時。タグの日付を優先し、無ければ公開日時を使う。</summary>
		public static DateTime ResolveDate(string tagName, DateTime publishedAt)
			=> ParseDate(tagName) ?? publishedAt;
	}
}
