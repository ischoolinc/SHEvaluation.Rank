
namespace SHEvaluation.Rank
{
    partial class SubjectSelect
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
            this.btnOk = new DevComponents.DotNetBar.ButtonX();
            this.btnSelAll = new DevComponents.DotNetBar.ButtonX();
            this.btnNoSelAll = new DevComponents.DotNetBar.ButtonX();
            this.lstSubjectView = new DevComponents.DotNetBar.Controls.ListViewEx();
            this.lblDept = new DevComponents.DotNetBar.LabelX();
            this.lblSubjectKind = new DevComponents.DotNetBar.LabelX();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.lblKind = new DevComponents.DotNetBar.LabelX();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.txtKeyWord = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnOk.BackColor = System.Drawing.Color.Transparent;
            this.btnOk.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnOk.Location = new System.Drawing.Point(12, 626);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(99, 23);
            this.btnOk.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "確定";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnSelAll
            // 
            this.btnSelAll.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSelAll.BackColor = System.Drawing.Color.Transparent;
            this.btnSelAll.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnSelAll.Location = new System.Drawing.Point(127, 627);
            this.btnSelAll.Name = "btnSelAll";
            this.btnSelAll.Size = new System.Drawing.Size(99, 23);
            this.btnSelAll.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnSelAll.TabIndex = 7;
            this.btnSelAll.Text = "全選";
            this.btnSelAll.Click += new System.EventHandler(this.btnSelAll_Click);
            // 
            // btnNoSelAll
            // 
            this.btnNoSelAll.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnNoSelAll.BackColor = System.Drawing.Color.Transparent;
            this.btnNoSelAll.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnNoSelAll.Location = new System.Drawing.Point(243, 627);
            this.btnNoSelAll.Name = "btnNoSelAll";
            this.btnNoSelAll.Size = new System.Drawing.Size(99, 23);
            this.btnNoSelAll.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnNoSelAll.TabIndex = 8;
            this.btnNoSelAll.Text = "全不選";
            this.btnNoSelAll.Click += new System.EventHandler(this.btnNoSelAll_Click);
            // 
            // lstSubjectView
            // 
            // 
            // 
            // 
            this.lstSubjectView.Border.Class = "ListViewBorder";
            this.lstSubjectView.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lstSubjectView.CheckBoxes = true;
            this.lstSubjectView.HideSelection = false;
            this.lstSubjectView.Location = new System.Drawing.Point(12, 108);
            this.lstSubjectView.Name = "lstSubjectView";
            this.lstSubjectView.Size = new System.Drawing.Size(572, 512);
            this.lstSubjectView.TabIndex = 10;
            this.lstSubjectView.UseCompatibleStateImageBehavior = false;
            this.lstSubjectView.View = System.Windows.Forms.View.List;
            // 
            // lblDept
            // 
            this.lblDept.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lblDept.BackgroundStyle.Class = "";
            this.lblDept.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblDept.Location = new System.Drawing.Point(85, 12);
            this.lblDept.Name = "lblDept";
            this.lblDept.Size = new System.Drawing.Size(502, 23);
            this.lblDept.TabIndex = 11;
            // 
            // lblSubjectKind
            // 
            this.lblSubjectKind.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lblSubjectKind.BackgroundStyle.Class = "";
            this.lblSubjectKind.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblSubjectKind.Location = new System.Drawing.Point(85, 41);
            this.lblSubjectKind.Name = "lblSubjectKind";
            this.lblSubjectKind.Size = new System.Drawing.Size(429, 23);
            this.lblSubjectKind.TabIndex = 12;
            // 
            // labelX1
            // 
            this.labelX1.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.Class = "";
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(21, 12);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(75, 23);
            this.labelX1.TabIndex = 13;
            this.labelX1.Text = "群組科別";
            // 
            // labelX2
            // 
            this.labelX2.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.Class = "";
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Location = new System.Drawing.Point(21, 41);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(75, 23);
            this.labelX2.TabIndex = 14;
            this.labelX2.Text = "比序科目";
            // 
            // lblKind
            // 
            this.lblKind.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.Class = "";
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblKind.Location = new System.Drawing.Point(520, 41);
            this.lblKind.Name = "lblKind";
            this.lblKind.Size = new System.Drawing.Size(67, 25);
            this.lblKind.TabIndex = 15;
            this.lblKind.Text = "挑選類別";
            this.lblKind.Visible = false;
            // 
            // labelX3
            // 
            this.labelX3.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.Class = "";
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(21, 79);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(75, 23);
            this.labelX3.TabIndex = 16;
            this.labelX3.Text = "篩選關鍵字";
            // 
            // txtKeyWord
            // 
            // 
            // 
            // 
            this.txtKeyWord.Border.Class = "TextBoxBorder";
            this.txtKeyWord.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.txtKeyWord.Location = new System.Drawing.Point(102, 77);
            this.txtKeyWord.Name = "txtKeyWord";
            this.txtKeyWord.Size = new System.Drawing.Size(100, 25);
            this.txtKeyWord.TabIndex = 17;
            this.txtKeyWord.TextChanged += new System.EventHandler(this.txtKeyWord_TextChanged);
            // 
            // SubjectSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(596, 659);
            this.Controls.Add(this.txtKeyWord);
            this.Controls.Add(this.labelX3);
            this.Controls.Add(this.lblKind);
            this.Controls.Add(this.lblDept);
            this.Controls.Add(this.lblSubjectKind);
            this.Controls.Add(this.labelX2);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.lstSubjectView);
            this.Controls.Add(this.btnNoSelAll);
            this.Controls.Add(this.btnSelAll);
            this.Controls.Add(this.btnOk);
            this.DoubleBuffered = true;
            this.Name = "SubjectSelect";
            this.Text = "科目挑選";
            this.ResumeLayout(false);

        }

        #endregion
        private DevComponents.DotNetBar.ButtonX btnOk;
        private DevComponents.DotNetBar.ButtonX btnSelAll;
        private DevComponents.DotNetBar.ButtonX btnNoSelAll;
        private DevComponents.DotNetBar.Controls.ListViewEx lstSubjectView;
        private DevComponents.DotNetBar.LabelX lblDept;
        private DevComponents.DotNetBar.LabelX lblSubjectKind;
        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.LabelX labelX2;
        private DevComponents.DotNetBar.LabelX lblKind;
        private DevComponents.DotNetBar.LabelX labelX3;
        private DevComponents.DotNetBar.Controls.TextBoxX txtKeyWord;
    }
}