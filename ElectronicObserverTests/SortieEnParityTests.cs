#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
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
	/// <summary>
	/// Tests porting critical parity behaviors from ElectronicObserverEN:
	/// 1. ReplayService slot mapping (Equip, Stars, Ace), empty slot 0, and event map airbase export.
	/// 2. SortieCostCalculator simulation vs actual diff parity (SortieCostTest0 pattern), special attack ammo,
	///    married ship discounts, whirlpool losses, and jet aircraft steel consumption.
	/// 3. SortieDetail / SortieNodeEmulator combined fleet day-to-night HP carry-over and special effect items.
	/// 4. SortieCsvExportService golden data column value verification across combat phases.
	/// </summary>
	[Collection("KCDatabase")]
	public class SortieEnParityTests
	{
		public SortieEnParityTests()
		{
			TestMasterDataFixture.EnsureMasterDataLoaded();
		}

		#region Test Fixture Loading Helpers

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		private static readonly Lazy<Dictionary<string, string>> FixtureCache = new Lazy<Dictionary<string, string>>(() =>
		{
			var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			string? brPath = FindCompressedFixturePath();
			if (brPath != null && File.Exists(brPath))
			{
				byte[] compressed = File.ReadAllBytes(brPath);
				string json = ElectronicObserverDbContext.DecompressContent(compressed);
				var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
				if (map != null)
				{
					foreach (var kvp in map)
					{
						dict[kvp.Key.Replace('\\', '/')] = kvp.Value;
						string simpleName = Path.GetFileName(kvp.Key);
						if (!dict.ContainsKey(simpleName))
						{
							dict[simpleName] = kvp.Value;
						}
					}
				}
			}

			return dict;
		});

		private static string? FindCompressedFixturePath()
		{
			string[] candidates = new[]
			{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "en_parity_fixtures.json.br"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fixtures", "en_parity_fixtures.json.br"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Fixtures", "en_parity_fixtures.json.br"),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ElectronicObserverTests", "Fixtures", "en_parity_fixtures.json.br"),
			};

			return candidates.FirstOrDefault(File.Exists);
		}

		private static string GetFixtureJson(string subFolder, string fileName)
		{
			string key = $"{subFolder}/{fileName}".Replace('\\', '/');
			if (FixtureCache.Value.TryGetValue(key, out string? cachedContent))
			{
				return cachedContent;
			}
			if (FixtureCache.Value.TryGetValue(fileName, out cachedContent))
			{
				return cachedContent;
			}

			// Local output directory fallback if unpacked loose files exist
			string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subFolder, fileName);
			if (File.Exists(localPath))
			{
				return File.ReadAllText(localPath);
			}

			throw new FileNotFoundException($"Fixture file not found in compressed cache or local directory: {subFolder}/{fileName}");
		}

		private static List<SortieRecord> LoadSortieRecords(string subFolder, string fileName)
		{
			string json = GetFixtureJson(subFolder, fileName);
			return JsonSerializer.Deserialize<List<SortieRecord>>(json, JsonOptions) ?? new List<SortieRecord>();
		}

		private static List<ApiFile> LoadApiFiles(string subFolder, string fileName)
		{
			string json = GetFixtureJson(subFolder, fileName);
			return JsonSerializer.Deserialize<List<ApiFile>>(json, JsonOptions) ?? new List<ApiFile>();
		}

		private static SortieRecord CloneSortieRecord(SortieRecord source)
		{
			string json = JsonSerializer.Serialize(source, JsonOptions);
			return JsonSerializer.Deserialize<SortieRecord>(json, JsonOptions)!;
		}

		#endregion

		#region 1. ReplayService Parity Tests

		[Theory]
		[InlineData("SortieDetailTest1.json")]
		[InlineData("SortieDetailTest2.json")]
		[InlineData("SortieDetailTest4.json")]
		public void ReplayExport_EquipmentImprovementAndAceSlotMapping_PreservesIndicesAndSlots(string fileName)
		{
			// What: Verify each ship's regular + expansion equipment slots output Equip, Stars, and Ace with 1:1 index alignment,
			//       preserving improvement stars (> 0) and ace levels without index displacement or drops.
			var sorties = LoadSortieRecords("SortieDetail", fileName);
			Assert.NotEmpty(sorties);
			var sortie = sorties.First();

			var replay = sortie.ToReplayData();
			Assert.NotNull(replay);

			var allReplayShips = new[] { replay.Fleet1, replay.Fleet2, replay.Fleet3, replay.Fleet4 }
				.Where(f => f != null)
				.SelectMany(f => f!)
				.ToList();

			Assert.NotEmpty(allReplayShips);

			foreach (var ship in allReplayShips)
			{
				Assert.Equal(ship.Equip.Count, ship.Stars.Count);
				Assert.Equal(ship.Equip.Count, ship.Ace.Count);
				// Empty slots in KC3 / EN format must be 0 (not -1 or missing)
				Assert.DoesNotContain(-1, ship.Equip);
			}

			// In test data, at least one ship has star improvements > 0
			Assert.Contains(allReplayShips, s => s.Stars.Any(v => v > 0));
		}

		[Fact]
		public void ReplayExport_ImprovementMatchesSourceRecordExactly()
		{
			// What: Verify exact correspondence between SortieShip (equipment + expansion) and ReplayShip Equip/Stars/Ace lists.
			var sorties = LoadSortieRecords("SortieDetail", "SortieDetailTest2.json");
			var sortie = sorties.First();

			var replay = sortie.ToReplayData();
			var replayShips = replay.Fleet1;
			var sourceShips = sortie.FleetData.Fleets[0]!.Ships;

			Assert.NotNull(replayShips);
			Assert.Equal(sourceShips.Count, replayShips.Count);

			for (int i = 0; i < sourceShips.Count; i++)
			{
				var source = sourceShips[i];
				var exported = replayShips[i];

				var slots = source.EquipmentSlots.Concat(new[] { source.ExpansionSlot }).ToList();

				var expectedEquips = slots.Select(s => s?.Equipment?.Id ?? 0).ToList();
				var expectedStars = slots.Select(s => s?.Equipment?.Level ?? 0).ToList();
				var expectedAces = slots.Select(s => s?.Equipment?.AircraftLevel ?? 0).ToList();

				Assert.Equal(expectedEquips, exported.Equip);
				Assert.Equal(expectedStars, exported.Stars);
				Assert.Equal(expectedAces, exported.Ace);
			}
		}

		[Fact]
		public void ReplayExport_AirBasesExportedWhenWorldDiffersFromMap()
		{
			// What: Verify event map (World=58, Map=4, World != Map) exports air bases without dropping them.
			var sorties = LoadSortieRecords("SortieDetail", "SortieDetailTest1.json");
			var sortie = sorties.First();

			Assert.NotEqual(sortie.World, sortie.Map);
			Assert.NotEmpty(sortie.FleetData.AirBases);

			var replay = sortie.ToReplayData();
			Assert.NotNull(replay.AirBases);
			Assert.Equal(sortie.FleetData.AirBases.Count, replay.AirBases.Count);
		}

		[Fact]
		public void ReplayService_GenerateReplayJson_ProducesValidKC3Json()
		{
			// What: Verify GenerateReplayJson produces valid, parseable JSON with KC3-compatible top-level keys.
			var sorties = LoadSortieRecords("SortieDetail", "SortieDetailTest1.json");
			var sortie = sorties.First();

			string json = ReplayService.GenerateReplayJson(sortie);
			Assert.False(string.IsNullOrWhiteSpace(json));

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			Assert.True(root.TryGetProperty("world", out _));
			Assert.True(root.TryGetProperty("mapnum", out _));
			Assert.True(root.TryGetProperty("fleet1", out _));
			Assert.True(root.TryGetProperty("lbas", out _));
		}

		#endregion

		#region 2. SortieCostCalculator Parity & Edge Case Tests

		[Theory]
		[InlineData("SortieCostTest01")]
		[InlineData("SortieCostTest02")]
		[InlineData("SortieCostTest03")]
		[InlineData("SortieCostTest11")]
		public void SortieCost_ActualDiffVsSimulationParity_SortieCostTest0(string testFilePrefix)
		{
			// What: Verify SortieCostTest0 pattern - Cost calculation with actual FleetAfterSortieData matches
			//       the cost calculated by simulating API battle/loss packets when FleetAfterSortieData is null.
			var sorties = LoadSortieRecords("SortieCost", $"{testFilePrefix}SortieRecords.json");
			var apiFiles = LoadApiFiles("SortieCost", $"{testFilePrefix}ApiFiles.json");

			Assert.NotEmpty(sorties);

			foreach (var sortie in sorties)
			{
				var sortieApiFiles = apiFiles.Where(f => f.SortieRecordId == sortie.Id).ToList();

				// 1. Cost with actual post-sortie diff
				var actualCost = SortieCostCalculator.CalculateAndCache(sortie, sortieApiFiles);

				// 2. Clone and clear FleetAfterSortieData to trigger simulation mode
				var simulatedSortie = CloneSortieRecord(sortie);
				simulatedSortie.FleetAfterSortieData = null;

				var simulatedCost = SortieCostCalculator.CalculateAndCache(simulatedSortie, sortieApiFiles);

				Assert.NotNull(actualCost);
				Assert.NotNull(simulatedCost);

				// Supply cost comparison
				if (actualCost.SortieFleetSupplyCost != null)
				{
					Assert.NotNull(simulatedCost.SortieFleetSupplyCost);
					Assert.Equal(actualCost.SortieFleetSupplyCost.Fuel, simulatedCost.SortieFleetSupplyCost.Fuel);
					Assert.Equal(actualCost.SortieFleetSupplyCost.Ammo, simulatedCost.SortieFleetSupplyCost.Ammo);
				}

				// Repair cost comparison
				if (actualCost.SortieFleetRepairCost != null)
				{
					Assert.NotNull(simulatedCost.SortieFleetRepairCost);
					Assert.Equal(actualCost.SortieFleetRepairCost.Fuel, simulatedCost.SortieFleetRepairCost.Fuel);
					Assert.Equal(actualCost.SortieFleetRepairCost.Steel, simulatedCost.SortieFleetRepairCost.Steel);
				}

				// Air base costs comparison
				if (actualCost.TotalAirBaseSortieCost != null)
				{
					Assert.NotNull(simulatedCost.TotalAirBaseSortieCost);
					Assert.Equal(actualCost.TotalAirBaseSortieCost.Fuel, simulatedCost.TotalAirBaseSortieCost.Fuel);
					Assert.Equal(actualCost.TotalAirBaseSortieCost.Ammo, simulatedCost.TotalAirBaseSortieCost.Ammo);
				}

				if (actualCost.TotalAirBaseSupplyCost != null)
				{
					Assert.NotNull(simulatedCost.TotalAirBaseSupplyCost);
					Assert.Equal(actualCost.TotalAirBaseSupplyCost.Fuel, simulatedCost.TotalAirBaseSupplyCost.Fuel);
					Assert.Equal(actualCost.TotalAirBaseSupplyCost.Bauxite, simulatedCost.TotalAirBaseSupplyCost.Bauxite);
				}
			}
		}

		[Theory]
		[InlineData(102, 0.30)] // 長門一斉射 (昼20% + 10% = 30%)
		[InlineData(103, 0.30)] // 陸奥一斉射 (昼20% + 10% = 30%)
		[InlineData(400, 0.36)] // 大和・武蔵特殊攻撃 3隻 (昼20% + 16% = 36%)
		[InlineData(401, 0.32)] // 大和・武蔵特殊攻撃 2隻 (昼20% + 12% = 32%)
		public void SortieCost_SpecialAttacks_SimulationCalculatesExtraAmmoLoss(int specialAttackType, double expectedAmmoRate)
		{
			// What: Verify special attack types trigger additional ammunition consumption rates as specified by KC mechanics.
			var record = new SortieRecord
			{
				World = 5,
				Map = 5,
				FleetData = new SortieFleetData
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
									Id = 99999, // ダミーマスターID (KCDatabase に非存在のため s.Ammo=100 が maxAmmo として使われる)
									Level = 99,
									Fuel = 100,
									Ammo = 100,
									MaxHp = 100,
								}
							}
						}
					}
				},
				// FleetAfterSortieData is null -> triggers simulation
				FleetAfterSortieData = null,
			};

			var battleApi = new ApiFile
			{
				SortieRecordId = record.Id,
				ApiFileType = ApiFileType.Response,
				Name = "api_req_sortie/battle",
				Content = $"{{\"api_result\":1,\"api_data\":{{\"api_flavor_info\":{{\"api_type\":{specialAttackType},\"api_ship_idx\":[1]}},\"api_hougeki1\":{{\"api_at_list\":[1],\"api_df_list\":[[1]],\"api_damage\":[[0]]}},\"api_f_nowhps\":[100],\"api_f_maxhps\":[100],\"api_e_nowhps\":[50],\"api_e_maxhps\":[50]}}}}"
			};

			var cost = SortieCostCalculator.CalculateAndCache(record, new[] { battleApi });

			Assert.NotNull(cost);
			Assert.NotNull(cost.SortieFleetSupplyCost);

			int expectedAmmo = (int)(100 * expectedAmmoRate);
			Assert.Equal(expectedAmmo, cost.SortieFleetSupplyCost.Ammo);
		}

		[Fact]
		public void SortieCost_MarriedShip_Calculates15PercentDiscount()
		{
			// What: Verify married ships (Lv >= 100) receive 15% discount on resupply costs.
			var record = new SortieRecord
			{
				FleetData = new SortieFleetData
				{
					FleetId = 1,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Ships = new List<SortieShip>
							{
								new SortieShip { Id = 1, Level = 120, Fuel = 100, Ammo = 100, MaxHp = 100 }
							}
						}
					}
				},
				FleetAfterSortieData = new SortieFleetData
				{
					FleetId = 1,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Ships = new List<SortieShip>
							{
								new SortieShip { Id = 1, Level = 120, Fuel = 80, Ammo = 80, MaxHp = 100 }
							}
						}
					}
				}
			};

			var cost = SortieCostCalculator.CalculateAndCache(record, Enumerable.Empty<ApiFile>());

			Assert.NotNull(cost);
			Assert.NotNull(cost.SortieFleetSupplyCost);
			// 20 lost * 0.85 = 17
			Assert.Equal(17, cost.SortieFleetSupplyCost.Fuel);
			Assert.Equal(17, cost.SortieFleetSupplyCost.Ammo);
		}

		[Fact]
		public void SortieCost_Whirlpool_LossCalculatedCorrectly()
		{
			// What: Verify whirlpool (api_happening) fuel and ammo losses are properly calculated into total cost.
			var record = new SortieRecord
			{
				FleetData = new SortieFleetData
				{
					FleetId = 1,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Ships = new List<SortieShip>
							{
								new SortieShip { Id = 1, Level = 50, Fuel = 50, Ammo = 50, MaxHp = 50 }
							}
						}
					}
				},
				FleetAfterSortieData = null
			};

			var whirlpoolApi = new ApiFile
			{
				SortieRecordId = record.Id,
				ApiFileType = ApiFileType.Response,
				Name = "api_req_map/next",
				Content = "{\"api_result\":1,\"api_data\":{\"api_event_id\":7,\"api_happening\":{\"api_mst_id\":1,\"api_count\":15}}}"
			};

			var cost = SortieCostCalculator.CalculateAndCache(record, new[] { whirlpoolApi });

			Assert.NotNull(cost);
			Assert.NotNull(cost.SortieFleetSupplyCost);
			Assert.Equal(15, cost.SortieFleetSupplyCost.Fuel);
		}

		[Theory]
		[InlineData(10, 0, 39, 0, 197, 26)]  // 橘花改 (10 * 13 * 0.2 = 26)
		[InlineData(10, 0, 39, 0, 200, 28)]  // 景雲改 (10 * 14 * 0.2 = 28)
		[InlineData(10, 0, 51, 0, 399, 34)]  // Ho229 (10 * 16.8 * 0.2 = 33.6 -> 34)
		[InlineData(10, 0, 52, 58, 501, 22)] // 震電改 (10 * 11 * 0.2 = 22)
		[InlineData(0, 0, 39, 0, 197, 0)]    // 0 aircraft -> 0 steel
		[InlineData(-5, 0, 39, 0, 197, 0)]   // Negative aircraft -> 0 steel
		[InlineData(10, 0, 6, 0, 100, 0)]    // Not a jet (cardType 6) -> 0 steel
		public void SortieCost_CalculateJetSteelCost_Boundaries(int aircraft, int cost, int cardType, int catType, int equipId, int expected)
		{
			// What: Verify jet aircraft steel consumption handles all specific aircraft formulas, zero, negative, and non-jet bounds.
			int result = SortieCostCalculator.CalculateJetSteelCost(aircraft, cost, cardType, catType, equipId);
			Assert.Equal(expected, result);
		}

		[Fact]
		public void SortieCost_AirBaseResupplyCost_BauxiteAndFuel()
		{
			// What: Verify airbase aircraft replenishment costs 5 bauxite and 3 fuel per lost plane.
			var record = new SortieRecord
			{
				FleetData = new SortieFleetData
				{
					AirBases = new List<SortieAirBase>
					{
						new SortieAirBase
						{
							AirCorpsId = 1,
							MapAreaId = 6,
							ActionKind = 1,
							Squadrons = new List<SortieAirBaseSquadron>
							{
								new SortieAirBaseSquadron
								{
									State = 1,
									AircraftCurrent = 18,
									EquipmentSlot = new SortieEquipmentSlot { AircraftCurrent = 18 }
								}
							}
						}
					}
				},
				FleetAfterSortieData = new SortieFleetData
				{
					AirBases = new List<SortieAirBase>
					{
						new SortieAirBase
						{
							AirCorpsId = 1,
							MapAreaId = 6,
							ActionKind = 1,
							Squadrons = new List<SortieAirBaseSquadron>
							{
								new SortieAirBaseSquadron
								{
									State = 1,
									AircraftCurrent = 14, // 4 lost
									EquipmentSlot = new SortieEquipmentSlot { AircraftCurrent = 14 }
								}
							}
						}
					}
				}
			};

			var cost = SortieCostCalculator.CalculateAndCache(record, Enumerable.Empty<ApiFile>());

			Assert.NotNull(cost);
			Assert.NotNull(cost.TotalAirBaseSupplyCost);
			// 4 lost * 5 bauxite = 20
			Assert.Equal(20, cost.TotalAirBaseSupplyCost.Bauxite);
			// 4 lost * 3 fuel = 12
			Assert.Equal(12, cost.TotalAirBaseSupplyCost.Fuel);
		}

		#endregion

		#region 3. SortieDetail & SortieNodeEmulator Parity Tests

		[Fact]
		public void SortieDetail_CombinedFleet_DayToNightHPCarryOver()
		{
			// What: Verify combined fleet battle nodes carry over HP seamlessly from day battle end to night battle start.
			var sorties = LoadSortieRecords("SortieDetail", "SortieDetailTest1.json");
			var sortie = sorties.First();

			Assert.NotEmpty(sortie.ApiFiles);
			var detail = SortieNodeEmulator.EmulateSortie(sortie, sortie.ApiFiles);
			Assert.NotNull(detail);
			Assert.NotEmpty(detail.Nodes);

			// Find battle node that had both day and night combat
			var dayNightNode = detail.Nodes.FirstOrDefault(n => n.HasNightBattle && n.FriendlyShipsDayAfter != null);
			Assert.NotNull(dayNightNode);

			Assert.NotNull(dayNightNode!.FriendlyShipsDayAfter);
			Assert.NotNull(dayNightNode.FriendlyShipsNightBefore);
			Assert.Equal(dayNightNode.FriendlyShipsDayAfter.Count, dayNightNode.FriendlyShipsNightBefore.Count);

			for (int i = 0; i < dayNightNode.FriendlyShipsDayAfter.Count; i++)
			{
				Assert.Equal(dayNightNode.FriendlyShipsDayAfter[i].HpAfter, dayNightNode.FriendlyShipsNightBefore[i].HpBefore);
			}

			if (dayNightNode.EscortShipsDayAfter != null && dayNightNode.EscortShipsNightBefore != null)
			{
				Assert.Equal(dayNightNode.EscortShipsDayAfter.Count, dayNightNode.EscortShipsNightBefore.Count);
				for (int i = 0; i < dayNightNode.EscortShipsDayAfter.Count; i++)
				{
					Assert.Equal(dayNightNode.EscortShipsDayAfter[i].HpAfter, dayNightNode.EscortShipsNightBefore[i].HpBefore);
				}

				// Zuihou Kai Ni B is the 2nd ship (index 1) in escort fleet and has HP 32 in SortieDetailTest1
				Assert.Equal(32, dayNightNode.EscortShipsDayAfter[1].HpAfter);
				Assert.Equal(32, dayNightNode.EscortShipsNightBefore[1].HpBefore);
			}
		}

		[Fact]
		public void SortieDetail_SpecialEffectItems_SnapshotPreserved()
		{
			// What: Verify SpecialEffectItems (e.g., Sea Ribbon = 1, White Tasuki = 2) are preserved on SortieShip models.
			var sorties = LoadSortieRecords("SortieDetail", "SortieDetailTest2.json");
			var sortie = sorties.First();

			var allShips = sortie.FleetData.Fleets.Where(f => f != null).SelectMany(f => f!.Ships).ToList();
			Assert.NotEmpty(allShips);

			var shipsWithItems = allShips.Where(s => s.SpecialEffectItems != null && s.SpecialEffectItems.Count > 0).ToList();
			Assert.NotEmpty(shipsWithItems);

			foreach (var ship in shipsWithItems)
			{
				Assert.All(ship.SpecialEffectItems!, item => Assert.True(item.ApiKind > 0));
			}
		}

		#endregion
		#region 4. Fixture Archive & CSV Export Parity Tests

		[Fact]
		public void Fixtures_SelfContainedArchive_ContainsAllRequiredFixtures()
		{
			// What: Verify compressed en_parity_fixtures.json.br exists and unpacks all required fixtures without external dependencies.
			string[] requiredKeys = new[]
			{
				"SortieDetail/SortieDetailTest1.json",
				"SortieDetail/SortieDetailTest2.json",
				"SortieDetail/SortieDetailTest4.json",
				"SortieCost/SortieCostTest01SortieRecords.json",
				"SortieCost/SortieCostTest01ApiFiles.json",
				"SortieCost/SortieCostTest02SortieRecords.json",
				"SortieCost/SortieCostTest02ApiFiles.json",
				"SortieCost/SortieCostTest03SortieRecords.json",
				"SortieCost/SortieCostTest03ApiFiles.json",
				"SortieCost/SortieCostTest11SortieRecords.json",
				"SortieCost/SortieCostTest11ApiFiles.json",
				"CsvExport/夜戦1.json"
			};

			var cache = FixtureCache.Value;
			Assert.NotEmpty(cache);

			foreach (string key in requiredKeys)
			{
				Assert.True(cache.ContainsKey(key), $"Fixture archive missing key: {key}");
				Assert.False(string.IsNullOrWhiteSpace(cache[key]), $"Fixture content empty for key: {key}");
			}
		}

		[Fact]
		public void SortieCsvExport_DetailData_ExportsSpecificGoldenValues()
		{
			// What: Verify SortieCsvExportService generates CSVs with exact golden column values for Shelling, Night, RedNight, BattleRank, Torpedo, AirBattle, and AirBase.
			var sorties = LoadSortieRecords("SortieDetail", "SortieDetailTest1.json");
			var sortie = sorties.First();
			var sortieInputs = new[] { (Record: sortie, ApiFiles: (IEnumerable<ApiFile>)sortie.ApiFiles) };

			// 1. BattleRank verification (SortieDetailTest1, Cell 7 boss node)
			string battleRankCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.BattleRank, sortieInputs);
			var rankLines = battleRankCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(rankLines.Length >= 3, $"Expected header + 2 data lines, got:\n{battleRankCsv}");
			Assert.Equal("SortieId,Map,Cell,CellDisplay,WinRank,BaseExp,AdmiralExp,MvpShip,DropShip", rankLines[0]);

			// Boss battle rank row (index 2: Cell 7)
			var rankCols = rankLines[2].Split(',');
			Assert.Equal(sortie.Id.ToString(), rankCols[0]);
			Assert.Equal("58-4", rankCols[1]);
			Assert.Equal("7", rankCols[2]);
			Assert.Equal("D (7)", rankCols[3]);
			Assert.Equal("S", rankCols[4]);
			Assert.Equal("150", rankCols[5]); // BaseExp
			Assert.Equal("175", rankCols[6]); // AdmiralExp
			Assert.Equal("大鳳改", rankCols[7]); // MvpShip (マスタデータロード時解決名)
			Assert.Equal("綾波", rankCols[8]); // DropShip

			// 2. Shelling verification (Day battle 2nd round in SortieDetailTest1)
			string shellingCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.Shelling, sortieInputs);
			var shellingLines = shellingCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(shellingLines.Length >= 2);
			Assert.Equal("SortieId,Map,Cell,CellDisplay,Phase,Attacker,Defender,AttackerIndex,DefenderIndex,Damage,IsCritical,AttackType", shellingLines[0]);

			// 砲撃戦2巡目の大鳳改行
			var shellingTaihouRow = shellingLines.Select(l => l.Split(',')).FirstOrDefault(cols => cols.Length >= 12 && cols[4] == "砲撃戦(2巡目)" && cols[5] == "大鳳改");
			Assert.NotNull(shellingTaihouRow);
			Assert.Equal(sortie.Id.ToString(), shellingTaihouRow[0]);
			Assert.Equal("58-4", shellingTaihouRow[1]);
			Assert.Equal("7", shellingTaihouRow[2]);
			Assert.Equal("D (7)", shellingTaihouRow[3]);
			Assert.Equal("砲撃戦(2巡目)", shellingTaihouRow[4]);
			Assert.Equal("大鳳改", shellingTaihouRow[5]);
			Assert.Equal("米駆逐棲姫(量産型)", shellingTaihouRow[6]);
			Assert.Equal("1", shellingTaihouRow[7]);
			Assert.Equal("7", shellingTaihouRow[8]);
			Assert.Equal("155", shellingTaihouRow[9]);
			Assert.Equal("False", shellingTaihouRow[10]);
			Assert.Equal("戦車CI", shellingTaihouRow[11]);

			// 3. Combined fleet night battle (Night: 第2艦隊・大淀改の夜戦連撃)
			string combinedNightCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.Night, sortieInputs);
			var combinedNightLines = combinedNightCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(combinedNightLines.Length >= 2);
			Assert.Equal("SortieId,Map,Cell,CellDisplay,Attacker,Defender,AttackerIndex,DefenderIndex,Damage,IsCritical,AttackType", combinedNightLines[0]);

			var combinedNightFirstRow = combinedNightLines[1].Split(',');
			Assert.Equal(sortie.Id.ToString(), combinedNightFirstRow[0]);
			Assert.Equal("58-4", combinedNightFirstRow[1]);
			Assert.Equal("7", combinedNightFirstRow[2]);
			Assert.Equal("D (7)", combinedNightFirstRow[3]);
			Assert.Equal("大淀改", combinedNightFirstRow[4]); // 味方第2艦隊旗艦 (rawAt=6 -> atIdx=1)
			Assert.Equal("米駆逐棲姫(量産型)", combinedNightFirstRow[5]); // 敵旗艦 (rawDf=0 -> dfIdx=7)
			Assert.Equal("1", combinedNightFirstRow[6]);
			Assert.Equal("7", combinedNightFirstRow[7]);
			Assert.Equal("73", combinedNightFirstRow[8]);
			Assert.Equal("False", combinedNightFirstRow[9]);
			Assert.Equal("連撃", combinedNightFirstRow[10]);

			// 4. Enemy night attack verification (RedNight: 夜戦1.json 5-3 から敵重巡リ級の夜戦攻撃)
			var nightSorties = LoadSortieRecords("CsvExport", "夜戦1.json");
			var nightInputs = nightSorties.Select(s => (Record: s, ApiFiles: (IEnumerable<ApiFile>)s.ApiFiles)).ToList();
			string redNightCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.RedNight, nightInputs);
			var redNightLines = redNightCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(redNightLines.Length >= 2);
			Assert.Equal("SortieId,Map,Cell,CellDisplay,Attacker,Defender,Damage,IsCritical,AttackType", redNightLines[0]);

			var redNightFirstRow = redNightLines[1].Split(',');
			Assert.Equal("17545", redNightFirstRow[0]);
			Assert.Equal("5-3", redNightFirstRow[1]);
			Assert.Equal("9", redNightFirstRow[2]);
			Assert.Equal("I (9)", redNightFirstRow[3]);
			Assert.Equal("重巡リ級 flagship", redNightFirstRow[4]);
			Assert.Equal("妙高改二", redNightFirstRow[5]);
			Assert.Equal("37", redNightFirstRow[6]);
			Assert.Equal("False", redNightFirstRow[7]);
			Assert.Equal("通常", redNightFirstRow[8]);

			// 5. Regular fleet night battle (Night: 夜戦1.json 5-3 battle)
			string nightCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.Night, nightInputs);
			var nightLines = nightCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(nightLines.Length >= 2);
			Assert.Equal("SortieId,Map,Cell,CellDisplay,Attacker,Defender,AttackerIndex,DefenderIndex,Damage,IsCritical,AttackType", nightLines[0]);

			var nightFirstRow = nightLines[1].Split(',');
			Assert.Equal("17545", nightFirstRow[0]);
			Assert.Equal("5-3", nightFirstRow[1]);
			Assert.Equal("9", nightFirstRow[2]);
			Assert.Equal("I (9)", nightFirstRow[3]);
			Assert.Equal("春風改", nightFirstRow[4]);
			Assert.Equal("軽巡ホ級 elite", nightFirstRow[5]);
			Assert.Equal("1", nightFirstRow[6]);
			Assert.Equal("10", nightFirstRow[7]);
			Assert.Equal("233", nightFirstRow[8]);
			Assert.Equal("False", nightFirstRow[9]);
			Assert.Equal("夜戦特殊(13)", nightFirstRow[10]);

			// 6. Torpedo verification (SortieDetailTest1 closing torpedo)
			string torpedoCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.Torpedo, sortieInputs);
			var torpedoLines = torpedoCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(torpedoLines.Length >= 2);
			Assert.Equal("SortieId,Map,Cell,CellDisplay,Phase,Attacker,Defender,Damage", torpedoLines[0]);

			var torpedoFirstRow = torpedoLines[1].Split(',');
			Assert.Equal(sortie.Id.ToString(), torpedoFirstRow[0]);
			Assert.Equal("58-4", torpedoFirstRow[1]);
			Assert.Equal("7", torpedoFirstRow[2]);
			Assert.Equal("D (7)", torpedoFirstRow[3]);
			Assert.Equal("閉幕雷撃", torpedoFirstRow[4]);
			Assert.Equal("味方雷撃", torpedoFirstRow[5]);
			Assert.Equal("軽母ヌ級改 flagship", torpedoFirstRow[6]);
			Assert.Equal("7", torpedoFirstRow[7]);

			// 7. AirBattle verification (SortieDetailTest1 aerial combat)
			string airCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.AirBattle, sortieInputs);
			var airLines = airCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(airLines.Length >= 3);
			Assert.Equal("SortieId,Map,Cell,CellDisplay,AirControlState,FriendlyAircraftLost,FriendlyDamageTotal,EnemyDamageTotal", airLines[0]);

			var airCell7Row = airLines[2].Split(',');
			Assert.Equal(sortie.Id.ToString(), airCell7Row[0]);
			Assert.Equal("58-4", airCell7Row[1]);
			Assert.Equal("7", airCell7Row[2]);
			Assert.Equal("D (7)", airCell7Row[3]);
			Assert.Equal("1", airCell7Row[4]); // AirControlState (AS+)
			Assert.Equal("48", airCell7Row[5]); // FriendlyAircraftLost
			Assert.Equal("0", airCell7Row[6]);  // FriendlyDamageTotal
			Assert.Equal("327", airCell7Row[7]); // EnemyDamageTotal

			// 8. AirBase strike verification (SortieDetailTest4 airbase strike)
			var s4 = LoadSortieRecords("SortieDetail", "SortieDetailTest4.json").First();
			var s4Inputs = new[] { (Record: s4, ApiFiles: (IEnumerable<ApiFile>)s4.ApiFiles) };
			string airBaseCsv = SortieCsvExportService.ExportCsv(SortieCsvExportType.AirBase, s4Inputs);
			var airBaseLines = airBaseCsv.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			Assert.True(airBaseLines.Length >= 2);
			Assert.Equal("SortieId,Map,Cell,CellDisplay,PhaseName,EnemyDamageTotal", airBaseLines[0]);

			var airBaseFirstRow = airBaseLines[1].Split(',');
			Assert.Equal(s4.Id.ToString(), airBaseFirstRow[0]);
			Assert.Equal("59-2", airBaseFirstRow[1]);
			Assert.Equal("26", airBaseFirstRow[2]);
			Assert.Equal("S (26)", airBaseFirstRow[3]);
			Assert.Equal("基地航空隊", airBaseFirstRow[4]);
			Assert.Equal("749", airBaseFirstRow[5]);
		}

		#endregion
	}
}
