using System.Collections.Generic;
using System.Linq;
using ElectronicObserver.Data;

namespace ElectronicObserver.Database.Sortie
{
	public static class SortieSnapshotFactory
	{
		public static SortieMapData CreateMapData(KCDatabase db, int world, int mapId)
		{
			if (db?.MapInfo == null) return new SortieMapData();

			MapInfoData map = db.MapInfo.Values.FirstOrDefault(m => m.MapAreaID == world && m.MapInfoID == mapId);
			if (map == null) return new SortieMapData();

			return new SortieMapData
			{
				RequiredDefeatedCount = map.RequiredDefeatedCount,
				MapHPCurrent = map.MapHPCurrent,
				MapHPMax = map.MapHPMax,
			};
		}

		public static int GetNodeSupportFleetId(KCDatabase db, int world)
		{
			if (db?.Fleet?.Fleets == null) return 0;
			int targetMission = world > 21 ? 301 : 33;
			var fleet = db.Fleet.Fleets.Values.FirstOrDefault(f => f != null && f.ExpeditionDestination == targetMission);
			return fleet?.FleetID ?? 0;
		}

		public static int GetBossSupportFleetId(KCDatabase db, int world)
		{
			if (db?.Fleet?.Fleets == null) return 0;
			int targetMission = world > 21 ? 302 : 34;
			var fleet = db.Fleet.Fleets.Values.FirstOrDefault(f => f != null && f.ExpeditionDestination == targetMission);
			return fleet?.FleetID ?? 0;
		}

		public static SortieFleetData CreateFleetData(KCDatabase db, int fleetId, int world)
		{
			if (db?.Fleet == null) return new SortieFleetData();

			int nodeSupportFleetId = GetNodeSupportFleetId(db, world);
			int bossSupportFleetId = GetBossSupportFleetId(db, world);
			int combinedFlag = db.Fleet.CombinedFlag;

			var fleets = db.Fleet.Fleets.Values;
			var airBases = db.BaseAirCorps?.Values ?? Enumerable.Empty<BaseAirCorpsData>();

			return MakeSortieFleetData(fleets, airBases, combinedFlag, fleetId, nodeSupportFleetId, bossSupportFleetId, world);
		}

		public static SortieFleetData CreateFleetAfterSortieData(KCDatabase db, SortieRecord sortie)
		{
			if (db?.Fleet == null || sortie == null) return new SortieFleetData();

			int fleetId = sortie.FleetData?.FleetId ?? 1;
			int nodeSupportFleetId = sortie.FleetData?.NodeSupportFleetId ?? 0;
			int bossSupportFleetId = sortie.FleetData?.BossSupportFleetId ?? 0;
			int combinedFlag = db.Fleet.CombinedFlag;

			var fleets = db.Fleet.Fleets.Values;
			var airBases = db.BaseAirCorps?.Values ?? Enumerable.Empty<BaseAirCorpsData>();

			return MakeSortieFleetData(fleets, airBases, combinedFlag, fleetId, nodeSupportFleetId, bossSupportFleetId, sortie.World);
		}

		public static SortieFleetData MakeSortieFleetData(IEnumerable<FleetData> fleets,
			IEnumerable<BaseAirCorpsData> airBases, int combinedFlag, int fleetId, int nodeSupportFleetId,
			int bossSupportFleetId, int world)
		{
			return new SortieFleetData
			{
				FleetId = fleetId,
				NodeSupportFleetId = nodeSupportFleetId,
				BossSupportFleetId = bossSupportFleetId,
				CombinedFlag = combinedFlag,
				Fleets = (fleets ?? Enumerable.Empty<FleetData>())
					.Select(f =>
					{
						if (f == null) return null;
						if (ShouldIncludeFleet(f, combinedFlag, fleetId, nodeSupportFleetId, bossSupportFleetId))
						{
							return MakeSortieFleet(f);
						}
						return null;
					}).ToList(),
				AirBases = (airBases ?? Enumerable.Empty<BaseAirCorpsData>())
					.Where(a => a != null && a.MapAreaID == world)
					.Select(MakeSortieAirBase)
					.ToList(),
			};
		}

		private static bool ShouldIncludeFleet(FleetData fleet, int combinedFlag, int fleetId, int nodeSupportFleetId, int bossSupportFleetId)
		{
			if (fleet == null) return false;
			return fleet.FleetID == fleetId ||
				(fleet.FleetID == 2 && combinedFlag != 0) ||
				(nodeSupportFleetId > 0 && fleet.FleetID == nodeSupportFleetId) ||
				(bossSupportFleetId > 0 && fleet.FleetID == bossSupportFleetId);
		}

		public static SortieFleet MakeSortieFleet(FleetData f)
		{
			if (f == null) return new SortieFleet();
			return new SortieFleet
			{
				Name = f.Name ?? "",
				Ships = f.MembersInstance?
					.Where(s => s != null)
					.Select(MakeSortieShip)
					.ToList() ?? new List<SortieShip>()
			};
		}

		public static SortieShip MakeSortieShip(ShipData s)
		{
			if (s == null) return new SortieShip();

			var ship = new SortieShip
			{
				Id = s.MasterShip?.ShipID ?? s.ShipID,
				DropId = s.MasterID,
				Level = s.Level,
				Condition = s.Condition,
				Kyouka = s.Kyouka != null ? new List<int>(s.Kyouka) : new List<int>(),
				Fuel = s.Fuel,
				Ammo = s.Ammo,
				Hp = s.HPCurrent,
				Armor = s.ArmorTotal,
				Evasion = s.EvasionTotal,
				Aircraft = s.Aircraft != null ? new List<int>(s.Aircraft) : new List<int>(),
				Range = s.Range,
				Speed = s.Speed,
				Firepower = s.FirepowerTotal,
				Torpedo = s.TorpedoTotal,
				Aa = s.AATotal,
				Asw = s.ASWTotal,
				Search = s.LOSTotal,
				Luck = s.LuckTotal,
				EquipmentSlots = new List<SortieEquipmentSlot>(),
			};

			if (s.SlotInstance != null && s.Aircraft != null)
			{
				var aircraftMax = s.AircraftMax ?? s.MasterShip?.Aircraft;
				for (int i = 0; i < s.SlotInstance.Count; i++)
				{
					var eq = s.SlotInstance[i];
					int current = i < s.Aircraft.Count ? s.Aircraft[i] : 0;
					int max = (aircraftMax != null && i < aircraftMax.Count) ? aircraftMax[i] : 0;

					ship.EquipmentSlots.Add(new SortieEquipmentSlot
					{
						Equipment = eq != null ? new SortieEquipment
						{
							Id = eq.EquipmentID,
							Level = eq.Level,
							AircraftLevel = eq.AircraftLevel,
						} : null,
						AircraftCurrent = current,
						AircraftMax = max,
					});
				}
			}

			if (s.IsExpansionSlotAvailable)
			{
				var eq = s.ExpansionSlotInstance;
				ship.ExpansionSlot = new SortieEquipmentSlot
				{
					Equipment = eq != null ? new SortieEquipment
					{
						Id = eq.EquipmentID,
						Level = eq.Level,
						AircraftLevel = eq.AircraftLevel,
					} : null,
					AircraftCurrent = 0,
					AircraftMax = 0,
				};
			}
			else
			{
				ship.ExpansionSlot = null;
			}

			if (s.SpItemKind > 0)
			{
				ship.SpecialEffectItems = new List<SpecialEffectItem>
				{
					new SpecialEffectItem
					{
						ApiKind = s.SpItemKind,
						Firepower = s.SpItemHoug,
						Torpedo = s.SpItemRaig,
						Armor = s.SpItemSouk,
						Evasion = s.SpItemKaih,
					}
				};
			}

			return ship;
		}

		public static SortieAirBase MakeSortieAirBase(BaseAirCorpsData a)
		{
			if (a == null) return new SortieAirBase();
			return new SortieAirBase
			{
				Name = a.Name ?? "",
				ActionKind = a.ActionKind,
				AirCorpsId = a.AirCorpsID,
				BaseDistance = a.BaseDistance,
				BonusDistance = a.BonusDistance,
				MapAreaId = a.MapAreaID,
				Squadrons = a.Squadrons?.Values
					.Select(MakeSortieAirBaseSquadron)
					.ToList() ?? new List<SortieAirBaseSquadron>()
			};
		}

		public static SortieAirBaseSquadron MakeSortieAirBaseSquadron(BaseAirCorpsSquadron s)
		{
			if (s == null) return new SortieAirBaseSquadron();
			return new SortieAirBaseSquadron
			{
				AircraftCurrent = s.AircraftCurrent,
				State = s.State,
				Condition = s.Condition,
				EquipmentSlot = new SortieEquipmentSlot
				{
					AircraftCurrent = s.AircraftCurrent,
					AircraftMax = s.AircraftMax,
					Equipment = s.EquipmentInstance != null ? new SortieEquipment
					{
						Id = s.EquipmentInstance.EquipmentID,
						Level = s.EquipmentInstance.Level,
						AircraftLevel = s.EquipmentInstance.AircraftLevel,
					} : null,
				}
			};
		}
	}
}
