using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterHistorySourceCheck
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckCatalog());
                errors.AddRange(CheckPeriod());
                errors.AddRange(CheckRestore());
                errors.AddRange(CheckParticipation());
                errors.AddRange(CheckForm());
                if (errors.Count == 0)
                {
                    Console.WriteLine("semester-history-source-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("screen-executed: false");
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

        private static List<string> CheckCatalog()
        {
            List<string> errors = new List<string>();
            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("school_year");
            table.Columns.Add("semester");
            table.Columns.Add("calculation_description");
            table.Columns.Add("setting");
            table.Columns.Add("extension");
            table.Rows.Add("12", "113", "2", "學期", Setting("學期成績", "<年級>1</年級><年級>2</年級>"), "[]");
            table.Rows.Add("40", "113", "2", "期中", Setting("期中考", "<年級>2</年級>"), "");
            table.Rows.Add("7", "114", "1", "另一期", Setting("學期成績", "<年級>1</年級>"), "");
            table.Rows.Add("3", "113", "2", "繁星", "<Setting><年級>3</年級></Setting>", "");
            table.Rows.Add("15", "113", "2", "壞", Setting("期末考", "<年級>1</年級>"), "{");
            List<HistoryRankBatch> batches = SemesterHistoryBatchCatalog.FilterPeriod(SemesterHistoryBatchCatalog.Read(table), "113", "2");
            SemesterHistoryBatchCatalog.SortByIdDescending(batches);
            Expect(errors, batches.Count == 3 && batches[0].Id == "40" && batches[1].Id == "15" && batches[2].Id == "12", "同一歷史期間應含學期成績與定期評量，並依 ID 數字降冪");
            Expect(errors, batches[0].SourceType == "定期評量" && batches[0].ExamName == "期中考" && batches[0].DisplayText.IndexOf("40｜定期評量｜113學年度第2學期｜期中考｜期中｜年級2｜快照無") == 0, "定期評量應顯示 ID、來源、期間、評量名稱、說明、年級與快照");
            Expect(errors, batches[1].SnapshotOrigin == HistorySnapshotOrigin.Json && batches[1].DisplayText.IndexOf("快照有(JSON)") >= 0, "損壞的 JSON 仍標成 JSON");
            Expect(errors, batches[2].GradeYears == "1、2" && batches[2].DisplayText.IndexOf("建立時間") < 0, "年級維持原顯示，且不稱作建立時間");
            bool otherPeriod = false;
            foreach (HistoryRankBatch batch in batches)
            {
                if (batch.Id == "7" || batch.Id == "3")
                    otherPeriod = true;
            }
            Expect(errors, !otherPeriod, "其他期間與沒有考試名稱的批次不應留在這次篩選");
            return errors;
        }

        private static List<string> CheckPeriod()
        {
            List<string> errors = new List<string>();
            string sql = SemesterHistoryPeriodGuard.SelectSql("91");
            Expect(errors, sql.IndexOf("school_year") >= 0 && sql.IndexOf("semester") >= 0 && sql.IndexOf("WHERE id = 91") >= 0 && sql.IndexOf("INSERT") < 0 && sql.IndexOf("UPDATE") < 0 && sql.IndexOf("DELETE") < 0, "下一步應重讀批次自己的學年度與學期");
            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("school_year");
            table.Columns.Add("semester");
            table.Rows.Add("91", 113, 2);
            Expect(errors, SemesterHistoryPeriodGuard.Matches(table, "91", "113", "2"), "批次期間與畫面歷史期間一致時應通過");
            Expect(errors, !SemesterHistoryPeriodGuard.Matches(table, "91", "113", "1"), "學期不一致時應停止");
            CalculationPeriod period = CalculationPeriod.CreateDefault("114", "1");
            period.SetCalculationPeriod("114", "2");
            period.SetHistoryPeriod("113", "2");
            period.SelectHistoryBatch("91");
            Expect(errors, period.SchoolYear == "114" && period.Semester == "2" && period.HistorySchoolYear == "113" && period.HistoryBatchId == "91", "選取批次不應改寫計算期間或歷史期間");
            period.ClearHistoryBatchSelection();
            Expect(errors, period.HistoryBatchId == "" && period.HistorySchoolYear == "113" && period.SchoolYear == "114", "清除批次選取不應清掉使用者剛選的歷史期間");
            period.ClearHistoryBatch();
            Expect(errors, period.HistoryBatchId == "" && period.HistorySchoolYear == "" && period.SchoolYear == "114", "既有 ClearHistoryBatch 仍同時清批次與歷史期間");
            return errors;
        }

        private static List<string> CheckRestore()
        {
            List<string> errors = new List<string>();
            string xml = "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"期中考\"><年級>1</年級><MotherGroupSnapshot version=\"1\"><Source kind=\"history\" historyBatchId=\"50\" /><Student studentId=\"8\" studentNumber=\"S8\" studentName=\"舊生\"><Rank type=\"班排名\" name=\"忠班\" participate=\"true\" /></Student></MotherGroupSnapshot></Setting>";
            HistorySnapshotLoad shared = HistorySnapshotRestore.Load("91", xml, "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, !shared.Restored && shared.Message == HistorySnapshotRestore.WrongExamMessage, "共用還原仍只接受考試名稱學期成績");

            MotherGroupConfirmList jsonList = new MotherGroupConfirmList();
            StudentMotherGroup source = new StudentMotherGroup();
            source.StudentId = "8";
            source.StudentNumber = "S8";
            source.StudentName = "舊生";
            source.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = "1年級", Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = "商科", Participate = false },
                new RankSlot { RankType = RankKinds.Class, GroupName = "忠班", Participate = false },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = false },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
            });
            jsonList.Load(new ConfirmStudentRow[] { new ConfirmStudentRow { Group = source } });
            string json = MotherGroupSnapshotJson.Compose("[]", jsonList, MotherGroupSourceKind.HistorySnapshot, "91");
            HistorySnapshotLoad restored = HistorySnapshotRestore.LoadSemester("91", json, xml, "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, restored.Restored && restored.Students.Count == 1 && restored.Students[0].GetWorking(RankKinds.Class).GroupName == "忠班" && !restored.Students[0].GetWorking(RankKinds.Class).Participate && restored.Students[0].GetWorking(RankKinds.Department).GroupName == "商科" && !restored.Students[0].GetWorking(RankKinds.Department).Participate, "有效 JSON 應維持快照名稱與參與");

            StudentMotherGroup studentA = Student("A", "甲生", false);
            StudentMotherGroup studentB = Student("B", "乙生", false);
            string xmlStudents = XmlSnapshot("期中考", new StudentMotherGroup[] { studentA, studentB });
            List<TagDefinition> tags = new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } };
            List<StudentTagLink> links = new List<StudentTagLink> { new StudentTagLink { StudentId = "B", TagId = "ex", Name = "休學" } };
            HistorySnapshotLoad xmlOnly = HistorySnapshotRestore.LoadSemester("91", "[]", xmlStudents, "不排名", tags, links);
            StudentMotherGroup restoredA = Find(xmlOnly.Students, "A");
            StudentMotherGroup restoredB = Find(xmlOnly.Students, "B");
            Expect(errors, xmlOnly.Restored && xmlOnly.Students.Count == 2 && restoredA != null && restoredB != null, "只有 XML 時應保留快照內的 A、B，不改查 rank_batch_student");
            Expect(errors, restoredA != null && restoredA.GetWorking(RankKinds.Class).GroupName == "高一甲" && restoredA.GetWorking(RankKinds.Year).Participate && restoredA.GetWorking(RankKinds.Department).Participate && restoredA.GetWorking(RankKinds.Class).Participate && !restoredA.GetWorking(RankKinds.Tag1).Participate, "不符合不排名的 XML 學生套用年科班全參與，不沿用歷史參與，類別空白仍不參與");
            Expect(errors, restoredB != null && restoredB.GetWorking(RankKinds.Class).GroupName == "高一甲" && !restoredB.GetWorking(RankKinds.Year).Participate && !restoredB.GetWorking(RankKinds.Department).Participate && !restoredB.GetWorking(RankKinds.Class).Participate, "符合不排名類別者年科班暫時維持不參與");
            HistorySnapshotLoad neither = HistorySnapshotRestore.LoadSemester("91", "[]", Setting("學期成績", "<年級>1</年級>"), "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, !neither.Restored && neither.Students.Count == 0 && neither.Message == HistorySnapshotRestore.MissingSnapshotMessage, "兩種快照都沒有時才回傳缺少快照，改由 rank_batch_student 接續");

            HistorySnapshotLoad broken = HistorySnapshotRestore.LoadSemester("91", "{", xmlStudents, "不排名", tags, links);
            Expect(errors, !broken.Restored && broken.Students.Count == 0 && broken.Message == MotherGroupSnapshotJson.InvalidDocumentMessage, "損壞的 JSON 應停止，不降級成 XML");

            string blankExam = "<Setting 學年度=\"113\" 學期=\"2\"><年級>3</年級></Setting>";
            HistorySnapshotLoad star = HistorySnapshotRestore.LoadSemester("91", "", blankExam, "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, !star.Restored && star.Message == HistorySnapshotRestore.UnrecognizedExamMessage, "沒有考試名稱的批次不應還原");
            return errors;
        }

        private static List<string> CheckParticipation()
        {
            List<string> errors = new List<string>();
            DataTable table = new DataTable();
            string[] columns = new string[] { "id", "ref_batch_id", "ref_student_id", "student_number", "student_name", "grade_year", "matrix_grade", "matrix_dept", "matrix_class", "matrix_tag1", "matrix_tag2" };
            foreach (string column in columns)
                table.Columns.Add(column);
            table.Rows.Add("1", "91", "5", "500", "甲", "1", "1年級", "商科", "忠班", "", "");
            table.Rows.Add("2", "91", "5", "500", "甲", "1", "1年級", "商科", "孝班", "", "");
            table.Rows.Add("3", "91", "9", "900", "乙", "2", "2年級", "", "高二甲", "甲組", "");
            List<TagDefinition> tags = new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } };
            List<StudentTagLink> links = new List<StudentTagLink> { new StudentTagLink { StudentId = "9", TagId = "ex", Name = "休學" } };
            HistoryRankLoad load = HistoryRankRestore.ReadBatchStudents("91", table, "不排名", tags, links);
            SemesterHistoryStudentParticipation.KeepYearDeptClassParticipating(load);
            StudentMotherGroup conflicted = load.Students[0];
            RankSlot classSlot = conflicted.GetWorking(RankKinds.Class);
            Expect(errors, classSlot.GroupName == "" && classSlot.HasConflict && classSlot.Participate && classSlot.AllowParticipationDespiteConflict && conflicted.GetWorking(RankKinds.Department).GroupName == "商科" && conflicted.GetWorking(RankKinds.Department).Participate, "班名衝突不任取、不取消年科班參與");
            StudentMotherGroup excluded = load.Students[1];
            Expect(errors, excluded.MatchesExcludedCategory && excluded.GetWorking(RankKinds.Year).GroupName == "2年級" && !excluded.GetWorking(RankKinds.Year).Participate && excluded.GetWorking(RankKinds.Department).GroupName == "" && !excluded.GetWorking(RankKinds.Department).Participate && !excluded.GetWorking(RankKinds.Class).Participate && !excluded.GetWorking(RankKinds.Tag1).Participate, "符合不排名類別者年科班暫時不改成參與，類別一仍不參與");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[] { new ConfirmStudentRow { Group = conflicted } });
            list.SetParticipate(new string[] { "5" }, RankKinds.Class, false);
            Expect(errors, !conflicted.GetWorking(RankKinds.Class).Participate && conflicted.GetWorking(RankKinds.Class).GroupName == "", "使用者可以取消衝突班排名的參與，名稱仍不可編輯");
            list.Restore();
            Expect(errors, conflicted.GetWorking(RankKinds.Class).Participate, "還原參與回到進入清單時的預設參與");
            string initial = File.ReadAllText("MotherGroup\\MotherGroupInitialParticipation.cs", Encoding.UTF8);
            Expect(errors, initial.IndexOf("KeepYearDeptClassParticipating") < 0 && initial.IndexOf("matchesExcluded") >= 0, "共用初始參與預設不改");
            string formula = File.ReadAllText("MotherGroup\\SemesterRankSql.cs", Encoding.UTF8);
            Expect(errors, formula.IndexOf("PARTITION BY rank_grade_year, rank_dept_name, subject") >= 0 && formula.IndexOf("PARTITION BY rank_class_name, subject") >= 0, "年科班的既有年級窗口不改");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string source = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int begin = source.IndexOf("void BeginHistorySnapshotRestore");
            int show = source.IndexOf("void ShowConfirmList");
            string history = begin >= 0 && show > begin ? source.Substring(begin, show - begin) : "";
            Expect(errors, history.IndexOf("SemesterHistoryPeriodGuard.SelectSql") >= 0 && history.IndexOf("HistorySnapshotRestore.LoadSemester") > history.IndexOf("SemesterHistoryPeriodGuard.SelectSql"), "下一步應先核對歷史期間，再還原");
            Expect(errors, history.IndexOf("HistoryRankRestore.SelectBatchStudentSql") > history.IndexOf("HistorySnapshotRestore.LoadSemester") && history.IndexOf("MissingSnapshotMessage") < history.IndexOf("HistoryRankRestore.SelectBatchStudentSql") && history.IndexOf("KeepYearDeptClassParticipating") > history.IndexOf("HistoryRankRestore.SelectBatchStudentSql"), "只有兩種快照都不存在時才讀 rank_batch_student，並套用年科班預設參與");
            Expect(errors, history.IndexOf("rankLoad.Notice") < 0 && history.IndexOf("lbSchoolYear.Text = calculationSchoolYear") >= 0 && history.IndexOf("lbSchoolYear.Text = selected.SchoolYear") < 0, "空白或衝突不跳出提示，圖2仍用計算期間");
            Expect(errors, source.IndexOf("SetHistoryFiltersEnabled(false)") >= 0 && source.IndexOf("SelectHistoryBatch") >= 0 && source.IndexOf("ShowHistoryPeriod") < 0, "離開歷史來源停用篩選，選取批次不回寫歷史期間");
            string regular = File.ReadAllText("CalculateRegularAssessmentRank.cs", Encoding.UTF8);
            Expect(errors, regular.IndexOf("SemesterHistoryPeriodGuard") < 0 && regular.IndexOf("LoadSemester") < 0, "定期評量計算檔不接這次學期歷史政策");
            return errors;
        }

        private static StudentMotherGroup Student(string id, string name, bool yearParticipate)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentNumber = id;
            student.StudentName = name;
            student.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = "1年級", Participate = yearParticipate },
                new RankSlot { RankType = RankKinds.Department, GroupName = "商科", Participate = false },
                new RankSlot { RankType = RankKinds.Class, GroupName = "高一甲", Participate = false },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = true },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = true }
            });
            return student;
        }

        private static string XmlSnapshot(string examName, StudentMotherGroup[] students)
        {
            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            document.AppendChild(setting);
            setting.SetAttribute("學年度", "113");
            setting.SetAttribute("學期", "2");
            setting.SetAttribute("考試名稱", examName);
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            ConfirmStudentRow[] rows = new ConfirmStudentRow[students.Length];
            for (int index = 0; index < students.Length; index++)
                rows[index] = new ConfirmStudentRow { Group = students[index] };
            list.Load(rows);
            MotherGroupSnapshot.Append(setting, list, MotherGroupSourceKind.HistorySnapshot, "50");
            return setting.OuterXml;
        }

        private static StudentMotherGroup Find(IList<StudentMotherGroup> students, string studentId)
        {
            if (students == null)
                return null;
            foreach (StudentMotherGroup student in students)
            {
                if (student != null && student.StudentId == studentId)
                    return student;
            }
            return null;
        }

        private static string Setting(string examName, string body)
        {
            return "<Setting 學年度=\"113\" 學期=\"2\" 考試名稱=\"" + examName + "\">" + body + "</Setting>";
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
