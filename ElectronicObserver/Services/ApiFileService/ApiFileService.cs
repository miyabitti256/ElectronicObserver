#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using ElectronicObserver.Data;
using ElectronicObserver.Database;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;

namespace ElectronicObserver.Services.ApiFileService
{
	public class ApiFileService
	{
		public static int CurrentApiFileVersion => 1;
		public static int CurrentSortieRecordVersion => 1;

		private static readonly HashSet<string> AlwaysSavedApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"api_start2/getData",
			"api_port/port",
			"api_get_member/ship2",
			"api_get_member/slot_item",
			"api_req_mission/start",
			"api_req_mission/result",
			"api_req_mission/return_instruction",
		};

		private static readonly HashSet<string> IgnoredApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"api_get_member/questlist",
			"api_get_member/unsetslot",
			"api_get_member/ship3",
			"api_get_member/furniture",
			"api_get_member/require_info",
		};

		private readonly Func<ElectronicObserverDbContext> _contextFactory;
		private readonly BlockingCollection<ApiFileData> _apiProcessingQueue;
		private readonly ConcurrentQueue<int> _sortieIdsToProcess = new ConcurrentQueue<int>();

		public KCDatabase KcDatabase { get; }
		public int? CurrentSortieId { get; private set; }

		public ApiFileService(KCDatabase? db = null, Func<ElectronicObserverDbContext>? contextFactory = null)
		{
			KcDatabase = db ?? KCDatabase.Instance;
			_contextFactory = contextFactory ?? (() => new ElectronicObserverDbContext());

			_apiProcessingQueue = new BlockingCollection<ApiFileData>();

			Task.Run((Action)ProcessApiData);
		}

		private void ProcessApiData()
		{
			foreach (var apiFile in _apiProcessingQueue.GetConsumingEnumerable())
			{
				try
				{
					SaveApiData(apiFile.ApiName, apiFile.RequestBody, apiFile.ResponseBody).GetAwaiter().GetResult();
				}
				catch (Exception e)
				{
					Utility.Logger.Add(3, $"Api file saving error: {e.Message} {e.StackTrace}");
				}
			}
		}

		public Task Add(string apiName, string requestBody, string responseBody)
		{
			_apiProcessingQueue.Add(new ApiFileData(apiName, requestBody, responseBody));
			return Task.CompletedTask;
		}

		public async Task SaveApiData(string apiName, string requestBody, string responseBody)
		{
			if (ShouldIgnoreApi(apiName)) return;

			using (var db = _contextFactory())
			{
				requestBody = FormatRequest(requestBody);
				responseBody = TrimSvdata(responseBody);
				responseBody = TrimPort(apiName, responseBody);

				var requestFile = new ApiFile
				{
					ApiFileType = ApiFileType.Request,
					Name = apiName,
					Content = requestBody,
					TimeStamp = DateTime.UtcNow,
					Version = CurrentApiFileVersion,
				};

				var responseFile = new ApiFile
				{
					ApiFileType = ApiFileType.Response,
					Name = apiName,
					Content = responseBody,
					TimeStamp = DateTime.UtcNow,
					Version = CurrentApiFileVersion,
				};

				await ProcessSortieData(db, requestFile, responseFile).ConfigureAwait(false);

				await db.SaveChangesAsync().ConfigureAwait(false);
			}
		}

		private bool ShouldIgnoreApi(string apiName)
		{
			if (CurrentSortieId.HasValue)
			{
				return false;
			}

			if (AlwaysSavedApis.Contains(apiName))
			{
				return false;
			}

			if (apiName == "api_req_map/start")
			{
				return false;
			}

			return true;
		}

		private async Task ProcessSortieData(ElectronicObserverDbContext db, ApiFile requestFile, ApiFile responseFile)
		{
			if (requestFile.Name == "api_req_map/start")
			{
				await CreateNewSortie(db, requestFile, responseFile).ConfigureAwait(false);
				return;
			}

			if (requestFile.Name == "api_port/port")
			{
				if (CurrentSortieId is int sortieId)
				{
					_sortieIdsToProcess.Enqueue(sortieId);
					requestFile.SortieRecordId = sortieId;
					responseFile.SortieRecordId = sortieId;
				}

				CurrentSortieId = null;

				await db.ApiFiles.AddAsync(requestFile).ConfigureAwait(false);
				await db.ApiFiles.AddAsync(responseFile).ConfigureAwait(false);
				return;
			}

			if (CurrentSortieId.HasValue)
			{
				int id = CurrentSortieId.Value;
				requestFile.SortieRecordId = id;
				responseFile.SortieRecordId = id;
			}

			await db.ApiFiles.AddAsync(requestFile).ConfigureAwait(false);
			await db.ApiFiles.AddAsync(responseFile).ConfigureAwait(false);
		}

		private async Task CreateNewSortie(ElectronicObserverDbContext db, ApiFile requestFile, ApiFile responseFile)
		{
			int fleetId = 1;
			int world = 0;
			int map = 0;

			try
			{
				using (var reqDoc = JsonDocument.Parse(requestFile.Content))
				{
					if (reqDoc.RootElement.TryGetProperty("api_deck_id", out var deckProp))
					{
						int.TryParse(deckProp.GetString(), out fleetId);
					}
				}

				using (var resDoc = JsonDocument.Parse(responseFile.Content))
				{
					if (resDoc.RootElement.TryGetProperty("api_data", out var apiData))
					{
						if (apiData.TryGetProperty("api_maparea_id", out var areaProp))
						{
							world = areaProp.GetInt32();
						}
						if (apiData.TryGetProperty("api_mapinfo_no", out var infoProp))
						{
							map = infoProp.GetInt32();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Utility.Logger.Add(2, "api_req_map/start パースエラー: " + ex.Message);
			}

			SortieMapData mapData = SortieSnapshotFactory.CreateMapData(KcDatabase, world, map);
			SortieFleetData fleetData = SortieSnapshotFactory.CreateFleetData(KcDatabase, fleetId, world);

			var sortieRecord = new SortieRecord
			{
				Version = CurrentSortieRecordVersion,
				World = world,
				Map = map,
				FleetData = fleetData,
				MapData = mapData,
			};

			await db.Sorties.AddAsync(sortieRecord).ConfigureAwait(false);
			await db.SaveChangesAsync().ConfigureAwait(false);

			CurrentSortieId = sortieRecord.Id;

			requestFile.SortieRecordId = sortieRecord.Id;
			responseFile.SortieRecordId = sortieRecord.Id;

			await db.ApiFiles.AddAsync(requestFile).ConfigureAwait(false);
			await db.ApiFiles.AddAsync(responseFile).ConfigureAwait(false);
		}

		public async Task ProcessedApi(string apiName)
		{
			if (apiName != "api_port/port") return;
			if (!_sortieIdsToProcess.TryDequeue(out int sortieId)) return;

			using (var db = _contextFactory())
			{
				var sortie = await db.Sorties.FindAsync(sortieId, includeApiFiles: false).ConfigureAwait(false);
				if (sortie == null) return;

				sortie.FleetAfterSortieData = SortieSnapshotFactory.CreateFleetAfterSortieData(KcDatabase, sortie);
				await db.Sorties.UpdateAsync(sortie).ConfigureAwait(false);
				await db.SaveChangesAsync().ConfigureAwait(false);
			}
		}

		public static string FormatRequest(string requestBody)
		{
			if (string.IsNullOrEmpty(requestBody)) return "{}";
			if (requestBody.TrimStart().StartsWith("{")) return requestBody;

			try
			{
				NameValueCollection query = HttpUtility.ParseQueryString(requestBody);
				var dictionary = query.AllKeys
					.Where(k => k != null)
					.ToDictionary(k => k!, k => query[k]!);

				return JsonSerializer.Serialize(dictionary);
			}
			catch
			{
				return requestBody;
			}
		}

		public static string FormatRequestBackToQueryString(string content)
		{
			if (string.IsNullOrEmpty(content)) return "";
			if (!content.TrimStart().StartsWith("{")) return content;

			try
			{
				var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
				if (dict == null) return content;

				var pairs = dict.Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}");
				return string.Join("&", pairs);
			}
			catch
			{
				return content;
			}
		}

		public static string TrimSvdata(string responseBody)
		{
			if (string.IsNullOrEmpty(responseBody)) return "";
			if (responseBody.StartsWith("svdata="))
			{
				return responseBody.Substring(7);
			}
			return responseBody;
		}

		public static string TrimPort(string apiName, string responseBody)
		{
			if (apiName != "api_port/port") return responseBody;

			try
			{
				JsonNode? portResponse = JsonNode.Parse(responseBody);
				if (portResponse?["api_data"] != null)
				{
					portResponse["api_data"]!["api_ship"] = new JsonArray();
					portResponse["api_data"]!["api_deck_port"] = new JsonArray();
					portResponse["api_data"]!["api_log"] = new JsonArray();
					portResponse["api_data"]!["api_ndock"] = new JsonArray();

					return JsonSerializer.Serialize(portResponse);
				}
			}
			catch
			{
				// ignored
			}

			return responseBody;
		}

		public static void ExportToApiList(string destinationDirectory, IEnumerable<ApiFile> apiFiles, string listFileName = "APIList.txt")
		{
			if (string.IsNullOrEmpty(destinationDirectory))
			{
				throw new ArgumentNullException(nameof(destinationDirectory));
			}

			if (!Directory.Exists(destinationDirectory))
			{
				Directory.CreateDirectory(destinationDirectory);
			}

			var sortedFiles = (apiFiles ?? Enumerable.Empty<ApiFile>())
				.OrderBy(f => f.TimeStamp)
				.ThenBy(f => f.ApiFileType == ApiFileType.Request ? 0 : 1)
				.ThenBy(f => f.Id)
				.ToList();

			var listLines = new List<string>();

			foreach (var file in sortedFiles)
			{
				string timeStampStr = file.TimeStamp.ToLocalTime().ToString("yyyyMMdd_HHmmssfff");
				string typeChar = file.ApiFileType == ApiFileType.Request ? "Q" : "S";
				string safeApiName = (file.Name ?? "").Replace('/', '@');
				string fileName = $"{timeStampStr}{typeChar}@{safeApiName}.json";
				string filePath = Path.Combine(destinationDirectory, fileName);

				string contentToWrite;
				if (file.ApiFileType == ApiFileType.Response)
				{
					contentToWrite = file.Content?.StartsWith("svdata=") == true
						? file.Content
						: "svdata=" + (file.Content ?? "");
				}
				else
				{
					contentToWrite = FormatRequestBackToQueryString(file.Content ?? "");
				}

				File.WriteAllText(filePath, contentToWrite, Encoding.UTF8);

				string logLine = $"{timeStampStr} : [{(file.ApiFileType == ApiFileType.Request ? "Request" : "Response")}] /kcsapi/{file.Name}";
				listLines.Add(logLine);
			}

			string listFilePath = Path.Combine(destinationDirectory, listFileName);
			File.WriteAllLines(listFilePath, listLines, Encoding.UTF8);
		}

		public async Task ExportSortieToApiListAsync(int sortieId, string destinationDirectory, string listFileName = "APIList.txt")
		{
			using (var db = _contextFactory())
			{
				var apiFiles = await db.ApiFiles.GetBySortieIdAsync(sortieId).ConfigureAwait(false);
				ExportToApiList(destinationDirectory, apiFiles, listFileName);
			}
		}
	}
}
