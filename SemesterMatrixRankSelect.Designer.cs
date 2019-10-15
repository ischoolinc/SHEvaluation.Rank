﻿namespace SHEvaluation.Rank
{
    partial class SemesterMatrixRankSelect
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.SeatNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.percentile = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SchoolYear = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Semester = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.labelX8 = new DevComponents.DotNetBar.LabelX();
            this.labelX7 = new DevComponents.DotNetBar.LabelX();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.ClassName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnExportToExcel = new DevComponents.DotNetBar.ButtonX();
            this.RankName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lbMemo = new DevComponents.DotNetBar.LabelX();
            this.labelX11 = new DevComponents.DotNetBar.LabelX();
            this.cboBatchId = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX9 = new DevComponents.DotNetBar.LabelX();
            this.lbRankType = new DevComponents.DotNetBar.LabelX();
            this.lbItemName = new DevComponents.DotNetBar.LabelX();
            this.lbScoreCategory = new DevComponents.DotNetBar.LabelX();
            this.lbScoreType = new DevComponents.DotNetBar.LabelX();
            this.lbSemester = new DevComponents.DotNetBar.LabelX();
            this.lbSchoolYear = new DevComponents.DotNetBar.LabelX();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.btnExit = new DevComponents.DotNetBar.ButtonX();
            this.dgvScoreRank = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.MatrixId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ScoreType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ScoreCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RankType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvScoreRank)).BeginInit();
            this.SuspendLayout();
            // 
            // labelX1
            // 
            this.labelX1.AutoSize = true;
            this.labelX1.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.Class = "";
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX1.Location = new System.Drawing.Point(12, 12);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(62, 22);
            this.labelX1.TabIndex = 83;
            this.labelX1.Text = "學年度：";
            // 
            // SeatNo
            // 
            this.SeatNo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SeatNo.HeaderText = "座號";
            this.SeatNo.MinimumWidth = 59;
            this.SeatNo.Name = "SeatNo";
            this.SeatNo.ReadOnly = true;
            this.SeatNo.Width = 59;
            // 
            // StudentNum
            // 
            this.StudentNum.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.StudentNum.HeaderText = "學號";
            this.StudentNum.MinimumWidth = 59;
            this.StudentNum.Name = "StudentNum";
            this.StudentNum.ReadOnly = true;
            this.StudentNum.Width = 59;
            // 
            // StudentName
            // 
            this.StudentName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.StudentName.HeaderText = "姓名";
            this.StudentName.MinimumWidth = 59;
            this.StudentName.Name = "StudentName";
            this.StudentName.ReadOnly = true;
            this.StudentName.Width = 59;
            // 
            // StudentStatus
            // 
            this.StudentStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.StudentStatus.HeaderText = "目前狀態";
            this.StudentStatus.MinimumWidth = 59;
            this.StudentStatus.Name = "StudentStatus";
            this.StudentStatus.ReadOnly = true;
            this.StudentStatus.Width = 85;
            // 
            // score
            // 
            this.score.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle3.NullValue = null;
            this.score.DefaultCellStyle = dataGridViewCellStyle3;
            this.score.HeaderText = "排名分數";
            this.score.MinimumWidth = 91;
            this.score.Name = "score";
            this.score.ReadOnly = true;
            // 
            // rank
            // 
            this.rank.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.rank.HeaderText = "名次";
            this.rank.MinimumWidth = 59;
            this.rank.Name = "rank";
            this.rank.ReadOnly = true;
            this.rank.Width = 59;
            // 
            // pr
            // 
            this.pr.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.pr.HeaderText = "PR";
            this.pr.MinimumWidth = 49;
            this.pr.Name = "pr";
            this.pr.ReadOnly = true;
            this.pr.Width = 49;
            // 
            // percentile
            // 
            this.percentile.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.percentile.HeaderText = "百分比";
            this.percentile.Name = "percentile";
            this.percentile.ReadOnly = true;
            this.percentile.Width = 72;
            // 
            // SchoolYear
            // 
            this.SchoolYear.HeaderText = "學年度";
            this.SchoolYear.Name = "SchoolYear";
            this.SchoolYear.ReadOnly = true;
            this.SchoolYear.Visible = false;
            this.SchoolYear.Width = 72;
            // 
            // Semester
            // 
            this.Semester.HeaderText = "學期";
            this.Semester.Name = "Semester";
            this.Semester.ReadOnly = true;
            this.Semester.Visible = false;
            this.Semester.Width = 59;
            // 
            // labelX8
            // 
            this.labelX8.AutoSize = true;
            this.labelX8.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX8.BackgroundStyle.Class = "";
            this.labelX8.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX8.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX8.Location = new System.Drawing.Point(432, 49);
            this.labelX8.Name = "labelX8";
            this.labelX8.Size = new System.Drawing.Size(47, 22);
            this.labelX8.TabIndex = 88;
            this.labelX8.Text = "母群：";
            // 
            // labelX7
            // 
            this.labelX7.AutoSize = true;
            this.labelX7.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX7.BackgroundStyle.Class = "";
            this.labelX7.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX7.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX7.Location = new System.Drawing.Point(232, 49);
            this.labelX7.Name = "labelX7";
            this.labelX7.Size = new System.Drawing.Size(47, 22);
            this.labelX7.TabIndex = 87;
            this.labelX7.Text = "項目：";
            // 
            // labelX5
            // 
            this.labelX5.AutoSize = true;
            this.labelX5.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX5.BackgroundStyle.Class = "";
            this.labelX5.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX5.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX5.Location = new System.Drawing.Point(12, 49);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(47, 22);
            this.labelX5.TabIndex = 86;
            this.labelX5.Text = "類別：";
            // 
            // labelX3
            // 
            this.labelX3.AutoSize = true;
            this.labelX3.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.Class = "";
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX3.Location = new System.Drawing.Point(432, 12);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(47, 22);
            this.labelX3.TabIndex = 85;
            this.labelX3.Text = "類型：";
            // 
            // labelX2
            // 
            this.labelX2.AutoSize = true;
            this.labelX2.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.Class = "";
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX2.Location = new System.Drawing.Point(232, 12);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(47, 22);
            this.labelX2.TabIndex = 84;
            this.labelX2.Text = "學期：";
            // 
            // ClassName
            // 
            this.ClassName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ClassName.HeaderText = "學生班級";
            this.ClassName.MinimumWidth = 85;
            this.ClassName.Name = "ClassName";
            this.ClassName.ReadOnly = true;
            this.ClassName.Width = 85;
            // 
            // btnExportToExcel
            // 
            this.btnExportToExcel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExportToExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExportToExcel.BackColor = System.Drawing.Color.Transparent;
            this.btnExportToExcel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExportToExcel.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnExportToExcel.Location = new System.Drawing.Point(12, 646);
            this.btnExportToExcel.Name = "btnExportToExcel";
            this.btnExportToExcel.Size = new System.Drawing.Size(91, 28);
            this.btnExportToExcel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExportToExcel.TabIndex = 90;
            this.btnExportToExcel.Text = "匯出";
            this.btnExportToExcel.Click += new System.EventHandler(this.btnExportToExcel_Click);
            // 
            // RankName
            // 
            this.RankName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.RankName.HeaderText = "母群名稱";
            this.RankName.MinimumWidth = 85;
            this.RankName.Name = "RankName";
            this.RankName.ReadOnly = true;
            this.RankName.Width = 85;
            // 
            // lbMemo
            // 
            this.lbMemo.AutoSize = true;
            this.lbMemo.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbMemo.BackgroundStyle.Class = "";
            this.lbMemo.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbMemo.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbMemo.Location = new System.Drawing.Point(65, 129);
            this.lbMemo.Name = "lbMemo";
            this.lbMemo.Size = new System.Drawing.Size(32, 22);
            this.lbMemo.TabIndex = 101;
            this.lbMemo.Text = "說明";
            // 
            // labelX11
            // 
            this.labelX11.AutoSize = true;
            this.labelX11.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX11.BackgroundStyle.Class = "";
            this.labelX11.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX11.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX11.Location = new System.Drawing.Point(12, 129);
            this.labelX11.Name = "labelX11";
            this.labelX11.Size = new System.Drawing.Size(47, 22);
            this.labelX11.TabIndex = 100;
            this.labelX11.Text = "說明：";
            // 
            // cboBatchId
            // 
            this.cboBatchId.DisplayMember = "Text";
            this.cboBatchId.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboBatchId.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBatchId.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboBatchId.FormattingEnabled = true;
            this.cboBatchId.ItemHeight = 21;
            this.cboBatchId.Location = new System.Drawing.Point(65, 88);
            this.cboBatchId.Name = "cboBatchId";
            this.cboBatchId.Size = new System.Drawing.Size(360, 27);
            this.cboBatchId.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboBatchId.TabIndex = 99;
            this.cboBatchId.SelectedIndexChanged += new System.EventHandler(this.LoadRowData);
            // 
            // labelX9
            // 
            this.labelX9.AutoSize = true;
            this.labelX9.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX9.BackgroundStyle.Class = "";
            this.labelX9.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX9.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX9.Location = new System.Drawing.Point(12, 88);
            this.labelX9.Name = "labelX9";
            this.labelX9.Size = new System.Drawing.Size(47, 22);
            this.labelX9.TabIndex = 98;
            this.labelX9.Text = "編號：";
            // 
            // lbRankType
            // 
            this.lbRankType.AutoSize = true;
            this.lbRankType.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbRankType.BackgroundStyle.Class = "";
            this.lbRankType.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbRankType.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbRankType.Location = new System.Drawing.Point(488, 49);
            this.lbRankType.Name = "lbRankType";
            this.lbRankType.Size = new System.Drawing.Size(32, 22);
            this.lbRankType.TabIndex = 97;
            this.lbRankType.Text = "母群";
            // 
            // lbItemName
            // 
            this.lbItemName.AutoSize = true;
            this.lbItemName.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbItemName.BackgroundStyle.Class = "";
            this.lbItemName.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbItemName.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbItemName.Location = new System.Drawing.Point(288, 49);
            this.lbItemName.Name = "lbItemName";
            this.lbItemName.Size = new System.Drawing.Size(32, 22);
            this.lbItemName.TabIndex = 96;
            this.lbItemName.Text = "項目";
            // 
            // lbScoreCategory
            // 
            this.lbScoreCategory.AutoSize = true;
            this.lbScoreCategory.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbScoreCategory.BackgroundStyle.Class = "";
            this.lbScoreCategory.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbScoreCategory.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbScoreCategory.Location = new System.Drawing.Point(65, 49);
            this.lbScoreCategory.Name = "lbScoreCategory";
            this.lbScoreCategory.Size = new System.Drawing.Size(32, 22);
            this.lbScoreCategory.TabIndex = 95;
            this.lbScoreCategory.Text = "類別";
            // 
            // lbScoreType
            // 
            this.lbScoreType.AutoSize = true;
            this.lbScoreType.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbScoreType.BackgroundStyle.Class = "";
            this.lbScoreType.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbScoreType.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbScoreType.Location = new System.Drawing.Point(488, 12);
            this.lbScoreType.Name = "lbScoreType";
            this.lbScoreType.Size = new System.Drawing.Size(32, 22);
            this.lbScoreType.TabIndex = 94;
            this.lbScoreType.Text = "類別";
            // 
            // lbSemester
            // 
            this.lbSemester.AutoSize = true;
            this.lbSemester.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbSemester.BackgroundStyle.Class = "";
            this.lbSemester.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbSemester.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbSemester.Location = new System.Drawing.Point(288, 12);
            this.lbSemester.Name = "lbSemester";
            this.lbSemester.Size = new System.Drawing.Size(32, 22);
            this.lbSemester.TabIndex = 93;
            this.lbSemester.Text = "學期";
            // 
            // lbSchoolYear
            // 
            this.lbSchoolYear.AutoSize = true;
            this.lbSchoolYear.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbSchoolYear.BackgroundStyle.Class = "";
            this.lbSchoolYear.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbSchoolYear.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbSchoolYear.Location = new System.Drawing.Point(84, 12);
            this.lbSchoolYear.Name = "lbSchoolYear";
            this.lbSchoolYear.Size = new System.Drawing.Size(47, 22);
            this.lbSchoolYear.TabIndex = 92;
            this.lbSchoolYear.Text = "學年度";
            // 
            // btnExit
            // 
            this.btnExit.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.BackColor = System.Drawing.Color.Transparent;
            this.btnExit.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExit.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnExit.Location = new System.Drawing.Point(1111, 646);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(91, 28);
            this.btnExit.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExit.TabIndex = 91;
            this.btnExit.Text = "離開";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // dgvScoreRank
            // 
            this.dgvScoreRank.AllowUserToAddRows = false;
            this.dgvScoreRank.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvScoreRank.BackgroundColor = System.Drawing.Color.White;
            this.dgvScoreRank.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvScoreRank.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MatrixId,
            this.ScoreType,
            this.ScoreCategory,
            this.ItemName,
            this.RankType,
            this.RankName,
            this.ClassName,
            this.SeatNo,
            this.StudentNum,
            this.StudentName,
            this.StudentStatus,
            this.score,
            this.rank,
            this.pr,
            this.percentile,
            this.SchoolYear,
            this.Semester});
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft JhengHei", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvScoreRank.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvScoreRank.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(215)))), ((int)(((byte)(229)))));
            this.dgvScoreRank.HighlightSelectedColumnHeaders = false;
            this.dgvScoreRank.Location = new System.Drawing.Point(12, 168);
            this.dgvScoreRank.MultiSelect = false;
            this.dgvScoreRank.Name = "dgvScoreRank";
            this.dgvScoreRank.ReadOnly = true;
            this.dgvScoreRank.RowTemplate.Height = 24;
            this.dgvScoreRank.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvScoreRank.Size = new System.Drawing.Size(1190, 472);
            this.dgvScoreRank.TabIndex = 89;
            // 
            // MatrixId
            // 
            this.MatrixId.HeaderText = "ID";
            this.MatrixId.Name = "MatrixId";
            this.MatrixId.ReadOnly = true;
            this.MatrixId.Visible = false;
            this.MatrixId.Width = 47;
            // 
            // ScoreType
            // 
            this.ScoreType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ScoreType.HeaderText = "成績類型";
            this.ScoreType.MinimumWidth = 85;
            this.ScoreType.Name = "ScoreType";
            this.ScoreType.ReadOnly = true;
            this.ScoreType.Width = 85;
            // 
            // ScoreCategory
            // 
            this.ScoreCategory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ScoreCategory.HeaderText = "成績類別";
            this.ScoreCategory.MinimumWidth = 85;
            this.ScoreCategory.Name = "ScoreCategory";
            this.ScoreCategory.ReadOnly = true;
            this.ScoreCategory.Width = 85;
            // 
            // ItemName
            // 
            this.ItemName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ItemName.HeaderText = "項目";
            this.ItemName.MinimumWidth = 59;
            this.ItemName.Name = "ItemName";
            this.ItemName.ReadOnly = true;
            this.ItemName.Width = 59;
            // 
            // RankType
            // 
            this.RankType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.RankType.HeaderText = "母群";
            this.RankType.MinimumWidth = 59;
            this.RankType.Name = "RankType";
            this.RankType.ReadOnly = true;
            this.RankType.Width = 59;
            // 
            // SemesterMatrixRankSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1214, 686);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.labelX8);
            this.Controls.Add(this.labelX7);
            this.Controls.Add(this.labelX5);
            this.Controls.Add(this.labelX3);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.btnExportToExcel);
            this.Controls.Add(this.lbMemo);
            this.Controls.Add(this.labelX11);
            this.Controls.Add(this.cboBatchId);
            this.Controls.Add(this.labelX9);
            this.Controls.Add(this.lbRankType);
            this.Controls.Add(this.lbItemName);
            this.Controls.Add(this.lbScoreCategory);
            this.Controls.Add(this.lbScoreType);
            this.Controls.Add(this.lbSemester);
            this.Controls.Add(this.lbSchoolYear);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.dgvScoreRank);
            this.DoubleBuffered = true;
            this.Name = "SemesterMatrixRankSelect";
            this.Text = "學期成績排名母群資料檢所";
            this.Load += new System.EventHandler(this.SemesterMatrixRankSelect_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvScoreRank)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.LabelX labelX1;
        private System.Windows.Forms.DataGridViewTextBoxColumn SeatNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentNum;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn score;
        private System.Windows.Forms.DataGridViewTextBoxColumn rank;
        private System.Windows.Forms.DataGridViewTextBoxColumn pr;
        private System.Windows.Forms.DataGridViewTextBoxColumn percentile;
        private System.Windows.Forms.DataGridViewTextBoxColumn SchoolYear;
        private System.Windows.Forms.DataGridViewTextBoxColumn Semester;
        private DevComponents.DotNetBar.LabelX labelX8;
        private DevComponents.DotNetBar.LabelX labelX7;
        private DevComponents.DotNetBar.LabelX labelX5;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.LabelX labelX2;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClassName;
        private DevComponents.DotNetBar.ButtonX btnExportToExcel;
        private System.Windows.Forms.DataGridViewTextBoxColumn RankName;
        private DevComponents.DotNetBar.LabelX lbMemo;
        private DevComponents.DotNetBar.LabelX labelX11;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboBatchId;
        private DevComponents.DotNetBar.LabelX labelX9;
        private DevComponents.DotNetBar.LabelX lbRankType;
        private DevComponents.DotNetBar.LabelX lbItemName;
        private DevComponents.DotNetBar.LabelX lbScoreCategory;
        private DevComponents.DotNetBar.LabelX lbScoreType;
        private DevComponents.DotNetBar.LabelX lbSemester;
        private DevComponents.DotNetBar.LabelX lbSchoolYear;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private DevComponents.DotNetBar.ButtonX btnExit;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvScoreRank;
        private System.Windows.Forms.DataGridViewTextBoxColumn MatrixId;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScoreType;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScoreCategory;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn RankType;
    }
}