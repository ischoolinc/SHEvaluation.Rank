using Aspose.Cells;
using SHEvaluation.Rank.MotherGroup;
using SHEvaluation.Rank.RegularRank;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace SHEvaluation.Rank
{
    public partial class RegularRankMotherGroupConfirmForm : FISCA.Presentation.Controls.BaseForm
    {
        public const string DeferredEntryMessage = "三種來源、歷史加人與計算呼叫鏈已接線；寫入前仍須通過執行當下結構／傳輸檢查。";

        private readonly CalculateRegularAssessmentRank _host;
        private MotherGroupConfirmList _list;
        private bool _binding;
        private bool _gridsReady;
        private bool _returning;
        private readonly RegularRankCalculationGuard _guard = new RegularRankCalculationGuard();
        private bool _adding;
        private bool _historySource;
        private string _validationText;
        private int _entryGeneration = -1;
        private readonly Dictionary<string, Dictionary<string, bool>> _entryFlags = new Dictionary<string, Dictionary<string, bool>>();
        private readonly System.Windows.Forms.Timer _searchTimer;

        public RegularRankMotherGroupConfirmForm()
        {
            InitializeComponent();
            if (components == null) components = new Container();
            _searchTimer = new System.Windows.Forms.Timer(components) { Interval = 250 };
            _searchTimer.Tick += delegate { _searchTimer.Stop(); ApplySearch(); };
            lblCalculateBlock.Click += delegate
            {
                if (!string.IsNullOrEmpty(_validationText)) MessageBox.Show(this, _validationText, "發布檢查結果");
            };
            lblCalculateBlock.Text = RegularRankCalculationEntry.BlockedMessage;
            btnCalculate.Enabled = false;
            btnAddStudent.Enabled = false;
            txtAddStudent.Enabled = false;
            btnExcel.Enabled = true;
        }

        public RegularRankMotherGroupConfirmForm(CalculateRegularAssessmentRank host)
            : this()
        {
            if (host == null)
                throw new ArgumentNullException("host");
            _host = host;
        }

        internal MotherGroupConfirmList List
        {
            get { return _list; }
        }

        internal DataGridView StudentGrid
        {
            get { return dgvStudents; }
        }

        internal DataGridView PopulationGrid
        {
            get { return dgvPopulation; }
        }

        internal DataGridView DetailGrid
        {
            get { return dgvDetail; }
        }

        internal bool CalculationRunning
        {
            get { return _guard.Running; }
        }

        internal bool CalculateButtonEnabled()
        {
            return btnCalculate.Enabled;
        }

        public void PrepareProtectedGrids()
        {
            EnsureGrids();
        }

        public void ShowStudents(RegularRankConfirmRequest request, IList<ConfirmStudentRow> rows)
        {
            EnsureGrids();
            _historySource = request != null && request.SourceKind == RegularRankSourceKind.History;
            lblTargetValue.Text = request == null
                ? ""
                : (request.TargetSchoolYear + "-" + request.TargetSemester + " " + request.TargetExamName + "（ID " + request.TargetExamId + "）");
            lblSourceValue.Text = request == null ? "" : "來源：" + RegularRankPeriodSourceEditor.SourceText(request.SourceKind)
                + (request.SourceKind == RegularRankSourceKind.Excel ? "｜" + request.ExcelIdentity
                    : request.SourceKind == RegularRankSourceKind.History ? "｜批次 " + request.SourceBatchId + "｜" + request.SourceSchoolYear + "-" + request.SourceSemester + " " + request.SourceExamName
                    : "。依來源母群計算指定期間成績。");
            lblDeferred.Text = "歷史來源可依學號加入學生；加入後回條件頁更新科目，再按下一步。";
            btnAddStudent.Text = "依學號加入";
            btnAddStudent.Enabled = request != null && request.SourceKind == RegularRankSourceKind.History;
            txtAddStudent.Enabled = btnAddStudent.Enabled;
            _validationText = _host == null ? "" : _host.PublicationValidationError;
            if (RegularRankCalculationEntry.ExecutionAllowed)
                lblCalculateBlock.Text = "排名資料表結構檢查已通過，寫入僅限 debug6。";
            else if (!string.IsNullOrEmpty(_validationText))
                lblCalculateBlock.Text = "最近發布檢查未通過；計算前會再檢查，失敗則不保存設定。點此查看原因。";
            else
                lblCalculateBlock.Text = "計算前會確認排名資料表結構。";
            _list = new MotherGroupConfirmList();
            _list.Load(rows ?? new List<ConfirmStudentRow>());
            int generation = _host == null ? _entryGeneration + 1 : _host.PopulationGeneration;
            if (_entryGeneration != generation) { _entryFlags.Clear(); _entryGeneration = generation; }
            foreach (ConfirmStudentRow row in _list.Rows)
                if (!_entryFlags.ContainsKey(row.Group.StudentId))
                {
                    Dictionary<string, bool> flags = new Dictionary<string, bool>();
                    foreach (RankSlot slot in row.Group.Working) flags.Add(slot.RankType, slot.Participate);
                    _entryFlags.Add(row.Group.StudentId, flags);
                }
            _searchTimer.Stop();
            _binding = true;
            txtSearch.Text = "";
            dgvStudents.SuspendLayout();
            try
            {
            MotherGroupConfirmGrid.Show(dgvStudents, _list.Rows);
            }
            finally { dgvStudents.ResumeLayout(); }
            _binding = false;
            dgvStudents.ContextMenuStrip = MotherGroupConfirmGrid.CreateParticipationMenu(
                dgvStudents, _list, lblSummary, dgvPopulation, dgvDetail, EditAllowed);
            MotherGroupConfirmGrid.RefreshSummary(lblSummary, dgvPopulation, dgvDetail, _list);
            btnCalculate.Enabled = RegularRankCalculationEntry.CanOfferCalculate(_list.Rows.Count);
            btnCalculate.Text = "計算排名";
        }

        public Workbook CreateExportWorkbook()
        {
            if (_list == null || _list.Rows.Count == 0)
                throw new InvalidOperationException("沒有可匯出的確認清單。");
            return ExportCopy(_list.CopyWorking());
        }

        private static Workbook ExportCopy(MotherGroupConfirmList copy)
        {
            List<string> missing = MotherGroupExcelExchange.MissingStatusIds(copy);
            Dictionary<string, int> current = null;
            if (missing.Count > 0)
                current = MotherGroupExcelExchange.ReadCurrentStatuses(missing,
                    new FISCA.Data.QueryHelper().Select(MotherGroupExcelExchange.SelectCurrentStatusSql(missing)));
            return MotherGroupExcelExchange.Create(copy, current);
        }

        private void RegularRankMotherGroupConfirmForm_Load(object sender, EventArgs e)
        {
            if (Site != null && Site.DesignMode)
                return;
            EnsureGrids();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // CenterParent can reposition an owned form after Load. Clamp once
            // the native window has actually been shown.
            if (_host != null)
            {
                System.Drawing.Rectangle area = Screen.FromControl(_host).WorkingArea;
                Width = Math.Min(Width, area.Width);
                Height = Math.Min(Height, area.Height);
                Location = new System.Drawing.Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
            }
        }

        private void EnsureGrids()
        {
            if (_gridsReady)
                return;
            MotherGroupConfirmGrid.Configure(dgvStudents);
            MotherGroupConfirmGrid.ConfigurePopulation(dgvPopulation);
            MotherGroupConfirmGrid.ConfigureDetail(dgvDetail);
            dgvStudents.CurrentCellDirtyStateChanged += delegate
            {
                if (!EditAllowed())
                    return;
                if (dgvStudents.IsCurrentCellDirty)
                    dgvStudents.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvStudents.CellValueChanged += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (_binding || _list == null)
                    return;
                if (!EditAllowed())
                {
                    _binding = true;
                    MotherGroupConfirmGrid.RefreshParticipation(dgvStudents, _list);
                    _binding = false;
                    return;
                }
                MotherGroupConfirmGrid.ApplyCellEdit(dgvStudents, _list, e.RowIndex, e.ColumnIndex);
                MotherGroupConfirmGrid.RefreshSummary(lblSummary, dgvPopulation, dgvDetail, _list);
            };
            dgvStudents.CellMouseDown += delegate (object sender, DataGridViewCellMouseEventArgs e)
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                    return;
                if (!dgvStudents.Rows[e.RowIndex].Selected)
                {
                    dgvStudents.ClearSelection();
                    dgvStudents.Rows[e.RowIndex].Selected = true;
                }
            };
            _gridsReady = true;
        }

        private bool EditAllowed()
        {
            return !_guard.Running && !_adding;
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_binding || _list == null || !_gridsReady)
                return;
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void ApplySearch()
        {
            if (_binding || _list == null || !_gridsReady || !EditAllowed()) return;
            HashSet<string> visible = new HashSet<string>(_list.VisibleStudentIds(txtSearch.Text));
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (ConfirmStudentRow row in _list.Rows) if (visible.Contains(row.Group.StudentId)) rows.Add(row);
            _binding = true;
            dgvStudents.SuspendLayout();
            try { MotherGroupConfirmGrid.Show(dgvStudents, rows); }
            finally { dgvStudents.ResumeLayout(); _binding = false; }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (!EditAllowed() || _list == null)
                return;
            _list.Restore();
            foreach (ConfirmStudentRow row in _list.Rows)
                if (_entryFlags.ContainsKey(row.Group.StudentId))
                    foreach (RankSlot slot in row.Group.Working) slot.Participate = _entryFlags[row.Group.StudentId][slot.RankType];
            _binding = true;
            MotherGroupConfirmGrid.RefreshParticipation(dgvStudents, _list);
            _binding = false;
            MotherGroupConfirmGrid.RefreshSummary(lblSummary, dgvPopulation, dgvDetail, _list);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (_list == null || _list.Rows.Count == 0)
            {
                MessageBox.Show("沒有可匯出的確認清單。");
                return;
            }
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "匯出完整母群清單";
            dialog.FileName = "定期評量排名母群.xlsx";
            dialog.Filter = "Excel (*.xlsx)|*.xlsx|所有檔案 (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            MotherGroupConfirmList export = _list.CopyWorking();
            string fileName = dialog.FileName;
            btnExport.Enabled = false;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate { ExportCopy(export).Save(fileName); };
            worker.RunWorkerCompleted += delegate(object source, RunWorkerCompletedEventArgs completed)
            {
                if (IsDisposed) return;
                btnExport.Enabled = true;
                if (completed.Error != null) { MessageBox.Show("匯出失敗：" + completed.Error.Message); return; }
                if (MessageBox.Show("檔案儲存完成。缺少歷史狀態者以目前主檔狀態匯出。是否開啟？", "是否開啟", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                try { System.Diagnostics.Process.Start(fileName); }
                catch (Exception ex) { MessageBox.Show("檔案開啟失敗：" + ex.Message); }
            };
            worker.RunWorkerAsync();
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            ReturnToHost();
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            if (_host == null)
            {
                MessageBox.Show("沒有條件畫面宿主，不能計算。");
                return;
            }
            if (_list == null || !RegularRankCalculationEntry.CanOfferCalculate(_list.Rows.Count))
            {
                MessageBox.Show("沒有確認清單，不能計算。");
                return;
            }
            if (!_guard.TryEnter())
            {
                MessageBox.Show("計算進行中，不能再次送出。");
                return;
            }
            try { if (!_host.TryCalculateFromConfirm(this)) FinishCalculation(); }
            catch (Exception ex) { FinishCalculation(); MessageBox.Show(ex.Message); }
        }

        internal void BeginCalculation()
        {
            dgvStudents.EndEdit();
            dgvStudents.Enabled = false;
            txtSearch.Enabled = false;
            btnRestore.Enabled = false;
            btnExport.Enabled = false;
            btnPrevious.Enabled = false;
            btnCalculate.Enabled = false;
            btnExcel.Enabled = false;
            btnAddStudent.Enabled = false;
            txtAddStudent.Enabled = false;
        }

        internal void FinishCalculation()
        {
            _guard.FailAndRestore();
            dgvStudents.Enabled = true;
            txtSearch.Enabled = true;
            btnRestore.Enabled = true;
            btnExport.Enabled = true;
            btnPrevious.Enabled = true;
            btnCalculate.Enabled = RegularRankCalculationEntry.CanOfferCalculate(_list == null ? 0 : _list.Rows.Count);
            btnExcel.Enabled = true;
            btnAddStudent.Enabled = _host != null && _historySource;
            txtAddStudent.Enabled = btnAddStudent.Enabled;
        }

        private void btnAddStudent_Click(object sender, EventArgs e)
        {
            if (_host == null || !EditAllowed() || _list == null) return;
            if (string.IsNullOrWhiteSpace(txtAddStudent.Text)) { MessageBox.Show("請輸入學號。"); return; }
            dgvStudents.EndEdit();
            _host.AddHistoryStudent(this, txtAddStudent.Text);
        }

        internal void SetAdding(bool adding)
        {
            _adding = adding;
            btnAddStudent.Enabled = !adding && _historySource;
            txtAddStudent.Enabled = btnAddStudent.Enabled;
            btnCalculate.Enabled = !adding && RegularRankCalculationEntry.CanOfferCalculate(_list == null ? 0 : _list.Rows.Count);
            btnExcel.Enabled = !adding;
            btnPrevious.Enabled = !adding;
            btnRestore.Enabled = !adding;
            btnExport.Enabled = !adding;
        }

        private void btnExcel_Click(object sender, EventArgs e)
        {
            if (_host != null && EditAllowed()) _host.ReturnForExcelImport();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_guard.Running || _adding)
            {
                e.Cancel = true;
                return;
            }
            if (!_returning && e.CloseReason == CloseReason.UserClosing && _host != null)
            {
                e.Cancel = true;
                ReturnToHost();
                return;
            }
            base.OnFormClosing(e);
        }

        private void ReturnToHost()
        {
            if (_host == null)
            {
                Hide();
                return;
            }
            _host.ReturnFromMotherGroupConfirm();
        }

        internal void HideForHost()
        {
            _returning = true;
            try
            {
                Hide();
            }
            finally
            {
                _returning = false;
            }
        }
    }
}
