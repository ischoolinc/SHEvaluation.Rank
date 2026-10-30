using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase5Task54Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckMissingScoreStaysListed());
                errors.AddRange(CheckUnparsedYear());
                errors.AddRange(CheckCurrentClassDoesNotReplaceHistory());
                errors.AddRange(CheckSharedScope());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase5-task54-check: ok");
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

        private static List<string> CheckMissingScoreStaysListed()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList list = ListOf(Student("5402", "2年級", "舊班", "普通科"));
            Expect(errors, HistoryCalculationScope.KeepsStudentWithoutScore(list, "5402") && list.Rows.Count == 1, "沒有本次成績的歷史學生仍應留在確認清單");
            SemesterRankStudentSqlResult sql = SemesterRankSql.BuildLegacySelects(list);
            string joined = string.Join("\n", sql.Grades[0].Selects);
            Expect(errors, sql.Grades.Count == 1 && joined.IndexOf("'5402'::BIGINT") >= 0, "可解析年級的歷史學生應進入計算學生清單，不因尚未查成績而刪除");
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            Expect(errors, form.IndexOf("subject_origin_score IS NOT NULL") >= 0 && form.IndexOf("entry_score IS NOT NULL") >= 0, "沒有該項成績時仍應沿用既有條件，不產生該項明細");
            int begin = form.IndexOf("void BeginHistorySnapshotRestore");
            int show = form.IndexOf("void ShowConfirmList");
            string history = begin >= 0 && show > begin ? form.Substring(begin, show - begin) : "";
            Expect(errors, history.IndexOf("sems_subj_score") < 0 && history.IndexOf("sems_entry_score") < 0, "歷史母群還原不應依本次成績決定學生是否留下");
            return errors;
        }

        private static List<string> CheckUnparsedYear()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup blank = Student("5401", "", "舊班", "普通科");
            StudentMotherGroup invalid = Student("5403", "高一", "舊班", "普通科");
            StudentMotherGroup parsed = Student("5402", "2年級", "舊班", "普通科");
            MotherGroupConfirmList list = ListOf(blank, invalid, parsed);
            string message = HistoryCalculationScope.UnparsedYearMessage(list);
            Expect(errors, message.IndexOf("5401") >= 0 && message.IndexOf("（空白）") >= 0 && message.IndexOf("5403") >= 0 && message.IndexOf("高一") >= 0, "空白或無法解析的年排名應提示學生與原名稱");
            Expect(errors, message.IndexOf("5402") < 0 && message.IndexOf("目前年級") >= 0 && message.IndexOf("新班") < 0, "可解析的學生不應被提示，也不應改用目前年級或目前班級");
            Expect(errors, list.Rows.Count == 3 && HistoryCalculationScope.KeepsStudentWithoutScore(list, "5401") && HistoryCalculationScope.KeepsStudentWithoutScore(list, "5403"), "年級解析失敗仍應保留學生");
            SemesterRankStudentSqlResult sql = SemesterRankSql.BuildLegacySelects(list);
            string joined = JoinSelects(sql);
            Expect(errors, sql.SkippedWithoutGrade.Contains("5401") && sql.SkippedWithoutGrade.Contains("5403") && joined.IndexOf("'5401'") < 0 && joined.IndexOf("'5403'") < 0, "無法解析年級時不產生該生的新排名明細");
            Expect(errors, joined.IndexOf("'5402'::BIGINT") >= 0, "其他可解析學生仍應產生排名學生列");
            return errors;
        }

        private static List<string> CheckCurrentClassDoesNotReplaceHistory()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup student = Student("5402", "2年級", "舊班", "歷史科");
            ConfirmStudentRow row = new ConfirmStudentRow();
            row.Group = student;
            row.ClassName = "新班";
            row.SeatNo = "9";
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow> { row });
            string sql = JoinSelects(SemesterRankSql.BuildLegacySelects(list));
            Expect(errors, student.GetWorking(RankKinds.Class).GroupName == "舊班" && student.GetWorking(RankKinds.Department).GroupName == "歷史科" && student.GetWorking(RankKinds.Year).GroupName == "2年級", "目前班級不應覆蓋歷史母群名稱");
            Expect(errors, sql.IndexOf("舊班") >= 0 && sql.IndexOf("歷史科") >= 0 && sql.IndexOf("新班") < 0, "計算學生列應使用歷史班名與科別");
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int shown = form.IndexOf("void ShowConfirmList");
            int clear = form.IndexOf("void ClearLoadedConfirmList");
            string confirm = shown >= 0 && clear > shown ? form.Substring(shown, clear - shown) : "";
            Expect(errors, confirm.IndexOf("row.ClassName") >= 0 && confirm.IndexOf("GroupName") < 0, "確認清單只把目前班級放在顯示欄，不寫入母群名稱");
            return errors;
        }

        private static List<string> CheckSharedScope()
        {
            List<string> errors = new List<string>();
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int next = form.IndexOf("void btnNext_Click");
            int history = form.IndexOf("void BeginHistorySnapshotRestore");
            string nextBody = next >= 0 && history > next ? form.Substring(next, history - next) : "";
            int captureCall = nextBody.IndexOf("CaptureCalculationScope()");
            int historyCall = nextBody.IndexOf("BeginHistorySnapshotRestore()");
            Expect(errors, captureCall >= 0 && historyCall > captureCall, "兩種來源按下一步時都應先重建年級與擇優採計");
            Expect(errors, nextBody.IndexOf("_GradeYearList =") < 0 && nextBody.IndexOf("_GradeSelectList =") < 0, "目前學生與歷史來源不應各自沿用前一次清單");
            int scope = form.IndexOf("void CaptureCalculationScope");
            int calculate = form.IndexOf("void btnCacluate_Click");
            string scopeBody = scope >= 0 && calculate > scope ? form.Substring(scope, calculate - scope) : "";
            Expect(errors, scopeBody.IndexOf("_GradeYearList = new List<int>()") >= 0 && scopeBody.IndexOf("listGradeYear") >= 0 && scopeBody.IndexOf("_GradeSelectList = new List<string>()") >= 0 && scopeBody.IndexOf("原始成績") >= 0 && scopeBody.IndexOf("chbSYAdj") >= 0, "共用流程應依圖1年級勾選與擇優採計重新建立");
            string historyBody = form.Substring(history, form.IndexOf("void ShowConfirmList") - history);
            Expect(errors, historyBody.IndexOf("lbSchoolYear.Text = calculationSchoolYear") >= 0 && historyBody.IndexOf("lbSemester.Text = calculationSemester") >= 0, "圖2計算期間仍取自圖1");
            int warn = form.IndexOf("UnparsedYearMessage", calculate);
            int gate = form.IndexOf("CanWriteRankTables", calculate);
            Expect(errors, warn > calculate && gate > warn, "年排名無法解析時應在寫入保護之前提示");
            string gateFile = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gateFile.IndexOf("return true;") >= 0 && gateFile.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static MotherGroupConfirmList ListOf(params StudentMotherGroup[] students)
        {
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup student in students)
            {
                ConfirmStudentRow row = new ConfirmStudentRow();
                row.Group = student;
                row.ClassName = "新班";
                rows.Add(row);
            }
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(rows);
            return list;
        }

        private static StudentMotherGroup Student(string id, string year, string className, string department)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentName = "歷史生";
            student.SetSourceSlots(new List<RankSlot>
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = year, Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = department, Participate = true },
                new RankSlot { RankType = RankKinds.Class, GroupName = className, Participate = true },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "", Participate = false },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
            });
            return student;
        }

        private static string JoinSelects(SemesterRankStudentSqlResult sql)
        {
            StringBuilder text = new StringBuilder();
            foreach (SemesterRankGradeStudents grade in sql.Grades)
            {
                foreach (string select in grade.Selects)
                    text.Append(select);
            }
            return text.ToString();
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
