#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ElectronicObserver.Data;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.DataExport;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Replay;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.SortieDetail
{
	/// <summary>
	/// SortieDetailWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class SortieDetailWindow : System.Windows.Window
	{
		private readonly SortieRecord? _record;
		private readonly IEnumerable<ApiFile>? _apiFiles;

		public SortieDetailData DetailData { get; }

		public SortieDetailWindow(SortieDetailData detailData, SortieRecord? record = null, IEnumerable<ApiFile>? apiFiles = null)
		{
			InitializeComponent();

			DetailData = detailData ?? throw new ArgumentNullException(nameof(detailData));
			_record = record;
			_apiFiles = apiFiles ?? record?.ApiFiles;

			Title = $"出撃戦闘詳細 - 出撃#{detailData.SortieId} ({detailData.World}-{detailData.Map})";
			TextSortieId.Text = detailData.SortieId.ToString();
			TextMap.Text = $"{detailData.World}-{detailData.Map}";
			TextStartTime.Text = detailData.StartTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");

			if (detailData.ReachedBoss)
			{
				BossStatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0xF5, 0xE9));
				TextBossStatus.Text = "ボス到達";
				TextBossStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32));
			}
			else
			{
				BossStatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xEB, 0xEE));
				TextBossStatus.Text = "途中撤退";
				TextBossStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC6, 0x28, 0x28));
			}

			if (record != null)
			{
				var vm = new SortieRecordViewModel(record);
				TextFleetSummary.Text = $"編成: {vm.FleetSummary}";
			}

			var nodeViewModels = detailData.Nodes.Select(n => new SortieNodeItemViewModel(n)).ToList();
			NodesItemsControl.ItemsSource = nodeViewModels;

			TextStatus.Text = $"全 {nodeViewModels.Count} マスのデータを読み込みました。";
		}

		public SortieDetailWindow(SortieRecord record, IEnumerable<ApiFile> apiFiles)
			: this(SortieNodeEmulator.EmulateSortie(record, apiFiles), record, apiFiles)
		{
		}

		#region リプレイ & 外部連携
		private void ReplayOpen_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string url = ReplayService.GenerateReplayUrl(_record!, _apiFiles ?? Enumerable.Empty<ApiFile>());
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				SetStatus("リプレイURLをブラウザで開きました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"ブラウザの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ReplayCopyUrl_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string url = ReplayService.GenerateReplayUrl(_record!, _apiFiles ?? Enumerable.Empty<ApiFile>());
				Clipboard.SetText(url);
				SetStatus("リプレイURLをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ReplayCopyJson_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string json = ReplayService.GenerateReplayJson(_record!, _apiFiles ?? Enumerable.Empty<ApiFile>());
				Clipboard.SetText(json);
				SetStatus("リプレイJSONをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void AirControlSim_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string url = SortieExternalLinkService.GenerateAirControlSimulatorUrl(_record!);
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				SetStatus("制空権シミュレータをブラウザで開きました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"ブラウザの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void AirControlSimCopy_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string url = SortieExternalLinkService.GenerateAirControlSimulatorUrl(_record!);
				Clipboard.SetText(url);
				SetStatus("制空権シミュレータURLをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OperationRoom_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string url = SortieExternalLinkService.GenerateOperationRoomUrl(_record!);
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				SetStatus("作戦室 (Jervis) をブラウザで開きました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"ブラウザの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OperationRoomCopy_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string url = SortieExternalLinkService.GenerateOperationRoomUrl(_record!);
				Clipboard.SetText(url);
				SetStatus("作戦室URLをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void SortieJsonCopy_Click(object sender, RoutedEventArgs e)
		{
			if (!ValidateRecord()) return;

			try
			{
				string json = SortieExternalLinkService.ExportSortieRecordJson(_record!, _apiFiles ?? Enumerable.Empty<ApiFile>());
				Clipboard.SetText(json);
				SetStatus("出撃データ(共有JSON)をクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private bool ValidateRecord()
		{
			if (_record == null)
			{
				SetStatus("エラー: 出撃レコード情報がありません。");
				MessageBox.Show(this, "出撃レコード情報が渡されていないため、この操作を実行できません。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}
			return true;
		}

		private void SetStatus(string msg)
		{
			TextStatus.Text = msg;
		}
		#endregion

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}

	public class SortieNodeItemViewModel
	{
		public SortieNodeInfo Model { get; }

		public int CellId => Model.CellId;
		public string CellDisplay => string.IsNullOrEmpty(Model.CellDisplay) ? $"マス #{CellId}" : Model.CellDisplay;
		public string NodeTypeDescription => string.IsNullOrEmpty(Model.NodeTypeDescription)
			? (Model.IsBoss ? "ボス戦闘" : (Model.IsBattleNode ? "通常戦闘" : "非戦闘"))
			: Model.NodeTypeDescription;

		public string WinRank => Model.WinRank;
		public string MvpShipName => string.IsNullOrEmpty(Model.MvpShipName) ? "-" : Model.MvpShipName;
		public string DropShipName => Model.DropShipName;
		public int BaseExp => Model.BaseExp;
		public int AdmiralExp => Model.AdmiralExp;
		public string NonBattleInfo => string.IsNullOrEmpty(Model.NonBattleInfo) ? "（特記事項なし）" : Model.NonBattleInfo;

		public string FriendlyFormationText => FormatFormation(Model.PlayerFormation);
		public string EnemyFormationText => FormatFormation(Model.EnemyFormation);
		public string EngagementFormText => FormatEngagement(Model.EngagementForm);
		public string AirControlText => FormatAirControl(Model.AirControlState);
		public string SmokeText => FormatSmoke(Model.SmokeType);

		public List<BattlePhaseInfo> Phases => Model.Phases ?? new List<BattlePhaseInfo>();

		public List<ShipHpItemViewModel> FriendlyCombinedShips { get; } = new List<ShipHpItemViewModel>();
		public List<ShipHpItemViewModel> EnemyShipsAfter { get; } = new List<ShipHpItemViewModel>();

		public SortieNodeItemViewModel(SortieNodeInfo model)
		{
			Model = model ?? throw new ArgumentNullException(nameof(model));

			if (model.FriendlyShipsAfter != null)
			{
				foreach (var s in model.FriendlyShipsAfter)
				{
					FriendlyCombinedShips.Add(new ShipHpItemViewModel(s));
				}
			}

			if (model.EscortShipsAfter != null && model.EscortShipsAfter.Count > 0)
			{
				foreach (var s in model.EscortShipsAfter)
				{
					var vm = new ShipHpItemViewModel(s);
					vm.Name = $"[随伴] {vm.Name}";
					FriendlyCombinedShips.Add(vm);
				}
			}

			if (model.EnemyShipsAfter != null)
			{
				foreach (var s in model.EnemyShipsAfter)
				{
					EnemyShipsAfter.Add(new ShipHpItemViewModel(s));
				}
			}
		}

		private static string FormatFormation(int id)
		{
			switch (id)
			{
				case 1: return "単縦陣";
				case 2: return "複縦陣";
				case 3: return "輪形陣";
				case 4: return "梯形陣";
				case 5: return "単横陣";
				case 6: return "警戒陣";
				case 11: return "第1警戒(対潜)";
				case 12: return "第2警戒(前方)";
				case 13: return "第3警戒(輪形)";
				case 14: return "第4警戒(戦闘)";
				default: return id > 0 ? $"陣形#{id}" : "-";
			}
		}

		private static string FormatEngagement(int id)
		{
			switch (id)
			{
				case 1: return "同航戦";
				case 2: return "反航戦";
				case 3: return "T字有利";
				case 4: return "T字不利";
				default: return id > 0 ? $"交戦#{id}" : "-";
			}
		}

		private static string FormatAirControl(int id)
		{
			switch (id)
			{
				case 0: return "制空均衡";
				case 1: return "制空権確保";
				case 2: return "航空優勢";
				case 3: return "航空劣勢";
				case 4: return "制空権喪失";
				default: return "-";
			}
		}

		private static string FormatSmoke(int id)
		{
			switch (id)
			{
				case 1: return "煙幕 (一段)";
				case 2: return "煙幕 (二重)";
				case 3: return "煙幕 (三重)";
				default: return "なし";
			}
		}
	}

	public class ShipHpItemViewModel
	{
		public BattleShipSnapshot Model { get; }

		public string Name { get; set; }
		public int Level => Model.Level;
		public int HpBefore => Model.HpBefore;
		public int HpAfter => Model.HpAfter;
		public int HpMax => Model.HpMax;
		public string DamageStateText => string.IsNullOrEmpty(Model.DamageStateText)
			? BattleShipSnapshot.CalculateDamageState(HpAfter, HpMax)
			: Model.DamageStateText;

		public string HpTransitionDisplay => $"{HpBefore} → {HpAfter} / {HpMax}";

		public ShipHpItemViewModel(BattleShipSnapshot model)
		{
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Name = model.Name;
		}
	}
}
