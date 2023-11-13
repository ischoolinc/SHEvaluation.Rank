
namespace SHEvaluation.Rank.TechnologyAssessmentRank_111
{
    partial class RegGroupForm
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
            this.lstGroupCode = new DevComponents.DotNetBar.Controls.ListViewEx();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnOK = new DevComponents.DotNetBar.ButtonX();
            this.SuspendLayout();
            // 
            // lstGroupCode
            // 
            // 
            // 
            // 
            this.lstGroupCode.Border.Class = "ListViewBorder";
            this.lstGroupCode.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lstGroupCode.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.lstGroupCode.FullRowSelect = true;
            this.lstGroupCode.HideSelection = false;
            this.lstGroupCode.Location = new System.Drawing.Point(12, 12);
            this.lstGroupCode.MultiSelect = false;
            this.lstGroupCode.Name = "lstGroupCode";
            this.lstGroupCode.Size = new System.Drawing.Size(205, 216);
            this.lstGroupCode.TabIndex = 0;
            this.lstGroupCode.UseCompatibleStateImageBehavior = false;
            this.lstGroupCode.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "群組代碼";
            this.columnHeader1.Width = 70;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "群組名稱";
            this.columnHeader2.Width = 120;
            // 
            // btnOK
            // 
            this.btnOK.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnOK.BackColor = System.Drawing.Color.Transparent;
            this.btnOK.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnOK.Location = new System.Drawing.Point(68, 239);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "確定";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // RegGroupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(229, 274);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lstGroupCode);
            this.DoubleBuffered = true;
            this.MaximumSize = new System.Drawing.Size(245, 313);
            this.Name = "RegGroupForm";
            this.Text = "報名群組資料";
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.Controls.ListViewEx lstGroupCode;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private DevComponents.DotNetBar.ButtonX btnOK;
    }
}