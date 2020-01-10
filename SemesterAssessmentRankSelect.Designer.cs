namespace SHEvaluation.Rank
{
    partial class SemesterAssessmentRankSelect
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
            this.MatrixId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ScoreCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RankType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RankName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ClassName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SeatNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ScoreType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.score = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.percentile = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.view = new DevComponents.DotNetBar.Controls.DataGridViewButtonXColumn();
            this.SchoolYear = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StudentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pbLoading = new System.Windows.Forms.PictureBox();
            this.btnExit = new DevComponents.DotNetBar.ButtonX();
            this.labelX8 = new DevComponents.DotNetBar.LabelX();
            this.labelX7 = new DevComponents.DotNetBar.LabelX();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.dgvScoreRank = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.Semester = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.txtStudentNum = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.btnExportToExcel = new DevComponents.DotNetBar.ButtonX();
            this.cboItemName = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.cboScoreCategory = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.cboScoreType = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.cboSemester = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.cboSchoolYear = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.cboRankType = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvScoreRank)).BeginInit();
            this.SuspendLayout();
            // 
            // MatrixId
            // 
            this.MatrixId.HeaderText = "ID";
            this.MatrixId.Name = "MatrixId";
            this.MatrixId.ReadOnly = true;
            this.MatrixId.Visible = false;
            this.MatrixId.Width = 47;
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
            // RankName
            // 
            this.RankName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.RankName.HeaderText = "母群名稱";
            this.RankName.MinimumWidth = 85;
            this.RankName.Name = "RankName";
            this.RankName.ReadOnly = true;
            this.RankName.Width = 85;
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
            // SeatNo
            // 
            this.SeatNo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SeatNo.HeaderText = "座號";
            this.SeatNo.MinimumWidth = 59;
            this.SeatNo.Name = "SeatNo";
            this.SeatNo.ReadOnly = true;
            this.SeatNo.Width = 59;
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
            // StudentNum
            // 
            this.StudentNum.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.StudentNum.HeaderText = "學號";
            this.StudentNum.MinimumWidth = 59;
            this.StudentNum.Name = "StudentNum";
            this.StudentNum.ReadOnly = true;
            this.StudentNum.Width = 59;
            // 
            // score
            // 
            this.score.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle3.NullValue = null;
            this.score.DefaultCellStyle = dataGridViewCellStyle3;
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
            this.percentile.MinimumWidth = 72;
            this.percentile.Name = "percentile";
            this.percentile.ReadOnly = true;
            this.percentile.Width = 72;
            // 
            // view
            // 
            this.view.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.view.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.view.HeaderText = "檢視";
            this.view.Name = "view";
            this.view.ReadOnly = true;
            this.view.Text = "檢視";
            this.view.UseColumnTextForButtonValue = true;
            this.view.Width = 40;
            // 
            // SchoolYear
            // 
            this.SchoolYear.HeaderText = "學年度";
            this.SchoolYear.Name = "SchoolYear";
            this.SchoolYear.ReadOnly = true;
            this.SchoolYear.Visible = false;
            this.SchoolYear.Width = 72;
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
            // pbLoading
            // 
            this.pbLoading.BackColor = System.Drawing.Color.Transparent;
            this.pbLoading.Image = global::SHEvaluation.Rank.Properties.Resources.loading;
            this.pbLoading.Location = new System.Drawing.Point(555, 295);
            this.pbLoading.Name = "pbLoading";
            this.pbLoading.Size = new System.Drawing.Size(34, 31);
            this.pbLoading.TabIndex = 74;
            this.pbLoading.TabStop = false;
            this.pbLoading.Visible = false;
            // 
            // btnExit
            // 
            this.btnExit.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.BackColor = System.Drawing.Color.Transparent;
            this.btnExit.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExit.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnExit.Location = new System.Drawing.Point(1031, 551);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(91, 28);
            this.btnExit.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExit.TabIndex = 73;
            this.btnExit.Text = "離開";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
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
            this.labelX8.Location = new System.Drawing.Point(468, 49);
            this.labelX8.Name = "labelX8";
            this.labelX8.Size = new System.Drawing.Size(54, 24);
            this.labelX8.TabIndex = 63;
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
            this.labelX7.Location = new System.Drawing.Point(233, 49);
            this.labelX7.Name = "labelX7";
            this.labelX7.Size = new System.Drawing.Size(54, 24);
            this.labelX7.TabIndex = 62;
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
            this.labelX5.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX5.Location = new System.Drawing.Point(12, 49);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(54, 24);
            this.labelX5.TabIndex = 61;
            this.labelX5.Text = "類別：";
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
            this.labelX4.Location = new System.Drawing.Point(910, 15);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(54, 24);
            this.labelX4.TabIndex = 60;
            this.labelX4.Text = "學號：";
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
            this.labelX3.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX3.Location = new System.Drawing.Point(468, 12);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(54, 24);
            this.labelX3.TabIndex = 59;
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
            this.labelX2.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX2.Location = new System.Drawing.Point(233, 12);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(54, 24);
            this.labelX2.TabIndex = 58;
            this.labelX2.Text = "學期：";
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
            this.labelX1.Location = new System.Drawing.Point(12, 12);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(69, 24);
            this.labelX1.TabIndex = 57;
            this.labelX1.Text = "學年度：";
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
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvScoreRank.DefaultCellStyle = dataGridViewCellStyle4;
            this.dgvScoreRank.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(215)))), ((int)(((byte)(229)))));
            this.dgvScoreRank.HighlightSelectedColumnHeaders = false;
            this.dgvScoreRank.Location = new System.Drawing.Point(12, 82);
            this.dgvScoreRank.MultiSelect = false;
            this.dgvScoreRank.Name = "dgvScoreRank";
            this.dgvScoreRank.ReadOnly = true;
            this.dgvScoreRank.RowTemplate.Height = 24;
            this.dgvScoreRank.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvScoreRank.Size = new System.Drawing.Size(1110, 463);
            this.dgvScoreRank.TabIndex = 64;
            this.dgvScoreRank.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvScoreRank_CellContentClick);
            // 
            // Semester
            // 
            this.Semester.HeaderText = "學期";
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
            this.txtStudentNum.Location = new System.Drawing.Point(966, 13);
            this.txtStudentNum.Name = "txtStudentNum";
            this.txtStudentNum.Size = new System.Drawing.Size(156, 27);
            this.txtStudentNum.TabIndex = 68;
            this.txtStudentNum.TextChanged += new System.EventHandler(this.FillingDataGridView);
            // 
            // btnExportToExcel
            // 
            this.btnExportToExcel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExportToExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExportToExcel.BackColor = System.Drawing.Color.Transparent;
            this.btnExportToExcel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExportToExcel.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnExportToExcel.Location = new System.Drawing.Point(12, 551);
            this.btnExportToExcel.Name = "btnExportToExcel";
            this.btnExportToExcel.Size = new System.Drawing.Size(91, 28);
            this.btnExportToExcel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExportToExcel.TabIndex = 72;
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
            this.cboItemName.Location = new System.Drawing.Point(289, 48);
            this.cboItemName.Name = "cboItemName";
            this.cboItemName.Size = new System.Drawing.Size(164, 27);
            this.cboItemName.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboItemName.TabIndex = 70;
            this.cboItemName.SelectedIndexChanged += new System.EventHandler(this.FillingDataGridView);
            // 
            // cboScoreCategory
            // 
            this.cboScoreCategory.DisplayMember = "Text";
            this.cboScoreCategory.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboScoreCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScoreCategory.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboScoreCategory.FormattingEnabled = true;
            this.cboScoreCategory.ItemHeight = 21;
            this.cboScoreCategory.Location = new System.Drawing.Point(72, 48);
            this.cboScoreCategory.Name = "cboScoreCategory";
            this.cboScoreCategory.Size = new System.Drawing.Size(159, 27);
            this.cboScoreCategory.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboScoreCategory.TabIndex = 69;
            this.cboScoreCategory.SelectedIndexChanged += new System.EventHandler(this.LoadRowData);
            // 
            // cboScoreType
            // 
            this.cboScoreType.DisplayMember = "Text";
            this.cboScoreType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboScoreType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScoreType.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboScoreType.FormattingEnabled = true;
            this.cboScoreType.ItemHeight = 21;
            this.cboScoreType.Location = new System.Drawing.Point(524, 10);
            this.cboScoreType.Name = "cboScoreType";
            this.cboScoreType.Size = new System.Drawing.Size(124, 27);
            this.cboScoreType.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboScoreType.TabIndex = 67;
            this.cboScoreType.SelectedIndexChanged += new System.EventHandler(this.LoadRowData);
            // 
            // cboSemester
            // 
            this.cboSemester.DisplayMember = "Text";
            this.cboSemester.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboSemester.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSemester.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboSemester.FormattingEnabled = true;
            this.cboSemester.ItemHeight = 21;
            this.cboSemester.Location = new System.Drawing.Point(289, 10);
            this.cboSemester.Name = "cboSemester";
            this.cboSemester.Size = new System.Drawing.Size(76, 27);
            this.cboSemester.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboSemester.TabIndex = 66;
            this.cboSemester.SelectedIndexChanged += new System.EventHandler(this.LoadRowData);
            // 
            // cboSchoolYear
            // 
            this.cboSchoolYear.DisplayMember = "Text";
            this.cboSchoolYear.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboSchoolYear.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSchoolYear.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboSchoolYear.FormattingEnabled = true;
            this.cboSchoolYear.ItemHeight = 21;
            this.cboSchoolYear.Location = new System.Drawing.Point(84, 10);
            this.cboSchoolYear.Name = "cboSchoolYear";
            this.cboSchoolYear.Size = new System.Drawing.Size(85, 27);
            this.cboSchoolYear.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboSchoolYear.TabIndex = 65;
            this.cboSchoolYear.SelectedIndexChanged += new System.EventHandler(this.LoadRowData);
            // 
            // cboRankType
            // 
            this.cboRankType.DisplayMember = "Text";
            this.cboRankType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboRankType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRankType.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboRankType.FormattingEnabled = true;
            this.cboRankType.ItemHeight = 21;
            this.cboRankType.Location = new System.Drawing.Point(524, 48);
            this.cboRankType.Name = "cboRankType";
            this.cboRankType.Size = new System.Drawing.Size(167, 27);
            this.cboRankType.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboRankType.TabIndex = 71;
            this.cboRankType.SelectedIndexChanged += new System.EventHandler(this.FillingDataGridView);
            // 
            // SemesterAssessmentRankSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1134, 591);
            this.Controls.Add(this.pbLoading);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.labelX8);
            this.Controls.Add(this.labelX7);
            this.Controls.Add(this.labelX5);
            this.Controls.Add(this.labelX4);
            this.Controls.Add(this.labelX3);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.dgvScoreRank);
            this.Controls.Add(this.txtStudentNum);
            this.Controls.Add(this.btnExportToExcel);
            this.Controls.Add(this.cboItemName);
            this.Controls.Add(this.cboScoreCategory);
            this.Controls.Add(this.cboScoreType);
            this.Controls.Add(this.cboSemester);
            this.Controls.Add(this.cboSchoolYear);
            this.Controls.Add(this.cboRankType);
            this.DoubleBuffered = true;
            this.Name = "SemesterAssessmentRankSelect";
            this.Text = "學期成績排名資料檢索";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SemesterAssessmentRankSelect_FormClosing);
            this.Load += new System.EventHandler(this.SemesterAssessmentRankSelect_Load);
            this.Resize += new System.EventHandler(this.SemesterAssessmentRankSelect_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvScoreRank)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridViewTextBoxColumn MatrixId;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScoreCategory;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemName;
        private System.Windows.Forms.DataGridViewTextBoxColumn RankType;
        private System.Windows.Forms.DataGridViewTextBoxColumn RankName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ClassName;
        private System.Windows.Forms.DataGridViewTextBoxColumn SeatNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn ScoreType;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentNum;
        private System.Windows.Forms.DataGridViewTextBoxColumn score;
        private System.Windows.Forms.DataGridViewTextBoxColumn rank;
        private System.Windows.Forms.DataGridViewTextBoxColumn pr;
        private System.Windows.Forms.DataGridViewTextBoxColumn percentile;
        private DevComponents.DotNetBar.Controls.DataGridViewButtonXColumn view;
        private System.Windows.Forms.DataGridViewTextBoxColumn SchoolYear;
        private System.Windows.Forms.DataGridViewTextBoxColumn StudentName;
        private System.Windows.Forms.PictureBox pbLoading;
        private DevComponents.DotNetBar.ButtonX btnExit;
        private DevComponents.DotNetBar.LabelX labelX8;
        private DevComponents.DotNetBar.LabelX labelX7;
        private DevComponents.DotNetBar.LabelX labelX5;
        private DevComponents.DotNetBar.LabelX labelX4;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvScoreRank;
        private System.Windows.Forms.DataGridViewTextBoxColumn Semester;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private DevComponents.DotNetBar.Controls.TextBoxX txtStudentNum;
        private DevComponents.DotNetBar.ButtonX btnExportToExcel;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboItemName;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboScoreCategory;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboScoreType;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboSemester;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboSchoolYear;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboRankType;
    }
}