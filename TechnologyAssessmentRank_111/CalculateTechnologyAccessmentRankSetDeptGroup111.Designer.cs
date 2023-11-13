
namespace SHEvaluation.Rank.TechnologyAssessmentRank_111
{
    partial class CalculateTechnologyAccessmentRankSetDeptGroup111
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
            this.btnAddDept = new DevComponents.DotNetBar.ButtonX();
            this.btnRemoveDept = new DevComponents.DotNetBar.ButtonX();
            this.lstDeptView = new DevComponents.DotNetBar.Controls.ListViewEx();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lstGroupDept = new DevComponents.DotNetBar.Controls.ListViewEx();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.txtGroupID = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.txtGroupName = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.btnDel = new DevComponents.DotNetBar.ButtonX();
            this.btnQuery = new DevComponents.DotNetBar.ButtonX();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnAddDept
            // 
            this.btnAddDept.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAddDept.BackColor = System.Drawing.Color.Transparent;
            this.btnAddDept.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnAddDept.Location = new System.Drawing.Point(198, 178);
            this.btnAddDept.Name = "btnAddDept";
            this.btnAddDept.Size = new System.Drawing.Size(75, 23);
            this.btnAddDept.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnAddDept.TabIndex = 0;
            this.btnAddDept.Text = "加入→";
            this.btnAddDept.Click += new System.EventHandler(this.btnAddDept_Click);
            // 
            // btnRemoveDept
            // 
            this.btnRemoveDept.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRemoveDept.BackColor = System.Drawing.Color.Transparent;
            this.btnRemoveDept.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnRemoveDept.Location = new System.Drawing.Point(198, 228);
            this.btnRemoveDept.Name = "btnRemoveDept";
            this.btnRemoveDept.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveDept.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnRemoveDept.TabIndex = 0;
            this.btnRemoveDept.Text = "移出←";
            this.btnRemoveDept.Click += new System.EventHandler(this.btnRemoveDept_Click);
            // 
            // lstDeptView
            // 
            // 
            // 
            // 
            this.lstDeptView.Border.Class = "ListViewBorder";
            this.lstDeptView.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lstDeptView.CheckBoxes = true;
            this.lstDeptView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lstDeptView.HideSelection = false;
            this.lstDeptView.Location = new System.Drawing.Point(12, 130);
            this.lstDeptView.Name = "lstDeptView";
            this.lstDeptView.Size = new System.Drawing.Size(180, 166);
            this.lstDeptView.TabIndex = 1;
            this.lstDeptView.UseCompatibleStateImageBehavior = false;
            this.lstDeptView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "科別名稱";
            this.columnHeader1.Width = 160;
            // 
            // lstGroupDept
            // 
            // 
            // 
            // 
            this.lstGroupDept.Border.Class = "ListViewBorder";
            this.lstGroupDept.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lstGroupDept.CheckBoxes = true;
            this.lstGroupDept.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
            this.lstGroupDept.HideSelection = false;
            this.lstGroupDept.Location = new System.Drawing.Point(290, 130);
            this.lstGroupDept.Name = "lstGroupDept";
            this.lstGroupDept.Size = new System.Drawing.Size(180, 166);
            this.lstGroupDept.TabIndex = 1;
            this.lstGroupDept.UseCompatibleStateImageBehavior = false;
            this.lstGroupDept.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "科別名稱";
            this.columnHeader2.Width = 160;
            // 
            // labelX1
            // 
            this.labelX1.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.Class = "";
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(12, 29);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(111, 23);
            this.labelX1.TabIndex = 2;
            this.labelX1.Text = "報名群別代碼";
            // 
            // labelX2
            // 
            this.labelX2.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.Class = "";
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Location = new System.Drawing.Point(12, 58);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(96, 23);
            this.labelX2.TabIndex = 2;
            this.labelX2.Text = "報名群組名稱";
            // 
            // labelX3
            // 
            this.labelX3.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.Class = "";
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(290, 105);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(167, 19);
            this.labelX3.TabIndex = 3;
            this.labelX3.Text = "群組包含科別";
            // 
            // labelX4
            // 
            this.labelX4.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX4.BackgroundStyle.Class = "";
            this.labelX4.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX4.Location = new System.Drawing.Point(12, 105);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(167, 19);
            this.labelX4.TabIndex = 3;
            this.labelX4.Text = "請選擇科別加入群組";
            // 
            // txtGroupID
            // 
            // 
            // 
            // 
            this.txtGroupID.Border.Class = "TextBoxBorder";
            this.txtGroupID.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtGroupID.Location = new System.Drawing.Point(99, 27);
            this.txtGroupID.Name = "txtGroupID";
            this.txtGroupID.Size = new System.Drawing.Size(143, 25);
            this.txtGroupID.TabIndex = 4;
            this.txtGroupID.TextChanged += new System.EventHandler(this.txtGroupID_TextChanged);
            this.txtGroupID.Leave += new System.EventHandler(this.txtGroupID_Leave);
            // 
            // txtGroupName
            // 
            // 
            // 
            // 
            this.txtGroupName.Border.Class = "TextBoxBorder";
            this.txtGroupName.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtGroupName.Location = new System.Drawing.Point(99, 56);
            this.txtGroupName.Name = "txtGroupName";
            this.txtGroupName.Size = new System.Drawing.Size(143, 25);
            this.txtGroupName.TabIndex = 4;
            this.txtGroupName.Leave += new System.EventHandler(this.txtGroupName_Leave);
            // 
            // btnDel
            // 
            this.btnDel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnDel.BackColor = System.Drawing.Color.Transparent;
            this.btnDel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnDel.Location = new System.Drawing.Point(351, 29);
            this.btnDel.Name = "btnDel";
            this.btnDel.Size = new System.Drawing.Size(119, 23);
            this.btnDel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnDel.TabIndex = 0;
            this.btnDel.Text = "刪除";
            this.btnDel.Click += new System.EventHandler(this.btnDel_Click);
            // 
            // btnQuery
            // 
            this.btnQuery.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnQuery.BackColor = System.Drawing.Color.Transparent;
            this.btnQuery.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnQuery.Image = global::SHEvaluation.Rank.Properties.Resources.Image;
            this.btnQuery.Location = new System.Drawing.Point(248, 29);
            this.btnQuery.Name = "btnQuery";
            this.btnQuery.Size = new System.Drawing.Size(40, 25);
            this.btnQuery.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnQuery.TabIndex = 5;
            this.btnQuery.Click += new System.EventHandler(this.btnQuery_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.ForeColor = System.Drawing.Color.DarkRed;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(203, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "新增群組直接輸入代碼名稱選科別";
            // 
            // CalculateTechnologyAccessmentRankSetDeptGroup111
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 314);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnQuery);
            this.Controls.Add(this.txtGroupName);
            this.Controls.Add(this.txtGroupID);
            this.Controls.Add(this.labelX4);
            this.Controls.Add(this.labelX3);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.lstGroupDept);
            this.Controls.Add(this.lstDeptView);
            this.Controls.Add(this.btnRemoveDept);
            this.Controls.Add(this.btnAddDept);
            this.Controls.Add(this.btnDel);
            this.DoubleBuffered = true;
            this.MaximumSize = new System.Drawing.Size(516, 353);
            this.Name = "CalculateTechnologyAccessmentRankSetDeptGroup111";
            this.Text = "技職繁星報名群別科系設定";
            this.Load += new System.EventHandler(this.CalculateTechnologyAccessmentRankSetDeptGroup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevComponents.DotNetBar.ButtonX btnAddDept;
        private DevComponents.DotNetBar.ButtonX btnRemoveDept;
        private DevComponents.DotNetBar.Controls.ListViewEx lstDeptView;
        private DevComponents.DotNetBar.Controls.ListViewEx lstGroupDept;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.LabelX labelX4;
        private DevComponents.DotNetBar.Controls.TextBoxX txtGroupID;
        private DevComponents.DotNetBar.Controls.TextBoxX txtGroupName;
        private DevComponents.DotNetBar.ButtonX btnDel;
        private DevComponents.DotNetBar.ButtonX btnQuery;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
    }
}