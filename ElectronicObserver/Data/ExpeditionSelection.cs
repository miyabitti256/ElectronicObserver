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
		/// 大成功の警告閾値の既定値 (%)。
		/// </summary>
		public const int DefaultGreatSuccessRateThreshold = 100;

		/// <summary>
		/// 保存された艦隊ごとの「大成功しない場合も警告」を検証し、未設定・範囲外は既定 (OFF) で補います。
		/// 戻り値の index は艦隊番号で、2〜4 を使用します。
		/// </summary>
		public static bool[] SanitizeGreatSuccessAlerts(bool[] saved)
		{
			var result = new bool[MaxFleetId + 1];
			if (saved == null) return result;

			for (int fleetId = MinFleetId; fleetId <= MaxFleetId && fleetId < saved.Length; fleetId++)
				result[fleetId] = saved[fleetId];

			return result;
		}

		/// <summary>
		/// 保存された艦隊ごとの大成功の警告閾値を検証し、未設定・範囲外は既定 (100%) で補います。
		/// 戻り値の index は艦隊番号で、2〜4 を使用します。
		/// </summary>
		public static int[] SanitizeGreatSuccessThresholds(int[] saved)
		{
			var result = new int[MaxFleetId + 1];
			for (int i = 0; i < result.Length; i++)
				result[i] = DefaultGreatSuccessRateThreshold;

			if (saved == null) return result;

			for (int fleetId = MinFleetId; fleetId <= MaxFleetId && fleetId < saved.Length; fleetId++)
			{
				if (saved[fleetId] >= 0 && saved[fleetId] <= 100)
					result[fleetId] = saved[fleetId];
			}

			return result;
		}

		/// <summary>
		/// 指定した艦隊の「大成功しない場合も警告」が有効か (保存値が無い・不正な場合は既定 = OFF)。
		/// </summary>
		public static bool IsGreatSuccessAlertEnabled(bool[] saved, int fleetId)
		{
			var values = SanitizeGreatSuccessAlerts(saved);
			return fleetId >= 0 && fleetId < values.Length && values[fleetId];
		}

		/// <summary>
		/// 指定した艦隊の大成功の警告閾値 (保存値が無い・不正な場合は既定 = 100%)。
		/// </summary>
		public static int GetGreatSuccessThreshold(int[] saved, int fleetId)
		{
			var values = SanitizeGreatSuccessThresholds(saved);
			return (fleetId >= 0 && fleetId < values.Length) ? values[fleetId] : DefaultGreatSuccessRateThreshold;
		}

		/// <summary>
		/// 指定した艦隊で「大成功しない可能性がある」警告を出すべきか。
		/// その艦隊のチェックが ON で、かつ大成功率がその艦隊の閾値を下回る場合のみ true です
		/// (艦隊ごとに独立して判定します)。
		/// </summary>
		public static bool ShouldWarnGreatSuccess(bool[] alerts, int[] thresholds, int fleetId, double rate)
		{
			if (!IsGreatSuccessAlertEnabled(alerts, fleetId)) return false;

			return ExpeditionHelper.IsGreatSuccessRateBelowThreshold(rate, GetGreatSuccessThreshold(thresholds, fleetId));
		}
	}
}
