
namespace SHEvaluation.Rank.TechnologyAssessmentRank_111
{
    partial class DeptGroupSet
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
            this.btnSave = new DevComponents.DotNetBar.ButtonX();
            this.btnCancel = new DevComponents.DotNetBar.ButtonX();
            this.lblKind = new DevComponents.DotNetBar.LabelX();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.SuspendLayout();
            // 
            // btnAddDept
            // 
            this.btnAddDept.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAddDept.BackColor = System.Drawing.Color.Transparent;
            this.btnAddDept.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnAddDept.Location = new System.Drawing.Point(204, 166);
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
            this.btnRemoveDept.Location = new System.Drawing.Point(204, 216);
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
            this.lstDeptView.Location = new System.Drawing.Point(14, 118);
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
            this.lstGroupDept.Location = new System.Drawing.Point(290, 118);
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
            this.labelX1.Location = new System.Drawing.Point(14, 29);
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
            this.labelX2.Location = new System.Drawing.Point(198, 29);
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
            this.labelX3.Location = new System.Drawing.Point(290, 93);
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
            this.labelX4.Location = new System.Drawing.Point(14, 93);
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
            this.txtGroupID.ImeMode = System.Windows.Forms.ImeMode.On;
            this.txtGroupID.Location = new System.Drawing.Point(99, 27);
            this.txtGroupID.Name = "txtGroupID";
            this.txtGroupID.Size = new System.Drawing.Size(93, 25);
            this.txtGroupID.TabIndex = 4;
            this.txtGroupID.ImeModeChanged += new System.EventHandler(this.txtGroupID_ImeModeChanged);
            // 
            // txtGroupName
            // 
            // 
            // 
            // 
            this.txtGroupName.Border.Class = "TextBoxBorder";
            this.txtGroupName.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtGroupName.Location = new System.Drawing.Point(284, 31);
            this.txtGroupName.Name = "txtGroupName";
            this.txtGroupName.Size = new System.Drawing.Size(186, 25);
            this.txtGroupName.TabIndex = 4;
            // 
            // btnSave
            // 
            this.btnSave.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSave.BackColor = System.Drawing.Color.Transparent;
            this.btnSave.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnSave.Location = new System.Drawing.Point(290, 290);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(76, 23);
            this.btnSave.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "儲存";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCancel.BackColor = System.Drawing.Color.Transparent;
            this.btnCancel.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnCancel.Location = new System.Drawing.Point(381, 290);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(76, 23);
            this.btnCancel.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "取消";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblKind
            // 
            this.lblKind.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lblKind.BackgroundStyle.Class = "";
            this.lblKind.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblKind.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lblKind.ForeColor = System.Drawing.Color.Red;
            this.lblKind.Location = new System.Drawing.Point(14, 3);
            this.lblKind.Name = "lblKind";
            this.lblKind.Size = new System.Drawing.Size(205, 23);
            this.lblKind.TabIndex = 2;
            this.lblKind.Text = "群科設定狀態";
            // 
            // labelX5
            // 
            this.labelX5.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX5.BackgroundStyle.Class = "";
            this.labelX5.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX5.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX5.ForeColor = System.Drawing.Color.Red;
            this.labelX5.Location = new System.Drawing.Point(198, 58);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(246, 23);
            this.labelX5.TabIndex = 5;
            this.labelX5.Text = "※報名群組名稱不可重覆";
            // 
            // DeptGroupSet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 328);
            this.Controls.Add(this.labelX5);
            this.Controls.Add(this.txtGroupName);
            this.Controls.Add(this.txtGroupID);
            this.Controls.Add(this.labelX4);
            this.Controls.Add(this.labelX3);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.lblKind);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.lstGroupDept);
            this.Controls.Add(this.lstDeptView);
            this.Controls.Add(this.btnRemoveDept);
            this.Controls.Add(this.btnAddDept);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.DoubleBuffered = true;
            this.MaximumSize = new System.Drawing.Size(505, 370);
            this.MinimumSize = new System.Drawing.Size(505, 353);
            this.Name = "DeptGroupSet";
            this.Text = "技職繁星報名群別科系設定";
            this.ResumeLayout(false);

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
        private DevComponents.DotNetBar.ButtonX btnSave;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private DevComponents.DotNetBar.ButtonX btnCancel;
        private DevComponents.DotNetBar.LabelX lblKind;
        private DevComponents.DotNetBar.LabelX labelX5;
    }
}