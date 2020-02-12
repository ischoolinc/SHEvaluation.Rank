namespace SHEvaluation.Rank
{
    partial class CalculateTechnologyAssessmentRankStep2
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
            this.btnPrevious = new DevComponents.DotNetBar.ButtonX();
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
            this.colRegGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnCacluate = new DevComponents.DotNetBar.ButtonX();
            this.labelX13 = new DevComponents.DotNetBar.LabelX();
            this.lblMsg = new DevComponents.DotNetBar.LabelX();
            this.pbLoading = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStudentList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).BeginInit();
            this.SuspendLayout();
            // 
            // btnPrevious
            // 
            this.btnPrevious.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnPrevious.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPrevious.AutoSize = true;
            this.btnPrevious.BackColor = System.Drawing.Color.Transparent;
            this.btnPrevious.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnPrevious.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnPrevious.ForeColor = System.Drawing.Color.Black;
            this.btnPrevious.Location = new System.Drawing.Point(931, 423);
            this.btnPrevious.Name = "btnPrevious";
            this.btnPrevious.Size = new System.Drawing.Size(75, 28);
            this.btnPrevious.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnPrevious.TabIndex = 41;
            this.btnPrevious.Text = "上一步";
            this.btnPrevious.Click += new System.EventHandler(this.btnPrevious_Click);
            // 
            // dgvStudentList
            // 
            this.dgvStudentList.AllowUserToAddRows = false;
            this.dgvStudentList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvStudentList.BackgroundColor = System.Drawing.Color.White;
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
            this.colRankType2,
            this.colRegGroup});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvStudentList.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvStudentList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(215)))), ((int)(((byte)(229)))));
            this.dgvStudentList.HighlightSelectedColumnHeaders = false;
            this.dgvStudentList.Location = new System.Drawing.Point(24, 47);
            this.dgvStudentList.Name = "dgvStudentList";
            this.dgvStudentList.ReadOnly = true;
            this.dgvStudentList.RowTemplate.Height = 24;
            this.dgvStudentList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStudentList.Size = new System.Drawing.Size(1080, 363);
            this.dgvStudentList.TabIndex = 40;
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
            // colRegGroup
            // 
            this.colRegGroup.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRegGroup.HeaderText = "母群：學群排名";
            this.colRegGroup.Name = "colRegGroup";
            this.colRegGroup.ReadOnly = true;
            // 
            // btnCacluate
            // 
            this.btnCacluate.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCacluate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCacluate.AutoSize = true;
            this.btnCacluate.BackColor = System.Drawing.Color.Transparent;
            this.btnCacluate.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.btnCacluate.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.btnCacluate.ForeColor = System.Drawing.Color.Black;
            this.btnCacluate.Location = new System.Drawing.Point(1022, 423);
            this.btnCacluate.Name = "btnCacluate";
            this.btnCacluate.Size = new System.Drawing.Size(81, 28);
            this.btnCacluate.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.btnCacluate.TabIndex = 39;
            this.btnCacluate.Text = "計算排名";
            this.btnCacluate.Click += new System.EventHandler(this.btnCacluate_Click);
            // 
            // labelX13
            // 
            this.labelX13.AutoSize = true;
            this.labelX13.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX13.BackgroundStyle.Class = "";
            this.labelX13.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX13.Font = new System.Drawing.Font("微軟正黑體", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.labelX13.ForeColor = System.Drawing.Color.Black;
            this.labelX13.Location = new System.Drawing.Point(27, 17);
            this.labelX13.Name = "labelX13";
            this.labelX13.Size = new System.Drawing.Size(85, 24);
            this.labelX13.TabIndex = 38;
            this.labelX13.Text = "母群資料：";
            // 
            // lblMsg
            // 
            this.lblMsg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblMsg.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.lblMsg.BackgroundStyle.Class = "";
            this.lblMsg.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.lblMsg.Location = new System.Drawing.Point(24, 426);
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.Size = new System.Drawing.Size(813, 23);
            this.lblMsg.TabIndex = 42;
            // 
            // pbLoading
            // 
            this.pbLoading.BackColor = System.Drawing.Color.Transparent;
            this.pbLoading.Image = global::SHEvaluation.Rank.Properties.Resources.loading;
            this.pbLoading.Location = new System.Drawing.Point(547, 215);
            this.pbLoading.Name = "pbLoading";
            this.pbLoading.Size = new System.Drawing.Size(32, 32);
            this.pbLoading.TabIndex = 58;
            this.pbLoading.TabStop = false;
            this.pbLoading.Visible = false;
            // 
            // CalculateTechnologyAssessmentRankStep2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1127, 463);
            this.Controls.Add(this.pbLoading);
            this.Controls.Add(this.lblMsg);
            this.Controls.Add(this.btnPrevious);
            this.Controls.Add(this.dgvStudentList);
            this.Controls.Add(this.btnCacluate);
            this.Controls.Add(this.labelX13);
            this.DoubleBuffered = true;
            this.Name = "CalculateTechnologyAssessmentRankStep2";
            this.Text = "技職繁星多學期成績固定排名計算";
            this.Load += new System.EventHandler(this.CalculateTechnologyAssessmentRankStep2_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStudentList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbLoading)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevComponents.DotNetBar.ButtonX btnPrevious;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn colRegGroup;
        private DevComponents.DotNetBar.ButtonX btnCacluate;
        private DevComponents.DotNetBar.LabelX labelX13;
        private DevComponents.DotNetBar.LabelX lblMsg;
        private System.Windows.Forms.PictureBox pbLoading;
    }
}