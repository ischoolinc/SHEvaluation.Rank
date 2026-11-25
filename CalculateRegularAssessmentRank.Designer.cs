namespace SHEvaluation.Rank
{
    partial class CalculateRegularAssessmentRank
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
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.cboStudentTag2 = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.cboStudentTag1 = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.cboStudentFilter = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.btnNext = new DevComponents.DotNetBar.ButtonX();
            this.cboExamType = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX6 = new DevComponents.DotNetBar.LabelX();
            this.lbCalcSchoolYear = new DevComponents.DotNetBar.LabelX();
            this.lbCalcSemester = new DevComponents.DotNetBar.LabelX();
            this.nudTargetSchoolYear = new System.Windows.Forms.NumericUpDown();
            this.cboTargetSemester = new System.Windows.Forms.ComboBox();
            this.lblSourceCaption = new System.Windows.Forms.Label();
            this.cboMotherGroupSource = new System.Windows.Forms.ComboBox();
            this.lblCurrentSourceNote = new System.Windows.Forms.Label();
            this.lblSystemPeriod = new System.Windows.Forms.Label();
            this.lblSourceYear = new System.Windows.Forms.Label();
            this.nudSourceSchoolYear = new System.Windows.Forms.NumericUpDown();
            this.lblSourceSemester = new System.Windows.Forms.Label();
            this.cboSourceSemester = new System.Windows.Forms.ComboBox();
            this.lblSourceExam = new System.Windows.Forms.Label();
            this.cboSourceExam = new System.Windows.Forms.ComboBox();
            this.lblSourceBatch = new System.Windows.Forms.Label();
            this.cboSourceBatch = new System.Windows.Forms.ComboBox();
            this.lblExcelIdentity = new System.Windows.Forms.Label();
            this.txtExcelIdentity = new System.Windows.Forms.TextBox();
            this.btnLoadMotherGroup = new DevComponents.DotNetBar.ButtonX();
            this.lblRankStatus = new System.Windows.Forms.Label();
            this.plSetting = new DevComponents.DotNetBar.PanelEx();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lvCalcSubjectTag2 = new DevComponents.DotNetBar.Controls.ListViewEx();
            this.labelX14 = new DevComponents.DotNetBar.LabelX();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lvCalcSubjectTag1 = new DevComponents.DotNetBar.Controls.ListViewEx();
            this.labelX12 = new DevComponents.DotNetBar.LabelX();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.labelX10 = new DevComponents.DotNetBar.LabelX();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lvCalcSubject = new DevComponents.DotNetBar.Controls.ListViewEx();
            this.labelX11 = new DevComponents.DotNetBar.LabelX();
            ((System.ComponentModel.ISupportInitialize)(this.nudTargetSchoolYear)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSourceSchoolYear)).BeginInit();
            this.plSetting.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.panel1.SuspendLayout();
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
            this.labelX1.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX1.Location = new System.Drawing.Point(12, 12);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(60, 21);
            this.labelX1.TabIndex = 0;
            this.labelX1.Text = "目標學年度：";
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
            this.labelX2.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX2.Location = new System.Drawing.Point(190, 12);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(47, 21);
            this.labelX2.TabIndex = 2;
            this.labelX2.Text = "目標學期：";
            // 
            // cboStudentTag2
            // 
            this.cboStudentTag2.DisplayMember = "Text";
            this.cboStudentTag2.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboStudentTag2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStudentTag2.Enabled = false;
            this.cboStudentTag2.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboStudentTag2.FormattingEnabled = true;
            this.cboStudentTag2.ItemHeight = 21;
            this.cboStudentTag2.Location = new System.Drawing.Point(93, 3);
            this.cboStudentTag2.Name = "cboStudentTag2";
            this.cboStudentTag2.Size = new System.Drawing.Size(164, 27);
            this.cboStudentTag2.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboStudentTag2.TabIndex = 6;
            this.cboStudentTag2.SelectedIndexChanged += new System.EventHandler(this.cboStudentTag2_SelectedIndexChanged);
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
            this.labelX5.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX5.Location = new System.Drawing.Point(0, 6);
            this.labelX5.Margin = new System.Windows.Forms.Padding(0, 6, 3, 3);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(87, 21);
            this.labelX5.TabIndex = 5;
            this.labelX5.Text = "類別排名二：";
            // 
            // cboStudentTag1
            // 
            this.cboStudentTag1.DisplayMember = "Text";
            this.cboStudentTag1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboStudentTag1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStudentTag1.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboStudentTag1.FormattingEnabled = true;
            this.cboStudentTag1.ItemHeight = 21;
            this.cboStudentTag1.Location = new System.Drawing.Point(93, 3);
            this.cboStudentTag1.Name = "cboStudentTag1";
            this.cboStudentTag1.Size = new System.Drawing.Size(164, 27);
            this.cboStudentTag1.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboStudentTag1.TabIndex = 4;
            this.cboStudentTag1.SelectedIndexChanged += new System.EventHandler(this.cboStudentTag1_SelectedIndexChanged);
            // 
            // labelX4
            // 
            this.labelX4.AutoSize = true;
            this.labelX4.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX4.BackgroundStyle.Class = "";
            this.labelX4.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX4.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX4.Location = new System.Drawing.Point(0, 6);
            this.labelX4.Margin = new System.Windows.Forms.Padding(0, 6, 3, 3);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(87, 21);
            this.labelX4.TabIndex = 3;
            this.labelX4.Text = "類別排名一：";
            // 
            // cboStudentFilter
            // 
            this.cboStudentFilter.DisplayMember = "Text";
            this.cboStudentFilter.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboStudentFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStudentFilter.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboStudentFilter.FormattingEnabled = true;
            this.cboStudentFilter.ItemHeight = 21;
            this.cboStudentFilter.Location = new System.Drawing.Point(93, 3);
            this.cboStudentFilter.Name = "cboStudentFilter";
            this.cboStudentFilter.Size = new System.Drawing.Size(164, 27);
            this.cboStudentFilter.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboStudentFilter.TabIndex = 2;
            this.cboStudentFilter.SelectedIndexChanged += new System.EventHandler(this.cboStudentFilter_SelectedIndexChanged);
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
            this.labelX3.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX3.Location = new System.Drawing.Point(0, 6);
            this.labelX3.Margin = new System.Windows.Forms.Padding(0, 6, 3, 3);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(87, 21);
            this.labelX3.TabIndex = 1;
            this.labelX3.Text = "不排名學生：";
            // 
            // btnNext
            // 
            this.btnNext.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNext.BackColor = System.Drawing.Color.Transparent;
            this.btnNext.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnNext.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnNext.Location = new System.Drawing.Point(772, 663);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(75, 25);
            this.btnNext.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnNext.Enabled = false;
            this.btnNext.TabIndex = 7;
            this.btnNext.Text = "下一步";
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // cboExamType
            // 
            this.cboExamType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cboExamType.DisplayMember = "Display";
            this.cboExamType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboExamType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboExamType.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboExamType.FormattingEnabled = true;
            this.cboExamType.ItemHeight = 21;
            this.cboExamType.Location = new System.Drawing.Point(685, 9);
            this.cboExamType.Name = "cboExamType";
            this.cboExamType.Size = new System.Drawing.Size(159, 27);
            this.cboExamType.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboExamType.TabIndex = 5;
            this.cboExamType.SelectedIndexChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            // 
            // labelX6
            // 
            this.labelX6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelX6.AutoSize = true;
            this.labelX6.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX6.BackgroundStyle.Class = "";
            this.labelX6.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX6.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX6.Location = new System.Drawing.Point(584, 12);
            this.labelX6.Name = "labelX6";
            this.labelX6.Size = new System.Drawing.Size(101, 21);
            this.labelX6.TabIndex = 4;
            this.labelX6.Text = "目標考試：";
            // 
            // lbCalcSchoolYear
            // 
            this.lbCalcSchoolYear.AutoSize = true;
            this.lbCalcSchoolYear.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbCalcSchoolYear.BackgroundStyle.Class = "";
            this.lbCalcSchoolYear.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbCalcSchoolYear.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbCalcSchoolYear.Location = new System.Drawing.Point(78, 12);
            this.lbCalcSchoolYear.Name = "lbCalcSchoolYear";
            this.lbCalcSchoolYear.Size = new System.Drawing.Size(47, 21);
            this.lbCalcSchoolYear.TabIndex = 1;
            this.lbCalcSchoolYear.Text = "學年度";
            this.lbCalcSchoolYear.Visible = false;
            // 
            // lbCalcSemester
            // 
            this.lbCalcSemester.AutoSize = true;
            this.lbCalcSemester.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lbCalcSemester.BackgroundStyle.Class = "";
            this.lbCalcSemester.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lbCalcSemester.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbCalcSemester.Location = new System.Drawing.Point(189, 12);
            this.lbCalcSemester.Name = "lbCalcSemester";
            this.lbCalcSemester.Size = new System.Drawing.Size(34, 21);
            this.lbCalcSemester.TabIndex = 3;
            this.lbCalcSemester.Text = "學期";
            this.lbCalcSemester.Visible = false;
            //
            // nudTargetSchoolYear
            //
            this.nudTargetSchoolYear.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.nudTargetSchoolYear.Location = new System.Drawing.Point(112, 8);
            this.nudTargetSchoolYear.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.nudTargetSchoolYear.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudTargetSchoolYear.Name = "nudTargetSchoolYear";
            this.nudTargetSchoolYear.Size = new System.Drawing.Size(68, 25);
            this.nudTargetSchoolYear.TabIndex = 30;
            this.nudTargetSchoolYear.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudTargetSchoolYear.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudTargetSchoolYear.ValueChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // cboTargetSemester
            //
            this.cboTargetSemester.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTargetSemester.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboTargetSemester.FormattingEnabled = true;
            this.cboTargetSemester.Items.AddRange(new object[] {
            "1",
            "2"});
            this.cboTargetSemester.Location = new System.Drawing.Point(270, 8);
            this.cboTargetSemester.Name = "cboTargetSemester";
            this.cboTargetSemester.Size = new System.Drawing.Size(48, 25);
            this.cboTargetSemester.TabIndex = 31;
            this.cboTargetSemester.SelectedIndexChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // lblSourceCaption
            //
            this.lblSourceCaption.AutoSize = true;
            this.lblSourceCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblSourceCaption.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblSourceCaption.Location = new System.Drawing.Point(12, 44);
            this.lblSourceCaption.Name = "lblSourceCaption";
            this.lblSourceCaption.Size = new System.Drawing.Size(73, 17);
            this.lblSourceCaption.TabIndex = 32;
            this.lblSourceCaption.Text = "母群來源：";
            //
            // cboMotherGroupSource
            //
            this.cboMotherGroupSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMotherGroupSource.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboMotherGroupSource.FormattingEnabled = true;
            this.cboMotherGroupSource.Items.AddRange(new object[] {
            "目前學生",
            "Excel",
            "歷史固定排名"});
            this.cboMotherGroupSource.Location = new System.Drawing.Point(96, 40);
            this.cboMotherGroupSource.Name = "cboMotherGroupSource";
            this.cboMotherGroupSource.Size = new System.Drawing.Size(140, 25);
            this.cboMotherGroupSource.TabIndex = 33;
            this.cboMotherGroupSource.SelectedIndexChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // lblCurrentSourceNote
            //
            this.lblCurrentSourceNote.AutoSize = true;
            this.lblCurrentSourceNote.BackColor = System.Drawing.Color.Transparent;
            this.lblCurrentSourceNote.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblCurrentSourceNote.Location = new System.Drawing.Point(246, 44);
            this.lblCurrentSourceNote.Name = "lblCurrentSourceNote";
            this.lblCurrentSourceNote.Size = new System.Drawing.Size(259, 17);
            this.lblCurrentSourceNote.TabIndex = 34;
            this.lblCurrentSourceNote.Text = "依目前學生母群計算指定期間成績";
            //
            // lblSystemPeriod
            //
            this.lblSystemPeriod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSystemPeriod.BackColor = System.Drawing.Color.Transparent;
            this.lblSystemPeriod.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblSystemPeriod.Location = new System.Drawing.Point(620, 44);
            this.lblSystemPeriod.Name = "lblSystemPeriod";
            this.lblSystemPeriod.Size = new System.Drawing.Size(220, 21);
            this.lblSystemPeriod.TabIndex = 35;
            this.lblSystemPeriod.Text = "系統期間";
            this.lblSystemPeriod.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblSourceYear
            //
            this.lblSourceYear.AutoSize = true;
            this.lblSourceYear.BackColor = System.Drawing.Color.Transparent;
            this.lblSourceYear.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblSourceYear.Location = new System.Drawing.Point(12, 76);
            this.lblSourceYear.Name = "lblSourceYear";
            this.lblSourceYear.Size = new System.Drawing.Size(86, 17);
            this.lblSourceYear.TabIndex = 36;
            this.lblSourceYear.Text = "來源學年度：";
            //
            // nudSourceSchoolYear
            //
            this.nudSourceSchoolYear.Enabled = false;
            this.nudSourceSchoolYear.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.nudSourceSchoolYear.Location = new System.Drawing.Point(104, 72);
            this.nudSourceSchoolYear.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.nudSourceSchoolYear.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSourceSchoolYear.Name = "nudSourceSchoolYear";
            this.nudSourceSchoolYear.Size = new System.Drawing.Size(68, 25);
            this.nudSourceSchoolYear.TabIndex = 37;
            this.nudSourceSchoolYear.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudSourceSchoolYear.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSourceSchoolYear.ValueChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // lblSourceSemester
            //
            this.lblSourceSemester.AutoSize = true;
            this.lblSourceSemester.BackColor = System.Drawing.Color.Transparent;
            this.lblSourceSemester.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblSourceSemester.Location = new System.Drawing.Point(180, 76);
            this.lblSourceSemester.Name = "lblSourceSemester";
            this.lblSourceSemester.Size = new System.Drawing.Size(73, 17);
            this.lblSourceSemester.TabIndex = 38;
            this.lblSourceSemester.Text = "來源學期：";
            //
            // cboSourceSemester
            //
            this.cboSourceSemester.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSourceSemester.Enabled = false;
            this.cboSourceSemester.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboSourceSemester.FormattingEnabled = true;
            this.cboSourceSemester.Items.AddRange(new object[] {
            "1",
            "2"});
            this.cboSourceSemester.Location = new System.Drawing.Point(258, 72);
            this.cboSourceSemester.Name = "cboSourceSemester";
            this.cboSourceSemester.Size = new System.Drawing.Size(48, 25);
            this.cboSourceSemester.TabIndex = 39;
            this.cboSourceSemester.SelectedIndexChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // lblSourceExam
            //
            this.lblSourceExam.AutoSize = true;
            this.lblSourceExam.BackColor = System.Drawing.Color.Transparent;
            this.lblSourceExam.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblSourceExam.Location = new System.Drawing.Point(316, 76);
            this.lblSourceExam.Name = "lblSourceExam";
            this.lblSourceExam.Size = new System.Drawing.Size(73, 17);
            this.lblSourceExam.TabIndex = 40;
            this.lblSourceExam.Text = "來源考試：";
            //
            // cboSourceExam
            //
            this.cboSourceExam.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSourceExam.Enabled = false;
            this.cboSourceExam.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboSourceExam.FormattingEnabled = true;
            this.cboSourceExam.Location = new System.Drawing.Point(392, 72);
            this.cboSourceExam.Name = "cboSourceExam";
            this.cboSourceExam.Size = new System.Drawing.Size(180, 25);
            this.cboSourceExam.TabIndex = 41;
            this.cboSourceExam.SelectedIndexChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // lblSourceBatch
            //
            this.lblSourceBatch.AutoSize = true;
            this.lblSourceBatch.BackColor = System.Drawing.Color.Transparent;
            this.lblSourceBatch.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblSourceBatch.Location = new System.Drawing.Point(580, 76);
            this.lblSourceBatch.Name = "lblSourceBatch";
            this.lblSourceBatch.Size = new System.Drawing.Size(73, 17);
            this.lblSourceBatch.TabIndex = 42;
            this.lblSourceBatch.Text = "來源批次：";
            //
            // cboSourceBatch
            //
            this.cboSourceBatch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSourceBatch.Enabled = false;
            this.cboSourceBatch.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboSourceBatch.FormattingEnabled = true;
            this.cboSourceBatch.DropDownWidth = 300;
            this.cboSourceBatch.Location = new System.Drawing.Point(656, 72);
            this.cboSourceBatch.Name = "cboSourceBatch";
            this.cboSourceBatch.Size = new System.Drawing.Size(184, 25);
            this.cboSourceBatch.TabIndex = 43;
            this.cboSourceBatch.SelectedIndexChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // lblExcelIdentity
            //
            this.lblExcelIdentity.AutoSize = true;
            this.lblExcelIdentity.BackColor = System.Drawing.Color.Transparent;
            this.lblExcelIdentity.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblExcelIdentity.Location = new System.Drawing.Point(12, 108);
            this.lblExcelIdentity.Name = "lblExcelIdentity";
            this.lblExcelIdentity.Size = new System.Drawing.Size(78, 17);
            this.lblExcelIdentity.TabIndex = 44;
            this.lblExcelIdentity.Text = "Excel 識別：";
            //
            // txtExcelIdentity
            //
            this.txtExcelIdentity.Enabled = false;
            this.txtExcelIdentity.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.txtExcelIdentity.Location = new System.Drawing.Point(96, 104);
            this.txtExcelIdentity.Name = "txtExcelIdentity";
            this.txtExcelIdentity.ReadOnly = true;
            this.txtExcelIdentity.Size = new System.Drawing.Size(280, 25);
            this.txtExcelIdentity.TabIndex = 45;
            this.txtExcelIdentity.TextChanged += new System.EventHandler(this.RegularPeriodSourceChanged);
            //
            // btnLoadMotherGroup
            //
            this.btnLoadMotherGroup.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnLoadMotherGroup.BackColor = System.Drawing.Color.Transparent;
            this.btnLoadMotherGroup.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnLoadMotherGroup.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnLoadMotherGroup.Location = new System.Drawing.Point(392, 102);
            this.btnLoadMotherGroup.Name = "btnLoadMotherGroup";
            this.btnLoadMotherGroup.Size = new System.Drawing.Size(180, 27);
            this.btnLoadMotherGroup.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnLoadMotherGroup.TabIndex = 46;
            this.btnLoadMotherGroup.Text = "載入／更新母群及科目";
            this.btnLoadMotherGroup.Click += new System.EventHandler(this.btnLoadMotherGroup_Click);
            //
            // lblRankStatus
            //
            this.lblRankStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRankStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblRankStatus.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblRankStatus.Location = new System.Drawing.Point(12, 132);
            this.lblRankStatus.Name = "lblRankStatus";
            this.lblRankStatus.Size = new System.Drawing.Size(835, 32);
            this.lblRankStatus.TabIndex = 48;
            this.lblRankStatus.Text = "尚未載入母群與科目。";
            //
            // plSetting
            // 
            this.plSetting.CanvasColor = System.Drawing.SystemColors.Control;
            this.plSetting.ColorSchemeStyle = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.plSetting.Controls.Add(this.lblRankStatus);
            this.plSetting.Controls.Add(this.btnLoadMotherGroup);
            this.plSetting.Controls.Add(this.txtExcelIdentity);
            this.plSetting.Controls.Add(this.lblExcelIdentity);
            this.plSetting.Controls.Add(this.cboSourceBatch);
            this.plSetting.Controls.Add(this.lblSourceBatch);
            this.plSetting.Controls.Add(this.cboSourceExam);
            this.plSetting.Controls.Add(this.lblSourceExam);
            this.plSetting.Controls.Add(this.cboSourceSemester);
            this.plSetting.Controls.Add(this.lblSourceSemester);
            this.plSetting.Controls.Add(this.nudSourceSchoolYear);
            this.plSetting.Controls.Add(this.lblSourceYear);
            this.plSetting.Controls.Add(this.lblSystemPeriod);
            this.plSetting.Controls.Add(this.lblCurrentSourceNote);
            this.plSetting.Controls.Add(this.cboMotherGroupSource);
            this.plSetting.Controls.Add(this.lblSourceCaption);
            this.plSetting.Controls.Add(this.cboTargetSemester);
            this.plSetting.Controls.Add(this.nudTargetSchoolYear);
            this.plSetting.Controls.Add(this.tableLayoutPanel1);
            this.plSetting.Controls.Add(this.lbCalcSemester);
            this.plSetting.Controls.Add(this.lbCalcSchoolYear);
            this.plSetting.Controls.Add(this.cboExamType);
            this.plSetting.Controls.Add(this.labelX6);
            this.plSetting.Controls.Add(this.btnNext);
            this.plSetting.Controls.Add(this.labelX2);
            this.plSetting.Controls.Add(this.labelX1);
            this.plSetting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plSetting.Location = new System.Drawing.Point(0, 0);
            this.plSetting.Name = "plSetting";
            this.plSetting.Size = new System.Drawing.Size(859, 700);
            this.plSetting.Style.Alignment = System.Drawing.StringAlignment.Center;
            this.plSetting.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
            this.plSetting.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
            this.plSetting.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
            this.plSetting.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
            this.plSetting.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
            this.plSetting.Style.GradientAngle = 90;
            this.plSetting.TabIndex = 8;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panel3, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel3, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel4, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 2);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 168);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33908F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33115F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.32977F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(835, 483);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.lvCalcSubjectTag2);
            this.panel3.Controls.Add(this.labelX14);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 354);
            this.panel3.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(835, 108);
            this.panel3.TabIndex = 6;
            // 
            // lvCalcSubjectTag2
            // 
            this.lvCalcSubjectTag2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.lvCalcSubjectTag2.Border.Class = "ListViewBorder";
            this.lvCalcSubjectTag2.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lvCalcSubjectTag2.CheckBoxes = true;
            this.lvCalcSubjectTag2.HideSelection = false;
            this.lvCalcSubjectTag2.Location = new System.Drawing.Point(0, 27);
            this.lvCalcSubjectTag2.Name = "lvCalcSubjectTag2";
            this.lvCalcSubjectTag2.Size = new System.Drawing.Size(834, 74);
            this.lvCalcSubjectTag2.TabIndex = 5;
            this.lvCalcSubjectTag2.UseCompatibleStateImageBehavior = false;
            this.lvCalcSubjectTag2.View = System.Windows.Forms.View.List;
            // 
            // labelX14
            // 
            this.labelX14.AutoSize = true;
            this.labelX14.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX14.BackgroundStyle.Class = "";
            this.labelX14.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX14.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX14.Location = new System.Drawing.Point(0, 0);
            this.labelX14.Margin = new System.Windows.Forms.Padding(0, 6, 3, 3);
            this.labelX14.Name = "labelX14";
            this.labelX14.Size = new System.Drawing.Size(74, 21);
            this.labelX14.TabIndex = 4;
            this.labelX14.Text = "採計科目：";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.lvCalcSubjectTag1);
            this.panel2.Controls.Add(this.labelX12);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 208);
            this.panel2.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(835, 107);
            this.panel2.TabIndex = 5;
            // 
            // lvCalcSubjectTag1
            // 
            this.lvCalcSubjectTag1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.lvCalcSubjectTag1.Border.Class = "ListViewBorder";
            this.lvCalcSubjectTag1.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lvCalcSubjectTag1.CheckBoxes = true;
            this.lvCalcSubjectTag1.HideSelection = false;
            this.lvCalcSubjectTag1.Location = new System.Drawing.Point(0, 27);
            this.lvCalcSubjectTag1.Name = "lvCalcSubjectTag1";
            this.lvCalcSubjectTag1.Size = new System.Drawing.Size(834, 74);
            this.lvCalcSubjectTag1.TabIndex = 4;
            this.lvCalcSubjectTag1.UseCompatibleStateImageBehavior = false;
            this.lvCalcSubjectTag1.View = System.Windows.Forms.View.List;
            // 
            // labelX12
            // 
            this.labelX12.AutoSize = true;
            this.labelX12.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX12.BackgroundStyle.Class = "";
            this.labelX12.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX12.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX12.Location = new System.Drawing.Point(1, 0);
            this.labelX12.Margin = new System.Windows.Forms.Padding(0, 6, 3, 3);
            this.labelX12.Name = "labelX12";
            this.labelX12.Size = new System.Drawing.Size(74, 21);
            this.labelX12.TabIndex = 3;
            this.labelX12.Text = "採計科目：";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.labelX10);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(835, 26);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // labelX10
            // 
            // 
            // 
            // 
            this.labelX10.BackgroundStyle.Class = "";
            this.labelX10.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX10.Location = new System.Drawing.Point(0, 0);
            this.labelX10.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.labelX10.Name = "labelX10";
            this.labelX10.Size = new System.Drawing.Size(75, 23);
            this.labelX10.TabIndex = 0;
            this.labelX10.Text = "計算年級：";
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.Controls.Add(this.labelX3);
            this.flowLayoutPanel2.Controls.Add(this.cboStudentFilter);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 26);
            this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(835, 33);
            this.flowLayoutPanel2.TabIndex = 1;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.Controls.Add(this.labelX4);
            this.flowLayoutPanel3.Controls.Add(this.cboStudentTag1);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(0, 172);
            this.flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(835, 33);
            this.flowLayoutPanel3.TabIndex = 2;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.AutoSize = true;
            this.flowLayoutPanel4.Controls.Add(this.labelX5);
            this.flowLayoutPanel4.Controls.Add(this.cboStudentTag2);
            this.flowLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel4.Location = new System.Drawing.Point(0, 318);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(835, 33);
            this.flowLayoutPanel4.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lvCalcSubject);
            this.panel1.Controls.Add(this.labelX11);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 62);
            this.panel1.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(835, 107);
            this.panel1.TabIndex = 4;
            // 
            // lvCalcSubject
            // 
            this.lvCalcSubject.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.lvCalcSubject.Border.Class = "ListViewBorder";
            this.lvCalcSubject.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lvCalcSubject.CheckBoxes = true;
            this.lvCalcSubject.HideSelection = false;
            this.lvCalcSubject.Location = new System.Drawing.Point(1, 33);
            this.lvCalcSubject.Name = "lvCalcSubject";
            this.lvCalcSubject.Size = new System.Drawing.Size(834, 74);
            this.lvCalcSubject.TabIndex = 3;
            this.lvCalcSubject.UseCompatibleStateImageBehavior = false;
            this.lvCalcSubject.View = System.Windows.Forms.View.List;
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
            this.labelX11.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX11.Location = new System.Drawing.Point(0, 6);
            this.labelX11.Margin = new System.Windows.Forms.Padding(0, 6, 3, 3);
            this.labelX11.Name = "labelX11";
            this.labelX11.Size = new System.Drawing.Size(101, 21);
            this.labelX11.TabIndex = 2;
            this.labelX11.Text = "參與排名科目：";
            // 
            // CalculateRegularAssessmentRank
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(859, 700);
            this.Controls.Add(this.plSetting);
            this.DoubleBuffered = true;
            this.MaximizeBox = true;
            this.Name = "CalculateRegularAssessmentRank";
            this.Text = "計算定期評量排名";
            this.Load += new System.EventHandler(this.CacluateRegularAssessmentRank_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudTargetSchoolYear)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSourceSchoolYear)).EndInit();
            this.plSetting.ResumeLayout(false);
            this.plSetting.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboStudentFilter;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.ButtonX btnNext;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboStudentTag2;
        private DevComponents.DotNetBar.LabelX labelX5;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboStudentTag1;
        private DevComponents.DotNetBar.LabelX labelX4;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboExamType;
        private DevComponents.DotNetBar.LabelX labelX6;
        private DevComponents.DotNetBar.LabelX lbCalcSchoolYear;
        private DevComponents.DotNetBar.LabelX lbCalcSemester;
        private System.Windows.Forms.NumericUpDown nudTargetSchoolYear;
        private System.Windows.Forms.ComboBox cboTargetSemester;
        private System.Windows.Forms.Label lblSourceCaption;
        private System.Windows.Forms.ComboBox cboMotherGroupSource;
        private System.Windows.Forms.Label lblCurrentSourceNote;
        private System.Windows.Forms.Label lblSystemPeriod;
        private System.Windows.Forms.Label lblSourceYear;
        private System.Windows.Forms.NumericUpDown nudSourceSchoolYear;
        private System.Windows.Forms.Label lblSourceSemester;
        private System.Windows.Forms.ComboBox cboSourceSemester;
        private System.Windows.Forms.Label lblSourceExam;
        private System.Windows.Forms.ComboBox cboSourceExam;
        private System.Windows.Forms.Label lblSourceBatch;
        private System.Windows.Forms.ComboBox cboSourceBatch;
        private System.Windows.Forms.Label lblExcelIdentity;
        private System.Windows.Forms.TextBox txtExcelIdentity;
        private DevComponents.DotNetBar.ButtonX btnLoadMotherGroup;
        private System.Windows.Forms.Label lblRankStatus;
        private DevComponents.DotNetBar.PanelEx plSetting;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private DevComponents.DotNetBar.LabelX labelX10;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Panel panel3;
        private DevComponents.DotNetBar.Controls.ListViewEx lvCalcSubjectTag2;
        private DevComponents.DotNetBar.LabelX labelX14;
        private System.Windows.Forms.Panel panel2;
        private DevComponents.DotNetBar.Controls.ListViewEx lvCalcSubjectTag1;
        private DevComponents.DotNetBar.LabelX labelX12;
        private System.Windows.Forms.Panel panel1;
        private DevComponents.DotNetBar.Controls.ListViewEx lvCalcSubject;
        private DevComponents.DotNetBar.LabelX labelX11;
    }
}
