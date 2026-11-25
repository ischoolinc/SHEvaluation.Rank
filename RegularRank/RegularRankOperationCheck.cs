using Aspose.Cells;
using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankOperationCheck
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string root = args != null && args.Length > 0 ? args[0] : @"C:\ischoolProject\SHEvaluation-Regular";
            List<string> errors = new List<string>();
            try
            {
                errors.AddRange(CheckPopulation());
                errors.AddRange(CheckSubjectsAndSnapshot());
                errors.AddRange(CheckParticipationAndPublish(root));
                errors.AddRange(CheckFormulas(root));
                errors.AddRange(CheckOldGridPath(root));
                CheckLargeGrids(errors);
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }
            string reportPath = Path.Combine(root, @"bin\Debug\regular-rank-operation-check.txt");
            List<string> report = new List<string>();
            if (errors.Count == 0)
                report.Add("regular-rank-operation-check: ok");
            else
                report.AddRange(errors);
            report.Add("database-executed: false; ischool-screen-executed: false; vs2022-designer-executed: false");
            File.WriteAllLines(reportPath, report.ToArray(), System.Text.Encoding.UTF8);
            foreach (string line in report)
                Console.WriteLine(line);
            return errors.Count == 0 ? 0 : 1;
        }

        private static List<string> CheckPopulation()
        {
            List<string> errors = new List<string>();
            List<CurrentStudentRow> students = new List<CurrentStudentRow>();
            students.Add(Row("1", "一般生", true, 1, "c1", 1, "高一1"));
            students.Add(Row("2", "延修生", false, RegularRankCurrentStudentSource.ExtendedStatus, "c2", 3, "高三1"));
            students.Add(Row("3", "休學生", false, 4, "c3", 1, "高一2"));
            students.Add(Row("4", "無班級", true, 1, "", 1, ""));
            Dictionary<string, string> seats = new Dictionary<string, string>();
            seats["1"] = "5";
            seats["2"] = "8";
            List<TagDefinition> tags = new List<TagDefinition>();
            tags.Add(new TagDefinition { Id = "t-exclude", Prefix = "不排名", Name = "不排名甲" });
            tags.Add(new TagDefinition { Id = "t1", Prefix = "類別一", Name = "甲類" });
            List<StudentTagLink> links = new List<StudentTagLink>();
            links.Add(new StudentTagLink { StudentId = "1", TagId = "t-exclude", Name = "不排名甲" });
            links.Add(new StudentTagLink { StudentId = "2", TagId = "t1", Name = "甲類" });
            Dictionary<string, string> depts = new Dictionary<string, string>();
            depts["2"] = "";
            List<ConfirmStudentRow> rows;
            string error = RegularRankCurrentStudentSource.Build(
                students, seats, tags, links, new int[] { 1, 3 }, "[不排名]", "[類別一]", "", depts, out rows);
            Expect(errors, error == "" && rows.Count == 2, "一般與延修都保留，休學與無班級排除");
            Expect(errors, rows[0].Group.Status == 1 && rows[0].Group.GetWorking(RankKinds.Year).Participate == false, "排除類別只改參與，一般生狀態仍是 1");
            Expect(errors, rows[0].Group.GetWorking(RankKinds.Year).GroupName == "1年級", "排除後母群名稱保留");
            List<StudentMotherGroup> semesterGroups = CurrentStudentMotherGroupBuilder.Build(
                students, tags, links, new int[] { 1, 3 }, "不排名", "類別一", "", depts);
            Expect(errors, semesterGroups.Count == 1 && semesterGroups[0].StudentId == "1", "學期共用來源仍只收一般生，定期轉接才保留延修");
            Expect(errors, rows[1].Group.Status == 2 && rows[1].Group.StudentName == "延修生", "延修生不以 IsRegular 冒充一般生");
            Expect(errors, rows[1].Group.GetWorking(RankKinds.Department).Participate && rows[1].Group.GetWorking(RankKinds.Department).GroupName == "", "目前來源的空白科別維持參與");
            Expect(errors, rows[1].Group.GetWorking(RankKinds.Tag1).Participate && rows[1].Group.GetWorking(RankKinds.Tag2).Participate == false, "有類別名稱才初始參與，空白類別不參與");
            Expect(errors, rows[1].SeatNo == "8" && rows[1].ClassName == "高三1", "班級與座號來自目前主檔");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(rows);
            list.SetParticipate(new string[] { "2" }, RankKinds.Year, false);
            Expect(errors, list.Rows[1].Group.GetWorking(RankKinds.Class).Participate && list.Rows[1].Group.GetWorking(RankKinds.Year).Participate == false, "只改年排名時其他旗標不變");
            MotherGroupCountSummary summary = list.Summarize();
            Expect(errors, summary.StudentCount == 2, "統計使用清單人數，不查成績");
            List<string> visible = list.VisibleStudentIds("延修");
            Expect(errors, visible.Count == 1 && list.Rows.Count == 2, "搜尋只改可見範圍，不刪除底層清單");
            list.Restore();
            Expect(errors, list.Rows[1].Group.GetWorking(RankKinds.Year).Participate, "恢復作用於完整清單並回到初始參與");
            return errors;
        }

        private static List<string> CheckSubjectsAndSnapshot()
        {
            List<string> errors = new List<string>();
            string sql;
            string error;
            Expect(errors, RegularRankSubjectCatalog.TryCreate("113", "2", "30", new string[] { "10", "20" }, out sql, out error), "可為目標期間組成科目查詢");
            Expect(errors, sql.Contains("course.school_year = 113") && sql.Contains("course.semester = 2") && sql.Contains("sce_take.ref_exam_id = 30"), "科目查詢使用目標期間與 exam ID");
            Expect(errors, sql.Contains("sc_attend.ref_student_id IN (10, 20)") && !sql.Contains("grade_year") && !sql.Contains("_DefaultSchoolYear"), "科目查詢使用母群學生，不靠目前班級年級或系統期間");
            Expect(errors, sql.Contains("course.credit DESC") && sql.Contains("course.period DESC") && sql.Contains("course.subject"), "科目排序維持學分、節數、科目");
            Expect(errors, !RegularRankSubjectCatalog.TryCreate("113", "2", "30", new string[] { "007" }, out sql, out error), "學生識別有前導零時不組 SQL");
            Expect(errors, !RegularRankSubjectCatalog.TryCreate("115", "1", "1", new string[0], out sql, out error), "沒有母群學生時不查科目");

            RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
            context.SetTargetPeriod("113", "2");
            context.SetCurrentSource();
            context.SetTargetExam(RegularRankExamResolution.Resolve("30", "", new RegularRankExamChoice[] { Choice("30", "第一次定期評量") }));
            context.SetGradeYears(new int[] { 1 });
            context.MarkMotherGroupLoaded();
            string applied = context.ApplySubjectCandidates(
                new string[] { "國文", "英文" }, new string[] { "國文", "英文" }, new string[] { "國文", "英文" },
                new string[] { "英文" }, new string[] { "英文" }, new string[] { "英文" });
            Expect(errors, applied == "" && context.GeneralSubjects.Checked.Contains("國文") && !context.GeneralSubjects.Checked.Contains("英文"), "既有不採計科目維持不勾選");
            context.ConfirmSubjects();
            context.SetTargetExam(RegularRankExamResolution.Resolve("31", "", new RegularRankExamChoice[] { Choice("31", "第二次定期評量") }));
            Expect(errors, context.LoadState == RegularRankLoadState.Stale && context.CanCreateRequest().Length > 0, "變更目標考試後不能用舊名單進入下一步");
            context.MarkSubjectLoadFailed();
            Expect(errors, context.CanCreateRequest().Contains("不能用上一份"), "科目載入失敗不能搭配新條件");

            context = RegularRankCalculationContext.CreateDefault("115", "1");
            context.SetTargetPeriod("113", "2");
            context.SetCurrentSource();
            context.SetTargetExam(RegularRankExamResolution.Resolve("30", "", new RegularRankExamChoice[] { Choice("30", "第一次\"評量") }));
            context.SetGradeYears(new int[] { 1 });
            context.MarkMotherGroupLoaded();
            context.ApplySubjectCandidates(new string[] { "國\"文" }, new string[] { "國\"文" }, new string[] { "國\"文" }, new string[0], new string[0], new string[0]);
            context.ConfirmSubjects();
            RegularRankConfirmRequest request = context.CreateRequest();
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[] { SnapshotRow("9", "A\"生", "特別班", false) });
            string json = RegularRankSnapshotDocument.ComposeExtension("[{\"extension_name\":\"OtherNote\",\"keep\":true}]", list);
            Expect(errors, json.Contains("OtherNote") && json.Contains("MotherGroupSnapshot"), "其他 extension 保留，並寫入母群快照");
            MotherGroupJsonRead read = MotherGroupSnapshotJson.Read(json);
            Expect(errors, read.Present && read.Restored && read.Students.Count == 1, "特殊字元與無法解析年級仍可還原");
            Expect(errors, read.Students[0].GetWorking(RankKinds.Year).Participate == false, "false 參與旗標可往返");
            string setting = RegularRankSnapshotDocument.BuildSettingXml(request);
            Expect(errors, setting.Contains("ref_exam_id=\"30\"") && setting.Contains("國") && !setting.Contains("MotherGroupSnapshot"), "Setting XML 保存 exam ID，不以 JSON 取代");
            Expect(errors, RegularRankSnapshotDocument.ConfirmationText(request, 1).Contains("不回滾該設定") && RegularRankSnapshotDocument.ConfirmationText(request, 1).Contains("傳輸檢查未通過"), "確認文案說明組態保存與傳輸檢查邊界");
            return errors;
        }

        private static List<string> CheckParticipationAndPublish(string root)
        {
            List<string> errors = new List<string>();
            string rankSql = File.ReadAllText(Path.Combine(root, "CalculateRegularAssessmentRank.cs"));
            RegularRankParticipationCoverage coverage = RegularRankParticipationGate.Cover(rankSql);
            Expect(errors, coverage.Covered && coverage.ClassWindowLacksGrade, "十八個分支都在，且班排名視窗仍沒有年級");
            Expect(errors, !RegularRankParticipationGate.Cover("SELECT 1").Covered, "標記缺漏時停止");
            Expect(errors, !rankSql.Contains("ApplyParticipation"), "定期排名 SQL 不呼叫學期參與轉換");
            Expect(errors, File.Exists(Path.Combine(root, "RegularRank", "RegularRankFormulaSql.cs"))
                && File.Exists(Path.Combine(root, "RegularRank", "RegularRankCandidateSql.cs")), "五旗標候選公式已抽出");
            string candidate = File.ReadAllText(Path.Combine(root, "RegularRank", "RegularRankCandidateSql.cs"));
            Expect(errors, candidate.Contains("GetWorking(kind).Participate") && candidate.Contains("rank_type = "), "十八分支前依參與種類與年級隔離候選");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[] { SnapshotRow("15", "學生", "1年級", true) });
            list.SetParticipate(new string[] { "15" }, RankKinds.Year, false);
            string valuesError;
            string values = RegularRankParticipationGate.PopulationValues(list, out valuesError);
            Expect(errors, valuesError.Length == 0 && values.Contains("(15,false,true,true,false,false)"), "年不參與仍留在參與集合，不先刪除學生");

            RegularRankPublishPlan missingDetail = RegularRankPublishPlan.Create(new RegularRankGradeOutcome[]
            {
                new RegularRankGradeOutcome { GradeYear = 1, StudentCount = 10, HasNewDetail = true },
                new RegularRankGradeOutcome { GradeYear = 2, StudentCount = 8, HasNewDetail = false },
                new RegularRankGradeOutcome { GradeYear = 3, StudentCount = 0, HasNewDetail = false }
            }, true);
            Expect(errors, missingDetail.WholeBatchFails && missingDetail.ReplacementGrades.Count == 2 && !missingDetail.ReplacementGrades.Contains(3), "沒有學生的勾選年級不納入取代，有學生但無明細則整次失敗");
            Expect(errors, missingDetail.SingleStatement && !missingDetail.PerGradeCommit && missingDetail.IsolateClassWindowsByGrade && !missingDetail.ExecutionAllowed, "全部年級同成同敗，班排名不跨年級合併，零明細計畫不可執行");
            RegularRankPublishPlan ready = RegularRankPublishPlan.Create(new RegularRankGradeOutcome[]
            {
                new RegularRankGradeOutcome { GradeYear = 1, StudentCount = 10, HasNewDetail = true }
            }, true);
            Expect(errors, ready.ExecutionAllowed, "有明細的計畫允許執行");
            RegularRankCalculationEntry.ClearEvidence();
            RegularRankCalculationGuard guard = new RegularRankCalculationGuard();
            Expect(errors, guard.TryEnter() && guard.Running, "清單就緒可進入防重入，不綁 Task 勾選");
            guard.FailAndRestore();
            Expect(errors, !guard.Running && guard.EditorsEnabled, "失敗後編輯狀態可恢復");
            Expect(errors, RegularRankCalculationEntry.BlockedMessage.Contains("傳輸契約") && RegularRankCalculationEntry.BlockedMessage.Contains("不會儲存學校組態") && !RegularRankCalculationEntry.BlockedMessage.Contains("Task 2.4"), "阻擋訊息說明傳輸檢查與不保存組態");
            Expect(errors, RegularRankExistingRankLookup.Describe().Contains("不混入學期成績"), "既有排名檢索限定期");
            Expect(errors, RegularRankExistingRankLookup.AliveMatricesSql("114", "2", "5", new int[] { 3 }).Contains("定期評量/%"), "既有排名 SQL 可組句");
            return errors;
        }

        private static List<string> CheckFormulas(string root)
        {
            List<string> errors = new List<string>();
            string current = File.ReadAllText(Path.Combine(root, "CalculateRegularAssessmentRank.cs"));
            string baseline = File.ReadAllText(Path.Combine(root, @"openspec\changes\regular-rank-mother-group-sources\baseline\source\CalculateRegularAssessmentRank.cs"));
            List<string> currentLines = FormulaLines(current);
            List<string> baselineLines = FormulaLines(baseline);
            Expect(errors, currentLines.Count == baselineLines.Count && currentLines.Count > 0, "排名、PR 與統計公式行數維持基準");
            if (currentLines.Count == baselineLines.Count)
            {
                for (int i = 0; i < currentLines.Count; i++)
                {
                    if (currentLines[i] != baselineLines[i])
                    {
                        errors.Add("公式行不同：" + baselineLines[i] + " => " + currentLines[i]);
                        break;
                    }
                }
            }
            Expect(errors, baseline.Contains("exam.exam_name= row.rank_exam_name") && current.Contains("AND exam.id = row.ref_exam_id::INT") && !current.Contains("exam.exam_name= row.rank_exam_name"), "成績接合改用目標 exam ID，名稱只留在顯示欄");
            Expect(errors, current.Contains("CASE WHEN sce_take.score = -1 THEN 0 ELSE sce_take.score END"), "特殊分數轉換保留");
            return errors;
        }

        private static List<string> CheckOldGridPath(string root)
        {
            List<string> errors = new List<string>();
            string current = File.ReadAllText(Path.Combine(root, "CalculateRegularAssessmentRank.cs"));
            Expect(errors, !current.Contains("new DataGridViewRow"), "舊確認清單不再直接建立一般資料列");
            Expect(errors, !current.Contains("course.school_year = \" + _DefaultSchoolYear"), "科目查詢不再綁系統學年度");
            Expect(errors, CountLiveSymbol(current, "plStudentView") == 0 && CountLiveSymbol(current, "dgvStudentList") == 0, "主程式不再操作 plStudentView 或 dgvStudentList");
            string designer = File.ReadAllText(Path.Combine(root, "CalculateRegularAssessmentRank.Designer.cs"));
            Expect(errors, !designer.Contains("plStudentView") && !designer.Contains("dgvStudentList") && designer.Contains("cboMotherGroupSource"), "條件 Designer 只保留條件頁與母群來源");
            string confirm = File.ReadAllText(Path.Combine(root, @"RegularRank\RegularRankMotherGroupConfirmForm.cs"));
            Expect(errors, confirm.Contains("MotherGroupConfirmGrid.Configure") && confirm.Contains("ConfigurePopulation") && confirm.Contains("ConfigureDetail") && !confirm.Contains("new DataGridViewRow"), "三個 Grid 都走受保護設定");
            Expect(errors, confirm.Contains("TryCalculateFromConfirm"), "確認頁計算銜接宿主");
            return errors;
        }

        private static void CheckLargeGrids(List<string> errors)
        {
            using (RegularRankMotherGroupConfirmForm form = new RegularRankMotherGroupConfirmForm())
            {
                form.ShowInTaskbar = false;
                form.PrepareProtectedGrids();
                List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
                for (int i = 0; i < 2066; i++)
                {
                    StudentMotherGroup group = new StudentMotherGroup();
                    group.StudentId = (i + 1).ToString();
                    group.StudentNumber = (i + 1).ToString();
                    group.StudentName = "學生" + i;
                    group.Status = i % 2 == 0 ? 1 : 2;
                    group.SetSourceSlots(new RankSlot[]
                    {
                        new RankSlot { RankType = RankKinds.Year, GroupName = "1年級", Participate = true },
                        new RankSlot { RankType = RankKinds.Department, GroupName = "普通科", Participate = true },
                        new RankSlot { RankType = RankKinds.Class, GroupName = "班" + i, Participate = true },
                        new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = false },
                        new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
                    });
                    rows.Add(new ConfirmStudentRow { Group = group, ClassName = "現班" + (i % 20), SeatNo = (i % 40).ToString() });
                }
                RegularRankCalculationContext context = RegularRankCalculationContext.CreateDefault("115", "1");
                context.SetTargetPeriod("113", "2");
                context.SetCurrentSource();
                context.SetTargetExam(RegularRankExamResolution.Resolve("30", "", new RegularRankExamChoice[] { Choice("30", "第一次定期評量") }));
                context.SetGradeYears(new int[] { 1 });
                context.MarkMotherGroupLoaded();
                context.ApplySubjectCandidates(new string[] { "國文" }, new string[] { "國文" }, new string[] { "國文" }, new string[0], new string[0], new string[0]);
                context.ConfirmSubjects();
                form.ShowStudents(context.CreateRequest(), rows);
                form.Show();
                Application.DoEvents();

                DataGridView grid = form.StudentGrid;
                DataGridView population = form.PopulationGrid;
                DataGridView detail = form.DetailGrid;
                Expect(errors, grid.Rows.Count == 2066 && population.Rows.Count == RankKinds.All.Length && detail.Rows.Count >= 2066, "三個 Grid 都保留大量列");
                Expect(errors, AllBounded(grid) && AllBounded(population) && AllBounded(detail), "三個 Grid 的資料列都使用受保護型別");
                int yearColumn = grid.Columns[MotherGroupConfirmGrid.ParticipateColumn(RankKinds.Year)].Index;
                Expect(errors, grid.AccessibilityObject.GetChildCount() >= 2066, "保留全部學生輔助存取子項");
                Expect(errors, grid.Rows[0].AccessibilityObject.GetChildCount() == grid.Columns.Count, "保留儲存格子項");
                AccessibleObject checkbox = grid.Rows[0].Cells[yearColumn].AccessibilityObject;
                Expect(errors, !string.IsNullOrEmpty(checkbox.DefaultAction), "核取方塊保留 DefaultAction");
                Expect(errors, grid.Rows[0].AccessibilityObject.Bounds.Height > 0, "可見列有範圍");
                Expect(errors, grid.Rows[2065].AccessibilityObject.Bounds.IsEmpty, "離屏最後一列回傳空範圍");

                Stopwatch watch = Stopwatch.StartNew();
                for (int i = 0; i < grid.Rows.Count; i++)
                {
                    Rectangle rowBounds = grid.Rows[i].AccessibilityObject.Bounds;
                    Rectangle cellBounds = grid.Rows[i].Cells[yearColumn].AccessibilityObject.Bounds;
                    if (rowBounds.Width < 0 || cellBounds.Width < 0)
                        errors.Add("資料列範圍無效");
                }
                watch.Stop();
                Console.WriteLine("2066 student row/cell bounds: " + watch.ElapsedMilliseconds + "ms");
                Expect(errors, watch.ElapsedMilliseconds < 10000, "學生清單範圍走訪超過 10 秒：" + watch.ElapsedMilliseconds);

                Stopwatch detailWatch = Stopwatch.StartNew();
                for (int i = 0; i < detail.Rows.Count; i++)
                {
                    Rectangle bounds = detail.Rows[i].AccessibilityObject.Bounds;
                    if (bounds.Width < 0)
                        errors.Add("明細範圍無效");
                }
                detailWatch.Stop();
                Console.WriteLine("detail bounds: " + detail.Rows.Count + " rows, " + detailWatch.ElapsedMilliseconds + "ms");
                Expect(errors, detailWatch.ElapsedMilliseconds < 10000, "明細清單範圍走訪超過 10 秒：" + detailWatch.ElapsedMilliseconds);

                grid.FirstDisplayedScrollingRowIndex = 2050;
                Application.DoEvents();
                Expect(errors, grid.Rows[2050].AccessibilityObject.Bounds.Height > 0, "捲動後可見列範圍更新");
                Expect(errors, grid.Rows[0].AccessibilityObject.Bounds.IsEmpty, "捲走的列回傳空範圍");
                checkbox.DoDefaultAction();
                grid.EndEdit();
                Expect(errors, grid.Rows[0].Cells[yearColumn].Value is bool && (bool)grid.Rows[0].Cells[yearColumn].Value == false, "輔助存取可切換核取方塊");

                MotherGroupConfirmList exportList = form.List;
                TextBox search = form.Controls["txtSearch"] as TextBox;
                Stopwatch typing = Stopwatch.StartNew();
                search.Text = "__regular_rank_no_match__";
                typing.Stop();
                Expect(errors, typing.ElapsedMilliseconds < 1000, "搜尋輸入不應同步重排大量列：" + typing.ElapsedMilliseconds);
                WaitForRows(grid, 0);
                Expect(errors, grid.Rows.Count == 0 && exportList.Rows.Count == 2066, "實際搜尋只重建顯示列，完整名單保留");
                List<string> found = exportList.VisibleStudentIds("學生1");
                Expect(errors, found.Count > 0 && exportList.Rows.Count == 2066, "搜尋後底層清單仍是 2066 人");
                Workbook workbook = form.CreateExportWorkbook();
                Expect(errors, workbook.Worksheets[0].Cells.MaxDataRow == 2066, "完整匯出包含全部學生，不受搜尋影響");
                search.Text = "";
                WaitForRows(grid, 2066);
                Expect(errors, grid.Rows.Count == 2066 && !(bool)grid.Rows[0].Cells[yearColumn].Value,
                    "清除搜尋後恢復完整列，保留先前修改的參與旗標");
                form.Close();
            }
        }

        private static void WaitForRows(DataGridView grid, int expected)
        {
            Stopwatch wait = Stopwatch.StartNew();
            while (grid.Rows.Count != expected && wait.ElapsedMilliseconds < 10000)
            { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
        }

        private static bool AllBounded(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (!MotherGroupConfirmGrid.IsBoundedRow(row))
                    return false;
            }
            return grid.Rows.Count > 0;
        }

        private static List<string> FormulaLines(string text)
        {
            List<string> lines = new List<string>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Contains("RANK() OVER") || line.Contains("FLOOR((") || line.Contains("CASE WHEN sce_take.score") || line.Contains("STDDEV_POP") || line.Contains("FILTER(WHERE"))
                    lines.Add(line);
            }
            return lines;
        }

        private static ConfirmStudentRow SnapshotRow(string id, string name, string yearName, bool participate)
        {
            StudentMotherGroup group = new StudentMotherGroup();
            group.StudentId = id;
            group.StudentNumber = id;
            group.StudentName = name;
            group.Status = 1;
            group.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = yearName, Participate = participate },
                new RankSlot { RankType = RankKinds.Department, GroupName = "普通科", Participate = true },
                new RankSlot { RankType = RankKinds.Class, GroupName = "1班", Participate = true },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = false },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
            });
            return new ConfirmStudentRow { Group = group, ClassName = "現班", SeatNo = "1" };
        }

        private static CurrentStudentRow Row(string id, string name, bool regular, int status, string classId, int grade, string className)
        {
            CurrentStudentRow row = new CurrentStudentRow();
            row.StudentId = id;
            row.StudentNumber = id;
            row.StudentName = name;
            row.IsRegular = regular;
            row.StatusValue = status;
            row.RefClassId = classId;
            row.GradeYear = grade;
            row.ClassName = className;
            return row;
        }

        private static RegularRankExamChoice Choice(string id, string name)
        {
            RegularRankExamChoice choice = new RegularRankExamChoice();
            choice.Id = id;
            choice.Name = name;
            return choice;
        }

        private static int CountLiveSymbol(string source, string name)
        {
            int count = 0;
            using (StringReader reader = new StringReader(source))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("//"))
                        continue;
                    if (line.Contains(name))
                        count++;
                }
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
