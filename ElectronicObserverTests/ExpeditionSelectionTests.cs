using ElectronicObserver.Data;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the persisted-selection restore logic used by FormExpeditionCheck
	/// (#9): invalid or missing saved values must fall back to the defaults.
	/// </summary>
	public class ExpeditionSelectionTests
	{
		[Fact]
		public void SanitizeTargetMissions_UsesDefaultsWhenSavedIsNull()
		{
			Assert.Equal(new[] { 0, 0, 2, 5, 38 }, ExpeditionSelection.SanitizeTargetMissions(null));
		}

		[Fact]
		public void SanitizeTargetMissions_UsesDefaultsForMissingEntries()
		{
			Assert.Equal(new[] { 0, 0, 2, 5, 38 }, ExpeditionSelection.SanitizeTargetMissions(new int[0]));
			Assert.Equal(new[] { 0, 0, 7, 5, 38 }, ExpeditionSelection.SanitizeTargetMissions(new[] { 0, 0, 7 }));
		}

		[Fact]
		public void SanitizeTargetMissions_KeepsValidValues()
		{
			Assert.Equal(new[] { 0, 0, 7, 5, 9 }, ExpeditionSelection.SanitizeTargetMissions(new[] { 0, 0, 7, 0, 9 }));
		}

		[Fact]
		public void SanitizeTargetMissions_ReplacesNonPositiveValuesWithDefaults()
		{
			Assert.Equal(new[] { 0, 0, 2, 5, 38 }, ExpeditionSelection.SanitizeTargetMissions(new[] { 0, 0, 0, -1, 0 }));
		}

		[Theory]
		[InlineData(0, 2)]
		[InlineData(1, 2)]
		[InlineData(2, 2)]
		[InlineData(3, 3)]
		[InlineData(4, 4)]
		[InlineData(5, 2)]
		[InlineData(-1, 2)]
		public void SanitizeFleetId_FallsBackToSecondFleet(int saved, int expected)
		{
			Assert.Equal(expected, ExpeditionSelection.SanitizeFleetId(saved));
		}

		[Theory]
		[InlineData(0, 2, 0)]
		[InlineData(1, 2, 1)]
		[InlineData(2, 2, 0)]
		[InlineData(-1, 2, 0)]
		[InlineData(0, 0, 0)]
		public void SanitizeTabIndex_FallsBackToFirstTab(int saved, int tabCount, int expected)
		{
			Assert.Equal(expected, ExpeditionSelection.SanitizeTabIndex(saved, tabCount));
		}

		[Theory]
		[InlineData(0, 0)]     // 0% は「警告しない」として有効
		[InlineData(80, 80)]
		[InlineData(100, 100)]
		[InlineData(-1, 100)]  // 範囲外は既定値
		[InlineData(101, 100)]
		[InlineData(int.MinValue, 100)]
		[InlineData(int.MaxValue, 100)]
		public void SanitizeGreatSuccessThresholds_ClampsEachFleet(int saved, int expected)
		{
			var sanitized = ExpeditionSelection.SanitizeGreatSuccessThresholds(new[] { 0, 0, saved });

			Assert.Equal(expected, sanitized[2]);
			Assert.Equal(ExpeditionSelection.DefaultGreatSuccessRateThreshold, sanitized[3]);
			Assert.Equal(ExpeditionSelection.MaxFleetId + 1, sanitized.Length);
		}

		[Fact]
		public void SanitizeGreatSuccessThresholds_UsesDefaultsWhenSavedIsNullOrShort()
		{
			Assert.Equal(new[] { 100, 100, 100, 100, 100 }, ExpeditionSelection.SanitizeGreatSuccessThresholds(null));
			Assert.Equal(new[] { 100, 100, 100, 100, 100 }, ExpeditionSelection.SanitizeGreatSuccessThresholds(new int[0]));
			Assert.Equal(new[] { 100, 100, 70, 100, 100 }, ExpeditionSelection.SanitizeGreatSuccessThresholds(new[] { 0, 0, 70 }));
		}

		[Fact]
		public void SanitizeGreatSuccessAlerts_KeepsOnlyFleetEntries()
		{
			Assert.Equal(new[] { false, false, false, false, false }, ExpeditionSelection.SanitizeGreatSuccessAlerts(null));
			Assert.Equal(new[] { false, false, true, false, true }, ExpeditionSelection.SanitizeGreatSuccessAlerts(new[] { true, true, true, false, true }));
			Assert.Equal(new[] { false, false, true, false, false }, ExpeditionSelection.SanitizeGreatSuccessAlerts(new[] { true, true, true }));
		}

		[Theory]
		[InlineData(new bool[] { true, true, false }, 2, false)]   // index 0/1 (使わない) は無視され、index 2 は false
		[InlineData(new bool[] { false, false, true, false, false }, 2, true)]
		[InlineData(new bool[] { false, false, true, false, false }, 3, false)]
		[InlineData(null, 2, false)]
		public void IsGreatSuccessAlertEnabled_ReadsEachFleetIndependently(bool[] saved, int fleetId, bool expected)
		{
			Assert.Equal(expected, ExpeditionSelection.IsGreatSuccessAlertEnabled(saved, fleetId));
		}

		[Theory]
		[InlineData(new int[] { 0, 0, 90, 100, 100 }, 2, 90)]
		[InlineData(new int[] { 0, 0, 90, 100, 100 }, 3, 100)]
		[InlineData(new int[] { 0, 0, 90, 100, 100 }, 4, 100)]
		[InlineData(new int[] { 0, 0, 150, 100, 100 }, 2, 100)]   // 範囲外は既定へ
		[InlineData(null, 3, 100)]
		public void GetGreatSuccessThreshold_ReadsEachFleetIndependently(int[] saved, int fleetId, int expected)
		{
			Assert.Equal(expected, ExpeditionSelection.GetGreatSuccessThreshold(saved, fleetId));
		}

		[Theory]
		// チェックが OFF の艦隊では、閾値を下回っていても警告しない
		[InlineData(null, null, 2, 0.96, false)]
		[InlineData(new bool[] { false, false, true, false, false }, null, 3, 0.10, false)]
		// ON の艦隊では、既定の閾値 100% を下回れば警告する (0.96 = 5/6 隻キラ)
		[InlineData(new bool[] { false, false, true, false, false }, null, 2, 0.96, true)]
		[InlineData(new bool[] { false, false, true, false, false }, null, 2, 1.0, false)]
		// 閾値を下げれば、それ以上なら警告しない
		[InlineData(new bool[] { false, false, true, false, false }, new int[] { 0, 0, 75, 0, 0 }, 2, 0.80, false)]
		[InlineData(new bool[] { false, false, true, false, false }, new int[] { 0, 0, 75, 0, 0 }, 2, 0.70, true)]
		// 艦隊ごとに独立していること (第4艦隊は OFF のまま)
		[InlineData(new bool[] { false, false, true, false, false }, new int[] { 0, 0, 75, 0, 0 }, 4, 0.10, false)]
		public void ShouldWarnGreatSuccess_UsesEachFleetSettingIndependently(bool[] alerts, int[] thresholds, int fleetId, double rate, bool expected)
		{
			Assert.Equal(expected, ExpeditionSelection.ShouldWarnGreatSuccess(alerts, thresholds, fleetId, rate));
		}
	}
}
