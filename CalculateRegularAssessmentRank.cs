using FISCA.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using K12.Data;
using FISCA.Data;
using FISCA.Presentation;
using System.Xml;
using System.Globalization;
using SHEvaluation.Rank.MotherGroup;
using SHEvaluation.Rank.RegularRank;

namespace SHEvaluation.Rank
{
    public partial class CalculateRegularAssessmentRank : BaseForm
    {
        string _DefaultSchoolYear = "";
        string _DefaultSemester = "";
        List<ExamRecord> _ExamList = new List<ExamRecord>();
        List<TagConfigRecord> _TagConfigRecord = new List<TagConfigRecord>();
        List<StudentRecord> _StudentRecord = new List<StudentRecord>();
        List<StudentRecord> _FilterStudentList = new List<StudentRecord>();

        XmlElement _ConfigElement = null;
        List<string> _FilteredSubject = new List<string>();
        List<string> _Tag1FilteredSubject = new List<string>();
        List<string> _Tag2FilteredSubject = new List<string>();
        RegularRankCalculationContext _rankContext;
        List<RegularRankExamChoice> _targetExamChoices = new List<RegularRankExamChoice>();
        List<RegularRankExamChoice> _sourceExamChoices = new List<RegularRankExamChoice>();
        bool _suspendRankEditors;
        bool _rankSubjectEventsAttached;
        List<ConfirmStudentRow> _loadedRows = new List<ConfirmStudentRow>();
        RegularRankMotherGroupConfirmForm _motherGroupConfirmForm;
        bool _motherGroupLoadRunning;
        int _loadGeneration;
        internal string PublicationValidationError { get; private set; }
        internal int PopulationGeneration { get { return _loadGeneration; } }
        string _historyCatalogPeriod = "";
        Dictionary<string, RegularRankHistoryBatch> _historyBatches = new Dictionary<string, RegularRankHistoryBatch>();

        public CalculateRegularAssessmentRank()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            cboSourceBatch.SelectedIndexChanged += HistoryBatchChanged;
            try
            {
                _DefaultSchoolYear = K12.Data.School.DefaultSchoolYear;
                _DefaultSemester = K12.Data.School.DefaultSemester;
                _ExamList = K12.Data.Exam.SelectAll();
                _TagConfigRecord = K12.Data.TagConfig.SelectByCategory(TagCategory.Student);
                _StudentRecord = K12.Data.Student.SelectAll().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("資料讀取失敗", ex);
            }

            string configString = K12.Data.School.Configuration["固定排名_定期評量排名計算"]["設定檔"];
            if (configString != "")
            {
                //< Setting 考試名稱 = "lbExam.Text" 不排名學生類別 = "cboStudentFilter.Text" 類別一 = "cboStudentTag1.Text" 類別二 = "cboStudentTag2.Text" >
                //    < 不採計科目 > OO </ 不採計科目 >
                //    < 不採計科目 > OO </ 不採計科目 >
                //    < 不採計科目 > OO </ 不採計科目 >
                //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                //</ Setting >
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(configString);
                _ConfigElement = doc.DocumentElement;
                foreach (XmlElement item in _ConfigElement.SelectNodes("不採計科目"))
                {
                    _FilteredSubject.Add(item.InnerText);
                }
                foreach (XmlElement item in _ConfigElement.SelectNodes("類別一不採計科目"))
                {
                    _Tag1FilteredSubject.Add(item.InnerText);
                }
                foreach (XmlElement item in _ConfigElement.SelectNodes("類別二不採計科目"))
                {
                    _Tag2FilteredSubject.Add(item.InnerText);
                }
            }
        }

        private void CacluateRegularAssessmentRank_Load(object sender, EventArgs e)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || (Site != null && Site.DesignMode))
                return;

            #region 讓Form回到起始狀態
            plSetting.Visible = true;
            #endregion

            _suspendRankEditors = true;
            _rankContext = RegularRankCalculationContext.CreateDefault(_DefaultSchoolYear, _DefaultSemester);

            #region 篩選學生資料
            _StudentRecord = _StudentRecord.Where(
                x => !string.IsNullOrEmpty(x.RefClassID)
                && (x.Status == StudentRecord.StudentStatus.一般 || x.Status == StudentRecord.StudentStatus.延修)
                && x.Class.GradeYear != null
            ).ToList();
            #endregion

            #region 動態產生年級的CheckBox
            //整理年級的清單
            List<int> gradeList = _StudentRecord.Select(x => Convert.ToInt32(x.Class.GradeYear)).Distinct().OrderBy(x => x).ToList();
            for (int i = 0; i < gradeList.Count; i++)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.AutoSize = true;
                checkBox.Name = "ch" + gradeList[i];
                checkBox.TabIndex = 7 + i;
                checkBox.Text = "" + gradeList[i] + "年級";
                checkBox.UseVisualStyleBackColor = true;
                checkBox.Checked = true;
                checkBox.CheckedChanged += RegularPeriodSourceChanged;
                flowLayoutPanel1.Controls.Add(checkBox);
            }
            #endregion

            #region 填資料進ComboBox
            cboExamType.Items.Clear();
            cboSourceExam.Items.Clear();
            cboStudentFilter.Items.Clear();
            cboStudentTag1.Items.Clear();
            cboStudentTag2.Items.Clear();
            BindRankPeriodEditors();
            //cboSchoolYear.Items.Add(_DefaultSchoolYear);//加入預設的學年度
            //cboSemester.Items.Add(_DefaultSemester);//加入預設的學年度
            //foreach (DataRow row in _SchoolYearTable.Rows)
            //{
            //    #region 現階段先不用匯入其他學年度及學期
            //    //現階段先不用匯入其他學年度
            //    //if (!string.IsNullOrEmpty("" + row["school_year"]) && !cboSchoolYear.Items.Contains("" + row["school_year"]))
            //    //{
            //    //    cboSchoolYear.Items.Add("" + row["school_year"]);
            //    //} 

            //    //if (!string.IsNullOrEmpty("" + row["semester"]) && !cboSemester.Items.Contains("" + row["semester"]))
            //    //{
            //    //    cboSemester.Items.Add("" + row["semester"]);
            //    //}
            //    #endregion
            //}
            //cboSchoolYear.SelectedIndex = 0;

            //if (cboSemester.Items.Contains(_DefaultSemester))
            //{
            //    cboSemester.SelectedIndex = cboSemester.Items.IndexOf(_DefaultSemester);
            //}
            //else
            //{
            //    cboSemester.SelectedIndex = 0;
            //}

            cboStudentFilter.Items.Add("");
            cboStudentTag1.Items.Add("");
            cboStudentTag2.Items.Add("");

            int cboStudentFilterIndex = 0;
            int cboStudentTag1Index = 0;
            int cboStudentTag2Index = 0;
            int cboExamTypeIndex = 0;
            foreach (var item in _TagConfigRecord.Select(x => x.Prefix).Distinct())
            {
                if (!string.IsNullOrEmpty(item))
                {
                    var index1 = cboStudentFilter.Items.Add("[" + item + "]");
                    var index2 = cboStudentTag1.Items.Add("[" + item + "]");
                    var index3 = cboStudentTag2.Items.Add("[" + item + "]");
                    //< Setting 考試名稱 = "cboExamType.Text" 不排名學生類別 = "cboStudentFilter.Text" 類別一 = "cboStudentTag1.Text" 類別二 = "cboStudentTag2.Text" >
                    //    < 不採計科目 > OO </ 不採計科目 >
                    //    < 不採計科目 > OO </ 不採計科目 >
                    //    < 不採計科目 > OO </ 不採計科目 >
                    //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                    //    < 類別一不採計科目 > OO </ 類別一不採計科目 >
                    //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                    //    < 類別二不採計科目 > OO </ 類別二不採計科目 >
                    //</ Setting >
                    if (_ConfigElement != null && _ConfigElement.GetAttribute("不排名學生類別") == "[" + item + "]")
                        cboStudentFilterIndex = index1;
                    if (_ConfigElement != null && _ConfigElement.GetAttribute("類別一") == "[" + item + "]")
                        cboStudentTag1Index = index2;
                    if (_ConfigElement != null && _ConfigElement.GetAttribute("類別二") == "[" + item + "]")
                        cboStudentTag2Index = index3;
                }
            }
            foreach (string tagName in _TagConfigRecord.Where(x => string.IsNullOrEmpty(x.Prefix)).Select(x => x.Name).ToList())
            {
                var index1 = cboStudentFilter.Items.Add(tagName);
                var index2 = cboStudentTag1.Items.Add(tagName);
                var index3 = cboStudentTag2.Items.Add(tagName);
                if (_ConfigElement != null && _ConfigElement.GetAttribute("不排名學生類別") == tagName)
                    cboStudentFilterIndex = index1;
                if (_ConfigElement != null && _ConfigElement.GetAttribute("類別一") == tagName)
                    cboStudentTag1Index = index2;
                if (_ConfigElement != null && _ConfigElement.GetAttribute("類別二") == tagName)
                    cboStudentTag2Index = index3;
            }
            cboStudentFilter.SelectedIndex = cboStudentFilterIndex;
            cboStudentTag1.SelectedIndex = cboStudentTag1Index;
            cboStudentTag2.SelectedIndex = cboStudentTag2Index;

            _targetExamChoices = new List<RegularRankExamChoice>();
            _sourceExamChoices = new List<RegularRankExamChoice>();
            if (!_rankSubjectEventsAttached)
            {
                lvCalcSubject.ItemChecked += RankSubjectItemChecked;
                lvCalcSubjectTag1.ItemChecked += RankSubjectItemChecked;
                lvCalcSubjectTag2.ItemChecked += RankSubjectItemChecked;
                _rankSubjectEventsAttached = true;
            }
            foreach (ExamRecord item in _ExamList)
            {
                if (item == null)
                    continue;
                RegularRankExamChoice choice = new RegularRankExamChoice();
                choice.Id = item.ID;
                choice.Name = item.Name ?? "";
                _targetExamChoices.Add(choice);
                RegularRankExamChoice sourceChoice = new RegularRankExamChoice();
                sourceChoice.Id = item.ID;
                sourceChoice.Name = item.Name ?? "";
                _sourceExamChoices.Add(sourceChoice);
            }
            IList<RegularRankExamListItem> targetItems = RegularRankPeriodSourceEditor.BuildExamItems(_targetExamChoices);
            IList<RegularRankExamListItem> sourceItems = RegularRankPeriodSourceEditor.BuildExamItems(_sourceExamChoices);
            foreach (RegularRankExamListItem item in targetItems)
                cboExamType.Items.Add(item);
            foreach (RegularRankExamListItem item in sourceItems)
                cboSourceExam.Items.Add(item);
            string savedExamName = _ConfigElement == null ? "" : _ConfigElement.GetAttribute("考試名稱");
            cboExamTypeIndex = RegularRankPeriodSourceEditor.IndexOfUniqueName(targetItems, savedExamName);
            if (cboExamTypeIndex < 0 && targetItems.Count == 1)
                cboExamTypeIndex = 0;
            cboExamType.SelectedIndex = cboExamTypeIndex;
            _suspendRankEditors = false;
            RefreshRankPeriodSource(false);
            #endregion
        }

        private void BindRankPeriodEditors()
        {
            lblSystemPeriod.Text = "系統期間：" + (_DefaultSchoolYear ?? "") + "-" + (_DefaultSemester ?? "");
            lblCurrentSourceNote.Text = RegularRankPeriodSourceEditor.CurrentSourceNote;
            int yearNumber;
            if (int.TryParse(_rankContext.TargetSchoolYear, NumberStyles.None, CultureInfo.InvariantCulture, out yearNumber))
            {
                if (yearNumber < nudTargetSchoolYear.Minimum)
                    yearNumber = decimal.ToInt32(nudTargetSchoolYear.Minimum);
                if (yearNumber > nudTargetSchoolYear.Maximum)
                    yearNumber = decimal.ToInt32(nudTargetSchoolYear.Maximum);
                nudTargetSchoolYear.Value = yearNumber;
                nudSourceSchoolYear.Value = yearNumber;
            }
            if (cboTargetSemester.Items.Contains(_rankContext.TargetSemester))
                cboTargetSemester.SelectedItem = _rankContext.TargetSemester;
            if (cboSourceSemester.Items.Contains(_rankContext.TargetSemester))
                cboSourceSemester.SelectedItem = _rankContext.TargetSemester;
            else if (cboSourceSemester.Items.Count > 0)
                cboSourceSemester.SelectedIndex = 0;
            if (cboMotherGroupSource.Items.Count > 0)
                cboMotherGroupSource.SelectedIndex = 0;
        }

        private void RegularPeriodSourceChanged(object sender, EventArgs e)
        {
            if (_suspendRankEditors || _rankContext == null)
                return;
            if (_motherGroupLoadRunning)
                _loadGeneration++;
            RefreshRankPeriodSource(false);
        }

        private void btnLoadMotherGroup_Click(object sender, EventArgs e)
        {
            if (_motherGroupLoadRunning)
            {
                lblRankStatus.Text = "母群與科目正在載入，請稍候。";
                return;
            }
            RegularRankSourceKind kind;
            string ignored;
            if (!RegularRankPeriodSourceEditor.TryReadSource(cboMotherGroupSource.Text, out kind, out ignored))
                kind = RegularRankSourceKind.Current;
            string historyPeriod = nudSourceSchoolYear.Value.ToString(CultureInfo.InvariantCulture) + "-" + cboSourceSemester.Text;
            if (kind == RegularRankSourceKind.History && (_historyCatalogPeriod != historyPeriod || cboSourceBatch.SelectedItem == null))
            {
                LoadHistoryCatalog(historyPeriod);
                return;
            }
            byte[] excelContent = null;
            if (kind == RegularRankSourceKind.Excel)
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = "選擇定期評量排名母群";
                    dialog.Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls";
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    try
                    {
                        excelContent = System.IO.File.ReadAllBytes(dialog.FileName);
                        using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
                            txtExcelIdentity.Text = dialog.SafeFileName + "｜" + BitConverter.ToString(hash.ComputeHash(excelContent)).Replace("-", "");
                    }
                    catch (Exception ex)
                    {
                        lblRankStatus.Text = "Excel 讀取失敗：" + ex.Message;
                        return;
                    }
                }
            }
            string applyError = RefreshRankPeriodSource(false);
            if (applyError.Length > 0) return;
            RegularRankHistoryBatch history = null;
            RegularRankBatchListItem selectedBatch = cboSourceBatch.SelectedItem as RegularRankBatchListItem;
            if (kind == RegularRankSourceKind.History && (selectedBatch == null || !_historyBatches.TryGetValue(selectedBatch.Id, out history)))
            { lblRankStatus.Text = "請重新載入歷史批次。"; return; }
            if (history != null && (history.Exam == null || !history.Exam.Resolved || history.Exam.ExamId != _rankContext.SourceExamId))
            { lblRankStatus.Text = "來源考試與批次不符，請重新選擇歷史批次。"; return; }

            _loadGeneration++;
            int generation = _loadGeneration;
            RegularRankCalculationEntry.SetTestEvidence(null);
            _loadedRows = new List<ConfirmStudentRow>();
            _rankContext.MarkSubjectLoadFailed();
            ClearRankSubjectLists();
            _motherGroupLoadRunning = true;
            btnNext.Enabled = false;
            List<StudentRecord> students = new List<StudentRecord>(_StudentRecord);
            List<TagConfigRecord> tags = new List<TagConfigRecord>(_TagConfigRecord);
            List<int> grades = ReadSelectedGradeYears();
            string excluded = cboStudentFilter.Text;
            string tag1 = cboStudentTag1.Text;
            string tag2 = cboStudentTag2.Text;
            string schoolYear = _rankContext.TargetSchoolYear;
            string semester = _rankContext.TargetSemester;
            string examId = _rankContext.TargetExamId;
            lblRankStatus.Text = "正在載入母群與目標期間科目。";

            BackgroundWorker worker = new BackgroundWorker();
            RegularRankLoadOutcome outcome = new RegularRankLoadOutcome();
            worker.DoWork += delegate
            {
                outcome = kind != RegularRankSourceKind.Current
                    ? LoadExcelPopulation(excelContent, tags, grades, excluded, schoolYear, semester, examId, history)
                    : LoadCurrentPopulation(students, tags, grades, excluded, tag1, tag2, schoolYear, semester, examId);
                if (outcome.Error.Length == 0)
                {
                    try { outcome.DatabaseEvidence = RegularRankPublicationService.VerifyTestDatabase(new QueryHelper()); }
                    catch (Exception ex) { outcome.ValidationError = "排名發布檢查未通過：" + RegularRankPublicationService.ValidationFailure(ex); }
                }
            };
            worker.RunWorkerCompleted += delegate
            {
                _motherGroupLoadRunning = false;
                if (IsDisposed)
                    return;
                if (generation != _loadGeneration)
                {
                    _loadedRows = new List<ConfirmStudentRow>();
                    _rankContext.MarkSubjectLoadFailed();
                    ClearRankSubjectLists();
                    btnNext.Enabled = false;
                    lblRankStatus.Text = "載入期間條件已變更，這次結果沒有採用。請重新載入。";
                    return;
                }
                ApplyLoadOutcome(outcome);
                if (outcome != null && outcome.Error.Length == 0 && outcome.ValidationError.Length > 0)
                    lblRankStatus.Text += "；" + outcome.ValidationError;
                if (outcome != null && outcome.Error.Length == 0 && outcome.Warning.Length > 0)
                    MessageBox.Show(this, outcome.Warning, "來源載入提醒");
            };
            worker.RunWorkerAsync();
        }

        private void RejectDeferredSource()
        {
            _loadedRows = new List<ConfirmStudentRow>();
            if (_rankContext != null)
                _rankContext.MarkSubjectLoadFailed();
            ClearRankSubjectLists();
            btnNext.Enabled = false;
            lblRankStatus.Text = RegularRankPeriodSourceEditor.DeferredSourceMessage;
        }

        private static RegularRankLoadOutcome LoadExcelPopulation(byte[] content,
            List<TagConfigRecord> tags, List<int> grades, string excluded,
            string schoolYear, string semester, string examId, RegularRankHistoryBatch history)
        {
            RegularRankLoadOutcome outcome = new RegularRankLoadOutcome();
            try
            {
                List<TagDefinition> definitions = new List<TagDefinition>();
                foreach (TagConfigRecord tag in tags)
                    definitions.Add(new TagDefinition { Id = tag.ID, Prefix = tag.Prefix, Name = tag.Name });
                List<StudentTagLink> links = new List<StudentTagLink>();
                foreach (StudentTagRecord tag in StudentTag.SelectAll())
                    links.Add(new StudentTagLink { StudentId = tag.RefStudentID, TagId = tag.RefTagID, Name = tag.Name });
                QueryHelper query = new QueryHelper();
                if (history != null)
                {
                    DataTable latest = query.Select(HistorySnapshotRestore.SelectSettingSql(history.Id));
                    string xml, extension;
                    if (!HistorySnapshotRestore.TryReadBatch(latest, history.Id, out xml, out extension)
                        || xml != history.Setting || extension != history.Extension)
                        throw new InvalidOperationException("歷史批次已變更，請重新載入批次目錄。");
                    outcome.Rows = RegularRankHistorySource.Restore(history, delegate(string sql) { return query.Select(sql); },
                        excluded, definitions, links, out outcome.Warning);
                }
                else using (System.IO.MemoryStream stream = new System.IO.MemoryStream(content, false))
                    outcome.Rows = RegularRankExcelSource.Load(new Aspose.Cells.Workbook(stream),
                        delegate(string sql) { return query.Select(sql); }, excluded, definitions, links, out outcome.Warning);
                List<string> ids = new List<string>();
                foreach (ConfirmStudentRow row in outcome.Rows)
                {
                    int grade;
                    if (SemesterRankSql.TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out grade)
                        && grades.Contains(grade)) ids.Add(row.Group.StudentId);
                }
                string subjectSql, error;
                if (!RegularRankSubjectCatalog.TryCreate(schoolYear, semester, examId, ids, out subjectSql, out error))
                    throw new InvalidOperationException(error);
                outcome.Subjects = new List<string>();
                foreach (DataRow row in query.Select(subjectSql).Rows)
                {
                    string name = ("" + row["subject"]).Trim();
                    if (name.Length > 0 && !outcome.Subjects.Contains(name)) outcome.Subjects.Add(name);
                }
            }
            catch (Exception ex) { outcome.Error = "Excel 母群或科目載入失敗：" + ex.Message; }
            return outcome;
        }

        private void LoadHistoryCatalog(string period)
        {
            _motherGroupLoadRunning = true;
            btnNext.Enabled = false;
            int generation = ++_loadGeneration;
            string year = nudSourceSchoolYear.Value.ToString(CultureInfo.InvariantCulture);
            string term = cboSourceSemester.Text;
            List<RegularRankExamChoice> exams = new List<RegularRankExamChoice>(_sourceExamChoices);
            List<RegularRankHistoryBatch> batches = null;
            BackgroundWorker worker = new BackgroundWorker();
            lblRankStatus.Text = "正在查詢歷史批次目錄。";
            worker.DoWork += delegate { batches = RegularRankHistorySource.ReadCatalog(new QueryHelper().Select(RegularRankHistorySource.CatalogSql(year, term)), exams); };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                _motherGroupLoadRunning = false;
                if (IsDisposed || generation != _loadGeneration) return;
                _suspendRankEditors = true;
                cboSourceBatch.Items.Clear();
                _historyBatches.Clear();
                _historyCatalogPeriod = "";
                if (e.Error == null && batches != null)
                {
                    foreach (RegularRankHistoryBatch batch in batches)
                    {
                        _historyBatches.Add(batch.Id, batch);
                        cboSourceBatch.Items.Add(new RegularRankBatchListItem(batch.Id, batch.ToString()));
                    }
                    _historyCatalogPeriod = period;
                }
                _suspendRankEditors = false;
                FitSourceBatchDropDown();
                lblRankStatus.Text = e.Error != null ? "歷史目錄載入失敗：" + e.Error.Message
                    : "找到 " + cboSourceBatch.Items.Count + " 個定期批次，請選批次後再按載入。";
                ApplyNextButtonState("");
            };
            worker.RunWorkerAsync();
        }

        private void HistoryBatchChanged(object sender, EventArgs e)
        {
            if (_suspendRankEditors) return;
            RegularRankBatchListItem item = cboSourceBatch.SelectedItem as RegularRankBatchListItem;
            RegularRankHistoryBatch batch;
            if (item == null || !_historyBatches.TryGetValue(item.Id, out batch)) return;
            _suspendRankEditors = true;
            cboSourceExam.SelectedIndex = -1;
            if (batch.Exam != null && batch.Exam.Resolved)
                for (int i = 0; i < cboSourceExam.Items.Count; i++)
                    if (((RegularRankExamListItem)cboSourceExam.Items[i]).Id == batch.Exam.ExamId) { cboSourceExam.SelectedIndex = i; break; }
            _suspendRankEditors = false;
            RefreshRankPeriodSource(false);
            if (batch.Error.Length > 0) lblRankStatus.Text = batch.Error;
        }

        private static RegularRankLoadOutcome LoadCurrentPopulation(
            List<StudentRecord> students,
            List<TagConfigRecord> tags,
            List<int> grades,
            string excluded,
            string tag1,
            string tag2,
            string schoolYear,
            string semester,
            string examId)
        {
            RegularRankLoadOutcome outcome = new RegularRankLoadOutcome();
            try
            {
                List<CurrentStudentRow> rows = new List<CurrentStudentRow>();
                Dictionary<string, string> seats = new Dictionary<string, string>();
                foreach (StudentRecord student in students)
                {
                    if (student == null)
                        continue;
                    int? gradeYear = null;
                    string className = "";
                    if (student.Class != null)
                    {
                        gradeYear = student.Class.GradeYear;
                        className = student.Class.Name;
                    }
                    int? status = null;
                    if (student.Status == StudentRecord.StudentStatus.一般)
                        status = 1;
                    else if (student.Status == StudentRecord.StudentStatus.延修)
                        status = RegularRankCurrentStudentSource.ExtendedStatus;
                    rows.Add(new CurrentStudentRow
                    {
                        StudentId = student.ID,
                        StudentNumber = student.StudentNumber,
                        StudentName = student.Name,
                        IsRegular = student.Status == StudentRecord.StudentStatus.一般,
                        StatusValue = status,
                        RefClassId = student.RefClassID,
                        GradeYear = gradeYear,
                        ClassName = className
                    });
                    if (!string.IsNullOrEmpty(student.ID))
                        seats[student.ID] = Convert.ToString(student.SeatNo) ?? "";
                }

                List<TagDefinition> definitions = new List<TagDefinition>();
                foreach (TagConfigRecord tag in tags)
                {
                    if (tag == null)
                        continue;
                    definitions.Add(new TagDefinition { Id = tag.ID, Prefix = tag.Prefix, Name = tag.Name });
                }
                List<StudentTagLink> links = new List<StudentTagLink>();
                foreach (StudentTagRecord tag in StudentTag.SelectAll())
                {
                    if (tag == null)
                        continue;
                    links.Add(new StudentTagLink { StudentId = tag.RefStudentID, TagId = tag.RefTagID, Name = tag.Name });
                }

                List<string> eligibleIds = new List<string>();
                foreach (CurrentStudentRow row in rows)
                {
                    if (!RegularRankCurrentStudentSource.IsEligible(row) || grades == null || !grades.Contains(row.GradeYear.Value))
                        continue;
                    eligibleIds.Add(row.StudentId);
                }
                Dictionary<string, string> deptNames = new Dictionary<string, string>();
                if (eligibleIds.Count > 0)
                {
                    string deptSql = CurrentStudentDeptQuery.Create(eligibleIds);
                    foreach (string studentId in eligibleIds)
                    {
                        long ignoredId;
                        if (!long.TryParse(studentId, out ignoredId) || ignoredId <= 0)
                        {
                            outcome.Error = "學生識別不是正整數，不能查科別。";
                            return outcome;
                        }
                    }
                    DataTable table = new QueryHelper().Select(deptSql);
                    foreach (DataRow dataRow in table.Rows)
                        deptNames["" + dataRow["student_id"]] = "" + dataRow["deptname"];
                }

                List<ConfirmStudentRow> confirmRows;
                string buildError = RegularRankCurrentStudentSource.Build(
                    rows, seats, definitions, links, grades, excluded, tag1, tag2, deptNames, out confirmRows);
                if (buildError.Length > 0)
                {
                    outcome.Error = buildError;
                    return outcome;
                }

                List<string> studentIds = new List<string>();
                foreach (ConfirmStudentRow row in confirmRows)
                    studentIds.Add(row.Group.StudentId);
                string subjectSql;
                string subjectError;
                if (!RegularRankSubjectCatalog.TryCreate(schoolYear, semester, examId, studentIds, out subjectSql, out subjectError))
                {
                    outcome.Error = subjectError;
                    return outcome;
                }
                DataTable subjects = new QueryHelper().Select(subjectSql);
                outcome.Rows = confirmRows;
                outcome.Subjects = new List<string>();
                foreach (DataRow dataRow in subjects.Rows)
                {
                    string name = ("" + dataRow["subject"]).Trim();
                    if (name.Length > 0 && !outcome.Subjects.Contains(name))
                        outcome.Subjects.Add(name);
                }
            }
            catch (Exception ex)
            {
                outcome.Error = "母群或科目查詢失敗：" + ex.Message;
            }
            return outcome;
        }

        private void ApplyLoadOutcome(RegularRankLoadOutcome outcome)
        {
            PublicationValidationError = outcome == null ? "尚未完成發布檢查。" : outcome.ValidationError;
            RegularRankCalculationEntry.SetTestEvidence(outcome == null ? null : outcome.DatabaseEvidence);
            if (outcome == null || outcome.Error.Length > 0 || outcome.Rows == null)
            {
                _loadedRows = new List<ConfirmStudentRow>();
                _rankContext.MarkSubjectLoadFailed();
                ClearRankSubjectLists();
                ApplyNextButtonState("");
                lblRankStatus.Text = outcome == null || outcome.Error.Length == 0 ? "母群或科目查詢失敗，不能用上一份名單。" : outcome.Error;
                return;
            }

            string marked = _rankContext.MarkMotherGroupLoaded();
            if (marked.Length > 0)
            {
                _loadedRows = new List<ConfirmStudentRow>();
                _rankContext.MarkSubjectLoadFailed();
                ClearRankSubjectLists();
                ApplyNextButtonState("");
                lblRankStatus.Text = marked;
                return;
            }

            string applied = _rankContext.ApplySubjectCandidates(
                outcome.Subjects, outcome.Subjects, outcome.Subjects,
                _FilteredSubject, _Tag1FilteredSubject, _Tag2FilteredSubject);
            _loadedRows = outcome.Rows;
            ShowRankSubjectLists();
            string changes = RegularRankPeriodSourceEditor.DescribeSubjectChanges(_rankContext);
            if (applied.Length > 0)
            {
                ApplyNextButtonState("");
                lblRankStatus.Text = applied;
                return;
            }
            if (outcome.Rows.Count == 0)
            {
                ApplyNextButtonState("");
                lblRankStatus.Text = "已載入" + LoadedSourceLabel() + " 0 人。沒有學生，不能進入下一步。";
                return;
            }
            ApplyNextButtonState("");
            lblRankStatus.Text = "已載入" + LoadedSourceLabel() + " " + outcome.Rows.Count + " 人、科目 " + outcome.Subjects.Count + " 科。可按下一步。"
                + (changes.Length > 0 ? changes : "");
        }

        private string LoadedSourceLabel()
        {
            if (_rankContext == null)
                return "母群";
            if (_rankContext.SourceKind == RegularRankSourceKind.Excel)
                return " Excel";
            return RegularRankPeriodSourceEditor.SourceText(_rankContext.SourceKind);
        }

        private void ShowRankSubjectLists()
        {
            _suspendRankEditors = true;
            FillRankSubjects(lvCalcSubject, _rankContext.GeneralSubjects);
            FillRankSubjects(lvCalcSubjectTag1, _rankContext.Tag1Subjects);
            FillRankSubjects(lvCalcSubjectTag2, _rankContext.Tag2Subjects);
            _suspendRankEditors = false;
        }

        private static void FillRankSubjects(ListView listView, RegularRankSubjectGroup group)
        {
            listView.Items.Clear();
            if (group == null)
                return;
            foreach (string name in group.Available)
            {
                ListViewItem item = listView.Items.Add(name);
                item.Checked = group.Checked.Contains(name);
            }
        }

        private string RefreshRankPeriodSource(bool keepSubjectLists)
        {
            if (_rankContext == null)
                return "沒有計算條件。";
            string error = RegularRankPeriodSourceEditor.Apply(_rankContext, ReadRankPeriodSelection());
            ApplyRankPeriodEnablement();
            if (_rankContext.LoadState != RegularRankLoadState.Loaded)
                _loadedRows = new List<ConfirmStudentRow>();
            if (!keepSubjectLists && (_rankContext.LoadState != RegularRankLoadState.Loaded || !_rankContext.SubjectsUsable))
                ClearRankSubjectLists();
            string changes = RegularRankPeriodSourceEditor.DescribeSubjectChanges(_rankContext);
            string requestError = _rankContext.CanCreateRequest();
            bool pendingConfirm = requestError == "尚未確認三組科目。";
            RegularRankSourceKind sourceKind;
            string sourceError;
            bool deferredSource = RegularRankPeriodSourceEditor.TryReadSource(cboMotherGroupSource.Text, out sourceKind, out sourceError)
                && sourceKind == RegularRankSourceKind.History && cboSourceBatch.SelectedItem == null;
            ApplyNextButtonState(error);
            if (deferredSource)
                lblRankStatus.Text = RegularRankPeriodSourceEditor.DeferredSourceMessage;
            else if (error.Length > 0)
                lblRankStatus.Text = error;
            else if (changes.Length > 0)
                lblRankStatus.Text = changes;
            else if (!pendingConfirm)
                lblRankStatus.Text = requestError;
            return error;
        }

        private void RankSubjectItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suspendRankEditors || _rankContext == null)
                return;
            if (_rankContext.LoadState != RegularRankLoadState.Loaded || !_rankContext.SubjectsUsable)
                return;
            string error = _rankContext.SetCheckedSubjects(RegularRankSubjectSet.General, ReadCheckedSubjects(lvCalcSubject));
            if (error.Length == 0)
                error = _rankContext.SetCheckedSubjects(RegularRankSubjectSet.Tag1, ReadCheckedSubjects(lvCalcSubjectTag1));
            if (error.Length == 0)
                error = _rankContext.SetCheckedSubjects(RegularRankSubjectSet.Tag2, ReadCheckedSubjects(lvCalcSubjectTag2));
            string changes = RegularRankPeriodSourceEditor.DescribeSubjectChanges(_rankContext);
            if (error.Length > 0)
                lblRankStatus.Text = error;
            else if (changes.Length > 0)
                lblRankStatus.Text = changes;
            ApplyNextButtonState(error);
        }

        private RegularRankPeriodSourceSelection ReadRankPeriodSelection()
        {
            RegularRankPeriodSourceSelection selection = new RegularRankPeriodSourceSelection();
            selection.TargetSchoolYear = decimal.ToInt32(nudTargetSchoolYear.Value).ToString(CultureInfo.InvariantCulture);
            selection.TargetSemester = cboTargetSemester.SelectedItem == null ? "" : cboTargetSemester.SelectedItem.ToString();
            RegularRankExamListItem targetExam = cboExamType.SelectedItem as RegularRankExamListItem;
            selection.TargetExamId = targetExam == null ? "" : targetExam.Id;
            selection.TargetExamName = targetExam == null ? "" : targetExam.Name;
            selection.TargetExamChoices = _targetExamChoices;
            selection.SourceText = cboMotherGroupSource.Text;
            selection.SourceSchoolYear = decimal.ToInt32(nudSourceSchoolYear.Value).ToString(CultureInfo.InvariantCulture);
            selection.SourceSemester = cboSourceSemester.SelectedItem == null ? "" : cboSourceSemester.SelectedItem.ToString();
            RegularRankBatchListItem batch = cboSourceBatch.SelectedItem as RegularRankBatchListItem;
            selection.SourceBatchId = batch == null ? "" : batch.Id;
            RegularRankExamListItem sourceExam = cboSourceExam.SelectedItem as RegularRankExamListItem;
            selection.SourceExamId = sourceExam == null ? "" : sourceExam.Id;
            selection.SourceExamName = sourceExam == null ? "" : sourceExam.Name;
            selection.SourceExamChoices = _sourceExamChoices;
            selection.ExcelIdentity = txtExcelIdentity.Text;
            selection.GradeYears = ReadSelectedGradeYears();
            selection.ExcludedCategory = cboStudentFilter.Text;
            selection.RankTag1 = cboStudentTag1.Text;
            selection.RankTag2 = cboStudentTag2.Text;
            return selection;
        }

        private List<int> ReadSelectedGradeYears()
        {
            List<int> grades = new List<int>();
            foreach (CheckBox checkBox in flowLayoutPanel1.Controls.OfType<CheckBox>())
            {
                if (!checkBox.Checked)
                    continue;
                int gradeYear;
                if (int.TryParse(checkBox.Text.Trim('年', '級'), NumberStyles.None, CultureInfo.InvariantCulture, out gradeYear))
                    grades.Add(gradeYear);
            }
            return grades;
        }

        private void ApplyRankPeriodEnablement()
        {
            RegularRankSourceKind kind;
            string ignored;
            if (!RegularRankPeriodSourceEditor.TryReadSource(cboMotherGroupSource.Text, out kind, out ignored))
                kind = RegularRankSourceKind.Current;
            RegularRankPeriodSourceEnablement enablement = RegularRankPeriodSourceEnablement.For(kind);
            _suspendRankEditors = true;
            nudTargetSchoolYear.Enabled = enablement.TargetSchoolYear;
            cboTargetSemester.Enabled = enablement.TargetSemester;
            nudSourceSchoolYear.Enabled = enablement.SourceSchoolYear;
            cboSourceSemester.Enabled = enablement.SourceSemester;
            cboSourceExam.Enabled = enablement.SourceExam;
            cboSourceBatch.Enabled = enablement.SourceBatch;
            txtExcelIdentity.Enabled = enablement.ExcelIdentity;
            lblCurrentSourceNote.Visible = enablement.CurrentSourceNote;
            _suspendRankEditors = false;
        }

        private void ClearRankSubjectLists()
        {
            _suspendRankEditors = true;
            lvCalcSubject.Items.Clear();
            lvCalcSubjectTag1.Items.Clear();
            lvCalcSubjectTag2.Items.Clear();
            _suspendRankEditors = false;
        }

        private static List<string> ReadCheckedSubjects(ListView listView)
        {
            List<string> names = new List<string>();
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Checked)
                    names.Add(item.Text);
            }
            return names;
        }

        private void reloadSubject(object eander, EventArgs e)
        {
            lblRankStatus.Text = "請使用「載入／更新母群及科目」。科目只依目標期間、目標 exam ID 與目前母群查詢，不再使用系統期間或同名考試的最後一筆。";
            btnNext.Enabled = false;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (_motherGroupLoadRunning)
            {
                lblRankStatus.Text = "母群與科目正在載入，請稍候。";
                btnNext.Enabled = false;
                return;
            }
            string blocked = RefreshRankPeriodSource(true);
            if (blocked.Length > 0 || !NextStepReady(blocked))
            {
                btnNext.Enabled = false;
                if (blocked.Length == 0)
                    lblRankStatus.Text = NextStepBlockedMessage();
                return;
            }

            string error = ApplyCheckedSubjectsFromLists();
            if (error.Length == 0)
                error = _rankContext.ConfirmSubjects();
            if (error.Length == 0)
                error = _rankContext.CanCreateRequest();
            if (error.Length > 0)
            {
                lblRankStatus.Text = error;
                btnNext.Enabled = false;
                return;
            }

            OpenMotherGroupConfirm();
        }

        private string ApplyCheckedSubjectsFromLists()
        {
            string error = _rankContext.SetCheckedSubjects(RegularRankSubjectSet.General, ReadCheckedSubjects(lvCalcSubject));
            if (error.Length == 0)
                error = _rankContext.SetCheckedSubjects(RegularRankSubjectSet.Tag1, ReadCheckedSubjects(lvCalcSubjectTag1));
            if (error.Length == 0)
                error = _rankContext.SetCheckedSubjects(RegularRankSubjectSet.Tag2, ReadCheckedSubjects(lvCalcSubjectTag2));
            return error;
        }

        private void ApplyNextButtonState(string applyError)
        {
            btnNext.Enabled = NextStepReady(applyError);
        }

        private bool NextStepReady(string applyError)
        {
            if (_motherGroupLoadRunning || _rankContext == null)
                return false;
            if (applyError != null && applyError.Length > 0)
                return false;
            if (_rankContext.LoadState != RegularRankLoadState.Loaded || !_rankContext.SubjectsUsable)
                return false;
            if (_loadedRows == null || _loadedRows.Count == 0)
                return false;
            if ((_rankContext.TargetExamId ?? "").Length == 0)
                return false;
            if (_rankContext.GradeYears == null || _rankContext.GradeYears.Count == 0)
                return false;
            RegularRankSourceKind screenKind;
            string screenError;
            if (!RegularRankPeriodSourceEditor.TryReadSource(cboMotherGroupSource.Text, out screenKind, out screenError))
                return false;
            if (screenKind != _rankContext.SourceKind)
                return false;
            if (screenKind == RegularRankSourceKind.History)
            {
                RegularRankBatchListItem batch = cboSourceBatch.SelectedItem as RegularRankBatchListItem;
                if (batch == null || batch.Id != (_rankContext.SourceBatchId ?? ""))
                    return false;
                if ((_rankContext.SourceExamId ?? "").Length == 0
                    || (_rankContext.SourceSchoolYear ?? "").Length == 0
                    || (_rankContext.SourceSemester ?? "").Length == 0)
                    return false;
            }
            if (screenKind == RegularRankSourceKind.Excel && (_rankContext.ExcelIdentity ?? "").Length == 0)
                return false;
            return true;
        }

        private string NextStepBlockedMessage()
        {
            if (_rankContext == null)
                return "沒有計算條件。";
            if (_motherGroupLoadRunning)
                return "母群與科目正在載入，請稍候。";
            if (_rankContext.LoadState == RegularRankLoadState.Failed)
                return "科目載入失敗，不能用上一份科目搭配新條件。";
            if (_rankContext.LoadState != RegularRankLoadState.Loaded)
                return "條件已變更，必須重新載入母群與科目。";
            if (_loadedRows == null || _loadedRows.Count == 0)
                return "尚未載入學生母群。";
            if (!_rankContext.SubjectsUsable)
                return "沒有可用科目，不能進入確認。";
            return "目標或來源條件尚未完成，不能進入下一步。";
        }

        private void FitSourceBatchDropDown()
        {
            List<string> displays = new List<string>();
            foreach (object item in cboSourceBatch.Items)
                displays.Add(item == null ? "" : item.ToString());
            int dpi = cboSourceBatch.DeviceDpi;
            cboSourceBatch.DropDownWidth = SourceBatchDropDownWidth(
                cboSourceBatch.Font,
                displays,
                cboSourceBatch.Width,
                dpi,
                SourceBatchAvailableDropDownWidth(dpi));
        }

        private int SourceBatchAvailableDropDownWidth(int dpi)
        {
            Screen screen = Screen.FromControl(cboSourceBatch);
            if (screen == null)
                return 0;
            int margin = Scale96(8, dpi);
            int room = screen.WorkingArea.Width - margin;
            try
            {
                Point origin = cboSourceBatch.PointToScreen(Point.Empty);
                int beside = screen.WorkingArea.Right - origin.X - margin;
                if (beside > 0)
                    room = beside;
            }
            catch (Exception)
            {
                return Math.Max(0, room);
            }
            return Math.Max(0, room);
        }

        internal static int SourceBatchDropDownWidth(Font font, IList<string> displays, int controlWidth, int dpi, int availableWidth)
        {
            if (dpi < 96)
                dpi = 96;
            int baseline = Scale96(300, dpi);
            int textWidth = 0;
            if (font != null && displays != null)
            {
                foreach (string display in displays)
                {
                    Size size = TextRenderer.MeasureText(
                        display ?? "",
                        font,
                        new Size(short.MaxValue, short.MaxValue),
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                    if (size.Width > textWidth)
                        textWidth = size.Width;
                }
            }
            int side = Scale96(8, dpi);
            int width = baseline;
            int measured = textWidth + SystemInformation.VerticalScrollBarWidth + side + side;
            if (measured > width)
                width = measured;
            if (controlWidth > width)
                width = controlWidth;
            if (availableWidth > 0 && width > availableWidth)
                width = availableWidth;
            if (width < 1)
                width = baseline;
            return width;
        }

        private static int Scale96(int pixels, int dpi)
        {
            if (dpi < 96)
                dpi = 96;
            return (pixels * dpi + 48) / 96;
        }

        private void OpenMotherGroupConfirm()
        {
            RegularRankSourceKind kind;
            string ignored;
            if (!RegularRankPeriodSourceEditor.TryReadSource(cboMotherGroupSource.Text, out kind, out ignored))
            {
                RejectDeferredSource();
                return;
            }
            if (_loadedRows == null || _loadedRows.Count == 0)
            {
                lblRankStatus.Text = "尚未載入學生母群。";
                btnNext.Enabled = false;
                return;
            }
            RegularRankConfirmRequest request;
            try
            {
                request = _rankContext.CreateRequest();
            }
            catch (InvalidOperationException ex)
            {
                lblRankStatus.Text = ex.Message;
                btnNext.Enabled = false;
                return;
            }
            if (_motherGroupConfirmForm == null || _motherGroupConfirmForm.IsDisposed)
                _motherGroupConfirmForm = new RegularRankMotherGroupConfirmForm(this);
            _motherGroupConfirmForm.ShowStudents(request, _loadedRows);
            if (_motherGroupConfirmForm.Owner != this)
                _motherGroupConfirmForm.Owner = this;
            Enabled = false;
            if (!_motherGroupConfirmForm.Visible)
                _motherGroupConfirmForm.Show(this);
            _motherGroupConfirmForm.BringToFront();
            _motherGroupConfirmForm.Activate();
        }

        internal void ReturnFromMotherGroupConfirm()
        {
            if (_motherGroupConfirmForm != null && !_motherGroupConfirmForm.IsDisposed)
                _motherGroupConfirmForm.HideForHost();
            Enabled = true;
            if (!Visible)
                Show();
            BringToFront();
            Activate();
        }

        internal void ReturnForExcelImport()
        {
            ReturnFromMotherGroupConfirm();
            cboMotherGroupSource.SelectedItem = RegularRankPeriodSourceEditor.ExcelSourceText;
            lblRankStatus.Text = "已選擇 Excel 名單，請按「載入／更新母群及科目」選擇檔案。";
        }

        internal void AddHistoryStudent(RegularRankMotherGroupConfirmForm confirm, string number)
        {
            if (_rankContext == null || _rankContext.SourceKind != RegularRankSourceKind.History || _motherGroupLoadRunning)
                return;
            MotherGroupConfirmList copy = confirm.List.CopyWorking();
            RegularRankConfirmRequest request = _rankContext.CreateRequest();
            List<TagConfigRecord> tags = new List<TagConfigRecord>(_TagConfigRecord);
            RegularRankLoadOutcome outcome = new RegularRankLoadOutcome();
            confirm.SetAdding(true);
            _motherGroupLoadRunning = true;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate
            {
                try
                {
                    QueryHelper query = new QueryHelper();
                    List<TagDefinition> definitions = new List<TagDefinition>();
                    foreach (TagConfigRecord tag in tags) definitions.Add(new TagDefinition { Id = tag.ID, Prefix = tag.Prefix, Name = tag.Name });
                    List<StudentTagLink> links = new List<StudentTagLink>();
                    foreach (StudentTagRecord tag in StudentTag.SelectAll()) links.Add(new StudentTagLink { StudentId = tag.RefStudentID, TagId = tag.RefTagID, Name = tag.Name });
                    DataTable students = query.Select(HistoryStudentAdd.SelectSql(number));
                    HistoryStudentAddResult added = HistoryStudentAdd.Read(number, students, copy,
                        RegularRankCurrentStudentSource.NormalizeCategory(request.ExcludedCategory),
                        RegularRankCurrentStudentSource.NormalizeCategory(request.RankTag1),
                        RegularRankCurrentStudentSource.NormalizeCategory(request.RankTag2), definitions, links);
                    if (added.Status != HistoryStudentAddStatus.Added) throw new InvalidOperationException(added.Message);
                    copy.Add(new ConfirmStudentRow { Group = added.Student, ClassName = added.ClassName, SeatNo = added.SeatNo });
                    List<string> ids = new List<string>();
                    foreach (ConfirmStudentRow row in copy.Rows)
                    {
                        int grade;
                        if (SemesterRankSql.TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out grade) && request.GradeYears.Contains(grade))
                            ids.Add(row.Group.StudentId);
                    }
                    string sql, error;
                    if (!RegularRankSubjectCatalog.TryCreate(request.TargetSchoolYear, request.TargetSemester, request.TargetExamId, ids, out sql, out error))
                        throw new InvalidOperationException(error);
                    outcome.Subjects = new List<string>();
                    foreach (DataRow row in query.Select(sql).Rows)
                    {
                        string subject = Convert.ToString(row["subject"]).Trim();
                        if (subject.Length > 0 && !outcome.Subjects.Contains(subject)) outcome.Subjects.Add(subject);
                    }
                    if (outcome.Subjects.Count == 0) throw new InvalidOperationException("新增後沒有可用科目，保留原清單。");
                    outcome.Rows = new List<ConfirmStudentRow>(copy.Rows);
                    try { outcome.DatabaseEvidence = RegularRankPublicationService.VerifyTestDatabase(query); }
                    catch (Exception ex) { outcome.ValidationError = RegularRankPublicationService.ValidationFailure(ex); }
                }
                catch (Exception ex) { outcome.Error = ex.Message; }
            };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                _motherGroupLoadRunning = false;
                if (!confirm.IsDisposed) confirm.SetAdding(false);
                if (IsDisposed) return;
                if (e.Error != null || outcome.Error.Length > 0)
                { MessageBox.Show(e.Error == null ? outcome.Error : e.Error.Message); return; }
                _rankContext.MarkSubjectLoadFailed();
                ApplyLoadOutcome(outcome);
                ReturnFromMotherGroupConfirm();
                if (btnNext.Enabled)
                    lblRankStatus.Text = "已加入學生並更新目標科目，原學生參與設定保留。可按下一步。";
            };
            worker.RunWorkerAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_motherGroupConfirmForm != null && _motherGroupConfirmForm.CalculationRunning)
            {
                e.Cancel = true;
                return;
            }
            if (_motherGroupConfirmForm != null && !_motherGroupConfirmForm.IsDisposed)
            {
                _motherGroupConfirmForm.HideForHost();
                _motherGroupConfirmForm.Close();
            }
            base.OnFormClosing(e);
        }

        internal bool TryCalculateFromConfirm(RegularRankMotherGroupConfirmForm confirm)
        {
            if (confirm == null || confirm.List == null || _rankContext == null) return false;
            string error = _rankContext.CanCreateRequest();
            if (error.Length > 0) { MessageBox.Show(error); return false; }
            RegularRankConfirmRequest request = _rankContext.CreateRequest();
            MotherGroupConfirmList copy = confirm.List.CopyWorking();
            List<int> actual = RegularRankCandidateSql.ActualGrades(request, copy);
            if (actual.Count == 0) { MessageBox.Show("沒有可計算的年級，不保存設定也不發布排名。"); return false; }
            int unknownGrades = 0;
            foreach (ConfirmStudentRow row in copy.Rows)
            {
                int grade;
                if (!SemesterRankSql.TryParseGradeYear(row.Group.GetWorking(RankKinds.Year).GroupName, out grade)) unknownGrades++;
            }
            string prompt = "目標：" + request.TargetSchoolYear + "-" + request.TargetSemester + " " + request.TargetExamName + "（ID " + request.TargetExamId + "）\n"
                + "來源：" + RegularRankPeriodSourceEditor.SourceText(request.SourceKind)
                + (request.SourceKind == RegularRankSourceKind.History ? "｜批次 " + request.SourceBatchId + "｜" + request.SourceSchoolYear + "-" + request.SourceSemester + " " + request.SourceExamName : "")
                + "\n實際計算及取代年級：" + string.Join("、", actual.Select(g => g.ToString()).ToArray())
                + "\n完整名單：" + copy.Rows.Count + " 人；無法解析年級 " + unknownGrades + " 人保留快照，不進入排名。"
                + "\n將取代相同目標期間、考試及上述年級的有效定期排名。"
                + "\n三組科目等學校設定會先保存，排名失敗時不回滾該設定。寫入僅限 debug6，計算前會確認排名資料表結構。是否繼續？";
            if (MessageBox.Show(confirm, prompt, "確認計算與發布", MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
            string configuration = SchoolConfigurationXml(request);
            confirm.BeginCalculation();
            BackgroundWorker worker = new BackgroundWorker();
            DataTable result = null;
            string operation = "";
            worker.DoWork += delegate
            {
                RegularRankPublicationService.VerifyTestDatabase(new QueryHelper());
                var config = K12.Data.School.Configuration["固定排名_定期評量排名計算"];
                config["設定檔"] = configuration;
                config.Save();
                result = RegularRankPublicationService.PublishForVerification(request, copy, false, out operation);
            };
            worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e)
            {
                if (!confirm.IsDisposed) confirm.FinishCalculation();
                if (IsDisposed) return;
                if (e.Error != null)
                {
                    MessageBox.Show(confirm, "排名發布未完成。操作識別：" + operation + "\n" + e.Error.Message
                        + "\n若訊息表示結果未確認，須先核對批次，不能直接重送。", "定期排名驗證");
                    return;
                }
                MessageBox.Show(confirm, "計算完成。批次：" + result.Rows[0]["batch_id"] + "；矩陣：" + result.Rows[0]["matrix_count"]
                    + "；明細：" + result.Rows[0]["detail_count"] + "\n操作識別：" + operation);
            };
            worker.RunWorkerAsync();
            return true;
        }

        private string SchoolConfigurationXml(RegularRankConfirmRequest request)
        {
            XmlDocument document = new XmlDocument { XmlResolver = null };
            document.LoadXml(RegularRankSnapshotDocument.BuildSettingXml(request));
            string[] elements = { "不採計科目", "類別一不採計科目", "類別二不採計科目" };
            RegularRankSubjectGroup[] groups = { _rankContext.GeneralSubjects, _rankContext.Tag1Subjects, _rankContext.Tag2Subjects };
            for (int i = 0; i < groups.Length; i++) foreach (string name in groups[i].Available)
                if (!groups[i].Checked.Contains(name))
                {
                    XmlElement subject = document.CreateElement(elements[i]);
                    subject.InnerText = name;
                    document.DocumentElement.AppendChild(subject);
                }
            return document.OuterXml;
        }

        private void LegacyCalculateFromConfirm(RegularRankMotherGroupConfirmForm confirm)
        {
            if (!RegularRankCalculationEntry.ExecutionAllowed)
            {
                MessageBox.Show(RegularRankCalculationEntry.BlockedMessage);
                return;
            }
            if (confirm == null || confirm.List == null || confirm.List.Rows.Count == 0)
            {
                MessageBox.Show("沒有確認清單，不能計算。");
                return;
            }
            if (_rankContext == null || _rankContext.CanCreateRequest().Length > 0)
            {
                MessageBox.Show(_rankContext == null ? "沒有計算條件。" : _rankContext.CanCreateRequest());
                return;
            }
            RegularRankConfirmRequest request = _rankContext.CreateRequest();

            // 學生依年級分批使用；資料來自確認頁完整清單，不是舊 plStudentView。
            Dictionary<string, List<string>> gradeStudentDict = new Dictionary<string, List<string>>();
            #region 依年級產生學生清單的SQL
            foreach (ConfirmStudentRow row in confirm.List.Rows)
            {
                if (row == null || row.Group == null)
                    continue;
                string yearName = row.Group.GetWorking(RankKinds.Year).GroupName ?? "";
                string gr = yearName;
                #region 單筆學生資料的SQL
                string studentSql = @"
SELECT
'" + ("" + row.Group.StudentId).Replace("'", "''") + @"'::BIGINT AS student_id
, '" + ("" + row.Group.StudentName).Replace("'", "''") + @"'::TEXT AS student_name
, '" + yearName.Trim('年', '級').Replace("'", "''") + @"'::INT AS rank_grade_year
, '" + ("" + row.Group.GetWorking(RankKinds.Department).GroupName).Replace("'", "''") + @"'::TEXT AS rank_dept_name
, '" + ("" + row.Group.GetWorking(RankKinds.Class).GroupName).Replace("'", "''") + @"'::TEXT AS rank_class_name
, '" + ("" + row.Group.GetWorking(RankKinds.Tag1).GroupName).Replace("'", "''") + @"'::TEXT AS rank_tag1
, '" + ("" + row.Group.GetWorking(RankKinds.Tag2).GroupName).Replace("'", "''") + @"'::TEXT AS rank_tag2
";
                #endregion

                if (!gradeStudentDict.ContainsKey(gr))
                    gradeStudentDict.Add(gr, new List<string>());

                gradeStudentDict[gr].Add(studentSql);
            }
            #endregion



            #region 儲存設定
            {
                XmlDocument document = new XmlDocument();
                XmlElement configEle = document.CreateElement("Setting");
                configEle.SetAttribute("考試名稱", request.TargetExamName);
                configEle.SetAttribute("不排名學生類別", cboStudentFilter.Text);
                configEle.SetAttribute("類別一", cboStudentTag1.Text);
                configEle.SetAttribute("類別二", cboStudentTag2.Text);
                foreach (ListViewItem item in lvCalcSubject.Items)
                {
                    if (item.Checked)
                    {
                        if (_FilteredSubject.Contains(item.Text))
                            _FilteredSubject.Remove(item.Text);
                    }
                    else
                    {
                        if (!_FilteredSubject.Contains(item.Text))
                            _FilteredSubject.Add(item.Text);
                    }
                }
                foreach (ListViewItem item in lvCalcSubjectTag1.Items)
                {
                    if (item.Checked)
                    {
                        if (_Tag1FilteredSubject.Contains(item.Text))
                            _Tag1FilteredSubject.Remove(item.Text);
                    }
                    else
                    {
                        if (!_Tag1FilteredSubject.Contains(item.Text))
                            _Tag1FilteredSubject.Add(item.Text);
                    }
                }
                foreach (ListViewItem item in lvCalcSubjectTag2.Items)
                {
                    if (item.Checked)
                    {
                        if (_Tag2FilteredSubject.Contains(item.Text))
                            _Tag2FilteredSubject.Remove(item.Text);
                    }
                    else
                    {
                        if (!_Tag2FilteredSubject.Contains(item.Text))
                            _Tag2FilteredSubject.Add(item.Text);
                    }
                }
                foreach (var item in _FilteredSubject)
                {
                    var ele = document.CreateElement("不採計科目");
                    ele.InnerText = item;
                    configEle.AppendChild(ele);
                }
                foreach (var item in _Tag1FilteredSubject)
                {
                    var ele = document.CreateElement("類別一不採計科目");
                    ele.InnerText = item;
                    configEle.AppendChild(ele);
                }
                foreach (var item in _Tag2FilteredSubject)
                {
                    var ele = document.CreateElement("類別二不採計科目");
                    ele.InnerText = item;
                    configEle.AppendChild(ele);
                }

                var cd = K12.Data.School.Configuration["固定排名_定期評量排名計算"];
                cd["設定檔"] = configEle.OuterXml;
                cd.Save();
            }
            #endregion

            string schoolYear = request.TargetSchoolYear;
            string semester = request.TargetSemester;
            string examName = request.TargetExamName;
            string examId = request.TargetExamId;
            string tag1 = cboStudentTag1.Text.Trim('[', ']');
            string tag2 = cboStudentTag2.Text.Trim('[', ']');
            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            List<int> gradeYearList = new List<int>(request.GradeYears);

            List<string> selSubjetList = new List<string>();
            List<string> selSubjetTag1List = new List<string>();
            List<string> selSubjetTag2List = new List<string>();

            foreach (ListViewItem item in lvCalcSubject.Items)
            {
                if (item.Checked)
                {
                    selSubjetList.Add(item.Text);
                    //var ele = doc.CreateElement("採計科目");
                    //ele.InnerText = item.Text;
                    //settingEle.AppendChild(ele);
                }
            }
            foreach (ListViewItem item in lvCalcSubjectTag1.Items)
            {
                if (item.Checked)
                {
                    selSubjetTag1List.Add(item.Text);
                    //var ele = doc.CreateElement("類別一採計科目");
                    //ele.InnerText = item.Text;
                    //settingEle.AppendChild(ele);
                }
            }
            foreach (ListViewItem item in lvCalcSubjectTag2.Items)
            {
                if (item.Checked)
                {
                    selSubjetTag2List.Add(item.Text);
                    //var ele = doc.CreateElement("類別二採計科目");
                    //ele.InnerText = item.Text;
                    //settingEle.AppendChild(ele);
                }
            }


            Exception bkwException = null;
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;

            bkw.ProgressChanged += delegate (object s1, ProgressChangedEventArgs e1)
            {
                MotherForm.SetStatusBarMessage("計算排名中", e1.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(1);

                    QueryHelper queryHelper = new QueryHelper();
                    int pr = 20;

                    int batchID = 0;
                    string calculationSetting = "";
                    #region 產生計算設定的字串
                    XmlDocument doc = new XmlDocument();
                    var settingEle = doc.CreateElement("Setting");
                    settingEle.SetAttribute("學年度", "" + schoolYear);
                    settingEle.SetAttribute("學期", "" + semester);
                    settingEle.SetAttribute("考試名稱", "" + examName);
                    settingEle.SetAttribute("不排名學生類別", "" + studentFilter);
                    settingEle.SetAttribute("類別一", "" + tag1);
                    settingEle.SetAttribute("類別二", "" + tag2);

                    foreach (string str in selSubjetList)
                    {
                        var ele = doc.CreateElement("採計科目");
                        ele.InnerText = str;
                        settingEle.AppendChild(ele);
                    }
                    foreach (string str in selSubjetTag1List)
                    {
                        var ele = doc.CreateElement("類別一採計科目");
                        ele.InnerText = str;
                        settingEle.AppendChild(ele);
                    }
                    foreach (string str in selSubjetTag2List)
                    {
                        var ele = doc.CreateElement("類別二採計科目");
                        ele.InnerText = str;
                        settingEle.AppendChild(ele);
                    }

                    foreach (string gr in gradeStudentDict.Keys)
                    {
                        var gradeYearEle = doc.CreateElement("年級");
                        gradeYearEle.InnerText = "" + gr.Trim('年', '級');
                        settingEle.AppendChild(gradeYearEle);
                    }

                    calculationSetting = settingEle.OuterXml;
                    #endregion

                    #region 插入 rank_batch

                    //bkw.ReportProgress(10);

                    #region 0. 插入rank_batch SQL
                    string rank_batch_row = @"SELECT
		 '" + ("" + schoolYear).Replace("'", "''") + @"'::TEXT AS rank_school_year
		, '" + ("" + semester).Replace("'", "''") + @"'::TEXT AS rank_semester
        , '" + ("" + examId).Replace("'", "''") + @"'::TEXT AS ref_exam_id
		, '" + ("" + examName).Replace("'", "''") + @"'::TEXT AS rank_exam_name
        , '" + calculationSetting.Replace("'", "''") + @"'::TEXT AS calculation_setting";


                    string insertRankBatchSql = @"
WITH row AS (
" + rank_batch_row + @")
, insert_batch_data AS (
	INSERT INTO rank_batch(
		school_year
		, semester
		, calculation_description
		, setting
	)
	SELECT
		DISTINCT
		row.rank_school_year::INT
		, row.rank_semester::INT
		, row.rank_school_year||' '||row.rank_semester||' 計算'||row.rank_exam_name||'排名' AS calculation_description
		, row.calculation_setting
	FROM
		row

	RETURNING *
)
SELECT * FROM insert_batch_data
";
                    try
                    {

                        DataTable dtq = queryHelper.Select(insertRankBatchSql);
                        foreach (DataRow dr in dtq.Rows)
                        {
                            string strBatchID = dr["id"].ToString();
                            batchID = int.Parse(strBatchID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    #endregion
                    //bkw.ReportProgress(30);
                    #endregion

                    // 依年級分批計算
                    foreach (string gr in gradeStudentDict.Keys)
                    {
                        #region 產生計算設定的字串
                        //XmlDocument doc = new XmlDocument();
                        //var settingEle = doc.CreateElement("Setting");
                        //settingEle.SetAttribute("學年度", "" + schoolYear);
                        //settingEle.SetAttribute("學期", "" + semester);
                        //settingEle.SetAttribute("考試名稱", "" + examName);
                        //settingEle.SetAttribute("不排名學生類別", "" + studentFilter);
                        //settingEle.SetAttribute("類別一", "" + tag1);
                        //settingEle.SetAttribute("類別二", "" + tag2);                   

                        //foreach(string str in selSubjetList)
                        //{
                        //    var ele = doc.CreateElement("採計科目");
                        //    ele.InnerText = str;
                        //    settingEle.AppendChild(ele);
                        //}
                        //foreach (string str in selSubjetTag1List)
                        //{
                        //    var ele = doc.CreateElement("類別一採計科目");
                        //    ele.InnerText = str;
                        //    settingEle.AppendChild(ele);
                        //}
                        //foreach (string str in selSubjetTag2List)
                        //{
                        //    var ele = doc.CreateElement("類別二採計科目");
                        //    ele.InnerText = str;
                        //    settingEle.AppendChild(ele);
                        //}

                        #endregion

                        //var gradeYearEle = doc.CreateElement("年級");
                        //gradeYearEle.InnerText = "" + gr.Trim('年', '級');
                        //settingEle.AppendChild(gradeYearEle);


                        List<string> rowSqlList = new List<string>();


                        //每一筆row(包含GradeYear, SchoolYear, Semester, ExamName)先組好加進List
                        rowSqlList.Add(@"
	SELECT
		'" + gr.Trim('年', '級') + @"'::TEXT  AS rank_grade_year
		, '" + ("" + schoolYear).Replace("'", "''") + @"'::TEXT AS rank_school_year
		, '" + ("" + semester).Replace("'", "''") + @"'::TEXT AS rank_semester
        , '" + ("" + examId).Replace("'", "''") + @"'::TEXT AS ref_exam_id
		, '" + ("" + examName).Replace("'", "''") + @"'::TEXT AS rank_exam_name
        , '" + calculationSetting.Replace("'", "''") + @"'::TEXT AS calculation_setting
        , " + batchID + @" AS batch_id
");

                        //2021-12 Cynthia  新增新五標、標準差
                        //2022-06-22 Cynthia 將總科目、類別一科目、類別二科目獨立計算
                        #region 計算排名的SQL 新增新五標、標準差
                        string insertRankSql = @"
WITH row AS (
" + string.Join(@"
    UNION ALL
", rowSqlList) + @"
), student_row AS (
" + string.Join(@"
    UNION ALL
", gradeStudentDict[gr]) + @"
), calc_subject AS ( --採計科目
        SELECT
         unnest(xpath('/Setting/採計科目/text()', xmlparse(content calculation_setting)))::TEXT AS subject
    FROM
        row
), calc_subject_tag1 AS ( --類別一採計科目
    SELECT
        unnest(xpath('/Setting/類別一採計科目/text()', xmlparse(content calculation_setting)))::TEXT AS subject
    FROM
        row	
), calc_subject_tag2 AS ( --類別二採計科目
     SELECT
        unnest(xpath('/Setting/類別二採計科目/text()', xmlparse(content calculation_setting)))::TEXT AS subject
    FROM
        row			
), score_detail_row AS (--取得學生的定期評量成績
	SELECT
		student_row.student_id
   		, student_row.student_name
		, sc_attend.id AS sc_attend_id
		, course.course_name
		, course.school_year AS rank_school_year
		, course.semester AS rank_semester
		, course.subject
		, course.domain
		, course.credit
		, exam.id AS exam_id
		, exam.exam_name
		, student_row.rank_class_name
		, student_row.rank_grade_year
        , student_row.rank_dept_name
		, student_row.rank_tag1
		, student_row.rank_tag2
		, CASE WHEN sce_take.score = -1 THEN 0 ELSE sce_take.score END AS score
	FROM  
        sce_take
		LEFT JOIN sc_attend 
			ON ref_sc_attend_id = sc_attend.id
		LEFT JOIN exam 
			ON ref_exam_id = exam.id
		LEFT JOIN course 
			ON sc_attend.ref_course_id = course.id
            AND course.subject IN (
                SELECT subject FROM calc_subject
				UNION
				SELECT subject FROM calc_subject_tag1
				UNION
				SELECT subject FROM calc_subject_tag2 
            )
		INNER JOIN student_row
			ON sc_attend.ref_student_id = student_row.student_id
		INNER JOIN row
			ON course.school_year = row.rank_school_year::int
			AND course.semester = row.rank_semester::int
			AND student_row.rank_grade_year = row.rank_grade_year::int
			AND exam.id = row.ref_exam_id::INT
	WHERE 
        sce_take.score IS NOT NULL
        AND sce_take.score <> -2

    --2.1 科目成績 年排名
    --2.2 科目成績 班排名
    --2.5 科目成績 科排名
), exam_score AS (-------結算定期評量總成績
	SELECT	
		score_detail_row.*
	FROM 
		score_detail_row
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject
        )
), subject_rank_row AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year,subject ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year,subject ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year,subject ) AS grade_count
		, COUNT (student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year,subject ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name, subject ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY score ASC) AS dept_row_number
	FROM 
        exam_score
	WHERE 
        subject IS NOT NULL
), subject_rank AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		subject_rank_row AS s1



    --2.3 科目成績 類別1排名
), exam_score_tag1 AS (-------結算定期評量總成績
	SELECT	
		score_detail_row.*
	FROM 
		score_detail_row
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
), subject_rank_row_tag1 AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score ASC) AS tag1_row_number
	FROM 
        exam_score_tag1
	WHERE 
        subject IS NOT NULL
), subject_rank_tag1 AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		subject_rank_row_tag1 AS s1



    --2.4 科目成績 類別2排名
), exam_score_tag2 AS (-------結算定期評量總成績
	SELECT	
		score_detail_row.*
	FROM 
		score_detail_row
	WHERE 
	    subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
), subject_rank_row_tag2 AS (--------計算科目排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/科目成績'::TEXT AS item_type
		, subject AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score ASC) AS tag2_row_number
	FROM 
        exam_score_tag2
	WHERE 
        subject IS NOT NULL
), subject_rank_tag2 AS (-----------計算科目排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		subject_rank_row_tag2 AS s1



    --1.1 領域成績 年排名
    --1.2 領域成績 班排名
), domain_score AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, domain ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name, domain ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, domain ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year, domain ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name, domain ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, domain ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, domain) AS grade_count
		, COUNT (student_id) OVER(PARTITION BY rank_class_name, domain) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, domain) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, domain ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name, domain ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name, domain ORDER BY score ASC) AS dept_row_number
	FROM 
		domain_score
	WHERE
		domain IS NOT NULL
), domain_rank AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		domain_rank_row AS s1



    --1.3 領域成績 類別1排名
), domain_score_tag1 AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row_tag1 AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, domain) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1, domain ORDER BY score ASC) AS tag1_row_number
	FROM 
		domain_score_tag1
	WHERE
		domain IS NOT NULL
), domain_rank_tag1 AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		domain_rank_row_tag1 AS s1



    --1.4 領域成績 類別2排名
), domain_score_tag2 AS (-----結算領域成績
	SELECT 
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, domain
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM  
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY
		domain
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , student_id
        , student_name
        , rank_tag1
        , rank_tag2
), domain_rank_row_tag2 AS (-------計算領域排名
	SELECT
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/領域成績'::TEXT AS item_type
		, domain::TEXT AS item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, domain) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2, domain ORDER BY score ASC) AS tag2_row_number
	FROM 
		domain_score_tag2
	WHERE
		domain IS NOT NULL
), domain_rank_tag2 AS (-----------計算領域排名排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		domain_rank_row_tag2 AS s1



    --3.1 總計成績 總分 年排名
    --3.2 總計成績 總分 班排名
    --3.5 總計成績 總分 科排名
), calc_sum_score AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		calc_sum_score
), calc_sum_rank AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		calc_sum_rank_row AS s1



    --3.3 總計成績 總分 類別1排名
), calc_sum_score_tag1 AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row_tag1 AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		calc_sum_score_tag1
), calc_sum_rank_tag1 AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		calc_sum_rank_row_tag1 AS s1



    --3.4 總計成績 總分 類別2排名
), calc_sum_score_tag2 AS (------算數總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal ) AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_sum_rank_row_tag2 AS (-----------計算總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		calc_sum_score_tag2
), calc_sum_rank_tag2 AS (-----------計算總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		calc_sum_rank_row_tag2 AS s1



    --4.1 總計成績 平均 年排名
    --4.2 總計成績 平均 班排名
    --4.5 總計成績 平均 科排名
), calc_avg_score AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		calc_avg_score
), calc_avg_rank AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		calc_avg_rank_row AS s1



    --4.3 總計成績 平均 類別1排名
), calc_avg_score_tag1 AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row_tag1 AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		calc_avg_score_tag1 AS calc_avg_score
), calc_avg_rank_tag1 AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		calc_avg_rank_row_tag1 AS s1



    --4.4 總計成績 平均 類別2排名
), calc_avg_score_tag2 AS (------算數平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, AVG( score::decimal ) AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), calc_avg_rank_row_tag2 AS (-----------計算平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		calc_avg_score_tag2 AS calc_avg_score
), calc_avg_rank_tag2 AS (-----------計算平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		calc_avg_rank_row_tag2 AS s1




    --5.1 總計成績 加權總分 年排名
    --5.2 總計成績 加權總分 班排名
    --5.5 總計成績 加權總分 科排名
), weight_sum_score AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		weight_sum_score
), weight_sum_rank AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		weight_sum_rank_row AS s1




    --5.3 總計成績 加權總分 類別1排名
), weight_sum_score_tag1 AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row_tag1 AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		weight_sum_score_tag1 AS weight_sum_score
), weight_sum_rank_tag1 AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		weight_sum_rank_row_tag1 AS s1




    --5.4 總計成績 加權總分 類別2排名
), weight_sum_score_tag2 AS (------加權總分排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, SUM( score::decimal * credit::decimal ) AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_sum_rank_row_tag2 AS (-----------計算加權總分排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權總分'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		weight_sum_score_tag2 AS weight_sum_score
), weight_sum_rank_tag2 AS (-----------計算加權總分排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		weight_sum_rank_row_tag2 AS s1




    --6.1 總計成績 加權平均 年排名
    --6.2 總計成績 加權平均 班排名
    --6.5 總計成績 加權平均 科排名
), weight_avg_score AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM 
		exam_score
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
		, rank_dept_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, rank_dept_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_rank_reverse
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_rank_reverse
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT (*) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT (student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year ORDER BY score ASC) AS grade_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_class_name ORDER BY score ASC) AS class_row_number
	    , ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY score ASC) AS dept_row_number
	FROM 
		weight_avg_score
), weight_avg_rank AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((grade_rank::DECIMAL-1)*100::DECIMAL/grade_count)+1 AS graderank_percentage
		, FLOOR((class_rank::DECIMAL-1)*100::DECIMAL/class_count)+1 AS classrank_percentage
		, FLOOR((dept_rank::DECIMAL-1)*100::DECIMAL/dept_count)+1 AS deptrank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
	FROM 
		weight_avg_rank_row AS s1




    --6.3 總計成績 加權平均 類別1排名
), weight_avg_score_tag1 AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM 
		exam_score_tag1
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag1
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row_tag1 AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY score ASC) AS tag1_row_number
	FROM 
		weight_avg_score_tag1 AS weight_avg_score
), weight_avg_rank_tag1 AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag1_rank::DECIMAL-1)*100::DECIMAL/tag1_count)+1 AS tag1rank_percentage
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1rank_pr
	FROM 
		weight_avg_rank_row_tag1 AS s1




    --6.4 總計成績 加權平均 類別2排名
), weight_avg_score_tag2 AS (------加權平均排名所需成績
	SELECT
		student_id
		, student_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, rank_tag1
		, rank_tag2
		, CASE 
            WHEN SUM(credit::DECIMAL) IS NULL THEN NULL::DECIMAL
            WHEN SUM(credit::DECIMAL) = 0 THEN 0::DECIMAL
            ELSE SUM(score::DECIMAL * credit::DECIMAL) / SUM(credit::DECIMAL)
            END AS score
	FROM 
		exam_score_tag2
	WHERE
		score IS NOT NULL
		AND credit IS NOT NULL
	    AND subject IN (
            SELECT subject
            FROM calc_subject_tag2
        )
	GROUP BY 
		student_id
        , student_name
        , rank_school_year
        , rank_semester
        , rank_grade_year
        , rank_class_name
        , exam_id
        , rank_tag1
        , rank_tag2
), weight_avg_rank_row_tag2 AS (-----------計算加權平均排名
	SELECT 
		student_id
		, rank_tag1
		, rank_tag2
		, '定期評量/總計成績'::text AS item_type
		, '加權平均'::TEXT As item_name
		, rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_class_name
		, exam_id
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score DESC) AS tag2_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_rank_reverse
		, COUNT (*) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
		, ROW_NUMBER() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY score ASC) AS tag2_row_number
	FROM 
		weight_avg_score_tag2 AS weight_avg_score
), weight_avg_rank_tag2 AS (-----------計算加權平均排名百分比及PR
	SELECT  
		s1.*
		, FLOOR((tag2_rank::DECIMAL-1)*100::DECIMAL/tag2_count)+1 AS tag2rank_percentage
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
	FROM 
		weight_avg_rank_row_tag2 AS s1
), score_list AS (

    --X.1 年排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (grade_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*88/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*88/100=grade_row_number OR grade_count*88/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (grade_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*75/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*75/100=grade_row_number OR grade_count*75/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (grade_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*50/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*50/100=grade_row_number OR grade_count*50/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (grade_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*25/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*25/100=grade_row_number OR grade_count*25/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (grade_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE grade_count*12/100+1=grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE grade_count*12/100=grade_row_number OR grade_count*12/100+1 = grade_row_number) OVER(PARTITION BY rank_grade_year, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank * 4 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE grade_rank * 2 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( grade_count - grade_rank + 1 ) * 2 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( grade_count - grade_rank + 1 ) * 4 <= grade_count) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND  score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, item_type, item_name)AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, graderank_percentage AS percentile
		, graderank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
 		    SELECT * FROM domain_rank--1.1 領域成績 年排名
            UNION ALL
            SELECT * FROM subject_rank--2.1 科目成績 年排名
            UNION ALL
            SELECT * FROM calc_sum_rank--3.1 總計成績 總分 年排名
            UNION ALL
            SELECT * FROM calc_avg_rank--4.1 總計成績 平均 年排名
            UNION ALL
            SELECT * FROM weight_sum_rank--5.1 總計成績 加權總分 年排名
            UNION ALL
            SELECT * FROM weight_avg_rank--6.1 總計成績 加權平均 年排名
        ) AS data
	UNION ALL
    --X.2 班排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_class_name, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (class_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*88/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*88/100=class_row_number OR class_count*88/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (class_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*75/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*75/100=class_row_number OR class_count*75/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (class_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*50/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*50/100=class_row_number OR class_count*50/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (class_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*25/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*25/100=class_row_number OR class_count*25/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (class_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE class_count*12/100+1=class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE class_count*12/100=class_row_number OR class_count*12/100+1 = class_row_number) OVER(PARTITION BY rank_class_name, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank * 4 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE class_rank * 2 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( class_count - class_rank + 1 ) * 2 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( class_count - class_rank + 1 ) * 4 <= class_count) OVER(PARTITION BY rank_class_name, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_class_name, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_class_name, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_class_name, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)  OVER(PARTITION BY rank_class_name, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_class_name, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_class_name, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_class_name, item_type, item_name)AS level_lt10 
		, student_id
		, score
		, class_rank AS rank
		, classrank_percentage AS percentile
		, classrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank--1.2 領域成績 班排名
            UNION ALL
            SELECT * FROM subject_rank--2.2 科目成績 班排名
            UNION ALL
            SELECT * FROM calc_sum_rank--3.2 總計成績 總分 班排名
            UNION ALL
            SELECT * FROM calc_avg_rank--4.2 總計成績 平均 班排名
            UNION ALL
            SELECT * FROM weight_sum_rank--5.2 總計成績 加權總分 班排名
            UNION ALL
            SELECT * FROM weight_avg_rank--6.2 總計成績 加權平均 班排名
        ) AS data
	UNION ALL
    --X.5 科排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (dept_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*88/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*88/100=dept_row_number OR dept_count*88/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (dept_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*75/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*75/100=dept_row_number OR dept_count*75/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (dept_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*50/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*50/100=dept_row_number OR dept_count*50/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (dept_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*25/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*25/100=dept_row_number OR dept_count*25/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (dept_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE dept_count*12/100+1=dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE dept_count*12/100=dept_row_number OR dept_count*12/100+1 = dept_row_number) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(score::DECIMAL)FILTER(WHERE dept_rank * 4 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_top_25
		, AVG(score::DECIMAL)FILTER(WHERE dept_rank * 2 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_top_50
		, AVG(score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg
		, AVG(score::DECIMAL)FILTER(WHERE ( dept_count - dept_rank + 1 ) * 2 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL)FILTER(WHERE ( dept_count - dept_rank + 1 ) * 4 <= dept_count) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)  OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL)  OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL)   OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL)  OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL)   OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_dept_name, item_type, item_name)AS level_lt10 
		, student_id
		, score
		, dept_rank AS rank
		, deptrank_percentage AS percentile
		, deptrank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank--1.5 領域成績 科排名
            UNION ALL
            SELECT * FROM subject_rank--2.5 科目成績 科排名
            UNION ALL
            SELECT * FROM calc_sum_rank--3.5 總計成績 總分 科排名
            UNION ALL
            SELECT * FROM calc_avg_rank--4.5 總計成績 平均 科排名
            UNION ALL
            SELECT * FROM weight_sum_rank--5.5 總計成績 加權總分 科排名
            UNION ALL
            SELECT * FROM weight_avg_rank--6.5 總計成績 加權平均 科排名
        ) AS data
	UNION ALL
    --X.3 類別1排名
    SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (tag1_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*88/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*88/100=tag1_row_number OR tag1_count*88/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag1_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*75/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*75/100=tag1_row_number OR tag1_count*75/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag1_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*50/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*50/100=tag1_row_number OR tag1_count*50/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag1_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*25/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*25/100=tag1_row_number OR tag1_count*25/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag1_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag1_count*12/100+1=tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag1_count*12/100=tag1_row_number OR tag1_count*12/100+1 = tag1_row_number) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank * 4 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag1_rank * 2 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag1_count - tag1_rank + 1 ) * 2 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag1_count - tag1_rank + 1 ) * 4 <= tag1_count) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag1, item_type, item_name)AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
		, tag1rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank_tag1--1.3 領域成績 類別1排名
            UNION ALL
            SELECT * FROM subject_rank_tag1--2.3 科目成績 類別1排名
            UNION ALL
            SELECT * FROM calc_sum_rank_tag1--3.3 總計成績 總分 類別1排名
            UNION ALL
            SELECT * FROM calc_avg_rank_tag1--4.3 總計成績 平均 類別1排名
            UNION ALL
            SELECT * FROM weight_sum_rank_tag1--5.3 總計成績 加權總分 類別1排名
            UNION ALL
            SELECT * FROM weight_avg_rank_tag1--6.3 總計成績 加權平均 類別1排名
        ) AS data
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''
	UNION ALL
    --X.4 類別2排名
	SELECT
		rank_school_year 
		, rank_semester
		, rank_grade_year
		, item_type
		, exam_id AS ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, STDDEV_POP(score) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS std_dev_pop
	    , CASE WHEN (tag2_count*88/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*88/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*88/100=tag2_row_number OR tag2_count*88/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_88
	    , CASE WHEN (tag2_count*75/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*75/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*75/100=tag2_row_number OR tag2_count*75/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_75
	    , CASE WHEN (tag2_count*50/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*50/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*50/100=tag2_row_number OR tag2_count*50/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_50
	    , CASE WHEN (tag2_count*25/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*25/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*25/100=tag2_row_number OR tag2_count*25/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_25
	    , CASE WHEN (tag2_count*12/100::DECIMAL % 1 <> 0) 
	        THEN AVG(score::DECIMAL)FILTER(WHERE tag2_count*12/100+1=tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	        ELSE AVG(score::DECIMAL)FILTER(WHERE tag2_count*12/100=tag2_row_number OR tag2_count*12/100+1 = tag2_row_number) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)::DECIMAL 
	      END pr_12
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank * 4 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_top_25
		, AVG(Score::DECIMAL)FILTER(WHERE tag2_rank * 2 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_top_50
		, AVG(Score::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag2_count - tag2_rank + 1 ) * 2 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_bottom_50
		, AVG(Score::DECIMAL)FILTER(WHERE ( tag2_count - tag2_rank + 1 ) * 4 <= tag2_count) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS avg_bottom_25
		, COUNT(*) FILTER (WHERE 100::DECIMAL<=score::DECIMAL ) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_gte100 
		, COUNT(*) FILTER (WHERE 90::DECIMAL<=score AND score <100::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_90
		, COUNT(*) FILTER (WHERE 80::DECIMAL<=score AND score <90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_80
		, COUNT(*) FILTER (WHERE 70::DECIMAL<=score AND score <80::DECIMAL)OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_70
		, COUNT(*) FILTER (WHERE 60::DECIMAL<=score AND score <70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_60
		, COUNT(*) FILTER (WHERE 50::DECIMAL<=score AND score <60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_50
		, COUNT(*) FILTER (WHERE 40::DECIMAL<=score AND score <50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_40
		, COUNT(*) FILTER (WHERE 30::DECIMAL<=score AND score <40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_30
		, COUNT(*) FILTER (WHERE 20::DECIMAL<=score AND score <30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name) AS level_20
		, COUNT(*) FILTER (WHERE 10::DECIMAL<=score AND score <20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_10
		, COUNT(*) FILTER (WHERE score<10::DECIMAL) OVER (PARTITION BY rank_grade_year, rank_tag2, item_type, item_name)AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2rank_percentage AS percentile
		, tag2rank_pr AS pr
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		(
            SELECT * FROM domain_rank_tag2--1.4 領域成績 類別2排名
            UNION ALL
            SELECT * FROM subject_rank_tag2--2.4 科目成績 類別2排名
            UNION ALL
            SELECT * FROM calc_sum_rank_tag2--3.4 總計成績 總分 類別2排名
            UNION ALL
            SELECT * FROM calc_avg_rank_tag2--4.4 總計成績 平均 類別2排名
            UNION ALL
            SELECT * FROM weight_sum_rank_tag2--5.4 總計成績 加權總分 類別2排名
            UNION ALL
            SELECT * FROM weight_avg_rank_tag2--6.4 總計成績 加權平均 類別2排名
        ) AS data
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
), update_data AS (
	UPDATE
		rank_matrix
	SET
		is_alive = NULL
	FROM 
		row
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = row.rank_school_year::INT
		AND rank_matrix.semester = row.rank_semester::INT
		AND rank_matrix.grade_year = row.rank_grade_year::INT
		AND rank_matrix.ref_exam_id = row.ref_exam_id::INT

	RETURNING rank_matrix.*
), insert_matrix_data AS (
	INSERT INTO rank_matrix(
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
	SELECT DISTINCT
		row.batch_id AS ref_batch_id
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
	FROM
		score_list
		LEFT OUTER JOIN update_data
			ON update_data.id  < 0 --永遠為false，只是為了讓insert等待update執行完
		CROSS JOIN row
	RETURNING *
), insert_batch_student_data AS (
	INSERT INTO rank_batch_student(
		ref_batch_id
		, ref_student_id
		, grade_year
		, matrix_grade
		, matrix_class
		, matrix_tag1
		, matrix_tag2
	)
	SELECT
		row.batch_id AS ref_batch_id
		, student_row.student_id
		, student_row.rank_grade_year
		, student_row.rank_grade_year||'年級' AS matrix_grade
		, student_row.rank_class_name
		, student_row.rank_tag1
		, student_row.rank_tag2
	FROM
		student_row
		CROSS JOIN row
), insert_detail_data AS (
	INSERT INTO rank_detail(
		ref_matrix_id
		, ref_student_id
		, score
		, rank
		, percentile
        , pr
	)
	SELECT
		insert_matrix_data.id AS ref_matrix_id
		, score_list.student_id AS ref_student_id
		, score_list.score AS score
		, score_list.rank AS rank
		, score_list.percentile AS percentile
		, score_list.pr AS pr
	FROM
		score_list
		LEFT OUTER JOIN insert_matrix_data
			ON insert_matrix_data.school_year = score_list.rank_school_year
			AND insert_matrix_data.semester = score_list.rank_semester
			AND insert_matrix_data.grade_year = score_list.rank_grade_year
			AND insert_matrix_data.item_type = score_list.item_type
			AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
			AND insert_matrix_data.item_name = score_list.item_name
			AND insert_matrix_data.rank_type = score_list.rank_type
			AND insert_matrix_data.rank_name = score_list.rank_name
)
SELECT count(*) FROM score_list
";

                        #endregion

                        //bkw.ReportProgress(50);
                        //QueryHelper queryHelper = new QueryHelper();
                        //queryHelper.Select(insertRankSql);
                        //bkw.ReportProgress(100);
                        try
                        {

                            //DataTable dt1 = queryHelper.Select(insertRankSql_Old);
                            DataTable dt1 = queryHelper.Select(insertRankSql);
                            //if (dt1.Rows.Count > 0)
                            //{
                            //    string fiPath = Application.StartupPath + @"\debugf.txt";
                            //    using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath, true))
                            //    {
                            //        fi.WriteLine(gr + " 筆數：" + dt1.Rows[0][0].ToString());
                            //    }
                            //}

                            bkw.ReportProgress(pr);
                            pr += 20;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }


                    }


                }
                catch (Exception exception)
                {
                    bkwException = exception;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (bkwException != null)
                {
                    throw new Exception("計算排名失敗", bkwException);
                }
                MessageBox.Show("計算完成");
                MotherForm.SetStatusBarMessage("排名計算完成");
                if (confirm != null && !confirm.IsDisposed)
                    confirm.HideForHost();
                Enabled = true;
                Close();
            };

            bkw.RunWorkerAsync();
        }

        private void cboStudentTag1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag1.SelectedIndex != 0)
            {
                cboStudentTag2.Enabled = true;
            }
            else
            {
                cboStudentTag2.SelectedIndex = 0;
                cboStudentTag2.Enabled = false;
            }
            if (cboStudentTag1.Text == cboStudentTag2.Text || cboStudentTag1.Text == cboStudentFilter.Text)
            {
                cboStudentTag1.SelectedIndex = 0;
            }
            RegularPeriodSourceChanged(sender, e);
        }

        private void cboStudentTag2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag2.Text == cboStudentTag1.Text || cboStudentTag2.Text == cboStudentFilter.Text)
            {
                cboStudentTag2.SelectedIndex = 0;
            }
            RegularPeriodSourceChanged(sender, e);
        }

        private void cboStudentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentFilter.Text == cboStudentTag1.Text || cboStudentFilter.Text == cboStudentTag2.Text)
            {
                cboStudentFilter.SelectedIndex = 0;
            }
            RegularPeriodSourceChanged(sender, e);
        }

    }
}
