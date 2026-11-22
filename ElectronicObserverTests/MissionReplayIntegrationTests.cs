#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ElectronicObserver.Data;
using ElectronicObserver.Database;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Observer;
using ElectronicObserver.Services.ApiFileService;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// 実通信パケット（Brotli圧縮フィクスチャ）を用いたリプレイおよび遠征可否判定の統合テスト。
	/// 出撃外パケット（マスタデータ、母港、装備、アイテム、遠征）の SQLite 永続化（SortieRecordId == null）、
	/// APIList エクスポート、APIObserver による KCDatabase 復元、復元艦隊に対する遠征判定スナップショットを検証します。
	/// </summary>
	[Collection("KCDatabase")]
	public class MissionReplayIntegrationTests
	{
		#region Fixture Data Structure

		public class ReplayPacketItem
		{
			public string FileName { get; set; } = "";
			public string ApiName { get; set; } = "";
			public int FileType { get; set; }
			public string Content { get; set; } = "";
		}

		private static string ResolveFixturePath()
		{
			string[] candidates = new[]
			{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "mission_replay_pack.json.br"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Fixtures", "mission_replay_pack.json.br"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fixtures", "mission_replay_pack.json.br"),
				@"d:\apps\EO\ElectronicObserver\ElectronicObserverTests\Fixtures\mission_replay_pack.json.br"
			};

			foreach (var path in candidates)
			{
				if (File.Exists(path))
				{
					return Path.GetFullPath(path);
				}
			}

			throw new FileNotFoundException("mission_replay_pack.json.br was not found in candidate paths.");
		}

		private static List<ReplayPacketItem> LoadFixturePack()
		{
			string path = ResolveFixturePath();
			byte[] compressed = File.ReadAllBytes(path);
			string json = ElectronicObserverDbContext.DecompressContent(compressed);
			var list = JsonSerializer.Deserialize<List<ReplayPacketItem>>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			return list ?? throw new InvalidOperationException("Failed to deserialize mission replay fixture.");
		}

		#endregion

		#region E2E Replay Tests

		[Fact]
		public async Task MissionReplay_ApiFileService_SavesMasterAndPortApis_WithNullSortieRecordId()
		{
			// What: Verify non-sortie packets (start2, port, slot_item, useitem, mission) are stored
			//       with SortieRecordId == null and Brotli compression in SQLite.
			string tempDbPath = Path.Combine(Path.GetTempPath(), $"EO_MissionDb_{Guid.NewGuid():N}.sqlite");

			try
			{
				var packets = LoadFixturePack();
				var apiFileService = new ApiFileService(KCDatabase.Instance, () => new ElectronicObserverDbContext(tempDbPath));

				var reqs = packets.Where(p => p.FileType == 1).ToDictionary(p => p.ApiName, p => p.Content);
				var resps = packets.Where(p => p.FileType == 2).ToDictionary(p => p.ApiName, p => p.Content);

				foreach (var apiName in resps.Keys)
				{
					string reqBody = reqs.ContainsKey(apiName) ? reqs[apiName] : "";
					string resBody = resps[apiName];
					await apiFileService.SaveApiData(apiName, reqBody, resBody);
				}

				using (var db = new ElectronicObserverDbContext(tempDbPath))
				{
					var allFiles = db.ApiFiles.ToList();
					Assert.NotEmpty(allFiles);

					// Non-sortie packets must have null SortieRecordId
					Assert.All(allFiles, f => Assert.Null(f.SortieRecordId));

					var savedNames = allFiles.Select(f => f.Name).ToHashSet();
					Assert.Contains("api_start2/getData", savedNames);
					Assert.Contains("api_port/port", savedNames);
					Assert.Contains("api_get_member/slot_item", savedNames);
					Assert.Contains("api_req_mission/start", savedNames);
					Assert.Contains("api_req_mission/result", savedNames);

					// Verify raw Brotli compression in SQLite table
					using (var cmd = db.Connection.CreateCommand())
					{
						cmd.CommandText = "SELECT Name, Content FROM ApiFiles WHERE Name = 'api_start2/getData' AND ApiFileType = @FileType";
						cmd.Parameters.AddWithValue("@FileType", (int)ApiFileType.Response);
						using (var reader = cmd.ExecuteReader())
						{
							Assert.True(reader.Read());
							byte[] blob = (byte[])reader["Content"];
							Assert.NotEmpty(blob);

							string decompressed = ElectronicObserverDbContext.DecompressContent(blob);
							Assert.Contains("api_mst_ship", decompressed);
						}
					}
				}
			}
			finally
			{
				if (File.Exists(tempDbPath))
				{
					try { File.Delete(tempDbPath); } catch { }
				}
			}
		}

		[Fact]
		public async Task MissionReplay_ExportToApiList_GeneratesValidApiListAndJsonFiles()
		{
			// What: Verify ApiFileService.ExportToApiList exports APIList.txt and matching JSON files
			//       for non-sortie packets, compatible with FormMain.LoadAPIList.
			string tempDir = Path.Combine(Path.GetTempPath(), $"EO_ExportApiList_{Guid.NewGuid():N}");
			string tempDbPath = Path.Combine(Path.GetTempPath(), $"EO_ExportDb_{Guid.NewGuid():N}.sqlite");
			Directory.CreateDirectory(tempDir);

			try
			{
				var packets = LoadFixturePack();
				var apiFileService = new ApiFileService(KCDatabase.Instance, () => new ElectronicObserverDbContext(tempDbPath));

				var reqs = packets.Where(p => p.FileType == 1).ToDictionary(p => p.ApiName, p => p.Content);
				var resps = packets.Where(p => p.FileType == 2).ToDictionary(p => p.ApiName, p => p.Content);

				foreach (var apiName in resps.Keys)
				{
					string reqBody = reqs.ContainsKey(apiName) ? reqs[apiName] : "";
					string resBody = resps[apiName];
					await apiFileService.SaveApiData(apiName, reqBody, resBody);
				}

				using (var db = new ElectronicObserverDbContext(tempDbPath))
				{
					var allFiles = db.ApiFiles.ToList();
					ApiFileService.ExportToApiList(tempDir, allFiles, "APIList.txt");
				}

				string listPath = Path.Combine(tempDir, "APIList.txt");
				Assert.True(File.Exists(listPath), "APIList.txt should be created.");

				string[] listLines = File.ReadAllLines(listPath, Encoding.UTF8);
				Assert.NotEmpty(listLines);
				Assert.Contains(listLines, l => l.Contains("api_start2/getData"));
				Assert.Contains(listLines, l => l.Contains("api_port/port"));
				Assert.Contains(listLines, l => l.Contains("api_get_member/slot_item"));

				foreach (var line in listLines.Where(l => !string.IsNullOrWhiteSpace(l)))
				{
					string[] parts = line.Split('\t');
					if (parts.Length >= 2)
					{
						string jsonRelPath = parts[1];
						string fullJsonPath = Path.Combine(tempDir, jsonRelPath);
						Assert.True(File.Exists(fullJsonPath), $"Exported JSON file should exist: {jsonRelPath}");
					}
				}
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					try { Directory.Delete(tempDir, true); } catch { }
				}
				if (File.Exists(tempDbPath))
				{
					try { File.Delete(tempDbPath); } catch { }
				}
			}
		}

		[Fact]
		public void MissionReplay_FullRestoreAndSnapshotVerification()
		{
			// What: Verify APIObserver.Instance.LoadResponse restores KCDatabase completely
			//       (MasterShips, MasterEquipments, Ships, Equipments, Admiral, Fleets),
			//       and MissionClearCondition.Check snapshot matches real game state.
			var packets = LoadFixturePack();
			var resps = packets.Where(p => p.FileType == 2).ToDictionary(p => p.ApiName, p => p.Content);

			// Feed responses into APIObserver in client sequence
			Assert.True(resps.ContainsKey("api_start2/getData"));
			APIObserver.Instance.LoadResponse("/kcsapi/api_start2/getData", resps["api_start2/getData"]);

			if (resps.ContainsKey("api_get_member/useitem"))
			{
				APIObserver.Instance.LoadResponse("/kcsapi/api_get_member/useitem", resps["api_get_member/useitem"]);
			}

			if (resps.ContainsKey("api_get_member/slot_item"))
			{
				APIObserver.Instance.LoadResponse("/kcsapi/api_get_member/slot_item", resps["api_get_member/slot_item"]);
			}

			Assert.True(resps.ContainsKey("api_port/port"));
			APIObserver.Instance.LoadResponse("/kcsapi/api_port/port", resps["api_port/port"]);

			// Verify restored KCDatabase state
			var db = KCDatabase.Instance;
			Assert.NotNull(db.MasterShips);
			Assert.True(db.MasterShips.Count > 100, "MasterShips should be loaded.");
			Assert.True(db.MasterEquipments.Count > 50, "MasterEquipments should be loaded.");
			Assert.True(db.Ships.Count > 0, "Ships should be loaded.");
			Assert.True(db.Equipments.Count > 0, "Equipments should be loaded.");
			Assert.NotNull(db.Admiral);
			Assert.Equal(120, db.Admiral.Level);

			// Verify restored Fleets (Decks 1-4)
			Assert.NotNull(db.Fleet);
			Assert.True(db.Fleet.Fleets.Count >= 4, "Fleets 1 to 4 should be available.");

			var fleet1 = db.Fleet[1];
			var fleet2 = db.Fleet[2];
			var fleet3 = db.Fleet[3];
			var fleet4 = db.Fleet[4];

			Assert.NotNull(fleet1);
			Assert.NotNull(fleet2);
			Assert.NotNull(fleet3);
			Assert.NotNull(fleet4);

			// Fleet 1: 6 ships in port
			Assert.Equal(6, fleet1.MembersInstance.Count(s => s != null));
			Assert.NotNull(fleet1.MembersInstance[0]);

			// Fleet 2: 4 ships, currently assigned to Mission 2 (長距離練習航海)
			Assert.Equal(4, fleet2.MembersInstance.Count(s => s != null));
			Assert.Equal(2, fleet2.ExpeditionDestination);
			Assert.Equal(1, fleet2.ExpeditionState);

			// Fleet 3: 4 ships, currently assigned to Mission 101 (海峡警備行動)
			Assert.Equal(4, fleet3.MembersInstance.Count(s => s != null));
			Assert.Equal(101, fleet3.ExpeditionDestination);
			Assert.Equal(1, fleet3.ExpeditionState);

			// Fleet 4: 4 ships, currently assigned to Mission 41 (ブルネイ泊地沖哨戒)
			Assert.Equal(4, fleet4.MembersInstance.Count(s => s != null));
			Assert.Equal(41, fleet4.ExpeditionDestination);
			Assert.Equal(1, fleet4.ExpeditionState);

			// Verification of MissionClearCondition against actual dispatched fleets:
			// 1. Fleet 2 must succeed on Mission 2 (長距離練習航海)
			var resultF2M2 = MissionClearCondition.Check(2, fleet2);
			Assert.True(resultF2M2.IsSuceeded, $"Fleet 2 should succeed Mission 2: {string.Join(", ", resultF2M2.FailureReason)}");

			// 2. Fleet 3 must succeed on Mission 101 (海峡警備行動)
			var resultF3M101 = MissionClearCondition.Check(101, fleet3);
			Assert.True(resultF3M101.IsSuceeded, $"Fleet 3 should succeed Mission 101: {string.Join(", ", resultF3M101.FailureReason)}");

			// 3. Fleet 4 must succeed on Mission 41 (ブルネイ泊地沖哨戒)
			var resultF4M41 = MissionClearCondition.Check(41, fleet4);
			Assert.True(resultF4M41.IsSuceeded, $"Fleet 4 should succeed Mission 41: {string.Join(", ", resultF4M41.FailureReason)}");

			// Snapshot validation: Evaluate all major mission IDs (1-40, 100-115) for Fleet 2 & 3
			// ensuring Check never throws and returns reproducible results.
			var testMissionIds = Enumerable.Range(1, 40).Concat(Enumerable.Range(100, 16)).ToList();
			foreach (int mId in testMissionIds)
			{
				var r2 = MissionClearCondition.Check(mId, fleet2);
				Assert.NotNull(r2);
				Assert.NotNull(r2.FailureReason);

				var r3 = MissionClearCondition.Check(mId, fleet3);
				Assert.NotNull(r3);
				Assert.NotNull(r3.FailureReason);
			}

			// Specific failure snapshot verification:
			// Fleet 2 has only 4 ships and low drum counts, so Mission 38 (東京急行(弐): requires 6 ships, 8 drums) must fail
			var rF2M38 = MissionClearCondition.Check(38, fleet2);
			Assert.False(rF2M38.IsSuceeded);
			Assert.Contains(rF2M38.FailureReason, f => f.Contains("艦船数"));
		}

		#endregion
	}
}
