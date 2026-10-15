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
	}
}
