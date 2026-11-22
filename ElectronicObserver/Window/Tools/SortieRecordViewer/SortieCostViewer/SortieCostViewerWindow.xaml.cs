#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ElectronicObserver.Database.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.SortieCostViewer
{
	/// <summary>
	/// SortieCostViewerWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class SortieCostViewerWindow : System.Windows.Window
	{
		private readonly List<SortieRecord> _records;
		private readonly List<SortieCostItemViewModel> _costItems = new List<SortieCostItemViewModel>();
		private readonly CalculatedSortieCost _totalCost;
		private readonly List<CostRowViewModel> _totalRows = new List<CostRowViewModel>();

		public SortieCostViewerWindow(IEnumerable<SortieRecord> records)
		{
			InitializeComponent();

			_records = records?.ToList() ?? new List<SortieRecord>();
			if (_records.Count == 0)
			{
				TextTargetTitle.Text = "対象出撃: 0 件";
				TextTargetDetails.Text = "-";
				_totalCost = new CalculatedSortieCost();
				return;
			}

			// 各出撃のコストを計算
			var costs = new List<CalculatedSortieCost>();
			foreach (var r in _records)
			{
				var cost = r.CalculatedSortieCost ?? SortieCostCalculator.CalculateAndCache(r, r.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>());
				costs.Add(cost);
				_costItems.Add(new SortieCostItemViewModel(r, cost));
			}

			_totalCost = SortieCostCalculator.CalculateAggregateCost(costs);

			if (_records.Count == 1)
			{
				var single = _records[0];
				TextTargetTitle.Text = $"出撃 ID: #{single.Id} コスト詳細";
				TextTargetDetails.Text = $"海域: {single.World}-{single.Map}";
			}
			else
			{
				TextTargetTitle.Text = $"出撃 コスト詳細 ({_records.Count} 件)";
				string ids = string.Join(", ", _records.Take(5).Select(r => $"#{r.Id}"));
				if (_records.Count > 5) ids += "...";
				TextTargetDetails.Text = $"出撃ID: {ids}";
			}

			// 上部: 出撃一覧
			SortieListDataGrid.ItemsSource = _costItems;

			// 右下: 全体合計内訳
			BuildCostRows(_totalRows, _totalCost);
			TotalDetailDataGrid.ItemsSource = _totalRows;
			UpdateTotalSummary();

			// 初期選択: 最初の出撃を選択
			if (_costItems.Count > 0)
			{
				SortieListDataGrid.SelectedIndex = 0;
			}
		}

		private static void BuildCostRows(List<CostRowViewModel> rows, CalculatedSortieCost cost)
		{
			rows.Clear();
			rows.Add(new CostRowViewModel("出撃艦隊 補給", cost.SortieFleetSupplyCost ?? SortieCostModel.Zero));
			rows.Add(new CostRowViewModel("入渠修理", cost.SortieFleetRepairCost ?? SortieCostModel.Zero));
			rows.Add(new CostRowViewModel("噴式機鋼材 (本隊)", cost.SortieFleetJetCost ?? SortieCostModel.Zero));
			rows.Add(new CostRowViewModel("道中支援 補給", cost.NodeSupportSupplyCost ?? SortieCostModel.Zero));
			rows.Add(new CostRowViewModel("決戦支援 補給", cost.BossSupportSupplyCost ?? SortieCostModel.Zero));
			rows.Add(new CostRowViewModel("基地航空隊 出撃", cost.TotalAirBaseSortieCost ?? SortieCostModel.Zero));
			rows.Add(new CostRowViewModel("基地航空隊 補充", cost.TotalAirBaseSupplyCost ?? SortieCostModel.Zero));
			rows.Add(new CostRowViewModel("基地航空隊 噴式機鋼材", cost.AirBaseJetCost ?? SortieCostModel.Zero));

			var totalModel = (cost.SortieFleetSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.SortieFleetRepairCost ?? SortieCostModel.Zero)
			                 + (cost.SortieFleetJetCost ?? SortieCostModel.Zero)
			                 + (cost.NodeSupportSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.BossSupportSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.TotalAirBaseSortieCost ?? SortieCostModel.Zero)
			                 + (cost.TotalAirBaseSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.AirBaseJetCost ?? SortieCostModel.Zero);

			rows.Add(new CostRowViewModel("【合計】", totalModel));
		}

		private void SortieListDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selectedItem = SortieListDataGrid.SelectedItem as SortieCostItemViewModel;
			if (selectedItem == null) return;

			TextSelectedSortieTitle.Text = $"選択出撃 #{selectedItem.Id} ({selectedItem.Area}) の内訳";
			SelectedDetailDataGrid.ItemsSource = selectedItem.DetailRows;

			TextSelectedBuckets.Text = $"バケツ: {selectedItem.BucketCount} 個";
			TextSelectedDamage.Text = $"損害: {selectedItem.DamageStateText}";
		}

		private void UpdateTotalSummary()
		{
			int totalBuckets = 0;
			if (_totalCost.ConsumedItems != null)
			{
				foreach (var item in _totalCost.ConsumedItems)
				{
					if (item.Id == 1 || item.Id == 6)
					{
						totalBuckets += item.Count;
					}
				}
			}

			TextTotalBuckets.Text = $"総バケツ: {totalBuckets} 個";

			if (_totalCost.DamageStateCounts != null && _totalCost.DamageStateCounts.Count > 0)
			{
				int healthy = GetCount("Healthy") + GetCount("無傷");
				int light = GetCount("Light") + GetCount("小破");
				int medium = GetCount("Medium") + GetCount("中破");
				int heavy = GetCount("Heavy") + GetCount("大破");
				int sunk = GetCount("Sunk") + GetCount("撃沈");

				TextTotalDamage.Text = $"総損害: 無傷 {healthy}, 小破 {light}, 中破 {medium}, 大破 {heavy}, 撃沈 {sunk}";
			}
			else
			{
				TextTotalDamage.Text = "総損害: データなし";
			}
		}

		private int GetCount(string key)
		{
			if (_totalCost.DamageStateCounts != null && _totalCost.DamageStateCounts.TryGetValue(key, out int count))
			{
				return count;
			}
			return 0;
		}

		private void CopyCsvButton_Click(object sender, RoutedEventArgs e)
		{
			var sb = new StringBuilder();
			sb.AppendLine("出撃ID,海域,出撃日時,燃料,弾薬,鋼材,ボーキサイト,バケツ");

			foreach (var item in _costItems)
			{
				sb.AppendLine($"{item.Id},{item.Area},{item.SortieStartDisplay},{item.TotalFuel},{item.TotalAmmo},{item.TotalSteel},{item.TotalBauxite},{item.BucketCount}");
			}

			// 合計行
			var totalModel = _totalRows.LastOrDefault();
			int totalBuckets = _costItems.Sum(i => i.BucketCount);
			if (totalModel != null)
			{
				sb.AppendLine($"【全体合計】,,,{totalModel.Fuel},{totalModel.Ammo},{totalModel.Steel},{totalModel.Bauxite},{totalBuckets}");
			}

			try
			{
				System.Windows.Clipboard.SetText(sb.ToString());
				TextStatus.Text = "出撃コスト一覧を CSV 形式でクリップボードにコピーしました。";
			}
			catch (Exception ex)
			{
				TextStatus.Text = $"コピーに失敗しました: {ex.Message}";
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}

	public class SortieCostItemViewModel
	{
		public SortieRecord Model { get; }
		public CalculatedSortieCost Cost { get; }

		public int Id => Model.Id;
		public string Area => $"{Model.World}-{Model.Map}";

		public DateTime SortieStartUtc { get; }
		public DateTime SortieStartLocal => SortieStartUtc.ToLocalTime();
		public string SortieStartDisplay => SortieStartUtc == DateTime.MinValue
			? "-"
			: SortieStartLocal.ToString("yyyy/MM/dd HH:mm:ss");

		public int TotalFuel { get; }
		public int TotalAmmo { get; }
		public int TotalSteel { get; }
		public int TotalBauxite { get; }
		public int BucketCount { get; }

		public string FuelDisplay => TotalFuel > 0 ? TotalFuel.ToString("#,##0") : "-";
		public string AmmoDisplay => TotalAmmo > 0 ? TotalAmmo.ToString("#,##0") : "-";
		public string SteelDisplay => TotalSteel > 0 ? TotalSteel.ToString("#,##0") : "-";
		public string BauxiteDisplay => TotalBauxite > 0 ? TotalBauxite.ToString("#,##0") : "-";
		public string BucketDisplay => BucketCount > 0 ? $"{BucketCount}個" : "-";

		public List<CostRowViewModel> DetailRows { get; } = new List<CostRowViewModel>();
		public string DamageStateText { get; }

		public SortieCostItemViewModel(SortieRecord model, CalculatedSortieCost cost)
		{
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Cost = cost ?? throw new ArgumentNullException(nameof(cost));

			if (model.ApiFiles != null && model.ApiFiles.Count > 0)
			{
				SortieStartUtc = model.ApiFiles.Min(f => f.TimeStamp);
			}
			else
			{
				SortieStartUtc = DateTime.MinValue;
			}

			// 内訳行の構築
			DetailRows.Add(new CostRowViewModel("出撃艦隊 補給", cost.SortieFleetSupplyCost ?? SortieCostModel.Zero));
			DetailRows.Add(new CostRowViewModel("入渠修理", cost.SortieFleetRepairCost ?? SortieCostModel.Zero));
			DetailRows.Add(new CostRowViewModel("噴式機鋼材 (本隊)", cost.SortieFleetJetCost ?? SortieCostModel.Zero));
			DetailRows.Add(new CostRowViewModel("道中支援 補給", cost.NodeSupportSupplyCost ?? SortieCostModel.Zero));
			DetailRows.Add(new CostRowViewModel("決戦支援 補給", cost.BossSupportSupplyCost ?? SortieCostModel.Zero));
			DetailRows.Add(new CostRowViewModel("基地航空隊 出撃", cost.TotalAirBaseSortieCost ?? SortieCostModel.Zero));
			DetailRows.Add(new CostRowViewModel("基地航空隊 補充", cost.TotalAirBaseSupplyCost ?? SortieCostModel.Zero));
			DetailRows.Add(new CostRowViewModel("基地航空隊 噴式機鋼材", cost.AirBaseJetCost ?? SortieCostModel.Zero));

			var totalModel = (cost.SortieFleetSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.SortieFleetRepairCost ?? SortieCostModel.Zero)
			                 + (cost.SortieFleetJetCost ?? SortieCostModel.Zero)
			                 + (cost.NodeSupportSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.BossSupportSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.TotalAirBaseSortieCost ?? SortieCostModel.Zero)
			                 + (cost.TotalAirBaseSupplyCost ?? SortieCostModel.Zero)
			                 + (cost.AirBaseJetCost ?? SortieCostModel.Zero);

			DetailRows.Add(new CostRowViewModel("【出撃計】", totalModel));

			TotalFuel = totalModel.Fuel;
			TotalAmmo = totalModel.Ammo;
			TotalSteel = totalModel.Steel;
			TotalBauxite = totalModel.Bauxite;

			if (cost.ConsumedItems != null)
			{
				foreach (var item in cost.ConsumedItems)
				{
					if (item.Id == 1 || item.Id == 6)
					{
						BucketCount += item.Count;
					}
				}
			}

			if (cost.DamageStateCounts != null && cost.DamageStateCounts.Count > 0)
			{
				int healthy = GetDamageCount(cost, "Healthy") + GetDamageCount(cost, "無傷");
				int light = GetDamageCount(cost, "Light") + GetDamageCount(cost, "小破");
				int medium = GetDamageCount(cost, "Medium") + GetDamageCount(cost, "中破");
				int heavy = GetDamageCount(cost, "Heavy") + GetDamageCount(cost, "大破");
				int sunk = GetDamageCount(cost, "Sunk") + GetDamageCount(cost, "撃沈");

				DamageStateText = $"無傷 {healthy}, 小破 {light}, 中破 {medium}, 大破 {heavy}, 撃沈 {sunk}";
			}
			else
			{
				DamageStateText = "データなし";
			}
		}

		private static int GetDamageCount(CalculatedSortieCost c, string key)
		{
			if (c.DamageStateCounts != null && c.DamageStateCounts.TryGetValue(key, out int count))
			{
				return count;
			}
			return 0;
		}
	}

	public class CostRowViewModel
	{
		public string Title { get; }
		public int Fuel { get; }
		public int Ammo { get; }
		public int Steel { get; }
		public int Bauxite { get; }

		public string FuelDisplay => Fuel > 0 ? Fuel.ToString("#,##0") : "-";
		public string AmmoDisplay => Ammo > 0 ? Ammo.ToString("#,##0") : "-";
		public string SteelDisplay => Steel > 0 ? Steel.ToString("#,##0") : "-";
		public string BauxiteDisplay => Bauxite > 0 ? Bauxite.ToString("#,##0") : "-";

		public CostRowViewModel(string title, SortieCostModel model)
		{
			Title = title;
			Fuel = model?.Fuel ?? 0;
			Ammo = model?.Ammo ?? 0;
			Steel = model?.Steel ?? 0;
			Bauxite = model?.Bauxite ?? 0;
		}
	}
}
