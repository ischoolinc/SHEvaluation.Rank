namespace SHEvaluation.Rank
{
    partial class CalculateTechnologyAssessmentRankStep1
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
            this.cboStudentTag2 = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.cboStudentTag1 = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.cboStudentFilter = new DevComponents.DotNetBar.Controls.ComboBoxEx();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.btnSetDeptGroup = new DevComponents.DotNetBar.ButtonX();
            this.btnSetSubject = new DevComponents.DotNetBar.ButtonX();
            this.btnNext = new DevComponents.DotNetBar.ButtonX();
            this.lblMsg = new DevComponents.DotNetBar.LabelX();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.iptParseNum = new DevComponents.Editors.IntegerInput();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            ((System.ComponentModel.ISupportInitialize)(this.iptParseNum)).BeginInit();
            this.SuspendLayout();
            // 
            // cboStudentTag2
            // 
            this.cboStudentTag2.DisplayMember = "Text";
            this.cboStudentTag2.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboStudentTag2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStudentTag2.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboStudentTag2.FormattingEnabled = true;
            this.cboStudentTag2.ItemHeight = 21;
            this.cboStudentTag2.Location = new System.Drawing.Point(120, 109);
            this.cboStudentTag2.Name = "cboStudentTag2";
            this.cboStudentTag2.Size = new System.Drawing.Size(242, 27);
            this.cboStudentTag2.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboStudentTag2.TabIndex = 2;
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
            this.labelX5.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX5.ForeColor = System.Drawing.Color.Black;
            this.labelX5.Location = new System.Drawing.Point(22, 111);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(100, 24);
            this.labelX5.TabIndex = 11;
            this.labelX5.Text = "類別排名二：";
            // 
            // cboStudentTag1
            // 
            this.cboStudentTag1.DisplayMember = "Text";
            this.cboStudentTag1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboStudentTag1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStudentTag1.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboStudentTag1.FormattingEnabled = true;
            this.cboStudentTag1.ItemHeight = 21;
            this.cboStudentTag1.Location = new System.Drawing.Point(120, 68);
            this.cboStudentTag1.Name = "cboStudentTag1";
            this.cboStudentTag1.Size = new System.Drawing.Size(242, 27);
            this.cboStudentTag1.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboStudentTag1.TabIndex = 1;
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
            this.labelX4.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX4.ForeColor = System.Drawing.Color.Black;
            this.labelX4.Location = new System.Drawing.Point(22, 70);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(100, 24);
            this.labelX4.TabIndex = 9;
            this.labelX4.Text = "類別排名一：";
            // 
            // cboStudentFilter
            // 
            this.cboStudentFilter.DisplayMember = "Text";
            this.cboStudentFilter.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cboStudentFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStudentFilter.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.cboStudentFilter.FormattingEnabled = true;
            this.cboStudentFilter.ItemHeight = 21;
            this.cboStudentFilter.Location = new System.Drawing.Point(150, 26);
            this.cboStudentFilter.Name = "cboStudentFilter";
            this.cboStudentFilter.Size = new System.Drawing.Size(242, 27);
            this.cboStudentFilter.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cboStudentFilter.TabIndex = 0;
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
            this.labelX3.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX3.ForeColor = System.Drawing.Color.Black;
            this.labelX3.Location = new System.Drawing.Point(22, 28);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(131, 24);
            this.labelX3.TabIndex = 7;
            this.labelX3.Text = "不排名學生類別：";
            // 
            // btnSetDeptGroup
            // 
            this.btnSetDeptGroup.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSetDeptGroup.AutoSize = true;
            this.btnSetDeptGroup.BackColor = System.Drawing.Color.Transparent;
            this.btnSetDeptGroup.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnSetDeptGroup.Location = new System.Drawing.Point(22, 257);
            this.btnSetDeptGroup.Name = "btnSetDeptGroup";
            this.btnSetDeptGroup.Size = new System.Drawing.Size(118, 25);
            this.btnSetDeptGroup.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnSetDeptGroup.TabIndex = 3;
            this.btnSetDeptGroup.Text = "設定報名科群對照";
            this.btnSetDeptGroup.Click += new System.EventHandler(this.btnSetDeptGroup_Click);
            // 
            // btnSetSubject
            // 
            this.btnSetSubject.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSetSubject.AutoSize = true;
            this.btnSetSubject.BackColor = System.Drawing.Color.Transparent;
            this.btnSetSubject.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnSetSubject.Location = new System.Drawing.Point(165, 257);
            this.btnSetSubject.Name = "btnSetSubject";
            this.btnSetSubject.Size = new System.Drawing.Size(118, 25);
            this.btnSetSubject.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnSetSubject.TabIndex = 4;
            this.btnSetSubject.Text = "設定報名科目對照";
            this.btnSetSubject.Click += new System.EventHandler(this.btnSetSubject_Click);
            // 
            // btnNext
            // 
            this.btnNext.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnNext.AutoSize = true;
            this.btnNext.BackColor = System.Drawing.Color.Transparent;
            this.btnNext.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnNext.Location = new System.Drawing.Point(410, 257);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(75, 25);
            this.btnNext.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnNext.TabIndex = 5;
            this.btnNext.Text = "下一步";
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // lblMsg
            // 
            this.lblMsg.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.Class = "";
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblMsg.Location = new System.Drawing.Point(22, 186);
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.Size = new System.Drawing.Size(479, 56);
            this.lblMsg.TabIndex = 12;
            this.lblMsg.Text = "說明：\r\n學生沒有設定學群不會被加入排名。";
            this.lblMsg.WordWrap = true;
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
            this.labelX1.ForeColor = System.Drawing.Color.Black;
            this.labelX1.Location = new System.Drawing.Point(30, 149);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(85, 24);
            this.labelX1.TabIndex = 13;
            this.labelX1.Text = "成績小數第";
            // 
            // iptParseNum
            // 
            this.iptParseNum.AllowEmptyState = false;
            this.iptParseNum.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.iptParseNum.BackgroundStyle.Class = "DateTimeInputBackground";
            this.iptParseNum.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.iptParseNum.ButtonFreeText.Shortcut = DevComponents.DotNetBar.eShortcut.F2;
            this.iptParseNum.Location = new System.Drawing.Point(120, 149);
            this.iptParseNum.Name = "iptParseNum";
            this.iptParseNum.ShowUpDown = true;
            this.iptParseNum.Size = new System.Drawing.Size(59, 25);
            this.iptParseNum.TabIndex = 14;
            this.iptParseNum.ValueChanged += new System.EventHandler(this.iptParseNum_ValueChanged);
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
            this.labelX2.ForeColor = System.Drawing.Color.Black;
            this.labelX2.Location = new System.Drawing.Point(185, 149);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(69, 24);
            this.labelX2.TabIndex = 15;
            this.labelX2.Text = "四捨五入";
            // 
            // CalculateTechnologyAssessmentRankStep1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(518, 303);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.iptParseNum);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.lblMsg);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.btnSetSubject);
            this.Controls.Add(this.btnSetDeptGroup);
            this.Controls.Add(this.cboStudentTag2);
            this.Controls.Add(this.labelX5);
            this.Controls.Add(this.cboStudentTag1);
            this.Controls.Add(this.labelX4);
            this.Controls.Add(this.cboStudentFilter);
            this.Controls.Add(this.labelX3);
            this.DoubleBuffered = true;
            this.Name = "CalculateTechnologyAssessmentRankStep1";
            this.Text = "技職繁星多學期成績固定排名設定";
            this.Load += new System.EventHandler(this.CalculateTechnologyAssessmentRankStep1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.iptParseNum)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.Controls.ComboBoxEx cboStudentTag2;
        private DevComponents.DotNetBar.LabelX labelX5;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboStudentTag1;
        private DevComponents.DotNetBar.LabelX labelX4;
        private DevComponents.DotNetBar.Controls.ComboBoxEx cboStudentFilter;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.ButtonX btnSetDeptGroup;
        private DevComponents.DotNetBar.ButtonX btnSetSubject;
        private DevComponents.DotNetBar.ButtonX btnNext;
        private DevComponents.DotNetBar.LabelX lblMsg;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.Editors.IntegerInput iptParseNum;
        private DevComponents.DotNetBar.LabelX labelX2;
    }
}