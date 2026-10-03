using ElectronicObserver.Utility.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElectronicObserver.Data
{
	/// <summary>
	/// 遠征の大成功タイプ
	/// </summary>
	public enum ExpeditionGreatSuccessType
	{
		/// <summary> 通常型 (全員キラ必須) </summary>
		Regular,
		/// <summary> ドラム缶型 (ドラム缶ボーナス + キラ数) </summary>
		Drum,
		/// <summary> 旗艦レベル型 (旗艦Lv + キラ数) </summary>
		Level,
	}

	/// <summary>
	/// 遠征の大成功条件・確率計算ヘルパー
	/// </summary>
	public static class ExpeditionHelper
	{
		private static readonly HashSet<int> DrumExpeditions = new HashSet<int>
		{
			21, 24, 37, 38, 40, 44, 142
		};

		private static readonly HashSet<int> LevelExpeditions = new HashSet<int>
		{
			32, 41, 43, 45, 46,
			101, 102, 103, 104, 105, 106,
			112, 113, 114, 115,
			131, 132, 133,
			141
		};

		/// <summary>
		/// 遠征IDの大成功タイプを取得します。
		/// </summary>
		public static ExpeditionGreatSuccessType GetGreatSuccessType(int missionId)
		{
			if (DrumExpeditions.Contains(missionId))
				return ExpeditionGreatSuccessType.Drum;
			if (LevelExpeditions.Contains(missionId))
				return ExpeditionGreatSuccessType.Level;
			return ExpeditionGreatSuccessType.Regular;
		}

		/// <summary>
		/// ドラム缶型遠征の大成功に必要なドラム缶数を取得します。
		/// </summary>
		public static int GetGreatSuccessDrumRequirement(int missionId)
		{
			switch (missionId)
			{
				case 21: return 4;
				case 24: return 2;
				case 37: return 5;
				case 38: return 10;
				case 40: return 4;
				case 44: return 8;
				case 142: return 6;
				default: return 0;
			}
		}

		/// <summary>
		/// 艦隊の大成功確率を計算します (0.0 - 1.0)。
		/// </summary>
		public static double CalculateGreatSuccessRate(FleetData fleet, int missionId)
		{
			if (fleet == null) return 0.0;

			var members = fleet.MembersInstance.Where(s => s != null).ToList();
			if (!members.Any()) return 0.0;

			int sparkleCount = members.Count(s => s.Condition > 49);
			bool allSparkled = members.All(s => s.Condition > 49);
			int flagshipLevel = members.FirstOrDefault()?.Level ?? 0;
			int drumCount = members.Sum(s => s.AllSlotInstance.Count(e => e != null && e.MasterEquipment.CategoryType == EquipmentTypes.TransportContainer));

			var type = GetGreatSuccessType(missionId);
			double rate = 0.0;

			switch (type)
			{
				case ExpeditionGreatSuccessType.Regular:
					// 全員キラキラの場合のみ大成功が発生 (キラ数 * 15% + 21%)
					rate = allSparkled ? (sparkleCount * 0.15 + 0.21) : 0.0;
					break;

				case ExpeditionGreatSuccessType.Drum:
					rate = sparkleCount * 0.15 + GetDrumBonus(missionId, drumCount);
					break;

				case ExpeditionGreatSuccessType.Level:
					rate = sparkleCount * 0.15 + 0.16 + Math.Sqrt(flagshipLevel) / 100.0 + flagshipLevel / 1000.0;
					break;
			}

			return Math.Max(0.0, Math.Min(1.0, rate));
		}

		private static double GetDrumBonus(int missionId, int drumCount)
		{
			int req = GetGreatSuccessDrumRequirement(missionId);
			if (req > 0 && drumCount >= req)
				return 0.41;
			return 0.06;
		}

		/// <summary>
		/// 大成功条件の解説テキストを取得します。
		/// </summary>
		public static string GetGreatSuccessConditionDescription(int missionId)
		{
			var type = GetGreatSuccessType(missionId);
			switch (type)
			{
				case ExpeditionGreatSuccessType.Regular:
					return "通常型: 全員キラキラで大成功率UP (全員キラ必須)";
				case ExpeditionGreatSuccessType.Drum:
					int drums = GetGreatSuccessDrumRequirement(missionId);
					return $"ドラム缶型: ドラム缶{drums}個以上 + キラ艦数で大成功率UP";
				case ExpeditionGreatSuccessType.Level:
					return "旗艦Lv型: 旗艦Lv + キラ艦数で大成功率UP";
				default:
					return "";
			}
		}
	}
}
