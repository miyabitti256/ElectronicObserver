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
			this.groupGreatSuccess = new System.Windows.Forms.GroupBox();
			this.tableGreatSuccess = new System.Windows.Forms.TableLayoutPanel();
			this.labelGSTypeTitle = new System.Windows.Forms.Label();
			this.labelGSType = new System.Windows.Forms.Label();
			this.labelGSRateTitle = new System.Windows.Forms.Label();
			this.labelGSRate = new System.Windows.Forms.Label();
			this.labelGSDetailTitle = new System.Windows.Forms.Label();
			this.labelGSDetail = new System.Windows.Forms.Label();
			this.labelGSDesc = new System.Windows.Forms.Label();
			this.groupExpeditionInfo = new System.Windows.Forms.GroupBox();
			this.tableExpInfo = new System.Windows.Forms.TableLayoutPanel();
			this.labelTimeTitle = new System.Windows.Forms.Label();
			this.labelTime = new System.Windows.Forms.Label();
			this.labelCostTitle = new System.Windows.Forms.Label();
			this.labelCost = new System.Windows.Forms.Label();
			this.labelRewardTitle = new System.Windows.Forms.Label();
			this.labelReward = new System.Windows.Forms.Label();
			this.labelItemTitle = new System.Windows.Forms.Label();
			this.labelItem = new System.Windows.Forms.Label();
			this.groupConditions = new System.Windows.Forms.GroupBox();
			this.gridConditions = new System.Windows.Forms.DataGridView();
			this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colRequirement = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.panelControl = new System.Windows.Forms.Panel();
			this.checkAlert = new System.Windows.Forms.CheckBox();
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
			this.groupGreatSuccess.SuspendLayout();
			this.tableGreatSuccess.SuspendLayout();
			this.groupExpeditionInfo.SuspendLayout();
			this.tableExpInfo.SuspendLayout();
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
			this.splitDetail.Location = new System.Drawing.Point(3, 38);
			this.splitDetail.Name = "splitDetail";
			// 
			// splitDetail.Panel1
			// 
			this.splitDetail.Panel1.Controls.Add(this.groupGreatSuccess);
			this.splitDetail.Panel1.Controls.Add(this.groupExpeditionInfo);
			this.splitDetail.Panel1MinSize = 220;
			// 
			// splitDetail.Panel2
			// 
			this.splitDetail.Panel2.Controls.Add(this.groupConditions);
			this.splitDetail.Size = new System.Drawing.Size(626, 413);
			this.splitDetail.SplitterDistance = 240;
			this.splitDetail.TabIndex = 1;
			// 
			// groupGreatSuccess
			// 
			this.groupGreatSuccess.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupGreatSuccess.Controls.Add(this.tableGreatSuccess);
			this.groupGreatSuccess.Location = new System.Drawing.Point(3, 140);
			this.groupGreatSuccess.Name = "groupGreatSuccess";
			this.groupGreatSuccess.Size = new System.Drawing.Size(234, 210);
			this.groupGreatSuccess.TabIndex = 1;
			this.groupGreatSuccess.TabStop = false;
			this.groupGreatSuccess.Text = "大成功情報";
			// 
			// tableGreatSuccess
			// 
			this.tableGreatSuccess.ColumnCount = 2;
			this.tableGreatSuccess.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
			this.tableGreatSuccess.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableGreatSuccess.Controls.Add(this.labelGSTypeTitle, 0, 0);
			this.tableGreatSuccess.Controls.Add(this.labelGSType, 1, 0);
			this.tableGreatSuccess.Controls.Add(this.labelGSRateTitle, 0, 1);
			this.tableGreatSuccess.Controls.Add(this.labelGSRate, 1, 1);
			this.tableGreatSuccess.Controls.Add(this.labelGSDetailTitle, 0, 2);
			this.tableGreatSuccess.Controls.Add(this.labelGSDetail, 1, 2);
			this.tableGreatSuccess.Controls.Add(this.labelGSDesc, 0, 3);
			this.tableGreatSuccess.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableGreatSuccess.Location = new System.Drawing.Point(3, 15);
			this.tableGreatSuccess.Name = "tableGreatSuccess";
			this.tableGreatSuccess.RowCount = 4;
			this.tableGreatSuccess.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
			this.tableGreatSuccess.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
			this.tableGreatSuccess.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
			this.tableGreatSuccess.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableGreatSuccess.Size = new System.Drawing.Size(228, 192);
			this.tableGreatSuccess.TabIndex = 0;
			// 
			// labelGSTypeTitle
			// 
			this.labelGSTypeTitle.AutoSize = true;
			this.labelGSTypeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSTypeTitle.Location = new System.Drawing.Point(3, 0);
			this.labelGSTypeTitle.Name = "labelGSTypeTitle";
			this.labelGSTypeTitle.Size = new System.Drawing.Size(64, 22);
			this.labelGSTypeTitle.TabIndex = 0;
			this.labelGSTypeTitle.Text = "タイプ:";
			this.labelGSTypeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelGSType
			// 
			this.labelGSType.AutoSize = true;
			this.labelGSType.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSType.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.labelGSType.Location = new System.Drawing.Point(73, 0);
			this.labelGSType.Name = "labelGSType";
			this.labelGSType.Size = new System.Drawing.Size(152, 22);
			this.labelGSType.TabIndex = 1;
			this.labelGSType.Text = "通常型";
			this.labelGSType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelGSRateTitle
			// 
			this.labelGSRateTitle.AutoSize = true;
			this.labelGSRateTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSRateTitle.Location = new System.Drawing.Point(3, 22);
			this.labelGSRateTitle.Name = "labelGSRateTitle";
			this.labelGSRateTitle.Size = new System.Drawing.Size(64, 34);
			this.labelGSRateTitle.TabIndex = 2;
			this.labelGSRateTitle.Text = "大成功率:";
			this.labelGSRateTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelGSRate
			// 
			this.labelGSRate.AutoSize = true;
			this.labelGSRate.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSRate.Font = new System.Drawing.Font("MS UI Gothic", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.labelGSRate.ForeColor = System.Drawing.Color.Green;
			this.labelGSRate.Location = new System.Drawing.Point(73, 22);
			this.labelGSRate.Name = "labelGSRate";
			this.labelGSRate.Size = new System.Drawing.Size(152, 34);
			this.labelGSRate.TabIndex = 3;
			this.labelGSRate.Text = "0.0%";
			this.labelGSRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelGSDetailTitle
			// 
			this.labelGSDetailTitle.AutoSize = true;
			this.labelGSDetailTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSDetailTitle.Location = new System.Drawing.Point(3, 56);
			this.labelGSDetailTitle.Name = "labelGSDetailTitle";
			this.labelGSDetailTitle.Size = new System.Drawing.Size(64, 60);
			this.labelGSDetailTitle.TabIndex = 4;
			this.labelGSDetailTitle.Text = "艦隊状況:";
			this.labelGSDetailTitle.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			// 
			// labelGSDetail
			// 
			this.labelGSDetail.AutoSize = true;
			this.labelGSDetail.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSDetail.Location = new System.Drawing.Point(73, 56);
			this.labelGSDetail.Name = "labelGSDetail";
			this.labelGSDetail.Size = new System.Drawing.Size(152, 60);
			this.labelGSDetail.TabIndex = 5;
			this.labelGSDetail.Text = "キラキラ: 0/6 隻\r\nドラム缶: 0 個\r\n旗艦Lv: 1";
			// 
			// labelGSDesc
			// 
			this.labelGSDesc.AutoSize = true;
			this.tableGreatSuccess.SetColumnSpan(this.labelGSDesc, 2);
			this.labelGSDesc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelGSDesc.ForeColor = System.Drawing.SystemColors.GrayText;
			this.labelGSDesc.Location = new System.Drawing.Point(3, 116);
			this.labelGSDesc.Name = "labelGSDesc";
			this.labelGSDesc.Size = new System.Drawing.Size(222, 76);
			this.labelGSDesc.TabIndex = 6;
			this.labelGSDesc.Text = "※説明文";
			// 
			// groupExpeditionInfo
			// 
			this.groupExpeditionInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupExpeditionInfo.Controls.Add(this.tableExpInfo);
			this.groupExpeditionInfo.Location = new System.Drawing.Point(3, 3);
			this.groupExpeditionInfo.Name = "groupExpeditionInfo";
			this.groupExpeditionInfo.Size = new System.Drawing.Size(234, 131);
			this.groupExpeditionInfo.TabIndex = 0;
			this.groupExpeditionInfo.TabStop = false;
			this.groupExpeditionInfo.Text = "遠征情報";
			// 
			// tableExpInfo
			// 
			this.tableExpInfo.ColumnCount = 2;
			this.tableExpInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 55F));
			this.tableExpInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableExpInfo.Controls.Add(this.labelTimeTitle, 0, 0);
			this.tableExpInfo.Controls.Add(this.labelTime, 1, 0);
			this.tableExpInfo.Controls.Add(this.labelCostTitle, 0, 1);
			this.tableExpInfo.Controls.Add(this.labelCost, 1, 1);
			this.tableExpInfo.Controls.Add(this.labelRewardTitle, 0, 2);
			this.tableExpInfo.Controls.Add(this.labelReward, 1, 2);
			this.tableExpInfo.Controls.Add(this.labelItemTitle, 0, 3);
			this.tableExpInfo.Controls.Add(this.labelItem, 1, 3);
			this.tableExpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableExpInfo.Location = new System.Drawing.Point(3, 15);
			this.tableExpInfo.Name = "tableExpInfo";
			this.tableExpInfo.RowCount = 4;
			this.tableExpInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
			this.tableExpInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
			this.tableExpInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
			this.tableExpInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableExpInfo.Size = new System.Drawing.Size(228, 113);
			this.tableExpInfo.TabIndex = 0;
			// 
			// labelTimeTitle
			// 
			this.labelTimeTitle.AutoSize = true;
			this.labelTimeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelTimeTitle.Location = new System.Drawing.Point(3, 0);
			this.labelTimeTitle.Name = "labelTimeTitle";
			this.labelTimeTitle.Size = new System.Drawing.Size(49, 22);
			this.labelTimeTitle.TabIndex = 0;
			this.labelTimeTitle.Text = "時間:";
			this.labelTimeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelTime
			// 
			this.labelTime.AutoSize = true;
			this.labelTime.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelTime.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.labelTime.Location = new System.Drawing.Point(58, 0);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(167, 22);
			this.labelTime.TabIndex = 1;
			this.labelTime.Text = "00:00";
			this.labelTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelCostTitle
			// 
			this.labelCostTitle.AutoSize = true;
			this.labelCostTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelCostTitle.Location = new System.Drawing.Point(3, 22);
			this.labelCostTitle.Name = "labelCostTitle";
			this.labelCostTitle.Size = new System.Drawing.Size(49, 22);
			this.labelCostTitle.TabIndex = 2;
			this.labelCostTitle.Text = "消費:";
			this.labelCostTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelCost
			// 
			this.labelCost.AutoSize = true;
			this.labelCost.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelCost.Location = new System.Drawing.Point(58, 22);
			this.labelCost.Name = "labelCost";
			this.labelCost.Size = new System.Drawing.Size(167, 22);
			this.labelCost.TabIndex = 3;
			this.labelCost.Text = "燃 0% / 弾 0%";
			this.labelCost.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelRewardTitle
			// 
			this.labelRewardTitle.AutoSize = true;
			this.labelRewardTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelRewardTitle.Location = new System.Drawing.Point(3, 44);
			this.labelRewardTitle.Name = "labelRewardTitle";
			this.labelRewardTitle.Size = new System.Drawing.Size(49, 34);
			this.labelRewardTitle.TabIndex = 4;
			this.labelRewardTitle.Text = "説明:";
			this.labelRewardTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelReward
			// 
			this.labelReward.AutoSize = true;
			this.labelReward.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelReward.Location = new System.Drawing.Point(58, 44);
			this.labelReward.Name = "labelReward";
			this.labelReward.Size = new System.Drawing.Size(167, 34);
			this.labelReward.TabIndex = 5;
			this.labelReward.Text = "-";
			this.labelReward.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelItemTitle
			// 
			this.labelItemTitle.AutoSize = true;
			this.labelItemTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelItemTitle.Location = new System.Drawing.Point(3, 78);
			this.labelItemTitle.Name = "labelItemTitle";
			this.labelItemTitle.Size = new System.Drawing.Size(49, 35);
			this.labelItemTitle.TabIndex = 6;
			this.labelItemTitle.Text = "帰投:";
			this.labelItemTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelItem
			// 
			this.labelItem.AutoSize = true;
			this.labelItem.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelItem.Location = new System.Drawing.Point(58, 78);
			this.labelItem.Name = "labelItem";
			this.labelItem.Size = new System.Drawing.Size(167, 35);
			this.labelItem.TabIndex = 7;
			this.labelItem.Text = "-";
			this.labelItem.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
			this.groupConditions.Text = "成功要件チェックリスト (未達成の項目をハイライト)";
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
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.gridConditions.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.gridConditions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
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
			this.gridConditions.RowTemplate.Height = 21;
			this.gridConditions.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridConditions.Size = new System.Drawing.Size(376, 395);
			this.gridConditions.TabIndex = 0;
			// 
			// colStatus
			// 
			this.colStatus.HeaderText = "判定";
			this.colStatus.Name = "colStatus";
			this.colStatus.ReadOnly = true;
			this.colStatus.Width = 45;
			// 
			// colItemName
			// 
			this.colItemName.HeaderText = "項目";
			this.colItemName.Name = "colItemName";
			this.colItemName.ReadOnly = true;
			this.colItemName.Width = 110;
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
			this.colCurrent.Name = "colCurrent";
			this.colCurrent.ReadOnly = true;
			// 
			// panelControl
			// 
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
			this.panelControl.Size = new System.Drawing.Size(626, 35);
			this.panelControl.TabIndex = 0;
			// 
			// checkAlert
			// 
			this.checkAlert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkAlert.AutoSize = true;
			this.checkAlert.Checked = true;
			this.checkAlert.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkAlert.Location = new System.Drawing.Point(475, 9);
			this.checkAlert.Name = "checkAlert";
			this.checkAlert.Size = new System.Drawing.Size(148, 16);
			this.checkAlert.TabIndex = 6;
			this.checkAlert.Text = "遠征画面表示時に警告";
			this.toolTip.SetToolTip(this.checkAlert, "遠征選択画面を開いた時、この艦隊の遠征条件を満たしていない場合に警告します");
			this.checkAlert.UseVisualStyleBackColor = true;
			this.checkAlert.CheckedChanged += new System.EventHandler(this.checkAlert_CheckedChanged);
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
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.matrixGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
			this.matrixGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
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
			this.matrixGrid.RowTemplate.Height = 21;
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
			this.matrixColID.Width = 60;
			// 
			// matrixColName
			// 
			this.matrixColName.HeaderText = "遠征名";
			this.matrixColName.Name = "matrixColName";
			this.matrixColName.ReadOnly = true;
			this.matrixColName.Width = 140;
			// 
			// matrixColFleet1
			// 
			this.matrixColFleet1.HeaderText = "#1";
			this.matrixColFleet1.Name = "matrixColFleet1";
			this.matrixColFleet1.ReadOnly = true;
			this.matrixColFleet1.Width = 50;
			// 
			// matrixColFleet2
			// 
			this.matrixColFleet2.HeaderText = "#2";
			this.matrixColFleet2.Name = "matrixColFleet2";
			this.matrixColFleet2.ReadOnly = true;
			this.matrixColFleet2.Width = 50;
			// 
			// matrixColFleet3
			// 
			this.matrixColFleet3.HeaderText = "#3";
			this.matrixColFleet3.Name = "matrixColFleet3";
			this.matrixColFleet3.ReadOnly = true;
			this.matrixColFleet3.Width = 50;
			// 
			// matrixColFleet4
			// 
			this.matrixColFleet4.HeaderText = "#4";
			this.matrixColFleet4.Name = "matrixColFleet4";
			this.matrixColFleet4.ReadOnly = true;
			this.matrixColFleet4.Width = 50;
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
			this.tabControl.ResumeLayout(false);
			this.tabDetail.ResumeLayout(false);
			this.splitDetail.Panel1.ResumeLayout(false);
			this.splitDetail.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitDetail)).EndInit();
			this.splitDetail.ResumeLayout(false);
			this.groupGreatSuccess.ResumeLayout(false);
			this.tableGreatSuccess.ResumeLayout(false);
			this.tableGreatSuccess.PerformLayout();
			this.groupExpeditionInfo.ResumeLayout(false);
			this.tableExpInfo.ResumeLayout(false);
			this.tableExpInfo.PerformLayout();
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
		private System.Windows.Forms.SplitContainer splitDetail;
		private System.Windows.Forms.GroupBox groupExpeditionInfo;
		private System.Windows.Forms.GroupBox groupGreatSuccess;
		private System.Windows.Forms.GroupBox groupConditions;
		private System.Windows.Forms.DataGridView gridConditions;
		private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
		private System.Windows.Forms.DataGridViewTextBoxColumn colItemName;
		private System.Windows.Forms.DataGridViewTextBoxColumn colRequirement;
		private System.Windows.Forms.DataGridViewTextBoxColumn colCurrent;
		private System.Windows.Forms.TableLayoutPanel tableExpInfo;
		private System.Windows.Forms.Label labelTimeTitle;
		private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.Label labelCostTitle;
		private System.Windows.Forms.Label labelCost;
		private System.Windows.Forms.Label labelRewardTitle;
		private System.Windows.Forms.Label labelReward;
		private System.Windows.Forms.Label labelItemTitle;
		private System.Windows.Forms.Label labelItem;
		private System.Windows.Forms.TableLayoutPanel tableGreatSuccess;
		private System.Windows.Forms.Label labelGSTypeTitle;
		private System.Windows.Forms.Label labelGSType;
		private System.Windows.Forms.Label labelGSRateTitle;
		private System.Windows.Forms.Label labelGSRate;
		private System.Windows.Forms.Label labelGSDetailTitle;
		private System.Windows.Forms.Label labelGSDetail;
		private System.Windows.Forms.Label labelGSDesc;
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
