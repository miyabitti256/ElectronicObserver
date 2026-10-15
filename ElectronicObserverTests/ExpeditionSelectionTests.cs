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
	}
}
