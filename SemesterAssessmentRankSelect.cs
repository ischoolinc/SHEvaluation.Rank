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
        private volatile bool _IsClosing = false;
        private bool _suppressEvents = false;
        private bool _detailRunning = false;
        private bool _localRowsForCheck;
        private int _initGeneration = 0;
        private volatile int _detailGeneration = 0;
        private List<RankDataInfo> _allRows = new List<RankDataInfo>();
        private List<RankDataInfo> _viewRows = new List<RankDataInfo>();
        private int _sortColumn = -1;
        private bool _sortDescending;
        private string _filterScope;
        private List<RankDataInfo> _filterRows;
        private readonly System.Windows.Forms.Timer _searchTimer = new System.Windows.Forms.Timer();
        internal const int DisplayPageSize = 500;
        private int _pageOffset;
        private int _viewGeneration;
        private bool _exportRunning;
        private readonly DevComponents.DotNetBar.ButtonX _previousPage = new DevComponents.DotNetBar.ButtonX();
        private readonly DevComponents.DotNetBar.ButtonX _nextPage = new DevComponents.DotNetBar.ButtonX();

        private readonly List<string> gradeYearList = new List<string>();
        private readonly List<string> rankTypeList = new List<string>();

        public SemesterAssessmentRankSelect()
        {
            InitializeComponent();
            _searchTimer.Interval = 350;
            _searchTimer.Tick += delegate { _searchTimer.Stop(); LoadRowData(null, null); };
            FormClosed += delegate { _searchTimer.Dispose(); };
            DevComponents.DotNetBar.ButtonX retry = new DevComponents.DotNetBar.ButtonX();
            retry.Text = "重新查詢";
            retry.AccessibleName = "重新查詢排名資料";
            retry.SetBounds(710, 48, 105, 28);
            retry.Click += LoadRowData;
            Controls.Add(retry);
            _previousPage.Text = "上一頁";
            _nextPage.Text = "下一頁";
            _previousPage.SetBounds(590, btnExportToExcel.Top, 90, 28);
            _nextPage.SetBounds(690, btnExportToExcel.Top, 90, 28);
            _previousPage.Anchor = _nextPage.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            _previousPage.Enabled = _nextPage.Enabled = false;
            _previousPage.Click += delegate { ChangePage(-1); };
            _nextPage.Click += delegate { ChangePage(1); };
            Controls.Add(_previousPage);
            Controls.Add(_nextPage);
            pbLoading.BackColor = Color.White;
            dgvScoreRank.VirtualMode = true;
            dgvScoreRank.CellValueNeeded += dgvScoreRank_CellValueNeeded;
            dgvScoreRank.ColumnHeaderMouseClick += dgvScoreRank_ColumnHeaderMouseClick;
            foreach (DataGridViewColumn column in dgvScoreRank.Columns)
            {
                if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.AllCells)
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    column.Width = 120;
                }
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
            if (_suppressEvents || _IsClosing || _exportRunning)
                return;
            if (string.IsNullOrEmpty(cboSchoolYear.Text)
                || string.IsNullOrEmpty(cboSemester.Text)
                || string.IsNullOrEmpty(cboScoreType.Text)
                || string.IsNullOrEmpty(cboScoreCategory.Text))
                return;
            _detailGeneration++;
            _viewGeneration++;
            if (_detailRunning)
                return;
            BeginDetail(_detailGeneration);
        }

        private void BeginDetail(int generation)
        {
            _searchTimer.Stop();
            _detailRunning = true;
            _IsLoading = true;
            _previousPage.Enabled = _nextPage.Enabled = false;
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
            string scope = schoolYear + "|" + semester + "|" + scoreType + "|" + scoreCategory;
            if (_filterScope != scope)
            {
                _filterScope = scope;
                _filterRows = null;
                ResetResultFilters();
            }
            string itemName = cboItemName.Text;
            string rankType = cboRankType.Text;
            string studentNumber = txtStudentNum.Text;
            List<string> grades = new List<string>(gradeYearList);
            List<string> ranks = new List<string>(rankTypeList);
            BackgroundWorker worker = new BackgroundWorker();
            List<RankDataInfo> loaded = null;
            List<RankDataInfo> options = null;
            Exception error = null;
            long queryMs = 0;
            worker.DoWork += delegate
            {
                try
                {
                    string sql = SemesterAssessmentRankQuery.MatrixSql(schoolYear, semester, scoreType, scoreCategory, grades, ranks);
                    if (sql.Length == 0)
                    {
                        loaded = new List<RankDataInfo>();
                        return;
                    }
                    Stopwatch watch = Stopwatch.StartNew();
                    QueryHelper helper = new QueryHelper();
                    DataTable matrices = helper.Select(sql);
                    List<long> matrixIds = SemesterAssessmentRankQuery.MatrixIds(matrices);
                    options = SemesterAssessmentRankQuery.ReadRows(matrices);
                    loaded = new List<RankDataInfo>();
                    if (matrixIds.Count > 0)
                    {
                        loaded = SemesterAssessmentRankQuery.ReadPages(
                            delegate(long cursor) { return helper.Select(SemesterAssessmentRankQuery.DetailPageSql(schoolYear, semester, scoreType, scoreCategory, grades, ranks, matrixIds, itemName, rankType, studentNumber, cursor)); },
                            delegate { return _IsClosing || generation != _detailGeneration; });
                        if (loaded == null) return;
                    }
                    if (!SemesterAssessmentRankQuery.SameMatrices(matrixIds, SemesterAssessmentRankQuery.MatrixIds(helper.Select(sql))))
                        throw new InvalidOperationException("讀取期間目前採計排名已變動，請重新查詢。");
                    SemesterAssessmentRankQuery.SortDefault(loaded);
                    queryMs = watch.ElapsedMilliseconds;
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
                        if (options != null) { _filterRows = options; FillResultFilters(); }
                        RestoreAfterDetailFailure(error);
                        return;
                    }
                    _allRows = loaded ?? new List<RankDataInfo>();
                    _filterRows = options;
                    FillResultFilters();
                    BeginView(queryMs);
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
            if (_suppressEvents || _IsClosing)
                return;
            if (!_localRowsForCheck)
            {
                if (sender == txtStudentNum)
                {
                    // Invalidate the running query immediately; debounce only the next request.
                    _detailGeneration++;
                    _viewGeneration++;
                    btnExportToExcel.Enabled = false;
                    _previousPage.Enabled = _nextPage.Enabled = false;
                    _viewRows = new List<RankDataInfo>();
                    dgvScoreRank.RowCount = 0;
                    lblMsgCount.Text = "等待查詢";
                    _searchTimer.Stop();
                    _searchTimer.Start();
                }
                else LoadRowData(sender, e);
                return;
            }
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

        private void ResetResultFilters()
        {
            _suppressEvents = true;
            try
            {
                foreach (ComboBox combo in new ComboBox[] { cboItemName, cboRankType })
                {
                    combo.Items.Clear();
                    combo.Items.Add("全部");
                    combo.SelectedIndex = 0;
                }
            }
            finally { _suppressEvents = false; }
        }

        private void FillDistinct(ComboBox combo, int columnIndex)
        {
            string selected = combo.Text;
            combo.Items.Clear();
            combo.Items.Add("全部");
            foreach (RankDataInfo row in _filterRows ?? _allRows)
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
            _pageOffset = 0;
            BindPage();
            watch.Stop();
            return watch.ElapsedMilliseconds;
        }

        private void BeginView(long queryMs)
        {
            int generation = ++_viewGeneration;
            List<RankDataInfo> source = _allRows;
            string item = cboItemName.Text, rank = cboRankType.Text, number = txtStudentNum.Text;
            int column = _sortColumn;
            bool descending = _sortDescending;
            _IsLoading = true;
            btnExportToExcel.Enabled = false;
            _previousPage.Enabled = _nextPage.Enabled = false;
            pbLoading.Visible = true;
            lblMsgCount.Text = "正在整理排名資料";
            BackgroundWorker worker = new BackgroundWorker();
            long elapsed = 0;
            worker.DoWork += delegate(object sender, DoWorkEventArgs e)
            {
                Stopwatch watch = Stopwatch.StartNew();
                List<RankDataInfo> result = SemesterAssessmentRankQuery.Filter(source, item, rank, number);
                SemesterAssessmentRankQuery.SortRows(result, column, descending);
                elapsed = watch.ElapsedMilliseconds;
                e.Result = result;
            };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                if (!CanTouchUi() || generation != _viewGeneration) return;
                if (e.Error != null) { RestoreAfterDetailFailure(e.Error); return; }
                try
                {
                    _viewRows = (List<RankDataInfo>)e.Result;
                    _pageOffset = 0;
                    _IsLoading = false;
                    pbLoading.Visible = false;
                    BindPage();
                    btnExportToExcel.Enabled = true;
                    SetEditorsEnabled(true);
                    MotherForm.SetStatusBarMessage("資料讀取完成，查詢 " + queryMs + " 毫秒，整理 " + elapsed + " 毫秒，共 " + _viewRows.Count + " 筆");
                }
                catch (Exception ex) { RestoreAfterDetailFailure(ex); }
            };
            worker.RunWorkerAsync();
        }

        private void BindPage()
        {
            UpdateSortGlyph();
            int count = Math.Min(DisplayPageSize, Math.Max(0, _viewRows.Count - _pageOffset));
            if (dgvScoreRank.RowCount != count) dgvScoreRank.RowCount = count;
            if (count > 0) dgvScoreRank.FirstDisplayedScrollingRowIndex = 0;
            dgvScoreRank.Invalidate();
            lblMsgCount.Text = "共 " + _viewRows.Count + " 筆"
                + (count == 0 ? "" : "，顯示 " + (_pageOffset + 1) + "～" + (_pageOffset + count) + " 筆");
            _previousPage.Enabled = !_IsLoading && !_exportRunning && _pageOffset > 0;
            _nextPage.Enabled = !_IsLoading && !_exportRunning && _pageOffset + count < _viewRows.Count;
        }

        private void ChangePage(int direction)
        {
            if (_IsLoading || _detailRunning || _exportRunning || _IsClosing) return;
            int offset = _pageOffset + direction * DisplayPageSize;
            if (offset < 0 || offset >= _viewRows.Count) return;
            _pageOffset = offset;
            BindPage();
        }

        private void dgvScoreRank_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            RankDataInfo row = RowForView(e.RowIndex);
            if (row != null) e.Value = SemesterAssessmentRankQuery.CellValue(row, e.ColumnIndex);
        }

        private void dgvScoreRank_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_IsLoading || _detailRunning || _exportRunning || _IsClosing)
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
            if (_localRowsForCheck) ApplyView();
            else BeginView(0);
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
            if (_IsLoading || _detailRunning || _exportRunning || _IsClosing) return;
            btnExportToExcel.Enabled = false;
            saveFileDialog.Title = "匯出排名資料";
            saveFileDialog.FileName = "匯出排名資料.xlsx";
            saveFileDialog.Filter = "Excel (*.xlsx)|*.xlsx|所有檔案 (*.*)|*.*";
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                btnExportToExcel.Enabled = true;
                return;
            }
            string path = saveFileDialog.FileName;
            List<int> columns = ExportColumnIndexes();
            List<string> headers = new List<string>();
            foreach (int column in columns) headers.Add(dgvScoreRank.Columns[column].HeaderText);
            List<RankDataInfo> rows = new List<RankDataInfo>(_viewRows);
            _exportRunning = true;
            _previousPage.Enabled = _nextPage.Enabled = false;
            SetEditorsEnabled(false);
            lblMsgCount.Text = "正在匯出全部 " + rows.Count + " 筆";
            pbLoading.Visible = true;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate { WriteExportWorkbook(path, rows, columns, headers); };
            worker.RunWorkerCompleted += delegate(object owner, RunWorkerCompletedEventArgs result)
            {
                _exportRunning = false;
                if (!CanTouchUi()) return;
                pbLoading.Visible = false;
                SetEditorsEnabled(true);
                BindPage();
                btnExportToExcel.Enabled = true;
                if (result.Error != null)
                {
                    MessageBox.Show("檔案儲存失敗：" + result.Error.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (MessageBox.Show("檔案儲存完成，是否開啟？", "是否開啟", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("檔案開啟失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            worker.RunWorkerAsync();
        }

        internal static void WriteExportWorkbook(string path, IList<RankDataInfo> rows, IList<int> columns, IList<string> headers)
        {
            Workbook workbook = new Workbook();
            Worksheet worksheet = workbook.Worksheets[0];
            worksheet.Name = "排名資料";
            for (int column = 0; column < columns.Count; column++)
                worksheet.Cells[0, column].PutValue(headers[column]);
            for (int index = 0; index < rows.Count; index++)
                for (int column = 0; column < columns.Count; column++)
                    worksheet.Cells[index + 1, column].PutValue("" + SemesterAssessmentRankQuery.CellValue(rows[index], columns[column]));
            workbook.Save(path);
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
            lblMsgCount.Text = "讀取失敗，請重新查詢";
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
            if (rowIndex < 0 || rowIndex >= dgvScoreRank.RowCount || _pageOffset + rowIndex >= _viewRows.Count)
                return null;
            return _viewRows[_pageOffset + rowIndex];
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
            _localRowsForCheck = true;
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

        internal void ChangePageForCheck(int direction) { ChangePage(direction); }

        internal void BeginViewForCheck()
        {
            _localRowsForCheck = false;
            BeginView(0);
        }

        internal bool ViewRunningForCheck() { return _IsLoading; }


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
