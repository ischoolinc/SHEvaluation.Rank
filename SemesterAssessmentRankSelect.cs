using Aspose.Cells;
using FISCA.Data;
using FISCA.Presentation;
using FISCA.Presentation.Controls;
using SHEvaluation.Rank.DAO;
using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SHEvaluation.Rank
{
    public partial class SemesterAssessmentRankSelect : BaseForm
    {
        private bool _IsLoading = false;
        private bool _IsClosing = false;
        private bool _suppressEvents = false;
        private bool _detailRunning = false;
        private bool _localRowsForCheck;
        private int _initGeneration = 0;
        private int _detailGeneration = 0;
        private List<RankDataInfo> _allRows = new List<RankDataInfo>();
        private List<RankDataInfo> _viewRows = new List<RankDataInfo>();
        private int _sortColumn = -1;
        private bool _sortDescending;

        private readonly List<string> gradeYearList = new List<string>();
        private readonly List<string> rankTypeList = new List<string>();

        public SemesterAssessmentRankSelect()
        {
            InitializeComponent();
            pbLoading.BackColor = Color.White;
            dgvScoreRank.VirtualMode = true;
            dgvScoreRank.CellValueNeeded += dgvScoreRank_CellValueNeeded;
            dgvScoreRank.ColumnHeaderMouseClick += dgvScoreRank_ColumnHeaderMouseClick;
            foreach (DataGridViewColumn column in dgvScoreRank.Columns)
            {
                if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.AllCells)
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                column.SortMode = column is DataGridViewButtonColumn
                    ? DataGridViewColumnSortMode.NotSortable
                    : DataGridViewColumnSortMode.Programmatic;
            }
            MotherGroupConfirmGrid.InstallBoundedAccessibility(dgvScoreRank);
        }

        private void SemesterAssessmentRankSelect_Load(object sender, EventArgs e)
        {
            if (_localRowsForCheck)
                return;
            btnExportToExcel.Enabled = false;
            rankTypeList.Clear();
            rankTypeList.Add("班排名");
            rankTypeList.Add("科排名");
            rankTypeList.Add("年排名");
            rankTypeList.Add("類別1排名");
            rankTypeList.Add("類別2排名");
            pbLoading.Visible = true;
            SetEditorsEnabled(false);
            int generation = ++_initGeneration;
            BackgroundWorker worker = new BackgroundWorker();
            DataTable grades = null;
            DataTable options = null;
            Exception error = null;
            long gradeMs = 0;
            long optionMs = 0;
            worker.DoWork += delegate
            {
                try
                {
                    QueryHelper queryHelper = new QueryHelper();
                    Stopwatch watch = Stopwatch.StartNew();
                    grades = queryHelper.Select(SemesterAssessmentRankQuery.GradeYearSql);
                    gradeMs = watch.ElapsedMilliseconds;
                    watch.Restart();
                    options = queryHelper.Select(SemesterAssessmentRankQuery.OptionSql);
                    optionMs = watch.ElapsedMilliseconds;
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            };
            worker.RunWorkerCompleted += delegate
            {
                try
                {
                    if (!CanTouchUi() || generation != _initGeneration)
                    {
                        RestoreIdle();
                        return;
                    }
                    if (error != null)
                    {
                        RestoreIdle();
                        ShowFailure(error, "檢索初始化", false);
                        return;
                    }
                    gradeYearList.Clear();
                    gradeYearList.AddRange(ReadGrades(grades));
                    if (!FillOptions(options))
                    {
                        RestoreIdle();
                        MessageBox.Show("無可檢視的資料");
                        return;
                    }
                    SetEditorsEnabled(true);
                    pbLoading.Visible = false;
                    MotherForm.SetStatusBarMessage("條件讀取完成，年級查詢 " + gradeMs + " 毫秒，選項查詢 " + optionMs + " 毫秒");
                    LoadRowData(null, null);
                }
                catch (Exception ex)
                {
                    RestoreIdle();
                    if (CanTouchUi())
                        ShowFailure(ex, "檢索初始化", false);
                }
            };
            worker.RunWorkerAsync();
        }

        private void LoadRowData(object sender, EventArgs e)
        {
            if (_suppressEvents || _IsClosing)
                return;
            if (string.IsNullOrEmpty(cboSchoolYear.Text)
                || string.IsNullOrEmpty(cboSemester.Text)
                || string.IsNullOrEmpty(cboScoreType.Text)
                || string.IsNullOrEmpty(cboScoreCategory.Text))
                return;
            _detailGeneration++;
            if (_detailRunning)
                return;
            BeginDetail(_detailGeneration);
        }

        private void BeginDetail(int generation)
        {
            _detailRunning = true;
            _IsLoading = true;
            btnExportToExcel.Enabled = false;
            pbLoading.Visible = true;
            lblMsgCount.Text = "資料讀取中";
            _allRows = new List<RankDataInfo>();
            _viewRows = new List<RankDataInfo>();
            dgvScoreRank.RowCount = 0;
            string schoolYear = cboSchoolYear.Text;
            string semester = cboSemester.Text;
            string scoreType = cboScoreType.Text;
            string scoreCategory = cboScoreCategory.Text;
            List<string> grades = new List<string>(gradeYearList);
            List<string> ranks = new List<string>(rankTypeList);
            BackgroundWorker worker = new BackgroundWorker();
            List<RankDataInfo> loaded = null;
            Exception error = null;
            long queryMs = 0;
            worker.DoWork += delegate
            {
                try
                {
                    string sql = SemesterAssessmentRankQuery.DetailSql(schoolYear, semester, scoreType, scoreCategory, grades, ranks);
                    if (sql.Length == 0)
                    {
                        loaded = new List<RankDataInfo>();
                        return;
                    }
                    Stopwatch watch = Stopwatch.StartNew();
                    DataTable table = new QueryHelper().Select(sql);
                    queryMs = watch.ElapsedMilliseconds;
                    loaded = SemesterAssessmentRankQuery.ReadRows(table);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            };
            worker.RunWorkerCompleted += delegate
            {
                try
                {
                    _detailRunning = false;
                    if (!CanTouchUi())
                    {
                        _IsLoading = false;
                        pbLoading.Visible = false;
                        return;
                    }
                    if (generation != _detailGeneration)
                    {
                        BeginDetail(_detailGeneration);
                        return;
                    }
                    if (error != null)
                    {
                        RestoreAfterDetailFailure(error);
                        return;
                    }
                    _allRows = loaded ?? new List<RankDataInfo>();
                    FillResultFilters();
                    long bindMs = ApplyView();
                    _IsLoading = false;
                    pbLoading.Visible = false;
                    btnExportToExcel.Enabled = true;
                    SetEditorsEnabled(true);
                    MotherForm.SetStatusBarMessage("資料讀取完成，查詢 " + queryMs + " 毫秒，畫面 " + bindMs + " 毫秒，共 " + _viewRows.Count + " 筆");
                }
                catch (Exception ex)
                {
                    RestoreAfterDetailFailure(ex);
                }
            };
            worker.RunWorkerAsync();
        }

        private void FillingDataGridView(object sender, EventArgs e)
        {
            if (_suppressEvents || _IsLoading || _detailRunning || _IsClosing)
                return;
            ApplyView();
            btnExportToExcel.Enabled = true;
        }

        private void FillResultFilters()
        {
            _suppressEvents = true;
            try
            {
                FillDistinct(cboItemName, 3);
                FillDistinct(cboRankType, 4);
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void FillDistinct(ComboBox combo, int columnIndex)
        {
            string selected = combo.Text;
            combo.Items.Clear();
            combo.Items.Add("全部");
            foreach (RankDataInfo row in _allRows)
            {
                string value = "" + SemesterAssessmentRankQuery.CellValue(row, columnIndex);
                if (!combo.Items.Contains(value))
                    combo.Items.Add(value);
            }
            if (!string.IsNullOrEmpty(selected) && combo.Items.Contains(selected))
                combo.SelectedIndex = combo.Items.IndexOf(selected);
            else
                combo.SelectedIndex = 0;
        }

        private long ApplyView()
        {
            Stopwatch watch = Stopwatch.StartNew();
            string itemName = cboItemName == null ? "" : cboItemName.Text;
            string rankType = cboRankType == null ? "" : cboRankType.Text;
            string studentNumber = txtStudentNum == null ? "" : txtStudentNum.Text;
            _viewRows = SemesterAssessmentRankQuery.Filter(_allRows, itemName, rankType, studentNumber);
            SemesterAssessmentRankQuery.SortRows(_viewRows, _sortColumn, _sortDescending);
            UpdateSortGlyph();
            dgvScoreRank.RowCount = 0;
            dgvScoreRank.RowCount = _viewRows.Count;
            lblMsgCount.Text = "共 " + _viewRows.Count + " 筆";
            watch.Stop();
            return watch.ElapsedMilliseconds;
        }

        private void dgvScoreRank_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _viewRows.Count)
                return;
            e.Value = SemesterAssessmentRankQuery.CellValue(_viewRows[e.RowIndex], e.ColumnIndex);
        }

        private void dgvScoreRank_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_IsLoading || _detailRunning || _IsClosing)
                return;
            if (e.ColumnIndex < 0 || e.ColumnIndex >= dgvScoreRank.Columns.Count)
                return;
            if (dgvScoreRank.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.Programmatic)
                return;
            if (_sortColumn == e.ColumnIndex)
                _sortDescending = !_sortDescending;
            else
            {
                _sortColumn = e.ColumnIndex;
                _sortDescending = false;
            }
            ApplyView();
        }

        private void UpdateSortGlyph()
        {
            foreach (DataGridViewColumn column in dgvScoreRank.Columns)
            {
                if (column.Index == _sortColumn)
                    column.HeaderCell.SortGlyphDirection = _sortDescending ? System.Windows.Forms.SortOrder.Descending : System.Windows.Forms.SortOrder.Ascending;
                else
                    column.HeaderCell.SortGlyphDirection = System.Windows.Forms.SortOrder.None;
            }
        }

        private void dgvScoreRank_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (dgvScoreRank.Columns[e.ColumnIndex].HeaderText != "檢視")
                return;
            RankDataInfo row = RowForView(e.RowIndex);
            if (row == null)
                return;
            SemesterMatrixRankSelect frm = new SemesterMatrixRankSelect(
                row.rank_matrix_id ?? "",
                row.ref_student_id ?? "",
                row.school_year ?? "",
                row.semester ?? "",
                row.score_type ?? "",
                row.score_category ?? "",
                row.item_name ?? "",
                row.rank_type ?? "",
                row.rank_name ?? "");
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
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                btnExportToExcel.Enabled = true;
                return;
            }
            try
            {
                Workbook workbook = new Workbook();
                Worksheet worksheet = workbook.Worksheets[0];
                worksheet.Name = "排名資料";
                List<int> columns = ExportColumnIndexes();
                List<List<string>> matrix = ExportMatrix(columns);
                for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                    worksheet.Cells[0, colIndex].PutValue(dgvScoreRank.Columns[columns[colIndex]].HeaderText);
                for (int rowIndex = 0; rowIndex < matrix.Count; rowIndex++)
                {
                    for (int colIndex = 0; colIndex < matrix[rowIndex].Count; colIndex++)
                        worksheet.Cells[rowIndex + 1, colIndex].PutValue(matrix[rowIndex][colIndex]);
                }
                workbook.Save(saveFileDialog.FileName);
                btnExportToExcel.Enabled = true;
                if (MessageBox.Show("檔案儲存完成，是否開啟？", "是否開啟", MessageBoxButtons.YesNo) == DialogResult.Yes)
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

        private void SemesterAssessmentRankSelect_Resize(object sender, EventArgs e)
        {
            pbLoading.Location = new Point(this.Width / 2 - 20, this.Height / 2 - 20);
        }

        private void SemesterAssessmentRankSelect_FormClosing(object sender, FormClosingEventArgs e)
        {
            _IsClosing = true;
        }

        private bool FillOptions(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
                return false;
            _suppressEvents = true;
            try
            {
                FillCombo(cboSchoolYear, table, 0, K12.Data.School.DefaultSchoolYear);
                FillCombo(cboSemester, table, 1, K12.Data.School.DefaultSemester);
                FillCombo(cboScoreType, table, 2, null);
                FillCombo(cboScoreCategory, table, 3, null);
                cboItemName.Items.Clear();
                cboItemName.Items.Add("全部");
                cboItemName.SelectedIndex = 0;
                cboRankType.Items.Clear();
                cboRankType.Items.Add("全部");
                foreach (string rankType in rankTypeList)
                    cboRankType.Items.Add(rankType);
                cboRankType.SelectedIndex = 0;
            }
            finally
            {
                _suppressEvents = false;
            }
            return cboSchoolYear.Items.Count > 0;
        }

        private static void FillCombo(ComboBox combo, DataTable table, int column, string preferred)
        {
            combo.Items.Clear();
            foreach (DataRow row in table.Rows)
            {
                string value = Convert.ToString(row[column]);
                if (!string.IsNullOrEmpty(value) && !combo.Items.Contains(value))
                    combo.Items.Add(value);
            }
            if (combo.Items.Count == 0)
                return;
            if (!string.IsNullOrEmpty(preferred) && combo.Items.Contains(preferred))
                combo.SelectedIndex = combo.Items.IndexOf(preferred);
            else
                combo.SelectedIndex = 0;
        }

        private static List<string> ReadGrades(DataTable table)
        {
            List<string> grades = new List<string>();
            if (table == null)
                return grades;
            foreach (DataRow row in table.Rows)
            {
                string grade = Convert.ToString(row["grade_year"]);
                if (!string.IsNullOrWhiteSpace(grade) && !grades.Contains(grade.Trim()))
                    grades.Add(grade.Trim());
            }
            return grades;
        }

        private void RestoreAfterDetailFailure(Exception error)
        {
            _IsLoading = false;
            _detailRunning = false;
            _allRows = new List<RankDataInfo>();
            _viewRows = new List<RankDataInfo>();
            if (!CanTouchUi())
                return;
            try
            {
                dgvScoreRank.RowCount = 0;
            }
            catch (Exception)
            {
            }
            pbLoading.Visible = false;
            btnExportToExcel.Enabled = false;
            lblMsgCount.Text = "";
            SetEditorsEnabled(true);
            ShowFailure(error, "檢索明細", false);
        }

        private void RestoreIdle()
        {
            _IsLoading = false;
            _detailRunning = false;
            if (!CanTouchUi())
                return;
            pbLoading.Visible = false;
            btnExportToExcel.Enabled = false;
            SetEditorsEnabled(true);
        }

        private void ShowFailure(Exception error, string stage, bool warnBeforeRetry)
        {
            if (!CanTouchUi())
                return;
            SemesterRankCalculationTrace trace = SemesterRankCalculationTrace.Start("", "", "排名資料檢索", "");
            trace.Mark(stage);
            SemesterRankFailureReport report = SemesterRankFailureReport.Create(trace, error, warnBeforeRetry);
            report.Save();
            MessageBox.Show(report.UserMessage);
        }

        private void SetEditorsEnabled(bool enabled)
        {
            cboSchoolYear.Enabled = enabled;
            cboSemester.Enabled = enabled;
            cboScoreType.Enabled = enabled;
            cboScoreCategory.Enabled = enabled;
            cboItemName.Enabled = enabled;
            cboRankType.Enabled = enabled;
            txtStudentNum.Enabled = enabled;
        }

        private bool CanTouchUi()
        {
            return !_IsClosing && !IsDisposed && IsHandleCreated;
        }

        private RankDataInfo RowForView(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _viewRows.Count)
                return null;
            return _viewRows[rowIndex];
        }

        private List<int> ExportColumnIndexes()
        {
            List<int> columns = new List<int>();
            foreach (DataGridViewColumn column in dgvScoreRank.Columns)
            {
                if (column.Visible && column.HeaderText != "檢視")
                    columns.Add(column.Index);
            }
            return columns;
        }

        private List<List<string>> ExportMatrix(List<int> columns)
        {
            List<List<string>> matrix = new List<List<string>>();
            if (columns == null)
                return matrix;
            for (int rowIndex = 0; rowIndex < _viewRows.Count; rowIndex++)
            {
                List<string> line = new List<string>();
                for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                    line.Add("" + SemesterAssessmentRankQuery.CellValue(_viewRows[rowIndex], columns[colIndex]));
                matrix.Add(line);
            }
            return matrix;
        }

        internal void UseLocalRowsForCheck()
        {
            _localRowsForCheck = true;
        }

        internal int ColumnCountForCheck()
        {
            return dgvScoreRank.Columns.Count;
        }

        internal DataGridView RetrievalGridForCheck()
        {
            return dgvScoreRank;
        }

        internal void FilterRankTypeForCheck(string rankType)
        {
            if (!cboRankType.Items.Contains(rankType))
                cboRankType.Items.Add(rankType);
            cboRankType.SelectedItem = rankType;
        }

        internal void LoadRowsForCheck(IList<RankDataInfo> rows)
        {
            _allRows = new List<RankDataInfo>(rows ?? new List<RankDataInfo>());
            _suppressEvents = true;
            try
            {
                cboItemName.Items.Clear();
                cboItemName.Items.Add("全部");
                cboItemName.SelectedIndex = 0;
                cboRankType.Items.Clear();
                cboRankType.Items.Add("全部");
                cboRankType.SelectedIndex = 0;
                txtStudentNum.Text = "";
            }
            finally
            {
                _suppressEvents = false;
            }
            ApplyView();
        }

        internal void ClickHeaderForCheck(string header)
        {
            dgvScoreRank_ColumnHeaderMouseClick(dgvScoreRank, new DataGridViewCellMouseEventArgs(ColumnIndexForCheck(header), -1, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));
        }

        internal void FilterItemForCheck(string itemName)
        {
            if (!cboItemName.Items.Contains(itemName))
                cboItemName.Items.Add(itemName);
            cboItemName.SelectedItem = itemName;
        }

        internal void FilterStudentNumberForCheck(string studentNumber)
        {
            txtStudentNum.Text = studentNumber ?? "";
        }

        internal int ColumnIndexForCheck(string header)
        {
            foreach (DataGridViewColumn column in dgvScoreRank.Columns)
            {
                if (column.HeaderText == header)
                    return column.Index;
            }
            return -1;
        }

        internal DataGridViewColumnSortMode SortModeForCheck(string header)
        {
            int index = ColumnIndexForCheck(header);
            if (index < 0)
                return DataGridViewColumnSortMode.NotSortable;
            return dgvScoreRank.Columns[index].SortMode;
        }

        internal string SortGlyphForCheck(string header)
        {
            int index = ColumnIndexForCheck(header);
            if (index < 0)
                return "";
            return dgvScoreRank.Columns[index].HeaderCell.SortGlyphDirection.ToString();
        }

        internal RankDataInfo ViewRowForCheck(int rowIndex)
        {
            return RowForView(rowIndex);
        }

        internal int ViewCountForCheck()
        {
            return _viewRows.Count;
        }

        internal string ShownStudentNumberForCheck(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvScoreRank.RowCount)
                return null;
            return Convert.ToString(dgvScoreRank.Rows[rowIndex].Cells[ColumnIndexForCheck("學號")].Value);
        }

        internal List<string> ExportLinesForCheck()
        {
            List<string> lines = new List<string>();
            foreach (List<string> line in ExportMatrix(ExportColumnIndexes()))
                lines.Add(string.Join("\t", line.ToArray()));
            return lines;
        }
    }
}
