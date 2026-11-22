#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ElectronicObserver.Database;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.DataExport;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Replay;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.SortieCostViewer;
using Xunit;

namespace ElectronicObserverTests
{
	[Collection("KCDatabase")]
	public class SortieServicesTests
	{
		private static SortieRecord CreateSampleSortieRecord(bool withAfterData = true)
		{
			var record = new SortieRecord
			{
				Id = 42,
				Version = 1,
				World = 5,
				Map = 5,
				FleetData = new SortieFleetData
				{
					FleetId = 1,
					CombinedFlag = 0,
					NodeSupportFleetId = 3,
					BossSupportFleetId = 4,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Name = "第1艦隊",
							Ships = new List<SortieShip>
							{
								new SortieShip
								{
									Id = 1, // 睦月
									Level = 120, // ケッコン艦
									Condition = 49,
									Fuel = 15,
									Ammo = 20,
									MaxHp = 20,
									Kyouka = new List<int> { 0, 0, 0, 0, 0 },
									EquipmentSlots = new List<SortieEquipmentSlot>
									{
										new SortieEquipmentSlot
										{
											Equipment = new SortieEquipment { Id = 1, Level = 10, AircraftLevel = 0 },
											AircraftCurrent = 0,
										},
									},
									ExpansionSlot = new SortieEquipmentSlot
									{
										Equipment = new SortieEquipment { Id = 2, Level = 4, AircraftLevel = 0 },
										AircraftCurrent = 0,
									},
								},
								new SortieShip
								{
									Id = 2, // 如月
									Level = 75,
									Condition = 49,
									Fuel = 15,
									Ammo = 20,
									MaxHp = 20,
									Kyouka = new List<int> { 0, 0, 0, 0, 0 },
									EquipmentSlots = new List<SortieEquipmentSlot>
									{
										new SortieEquipmentSlot
										{
											Equipment = new SortieEquipment { Id = 399, Level = 0, AircraftLevel = 7 }, // 噴式機想定マスターID
											AircraftCurrent = 18,
										},
									},
								},
							},
						},
					},
					AirBases = new List<SortieAirBase>
					{
						new SortieAirBase
						{
							AirCorpsId = 1,
							MapAreaId = 5,
							ActionKind = 1, // 出撃
							BaseDistance = 7,
							BonusDistance = 2,
							Squadrons = new List<SortieAirBaseSquadron>
							{
								new SortieAirBaseSquadron
								{
									EquipmentSlot = new SortieEquipmentSlot
									{
										Equipment = new SortieEquipment { Id = 168, Level = 10, AircraftLevel = 7 },
										AircraftCurrent = 18,
									},
									State = 1,
									Condition = 1, // 通常
								},
							},
						},
					},
				},
				MapData = new SortieMapData
				{
					MapHPCurrent = 500,
					MapHPMax = 1000,
					RequiredDefeatedCount = 4,
				},
			};

			if (withAfterData)
			{
				record.FleetAfterSortieData = new SortieFleetData
				{
					FleetId = 1,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Name = "第1艦隊",
							Ships = new List<SortieShip>
							{
								new SortieShip
								{
									Id = 1,
									Level = 120,
									Fuel = 5,  // 10消費 (ケッコン0.85 -> 8消費)
									Ammo = 8,  // 12消費 (ケッコン0.85 -> 10消費)
									Condition = 40,
									MaxHp = 20,
									EquipmentSlots = new List<SortieEquipmentSlot>
									{
										new SortieEquipmentSlot
										{
											Equipment = new SortieEquipment { Id = 1, Level = 10, AircraftLevel = 0 },
											AircraftCurrent = 0,
										},
									},
								},
								new SortieShip
								{
									Id = 2,
									Level = 75,
									Fuel = 5,  // 10消費
									Ammo = 8,  // 12消費
									Condition = 35,
									MaxHp = 20,
									EquipmentSlots = new List<SortieEquipmentSlot>
									{
										new SortieEquipmentSlot
										{
											Equipment = new SortieEquipment { Id = 399, Level = 0, AircraftLevel = 7 },
											AircraftCurrent = 15, // 3機喪失 -> ボーキ15
										},
									},
								},
							},
						},
					},
				};
			}

			return record;
		}

		private static SortieRecord CreateSampleCombinedSortieRecord()
		{
			var record = CreateSampleSortieRecord(withAfterData: true);
			record.Id = 43;
			record.FleetData!.CombinedFlag = 1; // 機動部隊等の連合艦隊フラグ

			// 第2艦隊を追加
			record.FleetData.Fleets.Add(new SortieFleet
			{
				Name = "第2艦隊",
				Ships = new List<SortieShip>
				{
					new SortieShip
					{
						Id = 3, // 弥生
						Level = 80,
						Fuel = 15,
						Ammo = 20,
						MaxHp = 20,
						Kyouka = new List<int> { 0, 0, 0, 0, 0 },
						EquipmentSlots = new List<SortieEquipmentSlot>(),
					},
					new SortieShip
					{
						Id = 4, // 卯月
						Level = 80,
						Fuel = 15,
						Ammo = 20,
						MaxHp = 20,
						Kyouka = new List<int> { 0, 0, 0, 0, 0 },
						EquipmentSlots = new List<SortieEquipmentSlot>(),
					},
				},
			});

			// 帰投後データにも第2艦隊を追加
			record.FleetAfterSortieData!.Fleets.Add(new SortieFleet
			{
				Name = "第2艦隊",
				Ships = new List<SortieShip>
				{
					new SortieShip
					{
						Id = 3,
						Level = 80,
						Fuel = 5,  // 10消費
						Ammo = 10, // 10消費
						MaxHp = 20,
						EquipmentSlots = new List<SortieEquipmentSlot>(),
					},
					new SortieShip
					{
						Id = 4,
						Level = 80,
						Fuel = 5,  // 10消費
						Ammo = 10, // 10消費
						MaxHp = 20,
						EquipmentSlots = new List<SortieEquipmentSlot>(),
					},
				},
			});

			return record;
		}

		private static List<ApiFile> CreateSampleSortieApiFiles()
		{
			var now = DateTime.UtcNow;
			var list = new List<ApiFile>();

			// 1. 出撃開始 -> セル1 (資源マス)
			list.Add(new ApiFile
			{
				Name = "api_req_map/start",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(1),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_no\":1,\"api_event_id\":2,\"api_event_kind\":0,\"api_itemget\":[{\"api_usemst\":1,\"api_getcount\":50}]}}",
			});

			// 2. 移動 -> セル2 (うずしおマス)
			list.Add(new ApiFile
			{
				Name = "api_req_map/next",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(2),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_no\":2,\"api_event_id\":3,\"api_event_kind\":0,\"api_happening\":{\"api_mst_id\":1,\"api_count\":15}}}",
			});

			// 3. 移動 -> セル3 (通常戦闘マス)
			list.Add(new ApiFile
			{
				Name = "api_req_map/next",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(3),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_no\":3,\"api_event_id\":4,\"api_event_kind\":1}}",
			});

			// 3-1. 昼戦
			list.Add(new ApiFile
			{
				Name = "api_req_sortie/battle",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(4),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{" +
					"\"api_formation\":[1,1,1]," +
					"\"api_smoke_type\":0," +
					"\"api_ship_ke\":[1501,1502]," +
					"\"api_e_nowhps\":[50,30]," +
					"\"api_e_maxhps\":[50,30]," +
					"\"api_kouku\":{\"api_stage1\":{\"api_disp_seiku\":1,\"api_f_lostcount\":1},\"api_stage2\":{\"api_f_lostcount\":0},\"api_stage3\":{\"api_fdam\":[-1,5,0],\"api_edam\":[-1,10,0]}}," +
					"\"api_opening_taisen\":{\"api_at_list\":[-1,1],\"api_df_list\":[-1,[7]],\"api_damage\":[-1,[15]],\"api_cl_list\":[-1,[1]]}," +
					"\"api_opening_atack\":{\"api_frai\":[-1,1,0],\"api_fdam\":[-1,0,4],\"api_erai\":[-1,0,1],\"api_edam\":[-1,20,0]}," +
					"\"api_hougeki1\":{\"api_at_list\":[-1,1,7],\"api_df_list\":[-1,[7],[2]],\"api_damage\":[-1,[10],[12]],\"api_at_type\":[-1,0,0],\"api_cl_list\":[-1,[1],[2]]}," +
					"\"api_raigeki\":{\"api_frai\":[-1,0,0],\"api_fdam\":[-1,0,0],\"api_erai\":[-1,0,0],\"api_edam\":[-1,0,0]}" +
				"}}",
			});

			// 3-2. 夜戦
			list.Add(new ApiFile
			{
				Name = "api_req_battle_midnight/battle",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(5),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{" +
					"\"api_hougeki\":{\"api_at_list\":[-1,1,7],\"api_df_list\":[-1,[8],[1]],\"api_damage\":[-1,[30],[5]],\"api_sp_list\":[-1,0,0],\"api_cl_list\":[-1,[1],[1]]}" +
				"}}",
			});

			// 3-3. 戦闘結果
			list.Add(new ApiFile
			{
				Name = "api_req_sortie/battleresult",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(6),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_win_rank\":\"S\",\"api_get_base_exp\":360,\"api_get_exp\":720,\"api_mvp\":1,\"api_get_ship\":{\"api_ship_id\":33,\"api_ship_name\":\"長月\"}}}",
			});

			// 4. 移動 -> セル4 (空襲戦マス)
			list.Add(new ApiFile
			{
				Name = "api_req_map/next",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(6.5),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_no\":4,\"api_event_id\":4,\"api_event_kind\":6}}",
			});

			// 4-1. 空襲戦
			list.Add(new ApiFile
			{
				Name = "api_req_sortie/battle",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(6.6),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_formation\":[1,1,1],\"api_ship_ke\":[1501],\"api_kouku\":{\"api_stage3\":{\"api_fdam\":[-1,2,0]}}}}",
			});

			// 4-2. 空襲戦結果
			list.Add(new ApiFile
			{
				Name = "api_req_sortie/battleresult",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(6.7),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_win_rank\":\"A\",\"api_get_base_exp\":200,\"api_get_exp\":400,\"api_mvp\":1}}",
			});

			// 5. 移動 -> セル5 (ボスマスター)
			list.Add(new ApiFile
			{
				Name = "api_req_map/next",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(7),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_no\":5,\"api_event_id\":5,\"api_event_kind\":1}}",
			});

			// 5-1. ボス戦
			list.Add(new ApiFile
			{
				Name = "api_req_sortie/battle",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(8),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{" +
					"\"api_formation\":[1,2,2]," +
					"\"api_ship_ke\":[1505]," +
					"\"api_e_nowhps\":[200]," +
					"\"api_e_maxhps\":[200]," +
					"\"api_air_base_attack\":[{\"api_stage3\":{\"api_edam\":[-1,40]}}]," +
					"\"api_support_info\":{\"api_support_hourai\":{\"api_damage\":[-1,35]}}," +
					"\"api_hougeki1\":{\"api_at_list\":[-1,1],\"api_df_list\":[-1,[7]],\"api_damage\":[-1,[50]]}" +
				"}}",
			});

			list.Add(new ApiFile
			{
				Name = "api_req_sortie/battleresult",
				ApiFileType = ApiFileType.Response,
				TimeStamp = now.AddSeconds(9),
				ContentString = "svdata={\"api_result\":1,\"api_data\":{\"api_win_rank\":\"A\",\"api_get_base_exp\":480,\"api_get_exp\":960,\"api_mvp\":1}}",
			});

			return list;
		}

		[Fact]
		public void ReplayService_ShouldGenerateValidReplayJsonAndUrl()
		{
			// What: Verify ReplayService produces valid KC3-compatible Replay JSON and Url
			var record = CreateSampleSortieRecord();
			var apiFiles = CreateSampleSortieApiFiles();

			string url = ReplayService.GenerateReplayUrl(record, apiFiles);
			Assert.StartsWith("https://kc3kai.github.io/kancolle-replay/battleplayer.html#", url);

			string json = ReplayService.GenerateReplayJson(record, apiFiles);
			Assert.NotEmpty(json);

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			Assert.Equal(5, root.GetProperty("world").GetInt32());
			Assert.Equal(5, root.GetProperty("mapnum").GetInt32());
			Assert.Equal(1, root.GetProperty("fleetnum").GetInt32());

			// Fleets & AirBase verification
			Assert.True(root.TryGetProperty("fleet1", out var f1));
			Assert.NotEmpty(f1.EnumerateArray());

			Assert.True(root.TryGetProperty("lbas", out var lbas));
			Assert.Single(lbas.EnumerateArray());

			// Battles verification
			Assert.True(root.TryGetProperty("battles", out var battles));
			var battleList = battles.EnumerateArray().ToList();
			Assert.Equal(3, battleList.Count);

			var b1 = battleList[0];
			Assert.Equal(3, b1.GetProperty("node").GetInt32());
			Assert.Equal("S", b1.GetProperty("rating").GetString());
			Assert.Equal(360, b1.GetProperty("baseEXP").GetInt32());
			Assert.Equal(33, b1.GetProperty("drop").GetInt32());
			Assert.Equal(JsonValueKind.Object, b1.GetProperty("data").ValueKind);
			Assert.Equal(JsonValueKind.Object, b1.GetProperty("yasen").ValueKind);

			var b3 = battleList[2];
			Assert.Equal(5, b3.GetProperty("node").GetInt32());
			Assert.Equal("A", b3.GetProperty("rating").GetString());
		}

		[Fact]
		public void SortieExternalLinkService_DeckBuilderAndUrls_ShouldBeCorrect()
		{
			// What: Verify DeckBuilder JSON structure, AirControlSimulator & OperationRoom links
			var record = CreateSampleSortieRecord();

			string deckJson = SortieExternalLinkService.GenerateDeckBuilderJson(record);
			Assert.NotEmpty(deckJson);

			using (var doc = JsonDocument.Parse(deckJson))
			{
				var root = doc.RootElement;
				Assert.Equal(4, root.GetProperty("version").GetInt32());
				Assert.True(root.TryGetProperty("f1", out var f1));
				Assert.True(f1.TryGetProperty("s1", out var s1));
				Assert.Equal(1, s1.GetProperty("id").GetInt32());
				Assert.Equal(120, s1.GetProperty("lv").GetInt32());
				Assert.True(s1.GetProperty("items").TryGetProperty("i1", out _));
				Assert.True(s1.GetProperty("items").TryGetProperty("ix", out _));

				Assert.True(root.TryGetProperty("a1", out var a1));
				Assert.True(a1.GetProperty("items").TryGetProperty("i1", out _));
			}

			string acsUrl = SortieExternalLinkService.GenerateAirControlSimulatorUrl(record);
			Assert.StartsWith("https://noro6.github.io/kc-web#import:", acsUrl);
			Assert.Contains("\"version\":4", acsUrl);

			string opRoomUrl = SortieExternalLinkService.GenerateOperationRoomUrl(record);
			Assert.StartsWith("https://jervis.vercel.app?predeck=", opRoomUrl);
		}

		[Fact]
		public void SortieExternalLinkService_ExportAndImportJson_ShouldRoundtripCompletely()
		{
			// What: Verify ExportSortieRecordJson and ImportSortieRecordJson roundtrip faithfully
			var record = CreateSampleSortieRecord();
			var apiFiles = CreateSampleSortieApiFiles();

			string exported = SortieExternalLinkService.ExportSortieRecordJson(record, apiFiles);
			Assert.NotEmpty(exported);

			var (restoredRecord, restoredApiFiles) = SortieExternalLinkService.ImportSortieRecordJson(exported);
			Assert.NotNull(restoredRecord);
			Assert.Equal(record.Id, restoredRecord.Id);
			Assert.Equal(record.World, restoredRecord.World);
			Assert.Equal(record.Map, restoredRecord.Map);
			Assert.Equal(record.FleetData?.Fleets?.Count, restoredRecord.FleetData?.Fleets?.Count);

			Assert.NotNull(restoredApiFiles);
			Assert.Equal(apiFiles.Count, restoredApiFiles.Count);
			Assert.Equal(apiFiles[0].Name, restoredApiFiles[0].Name);
			Assert.Equal(apiFiles[0].ContentString, restoredApiFiles[0].ContentString);
		}

		[Fact]
		public void SortieNodeEmulator_ShouldParseAllPhasesAndTrackDamage()
		{
			// What: Verify SortieNodeEmulator correctly tracks movements, battle phases, item gets, and boss reach
			var record = CreateSampleSortieRecord();
			var apiFiles = CreateSampleSortieApiFiles();

			var detail = SortieNodeEmulator.EmulateSortie(record, apiFiles);
			Assert.Equal(42, detail.SortieId);
			Assert.Equal(5, detail.World);
			Assert.Equal(5, detail.Map);
			Assert.Equal(apiFiles[0].TimeStamp, detail.StartTime);
			Assert.Equal(5, detail.Nodes.Count);
			Assert.True(detail.ReachedBoss);

			// Node 1: Item get
			var n1 = detail.Nodes[0];
			Assert.Equal(1, n1.CellId);
			Assert.Equal(2, n1.EventId);
			Assert.Equal("資源獲得", n1.NodeTypeDescription);
			Assert.Contains("燃料 +50", n1.NonBattleInfo);
			Assert.False(n1.IsBattleNode);

			// Node 2: Happening (Maelstrom)
			var n2 = detail.Nodes[1];
			Assert.Equal(2, n2.CellId);
			Assert.Equal(3, n2.EventId);
			Assert.Equal("うずしお", n2.NodeTypeDescription);
			Assert.Contains("うずしお", n2.NonBattleInfo);

			// Node 3: Battle
			var n3 = detail.Nodes[2];
			Assert.Equal(3, n3.CellId);
			Assert.Equal(4, n3.EventId);
			Assert.Equal("通常戦闘", n3.NodeTypeDescription);
			Assert.True(n3.IsBattleNode);
			Assert.Equal("S", n3.WinRank);
			Assert.Equal(360, n3.BaseExp);
			Assert.Equal(720, n3.AdmiralExp);

			// Check Battle Phases
			var phaseNames = n3.Phases.Select(p => p.PhaseName).ToList();
			Assert.Contains("航空戦", phaseNames);
			Assert.Contains("先制対潜", phaseNames);
			Assert.Contains("開幕雷撃", phaseNames);
			Assert.Contains("砲撃戦(1巡目)", phaseNames);
			Assert.Contains("閉幕雷撃", phaseNames);
			Assert.Contains("夜戦", phaseNames);

			// 回帰検証: 昼夜戦を通じた敵艦HPの継続減算
			Assert.NotNull(n3.EnemyShipsAfter);
			Assert.Equal(2, n3.EnemyShipsAfter.Count);
			Assert.Equal(0, n3.EnemyShipsAfter[0].HpAfter); // 昼戦で撃沈
			Assert.Equal(0, n3.EnemyShipsAfter[1].HpAfter); // 夜戦で撃沈 (30 - 30)

			// 回帰検証: api_cl_list 由来の IsCritical と AttackType
			var hougekiPhase = n3.Phases.First(p => p.PhaseName == "砲撃戦(1巡目)");
			var criticalAttack = hougekiPhase.Attacks.FirstOrDefault(a => a.IsCritical);
			Assert.NotNull(criticalAttack);
			Assert.Equal("通常", criticalAttack.AttackType);

			// Verify Friendly HP decrease from Hougeki
			Assert.NotEmpty(n3.FriendlyShipsAfter);
			Assert.True(n3.FriendlyShipsAfter[1].HpAfter < n3.FriendlyShipsAfter[1].HpMax);

			// 回帰検証: 次のマスへの HP 引き継ぎ (ノード3の HpAfter がノード4の HpBefore に反映)
			var nNext = detail.Nodes[3];
			Assert.Equal(n3.FriendlyShipsAfter[1].HpAfter, nNext.FriendlyShipsAfter[1].HpBefore);

			// Node 4: Air raid battle
			Assert.Equal(4, nNext.CellId);
			Assert.Contains("空襲", nNext.NodeTypeDescription);

			// Node 5: Boss
			var nBoss = detail.Nodes[4];
			Assert.Equal(5, nBoss.CellId);
			Assert.Equal(5, nBoss.EventId);
			Assert.True(nBoss.IsBoss);
			Assert.Equal("ボス戦闘", nBoss.NodeTypeDescription);
			Assert.Contains(nBoss.Phases, p => p.PhaseName == "基地航空隊");
			Assert.Contains(nBoss.Phases, p => p.PhaseName == "支援艦隊");
		}

		[Fact]
		public void SortieCostCalculator_CalculateAndCache_ShouldComputeCostsAccurately()
		{
			// What: Verify SortieCostCalculator accurately computes supply, repair, airbase, jet, and aggregate costs
			using var db = new ElectronicObserverDbContext(inMemory: true);
			var record = CreateSampleSortieRecord(withAfterData: true);
			var apiFiles = CreateSampleSortieApiFiles();

			db.Sorties.Add(record);
			db.SaveChanges();

			// 1. Calculate with FleetAfterSortieData (Single fleet)
			var cost = SortieCostCalculator.CalculateAndCache(record, apiFiles, db);
			Assert.NotNull(cost);
			Assert.NotNull(cost.SortieFleetSupplyCost);
			Assert.NotNull(cost.SortieFleetRepairCost);
			Assert.NotNull(cost.TotalAirBaseSortieCost);
			Assert.NotNull(cost.SortieFleetJetCost);

			// Fuel: Ship 1 (diff 10 * 0.85 = 8) + Ship 2 (diff 10) = 18
			Assert.Equal(18, cost.SortieFleetSupplyCost!.Fuel);
			// Ammo: Ship 1 (diff 12 * 0.85 = 10) + Ship 2 (diff 12) = 22
			Assert.Equal(22, cost.SortieFleetSupplyCost!.Ammo);
			// Bauxite: Ship 2 lost 3 aircraft * 5 = 15
			Assert.Equal(15, cost.SortieFleetSupplyCost!.Bauxite);

			// Airbase sortie cost: 18 * ceil(1.5 * 18) = 27 fuel, (0.7 * 18) = 12 ammo
			Assert.Equal(27, cost.TotalAirBaseSortieCost!.Fuel);
			Assert.Equal(12, cost.TotalAirBaseSortieCost!.Ammo);

			// Repair cost & Damage state counts
			Assert.NotNull(cost.DamageStateCounts);
			Assert.True(cost.DamageStateCounts!.ContainsKey("Heavy") || cost.DamageStateCounts.ContainsKey("Medium") || cost.DamageStateCounts.ContainsKey("Light"));

			// 2. Fallback calculation without FleetAfterSortieData
			var recordWithoutAfter = CreateSampleSortieRecord(withAfterData: false);
			var fallbackCost = SortieCostCalculator.CalculateAndCache(recordWithoutAfter, apiFiles, db);
			Assert.NotNull(fallbackCost.SortieFleetSupplyCost);
			Assert.True(fallbackCost.SortieFleetSupplyCost!.Fuel > 0);
			Assert.True(fallbackCost.SortieFleetSupplyCost!.Ammo > 0);

			// 3. 連合艦隊（第1艦隊＋第2艦隊）のコスト合算テスト
			var combinedRecord = CreateSampleCombinedSortieRecord();
			var combinedCost = SortieCostCalculator.CalculateAndCache(combinedRecord, apiFiles, db);
			Assert.NotNull(combinedCost.SortieFleetSupplyCost);
			// 第1艦隊(18 fuel) + 第2艦隊(20 fuel) = 38 fuel
			Assert.Equal(38, combinedCost.SortieFleetSupplyCost!.Fuel);
			// 第1艦隊(22 ammo) + 第2艦隊(20 ammo) = 42 ammo
			Assert.Equal(42, combinedCost.SortieFleetSupplyCost!.Ammo);
			Assert.True(combinedCost.SortieFleetRepairCost!.Fuel >= 0);

			// 4. Aggregate calculation
			var aggregate = SortieCostCalculator.CalculateAggregateCost(new[] { cost, fallbackCost, combinedCost });
			Assert.NotNull(aggregate);
			Assert.Equal(cost.SortieFleetSupplyCost!.Fuel + fallbackCost.SortieFleetSupplyCost!.Fuel + combinedCost.SortieFleetSupplyCost!.Fuel, aggregate.SortieFleetSupplyCost!.Fuel);
		}

		[Fact]
		public void SortieCsvExportService_AllTypes_ShouldExportValidCsv()
		{
			// What: Verify SortieCsvExportService exports non-empty CSV with headers and data rows for all 12 export types
			var record = CreateSampleSortieRecord();
			var apiFiles = CreateSampleSortieApiFiles();
			var sorties = new List<(SortieRecord, IEnumerable<ApiFile>)> { (record, apiFiles) };

			foreach (SortieCsvExportType exportType in Enum.GetValues(typeof(SortieCsvExportType)))
			{
				string csv = SortieCsvExportService.ExportCsv(exportType, sorties);
				Assert.NotNull(csv);
				Assert.NotEmpty(csv);

				// Must contain header line and at least one data row
				string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
				Assert.True(lines.Length >= 2, $"ExportType {exportType} produced only header or empty CSV (line count: {lines.Length}).");
				Assert.Contains("SortieId", lines[0]);
			}
		}
	}
}
