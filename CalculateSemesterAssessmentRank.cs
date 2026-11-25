using FISCA.Data;
using FISCA.Presentation;
using FISCA.Presentation.Controls;
using K12.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SHEvaluation.Rank.MotherGroup;

namespace SHEvaluation.Rank
{
    public partial class CalculateSemesterAssessmentRank : BaseForm
    {
        string _DefaultSchoolYear = "";
        string _DefaultSemester = "";
        List<TagConfigRecord> _TagList = new List<TagConfigRecord>();
        List<StudentRecord> _StudentList = new List<StudentRecord>();
        List<int> _GradeYearList = new List<int>();
        List<string> _GradeSelectList = new List<string>();
        XmlElement _ConfigElement = null;
        CalculationPeriod _calculationPeriod;
        MotherGroupConfirmList _confirmList;
        bool _bindingConfirmGrid;
        bool _confirmEventsReady;
        bool _bindingHistoryBatch;
        bool _historySourceEventsReady;
        bool _applyingCalculationPeriod;
        readonly EditableCalculationPeriodMemory _editableCalculation = new EditableCalculationPeriodMemory();
        bool _screenLoadQueued;
        bool _screenLoadSettled;
        bool _screenLoadFailed;
        bool _nextStepRunning;
        int _historyLoadGeneration;
        volatile bool _settingsPageClosed;
        bool _historyInputsLocked;
        SemesterRankConfirmRequest _frozenHistoryRequest;
        BackgroundWorker _historyLoadWorker;
        static Func<DataTable> _historyBatchQuery;
        static Action<string> _historyBatchFailureDialog;

        internal static void SetHistoryBatchQuery(Func<DataTable> query)
        {
            _historyBatchQuery = query;
        }

        internal static void SetHistoryBatchFailureDialog(Action<string> dialog)
        {
            _historyBatchFailureDialog = dialog;
        }
        bool _rankCalculationRunning;
        bool _capturingCalculationEdit;
        bool _calculationUiDisabled;
        CalculationUiState _calculationUiState;
        ToolTip _screenLoadErrorTip;
        SemesterRankMotherGroupConfirmForm plStudentView;
        DevComponents.DotNetBar.LabelX lbSchoolYear;
        DevComponents.DotNetBar.LabelX lbSemester;
        DevComponents.DotNetBar.ButtonX btnExport;
        DevComponents.DotNetBar.ButtonX btnCacluate;
        DevComponents.DotNetBar.ButtonX btnPrevious;
        DevComponents.DotNetBar.Controls.DataGridViewX dgvStudentList;
        PictureBox pbLoading;
        Label _lblMotherGroupSummary;
        TextBox _txtMotherGroupSearch;
        DataGridView _dgvRankPopulation;
        DataGridView _dgvGroupCounts;
        Button _btnRestoreParticipation;
        Label _lblAddStudentCaption;
        TextBox _txtAddStudentNumber;
        Button _btnAddHistoryStudent;
        const string ScreenLoadReadyText = "說明：班、科、年排名會自動計算。分項成績擇優項目請參考成績計算規則。";
        const string NoHistoryBatchText = SemesterRankSourceChrome.NoSemesterBatchText;
        const string HistoryBatchQueryFailureText = "歷史批次清單讀取失敗。";
        public CalculateSemesterAssessmentRank()
        {
            InitializeComponent();
            CreateConfirmForm();
            btnExport.Click += delegate { ExportMotherGroup(); };
            _nudCalcSchoolYear.Visible = false;
            _cboCalcSemester.Visible = false;
            _cboHistorySemester.Visible = false;
            plStudentView.Visible = false;
            btnNext.Enabled = false;
            labelX6.Text = "正在初始化畫面…";
            ApplySourceVisibility();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_screenLoadQueued || _screenLoadSettled || IsDisposed)
                return;
            _screenLoadQueued = true;
            PaintLoadStage();
            BeginInvoke(new MethodInvoker(LoadScreenInitialization));
        }

        private void LoadScreenInitialization()
        {
            if (_screenLoadSettled || IsDisposed)
                return;
            try
            {
                EnsureFigureControls();
            }
            catch (Exception ex)
            {
                FailScreenLoad("畫面初始化", ex);
                return;
            }
            if (_screenLoadSettled || IsDisposed)
                return;
            labelX6.Text = "正在載入學校設定…";
            PaintLoadStage();
            BeginInvoke(new MethodInvoker(LoadSchoolSettings));
        }

        private void LoadSchoolSettings()
        {
            if (_screenLoadSettled || IsDisposed)
                return;
            try
            {
                _DefaultSchoolYear = K12.Data.School.DefaultSchoolYear;
                _DefaultSemester = K12.Data.School.DefaultSemester;

                string configString = K12.Data.School.Configuration["固定排名_學期成績排名計算"]["設定檔"];
                if (configString != "")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(configString);
                    _ConfigElement = doc.DocumentElement;

                    chbMakeUp.Checked = chbRetake.Checked = chbSYAdj.Checked = chbHandAdj.Checked = false;
                    foreach (XmlElement item in _ConfigElement.SelectNodes("擇優採計成績"))
                    {
                        if (item.InnerText == "補考成績")
                            chbMakeUp.Checked = true;

                        if (item.InnerText == "重修成績")
                            chbRetake.Checked = true;

                        if (item.InnerText == "學年調整成績")
                            chbSYAdj.Checked = true;

                        if (item.InnerText == "手動調整成績")
                            chbHandAdj.Checked = true;
                    }
                }

                //因為目前只提供計算預設學年度學期的排名，所以這邊先註解起來
                //cboSchoolYear.Items.Add(_DefaultSchoolYear);
                //cboSchoolYear.SelectedIndex = 0;
                //cboSemester.Items.Add(_DefaultSemester);
                //cboSemester.SelectedIndex = 0;
                //lbCalcSchoolYear.Text = _DefaultSchoolYear;
                //lbCalcSemester.Text = _DefaultSemester;
                _calculationPeriod = CalculationPeriod.CreateDefault(_DefaultSchoolYear, _DefaultSemester);
                CalculationPeriodBinder.Apply(_nudCalcSchoolYear, _cboCalcSemester, _cboMotherGroupSource, _nudHistorySchoolYear, _cboHistorySemester, _calculationPeriod);
                EnsureHistorySourceEvents();
                ApplyCalculationPeriodEditors();
                ApplySourceVisibility();
            }
            catch (Exception ex)
            {
                FailScreenLoad("學校設定", ex);
                return;
            }
            if (_screenLoadSettled || IsDisposed)
                return;
            labelX6.Text = "正在載入學生類別…";
            PaintLoadStage();
            BeginInvoke(new MethodInvoker(LoadStudentCategories));
        }

        private void LoadStudentCategories()
        {
            if (_screenLoadSettled || IsDisposed)
                return;
            try
            {
                _TagList = K12.Data.TagConfig.SelectByCategory(TagCategory.Student);
                cboStudentFilter.Items.Add("");
                cboStudentTag1.Items.Add("");
                cboStudentTag2.Items.Add("");
                foreach (string tagName in _TagList.Select(x => x.Prefix).Distinct().ToList())
                {
                    if (!string.IsNullOrEmpty(tagName))
                    {
                        cboStudentFilter.Items.Add("[" + tagName + "]");
                        cboStudentTag1.Items.Add("[" + tagName + "]");
                        cboStudentTag2.Items.Add("[" + tagName + "]");
                    }
                }
                foreach (string tagName in _TagList.Where(x => string.IsNullOrEmpty(x.Prefix)).Select(x => x.Name).ToList())
                {
                    cboStudentFilter.Items.Add(tagName);
                    cboStudentTag1.Items.Add(tagName);
                    cboStudentTag2.Items.Add(tagName);
                }
            }
            catch (Exception ex)
            {
                FailScreenLoad("學生類別", ex);
                return;
            }
            if (_screenLoadSettled || IsDisposed)
                return;
            labelX6.Text = "正在載入學生清單…";
            PaintLoadStage();
            BeginInvoke(new MethodInvoker(LoadStudentList));
        }

        private void LoadStudentList()
        {
            if (_screenLoadSettled || IsDisposed)
                return;
            try
            {
                _StudentList = K12.Data.Student.SelectAll().Where(x => (x.Status == StudentRecord.StudentStatus.一般)
                                                                && !string.IsNullOrEmpty(x.RefClassID)
                                                                && x.Class.GradeYear != null).ToList();
            }
            catch (Exception ex)
            {
                FailScreenLoad("學生清單", ex);
                return;
            }
            if (_screenLoadSettled || IsDisposed)
                return;
            labelX6.Text = "正在載入年級清單…";
            PaintLoadStage();
            BeginInvoke(new MethodInvoker(LoadGradeYears));
        }

        private void LoadGradeYears()
        {
            if (_screenLoadSettled || IsDisposed)
                return;
            try
            {
                foreach (int gradeYear in _StudentList.Select(x => x.Class.GradeYear).Distinct().OrderBy(x => x).ToList())
                {
                    ListViewItem listViewItem = new ListViewItem();
                    listViewItem.Text = "" + gradeYear + "年級";
                    listViewItem.Checked = true;
                    listGradeYear.Items.Add(listViewItem);
                }
                FinishScreenLoad();
            }
            catch (Exception ex)
            {
                FailScreenLoad("年級清單", ex);
            }
        }

        private void CalculateSemesterAssessmentRank_Load(object sender, EventArgs e)
        {
        }

        private void CreateConfirmForm()
        {
            plStudentView = new SemesterRankMotherGroupConfirmForm(this);
            lbSchoolYear = plStudentView.SchoolYearLabel;
            lbSemester = plStudentView.SemesterLabel;
            btnExport = plStudentView.ExportButton;
            btnCacluate = plStudentView.CalculateButton;
            btnPrevious = plStudentView.PreviousButton;
            dgvStudentList = plStudentView.StudentGrid;
            pbLoading = plStudentView.LoadingImage;
            _lblMotherGroupSummary = plStudentView.SummaryLabel;
            _txtMotherGroupSearch = plStudentView.SearchBox;
            _dgvRankPopulation = plStudentView.PopulationGrid;
            _dgvGroupCounts = plStudentView.DetailGrid;
            _btnRestoreParticipation = plStudentView.RestoreButton;
            _lblAddStudentCaption = plStudentView.AddStudentCaption;
            _txtAddStudentNumber = plStudentView.AddStudentNumber;
            _btnAddHistoryStudent = plStudentView.AddStudentButton;
        }

        private SemesterRankConfirmRequest CreateConfirmRequest()
        {
            SemesterRankConfirmRequest request = new SemesterRankConfirmRequest();
            request.SchoolYear = _calculationPeriod == null ? "" : _calculationPeriod.SchoolYear;
            request.Semester = _calculationPeriod == null ? "" : _calculationPeriod.Semester;
            request.SourceName = CalculationPeriod.CurrentSourceName;
            if (_cboMotherGroupSource != null && _cboMotherGroupSource.SelectedItem != null)
                request.SourceName = _cboMotherGroupSource.SelectedItem.ToString();
            request.HistoryBatchId = _calculationPeriod == null ? "" : (_calculationPeriod.HistoryBatchId ?? "");
            request.HistorySchoolYear = _nudHistorySchoolYear == null ? "" : decimal.ToInt32(_nudHistorySchoolYear.Value).ToString();
            request.HistorySemester = _cboHistorySemester == null || _cboHistorySemester.SelectedItem == null ? "" : _cboHistorySemester.SelectedItem.ToString();
            request.Students = _StudentList;
            request.Tags = _TagList;
            request.GradeYears = _GradeYearList == null ? new List<int>() : new List<int>(_GradeYearList);
            request.GradeSelections = _GradeSelectList == null ? new List<string>() : new List<string>(_GradeSelectList);
            request.ExcludedCategory = cboStudentFilter.Text.Trim('[', ']');
            request.RankTag1 = cboStudentTag1.Text.Trim('[', ']');
            request.RankTag2 = cboStudentTag2.Text.Trim('[', ']');
            request.RankTypes = RankKinds.All;
            return request;
        }

        private void PublishConfirmRequest()
        {
            PublishConfirmRequest(null);
        }

        private void PublishConfirmRequest(SemesterRankConfirmRequest request)
        {
            if (plStudentView == null)
                return;
            plStudentView.Request = request ?? CreateConfirmRequest();
            ShowConfirmForm();
        }

        private SemesterRankConfirmRequest FreezeConfirmRequest()
        {
            SemesterRankConfirmRequest request = CreateConfirmRequest();
            request.Students = request.Students == null ? new List<StudentRecord>() : new List<StudentRecord>(request.Students);
            request.Tags = request.Tags == null ? new List<TagConfigRecord>() : new List<TagConfigRecord>(request.Tags);
            request.GradeYears = request.GradeYears == null ? new List<int>() : new List<int>(request.GradeYears);
            request.GradeSelections = request.GradeSelections == null ? new List<string>() : new List<string>(request.GradeSelections);
            request.RankTypes = request.RankTypes == null ? null : new List<string>(request.RankTypes);
            return request;
        }

        private void ShowConfirmForm()
        {
            if (plStudentView == null)
                return;
            if (plStudentView.Owner != this)
                plStudentView.Owner = this;
            if (!plStudentView.Visible)
                plStudentView.Show();
            Enabled = false;
            plStudentView.BringToFront();
            plStudentView.Activate();
        }

        private void ShowConditionForm()
        {
            Enabled = true;
            if (!Visible)
                Show();
            Activate();
        }

        internal bool RankCalculationRunning
        {
            get { return _rankCalculationRunning; }
        }

        private void PaintLoadStage()
        {
            if (IsDisposed || labelX6 == null || !IsHandleCreated)
                return;
            labelX6.Refresh();
        }

        private void ShowNextStage(string text)
        {
            if (IsDisposed || labelX6 == null)
                return;
            labelX6.Text = text;
            PaintLoadStage();
        }

        private void ReleaseNextStep(bool enableNext)
        {
            _nextStepRunning = false;
            if (IsDisposed || labelX6 == null)
                return;
            labelX6.Text = ScreenLoadReadyText;
            if (_screenLoadErrorTip != null)
                _screenLoadErrorTip.SetToolTip(labelX6, "");
            if (enableNext && _screenLoadSettled)
                btnNext.Enabled = true;
            if (enableNext)
                ApplyHistoryNextGate();
        }

        private void FinishScreenLoad()
        {
            if (_screenLoadSettled || IsDisposed)
                return;
            _screenLoadSettled = true;
            labelX6.Text = ScreenLoadReadyText;
            if (_screenLoadErrorTip != null)
                _screenLoadErrorTip.SetToolTip(labelX6, "");
            _nudCalcSchoolYear.Visible = true;
            _cboCalcSemester.Visible = true;
            btnNext.Enabled = true;
            ApplyHistoryNextGate();
        }

        private void FailScreenLoad(string stage, Exception ex)
        {
            if (_screenLoadSettled)
                return;
            _screenLoadSettled = true;
            _screenLoadFailed = true;
            _TagList = new List<TagConfigRecord>();
            _StudentList = new List<StudentRecord>();
            _GradeYearList = new List<int>();
            if (IsDisposed)
                return;
            cboStudentFilter.Items.Clear();
            cboStudentTag1.Items.Clear();
            cboStudentTag2.Items.Clear();
            cboStudentFilter.Text = "";
            cboStudentTag1.Text = "";
            cboStudentTag2.Text = "";
            cboStudentTag2.Enabled = false;
            listGradeYear.Items.Clear();
            _nudCalcSchoolYear.Visible = false;
            _cboCalcSemester.Visible = false;
            btnNext.Enabled = false;
            string reason = ex == null || string.IsNullOrEmpty(ex.Message) ? "未知錯誤" : ex.Message;
            string message = "載入" + stage + "失敗：" + reason;
            labelX6.Text = message;
            if (_screenLoadErrorTip == null)
            {
                _screenLoadErrorTip = new ToolTip();
                _screenLoadErrorTip.AutoPopDelay = 30000;
                _screenLoadErrorTip.ShowAlways = true;
            }
            _screenLoadErrorTip.SetToolTip(labelX6, message);
        }

        private void cboStudentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentFilter.Text == cboStudentTag1.Text || cboStudentFilter.Text == cboStudentTag2.Text)
            {
                cboStudentFilter.Text = "";
            }
        }

        private void cboStudentTag1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag1.Text == cboStudentFilter.Text || cboStudentTag1.Text == cboStudentTag2.Text)
            {
                cboStudentTag1.Text = "";
            }
            if (cboStudentTag1.Text != "")
            {
                cboStudentTag2.Enabled = true;
            }
            else
            {
                cboStudentTag2.Text = "";
                cboStudentTag2.Enabled = false;
            }
        }

        private void cboStudentTag2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag2.Text == cboStudentFilter.Text || cboStudentTag2.Text == cboStudentTag1.Text)
            {
                cboStudentTag2.Text = "";
            }
        }

        private void EnsureHistorySourceEvents()
        {
            if (_historySourceEventsReady || _cboMotherGroupSource == null || _cboHistoryBatch == null)
                return;
            _historySourceEventsReady = true;
            _cboMotherGroupSource.SelectedIndexChanged += delegate
            {
                if (_historyInputsLocked)
                    return;
                ApplyCalculationPeriodEditors();
                ApplySourceVisibility();
                if (!IsHistorySourceSelected())
                {
                    SetHistoryFiltersEnabled(false);
                    if (_cboHistoryBatch != null)
                    {
                        _bindingHistoryBatch = true;
                        _cboHistoryBatch.SelectedIndex = -1;
                        _cboHistoryBatch.Items.Clear();
                        _bindingHistoryBatch = false;
                    }
                    if (_calculationPeriod != null)
                        _calculationPeriod.ClearHistoryBatch();
                    ApplyHistoryNextGate();
                    return;
                }
                SetHistoryFiltersEnabled(true);
                try
                {
                    LoadHistoryBatches(true);
                }
                catch (Exception ex)
                {
                    ClearFailedHistorySelection();
                    ReportHistoryBatchQueryFailure(ex);
                }
            };
            _nudHistorySchoolYear.ValueChanged += delegate { OnHistoryPeriodChanged(); };
            _cboHistorySemester.SelectedIndexChanged += delegate { OnHistoryPeriodChanged(); };
            _nudCalcSchoolYear.ValueChanged += delegate
            {
                if (_historyInputsLocked)
                    return;
                OnEditableCalculationChanged();
            };
            _cboCalcSemester.SelectedIndexChanged += delegate
            {
                if (_historyInputsLocked)
                    return;
                OnEditableCalculationChanged();
            };
            _cboHistoryBatch.SelectedIndexChanged += delegate
            {
                if (_historyInputsLocked || _bindingHistoryBatch || _calculationPeriod == null || _cboHistoryBatch == null)
                    return;
                HistoryRankBatch selected = _cboHistoryBatch.SelectedItem as HistoryRankBatch;
                if (selected == null)
                {
                    _calculationPeriod.ClearHistoryBatchSelection();
                    ApplyHistoryNextGate();
                    return;
                }
                _calculationPeriod.SelectHistoryBatch(selected.Id);
                if (labelX6 != null && IsHistoryMotherGroupLoadFailure(labelX6.Text))
                    labelX6.Text = ScreenLoadReadyText;
                ApplyHistoryNextGate();
            };
        }

        private string SelectedSourceName()
        {
            if (_cboMotherGroupSource == null || _cboMotherGroupSource.SelectedItem == null)
                return CalculationPeriod.CurrentSourceName;
            return _cboMotherGroupSource.SelectedItem.ToString();
        }

        private void ApplySourceVisibility()
        {
            string source = SelectedSourceName();
            bool history = source == CalculationPeriod.HistorySourceName;
            bool excel = source == CalculationPeriod.ExcelSourceName;
            bool current = source == CalculationPeriod.CurrentSourceName;
            SetControlVisible(history, _lblHistoryYearCaption, _nudHistorySchoolYear, _lblHistorySemesterCaption, _cboHistorySemester, _lblHistoryBatchCaption, _cboHistoryBatch);
            SetControlVisible(excel, _btnDownloadExcelTemplate, _btnExcelFillGuide);
            SetControlVisible(current, labelX4, cboStudentTag1, labelX5, cboStudentTag2);
            if (_lblGradeYearHint != null)
            {
                if (history)
                    _lblGradeYearHint.Text = SemesterRankSourceChrome.HistoryGradeHint;
                else if (excel)
                    _lblGradeYearHint.Text = SemesterRankSourceChrome.ExcelGradeHint;
                else
                    _lblGradeYearHint.Text = SemesterRankSourceChrome.CurrentGradeHint;
            }
            if (btnNext != null && !_nextStepRunning)
                btnNext.Text = excel ? SemesterRankSourceChrome.ExcelNextText : SemesterRankSourceChrome.NextText;
            LayoutConditionPage();
            ApplyHistoryNextGate();
        }

        private static void SetControlVisible(bool visible, params Control[] controls)
        {
            if (controls == null)
                return;
            foreach (Control control in controls)
            {
                if (control != null)
                    control.Visible = visible;
            }
        }

        private void LayoutConditionPage()
        {
            if (gpRankPeople == null || btnNext == null)
                return;
            int top = _lblCalcPeriodHint != null ? _lblCalcPeriodHint.Bottom + 8 : gpRankPeople.Top;
            if (_cboHistoryBatch != null && _cboHistoryBatch.Visible)
                top = _cboHistoryBatch.Bottom + 8;
            else if (_btnDownloadExcelTemplate != null && _btnDownloadExcelTemplate.Visible)
                top = _btnDownloadExcelTemplate.Bottom + 8;
            int height = btnNext.Top - 8 - top;
            if (height < 160)
                height = 160;
            gpRankPeople.SetBounds(gpRankPeople.Left, top, gpRankPeople.Width, height);
        }

        private bool CanUpdateSourceGate()
        {
            return _screenLoadSettled && !_screenLoadFailed && !_nextStepRunning && btnNext != null;
        }

        private void ApplyHistoryNextGate()
        {
            if (!CanUpdateSourceGate())
                return;
            if (!IsHistorySourceSelected())
            {
                if (labelX6 != null && (labelX6.Text == NoHistoryBatchText || IsHistoryBatchQueryFailure(labelX6.Text)))
                    labelX6.Text = ScreenLoadReadyText;
                btnNext.Enabled = true;
                return;
            }
            bool hasBatch = _cboHistoryBatch != null && _cboHistoryBatch.Items.Count > 0;
            bool selected = _cboHistoryBatch != null && _cboHistoryBatch.SelectedItem is HistoryRankBatch;
            btnNext.Enabled = hasBatch && selected;
            if (labelX6 == null)
                return;
            if (!hasBatch)
            {
                if (!IsHistoryBatchQueryFailure(labelX6.Text))
                    labelX6.Text = NoHistoryBatchText;
            }
            else if (labelX6.Text == NoHistoryBatchText || IsHistoryBatchQueryFailure(labelX6.Text))
                labelX6.Text = ScreenLoadReadyText;
        }

        private void OnEditableCalculationChanged()
        {
            if (_applyingCalculationPeriod || SelectedSourceName() == CalculationPeriod.CurrentSourceName)
                return;
            RememberEditableCalculation();
        }

        private void RememberEditableCalculation()
        {
            if (_nudCalcSchoolYear == null)
                return;
            string semester = _cboCalcSemester == null || _cboCalcSemester.SelectedItem == null
                ? "1"
                : _cboCalcSemester.SelectedItem.ToString();
            _editableCalculation.Remember(decimal.ToInt32(_nudCalcSchoolYear.Value).ToString(), semester);
        }

        private void ShowEditableCalculation()
        {
            if (!_editableCalculation.HasValue || _nudCalcSchoolYear == null)
                return;
            _applyingCalculationPeriod = true;
            try
            {
                int yearNumber;
                if (int.TryParse(_editableCalculation.SchoolYear, out yearNumber)
                    && yearNumber >= _nudCalcSchoolYear.Minimum
                    && yearNumber <= _nudCalcSchoolYear.Maximum)
                    _nudCalcSchoolYear.Value = yearNumber;
                if (_cboCalcSemester != null && _cboCalcSemester.Items.Contains(_editableCalculation.Semester))
                    _cboCalcSemester.SelectedItem = _editableCalculation.Semester;
                if (_calculationPeriod != null)
                    _calculationPeriod.SetCalculationPeriod(_editableCalculation.SchoolYear, _editableCalculation.Semester);
            }
            finally
            {
                _applyingCalculationPeriod = false;
            }
        }

        private void btnDownloadExcelTemplate_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "下載匯入範本";
            dialog.Filter = "Excel (*.xlsx)|*.xlsx";
            dialog.FileName = "學期排名母群匯入範本.xlsx";
            dialog.OverwritePrompt = true;
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                MotherGroupExcelExchange.SaveWorkbook(dialog.FileName, MotherGroupExcelExchange.CreateBlankTemplate());
            }
            catch (Exception ex)
            {
                MessageBox.Show("匯入範本儲存失敗。\n" + ex.Message);
            }
        }

        private void btnExcelFillGuide_Click(object sender, EventArgs e)
        {
            MessageBox.Show(MotherGroupExcelExchange.FillGuideText, "填寫說明");
        }

        private void SetHistoryFiltersEnabled(bool enabled)
        {
            if (_nudHistorySchoolYear != null)
                _nudHistorySchoolYear.Enabled = enabled;
            if (_cboHistorySemester != null)
                _cboHistorySemester.Enabled = enabled;
            if (_cboHistoryBatch != null)
                _cboHistoryBatch.Enabled = enabled;
        }

        private void OnHistoryPeriodChanged()
        {
            if (_historyInputsLocked || _bindingHistoryBatch || !_historySourceEventsReady || !IsHistorySourceSelected())
                return;
            SyncHistoryPeriodFromControls();
            if (_calculationPeriod != null)
                _calculationPeriod.ClearHistoryBatchSelection();
            try
            {
                LoadHistoryBatches(false);
            }
            catch (Exception ex)
            {
                ClearFailedHistorySelection();
                ReportHistoryBatchQueryFailure(ex);
            }
        }

        private void SyncHistoryPeriodFromControls()
        {
            if (_calculationPeriod == null || _nudHistorySchoolYear == null)
                return;
            string semester = _cboHistorySemester == null || _cboHistorySemester.SelectedItem == null
                ? ""
                : _cboHistorySemester.SelectedItem.ToString();
            _calculationPeriod.SetHistoryPeriod(decimal.ToInt32(_nudHistorySchoolYear.Value).ToString(), semester);
        }

        private bool IsCurrentSourceSelected()
        {
            return _cboMotherGroupSource != null
                && _cboMotherGroupSource.SelectedItem != null
                && _cboMotherGroupSource.SelectedItem.ToString() == CalculationPeriod.CurrentSourceName;
        }

        private void ApplyCalculationPeriodEditors()
        {
            if (_nudCalcSchoolYear == null || _cboCalcSemester == null)
                return;
            if (SelectedSourceName() == CalculationPeriod.CurrentSourceName)
            {
                if (_nudCalcSchoolYear.Enabled)
                    RememberEditableCalculation();
                _applyingCalculationPeriod = true;
                try
                {
                    CalculationPeriodBinder.RestoreSystemPeriod(_nudCalcSchoolYear, _cboCalcSemester, _calculationPeriod, _DefaultSchoolYear, _DefaultSemester);
                }
                finally
                {
                    _applyingCalculationPeriod = false;
                }
                _nudCalcSchoolYear.Enabled = false;
                _cboCalcSemester.Enabled = false;
                return;
            }
            if (IsExcelSourceSelected() || IsHistorySourceSelected())
            {
                _nudCalcSchoolYear.Enabled = true;
                _cboCalcSemester.Enabled = true;
                ShowEditableCalculation();
            }
        }

        private bool IsExcelSourceSelected()
        {
            return _cboMotherGroupSource != null
                && _cboMotherGroupSource.SelectedItem != null
                && _cboMotherGroupSource.SelectedItem.ToString() == CalculationPeriod.ExcelSourceName;
        }

        private void BeginExcelImport()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "匯入母群";
            dialog.Filter = "Excel (*.xlsx)|*.xlsx|所有檔案 (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                ReleaseNextStep(true);
                return;
            }

            string fileName = dialog.FileName;
            string excludedCategory = cboStudentFilter.Text.Trim('[', ']');
            List<TagDefinition> tagDefinitions = new List<TagDefinition>();
            foreach (TagConfigRecord tag in _TagList)
            {
                if (tag == null)
                    continue;
                tagDefinitions.Add(new TagDefinition { Id = tag.ID, Prefix = tag.Prefix, Name = tag.Name });
            }

            BackgroundWorker bkw = new BackgroundWorker();
            Exception bkwException = null;
            string bkwFailureTitle = "Excel 母群讀取失敗。";
            MotherGroupExcelValidation validation = null;
            List<StudentMotherGroup> groups = null;
            bkw.DoWork += delegate
            {
                try
                {
                    Aspose.Cells.Workbook workbook = MotherGroupExcelExchange.Open(fileName);
                    List<MotherGroupExcelIdentity> identities = MotherGroupExcelImport.IdentityKeys(workbook);
                    DataTable table = null;
                    if (identities.Count > 0)
                    {
                        QueryHelper queryHelper = new QueryHelper();
                        table = queryHelper.Select(MotherGroupExcelImport.SelectSql(identities));
                    }
                    validation = MotherGroupExcelImport.Validate(workbook, table);
                    if (validation == null || !validation.IsValid)
                        return;
                    List<StudentTagLink> links = new List<StudentTagLink>();
                    bkwFailureTitle = "學生類別讀取失敗。";
                    foreach (StudentTagRecord tag in StudentTag.SelectAll())
                    {
                        if (tag == null)
                            continue;
                        links.Add(new StudentTagLink
                        {
                            StudentId = tag.RefStudentID,
                            TagId = tag.RefTagID,
                            Name = tag.Name
                        });
                    }
                    bkwFailureTitle = "Excel 母群讀取失敗。";
                    groups = MotherGroupExcelApply.CreateGroups(validation, excludedCategory, tagDefinitions, links);
                }
                catch (Exception ex)
                {
                    bkwException = ex;
                }
            };
            bkw.RunWorkerCompleted += delegate
            {
                if (IsDisposed)
                {
                    _nextStepRunning = false;
                    return;
                }
                if (bkwException != null)
                {
                    ReleaseNextStep(true);
                    MessageBox.Show(bkwFailureTitle + "\n" + bkwException.Message);
                    return;
                }
                if (validation == null || !validation.IsValid)
                {
                    ReleaseNextStep(true);
                    MessageBox.Show(MotherGroupExcelApply.IssueText(validation));
                    return;
                }

                string warnings = MotherGroupExcelApply.WarningText(validation);
                if (warnings.Length > 0)
                    MessageBox.Show(warnings);
                Dictionary<string, StudentRecord> studentsById = new Dictionary<string, StudentRecord>();
                foreach (StudentRecord student in _StudentList)
                {
                    if (student != null && !studentsById.ContainsKey(student.ID))
                        studentsById.Add(student.ID, student);
                }
                try
                {
                    plStudentView.Visible = true;
                    lbSchoolYear.Text = _calculationPeriod.SchoolYear;
                    lbSemester.Text = _calculationPeriod.Semester;
                    PublishConfirmRequest();
                    EnsureConfirmEvents();
                    ShowConfirmList(groups, studentsById);
                }
                catch (Exception ex)
                {
                    ReleaseNextStep(true);
                    MessageBox.Show("開啟母群確認畫面失敗。\n" + ex.Message);
                }
            };
            bkw.RunWorkerAsync();
        }

        private bool IsHistorySourceSelected()
        {
            return _cboMotherGroupSource != null
                && _cboMotherGroupSource.SelectedItem != null
                && _cboMotherGroupSource.SelectedItem.ToString() == CalculationPeriod.HistorySourceName;
        }

        private void LoadHistoryBatches(bool keepSelection)
        {
            SyncHistoryPeriodFromControls();
            string previousId = keepSelection && _calculationPeriod != null ? _calculationPeriod.HistoryBatchId : "";
            // 未設定時走資料庫。表單事件測試才代入批次表，避免連到學校資料庫。
            DataTable table = _historyBatchQuery != null
                ? _historyBatchQuery()
                : new QueryHelper().Select(SemesterHistoryBatchCatalog.SelectSql);
            string filterYear = _nudHistorySchoolYear == null ? "" : decimal.ToInt32(_nudHistorySchoolYear.Value).ToString();
            string filterSemester = _cboHistorySemester == null || _cboHistorySemester.SelectedItem == null ? "" : _cboHistorySemester.SelectedItem.ToString();
            List<HistoryRankBatch> batches = SemesterHistoryBatchCatalog.FilterPeriod(SemesterHistoryBatchCatalog.Read(table), filterYear, filterSemester);
            SemesterHistoryBatchCatalog.SortByIdDescending(batches);
            HistoryRankBatch reselect = null;
            _cboHistoryBatch.Sorted = false;
            _bindingHistoryBatch = true;
            try
            {
                _cboHistoryBatch.Items.Clear();
                foreach (HistoryRankBatch batch in batches)
                {
                    _cboHistoryBatch.Items.Add(batch);
                    if (previousId.Length > 0 && batch.Id == previousId)
                        reselect = batch;
                }
            }
            finally
            {
                _bindingHistoryBatch = false;
            }
            _cboHistoryBatch.SelectedItem = reselect;
            if (reselect == null && _calculationPeriod != null)
                _calculationPeriod.ClearHistoryBatchSelection();
            if (CanUpdateSourceGate() && labelX6 != null)
            {
                if (batches.Count == 0)
                    labelX6.Text = NoHistoryBatchText;
                else if (labelX6.Text == NoHistoryBatchText || IsHistoryBatchQueryFailure(labelX6.Text))
                    labelX6.Text = ScreenLoadReadyText;
            }
            ApplyHistoryNextGate();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (!_screenLoadSettled || !btnNext.Enabled)
                return;
            if (_nextStepRunning)
                return;
            _nextStepRunning = true;
            btnNext.Enabled = false;
            try
            {
                ApplyCalculationPeriodEditors();
                CalculationPeriodBinder.ReadCalculation(_nudCalcSchoolYear, _cboCalcSemester, _calculationPeriod);
                CaptureCalculationScope();
            }
            catch (Exception ex)
            {
                ReleaseNextStep(true);
                MessageBox.Show("下一步處理失敗。\n" + ex.Message);
                return;
            }
            if (IsHistorySourceSelected())
            {
                ShowNextStage("正在讀取歷史批次及母群資料...");
                BeginHistorySnapshotRestore();
                return;
            }
            if (IsExcelSourceSelected())
            {
                ShowNextStage("正在匯入 Excel 母群資料...");
                BeginExcelImport();
                return;
            }
            ShowNextStage("正在載入目前學生母群資料...");

            try
            {
            lbSchoolYear.Text = _calculationPeriod.SchoolYear;
            lbSemester.Text = _calculationPeriod.Semester;
            PublishConfirmRequest();

            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            string studentTag1 = cboStudentTag1.Text.Trim('[', ']');
            string studentTag2 = cboStudentTag2.Text.Trim('[', ']');

            #region 讀取學生清單
            btnPrevious.Enabled = false;
            btnCacluate.Enabled = false;
            EnsureConfirmEvents();

            Dictionary<string, StudentRecord> studentsById = new Dictionary<string, StudentRecord>();
            foreach (StudentRecord student in _StudentList)
            {
                if (student != null && !studentsById.ContainsKey(student.ID))
                    studentsById.Add(student.ID, student);
            }

            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
            List<StudentMotherGroup> loadedGroups = null;

            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs e1)
            {
                MotherForm.SetStatusBarMessage("讀取學生清單", e1.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(1);
                    CurrentStudentMotherGroupSource source = new CurrentStudentMotherGroupSource();
                    loadedGroups = source.Load(_StudentList, _TagList, _GradeYearList, studentFilter, studentTag1, studentTag2);
                    bkw.ReportProgress(80);
                    foreach (StudentMotherGroup group in loadedGroups)
                        MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, group, group.MatchesExcludedCategory);
                    bkw.ReportProgress(100);
                }
                catch (Exception ex)
                {
                    bkwException = ex;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (IsDisposed)
                {
                    _nextStepRunning = false;
                    return;
                }
                if (bkwException != null)
                {
                    ReleaseNextStep(true);
                    MessageBox.Show("學生列表讀取失敗。\n" + bkwException.Message);
                    return;
                }
                if (loadedGroups == null || loadedGroups.Count == 0)
                {
                    ReleaseNextStep(true);
                    MessageBox.Show("沒有找到符合條件的學生");
                    btnPrevious.Enabled = true;
                    return;
                }

                try
                {
                    plStudentView.Visible = true;
                    ShowConfirmList(loadedGroups, studentsById);
                }
                catch (Exception ex)
                {
                    ReleaseNextStep(true);
                    MessageBox.Show("開啟母群確認畫面失敗。\n" + ex.Message);
                }
            };

            bkw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ReleaseNextStep(true);
                MessageBox.Show("學生列表讀取失敗。\n" + ex.Message);
            }
            #endregion
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelHistorySnapshotLoad();
            base.OnFormClosing(e);
        }

        private void CancelHistorySnapshotLoad()
        {
            _settingsPageClosed = true;
            System.Threading.Interlocked.Increment(ref _historyLoadGeneration);
            BackgroundWorker worker = _historyLoadWorker;
            if (worker != null && worker.WorkerSupportsCancellation && worker.IsBusy)
                worker.CancelAsync();
        }

        private int HistoryLoadGeneration()
        {
            return System.Threading.Interlocked.CompareExchange(ref _historyLoadGeneration, 0, 0);
        }

        private bool IsHistoryLoadCurrent(int generation)
        {
            if (generation != HistoryLoadGeneration())
                return false;
            if (_settingsPageClosed)
                return false;
            if (IsDisposed)
                return false;
            return true;
        }

        private void LockHistoryLoadInputs()
        {
            _historyInputsLocked = true;
            if (_cboMotherGroupSource != null)
                _cboMotherGroupSource.Enabled = false;
            SetHistoryFiltersEnabled(false);
            if (_nudCalcSchoolYear != null)
                _nudCalcSchoolYear.Enabled = false;
            if (_cboCalcSemester != null)
                _cboCalcSemester.Enabled = false;
            if (listGradeYear != null)
                listGradeYear.Enabled = false;
            if (cboStudentFilter != null)
                cboStudentFilter.Enabled = false;
            chbHandAdj.Enabled = false;
            chbMakeUp.Enabled = false;
            chbRetake.Enabled = false;
            chbSYAdj.Enabled = false;
        }

        private void RestoreHistoryLoadInputs()
        {
            if (!_historyInputsLocked)
                return;
            _historyInputsLocked = false;
            if (_cboMotherGroupSource != null)
                _cboMotherGroupSource.Enabled = true;
            SetHistoryFiltersEnabled(IsHistorySourceSelected());
            ApplyCalculationPeriodEditors();
            if (listGradeYear != null)
                listGradeYear.Enabled = true;
            if (cboStudentFilter != null)
                cboStudentFilter.Enabled = true;
            chbHandAdj.Enabled = true;
            chbMakeUp.Enabled = true;
            chbRetake.Enabled = true;
            chbSYAdj.Enabled = true;
        }

        private void ShowHistoryLoadFailure(string failure)
        {
            if (_settingsPageClosed || IsDisposed)
            {
                _nextStepRunning = false;
                return;
            }
            RestoreHistoryLoadInputs();
            _frozenHistoryRequest = null;
            ClearLoadedConfirmList();
            btnPrevious.Enabled = true;
            ReleaseNextStep(true);
            if (labelX6 != null)
                labelX6.Text = failure;
            MessageBox.Show(failure);
        }

        private void RejectHistoryLoad(string batchId, string step, string reason)
        {
            ReleaseFailedHistoryPick();
            ShowHistoryLoadFailure(HistoryRestoreLog.FailureMessage(batchId, step, reason));
        }

        private void BeginHistorySnapshotRestore()
        {
            HistoryRankBatch selected = _cboHistoryBatch == null ? null : _cboHistoryBatch.SelectedItem as HistoryRankBatch;
            if (selected == null)
            {
                ReleaseNextStep(true);
                MessageBox.Show("請選擇一筆歷史學期成績排名批次。");
                return;
            }
            string selectedType;
            if (!SemesterHistoryBatchCatalog.TryClassifyExam(selected.ExamName, out selectedType))
            {
                ReleaseNextStep(true);
                MessageBox.Show(HistorySnapshotRestore.UnrecognizedExamMessage);
                return;
            }

            SyncHistoryPeriodFromControls();
            if (selected.SchoolYear != _calculationPeriod.HistorySchoolYear || selected.Semester != _calculationPeriod.HistorySemester)
            {
                RejectHistoryLoad(selected.Id, HistoryRestoreLog.PeriodStep, SemesterHistoryPeriodGuard.MismatchMessage);
                return;
            }
            _calculationPeriod.RememberHistoryBatch(selected.Id, _calculationPeriod.HistorySchoolYear, _calculationPeriod.HistorySemester);
            string batchId = selected.Id;
            string failedStep = "";
            string historyYear = _calculationPeriod.HistorySchoolYear;
            string historySemester = _calculationPeriod.HistorySemester;
            // 年級與擇優採計已由下一步共用的 CaptureCalculationScope 依圖1重設。這裡不讀目前班級或年級。
            string calculationSchoolYear = _calculationPeriod.SchoolYear;
            string calculationSemester = _calculationPeriod.Semester;
            string excludedCategory = cboStudentFilter.Text.Trim('[', ']');
            List<TagDefinition> tagDefinitions = new List<TagDefinition>();
            foreach (TagConfigRecord tag in _TagList)
            {
                if (tag == null)
                    continue;
                tagDefinitions.Add(new TagDefinition { Id = tag.ID, Prefix = tag.Prefix, Name = tag.Name });
            }

            try
            {
            _frozenHistoryRequest = FreezeConfirmRequest();
            ClearLoadedConfirmList();
            btnPrevious.Enabled = false;
            btnCacluate.Enabled = false;
            EnsureConfirmEvents();

            Dictionary<string, StudentRecord> studentsById = new Dictionary<string, StudentRecord>();
            foreach (StudentRecord student in _StudentList)
            {
                if (student != null && !studentsById.ContainsKey(student.ID))
                    studentsById.Add(student.ID, student);
            }

            int generation = System.Threading.Interlocked.Increment(ref _historyLoadGeneration);
            Func<bool> stillCurrent = delegate
            {
                return generation == HistoryLoadGeneration() && !_settingsPageClosed;
            };
            if (!IsHistoryLoadCurrent(generation))
            {
                _frozenHistoryRequest = null;
                _nextStepRunning = false;
                return;
            }
            LockHistoryLoadInputs();

            BackgroundWorker bkw = new BackgroundWorker();
            _historyLoadWorker = bkw;
            bkw.WorkerReportsProgress = true;
            bkw.WorkerSupportsCancellation = true;
            Exception bkwException = null;
            bool settingFound = false;
            bool periodRejected = false;
            HistorySnapshotLoad load = null;
            HistoryRankLoad rankLoad = null;
            HistoryRestoreLog trace = HistoryRestoreLog.Start(batchId);
            Stopwatch stepWatch = new Stopwatch();
            bkw.ProgressChanged += delegate (object sender, ProgressChangedEventArgs args)
            {
                string stage = args.UserState as string;
                if (!IsHistoryLoadCurrent(generation) || string.IsNullOrEmpty(stage))
                    return;
                labelX6.Text = stage;
            };
            bkw.DoWork += delegate
            {
                try
                {
                    if (!stillCurrent())
                        return;
                    bkw.ReportProgress(0, "正在讀取歷史批次及母群資料...");
                    QueryHelper queryHelper = new QueryHelper();
                    failedStep = HistoryRestoreLog.PeriodStep;
                    stepWatch.Restart();
                    DataTable periodTable = queryHelper.Select(SemesterHistoryPeriodGuard.SelectSql(batchId));
                    trace.Record(failedStep, stepWatch.ElapsedMilliseconds, periodTable == null ? 0 : periodTable.Rows.Count, "");
                    if (!stillCurrent())
                        return;
                    if (!SemesterHistoryPeriodGuard.Matches(periodTable, batchId, historyYear, historySemester))
                    {
                        periodRejected = true;
                        return;
                    }
                    failedStep = HistoryRestoreLog.SettingStep;
                    stepWatch.Restart();
                    DataTable table = queryHelper.Select(HistorySnapshotRestore.SelectSettingSql(batchId));
                    string settingXml;
                    string extensionJson;
                    settingFound = HistorySnapshotRestore.TryReadBatch(table, batchId, out settingXml, out extensionJson);
                    trace.Record(failedStep, stepWatch.ElapsedMilliseconds, table == null ? 0 : table.Rows.Count, "設定字元數=" + (settingXml == null ? 0 : settingXml.Length) + " 延伸字元數=" + (extensionJson == null ? 0 : extensionJson.Length));
                    if (!stillCurrent())
                        return;
                    if (!settingFound)
                        return;
                    failedStep = HistoryRestoreLog.TagStep;
                    stepWatch.Restart();
                    List<StudentTagLink> links = new List<StudentTagLink>();
                    foreach (StudentTagRecord tag in StudentTag.SelectAll())
                    {
                        if (tag == null)
                            continue;
                        links.Add(new StudentTagLink
                        {
                            StudentId = tag.RefStudentID,
                            TagId = tag.RefTagID,
                            Name = tag.Name
                        });
                    }
                    trace.Record(failedStep, stepWatch.ElapsedMilliseconds, links.Count, "");
                    if (!stillCurrent())
                        return;
                    failedStep = HistoryRestoreLog.SettingStep;
                    stepWatch.Restart();
                    load = HistorySnapshotRestore.LoadSemester(batchId, extensionJson, settingXml, excludedCategory, tagDefinitions, links);
                    if (load.Restored || load.Message != HistorySnapshotRestore.MissingSnapshotMessage)
                    {
                        trace.Record(HistoryRestoreLog.StudentStep, stepWatch.ElapsedMilliseconds, 0, load.Restored ? "未查排名學生" : "快照停止");
                        return;
                    }
                    if (!stillCurrent())
                        return;
                    failedStep = HistoryRestoreLog.StudentStep;
                    stepWatch.Restart();
                    bkw.ReportProgress(0, "正在統計歷史排名學生...");
                    int pageCount;
                    bool discarded;
                    DataTable rankTable = HistoryRankRestore.ReadBatchStudentTable(
                        batchId,
                        HistoryRankRestore.BatchStudentPageSize,
                        delegate (string sql) { return queryHelper.Select(sql); },
                        stillCurrent,
                        delegate (int done, int total)
                        {
                            if (!stillCurrent())
                                return;
                            int percent = total <= 0 ? 0 : done * 100 / total;
                            if (percent > 99)
                                percent = 99;
                            bkw.ReportProgress(percent, "正在讀取歷史排名學生（第 " + done.ToString() + " / " + total.ToString() + " 頁）");
                        },
                        out pageCount,
                        out discarded);
                    long queryMs = stepWatch.ElapsedMilliseconds;
                    if (discarded || !stillCurrent())
                    {
                        rankTable = null;
                        rankLoad = null;
                        trace.Record(HistoryRestoreLog.StudentStep, queryMs, 0, "已停止後續分頁 頁數=" + pageCount.ToString());
                        return;
                    }
                    stepWatch.Restart();
                    bkw.ReportProgress(99, "正在合併歷史排名學生...");
                    rankLoad = HistoryRankRestore.ReadBatchStudents(batchId, rankTable, excludedCategory, tagDefinitions, links);
                    long mergeMs = stepWatch.ElapsedMilliseconds;
                    SemesterHistoryStudentParticipation.KeepYearDeptClassParticipating(rankLoad);
                    trace.RecordStudentRead(queryMs, mergeMs, pageCount, rankTable == null ? 0 : rankTable.Rows.Count);
                }
                catch (Exception ex)
                {
                    bkwException = ex;
                    rankLoad = null;
                    trace.Fail(failedStep, stepWatch.ElapsedMilliseconds, ex);
                }
                finally
                {
                    trace.Write();
                }
            };
            bkw.RunWorkerCompleted += delegate
            {
                if (!IsHistoryLoadCurrent(generation))
                {
                    _nextStepRunning = false;
                    return;
                }
                if (periodRejected)
                {
                    ReleaseFailedHistoryPick();
                    ShowHistoryLoadFailure(HistoryRestoreLog.FailureMessage(batchId, HistoryRestoreLog.PeriodStep, SemesterHistoryPeriodGuard.MismatchMessage));
                    return;
                }
                if (bkwException != null || !settingFound)
                {
                    ReleaseFailedHistoryPick();
                    string failure = bkwException == null
                        ? HistoryRestoreLog.FailureMessage(batchId, HistoryRestoreLog.SettingStep, "找不到所選的歷史批次，無法讀取設定。本次不會改用目前學生資料。")
                        : HistoryRestoreLog.FailureMessage(batchId, failedStep, bkwException);
                    ShowHistoryLoadFailure(failure);
                    return;
                }
                if (load != null && load.Restored)
                {
                    try
                    {
                        RestoreHistoryLoadInputs();
                        plStudentView.Visible = true;
                        lbSchoolYear.Text = calculationSchoolYear;
                        lbSemester.Text = calculationSemester;
                        PublishConfirmRequest(_frozenHistoryRequest);
                        _frozenHistoryRequest = null;
                        ShowConfirmList(load.Students, studentsById);
                    }
                    catch (Exception ex)
                    {
                        ClearLoadedConfirmList();
                        btnPrevious.Enabled = true;
                        ReleaseNextStep(true);
                        MessageBox.Show("開啟母群確認畫面失敗。\n" + ex.Message);
                    }
                    return;
                }
                if (load == null || load.Message != HistorySnapshotRestore.MissingSnapshotMessage)
                {
                    ReleaseFailedHistoryPick();
                    string restoreFailure = HistoryRestoreLog.FailureMessage(batchId, HistoryRestoreLog.SettingStep, load == null ? HistorySnapshotRestore.UnreadableSnapshotMessage : load.Message);
                    ShowHistoryLoadFailure(restoreFailure);
                    return;
                }
                if (rankLoad == null || !rankLoad.Restored)
                {
                    ReleaseFailedHistoryPick();
                    string restoreFailure = HistoryRestoreLog.FailureMessage(batchId, HistoryRestoreLog.StudentStep, rankLoad == null ? HistoryRankRestore.EmptyBatchMessage : rankLoad.Message);
                    ShowHistoryLoadFailure(restoreFailure);
                    return;
                }

                try
                {
                    RestoreHistoryLoadInputs();
                    plStudentView.Visible = true;
                    lbSchoolYear.Text = calculationSchoolYear;
                    lbSemester.Text = calculationSemester;
                    PublishConfirmRequest(_frozenHistoryRequest);
                    _frozenHistoryRequest = null;
                    ShowConfirmList(rankLoad.Students, studentsById);
                }
                catch (Exception ex)
                {
                    ClearLoadedConfirmList();
                    btnPrevious.Enabled = true;
                    ReleaseNextStep(true);
                    MessageBox.Show("開啟母群確認畫面失敗。\n" + ex.Message);
                }
            };
            if (!IsHistoryLoadCurrent(generation))
            {
                _nextStepRunning = false;
                return;
            }
            bkw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                if (_settingsPageClosed || IsDisposed)
                {
                    _nextStepRunning = false;
                    return;
                }
                ReleaseFailedHistoryPick();
                ShowHistoryLoadFailure(HistoryRestoreLog.FailureMessage(batchId, failedStep.Length == 0 ? HistoryRestoreLog.PeriodStep : failedStep, ex));
            }
        }

        private void ShowConfirmList(List<StudentMotherGroup> loadedGroups, Dictionary<string, StudentRecord> studentsById)
        {
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup group in loadedGroups)
            {
                ConfirmStudentRow row = new ConfirmStudentRow();
                row.Group = group;
                StudentRecord student;
                if (studentsById != null && studentsById.TryGetValue(group.StudentId, out student) && student.Class != null)
                {
                    row.ClassName = student.Class.Name;
                    row.SeatNo = "" + student.SeatNo;
                }
                rows.Add(row);
            }

            bool historySource = IsHistorySourceSelected();
            if (_lblAddStudentCaption != null)
                _lblAddStudentCaption.Visible = historySource;
            if (_txtAddStudentNumber != null)
                _txtAddStudentNumber.Visible = historySource;
            if (_btnAddHistoryStudent != null)
            {
                _btnAddHistoryStudent.Text = "加入學生";
                _btnAddHistoryStudent.Visible = historySource;
            }

            _confirmList = new MotherGroupConfirmList();
            _confirmList.Load(rows);
            _bindingConfirmGrid = true;
            MotherGroupConfirmGrid.Configure(dgvStudentList);
            MotherGroupConfirmGrid.Show(dgvStudentList, _confirmList.Rows);
            _bindingConfirmGrid = false;
            if (_txtMotherGroupSearch != null)
                _txtMotherGroupSearch.Text = "";
            MotherGroupConfirmGrid.RefreshSummary(_lblMotherGroupSummary, _dgvRankPopulation, _dgvGroupCounts, _confirmList);
            dgvStudentList.ContextMenuStrip = MotherGroupConfirmGrid.CreateParticipationMenu(dgvStudentList, _confirmList, _lblMotherGroupSummary, _dgvRankPopulation, _dgvGroupCounts, CalculationEditAllowed);
            btnPrevious.Enabled = true;
            btnCacluate.Enabled = true;
            btnExport.Visible = true;
            ShowConfirmForm();
            ReleaseNextStep(false);
            MotherForm.SetStatusBarMessage("學生列表讀取完成");
        }

        private void AddHistoryStudent()
        {
            if (!CalculationEditAllowed() || !IsHistorySourceSelected() || _confirmList == null)
                return;
            string studentNumber = _txtAddStudentNumber == null ? "" : _txtAddStudentNumber.Text.Trim();
            if (studentNumber.Length == 0)
            {
                MessageBox.Show("請輸入學號。");
                return;
            }

            DataTable table;
            List<StudentTagLink> links = new List<StudentTagLink>();
            try
            {
                QueryHelper queryHelper = new QueryHelper();
                table = queryHelper.Select(HistoryStudentAdd.SelectSql(studentNumber));
                foreach (StudentTagRecord tag in StudentTag.SelectAll())
                {
                    if (tag == null)
                        continue;
                    links.Add(new StudentTagLink
                    {
                        StudentId = tag.RefStudentID,
                        TagId = tag.RefTagID,
                        Name = tag.Name
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("查詢學生失敗。\n" + ex.Message);
                return;
            }

            List<TagDefinition> tagDefinitions = new List<TagDefinition>();
            foreach (TagConfigRecord tag in _TagList)
            {
                if (tag == null)
                    continue;
                tagDefinitions.Add(new TagDefinition { Id = tag.ID, Prefix = tag.Prefix, Name = tag.Name });
            }
            HistoryStudentAddResult result = HistoryStudentAdd.Read(
                studentNumber,
                table,
                _confirmList,
                cboStudentFilter.Text.Trim('[', ']'),
                cboStudentTag1.Text.Trim('[', ']'),
                cboStudentTag2.Text.Trim('[', ']'),
                tagDefinitions,
                links);
            if (result.Status != HistoryStudentAddStatus.Added || result.Student == null)
            {
                MessageBox.Show(result.Message);
                return;
            }

            ConfirmStudentRow row = new ConfirmStudentRow();
            row.Group = result.Student;
            row.ClassName = result.ClassName;
            row.SeatNo = result.SeatNo;
            _confirmList.Add(row);
            _bindingConfirmGrid = true;
            MotherGroupConfirmGrid.Show(dgvStudentList, _confirmList.Rows);
            _bindingConfirmGrid = false;
            if (_txtMotherGroupSearch != null && _txtMotherGroupSearch.Text.Length > 0)
                MotherGroupConfirmGrid.ApplyVisibility(dgvStudentList, _confirmList.VisibleStudentIds(_txtMotherGroupSearch.Text));
            MotherGroupConfirmGrid.RefreshSummary(_lblMotherGroupSummary, _dgvRankPopulation, _dgvGroupCounts, _confirmList);
        }

        private void ClearLoadedConfirmList()
        {
            plStudentView.Visible = false;
            ShowConditionForm();
            btnExport.Visible = false;
            _confirmList = null;
            if (_txtMotherGroupSearch != null)
                _txtMotherGroupSearch.Text = "";
            if (dgvStudentList.Rows.Count > 0)
                dgvStudentList.Rows.Clear();
        }

        private void ReleaseFailedHistoryPick()
        {
            if (_cboHistoryBatch != null)
            {
                _bindingHistoryBatch = true;
                _cboHistoryBatch.SelectedIndex = -1;
                _bindingHistoryBatch = false;
            }
            if (_calculationPeriod != null)
                _calculationPeriod.ClearHistoryBatchSelection();
        }

        private void ClearFailedHistorySelection()
        {
            if (_cboHistoryBatch != null)
            {
                _bindingHistoryBatch = true;
                _cboHistoryBatch.SelectedIndex = -1;
                _cboHistoryBatch.Items.Clear();
                _bindingHistoryBatch = false;
            }
            if (_calculationPeriod != null)
                _calculationPeriod.ClearHistoryBatchSelection();
            ApplyHistoryNextGate();
        }

        private void ReportHistoryBatchQueryFailure(Exception ex)
        {
            string message = HistoryBatchQueryFailureText + "\n" + (ex == null ? "" : ex.Message);
            if (CanUpdateSourceGate() && labelX6 != null)
                labelX6.Text = message;
            if (_historyBatchFailureDialog != null)
                _historyBatchFailureDialog(message);
            else
                MessageBox.Show(message);
        }

        private static bool IsHistoryBatchQueryFailure(string text)
        {
            return text != null && text.StartsWith(HistoryBatchQueryFailureText);
        }

        private static bool IsHistoryMotherGroupLoadFailure(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            return text.StartsWith("歷史批次 ")
                && text.IndexOf("讀取失敗。") >= 0
                && text.EndsWith("本次不會進入確認清單，也不會開始排名計算。");
        }

        internal void btnPrevious_Click(object sender, EventArgs e)
        {
            if (_rankCalculationRunning)
                return;
            plStudentView.Visible = false;
            ShowConditionForm();
            btnExport.Visible = false;
            btnCacluate.Enabled = true;
            if (_txtMotherGroupSearch != null)
                _txtMotherGroupSearch.Text = "";
            _confirmList = null;
            if (dgvStudentList.Rows.Count > 0)
            {
                dgvStudentList.Rows.Clear();
            }
            ReleaseNextStep(true);
        }

        private void ExportMotherGroup()
        {
            if (_confirmList == null || _confirmList.Rows.Count == 0)
            {
                MessageBox.Show("沒有可匯出的確認清單。");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "匯出母群";
            dialog.FileName = "學期成績排名母群.xlsx";
            dialog.Filter = "Excel (*.xlsx)|*.xlsx|所有檔案 (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                IDictionary<string, int> currentStatuses = null;
                List<string> missingStatusIds = MotherGroupExcelExchange.MissingStatusIds(_confirmList);
                if (missingStatusIds.Count > 0)
                {
                    DataTable statusTable;
                    try
                    {
                        QueryHelper queryHelper = new QueryHelper();
                        statusTable = queryHelper.Select(MotherGroupExcelExchange.SelectCurrentStatusSql(missingStatusIds));
                    }
                    catch (Exception queryError)
                    {
                        MessageBox.Show("學生狀態查詢失敗：" + queryError.Message);
                        return;
                    }
                    try
                    {
                        currentStatuses = MotherGroupExcelExchange.ReadCurrentStatuses(missingStatusIds, statusTable);
                    }
                    catch (InvalidOperationException statusError)
                    {
                        MessageBox.Show(statusError.Message);
                        return;
                    }
                }
                MotherGroupExcelExchange.Save(dialog.FileName, _confirmList, currentStatuses);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("檔案儲存失敗：" + ex.Message);
                return;
            }

            if (MessageBox.Show("檔案儲存完成，是否開啟？", "是否開啟", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            try
            {
                System.Diagnostics.Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("檔案開啟失敗：" + ex.Message);
            }
        }

        private void EnsureFigureControls()
        {
            if (_dgvRankPopulation.Columns.Count == 0)
                MotherGroupConfirmGrid.ConfigurePopulation(_dgvRankPopulation);
            if (_dgvGroupCounts.Columns.Count == 0)
                MotherGroupConfirmGrid.ConfigureDetail(_dgvGroupCounts);
            if (MinimumSize.IsEmpty)
                MinimumSize = Size;
        }

        internal void txtMotherGroupSearch_TextChanged(object sender, EventArgs e)
        {
            if (_bindingConfirmGrid || _confirmList == null)
                return;
            MotherGroupConfirmGrid.ApplyVisibility(dgvStudentList, _confirmList.VisibleStudentIds(_txtMotherGroupSearch.Text));
        }

        internal void btnRestoreParticipation_Click(object sender, EventArgs e)
        {
            if (!CalculationEditAllowed() || _confirmList == null)
                return;
            _confirmList.Restore();
            _bindingConfirmGrid = true;
            MotherGroupConfirmGrid.RefreshParticipation(dgvStudentList, _confirmList);
            _bindingConfirmGrid = false;
            MotherGroupConfirmGrid.RefreshSummary(_lblMotherGroupSummary, _dgvRankPopulation, _dgvGroupCounts, _confirmList);
        }

        internal void btnAddHistoryStudent_Click(object sender, EventArgs e)
        {
            AddHistoryStudent();
        }

        private void EnsureConfirmEvents()
        {
            if (_confirmEventsReady)
                return;
            _confirmEventsReady = true;
            dgvStudentList.CurrentCellDirtyStateChanged += delegate
            {
                if (!CalculationEditAllowed())
                    return;
                if (dgvStudentList.IsCurrentCellDirty)
                    dgvStudentList.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvStudentList.CellValueChanged += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (_bindingConfirmGrid || _confirmList == null)
                    return;
                if (!CalculationEditAllowed())
                {
                    _bindingConfirmGrid = true;
                    MotherGroupConfirmGrid.RefreshParticipation(dgvStudentList, _confirmList);
                    _bindingConfirmGrid = false;
                    return;
                }
                MotherGroupConfirmGrid.ApplyCellEdit(dgvStudentList, _confirmList, e.RowIndex, e.ColumnIndex);
                MotherGroupConfirmGrid.RefreshSummary(_lblMotherGroupSummary, _dgvRankPopulation, _dgvGroupCounts, _confirmList);
            };
            dgvStudentList.CellMouseDown += delegate (object sender, DataGridViewCellMouseEventArgs e)
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                    return;
                if (!dgvStudentList.Rows[e.RowIndex].Selected)
                {
                    dgvStudentList.ClearSelection();
                    dgvStudentList.Rows[e.RowIndex].Selected = true;
                }
            };
        }

        private void CaptureCalculationScope()
        {
            _GradeYearList = new List<int>();
            foreach (ListViewItem listViewItem in listGradeYear.Items)
            {
                if (listViewItem.Checked == true)
                {
                    int gradeYear = Convert.ToInt32(listViewItem.Text.Trim('年', '級'));
                    _GradeYearList.Add(gradeYear);
                }
            }

            _GradeSelectList = new List<string>();
            _GradeSelectList.Add("原始成績");
            if (chbHandAdj.Checked)
                _GradeSelectList.Add("手動調整成績");
            if (chbMakeUp.Checked)
                _GradeSelectList.Add("補考成績");
            if (chbRetake.Checked)
                _GradeSelectList.Add("重修成績");
            if (chbSYAdj.Checked)
                _GradeSelectList.Add("學年調整成績");
        }

        private bool CalculationEditAllowed()
        {
            return !_rankCalculationRunning || _capturingCalculationEdit;
        }

        private void CommitPendingParticipationEdit()
        {
            _capturingCalculationEdit = true;
            try
            {
                if (dgvStudentList != null && dgvStudentList.IsCurrentCellDirty)
                    dgvStudentList.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
            finally
            {
                _capturingCalculationEdit = false;
            }
        }

        private void RememberCalculationUi()
        {
            _calculationUiState = new CalculationUiState();
            _calculationUiState.CalculateEnabled = btnCacluate.Enabled;
            _calculationUiState.PreviousEnabled = btnPrevious.Enabled;
            _calculationUiState.GridEnabled = dgvStudentList == null || dgvStudentList.Enabled;
            if (_btnRestoreParticipation != null)
            {
                _calculationUiState.RestoreEnabled = _btnRestoreParticipation.Enabled;
                _calculationUiState.RestoreVisible = _btnRestoreParticipation.Visible;
            }
            if (_btnAddHistoryStudent != null)
            {
                _calculationUiState.AddEnabled = _btnAddHistoryStudent.Enabled;
                _calculationUiState.AddVisible = _btnAddHistoryStudent.Visible;
            }
            if (_txtAddStudentNumber != null)
            {
                _calculationUiState.AddTextEnabled = _txtAddStudentNumber.Enabled;
                _calculationUiState.AddTextVisible = _txtAddStudentNumber.Visible;
            }
            _calculationUiDisabled = true;
        }

        private void RestoreCalculationEditors()
        {
            if (!_calculationUiDisabled)
                return;
            _calculationUiDisabled = false;
            if (plStudentView == null || !plStudentView.Visible)
                Enabled = true;
            btnCacluate.Enabled = _calculationUiState.CalculateEnabled;
            btnPrevious.Enabled = _calculationUiState.PreviousEnabled;
            if (dgvStudentList != null)
                dgvStudentList.Enabled = _calculationUiState.GridEnabled;
            if (_btnRestoreParticipation != null)
            {
                _btnRestoreParticipation.Enabled = _calculationUiState.RestoreEnabled;
                _btnRestoreParticipation.Visible = _calculationUiState.RestoreVisible;
            }
            if (_btnAddHistoryStudent != null)
            {
                _btnAddHistoryStudent.Enabled = _calculationUiState.AddEnabled;
                _btnAddHistoryStudent.Visible = _calculationUiState.AddVisible;
            }
            if (_txtAddStudentNumber != null)
            {
                _txtAddStudentNumber.Enabled = _calculationUiState.AddTextEnabled;
                _txtAddStudentNumber.Visible = _calculationUiState.AddTextVisible;
            }
        }

        internal void btnCacluate_Click(object sender, EventArgs e)
        {
            if (!_rankCalculationRunning)
                RunRankCalculation();
        }

        private void RunRankCalculation()
        {
            _rankCalculationRunning = true;
            bool rankWorkerStarted = false;
            SemesterRankCalculationTrace rankTrace = null;
            try
            {
            CommitPendingParticipationEdit();
            string unparsedYear = HistoryCalculationScope.UnparsedYearMessage(_confirmList);
            if (unparsedYear.Length > 0)
                MessageBox.Show(unparsedYear);
            if (!SemesterRankWriteGate.CanWriteRankTables())
            {
                MessageBox.Show(SemesterRankWriteGate.BlockedMessage);
                return;
            }
            MotherGroupConfirmList calculationList = _confirmList == null ? new MotherGroupConfirmList() : _confirmList.CopyWorking();
            List<string> gradeSelectCopy = new List<string>();
            if (_GradeSelectList != null)
            {
                foreach (string selectedGrade in _GradeSelectList)
                    gradeSelectCopy.Add(selectedGrade);
            }
            string schoolYear = lbSchoolYear.Text;
            string semester = lbSemester.Text;
            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            string tag1 = cboStudentTag1.Text.Trim('[', ']');
            string tag2 = cboStudentTag2.Text.Trim('[', ']');
            string motherGroupSourceName = CalculationPeriod.CurrentSourceName;
            if (_cboMotherGroupSource != null && _cboMotherGroupSource.SelectedItem != null)
                motherGroupSourceName = _cboMotherGroupSource.SelectedItem.ToString();
            string historyBatchId = "";
            if (motherGroupSourceName == CalculationPeriod.HistorySourceName && _calculationPeriod != null)
                historyBatchId = _calculationPeriod.HistoryBatchId ?? "";
            if (RankCalculationConfirm.GradeYears(calculationList, _GradeYearList).Count == 0)
            {
                MessageBox.Show("沒有可計算的年級");
                return;
            }
            if (MessageBox.Show(
                RankCalculationConfirm.Message(schoolYear, semester, motherGroupSourceName, RankCalculationConfirm.GradeList(calculationList, _GradeYearList)),
                "計算學期成績固定排名",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning) != DialogResult.OK)
            {
                _rankCalculationRunning = false;
                return;
            }
            RememberCalculationUi();
            btnCacluate.Enabled = false;
            btnPrevious.Enabled = false;
            Enabled = false;
            if (dgvStudentList != null)
                dgvStudentList.Enabled = false;
            if (_btnRestoreParticipation != null)
                _btnRestoreParticipation.Enabled = false;
            if (_btnAddHistoryStudent != null)
                _btnAddHistoryStudent.Enabled = false;
            if (_txtAddStudentNumber != null)
                _txtAddStudentNumber.Enabled = false;
            pbLoading.Visible = true;

            // 學生依年級分批使用
            Dictionary<string, List<string>> gradeStudentDict = new Dictionary<string, List<string>>();
            SemesterRankStudentSqlResult rankStudents = null;
            rankTrace = SemesterRankCalculationTrace.Start(schoolYear, semester, motherGroupSourceName, historyBatchId);

            try
            {
            #region 儲存設定
            XmlDocument document = new XmlDocument();
            XmlElement configEle = document.CreateElement("Setting");

            foreach (var item in gradeSelectCopy)
            {
                var ele = document.CreateElement("擇優採計成績");
                ele.InnerText = item;
                configEle.AppendChild(ele);
            }

            var cd = K12.Data.School.Configuration["固定排名_學期成績排名計算"];
            cd["設定檔"] = configEle.OuterXml;
            // 排名語句若在之後失敗，這份擇優設定已經寫入學校組態，不會隨排名語句回滾。本次不改寫入時機，也不寫快照。
            cd.Save();
            rankTrace.Mark("擇優設定");
            #endregion
            //        #region 產生學生清單的SQL
            //        List<string> studentSqlList = new List<string>();
            //        foreach (DataGridViewRow row in dgvStudentList.Rows)
            //        {
            //            #region 單筆學生資料的SQL
            //            string studentSql = @"
            //SELECT
            //    '" + row.Tag + @"'::BIGINT AS student_id
            //    , '" + "" + row.Cells[3].Value + @"'::TEXT AS student_name
            //    , '" + ("" + row.Cells[4].Value).Trim('年', '級') + @"'::INT AS rank_grade_year
            //    , '" + "" + row.Cells[5].Value + @"'::TEXT AS rank_dept_name
            //    , '" + "" + row.Cells[6].Value + @"'::TEXT AS rank_class_name
            //    , '" + "" + row.Cells[7].Value + @"'::TEXT AS rank_tag1
            //    , '" + "" + row.Cells[8].Value + @"'::TEXT AS rank_tag2
            //";
            //            #endregion
            //            //把單筆學生資料的SQL加入到List
            //            studentSqlList.Add(studentSql);
            //        }

            #region 依年級產生學生清單的SQL
            rankStudents = SemesterRankSql.Build(calculationList);
            rankStudents.Grades = RankCalculationConfirm.LimitToSelectedGrades(rankStudents.Grades, _GradeYearList);
            rankTrace.ReadStudents(rankStudents);
            rankTrace.Mark("學生清單");
            foreach (SemesterRankGradeStudents grade in rankStudents.Grades)
            {
                string gr = grade.GradeYear + "年級";
                if (!gradeStudentDict.ContainsKey(gr))
                    gradeStudentDict.Add(gr, new List<string>());
                gradeStudentDict[gr].AddRange(grade.Selects);
            }
            #endregion
            }
            catch (Exception rankStartException)
            {
                RestoreCalculationEditors();
                pbLoading.Visible = false;
                rankTrace.Mark("準備失敗");
                SemesterRankPublishOutcome startOutcome = SemesterRankPublishOutcome.From(rankStartException, null, rankTrace);
                string startMessage = startOutcome.DialogMessage ?? "";
                if (startMessage.IndexOf("計算排名失敗") != 0)
                    startMessage = "計算排名失敗。\n" + startMessage;
                MessageBox.Show(startMessage);
                return;
            }


            string calculationSetting = "";
            string extensionJson = "";



            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
            DataTable publishResult = null;
            pbLoading.Visible = true;

            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                MotherForm.SetStatusBarMessage("計算排名中", eventArgs.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                QueryHelper queryHelper = null;
                try
                {
                    bkw.ReportProgress(1);

                    queryHelper = SemesterRankPublishTransport.OpenWriter(rankTrace);

                    #region 產生要儲存到rank_batch的setting的Xml
                    XmlDocument xdoc = new XmlDocument();
                    var settingEle = xdoc.CreateElement("Setting");
                    settingEle.SetAttribute("學年度", schoolYear);
                    settingEle.SetAttribute("學期", semester);
                    settingEle.SetAttribute("考試名稱", "學期成績");
                    settingEle.SetAttribute("不排名學生類別", studentFilter);
                    settingEle.SetAttribute("類別一", tag1);
                    settingEle.SetAttribute("類別二", tag2);

                    foreach (string gr in gradeStudentDict.Keys)
                    {
                        var gradeYearEle = xdoc.CreateElement("年級");
                        gradeYearEle.InnerText = "" + gr.Trim('年', '級');
                        settingEle.AppendChild(gradeYearEle);
                    }

                    foreach (string type in gradeSelectCopy)
                    {
                        var gradeSelectEle = xdoc.CreateElement("擇優採計成績");
                        gradeSelectEle.InnerText = type;
                        settingEle.AppendChild(gradeSelectEle);
                    }

                    calculationSetting = settingEle.OuterXml;
                    extensionJson = MotherGroupSnapshotJson.Compose("[]", calculationList, MotherGroupSnapshot.KindFromSourceName(motherGroupSourceName), historyBatchId);
                    #endregion

                    // 批次與所有勾選年級由 SemesterRankPublishSql 組成同一句，不在這裡先取得批次識別。

                    //2022-04 Cynthia 新增勾選「擇優採計成績」功能；公式與診斷共用 SemesterRankFormulaSql。
                        #region 計算排名的SQL 新增新五標、標準差
                    string insertRankSql = SemesterRankFormulaSql.Build(gradeSelectCopy);
                        #endregion



					//DataTable dt1 = queryHelper.Select(insertRankSql_Old);
					SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(rankStudents.Grades, schoolYear, semester, calculationSetting, extensionJson, insertRankSql, false, false, rankTrace.Id);
                        insertRankSql = SemesterRankSql.ApplyParticipation(publish.Sql);
                    //// debug 
                    //string fiPath = Application.StartupPath + @"\sems_sql1.sql";
                    //using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                    //{
                    //    fi.WriteLine(insertRankSql);
                    //}

                    rankTrace.SqlBytes = Encoding.UTF8.GetByteCount(insertRankSql);
                    rankTrace.Mark("排名語句");
                    publishResult = queryHelper.Select(insertRankSql);
                    rankTrace.Mark("資料庫查詢");
                    if (rankTrace.LargeWriteTrips != 1)
                        throw new InvalidOperationException("結果未確認。公布寫入行程不是一次。");
                        if (SemesterRankPublishSql.HasPublishedDetails(publishResult))
                            bkw.ReportProgress(100);
                        else
                            bkwException = new InvalidOperationException("排名明細沒有寫入。");
                }
                catch (Exception ex)
                {
                    rankTrace.Mark("資料庫查詢失敗");
                    bool transport = SemesterRankPublishTransport.IsUnconfirmed(ex);
                    bool repeatedWrite = rankTrace.LargeWriteTrips > 1;
                    bool writeCountRejected = (ex.Message ?? "").IndexOf("公布寫入行程不是一次") >= 0;
                    DataTable reconciled = null;
                    if ((transport || repeatedWrite || writeCountRejected) && queryHelper != null)
                    {
                        rankTrace.Mark("結果未確認");
                        try
                        {
                            reconciled = SemesterRankPublishTransport.Reconcile(queryHelper, rankTrace);
                        }
                        catch (Exception)
                        {
                            reconciled = null;
                            rankTrace.Mark("核對失敗");
                        }
                    }
                    if (SemesterRankPublishSql.HasPublishedDetails(reconciled))
                    {
                        publishResult = reconciled;
                        bkwException = null;
                        bkw.ReportProgress(100);
                    }
                    else if (transport || writeCountRejected)
                        bkwException = new InvalidOperationException(SemesterRankPublishTransport.UnconfirmedMessage(rankTrace), ex);
                    else
                        bkwException = ex;
                }
                finally
                {
                    if (rankTrace != null)
                    {
                        if (bkwException == null && SemesterRankPublishSql.HasPublishedDetails(publishResult))
                            rankTrace.TransportResult = "成功";
                        else if (bkwException != null && (bkwException.Message ?? "").IndexOf("結果未確認") >= 0)
                            rankTrace.TransportResult = "未確認";
                        else
                            rankTrace.TransportResult = "明確失敗";
                        SemesterRankPublishTransport.WriteLog(rankTrace);
                    }
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                try
                {
                    SemesterRankPublishOutcome.PendingTrace = rankTrace;
                    SemesterRankPublishOutcome outcome = SemesterRankPublishOutcome.From(bkwException, publishResult);
                    _rankCalculationRunning = false;
                    RestoreCalculationEditors();
                    pbLoading.Visible = false;
                    MotherForm.SetStatusBarMessage(outcome.StatusMessage);
                    MessageBox.Show(outcome.DialogMessage);
                }
                finally
                {
                    _rankCalculationRunning = false;
                    RestoreCalculationEditors();
                    pbLoading.Visible = false;
                }
            };

            bkw.RunWorkerAsync();
            rankWorkerStarted = true;
            }
            catch (Exception rankFlowException)
            {
                RestoreCalculationEditors();
                pbLoading.Visible = false;
                if (rankTrace == null)
                    rankTrace = SemesterRankCalculationTrace.Start("", "", "", "");
                rankTrace.Mark("流程失敗");
                SemesterRankPublishOutcome flowOutcome = SemesterRankPublishOutcome.From(rankFlowException, null, rankTrace);
                string flowMessage = flowOutcome.DialogMessage ?? "";
                if (flowMessage.IndexOf("計算排名失敗") != 0)
                    flowMessage = "計算排名失敗。\n" + flowMessage;
                MessageBox.Show(flowMessage);
            }
            finally
            {
                if (!rankWorkerStarted)
                    _rankCalculationRunning = false;
            }
        }

        private void CalculateSemesterAssessmentRank_Resize(object sender, EventArgs e)
        {
            LayoutConditionPage();
            if (pbLoading == null || pbLoading.Parent == null)
                return;
            pbLoading.Location = new Point(pbLoading.Parent.ClientSize.Width / 2 - 16, pbLoading.Parent.ClientSize.Height / 2 - 16);
        }

        private struct CalculationUiState
        {
            public bool CalculateEnabled;
            public bool PreviousEnabled;
            public bool GridEnabled;
            public bool RestoreEnabled;
            public bool RestoreVisible;
            public bool AddEnabled;
            public bool AddVisible;
            public bool AddTextEnabled;
            public bool AddTextVisible;
        }
    }
}
