using Aspose.Cells;
using FISCA.Data;
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

namespace SHEvaluation.Rank
{
    public partial class RegularMatrixRankSelect : BaseForm
    {
        bool _IsLoading = false;
        string _RankName = "", _RankMatrixId = "";
        Dictionary<string, string> _MatrixIdDic = new Dictionary<string, string>();

        public RegularMatrixRankSelect(string rankMatrixId, string schoolYear, string semester, string scoreType, string scoreCategory, string examName, string itemName, string rankType, string rankName)
        {
            InitializeComponent();

            lbSchoolYear.Text = schoolYear;
            lbSemester.Text = semester;
            lbScoreType.Text = scoreType;
            lbScoreCategory.Text = scoreCategory;
            lbExamName.Text = examName;
            lbItemName.Text = itemName;
            lbRankType.Text = rankType;
            _RankName = rankName;
            _RankMatrixId = rankMatrixId;
        }

        private void RegularMatrixRankSelect_Load(object sender, EventArgs e)
        {
            try
            {
                QueryHelper queryHelper = new QueryHelper();

                #region 要顯示的資料的sql字串
                string queryTable = @"
SELECT
	*
FROM
(
	SELECT rank_matrix.id AS rank_matrix_id
        , ref_batch_id
		, SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) as score_type
		, SUBSTRING(rank_matrix.item_type, position('/' in rank_matrix.item_type) + 1, LENGTH(rank_matrix.item_type)) as score_category 
		, exam.exam_name 
		, rank_matrix.item_name 
		, rank_matrix.rank_type
		, rank_matrix.rank_name
		, rank_matrix.school_year
		, rank_matrix.semester 
		, rank_matrix.is_alive
		, rank_matrix.create_time
	FROM rank_matrix 
		LEFT OUTER JOIN 
			rank_detail ON rank_detail.ref_matrix_id = rank_matrix.id
		LEFT OUTER JOIN 
			exam ON exam.id=rank_matrix.ref_exam_id
	ORDER BY
		create_time DESC
) AS Rank_Table
Where school_year = " + Convert.ToInt32(lbSchoolYear.Text) + @"
	AND semester = " + Convert.ToInt32(lbSemester.Text) + @"
	AND score_type = '" + lbScoreType.Text + "'" + @"
	AND score_category = '" + lbScoreCategory.Text + "'" + @"
	AND exam_name = '" + lbExamName.Text + "'" + @"
	AND item_name = '" + lbItemName.Text + "'" + @"
	AND rank_name = '" + _RankName + "'";
                #endregion

                DataTable dataTable = new DataTable();
                dataTable = queryHelper.Select(queryTable);

                #region 填入編號的ComboBox
                foreach (DataRow row in dataTable.Rows)
                {
                    if (!cboBatchId.Items.Contains("" + Convert.ToDateTime(row["create_time"]).ToString("yyyy/MM/dd HH:mm") + " （批號：" + row["ref_batch_id"] + "）")
                        && !cboBatchId.Items.Contains("*" + Convert.ToDateTime(row["create_time"]).ToString("yyyy/MM/dd HH:mm") + " （批號：" + row["ref_batch_id"] + "）"))
                    {
                        string isAlive = "";
                        if (!string.IsNullOrEmpty("" + row["is_alive"]))
                        {
                            if (Convert.ToBoolean(row["is_alive"]) == true)
                            {
                                isAlive = "*";
                            }
                        }
                        cboBatchId.Items.Add(isAlive + Convert.ToDateTime(row["create_time"]).ToString("yyyy/MM/dd HH:mm") + " （批號：" + row["ref_batch_id"] + "）");
                        _MatrixIdDic.Add(isAlive + Convert.ToDateTime(row["create_time"]).ToString("yyyy/MM/dd HH:mm") + " （批號：" + row["ref_batch_id"] + "）", "" + row["rank_matrix_id"]);
                    }
                }

                if (cboBatchId.Items.Contains("*"))
                {
                    cboBatchId.SelectedIndex = cboBatchId.Items.IndexOf("*");
                }
                else
                {
                    cboBatchId.SelectedIndex = 0;
                }
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void LoadRowData(object sender, EventArgs e)
        {
            if (_IsLoading == true)
            {
                return;
            }

            _IsLoading = true;
            string matrixId = _MatrixIdDic[cboBatchId.Text];

            #region 要顯示的資料的sql字串
            string queryString = @"
Select 
    *
From
	(
        SELECT 
            rank_matrix.id AS rank_matrix_id 
		    , SUBSTRING(rank_matrix.item_type, 1, position('/' in rank_matrix.item_type) - 1) as score_type
		    , SUBSTRING(rank_matrix.item_type, position('/' in rank_matrix.item_type) + 1, LENGTH(rank_matrix.item_type)) as score_category 
		    , exam.exam_name 
		    , rank_matrix.item_name 
		    , rank_matrix.rank_type 
		    , rank_matrix.rank_name 
		    , class.class_name 
		    , student.seat_no 
		    , student.student_number
		    , student.name AS student_name
            , CASE WHEN student.status = 1 then '一般'::TEXT
					 WHEN student.status = 2 then '延修' ::TEXT
					 WHEN student.status = 4 then '休學'::TEXT
					 WHEN student.status = 8 then '輟學'::TEXT
					 WHEN student.status = 16 then '畢業或離校'::TEXT
					 WHEN student.status = 256 then '刪除'::TEXT
					 ELSE ''||student.status
		    END as student_status
		    , rank_detail.score
		    , rank_detail.rank
		    , rank_detail.pr
		    , rank_detail.percentile
		    , rank_matrix.school_year
		    , rank_matrix.semester
		    , rank_matrix.create_time
		    , rank_matrix.memo
	    FROM rank_matrix LEFT OUTER JOIN 
		    rank_detail ON rank_detail.ref_matrix_id = rank_matrix.id LEFT OUTER JOIN 
		    student ON student.id = rank_detail.ref_student_id LEFT OUTER JOIN 
		    class ON class.id = student.ref_class_id LEFT OUTER JOIN 
		    exam ON exam.id=rank_matrix.ref_exam_id
    ) as Rank_Table
Where rank_matrix_id = '" + matrixId + @"'
";
            #endregion

            BackgroundWorker bkw = new BackgroundWorker();
            DataTable dt = new DataTable();
            Exception bkwException = null;

            bkw.DoWork += delegate
            {
                string query = queryString;
                try
                {
                    QueryHelper queryHelper = new QueryHelper();
                    dt = queryHelper.Select(query);
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
                    throw new Exception("資料讀取錯誤", bkwException);
                }
                string selectedMatrixId = _MatrixIdDic[cboBatchId.Text];
                if (matrixId != selectedMatrixId)
                {
                    _IsLoading = false;
                    LoadRowData(null, null);
                }
                else
                {
                    try
                    {
                        #region 塞資料進dataGridView
                        List<DataGridViewRow> gridViewRowList = new List<DataGridViewRow>();
                        dgvScoreRank.Rows.Clear();
                        dgvScoreRank.SuspendLayout();
                        for (int row = 0; row < dt.Rows.Count; row++)
                        {
                            int tryParseInt;
                            decimal tryParseDecimal;
                            DataGridViewRow gridViewRow = new DataGridViewRow();
                            gridViewRow.CreateCells(dgvScoreRank);
                            gridViewRow.Cells[0].Value = "" + dt.Rows[row]["rank_matrix_id"];
                            gridViewRow.Cells[1].Value = "" + dt.Rows[row]["score_type"];
                            gridViewRow.Cells[2].Value = "" + dt.Rows[row]["score_category"];
                            gridViewRow.Cells[3].Value = "" + dt.Rows[row]["exam_name"];
                            gridViewRow.Cells[4].Value = "" + dt.Rows[row]["item_name"];
                            gridViewRow.Cells[5].Value = "" + dt.Rows[row]["rank_type"];
                            gridViewRow.Cells[6].Value = "" + dt.Rows[row]["rank_name"];
                            gridViewRow.Cells[7].Value = "" + dt.Rows[row]["class_name"];
                            gridViewRow.Cells[8].Value = Int32.TryParse("" + dt.Rows[row]["seat_no"], out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[9].Value = "" + dt.Rows[row]["student_number"];
                            gridViewRow.Cells[10].Value = "" + dt.Rows[row]["student_name"];
                            gridViewRow.Cells[11].Value = "" + dt.Rows[row]["student_status"];
                            gridViewRow.Cells[12].Value = Decimal.TryParse("" + dt.Rows[row]["score"], out tryParseDecimal) ? (decimal?)tryParseDecimal : null;
                            gridViewRow.Cells[13].Value = Int32.TryParse("" + dt.Rows[row]["rank"], out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[14].Value = Int32.TryParse("" + dt.Rows[row]["pr"], out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[15].Value = Int32.TryParse("" + dt.Rows[row]["percentile"], out tryParseInt) ? (int?)tryParseInt : null;
                            gridViewRow.Cells[16].Value = "" + dt.Rows[row]["school_year"];
                            gridViewRow.Cells[17].Value = "" + dt.Rows[row]["semester"];
                            gridViewRowList.Add(gridViewRow);
                        }
                        dgvScoreRank.Rows.AddRange(gridViewRowList.ToArray());
                        dgvScoreRank.ResumeLayout();
                        #endregion

                        lbMemo.Text = "" + dt.Rows[0]["memo"];
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            };

            bkw.RunWorkerAsync();

            _IsLoading = false;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExportToExcel_Click(object sender, EventArgs e)
        {
            saveFileDialog.Title = "匯出排名母群資料";
            saveFileDialog.FileName = "匯出排名母群資料.xlsx";
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
                        worksheet.Name = "排名母群資料";

                        int colIndex = 0;
                        foreach (DataGridViewColumn column in dgvScoreRank.Columns)
                        {
                            if (column.Visible == true)
                            {
                                worksheet.Cells[0, colIndex].PutValue(column.HeaderText);
                                colIndex++;
                            }
                        }

                        colIndex = 0;
                        foreach (DataGridViewColumn column in dgvScoreRank.Columns)
                        {
                            if (column.Visible == true)
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
    }
}
