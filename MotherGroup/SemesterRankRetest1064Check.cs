using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using SHEvaluation.Rank;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankRetest1064Check
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckSemesterBatches());
                errors.AddRange(CheckEditablePeriod());
                errors.AddRange(CheckExcelTemplate());
                errors.AddRange(CheckParticipationHint());
                errors.AddRange(CheckGradeSource());
                errors.AddRange(CheckFormWiring());
                errors.AddRange(CheckDesignerReasonColumn());
                errors.AddRange(CheckSourceSwitchEvents());
                errors.AddRange(CheckHistoryQueryFailure());
                if (errors.Count == 0)
                {
                    Console.WriteLine("semester-rank-retest-1064: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("screen-executed: true");
                    Console.WriteLine("school-batch-285: not-run");
                    Console.WriteLine("cross-semester-calc: not-run");
                    Console.WriteLine("excel-file-import: not-run");
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

        private static List<string> CheckSemesterBatches()
        {
            List<string> errors = new List<string>();
            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("school_year");
            table.Columns.Add("semester");
            table.Columns.Add("calculation_description");
            table.Columns.Add("setting");
            table.Columns.Add("extension");
            table.Rows.Add("285", "114", "2", "學期成績", Setting("114", "2", "學期成績"), "[]");
            for (int id = 272; id <= 277; id++)
                table.Rows.Add(id.ToString(), "114", "2", "定期", Setting("114", "2", id == 272 ? "複習考" : "期中考"), "");
            table.Rows.Add("200", "113", "1", "只有定期", Setting("113", "1", "期中考"), "");
            List<HistoryRankBatch> listed = SemesterHistoryBatchCatalog.FilterPeriod(SemesterHistoryBatchCatalog.Read(table), "114", "2");
            SemesterHistoryBatchCatalog.SortByIdDescending(listed);
            Expect(errors, listed.Count == 1 && listed[0].Id == "285" && listed[0].ExamName == "學期成績", "114-2 只可選學期批次 285");
            List<HistoryRankBatch> empty = SemesterHistoryBatchCatalog.FilterPeriod(SemesterHistoryBatchCatalog.Read(table), "113", "1");
            Expect(errors, empty.Count == 0, "只有定期評量的期間沒有可選批次");
            string sourceType;
            Expect(errors, SemesterHistoryBatchCatalog.TryClassifyExam("學期成績", out sourceType) && sourceType == "學期成績", "考試名稱學期成績要能通過");
            Expect(errors, !SemesterHistoryBatchCatalog.TryClassifyExam("期中考", out sourceType), "定期評量考試名稱不能進入學期排名清單");
            HistorySnapshotLoad rejected = HistorySnapshotRestore.LoadSemester("272", "[]", Setting("114", "2", "期中考"), "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, !rejected.Restored && rejected.Message == HistorySnapshotRestore.UnrecognizedExamMessage, "下一步要拒絕非學期成績批次");
            return errors;
        }

        private static List<string> CheckEditablePeriod()
        {
            List<string> errors = new List<string>();
            EditableCalculationPeriodMemory memory = new EditableCalculationPeriodMemory();
            memory.Remember("114", "2");
            memory.Remember("115", "1");
            CalculationPeriod period = CalculationPeriod.CreateDefault("114", "2");
            period.SetHistoryPeriod("114", "2");
            period.SetCalculationPeriod(memory.SchoolYear, memory.Semester);
            Expect(errors, memory.SchoolYear == "115" && memory.Semester == "1" && period.SchoolYear == "115" && period.Semester == "1" && period.HistorySchoolYear == "114", "歷史或 Excel 的計算期間要能恢復，而且不改歷史期間");
            period.SetCalculationPeriod("114", "2");
            Expect(errors, memory.SchoolYear == "115" && memory.Semester == "1", "切回目前學生時，先前可編輯的計算期間仍留著");
            return errors;
        }

        private static List<string> CheckExcelTemplate()
        {
            List<string> errors = new List<string>();
            string path = Path.Combine(Path.GetTempPath(), "semester-rank-template-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                Workbook blank = MotherGroupExcelExchange.CreateBlankTemplate();
                MotherGroupExcelExchange.SaveWorkbook(path, blank);
                Workbook opened = MotherGroupExcelExchange.Open(path);
                Worksheet sheet = opened.Worksheets["母群"];
                Expect(errors, sheet != null, "空白範本要有母群工作表");
                if (sheet == null)
                    return errors;
                for (int column = 0; column < MotherGroupExcelExchange.Headers.Length; column++)
                    Expect(errors, sheet.Cells[0, column].StringValue == MotherGroupExcelExchange.Headers[column], "空白範本欄位應為第 " + (column + 1) + " 欄");
                Expect(errors, sheet.Cells.MaxDataRow == 0, "空白範本只有表頭");
                Expect(errors, sheet.Cells[0, 0].GetStyle().Number == 49 || sheet.Cells.Columns[0].Style.Number == 49, "學號欄要預設為文字格式");
                string example = "001234｜示意學生｜一般｜2年級｜高二甲｜普通科｜類別甲｜類別乙｜參與｜參與｜參與｜不參與｜不參與";
                string[] exampleCells = example.Split('｜');
                Expect(errors, exampleCells.Length == MotherGroupExcelExchange.Headers.Length, "填寫說明的示意要有 13 欄");
                Expect(errors, MotherGroupExcelExchange.FillGuideText.IndexOf(example) >= 0 && MotherGroupExcelExchange.FillGuideText.IndexOf("學號＋學生狀態") >= 0, "填寫說明要依表頭順序顯示完整示意，並交代學號加狀態");
                Expect(errors, sheet.Cells[1, 0].StringValue != "001234" && sheet.Cells[1, 1].StringValue != "示意學生", "下載範本不含示意資料");

                PutRow(sheet, "001234", "測試生", "一般", "2年級", "高一甲", "普通科", "Excel類別甲", "Excel類別乙", "不參與", "參與", "不參與", "不參與", "不參與");
                string filled = Path.Combine(Path.GetTempPath(), "semester-rank-filled-" + Guid.NewGuid().ToString("N") + ".xlsx");
                opened.Save(filled);
                Workbook reread = MotherGroupExcelExchange.Open(filled);
                Expect(errors, reread.Worksheets["母群"].Cells[1, 0].StringValue == "001234", "匯入檔要保留學號前導零");
                MotherGroupExcelValidation valid = MotherGroupExcelImport.Validate(reread, StudentTable());
                Expect(errors, valid.IsValid && valid.Students.Count == 1 && valid.Students[0].StudentNumber == "001234" && valid.Students[0].StudentId == "88001", "符合測試資料的一列要能匯入");
                reread.Worksheets["母群"].Cells[1, 9].PutValue("參加");
                MotherGroupExcelValidation invalid = MotherGroupExcelImport.Validate(reread, StudentTable());
                Expect(errors, !invalid.IsValid && invalid.Students.Count == 0 && MotherGroupExcelApply.IssueText(invalid).IndexOf("參與") >= 0, "不是參與或不參與時，既有匯入器要擋下整份檔案");
                try
                {
                    File.Delete(filled);
                }
                catch (IOException)
                {
                }
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
            return errors;
        }

        private static List<string> CheckParticipationHint()
        {
            List<string> errors = new List<string>();
            string path = Path.Combine(Path.GetTempPath(), "semester-rank-hint-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                Workbook workbook = MotherGroupExcelExchange.CreateBlankTemplate();
                PutRow(workbook.Worksheets[0], "001234", "測試生", "一般", "2年級", "高一甲", "普通科", "Excel類別甲", "Excel類別乙", "不參與", "參與", "不參與", "不參與", "不參與");
                workbook.Save(path);
                MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(MotherGroupExcelExchange.Open(path), StudentTable());
                List<TagDefinition> tags = new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } };
                List<StudentTagLink> links = new List<StudentTagLink> { new StudentTagLink { StudentId = "88001", TagId = "ex", Name = "休學" } };
                List<StudentMotherGroup> groups = MotherGroupExcelApply.CreateGroups(validation, "不排名", tags, links);
                Expect(errors, groups.Count == 1, "匯入應建立一名學生");
                if (groups.Count != 1)
                    return errors;
                StudentMotherGroup student = groups[0];
                Expect(errors, student.MatchesExcludedCategory && !student.GetWorking(RankKinds.Class).Participate && student.GetWorking(RankKinds.Class).GroupName == "高一甲", "符合不排名類別時班排名預設不參與，母群名稱仍是 Excel 的值");
                Expect(errors, student.GetWorking(RankKinds.Tag1).GroupName == "Excel類別甲" && student.GetWorking(RankKinds.Tag2).GroupName == "Excel類別乙", "Excel 類別母群名稱不被圖1類別排名設定覆寫");
                MotherGroupConfirmList list = new MotherGroupConfirmList();
                list.Load(new ConfirmStudentRow[] { new ConfirmStudentRow { Group = student } });
                using (DataGridView grid = new DataGridView())
                {
                    MotherGroupConfirmGrid.Configure(grid);
                    MotherGroupConfirmGrid.Show(grid, list.Rows);
                    Expect(errors, ("" + grid.Rows[0].Cells["colExcludedReason"].Value) == SemesterRankSourceChrome.ExcludedRowHint, "確認清單要顯示預設不參與原因");
                    Expect(errors, grid.Columns["colExcludedReason"].ReadOnly, "預設原因欄是唯讀");
                    list.SetParticipate(new string[] { "88001" }, RankKinds.Class, true);
                    MotherGroupConfirmGrid.RefreshParticipation(grid, list);
                    Expect(errors, student.GetWorking(RankKinds.Class).Participate && student.GetWorking(RankKinds.Class).GroupName == "高一甲", "手動勾回後，計算採用改後的參與，母群名稱不變");
                    Expect(errors, ("" + grid.Rows[0].Cells["colExcludedReason"].Value) == SemesterRankSourceChrome.ExcludedRowHint, "改回參與後仍看得到當初的預設原因");
                }
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
            return errors;
        }

        private static List<string> CheckGradeSource()
        {
            List<string> errors = new List<string>();
            CurrentStudentRow current = new CurrentStudentRow();
            current.StudentId = "1";
            current.StudentNumber = "001234";
            current.StudentName = "目前生";
            current.IsRegular = true;
            current.StatusValue = 1;
            current.RefClassId = "class";
            current.GradeYear = 1;
            current.ClassName = "高一甲";
            List<StudentMotherGroup> currentGroups = CurrentStudentMotherGroupBuilder.Build(
                new CurrentStudentRow[] { current },
                new List<TagDefinition>(),
                new List<StudentTagLink>(),
                new int[] { 1 },
                "",
                "",
                "",
                new Dictionary<string, string>());
            Expect(errors, currentGroups.Count == 1 && currentGroups[0].GetWorking(RankKinds.Year).GroupName == "1年級", "目前學生依目前班級年級產生年排名母群");
            List<StudentMotherGroup> otherGrade = CurrentStudentMotherGroupBuilder.Build(
                new CurrentStudentRow[] { current },
                new List<TagDefinition>(),
                new List<StudentTagLink>(),
                new int[] { 2 },
                "",
                "",
                "",
                new Dictionary<string, string>());
            Expect(errors, otherGrade.Count == 0, "目前學生不在勾選的班級年級時不進入名單");

            StudentMotherGroup excel = new StudentMotherGroup();
            excel.StudentId = "88001";
            excel.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = "2年級", Participate = true },
                new RankSlot { RankType = RankKinds.Class, GroupName = "高一甲", Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = "普通科", Participate = true },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "Excel類別甲", Participate = true },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
            });
            int parsed;
            Expect(errors, excel.GetWorking(RankKinds.Year).GroupName == "2年級" && SemesterRankSql.TryParseGradeYear(excel.GetWorking(RankKinds.Year).GroupName, out parsed) && parsed == 2, "Excel 以年排名母群判定年級，不用目前班級年級 1");
            StudentMotherGroup unresolved = new StudentMotherGroup();
            unresolved.StudentId = "88002";
            unresolved.StudentNumber = "88002";
            unresolved.StudentName = "無法判定";
            unresolved.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = "高一", Participate = true }
            });
            MotherGroupConfirmList kept = new MotherGroupConfirmList();
            kept.Load(new ConfirmStudentRow[] { new ConfirmStudentRow { Group = unresolved } });
            Expect(errors, kept.Rows.Count == 1 && !SemesterRankSql.TryParseGradeYear("高一", out parsed) && HistoryCalculationScope.UnparsedYearMessage(kept).IndexOf("88002") >= 0, "無法判定年級的學生留在確認清單，並沿用原提示");
            return errors;
        }

        private static List<string> CheckFormWiring()
        {
            List<string> errors = new List<string>();
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            string designer = File.ReadAllText("CalculateSemesterAssessmentRank.Designer.cs", Encoding.UTF8);
            Expect(errors, designer.IndexOf("計算學年度：") >= 0 && designer.IndexOf("計算學期：") >= 0 && designer.IndexOf(SemesterRankSourceChrome.CalcPeriodHint) >= 0, "設計畫面要標出計算學年度、學期與用途");
            Expect(errors, designer.IndexOf("下載匯入範本") >= 0 && designer.IndexOf("填寫說明") >= 0 && designer.IndexOf(SemesterRankSourceChrome.ExcludedCategoryHint) >= 0 && designer.IndexOf("計算年級") >= 0, "不排名說明、計算年級、Excel 按鈕要放在 Designer");
            Expect(errors, form.IndexOf("SetControlVisible(history") >= 0 && form.IndexOf("SetControlVisible(excel") >= 0 && form.IndexOf("SetControlVisible(current") >= 0, "切換來源時要一併顯示或隱藏專屬欄位");
            Expect(errors, form.IndexOf("LoadHistoryBatches(false)") >= 0 && form.IndexOf("ClearHistoryBatchSelection()") >= 0, "切換歷史期間要清除批次選取");
            Expect(errors, form.IndexOf("NoSemesterBatchText") >= 0 && form.IndexOf("btnNext.Enabled = hasBatch && selected") >= 0, "沒有學期成績批次時停用下一步");
            Expect(errors, form.IndexOf("TryClassifyExam(selected.ExamName") >= 0 && form.IndexOf("lbSchoolYear.Text = calculationSchoolYear") >= 0, "下一步驗證批次類型，成績期間仍用計算學年度");
            Expect(errors, form.IndexOf("CreateBlankTemplate()") >= 0 && form.IndexOf("SaveFileDialog") >= 0 && form.IndexOf("dialog.ShowDialog() != DialogResult.OK") >= 0, "範本以另存新檔產生，取消則留在設定頁");
            Expect(errors, form.IndexOf("RememberEditableCalculation()") >= 0 && form.IndexOf("ShowEditableCalculation()") >= 0 && form.IndexOf("RestoreSystemPeriod") >= 0, "目前學生顯示系統期間，歷史與 Excel 恢復先前的計算期間");
            return errors;
        }

        private static void PutRow(Worksheet sheet, params string[] values)
        {
            for (int column = 0; column < values.Length; column++)
            {
                Cell cell = sheet.Cells[1, column];
                if (column == 0)
                {
                    Style style = cell.GetStyle();
                    style.Number = 49;
                    cell.SetStyle(style);
                    cell.PutValue(values[column], false);
                }
                else
                {
                    cell.PutValue(values[column]);
                }
            }
        }

        private static DataTable StudentTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id");
            table.Columns.Add("student_number");
            table.Columns.Add("student_name");
            table.Columns.Add("student_status");
            table.Rows.Add("88001", "001234", "測試生", "1");
            return table;
        }

        private static string Setting(string schoolYear, string semester, string examName)
        {
            return "<Setting 學年度=\"" + schoolYear + "\" 學期=\"" + semester + "\" 考試名稱=\"" + examName + "\"><年級>1</年級></Setting>";
        }

        private const string ReadyText = "說明：班、科、年排名會自動計算。分項成績擇優項目請參考成績計算規則。";

        private static List<string> CheckDesignerReasonColumn()
        {
            List<string> errors = new List<string>();
            SemesterRankMotherGroupConfirmForm confirm = null;
            try
            {
                confirm = new SemesterRankMotherGroupConfirmForm();
                DataGridView grid = confirm.StudentGrid;
                DataGridViewColumn designed = grid.Columns["colExcludedReason"];
                Expect(errors, designed != null && designed.HeaderText == "預設原因" && designed.ReadOnly && designed.Width == 240 && designed.DisplayIndex == 5, "Designer 要有唯讀、欄寬 240、順序 5 的預設原因");
                if (designed == null)
                    return errors;
                designed.Width = 188;
                designed.DisplayIndex = 2;
                MotherGroupConfirmGrid.Configure(grid);
                DataGridViewColumn after = grid.Columns["colExcludedReason"];
                Expect(errors, object.ReferenceEquals(designed, after) && after.Width == 188 && after.DisplayIndex == 2 && after.ReadOnly, "執行時要保留 Designer 欄寬、順序與同一個欄位物件");
                Expect(errors, CountReasonColumns(grid) == 1, "第一次開啟確認頁只能有一個預設原因欄");
                MotherGroupConfirmGrid.Configure(grid);
                Expect(errors, CountReasonColumns(grid) == 1 && object.ReferenceEquals(designed, grid.Columns["colExcludedReason"]) && grid.Columns["colExcludedReason"].Width == 188 && grid.Columns["colExcludedReason"].DisplayIndex == 2, "再次開啟確認頁仍是同一個預設原因欄");

                StudentMotherGroup student = new StudentMotherGroup();
                student.StudentId = "88001";
                student.StudentNumber = "001234";
                student.StudentName = "示意學生";
                student.MatchesExcludedCategory = true;
                student.GetWorking(RankKinds.Class).GroupName = "高二甲";
                student.GetWorking(RankKinds.Class).Participate = false;
                MotherGroupConfirmList list = new MotherGroupConfirmList();
                list.Load(new ConfirmStudentRow[] { new ConfirmStudentRow { Group = student, ClassName = "高二甲", SeatNo = "1" } });
                MotherGroupConfirmGrid.Show(grid, list.Rows);
                Expect(errors, ("" + grid.Rows[0].Cells["colExcludedReason"].Value) == SemesterRankSourceChrome.ExcludedRowHint, "符合不排名類別時，Designer 欄要顯示預設原因");
                list.SetParticipate(new string[] { "88001" }, RankKinds.Class, true);
                MotherGroupConfirmGrid.RefreshParticipation(grid, list);
                Expect(errors, student.GetWorking(RankKinds.Class).Participate && ("" + grid.Rows[0].Cells["colExcludedReason"].Value) == SemesterRankSourceChrome.ExcludedRowHint, "手動改回參與後仍看得到預設原因");
                Console.WriteLine("designer-reason: columns=" + CountReasonColumns(grid) + " width=" + grid.Columns["colExcludedReason"].Width + " index=" + grid.Columns["colExcludedReason"].DisplayIndex + " kept-after-participate=true");
            }
            catch (Exception ex)
            {
                errors.Add("確認頁 Designer 欄位驗證失敗：" + ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                if (confirm != null)
                    confirm.Dispose();
            }
            return errors;
        }

        private static List<string> CheckSourceSwitchEvents()
        {
            List<string> errors = new List<string>();
            CalculateSemesterAssessmentRank form = null;
            try
            {
                form = new CalculateSemesterAssessmentRank();
                SetField(form, "_DefaultSchoolYear", "114");
                SetField(form, "_DefaultSemester", "2");
                CalculationPeriod period = CalculationPeriod.CreateDefault("114", "2");
                SetField(form, "_calculationPeriod", period);
                NumericUpDown calcYear = (NumericUpDown)GetField(form, "_nudCalcSchoolYear");
                ComboBox calcSemester = (ComboBox)GetField(form, "_cboCalcSemester");
                ComboBox source = (ComboBox)GetField(form, "_cboMotherGroupSource");
                NumericUpDown historyYear = (NumericUpDown)GetField(form, "_nudHistorySchoolYear");
                ComboBox historySemester = (ComboBox)GetField(form, "_cboHistorySemester");
                ComboBox batches = (ComboBox)GetField(form, "_cboHistoryBatch");
                CalculationPeriodBinder.Apply(calcYear, calcSemester, source, historyYear, historySemester, period);
                SetField(form, "_screenLoadSettled", true);
                SetField(form, "_screenLoadFailed", false);
                SetField(form, "_nextStepRunning", false);
                form.GetType().GetMethod("EnsureHistorySourceEvents", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
                Control next = (Control)GetField(form, "btnNext");
                Control status = (Control)GetField(form, "labelX6");
                status.Text = ReadyText;
                next.Enabled = true;

                SetHistoryQuery(EmptyBatchTable);
                source.SelectedItem = CalculationPeriod.HistorySourceName;
                Expect(errors, !next.Enabled && status.Text == SemesterRankSourceChrome.NoSemesterBatchText && next.Text == SemesterRankSourceChrome.NextText, "歷史沒有批次時，下一步停用並顯示無批次提示");
                source.SelectedItem = CalculationPeriod.ExcelSourceName;
                Expect(errors, next.Enabled && status.Text == ReadyText && next.Text == SemesterRankSourceChrome.ExcelNextText, "歷史無批次切到 Excel 後，要清除提示並啟用選擇 Excel 檔案");
                Console.WriteLine("observed empty-to-excel: enabled=" + next.Enabled + " text=" + next.Text + " label=" + status.Text);
                source.SelectedItem = CalculationPeriod.HistorySourceName;
                Expect(errors, !next.Enabled && status.Text == SemesterRankSourceChrome.NoSemesterBatchText, "從 Excel 切回沒有批次的歷史模式時，下一步停用並恢復無批次提示");
                source.SelectedItem = CalculationPeriod.CurrentSourceName;
                Expect(errors, next.Enabled && status.Text == ReadyText && next.Text == SemesterRankSourceChrome.NextText, "歷史無批次切到目前學生後，要清除提示並啟用下一步");
                Console.WriteLine("observed empty-to-current: enabled=" + next.Enabled + " text=" + next.Text + " label=" + status.Text);

                SetHistoryQuery(BatchTable);
                source.SelectedItem = CalculationPeriod.HistorySourceName;
                Expect(errors, batches.Items.Count == 1 && batches.SelectedIndex < 0 && !next.Enabled && status.Text == ReadyText, "有批次但尚未選取時，下一步停用並清除無批次提示");
                status.Text = SemesterRankSourceChrome.NoSemesterBatchText;
                batches.SelectedIndex = 0;
                Expect(errors, next.Enabled && status.Text == ReadyText && batches.SelectedItem is HistoryRankBatch, "選取有效批次後啟用下一步，並清除無批次提示");
                batches.SelectedIndex = -1;
                Expect(errors, !next.Enabled && status.Text == ReadyText, "取消批次選取後停用下一步，並維持清除無批次提示");
                source.SelectedItem = CalculationPeriod.ExcelSourceName;
                Expect(errors, next.Enabled && status.Text == ReadyText && next.Text == SemesterRankSourceChrome.ExcelNextText, "有批次但未選取時切到 Excel，要啟用選擇 Excel 檔案並清除提示");
                Console.WriteLine("observed unselected-to-excel: enabled=" + next.Enabled + " text=" + next.Text + " label=" + status.Text);
                source.SelectedItem = CalculationPeriod.CurrentSourceName;
                Expect(errors, next.Enabled && next.Text == SemesterRankSourceChrome.NextText && status.Text == ReadyText, "有批次但未選取時切到目前學生，要啟用下一步並清除提示");
                source.SelectedItem = CalculationPeriod.HistorySourceName;
                Expect(errors, batches.SelectedIndex < 0 && !next.Enabled && status.Text == ReadyText && next.Text == SemesterRankSourceChrome.NextText, "切回有批次的歷史模式且尚未選取時，下一步停用且不顯示無批次提示");
                batches.SelectedIndex = 0;
                Expect(errors, next.Enabled, "切回歷史模式後改為選取批次，下一步改為啟用");
                string selectedId = ((HistoryRankBatch)batches.SelectedItem).Id;
                source.SelectedItem = CalculationPeriod.ExcelSourceName;
                Expect(errors, next.Enabled && next.Text == SemesterRankSourceChrome.ExcelNextText, "已選批次再切到 Excel 時，選擇 Excel 檔案保持啟用");
                source.SelectedItem = CalculationPeriod.HistorySourceName;
                Expect(errors, batches.SelectedIndex < 0 && !next.Enabled && status.Text == ReadyText, "離開歷史會清掉批次選取，切回後依未選取狀態停用下一步");
                Console.WriteLine("observed switch-back: selected-before=" + selectedId + " enabled=" + next.Enabled + " selectedIndex=" + batches.SelectedIndex + " label=" + status.Text);

                SetField(form, "_nextStepRunning", true);
                status.Text = "正在匯入 Excel 母群資料...";
                next.Enabled = false;
                source.SelectedItem = CalculationPeriod.CurrentSourceName;
                Expect(errors, !next.Enabled && status.Text == "正在匯入 Excel 母群資料...", "下一步處理中切換來源時，按鈕維持停用且不改處理提示");
                SetField(form, "_nextStepRunning", false);
                SetField(form, "_screenLoadFailed", true);
                status.Text = "載入學生清單失敗：測試";
                next.Enabled = false;
                source.SelectedItem = CalculationPeriod.ExcelSourceName;
                Expect(errors, !next.Enabled && status.Text == "載入學生清單失敗：測試", "初始化失敗後切換來源時，按鈕維持停用且不改錯誤提示");
                SetField(form, "_screenLoadFailed", false);
                SetField(form, "_screenLoadSettled", false);
                status.Text = "正在載入學生清單…";
                next.Enabled = false;
                source.SelectedItem = CalculationPeriod.CurrentSourceName;
                Expect(errors, !next.Enabled && status.Text == "正在載入學生清單…", "畫面載入中切換來源時，按鈕維持停用且不改載入提示");
            }
            catch (Exception ex)
            {
                errors.Add("來源切換表單事件失敗：" + ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                SetHistoryQuery(null);
                SetHistoryFailureDialog(null);
                if (form != null)
                    form.Dispose();
            }
            return errors;
        }

        private static List<string> CheckHistoryQueryFailure()
        {
            List<string> errors = new List<string>();
            CalculateSemesterAssessmentRank form = null;
            try
            {
                form = new CalculateSemesterAssessmentRank();
                SetField(form, "_DefaultSchoolYear", "114");
                SetField(form, "_DefaultSemester", "2");
                CalculationPeriod period = CalculationPeriod.CreateDefault("114", "2");
                SetField(form, "_calculationPeriod", period);
                NumericUpDown calcYear = (NumericUpDown)GetField(form, "_nudCalcSchoolYear");
                ComboBox calcSemester = (ComboBox)GetField(form, "_cboCalcSemester");
                ComboBox source = (ComboBox)GetField(form, "_cboMotherGroupSource");
                NumericUpDown historyYear = (NumericUpDown)GetField(form, "_nudHistorySchoolYear");
                ComboBox historySemester = (ComboBox)GetField(form, "_cboHistorySemester");
                ComboBox batches = (ComboBox)GetField(form, "_cboHistoryBatch");
                CalculationPeriodBinder.Apply(calcYear, calcSemester, source, historyYear, historySemester, period);
                SetField(form, "_screenLoadSettled", true);
                SetField(form, "_screenLoadFailed", false);
                SetField(form, "_nextStepRunning", false);
                form.GetType().GetMethod("EnsureHistorySourceEvents", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
                Control next = (Control)GetField(form, "btnNext");
                Control status = (Control)GetField(form, "labelX6");
                status.Text = ReadyText;
                next.Enabled = true;
                bool failQuery = false;
                string shown = "";
                SetHistoryQuery(delegate
                {
                    if (failQuery)
                        throw new InvalidOperationException("連線中斷");
                    return BatchTable();
                });
                SetHistoryFailureDialog(delegate(string message) { shown = message; });
                source.SelectedItem = CalculationPeriod.HistorySourceName;
                batches.SelectedIndex = 0;
                Expect(errors, next.Enabled && batches.Items.Count == 1 && period.HistoryBatchId == "285" && period.SchoolYear == "114" && period.Semester == "2", "切換前已選 114-2 的學期成績批次，下一步啟用");
                failQuery = true;
                historySemester.SelectedItem = "1";
                Expect(errors, batches.Items.Count == 0 && batches.SelectedIndex < 0 && period.HistoryBatchId == "" && !next.Enabled, "歷史查詢失敗後清單與選取清空，下一步停用");
                Expect(errors, status.Text.IndexOf("歷史批次清單讀取失敗。") == 0 && status.Text.IndexOf("連線中斷") >= 0 && status.Text != SemesterRankSourceChrome.NoSemesterBatchText, "查詢失敗應留下失敗訊息，而不是無批次提示");
                Expect(errors, shown == "歷史批次清單讀取失敗。\n連線中斷", "查詢失敗對話框仍說明讀取失敗與原因");
                Expect(errors, period.SchoolYear == "114" && period.Semester == "2" && period.HistorySemester == "1", "失敗的歷史學期切換不改計算期間");
                Console.WriteLine("observed history-query-failure: items=" + batches.Items.Count + " selected=" + batches.SelectedIndex + " batchId=" + period.HistoryBatchId + " enabled=" + next.Enabled + " label=" + status.Text.Replace("\r", "").Replace("\n", " / "));
                failQuery = false;
                historyYear.Value = 113;
                Expect(errors, batches.Items.Count == 0 && batches.SelectedIndex < 0 && period.HistoryBatchId == "" && !next.Enabled && status.Text == SemesterRankSourceChrome.NoSemesterBatchText, "查詢成功但沒有批次時，仍顯示無批次提示並停用下一步");
                Console.WriteLine("observed empty-after-failure: items=" + batches.Items.Count + " selected=" + batches.SelectedIndex + " enabled=" + next.Enabled + " label=" + status.Text);
            }
            catch (Exception ex)
            {
                errors.Add("歷史批次查詢失敗表單事件失敗：" + ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                SetHistoryQuery(null);
                SetHistoryFailureDialog(null);
                if (form != null)
                    form.Dispose();
            }
            return errors;
        }

        private static DataTable EmptyBatchTable()
        {
            return null;
        }

        private static DataTable BatchTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("school_year");
            table.Columns.Add("semester");
            table.Columns.Add("calculation_description");
            table.Columns.Add("setting");
            table.Columns.Add("extension");
            table.Rows.Add("285", "114", "2", "學期成績", Setting("114", "2", "學期成績"), "[]");
            table.Rows.Add("272", "114", "2", "定期", Setting("114", "2", "期中考"), "");
            return table;
        }

        private static void SetHistoryQuery(Func<DataTable> query)
        {
            CalculateSemesterAssessmentRank.SetHistoryBatchQuery(query);
        }

        private static void SetHistoryFailureDialog(Action<string> dialog)
        {
            CalculateSemesterAssessmentRank.SetHistoryBatchFailureDialog(dialog);
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static int CountReasonColumns(DataGridView grid)
        {
            int count = 0;
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.Name == "colExcludedReason" || column.HeaderText == "預設原因")
                    count++;
            }
            return count;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
