using System;

namespace ElectronicObserver.Data
{
	/// <summary>
	/// 遠征可否ウィンドウの選択状態 (選択中の艦隊・タブ・艦隊ごとの遠征) の
	/// 妥当性検証と既定値へのフォールバックを行う純ロジックです。
	/// UI に依存しないため単体テストできます。
	/// </summary>
	public static class ExpeditionSelection
	{
		public const int MinFleetId = 2;
		public const int MaxFleetId = 4;

		/// <summary>
		/// 既定の選択遠征 (index = 艦隊番号)。
		/// 第2艦隊: 2 (長距離練習航海), 第3艦隊: 5 (海上護衛任務), 第4艦隊: 38 (東京急行(弐))
		/// </summary>
		public static readonly int[] DefaultTargetMissions = { 0, 0, 2, 5, 38 };

		/// <summary>
		/// 保存された艦隊ごとの遠征IDを検証し、未設定・範囲外は既定値で補います。
		/// </summary>
		public static int[] SanitizeTargetMissions(int[] saved)
		{
			var result = (int[])DefaultTargetMissions.Clone();
			if (saved == null) return result;

			for (int fleetId = MinFleetId; fleetId <= MaxFleetId && fleetId < saved.Length; fleetId++)
			{
				if (saved[fleetId] > 0)
					result[fleetId] = saved[fleetId];
			}

			return result;
		}

		/// <summary>
		/// 保存された選択中の艦隊を検証し、範囲外は既定 (第2艦隊) にフォールバックします。
		/// </summary>
		public static int SanitizeFleetId(int saved)
		{
			return (saved >= MinFleetId && saved <= MaxFleetId) ? saved : MinFleetId;
		}

		/// <summary>
		/// 保存された選択中のタブを検証し、範囲外は既定 (先頭タブ) にフォールバックします。
		/// </summary>
		public static int SanitizeTabIndex(int saved, int tabCount)
		{
			return (saved >= 0 && saved < tabCount) ? saved : 0;
		}

		/// <summary>
		/// 大成功の警告閾値を検証し、範囲外 (0〜100 の外) は既定 (100%) にフォールバックします。
		/// </summary>
		public static int SanitizeGreatSuccessRateThreshold(int saved)
		{
			return (saved >= 0 && saved <= 100) ? saved : DefaultGreatSuccessRateThreshold;
		}

		/// <summary>
		/// 大成功の警告閾値の既定値 (%)。
		/// </summary>
		public const int DefaultGreatSuccessRateThreshold = 100;
	}
}
