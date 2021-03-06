﻿using Aspose.Cells;
using FISCA.Data;
using FISCA.Presentation;
using FISCA.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SHEvaluation.Rank.DAO;

namespace SHEvaluation.Rank
{
    public partial class SemesterAssessmentRankSelect : BaseForm
    {
        private bool _IsLoading = false;
        private bool _IsClosing = false;
        private string _LoadSchoolYear = "", _LoadSemester = "", _LoadScoreType = "", _LoadScoreCategory = ""
                     , _FilterStudentNumber = "", _FilterItemName = "", _FilterRankType = "";
        private List<DataGridViewRow> _RowList = new List<DataGridViewRow>();

        // 讀取分批使用
        List<string> gradeYearList = new List<string>();
        List<string> rankTypeList = new List<string>();
        List<RankDataInfo> RankDataList = new List<RankDataInfo>();
        public SemesterAssessmentRankSelect()
        {
            InitializeComponent();
            //目前loading圖片因不明原因背景色會自動變成透明，所以加這行讓他變成白色
            pbLoading.BackColor = Color.White;
        }

        private void SemesterAssessmentRankSelect_Load(object sender, EventArgs e)
        {
            btnExportToExcel.Enabled = false;
            rankTypeList.Clear();
            rankTypeList.Add("班排名");
            rankTypeList.Add("科排名");
            rankTypeList.Add("年排名");
            rankTypeList.Add("類別1排名");
            rankTypeList.Add("類別2排名");
            #region 取得學生年級
            QueryHelper queryHelper = new QueryHelper();
            gradeYearList.Clear();
            string grQuery = "SELECT DISTINCT class.grade_year FROM class INNER JOIN student ON student.ref_class_id = class.id WHERE student.status = 1 AND class.grade_year IS NOT NULL ORDER BY class.grade_year ASC;";
            DataTable dtG = queryHelper.Select(grQuery);
            foreach (DataRow dr in dtG.Rows)
            {
                string gr = dr["grade_year"].ToString();
                if (!string.IsNullOrWhiteSpace(gr))
                    gradeYearList.Add(gr);
            }

            #endregion


            #region 取得前四個ComboBox的資料的SQL
            string querySQL = @"
SELECT
	rank_matrix.school_year
	, rank_matrix.semester
	, SUBSTRING(rank_matrix.item_type from 1 for position('/' in rank_matrix.item_type) - 1) AS score_type
	, SUBSTRING(rank_matrix.item_type from position('/' in rank_matrix.item_type) + 1 for LENGTH(rank_matrix.item_type)) AS score_category
FROM
	rank_matrix
WHERE
	is_alive = true
	AND SUBSTRING(rank_matrix.item_type from 1 for position('/' in rank_matrix.item_type) - 1) = '學期'
";
            #endregion


            DataTable dt = queryHelper.Select(querySQL);

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("無可檢視的資料");
                return;
            }

            #region 填入前4個ComboBox
            //學年度ComboBox
            foreach (DataRow row in dt.Rows)
            {
                string value = "" + row[0];
                if (!cboSchoolYear.Items.Contains(value))
                {
                    cboSchoolYear.Items.Add(value);
                }
            }

            if (cboSchoolYear.Items.Contains(K12.Data.School.DefaultSchoolYear))
            {
                cboSchoolYear.SelectedIndex = cboSchoolYear.Items.IndexOf(K12.Data.School.DefaultSchoolYear);
            }
            else
            {
                cboSchoolYear.SelectedIndex = 0;
            }

            //學期ComboBox
            foreach (DataRow row in dt.Rows)
            {
                string value = "" + row[1];
                if (!cboSemester.Items.Contains(value))
                {
                    cboSemester.Items.Add(value);
                }
            }

            if (cboSemester.Items.Contains(K12.Data.School.DefaultSemester))
            {
                cboSemester.SelectedIndex = cboSemester.Items.IndexOf(K12.Data.School.DefaultSemester);
            }
            else
            {
                cboSemester.SelectedIndex = 0;
            }

            //類型ComboBox
            foreach (DataRow row in dt.Rows)
            {
                string value = "" + row[2];
                if (!cboScoreType.Items.Contains(value))
                {
                    cboScoreType.Items.Add(value);
                }
            }
            cboScoreType.SelectedIndex = 0;

            //類別ComboBox
            foreach (DataRow row in dt.Rows)
            {
                string value = "" + row[3];
                if (!cboScoreCategory.Items.Contains(value))
                {
                    cboScoreCategory.Items.Add(value);
                }
            }
            cboScoreCategory.SelectedIndex = 0;
            #endregion
        }

        private void LoadRowData(object sender, EventArgs e)
        {
            btnExportToExcel.Enabled = false;
            lblMsgCount.Text = "";
            if (_IsLoading)
            {
                return;
            }

            if (!string.IsNullOrEmpty(cboSchoolYear.Text)
                && !string.IsNullOrEmpty(cboSemester.Text)
                && !string.IsNullOrEmpty(cboScoreType.Text)
                && !string.IsNullOrEmpty(cboScoreCategory.Text))
            {
                _IsLoading = true;
                dgvScoreRank.Rows.Clear();
                _LoadSchoolYear = cboSchoolYear.Text;
                _LoadSemester = cboSemester.Text;
                _LoadScoreType = cboScoreType.Text;
                _LoadScoreCategory = cboScoreCategory.Text;

                //DataTable dt = new DataTable();
                BackgroundWorker bkw = new BackgroundWorker();
                bkw.WorkerReportsProgress = true;
                Exception bkwException = null;
                pbLoading.Visible = true;

                bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
                {
                    MotherForm.SetStatusBarMessage("資料讀取中", eventArgs.ProgressPercentage);
                };

                bkw.DoWork += delegate
                {
                    try
                    {
                        bkw.ReportProgress(0);

                        QueryHelper queryHelper = new QueryHelper();
                        RankDataList.Clear();
                        bool isFirst = true;
                        int pr = 20;
                        foreach (string gr in gradeYearList)
                        {
                            foreach (string rkType in rankTypeList)
                            {
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
        , rank_matrix.grade_year
        , rank_matrix.item_type
    FROM rank_matrix 
        LEFT OUTER JOIN 
            rank_detail ON rank_detail.ref_matrix_id = rank_matrix.id 
        LEFT OUTER JOIN 
            student ON student.id = rank_detail.ref_student_id 
        LEFT OUTER JOIN 
            class ON class.id = student.ref_class_id
    WHERE 
        rank_matrix.is_alive = true
    ORDER BY rank_matrix.rank_type
        , rank_matrix.rank_name
        , rank_detail.rank
        , rank_matrix.create_time DESC
) AS Rank_Table
WHERE  
    school_year = " + _LoadSchoolYear + @"
    And semester = " + _LoadSemester + @"
    And score_type = '" + _LoadScoreType + @"'
    And score_category = '" + _LoadScoreCategory + "' AND item_type LIKE '學期%'" + " AND grade_year = '" + gr + "' AND rank_type = '" + rkType + "'";
                                #endregion


                                // 第一次載入
                                if (isFirst)
                                {
                                    //dt = queryHelper.Select(queryString);
                                    //isFirst = false;
                                    DataTable dt = queryHelper.Select(queryString);
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        RankDataInfo rd = new RankDataInfo();
                                        rd.rank_matrix_id = dr["rank_matrix_id"] + "";
                                        rd.score_type = dr["score_type"] + "";
                                        rd.score_category = dr["score_category"] + "";
                                       
                                        rd.item_name = dr["item_name"] + "";
                                        rd.rank_type = dr["rank_type"] + "";
                                        rd.rank_name = dr["rank_name"] + "";
                                        rd.class_name = dr["class_name"] + "";
                                        rd.seat_no = dr["seat_no"] + "";
                                        rd.student_number = dr["student_number"] + "";
                                        rd.student_name = dr["student_name"] + "";
                                        rd.score = dr["score"] + "";
                                        rd.rank = dr["rank"] + "";
                                        rd.pr = dr["pr"] + "";
                                        rd.percentile = dr["percentile"] + "";
                                        rd.school_year = dr["school_year"] + "";
                                        rd.semester = dr["semester"] + "";
                                        rd.create_time = dr["create_time"] + "";
                                        rd.ref_student_id = dr["ref_student_id"] + "";
                                        rd.grade_year = dr["grade_year"] + "";
                                        rd.item_type = dr["item_type"] + "";
                                        RankDataList.Add(rd);
                                    }
                                    dt = null;
                                    GC.Collect();
                                    isFirst = false;
                                }
                                else
                                {
                                    //DataTable dt1 = queryHelper.Select(queryString);

                                    //foreach (DataRow dr in dt1.Rows)
                                    //{
                                    //    dt.ImportRow(dr);
                                    //}
                                    DataTable dt1 = queryHelper.Select(queryString);

                                    foreach (DataRow dr in dt1.Rows)
                                    {
                                        RankDataInfo rd = new RankDataInfo();
                                        rd.rank_matrix_id = dr["rank_matrix_id"] + "";
                                        rd.score_type = dr["score_type"] + "";
                                        rd.score_category = dr["score_category"] + "";
                                        
                                        rd.item_name = dr["item_name"] + "";
                                        rd.rank_type = dr["rank_type"] + "";
                                        rd.rank_name = dr["rank_name"] + "";
                                        rd.class_name = dr["class_name"] + "";
                                        rd.seat_no = dr["seat_no"] + "";
                                        rd.student_number = dr["student_number"] + "";
                                        rd.student_name = dr["student_name"] + "";
                                        rd.score = dr["score"] + "";
                                        rd.rank = dr["rank"] + "";
                                        rd.pr = dr["pr"] + "";
                                        rd.percentile = dr["percentile"] + "";
                                        rd.school_year = dr["school_year"] + "";
                                        rd.semester = dr["semester"] + "";
                                        rd.create_time = dr["create_time"] + "";
                                        rd.ref_student_id = dr["ref_student_id"] + "";
                                        rd.grade_year = dr["grade_year"] + "";
                                        rd.item_type = dr["item_type"] + "";
                                        RankDataList.Add(rd);
                                    }
                                    dt1 = null;
                                    GC.Collect();
                                }

                                bkw.ReportProgress(pr);
                                pr += 5;


                            }

                        }
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

                    if (cboSchoolYear.Text != _LoadSchoolYear
                       || cboSemester.Text != _LoadSemester
                       || cboScoreType.Text != _LoadScoreType
                       || cboScoreCategory.Text != _LoadScoreCategory)
                    {
                        _IsLoading = false;
                        LoadRowData(null, null);
                    }
                    else
                    {
                        #region 填入最後2個ComboBox
                        //項目ComboBox
                        cboItemName.Items.Clear();
                        cboItemName.Items.Add("全部");
                        //foreach (DataRow row in dt.Rows)
                        foreach (RankDataInfo rd in RankDataList)
                        {
                            string value = "" + rd.item_name;// row[3];
                            if (!cboItemName.Items.Contains(value))
                            {
                                cboItemName.Items.Add(value);
                            }
                        }
                        cboItemName.SelectedIndex = 0;

                        //母群ComboBox
                        cboRankType.Items.Clear();
                        cboRankType.Items.Add("全部");
                        // foreach (DataRow row in dt.Rows)
                        foreach (RankDataInfo rd in RankDataList)
                        {
                            string value = "" + rd.rank_type;//row[4];
                            if (!cboRankType.Items.Contains(value))
                            {
                                cboRankType.Items.Add(value);
                            }
                        }
                        cboRankType.SelectedIndex = 0;
                        #endregion

                        #region 整理資料
                        _RowList.Clear(); // = new List<DataGridViewRow>();
                        //for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
                        foreach (RankDataInfo rd in RankDataList)
                        {
                            int tryParseInt;
                            decimal tryParseDecimal;
                            DataGridViewRow gridViewRow = new DataGridViewRow();
                            gridViewRow.CreateCells(dgvScoreRank);
                            //gridViewRow.Cells[0].Value = "" + dt.Rows[rowIndex]["rank_matrix_id"];
                            //gridViewRow.Cells[1].Value = "" + dt.Rows[rowIndex]["score_type"];
                            //gridViewRow.Cells[2].Value = "" + dt.Rows[rowIndex]["score_category"];
                            //gridViewRow.Cells[3].Value = "" + dt.Rows[rowIndex]["item_name"];
                            //gridViewRow.Cells[4].Value = "" + dt.Rows[rowIndex]["rank_type"];
                            //gridViewRow.Cells[5].Value = "" + dt.Rows[rowIndex]["rank_name"];
                            //gridViewRow.Cells[6].Value = "" + dt.Rows[rowIndex]["class_name"];
                            //gridViewRow.Cells[7].Value = Int32.TryParse("" + dt.Rows[rowIndex]["seat_no"], out tryParseInt) ? (int?)tryParseInt : null;
                            //gridViewRow.Cells[8].Value = "" + dt.Rows[rowIndex]["student_number"];
                            //gridViewRow.Cells[9].Value = "" + dt.Rows[rowIndex]["student_name"];
                            //gridViewRow.Cells[10].Value = Decimal.TryParse("" + dt.Rows[rowIndex]["score"], out tryParseDecimal) ? (decimal?)tryParseDecimal : null;
                            //gridViewRow.Cells[11].Value = Int32.TryParse("" + dt.Rows[rowIndex]["rank"], out tryParseInt) ? (int?)tryParseInt : null;
                            //gridViewRow.Cells[12].Value = Int32.TryParse("" + dt.Rows[rowIndex]["pr"], out tryParseInt) ? (int?)tryParseInt : null;
                            //gridViewRow.Cells[13].Value = Int32.TryParse("" + dt.Rows[rowIndex]["percentile"], out tryParseInt) ? (int?)tryParseInt : null;
                            //gridViewRow.Cells[15].Value = "" + dt.Rows[rowIndex]["school_year"];
                            //gridViewRow.Cells[16].Value = "" + dt.Rows[rowIndex]["semester"];
                            //gridViewRow.Tag = "" + dt.Rows[rowIndex]["ref_student_id"];
                            gridViewRow.Cells[0].Value = "" + rd.rank_matrix_id;// dt.Rows[rowIndex]["rank_matrix_id"];
                            gridViewRow.Cells[1].Value = "" + rd.score_type;// dt.Rows[rowIndex]["score_type"];
                            gridViewRow.Cells[2].Value = "" + rd.score_category;//dt.Rows[rowIndex]["score_category"];                       
                            gridViewRow.Cells[3].Value = "" + rd.item_name;//dt.Rows[rowIndex]["item_name"];
                            gridViewRow.Cells[4].Value = "" + rd.rank_type; //dt.Rows[rowIndex]["rank_type"];
                            gridViewRow.Cells[5].Value = "" + rd.rank_name;//dt.Rows[rowIndex]["rank_name"];
                            gridViewRow.Cells[6].Value = "" + rd.class_name; //dt.Rows[rowIndex]["class_name"];
                            gridViewRow.Cells[7].Value = Int32.TryParse("" + rd.seat_no, out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[8].Value = "" + rd.student_number;// dt.Rows[rowIndex]["student_number"];
                            gridViewRow.Cells[9].Value = "" + rd.student_name;//dt.Rows[rowIndex]["student_name"];
                            gridViewRow.Cells[10].Value = Decimal.TryParse("" + rd.score, out tryParseDecimal) ? (decimal?)tryParseDecimal : null;
                            gridViewRow.Cells[11].Value = Int32.TryParse("" + rd.rank, out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[12].Value = Int32.TryParse("" + rd.pr, out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[13].Value = Int32.TryParse("" + rd.percentile, out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[15].Value = "" + rd.school_year;//dt.Rows[rowIndex]["school_year"];
                            gridViewRow.Cells[16].Value = "" + rd.semester;//dt.Rows[rowIndex]["semester"];
                            gridViewRow.Tag = "" + rd.ref_student_id; // dt.Rows[rowIndex]["ref_student_id"];

                            _RowList.Add(gridViewRow);
                        }
                        #endregion

                        MotherForm.SetStatusBarMessage("資料讀取完成");
                        RankDataList.Clear();
                        GC.Collect();
                        pbLoading.Visible = false;
                        _IsLoading = false;
                        FillingDataGridView(null, null);
                    }
                };

                bkw.RunWorkerAsync();
            }
        }

        private void FillingDataGridView(object sender, EventArgs e)
        {
            if (_IsLoading)
            {
                return;
            }
            btnExportToExcel.Enabled = false;
            pbLoading.Visible = true;
            lblMsgCount.Text = "";
            _IsLoading = true;
            dgvScoreRank.Rows.Clear();
            _FilterItemName = cboItemName.Text;
            _FilterRankType = cboRankType.Text;
            _FilterStudentNumber = txtStudentNum.Text;

            List<DataGridViewRow> newList = new List<DataGridViewRow>();
            foreach (DataGridViewRow gridViewRow in _RowList)
            {
                var show = true;

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

                    if (cboSchoolYear.Text != _LoadSchoolYear
                        || cboSemester.Text != _LoadSemester
                        || cboScoreType.Text != _LoadScoreType
                        || cboScoreCategory.Text != _LoadScoreCategory)
                    {
                        _IsLoading = false;
                        LoadRowData(null, null);
                        return;
                    }

                    if (_FilterItemName != cboItemName.Text
                        || _FilterRankType != cboRankType.Text
                        || _FilterStudentNumber != txtStudentNum.Text)

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

            btnExportToExcel.Enabled = true;
            pbLoading.Visible = false;
            lblMsgCount.Text = "共 " + dgvScoreRank.Rows.Count + " 筆";
            _IsLoading = false;
        }

        private void dgvScoreRank_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (dgvScoreRank.Columns[e.ColumnIndex].HeaderText != "檢視")
            {
                return;
            }

            SemesterMatrixRankSelect frm = new SemesterMatrixRankSelect("" + dgvScoreRank[0, e.RowIndex].Value
                                                      , "" + dgvScoreRank.Rows[e.RowIndex].Tag
                                                      , "" + dgvScoreRank[15, e.RowIndex].Value
                                                      , "" + dgvScoreRank[16, e.RowIndex].Value
                                                      , "" + dgvScoreRank[1, e.RowIndex].Value
                                                      , "" + dgvScoreRank[2, e.RowIndex].Value
                                                      , "" + dgvScoreRank[3, e.RowIndex].Value
                                                      , "" + dgvScoreRank[4, e.RowIndex].Value
                                                      , "" + dgvScoreRank[5, e.RowIndex].Value);
            frm.ShowDialog();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExportToExcel_Click(object sender, EventArgs e)
        {
            btnExportToExcel.Enabled = false;
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
                    btnExportToExcel.Enabled = true;
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
                    btnExportToExcel.Enabled = true;
                }
            }
        }

        private void SemesterAssessmentRankSelect_Resize(object sender, EventArgs e)
        {
            //調整Loading圖案的位置
            pbLoading.Location = new Point(this.Width / 2 - 20, this.Height / 2 - 20);
        }

        private void SemesterAssessmentRankSelect_FormClosing(object sender, FormClosingEventArgs e)
        {
            _IsClosing = true;
        }
    }
}
