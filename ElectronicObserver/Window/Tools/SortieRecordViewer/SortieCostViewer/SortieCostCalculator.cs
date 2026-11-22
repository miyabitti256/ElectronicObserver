#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ElectronicObserver.Data;
using ElectronicObserver.Database;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.SortieCostViewer
{
	public static class SortieCostCalculator
	{
		/// <summary>
		/// 出撃レコードの消費コストを計算し、キャッシュ（DB更新）します。
		/// </summary>
		public static CalculatedSortieCost CalculateAndCache(SortieRecord record, IEnumerable<ApiFile> apiFiles, ElectronicObserverDbContext? db = null)
		{
			if (record == null) throw new ArgumentNullException(nameof(record));

			// 既に計算済みの場合はキャッシュを返す
			if (record.CalculatedSortieCost?.SortieFleetSupplyCost != null)
			{
				return record.CalculatedSortieCost;
			}

			var cost = new CalculatedSortieCost();
			var emulatorResult = SortieNodeEmulator.EmulateSortie(record, apiFiles);

			// 1. 補給コスト計算 (艦隊)
			cost.SortieFleetSupplyCost = CalculateSupplyCost(record, emulatorResult);

			// 2. 修理コスト & 損傷集計
			var repairResult = CalculateRepairCost(record, emulatorResult);
			cost.SortieFleetRepairCost = repairResult.Cost;
			cost.DamageStateCounts = repairResult.DamageCounts;

			// 3. 基地航空隊出撃・補充コスト
			cost.TotalAirBaseSortieCost = CalculateAirBaseSortieCost(record);
			cost.TotalAirBaseSupplyCost = CalculateAirBaseSupplyCost(record, emulatorResult);

			// 4. 噴式機コスト
			cost.SortieFleetJetCost = CalculateJetCost(record);
			cost.AirBaseJetCost = SortieCostModel.Zero;

			// 5. 支援艦隊補給コスト
			cost.NodeSupportSupplyCost = CalculateSupportCost(record, isBoss: false, emulatorResult);
			cost.BossSupportSupplyCost = CalculateSupportCost(record, isBoss: true, emulatorResult);

			// 6. 消費資材リスト
			cost.ConsumedItems = new List<ConsumableItemData>();
			if (repairResult.BucketsNeeded > 0)
			{
				cost.ConsumedItems.Add(new ConsumableItemData
				{
					Id = 6, // 高速修復材 (バケツ)
					Count = repairResult.BucketsNeeded,
				});
			}

			record.CalculatedSortieCost = cost;

			if (db != null)
			{
				try
				{
					db.Sorties.Update(record);
					db.SaveChanges();
				}
				catch
				{
					// Why not throw: Cache saving is an optimization; failure to write back shouldn't crash viewer.
				}
			}

			return cost;
		}

		/// <summary>
		/// 複数出撃のコスト合計を集計します。
		/// </summary>
		public static CalculatedSortieCost CalculateAggregateCost(IEnumerable<CalculatedSortieCost> costs)
		{
			if (costs == null) throw new ArgumentNullException(nameof(costs));

			var aggregate = new CalculatedSortieCost
			{
				SortieFleetSupplyCost = SortieCostModel.Zero,
				SortieFleetRepairCost = SortieCostModel.Zero,
				SortieFleetJetCost = SortieCostModel.Zero,
				NodeSupportSupplyCost = SortieCostModel.Zero,
				BossSupportSupplyCost = SortieCostModel.Zero,
				TotalAirBaseSortieCost = SortieCostModel.Zero,
				TotalAirBaseSupplyCost = SortieCostModel.Zero,
				AirBaseJetCost = SortieCostModel.Zero,
				DamageStateCounts = new Dictionary<string, int>
				{
					["Healthy"] = 0,
					["Light"] = 0,
					["Medium"] = 0,
					["Heavy"] = 0,
				},
				ConsumedItems = new List<ConsumableItemData>(),
			};

			int totalBuckets = 0;

			foreach (var c in costs)
			{
				if (c == null) continue;

				aggregate.SortieFleetSupplyCost += c.SortieFleetSupplyCost ?? SortieCostModel.Zero;
				aggregate.SortieFleetRepairCost += c.SortieFleetRepairCost ?? SortieCostModel.Zero;
				aggregate.SortieFleetJetCost += c.SortieFleetJetCost ?? SortieCostModel.Zero;
				aggregate.NodeSupportSupplyCost += c.NodeSupportSupplyCost ?? SortieCostModel.Zero;
				aggregate.BossSupportSupplyCost += c.BossSupportSupplyCost ?? SortieCostModel.Zero;
				aggregate.TotalAirBaseSortieCost += c.TotalAirBaseSortieCost ?? SortieCostModel.Zero;
				aggregate.TotalAirBaseSupplyCost += c.TotalAirBaseSupplyCost ?? SortieCostModel.Zero;
				aggregate.AirBaseJetCost += c.AirBaseJetCost ?? SortieCostModel.Zero;

				if (c.DamageStateCounts != null)
				{
					foreach (var kvp in c.DamageStateCounts)
					{
						if (aggregate.DamageStateCounts.ContainsKey(kvp.Key))
						{
							aggregate.DamageStateCounts[kvp.Key] += kvp.Value;
						}
					}
				}

				if (c.ConsumedItems != null)
				{
					foreach (var item in c.ConsumedItems)
					{
						if (item.Id == 6 || item.Id == 1) totalBuckets += item.Count;
					}
				}
			}

			if (totalBuckets > 0)
			{
				aggregate.ConsumedItems.Add(new ConsumableItemData
				{
					Id = 6,
					Count = totalBuckets,
				});
			}

			return aggregate;
		}

		private static SortieCostModel CalculateSupplyCost(SortieRecord record, SortieDetailData details)
		{
			bool isCombined = (int)(record.FleetData?.CombinedFlag ?? 0) > 0;
			int fleetCount = isCombined ? 2 : 1;

			int totalFuel = 0;
			int totalAmmo = 0;
			int totalBauxite = 0;

			// 帰投後データがある場合
			if (record.FleetAfterSortieData?.Fleets != null && record.FleetData?.Fleets != null)
			{
				for (int f = 0; f < fleetCount; f++)
				{
					var beforeFleet = record.FleetData.Fleets.Skip(f).FirstOrDefault();
					var afterFleet = record.FleetAfterSortieData.Fleets.Skip(f).FirstOrDefault();

					if (beforeFleet?.Ships == null || afterFleet?.Ships == null) continue;

					for (int i = 0; i < beforeFleet.Ships.Count && i < afterFleet.Ships.Count; i++)
					{
						var b = beforeFleet.Ships[i];
						var a = afterFleet.Ships[i];
						if (b == null || a == null) continue;

						int fuelDiff = Math.Max(0, b.Fuel - a.Fuel);
						int ammoDiff = Math.Max(0, b.Ammo - a.Ammo);

						// ケッコン艦補正 (0.85)
						totalFuel += b.Level > 99 && fuelDiff > 0 ? Math.Max(1, (int)(fuelDiff * 0.85)) : fuelDiff;
						totalAmmo += b.Level > 99 && ammoDiff > 0 ? Math.Max(1, (int)(ammoDiff * 0.85)) : ammoDiff;

						int bAircraft = b.EquipmentSlots?.Sum(s => s?.AircraftCurrent ?? 0) ?? 0;
						int aAircraft = a.EquipmentSlots?.Sum(s => s?.AircraftCurrent ?? 0) ?? 0;
						totalBauxite += Math.Max(0, bAircraft - aAircraft) * 5;
					}
				}

				return new SortieCostModel { Fuel = totalFuel, Ammo = totalAmmo, Bauxite = totalBauxite };
			}

			// 帰投後データがない場合は各ノードの戦闘・渦潮・特殊攻撃をシミュレーションして推計
			for (int f = 0; f < fleetCount; f++)
			{
				var fleet = record.FleetData?.Fleets?.Skip(f).FirstOrDefault();
				if (fleet?.Ships == null) continue;

				for (int sIndex = 0; sIndex < fleet.Ships.Count; sIndex++)
				{
					var s = fleet.Ships[sIndex];
					if (s == null) continue;
					var master = KCDatabase.Instance?.MasterShips[s.Id];
					int maxFuel = master?.Fuel ?? (s.Fuel > 0 ? s.Fuel : 50);
					int maxAmmo = master?.Ammo ?? (s.Ammo > 0 ? s.Ammo : 50);

					int currentFuel = s.Fuel > 0 ? s.Fuel : maxFuel;
					int currentAmmo = s.Ammo > 0 ? s.Ammo : maxAmmo;

					foreach (var node in details.Nodes)
					{
						// 1. 渦潮 (Happening)
						if (node.HappeningMstId > 0 && node.HappeningCount > 0)
						{
							if (node.HappeningMstId == 1) // 燃料
							{
								int maxFleetFuel = fleet.Ships.Max(x => x?.Fuel > 0 ? x.Fuel : (KCDatabase.Instance?.MasterShips[x?.Id ?? 0]?.Fuel ?? maxFuel));
								if (maxFleetFuel > 0)
								{
									int fuelLoss = (int)Math.Max(1, Math.Floor(currentFuel * ((double)node.HappeningCount / maxFleetFuel)));
									currentFuel = Math.Max(0, currentFuel - fuelLoss);
								}
							}
							else if (node.HappeningMstId == 2) // 弾薬
							{
								int maxFleetAmmo = fleet.Ships.Max(x => x?.Ammo > 0 ? x.Ammo : (KCDatabase.Instance?.MasterShips[x?.Id ?? 0]?.Ammo ?? maxAmmo));
								if (maxFleetAmmo > 0)
								{
									int ammoLoss = (int)Math.Max(1, Math.Floor(currentAmmo * ((double)node.HappeningCount / maxFleetAmmo)));
									currentAmmo = Math.Max(0, currentAmmo - ammoLoss);
								}
							}
						}

						// 2. 戦闘マス
						if (node.IsBattleNode)
						{
							double fuelRate;
							double ammoRate;

							if (node.IsAirRaid)
							{
								fuelRate = (record.World == 6) ? 0.04 : 0.06;
								ammoRate = (record.World == 6) ? 0.08 : 0.04;
							}
							else if (node.IsSubmarineOnly)
							{
								fuelRate = 0.08;
								ammoRate = 0;
							}
							else if (node.IsNightBattleStart)
							{
								fuelRate = 0.10;
								ammoRate = 0.10;
							}
							else
							{
								fuelRate = 0.20;
								ammoRate = 0.20;
							}

							int fuelUse = (int)Math.Max(1, Math.Floor(maxFuel * fuelRate));
							currentFuel = Math.Max(0, currentFuel - fuelUse);

							// 特殊攻撃参加判定
							bool participates = f == 0 && node.SpecialAttackShipIndexes.Contains(sIndex);
							double specialBonus = 0;
							if (participates)
							{
								switch (node.SpecialAttackType)
								{
									case 101: // 長門一斉射
									case 102: // 陸奥一斉射
									case 103: // Colorado
										specialBonus = ammoRate / 2.0; // +10%
										break;
									case 401: // 大和2隻
										specialBonus = 0.12;
										break;
									case 400: // 大和3隻
										specialBonus = 0.16;
										break;
								}
							}

							int ammoUse;
							if (participates && node.SpecialAttackType == 104) // 金剛型僚艦夜戦突撃
							{
								ammoUse = (int)Math.Max(1, Math.Floor(maxAmmo * (ammoRate * 1.2)));
							}
							else if (participates && (node.SpecialAttackType == 101 || node.SpecialAttackType == 102 || node.SpecialAttackType == 103) && node.HasNightBattle)
							{
								ammoUse = (int)Math.Max(1, Math.Ceiling(maxAmmo * (ammoRate + specialBonus)));
							}
							else
							{
								ammoUse = (int)Math.Max(1, Math.Floor(maxAmmo * ammoRate) + (specialBonus > 0 ? Math.Floor(maxAmmo * specialBonus) : 0));
							}

							// 昼戦からの夜戦突入 (+10%弾薬消費)
							if (node.HasNightBattle && !node.IsNightBattleStart)
							{
								if (participates && node.SpecialAttackType == 104)
								{
									ammoUse += (int)Math.Max(1, Math.Floor(maxAmmo * (ammoRate * 1.2 / 2.0)));
								}
								else
								{
									ammoUse += (int)Math.Max(1, Math.Ceiling(maxAmmo * ((ammoRate + specialBonus) / 2.0)));
								}
							}

							currentAmmo = Math.Max(0, currentAmmo - ammoUse);
						}
					}

					int resupplyFuel = Math.Max(0, s.Fuel - currentFuel);
					int resupplyAmmo = Math.Max(0, s.Ammo - currentAmmo);

					totalFuel += s.Level > 99 && resupplyFuel > 0 ? Math.Max(1, (int)(resupplyFuel * 0.85)) : resupplyFuel;
					totalAmmo += s.Level > 99 && resupplyAmmo > 0 ? Math.Max(1, (int)(resupplyAmmo * 0.85)) : resupplyAmmo;
				}
			}

			// 喪失機数推計
			totalBauxite = details.Nodes.SelectMany(n => n.Phases).Sum(p => p.FriendlyAircraftLost) * 5;

			return new SortieCostModel { Fuel = totalFuel, Ammo = totalAmmo, Bauxite = totalBauxite };
		}

		private static (SortieCostModel Cost, Dictionary<string, int> DamageCounts, int BucketsNeeded) CalculateRepairCost(
			SortieRecord record, SortieDetailData details)
		{
			var damageCounts = new Dictionary<string, int>
			{
				["Healthy"] = 0,
				["Light"] = 0,
				["Medium"] = 0,
				["Heavy"] = 0,
			};

			int fuel = 0;
			int steel = 0;
			int buckets = 0;

			bool isCombined = (int)(record.FleetData?.CombinedFlag ?? 0) > 0;
			var lastNode = details.Nodes.LastOrDefault();

			// 第1艦隊
			var mainFleet = record.FleetData?.Fleets?.Skip(0).FirstOrDefault();
			ProcessFleetRepair(mainFleet?.Ships, lastNode?.FriendlyShipsAfter);

			// 第2艦隊（連合艦隊時）
			if (isCombined)
			{
				var escortFleet = record.FleetData?.Fleets?.Skip(1).FirstOrDefault();
				ProcessFleetRepair(escortFleet?.Ships, lastNode?.EscortShipsAfter);
			}

			void ProcessFleetRepair(List<SortieShip>? initialShips, List<BattleShipSnapshot>? finalShips)
			{
				if (initialShips == null) return;

				for (int i = 0; i < initialShips.Count; i++)
				{
					var initialShip = initialShips[i];
					if (initialShip == null) continue;

					var master = KCDatabase.Instance?.MasterShips[initialShip.Id];
					int maxHp = initialShip.MaxHp > 0 ? initialShip.MaxHp : (master?.HPMax ?? 100);

					int afterHp = maxHp;
					if (finalShips != null && i < finalShips.Count)
					{
						afterHp = finalShips[i].HpAfter;
					}

					int damage = Math.Max(0, maxHp - afterHp);
					if (damage > 0)
					{
						int baseFuel = master?.Fuel ?? 50;
						fuel += (int)(baseFuel * 0.032 * damage);
						steel += (int)(baseFuel * 0.06 * damage);

						double hpRate = (double)afterHp / maxHp;
						if (hpRate <= 0.25)
						{
							damageCounts["Heavy"]++;
							buckets++;
						}
						else if (hpRate <= 0.50)
						{
							damageCounts["Medium"]++;
							buckets++;
						}
						else if (hpRate <= 0.75)
						{
							damageCounts["Light"]++;
							if (damage >= 20) buckets++;
						}
						else
						{
							damageCounts["Healthy"]++;
						}
					}
					else
					{
						damageCounts["Healthy"]++;
					}
				}
			}

			return (new SortieCostModel { Fuel = fuel, Steel = steel }, damageCounts, buckets);
		}

		private static SortieCostModel CalculateAirBaseSortieCost(SortieRecord record)
		{
			if (record.FleetData?.AirBases == null) return SortieCostModel.Zero;

			int fuel = 0;
			int ammo = 0;

			foreach (var ab in record.FleetData.AirBases.Where(b => b.MapAreaId == record.World && (int)b.ActionKind == 1))
			{
				if (ab.Squadrons == null) continue;
				foreach (var sq in ab.Squadrons)
				{
					if (sq?.EquipmentSlot?.Equipment == null) continue;
					int count = sq.EquipmentSlot.AircraftCurrent;
					if (count <= 0) continue;

					var master = KCDatabase.Instance?.MasterEquipments[sq.EquipmentSlot.Equipment.Id];
					int cat = (int)(master?.CategoryType ?? 0);

					// 陸上攻撃機 (47) または未ロード時フォールバック
					if (cat == 47 || sq.EquipmentSlot.Equipment.Id == 168 || sq.EquipmentSlot.Equipment.Id == 169)
					{
						fuel += (int)Math.Ceiling(1.5 * count);
						ammo += (int)(0.7 * count);
					}
					// 大型飛行艇 (41) / 大型陸上機
					else if (cat == 41)
					{
						fuel += 2 * count;
						ammo += 2 * count;
					}
					else
					{
						fuel += count;
						ammo += (int)Math.Ceiling(0.6 * count);
					}
				}
			}

			return new SortieCostModel { Fuel = fuel, Ammo = ammo };
		}

		private static SortieCostModel CalculateAirBaseSupplyCost(SortieRecord record, SortieDetailData details)
		{
			int lost = 0;

			// 帰投後データがある場合は実績の機数損耗差分から正確に算出
			if (record.FleetData?.AirBases != null && record.FleetAfterSortieData?.AirBases != null)
			{
				foreach (var beforeAb in record.FleetData.AirBases)
				{
					var afterAb = record.FleetAfterSortieData.AirBases.FirstOrDefault(a => a.AirCorpsId == beforeAb.AirCorpsId && a.MapAreaId == beforeAb.MapAreaId);
					if (afterAb?.Squadrons == null || beforeAb.Squadrons == null) continue;

					for (int i = 0; i < beforeAb.Squadrons.Count && i < afterAb.Squadrons.Count; i++)
					{
						int bCount = beforeAb.Squadrons[i]?.AircraftCurrent ?? beforeAb.Squadrons[i]?.EquipmentSlot?.AircraftCurrent ?? 0;
						int aCount = afterAb.Squadrons[i]?.AircraftCurrent ?? afterAb.Squadrons[i]?.EquipmentSlot?.AircraftCurrent ?? 0;
						lost += Math.Max(0, bCount - aCount);
					}
				}
			}
			else
			{
				// 帰投後データがない場合は戦闘詳細の損失機数から推計
				lost = details.Nodes
					.SelectMany(n => n.Phases)
					.Where(p => p.PhaseName.Contains("基地"))
					.Sum(p => p.FriendlyAircraftLost);
			}

			return new SortieCostModel
			{
				Fuel = lost * 3,
				Bauxite = lost * 5,
			};
		}

		/// <summary>
		/// 噴式機の機体数と装備種別・配置コストから鋼材消費量を正確に算出します。
		/// </summary>
		public static int CalculateJetSteelCost(int aircraft, int aircraftCost, int cardType, int categoryType, int equipmentId = 0)
		{
			if (aircraft <= 0) return 0;

			// MasterEquipmentが未ロードの場合の既知装備フォールバック (Keiun: 199/200, Kikka: 197, Ho229: 399, Shinden: 501)
			if (aircraftCost <= 0)
			{
				switch (equipmentId)
				{
					case 197: // 橘花改
						aircraftCost = 13;
						if (cardType <= 0) cardType = 39;
						break;
					case 198:
					case 199:
					case 200: // 噴式景雲改
						aircraftCost = 14;
						if (cardType <= 0) cardType = 39;
						break;
					case 399: // Ho229
						aircraftCost = 14;
						cardType = 51;
						break;
					case 500:
					case 501: // 試製 震電改
						aircraftCost = 10;
						cardType = 52;
						break;
				}
			}

			double mult = cardType switch
			{
				51 => aircraftCost * 1.2, // AllFlyingWingJetBomber (Ho229)
				52 => aircraftCost * 1.1, // PrototypeJetFighter (Shinden)
				_ when (categoryType >= 56 && categoryType <= 59) || cardType == 38 || cardType == 39 || aircraftCost > 0
					=> aircraftCost > 0 ? (double)aircraftCost : 13.0,
				_ => 0,
			};

			if (mult <= 0) return 0;
			return (int)Math.Round(aircraft * mult * 0.2);
		}

		private static SortieCostModel CalculateJetCost(SortieRecord record)
		{
			if (record.FleetData?.Fleets == null) return SortieCostModel.Zero;

			int steel = 0;
			var mainFleet = record.FleetData.Fleets.Skip(0).FirstOrDefault();
			if (mainFleet?.Ships != null)
			{
				foreach (var s in mainFleet.Ships)
				{
					if (s?.EquipmentSlots == null) continue;
					foreach (var slot in s.EquipmentSlots)
					{
						if (slot?.Equipment == null) continue;
						var master = KCDatabase.Instance?.MasterEquipments[slot.Equipment.Id];
						int cat = (int)(master?.CategoryType ?? 0);
						int card = master?.CardType ?? 0;
						int cost = master?.AircraftCost ?? 0;
						steel += CalculateJetSteelCost(slot.AircraftCurrent, cost, card, cat, slot.Equipment.Id);
					}
				}
			}

			return new SortieCostModel { Steel = steel };
		}

		private static SortieCostModel CalculateSupportCost(SortieRecord record, bool isBoss, SortieDetailData details)
		{
			int fleetId = isBoss
				? (record.FleetData?.BossSupportFleetId ?? 0)
				: (record.FleetData?.NodeSupportFleetId ?? 0);

			if (fleetId <= 0) return SortieCostModel.Zero;

			bool supportTriggered = isBoss
				? details.Nodes.Any(n => n.IsBoss && n.Phases.Any(p => p.PhaseName.Contains("支援")))
				: details.Nodes.Any(n => !n.IsBoss && n.Phases.Any(p => p.PhaseName.Contains("支援")));

			if (!supportTriggered && !isBoss) return SortieCostModel.Zero;

			// 支援艦隊の消費概算（砲雷撃支援: 燃料50%, 弾薬80%）
			var supportFleet = record.FleetData?.Fleets?.Skip(fleetId - 1).FirstOrDefault();
			int fuel = 0;
			int ammo = 0;

			if (supportFleet?.Ships != null)
			{
				foreach (var s in supportFleet.Ships)
				{
					if (s == null) continue;
					var master = KCDatabase.Instance?.MasterShips[s.Id];
					int maxFuel = master?.Fuel ?? 60;
					int maxAmmo = master?.Ammo ?? 75;

					fuel += (int)(maxFuel * 0.50);
					ammo += (int)(maxAmmo * 0.80);
				}
			}
			else
			{
				fuel = 120;
				ammo = 180;
			}

			return new SortieCostModel { Fuel = fuel, Ammo = ammo };
		}
	}
}
