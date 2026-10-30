namespace SHEvaluation.Rank
{
    partial class SemesterRankMotherGroupConfirmForm
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
        {            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.labelX9 = new DevComponents.DotNetBar.LabelX();
            this.labelX8 = new DevComponents.DotNetBar.LabelX();
            this.lbSchoolYear = new DevComponents.DotNetBar.LabelX();
            this.lbSemester = new DevComponents.DotNetBar.LabelX();
            this.labelX13 = new DevComponents.DotNetBar.LabelX();
            this.btnExport = new DevComponents.DotNetBar.ButtonX();
            this.btnImport = new DevComponents.DotNetBar.ButtonX();
            this.btnCacluate = new DevComponents.DotNetBar.ButtonX();
            this.dgvStudentList = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.colClass = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSeatNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStudentNum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStudentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSchoolRank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDeptName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colClassRank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRankType1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRankType2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnPrevious = new DevComponents.DotNetBar.ButtonX();
            this.pbLoading = new System.Windows.Forms.PictureBox();
            this._lblSearchCaption = new System.Windows.Forms.Label();
            this._txtMotherGroupSearch = new System.Windows.Forms.TextBox();
            this._lblMotherGroupSummary = new System.Windows.Forms.Label();
            this._dgvRankPopulation = new System.Windows.Forms.DataGridView();
            this._dgvGroupCounts = new System.Windows.Forms.DataGridView();
            this._btnRestoreParticipation = new System.Windows.Forms.Button();
            this._lblAddStudentCaption = new System.Windows.Forms.Label();
            this._txtAddStudentNumber = new System.Windows.Forms.TextBox();
            this._btnAddHistoryStudent = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStudentList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._dgvRankPopulation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._dgvGroupCounts)).BeginInit();
            this.SuspendLayout();
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
            this.labelX9.ForeColor = System.Drawing.Color.Black;
            this.labelX9.Location = new System.Drawing.Point(12, 12);
            this.labelX9.Name = "labelX9";
            this.labelX9.Size = new System.Drawing.Size(69, 24);
            this.labelX9.TabIndex = 26;
            this.labelX9.Text = "學年度：";
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
            this.labelX8.ForeColor = System.Drawing.Color.Black;
            this.labelX8.Location = new System.Drawing.Point(165, 12);
            this.labelX8.Name = "labelX8";
            this.labelX8.Size = new System.Drawing.Size(54, 24);
            this.labelX8.TabIndex = 27;
            this.labelX8.Text = "學期：";
            // 
            // lbSchoolYear
            // 
            this.lbSchoolYear.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbSchoolYear.BackgroundStyle.Class = "";
            this.lbSchoolYear.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbSchoolYear.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbSchoolYear.ForeColor = System.Drawing.Color.Black;
            this.lbSchoolYear.Location = new System.Drawing.Point(89, 12);
            this.lbSchoolYear.Name = "lbSchoolYear";
            this.lbSchoolYear.Size = new System.Drawing.Size(64, 24);
            this.lbSchoolYear.TabIndex = 29;
            this.lbSchoolYear.Text = "學年度";
            // 
            // lbSemester
            // 
            this.lbSemester.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbSemester.BackgroundStyle.Class = "";
            this.lbSemester.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbSemester.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbSemester.ForeColor = System.Drawing.Color.Black;
            this.lbSemester.Location = new System.Drawing.Point(227, 12);
            this.lbSemester.Name = "lbSemester";
            this.lbSemester.Size = new System.Drawing.Size(40, 24);
            this.lbSemester.TabIndex = 30;
            this.lbSemester.Text = "學期";
            // 
            // labelX13
            // 
            this.labelX13.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX13.BackgroundStyle.Class = "";
            this.labelX13.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX13.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX13.ForeColor = System.Drawing.Color.Black;
            this.labelX13.Location = new System.Drawing.Point(12, 44);
            this.labelX13.Name = "labelX13";
            this.labelX13.Size = new System.Drawing.Size(88, 24);
            this.labelX13.TabIndex = 32;
            this.labelX13.Text = "母群資料：";
            // 
            // btnExport
            // 
            this.btnExport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExport.BackColor = System.Drawing.Color.Transparent;
            this.btnExport.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExport.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnExport.ForeColor = System.Drawing.Color.Black;
            this.btnExport.Location = new System.Drawing.Point(723, 715);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 25);
            this.btnExport.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExport.TabIndex = 33;
            this.btnExport.Text = "匯出";
            this.btnExport.Visible = false;
            // 
            // btnImport
            // 
            this.btnImport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImport.BackColor = System.Drawing.Color.Transparent;
            this.btnImport.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnImport.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnImport.ForeColor = System.Drawing.Color.Black;
            this.btnImport.Location = new System.Drawing.Point(814, 715);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(75, 25);
            this.btnImport.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnImport.TabIndex = 34;
            this.btnImport.Text = "匯入";
            this.btnImport.Visible = false;
            // 
            // btnCacluate
            // 
            this.btnCacluate.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCacluate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCacluate.BackColor = System.Drawing.Color.Transparent;
            this.btnCacluate.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnCacluate.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnCacluate.ForeColor = System.Drawing.Color.Black;
            this.btnCacluate.Location = new System.Drawing.Point(996, 715);
            this.btnCacluate.Name = "btnCacluate";
            this.btnCacluate.Size = new System.Drawing.Size(81, 25);
            this.btnCacluate.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnCacluate.TabIndex = 35;
            this.btnCacluate.Text = "計算排名";
            this.btnCacluate.Click += new System.EventHandler(this.btnCacluate_Click);
            // 
            // dgvStudentList
            // 
            this.dgvStudentList.AllowUserToAddRows = false;
            this.dgvStudentList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvStudentList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStudentList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colClass,
            this.colSeatNum,
            this.colStudentNum,
            this.colStudentName,
            this.colSchoolRank,
            this.colDeptName,
            this.colClassRank,
            this.colRankType1,
            this.colRankType2});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft JhengHei", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvStudentList.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvStudentList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(215)))), ((int)(((byte)(229)))));
            this.dgvStudentList.HighlightSelectedColumnHeaders = false;
            this.dgvStudentList.Location = new System.Drawing.Point(12, 74);
            this.dgvStudentList.Name = "dgvStudentList";
            this.dgvStudentList.ReadOnly = true;
            this.dgvStudentList.RowTemplate.Height = 24;
            this.dgvStudentList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStudentList.Size = new System.Drawing.Size(1065, 429);
            this.dgvStudentList.TabIndex = 36;
            // 
            // colClass
            // 
            this.colClass.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colClass.DataPropertyName = "ClassName";
            this.colClass.HeaderText = "班級";
            this.colClass.MinimumWidth = 59;
            this.colClass.Name = "colClass";
            this.colClass.ReadOnly = true;
            this.colClass.Width = 59;
            // 
            // colSeatNum
            // 
            this.colSeatNum.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colSeatNum.DataPropertyName = "SeatNo";
            this.colSeatNum.HeaderText = "座號";
            this.colSeatNum.MinimumWidth = 59;
            this.colSeatNum.Name = "colSeatNum";
            this.colSeatNum.ReadOnly = true;
            this.colSeatNum.Width = 59;
            // 
            // colStudentNum
            // 
            this.colStudentNum.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colStudentNum.DataPropertyName = "StudentNumber";
            this.colStudentNum.HeaderText = "學號";
            this.colStudentNum.MinimumWidth = 59;
            this.colStudentNum.Name = "colStudentNum";
            this.colStudentNum.ReadOnly = true;
            this.colStudentNum.Width = 59;
            // 
            // colStudentName
            // 
            this.colStudentName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colStudentName.DataPropertyName = "Name";
            this.colStudentName.HeaderText = "姓名";
            this.colStudentName.MinimumWidth = 59;
            this.colStudentName.Name = "colStudentName";
            this.colStudentName.ReadOnly = true;
            this.colStudentName.Width = 59;
            // 
            // colSchoolRank
            // 
            this.colSchoolRank.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colSchoolRank.DataPropertyName = "RankGradeYear";
            this.colSchoolRank.HeaderText = "母群：年排名";
            this.colSchoolRank.MinimumWidth = 111;
            this.colSchoolRank.Name = "colSchoolRank";
            this.colSchoolRank.ReadOnly = true;
            this.colSchoolRank.Width = 111;
            // 
            // colDeptName
            // 
            this.colDeptName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colDeptName.HeaderText = "母群：科排名";
            this.colDeptName.MinimumWidth = 111;
            this.colDeptName.Name = "colDeptName";
            this.colDeptName.ReadOnly = true;
            this.colDeptName.Width = 111;
            // 
            // colClassRank
            // 
            this.colClassRank.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.colClassRank.DataPropertyName = "RankClassName";
            this.colClassRank.HeaderText = "母群：班排名";
            this.colClassRank.MinimumWidth = 111;
            this.colClassRank.Name = "colClassRank";
            this.colClassRank.ReadOnly = true;
            this.colClassRank.Width = 111;
            // 
            // colRankType1
            // 
            this.colRankType1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRankType1.DataPropertyName = "StudentTag1";
            this.colRankType1.HeaderText = "母群：類別一";
            this.colRankType1.MinimumWidth = 111;
            this.colRankType1.Name = "colRankType1";
            this.colRankType1.ReadOnly = true;
            // 
            // colRankType2
            // 
            this.colRankType2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRankType2.DataPropertyName = "StudentTag2";
            this.colRankType2.HeaderText = "母群：類別二";
            this.colRankType2.MinimumWidth = 111;
            this.colRankType2.Name = "colRankType2";
            this.colRankType2.ReadOnly = true;
            // 
            // btnPrevious
            // 
            this.btnPrevious.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnPrevious.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPrevious.BackColor = System.Drawing.Color.Transparent;
            this.btnPrevious.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnPrevious.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnPrevious.ForeColor = System.Drawing.Color.Black;
            this.btnPrevious.Location = new System.Drawing.Point(905, 715);
            this.btnPrevious.Name = "btnPrevious";
            this.btnPrevious.Size = new System.Drawing.Size(75, 25);
            this.btnPrevious.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnPrevious.TabIndex = 37;
            this.btnPrevious.Text = "上一步";
            this.btnPrevious.Click += new System.EventHandler(this.btnPrevious_Click);
            // 
            // pbLoading
            // 
            this.pbLoading.BackColor = System.Drawing.Color.Transparent;
            this.pbLoading.Image = global::SHEvaluation.Rank.Properties.Resources.loading;
            this.pbLoading.Location = new System.Drawing.Point(385, 235);
            this.pbLoading.Name = "pbLoading";
            this.pbLoading.Size = new System.Drawing.Size(32, 32);
            this.pbLoading.TabIndex = 57;
            this.pbLoading.TabStop = false;
            this.pbLoading.Visible = false;
            // 
            // _lblSearchCaption
            // 
            this._lblSearchCaption.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._lblSearchCaption.BackColor = System.Drawing.Color.Transparent;
            this._lblSearchCaption.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this._lblSearchCaption.Location = new System.Drawing.Point(813, 12);
            this._lblSearchCaption.Name = "_lblSearchCaption";
            this._lblSearchCaption.Size = new System.Drawing.Size(60, 24);
            this._lblSearchCaption.TabIndex = 40;
            this._lblSearchCaption.Text = "搜尋：";
            // 
            // _txtMotherGroupSearch
            // 
            this._txtMotherGroupSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._txtMotherGroupSearch.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this._txtMotherGroupSearch.Location = new System.Drawing.Point(877, 8);
            this._txtMotherGroupSearch.Name = "_txtMotherGroupSearch";
            this._txtMotherGroupSearch.Size = new System.Drawing.Size(200, 27);
            this._txtMotherGroupSearch.TabIndex = 41;
            this._txtMotherGroupSearch.TextChanged += new System.EventHandler(this.txtMotherGroupSearch_TextChanged);
            // 
            // _lblMotherGroupSummary
            // 
            this._lblMotherGroupSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._lblMotherGroupSummary.BackColor = System.Drawing.Color.Transparent;
            this._lblMotherGroupSummary.Font = new System.Drawing.Font("Microsoft JhengHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this._lblMotherGroupSummary.Location = new System.Drawing.Point(108, 44);
            this._lblMotherGroupSummary.Name = "_lblMotherGroupSummary";
            this._lblMotherGroupSummary.Size = new System.Drawing.Size(969, 24);
            this._lblMotherGroupSummary.TabIndex = 42;
            this._lblMotherGroupSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dgvRankPopulation
            // 
            this._dgvRankPopulation.AllowUserToAddRows = false;
            this._dgvRankPopulation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._dgvRankPopulation.BackgroundColor = System.Drawing.Color.White;
            this._dgvRankPopulation.Location = new System.Drawing.Point(673, 520);
            this._dgvRankPopulation.Name = "_dgvRankPopulation";
            this._dgvRankPopulation.RowHeadersVisible = false;
            this._dgvRankPopulation.Size = new System.Drawing.Size(404, 168);
            this._dgvRankPopulation.TabIndex = 43;
            // 
            // _dgvGroupCounts
            // 
            this._dgvGroupCounts.AllowUserToAddRows = false;
            this._dgvGroupCounts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._dgvGroupCounts.BackgroundColor = System.Drawing.Color.White;
            this._dgvGroupCounts.Location = new System.Drawing.Point(11, 520);
            this._dgvGroupCounts.Name = "_dgvGroupCounts";
            this._dgvGroupCounts.RowHeadersVisible = false;
            this._dgvGroupCounts.Size = new System.Drawing.Size(656, 168);
            this._dgvGroupCounts.TabIndex = 44;
            // 
            // _btnRestoreParticipation
            // 
            this._btnRestoreParticipation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnRestoreParticipation.BackColor = System.Drawing.Color.Transparent;
            this._btnRestoreParticipation.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this._btnRestoreParticipation.Location = new System.Drawing.Point(601, 715);
            this._btnRestoreParticipation.Name = "_btnRestoreParticipation";
            this._btnRestoreParticipation.Size = new System.Drawing.Size(110, 25);
            this._btnRestoreParticipation.TabIndex = 45;
            this._btnRestoreParticipation.Text = "還原參與";
            this._btnRestoreParticipation.UseVisualStyleBackColor = false;
            this._btnRestoreParticipation.Click += new System.EventHandler(this.btnRestoreParticipation_Click);
            // 
            // _lblAddStudentCaption
            // 
            this._lblAddStudentCaption.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._lblAddStudentCaption.BackColor = System.Drawing.Color.Transparent;
            this._lblAddStudentCaption.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this._lblAddStudentCaption.Location = new System.Drawing.Point(327, 715);
            this._lblAddStudentCaption.Name = "_lblAddStudentCaption";
            this._lblAddStudentCaption.Size = new System.Drawing.Size(56, 25);
            this._lblAddStudentCaption.TabIndex = 46;
            this._lblAddStudentCaption.Text = "學號：";
            this._lblAddStudentCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._lblAddStudentCaption.Visible = false;
            // 
            // _txtAddStudentNumber
            // 
            this._txtAddStudentNumber.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._txtAddStudentNumber.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this._txtAddStudentNumber.Location = new System.Drawing.Point(389, 714);
            this._txtAddStudentNumber.Name = "_txtAddStudentNumber";
            this._txtAddStudentNumber.Size = new System.Drawing.Size(110, 27);
            this._txtAddStudentNumber.TabIndex = 47;
            this._txtAddStudentNumber.Visible = false;
            // 
            // _btnAddHistoryStudent
            // 
            this._btnAddHistoryStudent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnAddHistoryStudent.BackColor = System.Drawing.Color.Transparent;
            this._btnAddHistoryStudent.Font = new System.Drawing.Font("Microsoft JhengHei", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this._btnAddHistoryStudent.Location = new System.Drawing.Point(505, 715);
            this._btnAddHistoryStudent.Name = "_btnAddHistoryStudent";
            this._btnAddHistoryStudent.Size = new System.Drawing.Size(88, 25);
            this._btnAddHistoryStudent.TabIndex = 48;
            this._btnAddHistoryStudent.Text = "加入學生";
            this._btnAddHistoryStudent.UseVisualStyleBackColor = false;
            this._btnAddHistoryStudent.Visible = false;
            this._btnAddHistoryStudent.Click += new System.EventHandler(this.btnAddHistoryStudent_Click);
            // 
            // SemesterRankMotherGroupConfirmForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1089, 752);
            this.Controls.Add(this._btnAddHistoryStudent);
            this.Controls.Add(this._txtAddStudentNumber);
            this.Controls.Add(this._lblAddStudentCaption);
            this.Controls.Add(this._btnRestoreParticipation);
            this.Controls.Add(this._dgvGroupCounts);
            this.Controls.Add(this._dgvRankPopulation);
            this.Controls.Add(this._lblMotherGroupSummary);
            this.Controls.Add(this._txtMotherGroupSearch);
            this.Controls.Add(this._lblSearchCaption);
            this.Controls.Add(this.pbLoading);
            this.Controls.Add(this.btnPrevious);
            this.Controls.Add(this.dgvStudentList);
            this.Controls.Add(this.btnCacluate);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.labelX13);
            this.Controls.Add(this.lbSemester);
            this.Controls.Add(this.lbSchoolYear);
            this.Controls.Add(this.labelX8);
            this.Controls.Add(this.labelX9);
            this.DoubleBuffered = true;
            this.Name = "SemesterRankMotherGroupConfirmForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "確認學期成績排名母群";
            this.Load += new System.EventHandler(this.SemesterRankMotherGroupConfirmForm_Load);
            this.Resize += new System.EventHandler(this.SemesterRankMotherGroupConfirmForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStudentList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._dgvRankPopulation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._dgvGroupCounts)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.LabelX labelX9;
        private DevComponents.DotNetBar.LabelX labelX8;
        private DevComponents.DotNetBar.LabelX lbSchoolYear;
        private DevComponents.DotNetBar.LabelX lbSemester;
        private DevComponents.DotNetBar.LabelX labelX13;
        private DevComponents.DotNetBar.ButtonX btnExport;
        private DevComponents.DotNetBar.ButtonX btnImport;
        private DevComponents.DotNetBar.ButtonX btnCacluate;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvStudentList;
        private System.Windows.Forms.DataGridViewTextBoxColumn colClass;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeatNum;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStudentNum;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStudentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSchoolRank;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDeptName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colClassRank;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRankType1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRankType2;
        private DevComponents.DotNetBar.ButtonX btnPrevious;
        private System.Windows.Forms.PictureBox pbLoading;
        private System.Windows.Forms.Label _lblSearchCaption;
        private System.Windows.Forms.TextBox _txtMotherGroupSearch;
        private System.Windows.Forms.Label _lblMotherGroupSummary;
        private System.Windows.Forms.DataGridView _dgvRankPopulation;
        private System.Windows.Forms.DataGridView _dgvGroupCounts;
        private System.Windows.Forms.Button _btnRestoreParticipation;
        private System.Windows.Forms.Label _lblAddStudentCaption;
        private System.Windows.Forms.TextBox _txtAddStudentNumber;
        private System.Windows.Forms.Button _btnAddHistoryStudent;
    }
}
