#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ElectronicObserver.Database;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.SortieCostViewer;
using ElectronicObserver.Window.Tools.SortieRecordViewer.SortieDetail;
using Xunit;

namespace ElectronicObserverTests
{
	[Collection("KCDatabase")]
	public class SortieWpfWindowTests
	{
		/// <summary>
		/// WPF ウィンドウをテストするために STA スレッド上でデリゲートを実行します。
		/// </summary>
		private static void RunInSta(Action action)
		{
			Exception? exception = null;
			var thread = new Thread(() =>
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					exception = ex;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();

			if (exception != null)
			{
				throw new TargetInvocationException("STA execution failed", exception);
			}
		}

		private static SortieRecord CreateSampleSortieRecord()
		{
			return new SortieRecord
			{
				Id = 1001,
				Version = 1,
				World = 7,
				Map = 1,
				FleetData = new SortieFleetData
				{
					FleetId = 1,
					CombinedFlag = 0,
					NodeSupportFleetId = 2,
					BossSupportFleetId = 3,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Name = "第1艦隊",
							Ships = new List<SortieShip>
							{
								new SortieShip
								{
									Id = 1, // 睦月
									Level = 99,
									Fuel = 15,
									Ammo = 20,
									Hp = 20,
									Condition = 49,
								},
								new SortieShip
								{
									Id = 9, // 吹雪
									Level = 90,
									Fuel = 15,
									Ammo = 20,
									Hp = 30,
									Condition = 49,
								},
							},
						},
					},
					AirBases = new List<SortieAirBase>
					{
						new SortieAirBase
						{
							AirBaseId = 1,
							Name = "第1基地隊",
							MapAreaId = 7,
							ActionKind = 1,
						},
					},
				},
				ApiFiles = new List<ApiFile>
				{
					new ApiFile
					{
						Id = 1,
						ApiFileType = ApiFileType.Request,
						Name = "api_req_map/start",
						TimeStamp = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc),
						Content = "",
					},
					new ApiFile
					{
						Id = 2,
						ApiFileType = ApiFileType.Response,
						Name = "api_req_map/start",
						TimeStamp = new DateTime(2026, 11, 5, 12, 0, 1, DateTimeKind.Utc),
						Content = "{\"api_data\":{\"api_rashin_flg\":0,\"api_no\":1,\"api_color_no\":0,\"api_event_id\":2,\"api_event_kind\":1,\"api_next\":1}}",
					},
				},
				CalculatedSortieCost = new CalculatedSortieCost
				{
					SortieFleetSupplyCost = new SortieCostModel { Fuel = 100, Ammo = 120, Steel = 0, Bauxite = 0 },
					SortieFleetRepairCost = new SortieCostModel { Fuel = 30, Ammo = 0, Steel = 50, Bauxite = 0 },
					SortieFleetJetCost = SortieCostModel.Zero,
					NodeSupportSupplyCost = new SortieCostModel { Fuel = 50, Ammo = 80, Steel = 0, Bauxite = 0 },
					BossSupportSupplyCost = new SortieCostModel { Fuel = 50, Ammo = 80, Steel = 0, Bauxite = 0 },
					TotalAirBaseSortieCost = new SortieCostModel { Fuel = 40, Ammo = 20, Steel = 0, Bauxite = 0 },
					TotalAirBaseSupplyCost = new SortieCostModel { Fuel = 10, Ammo = 10, Steel = 0, Bauxite = 10 },
					AirBaseJetCost = SortieCostModel.Zero,
					DamageStateCounts = new Dictionary<string, int>
					{
						["無傷"] = 1,
						["小破"] = 1,
					},
					ConsumedItems = new List<ConsumableItemData>
					{
						new ConsumableItemData { Id = 1, Count = 2 },
					},
				},
			};
		}

		[Fact]
		public void SortieRecordViewModel_ShouldInstantiateAndExposePropertiesCorrectly()
		{
			var record = CreateSampleSortieRecord();
			var vm = new SortieRecordViewModel(record);

			Assert.Equal(1001, vm.Id);
			Assert.Equal(7, vm.World);
			Assert.Equal(1, vm.Map);
			Assert.Equal("7-1", vm.MapDisplay);
			Assert.Equal("通常艦隊", vm.FleetTypeDisplay);
			Assert.Equal("道中/決戦", vm.SupportDisplay);
			Assert.Equal("1隊", vm.AirBaseDisplay);
			Assert.Contains("Lv99", vm.FleetSummary);
			Assert.NotEmpty(vm.FleetDetailsTooltip);
			Assert.Equal(record.ApiFiles[0].TimeStamp.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"), vm.SortieStartDisplay);
		}

		[Fact]
		public void SortieRecordViewerWindow_ShouldInstantiateSuccessfullyInSta()
		{
			RunInSta(() =>
			{
				var window = new SortieRecordViewerWindow(() => new ElectronicObserverDbContext(inMemory: true));
				Assert.NotNull(window);
				Assert.Equal("出撃記録", window.Title);
				window.Close();
			});
		}

		[Fact]
		public void SortieDetailWindow_ShouldInstantiateAndBindSuccessfullyInSta()
		{
			RunInSta(() =>
			{
				var record = CreateSampleSortieRecord();
				var detailData = new SortieDetailData
				{
					SortieId = record.Id,
					World = record.World,
					Map = record.Map,
					StartTime = DateTime.UtcNow,
					Nodes = new List<SortieNodeInfo>
					{
						new SortieNodeInfo
						{
							CellId = 1,
							CellDisplay = "A (1)",
							EventId = 4,
							EventKind = 1,
							NodeTypeDescription = "通常戦闘",
							WinRank = "S",
							MvpShipName = "睦月",
							DropShipName = "吹雪",
							PlayerFormation = 1,
							EnemyFormation = 1,
							EngagementForm = 1,
							AirControlState = 1,
							Phases = new List<BattlePhaseInfo>
							{
								new BattlePhaseInfo
								{
									PhaseName = "昼戦砲撃戦",
									Summary = "1巡目",
									Attacks = new List<BattleAttackEntry>
									{
										new BattleAttackEntry
										{
											AttackerName = "睦月",
											DefenderName = "駆逐イ級",
											Damage = 45,
											IsCritical = true,
											AttackType = "通常攻撃",
										},
									},
								},
							},
							FriendlyShipsAfter = new List<BattleShipSnapshot>
							{
								new BattleShipSnapshot
								{
									ShipMasterId = 1,
									Name = "睦月",
									Level = 99,
									HpBefore = 20,
									HpAfter = 20,
									HpMax = 20,
									DamageStateText = "無傷",
								},
							},
							EnemyShipsAfter = new List<BattleShipSnapshot>
							{
								new BattleShipSnapshot
								{
									ShipMasterId = 501,
									Name = "駆逐イ級",
									Level = 1,
									HpBefore = 20,
									HpAfter = 0,
									HpMax = 20,
									DamageStateText = "撃沈",
								},
							},
						},
					},
				};

				var window = new SortieDetailWindow(detailData, record);
				Assert.NotNull(window);
				Assert.Contains("出撃戦闘詳細", window.Title);
				window.Close();
			});
		}

		[Fact]
		public void SortieCostViewerWindow_ShouldInstantiateAndBindSuccessfullyInSta()
		{
			RunInSta(() =>
			{
				var record1 = CreateSampleSortieRecord();
				var record2 = CreateSampleSortieRecord();
				record2.Id = 1002;
				record2.Map = 2;

				var window = new SortieCostViewerWindow(new[] { record1, record2 });
				Assert.NotNull(window);
				Assert.Equal("出撃コスト詳細", window.Title);

				var listGrid = window.FindName("SortieListDataGrid") as System.Windows.Controls.DataGrid;
				var selectedGrid = window.FindName("SelectedDetailDataGrid") as System.Windows.Controls.DataGrid;
				var totalGrid = window.FindName("TotalDetailDataGrid") as System.Windows.Controls.DataGrid;

				Assert.NotNull(listGrid);
				Assert.Equal(2, listGrid!.Items.Count);
				Assert.NotNull(selectedGrid?.ItemsSource);
				Assert.NotNull(totalGrid?.ItemsSource);
				window.Close();
			});
		}
	}
}
