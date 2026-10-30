using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase5Task53Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckNoSnapshotRows());
                errors.AddRange(CheckDuplicateDetails());
                errors.AddRange(CheckMissingDepartment());
                errors.AddRange(CheckBlankCategory());
                errors.AddRange(CheckBlankStudentNameBeatsRankName());
                errors.AddRange(CheckConflictKeepsNames());
                errors.AddRange(CheckRestoreFailure());
                errors.AddRange(CheckSql());
                errors.AddRange(CheckForm());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase5-task53-check: ok");
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

        private static List<string> CheckNoSnapshotRows()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            AddStudent(table, "5301", "S301", "歷史甲", "1年級", "忠班", "甲", "");
            AddRank(table, "5301", "S301", "歷史甲", RankKinds.Department, "普通科");
            AddRank(table, "5301", "S301", "歷史甲", RankKinds.Department, "普通科");
            HistoryRankLoad load = HistoryRankRestore.Read("91", table, "不排名", Tags(), Links("5301"));
            Expect(errors, load.Restored && load.Students.Count == 1 && load.SelectedBatchId == "91", "同一學生的重複列應合成一人");
            StudentMotherGroup student = load.Students[0];
            Expect(errors, student.GetOriginal(RankKinds.Class).GroupName == "忠班" && student.GetOriginal(RankKinds.Department).GroupName == "普通科", "年班類別與科別應來自歷史資料");
            Expect(errors, student.GetWorking(RankKinds.Class).GroupName == "忠班", "不排名類別不應改寫歷史班名");
            foreach (string rankType in RankKinds.All)
                Expect(errors, !student.GetWorking(rankType).Participate, "符合本次不排名類別時，" + rankType + "初始參與應為不參與");
            Expect(errors, load.Warnings.Count == 0, "有科排名且類別空白不應發缺漏警示");
            return errors;
        }

        private static List<string> CheckDuplicateDetails()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            AddRank(table, "5302", "S302", "乙", RankKinds.Year, "2年級");
            AddRank(table, "5302", "S302", "乙", RankKinds.Year, "2年級");
            AddRank(table, "5302", "S302", "乙", RankKinds.Class, "孝班");
            AddRank(table, "5302", "S302", "乙", RankKinds.Department, "商業科");
            HistoryRankLoad load = HistoryRankRestore.Read("91", table, "", Tags(), new List<StudentTagLink>());
            Expect(errors, load.Restored && load.Students.Count == 1 && load.Conflicts.Count == 0, "相同排名名稱的重複明細不是衝突");
            StudentMotherGroup student = load.Students[0];
            Expect(errors, student.GetWorking(RankKinds.Year).GroupName == "2年級" && student.GetWorking(RankKinds.Year).Participate, "沒有學生列時應採用年排名名稱");
            Expect(errors, student.GetWorking(RankKinds.Department).GroupName == "商業科" && student.GetWorking(RankKinds.Department).Participate, "重複的科排名明細應還原成同一個科別");
            return errors;
        }

        private static List<string> CheckMissingDepartment()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            AddStudent(table, "5303", "S303", "丙", "3年級", "仁班", "", "社團");
            HistoryRankLoad load = HistoryRankRestore.Read("91", table, "", Tags(), new List<StudentTagLink>());
            StudentMotherGroup student = load.Students[0];
            Expect(errors, load.Restored && student.GetWorking(RankKinds.Department).GroupName == "" && !student.GetWorking(RankKinds.Department).Participate, "沒有科排名時科別應留白且不參與");
            Expect(errors, HasWarning(load, "5303", RankKinds.Department) && student.GetWorking(RankKinds.Class).GroupName == "仁班", "缺科排名應警示，且不改用其他名稱補科別");
            Expect(errors, !HasWarning(load, "5303", RankKinds.Tag1), "類別一原本空白不應發缺漏警示");
            return errors;
        }

        private static List<string> CheckBlankCategory()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            AddStudent(table, "5304", "S304", "丁", "1年級", "忠班", "", "");
            AddRank(table, "5304", "S304", "丁", RankKinds.Department, "");
            HistoryRankLoad load = HistoryRankRestore.Read("91", table, "", Tags(), new List<StudentTagLink>());
            StudentMotherGroup student = load.Students[0];
            Expect(errors, student.GetWorking(RankKinds.Tag1).GroupName == "" && !student.GetWorking(RankKinds.Tag1).Participate, "空白類別應留白且不參與");
            Expect(errors, student.GetWorking(RankKinds.Department).GroupName == "" && student.GetWorking(RankKinds.Department).Participate, "已還原的空白科別仍應參與");
            Expect(errors, load.Warnings.Count == 0 && load.Conflicts.Count == 0, "空白類別與空白科別都不是缺漏或衝突");
            return errors;
        }

        private static List<string> CheckBlankStudentNameBeatsRankName()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            AddStudent(table, "5306", "S306", "己", "", "", "", "");
            AddRank(table, "5306", "S306", "己", RankKinds.Year, "1年級");
            AddRank(table, "5306", "S306", "己", RankKinds.Class, "新班");
            AddRank(table, "5306", "S306", "己", RankKinds.Tag1, "甲");
            AddRank(table, "5306", "S306", "己", RankKinds.Department, "普通科");
            HistoryRankLoad load = HistoryRankRestore.Read("91", table, "", Tags(), new List<StudentTagLink>());
            Expect(errors, load.Restored && load.Students.Count == 1 && load.Conflicts.Count == 0, "學生列空白不應與排名明細另有名稱形成衝突");
            StudentMotherGroup student = load.Students[0];
            Expect(errors, student.GetWorking(RankKinds.Year).GroupName == "" && student.GetWorking(RankKinds.Class).GroupName == "" && student.GetWorking(RankKinds.Tag1).GroupName == "", "學生列的空白母群應優先於排名明細名稱");
            Expect(errors, student.GetWorking(RankKinds.Department).GroupName == "普通科", "科別仍只取科排名，不受學生列空白影響");
            Expect(errors, load.Notice.IndexOf("完全沒有排名資料的學生不會出現") >= 0 && HistoryRankRestore.EmptyBatchMessage.IndexOf("完全沒有排名資料的學生不會出現") >= 0, "應保留舊批次無法涵蓋完全沒有排名紀錄學生的提示");
            return errors;
        }

        private static List<string> CheckConflictKeepsNames()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            AddStudent(table, "5305", "S305", "戊", "1年級", "忠班", "甲", "");
            AddStudent(table, "5305", "S305", "戊", "1年級", "孝班", "甲", "");
            AddRank(table, "5305", "S305", "戊", RankKinds.Department, "普通科");
            HistoryRankLoad load = HistoryRankRestore.Read("91", table, "", Tags(), new List<StudentTagLink>());
            Expect(errors, load.Restored && load.Students.Count == 1, "班名衝突仍應留下這名學生");
            StudentMotherGroup student = load.Students[0];
            Expect(errors, student.GetWorking(RankKinds.Class).GroupName == "" && !student.GetWorking(RankKinds.Class).Participate, "不同班名不應任取第一筆");
            Expect(errors, load.Conflicts.Count == 1 && load.Conflicts[0].Names.Contains("忠班") && load.Conflicts[0].Names.Contains("孝班"), "衝突應保留全部母群名稱");
            Expect(errors, student.GetWorking(RankKinds.Year).GroupName == "1年級" && student.GetWorking(RankKinds.Year).Participate, "沒有衝突的年排名仍應還原");
            Expect(errors, load.Notice.IndexOf("忠班") >= 0 && load.Notice.IndexOf("孝班") >= 0, "衝突名稱應留在提示，而不是被丟掉");
            return errors;
        }

        private static List<string> CheckRestoreFailure()
        {
            List<string> errors = new List<string>();
            HistoryRankLoad missingTable = HistoryRankRestore.Read("91", null, "", Tags(), new List<StudentTagLink>());
            Expect(errors, !missingTable.Restored && missingTable.Students.Count == 0 && missingTable.Message.IndexOf("不會改用目前學生資料") >= 0, "沒有查詢結果時不應還原出學生");
            DataTable broken = new DataTable();
            broken.Columns.Add("student_id");
            HistoryRankLoad missingColumn = HistoryRankRestore.Read("91", broken, "", Tags(), new List<StudentTagLink>());
            Expect(errors, !missingColumn.Restored && missingColumn.Students.Count == 0 && missingColumn.Message.IndexOf("缺少欄位") >= 0, "缺少欄位時應說明原因且不回傳部分學生");
            HistoryRankLoad empty = HistoryRankRestore.Read("91", Table(), "", Tags(), new List<StudentTagLink>());
            Expect(errors, !empty.Restored && empty.Students.Count == 0, "沒有排名學生時不應視為還原成功");
            return errors;
        }

        private static List<string> CheckSql()
        {
            List<string> errors = new List<string>();
            string sql = HistoryRankRestore.SelectSql("91");
            Expect(errors, sql.IndexOf("rank_batch_student") >= 0 && sql.IndexOf("rank_detail") >= 0 && sql.IndexOf("rank_matrix") >= 0, "應讀取歷史學生、明細與矩陣");
            Expect(errors, sql.IndexOf("matrix_grade") >= 0 && sql.IndexOf("matrix_class") >= 0 && sql.IndexOf("matrix_tag1") >= 0 && sql.IndexOf("matrix_tag2") >= 0 && sql.IndexOf("rank_name") >= 0 && sql.IndexOf("ref_matrix_id") >= 0, "應使用既有欄位還原母群名稱");
            Expect(errors, sql.IndexOf("WHERE ref_batch_id = 91") >= 0 && sql.IndexOf("rank_matrix.ref_batch_id = 91") >= 0, "只應依選取的批次識別查詢");
            Expect(errors, sql.IndexOf("school_year") < 0 && sql.IndexOf("semester") < 0 && sql.IndexOf("92") < 0, "不應用學年度、學期或其他批次推測");
            Expect(errors, sql.IndexOf("class.class_name") < 0 && sql.IndexOf("ref_dept_id") < 0 && sql.IndexOf("dept.name") < 0, "不應讀取目前班級或科別來覆蓋歷史母群");
            Expect(errors, sql.IndexOf("INSERT") < 0 && sql.IndexOf("UPDATE") < 0 && sql.IndexOf("DELETE") < 0 && sql.IndexOf(";") < 0 && sql.IndexOf("XMLTABLE") < 0 && sql.IndexOf("MERGE") < 0, "歷史還原查詢應為 PostgreSQL 9.6 到 17.1 的唯讀語句");
            bool rejected = false;
            try
            {
                HistoryRankRestore.SelectSql("91 OR 1=1");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "不合法的批次識別不應組成查詢");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string source = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int begin = source.IndexOf("void BeginHistorySnapshotRestore");
            int show = source.IndexOf("void ShowConfirmList");
            string history = begin >= 0 && show > begin ? source.Substring(begin, show - begin) : "";
            int snapshot = history.IndexOf("HistorySnapshotRestore.Load");
            int legacy = history.IndexOf("HistoryRankRestore.SelectBatchStudentSql");
            Expect(errors, snapshot >= 0 && legacy > snapshot, "沒有 JSON 與 XML 快照時才查排名學生，且仍先嘗試快照");
            Expect(errors, history.IndexOf("CurrentStudentMotherGroupSource") < 0 && history.IndexOf("btnCacluate.Enabled = true") < 0, "還原失敗不應改用目前學生，也不應在此啟用計算按鈕");
            Expect(errors, history.IndexOf("ClearLoadedConfirmList") >= 0, "還原失敗應清空確認清單");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            string restore = File.ReadAllText("MotherGroup\\HistoryRankRestore.cs", Encoding.UTF8);
            Expect(errors, restore.IndexOf("INSERT") < 0 && restore.IndexOf("UPDATE") < 0 && restore.IndexOf("DELETE") < 0, "無快照還原不應含寫入語句");
            return errors;
        }

        private static DataTable Table()
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id");
            table.Columns.Add("student_number");
            table.Columns.Add("student_name");
            table.Columns.Add("rank_type");
            table.Columns.Add("group_name");
            table.Columns.Add("name_origin");
            return table;
        }

        private static void AddStudent(DataTable table, string id, string number, string name, string year, string className, string tag1, string tag2)
        {
            table.Rows.Add(id, number, name, RankKinds.Year, year, "student");
            table.Rows.Add(id, number, name, RankKinds.Class, className, "student");
            table.Rows.Add(id, number, name, RankKinds.Tag1, tag1, "student");
            table.Rows.Add(id, number, name, RankKinds.Tag2, tag2, "student");
        }

        private static void AddRank(DataTable table, string id, string number, string name, string rankType, string groupName)
        {
            table.Rows.Add(id, number, name, rankType, groupName, "rank");
        }

        private static bool HasWarning(HistoryRankLoad load, string studentId, string rankType)
        {
            foreach (HistoryRankWarning warning in load.Warnings)
            {
                if (warning.StudentId == studentId && warning.RankType == rankType)
                    return true;
            }
            return false;
        }

        private static List<TagDefinition> Tags()
        {
            return new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } };
        }

        private static List<StudentTagLink> Links(string studentId)
        {
            return new List<StudentTagLink> { new StudentTagLink { StudentId = studentId, TagId = "ex", Name = "休學" } };
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
