#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ElectronicObserver.Data;
using ElectronicObserver.Database;
using ElectronicObserver.Observer;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the escort-fleet (護衛隊) composition rules of the expedition check.
	/// The base requirement is 軽巡1+(駆逐/海防)n, but the game also accepts the
	/// special combinations 護母1+(駆逐2 or 海防2) / 駆逐1+海防3 / 軽巡or練巡1+海防2,
	/// so the check has to report which combination matched.
	/// </summary>
	[Collection("KCDatabase")]
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

		#region Helper & Setup for MissionClearCondition.Check Unit Tests

		private static bool _isRealDataLoaded = false;
		private static readonly object _dbInitLock = new object();

		private static void EnsureRealDataLoaded()
		{
			if (_isRealDataLoaded && KCDatabase.Instance.Ships.ContainsKey(23865) && KCDatabase.Instance.Fleet[3]?.ExpeditionDestination == 101) return;

			lock (_dbInitLock)
			{
				if (_isRealDataLoaded && KCDatabase.Instance.Ships.ContainsKey(23865) && KCDatabase.Instance.Fleet[3]?.ExpeditionDestination == 101) return;

				string[] candidatePaths = new[]
				{
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "mission_replay_pack.json.br"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Fixtures", "mission_replay_pack.json.br"),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fixtures", "mission_replay_pack.json.br"),
					@"d:\apps\EO\ElectronicObserver\ElectronicObserverTests\Fixtures\mission_replay_pack.json.br"
				};

				string? path = candidatePaths.FirstOrDefault(File.Exists);
				if (path == null)
				{
					throw new FileNotFoundException("mission_replay_pack.json.br not found.");
				}

				byte[] compressed = File.ReadAllBytes(path);
				string json = ElectronicObserverDbContext.DecompressContent(compressed);
				var items = JsonSerializer.Deserialize<List<MissionReplayIntegrationTests.ReplayPacketItem>>(json, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				var resps = items!.Where(i => i.FileType == 2).ToDictionary(i => i.ApiName, i => i.Content);

				if (resps.ContainsKey("api_start2/getData"))
				{
					APIObserver.Instance.LoadResponse("/kcsapi/api_start2/getData", resps["api_start2/getData"]);
				}
				if (resps.ContainsKey("api_get_member/slot_item"))
				{
					APIObserver.Instance.LoadResponse("/kcsapi/api_get_member/slot_item", resps["api_get_member/slot_item"]);
				}
				if (resps.ContainsKey("api_port/port"))
				{
					APIObserver.Instance.LoadResponse("/kcsapi/api_port/port", resps["api_port/port"]);
				}

				_isRealDataLoaded = true;
			}
		}

		private static FleetData RegisterCustomFleet(int fleetId, params int[] shipIds)
		{
			EnsureRealDataLoaded();

			var padded = shipIds.Concat(Enumerable.Repeat(-1, 6 - shipIds.Length)).Take(6);
			string json = $"svdata={{\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":[{{\"api_member_id\":1,\"api_id\":{fleetId},\"api_name\":\"テスト艦隊\",\"api_ship\":[{string.Join(",", padded)}],\"api_mission\":[0,0,0,0]}}]}}";
			APIObserver.Instance.LoadResponse("/kcsapi/api_get_member/deck", json);
			return KCDatabase.Instance.Fleet[fleetId];
		}

		#endregion

		#region MissionClearCondition.Check Unit & Boundary Tests

		[Theory]
		[InlineData(1, new[] { "旗艦Lv1", "艦船数2" })]
		[InlineData(2, new[] { "旗艦Lv2", "艦船数4" })]
		[InlineData(6, new[] { "旗艦Lv4", "艦船数4" })]
		[InlineData(7, new[] { "旗艦Lv5", "艦船数6" })]
		[InlineData(37, new[] { "旗艦Lv50", "Lv合計200", "軽巡洋艦1", "駆逐艦5", "簡易輸送部材:装備艦船数3", "簡易輸送部材:装備数4" })]
		[InlineData(38, new[] { "旗艦Lv65", "Lv合計240", "艦船数6", "駆逐艦5", "簡易輸送部材:装備艦船数4", "簡易輸送部材:装備数8" })]
		[InlineData(101, new[] { "旗艦Lv20", "(駆逐+海防)4", "火力50", "対空70", "対潜180" })]
		[InlineData(9999, new[] { "未対応(ID:9999)" })]
		public void Check_WhenFleetIsNull_DoesNotThrowAndReturnsExpectedRequirements(int missionId, string[] expectedReasons)
		{
			// What: Verify Check(missionID, null) succeeds without throwing any exception
			//       and populates FailureReason with all required expedition conditions.
			EnsureRealDataLoaded();

			var result = MissionClearCondition.Check(missionId, null);

			if (missionId != 9999)
			{
				Assert.False(result.IsSuceeded);
			}
			Assert.NotEmpty(result.FailureReason);

			foreach (string expected in expectedReasons)
			{
				Assert.Contains(result.FailureReason, reason => reason.Contains(expected));
			}
		}

		[Fact]
		public void Check_Expedition6_DefenseExercise_Boundaries()
		{
			// What: Validate Mission 6 (防空射撃演習) boundary cases: Flagship Lv >= 4, ShipCount >= 4.
			EnsureRealDataLoaded();

			// 吹雪改二(ID:1, Lv97), 長月改(ID:3, Lv25), 電改(ID:4, Lv76), 五月雨改(ID:5, Lv26)
			// 1. Pass case: Flagship Lv97 (>= 4), 4 ships
			var passFleet = RegisterCustomFleet(1, 1, 3, 4, 5);
			var passResult = MissionClearCondition.Check(6, passFleet);
			Assert.True(passResult.IsSuceeded, $"Expedition 6 should succeed: {string.Join(", ", passResult.FailureReason)}");

			// 2. Fail case: Less than 4 ships (3 ships)
			var failCountFleet = RegisterCustomFleet(1, 1, 3, 4);
			var failCountResult = MissionClearCondition.Check(6, failCountFleet);
			Assert.False(failCountResult.IsSuceeded);
			Assert.Contains(failCountResult.FailureReason, r => r.Contains("艦船数3/4"));
		}

		[Fact]
		public void Check_Expedition7_NavalReviewPractice_Boundaries()
		{
			// What: Validate Mission 7 (観艦式予行) boundary cases: Flagship Lv >= 5, ShipCount >= 6.
			EnsureRealDataLoaded();

			// Ships: 1(Lv97), 3(Lv25), 4(Lv76), 5(Lv26), 6(Lv80), 8(Lv74)
			// 1. Pass case: Flagship Lv97, 6 ships
			var passFleet = RegisterCustomFleet(1, 1, 3, 4, 5, 6, 8);
			var passResult = MissionClearCondition.Check(7, passFleet);
			Assert.True(passResult.IsSuceeded, $"Expedition 7 should succeed: {string.Join(", ", passResult.FailureReason)}");

			// 2. Fail case: 5 ships (short of 6)
			var failCountFleet = RegisterCustomFleet(1, 1, 3, 4, 5, 6);
			var failCountResult = MissionClearCondition.Check(7, failCountFleet);
			Assert.False(failCountResult.IsSuceeded);
			Assert.Contains(failCountResult.FailureReason, r => r.Contains("艦船数5/6"));
		}

		[Fact]
		public void Check_Expedition37_TokyoExpress_Boundaries()
		{
			// What: Validate Mission 37 (東京急行) boundaries:
			//       Flagship Lv >= 50, LevelSum >= 200, 1 CL + 5 DD, 3 drum carriers, 4 total drums.
			EnsureRealDataLoaded();

			// CL: 天龍改二(ID:30, Lv86, drums:2)
			// DD with drums: ID:13526 (磯風改二 Lv89, drums:1), ID:34319 (秋霜改 Lv13, drums:2)
			// DD without drums: ID:1 (吹雪改二 Lv97, drums:0), ID:4 (電改 Lv76, drums:0), ID:6 (白雪改二 Lv80, drums:0), ID:8 (叢雲改 Lv74, drums:0)

			// 1. Pass case: 1 CL (drums:2) + 5 DD (two have drums 1+2=3, three have 0 drums).
			//    Total drums: 2 + 1 + 2 = 5 (>= 4), drum carriers: 3 (>= 3), LvSum > 400 (>= 200).
			var passFleet = RegisterCustomFleet(1, 30, 13526, 34319, 1, 4, 6);
			var passResult = MissionClearCondition.Check(37, passFleet);
			Assert.True(passResult.IsSuceeded, $"Expedition 37 should succeed: {string.Join(", ", passResult.FailureReason)}");

			// 2. Fail case: Drum carriers < 3 (only 2 carriers 30(2) + 34319(2) = 4 drums >= 4, but 2 carriers)
			var failCarrierFleet = RegisterCustomFleet(1, 30, 34319, 1, 4, 6);
			var failCarrierResult = MissionClearCondition.Check(37, failCarrierFleet);
			Assert.False(failCarrierResult.IsSuceeded);
			Assert.Contains(failCarrierResult.FailureReason, r => r.Contains("装備艦船数2/3"));

			// 3. Fail case: Composition invalid (No CL, 6 DDs)
			var failCompFleet = RegisterCustomFleet(1, 13526, 34319, 1, 4, 6, 8);
			var failCompResult = MissionClearCondition.Check(37, failCompFleet);
			Assert.False(failCompResult.IsSuceeded);
			Assert.Contains(failCompResult.FailureReason, r => r.Contains("軽巡0/1") || r.Contains("軽巡"));
		}

		[Fact]
		public void Check_Expedition38_TokyoExpress2_Boundaries()
		{
			// What: Validate Mission 38 (東京急行(弐)) boundaries:
			//       Flagship Lv >= 65, LevelSum >= 240, ShipCount >= 6, 5 DDs, 4 drum carriers, 8 total drums.
			EnsureRealDataLoaded();

			// DD drum carriers:
			// 6332(drums:3), 34319(drums:2), 35096(drums:2), 37655(drums:2) -> 4 carriers, 3+2+2+2 = 9 drums
			// Other DDs: 1(吹雪改二 Lv97), 6(白雪改二 Lv80), 3(長月改 Lv25)

			// 1. Pass case: 6 DDs, Flagship ID 1 (Lv97 >= 65), LvSum > 280, 4 drum carriers with 9 drums
			var passFleet = RegisterCustomFleet(1, 1, 6332, 34319, 35096, 37655, 6);
			var passResult = MissionClearCondition.Check(38, passFleet);
			Assert.True(passResult.IsSuceeded, $"Expedition 38 should succeed: {string.Join(", ", passResult.FailureReason)}");

			// 2. Fail case: Flagship level < 65 (Flagship 長月改 Lv25)
			var failFlagshipFleet = RegisterCustomFleet(1, 3, 6332, 34319, 35096, 37655, 6);
			var failFlagshipResult = MissionClearCondition.Check(38, failFlagshipFleet);
			Assert.False(failFlagshipResult.IsSuceeded);
			Assert.Contains(failFlagshipResult.FailureReason, r => r.Contains("旗艦Lv25/65"));

			// 3. Fail case: Drum carriers < 4 (only 3 carriers: 6332(3), 34319(2), 35096(2) = 7 drums, 3 carriers)
			var failCarrierFleet = RegisterCustomFleet(1, 1, 6332, 34319, 35096, 6);
			var failCarrierResult = MissionClearCondition.Check(38, failCarrierFleet);
			Assert.False(failCarrierResult.IsSuceeded);
			Assert.Contains(failCarrierResult.FailureReason, r => r.Contains("装備艦船数3/4"));
		}

		[Fact]
		public void Check_Expedition101_StraitPatrol_StatRequirements()
		{
			// What: Validate Mission 101 (海峡警備行動) stat requirements:
			//       Flagship Lv >= 20, (DD+DE) >= 4, Firepower >= 50, AA >= 70, ASW >= 180.
			EnsureRealDataLoaded();

			// Real Fleet 3 is on mission 101, so it must pass
			var fleet3 = KCDatabase.Instance.Fleet[3];
			Assert.NotNull(fleet3);
			var resultF3 = MissionClearCondition.Check(101, fleet3);
			Assert.True(resultF3.IsSuceeded, $"Fleet 3 should satisfy Mission 101: {string.Join(", ", resultF3.FailureReason)}");

			// Fail case: 3 ships only (less than 4)
			var failCountFleet = RegisterCustomFleet(1, fleet3[0], fleet3[1], fleet3[2]);
			var failCountResult = MissionClearCondition.Check(101, failCountFleet);
			Assert.False(failCountResult.IsSuceeded);
			Assert.Contains(failCountResult.FailureReason, r => r.Contains("(駆逐+海防)3/4"));
		}

		#endregion
	}
}
