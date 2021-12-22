using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aspose.Cells;
using FISCA.Data;
using FISCA.Presentation;
using FISCA.Presentation.Controls;
using System.Diagnostics;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankSelect_111 : BaseForm
    {
        private bool _IsLoading = false;
        private bool _IsClosing = false;
        private string _FilterStudentNumber = "", _FilterItemName = "", _FilterRankType = "", _FilterCategory="";

        List<string> ItemNameList = new List<string>();
        List<string> RankTypeList = new List<string>();
        

        private void btnExportToExcel_Click(object sender, EventArgs e)
        {
            saveFileDialog.Title = "匯出排名資料";
            saveFileDialog.FileName = "匯出排名資料.xlsx";
            saveFileDialog.Filter = "Excel (*.xlsx)|*.xlsx|所有檔案 (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DialogResult dialogResult = new DialogResult();
                    if (dgvScoreRank.Columns.Count > 0)
                    {
                        Workbook workbook = new Workbook();
                        Worksheet worksheet = workbook.Worksheets[0];
                        worksheet.Name = "排名資料";

                        int colIndex = 0;
                        foreach (DataGridViewColumn column in dgvScoreRank.Columns)
                        {
                            if (column.Visible == true && column.HeaderText != "檢視")
                            {
                                worksheet.Cells[0, colIndex].PutValue(column.HeaderText);
                                colIndex++;
                            }
                        }

                        colIndex = 0;
                        foreach (DataGridViewColumn column in dgvScoreRank.Columns)
                        {
                            if (column.Visible == true && column.HeaderText != "檢視")
                            {
                                for (int rowIndex = 0; rowIndex < dgvScoreRank.Rows.Count; rowIndex++)
                                {
                                    worksheet.Cells[rowIndex + 1, colIndex].PutValue("" + dgvScoreRank[column.Index, rowIndex].Value);
                                }
                                colIndex++;
                            }
                        }

                        workbook.Save(saveFileDialog.FileName);
                    }

                    dialogResult = MessageBox.Show("檔案儲存完成，是否開啟？", "是否開啟", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        try
                        {
                            Process.Start(saveFileDialog.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("檔案開啟失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("檔案儲存失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CalculateTechnologyAssessmentRankSelect_Resize(object sender, EventArgs e)
        {
            pbLoading.Location = new Point(this.Width / 2 - 20, this.Height / 2 - 20);
        }

        private void LoadRowData(object sender, EventArgs e)
        {
            UserControlEnable(false);

            if (_IsLoading)
            {
                return;
            }

            _IsLoading = true;
            dgvScoreRank.Rows.Clear();
            _FilterCategory = cboScoreCategory.Text;
            DataTable dt = new DataTable();
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
           

            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                MotherForm.SetStatusBarMessage("資料讀取中", eventArgs.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(0);

                    #region 要顯示的資料的sql字串
                    string queryString = @"
SELECT 
    *
FROM
(
    SELECT rank_matrix.id AS rank_matrix_id 
        , SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) AS score_type
        , SUBSTRING(rank_matrix.item_type, position('/' in rank_matrix.item_type) + 1, LENGTH(rank_matrix.item_type)) AS score_category
        , rank_matrix.item_name 
        , rank_matrix.rank_type 
        , rank_matrix.rank_name 
        , class.class_name 
        , student.seat_no 
        , student.student_number
        , student.name AS student_name
        , rank_detail.ref_student_id
        , rank_detail.score
        , rank_detail.rank
        , rank_detail.pr
        , rank_detail.percentile
        , rank_matrix.school_year
        , rank_matrix.semester 
        , rank_matrix.create_time
    FROM rank_matrix 
        LEFT OUTER JOIN 
            rank_detail ON rank_detail.ref_matrix_id = rank_matrix.id 
        LEFT OUTER JOIN 
            student ON student.id = rank_detail.ref_student_id 
        LEFT OUTER JOIN 
            class ON class.id = student.ref_class_id
    WHERE 
        rank_matrix.is_alive = true AND rank_matrix.item_type IN('5學期/"+_FilterCategory+@"')
    ORDER BY rank_matrix.rank_type
        , rank_matrix.rank_name
        , rank_detail.rank
        , rank_matrix.create_time DESC
) AS Rank_Table";
                    #endregion

                    QueryHelper queryHelper = new QueryHelper();
                    dt = queryHelper.Select(queryString);

                    bkw.ReportProgress(50);
                }
                catch (Exception ex)
                {
                    bkwException = ex;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (bkwException != null)
                {
                    throw new Exception("資料讀取失敗", bkwException);
                }

                if (dt.Rows.Count == 0)
                {
                    MotherForm.SetStatusBarMessage("無可檢視的資料。");
                    MessageBox.Show("無可檢視的資料。");
                    this.Close();
                    return;
                }





                #region 填入最後2個ComboBox
                //項目ComboBox
                cboItemName.Items.Clear();
                cboItemName.Items.Add("全部");

                List<string> tmpNameList = new List<string>();

                foreach (DataRow row in dt.Rows)
                {
                    string value = "" + row[3];
                    if (!tmpNameList.Contains(value))
                    {
                        tmpNameList.Add(value);
                    }
                }

                foreach (string item in ItemNameList)
                {
                    if (tmpNameList.Contains(item))
                        cboItemName.Items.Add(item);
                }
                cboItemName.SelectedIndex = 0;



                //母群ComboBox
                cboRankType.Items.Clear();
                cboRankType.Items.Add("全部");
                foreach (DataRow row in dt.Rows)
                {
                    string value = "" + row[4];
                    if (!tmpNameList.Contains(value))
                    {
                        tmpNameList.Add(value);
                    }
                }

                foreach (string item in RankTypeList)
                {
                    if (tmpNameList.Contains(item))
                        cboRankType.Items.Add(item);
                }

                cboRankType.SelectedIndex = 0;
                #endregion

                #region 整理資料
                _RowList = new List<DataGridViewRow>();
                for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
                {
                    int tryParseInt;
                    decimal tryParseDecimal;
                    DataGridViewRow gridViewRow = new DataGridViewRow();
                    gridViewRow.CreateCells(dgvScoreRank);
                    gridViewRow.Cells[0].Value = "" + dt.Rows[rowIndex]["rank_matrix_id"];
                    gridViewRow.Cells[1].Value = "" + dt.Rows[rowIndex]["score_type"];
                    gridViewRow.Cells[2].Value = "" + dt.Rows[rowIndex]["score_category"];
                    gridViewRow.Cells[3].Value = "" + dt.Rows[rowIndex]["item_name"];
                    gridViewRow.Cells[4].Value = "" + dt.Rows[rowIndex]["rank_type"];
                    gridViewRow.Cells[5].Value = "" + dt.Rows[rowIndex]["rank_name"];
                    gridViewRow.Cells[6].Value = "" + dt.Rows[rowIndex]["class_name"];
                    gridViewRow.Cells[7].Value = Int32.TryParse("" + dt.Rows[rowIndex]["seat_no"], out tryParseInt) ? (int?)tryParseInt : null;
                    gridViewRow.Cells[8].Value = "" + dt.Rows[rowIndex]["student_number"];
                    gridViewRow.Cells[9].Value = "" + dt.Rows[rowIndex]["student_name"];
                    gridViewRow.Cells[10].Value = Decimal.TryParse("" + dt.Rows[rowIndex]["score"], out tryParseDecimal) ? (decimal?)tryParseDecimal : null;
                    gridViewRow.Cells[11].Value = Int32.TryParse("" + dt.Rows[rowIndex]["rank"], out tryParseInt) ? (int?)tryParseInt : null;
                    gridViewRow.Cells[12].Value = Int32.TryParse("" + dt.Rows[rowIndex]["pr"], out tryParseInt) ? (int?)tryParseInt : null;
                    gridViewRow.Cells[13].Value = Int32.TryParse("" + dt.Rows[rowIndex]["percentile"], out tryParseInt) ? (int?)tryParseInt : null;
                    gridViewRow.Cells[15].Value = "" + dt.Rows[rowIndex]["school_year"];
                    gridViewRow.Cells[16].Value = "" + dt.Rows[rowIndex]["semester"];
                    gridViewRow.Tag = "" + dt.Rows[rowIndex]["ref_student_id"];
                    _RowList.Add(gridViewRow);
                }
                #endregion

                MotherForm.SetStatusBarMessage("資料讀取完成");
               
                _IsLoading = false;
                FillingDataGridView(null, null);

            };

            bkw.RunWorkerAsync();

        }

        private void FillingDataGridView(object sender, EventArgs e)
        {
            if (_IsLoading)
            {
                return;
            }
            lblRowCount.Text = "";
            UserControlEnable(false);
            _IsLoading = true;
            dgvScoreRank.Rows.Clear();
            _FilterItemName = cboItemName.Text;
            _FilterRankType = cboRankType.Text;
            _FilterStudentNumber = txtStudentNum.Text;
            _FilterCategory = cboScoreCategory.Text;
            List<DataGridViewRow> newList = new List<DataGridViewRow>();
            foreach (DataGridViewRow gridViewRow in _RowList)
            {
                var show = true;

                if (_FilterCategory == ("" + gridViewRow.Cells[2].Value))
                {
                    show = true;
                }

                if (_FilterItemName != "" && _FilterItemName != "全部" && _FilterItemName != ("" + gridViewRow.Cells[3].Value))
                {
                    show = show & false;
                }
                if (_FilterRankType != "" && _FilterRankType != "全部" && _FilterRankType != ("" + gridViewRow.Cells[4].Value))
                {
                    show = show & false;
                }
                if (_FilterStudentNumber != "" && !("" + gridViewRow.Cells[8].Value).Contains(_FilterStudentNumber))
                {
                    show = show & false;
                }
                if (show)
                    newList.Add(gridViewRow);
                if (newList.Count > 200)
                {
                    dgvScoreRank.Rows.AddRange(newList.ToArray());
                    newList.Clear();
                    Application.DoEvents();

                    if (_FilterItemName != cboItemName.Text
                        || _FilterRankType != cboRankType.Text
                        || _FilterStudentNumber != txtStudentNum.Text
                        || _FilterCategory != cboScoreCategory.Text)

                    {
                        _IsLoading = false;
                        FillingDataGridView(null, null);
                        return;
                    }

                    if (_IsClosing)
                    {
                        _IsClosing = false;
                        _IsLoading = false;
                        return;
                    }
                }
            }

            if (newList.Count > 0)
            {
                dgvScoreRank.Rows.AddRange(newList.ToArray());
                newList.Clear();
            }

            lblRowCount.Text = "共 " + dgvScoreRank.Rows.Count+ " 筆";
            _IsLoading = false;
            UserControlEnable(true);
        }

        private void CalculateTechnologyAssessmentRankSelect_FormClosing(object sender, FormClosingEventArgs e)
        {
            _IsClosing = true;
        }

        private void UserControlEnable(bool value)
        {
            btnExportToExcel.Enabled = value;
            cboItemName.Enabled = value;
            cboRankType.Enabled = value;
            txtStudentNum.Enabled = value;
            pbLoading.Visible = !value;
        }


        private void CalculateTechnologyAssessmentRankSelect_Load(object sender, EventArgs e)
        {
            //_FilterCategory = cboScoreCategory.Text;
            ItemNameList.Clear();
            RankTypeList.Clear();
            ItemNameList.Add("學業");
            ItemNameList.Add("專業及實習部必");
            ItemNameList.Add("專業及實習");
            ItemNameList.Add("技能領域部必");
            ItemNameList.Add("技能領域");
            ItemNameList.Add("國文部必");
            ItemNameList.Add("英文部必");
            ItemNameList.Add("數學部必");
            ItemNameList.Add("國文");
            ItemNameList.Add("英文");
            ItemNameList.Add("數學");

            RankTypeList.Add("學群排名");
            RankTypeList.Add("科排名");
            RankTypeList.Add("年排名");
            RankTypeList.Add("班排名");
            RankTypeList.Add("類別1排名");
            LoadCboScoreCategory();
            LoadRowData(null, null);


        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private List<DataGridViewRow> _RowList = new List<DataGridViewRow>();


        public CalculateTechnologyAssessmentRankSelect_111()
        {

            InitializeComponent();
            //目前loading圖片因不明原因背景色會自動變成透明，所以加這行讓他變成白色
            pbLoading.BackColor = Color.White;
        }

        public void LoadCboScoreCategory()
        {
            string sql = @"
SELECT 
    *
FROM
(
    SELECT 
		rank_matrix.id AS rank_matrix_id 
        , SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) AS score_type
        , SUBSTRING(rank_matrix.item_type, position('/' in rank_matrix.item_type) + 1, LENGTH(rank_matrix.item_type)) AS score_category
        , rank_matrix.item_name 
        , rank_matrix.rank_type 
    FROM rank_matrix 
        LEFT OUTER JOIN 
            rank_detail ON rank_detail.ref_matrix_id = rank_matrix.id 
    WHERE 
        rank_matrix.is_alive = true AND rank_matrix.item_type IN( '5學期/技職繁星比序','5學期/技職繁星比序(111年學年度適用)')
    ORDER BY rank_matrix.rank_type
        , rank_matrix.rank_name
        , rank_detail.rank
        , rank_matrix.create_time DESC
) AS Rank_Table
";
            DataTable dt = new DataTable();
            QueryHelper queryHelper = new QueryHelper();
            dt = queryHelper.Select(sql);

            if (dt.Rows.Count > 0)
            {
            //類別ComboBox
            foreach (DataRow row in dt.Rows)
            {
                string value = "" + row[2];
                if (!cboScoreCategory.Items.Contains(value))
                {
                    cboScoreCategory.Items.Add(value);
                }
            }
            cboScoreCategory.SelectedIndex = 0;
            }

        }
    }
}
