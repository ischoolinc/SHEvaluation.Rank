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
        bool _screenLoadQueued;
        bool _screenLoadSettled;
        bool _nextStepRunning;
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
        const string NoHistoryBatchText = "這個歷史期間沒有可選的歷史批次。";
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
            if (plStudentView == null)
                return;
            plStudentView.Request = CreateConfirmRequest();
            ShowConfirmForm();
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
            _cboHistorySemester.Visible = true;
            btnNext.Enabled = true;
        }

        private void FailScreenLoad(string stage, Exception ex)
        {
            if (_screenLoadSettled)
                return;
            _screenLoadSettled = true;
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
            _cboHistorySemester.Visible = false;
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
                ApplyCalculationPeriodEditors();
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
                    return;
                }
                SetHistoryFiltersEnabled(true);
                try
                {
                    LoadHistoryBatches();
                }
                catch (Exception ex)
                {
                    ClearFailedHistorySelection();
                    MessageBox.Show("歷史批次清單讀取失敗。\n" + ex.Message);
                }
            };
            _nudHistorySchoolYear.ValueChanged += delegate { OnHistoryPeriodChanged(); };
            _cboHistorySemester.SelectedIndexChanged += delegate { OnHistoryPeriodChanged(); };
            _cboHistoryBatch.SelectedIndexChanged += delegate
            {
                if (_bindingHistoryBatch || _calculationPeriod == null || _cboHistoryBatch == null)
                    return;
                HistoryRankBatch selected = _cboHistoryBatch.SelectedItem as HistoryRankBatch;
                if (selected == null)
                {
                    _calculationPeriod.ClearHistoryBatchSelection();
                    return;
                }
                _calculationPeriod.SelectHistoryBatch(selected.Id);
            };
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
            if (_bindingHistoryBatch || !_historySourceEventsReady || !IsHistorySourceSelected())
                return;
            SyncHistoryPeriodFromControls();
            try
            {
                LoadHistoryBatches();
            }
            catch (Exception ex)
            {
                ClearFailedHistorySelection();
                MessageBox.Show("歷史批次清單讀取失敗。\n" + ex.Message);
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
            if (IsCurrentSourceSelected())
            {
                CalculationPeriodBinder.RestoreSystemPeriod(_nudCalcSchoolYear, _cboCalcSemester, _calculationPeriod, _DefaultSchoolYear, _DefaultSemester);
                _nudCalcSchoolYear.Enabled = false;
                _cboCalcSemester.Enabled = false;
                return;
            }
            if (IsExcelSourceSelected() || IsHistorySourceSelected())
            {
                _nudCalcSchoolYear.Enabled = true;
                _cboCalcSemester.Enabled = true;
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

        private void LoadHistoryBatches()
        {
            SyncHistoryPeriodFromControls();
            string previousId = _calculationPeriod == null ? "" : _calculationPeriod.HistoryBatchId;
            QueryHelper queryHelper = new QueryHelper();
            DataTable table = queryHelper.Select(SemesterHistoryBatchCatalog.SelectSql);
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
            if (labelX6 != null)
            {
                if (batches.Count == 0)
                    labelX6.Text = NoHistoryBatchText;
                else if (labelX6.Text == NoHistoryBatchText)
                    labelX6.Text = ScreenLoadReadyText;
            }
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

        private void BeginHistorySnapshotRestore()
        {
            HistoryRankBatch selected = _cboHistoryBatch == null ? null : _cboHistoryBatch.SelectedItem as HistoryRankBatch;
            if (selected == null)
            {
                ReleaseNextStep(true);
                MessageBox.Show("請選擇一筆歷史學期成績排名批次。");
                return;
            }

            SyncHistoryPeriodFromControls();
            if (selected.SchoolYear != _calculationPeriod.HistorySchoolYear || selected.Semester != _calculationPeriod.HistorySemester)
            {
                ReleaseNextStep(true);
                MessageBox.Show(SemesterHistoryPeriodGuard.MismatchMessage);
                return;
            }
            _calculationPeriod.RememberHistoryBatch(selected.Id, _calculationPeriod.HistorySchoolYear, _calculationPeriod.HistorySemester);
            string batchId = selected.Id;
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

            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
            bool settingFound = false;
            bool periodRejected = false;
            HistorySnapshotLoad load = null;
            HistoryRankLoad rankLoad = null;
            bkw.ProgressChanged += delegate (object sender, ProgressChangedEventArgs args)
            {
                string stage = args.UserState as string;
                if (IsDisposed || string.IsNullOrEmpty(stage))
                    return;
                labelX6.Text = stage;
            };
            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(0, "正在讀取歷史批次及母群資料...");
                    QueryHelper queryHelper = new QueryHelper();
                    DataTable periodTable = queryHelper.Select(SemesterHistoryPeriodGuard.SelectSql(batchId));
                    if (!SemesterHistoryPeriodGuard.Matches(periodTable, batchId, historyYear, historySemester))
                    {
                        periodRejected = true;
                        return;
                    }
                    DataTable table = queryHelper.Select(HistorySnapshotRestore.SelectSettingSql(batchId));
                    string settingXml;
                    string extensionJson;
                    settingFound = HistorySnapshotRestore.TryReadBatch(table, batchId, out settingXml, out extensionJson);
                    if (!settingFound)
                        return;
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
                    load = HistorySnapshotRestore.LoadSemester(batchId, extensionJson, settingXml, excludedCategory, tagDefinitions, links);
                    if (load.Restored || load.Message != HistorySnapshotRestore.MissingSnapshotMessage)
                        return;
                    bkw.ReportProgress(0, "正在讀取歷史批次及母群資料...");
                    DataTable rankTable = queryHelper.Select(HistoryRankRestore.SelectBatchStudentSql(batchId));
                    rankLoad = HistoryRankRestore.ReadBatchStudents(batchId, rankTable, excludedCategory, tagDefinitions, links);
                    SemesterHistoryStudentParticipation.KeepYearDeptClassParticipating(rankLoad);
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
                if (periodRejected)
                {
                    ClearLoadedConfirmList();
                    btnPrevious.Enabled = true;
                    ReleaseNextStep(true);
                    MessageBox.Show(SemesterHistoryPeriodGuard.MismatchMessage);
                    return;
                }
                if (bkwException != null || !settingFound)
                {
                    if (!settingFound)
                        ClearFailedHistorySelection();
                    ClearLoadedConfirmList();
                    btnPrevious.Enabled = true;
                    ReleaseNextStep(true);
                    if (bkwException != null)
                        MessageBox.Show("歷史批次設定讀取失敗。\n" + bkwException.Message);
                    else
                        MessageBox.Show("找不到所選的歷史批次，無法讀取設定。本次不會改用目前學生資料。");
                    return;
                }
                if (load != null && load.Restored)
                {
                    try
                    {
                        plStudentView.Visible = true;
                        lbSchoolYear.Text = calculationSchoolYear;
                        lbSemester.Text = calculationSemester;
                        PublishConfirmRequest();
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
                    ClearLoadedConfirmList();
                    btnPrevious.Enabled = true;
                    ReleaseNextStep(true);
                    MessageBox.Show(load == null ? HistorySnapshotRestore.UnreadableSnapshotMessage : load.Message);
                    return;
                }
                if (rankLoad == null || !rankLoad.Restored)
                {
                    ClearLoadedConfirmList();
                    btnPrevious.Enabled = true;
                    ReleaseNextStep(true);
                    MessageBox.Show(rankLoad == null ? HistoryRankRestore.EmptyBatchMessage : rankLoad.Message);
                    return;
                }

                try
                {
                    plStudentView.Visible = true;
                    lbSchoolYear.Text = calculationSchoolYear;
                    lbSemester.Text = calculationSemester;
                    PublishConfirmRequest();
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
            bkw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                ReleaseNextStep(true);
                MessageBox.Show("歷史批次設定讀取失敗。\n" + ex.Message);
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
