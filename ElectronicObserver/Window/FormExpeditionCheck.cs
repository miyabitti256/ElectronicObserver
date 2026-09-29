using ElectronicObserver.Data;
using ElectronicObserver.Observer;
using ElectronicObserver.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace ElectronicObserver.Window
{
	/// <summary>
	/// 遠征可否チェックおよび大成功判定を行うドッキングウィンドウです。
	/// </summary>
	public partial class FormExpeditionCheck : DockContent
	{
		private FormMain _parent;
		private bool _isLoaded;
		private int[] _targetMissions = new int[5]; // 艦隊ごとの設定遠征ID (1-based, 2..4)

		public FormExpeditionCheck(FormMain parent)
		{
			InitializeComponent();
			_parent = parent;
			Icon = ResourceManager.ImageToIcon(ResourceManager.Instance.Icons.Images[(int)ResourceManager.IconContent.FormExpeditionCheck]);

			// デフォルト遠征の初期化 (第2: 2(長距離), 第3: 5(海上護衛), 第4: 38(東急2))
			_targetMissions[2] = 2;
			_targetMissions[3] = 5;
			_targetMissions[4] = 38;

			SubscribeToApis();
		}

		private void SubscribeToApis()
		{
			APIObserver o = APIObserver.Instance;

			void UpdateHandler(string apiname, dynamic data)
			{
				if (InvokeRequired)
				{
					BeginInvoke(new Action(() => UpdateAllViews()));
				}
				else
				{
					UpdateAllViews();
				}
			}

			void SubscribeResponse(string apiName, APIReceivedEventHandler handler)
			{
				var api = o[apiName];
				if (api != null)
				{
					api.ResponseReceived += handler;
				}
			}

			void SubscribeRequest(string apiName, APIReceivedEventHandler handler)
			{
				var api = o[apiName];
				if (api != null)
				{
					api.RequestReceived += handler;
				}
			}

			SubscribeResponse("api_start2/getData", UpdateHandler);
			SubscribeResponse("api_port/port", UpdateHandler);
			SubscribeResponse("api_get_member/ship2", UpdateHandler);
			SubscribeResponse("api_get_member/ship3", UpdateHandler);
			SubscribeResponse("api_get_member/slot_item", UpdateHandler);
			SubscribeResponse("api_req_hensei/change", UpdateHandler);
			SubscribeResponse("api_req_hensei/preset_select", UpdateHandler);
			SubscribeResponse("api_req_kaisou/slot_deprive", UpdateHandler);
			SubscribeResponse("api_req_kaisou/slot_exchange_index", UpdateHandler);
			SubscribeResponse("api_req_kaisou/powerup", UpdateHandler);
			SubscribeResponse("api_req_kaisou/remodeling", UpdateHandler);
			SubscribeResponse("api_req_hokyu/charge", UpdateHandler);
			SubscribeResponse("api_req_nyukyo/start", UpdateHandler);
			SubscribeResponse("api_req_mission/result", UpdateHandler);

			// 遠征画面を開いた瞬間 (api_get_member/mission) のチェック警告
			SubscribeResponse("api_get_member/mission", (apiname, data) =>
			{
				if (InvokeRequired)
					BeginInvoke(new Action(CheckOnMissionScreenOpened));
				else
					CheckOnMissionScreenOpened();
			});

			// 遠征出発時 (api_req_mission/start) のチェック警告
			SubscribeRequest("api_req_mission/start", (apiname, data) =>
			{
				try
				{
					int fleetId = int.Parse(data["api_deck_id"]);
					int missionId = int.Parse(data["api_mission_id"]);

					if (InvokeRequired)
						BeginInvoke(new Action(() => CheckOnMissionStart(fleetId, missionId)));
					else
						CheckOnMissionStart(fleetId, missionId);
				}
				catch { }
			});
		}

		private void FormExpeditionCheck_Load(object sender, EventArgs e)
		{
			try
			{
				InitAreaAndMissionList();
				comboFleet.SelectedIndex = 0; // 第2艦隊

				var config = Utility.Configuration.Config.FormExpeditionCheck;
				if (config != null)
				{
					checkAlert.Checked = config.AlertOnMissionScreenOpened;
					checkAlertSupply.Checked = config.AlertSupplyDepleted;
				}

				_isLoaded = true;
				SelectMission(_targetMissions[SelectedFleetId]);
				UpdateControlPanelLayout();
				UpdateResponsiveLayout();
				UpdateAllViews();
			}
			catch (Exception ex)
			{
				Utility.Logger.Add(3, "遠征可否ウィンドウの初期化でエラーが発生しました: " + ex.Message);
			}
		}

		private void checkAlert_CheckedChanged(object sender, EventArgs e)
		{
			if (!_isLoaded) return;
			var config = Utility.Configuration.Config.FormExpeditionCheck;
			if (config != null)
			{
				config.AlertOnMissionScreenOpened = checkAlert.Checked;
			}
		}

		private void checkAlertSupply_CheckedChanged(object sender, EventArgs e)
		{
			if (!_isLoaded) return;
			var config = Utility.Configuration.Config.FormExpeditionCheck;
			if (config != null)
			{
				config.AlertSupplyDepleted = checkAlertSupply.Checked;
			}
		}

		private void FormExpeditionCheck_Resize(object sender, EventArgs e)
		{
			try
			{
				UpdateResponsiveLayout();
			}
			catch { }
		}

		private void panelControl_Resize(object sender, EventArgs e)
		{
			try
			{
				UpdateControlPanelLayout();
			}
			catch { }
		}

		/// <summary>
		/// ウィンドウサイズに応じたレスポンシブな分割レイアウト計算を行います。
		/// </summary>
		private void UpdateResponsiveLayout()
		{
			try
			{
				if (splitDetail == null || tabControl.SelectedTab != tabDetail) return;

				int w = tabDetail.ClientSize.Width;
				int h = tabDetail.ClientSize.Height - panelControl.Height;
				if (w <= 0 || h <= 0) return;

				// 横幅が十分広い (>= 500px) かつ縦横比で横長の場合は左右分割、それ以外（基本的に小さい・細長いウィンドウ）は上下分割
				if (w >= 500 && w > h * 1.2)
				{
					if (splitDetail.Orientation != Orientation.Vertical)
					{
						splitDetail.Orientation = Orientation.Vertical;
					}

					int min = Math.Max(0, splitDetail.Panel1MinSize);
					int max = splitDetail.Width - splitDetail.SplitterWidth - Math.Max(0, splitDetail.Panel2MinSize);
					if (max > min)
					{
						int desiredDistance = Math.Min(240, Math.Max(180, (int)(w * 0.38)));
						splitDetail.SplitterDistance = Math.Max(min, Math.Min(max, desiredDistance));
					}
				}
				else
				{
					if (splitDetail.Orientation != Orientation.Horizontal)
					{
						splitDetail.Orientation = Orientation.Horizontal;
					}

					int min = Math.Max(0, splitDetail.Panel1MinSize);
					int max = splitDetail.Height - splitDetail.SplitterWidth - Math.Max(0, splitDetail.Panel2MinSize);
					if (max > min)
					{
						// 上部の概要パネルは必要最小限 (115px 前後) に留め、下のチェックリストに画面領域を最大限配分する
						int desiredDistance = Math.Min(125, Math.Max(95, (int)(h * 0.35)));
						splitDetail.SplitterDistance = Math.Max(min, Math.Min(max, desiredDistance));
					}
				}
			}
			catch
			{
				// SplitContainerの寸法変更時のWinForms例外を遮断
			}
		}

		/// <summary>
		/// 操作パネル（艦隊・海域・遠征コンボ、警告チェック）のリサイズ計算を行います。
		/// </summary>
		private void UpdateControlPanelLayout()
		{
			int w = panelControl.ClientSize.Width;
			if (w <= 0) return;

			panelControl.SuspendLayout();

			if (w >= 560)
			{
				// 1行配置
				panelControl.Height = 32;

				labelFleet.Location = new Point(4, 8);
				comboFleet.Location = new Point(36, 5);
				comboFleet.Width = 62;

				labelArea.Location = new Point(104, 8);
				comboArea.Location = new Point(136, 5);
				comboArea.Width = 85;

				labelMission.Location = new Point(227, 8);
				int missionLeft = 258;
				int alertSupplyWidth = 95;
				int alertWidth = 135;
				int missionWidth = Math.Max(90, w - missionLeft - alertWidth - alertSupplyWidth - 15);
				comboMission.Location = new Point(missionLeft, 5);
				comboMission.Width = missionWidth;

				checkAlert.Location = new Point(w - alertWidth - alertSupplyWidth - 8, 8);
				checkAlert.Size = new Size(alertWidth, 16);

				checkAlertSupply.Location = new Point(w - alertSupplyWidth - 4, 8);
				checkAlertSupply.Size = new Size(alertSupplyWidth, 16);
			}
			else
			{
				// 小さいウィンドウ用の2行配置
				panelControl.Height = 52;

				// 1行目: [艦隊] [海域] [遠征]
				labelFleet.Location = new Point(3, 6);
				comboFleet.Location = new Point(34, 3);
				comboFleet.Width = 58;

				labelArea.Location = new Point(96, 6);
				comboArea.Location = new Point(127, 3);
				comboArea.Width = 78;

				labelMission.Location = new Point(210, 6);
				int missionLeft = 241;
				comboMission.Location = new Point(missionLeft, 3);
				comboMission.Width = Math.Max(70, w - missionLeft - 4);

				// 2行目: [遠征画面表示時に警告] [未補給も警告]
				checkAlert.Location = new Point(4, 29);
				checkAlert.Size = new Size(135, 16);

				checkAlertSupply.Location = new Point(144, 29);
				checkAlertSupply.Size = new Size(95, 16);
			}

			panelControl.ResumeLayout();
		}

		private void InitAreaAndMissionList()
		{
			var db = KCDatabase.Instance;
			if (!db.Mission.Any()) return;

			comboArea.Items.Clear();
			var areas = db.Mission.Values
				.Select(m => m.MapAreaID)
				.Distinct()
				.OrderBy(a => a)
				.ToList();

			foreach (var areaId in areas)
			{
				string name = db.MapArea.ContainsKey(areaId) ? db.MapArea[areaId].Name : $"海域{areaId}";
				comboArea.Items.Add(new KeyValuePair<int, string>(areaId, $"[{areaId}] {name}"));
			}

			comboArea.DisplayMember = "Value";
			comboArea.ValueMember = "Key";

			if (comboArea.Items.Count > 0)
				comboArea.SelectedIndex = 0;
		}

		private void UpdateMissionCombo()
		{
			if (comboArea.SelectedItem == null) return;
			var areaId = ((KeyValuePair<int, string>)comboArea.SelectedItem).Key;
			var db = KCDatabase.Instance;

			comboMission.Items.Clear();
			var missions = db.Mission.Values
				.Where(m => m.MapAreaID == areaId)
				.OrderBy(m => m.MissionID)
				.ToList();

			foreach (var m in missions)
			{
				comboMission.Items.Add(new KeyValuePair<int, string>(m.MissionID, $"{m.DisplayID} {m.Name}"));
			}

			comboMission.DisplayMember = "Value";
			comboMission.ValueMember = "Key";

			int selectedFleet = SelectedFleetId;
			int targetId = _targetMissions[selectedFleet];
			int foundIndex = -1;

			for (int i = 0; i < comboMission.Items.Count; i++)
			{
				if (((KeyValuePair<int, string>)comboMission.Items[i]).Key == targetId)
				{
					foundIndex = i;
					break;
				}
			}

			if (foundIndex >= 0)
				comboMission.SelectedIndex = foundIndex;
			else if (comboMission.Items.Count > 0)
				comboMission.SelectedIndex = 0;
		}

		private int SelectedFleetId => comboFleet.SelectedIndex + 2;

		private int SelectedMissionId
		{
			get
			{
				if (comboMission.SelectedItem == null) return 1;
				return ((KeyValuePair<int, string>)comboMission.SelectedItem).Key;
			}
		}

		private void comboFleet_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_isLoaded) return;
			int targetId = _targetMissions[SelectedFleetId];
			SelectMission(targetId);
			UpdateDetailView();
		}

		private void comboArea_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateMissionCombo();
			UpdateDetailView();
		}

		private void comboMission_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboMission.SelectedItem == null) return;
			_targetMissions[SelectedFleetId] = SelectedMissionId;
			UpdateDetailView();
		}

		public void SelectMission(int missionId)
		{
			var db = KCDatabase.Instance;
			if (!db.Mission.ContainsKey(missionId)) return;
			var mission = db.Mission[missionId];

			for (int i = 0; i < comboArea.Items.Count; i++)
			{
				if (((KeyValuePair<int, string>)comboArea.Items[i]).Key == mission.MapAreaID)
				{
					comboArea.SelectedIndex = i;
					break;
				}
			}

			for (int i = 0; i < comboMission.Items.Count; i++)
			{
				if (((KeyValuePair<int, string>)comboMission.Items[i]).Key == missionId)
				{
					comboMission.SelectedIndex = i;
					break;
				}
			}
		}

		public void UpdateAllViews()
		{
			if (!_isLoaded) return;
			if (comboArea.Items.Count == 0)
			{
				InitAreaAndMissionList();
				SelectMission(_targetMissions[SelectedFleetId]);
			}
			UpdateDetailView();
			UpdateMatrixView();
		}

		/// <summary>
		/// 詳細チェックビューを更新します。
		/// </summary>
		private void UpdateDetailView()
		{
			var db = KCDatabase.Instance;
			int missionId = SelectedMissionId;
			if (!db.Mission.ContainsKey(missionId)) return;

			var mission = db.Mission[missionId];
			int fleetId = SelectedFleetId;
			var fleet = db.Fleet[fleetId];

			// 1. 遠征基本情報
			labelTime.Text = $"{mission.Time / 60:D2}:{mission.Time % 60:D2}";
			labelCost.Text = $"燃 {(int)(mission.Fuel * 100)}% / 弾 {(int)(mission.Ammo * 100)}%";
			labelReward.Text = string.IsNullOrEmpty(mission.Detail) ? "-" : mission.Detail;
			labelItem.Text = mission.Cancelable ? "強制帰投可能" : "強制帰投不可";

			// 2. 大成功情報
			var gsType = ExpeditionHelper.GetGreatSuccessType(missionId);
			switch (gsType)
			{
				case ExpeditionGreatSuccessType.Regular:
					labelGSType.Text = "通常型 (全員キラ必須)";
					break;
				case ExpeditionGreatSuccessType.Drum:
					labelGSType.Text = "ドラム缶型 (缶ボーナス)";
					break;
				case ExpeditionGreatSuccessType.Level:
					labelGSType.Text = "旗艦Lv型 (Lvボーナス)";
					break;
			}

			double gsRate = ExpeditionHelper.CalculateGreatSuccessRate(fleet, missionId);
			labelGSRate.Text = $"{gsRate:P1}";
			labelGSRate.ForeColor = gsRate > 0.8 ? Color.Green : (gsRate > 0.4 ? Color.DarkGoldenrod : Color.Firebrick);

			int sparkleCount = fleet?.MembersInstance.Count(s => s != null && s.Condition > 49) ?? 0;
			int totalShips = fleet?.MembersInstance.Count(s => s != null) ?? 0;
			int drumCount = fleet?.MembersInstance.Where(s => s != null).Sum(s => s.AllSlotInstance.Count(e => e != null && e.MasterEquipment.CategoryType == EquipmentTypes.TransportContainer)) ?? 0;
			int flagshipLv = fleet?.MembersInstance.FirstOrDefault(s => s != null)?.Level ?? 0;

			labelGSDetail.Text = $"キラ: {sparkleCount}/{totalShips} 隻 | 缶: {drumCount} 個 | 旗艦: Lv{flagshipLv}";
			labelGSDesc.Text = ExpeditionHelper.GetGreatSuccessConditionDescription(missionId);

			// 3. 成功要件詳細リスト
			gridConditions.SuspendLayout();
			gridConditions.Rows.Clear();

			var result = MissionClearCondition.Check(missionId, fleet);

			// 未補給チェック
			bool isFuelEmpty = fleet?.MembersInstance.Any(s => s != null && s.FuelRate < 1) ?? false;
			bool isAmmoEmpty = fleet?.MembersInstance.Any(s => s != null && s.AmmoRate < 1) ?? false;
			if (isFuelEmpty || isAmmoEmpty)
			{
				int r = gridConditions.Rows.Add("×", "補給状態", "燃料・弾薬100%", isFuelEmpty && isAmmoEmpty ? "燃・弾未補給" : (isFuelEmpty ? "燃料未補給" : "弾薬未補給"));
				gridConditions.Rows[r].DefaultCellStyle.BackColor = Color.MistyRose;
				gridConditions.Rows[r].DefaultCellStyle.ForeColor = Color.DarkRed;
				gridConditions.Rows[r].DefaultCellStyle.Font = new Font(gridConditions.Font, FontStyle.Bold);
			}
			else
			{
				gridConditions.Rows.Add("○", "補給状態", "燃料・弾薬100%", "補給完了");
			}

			foreach (var detail in result.Details)
			{
				string status = detail.IsSatisfied ? "○" : "×";
				int r = gridConditions.Rows.Add(status, detail.ItemName, detail.RequiredValue, detail.CurrentValue);

				if (!detail.IsSatisfied)
				{
					gridConditions.Rows[r].DefaultCellStyle.BackColor = Color.MistyRose;
					gridConditions.Rows[r].DefaultCellStyle.ForeColor = Color.DarkRed;
					gridConditions.Rows[r].DefaultCellStyle.Font = new Font(gridConditions.Font, FontStyle.Bold);
				}
			}

			gridConditions.ResumeLayout();
		}

		/// <summary>
		/// 全体マトリクスビューを更新します。
		/// </summary>
		private void UpdateMatrixView()
		{
			var db = KCDatabase.Instance;
			if (!db.Mission.Any()) return;

			int displayedRow = matrixGrid.FirstDisplayedScrollingRowIndex;
			int selectedRow = matrixGrid.SelectedRows.OfType<DataGridViewRow>().FirstOrDefault()?.Index ?? -1;

			matrixGrid.SuspendLayout();
			matrixGrid.Rows.Clear();

			var defaultStyle = matrixGrid.RowsDefaultCellStyle;
			var failedStyle = defaultStyle.Clone();
			failedStyle.BackColor = Color.MistyRose;
			failedStyle.SelectionBackColor = Color.Brown;

			var rows = new List<DataGridViewRow>(db.Mission.Count);

			foreach (var mission in db.Mission.Values)
			{
				var results = new[]
				{
					MissionClearCondition.Check(mission.MissionID, db.Fleet[1]),
					MissionClearCondition.Check(mission.MissionID, db.Fleet[2]),
					MissionClearCondition.Check(mission.MissionID, db.Fleet[3]),
					MissionClearCondition.Check(mission.MissionID, db.Fleet[4]),
					MissionClearCondition.Check(mission.MissionID, null),
				};

				var row = new DataGridViewRow();
				row.CreateCells(matrixGrid);
				row.SetValues(
					mission.MissionID,
					mission.MissionID,
					results[0],
					results[1],
					results[2],
					results[3],
					results[4]);

				row.Cells[0].ToolTipText = $"ID: {mission.MissionID}";

				for (int i = 0; i < 5; i++)
				{
					var res = results[i];
					var cell = row.Cells[i + 2];

					if (res.IsSuceeded || i == 4)
					{
						if (!res.FailureReason.Any())
						{
							double rate = (i < 4 && db.Fleet[i + 1] != null) ? ExpeditionHelper.CalculateGreatSuccessRate(db.Fleet[i + 1], mission.MissionID) : 0.0;
							cell.Value = rate > 0 ? $"○ ({rate:P0})" : "○";
						}
						else
						{
							cell.Value = string.Join(", ", res.FailureReason);
						}
						cell.Style = defaultStyle;
					}
					else
					{
						cell.Value = string.Join(", ", res.FailureReason);
						cell.Style = failedStyle;
					}
				}

				rows.Add(row);
			}

			matrixGrid.Rows.AddRange(rows.ToArray());
			matrixGrid.Sort(matrixColID, ListSortDirection.Ascending);

			if (0 <= displayedRow && displayedRow < matrixGrid.RowCount)
				matrixGrid.FirstDisplayedScrollingRowIndex = displayedRow;
			if (0 <= selectedRow && selectedRow < matrixGrid.RowCount)
			{
				matrixGrid.ClearSelection();
				matrixGrid.Rows[selectedRow].Selected = true;
			}

			matrixGrid.ResumeLayout();
		}

		private void matrixGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.ColumnIndex == matrixColName.Index)
			{
				e.Value = KCDatabase.Instance.Mission[(int)e.Value].Name;
				e.FormattingApplied = true;
			}
			else if (e.ColumnIndex == matrixColID.Index)
			{
				var mission = KCDatabase.Instance.Mission[(int)e.Value];
				e.Value = $"{mission.DisplayID}:{KCDatabase.Instance.MapArea[mission.MapAreaID].Name}";
				e.FormattingApplied = true;
			}
		}

		private void matrixGrid_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
		{
			if (e.Column.Index == matrixColName.Index || e.Column.Index == matrixColID.Index)
			{
				var m1 = KCDatabase.Instance.Mission[(int)e.CellValue1];
				var m2 = KCDatabase.Instance.Mission[(int)e.CellValue2];

				int diff = m1.MapAreaID - m2.MapAreaID;
				if (diff == 0)
					diff = m1.MissionID - m2.MissionID;

				e.SortResult = diff;
				e.Handled = true;
			}
		}

		private void matrixGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;
			int missionId = (int)matrixGrid.Rows[e.RowIndex].Cells[matrixColID.Index].Value;

			// 詳細タブに切り替えて対象遠征を選択
			SelectMission(missionId);
			tabControl.SelectedTab = tabDetail;
		}

		/// <summary>
		/// 遠征画面を開いた瞬間の警告チェック (api_get_member/mission)
		/// </summary>
		private void CheckOnMissionScreenOpened()
		{
			if (!checkAlert.Checked) return;

			var db = KCDatabase.Instance;
			var warnings = new List<string>();

			for (int fleetId = 2; fleetId <= 4; fleetId++)
			{
				var fleet = db.Fleet[fleetId];
				if (fleet == null || fleet.MembersInstance.All(s => s == null)) continue;
				if (fleet.ExpeditionState != 0) continue; // 既に遠征中の艦隊は除外

				int missionId = _targetMissions[fleetId];
				if (!db.Mission.ContainsKey(missionId)) continue;

				var mission = db.Mission[missionId];
				var result = MissionClearCondition.Check(missionId, fleet);

				bool isFuelEmpty = fleet.MembersInstance.Any(s => s != null && s.FuelRate < 1);
				bool isAmmoEmpty = fleet.MembersInstance.Any(s => s != null && s.AmmoRate < 1);
				bool isSupplyEmpty = isFuelEmpty || isAmmoEmpty;

				// 未補給のみで、かつ「未補給も警告」がOFFの場合はモーダル警告の対象外（緊急補給で対応可能）
				if (!result.IsSuceeded || (isSupplyEmpty && checkAlertSupply.Checked))
				{
					var reasons = new List<string>(result.FailureReason);
					if (isSupplyEmpty)
						reasons.Add("燃料/弾薬が未補給です");

					warnings.Add($"【第{fleetId}艦隊: {fleet.Name}】\r\n設定遠征: [{mission.DisplayID}] {mission.Name}\r\n失敗原因: {string.Join(", ", reasons)}");
				}
				else if (isSupplyEmpty && !checkAlertSupply.Checked)
				{
					// 未補給のみで警告OFFの場合は情報ログのみ記録
					Utility.Logger.Add(2, $"遠征確認: 第{fleetId}艦隊は未補給ですが、編成条件を満たしています（緊急補給可能）。");
				}
			}

			if (warnings.Any())
			{
				foreach (var w in warnings)
				{
					Utility.Logger.Add(3, "遠征警告: " + w.Replace("\r\n", " "));
				}

				System.Media.SystemSounds.Exclamation.Play();
				MessageBox.Show(
					"遠征条件を満たしていない艦隊があります！\r\n出撃前に編成や補給を確認してください。\r\n\r\n" + string.Join("\r\n\r\n", warnings),
					"遠征可否チェック警告",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}

		/// <summary>
		/// 遠征出撃時のチェック (api_req_mission/start)
		/// </summary>
		private void CheckOnMissionStart(int fleetId, int missionId)
		{
			var db = KCDatabase.Instance;
			var fleet = db.Fleet[fleetId];
			if (fleet == null || !db.Mission.ContainsKey(missionId)) return;

			var mission = db.Mission[missionId];
			var result = MissionClearCondition.Check(missionId, fleet);

			bool isFuelEmpty = fleet.MembersInstance.Any(s => s != null && s.FuelRate < 1);
			bool isAmmoEmpty = fleet.MembersInstance.Any(s => s != null && s.AmmoRate < 1);

			if (!result.IsSuceeded || isFuelEmpty || isAmmoEmpty)
			{
				var reasons = new List<string>(result.FailureReason);
				if (isFuelEmpty || isAmmoEmpty)
					reasons.Add("燃料/弾薬未補給");

				string msg = $"⚠️ 出発警告: 第{fleetId}艦隊が遠征 [{mission.DisplayID}] {mission.Name} の条件を満たしていません！ 理由: {string.Join(", ", reasons)}";
				Utility.Logger.Add(3, msg);

				System.Media.SystemSounds.Hand.Play();
				MessageBox.Show(
					$"⚠️ 出発警告: 第{fleetId}艦隊が遠征 [{mission.DisplayID}] {mission.Name} の条件を満たしていません！\r\n\r\n理由: {string.Join(", ", reasons)}",
					"遠征失敗警告",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		protected override string GetPersistString()
		{
			return "ExpeditionCheck";
		}
	}
}
