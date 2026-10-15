using ElectronicObserver.Data;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Tests for the pure (non-UI, non-database) part of the expedition helper.
	/// These guard the mission-id -&gt; great-success-type mapping tables.
	/// </summary>
	public class ExpeditionHelperTests
	{
		[Theory]
		[InlineData(21, ExpeditionGreatSuccessType.Drum)]
		[InlineData(24, ExpeditionGreatSuccessType.Drum)]
		[InlineData(37, ExpeditionGreatSuccessType.Drum)]
		[InlineData(38, ExpeditionGreatSuccessType.Drum)]
		[InlineData(40, ExpeditionGreatSuccessType.Drum)]
		[InlineData(44, ExpeditionGreatSuccessType.Drum)]
		[InlineData(142, ExpeditionGreatSuccessType.Drum)]
		[InlineData(32, ExpeditionGreatSuccessType.Level)]
		[InlineData(41, ExpeditionGreatSuccessType.Level)]
		[InlineData(43, ExpeditionGreatSuccessType.Level)]
		[InlineData(45, ExpeditionGreatSuccessType.Level)]
		[InlineData(46, ExpeditionGreatSuccessType.Level)]
		[InlineData(101, ExpeditionGreatSuccessType.Level)]
		[InlineData(106, ExpeditionGreatSuccessType.Level)]
		[InlineData(112, ExpeditionGreatSuccessType.Level)]
		[InlineData(115, ExpeditionGreatSuccessType.Level)]
		[InlineData(131, ExpeditionGreatSuccessType.Level)]
		[InlineData(133, ExpeditionGreatSuccessType.Level)]
		[InlineData(141, ExpeditionGreatSuccessType.Level)]
		[InlineData(1, ExpeditionGreatSuccessType.Regular)]
		[InlineData(2, ExpeditionGreatSuccessType.Regular)]
		[InlineData(100, ExpeditionGreatSuccessType.Regular)]
		[InlineData(140, ExpeditionGreatSuccessType.Regular)]
		public void GetGreatSuccessType_ReturnsExpectedType(int missionId, ExpeditionGreatSuccessType expected)
		{
			Assert.Equal(expected, ExpeditionHelper.GetGreatSuccessType(missionId));
		}

		[Theory]
		[InlineData(21, 4)]
		[InlineData(24, 2)]
		[InlineData(37, 5)]
		[InlineData(38, 10)]
		[InlineData(40, 4)]
		[InlineData(44, 8)]
		[InlineData(142, 6)]
		[InlineData(2, 0)]
		[InlineData(32, 0)]
		public void GetGreatSuccessDrumRequirement_ReturnsExpectedCount(int missionId, int expected)
		{
			Assert.Equal(expected, ExpeditionHelper.GetGreatSuccessDrumRequirement(missionId));
		}

		[Fact]
		public void CalculateGreatSuccessRate_ReturnsZeroForNullFleet()
		{
			Assert.Equal(0.0, ExpeditionHelper.CalculateGreatSuccessRate(null, 2));
		}

		// --- 大成功しない可能性がある場合の警告 (#34) ---

		[Theory]
		[InlineData(1.0, 100, false)]   // 大成功確定 (100%) は閾値 100% で警告しない
		[InlineData(0.96, 100, true)]   // 5/6 隻キラ (96%) は警告する
		[InlineData(0.621, 100, true)]
		[InlineData(0.0, 100, true)]
		[InlineData(0.8, 80, false)]    // 閾値ちょうどは警告しない
		[InlineData(0.79, 80, true)]
		[InlineData(0.5, 0, false)]     // 閾値 0 は「警告しない」
		[InlineData(0.0, 0, false)]
		public void IsGreatSuccessRateBelowThreshold_ReturnsExpectedResult(double rate, int thresholdPercent, bool expected)
		{
			Assert.Equal(expected, ExpeditionHelper.IsGreatSuccessRateBelowThreshold(rate, thresholdPercent));
		}

		[Fact]
		public void GetGreatSuccessWarningText_IncludesRateSparklesAndThreshold()
		{
			string text = ExpeditionHelper.GetGreatSuccessWarningText(0.621, 100, 4, 6, 0, 0);

			Assert.Contains("大成功しない可能性があります", text);
			Assert.Contains("62.1%", text);
			Assert.Contains("キラ 4/6", text);
			Assert.Contains("閾値 100%", text);
			Assert.DoesNotContain("缶", text);
		}

		[Fact]
		public void GetGreatSuccessWarningText_IncludesDrumsForDrumExpeditions()
		{
			// 遠征38: ドラム缶 10 個が必要
			string text = ExpeditionHelper.GetGreatSuccessWarningText(0.5, 100, 6, 6, 3, ExpeditionHelper.GetGreatSuccessDrumRequirement(38));

			Assert.Contains("缶 3/10", text);
		}
	}
}
