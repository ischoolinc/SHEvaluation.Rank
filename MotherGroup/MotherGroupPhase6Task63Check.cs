using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase6Task63Check
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckSuccessfulImport());
                errors.AddRange(CheckSameNumberRoundTrip());
                errors.AddRange(CheckFailureLeavesList());
                errors.AddRange(CheckBlankScoreAndYear());
                errors.AddRange(CheckExcelEditEvents());
                errors.AddRange(CheckForm());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase6-task63-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("excel-ui-opened: false");
                    return 0;
                }
                foreach (string error in errors)
                    Console.WriteLine(error);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static List<string> CheckSuccessfulImport()
        {
            List<string> errors = new List<string>();
            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(
                RoundTrip(Student("00123", "Excel姓名", "2年級", "忠班", "普通科", "甲組", "", true, true, true, true, false), Student("S9", "排除生", "1年級", "孝班", "", "", "", true, true, true, true, true)),
                Lookup(Row("91", "00123", "主檔姓名", 1), Row("92", "S9", "排除生", 1)));
            Expect(errors, validation.IsValid && validation.Students.Count == 2, "正常 Excel 應通過驗證");
            string warnings = MotherGroupExcelApply.WarningText(validation);
            Expect(errors, warnings.IndexOf("00123") >= 0 && warnings.IndexOf("主檔") >= 0, "姓名不一致時應留下可見警告");
            List<StudentMotherGroup> groups = MotherGroupExcelApply.CreateGroups(validation, "不排名", Tags(), Links("92"));
            StudentMotherGroup named = groups[0];
            StudentMotherGroup excluded = groups[1];
            Expect(errors, named.StudentId == "91" && named.Status == 1 && named.StudentName == "主檔姓名" && named.StudentNumber == "00123", "身分與清單姓名應採用學號加狀態對到的主檔，並保留前導零");
            Expect(errors, named.GetWorking(RankKinds.Year).GroupName == "2年級" && named.GetWorking(RankKinds.Year).Participate, "年排名應採用 Excel 名稱與參與");
            Expect(errors, named.GetWorking(RankKinds.Class).GroupName == "忠班" && named.GetWorking(RankKinds.Department).GroupName == "普通科" && named.GetWorking(RankKinds.Tag1).GroupName == "甲組", "班、科與類別一應採用 Excel 母群名稱");
            Expect(errors, named.GetWorking(RankKinds.Tag2).GroupName == "" && !named.GetWorking(RankKinds.Tag2).Participate, "空白類別二應保留空白且不參與");
            foreach (string rankType in RankKinds.All)
                Expect(errors, !excluded.GetWorking(rankType).Participate && excluded.GetOriginal(rankType).GroupName == excluded.GetWorking(rankType).GroupName, "符合不排名類別時，" + rankType + "初始不參與且名稱不變");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(Rows(groups));
            Expect(errors, list.VisibleStudentIds("00123").Count == 1 && list.VisibleStudentIds("沒有這個人").Count == 0, "搜尋應使用確認清單");
            list.SetParticipate(new string[] { "91" }, RankKinds.Class, false);
            MotherGroupCountSummary summary = list.Summarize();
            Expect(errors, summary.StudentCount == 2 && RankCount(summary, RankKinds.Class, true) == 0, "批次設定與人數統計應涵蓋 Excel 學生");
            list.Restore();
            Expect(errors, named.GetWorking(RankKinds.Class).Participate && named.GetWorking(RankKinds.Class).GroupName == "忠班", "還原參與應回到 Excel 套用後的初始值，且不改名稱");
            return errors;
        }

        private static List<string> CheckSameNumberRoundTrip()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup regular = Student("101", "一般生", "1年級", "忠班", "普通科", "甲組", "乙組", true, true, true, true, true);
            regular.StudentId = "1";
            regular.Status = 1;
            StudentMotherGroup suspended = Student("101", "休學生", "1年級", "孝班", "普通科", "甲組", "乙組", true, false, true, true, false);
            suspended.StudentId = "2";
            suspended.Status = 4;
            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(
                RoundTrip(regular, suspended),
                Lookup(Row("1", "101", "一般生", 1), Row("2", "101", "休學生", 4)));
            Expect(errors, validation.IsValid && validation.Students.Count == 2, "相同學號的一般與休學應同時通過驗證");
            List<StudentMotherGroup> kept = MotherGroupExcelApply.CreateGroups(validation, "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, kept.Count == 2 && kept[0].StudentId == "1" && kept[0].Status == 1 && kept[1].StudentId == "2" && kept[1].Status == 4, "兩位學生應保留各自的 student.id 與 student.status");
            Expect(errors, kept[0].StudentNumber == "101" && kept[1].StudentNumber == "101", "相同學號應同時留在確認清單");
            Expect(errors, SameNames(kept[0], "1年級", "忠班", "普通科", "甲組", "乙組") && SameNames(kept[1], "1年級", "孝班", "普通科", "甲組", "乙組"), "再次匯入後五種母群名稱應與匯出檔一致");
            Expect(errors, Participation(kept[0], true, true, true, true, true) && Participation(kept[1], true, false, true, true, false), "未套用不排名類別時，五種參與應與匯出檔一致");

            List<StudentMotherGroup> excluded = MotherGroupExcelApply.CreateGroups(validation, "不排名", Tags(), Links("1"));
            Expect(errors, Participation(excluded[0], false, false, false, false, false) && SameNames(excluded[0], "1年級", "忠班", "普通科", "甲組", "乙組"), "符合不排名類別時五種初始不參與，名稱保持 Excel 原值");
            Expect(errors, Participation(excluded[1], true, false, true, true, false) && excluded[1].StudentId == "2", "不符合不排名類別的學生應保留 Excel 參與值");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(Rows(kept));
            Expect(errors, list.Rows.Count == 2 && list.Rows[0].Group.StudentId == "1" && list.Rows[1].Group.StudentId == "2", "確認清單載入不應依學號合併不同 student.id");

            StudentMotherGroup named = Student("00123", "Excel姓名", "2年級", "忠班", "普通科", "甲組", "乙組", true, true, true, true, true);
            named.Status = 1;
            MotherGroupExcelValidation nameCheck = MotherGroupExcelImport.Validate(RoundTrip(named), Lookup(Row("91", "00123", "主檔姓名", 1)));
            string warnings = MotherGroupExcelApply.WarningText(nameCheck);
            List<StudentMotherGroup> namedGroups = MotherGroupExcelApply.CreateGroups(nameCheck, "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, namedGroups[0].StudentNumber == "00123" && namedGroups[0].StudentName == "主檔姓名" && namedGroups[0].StudentId == "91" && warnings.IndexOf("主檔") >= 0, "姓名不同時採用主檔姓名、保留警告，學號保留前導零");
            Expect(errors, namedGroups[0].GetWorking(RankKinds.Class).GroupName == "忠班" && namedGroups[0].GetWorking(RankKinds.Department).GroupName == "普通科" && namedGroups[0].GetWorking(RankKinds.Tag1).GroupName == "甲組", "母群名稱應維持 Excel 原值，不用目前班級、科別或標籤覆蓋");

            StudentMotherGroup existing = Student("S500", "歷史生", "舊年", "舊班", "歷史科", "舊類", "舊類二", true, true, true, true, false);
            MotherGroupConfirmList previous = new MotherGroupConfirmList();
            previous.Load(new List<ConfirmStudentRow> { new ConfirmStudentRow { Group = existing } });
            string before = Signature(previous);
            Workbook partial = RoundTrip(regular, suspended);
            partial.Worksheets[0].Cells[2, 8].PutValue("是");
            MotherGroupExcelValidation failed = MotherGroupExcelImport.Validate(partial, Lookup(Row("1", "101", "一般生", 1), Row("2", "101", "休學生", 4)));
            Expect(errors, !failed.IsValid && failed.Students.Count == 0, "任一列失敗時不產生部分成功的學生清單");
            Expect(errors, Signature(previous) == before, "匯入失敗時原有確認清單保持不變");

            string confirm = File.ReadAllText("MotherGroup\\MotherGroupConfirmList.cs", Encoding.UTF8);
            int load = confirm.IndexOf("public void Load");
            int copy = confirm.IndexOf("public MotherGroupConfirmList CopyWorking");
            string loadBody = load >= 0 && copy > load ? confirm.Substring(load, copy - load) : "";
            Expect(errors, loadBody.IndexOf("ContainsIdentity") < 0 && loadBody.IndexOf("StudentNumber") < 0, "Load 不應以學號去除重複");
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int show = form.IndexOf("void ShowConfirmList");
            int add = form.IndexOf("void AddHistoryStudent");
            string showBody = show >= 0 && add > show ? form.Substring(show, add - show) : "";
            Expect(errors, showBody.IndexOf("ContainsIdentity") < 0 && showBody.IndexOf(".GroupName") < 0, "ShowConfirmList 不應依學號合併，也不應改寫五種母群名稱");
            string apply = File.ReadAllText("MotherGroup\\MotherGroupExcelApply.cs", Encoding.UTF8);
            Expect(errors, apply.IndexOf("group.StudentId = student.StudentId") >= 0 && apply.IndexOf("group.Status = student.Status") >= 0 && apply.IndexOf("student_number") < 0, "建立確認清單應使用已解析的學生識別與狀態");
            return errors;
        }

        private static bool SameNames(StudentMotherGroup group, string year, string className, string department, string tag1, string tag2)
        {
            return group.GetWorking(RankKinds.Year).GroupName == year
                && group.GetWorking(RankKinds.Class).GroupName == className
                && group.GetWorking(RankKinds.Department).GroupName == department
                && group.GetWorking(RankKinds.Tag1).GroupName == tag1
                && group.GetWorking(RankKinds.Tag2).GroupName == tag2;
        }

        private static bool Participation(StudentMotherGroup group, bool year, bool classOn, bool department, bool tag1, bool tag2)
        {
            return group.GetWorking(RankKinds.Year).Participate == year
                && group.GetWorking(RankKinds.Class).Participate == classOn
                && group.GetWorking(RankKinds.Department).Participate == department
                && group.GetWorking(RankKinds.Tag1).Participate == tag1
                && group.GetWorking(RankKinds.Tag2).Participate == tag2;
        }

        private static List<string> CheckFailureLeavesList()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup existing = Student("S500", "歷史生", "舊年", "舊班", "歷史科", "舊類", "", true, true, true, true, false);
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow> { new ConfirmStudentRow { Group = existing } });
            string before = Signature(list);
            Workbook broken = RoundTrip(Student("00123", "甲", "1年級", "忠班", "普通科", "", "", true, true, true, false, false));
            broken.Worksheets[0].Cells[0, 2].PutValue("錯欄");
            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(broken, Lookup(Row("91", "00123", "甲", 1)));
            Expect(errors, !validation.IsValid && validation.Students.Count == 0 && MotherGroupExcelApply.IssueText(validation).IndexOf("欄位不符") >= 0, "格式錯誤應列出完整原因且沒有可套用學生");
            bool rejected = false;
            try
            {
                MotherGroupExcelApply.CreateGroups(validation, "", new List<TagDefinition>(), new List<StudentTagLink>());
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Expect(errors, rejected && Signature(list) == before, "驗證失敗不應建立清單，也不應修改原有確認清單");
            return errors;
        }

        private static List<string> CheckBlankScoreAndYear()
        {
            List<string> errors = new List<string>();
            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(
                RoundTrip(Student("S8", "無成績", "高一", "", "", "", "", true, false, false, false, false)),
                Lookup(Row("80", "S8", "無成績", 1)));
            List<StudentMotherGroup> groups = MotherGroupExcelApply.CreateGroups(validation, "", new List<TagDefinition>(), new List<StudentTagLink>());
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(Rows(groups));
            Expect(errors, HistoryCalculationScope.KeepsStudentWithoutScore(list, "80"), "沒有本次成績的 Excel 學生仍應留在確認清單");
            string message = HistoryCalculationScope.UnparsedYearMessage(list);
            SemesterRankStudentSqlResult sql = SemesterRankSql.BuildLegacySelects(list);
            string selects = "";
            foreach (SemesterRankGradeStudents grade in sql.Grades)
            {
                foreach (string select in grade.Selects)
                    selects += select;
            }
            Expect(errors, message.IndexOf("80") >= 0 && message.IndexOf("高一") >= 0 && groups[0].GetWorking(RankKinds.Year).GroupName == "高一", "無法解析的年排名應提示原名稱，不用目前年級取代");
            Expect(errors, sql.SkippedWithoutGrade.Contains("80") && selects.IndexOf("'80'") < 0 && groups[0].GetWorking(RankKinds.Class).GroupName == "", "無法解析年級時不產生新明細，空白班名維持空白");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int next = form.IndexOf("void btnNext_Click");
            int history = form.IndexOf("void BeginHistorySnapshotRestore");
            string nextBody = next >= 0 && history > next ? form.Substring(next, history - next) : "";
            int historyCall = nextBody.IndexOf("BeginHistorySnapshotRestore()");
            int excelCall = nextBody.IndexOf("BeginExcelImport()");
            Expect(errors, historyCall >= 0 && excelCall > historyCall, "Excel 匯入不應取代歷史來源流程");
            int begin = form.IndexOf("void BeginExcelImport");
            int end = form.IndexOf("bool IsHistorySourceSelected");
            string body = begin >= 0 && end > begin ? form.Substring(begin, end - begin) : "";
            int failed = body.IndexOf("!validation.IsValid");
            int events = body.IndexOf("EnsureConfirmEvents()");
            int show = body.IndexOf("ShowConfirmList");
            int cancel = body.IndexOf("ShowDialog() != DialogResult.OK");
            Expect(errors, failed >= 0 && events > failed && show > events && body.IndexOf("MotherGroupExcelImport.SelectSql") >= 0 && body.IndexOf("MotherGroupExcelApply.CreateGroups") >= 0, "Excel 成功進入確認清單前應初始化編輯事件");
            Expect(errors, cancel >= 0 && cancel < events && Count(body, "EnsureConfirmEvents()") == 1, "取消或驗證失敗不應初始化事件，成功路徑只呼叫一次");
            Expect(errors, body.IndexOf("INSERT") < 0 && body.IndexOf("UPDATE") < 0 && body.IndexOf("DELETE") < 0 && body.IndexOf("_confirmList =") < 0, "匯入流程不應寫入資料庫或在失敗前改確認清單");
            string snapshot = File.ReadAllText("MotherGroup\\MotherGroupSnapshot.cs", Encoding.UTF8);
            Expect(errors, snapshot.IndexOf("ExcelSourceName") >= 0, "Excel 母群來源應能對應既有快照種類");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            string rank = File.ReadAllText("MotherGroup\\SemesterRankSql.cs", Encoding.UTF8);
            Expect(errors, rank.IndexOf("subject_origin_score IS NOT NULL") < 0 || form.IndexOf("subject_origin_score IS NOT NULL") >= 0, "沒有成績時仍沿用既有分數條件");
            int currentEvents = nextBody.IndexOf("EnsureConfirmEvents()");
            Expect(errors, excelCall < currentEvents && Count(nextBody, "EnsureConfirmEvents()") == 1, "目前學生來源仍在自己的下一步流程初始化事件");
            int historyEnd = form.IndexOf("void ShowConfirmList");
            string historyBody = history >= 0 && historyEnd > history ? form.Substring(history, historyEnd - history) : "";
            int historyEvents = historyBody.IndexOf("EnsureConfirmEvents()");
            Expect(errors, historyEvents >= 0 && historyEvents < historyBody.IndexOf("ShowConfirmList") && Count(historyBody, "EnsureConfirmEvents()") == 1, "歷史來源仍在原本位置初始化事件");
            int guard = form.IndexOf("if (_confirmEventsReady)");
            int mark = form.IndexOf("_confirmEventsReady = true");
            int handler = form.IndexOf("CellValueChanged +=");
            Expect(errors, guard >= 0 && mark > guard && handler > mark && Count(form, "_confirmEventsReady = true") == 1 && Count(form, "CellValueChanged +=") == 1, "確認清單事件應只綁定一次");
            int firstSource = form.IndexOf("bool IsExcelSourceSelected");
            Expect(errors, firstSource > 0 && form.IndexOf("EnsureConfirmEvents()", 0, firstSource) < 0, "第一次開啟畫面時尚未綁定確認清單事件");
            return errors;
        }

        private static List<string> CheckExcelEditEvents()
        {
            List<string> errors = new List<string>();
            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(
                RoundTrip(Student("00123", "甲", "2年級", "忠班", "普通科", "甲組", "乙組", true, true, true, true, false)),
                Lookup(Row("91", "00123", "甲", 1)));
            List<StudentMotherGroup> groups = MotherGroupExcelApply.CreateGroups(validation, "", new List<TagDefinition>(), new List<StudentTagLink>());
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            Form form = new Form();
            form.ShowInTaskbar = false;
            form.Show();
            DataGridView grid = new DataGridView();
            grid.Size = new System.Drawing.Size(1100, 180);
            form.Controls.Add(grid);
            Label summary = new Label();
            summary.AutoSize = false;
            summary.Size = new System.Drawing.Size(400, 24);
            form.Controls.Add(summary);
            DataGridView population = new DataGridView();
            population.Size = new System.Drawing.Size(400, MotherGroupConfirmGrid.PopulationAreaHeight());
            form.Controls.Add(population);
            MotherGroupConfirmGrid.ConfigurePopulation(population);
            DataGridView detail = new DataGridView();
            form.Controls.Add(detail);
            MotherGroupConfirmGrid.ConfigureDetail(detail);

            bool binding = false;
            bool ready = false;
            int attached = 0;
            System.Action bind = delegate
            {
                if (ready)
                    return;
                ready = true;
                attached++;
                grid.CurrentCellDirtyStateChanged += delegate
                {
                    if (grid.IsCurrentCellDirty)
                        grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                };
                grid.CellValueChanged += delegate (object sender, DataGridViewCellEventArgs e)
                {
                    if (binding)
                        return;
                    MotherGroupConfirmGrid.ApplyCellEdit(grid, list, e.RowIndex, e.ColumnIndex);
                    MotherGroupConfirmGrid.RefreshSummary(summary, population, detail, list);
                };
            };
            Expect(errors, attached == 0, "第一次開啟且尚未匯入成功時，不應先綁定編輯事件");
            list.Load(Rows(groups));
            bind();
            bind();
            Expect(errors, attached == 1, "再次進入確認清單不應重複綁定事件");
            binding = true;
            MotherGroupConfirmGrid.Configure(grid);
            MotherGroupConfirmGrid.Show(grid, list.Rows);
            binding = false;
            MotherGroupConfirmGrid.RefreshSummary(summary, population, detail, list);
            grid.ContextMenuStrip = MotherGroupConfirmGrid.CreateParticipationMenu(grid, list, summary, population, detail);

            SetParticipate(grid, RankKinds.Year, false);
            Expect(errors, !groups[0].GetWorking(RankKinds.Year).Participate && groups[0].GetWorking(RankKinds.Year).GroupName == "2年級", "修改年排名參與應更新確認清單且不改母群名稱");
            Expect(errors, PopulationValue(population, RankKinds.Year, "populationParticipate") == "0", "修改年排名後人數統計應同步");
            SetParticipate(grid, RankKinds.Class, false);
            SetParticipate(grid, RankKinds.Department, false);
            SetParticipate(grid, RankKinds.Tag1, false);
            SetParticipate(grid, RankKinds.Tag2, true);
            Expect(errors, !groups[0].GetWorking(RankKinds.Class).Participate && !groups[0].GetWorking(RankKinds.Department).Participate && !groups[0].GetWorking(RankKinds.Tag1).Participate && groups[0].GetWorking(RankKinds.Tag2).Participate, "五種排名參與都應能修改");
            Expect(errors, PopulationValue(population, RankKinds.Tag2, "populationParticipate") == "1" && PopulationValue(population, RankKinds.Class, "populationExclude") == "1", "五種修改後人數統計應同步");

            grid.ClearSelection();
            grid.Rows[0].Selected = true;
            ClickMenu(grid, "年排名設為參與");
            Expect(errors, groups[0].GetWorking(RankKinds.Year).Participate && PopulationValue(population, RankKinds.Year, "populationParticipate") == "1", "批次設定應更新確認清單與人數");
            list.Restore();
            binding = true;
            MotherGroupConfirmGrid.RefreshParticipation(grid, list);
            binding = false;
            MotherGroupConfirmGrid.RefreshSummary(summary, population, detail, list);
            Expect(errors, groups[0].GetWorking(RankKinds.Year).Participate && !groups[0].GetWorking(RankKinds.Tag2).Participate && groups[0].GetWorking(RankKinds.Class).GroupName == "忠班", "還原參與應回到 Excel 套用後的初始值");

            SetParticipate(grid, RankKinds.Year, false);
            Workbook exported = MotherGroupExcelExchange.Create(list);
            Expect(errors, exported.Worksheets[0].Cells[1, 8].StringValue == "不參與" && exported.Worksheets[0].Cells[1, 3].StringValue == "2年級" && exported.Worksheets[0].Cells[1, 2].StringValue == "一般", "匯出應使用修改後的參與狀態，並保留母群名稱與學生狀態");
            form.Close();
            return errors;
        }

        private static void SetParticipate(DataGridView grid, string rankType, bool participate)
        {
            grid.Rows[0].Cells[MotherGroupConfirmGrid.ParticipateColumn(rankType)].Value = participate;
        }

        private static void ClickMenu(DataGridView grid, string text)
        {
            foreach (ToolStripItem item in grid.ContextMenuStrip.Items)
            {
                if (item.Text == text)
                {
                    item.PerformClick();
                    return;
                }
            }
        }

        private static string PopulationValue(DataGridView population, string rankType, string column)
        {
            foreach (DataGridViewRow row in population.Rows)
            {
                if ((row.Tag as string) == rankType)
                    return row.Cells[column].Value.ToString();
            }
            return "";
        }

        private static int Count(string text, string token)
        {
            int count = 0;
            int index = 0;
            while (text != null && (index = text.IndexOf(token, index)) >= 0)
            {
                count++;
                index += token.Length;
            }
            return count;
        }

        private static Workbook RoundTrip(params StudentMotherGroup[] students)
        {
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup student in students)
                rows.Add(new ConfirmStudentRow { Group = student });
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(rows);
            Workbook created = MotherGroupExcelExchange.Create(list);
            MemoryStream stream = new MemoryStream();
            created.Save(stream, SaveFormat.Xlsx);
            stream.Position = 0;
            return MotherGroupExcelExchange.Open(stream);
        }

        private static StudentMotherGroup Student(string number, string name, string year, string className, string department, string tag1, string tag2, bool yearOn, bool classOn, bool departmentOn, bool tag1On, bool tag2On)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = number;
            student.StudentNumber = number;
            student.StudentName = name;
            student.Status = 1;
            student.SetSourceSlots(new List<RankSlot>
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = year, Participate = yearOn },
                new RankSlot { RankType = RankKinds.Department, GroupName = department, Participate = departmentOn },
                new RankSlot { RankType = RankKinds.Class, GroupName = className, Participate = classOn },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = tag1, Participate = tag1On },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = tag2, Participate = tag2On }
            });
            return student;
        }

        private static List<ConfirmStudentRow> Rows(List<StudentMotherGroup> groups)
        {
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup group in groups)
                rows.Add(new ConfirmStudentRow { Group = group });
            return rows;
        }

        private static int RankCount(MotherGroupCountSummary summary, string rankType, bool participate)
        {
            foreach (RankPopulationCount rank in summary.Ranks)
            {
                if (rank.RankType == rankType)
                    return participate ? rank.ParticipateCount : rank.ExcludeCount;
            }
            return -1;
        }

        private static string Signature(MotherGroupConfirmList list)
        {
            StringBuilder text = new StringBuilder();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                text.Append(row.Group.StudentNumber).Append('|').Append(row.Group.StudentName);
                foreach (string rankType in RankKinds.All)
                {
                    RankSlot slot = row.Group.GetWorking(rankType);
                    text.Append('|').Append(slot.GroupName).Append('|').Append(slot.Participate);
                }
            }
            return text.ToString();
        }

        private static List<TagDefinition> Tags()
        {
            return new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } };
        }

        private static List<StudentTagLink> Links(string studentId)
        {
            return new List<StudentTagLink> { new StudentTagLink { StudentId = studentId, TagId = "ex", Name = "休學" } };
        }

        private static DataTable Lookup(params object[][] rows)
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id");
            table.Columns.Add("student_number");
            table.Columns.Add("student_name");
            table.Columns.Add("student_status");
            if (rows != null)
            {
                foreach (object[] row in rows)
                    table.Rows.Add(row);
            }
            return table;
        }

        private static object[] Row(string id, string number, string name, int status)
        {
            return new object[] { id, number, name, status };
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
