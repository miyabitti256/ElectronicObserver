namespace ElectronicObserver.Window
{
	partial class FormExpeditionCheck
	{
		/// <summary>
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabDetail = new System.Windows.Forms.TabPage();
			this.splitDetail = new System.Windows.Forms.SplitContainer();
			this.groupSummary = new System.Windows.Forms.GroupBox();
			this.tableSummary = new System.Windows.Forms.TableLayoutPanel();
			this.labelTime = new System.Windows.Forms.Label();
			this.labelCost = new System.Windows.Forms.Label();
			this.labelGSRate = new System.Windows.Forms.Label();
			this.labelGSDetail = new System.Windows.Forms.Label();
			this.groupConditions = new System.Windows.Forms.GroupBox();
			this.gridConditions = new System.Windows.Forms.DataGridView();
			this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colRequirement = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.panelControl = new System.Windows.Forms.Panel();
			this.checkAlert = new System.Windows.Forms.CheckBox();
			this.checkAlertSupply = new System.Windows.Forms.CheckBox();
			this.comboMission = new System.Windows.Forms.ComboBox();
			this.labelMission = new System.Windows.Forms.Label();
			this.comboArea = new System.Windows.Forms.ComboBox();
			this.labelArea = new System.Windows.Forms.Label();
			this.comboFleet = new System.Windows.Forms.ComboBox();
			this.labelFleet = new System.Windows.Forms.Label();
			this.tabMatrix = new System.Windows.Forms.TabPage();
			this.matrixGrid = new System.Windows.Forms.DataGridView();
			this.matrixColID = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.matrixColName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.matrixColFleet1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.matrixColFleet2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.matrixColFleet3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.matrixColFleet4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.matrixColCondition = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.tabControl.SuspendLayout();
			this.tabDetail.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitDetail)).BeginInit();
			this.splitDetail.Panel1.SuspendLayout();
			this.splitDetail.Panel2.SuspendLayout();
			this.splitDetail.SuspendLayout();
			this.groupSummary.SuspendLayout();
			this.tableSummary.SuspendLayout();
			this.groupConditions.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.gridConditions)).BeginInit();
			this.panelControl.SuspendLayout();
			this.tabMatrix.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.matrixGrid)).BeginInit();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Controls.Add(this.tabDetail);
			this.tabControl.Controls.Add(this.tabMatrix);
			this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(640, 480);
			this.tabControl.TabIndex = 0;
			// 
			// tabDetail
			// 
			this.tabDetail.AutoScroll = true;
			this.tabDetail.Controls.Add(this.splitDetail);
			this.tabDetail.Controls.Add(this.panelControl);
			this.tabDetail.Location = new System.Drawing.Point(4, 22);
			this.tabDetail.Name = "tabDetail";
			this.tabDetail.Padding = new System.Windows.Forms.Padding(3);
			this.tabDetail.Size = new System.Drawing.Size(632, 454);
			this.tabDetail.TabIndex = 0;
			this.tabDetail.Text = "詳細チェック";
			this.tabDetail.UseVisualStyleBackColor = true;
			// 
			// splitDetail
			// 
			this.splitDetail.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitDetail.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitDetail.IsSplitterFixed = true;
			this.splitDetail.Location = new System.Drawing.Point(3, 55);
			this.splitDetail.Name = "splitDetail";
			this.splitDetail.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitDetail.Panel1
			// 
			this.splitDetail.Panel1.Controls.Add(this.groupSummary);
			this.splitDetail.Panel1MinSize = 0;
			// 
			// splitDetail.Panel2
			// 
			this.splitDetail.Panel2.Controls.Add(this.groupConditions);
			this.splitDetail.Panel2MinSize = 0;
			this.splitDetail.Size = new System.Drawing.Size(626, 396);
			this.splitDetail.SplitterDistance = 62;
			this.splitDetail.TabIndex = 1;
			// 
			// groupSummary
			// 
			this.groupSummary.Controls.Add(this.tableSummary);
			this.groupSummary.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupSummary.Location = new System.Drawing.Point(0, 0);
			this.groupSummary.Margin = new System.Windows.Forms.Padding(0);
			this.groupSummary.Name = "groupSummary";
			this.groupSummary.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.groupSummary.Size = new System.Drawing.Size(626, 62);
			this.groupSummary.TabIndex = 0;
			this.groupSummary.TabStop = false;
			this.groupSummary.Text = "遠征・大成功概要";
			// 
			// tableSummary
			// 
			this.tableSummary.ColumnCount = 3;
			this.tableSummary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28F));
			this.tableSummary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
			this.tableSummary.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
			this.tableSummary.Controls.Add(this.labelTime, 0, 0);
			this.tableSummary.Controls.Add(this.labelCost, 1, 0);
			this.tableSummary.Controls.Add(this.labelGSRate, 2, 0);
			this.tableSummary.Controls.Add(this.labelGSDetail, 0, 1);
			this.tableSummary.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableSummary.Location = new System.Drawing.Point(3, 14);
			this.tableSummary.Margin = new System.Windows.Forms.Padding(0);
			this.tableSummary.Name = "tableSummary";
			this.tableSummary.RowCount = 2;
			this.tableSummary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableSummary.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
			this.tableSummary.Size = new System.Drawing.Size(620, 46);
			this.tableSummary.TabIndex = 0;
			// 
			// labelTime
			// 
			this.labelTime.AutoSize = true;
			this.labelTime.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelTime.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.labelTime.Location = new System.Drawing.Point(2, 0);
			this.labelTime.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(169, 20);
			this.labelTime.TabIndex = 0;
			this.labelTime.Text = "時間: 00:00";
			this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelCost
			// 
			this.labelCost.AutoSize = true;
			this.labelCost.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelCost.Location = new System.Drawing.Point(175, 0);
			this.labelCost.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelCost.Name = "labelCost";
			this.labelCost.Size = new System.Drawing.Size(206, 20);
			this.labelCost.TabIndex = 1;
			this.labelCost.Text = "消費: 燃 0% / 弾 0%";
			this.labelCost.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelGSRate
			// 
			this.labelGSRate.AutoSize = true;
			this.labelGSRate.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSRate.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.labelGSRate.ForeColor = System.Drawing.Color.Green;
			this.labelGSRate.Location = new System.Drawing.Point(385, 0);
			this.labelGSRate.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelGSRate.Name = "labelGSRate";
			this.labelGSRate.Size = new System.Drawing.Size(233, 20);
			this.labelGSRate.TabIndex = 2;
			this.labelGSRate.Text = "大成功: 0.0% (通常型)";
			this.labelGSRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelGSDetail
			// 
			this.labelGSDetail.AutoSize = true;
			this.tableSummary.SetColumnSpan(this.labelGSDetail, 3);
			this.labelGSDetail.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSDetail.Location = new System.Drawing.Point(2, 20);
			this.labelGSDetail.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.labelGSDetail.Name = "labelGSDetail";
			this.labelGSDetail.Size = new System.Drawing.Size(616, 26);
			this.labelGSDetail.TabIndex = 3;
			this.labelGSDetail.Text = "キラ: 0/6 隻 | ドラム缶: 0 個 | 旗艦: Lv1";
			this.labelGSDetail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// groupConditions
			// 
			this.groupConditions.Controls.Add(this.gridConditions);
			this.groupConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupConditions.Location = new System.Drawing.Point(0, 0);
			this.groupConditions.Name = "groupConditions";
			this.groupConditions.Size = new System.Drawing.Size(382, 413);
			this.groupConditions.TabIndex = 0;
			this.groupConditions.TabStop = false;
			this.groupConditions.Text = "成功要件チェックリスト";
			// 
			// gridConditions
			// 
			this.gridConditions.AllowUserToAddRows = false;
			this.gridConditions.AllowUserToDeleteRows = false;
			this.gridConditions.AllowUserToResizeRows = false;
			this.gridConditions.BackgroundColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.gridConditions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.gridConditions.ColumnHeadersHeight = 22;
			this.gridConditions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.gridConditions.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colStatus,
            this.colItemName,
            this.colRequirement,
            this.colCurrent});
			this.gridConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridConditions.Location = new System.Drawing.Point(3, 15);
			this.gridConditions.MultiSelect = false;
			this.gridConditions.Name = "gridConditions";
			this.gridConditions.ReadOnly = true;
			this.gridConditions.RowHeadersVisible = false;
			this.gridConditions.RowTemplate.Height = 20;
			this.gridConditions.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.gridConditions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridConditions.Size = new System.Drawing.Size(376, 395);
			this.gridConditions.TabIndex = 0;
			// 
			// colStatus
			// 
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.colStatus.DefaultCellStyle = dataGridViewCellStyle3;
			this.colStatus.HeaderText = "判定";
			this.colStatus.Name = "colStatus";
			this.colStatus.ReadOnly = true;
			this.colStatus.Width = 36;
			this.colStatus.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			// 
			// colItemName
			// 
			this.colItemName.HeaderText = "項目";
			this.colItemName.Name = "colItemName";
			this.colItemName.ReadOnly = true;
			this.colItemName.Width = 80;
			// 
			// colRequirement
			// 
			this.colRequirement.HeaderText = "要求条件";
			this.colRequirement.Name = "colRequirement";
			this.colRequirement.ReadOnly = true;
			this.colRequirement.Width = 120;
			// 
			// colCurrent
			// 
			this.colCurrent.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colCurrent.HeaderText = "艦隊の現在値";
			this.colCurrent.MinimumWidth = 100;
			this.colCurrent.Name = "colCurrent";
			this.colCurrent.ReadOnly = true;
			// 
			// panelControl
			// 
			this.panelControl.Controls.Add(this.checkAlertSupply);
			this.panelControl.Controls.Add(this.checkAlert);
			this.panelControl.Controls.Add(this.comboMission);
			this.panelControl.Controls.Add(this.labelMission);
			this.panelControl.Controls.Add(this.comboArea);
			this.panelControl.Controls.Add(this.labelArea);
			this.panelControl.Controls.Add(this.comboFleet);
			this.panelControl.Controls.Add(this.labelFleet);
			this.panelControl.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelControl.Location = new System.Drawing.Point(3, 3);
			this.panelControl.Name = "panelControl";
			this.panelControl.Size = new System.Drawing.Size(626, 52);
			this.panelControl.TabIndex = 0;
			this.panelControl.Resize += new System.EventHandler(this.panelControl_Resize);
			// 
			// checkAlert
			// 
			this.checkAlert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
			this.checkAlert.AutoSize = true;
			this.checkAlert.Checked = true;
			this.checkAlert.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkAlert.Location = new System.Drawing.Point(6, 30);
			this.checkAlert.Name = "checkAlert";
			this.checkAlert.Size = new System.Drawing.Size(148, 16);
			this.checkAlert.TabIndex = 6;
			this.checkAlert.Text = "遠征画面表示時に警告";
			this.toolTip.SetToolTip(this.checkAlert, "遠征選択画面を開いた時、設定した遠征条件を満たしていない場合に警告します");
			this.checkAlert.UseVisualStyleBackColor = true;
			this.checkAlert.CheckedChanged += new System.EventHandler(this.checkAlert_CheckedChanged);
			// 
			// checkAlertSupply
			// 
			this.checkAlertSupply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
			this.checkAlertSupply.AutoSize = true;
			this.checkAlertSupply.Checked = false;
			this.checkAlertSupply.Location = new System.Drawing.Point(165, 30);
			this.checkAlertSupply.Name = "checkAlertSupply";
			this.checkAlertSupply.Size = new System.Drawing.Size(98, 16);
			this.checkAlertSupply.TabIndex = 7;
			this.checkAlertSupply.Text = "未補給も警告";
			this.toolTip.SetToolTip(this.checkAlertSupply, "燃料・弾薬が不足している場合も警告モーダルを表示します（OFFの場合、緊急補給を想定して編成条件不足のみ警告します）");
			this.checkAlertSupply.UseVisualStyleBackColor = true;
			this.checkAlertSupply.CheckedChanged += new System.EventHandler(this.checkAlertSupply_CheckedChanged);
			// 
			// comboMission
			// 
			this.comboMission.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboMission.FormattingEnabled = true;
			this.comboMission.Location = new System.Drawing.Point(286, 7);
			this.comboMission.Name = "comboMission";
			this.comboMission.Size = new System.Drawing.Size(170, 20);
			this.comboMission.TabIndex = 5;
			this.comboMission.SelectedIndexChanged += new System.EventHandler(this.comboMission_SelectedIndexChanged);
			// 
			// labelMission
			// 
			this.labelMission.AutoSize = true;
			this.labelMission.Location = new System.Drawing.Point(249, 10);
			this.labelMission.Name = "labelMission";
			this.labelMission.Size = new System.Drawing.Size(31, 12);
			this.labelMission.TabIndex = 4;
			this.labelMission.Text = "遠征:";
			// 
			// comboArea
			// 
			this.comboArea.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboArea.FormattingEnabled = true;
			this.comboArea.Location = new System.Drawing.Point(145, 7);
			this.comboArea.Name = "comboArea";
			this.comboArea.Size = new System.Drawing.Size(95, 20);
			this.comboArea.TabIndex = 3;
			this.comboArea.SelectedIndexChanged += new System.EventHandler(this.comboArea_SelectedIndexChanged);
			// 
			// labelArea
			// 
			this.labelArea.AutoSize = true;
			this.labelArea.Location = new System.Drawing.Point(108, 10);
			this.labelArea.Name = "labelArea";
			this.labelArea.Size = new System.Drawing.Size(31, 12);
			this.labelArea.TabIndex = 2;
			this.labelArea.Text = "海域:";
			// 
			// comboFleet
			// 
			this.comboFleet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboFleet.FormattingEnabled = true;
			this.comboFleet.Items.AddRange(new object[] {
            "第2艦隊",
            "第3艦隊",
            "第4艦隊"});
			this.comboFleet.Location = new System.Drawing.Point(41, 7);
			this.comboFleet.Name = "comboFleet";
			this.comboFleet.Size = new System.Drawing.Size(60, 20);
			this.comboFleet.TabIndex = 1;
			this.comboFleet.SelectedIndexChanged += new System.EventHandler(this.comboFleet_SelectedIndexChanged);
			// 
			// labelFleet
			// 
			this.labelFleet.AutoSize = true;
			this.labelFleet.Location = new System.Drawing.Point(4, 10);
			this.labelFleet.Name = "labelFleet";
			this.labelFleet.Size = new System.Drawing.Size(31, 12);
			this.labelFleet.TabIndex = 0;
			this.labelFleet.Text = "艦隊:";
			// 
			// tabMatrix
			// 
			this.tabMatrix.Controls.Add(this.matrixGrid);
			this.tabMatrix.Location = new System.Drawing.Point(4, 22);
			this.tabMatrix.Name = "tabMatrix";
			this.tabMatrix.Padding = new System.Windows.Forms.Padding(3);
			this.tabMatrix.Size = new System.Drawing.Size(632, 454);
			this.tabMatrix.TabIndex = 1;
			this.tabMatrix.Text = "全体一覧";
			this.tabMatrix.UseVisualStyleBackColor = true;
			// 
			// matrixGrid
			// 
			this.matrixGrid.AllowUserToAddRows = false;
			this.matrixGrid.AllowUserToDeleteRows = false;
			this.matrixGrid.AllowUserToResizeRows = false;
			this.matrixGrid.BackgroundColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.matrixGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
			this.matrixGrid.ColumnHeadersHeight = 22;
			this.matrixGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			this.matrixGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.matrixColID,
            this.matrixColName,
            this.matrixColFleet1,
            this.matrixColFleet2,
            this.matrixColFleet3,
            this.matrixColFleet4,
            this.matrixColCondition});
			this.matrixGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.matrixGrid.Location = new System.Drawing.Point(3, 3);
			this.matrixGrid.Name = "matrixGrid";
			this.matrixGrid.ReadOnly = true;
			this.matrixGrid.RowHeadersVisible = false;
			this.matrixGrid.RowTemplate.Height = 20;
			this.matrixGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.matrixGrid.Size = new System.Drawing.Size(626, 448);
			this.matrixGrid.TabIndex = 0;
			this.matrixGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.matrixGrid_CellDoubleClick);
			this.matrixGrid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.matrixGrid_CellFormatting);
			this.matrixGrid.SortCompare += new System.Windows.Forms.DataGridViewSortCompareEventHandler(this.matrixGrid_SortCompare);
			// 
			// matrixColID
			// 
			this.matrixColID.HeaderText = "ID";
			this.matrixColID.Name = "matrixColID";
			this.matrixColID.ReadOnly = true;
			this.matrixColID.Width = 40;
			// 
			// matrixColName
			// 
			this.matrixColName.HeaderText = "遠征名";
			this.matrixColName.Name = "matrixColName";
			this.matrixColName.ReadOnly = true;
			this.matrixColName.Width = 95;
			// 
			// matrixColFleet1
			// 
			this.matrixColFleet1.HeaderText = "#1";
			this.matrixColFleet1.Name = "matrixColFleet1";
			this.matrixColFleet1.ReadOnly = true;
			this.matrixColFleet1.Width = 36;
			// 
			// matrixColFleet2
			// 
			this.matrixColFleet2.HeaderText = "#2";
			this.matrixColFleet2.Name = "matrixColFleet2";
			this.matrixColFleet2.ReadOnly = true;
			this.matrixColFleet2.Width = 36;
			// 
			// matrixColFleet3
			// 
			this.matrixColFleet3.HeaderText = "#3";
			this.matrixColFleet3.Name = "matrixColFleet3";
			this.matrixColFleet3.ReadOnly = true;
			this.matrixColFleet3.Width = 36;
			// 
			// matrixColFleet4
			// 
			this.matrixColFleet4.HeaderText = "#4";
			this.matrixColFleet4.Name = "matrixColFleet4";
			this.matrixColFleet4.ReadOnly = true;
			this.matrixColFleet4.Width = 36;
			// 
			// matrixColCondition
			// 
			this.matrixColCondition.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.matrixColCondition.HeaderText = "必要要件";
			this.matrixColCondition.Name = "matrixColCondition";
			this.matrixColCondition.ReadOnly = true;
			// 
			// FormExpeditionCheck
			// 
			this.AutoHidePortion = 150D;
			this.ClientSize = new System.Drawing.Size(640, 480);
			this.Controls.Add(this.tabControl);
			this.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.Name = "FormExpeditionCheck";
			this.Text = "遠征可否";
			this.Load += new System.EventHandler(this.FormExpeditionCheck_Load);
			this.Resize += new System.EventHandler(this.FormExpeditionCheck_Resize);
			this.tabControl.ResumeLayout(false);
			this.tabDetail.ResumeLayout(false);
			this.splitDetail.Panel1.ResumeLayout(false);
			this.splitDetail.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitDetail)).EndInit();
			this.splitDetail.ResumeLayout(false);
			this.groupSummary.ResumeLayout(false);
			this.tableSummary.ResumeLayout(false);
			this.tableSummary.PerformLayout();
			this.groupConditions.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.gridConditions)).EndInit();
			this.panelControl.ResumeLayout(false);
			this.panelControl.PerformLayout();
			this.tabMatrix.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.matrixGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabDetail;
		private System.Windows.Forms.TabPage tabMatrix;
		private System.Windows.Forms.Panel panelControl;
		private System.Windows.Forms.Label labelFleet;
		private System.Windows.Forms.ComboBox comboFleet;
		private System.Windows.Forms.Label labelArea;
		private System.Windows.Forms.ComboBox comboArea;
		private System.Windows.Forms.Label labelMission;
		private System.Windows.Forms.ComboBox comboMission;
		private System.Windows.Forms.CheckBox checkAlert;
		private System.Windows.Forms.CheckBox checkAlertSupply;
		private System.Windows.Forms.SplitContainer splitDetail;
		private System.Windows.Forms.GroupBox groupSummary;
		private System.Windows.Forms.TableLayoutPanel tableSummary;
		private System.Windows.Forms.GroupBox groupConditions;
		private System.Windows.Forms.DataGridView gridConditions;
		private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
		private System.Windows.Forms.DataGridViewTextBoxColumn colItemName;
		private System.Windows.Forms.DataGridViewTextBoxColumn colRequirement;
		private System.Windows.Forms.DataGridViewTextBoxColumn colCurrent;
		private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.Label labelCost;
		private System.Windows.Forms.Label labelGSRate;
		private System.Windows.Forms.Label labelGSDetail;
		private System.Windows.Forms.DataGridView matrixGrid;
		private System.Windows.Forms.DataGridViewTextBoxColumn matrixColID;
		private System.Windows.Forms.DataGridViewTextBoxColumn matrixColName;
		private System.Windows.Forms.DataGridViewTextBoxColumn matrixColFleet1;
		private System.Windows.Forms.DataGridViewTextBoxColumn matrixColFleet2;
		private System.Windows.Forms.DataGridViewTextBoxColumn matrixColFleet3;
		private System.Windows.Forms.DataGridViewTextBoxColumn matrixColFleet4;
		private System.Windows.Forms.DataGridViewTextBoxColumn matrixColCondition;
		private System.Windows.Forms.ToolTip toolTip;
	}
}
