#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ElectronicObserver.Data;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie
{
	public static class SortieNodeEmulator
	{
		/// <summary>
		/// 出撃レコードと API パケット群から各ノードの移動、戦闘フェーズ、ダメージ推移をエミュレートして構築します。
		/// </summary>
		public static SortieDetailData EmulateSortie(SortieRecord record, IEnumerable<ApiFile> apiFiles)
		{
			if (record == null) throw new ArgumentNullException(nameof(record));
			if (apiFiles == null) throw new ArgumentNullException(nameof(apiFiles));

			var fileList = apiFiles as IList<ApiFile> ?? apiFiles.ToList();
			DateTime startTime = fileList.Count > 0 ? fileList.Min(f => f.TimeStamp) : DateTime.UtcNow;

			var detail = new SortieDetailData
			{
				SortieId = record.Id,
				World = record.World,
				Map = record.Map,
				StartTime = startTime,
				Nodes = new List<SortieNodeInfo>(),
			};

			var responses = apiFiles
				.Where(f => f.ApiFileType == ApiFileType.Response)
				.OrderBy(f => f.TimeStamp)
				.ToList();

			SortieNodeInfo? currentNode = null;

			// 味方艦の追跡リスト
			var friendlyShips = InitFriendlyShips(record);
			var escortShips = InitEscortShips(record);

			foreach (var file in responses)
			{
				string name = file.Name ?? "";
				string content = file.ContentString;
				if (string.IsNullOrEmpty(content)) continue;
				if (content.StartsWith("svdata="))
				{
					content = content.Substring(7);
				}

				if (name.Contains("api_req_map/start") || name.Contains("api_req_map/next"))
				{
					currentNode = ProcessMapNext(record, content);
					if (currentNode != null)
					{
						detail.Nodes.Add(currentNode);
					}
					continue;
				}

				if (currentNode == null)
				{
					// Why not skip: In cases where map/start packet is missing, instantiate a dummy root node.
					currentNode = new SortieNodeInfo
					{
						CellId = 0,
						CellDisplay = "開始",
					};
					detail.Nodes.Add(currentNode);
				}

				if (name.Contains("battleresult"))
				{
					ProcessBattleResult(currentNode, content, friendlyShips, escortShips);
					continue;
				}

				if (IsBattleApi(name))
				{
					ProcessBattleApi(currentNode, name, content, friendlyShips, escortShips);
					continue;
				}
			}

			return detail;
		}

		private static List<BattleShipSnapshot> InitFriendlyShips(SortieRecord record)
		{
			var list = new List<BattleShipSnapshot>();
			var mainFleet = record.FleetData?.Fleets?.Skip(0).FirstOrDefault();
			if (mainFleet?.Ships != null)
			{
				foreach (var s in mainFleet.Ships)
				{
					int maxHp = s.MaxHp > 0 ? s.MaxHp : (KCDatabase.Instance?.MasterShips[s.Id]?.HPMax ?? 100);
					list.Add(new BattleShipSnapshot
					{
						ShipMasterId = s.Id,
						Name = KCDatabase.Instance?.MasterShips[s.Id]?.NameWithClass ?? $"艦船ID:{s.Id}",
						Level = s.Level,
						HpBefore = maxHp,
						HpAfter = maxHp,
						HpMax = maxHp,
						DamageStateText = "無傷",
					});
				}
			}
			return list;
		}

		private static List<BattleShipSnapshot> InitEscortShips(SortieRecord record)
		{
			var list = new List<BattleShipSnapshot>();
			if ((int)(record.FleetData?.CombinedFlag ?? 0) > 0)
			{
				var escortFleet = record.FleetData?.Fleets?.Skip(1).FirstOrDefault();
				if (escortFleet?.Ships != null)
				{
					foreach (var s in escortFleet.Ships)
					{
						int maxHp = s.MaxHp > 0 ? s.MaxHp : (KCDatabase.Instance?.MasterShips[s.Id]?.HPMax ?? 100);
						list.Add(new BattleShipSnapshot
						{
							ShipMasterId = s.Id,
							Name = KCDatabase.Instance?.MasterShips[s.Id]?.NameWithClass ?? $"艦船ID:{s.Id}",
							Level = s.Level,
							HpBefore = maxHp,
							HpAfter = maxHp,
							HpMax = maxHp,
							DamageStateText = "無傷",
						});
					}
				}
			}
			return list;
		}

		private static SortieNodeInfo? ProcessMapNext(SortieRecord record, string json)
		{
			try
			{
				using var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;
				var data = root.TryGetProperty("api_data", out var d) ? d : root;

				int cellId = data.TryGetProperty("api_no", out var noProp) ? noProp.GetInt32() : 0;
				int eventId = data.TryGetProperty("api_event_id", out var evProp) ? evProp.GetInt32() : 0;
				int eventKind = data.TryGetProperty("api_event_kind", out var ekProp) ? ekProp.GetInt32() : 0;

				string cellDisplay = KCDatabase.Instance?.Destination?.CellDisplayWithId(record.World, record.Map, cellId)
					?? cellId.ToString();

				var node = new SortieNodeInfo
				{
					CellId = cellId,
					CellDisplay = cellDisplay,
					EventId = eventId,
					EventKind = eventKind,
					NodeTypeDescription = GetNodeDescription(eventId, eventKind),
				};

				// 非戦闘イベント（資源獲得・うずしお）情報の抽出
				if (data.TryGetProperty("api_itemget", out var itemGet) && itemGet.ValueKind == JsonValueKind.Array)
				{
					var items = new List<string>();
					foreach (var item in itemGet.EnumerateArray())
					{
						int id = item.TryGetProperty("api_usemst", out var u) ? u.GetInt32() : (item.TryGetProperty("api_icon_id", out var ic) ? ic.GetInt32() : 0);
						int count = item.TryGetProperty("api_getcount", out var c) ? c.GetInt32() : 0;
						items.Add($"{GetItemName(id)} +{count}");
					}
					node.NonBattleInfo = string.Join(", ", items);
				}
				else if (data.TryGetProperty("api_happening", out var hap))
				{
					int id = hap.TryGetProperty("api_mst_id", out var m) ? m.GetInt32() : 0;
					int count = hap.TryGetProperty("api_count", out var c) ? c.GetInt32() : 0;
					node.NonBattleInfo = $"うずしお ({GetItemName(id)} -{count})";
					node.HappeningMstId = id;
					node.HappeningCount = count;
				}

				return node;
			}
			catch
			{
				return null;
			}
		}

		private static void ProcessBattleApi(SortieNodeInfo node, string apiName, string json,
			List<BattleShipSnapshot> friendlyShips, List<BattleShipSnapshot> escortShips)
		{
			try
			{
				using var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;
				var data = root.TryGetProperty("api_data", out var d) ? d : root;

				bool isNewBattleNode = node.Phases.Count == 0;
				bool isSecondBattle = IsSecondBattleApi(apiName);

				if (apiName.Contains("sp_midnight")) node.IsNightBattleStart = true;
				if (apiName.Contains("ld_airbattle") || node.EventKind == 4) node.IsAirRaid = true;

				if (isNewBattleNode)
				{
					// 前マス終了時点の HpAfter を HpBefore に引き継ぐ
					friendlyShips.ForEach(s => s.HpBefore = s.HpAfter);
					escortShips.ForEach(s => s.HpBefore = s.HpAfter);

					// 戦闘 API に実測 api_f_nowhps / api_f_maxhps が存在する場合は同期
					if (data.TryGetProperty("api_f_nowhps", out var fnh) && fnh.ValueKind == JsonValueKind.Array)
					{
						var fNowHps = fnh.EnumerateArray().Select(x => x.GetInt32()).ToArray();
						var fMaxHps = data.TryGetProperty("api_f_maxhps", out var fmh) && fmh.ValueKind == JsonValueKind.Array
							? fmh.EnumerateArray().Select(x => x.GetInt32()).ToArray() : Array.Empty<int>();

						int offset = (fNowHps.Length > 0 && fNowHps[0] == -1) ? 1 : 0;
						for (int i = 0; i < friendlyShips.Count && i + offset < fNowHps.Length; i++)
						{
							int now = fNowHps[i + offset];
							int max = i + offset < fMaxHps.Length ? fMaxHps[i + offset] : friendlyShips[i].HpMax;
							friendlyShips[i].HpBefore = now;
							friendlyShips[i].HpAfter = now;
							friendlyShips[i].HpMax = max;
						}
					}

					if (data.TryGetProperty("api_f_nowhps_combined", out var fnhc) && fnhc.ValueKind == JsonValueKind.Array)
					{
						var fNowHpsC = fnhc.EnumerateArray().Select(x => x.GetInt32()).ToArray();
						var fMaxHpsC = data.TryGetProperty("api_f_maxhps_combined", out var fmhc) && fmhc.ValueKind == JsonValueKind.Array
							? fmhc.EnumerateArray().Select(x => x.GetInt32()).ToArray() : Array.Empty<int>();

						int offsetC = (fNowHpsC.Length > 0 && fNowHpsC[0] == -1) ? 1 : 0;
						for (int i = 0; i < escortShips.Count && i + offsetC < fNowHpsC.Length; i++)
						{
							int now = fNowHpsC[i + offsetC];
							int max = i + offsetC < fMaxHpsC.Length ? fMaxHpsC[i + offsetC] : escortShips[i].HpMax;
							escortShips[i].HpBefore = now;
							escortShips[i].HpAfter = now;
							escortShips[i].HpMax = max;
						}
					}
				}
				else if (isSecondBattle)
				{
					node.HasNightBattle = true;

					// 昼戦から夜戦への引き継ぎ: パケットに実測値があれば反映
					if (data.TryGetProperty("api_f_nowhps", out var fnh) && fnh.ValueKind == JsonValueKind.Array)
					{
						var fNowHps = fnh.EnumerateArray().Select(x => x.GetInt32()).ToArray();
						int offset = (fNowHps.Length > 0 && fNowHps[0] == -1) ? 1 : 0;
						for (int i = 0; i < friendlyShips.Count && i + offset < fNowHps.Length; i++)
						{
							friendlyShips[i].HpBefore = fNowHps[i + offset];
							friendlyShips[i].HpAfter = fNowHps[i + offset];
						}
					}

					if (data.TryGetProperty("api_f_nowhps_combined", out var fnhc) && fnhc.ValueKind == JsonValueKind.Array)
					{
						var fNowHpsC = fnhc.EnumerateArray().Select(x => x.GetInt32()).ToArray();
						int offsetC = (fNowHpsC.Length > 0 && fNowHpsC[0] == -1) ? 1 : 0;
						for (int i = 0; i < escortShips.Count && i + offsetC < fNowHpsC.Length; i++)
						{
							escortShips[i].HpBefore = fNowHpsC[i + offsetC];
							escortShips[i].HpAfter = fNowHpsC[i + offsetC];
						}
					}

					// 昼戦終了時・夜戦開始時の HP を一致させてスナップショット保持
					node.FriendlyShipsDayAfter ??= friendlyShips.Select(s => s.Clone()).ToList();
					node.EscortShipsDayAfter ??= escortShips.Select(s => s.Clone()).ToList();
					node.FriendlyShipsNightBefore = friendlyShips.Select(s => s.Clone()).ToList();
					node.EscortShipsNightBefore = escortShips.Select(s => s.Clone()).ToList();
				}

				// 陣形・交戦形態・制空
				if (data.TryGetProperty("api_formation", out var form) && form.ValueKind == JsonValueKind.Array)
				{
					var fArr = form.EnumerateArray().Select(x => x.GetInt32()).ToArray();
					if (fArr.Length > 0) node.PlayerFormation = fArr[0];
					if (fArr.Length > 1) node.EnemyFormation = fArr[1];
					if (fArr.Length > 2) node.EngagementForm = fArr[2];
				}

				if (data.TryGetProperty("api_smoke_type", out var smoke))
				{
					node.SmokeType = smoke.GetInt32();
				}

				// 特殊攻撃（カットイン演出情報 api_flavor_info）の抽出
				if (data.TryGetProperty("api_flavor_info", out var flav) && flav.ValueKind == JsonValueKind.Object)
				{
					int flavType = flav.TryGetProperty("api_type", out var ft) ? ft.GetInt32() : 0;
					if (flavType >= 100)
					{
						node.SpecialAttackType = flavType;
						if (flav.TryGetProperty("api_ship_idx", out var sIdxArr) && sIdxArr.ValueKind == JsonValueKind.Array)
						{
							node.SpecialAttackShipIndexes = sIdxArr.EnumerateArray().Select(x => x.GetInt32() - 1).Where(x => x >= 0).ToList();
						}
						else
						{
							node.SpecialAttackShipIndexes = (flavType == 400) ? new List<int> { 0, 1, 2 } : new List<int> { 0, 1 };
						}
					}
				}

				// 敵艦リストの生成または引き継ぎ
				List<BattleShipSnapshot> enemyShips;
				if (!isNewBattleNode && node.EnemyShipsAfter != null && node.EnemyShipsAfter.Count > 0)
				{
					// 昼戦から夜戦への突入: 昼戦終了時の敵状態を引き継ぐ (HpBefore は昼戦開始時の状態を維持)
					enemyShips = node.EnemyShipsAfter;
				}
				else
				{
					enemyShips = new List<BattleShipSnapshot>();
					if (data.TryGetProperty("api_ship_ke", out var shipKe) && shipKe.ValueKind == JsonValueKind.Array)
					{
						var eNowHps = data.TryGetProperty("api_e_nowhps", out var enh) && enh.ValueKind == JsonValueKind.Array
							? enh.EnumerateArray().Select(x => x.GetInt32()).ToArray() : Array.Empty<int>();
						var eMaxHps = data.TryGetProperty("api_e_maxhps", out var emh) && emh.ValueKind == JsonValueKind.Array
							? emh.EnumerateArray().Select(x => x.GetInt32()).ToArray() : Array.Empty<int>();

						int eOffset = (eNowHps.Length > 0 && eNowHps[0] == -1) ? 1 : 0;
						int eIndex = 0;
						foreach (var sId in shipKe.EnumerateArray())
						{
							int id = sId.GetInt32();
							if (id <= 0) continue;

							int now = (eIndex + eOffset) < eNowHps.Length ? eNowHps[eIndex + eOffset] : 100;
							int max = (eIndex + eOffset) < eMaxHps.Length ? eMaxHps[eIndex + eOffset] : 100;

							enemyShips.Add(new BattleShipSnapshot
							{
								ShipMasterId = id,
								Name = KCDatabase.Instance?.MasterShips[id]?.NameWithClass ?? $"敵ID:{id}",
								HpBefore = now,
								HpAfter = now,
								HpMax = max,
								DamageStateText = BattleShipSnapshot.CalculateDamageState(now, max),
							});
							eIndex++;
						}
					}
				}

				// 1. 基地航空隊攻撃
				if (data.TryGetProperty("api_air_base_attack", out var airBase) && airBase.ValueKind == JsonValueKind.Array)
				{
					var phase = new BattlePhaseInfo { PhaseName = "基地航空隊" };
					foreach (var attack in airBase.EnumerateArray())
					{
						if (attack.TryGetProperty("api_stage3", out var st3))
						{
							ApplyDamages(phase, "基地航空隊", enemyShips, st3, "api_edam");
						}
					}
					node.Phases.Add(phase);
				}

				// 2. 噴式強襲
				if (data.TryGetProperty("api_injection_kouku", out var inj))
				{
					var phase = new BattlePhaseInfo { PhaseName = "噴式強襲" };
					if (inj.TryGetProperty("api_stage3", out var st3))
					{
						ApplyDamages(phase, "噴式強襲", enemyShips, st3, "api_edam");
					}
					node.Phases.Add(phase);
				}

				// 3. 航空戦
				if (data.TryGetProperty("api_kouku", out var kouku) && kouku.ValueKind == JsonValueKind.Object)
				{
					var phase = new BattlePhaseInfo { PhaseName = "航空戦" };
					if (kouku.TryGetProperty("api_stage1", out var st1) && st1.ValueKind == JsonValueKind.Object)
					{
						if (st1.TryGetProperty("api_disp_seiku", out var seiku))
							node.AirControlState = seiku.GetInt32();
						if (st1.TryGetProperty("api_f_lostcount", out var flost))
							phase.FriendlyAircraftLost += flost.GetInt32();
					}
					if (kouku.TryGetProperty("api_stage2", out var st2) && st2.ValueKind == JsonValueKind.Object)
					{
						if (st2.TryGetProperty("api_f_lostcount", out var flost))
							phase.FriendlyAircraftLost += flost.GetInt32();
					}
					if (kouku.TryGetProperty("api_stage3", out var st3) && st3.ValueKind == JsonValueKind.Object)
					{
						ApplyDamages(phase, "敵航空隊", friendlyShips, st3, "api_fdam");
						ApplyDamages(phase, "味方航空隊", enemyShips, st3, "api_edam");
					}
					if (kouku.TryGetProperty("api_stage3_combined", out var st3c) && st3c.ValueKind == JsonValueKind.Object)
					{
						ApplyDamages(phase, "敵航空隊", escortShips, st3c, "api_fdam");
						ApplyDamages(phase, "味方航空隊", enemyShips, st3c, "api_edam");
					}
					node.Phases.Add(phase);
				}

				// 4. 支援艦隊
				if (data.TryGetProperty("api_support_info", out var support) && support.ValueKind == JsonValueKind.Object)
				{
					var phase = new BattlePhaseInfo { PhaseName = "支援艦隊" };
					if (support.TryGetProperty("api_support_airatack", out var sAir) && sAir.ValueKind == JsonValueKind.Object && sAir.TryGetProperty("api_stage3", out var sSt3) && sSt3.ValueKind == JsonValueKind.Object)
					{
						ApplyDamages(phase, "航空支援", enemyShips, sSt3, "api_edam");
					}
					else if (support.TryGetProperty("api_support_hourai", out var sHou) && sHou.ValueKind == JsonValueKind.Object && sHou.TryGetProperty("api_damage", out var sDam))
					{
						ApplyDamages(phase, "砲雷撃支援", enemyShips, sDam);
					}
					node.Phases.Add(phase);
				}

				// 5. 先制対潜
				if (data.TryGetProperty("api_opening_taisen", out var taisen) && taisen.ValueKind == JsonValueKind.Object)
				{
					var phase = new BattlePhaseInfo { PhaseName = "先制対潜" };
					ParseHougeki(phase, taisen, friendlyShips, enemyShips, isNight: false, escort: escortShips);
					node.Phases.Add(phase);
				}

				// 6. 開幕雷撃
				if (data.TryGetProperty("api_opening_atack", out var opRai) && opRai.ValueKind == JsonValueKind.Object)
				{
					var phase = new BattlePhaseInfo { PhaseName = "開幕雷撃" };
					ParseRaigeki(phase, opRai, friendlyShips, enemyShips);
					node.Phases.Add(phase);
				}

				// 7. 昼戦砲撃戦 (1〜3巡目)
				for (int i = 1; i <= 3; i++)
				{
					if (data.TryGetProperty($"api_hougeki{i}", out var hou) && hou.ValueKind == JsonValueKind.Object)
					{
						var phase = new BattlePhaseInfo { PhaseName = $"砲撃戦({i}巡目)" };
						ParseHougeki(phase, hou, friendlyShips, enemyShips, isNight: false, node: node, escort: escortShips);
						node.Phases.Add(phase);
					}
				}

				// 8. 閉幕雷撃
				if (data.TryGetProperty("api_raigeki", out var clsRai) && clsRai.ValueKind == JsonValueKind.Object)
				{
					var phase = new BattlePhaseInfo { PhaseName = "閉幕雷撃" };
					ParseRaigeki(phase, clsRai, friendlyShips, enemyShips);
					node.Phases.Add(phase);
				}

				// 9. 夜戦
				if (data.TryGetProperty("api_hougeki", out var yasenHou) && yasenHou.ValueKind == JsonValueKind.Object)
				{
					var phase = new BattlePhaseInfo { PhaseName = "夜戦" };
					bool isCombinedMidnight = (apiName.Contains("combined_battle") || escortShips.Count > 0);
					var attackingFleet = isCombinedMidnight && escortShips.Count > 0 ? escortShips : friendlyShips;
					ParseHougeki(phase, yasenHou, attackingFleet, enemyShips, isNight: true, node: node, escort: escortShips);
					node.Phases.Add(phase);
				}

				// 戦闘後スナップショットの保存
				node.FriendlyShipsAfter = friendlyShips.Select(s => new BattleShipSnapshot
				{
					ShipMasterId = s.ShipMasterId,
					Name = s.Name,
					Level = s.Level,
					HpBefore = s.HpBefore,
					HpAfter = s.HpAfter,
					HpMax = s.HpMax,
					DamageStateText = BattleShipSnapshot.CalculateDamageState(s.HpAfter, s.HpMax),
				}).ToList();

				node.EscortShipsAfter = escortShips.Select(s => new BattleShipSnapshot
				{
					ShipMasterId = s.ShipMasterId,
					Name = s.Name,
					Level = s.Level,
					HpBefore = s.HpBefore,
					HpAfter = s.HpAfter,
					HpMax = s.HpMax,
					DamageStateText = BattleShipSnapshot.CalculateDamageState(s.HpAfter, s.HpMax),
				}).ToList();

				node.EnemyShipsAfter = enemyShips.Select(s => new BattleShipSnapshot
				{
					ShipMasterId = s.ShipMasterId,
					Name = s.Name,
					Level = s.Level,
					HpBefore = s.HpBefore,
					HpAfter = s.HpAfter,
					HpMax = s.HpMax,
					DamageStateText = BattleShipSnapshot.CalculateDamageState(s.HpAfter, s.HpMax),
				}).ToList();
			}
			catch (Exception ex)
			{
				Utility.Logger.Add(2, $"ProcessBattleApi パースエラー: {ex.Message}");
			}
		}

		private static void ParseHougeki(BattlePhaseInfo phase, JsonElement hougeki,
			List<BattleShipSnapshot> friendly, List<BattleShipSnapshot> enemy, bool isNight = false, SortieNodeInfo? node = null, List<BattleShipSnapshot>? escort = null)
		{
			if (!hougeki.TryGetProperty("api_at_list", out var atList) ||
				!hougeki.TryGetProperty("api_df_list", out var dfList) ||
				!hougeki.TryGetProperty("api_damage", out var damageList)) return;

			var atArr = atList.EnumerateArray().Select(x => x.GetInt32()).ToArray();
			var dfArr = dfList.EnumerateArray().ToArray();
			var damArr = damageList.EnumerateArray().ToArray();

			JsonElement[]? clArr = hougeki.TryGetProperty("api_cl_list", out var clProp) && clProp.ValueKind == JsonValueKind.Array
				? clProp.EnumerateArray().ToArray()
				: null;

			int[]? atTypeArr = !isNight && hougeki.TryGetProperty("api_at_type", out var atTypeProp) && atTypeProp.ValueKind == JsonValueKind.Array
				? atTypeProp.EnumerateArray().Select(x => x.GetInt32()).ToArray()
				: null;

			int[]? spListArr = isNight && hougeki.TryGetProperty("api_sp_list", out var spProp) && spProp.ValueKind == JsonValueKind.Array
				? spProp.EnumerateArray().Select(x => x.GetInt32()).ToArray()
				: null;

			int[]? eflagArr = hougeki.TryGetProperty("api_at_eflag", out var efProp)
				? efProp.EnumerateArray().Select(x => x.GetInt32()).ToArray()
				: null;

			int startIndex = (atArr.Length > 0 && atArr[0] == -1) ? 1 : 0;

			for (int i = startIndex; i < atArr.Length; i++)
			{
				int rawAt = atArr[i];
				if (rawAt < 0) continue;

				int atIdx;
				string atName;

				if (eflagArr != null && i < eflagArr.Length)
				{
					bool isEnemyAttack = eflagArr[i] == 1;
					if (!isEnemyAttack)
					{
						if (rawAt >= 6 && escort != null && (rawAt - 6) < escort.Count)
						{
							atName = escort[rawAt - 6].Name;
							atIdx = (rawAt - 6) + 1;
						}
						else if (rawAt < friendly.Count)
						{
							atName = friendly[rawAt].Name;
							atIdx = rawAt + 1;
						}
						else
						{
							atName = $"味方艦:{rawAt + 1}";
							atIdx = (rawAt % 6) + 1;
						}
					}
					else
					{
						int eIdx = rawAt < enemy.Count ? rawAt : (rawAt % 6);
						atName = (eIdx < enemy.Count) ? enemy[eIdx].Name : $"敵艦:{rawAt + 1}";
						atIdx = (rawAt % 6) + 7;
					}
				}
				else
				{
					atIdx = rawAt;
					if (atIdx <= 0) continue;
					atName = GetShipNameByIndex(atIdx, friendly, enemy);
				}

				if (i >= dfArr.Length || i >= damArr.Length) continue;

				string attackType = "";
				if (!isNight && atTypeArr != null && i < atTypeArr.Length)
				{
					int t = atTypeArr[i];
					attackType = GetDayAttackType(t);
					if (node != null && t >= 100)
					{
						node.SpecialAttackType = t;
						node.SpecialAttackShipIndexes = (t == 400) ? new List<int> { 0, 1, 2 } : new List<int> { 0, 1 };
					}
				}
				else if (isNight && spListArr != null && i < spListArr.Length)
				{
					int sp = spListArr[i];
					attackType = GetNightAttackType(sp);
					if (node != null && sp >= 100)
					{
						node.SpecialAttackType = sp;
						node.SpecialAttackShipIndexes = (sp == 400) ? new List<int> { 0, 1, 2 } : new List<int> { 0, 1 };
					}
				}

				var dfTargets = dfArr[i].ValueKind == JsonValueKind.Array
					? dfArr[i].EnumerateArray().Select(x => x.GetInt32()).ToArray()
					: new[] { dfArr[i].GetInt32() };

				var damages = damArr[i].ValueKind == JsonValueKind.Array
					? damArr[i].EnumerateArray().Select(x => (int)Math.Floor(x.GetDouble())).ToArray()
					: new[] { (int)Math.Floor(damArr[i].GetDouble()) };

				int[]? cls = null;
				if (clArr != null && i < clArr.Length)
				{
					if (clArr[i].ValueKind == JsonValueKind.Array)
					{
						cls = clArr[i].EnumerateArray().Select(x => x.GetInt32()).ToArray();
					}
					else if (clArr[i].ValueKind == JsonValueKind.Number)
					{
						cls = new[] { clArr[i].GetInt32() };
					}
				}

				for (int t = 0; t < dfTargets.Length; t++)
				{
					int rawDf = dfTargets[t];
					if (rawDf < 0) continue;

					int dfIdx;
					string dfName;
					int dam = t < damages.Length ? damages[t] : 0;

					if (eflagArr != null && i < eflagArr.Length)
					{
						bool isEnemyAttack = eflagArr[i] == 1;
						if (!isEnemyAttack) // 味方攻撃 -> 敵被弾
						{
							int eIdx = rawDf < enemy.Count ? rawDf : (rawDf % 6);
							if (eIdx < enemy.Count)
							{
								dfName = enemy[eIdx].Name;
								enemy[eIdx].HpAfter = Math.Max(0, enemy[eIdx].HpAfter - dam);
								enemy[eIdx].DamageStateText = BattleShipSnapshot.CalculateDamageState(enemy[eIdx].HpAfter, enemy[eIdx].HpMax);
							}
							else
							{
								dfName = $"敵艦:{rawDf + 1}";
							}
							dfIdx = (rawDf % 6) + 7;
						}
						else // 敵攻撃 -> 味方被弾
						{
							if (rawDf >= 6 && escort != null && (rawDf - 6) < escort.Count)
							{
								var targetShip = escort[rawDf - 6];
								dfName = targetShip.Name;
								targetShip.HpAfter = Math.Max(0, targetShip.HpAfter - dam);
								targetShip.DamageStateText = BattleShipSnapshot.CalculateDamageState(targetShip.HpAfter, targetShip.HpMax);
								dfIdx = (rawDf - 6) + 1;
							}
							else if (rawDf < friendly.Count)
							{
								var targetShip = friendly[rawDf];
								dfName = targetShip.Name;
								targetShip.HpAfter = Math.Max(0, targetShip.HpAfter - dam);
								targetShip.DamageStateText = BattleShipSnapshot.CalculateDamageState(targetShip.HpAfter, targetShip.HpMax);
								dfIdx = rawDf + 1;
							}
							else
							{
								dfName = $"味方艦:{rawDf + 1}";
								dfIdx = (rawDf % 6) + 1;
							}
						}
					}
					else
					{
						dfIdx = rawDf;
						if (dfIdx <= 0) continue;
						dfName = GetShipNameByIndex(dfIdx, friendly, enemy);
						ApplyDamageToTarget(dfIdx, dam, friendly, enemy);
					}

					bool isCrit;
					if (cls != null && t < cls.Length)
					{
						isCrit = (cls[t] == 2);
					}
					else if (cls != null && cls.Length == 1)
					{
						isCrit = (cls[0] == 2);
					}
					else
					{
						isCrit = dam > 40;
					}

					phase.Attacks.Add(new BattleAttackEntry
					{
						AttackerName = atName,
						DefenderName = dfName,
						AttackerIndex = atIdx,
						DefenderIndex = dfIdx,
						Damage = dam,
						IsCritical = isCrit,
						AttackType = attackType,
					});
				}
			}
		}

		private static string GetDayAttackType(int type) => type switch
		{
			0 => "通常",
			1 => "レーザー攻撃",
			2 => "連撃",
			3 => "主副CI",
			4 => "主電CI",
			5 => "主徹CI",
			6 => "主主CI",
			7 => "戦車CI",
			100 => "Nelson Touch",
			101 => "一斉射(長門)",
			102 => "一斉射(陸奥)",
			103 => "Colorado特殊攻撃",
			104 => "僚艦夜戦突撃",
			200 => "瑞雲立体攻撃",
			201 => "海色立体攻撃",
			400 => "特殊攻撃(大和3隻)",
			401 => "特殊攻撃(大和2隻)",
			_ => $"攻撃({type})"
		};

		private static string GetNightAttackType(int sp) => sp switch
		{
			0 => "通常",
			1 => "連撃",
			2 => "主魚CI",
			3 => "魚雷CI",
			4 => "主副CI",
			5 => "主主CI",
			100 => "Nelson Touch",
			101 => "一斉射(長門)",
			102 => "一斉射(陸奥)",
			103 => "Colorado特殊攻撃",
			104 => "僚艦夜戦突撃",
			200 => "夜間瑞雲CI",
			400 => "特殊攻撃(大和3隻)",
			401 => "特殊攻撃(大和2隻)",
			_ => $"夜戦特殊({sp})"
		};

		private static void ParseRaigeki(BattlePhaseInfo phase, JsonElement raigeki,
			List<BattleShipSnapshot> friendly, List<BattleShipSnapshot> enemy)
		{
			if (raigeki.TryGetProperty("api_frai", out var frai) && raigeki.TryGetProperty("api_fdam", out var fdam))
			{
				var damArr = fdam.EnumerateArray().Select(x => (int)Math.Floor(x.GetDouble())).ToArray();
				int offset = (damArr.Length > 0 && damArr[0] == -1) ? 1 : 0;
				for (int i = offset; i < damArr.Length && (i - offset) < friendly.Count; i++)
				{
					int dam = damArr[i];
					if (dam > 0)
					{
						int targetIdx = i - offset;
						friendly[targetIdx].HpAfter = Math.Max(0, friendly[targetIdx].HpAfter - dam);
						phase.Attacks.Add(new BattleAttackEntry
						{
							AttackerName = "敵雷撃",
							DefenderName = friendly[targetIdx].Name,
							Damage = dam,
						});
					}
				}
			}

			if (raigeki.TryGetProperty("api_erai", out var erai) && raigeki.TryGetProperty("api_edam", out var edam))
			{
				var damArr = edam.EnumerateArray().Select(x => (int)Math.Floor(x.GetDouble())).ToArray();
				int offset = (damArr.Length > 0 && damArr[0] == -1) ? 1 : 0;
				for (int i = offset; i < damArr.Length && (i - offset) < enemy.Count; i++)
				{
					int dam = damArr[i];
					if (dam > 0)
					{
						int targetIdx = i - offset;
						enemy[targetIdx].HpAfter = Math.Max(0, enemy[targetIdx].HpAfter - dam);
						phase.Attacks.Add(new BattleAttackEntry
						{
							AttackerName = "味方雷撃",
							DefenderName = enemy[targetIdx].Name,
							Damage = dam,
						});
					}
				}
			}
		}

		private static void ApplyDamages(BattlePhaseInfo phase, string attacker, List<BattleShipSnapshot> targets, JsonElement element, string propName = "")
		{
			var targetProp = string.IsNullOrEmpty(propName) ? element : (element.TryGetProperty(propName, out var p) ? p : default);
			if (targetProp.ValueKind != JsonValueKind.Array) return;

			var damArr = targetProp.EnumerateArray().Select(x => (int)Math.Floor(x.GetDouble())).ToArray();
			int offset = (damArr.Length > 0 && damArr[0] == -1) ? 1 : 0;
			for (int i = offset; i < damArr.Length && (i - offset) < targets.Count; i++)
			{
				int dam = damArr[i];
				if (dam > 0)
				{
					int targetIdx = i - offset;
					targets[targetIdx].HpAfter = Math.Max(0, targets[targetIdx].HpAfter - dam);
					phase.Attacks.Add(new BattleAttackEntry
					{
						AttackerName = attacker,
						DefenderName = targets[targetIdx].Name,
						Damage = dam,
					});
				}
			}
		}

		private static string GetShipNameByIndex(int index, List<BattleShipSnapshot> friendly, List<BattleShipSnapshot> enemy)
		{
			if (index >= 1 && index <= 6 && index - 1 < friendly.Count) return friendly[index - 1].Name;
			if (index >= 7 && index <= 12 && index - 7 < enemy.Count) return enemy[index - 7].Name;
			return $"艦船[{index}]";
		}

		private static void ApplyDamageToTarget(int dfIndex, int damage, List<BattleShipSnapshot> friendly, List<BattleShipSnapshot> enemy)
		{
			if (damage <= 0) return;
			if (dfIndex >= 1 && dfIndex <= 6 && dfIndex - 1 < friendly.Count)
			{
				friendly[dfIndex - 1].HpAfter = Math.Max(0, friendly[dfIndex - 1].HpAfter - damage);
			}
			else if (dfIndex >= 7 && dfIndex <= 12 && dfIndex - 7 < enemy.Count)
			{
				enemy[dfIndex - 7].HpAfter = Math.Max(0, enemy[dfIndex - 7].HpAfter - damage);
			}
		}

		private static void ProcessBattleResult(SortieNodeInfo node, string json,
			List<BattleShipSnapshot> friendly, List<BattleShipSnapshot> escort)
		{
			try
			{
				using var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;
				var data = root.TryGetProperty("api_data", out var d) ? d : root;

				if (data.TryGetProperty("api_win_rank", out var rank))
					node.WinRank = rank.GetString() ?? "";

				if (data.TryGetProperty("api_get_base_exp", out var bexp))
					node.BaseExp = bexp.GetInt32();

				if (data.TryGetProperty("api_get_exp", out var aexp))
					node.AdmiralExp = aexp.GetInt32();

				if (data.TryGetProperty("api_mvp", out var mvp))
				{
					int mvpIdx = mvp.GetInt32();
					if (mvpIdx >= 1 && mvpIdx <= friendly.Count)
						node.MvpShipName = friendly[mvpIdx - 1].Name;
				}

				if (data.TryGetProperty("api_get_ship", out var ship))
				{
					if (ship.TryGetProperty("api_ship_name", out var sName))
					{
						node.DropShipName = sName.GetString() ?? "";
					}
					else if (ship.TryGetProperty("api_ship_id", out var sid))
					{
						int id = sid.GetInt32();
						node.DropShipName = KCDatabase.Instance?.MasterShips[id]?.NameWithClass ?? $"艦船ID:{id}";
					}
				}
			}
			catch (Exception ex)
			{
				Utility.Logger.Add(2, $"ProcessBattleResult パースエラー: {ex.Message}");
			}
		}

		private static string GetNodeDescription(int eventId, int eventKind)
		{
			return eventId switch
			{
				2 => "資源獲得",
				3 => "うずしお",
				4 => eventKind switch
				{
					2 => "夜昼戦",
					3 => "開幕夜戦",
					4 => "航空戦",
					6 => "空襲戦",
					7 => "レーダー射撃",
					_ => "通常戦闘",
				},
				5 => "ボス戦闘",
				6 => "気のせい",
				7 => "航空偵察",
				8 => "船団護衛成功",
				9 => "揚陸地点",
				10 => "長距離空襲戦",
				_ => $"イベント({eventId}/{eventKind})",
			};
		}

		private static string GetItemName(int id)
		{
			return id switch
			{
				1 => "燃料",
				2 => "弾薬",
				3 => "鋼材",
				4 => "ボーキサイト",
				5 => "高速建造材",
				6 => "高速修復材",
				7 => "開発資材",
				8 => "改修資材",
				_ => $"資材[{id}]",
			};
		}

		private static bool IsBattleApi(string name)
		{
			return name.Contains("/battle")
				|| name.Contains("/airbattle")
				|| name.Contains("/sp_midnight")
				|| name.Contains("/midnight_battle")
				|| name.Contains("/ec_midnight_battle");
		}

		private static bool IsSecondBattleApi(string apiName)
		{
			return apiName.Contains("api_req_battle_midnight/battle")
				|| apiName.Contains("api_req_combined_battle/midnight_battle")
				|| apiName.Contains("api_req_combined_battle/ec_midnight_battle");
		}
	}
}
