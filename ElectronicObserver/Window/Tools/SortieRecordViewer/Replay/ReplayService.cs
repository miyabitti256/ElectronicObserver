#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.Replay
{
	public static class ReplayService
	{
		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			WriteIndented = false,
		};

		/// <summary>
		/// KC3改 互換のリプレイURLを生成します。
		/// </summary>
		public static string GenerateReplayUrl(SortieRecord record, IEnumerable<ApiFile> apiFiles)
		{
			string json = GenerateReplayJson(record, apiFiles);
			return "https://kc3kai.github.io/kancolle-replay/battleplayer.html#" + json;
		}

		/// <summary>
		/// KC3改 互換のリプレイJSONを生成します。
		/// </summary>
		public static string GenerateReplayJson(SortieRecord record, IEnumerable<ApiFile>? apiFiles = null)
		{
			if (record == null) throw new ArgumentNullException(nameof(record));
			apiFiles ??= record.ApiFiles ?? Enumerable.Empty<ApiFile>();

			var replay = record.ToReplayData();

			var battle = new ReplayBattle();
			var responses = apiFiles
				.Where(f => f.ApiFileType == ApiFileType.Response)
				.OrderBy(f => f.TimeStamp)
				.ToList();

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
					try
					{
						using var doc = JsonDocument.Parse(content);
						var root = doc.RootElement;
						var data = root.TryGetProperty("api_data", out var d) ? d : root;
						if (data.TryGetProperty("api_no", out var noProp))
						{
							battle.Node = noProp.GetInt32();
						}
					}
					catch
					{
						// Skip malformed packets
					}
					continue;
				}

				if (name.Contains("battleresult"))
				{
					try
					{
						using var doc = JsonDocument.Parse(content);
						var root = doc.RootElement;
						var data = root.TryGetProperty("api_data", out var d) ? d : root;

						battle.FirstBattle ??= new { };
						battle.SecondBattle ??= new { };
						if (data.TryGetProperty("api_get_base_exp", out var baseExpProp))
							battle.BaseExp = baseExpProp.GetInt32();
						if (data.TryGetProperty("api_get_exp", out var hqExpProp))
							battle.HqExp = hqExpProp.GetInt32();
						if (data.TryGetProperty("api_get_ship", out var shipProp) && shipProp.TryGetProperty("api_ship_id", out var shipIdProp))
							battle.Drop = shipIdProp.GetInt32();
						if (data.TryGetProperty("api_win_rank", out var rankProp))
							battle.Rating = rankProp.GetString();
						battle.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

						if (data.TryGetProperty("api_mvp", out var mvpProp))
						{
							battle.Mvp = new List<int> { mvpProp.GetInt32() };
						}
						if (data.TryGetProperty("api_mvp_combined", out var mvpCombProp) && mvpCombProp.ValueKind == JsonValueKind.Number)
						{
							battle.Mvp.Add(mvpCombProp.GetInt32());
						}

						replay.Battles.Add(battle);
						battle = new ReplayBattle();
					}
					catch
					{
						// Skip malformed packets
					}
					continue;
				}

				if (IsFirstBattleApi(name))
				{
					try
					{
						using var doc = JsonDocument.Parse(content);
						var root = doc.RootElement.Clone();
						var data = root.TryGetProperty("api_data", out var d) ? d.Clone() : root;
						battle.FirstBattle = data;
					}
					catch
					{
						// Skip malformed packets
					}
					continue;
				}

				if (IsSecondBattleApi(name))
				{
					try
					{
						using var doc = JsonDocument.Parse(content);
						var root = doc.RootElement.Clone();
						var data = root.TryGetProperty("api_data", out var d) ? d.Clone() : root;
						battle.SecondBattle = data;
					}
					catch
					{
						// Skip malformed packets
					}
					continue;
				}
			}

			// 戦闘終了パケットがなく途中で中断した戦闘の保持
			if (battle.FirstBattle != null || battle.SecondBattle != null)
			{
				battle.FirstBattle ??= new { };
				battle.SecondBattle ??= new { };
				replay.Battles.Add(battle);
			}

			return JsonSerializer.Serialize(replay, JsonOptions);
		}

		/// <summary>
		/// 出撃レコードから KC3改 互換の ReplayData を生成します。
		/// </summary>
		public static ReplayData ToReplayData(this SortieRecord record)
		{
			if (record == null) throw new ArgumentNullException(nameof(record));

			var replay = new ReplayData
			{
				Id = 1,
				Support1 = record.FleetData?.NodeSupportFleetId ?? 0,
				Support2 = record.FleetData?.BossSupportFleetId ?? 0,
				Combined = (int)(record.FleetData?.CombinedFlag ?? 0),
				DefeatCount = record.MapData?.RequiredDefeatedCount switch
				{
					null or -1 => 0,
					_ => record.MapData.RequiredDefeatedCount,
				},
				NowMaphp = record.MapData != null && record.MapData.MapHPMax > 0 ? record.MapData.MapHPCurrent : 0,
				MaxMaphp = record.MapData != null && record.MapData.MapHPMax > 0 ? record.MapData.MapHPMax : 0,
				Fleetnum = record.FleetData?.FleetId ?? 1,
				World = record.World,
				Mapnum = record.Map,
				Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
				Battles = new List<ReplayBattle>(),
			};

			if (record.FleetData?.Fleets != null)
			{
				replay.Fleet1 = ToReplayFleet(record.FleetData.Fleets.Skip(0).FirstOrDefault());
				replay.Fleet2 = ToReplayFleet(record.FleetData.Fleets.Skip(1).FirstOrDefault());
				replay.Fleet3 = ToReplayFleet(record.FleetData.Fleets.Skip(2).FirstOrDefault());
				replay.Fleet4 = ToReplayFleet(record.FleetData.Fleets.Skip(3).FirstOrDefault());
			}

			if (record.FleetData?.AirBases != null)
			{
				// イベント海域 (World != Map) でも基地航空隊が欠落しないよう、MapAreaId と World / Map の一致を判定
				replay.AirBases = record.FleetData.AirBases
					.Where(b => b.MapAreaId == record.World || b.MapAreaId == record.Map)
					.Select(b => new ReplayAirBase
					{
						Rid = b.AirCorpsId,
						Action = (int)b.ActionKind,
						Range = new ReplayRange
						{
							ApiBase = b.BaseDistance,
							ApiBonus = b.BonusDistance,
						},
						Planes = b.Squadrons?.Select(s => new ReplayAirBaseSquadron
						{
							EquipmentId = s.EquipmentSlot?.Equipment?.Id ?? 0,
							State = s.State,
							Count = s.EquipmentSlot?.AircraftCurrent ?? 0,
							Morale = (int)s.Condition,
							Stars = s.EquipmentSlot?.Equipment?.Level ?? 0,
							Ace = s.EquipmentSlot?.Equipment?.AircraftLevel ?? 0,
						}).ToList() ?? new List<ReplayAirBaseSquadron>(),
					}).ToList();
			}

			return replay;
		}

		private static List<ReplayShip>? ToReplayFleet(SortieFleet? fleet)
		{
			if (fleet == null || fleet.Ships == null) return null;
			return fleet.Ships.Select(ToReplayShip).ToList();
		}

		private static ReplayShip ToReplayShip(SortieShip ship)
		{
			var slots = new List<SortieEquipmentSlot?>();
			if (ship.EquipmentSlots != null) slots.AddRange(ship.EquipmentSlots);
			slots.Add(ship.ExpansionSlot);

			return new ReplayShip
			{
				ShipId = ship.Id,
				Level = ship.Level,
				Morale = ship.Condition,
				Kyouka = ship.Kyouka != null ? new List<int>(ship.Kyouka) : new List<int>(),
				Equip = slots.Select(s => s?.Equipment?.Id ?? 0).ToList(),
				Stars = slots.Select(s => s?.Equipment?.Level ?? 0).ToList(),
				Ace = slots.Select(s => s?.Equipment?.AircraftLevel ?? 0).ToList(),
			};
		}

		private static bool IsFirstBattleApi(string apiName)
		{
			return apiName.Contains("api_req_sortie/battle")
				|| apiName.Contains("api_req_sortie/airbattle")
				|| apiName.Contains("api_req_sortie/ld_airbattle")
				|| apiName.Contains("api_req_combined_battle/battle")
				|| apiName.Contains("api_req_combined_battle/airbattle")
				|| apiName.Contains("api_req_combined_battle/ld_airbattle")
				|| apiName.Contains("api_req_combined_battle/battle_water")
				|| apiName.Contains("api_req_combined_battle/each_battle")
				|| apiName.Contains("api_req_combined_battle/each_battle_water")
				|| apiName.Contains("api_req_combined_battle/ec_battle")
				|| apiName.Contains("api_req_combined_battle/ec_each_battle")
				|| apiName.Contains("api_req_battle_midnight/sp_midnight")
				|| apiName.Contains("api_req_combined_battle/sp_midnight");
		}

		private static bool IsSecondBattleApi(string apiName)
		{
			return apiName.Contains("api_req_battle_midnight/battle")
				|| apiName.Contains("api_req_combined_battle/midnight_battle")
				|| apiName.Contains("api_req_combined_battle/ec_midnight_battle");
		}
	}
}
