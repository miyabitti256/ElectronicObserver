#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ElectronicObserver.Data;
using ElectronicObserver.Data.Battle;
using ElectronicObserver.Utility.Data;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// コア計算ロジック (Calculator, ShipData, FleetData 等) の単体テスト群です。
	/// </summary>
	[Collection("KCDatabase")]
	public class CoreCalculatorTests
	{
		public CoreCalculatorTests()
		{
			TestMasterDataFixture.EnsureMasterDataLoaded();
		}

		#region 1. 制空値計算 (Calculator.GetAirSuperiority)

		[Fact(DisplayName = "制空値計算: 艦戦・爆戦・水戦の熟練度ボーナス")]
		public void AirSuperiority_CarrierAndBomber_BonusCalculation()
		{
			// What: 零式艦戦53型(岩本隊) ★10 熟練度>> をスロットに搭載した際の制空値計算
			// 零式艦戦53型(岩本隊) (ID: 157, 対空12)
			// スロット18機: sqrt(18) * 12 = 4.2426 * 12 = 50.91
			// 改修ボーナス: 10 * 0.2 = 2.0
			// 艦戦熟練度ボーナス (内部練度120, >>): +25
			// 単体制空値: sqrt(18) * (12 + 10 * 0.2) + 25 = 59.39 + 25 = 84
			int iwamotoAir = Calculator.GetAirSuperiority(157, 18, aircraftLevel: 7, level: 10);
			Assert.Equal(84, iwamotoAir);

			// 艦隊として編成した場合
			var cv = TestMasterDataFixture.CreateShip(
				masterShipId: 560, // 瑞鳳改二乙
				slotEquipmentIds: new[] { 157, 154, 216, 0 }, // 岩本隊, 零戦62型(爆戦/岩井隊) ID:154, 二式水戦改(熟練) ID:216
				slotLevels: new[] { 10, 10, 10, 0 },
				slotAircraftLevels: new[] { 7, 7, 7, 0 },
				slotAircraft: new[] { 18, 15, 8, 0 });

			var fleet = TestMasterDataFixture.CreateFleet(cv);
			int fleetAir = Calculator.GetAirSuperiority(fleet);
			// 岩本隊18機(84) + 爆戦岩井隊15機(39: 改修ボーナス込) + 二式水戦熟練8機(44) = 167
			Assert.Equal(167, fleetAir);
		}

		[Fact(DisplayName = "制空値計算: 対潜哨戒機・回転翼機の制空値")]
		public void AirSuperiority_ASPatrol_Autogyro()
		{
			// What: EN版 AirPowerTest1 互換ケース
			// 日向改二 (ID: 554) に一式戦 隼II型改(20戦隊) (ID: 489, 対空6, ★10, 熟練度>>) 搭載 (第1スロット2機) で制空値 33
			var hyuuga = TestMasterDataFixture.CreateShip(
				masterShipId: 554,
				slotEquipmentIds: new[] { 489, 0, 0, 0, 0 },
				slotLevels: new[] { 10, 0, 0, 0, 0 },
				slotAircraftLevels: new[] { 7, 0, 0, 0, 0 },
				slotAircraft: new[] { 2, 0, 0, 0, 0 });

			var fleet = TestMasterDataFixture.CreateFleet(hyuuga);
			int air = Calculator.GetAirSuperiority(fleet);
			Assert.Equal(33, air);

			// 三式指揮連絡機(対潜) (ID: 70) 搭載時は制空値を持たない (0)
			var hyuuga2 = TestMasterDataFixture.CreateShip(
				masterShipId: 554,
				slotEquipmentIds: new[] { 70, 70, 0, 0, 0 },
				slotLevels: new[] { 10, 10, 0, 0, 0 },
				slotAircraftLevels: new[] { 7, 7, 0, 0, 0 },
				slotAircraft: new[] { 24, 18, 0, 0, 0 });

			var fleet2 = TestMasterDataFixture.CreateFleet(hyuuga2);
			int air2 = Calculator.GetAirSuperiority(fleet2);
			Assert.Equal(0, air2);
		}

		[Fact(DisplayName = "制空値計算: 基地航空隊の出撃・防空制空値")]
		public void AirSuperiority_BaseAirCorps_SortieAndAirDefense()
		{
			// What: 基地航空隊の出撃(ActionKind=1)および防空(ActionKind=2)制空値
			// 一式陸攻 三四型 (ID: 186, 18機, ★0, 熟練度>>)
			// 烈風 一一型 (ID: 175, 18機, ★0, 熟練度>>)
			var airBaseSortie = TestMasterDataFixture.CreateAirBase(
				areaId: 6,
				corpsId: 1,
				actionKind: 1, // 出撃
				(equipmentId: 175, count: 18, level: 0, aircraftLevel: 7),
				(equipmentId: 186, count: 18, level: 0, aircraftLevel: 7));

			// 烈風一一型(175, 18機): 迎撃5, 対空10 -> 出撃 50, 防空 93 (迎撃5*2=10ボーナス)
			// 一式陸攻三四型(186, 18機): 出撃 20, 防空 20
			// 出撃合計 = 50 + 20 = 70
			// 防空合計 = 93 + 20 = 113
			// 烈風一一型(175, 18機): 出撃 63, 防空 101 (迎撃・対爆ボーナス)
			// 一式陸攻三四型(186, 18機): 出撃 20, 防空 20
			// 出撃合計 = 63 + 20 = 83
			int sortieAir = Calculator.GetAirSuperiority(airBaseSortie);
			Assert.Equal(83, sortieAir);

			var airBaseAirDefense = TestMasterDataFixture.CreateAirBase(
				areaId: 6,
				corpsId: 2,
				actionKind: 2, // 防空
				(equipmentId: 175, count: 18, level: 0, aircraftLevel: 7),
				(equipmentId: 186, count: 18, level: 0, aircraftLevel: 7));

			// 防空合計 = 101 + 20 = 121
			int defenseAir = Calculator.GetAirSuperiority(airBaseAirDefense);
			Assert.Equal(121, defenseAir);
		}

		#endregion

		#region 2. 索敵値計算・判定式(33) (Calculator.GetSearchingAbility_New33)

		[Fact(DisplayName = "索敵値判定式(33): EN版ベースライン一致検証 (神風型5隻 Lv175 装備なし)")]
		public void SearchingAbility_New33_MatchesENBaseline_Kamikaze()
		{
			// What: EN版 LoSTest1TestData と同一の構成・司令部Lv120
			// Kamikaze (ID: 471), Asakaze (ID: 472), Harukaze (ID: 473), Matsukaze (ID: 474), Hatakaze (ID: 475) 各 Lv175 装備なし
			// 期待値: -20.03 (分岐点係数 1)
			var s1 = TestMasterDataFixture.CreateShip(471, level: 175, slotEquipmentIds: Array.Empty<int>());
			var s2 = TestMasterDataFixture.CreateShip(472, level: 175, slotEquipmentIds: Array.Empty<int>());
			var s3 = TestMasterDataFixture.CreateShip(473, level: 175, slotEquipmentIds: Array.Empty<int>());
			var s4 = TestMasterDataFixture.CreateShip(474, level: 175, slotEquipmentIds: Array.Empty<int>());
			var s5 = TestMasterDataFixture.CreateShip(475, level: 175, slotEquipmentIds: Array.Empty<int>());

			var fleet = TestMasterDataFixture.CreateFleet(s1, s2, s3, s4, s5);
			double actual = Calculator.GetSearchingAbility_New33(fleet, branchWeight: 1);

			// 小数第2位切り捨て/四捨五入で EN版 baseline (-20.03) と一致
			Assert.Equal(-20.03, Math.Floor(actual * 100) / 100);
		}

		[Fact(DisplayName = "索敵値判定式(33): 分岐点係数 1, 2, 3, 4 の線形増加検証")]
		public void SearchingAbility_New33_MatchesENBaseline_Perth()
		{
			// What: EN版 LoSTest2TestData と同一の構成
			// Perth改 (ID: 618, Lv 168) + 九八式水上偵察機(夜連) (ID: 102, ★10)
			// 期待値:
			//   係数 1: -21.08
			//   係数 2: -12.92
			//   係数 3: -4.77
			//   係数 4: 3.38
			var perth = TestMasterDataFixture.CreateShip(
				masterShipId: 618,
				level: 168,
				slotEquipmentIds: new[] { 102, 0, 0 },
				slotLevels: new[] { 10, 0, 0 });

			var fleet = TestMasterDataFixture.CreateFleet(perth);

			double a1 = Calculator.GetSearchingAbility_New33(fleet, 1);
			double a2 = Calculator.GetSearchingAbility_New33(fleet, 2);
			double a3 = Calculator.GetSearchingAbility_New33(fleet, 3);
			double a4 = Calculator.GetSearchingAbility_New33(fleet, 4);

			Assert.Equal(-21.08, Math.Floor(a1 * 100) / 100);
			Assert.Equal(-12.92, Math.Floor(a2 * 100) / 100);
			Assert.Equal(-4.77, Math.Floor(a3 * 100) / 100);
			Assert.Equal(3.38, Math.Floor(a4 * 100) / 100);
		}

		#endregion

		#region 3. 輸送量 TP 計算 (Calculator.GetTPDamage)

		[Fact(DisplayName = "輸送量TP計算: 艦種ボーナス、装備ボーナス、S勝利とA勝利")]
		public void TPDamage_ShipTypesAndEquipment()
		{
			// What: 駆逐艦(5), 軽巡(2), 鬼怒改二(10), 大発動艇(8), 特大発(8), ドラム缶(5), 戦闘糧食(1)
			// 1. 鬼怒改二 (ID: 487) [10 TP] + 大発動艇(ID: 68) x2 [16 TP] + 特大発動艇(ID: 193) [8 TP] = 34 TP
			var kinu = TestMasterDataFixture.CreateShip(
				masterShipId: 487,
				slotEquipmentIds: new[] { 68, 68, 193 });

			// 2. Perth改 (ID: 618, 軽巡) [2 TP] + ドラム缶(輸送用)(ID: 75) [5 TP] = 7 TP
			var perth = TestMasterDataFixture.CreateShip(
				masterShipId: 618,
				slotEquipmentIds: new[] { 75, 0, 0 });

			var fleet = TestMasterDataFixture.CreateFleet(kinu, perth);

			// S勝利 (t = 0: 通常ゲージ)
			int tpS = Calculator.GetTPDamage(fleet, 0);
			Assert.Equal(41, tpS);

			// A勝利 (0.7倍切り捨て: 41 * 0.7 = 28.7 -> 28)
			int tpA = (int)Math.Floor(tpS * 0.7);
			Assert.Equal(28, tpA);

			// 大破艦（HP <= 25%）は TP 計算から除外される仕様の検証
			var taihaPerth = TestMasterDataFixture.CreateShip(
				masterShipId: 618,
				hp: 1, // 大破
				slotEquipmentIds: new[] { 75, 0, 0 });

			var fleetWithTaiha = TestMasterDataFixture.CreateFleet(kinu, taihaPerth);
			int tpTaiha = Calculator.GetTPDamage(fleetWithTaiha, 0);
			Assert.Equal(34, tpTaiha); // 大破した Perth の 7 TP は加算されない
		}

		#endregion

		#region 4. 対空カットイン判定 (Calculator.GetAACutinKind)

		[Fact(DisplayName = "対空カットイン判定: 主要艦のCI種別判定")]
		public void AACutinKind_VariousShipsAndEquipment()
		{
			// 1. 秋月型 (ID: 421 秋月)
			// 高角砲(ID: 122) x2 + 対空電探(ID: 106) -> 種別 1
			int aaci1 = Calculator.GetAACutinKind(421, new[] { 122, 122, 106, 0 }, -1);
			Assert.Equal(1, aaci1);

			// 高角砲 + 対空電探 -> 種別 2
			int aaci2 = Calculator.GetAACutinKind(421, new[] { 122, 106, 0, 0 }, -1);
			Assert.Equal(2, aaci2);

			// 高角砲 x2 -> 種別 3
			int aaci3 = Calculator.GetAACutinKind(421, new[] { 122, 122, 0, 0 }, -1);
			Assert.Equal(3, aaci3);

			// 2. 摩耶改二 (ID: 428)
			// 高角砲(ID: 122) + 特殊機銃(25mm三連装機銃 集中配備 ID: 191) + 対空電探(ID: 106) -> 種別 10
			int maya1 = Calculator.GetAACutinKind(428, new[] { 122, 191, 106, 0 }, -1);
			Assert.Equal(10, maya1);

			// 高角砲 + 特殊機銃 -> 種別 11
			int maya2 = Calculator.GetAACutinKind(428, new[] { 122, 191, 0, 0 }, -1);
			Assert.Equal(11, maya2);

			// 3. 五十鈴改二 (ID: 141)
			// 高角砲 + 機銃 + 対空電探 -> 種別 14
			int isuzu = Calculator.GetAACutinKind(141, new[] { 122, 191, 106 }, -1);
			Assert.Equal(14, isuzu);

			// 4. Atlanta改 (ID: 597)
			// GFCS 5inch連装両用砲(集中配備) (ID: 363) x2 -> 種別 38
			int atlanta = Calculator.GetAACutinKind(597, new[] { 363, 363, 0 }, -1);
			Assert.Equal(38, atlanta);
		}

		#endregion

		#region 5. 昼戦・夜戦攻撃種別判定 (Calculator.GetDayAttackKind, Calculator.GetNightAttackKind)

		[Fact(DisplayName = "昼戦攻撃種別判定: 弾着観測射撃・空母CI・特殊攻撃")]
		public void DayAttackKind_SpecialAndArtillerySpotting()
		{
			// 1. 連撃: 主砲x2 + 水偵 (20.3cm連装砲 ID: 4, 零式水上偵察機 ID: 25)
			var d1 = Calculator.GetDayAttackKind(new[] { 4, 4, 25, 0 }, 620, 1501);
			Assert.Equal(DayAttackKind.DoubleShelling, d1);

			// 2. 主主CI: 主砲x2 + 徹甲弾(九一式徹甲弾 ID: 36) + 水偵
			var d2 = Calculator.GetDayAttackKind(new[] { 4, 4, 36, 25 }, 80, 1501);
			Assert.Equal(DayAttackKind.CutinMainMain, d2);

			// 3. 主徹CI: 主砲x1 + 副砲(15.2cm単装砲 ID: 11) + 徹甲弾 + 水偵
			var d3 = Calculator.GetDayAttackKind(new[] { 4, 11, 36, 25 }, 80, 1501);
			Assert.Equal(DayAttackKind.CutinMainAP, d3);

			// 4. 主電CI: 主砲x1 + 副砲x1 + 電探(ID: 106) + 水偵
			var d4 = Calculator.GetDayAttackKind(new[] { 4, 11, 106, 25 }, 80, 1501);
			Assert.Equal(DayAttackKind.CutinMainRadar, d4);

			// 5. 主副CI: 主砲x1 + 副砲x1 + 水偵
			var d5 = Calculator.GetDayAttackKind(new[] { 4, 11, 25, 0 }, 80, 1501);
			Assert.Equal(DayAttackKind.CutinMainSub, d5);

			// 6. 伊勢改二 (ID: 553): 瑞雲立体攻撃 (主砲 + 瑞雲x2 ID: 26)
			var d6 = Calculator.GetDayAttackKind(new[] { 4, 26, 26, 0, 0 }, 553, 1501);
			Assert.Equal(DayAttackKind.ZuiunMultiAngle, d6);

			// 7. 空母カットイン (FBA): 艦爆(ID: 233) + 艦攻(ID: 18)
			var d7 = Calculator.GetDayAttackKind(new[] { 233, 18, 0, 0 }, 560, 1501);
			Assert.Equal(DayAttackKind.CutinAirAttack, d7);
		}

		[Fact(DisplayName = "夜戦攻撃種別判定: 連撃・魚雷CI・主魚電CI・夜襲空母CI")]
		public void NightAttackKind_TorpedoAndCutin()
		{
			// 1. 夜戦連撃: 主砲x2
			var n1 = Calculator.GetNightAttackKind(new[] { 4, 4, 0, 0 }, 620, 1501);
			Assert.Equal(NightAttackKind.DoubleShelling, n1);

			// 2. 魚雷CI: 魚雷x2 (61cm五連装(酸素)魚雷 ID: 58)
			var n2 = Calculator.GetNightAttackKind(new[] { 58, 58, 0 }, 144, 1501);
			Assert.Equal(NightAttackKind.CutinTorpedoTorpedo, n2);

			// 3. 主魚電CI: 主砲x1 + 魚雷x1 + 水上電探(22号対水上電探 ID: 28)
			var n3 = Calculator.GetNightAttackKind(new[] { 4, 58, 28 }, 144, 1501);
			Assert.Equal(NightAttackKind.CutinTorpedoRadar, n3);

			// 4. 主主CI: 主砲x3
			var n4 = Calculator.GetNightAttackKind(new[] { 4, 4, 4, 0 }, 80, 1501);
			Assert.Equal(NightAttackKind.CutinMainMain, n4);

			// 5. 主副CI: 主砲x2 + 副砲x1
			var n5 = Calculator.GetNightAttackKind(new[] { 4, 4, 11, 0 }, 80, 1501);
			Assert.Equal(NightAttackKind.CutinMainSub, n5);
		}

		#endregion

		#region 6. 先制対潜 (ShipData.CanOpeningASW)

		[Fact(DisplayName = "先制対潜: 無条件艦、ソナー要件、海防艦、護衛空母")]
		public void OpeningASW_ConditionsAndExceptions()
		{
			// 1. 無条件先制対潜艦: 五十鈴改二 (ID: 141), Jervis改 (ID: 394), Fletcher (ID: 596)
			var isuzu = TestMasterDataFixture.CreateShip(141, level: 50, slotEquipmentIds: Array.Empty<int>());
			Assert.True(isuzu.CanOpeningASW, "五十鈴改二は装備なしでも先制対潜可能であるべき");

			var jervis = TestMasterDataFixture.CreateShip(394, level: 50, slotEquipmentIds: Array.Empty<int>());
			Assert.True(jervis.CanOpeningASW, "Jervis改は装備なしでも先制対潜可能であるべき");

			// 2. 一般駆逐艦: 夕立改二 (ID: 144)
			// ソナーなし + 対潜 100未満 -> 不可
			var yuudachi1 = TestMasterDataFixture.CreateShip(144, level: 80, asw: 60, slotEquipmentIds: Array.Empty<int>());
			Assert.False(yuudachi1.CanOpeningASW);

			// ソナーあり (三式水中探信儀 ID: 47) + 対潜 100以上 -> 可能
			var yuudachi2 = TestMasterDataFixture.CreateShip(144, level: 99, asw: 100, slotEquipmentIds: new[] { 47 });
			Assert.True(yuudachi2.CanOpeningASW);

			// ソナーなし + 対潜 100以上 -> 不可 (ソナー必須)
			var yuudachi3 = TestMasterDataFixture.CreateShip(144, level: 99, asw: 100, slotEquipmentIds: Array.Empty<int>());
			Assert.False(yuudachi3.CanOpeningASW);

			// 3. 海防艦: 択捉 (ID: 517)
			// ソナーあり + 対潜 60以上 -> 可能
			var etorofu1 = TestMasterDataFixture.CreateShip(517, level: 50, asw: 60, slotEquipmentIds: new[] { 47 });
			Assert.True(etorofu1.CanOpeningASW);

			// 対潜 60未満 (素対潜40 + ソナー10 = 50 < 60) -> 不可
			var etorofu2 = TestMasterDataFixture.CreateShip(517, level: 10, asw: 40, slotEquipmentIds: new[] { 47 });
			Assert.False(etorofu2.CanOpeningASW);

			// 4. 攻撃型軽空母 (鈴谷航改二 ID: 508): いかなる装備でも先制対潜不可
			var suzuya = TestMasterDataFixture.CreateShip(508, level: 99, asw: 120, slotEquipmentIds: new[] { 47, 70, 70 });
			Assert.False(suzuya.CanOpeningASW);
		}

		#endregion

		#region 7. 支援艦隊種別 (FleetData.SupportType)

		[Fact(DisplayName = "支援艦隊種別: 駆逐艦数要件、航空支援、砲撃支援、雷撃支援、対潜支援")]
		public void SupportType_FleetComposition()
		{
			// 駆逐艦2隻の準備
			var dd1 = TestMasterDataFixture.CreateShip(471); // 神風
			var dd2 = TestMasterDataFixture.CreateShip(472); // 朝風

			// 1. 駆逐艦が1隻のみ -> 支援発生しない (0)
			var fleet0 = TestMasterDataFixture.CreateFleet(dd1);
			Assert.Equal(0, fleet0.SupportType);

			// 2. 対潜支援: 駆逐2 + 軽空母2 (ID: 560 瑞鳳改二乙 x2) -> 4
			var cvl1 = TestMasterDataFixture.CreateShip(560);
			var cvl2 = TestMasterDataFixture.CreateShip(560);
			var fleetAsw = TestMasterDataFixture.CreateFleet(dd1, dd2, cvl1, cvl2);
			Assert.Equal(4, fleetAsw.SupportType);

			// 3. 航空支援: 駆逐2 + 正規空母2 (赤城 ID: 83, 加賀 ID: 84) -> 1
			var cv1 = TestMasterDataFixture.CreateShip(83);
			var cv2 = TestMasterDataFixture.CreateShip(84);
			var fleetAir = TestMasterDataFixture.CreateFleet(dd1, dd2, cv1, cv2);
			Assert.Equal(1, fleetAir.SupportType);

			// 4. 砲撃支援: 駆逐2 + 戦艦2 (長門 ID: 80, 陸奥 ID: 81) + 重巡2 (妙高 ID: 62, 那智 ID: 63) -> 2
			var bb1 = TestMasterDataFixture.CreateShip(80);
			var bb2 = TestMasterDataFixture.CreateShip(81);
			var ca1 = TestMasterDataFixture.CreateShip(62);
			var ca2 = TestMasterDataFixture.CreateShip(63);
			var fleetShelling = TestMasterDataFixture.CreateFleet(dd1, dd2, bb1, bb2, ca1, ca2);
			Assert.Equal(2, fleetShelling.SupportType);

			// 5. 雷撃支援: 駆逐2 + 軽巡2 (川内 ID: 46, 神通 ID: 47) + 雷巡2 (大井改二 ID: 118, 北上改二 ID: 119) -> 3
			var cl1 = TestMasterDataFixture.CreateShip(46);
			var cl2 = TestMasterDataFixture.CreateShip(47);
			var clt1 = TestMasterDataFixture.CreateShip(118);
			var clt2 = TestMasterDataFixture.CreateShip(119);
			var fleetTorpedo = TestMasterDataFixture.CreateFleet(dd1, dd2, cl1, cl2, clt1, clt2);
			Assert.Equal(3, fleetTorpedo.SupportType);
		}

		#endregion

		#region 8. 泊地修理 (Calculator.CalculateAnchorageRepairHealAmount, CalculateAnchorageRepairTime)

		[Fact(DisplayName = "泊地修理: 回復量計算と所要時間逆算、工作艦配置ボーナス")]
		public void AnchorageRepair_CalculationAndRepairShipBonus()
		{
			// 被ダメージ 10, 入渠所要時間 3600秒 (1時間)
			int damage = 10;
			double dockingSeconds = 3600.0;

			// 1. 通常泊地修理: 20分 (1200秒) 経過時
			// 1200 / (3600 / 10) = 1200 / 360 = 3.33 -> 3 回復
			int healNormal = Calculator.CalculateAnchorageRepairHealAmount(damage, dockingSeconds, TimeSpan.FromMinutes(20), is1st2ndRepairShip: false);
			Assert.Equal(3, healNormal);

			// 2. 23分 (1380秒) 経過時の工作艦 (明石/朝日) 効果: 通常 3 回復 vs 工作艦ボーナス 4 回復
			// 通常: 1380 / 360 = 3.83 -> 3 回復
			// 工作艦 (所要時間 0.85倍 -> 3060秒): 1380 / 306 = 4.51 -> 4 回復
			int healNormal23 = Calculator.CalculateAnchorageRepairHealAmount(damage, dockingSeconds, TimeSpan.FromMinutes(23), is1st2ndRepairShip: false);
			Assert.Equal(3, healNormal23);

			int healBonus23 = Calculator.CalculateAnchorageRepairHealAmount(damage, dockingSeconds, TimeSpan.FromMinutes(23), is1st2ndRepairShip: true);
			Assert.Equal(4, healBonus23);

			// 3. 所要時間逆算: 1回復に必要な最低時間は 20分
			TimeSpan timeFor1 = Calculator.CalculateAnchorageRepairTime(damage, dockingSeconds, healAmount: 1, is1st2ndRepairShip: false);
			Assert.Equal(TimeSpan.FromMinutes(20), timeFor1);

			// 4. 工作艦効果時の所要時間短縮検証 (0.85倍)
			TimeSpan timeFor3Normal = Calculator.CalculateAnchorageRepairTime(damage, dockingSeconds, healAmount: 3, is1st2ndRepairShip: false);
			TimeSpan timeFor3Bonus = Calculator.CalculateAnchorageRepairTime(damage, dockingSeconds, healAmount: 3, is1st2ndRepairShip: true);
			Assert.True(timeFor3Bonus <= timeFor3Normal, $"工作艦効果時は所要時間が短縮されるべき: {timeFor3Bonus} <= {timeFor3Normal}");
		}

		#endregion

		#region 9. 艦娘ダメージ状態・夜戦可否・ダメコン判定 (ShipData.DamageControlID, ShipData.CanAttackAtNight)

		[Fact(DisplayName = "ダメコン判定: 補強増設優先度および通常スロット走査順の境界値検証")]
		public void ShipData_DamageControlID_PriorityAndFallback()
		{
			// 1. 補強増設に応急修理女神 (ID: 43)、通常スロット第1に応急修理要員 (ID: 42)
			// 期待値: 増設最優先で 43
			var ship1 = TestMasterDataFixture.CreateShip(
				masterShipId: 426, // 吹雪改二
				slotEquipmentIds: new[] { 42, 0, 0 },
				expansionSlotEquipmentId: 43);
			Assert.Equal(43, ship1.DamageControlID);

			// 2. 補強増設に応急修理要員 (ID: 42)、通常スロット第1に応急修理女神 (ID: 43)
			// 期待値: 増設最優先で 42
			var ship2 = TestMasterDataFixture.CreateShip(
				masterShipId: 426,
				slotEquipmentIds: new[] { 43, 0, 0 },
				expansionSlotEquipmentId: 42);
			Assert.Equal(42, ship2.DamageControlID);

			// 3. 補強増設なし (-1)、スロット1に要員(42)、スロット2に女神(43)
			// 期待値: スロット先頭優先で 42
			var ship3 = TestMasterDataFixture.CreateShip(
				masterShipId: 426,
				slotEquipmentIds: new[] { 42, 43, 0 },
				expansionSlotEquipmentId: -1);
			Assert.Equal(42, ship3.DamageControlID);

			// 4. 補強増設なし (-1)、スロット1に女神(43)、スロット2に要員(42)
			// 期待値: スロット先頭優先で 43
			var ship4 = TestMasterDataFixture.CreateShip(
				masterShipId: 426,
				slotEquipmentIds: new[] { 43, 42, 0 },
				expansionSlotEquipmentId: -1);
			Assert.Equal(43, ship4.DamageControlID);

			// 5. 補強増設なし (-1)、ダメコン未装備 (主砲等のみ)
			// 期待値: -1
			var ship5 = TestMasterDataFixture.CreateShip(
				masterShipId: 426,
				slotEquipmentIds: new[] { 2, 2, 0 }, // 12.7cm連装砲
				expansionSlotEquipmentId: -1);
			Assert.Equal(-1, ship5.DamageControlID);
		}

		[Fact(DisplayName = "夜戦可否判定: 通常艦・正規空母・装甲空母・Ark Royal の被ダメージ状態と装備条件")]
		public void ShipData_CanAttackAtNight_DamagedAndCarrierConditions()
		{
			// 1. 通常艦 (吹雪改二 ID: 426, HP 31)
			// 健在・小破・中破は夜戦可能、大破 (HP <= 0.25) は不可
			var ddHealthy = TestMasterDataFixture.CreateShip(426, hp: 31);
			var ddMinor = TestMasterDataFixture.CreateShip(426, hp: 23); // 23/31 = 74.1% (小破)
			var ddModerate = TestMasterDataFixture.CreateShip(426, hp: 15); // 15/31 = 48.3% (中破)
			var ddHeavy = TestMasterDataFixture.CreateShip(426, hp: 7); // 7/31 = 22.5% <= 25% (大破)

			Assert.True(ddHealthy.CanAttackAtNight, "通常艦・健在は夜戦可能");
			Assert.True(ddMinor.CanAttackAtNight, "通常艦・小破は夜戦可能");
			Assert.True(ddModerate.CanAttackAtNight, "通常艦・中破は夜戦可能");
			Assert.False(ddHeavy.CanAttackAtNight, "通常艦・大破は夜戦不可");

			// 2. 通常空母 (赤城改 ID: 277, HP 77)
			// 夜間作戦航空要員 (ID: 258) + 夜戦 (ID: 254: F6F-3N) 装備時
			// 装甲空母ではないため中破 (HP <= 50%) 以下で不可
			var cvHealthy = TestMasterDataFixture.CreateShip(277, hp: 77, slotEquipmentIds: new[] { 258, 254, 0, 0 });
			var cvMinor = TestMasterDataFixture.CreateShip(277, hp: 40, slotEquipmentIds: new[] { 258, 254, 0, 0 }); // 40/77 = 51.9% (> 50%)
			var cvModerate = TestMasterDataFixture.CreateShip(277, hp: 38, slotEquipmentIds: new[] { 258, 254, 0, 0 }); // 38/77 = 49.3% (<= 50%)
			var cvHeavy = TestMasterDataFixture.CreateShip(277, hp: 1, slotEquipmentIds: new[] { 258, 254, 0, 0 });
			var cvNoNightEq = TestMasterDataFixture.CreateShip(277, hp: 77, slotEquipmentIds: new[] { 21, 21, 0, 0 }); // 夜間装備なし

			Assert.True(cvHealthy.CanAttackAtNight, "通常空母・夜間装備あり・健在は夜戦可能");
			Assert.True(cvMinor.CanAttackAtNight, "通常空母・夜間装備あり・小破は夜戦可能");
			Assert.False(cvModerate.CanAttackAtNight, "通常空母・夜間装備あり・中破は夜戦不可");
			Assert.False(cvHeavy.CanAttackAtNight, "通常空母・夜間装備あり・大破は夜戦不可");
			Assert.False(cvNoNightEq.CanAttackAtNight, "通常空母・夜間装備なしは健在でも夜戦不可");

			// 3. 装甲空母 (大鳳改 ID: 156, HP 70)
			// 夜間作戦航空要員 (ID: 258) + 夜戦 (ID: 254) 装備時
			// 装甲空母のため中破でも夜戦可能、大破 (HP <= 25%) のみ不可
			var cvbHealthy = TestMasterDataFixture.CreateShip(156, hp: 70, slotEquipmentIds: new[] { 258, 254, 0, 0 });
			var cvbModerate = TestMasterDataFixture.CreateShip(156, hp: 35, slotEquipmentIds: new[] { 258, 254, 0, 0 }); // 35/70 = 50.0% (中破)
			var cvbHeavy = TestMasterDataFixture.CreateShip(156, hp: 1, slotEquipmentIds: new[] { 258, 254, 0, 0 });

			Assert.True(cvbHealthy.CanAttackAtNight, "装甲空母・夜間装備あり・健在は夜戦可能");
			Assert.True(cvbModerate.CanAttackAtNight, "装甲空母・夜間装備あり・中破でも夜戦可能");
			Assert.False(cvbHeavy.CanAttackAtNight, "装甲空母・夜間装備あり・大破は夜戦不可");

			// 4. Ark Royal改 (ID: 393, HP 71)
			// Swordfish (ID: 242) 装備時は夜間要員なしで中破以下夜戦可能
			var arkHealthy = TestMasterDataFixture.CreateShip(393, hp: 71, slotEquipmentIds: new[] { 242, 0, 0, 0 });
			var arkModerate = TestMasterDataFixture.CreateShip(393, hp: 35, slotEquipmentIds: new[] { 242, 0, 0, 0 }); // 35/71 = 49.2% (中破)
			var arkHeavy = TestMasterDataFixture.CreateShip(393, hp: 1, slotEquipmentIds: new[] { 242, 0, 0, 0 });
			var arkNoSwordfish = TestMasterDataFixture.CreateShip(393, hp: 71, slotEquipmentIds: new[] { 21, 0, 0, 0 }); // Swordfishなし

			Assert.True(arkHealthy.CanAttackAtNight, "Ark Royal改・Swordfish装備・健在は夜戦可能");
			Assert.True(arkModerate.CanAttackAtNight, "Ark Royal改・Swordfish装備・中破は夜戦可能");
			Assert.False(arkHeavy.CanAttackAtNight, "Ark Royal改・Swordfish装備・大破は夜戦不可");
			Assert.False(arkNoSwordfish.CanAttackAtNight, "Ark Royal改・Swordfish未装備は夜戦不可");
		}

		#endregion

		#region 10. 戦闘勝敗ランク判定 (BattleManager.GetWinRank, BattleManager.GetWinRankAirRaid)

		private static int InvokeGetWinRank(
			int countFriend, int countEnemy,
			int sunkFriend, int sunkEnemy,
			double friendrate, double enemyrate,
			bool isfriendFlagshipHeavilyDamaged, bool defeatEnemyFlagship)
		{
			var method = typeof(BattleManager).GetMethod("GetWinRank", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);
			return (int)method.Invoke(null, new object[]
			{
				countFriend, countEnemy, sunkFriend, sunkEnemy, friendrate, enemyrate, isfriendFlagshipHeavilyDamaged, defeatEnemyFlagship
			})!;
		}

		private static int InvokeGetWinRankAirRaid(int countFriend, int sunkFriend, double friendrate)
		{
			var method = typeof(BattleManager).GetMethod("GetWinRankAirRaid", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);
			return (int)method.Invoke(null, new object[] { countFriend, sunkFriend, friendrate })!;
		}

		[Fact(DisplayName = "勝敗ランク判定: 通常戦闘の各ランク判定 (完全S, S, A, B, C, D, E) 境界値検証")]
		public void BattleRank_NormalBattle_BoundaryTests()
		{
			// 1. 完全勝利 SS (7): 友軍轟沈0, 敵全撃沈, 友軍損害率 0.0
			int rankSS = InvokeGetWinRank(6, 6, 0, 6, 0.0, 1.0, false, true);
			Assert.Equal(7, rankSS);

			// 2. 勝利 S (6): 友軍轟沈0, 敵全撃沈, 友軍損害率 > 0
			int rankS = InvokeGetWinRank(6, 6, 0, 6, 0.05, 1.0, false, true);
			Assert.Equal(6, rankS);

			// 3. 勝利 A (5): 友軍轟沈0, 敵の 70% 以上撃沈 (敵6隻中4隻: (int)(6 * 0.7) = 4)
			// 4隻撃沈 -> A (5)
			int rankA = InvokeGetWinRank(6, 6, 0, 4, 0.2, 0.6, false, false);
			Assert.Equal(5, rankA);

			// 3隻撃沈 (3 < 4) -> A にならず (敵旗艦未撃沈、損害率ゲージ比 2.5倍未満なら C/D)
			int rankNotA = InvokeGetWinRank(6, 6, 0, 3, 0.4, 0.45, false, false);
			Assert.NotEqual(5, rankNotA);

			// 4. 戦術的勝利 B (4):
			// ケース 4-1: 敵旗艦撃沈 かつ 友軍轟沈数 < 敵撃沈数
			int rankB_Flagship = InvokeGetWinRank(6, 6, 1, 2, 0.3, 0.3, false, true);
			Assert.Equal(4, rankB_Flagship);

			// ケース 4-2: 敵旗艦未撃沈だが、損害率ゲージ比 > 2.5 (rienemy > 2.5 * rifriend)
			// rifriend = 20, 2.5 * 20 = 50 -> rienemy = 51 で B
			int rankB_Gauge = InvokeGetWinRank(6, 6, 0, 1, 0.20, 0.51, false, false);
			Assert.Equal(4, rankB_Gauge);

			// 境界値: rienemy = 50 のときは 50 > 50 が不成立のため B ではなく C (3)
			int rankC_GaugeBoundary = InvokeGetWinRank(6, 6, 0, 1, 0.20, 0.50, false, false);
			Assert.Equal(3, rankC_GaugeBoundary);

			// 5. 戦術的敗北 C (3): ゲージ比 > 0.9 (rienemy > 0.9 * rifriend)
			// rifriend = 40, 0.9 * 40 = 36 -> rienemy = 37 で C
			int rankC = InvokeGetWinRank(6, 6, 0, 0, 0.40, 0.37, false, false);
			Assert.Equal(3, rankC);

			// 境界値: rienemy = 36 のときは 36 > 36 が不成立のため D (2)
			int rankD_GaugeBoundary = InvokeGetWinRank(6, 6, 0, 0, 0.40, 0.36, false, false);
			Assert.Equal(2, rankD_GaugeBoundary);

			// 6. 敗北 D (2):
			// ケース 6-1: 1隻編成 かつ 旗艦大破
			int rankD_FlagshipDamaged = InvokeGetWinRank(1, 6, 0, 0, 0.8, 0.0, true, false);
			Assert.Equal(2, rankD_FlagshipDamaged);

			// ケース 6-2: ゲージ比 0.9倍以下
			int rankD = InvokeGetWinRank(6, 6, 0, 0, 0.50, 0.40, false, false);
			Assert.Equal(2, rankD);

			// 7. 敗北 E (1): 友軍轟沈あり かつ 残存艦が1隻のみ (sunkFriend > 0 && countFriend - sunkFriend == 1)
			int rankE = InvokeGetWinRank(6, 6, 5, 0, 0.9, 0.0, false, false);
			Assert.Equal(1, rankE);
		}

		[Fact(DisplayName = "勝敗ランク判定: 空襲戦ランク判定 (SS, A, B, C, D, E) の友軍損害率境界値検証")]
		public void BattleRank_AirRaid_BoundaryTests()
		{
			// SS: friendrate <= 0.0
			Assert.Equal(7, InvokeGetWinRankAirRaid(6, 0, 0.0));

			// A: friendrate < 0.1
			Assert.Equal(5, InvokeGetWinRankAirRaid(6, 0, 0.0001));
			Assert.Equal(5, InvokeGetWinRankAirRaid(6, 0, 0.0999));

			// B: 0.1 <= friendrate < 0.2
			Assert.Equal(4, InvokeGetWinRankAirRaid(6, 0, 0.1000));
			Assert.Equal(4, InvokeGetWinRankAirRaid(6, 0, 0.1999));

			// C: 0.2 <= friendrate < 0.5
			Assert.Equal(3, InvokeGetWinRankAirRaid(6, 0, 0.2000));
			Assert.Equal(3, InvokeGetWinRankAirRaid(6, 0, 0.4999));

			// D: 0.5 <= friendrate < 0.8
			Assert.Equal(2, InvokeGetWinRankAirRaid(6, 0, 0.5000));
			Assert.Equal(2, InvokeGetWinRankAirRaid(6, 0, 0.7999));

			// E: friendrate >= 0.8
			Assert.Equal(1, InvokeGetWinRankAirRaid(6, 0, 0.8000));
			Assert.Equal(1, InvokeGetWinRankAirRaid(6, 0, 1.0));
		}

		#endregion
	}
}
