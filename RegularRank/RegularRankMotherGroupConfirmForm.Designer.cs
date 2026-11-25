namespace SHEvaluation.Rank
{
    partial class RegularRankMotherGroupConfirmForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTargetCaption = new System.Windows.Forms.Label();
            this.lblTargetValue = new System.Windows.Forms.Label();
            this.lblSourceValue = new System.Windows.Forms.Label();
            this.lblIdentityNote = new System.Windows.Forms.Label();
            this.lblDeferred = new System.Windows.Forms.Label();
            this.lblSearch = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSummary = new System.Windows.Forms.Label();
            this.dgvStudents = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.dgvPopulation = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.dgvDetail = new DevComponents.DotNetBar.Controls.DataGridViewX();
            this.btnRestore = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnPrevious = new System.Windows.Forms.Button();
            this.btnCalculate = new System.Windows.Forms.Button();
            this.lblCalculateBlock = new System.Windows.Forms.Label();
            this.lblAddCaption = new System.Windows.Forms.Label();
            this.txtAddStudent = new System.Windows.Forms.TextBox();
            this.btnAddStudent = new System.Windows.Forms.Button();
            this.btnExcel = new System.Windows.Forms.Button();
            this.lblLoading = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStudents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPopulation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetail)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTargetCaption
            // 
            this.lblTargetCaption.AutoSize = true;
            this.lblTargetCaption.Location = new System.Drawing.Point(12, 12);
            this.lblTargetCaption.Name = "lblTargetCaption";
            this.lblTargetCaption.Size = new System.Drawing.Size(68, 16);
            this.lblTargetCaption.TabIndex = 0;
            this.lblTargetCaption.Text = "目標期間";
            // 
            // lblTargetValue
            // 
            this.lblTargetValue.AutoSize = true;
            this.lblTargetValue.Location = new System.Drawing.Point(86, 12);
            this.lblTargetValue.Name = "lblTargetValue";
            this.lblTargetValue.Size = new System.Drawing.Size(20, 16);
            this.lblTargetValue.TabIndex = 1;
            this.lblTargetValue.Text = "-";
            // 
            // lblSourceValue
            // 
            this.lblSourceValue.AutoSize = true;
            this.lblSourceValue.Location = new System.Drawing.Point(12, 36);
            this.lblSourceValue.Name = "lblSourceValue";
            this.lblSourceValue.Size = new System.Drawing.Size(68, 16);
            this.lblSourceValue.TabIndex = 2;
            this.lblSourceValue.Text = "來源";
            // 
            // lblIdentityNote
            // 
            this.lblIdentityNote.AutoSize = true;
            this.lblIdentityNote.Location = new System.Drawing.Point(12, 60);
            this.lblIdentityNote.Name = "lblIdentityNote";
            this.lblIdentityNote.Size = new System.Drawing.Size(320, 16);
            this.lblIdentityNote.TabIndex = 3;
            this.lblIdentityNote.Text = "目前學生來源：班級與座號為目前主檔。";
            // 
            // lblDeferred
            // 
            this.lblDeferred.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDeferred.Location = new System.Drawing.Point(12, 84);
            this.lblDeferred.Name = "lblDeferred";
            this.lblDeferred.Size = new System.Drawing.Size(1060, 20);
            this.lblDeferred.TabIndex = 4;
            this.lblDeferred.Text = "三種來源與歷史加人已接線；計算前須通過傳輸檢查。";
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(12, 112);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(68, 16);
            this.lblSearch.TabIndex = 5;
            this.lblSearch.Text = "搜尋學生";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(86, 109);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(220, 23);
            this.txtSearch.TabIndex = 6;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // lblSummary
            // 
            this.lblSummary.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSummary.Location = new System.Drawing.Point(700, 112);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(372, 20);
            this.lblSummary.TabIndex = 7;
            this.lblSummary.Text = "學生總人數 0";
            this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dgvStudents
            // 
            this.dgvStudents.AllowUserToAddRows = false;
            this.dgvStudents.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvStudents.Location = new System.Drawing.Point(12, 140);
            this.dgvStudents.Name = "dgvStudents";
            this.dgvStudents.RowTemplate.Height = 24;
            this.dgvStudents.Size = new System.Drawing.Size(1060, 360);
            this.dgvStudents.TabIndex = 8;
            // 
            // dgvPopulation
            // 
            this.dgvPopulation.AllowUserToAddRows = false;
            this.dgvPopulation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPopulation.Location = new System.Drawing.Point(12, 508);
            this.dgvPopulation.Name = "dgvPopulation";
            this.dgvPopulation.RowTemplate.Height = 24;
            this.dgvPopulation.Size = new System.Drawing.Size(520, 140);
            this.dgvPopulation.TabIndex = 9;
            // 
            // dgvDetail
            // 
            this.dgvDetail.AllowUserToAddRows = false;
            this.dgvDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDetail.Location = new System.Drawing.Point(540, 508);
            this.dgvDetail.Name = "dgvDetail";
            this.dgvDetail.RowTemplate.Height = 24;
            this.dgvDetail.Size = new System.Drawing.Size(532, 140);
            this.dgvDetail.TabIndex = 10;
            // 
            // btnRestore
            // 
            this.btnRestore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRestore.Location = new System.Drawing.Point(12, 688);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(196, 28);
            this.btnRestore.TabIndex = 11;
            this.btnRestore.Text = "恢復全部學生初始參與";
            this.btnRestore.UseVisualStyleBackColor = true;
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            // 
            // btnExport
            // 
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExport.Location = new System.Drawing.Point(620, 688);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(110, 28);
            this.btnExport.TabIndex = 12;
            this.btnExport.Text = "匯出完整清單";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnPrevious
            // 
            this.btnPrevious.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPrevious.Location = new System.Drawing.Point(736, 688);
            this.btnPrevious.Name = "btnPrevious";
            this.btnPrevious.Size = new System.Drawing.Size(88, 28);
            this.btnPrevious.TabIndex = 13;
            this.btnPrevious.Text = "上一步";
            this.btnPrevious.UseVisualStyleBackColor = true;
            this.btnPrevious.Click += new System.EventHandler(this.btnPrevious_Click);
            // 
            // btnCalculate
            // 
            this.btnCalculate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCalculate.Enabled = false;
            this.btnCalculate.Location = new System.Drawing.Point(830, 688);
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.Size = new System.Drawing.Size(242, 28);
            this.btnCalculate.TabIndex = 14;
            this.btnCalculate.Text = "計算排名";
            this.btnCalculate.UseVisualStyleBackColor = true;
            this.btnCalculate.Click += new System.EventHandler(this.btnCalculate_Click);
            // 
            // lblCalculateBlock
            // 
            this.lblCalculateBlock.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCalculateBlock.Location = new System.Drawing.Point(12, 656);
            this.lblCalculateBlock.Name = "lblCalculateBlock";
            this.lblCalculateBlock.Size = new System.Drawing.Size(1060, 28);
            this.lblCalculateBlock.TabIndex = 15;
            this.lblCalculateBlock.Text = "計算前會執行結構／傳輸檢查。";
            // 
            // lblAddCaption
            // 
            this.lblAddCaption.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblAddCaption.AutoSize = true;
            this.lblAddCaption.Location = new System.Drawing.Point(214, 694);
            this.lblAddCaption.Name = "lblAddCaption";
            this.lblAddCaption.Size = new System.Drawing.Size(44, 16);
            this.lblAddCaption.TabIndex = 16;
            this.lblAddCaption.Text = "學號";
            // 
            // txtAddStudent
            // 
            this.txtAddStudent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtAddStudent.Enabled = false;
            this.txtAddStudent.Location = new System.Drawing.Point(264, 691);
            this.txtAddStudent.Name = "txtAddStudent";
            this.txtAddStudent.Size = new System.Drawing.Size(90, 23);
            this.txtAddStudent.TabIndex = 17;
            // 
            // btnAddStudent
            // 
            this.btnAddStudent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddStudent.Enabled = false;
            this.btnAddStudent.Location = new System.Drawing.Point(360, 688);
            this.btnAddStudent.Name = "btnAddStudent";
            this.btnAddStudent.Size = new System.Drawing.Size(150, 28);
            this.btnAddStudent.TabIndex = 18;
            this.btnAddStudent.Text = "人工加入（未提供）";
            this.btnAddStudent.UseVisualStyleBackColor = true;
            this.btnAddStudent.Click += new System.EventHandler(this.btnAddStudent_Click);
            // 
            // btnExcel
            // 
            this.btnExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExcel.Enabled = false;
            this.btnExcel.Location = new System.Drawing.Point(900, 12);
            this.btnExcel.Name = "btnExcel";
            this.btnExcel.Size = new System.Drawing.Size(172, 28);
            this.btnExcel.TabIndex = 19;
            this.btnExcel.Text = "使用 Excel 名單";
            this.btnExcel.UseVisualStyleBackColor = true;
            this.btnExcel.Click += new System.EventHandler(this.btnExcel_Click);
            // 
            // lblLoading
            // 
            this.lblLoading.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblLoading.AutoSize = true;
            this.lblLoading.BackColor = System.Drawing.Color.White;
            this.lblLoading.Location = new System.Drawing.Point(500, 300);
            this.lblLoading.Name = "lblLoading";
            this.lblLoading.Size = new System.Drawing.Size(68, 16);
            this.lblLoading.TabIndex = 20;
            this.lblLoading.Text = "資料載入中";
            this.lblLoading.Visible = false;
            // 
            // RegularRankMotherGroupConfirmForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1084, 728);
            this.Controls.Add(this.lblLoading);
            this.Controls.Add(this.btnExcel);
            this.Controls.Add(this.btnAddStudent);
            this.Controls.Add(this.txtAddStudent);
            this.Controls.Add(this.lblAddCaption);
            this.Controls.Add(this.lblCalculateBlock);
            this.Controls.Add(this.btnCalculate);
            this.Controls.Add(this.btnPrevious);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.dgvDetail);
            this.Controls.Add(this.dgvPopulation);
            this.Controls.Add(this.dgvStudents);
            this.Controls.Add(this.lblSummary);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblSearch);
            this.Controls.Add(this.lblDeferred);
            this.Controls.Add(this.lblIdentityNote);
            this.Controls.Add(this.lblSourceValue);
            this.Controls.Add(this.lblTargetValue);
            this.Controls.Add(this.lblTargetCaption);
            this.Font = new System.Drawing.Font("Microsoft JhengHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.MinimumSize = new System.Drawing.Size(900, 640);
            this.Name = "RegularRankMotherGroupConfirmForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "確認定期評量排名母群";
            this.Load += new System.EventHandler(this.RegularRankMotherGroupConfirmForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStudents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPopulation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDetail)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTargetCaption;
        private System.Windows.Forms.Label lblTargetValue;
        private System.Windows.Forms.Label lblSourceValue;
        private System.Windows.Forms.Label lblIdentityNote;
        private System.Windows.Forms.Label lblDeferred;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSummary;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvStudents;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvPopulation;
        private DevComponents.DotNetBar.Controls.DataGridViewX dgvDetail;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnPrevious;
        private System.Windows.Forms.Button btnCalculate;
        private System.Windows.Forms.Label lblCalculateBlock;
        private System.Windows.Forms.Label lblAddCaption;
        private System.Windows.Forms.TextBox txtAddStudent;
        private System.Windows.Forms.Button btnAddStudent;
        private System.Windows.Forms.Button btnExcel;
        private System.Windows.Forms.Label lblLoading;
    }
}
