using ElectronicObserver.Data;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the escort-fleet (護衛隊) composition rules of the expedition check.
	/// The base requirement is 軽巡1+(駆逐/海防)n, but the game also accepts the
	/// special combinations 護母1+(駆逐2 or 海防2) / 駆逐1+海防3 / 軽巡or練巡1+海防2,
	/// so the check has to report which combination matched.
	/// </summary>
	public class MissionClearConditionTests
	{
		// 遠征4/5/9/42: 軽巡1+(駆逐+海防)2
		[Theory]
		[InlineData(0, 0, 1, 3, 0, "駆逐1+海防3")]                 // 攻略wiki にも載っている特例
		[InlineData(0, 0, 2, 3, 0, "駆逐1+海防3")]                 // 駆逐2+海防3 でも成功する
		[InlineData(1, 0, 2, 0, 0, "軽巡1+(駆逐+海防)2")]
		[InlineData(1, 0, 0, 2, 0, "軽巡1+(駆逐+海防)2")]
		[InlineData(1, 0, 1, 1, 0, "軽巡1+(駆逐+海防)2")]
		[InlineData(0, 1, 0, 2, 0, "練巡1+海防2")]
		[InlineData(0, 0, 2, 0, 1, "護母1+(駆逐2 or 海防2)")]
		[InlineData(0, 0, 0, 2, 1, "護母1+(駆逐2 or 海防2)")]
		public void EvaluateEscortFleet_MatchesTheDocumentedCombinations(
			int lightCruiser,
			int trainingCruiser,
			int destroyer,
			int escort,
			int escortAircraftCarrier,
			string expectedPattern)
		{
			var check = MissionClearCondition.EvaluateEscortFleet(true, 2, lightCruiser, trainingCruiser, destroyer, escort, escortAircraftCarrier);

			Assert.True(check.IsSatisfied);
			Assert.Equal(expectedPattern, check.MatchedPatternText);
		}

		[Theory]
		[InlineData(0, 0, 4, 0, 0)]     // 駆逐のみでは失敗する
		[InlineData(0, 0, 0, 4, 0)]     // 海防のみでも失敗する
		[InlineData(0, 1, 0, 1, 0)]     // 練巡1+海防1 では足りない
		[InlineData(0, 0, 1, 2, 0)]     // 軽巡なしの 駆逐1+海防2 では足りない
		[InlineData(0, 0, 2, 2, 0)]     // 軽巡なしの 駆逐2+海防2 では足りない
		[InlineData(0, 0, 1, 0, 1)]     // 護母1+駆逐1 では足りない
		public void EvaluateEscortFleet_RejectsCombinationsThatTheGameRejects(
			int lightCruiser,
			int trainingCruiser,
			int destroyer,
			int escort,
			int escortAircraftCarrier)
		{
			var check = MissionClearCondition.EvaluateEscortFleet(true, 2, lightCruiser, trainingCruiser, destroyer, escort, escortAircraftCarrier);

			Assert.False(check.IsSatisfied);
			Assert.Null(check.MatchedPatternText);
		}

		// 遠征42/A4: 軽巡1+駆逐2（海防は基本条件に含まれない）
		[Theory]
		[InlineData(1, 0, 2, 0, 0, "軽巡1+駆逐2")]
		[InlineData(0, 0, 1, 3, 0, "駆逐1+海防3")]
		[InlineData(1, 0, 0, 2, 0, "軽巡1+海防2")]
		[InlineData(0, 1, 0, 2, 0, "練巡1+海防2")]
		[InlineData(0, 0, 2, 0, 1, "護母1+(駆逐2 or 海防2)")]
		public void EvaluateEscortFleet_DestroyerOnlyBaseRequirement(
			int lightCruiser,
			int trainingCruiser,
			int destroyer,
			int escort,
			int escortAircraftCarrier,
			string expectedPattern)
		{
			var check = MissionClearCondition.EvaluateEscortFleet(false, 2, lightCruiser, trainingCruiser, destroyer, escort, escortAircraftCarrier);

			Assert.True(check.IsSatisfied);
			Assert.Equal(expectedPattern, check.MatchedPatternText);
		}

		[Fact]
		public void EvaluateEscortFleet_DestroyerOnlyBaseRequirementDoesNotCountEscorts()
		{
			// 軽巡1+駆逐1+海防1 は 駆逐2 に届かない
			var check = MissionClearCondition.EvaluateEscortFleet(false, 2, 1, 0, 1, 1, 0);

			Assert.False(check.IsSatisfied);
		}

		// A3: 軽巡1+(駆逐+海防)3 / A5・A6: 軽巡1+駆逐3 / 43: 軽巡1+駆逐4
		[Theory]
		[InlineData(true, 3, 1, 0, 3, 0, 0, "軽巡1+(駆逐+海防)3")]
		[InlineData(true, 3, 1, 0, 0, 3, 0, "軽巡1+(駆逐+海防)3")]
		[InlineData(true, 3, 0, 0, 1, 3, 0, "駆逐1+海防3")]
		[InlineData(false, 3, 1, 0, 3, 0, 0, "軽巡1+駆逐3")]
		[InlineData(false, 3, 0, 0, 1, 3, 0, "駆逐1+海防3")]
		[InlineData(false, 4, 1, 0, 4, 0, 0, "軽巡1+駆逐4")]
		[InlineData(false, 4, 0, 0, 2, 3, 0, "駆逐1+海防3")]
		public void EvaluateEscortFleet_MatchesTheHigherRequirements(
			bool countsEscortInBaseRequirement,
			int leastSmallShipCount,
			int lightCruiser,
			int trainingCruiser,
			int destroyer,
			int escort,
			int escortAircraftCarrier,
			string expectedPattern)
		{
			var check = MissionClearCondition.EvaluateEscortFleet(countsEscortInBaseRequirement, leastSmallShipCount, lightCruiser, trainingCruiser, destroyer, escort, escortAircraftCarrier);

			Assert.True(check.IsSatisfied);
			Assert.Equal(expectedPattern, check.MatchedPatternText);
		}

		[Theory]
		[InlineData(true, 3, 1, 0, 2, 0, 0)]     // 軽巡1+駆逐2 では A3 に足りない
		[InlineData(false, 3, 1, 0, 2, 0, 0)]    // 軽巡1+駆逐2 では A5/A6 に足りない
		[InlineData(false, 4, 1, 0, 3, 0, 0)]    // 軽巡1+駆逐3 では 43 に足りない
		public void EvaluateEscortFleet_RejectsFleetsBelowTheRequirement(
			bool countsEscortInBaseRequirement,
			int leastSmallShipCount,
			int lightCruiser,
			int trainingCruiser,
			int destroyer,
			int escort,
			int escortAircraftCarrier)
		{
			var check = MissionClearCondition.EvaluateEscortFleet(countsEscortInBaseRequirement, leastSmallShipCount, lightCruiser, trainingCruiser, destroyer, escort, escortAircraftCarrier);

			Assert.False(check.IsSatisfied);
		}

		[Fact]
		public void EvaluateEscortFleet_ReportsTheMatchedCombination()
		{
			// 軽巡がいなくても 駆逐1+海防3 なら成功する（画面上で理由が分かるようにする）
			var check = MissionClearCondition.EvaluateEscortFleet(true, 2, 0, 0, 1, 3, 0);

			Assert.True(check.IsSatisfied);
			Assert.Equal("軽巡1+(駆逐+海防)2等", check.RequirementText);
			Assert.Equal("護衛隊(軽巡1(駆逐+海防)2他)", check.FailureReasonText);
			Assert.Equal("達成（駆逐1+海防3）", check.CurrentValueText);
		}

		[Fact]
		public void EvaluateEscortFleet_ReportsTheActualCompositionWhenItFails()
		{
			var check = MissionClearCondition.EvaluateEscortFleet(false, 2, 0, 0, 4, 0, 0);

			Assert.False(check.IsSatisfied);
			Assert.Equal("軽巡1+駆逐2等", check.RequirementText);
			Assert.Equal("護衛隊(軽巡1駆逐2他)", check.FailureReasonText);
			Assert.Equal("不適合（軽巡0・駆逐4・海防0・護母0・練巡0）", check.CurrentValueText);
		}
	}
}
