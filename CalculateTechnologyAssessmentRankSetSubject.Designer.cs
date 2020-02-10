namespace SHEvaluation.Rank
{
    partial class CalculateTechnologyAssessmentRankSetSubject
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.dgData = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.colSubjName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubj_1a = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubj_1b = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubj_2a = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubj_2b = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubj_3a = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnSave = new DevComponents.DotNetBar.ButtonX();
            this.btnExit = new DevComponents.DotNetBar.ButtonX();
            ((System.ComponentModel.ISupportInitialize)(this.dgData)).BeginInit();
            this.SuspendLayout();
            // 
            // labelX1
            // 
            this.labelX1.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.Class = "";
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(13, 22);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(237, 23);
            this.labelX1.TabIndex = 0;
            this.labelX1.Text = "報名科目與系統內科目對照";
            // 
            // dgData
            // 
            this.dgData.AllowUserToAddRows = false;
            this.dgData.AllowUserToDeleteRows = false;
            this.dgData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgData.BackgroundColor = System.Drawing.Color.White;
            this.dgData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgData.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSubjName,
            this.colSubj_1a,
            this.colSubj_1b,
            this.colSubj_2a,
            this.colSubj_2b,
            this.colSubj_3a});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgData.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgData.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(215)))), ((int)(((byte)(229)))));
            this.dgData.Location = new System.Drawing.Point(13, 52);
            this.dgData.Name = "dgData";
            this.dgData.RowTemplate.Height = 24;
            this.dgData.Size = new System.Drawing.Size(764, 159);
            this.dgData.TabIndex = 1;
            // 
            // colSubjName
            // 
            this.colSubjName.HeaderText = "報考科目";
            this.colSubjName.Name = "colSubjName";
            this.colSubjName.ReadOnly = true;
            // 
            // colSubj_1a
            // 
            this.colSubj_1a.HeaderText = "一上科目";
            this.colSubj_1a.Name = "colSubj_1a";
            this.colSubj_1a.Width = 120;
            // 
            // colSubj_1b
            // 
            this.colSubj_1b.HeaderText = "一下科目";
            this.colSubj_1b.Name = "colSubj_1b";
            this.colSubj_1b.Width = 120;
            // 
            // colSubj_2a
            // 
            this.colSubj_2a.HeaderText = "二上科目";
            this.colSubj_2a.Name = "colSubj_2a";
            this.colSubj_2a.Width = 120;
            // 
            // colSubj_2b
            // 
            this.colSubj_2b.HeaderText = "二下科目";
            this.colSubj_2b.Name = "colSubj_2b";
            this.colSubj_2b.Width = 120;
            // 
            // colSubj_3a
            // 
            this.colSubj_3a.HeaderText = "三上科目";
            this.colSubj_3a.Name = "colSubj_3a";
            this.colSubj_3a.Width = 120;
            // 
            // btnSave
            // 
            this.btnSave.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.AutoSize = true;
            this.btnSave.BackColor = System.Drawing.Color.Transparent;
            this.btnSave.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnSave.Location = new System.Drawing.Point(600, 228);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 25);
            this.btnSave.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "儲存";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnExit
            // 
            this.btnExit.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.AutoSize = true;
            this.btnExit.BackColor = System.Drawing.Color.Transparent;
            this.btnExit.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnExit.Location = new System.Drawing.Point(702, 228);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 25);
            this.btnExit.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnExit.TabIndex = 3;
            this.btnExit.Text = "離開";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // CalculateTechnologyAssessmentRankSetSubject
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(789, 266);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.dgData);
            this.Controls.Add(this.labelX1);
            this.DoubleBuffered = true;
            this.Name = "CalculateTechnologyAssessmentRankSetSubject";
            this.Text = "設定報名科目對照";
            this.Load += new System.EventHandler(this.CalculateTechnologyAssessmentRankSetSubject_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgData)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.LabelX labelX1;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgData;
        private DevComponents.DotNetBar.ButtonX btnSave;
        private DevComponents.DotNetBar.ButtonX btnExit;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubjName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubj_1a;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubj_1b;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubj_2a;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubj_2b;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubj_3a;
    }
}