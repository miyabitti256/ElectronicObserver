#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using BrotliSharpLib;
using DynaJson;
using ElectronicObserver.Data;
using ElectronicObserver.Observer;
using ElectronicObserver.Resource.Record;
using ElectronicObserver.Utility.Data;

namespace ElectronicObserverTests
{
	/// <summary>
	/// テスト実行時に api_start2/getData および艦船パラメータレコードを一度だけ初期化し、
	/// 任意の艦船・装備・艦隊をインメモリで生成するフィクスチャヘルパーです。
	/// </summary>
	public static class TestMasterDataFixture
	{
		private static readonly object _lock = new object();
		private static bool _isLoaded = false;
		private static int _nextShipId = 1000;
		private static int _nextEquipmentId = 1000;
		private static int _nextFleetId = 10;

		/// <summary>
		/// マスタデータ (api_start2/getData) および艦船パラメータレコードが未ロードの場合、
		/// Brotli 圧縮フィクスチャから解凍して初期化します。
		/// </summary>
		public static void EnsureMasterDataLoaded()
		{
			if (_isLoaded && KCDatabase.Instance.MasterShips.Count > 100 && KCDatabase.Instance.Admiral != null && KCDatabase.Instance.Admiral.Level > 0)
			{
				return;
			}

			lock (_lock)
			{
				if (_isLoaded && KCDatabase.Instance.MasterShips.Count > 100 && KCDatabase.Instance.Admiral != null && KCDatabase.Instance.Admiral.Level > 0)
				{
					return;
				}

				// 1. api_start2.json.br のロード
				string[] candidatePaths = new[]
				{
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "api_start2.json.br"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Fixtures", "api_start2.json.br"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "ElectronicObserverTests", "Fixtures", "api_start2.json.br"),
					@"d:\apps\EO\ElectronicObserver\ElectronicObserverTests\Fixtures\api_start2.json.br",
				};

				string? fixturePath = candidatePaths.FirstOrDefault(File.Exists);
				if (fixturePath == null)
				{
					throw new FileNotFoundException($"api_start2.json.br が見つかりませんでした。探索パス: {string.Join("; ", candidatePaths)}");
				}

				byte[] compressed = File.ReadAllBytes(fixturePath);
				byte[] decompressed = Brotli.DecompressBuffer(compressed, 0, compressed.Length);
				string json = Encoding.UTF8.GetString(decompressed);

				if (!json.StartsWith("svdata="))
				{
					json = "svdata=" + json;
				}

				APIObserver.Instance.LoadResponse("/kcsapi/api_start2/getData", json);

				// 2. ShipParameterRecord.csv.br のロード
				string[] candidateCsvPaths = new[]
				{
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "ShipParameterRecord.csv.br"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Fixtures", "ShipParameterRecord.csv.br"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "ElectronicObserverTests", "Fixtures", "ShipParameterRecord.csv.br"),
					@"d:\apps\EO\ElectronicObserver\ElectronicObserverTests\Fixtures\ShipParameterRecord.csv.br",
				};

				string? csvFixturePath = candidateCsvPaths.FirstOrDefault(File.Exists);
				if (csvFixturePath != null)
				{
					byte[] compressedCsv = File.ReadAllBytes(csvFixturePath);
					byte[] decompressedCsv = Brotli.DecompressBuffer(compressedCsv, 0, compressedCsv.Length);
					using var ms = new MemoryStream(decompressedCsv);
					using var reader = new StreamReader(ms, Encoding.UTF8);
					string? line;
					reader.ReadLine(); // ヘッダ読み飛ばし
					while ((line = reader.ReadLine()) != null)
					{
						if (string.IsNullOrWhiteSpace(line)) continue;
						try
						{
							var elem = new ShipParameterRecord.ShipParameterElement(line);
							RecordManager.Instance.ShipParameter.Update(elem);
						}
						catch
						{
							// パースエラー行はスキップ
						}
					}
				}

				// 3. 提督情報のデフォルト設定 (Lv120)
				SetAdmiralLevel(120);

				_isLoaded = true;
			}
		}

		/// <summary>
		/// 提督レベルを設定・更新します。
		/// </summary>
		public static void SetAdmiralLevel(int level)
		{
			var admJson = JsonObject.Parse($"{{\"api_member_id\":1,\"api_nickname\":\"提督\",\"api_level\":{level},\"api_rank\":1,\"api_experience\":50000000}}");
			KCDatabase.Instance.Admiral.LoadFromResponse("api_get_member/basic", admJson);
		}

		/// <summary>
		/// 任意のパラメータを持つ艦船データ (ShipData) を生成して KCDatabase に登録します。
		/// </summary>
		public static ShipData CreateShip(
			int masterShipId,
			int level = 99,
			int hp = -1,
			int[]? slotEquipmentIds = null,
			int[]? slotLevels = null,
			int[]? slotAircraftLevels = null,
			int[]? slotAircraft = null,
			int expansionSlotEquipmentId = -1,
			int expansionSlotLevel = 0,
			int[]? kyouka = null,
			int asw = -1,
			int los = -1)
		{
			EnsureMasterDataLoaded();

			var master = KCDatabase.Instance.MasterShips[masterShipId]
				?? throw new ArgumentException($"MasterShip ID {masterShipId} が見つかりません。");

			var eqList = new List<int>();
			if (slotEquipmentIds != null)
			{
				for (int i = 0; i < slotEquipmentIds.Length; i++)
				{
					int eqId = slotEquipmentIds[i];
					if (eqId <= 0)
					{
						eqList.Add(-1);
						continue;
					}

					int star = (slotLevels != null && i < slotLevels.Length) ? slotLevels[i] : 0;
					int alv = (slotAircraftLevels != null && i < slotAircraftLevels.Length) ? slotAircraftLevels[i] : 0;
					int uniqueId = Interlocked.Increment(ref _nextEquipmentId);

					var eq = new EquipmentData();
					string eqJsonStr = $"{{\"api_id\":{uniqueId},\"api_slotitem_id\":{eqId},\"api_level\":{star},\"api_alv\":{alv},\"api_locked\":1}}";
					eq.LoadFromResponse("api_get_member/slot_item", JsonObject.Parse(eqJsonStr));

					AddToIDDictionary(KCDatabase.Instance.Equipments, eq);
					eqList.Add(uniqueId);
				}
			}

			int exUniqueId = 0;
			if (expansionSlotEquipmentId > 0)
			{
				exUniqueId = Interlocked.Increment(ref _nextEquipmentId);
				var exEq = new EquipmentData();
				string exJsonStr = $"{{\"api_id\":{exUniqueId},\"api_slotitem_id\":{expansionSlotEquipmentId},\"api_level\":{expansionSlotLevel},\"api_locked\":1}}";
				exEq.LoadFromResponse("api_get_member/slot_item", JsonObject.Parse(exJsonStr));
				AddToIDDictionary(KCDatabase.Instance.Equipments, exEq);
			}
			else if (expansionSlotEquipmentId == -1)
			{
				exUniqueId = -1;
			}

			// 装備ステータス合計値の算出 (LOSTotal / ASWTotal への加算用)
			int totalEquipLos = 0;
			int totalEquipAsw = 0;
			foreach (int uid in eqList)
			{
				if (uid > 0 && KCDatabase.Instance.Equipments.ContainsKey(uid))
				{
					var eqData = KCDatabase.Instance.Equipments[uid];
					if (eqData?.MasterEquipment != null)
					{
						totalEquipLos += eqData.MasterEquipment.LOS;
						totalEquipAsw += eqData.MasterEquipment.ASW;
					}
				}
			}
			if (exUniqueId > 0 && KCDatabase.Instance.Equipments.ContainsKey(exUniqueId))
			{
				var exEqData = KCDatabase.Instance.Equipments[exUniqueId];
				if (exEqData?.MasterEquipment != null)
				{
					totalEquipLos += exEqData.MasterEquipment.LOS;
					totalEquipAsw += exEqData.MasterEquipment.ASW;
				}
			}

			int shipUniqueId = Interlocked.Increment(ref _nextShipId);
			int maxHp = master.HPMin + (level > 99 ? Math.Max(0, master.HPMax - master.HPMin) : 0);
			if (hp <= 0) hp = maxHp;

			int[] onslot = slotAircraft ?? master.Aircraft.ToArray();
			int[] kyoukaArray = kyouka ?? new int[] { 0, 0, 0, 0, 0, 0, 0 };

			int aswBase = asw >= 0 ? asw : (master.ASW?.GetParameter(level) ?? 0);
			int losBase = los >= 0 ? los : (master.LOS?.GetParameter(level) ?? 20);

			int aswMax = master.ASW?.Maximum ?? 100;
			int losMax = master.LOS?.Maximum ?? 100;

			var shipPayload = new
			{
				api_id = shipUniqueId,
				api_sortno = master.SortID,
				api_ship_id = masterShipId,
				api_lv = level,
				api_exp = new long[] { 100000, 0 },
				api_nowhp = hp,
				api_maxhp = maxHp,
				api_soku = master.Speed,
				api_leng = master.Range,
				api_slot = eqList.ToArray(),
				api_slot_ex = exUniqueId,
				api_onslot = onslot,
				api_kyouka = kyoukaArray,
				api_fuel = master.Fuel,
				api_bull = master.Ammo,
				api_cond = 49,
				api_taisen = new int[] { aswBase + totalEquipAsw, aswMax },
				api_sakuteki = new int[] { losBase + totalEquipLos, losMax },
				api_karyoku = new int[] { master.FirepowerMin, master.FirepowerMax },
				api_raisou = new int[] { master.TorpedoMin, master.TorpedoMax },
				api_taiku = new int[] { master.AAMin, master.AAMax },
				api_soukou = new int[] { master.ArmorMin, master.ArmorMax },
				api_lucky = new int[] { master.LuckMin, master.LuckMax }
			};

			var ship = new ShipData();
			string shipJsonStr = JsonSerializer.Serialize(shipPayload);
			ship.LoadFromResponse("api_port/port", JsonObject.Parse(shipJsonStr));

			AddToIDDictionary(KCDatabase.Instance.Ships, ship);
			return ship;
		}

		/// <summary>
		/// 指定された艦娘たちから艦隊データ (FleetData) を生成して KCDatabase に登録します。
		/// </summary>
		public static FleetData CreateFleet(params ShipData[] ships)
		{
			EnsureMasterDataLoaded();

			int fleetId = Interlocked.Increment(ref _nextFleetId);
			var memberIds = ships.Select(s => s.MasterID).Concat(Enumerable.Repeat(-1, 6 - ships.Length)).Take(6).ToArray();

			var fleetPayload = new
			{
				api_id = fleetId,
				api_name = $"第{fleetId}艦隊",
				api_ship = memberIds,
				api_mission = new long[] { 0, 0, 0, 0 }
			};

			var fleet = new FleetData();
			string fleetJsonStr = JsonSerializer.Serialize(fleetPayload);
			fleet.LoadFromResponse("api_port/port", JsonObject.Parse(fleetJsonStr));

			AddToIDDictionary(KCDatabase.Instance.Fleet.Fleets, fleet);
			return fleet;
		}

		/// <summary>
		/// 基地航空隊データ (BaseAirCorpsData) を生成して KCDatabase に登録します。
		/// </summary>
		public static BaseAirCorpsData CreateAirBase(int areaId, int corpsId, int actionKind, params (int equipmentId, int count, int level, int aircraftLevel)[] squadrons)
		{
			EnsureMasterDataLoaded();

			var planes = new List<object>();
			for (int i = 0; i < squadrons.Length && i < 4; i++)
			{
				var sq = squadrons[i];
				int uniqueEqId = 0;
				if (sq.equipmentId > 0)
				{
					uniqueEqId = Interlocked.Increment(ref _nextEquipmentId);
					var eq = new EquipmentData();
					string eqJsonStr = $"{{\"api_id\":{uniqueEqId},\"api_slotitem_id\":{sq.equipmentId},\"api_level\":{sq.level},\"api_alv\":{sq.aircraftLevel},\"api_locked\":1}}";
					eq.LoadFromResponse("api_get_member/slot_item", JsonObject.Parse(eqJsonStr));
					AddToIDDictionary(KCDatabase.Instance.Equipments, eq);
				}

				planes.Add(new
				{
					api_squadron_id = i + 1,
					api_state = sq.equipmentId > 0 ? 1 : 0,
					api_slotid = uniqueEqId,
					api_count = sq.count,
					api_max_count = 18,
					api_cond = 1
				});
			}

			var corpsPayload = new
			{
				api_area_id = areaId,
				api_rid = corpsId,
				api_name = $"第{corpsId}航空隊",
				api_distance = new { api_base = 7, api_bonus = 0 },
				api_action_kind = actionKind,
				api_plane_info = planes.ToArray()
			};

			var corps = new BaseAirCorpsData();
			string corpsJsonStr = JsonSerializer.Serialize(corpsPayload);
			corps.LoadFromResponse("api_get_member/base_air_corps", JsonObject.Parse(corpsJsonStr));

			AddToIDDictionary(KCDatabase.Instance.BaseAirCorps, corps);
			return corps;
		}

		/// <summary>
		/// IDDictionary にアイテムを登録または上書きします（内部の Remove/Add をリフレクション経由で呼び出します）。
		/// </summary>
		private static void AddToIDDictionary<T>(IDDictionary<T> dict, T item) where T : class, IIdentifiable
		{
			var type = typeof(IDDictionary<T>);
			var removeMethod = type.GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(int) }, null);
			var addMethod = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(T) }, null);

			removeMethod?.Invoke(dict, new object[] { item.ID });
			addMethod?.Invoke(dict, new object[] { item });
		}
	}
}