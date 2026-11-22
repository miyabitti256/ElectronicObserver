#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ElectronicObserver.Database;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Services.ApiFileService;
using ElectronicObserver.Window.Tools.SortieRecordViewer.DataExport;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Replay;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.SortieCostViewer;
using ElectronicObserver.Window.Tools.SortieRecordViewer.SortieDetail;
using Microsoft.Win32;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer
{
	/// <summary>
	/// SortieRecordViewerWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class SortieRecordViewerWindow : System.Windows.Window
	{
		private readonly Func<ElectronicObserverDbContext> _contextFactory;
		private List<SortieRecord> _allRecords = new List<SortieRecord>();
		private List<SortieRecordViewModel> _displayedRecords = new List<SortieRecordViewModel>();

		public SortieRecordViewerWindow() : this(() => new ElectronicObserverDbContext())
		{
		}

		public SortieRecordViewerWindow(Func<ElectronicObserverDbContext> contextFactory)
		{
			InitializeComponent();

			_contextFactory = contextFactory ?? (() => new ElectronicObserverDbContext());

			// 期間初期値: 過去30日〜今日
			PickerStartDate.SelectedDate = DateTime.Today.AddDays(-30);
			PickerEndDate.SelectedDate = DateTime.Today;

			Loaded += async (s, e) => await LoadDataAsync();
		}

		private async Task LoadDataAsync()
		{
			SetStatusMessage("データを読み込み中...");

			try
			{
				List<SortieRecord> records = new List<SortieRecord>();
				await Task.Run(() =>
				{
					using (var db = _contextFactory())
					{
						records = db.Sorties.ToList(includeApiFiles: true);
					}
				});

				Dispatcher.Invoke(() =>
				{
					_allRecords = records;
					PopulateFilters();
					ApplyFilter();
					SetStatusMessage("出撃データを読み込みました。");
				});
			}
			catch (Exception ex)
			{
				Dispatcher.Invoke(() =>
				{
					SetStatusMessage($"読み込みエラー: {ex.Message}");
					MessageBox.Show(this, $"データの読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				});
			}
		}

		private void PopulateFilters()
		{
			// 海域 (World) リスト構築
			var worlds = _allRecords.Select(r => r.World).Distinct().OrderBy(w => w).ToList();
			ComboWorld.Items.Clear();
			ComboWorld.Items.Add("* (すべて)");
			foreach (var w in worlds)
			{
				ComboWorld.Items.Add(w.ToString());
			}
			ComboWorld.SelectedIndex = 0;

			// マップ (Map) リスト構築
			var maps = _allRecords.Select(r => r.Map).Distinct().OrderBy(m => m).ToList();
			ComboMap.Items.Clear();
			ComboMap.Items.Add("* (すべて)");
			foreach (var m in maps)
			{
				ComboMap.Items.Add(m.ToString());
			}
			ComboMap.SelectedIndex = 0;
		}

		private void ApplyFilter()
		{
			var sw = Stopwatch.StartNew();

			DateTime startLimit = GetDateTime(PickerStartDate.SelectedDate, TextStartTime.Text, DateTime.MinValue, isEnd: false);
			DateTime endLimit = GetDateTime(PickerEndDate.SelectedDate, TextEndTime.Text, DateTime.MaxValue, isEnd: true);

			string selectedWorld = ComboWorld.SelectedItem?.ToString() ?? "*";
			string selectedMap = ComboMap.SelectedItem?.ToString() ?? "*";
			bool bossOnly = CheckBossOnly.IsChecked == true;

			var filtered = _allRecords.AsEnumerable();

			// 日時フィルタ
			filtered = filtered.Where(r =>
			{
				DateTime startUtc = r.ApiFiles != null && r.ApiFiles.Count > 0
					? r.ApiFiles.Min(f => f.TimeStamp)
					: DateTime.MinValue;
				DateTime startLocal = startUtc.ToLocalTime();
				return startLocal >= startLimit && startLocal <= endLimit;
			});

			// 海域フィルタ
			if (!string.IsNullOrEmpty(selectedWorld) && !selectedWorld.StartsWith("*"))
			{
				if (int.TryParse(selectedWorld, out int w))
				{
					filtered = filtered.Where(r => r.World == w);
				}
			}

			// マップフィルタ
			if (!string.IsNullOrEmpty(selectedMap) && !selectedMap.StartsWith("*"))
			{
				if (int.TryParse(selectedMap, out int m))
				{
					filtered = filtered.Where(r => r.Map == m);
				}
			}

			var vms = filtered.Select(r => new SortieRecordViewModel(r)).ToList();

			// ボス到達フィルタ
			if (bossOnly)
			{
				vms = vms.Where(vm =>
				{
					if (!vm.ReachedBoss.HasValue)
					{
						var detail = SortieNodeEmulator.EmulateSortie(vm.Model, vm.Model.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>());
						vm.ReachedBoss = detail.ReachedBoss;
					}
					return vm.ReachedBoss == true;
				}).ToList();
			}

			// 降順ソート (新しい出撃順)
			_displayedRecords = vms.OrderByDescending(v => v.SortieStartUtc).ThenByDescending(v => v.Id).ToList();
			SortieDataGrid.ItemsSource = _displayedRecords;

			sw.Stop();

			TextStatusCount.Text = $"表示件数: {_displayedRecords.Count} 件 (選択: 0 件)";
			TextStatusTime.Text = $"検索時間: {sw.ElapsedMilliseconds} ms";
		}

		private static DateTime GetDateTime(DateTime? date, string timeText, DateTime defaultTime, bool isEnd = false)
		{
			if (!date.HasValue) return defaultTime;

			if (TimeSpan.TryParse(timeText, out TimeSpan time))
			{
				var dt = date.Value.Date + time;
				// Why not exact second: When seconds are omitted in UI (e.g. 23:59), extend to end of minute (+59.999s) for end bound.
				if (isEnd && time.Seconds == 0 && time.Milliseconds == 0)
				{
					dt = dt.AddMinutes(1).AddTicks(-1);
				}
				return dt;
			}

			return isEnd ? date.Value.Date.AddDays(1).AddTicks(-1) : date.Value.Date;
		}

		private void FilterControl_Changed(object sender, SelectionChangedEventArgs e)
		{
			// ユーザーの操作でコンボボックスが変わった場合に自動検索 (Loaded後)
			if (IsLoaded)
			{
				ApplyFilter();
			}
		}

		private void SearchButton_Click(object sender, RoutedEventArgs e)
		{
			ApplyFilter();
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			PickerStartDate.SelectedDate = DateTime.Today.AddDays(-30);
			TextStartTime.Text = "00:00";
			PickerEndDate.SelectedDate = DateTime.Today;
			TextEndTime.Text = "23:59";
			ComboWorld.SelectedIndex = 0;
			ComboMap.SelectedIndex = 0;
			CheckBossOnly.IsChecked = false;

			ApplyFilter();
		}

		private void SortieDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			int selectedCount = SortieDataGrid.SelectedItems.Count;
			TextStatusCount.Text = $"表示件数: {_displayedRecords.Count} 件 (選択: {selectedCount} 件)";
		}

		private void SortieDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			OpenSelectedDetail();
		}

		private void OpenSelectedDetail()
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			var detailWin = new SortieDetailWindow(selected.Model, selected.Model.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>())
			{
				Owner = this,
			};
			detailWin.Show();
		}

		private void Context_OpenDetail_Click(object sender, RoutedEventArgs e)
		{
			OpenSelectedDetail();
		}

		private void Context_OpenCost_Click(object sender, RoutedEventArgs e)
		{
			var selectedRecords = SortieDataGrid.SelectedItems.OfType<SortieRecordViewModel>()
				.Select(v => v.Model)
				.ToList();

			if (selectedRecords.Count == 0)
			{
				MessageBox.Show(this, "出撃が選択されていません。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var costWin = new SortieCostViewerWindow(selectedRecords)
			{
				Owner = this,
			};
			costWin.Show();
		}

		#region リプレイ連携
		private void Context_OpenReplayUrl_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string url = ReplayService.GenerateReplayUrl(selected.Model, selected.Model.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>());
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				SetStatusMessage("リプレイURLをブラウザで開きました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"ブラウザの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Context_CopyReplayUrl_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string url = ReplayService.GenerateReplayUrl(selected.Model, selected.Model.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>());
				Clipboard.SetText(url);
				SetStatusMessage("リプレイURLをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Context_CopyReplayJson_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string json = ReplayService.GenerateReplayJson(selected.Model, selected.Model.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>());
				Clipboard.SetText(json);
				SetStatusMessage("リプレイJSONをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		#region 外部連携
		private void Context_OpenKcWeb_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string url = SortieExternalLinkService.GenerateAirControlSimulatorUrl(selected.Model);
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				SetStatusMessage("制空権シミュレータをブラウザで開きました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"ブラウザの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Context_CopyKcWebUrl_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string url = SortieExternalLinkService.GenerateAirControlSimulatorUrl(selected.Model);
				Clipboard.SetText(url);
				SetStatusMessage("制空権シミュレータURLをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Context_OpenJervis_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string url = SortieExternalLinkService.GenerateOperationRoomUrl(selected.Model);
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				SetStatusMessage("作戦室 (Jervis) をブラウザで開きました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"ブラウザの起動に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Context_CopyJervisUrl_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string url = SortieExternalLinkService.GenerateOperationRoomUrl(selected.Model);
				Clipboard.SetText(url);
				SetStatusMessage("作戦室URLをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Context_CopyDeckBuilder_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string json = SortieExternalLinkService.GenerateDeckBuilderJson(selected.Model);
				Clipboard.SetText(json);
				SetStatusMessage("デッキビルダー形式JSONをクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		#region データ出力 & インポート
		private void Context_CopySortieJson_Click(object sender, RoutedEventArgs e)
		{
			var selected = SortieDataGrid.SelectedItem as SortieRecordViewModel;
			if (selected == null) return;

			try
			{
				string json = SortieExternalLinkService.ExportSortieRecordJson(selected.Model, selected.Model.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>());
				Clipboard.SetText(json);
				SetStatusMessage("出撃データ(共有JSON)をクリップボードにコピーしました。");
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async void MenuFile_ImportJson_Click(object sender, RoutedEventArgs e)
		{
			string json;
			try
			{
				json = Clipboard.GetText();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"クリップボードの取得に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (string.IsNullOrWhiteSpace(json))
			{
				MessageBox.Show(this, "クリップボードが空です。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try
			{
				var (record, apiFiles) = SortieExternalLinkService.ImportSortieRecordJson(json);

				using (var db = _contextFactory())
				{
					// レコードを新規保存
					record.Id = 0; // 自動採番
					db.Sorties.Add(record);
					db.SaveChanges();

					foreach (var file in apiFiles)
					{
						file.Id = 0;
						file.SortieRecordId = record.Id;
						db.ApiFiles.Add(file);
					}
					db.SaveChanges();
				}

				SetStatusMessage($"出撃データをインポートしました (海域: {record.World}-{record.Map})。");
				await LoadDataAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"出撃データのインポートに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Context_ExportApiList_Click(object sender, RoutedEventArgs e)
		{
			ExportApiListSelected();
		}

		private void MenuFile_ExportApiList_Click(object sender, RoutedEventArgs e)
		{
			ExportApiListSelected();
		}

		private void ExportApiListSelected()
		{
			var selected = SortieDataGrid.SelectedItems.OfType<SortieRecordViewModel>().ToList();
			if (selected.Count == 0)
			{
				MessageBox.Show(this, "エクスポート対象の出撃が選択されていません。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
			{
				fbd.Description = "APIリスト形式 (LoadAPIList互換) の出力先フォルダを選択してください";
				if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
				{
					return;
				}

				try
				{
					foreach (var item in selected)
					{
						string destDir = selected.Count == 1
							? fbd.SelectedPath
							: Path.Combine(fbd.SelectedPath, $"Sortie_{item.Id}_{item.World}-{item.Map}_{item.SortieStartLocal:yyyyMMdd_HHmmss}");

						ApiFileService.ExportToApiList(destDir, item.Model.ApiFiles ?? Enumerable.Empty<Database.KancolleApi.ApiFile>());
					}

					SetStatusMessage($"{selected.Count} 件の出撃を API リスト形式でエクスポートしました。");
					MessageBox.Show(this, "エクスポートが完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, $"エクスポートに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion

		#region CSVエクスポート
		private void QuickCsvButton_Click(object sender, RoutedEventArgs e)
		{
			string? tag = (sender as FrameworkElement)?.Tag?.ToString();
			if (string.IsNullOrEmpty(tag) || !Enum.TryParse<SortieCsvExportType>(tag, out var exportType))
			{
				return;
			}

			// 選択された出撃があれば選択、なければ現在表示されている全出撃
			var targets = SortieDataGrid.SelectedItems.Count > 0
				? SortieDataGrid.SelectedItems.OfType<SortieRecordViewModel>().Select(v => (v.Model, (IEnumerable<Database.KancolleApi.ApiFile>)(v.Model.ApiFiles ?? new List<Database.KancolleApi.ApiFile>()))).ToList()
				: _displayedRecords.Select(v => (v.Model, (IEnumerable<Database.KancolleApi.ApiFile>)(v.Model.ApiFiles ?? new List<Database.KancolleApi.ApiFile>()))).ToList();

			if (targets.Count == 0)
			{
				MessageBox.Show(this, "エクスポート対象のデータがありません。", "案内", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var sfd = new SaveFileDialog
			{
				Title = $"{tag} CSV エクスポート",
				Filter = "CSVファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
				FileName = $"{tag}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
			};

			if (sfd.ShowDialog(this) == true)
			{
				try
				{
					string csv = SortieCsvExportService.ExportCsv(exportType, targets);
					File.WriteAllText(sfd.FileName, csv, Encoding.UTF8);
					SetStatusMessage($"{targets.Count} 件の出撃データを '{Path.GetFileName(sfd.FileName)}' に保存しました。");
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, $"CSVエクスポートに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion

		#region メニュー & 表示切り替え
		private void MenuView_ShowQuickCsvBar_Click(object sender, RoutedEventArgs e)
		{
			QuickCsvBorder.Visibility = MenuView_ShowQuickCsvBar.IsChecked
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		private async void MenuTool_Reload_Click(object sender, RoutedEventArgs e)
		{
			await LoadDataAsync();
		}

		private void MenuFile_Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void SetStatusMessage(string message)
		{
			if (Dispatcher.CheckAccess())
			{
				TextStatusMessage.Text = message;
			}
			else
			{
				Dispatcher.Invoke(() => TextStatusMessage.Text = message);
			}
		}
		#endregion
	}
}
