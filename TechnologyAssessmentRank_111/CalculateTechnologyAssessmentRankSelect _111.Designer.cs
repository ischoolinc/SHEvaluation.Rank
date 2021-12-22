namespace SHEvaluation.Rank
{
    partial class CalculateTechnologyAssessmentRankSelect_111
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnExit = new DevComponents.DotNetBar.ButtonX();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.pbLoading = new System.Windows.Forms.PictureBox();
            this.labelX8 = new DevComponents.DotNetBar.LabelX();
            this.labelX7 = new DevComponents.DotNetBar.LabelX();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.dgvScoreRank = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.MatrixId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ScoreType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ScoreCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RankType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RankName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ClassName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SeatNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.percentile = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.view = new DevComponents.DotNetBar.Controls.DataGridViewButtonXColumn();
            this.SchoolYear = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Semester = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtStudentNum = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.btnExportToExcel = new DevComponents.DotNetBar.ButtonX();
            this.cboItemName = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.cboRankType = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.lblRowCount = new DevComponents.DotNetBar.LabelX();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.cboScoreCategory = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvScoreRank)).BeginInit();
            this.SuspendLayout();
            // 
            // btnExit
            // 
            this.btnExit.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.BackColor = System.Drawing.Color.Transparent;
            this.btnExit.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExit.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnExit.Location = new System.Drawing.Point(1031, 552);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(91, 28);
            this.btnExit.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExit.TabIndex = 91;
            this.btnExit.Text = "離開";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // pbLoading
            // 
            this.pbLoading.BackColor = System.Drawing.Color.Transparent;
            this.pbLoading.Image = global::SHEvaluation.Rank.Properties.Resources.loading;
            this.pbLoading.Location = new System.Drawing.Point(555, 296);
            this.pbLoading.Name = "pbLoading";
            this.pbLoading.Size = new System.Drawing.Size(34, 31);
            this.pbLoading.TabIndex = 92;
            this.pbLoading.TabStop = false;
            this.pbLoading.Visible = false;
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
            this.labelX8.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX8.Location = new System.Drawing.Point(565, 17);
            this.labelX8.Name = "labelX8";
            this.labelX8.Size = new System.Drawing.Size(67, 30);
            this.labelX8.TabIndex = 81;
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
            this.labelX7.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX7.Location = new System.Drawing.Point(330, 17);
            this.labelX7.Name = "labelX7";
            this.labelX7.Size = new System.Drawing.Size(67, 30);
            this.labelX7.TabIndex = 80;
            this.labelX7.Text = "項目：";
            // 
            // labelX4
            // 
            this.labelX4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelX4.AutoSize = true;
            this.labelX4.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX4.BackgroundStyle.Class = "";
            this.labelX4.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX4.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX4.Location = new System.Drawing.Point(910, 16);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(67, 30);
            this.labelX4.TabIndex = 78;
            this.labelX4.Text = "學號：";
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
            this.score,
            this.rank,
            this.pr,
            this.percentile,
            this.view,
            this.SchoolYear,
            this.Semester});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvScoreRank.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvScoreRank.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(215)))), ((int)(((byte)(229)))));
            this.dgvScoreRank.HighlightSelectedColumnHeaders = false;
            this.dgvScoreRank.Location = new System.Drawing.Point(12, 63);
            this.dgvScoreRank.MultiSelect = false;
            this.dgvScoreRank.Name = "dgvScoreRank";
            this.dgvScoreRank.ReadOnly = true;
            this.dgvScoreRank.RowHeadersWidth = 51;
            this.dgvScoreRank.RowTemplate.Height = 24;
            this.dgvScoreRank.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvScoreRank.Size = new System.Drawing.Size(1110, 483);
            this.dgvScoreRank.TabIndex = 82;
            // 
            // MatrixId
            // 
            this.MatrixId.HeaderText = "ID";
            this.MatrixId.MinimumWidth = 6;
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
            this.ScoreType.Visible = false;
            this.ScoreType.Width = 125;
            // 
            // ScoreCategory
            // 
            this.ScoreCategory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ScoreCategory.HeaderText = "成績類別";
            this.ScoreCategory.MinimumWidth = 85;
            this.ScoreCategory.Name = "ScoreCategory";
            this.ScoreCategory.ReadOnly = true;
            this.ScoreCategory.Visible = false;
            this.ScoreCategory.Width = 125;
            // 
            // ItemName
            // 
            this.ItemName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ItemName.HeaderText = "項目";
            this.ItemName.MinimumWidth = 59;
            this.ItemName.Name = "ItemName";
            this.ItemName.ReadOnly = true;
            this.ItemName.Width = 73;
            // 
            // RankType
            // 
            this.RankType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.RankType.HeaderText = "母群";
            this.RankType.MinimumWidth = 59;
            this.RankType.Name = "RankType";
            this.RankType.ReadOnly = true;
            this.RankType.Width = 73;
            // 
            // RankName
            // 
            this.RankName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.RankName.HeaderText = "母群名稱";
            this.RankName.MinimumWidth = 85;
            this.RankName.Name = "RankName";
            this.RankName.ReadOnly = true;
            this.RankName.Width = 107;
            // 
            // ClassName
            // 
            this.ClassName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.ClassName.HeaderText = "學生班級";
            this.ClassName.MinimumWidth = 85;
            this.ClassName.Name = "ClassName";
            this.ClassName.ReadOnly = true;
            this.ClassName.Width = 107;
            // 
            // SeatNo
            // 
            this.SeatNo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SeatNo.HeaderText = "座號";
            this.SeatNo.MinimumWidth = 59;
            this.SeatNo.Name = "SeatNo";
            this.SeatNo.ReadOnly = true;
            this.SeatNo.Width = 73;
            // 
            // StudentNum
            // 
            this.StudentNum.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.StudentNum.HeaderText = "學號";
            this.StudentNum.MinimumWidth = 59;
            this.StudentNum.Name = "StudentNum";
            this.StudentNum.ReadOnly = true;
            this.StudentNum.Width = 73;
            // 
            // StudentName
            // 
            this.StudentName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.StudentName.HeaderText = "姓名";
            this.StudentName.MinimumWidth = 59;
            this.StudentName.Name = "StudentName";
            this.StudentName.ReadOnly = true;
            this.StudentName.Width = 73;
            // 
            // score
            // 
            this.score.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle1.NullValue = null;
            this.score.DefaultCellStyle = dataGridViewCellStyle1;
            this.score.HeaderText = "排名分數";
            this.score.MinimumWidth = 85;
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
            this.rank.Width = 68;
            // 
            // pr
            // 
            this.pr.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.pr.HeaderText = "PR";
            this.pr.MinimumWidth = 49;
            this.pr.Name = "pr";
            this.pr.ReadOnly = true;
            this.pr.Width = 60;
            // 
            // percentile
            // 
            this.percentile.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.percentile.HeaderText = "百分比";
            this.percentile.MinimumWidth = 72;
            this.percentile.Name = "percentile";
            this.percentile.ReadOnly = true;
            this.percentile.Width = 83;
            // 
            // view
            // 
            this.view.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.view.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.view.HeaderText = "檢視";
            this.view.MinimumWidth = 6;
            this.view.Name = "view";
            this.view.ReadOnly = true;
            this.view.Text = "檢視";
            this.view.UseColumnTextForButtonValue = true;
            this.view.Visible = false;
            this.view.Width = 125;
            // 
            // SchoolYear
            // 
            this.SchoolYear.HeaderText = "學年度";
            this.SchoolYear.MinimumWidth = 6;
            this.SchoolYear.Name = "SchoolYear";
            this.SchoolYear.ReadOnly = true;
            this.SchoolYear.Visible = false;
            this.SchoolYear.Width = 72;
            // 
            // Semester
            // 
            this.Semester.HeaderText = "學期";
            this.Semester.MinimumWidth = 6;
            this.Semester.Name = "Semester";
            this.Semester.ReadOnly = true;
            this.Semester.Visible = false;
            this.Semester.Width = 59;
            // 
            // txtStudentNum
            // 
            this.txtStudentNum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.txtStudentNum.Border.Class = "TextBoxBorder";
            this.txtStudentNum.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtStudentNum.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.txtStudentNum.Location = new System.Drawing.Point(966, 14);
            this.txtStudentNum.Name = "txtStudentNum";
            this.txtStudentNum.Size = new System.Drawing.Size(156, 39);
            this.txtStudentNum.TabIndex = 86;
            this.txtStudentNum.TextChanged += new System.EventHandler(this.FillingDataGridView);
            // 
            // btnExportToExcel
            // 
            this.btnExportToExcel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExportToExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExportToExcel.BackColor = System.Drawing.Color.Transparent;
            this.btnExportToExcel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExportToExcel.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnExportToExcel.Location = new System.Drawing.Point(12, 552);
            this.btnExportToExcel.Name = "btnExportToExcel";
            this.btnExportToExcel.Size = new System.Drawing.Size(91, 28);
            this.btnExportToExcel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExportToExcel.TabIndex = 90;
            this.btnExportToExcel.Text = "匯出";
            this.btnExportToExcel.Click += new System.EventHandler(this.btnExportToExcel_Click);
            // 
            // cboItemName
            // 
            this.cboItemName.DisplayMember = "Text";
            this.cboItemName.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboItemName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboItemName.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboItemName.FormattingEnabled = true;
            this.cboItemName.ItemHeight = 21;
            this.cboItemName.Location = new System.Drawing.Point(386, 16);
            this.cboItemName.Name = "cboItemName";
            this.cboItemName.Size = new System.Drawing.Size(164, 27);
            this.cboItemName.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboItemName.TabIndex = 88;
            this.cboItemName.SelectedIndexChanged += new System.EventHandler(this.FillingDataGridView);
            // 
            // cboRankType
            // 
            this.cboRankType.DisplayMember = "Text";
            this.cboRankType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboRankType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRankType.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboRankType.FormattingEnabled = true;
            this.cboRankType.ItemHeight = 21;
            this.cboRankType.Location = new System.Drawing.Point(621, 16);
            this.cboRankType.Name = "cboRankType";
            this.cboRankType.Size = new System.Drawing.Size(167, 27);
            this.cboRankType.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboRankType.TabIndex = 89;
            this.cboRankType.SelectedIndexChanged += new System.EventHandler(this.FillingDataGridView);
            // 
            // lblRowCount
            // 
            this.lblRowCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblRowCount.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lblRowCount.BackgroundStyle.Class = "";
            this.lblRowCount.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblRowCount.Location = new System.Drawing.Point(123, 555);
            this.lblRowCount.Name = "lblRowCount";
            this.lblRowCount.Size = new System.Drawing.Size(290, 23);
            this.lblRowCount.TabIndex = 93;
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
            this.labelX1.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX1.Location = new System.Drawing.Point(12, 17);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(67, 30);
            this.labelX1.TabIndex = 94;
            this.labelX1.Text = "類別：";
            // 
            // cboScoreCategory
            // 
            this.cboScoreCategory.DisplayMember = "Text";
            this.cboScoreCategory.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboScoreCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScoreCategory.DropDownWidth = 256;
            this.cboScoreCategory.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboScoreCategory.FormattingEnabled = true;
            this.cboScoreCategory.ItemHeight = 21;
            this.cboScoreCategory.Location = new System.Drawing.Point(68, 16);
            this.cboScoreCategory.Name = "cboScoreCategory";
            this.cboScoreCategory.Size = new System.Drawing.Size(256, 27);
            this.cboScoreCategory.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboScoreCategory.TabIndex = 95;
            this.cboScoreCategory.SelectedIndexChanged += new System.EventHandler(this.LoadRowData);
            // 
            // CalculateTechnologyAssessmentRankSelect_111
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1134, 591);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.cboScoreCategory);
            this.Controls.Add(this.lblRowCount);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.pbLoading);
            this.Controls.Add(this.labelX8);
            this.Controls.Add(this.labelX7);
            this.Controls.Add(this.labelX4);
            this.Controls.Add(this.dgvScoreRank);
            this.Controls.Add(this.txtStudentNum);
            this.Controls.Add(this.btnExportToExcel);
            this.Controls.Add(this.cboItemName);
            this.Controls.Add(this.cboRankType);
            this.DoubleBuffered = true;
            this.Name = "CalculateTechnologyAssessmentRankSelect_111";
            this.Text = "技職繁星成績排名資料檢索";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CalculateTechnologyAssessmentRankSelect_FormClosing);
            this.Load += new System.EventHandler(this.CalculateTechnologyAssessmentRankSelect_Load);
            this.Resize += new System.EventHandler(this.CalculateTechnologyAssessmentRankSelect_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvScoreRank)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.ButtonX btnExit;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.PictureBox pbLoading;
        private DevComponents.DotNetBar.LabelX labelX8;
        private DevComponents.DotNetBar.LabelX labelX7;
        private DevComponents.DotNetBar.LabelX labelX4;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvScoreRank;
        private DevComponents.DotNetBar.Controls.TextBoxX txtStudentNum;
        private DevComponents.DotNetBar.ButtonX btnExportToExcel;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboItemName;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboRankType;
        private System.Windows.Forms.DataGridViewTextBoxColumn MatrixId;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScoreType;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScoreCategory;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn RankType;
        private System.Windows.Forms.DataGridViewTextBoxColumn RankName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClassName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SeatNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentNum;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn score;
        private System.Windows.Forms.DataGridViewTextBoxColumn rank;
        private System.Windows.Forms.DataGridViewTextBoxColumn pr;
        private System.Windows.Forms.DataGridViewTextBoxColumn percentile;
        private DevComponents.DotNetBar.Controls.DataGridViewButtonXColumn view;
        private System.Windows.Forms.DataGridViewTextBoxColumn SchoolYear;
        private System.Windows.Forms.DataGridViewTextBoxColumn Semester;
        private DevComponents.DotNetBar.LabelX lblRowCount;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboScoreCategory;
    }
}