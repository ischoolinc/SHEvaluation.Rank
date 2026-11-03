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
                MessageBox.Show("計算排名失敗。\n" + rankStartException.Message);
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
                try
                {
                    bkw.ReportProgress(1);

                    QueryHelper queryHelper = new QueryHelper();

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

                    //2022-04 Cynthia 新增勾選「擇優採計成績」功能
                        string schoolYearAdjScore = "";
                        string handAdjScore = "";
                        string makeUpScore = "";
                        string RetakeScore = "";
                        if (gradeSelectCopy.Contains("學年調整成績"))
                            schoolYearAdjScore = ", NULLIF(array_to_string(xpath('/root/Subject/@學年調整成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";
                        if (gradeSelectCopy.Contains("手動調整成績"))
                            handAdjScore = ", NULLIF(array_to_string(xpath('/root/Subject/@擇優採計成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";
                        if (gradeSelectCopy.Contains("補考成績"))
                            makeUpScore = ", NULLIF(array_to_string(xpath('/root/Subject/@補考成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";
                        if (gradeSelectCopy.Contains("重修成績"))
                            RetakeScore = ", NULLIF(array_to_string(xpath('/root/Subject/@重修成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL";
                        #region 計算排名的SQL 新增新五標、標準差
                    string insertRankSql = @"
, subject_score AS
(
	SELECT
		calc_condition.rank_school_year
		, calc_condition.rank_semester
		, student_list.student_id
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, calc_condition.ref_exam_id::BIGINT AS exam_id
		, array_to_string(xpath('/root/Subject/@科目', xmlparse(content  concat('<root>', subj_score_ele , '</root>'))), '')::TEXT As subject
		, NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_origin_score
		, GREATEST
		(
			NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL"
+ schoolYearAdjScore + handAdjScore + makeUpScore + RetakeScore
                        //, NULLIF(array_to_string(xpath('/root/Subject/@學年調整成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
                        //, NULLIF(array_to_string(xpath('/root/Subject/@擇優採計成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
                        //, NULLIF(array_to_string(xpath('/root/Subject/@補考成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
                        //, NULLIF(array_to_string(xpath('/root/Subject/@重修成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''), '')::DECIMAL
                        + @"
		) AS subject_greatest_score
	FROM 
	(
		SELECT 
			sems_subj_score.*
			, unnest(xpath('/root/SemesterSubjectScoreInfo/Subject', xmlparse(content concat('<root>', score_info , '</root>') ))) AS subj_score_ele
		FROM 
			sems_subj_score
	) AS sems_subj_score_ext
	INNER JOIN student_list
		ON sems_subj_score_ext.ref_student_id = student_list.student_id
	INNER JOIN calc_condition
		ON sems_subj_score_ext.school_year = calc_condition.rank_school_year::INT
		AND sems_subj_score_ext.semester = calc_condition.rank_semester::INT
		AND student_list.rank_grade_year = calc_condition.rank_grade_year::INT
)
, subj_origin_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, subject::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, subject_origin_score AS origin_score
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score DESC) AS grade_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score DESC) AS dept_origin_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score DESC) AS class_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score DESC) AS tag1_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score DESC) AS tag2_origin_rank
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score ASC) AS grade_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score ASC) AS dept_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score ASC) AS class_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score ASC) AS tag1_origin_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score ASC) AS tag2_origin_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_origin_score ASC) AS grade_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_origin_score ASC) AS dept_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_origin_score ASC) AS class_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_origin_score ASC) AS tag1_origin_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_origin_score ASC) AS tag2_origin_row_number
	FROM
		subject_score
	WHERE
		subject IS NOT NULL
		AND subject_origin_score IS NOT NULL
)
, subj_greatest_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, subject::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, subject_greatest_score AS greatest_score
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score ASC) AS dept_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score ASC) AS tag1_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score ASC) AS tag2_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, subject ORDER BY subject_greatest_score ASC) AS grade_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY subject_greatest_score ASC) AS dept_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_class_name, subject ORDER BY subject_greatest_score ASC) AS class_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY subject_greatest_score ASC) AS tag1_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY subject_greatest_score ASC) AS tag2_row_number
	FROM
		subject_score
	WHERE
		subject IS NOT NULL
		AND subject_greatest_score IS NOT NULL
)
, subj_origin_rank_expand AS
(
	SELECT
		subj_origin_rank.*
		, FLOOR((grade_origin_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_origin_percentage
		, FLOOR((dept_origin_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_origin_percentage
		, FLOOR((class_origin_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_origin_percentage
		, FLOOR((tag1_origin_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_origin_percentage
		, FLOOR((tag2_origin_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_origin_percentage
		, FLOOR((grade_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count)  AS graderank_origin_pr
		, FLOOR((dept_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count)  AS deptrank_origin_pr
		, FLOOR((class_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count)  AS classrank_origin_pr
		, FLOOR((tag1_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count)  AS tag1rank_origin_pr
		, FLOOR((tag2_origin_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count)  AS tag2rank_origin_pr
	FROM
		subj_origin_rank
)
, subj_greatest_rank_expand AS
(
	SELECT
		subj_greatest_rank.*
		, FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
		, FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
		, FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
		, FLOOR((grade_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count) AS graderank_pr
		, FLOOR((dept_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count) AS deptrank_pr
		, FLOOR((class_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count) AS classrank_pr
		, FLOOR((tag1_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count) AS tag1rank_pr
		, FLOOR((tag2_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count) AS tag2rank_pr
	FROM
		subj_greatest_rank
)
, entry_score_list AS
(
	SELECT
		calc_condition.rank_school_year
		, calc_condition.rank_semester
		, student_list.student_id
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, calc_condition.ref_exam_id::BIGINT AS exam_id
		, array_to_string(xpath('/root/Entry/@分項', xmlparse(content  concat('<root>', entry_score_ele , '</root>'))),'')::TEXT As entry
		, NULLIF(array_to_string(xpath('/root/Entry/@成績', xmlparse(content   concat('<root>', entry_score_ele , '</root>'))), ''),'')::DECIMAL AS entry_score
	FROM
	(
		SELECT
			sems_entry_score.*
			, unnest(xpath('/root/SemesterEntryScore/Entry', xmlparse(content  concat('<root>', sems_entry_score.score_info , '</root>') ))) As entry_score_ele
		FROM
		sems_entry_score
	) AS sems_entry_score_ext
	INNER JOIN student_list
		ON sems_entry_score_ext.ref_student_id = student_list.student_id
	INNER JOIN calc_condition
		ON sems_entry_score_ext.school_year = calc_condition.rank_school_year::INT
		AND sems_entry_score_ext.semester = calc_condition.rank_semester::INT
		AND student_list.rank_grade_year = calc_condition.rank_grade_year::INT
)
, entry_rank_list AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, exam_id
		, entry::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, entry_score
		, RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score ASC) AS dept_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score ASC) AS tag1_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score ASC) AS tag2_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, entry) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, entry) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, entry) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, entry) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, entry) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score ASC) AS grade_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score ASC) AS dept_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score ASC) AS class_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score ASC) AS tag1_row_number
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score ASC) AS tag2_row_number
	FROM
		entry_score_list
	WHERE
		entry IS NOT NULL
		AND entry_score IS NOT NULL
)
, entry_rank_expand AS
(
	SELECT
		entry_rank_list.*
		, FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
		, FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
		, FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL - 1)*100::DECIMAL / grade_count)  AS graderank_pr
        , FLOOR((dept_rank_reverse::DECIMAL - 1)*100::DECIMAL / dept_count)  AS deptrank_pr
        , FLOOR((class_rank_reverse::DECIMAL - 1)*100::DECIMAL / class_count)  AS classrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag1_count)  AS tag1rank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL - 1)*100::DECIMAL / tag2_count)  AS tag2rank_pr
	FROM
		entry_rank_list
)
, score_list AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_origin_row_number OR grade_count*88/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_origin_row_number OR grade_count*75/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_origin_row_number OR grade_count*50/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_origin_row_number OR grade_count*25/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_origin_row_number OR grade_count*12/100+1 = grade_origin_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE grade_origin_rank *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE grade_origin_rank *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (grade_count - grade_origin_rank + 1) *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (grade_count - grade_origin_rank + 1) *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, grade_origin_rank AS rank
        , graderank_origin_pr AS pr
		, graderank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_origin_row_number OR dept_count*88/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_origin_row_number OR dept_count*75/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_origin_row_number OR dept_count*50/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_origin_row_number OR dept_count*25/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_origin_row_number OR dept_count*12/100+1 = dept_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE dept_origin_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE dept_origin_rank * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( dept_count - dept_origin_rank +1) * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( dept_count - dept_origin_rank +1) * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, dept_origin_rank AS rank
        , deptrank_origin_pr AS pr
		, deptrank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*88/100=class_origin_row_number OR class_count*88/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*75/100=class_origin_row_number OR class_count*75/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*50/100=class_origin_row_number OR class_count*50/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*25/100=class_origin_row_number OR class_count*25/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE class_count*12/100=class_origin_row_number OR class_count*12/100+1 = class_origin_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE class_origin_rank *4 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE class_origin_rank *2 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (class_count -class_origin_rank +1) *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (class_count -class_origin_rank +1) *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, class_origin_rank AS rank
        , classrank_origin_pr AS pr
		, classrank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_origin_row_number OR tag1_count*88/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_origin_row_number OR tag1_count*75/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_origin_row_number OR tag1_count*50/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_origin_row_number OR tag1_count*25/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_origin_row_number OR tag1_count*12/100+1 = tag1_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag1_origin_rank *4 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag1_origin_rank *2 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE (tag1_count -tag1_origin_rank +1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE (tag1_count -tag1_origin_rank +1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, tag1_origin_rank AS rank
        , tag1rank_origin_pr AS pr
		, tag1rank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_origin_row_number OR tag2_count*88/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_origin_row_number OR tag2_count*75/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_origin_row_number OR tag2_count*50/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_origin_row_number OR tag2_count*25/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(origin_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_origin_row_number OR tag2_count*12/100+1 = tag2_origin_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_12
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag2_origin_rank *4 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(origin_score::DECIMAL) FILTER(WHERE tag2_origin_rank *2 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(origin_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( tag2_count - tag2_origin_rank +1) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(origin_score::DECIMAL) FILTER(WHERE ( tag2_count - tag2_origin_rank +1) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=origin_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=origin_score AND origin_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=origin_score AND origin_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=origin_score AND origin_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=origin_score AND origin_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=origin_score AND origin_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=origin_score AND origin_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=origin_score AND origin_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=origin_score AND origin_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=origin_score AND origin_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE origin_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, origin_score AS score
		, tag2_origin_rank AS rank
        , tag2rank_origin_pr AS pr
		, tag2rank_origin_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_origin_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE grade_rank * 4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE grade_rank * 2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (grade_count - grade_rank +1) * 2 <= grade_count) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (grade_count - grade_rank +1) * 4 <= grade_count) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE dept_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE dept_rank * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (dept_count - dept_rank + 1) * 2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (dept_count - dept_rank + 1) * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE class_rank *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE class_rank *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (class_count - class_rank +1) * 2  <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (class_count - class_rank +1) * 4  <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag1_rank * 4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag1_rank * 2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/科目成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(greatest_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_12
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag2_rank *4 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(greatest_score::DECIMAL) FILTER(WHERE tag2_rank *2 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(greatest_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (tag2_count - tag2_rank+1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(greatest_score::DECIMAL) FILTER(WHERE (tag2_count - tag2_rank+1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=greatest_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=greatest_score AND greatest_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=greatest_score AND greatest_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=greatest_score AND greatest_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=greatest_score AND greatest_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=greatest_score AND greatest_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=greatest_score AND greatest_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=greatest_score AND greatest_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=greatest_score AND greatest_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=greatest_score AND greatest_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE greatest_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, greatest_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subj_greatest_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank *4  <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank *2  <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (grade_count -grade_rank +1 )*2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (grade_count -grade_rank +1 )*4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank * 4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank  *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 )*2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count- dept_rank +1 )*4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE  class_rank *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE  class_rank *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 ) *2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 ) *4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag1_count - tag1_rank + 1 ) *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag1_count - tag1_rank + 1 ) *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
		AND rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績(原始)'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, REPLACE(item_name, '(原始)', '') AS item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *4 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *2 <=  tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name LIKE '%(原始)%'
		AND rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank  *4 <=  grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE grade_rank  *2 <=  grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( grade_count -grade_rank +1 ) *2 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( grade_count -grade_rank +1 ) *4 <= grade_count ) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, grade_rank AS rank
        , graderank_pr AS pr
		, graderank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank *4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE dept_rank *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 ) *2 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (dept_count -dept_rank +1 ) *4 <= dept_count ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, dept_rank AS rank
        , deptrank_pr AS pr
		, deptrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_class_name, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	        ELSE AVG(entry_score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_name)::DECIMAL 
	      END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE class_rank  *4 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE class_rank  *2 <=  class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 )*2 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (class_count -class_rank +1 )*4 <= class_count ) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_class_name, item_name)AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, class_rank AS rank
        , classrank_pr AS pr
		, classrank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name)::DECIMAL 
	    END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag1_rank *2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE ( tag1_count - tag1_rank +1 ) * 2 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE  (tag1_count - tag1_rank +1 ) * 4 <= tag1_count ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag1_rank AS rank
        , tag1rank_pr AS pr
		, tag1rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
		AND rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	
	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, '學期/分項成績'::TEXT AS item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	      THEN AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	      ELSE AVG(entry_score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name)::DECIMAL 
	    END pr_12
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *4 <=  tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(entry_score::DECIMAL) FILTER(WHERE tag2_rank  *2 <=  tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(entry_score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *2 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(entry_score::DECIMAL) FILTER(WHERE (tag2_count -tag2_rank +1 ) *4 <= tag2_count ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL<=entry_score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100 
		, COUNT(*) FILTER(WHERE 90::DECIMAL<=entry_score AND entry_score <100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL<=entry_score AND entry_score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL<=entry_score AND entry_score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL<=entry_score AND entry_score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL<=entry_score AND entry_score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL<=entry_score AND entry_score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL<=entry_score AND entry_score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL<=entry_score AND entry_score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL<=entry_score AND entry_score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE entry_score<10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10 
		, student_id
		, entry_score AS score
		, tag2_rank AS rank
        , tag2rank_pr AS pr
		, tag2rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		entry_rank_expand
	WHERE
		item_name NOT LIKE '%(原始)%'
		AND rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
		 
)
, update_data AS
(
	UPDATE
		rank_matrix
	SET
		is_alive = NULL
	FROM
		calc_condition
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = calc_condition.rank_school_year::INT
		AND rank_matrix.semester = calc_condition.rank_semester::INT
		AND rank_matrix.grade_year = calc_condition.rank_grade_year::INT
		AND rank_matrix.ref_exam_id = calc_condition.ref_exam_id::INT

	RETURNING rank_matrix.*
)
, insert_matrix_data AS
(
	INSERT INTO
		rank_matrix
		(
			ref_batch_id
			, school_year
			, semester
			, grade_year
			, item_type
			, ref_exam_id
			, item_name
			, rank_type
			, rank_name
			, is_alive
			, matrix_count
			, avg_top_25
			, avg_top_50
			, avg
			, avg_bottom_50
			, avg_bottom_25
			, level_gte100
			, level_90
			, level_80
			, level_70
			, level_60
			, level_50
			, level_40
			, level_30
			, level_20
			, level_10
			, level_lt10
			, pr_88
			, pr_75
			, pr_50
			, pr_25
			, pr_12
			, std_dev_pop
		)
		SELECT
			calc_condition.batch_id AS ref_batch_id
			, score_list.rank_school_year::INT
			, score_list.rank_semester::INT
			, score_list.rank_grade_year::INT
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count
			, score_list.avg_top_25
			, score_list.avg_top_50
			, score_list.avg
			, score_list.avg_bottom_50
			, score_list.avg_bottom_25
			, score_list.level_gte100
			, score_list.level_90
			, score_list.level_80
			, score_list.level_70
			, score_list.level_60
			, score_list.level_50
			, score_list.level_40
			, score_list.level_30
			, score_list.level_20
			, score_list.level_10
			, score_list.level_lt10
			, score_list.pr_88
			, score_list.pr_75
			, score_list.pr_50
			, score_list.pr_25
			, score_list.pr_12
			, score_list.std_dev_pop
		FROM
			score_list
			LEFT OUTER JOIN update_data
				ON update_data.id  < 0 --永遠為false，只是為了讓insert等待update執行完
			CROSS JOIN calc_condition
		GROUP BY
			calc_condition.batch_id
			, score_list.rank_school_year
			, score_list.rank_semester
			, score_list.rank_grade_year
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count
			, score_list.avg_top_25
			, score_list.avg_top_50
			, score_list.avg
			, score_list.avg_bottom_50
			, score_list.avg_bottom_25
			, score_list.level_gte100
			, score_list.level_90
			, score_list.level_80
			, score_list.level_70
			, score_list.level_60
			, score_list.level_50
			, score_list.level_40
			, score_list.level_30
			, score_list.level_20
			, score_list.level_10
			, score_list.level_lt10
			, score_list.pr_88
			, score_list.pr_75
			, score_list.pr_50
			, score_list.pr_25
			, score_list.pr_12
			, score_list.std_dev_pop

	RETURNING *
)
, insert_batch_student_data AS
(
	INSERT INTO
		rank_batch_student
		(
			ref_batch_id
			, ref_student_id
			, grade_year
			, matrix_grade
			, matrix_class
			, matrix_tag1
			, matrix_tag2
		)
		SELECT
			calc_condition.batch_id AS ref_batch_id
			, score_list.student_id
			, score_list.rank_grade_year::INT
			, score_list.rank_grade_year||'年級' AS matrix_grade
			, score_list.rank_class_name
			, score_list.rank_tag1
			, score_list.rank_tag2
		FROM
			score_list
			CROSS JOIN calc_condition
)
, insert_detail_data AS
(
	INSERT INTO
		rank_detail
		(
			ref_matrix_id
			, ref_student_id
			, score
			, rank
            , pr
			, percentile
		)
		SELECT
			insert_matrix_data.id AS ref_matrix_id
			, score_list.student_id AS ref_student_id
			, score_list.score AS score
			, score_list.rank AS rank
            , score_list.pr AS pr
			, score_list.percentile AS percentile
		FROM
			score_list
			LEFT OUTER JOIN
				insert_matrix_data
					ON insert_matrix_data.school_year = score_list.rank_school_year::INT
					AND insert_matrix_data.semester = score_list.rank_semester::INT
					AND insert_matrix_data.grade_year = score_list.rank_grade_year::INT
					AND insert_matrix_data.item_type = score_list.item_type
					AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
					AND insert_matrix_data.item_name = score_list.item_name
					AND insert_matrix_data.rank_type = score_list.rank_type
					AND insert_matrix_data.rank_name = score_list.rank_name
)
SELECT count(*) FROM score_list


";


					#endregion


					//DataTable dt1 = queryHelper.Select(insertRankSql_Old);
					SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(rankStudents.Grades, schoolYear, semester, calculationSetting, extensionJson, insertRankSql, false);
                        insertRankSql = SemesterRankSql.ApplyParticipation(publish.Sql);
                    //// debug 
                    //string fiPath = Application.StartupPath + @"\sems_sql1.sql";
                    //using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                    //{
                    //    fi.WriteLine(insertRankSql);
                    //}

                    publishResult = queryHelper.Select(insertRankSql);
                        if (SemesterRankPublishSql.HasPublishedDetails(publishResult))
                            bkw.ReportProgress(100);
                        else
                            bkwException = new InvalidOperationException("排名明細沒有寫入。");
                }
                catch (Exception ex)
                {
                    bkwException = ex;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                try
                {
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
                MessageBox.Show("計算排名失敗。\n" + rankFlowException.Message);
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
