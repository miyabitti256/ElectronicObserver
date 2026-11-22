#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElectronicObserver.Data;
using ElectronicObserver.Database.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer
{
	public class SortieRecordViewModel
	{
		public SortieRecord Model { get; }

		public int Id => Model.Id;
		public int World => Model.World;
		public int Map => Model.Map;
		public string MapDisplay => $"{World}-{Map}";

		public DateTime SortieStartUtc { get; }
		public DateTime SortieStartLocal => SortieStartUtc.ToLocalTime();
		public string SortieStartDisplay => SortieStartUtc == DateTime.MinValue
			? "-"
			: SortieStartLocal.ToString("yyyy/MM/dd HH:mm:ss");

		public string FleetTypeDisplay
		{
			get
			{
				switch (Model.FleetData?.CombinedFlag ?? 0)
				{
					case 1:
						return "機動部隊";
					case 2:
						return "水上打撃";
					case 3:
						return "輸送部隊";
					case 0:
						return "通常艦隊";
					default:
						return "連合艦隊";
				}
			}
		}

		public bool HasSupport => (Model.FleetData?.NodeSupportFleetId ?? 0) > 0 ||
		                          (Model.FleetData?.BossSupportFleetId ?? 0) > 0;

		public string SupportDisplay
		{
			get
			{
				int node = Model.FleetData?.NodeSupportFleetId ?? 0;
				int boss = Model.FleetData?.BossSupportFleetId ?? 0;

				if (node > 0 && boss > 0) return "道中/決戦";
				if (node > 0) return "道中";
				if (boss > 0) return "決戦";
				return "-";
			}
		}

		public bool HasAirBase => (Model.FleetData?.AirBases?.Count ?? 0) > 0;

		public string AirBaseDisplay => HasAirBase
			? $"{Model.FleetData!.AirBases.Count}隊"
			: "-";

		public List<string> ShipNames { get; } = new List<string>();

		public string FleetSummary { get; }

		public string FleetDetailsTooltip { get; }

		public bool? ReachedBoss { get; set; }

		public CalculatedSortieCost? Cost => Model.CalculatedSortieCost;

		public SortieRecordViewModel(SortieRecord model)
		{
			Model = model ?? throw new ArgumentNullException(nameof(model));

			if (model.ApiFiles != null && model.ApiFiles.Count > 0)
			{
				SortieStartUtc = model.ApiFiles.Min(f => f.TimeStamp);
			}
			else
			{
				SortieStartUtc = DateTime.MinValue;
			}

			// 出撃艦隊の抽出
			var ships = GetMainFleetShips(model);
			var shipDescriptions = new List<string>();

			foreach (var ship in ships)
			{
				string name = GetShipName(ship.Id);
				ShipNames.Add(name);
				shipDescriptions.Add($"{name}(Lv{ship.Level})");
			}

			FleetSummary = shipDescriptions.Count > 0
				? string.Join(", ", shipDescriptions)
				: "編成情報なし";

			FleetDetailsTooltip = BuildFleetDetailsTooltip(model);
		}

		private static List<SortieShip> GetMainFleetShips(SortieRecord record)
		{
			var result = new List<SortieShip>();
			if (record.FleetData?.Fleets == null || record.FleetData.Fleets.Count == 0)
			{
				return result;
			}

			bool isCombined = (record.FleetData.CombinedFlag != 0);

			if (isCombined)
			{
				var fleet1 = record.FleetData.Fleets.ElementAtOrDefault(0);
				if (fleet1?.Ships != null)
				{
					result.AddRange(fleet1.Ships.Where(s => s != null));
				}

				var fleet2 = record.FleetData.Fleets.ElementAtOrDefault(1);
				if (fleet2?.Ships != null)
				{
					result.AddRange(fleet2.Ships.Where(s => s != null));
				}
			}
			else
			{
				int fleetIndex = (record.FleetData.FleetId > 0) ? record.FleetData.FleetId - 1 : 0;
				var fleet = record.FleetData.Fleets.ElementAtOrDefault(fleetIndex)
				            ?? record.FleetData.Fleets.FirstOrDefault(f => f != null && f.Ships?.Count > 0);

				if (fleet?.Ships != null)
				{
					result.AddRange(fleet.Ships.Where(s => s != null));
				}
			}

			return result;
		}

		private static string GetShipName(int masterId)
		{
			var master = KCDatabase.Instance?.MasterShips?[masterId];
			if (master != null)
			{
				return master.NameWithClass;
			}
			return $"艦#{masterId}";
		}

		private static string BuildFleetDetailsTooltip(SortieRecord record)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"出撃 ID: {record.Id}");
			sb.AppendLine($"海域: {record.World}-{record.Map}");

			if (record.FleetData?.Fleets != null)
			{
				for (int i = 0; i < record.FleetData.Fleets.Count; i++)
				{
					var fleet = record.FleetData.Fleets[i];
					if (fleet == null || fleet.Ships == null || fleet.Ships.Count == 0)
						continue;

					sb.AppendLine();
					sb.AppendLine($"【第{i + 1}艦隊: {fleet.Name}】");
					for (int sIdx = 0; sIdx < fleet.Ships.Count; sIdx++)
					{
						var ship = fleet.Ships[sIdx];
						if (ship == null) continue;
						string sName = GetShipName(ship.Id);
						sb.AppendLine($"  {sIdx + 1}. {sName} Lv.{ship.Level} (HP: {ship.Hp})");
					}
				}
			}

			if (record.FleetData?.AirBases != null && record.FleetData.AirBases.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine("【基地航空隊】");
				foreach (var ab in record.FleetData.AirBases)
				{
					sb.AppendLine($"  第{ab.AirBaseId}基地 ({ab.Name}) - 行動: {ab.ActionKind}");
				}
			}

			return sb.ToString().TrimEnd();
		}
	}
}
