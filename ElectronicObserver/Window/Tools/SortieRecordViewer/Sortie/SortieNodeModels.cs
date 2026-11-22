#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie
{
	public class SortieDetailData
	{
		public int SortieId { get; set; }
		public int World { get; set; }
		public int Map { get; set; }
		public DateTime StartTime { get; set; }
		public List<SortieNodeInfo> Nodes { get; set; } = new List<SortieNodeInfo>();
		public bool ReachedBoss => Nodes.Any(n => n.IsBoss);
	}

	public class SortieNodeInfo
	{
		public int CellId { get; set; }
		public string CellDisplay { get; set; } = "";
		public int EventId { get; set; }
		public int EventKind { get; set; }
		public string NodeTypeDescription { get; set; } = "";
		public bool IsBoss => EventId == 5;
		public bool IsBattleNode => Phases.Count > 0 || !string.IsNullOrEmpty(WinRank) || SpecialAttackType > 0 || IsNightBattleStart || IsAirRaid || HasNightBattle;
		public string WinRank { get; set; } = "";
		public string MvpShipName { get; set; } = "";
		public int BaseExp { get; set; }
		public int AdmiralExp { get; set; }
		public string DropShipName { get; set; } = "";
		public string NonBattleInfo { get; set; } = "";
		public int PlayerFormation { get; set; }
		public int EnemyFormation { get; set; }
		public int EngagementForm { get; set; }
		public int AirControlState { get; set; }
		public int SmokeType { get; set; }

		public int HappeningMstId { get; set; }
		public int HappeningCount { get; set; }
		public bool HasNightBattle { get; set; }
		public bool IsNightBattleStart { get; set; }
		public bool IsAirRaid { get; set; }
		public bool IsSubmarineOnly { get; set; }
		public int SpecialAttackType { get; set; }
		public List<int> SpecialAttackShipIndexes { get; set; } = new List<int>();

		public List<BattlePhaseInfo> Phases { get; set; } = new List<BattlePhaseInfo>();
		public List<BattleShipSnapshot> FriendlyShipsAfter { get; set; } = new List<BattleShipSnapshot>();
		public List<BattleShipSnapshot> EscortShipsAfter { get; set; } = new List<BattleShipSnapshot>();
		public List<BattleShipSnapshot> EnemyShipsAfter { get; set; } = new List<BattleShipSnapshot>();

		public List<BattleShipSnapshot>? FriendlyShipsDayAfter { get; set; }
		public List<BattleShipSnapshot>? EscortShipsDayAfter { get; set; }
		public List<BattleShipSnapshot>? FriendlyShipsNightBefore { get; set; }
		public List<BattleShipSnapshot>? EscortShipsNightBefore { get; set; }
	}

	public class BattlePhaseInfo
	{
		public string PhaseName { get; set; } = "";
		public string Summary { get; set; } = "";
		public int FriendlyAircraftLost { get; set; }
		public List<BattleAttackEntry> Attacks { get; set; } = new List<BattleAttackEntry>();
	}

	public class BattleAttackEntry
	{
		public string AttackerName { get; set; } = "";
		public string DefenderName { get; set; } = "";
		public int AttackerIndex { get; set; }
		public int DefenderIndex { get; set; }
		public int Damage { get; set; }
		public bool IsCritical { get; set; }
		public string AttackType { get; set; } = "";

		public string DisplayText => $"{AttackerName} → {DefenderName} : {Damage} ダメージ{(IsCritical ? " (Critical)" : "")}{(string.IsNullOrEmpty(AttackType) ? "" : $" [{AttackType}]")}";
	}

	public class BattleShipSnapshot
	{
		public int ShipMasterId { get; set; }
		public string Name { get; set; } = "";
		public int Level { get; set; }
		public int HpBefore { get; set; }
		public int HpAfter { get; set; }
		public int HpMax { get; set; }
		public string DamageStateText { get; set; } = "";

		public string HpDisplay => $"{HpAfter}/{HpMax} ({DamageStateText})";

		public BattleShipSnapshot Clone() => new BattleShipSnapshot
		{
			ShipMasterId = ShipMasterId,
			Name = Name,
			Level = Level,
			HpBefore = HpBefore,
			HpAfter = HpAfter,
			HpMax = HpMax,
			DamageStateText = DamageStateText,
		};

		public static string CalculateDamageState(int hpCurrent, int hpMax)
		{
			if (hpMax <= 0) return "無傷";
			if (hpCurrent <= 0) return "撃沈";

			double rate = (double)hpCurrent / hpMax;
			if (rate > 0.75) return hpCurrent == hpMax ? "無傷" : "小破未満";
			if (rate > 0.50) return "小破";
			if (rate > 0.25) return "中破";
			return "大破";
		}
	}
}
