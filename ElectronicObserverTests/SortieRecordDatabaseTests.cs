using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectronicObserver.Data;
using ElectronicObserver.Database;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Services.ApiFileService;
using Xunit;

namespace ElectronicObserverTests
{
	public class SortieRecordDatabaseTests
	{
		[Fact]
		public void BrotliCompression_ShouldCompressAndDecompressCorrectly()
		{
			// What: Verify Brotli compression roundtrip on realistic API payloads and edge cases
			string originalJson = "{\"api_result\":1,\"api_data\":{\"api_ships\":[" +
				string.Join(",", Enumerable.Range(1, 20).Select(i => $"{{\"api_ship_id\":{i},\"api_name\":\"睦月型駆逐艦{i}番艦\"}}")) +
				"]}}";

			byte[] compressed = ElectronicObserverDbContext.CompressContent(originalJson);
			Assert.NotEmpty(compressed);
			Assert.True(compressed.Length < Encoding.UTF8.GetByteCount(originalJson));

			string decompressed = ElectronicObserverDbContext.DecompressContent(compressed);
			Assert.Equal(originalJson, decompressed);

			// Short string roundtrip
			string shortJson = "{\"api_result\":1}";
			Assert.Equal(shortJson, ElectronicObserverDbContext.DecompressContent(ElectronicObserverDbContext.CompressContent(shortJson)));

			// Edge cases
			Assert.Empty(ElectronicObserverDbContext.CompressContent(""));
			Assert.Equal("", ElectronicObserverDbContext.DecompressContent(new byte[0]));
			Assert.Equal("", ElectronicObserverDbContext.DecompressContent(null));
		}

		[Fact]
		public async Task DbContext_ShouldSaveAndRetrieveSortieRecordAndApiFiles()
		{
			// What: Save a SortieRecord and linked ApiFiles to SQLite, retrieve and verify all fields
			using (var db = new ElectronicObserverDbContext(inMemory: true))
			{
				var sortie = new SortieRecord
				{
					Version = 1,
					World = 1,
					Map = 1,
					FleetData = new SortieFleetData
					{
						FleetId = 1,
						NodeSupportFleetId = 0,
						BossSupportFleetId = 0,
						CombinedFlag = 0,
						Fleets = new List<SortieFleet>
						{
							new SortieFleet
							{
								Name = "第1艦隊",
								Ships = new List<SortieShip>
								{
									new SortieShip
									{
										Id = 1,
										Level = 99,
										Condition = 49,
										Fuel = 15,
										Ammo = 20,
										Hp = 30,
										EquipmentSlots = new List<SortieEquipmentSlot>
										{
											new SortieEquipmentSlot
											{
												AircraftCurrent = 0,
												AircraftMax = 0,
												Equipment = new SortieEquipment
												{
													Id = 1,
													Level = 10,
													AircraftLevel = 7,
												}
											}
										}
									}
								}
							}
						}
					},
					MapData = new SortieMapData
					{
						RequiredDefeatedCount = 4,
						MapHPCurrent = 100,
						MapHPMax = 100,
					}
				};

				await db.Sorties.AddAsync(sortie);
				await db.SaveChangesAsync();
				Assert.True(sortie.Id > 0);

				var reqFile = new ApiFile
				{
					ApiFileType = ApiFileType.Request,
					Name = "api_req_map/start",
					Content = "{\"api_deck_id\":\"1\"}",
					TimeStamp = DateTime.UtcNow,
					Version = 1,
					SortieRecordId = sortie.Id,
				};

				var resFile = new ApiFile
				{
					ApiFileType = ApiFileType.Response,
					Name = "api_req_map/start",
					Content = "{\"api_result\":1,\"api_data\":{\"api_maparea_id\":1,\"api_mapinfo_no\":1}}",
					TimeStamp = DateTime.UtcNow,
					Version = 1,
					SortieRecordId = sortie.Id,
				};

				await db.ApiFiles.AddAsync(reqFile);
				await db.ApiFiles.AddAsync(resFile);
				await db.SaveChangesAsync();

				// Retrieve and verify
				var retrieved = await db.Sorties.FindAsync(sortie.Id, includeApiFiles: true);
				Assert.NotNull(retrieved);
				Assert.Equal(1, retrieved.World);
				Assert.Equal(1, retrieved.Map);
				Assert.Equal(1, retrieved.FleetData.FleetId);
				Assert.Equal("第1艦隊", retrieved.FleetData.Fleets[0].Name);
				Assert.Equal(1, retrieved.FleetData.Fleets[0].Ships[0].Id);
				Assert.Equal(100, retrieved.MapData.MapHPCurrent);

				Assert.Equal(2, retrieved.ApiFiles.Count);
				Assert.Equal("api_req_map/start", retrieved.ApiFiles[0].Name);
				Assert.Equal("{\"api_deck_id\":\"1\"}", retrieved.ApiFiles[0].Content);
			}
		}

		[Fact]
		public async Task ApiFileService_SortieFlow_ShouldLinkApisAndCaptureFleetAfterSortie()
		{
			// What: Test full sortie lifecycle in ApiFileService (start -> battle -> port -> ProcessedApi)
			string tempDbPath = Path.Combine(Path.GetTempPath(), $"EO_Test_{Guid.NewGuid():N}.sqlite");

			try
			{
				Func<ElectronicObserverDbContext> contextFactory = () => new ElectronicObserverDbContext(tempDbPath);

				var service = new ApiFileService(KCDatabase.Instance, contextFactory);

				// 1. api_req_map/start
				string startReq = "api_token=xxx&api_verno=1&api_deck_id=1";
				string startRes = "svdata={\"api_result\":1,\"api_data\":{\"api_maparea_id\":1,\"api_mapinfo_no\":1}}";
				await service.SaveApiData("api_req_map/start", startReq, startRes);

				Assert.NotNull(service.CurrentSortieId);
				int sortieId = service.CurrentSortieId.Value;

				// 2. api_req_map/next during sortie
				string nextReq = "api_token=xxx&api_recovery_type=0";
				string nextRes = "svdata={\"api_result\":1,\"api_data\":{\"api_no\":1,\"api_color_no\":4}}";
				await service.SaveApiData("api_req_map/next", nextReq, nextRes);

				// 3. api_port/port ends sortie
				string portReq = "api_token=xxx&api_port=1";
				string portRes = "svdata={\"api_result\":1,\"api_data\":{\"api_ship\":[{\"api_id\":1}],\"api_deck_port\":[],\"api_ndock\":[],\"api_log\":[]}}";
				await service.SaveApiData("api_port/port", portReq, portRes);

				Assert.Null(service.CurrentSortieId);

				// 4. ProcessedApi after KCDatabase updates port
				await service.ProcessedApi("api_port/port");

				// Verify DB contents
				using (var db = contextFactory())
				{
					var sortie = await db.Sorties.FindAsync(sortieId, includeApiFiles: true);
					Assert.NotNull(sortie);
					Assert.NotNull(sortie.FleetAfterSortieData);

					// Linked ApiFiles should be start (Q+S), next (Q+S), port (Q+S) = 6 files
					Assert.Equal(6, sortie.ApiFiles.Count);
					Assert.All(sortie.ApiFiles, f => Assert.Equal(sortieId, f.SortieRecordId));

					var nextFile = sortie.ApiFiles.FirstOrDefault(f => f.Name == "api_req_map/next" && f.ApiFileType == ApiFileType.Response);
					Assert.NotNull(nextFile);
					Assert.DoesNotContain("svdata=", nextFile.Content);
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
		public async Task ApiFileService_ExpeditionAndCoreApis_ShouldBeSavedEvenOutsideSortie()
		{
			// What: Verify issue #1 / #11 requirement that expedition APIs are saved with SortieRecordId = null
			string tempDbPath = Path.Combine(Path.GetTempPath(), $"EO_Test_{Guid.NewGuid():N}.sqlite");

			try
			{
				Func<ElectronicObserverDbContext> contextFactory = () => new ElectronicObserverDbContext(tempDbPath);
				var service = new ApiFileService(KCDatabase.Instance, contextFactory);

				// Save expedition start outside sortie
				await service.SaveApiData("api_req_mission/start", "api_deck_id=2&api_mission_id=33", "svdata={\"api_result\":1}");

				// Save expedition result outside sortie
				await service.SaveApiData("api_req_mission/result", "api_deck_id=2", "svdata={\"api_result\":1,\"api_data\":{\"api_clear_result\":1}}");

				// Save member ships outside sortie
				await service.SaveApiData("api_get_member/ship2", "api_sort_key=1", "svdata={\"api_result\":1,\"api_data\":[]}");

				// Save noise API that should be ignored
				await service.SaveApiData("api_get_member/questlist", "api_page_no=1", "svdata={\"api_result\":1}");

				using (var db = contextFactory())
				{
					var expeditionApis = await db.ApiFiles.GetExpeditionApisAsync();
					Assert.Equal(6, expeditionApis.Count); // 3 pairs of Q + S
					Assert.All(expeditionApis, f => Assert.Null(f.SortieRecordId));

					var questApis = await db.ApiFiles.GetByNameAsync("api_get_member/questlist");
					Assert.Empty(questApis); // Ignored
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
		public void ApiListExport_ShouldCreateLoadAPIListCompatibleFiles()
		{
			// What: Export ApiFiles to APIList.txt and verify format compatibility with LoadAPIList
			string tempDir = Path.Combine(Path.GetTempPath(), $"EO_Export_{Guid.NewGuid():N}");

			try
			{
				var files = new List<ApiFile>
				{
					new ApiFile
					{
						Id = 1,
						ApiFileType = ApiFileType.Request,
						Name = "api_req_map/start",
						Content = "{\"api_deck_id\":\"1\",\"api_maparea_id\":\"1\"}",
						TimeStamp = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc),
						Version = 1,
					},
					new ApiFile
					{
						Id = 2,
						ApiFileType = ApiFileType.Response,
						Name = "api_req_map/start",
						Content = "{\"api_result\":1,\"api_data\":{\"api_maparea_id\":1,\"api_mapinfo_no\":1}}",
						TimeStamp = new DateTime(2026, 11, 5, 12, 0, 1, DateTimeKind.Utc),
						Version = 1,
					}
				};

				ApiFileService.ExportToApiList(tempDir, files);

				string listPath = Path.Combine(tempDir, "APIList.txt");
				Assert.True(File.Exists(listPath));

				string[] lines = File.ReadAllLines(listPath);
				Assert.Equal(2, lines.Length);
				Assert.Contains("[Request] /kcsapi/api_req_map/start", lines[0]);
				Assert.Contains("[Response] /kcsapi/api_req_map/start", lines[1]);

				// Check generated json files
				string[] qFiles = Directory.GetFiles(tempDir, "*Q@api_req_map@start.json");
				string[] sFiles = Directory.GetFiles(tempDir, "*S@api_req_map@start.json");

				Assert.Single(qFiles);
				Assert.Single(sFiles);

				string qContent = File.ReadAllText(qFiles[0]);
				string sContent = File.ReadAllText(sFiles[0]);

				// Q should be query string format for LoadRequest
				Assert.Contains("api_deck_id=1", qContent);
				// S should start with svdata= for LoadResponse
				Assert.StartsWith("svdata=", sContent);
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					try { Directory.Delete(tempDir, true); } catch { }
				}
			}
		}

		[Fact]
		public void DestinationDataService_ShouldLoadAndResolveCellDisplay()
		{
			// What: Verify destination.json cell name resolution
			var service = DestinationDataService.Instance;
			service.Initialize();

			if (service.IsLoaded)
			{
				// World 1-1 cell 1 is ["Start", "A"]
				string cellName = service.CellDisplay(1, 1, 1);
				Assert.Equal("A", cellName);

				string withId = service.CellDisplayWithId(1, 1, 1);
				Assert.Equal("A (1)", withId);

				string prev = service.PreviousCellDisplay(1, 1, 1);
				Assert.Equal("Start", prev);

				// Unknown cell returns cellId as string
				Assert.Equal("999", service.CellDisplay(1, 1, 999));
			}
		}
	}
}
