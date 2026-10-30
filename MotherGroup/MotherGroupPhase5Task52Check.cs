using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase5Task52Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckExcludedStudent());
                errors.AddRange(CheckKeptStudents());
                errors.AddRange(CheckMissingSnapshot());
                errors.AddRange(CheckUnreadableSnapshot());
                errors.AddRange(CheckSettingQuery());
                errors.AddRange(CheckSnapshotIdentity());
                errors.AddRange(CheckForm());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase5-task52-check: ok");
                    Console.WriteLine("database-executed: false");
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

        private static List<string> CheckExcludedStudent()
        {
            List<string> errors = new List<string>();
            string xml = SnapshotXml(StudentXml("5201", "S201", "歷史甲", true, true, true, true, true));
            string before = xml;
            HistorySnapshotLoad load = HistorySnapshotRestore.Load("91", xml, "不排名", Tags(), Links("5201"));
            Expect(errors, load.Restored && load.Students.Count == 1 && load.SettingXml == before && xml == before, "還原不應改寫歷史設定文字");
            StudentMotherGroup student = load.Students[0];
            Expect(errors, student.GetOriginal(RankKinds.Year).Participate && student.GetOriginal(RankKinds.Class).GroupName == "忠班", "快照參與與名稱應先成為來源原值");
            foreach (string rankType in RankKinds.All)
            {
                Expect(errors, !student.GetWorking(rankType).Participate, "符合本次不排名類別時，" + rankType + "初始參與應為不參與");
                Expect(errors, student.GetWorking(rankType).GroupName == student.GetOriginal(rankType).GroupName, rankType + "名稱不應被目前班級或科別覆蓋");
            }
            Expect(errors, student.GetWorking(RankKinds.Class).GroupName == "忠班" && student.GetWorking(RankKinds.Department).GroupName == "普通科", "歷史班名與科別應保持快照原值");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[] { new ConfirmStudentRow { Group = student, ClassName = "現在班", SeatNo = "9" } });
            list.SetParticipate(new string[] { "5201" }, RankKinds.Year, true);
            Expect(errors, student.GetWorking(RankKinds.Year).Participate && student.GetWorking(RankKinds.Class).GroupName == "忠班", "個別調整只改參與，不改歷史母群名稱");
            list.Restore();
            Expect(errors, !student.GetWorking(RankKinds.Year).Participate && student.GetWorking(RankKinds.Class).GroupName == "忠班", "還原應回到套用不排名類別後的初始不參與");
            return errors;
        }

        private static List<string> CheckKeptStudents()
        {
            List<string> errors = new List<string>();
            string xml = SnapshotXml(
                StudentXml("5202", "S202", "全不參與", false, false, false, false, false)
                + StudentXml("5203", "S203", "無本次成績", true, true, true, false, false));
            HistorySnapshotLoad load = HistorySnapshotRestore.Load("91", xml, "", Tags(), new List<StudentTagLink>());
            Expect(errors, load.Restored && load.Students.Count == 2, "五種都不參與與沒有本次成績的學生都應留下");
            StudentMotherGroup none = Find(load.Students, "5202");
            StudentMotherGroup noScore = Find(load.Students, "5203");
            Expect(errors, none != null && !none.GetWorking(RankKinds.Year).Participate && none.GetWorking(RankKinds.Class).GroupName == "忠班", "全不參與的學生應保留快照名稱與不參與");
            Expect(errors, noScore != null && noScore.GetWorking(RankKinds.Year).Participate && noScore.GetWorking(RankKinds.Tag1).GroupName == "", "沒有本次成績的學生仍應依快照參與");
            Expect(errors, !noScore.GetWorking(RankKinds.Tag1).Participate, "類別名稱空白時該項應維持不參與");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[]
            {
                new ConfirmStudentRow { Group = none },
                new ConfirmStudentRow { Group = noScore }
            });
            list.SetParticipate(new string[] { "5202", "5203" }, RankKinds.Department, false);
            Expect(errors, !none.GetWorking(RankKinds.Department).Participate && !noScore.GetWorking(RankKinds.Department).Participate, "多選調整應同時改兩名歷史學生");
            Expect(errors, list.VisibleStudentIds("無本次").Count == 1 && list.VisibleStudentIds("無本次")[0] == "5203", "搜尋應找到歷史學生");
            MotherGroupCountSummary summary = list.Summarize();
            Expect(errors, summary.StudentCount == 2 && summary.Ranks[0].ParticipateCount == 1 && summary.Ranks[0].ExcludeCount == 1, "人數應含全部歷史學生");
            list.Restore();
            Expect(errors, noScore.GetWorking(RankKinds.Department).Participate && noScore.GetWorking(RankKinds.Department).GroupName == "普通科", "還原應回到快照的科排名參與與名稱");
            return errors;
        }

        private static List<string> CheckMissingSnapshot()
        {
            List<string> errors = new List<string>();
            string xml = "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"學期成績\"><年級>1</年級></Setting>";
            HistorySnapshotLoad missing = HistorySnapshotRestore.Load("91", xml, "不排名", Tags(), Links("5201"));
            Expect(errors, !missing.Restored && missing.Students.Count == 0 && missing.Message == HistorySnapshotRestore.MissingSnapshotMessage, "沒有版本 1 快照時應拒絕，且不回傳學生");
            Expect(errors, missing.Message.IndexOf("不會改用目前學生資料") >= 0 && missing.SettingXml == xml, "沒有快照時應提示，且不改寫設定");

            string versionTwo = "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"學期成績\"><MotherGroupSnapshot version=\"2\" /></Setting>";
            HistorySnapshotLoad oldVersion = HistorySnapshotRestore.Load("91", versionTwo, "不排名", Tags(), Links("5201"));
            Expect(errors, !oldVersion.Restored && oldVersion.Students.Count == 0, "版本不是 1 時不應還原出部分學生");
            return errors;
        }

        private static List<string> CheckUnreadableSnapshot()
        {
            List<string> errors = new List<string>();
            string duplicate = SnapshotXml(StudentXml("5201", "S201", "甲", true, true, true, true, true) + StudentXml("5201", "S201", "乙", true, true, true, true, true));
            HistorySnapshotLoad load = HistorySnapshotRestore.Load("91", duplicate, "不排名", Tags(), new List<StudentTagLink>());
            Expect(errors, !load.Restored && load.Students.Count == 0 && load.Message == HistorySnapshotRestore.UnreadableSnapshotMessage, "快照無法還原時學生清單應為空");

            HistorySnapshotLoad broken = HistorySnapshotRestore.Load("91", "<Setting>", "", Tags(), new List<StudentTagLink>());
            Expect(errors, !broken.Restored && broken.Students.Count == 0, "非法設定不應還原出學生");
            return errors;
        }

        private static List<string> CheckSettingQuery()
        {
            List<string> errors = new List<string>();
            string sql = HistorySnapshotRestore.SelectSettingSql("91");
            Expect(errors, sql.IndexOf("SELECT") >= 0 && sql.IndexOf("FROM rank_batch") >= 0 && sql.IndexOf("WHERE id = 91") >= 0, "應依選取的批次識別讀取設定");
            Expect(errors, sql.IndexOf("school_year") < 0 && sql.IndexOf("semester") < 0, "不應使用計算學年度或學期推測歷史批次");
            Expect(errors, sql.IndexOf("INSERT") < 0 && sql.IndexOf("UPDATE") < 0 && sql.IndexOf("DELETE") < 0 && sql.IndexOf(";") < 0 && sql.IndexOf("XMLTABLE") < 0, "設定查詢應為 PostgreSQL 9.6 到 17.1 的唯讀語句");
            bool rejected = false;
            try
            {
                HistorySnapshotRestore.SelectSettingSql("91 OR 1=1");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "不合法的批次識別不應組成查詢");

            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("setting");
            table.Rows.Add("91", "<Setting />");
            string settingXml;
            Expect(errors, HistorySnapshotRestore.TryReadSetting(table, "91", out settingXml) && settingXml == "<Setting />", "應讀回所選批次的設定");
            table.Rows.Add("92", "<Setting />");
            Expect(errors, !HistorySnapshotRestore.TryReadSetting(table, "91", out settingXml), "不是剛好一筆時不應採用設定");

            string catalog = File.ReadAllText("MotherGroup\\SemesterHistoryBatchCatalog.cs", Encoding.UTF8);
            Expect(errors, catalog.IndexOf("快照很大") >= 0 && SemesterHistoryBatchCatalog.SelectSql.IndexOf("WHERE") < 0, "應記錄全批次查詢的快照效能風險，且不改資料庫篩選");
            return errors;
        }

        private static List<string> CheckSnapshotIdentity()
        {
            List<string> errors = new List<string>();
            string student = StudentXml("5201", "S201", "歷史甲", true, true, true, true, true);
            string wrongExam = "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"定期評量\"><MotherGroupSnapshot version=\"1\"><Source kind=\"history\" historyBatchId=\"50\" />" + student + "</MotherGroupSnapshot></Setting>";
            string beforeWrong = wrongExam;
            HistorySnapshotLoad wrong = HistorySnapshotRestore.Load("91", wrongExam, "不排名", Tags(), new List<StudentTagLink>());
            Expect(errors, !wrong.Restored && wrong.Students.Count == 0 && wrong.Message == HistorySnapshotRestore.WrongExamMessage && wrong.SettingXml == beforeWrong, "考試名稱不是學期成績時不應還原學生");

            string duplicateNodes = "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"學期成績\">"
                + "<MotherGroupSnapshot version=\"1\"><Source kind=\"history\" historyBatchId=\"50\" />" + student + "</MotherGroupSnapshot>"
                + "<MotherGroupSnapshot version=\"1\"><Source kind=\"current\" historyBatchId=\"91\" />" + StudentXml("5209", "S209", "另一筆", true, true, true, true, true) + "</MotherGroupSnapshot>"
                + "</Setting>";
            HistorySnapshotLoad duplicate = HistorySnapshotRestore.Load("91", duplicateNodes, "", Tags(), new List<StudentTagLink>());
            Expect(errors, !duplicate.Restored && duplicate.Students.Count == 0 && duplicate.Message == HistorySnapshotRestore.DuplicateSnapshotMessage, "重複的版本 1 快照不應回傳部分學生");

            string correct = "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"學期成績\"><MotherGroupSnapshot version=\"1\"><Source kind=\"history\" historyBatchId=\"50\" />" + student + "</MotherGroupSnapshot></Setting>";
            string beforeCorrect = correct;
            HistorySnapshotLoad restored = HistorySnapshotRestore.Load("91", correct, "", Tags(), new List<StudentTagLink>());
            Expect(errors, restored.Restored && restored.Students.Count == 1 && restored.SelectedBatchId == "91" && restored.SettingXml == beforeCorrect, "應使用選取的 rank_batch.id，而不是快照內的 historyBatchId");
            Expect(errors, restored.Students[0].StudentId == "5201" && restored.Students[0].GetWorking(RankKinds.Class).GroupName == "忠班", "正確的學期成績批次應還原快照學生");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string source = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int begin = source.IndexOf("void BeginHistorySnapshotRestore");
            int show = source.IndexOf("void ShowConfirmList");
            Expect(errors, begin >= 0 && show > begin, "歷史還原應進入既有確認清單");
            string history = source.Substring(begin, show - begin);
            int clear = history.IndexOf("ClearLoadedConfirmList");
            int visible = history.IndexOf("plStudentView.Visible = true");
            int failureReturn = history.IndexOf("return;", clear);
            Expect(errors, clear >= 0 && failureReturn > clear && visible > failureReturn, "快照無法還原時應先清空確認清單，再決定是否進入圖2");
            Expect(errors, history.IndexOf("lbSchoolYear.Text = calculationSchoolYear") > visible && history.IndexOf("lbSemester.Text = calculationSemester") > visible, "圖2學年度與學期應使用本次計算期間");
            Expect(errors, history.IndexOf("CurrentStudentMotherGroupSource") < 0 && history.IndexOf("rank_batch_student") < 0 && history.IndexOf("UPDATE") < 0, "歷史快照還原不應改用目前學生來源，也不應寫入排名表");
            Expect(errors, history.IndexOf("SelectSettingSql") >= 0 && history.IndexOf("school_year") < 0, "歷史設定查詢不應帶入計算學年度");
            Expect(errors, history.IndexOf("btnCacluate.Enabled = true") < 0 && history.IndexOf("btnCacluate.Enabled = false") >= 0, "還原失敗、查無批次或例外時不應啟用計算按鈕");
            Expect(errors, history.IndexOf("ClearLoadedConfirmList") >= 0, "失敗時應清空確認清單");
            int shown = source.IndexOf("void ShowConfirmList");
            int nextMethod = source.IndexOf("void ClearLoadedConfirmList");
            string confirm = shown >= 0 && nextMethod > shown ? source.Substring(shown, nextMethod - shown) : "";
            int created = confirm.IndexOf("_confirmList = new MotherGroupConfirmList()");
            int enabled = confirm.IndexOf("btnCacluate.Enabled = true");
            Expect(errors, created >= 0 && enabled > created, "只有確認清單建立後才啟用計算按鈕");
            Expect(errors, source.IndexOf("ClearFailedHistorySelection") >= 0 && source.IndexOf("ClearHistoryBatch") >= 0, "歷史批次查詢失敗應清除選取與批次識別");
            Expect(errors, history.IndexOf("_GradeYearList =") < 0 && history.IndexOf("_GradeSelectList =") < 0, "歷史還原尚未重填年級與擇優採計");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            string restore = File.ReadAllText("MotherGroup\\HistorySnapshotRestore.cs", Encoding.UTF8);
            Expect(errors, restore.IndexOf("INSERT") < 0 && restore.IndexOf("UPDATE") < 0 && restore.IndexOf("DELETE") < 0, "快照還原不應含寫入語句");
            return errors;
        }

        private static List<TagDefinition> Tags()
        {
            return new List<TagDefinition>
            {
                new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" }
            };
        }

        private static List<StudentTagLink> Links(string studentId)
        {
            return new List<StudentTagLink>
            {
                new StudentTagLink { StudentId = studentId, TagId = "ex", Name = "休學" }
            };
        }

        private static string SnapshotXml(string students)
        {
            return "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"學期成績\"><MotherGroupSnapshot version=\"1\"><Source kind=\"history\" historyBatchId=\"91\" />" + students + "</MotherGroupSnapshot></Setting>";
        }

        private static string StudentXml(string id, string number, string name, bool year, bool department, bool classOn, bool tag1, bool tag2)
        {
            return "<Student id=\"" + id + "\" number=\"" + number + "\" name=\"" + name + "\">"
                + Rank("年排名", "1年級", year)
                + Rank("科排名", "普通科", department)
                + Rank("班排名", "忠班", classOn)
                + Rank("類別1排名", "", tag1)
                + Rank("類別2排名", "社團", tag2)
                + "</Student>";
        }

        private static string Rank(string type, string groupName, bool participate)
        {
            return "<Rank type=\"" + type + "\" name=\"" + groupName + "\" participate=\"" + (participate ? "true" : "false") + "\" />";
        }

        private static StudentMotherGroup Find(List<StudentMotherGroup> students, string studentId)
        {
            foreach (StudentMotherGroup student in students)
            {
                if (student.StudentId == studentId)
                    return student;
            }
            return null;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
