using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase5Task55Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckUniqueStudent());
                errors.AddRange(CheckNotFound());
                errors.AddRange(CheckAmbiguous());
                errors.AddRange(CheckAlreadyListed());
                errors.AddRange(CheckExistingHistoryUntouched());
                errors.AddRange(CheckMissingScoreStays());
                errors.AddRange(CheckSql());
                errors.AddRange(CheckForm());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase5-task55-check: ok");
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

        private static List<string> CheckUniqueStudent()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList list = HistoryList();
            HistoryStudentAddResult result = HistoryStudentAdd.Read(" S550 ", Table(Row("550", "S550", "新同學", "1", "忠班", "3", "普通科")), list, "不排名", "類別一", "類別二", Tags(), Links("550", false));
            Expect(errors, result.Status == HistoryStudentAddStatus.Added && result.Student != null, "唯一學號應可新增");
            StudentMotherGroup student = result.Student;
            Expect(errors, student.StudentId == "550" && student.StudentNumber == "S550" && student.StudentName == "新同學", "應採用查到的學生識別、學號與姓名");
            Expect(errors, student.GetWorking(RankKinds.Year).GroupName == "1年級" && student.GetWorking(RankKinds.Year).Participate, "年排名應來自目前年級");
            Expect(errors, student.GetWorking(RankKinds.Department).GroupName == "普通科" && student.GetWorking(RankKinds.Department).Participate, "科排名應來自目前科別");
            Expect(errors, student.GetWorking(RankKinds.Class).GroupName == "忠班" && student.GetWorking(RankKinds.Class).Participate, "班排名應來自目前班級");
            Expect(errors, student.GetWorking(RankKinds.Tag1).GroupName == "甲組" && student.GetWorking(RankKinds.Tag1).Participate, "類別一應沿用目前標籤的第一個名稱");
            Expect(errors, student.GetWorking(RankKinds.Tag2).GroupName == "" && !student.GetWorking(RankKinds.Tag2).Participate, "沒有類別二時名稱空白且不參與");
            list.Add(new ConfirmStudentRow { Group = student, ClassName = result.ClassName, SeatNo = result.SeatNo });
            Expect(errors, list.Rows.Count == 2 && list.Rows[1].Group.StudentId == "550", "新增後應立即出現在確認清單");
            list.SetParticipate(new string[] { "550" }, RankKinds.Year, false);
            Expect(errors, !student.GetWorking(RankKinds.Year).Participate && student.GetWorking(RankKinds.Year).GroupName == "1年級", "新增後應可調整參與，且母群名稱保持唯讀值");
            return errors;
        }

        private static List<string> CheckNotFound()
        {
            List<string> errors = new List<string>();
            HistoryStudentAddResult result = HistoryStudentAdd.Read("S551", Table(), HistoryList(), "", "", "", Tags(), new List<StudentTagLink>());
            Expect(errors, result.Status == HistoryStudentAddStatus.NotFound && result.Student == null && result.Message.IndexOf("找不到學號") >= 0, "找不到學生時應說明且不新增");
            bool rejected = false;
            try
            {
                HistoryStudentAdd.SelectSql("  ");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "空白學號不應組成查詢");
            return errors;
        }

        private static List<string> CheckAmbiguous()
        {
            List<string> errors = new List<string>();
            DataTable table = Table();
            table.Rows.Add(Row("551", "S552", "甲", "1", "忠班", "1", "普通科"));
            table.Rows.Add(Row("552", "S552", "乙", "2", "孝班", "2", "商業科"));
            MotherGroupConfirmList list = HistoryList();
            HistoryStudentAddResult result = HistoryStudentAdd.Read("S552", table, list, "", "", "", Tags(), new List<StudentTagLink>());
            Expect(errors, result.Status == HistoryStudentAddStatus.Ambiguous && result.Student == null && result.Message.IndexOf("多位學生") >= 0, "相同學號有多位學生時不應任選");
            Expect(errors, list.Rows.Count == 1, "無法唯一對應時不應新增列");
            return errors;
        }

        private static List<string> CheckAlreadyListed()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList list = HistoryList();
            HistoryStudentAddResult result = HistoryStudentAdd.Read("S500", Table(Row("500", "S500", "歷史生", "3", "新班", "8", "商業科")), list, "", "", "", Tags(), new List<StudentTagLink>());
            Expect(errors, result.Status == HistoryStudentAddStatus.AlreadyListed && result.Student == null && list.Rows.Count == 1, "已在確認清單的學生不應重複新增");
            return errors;
        }

        private static List<string> CheckExistingHistoryUntouched()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList list = HistoryList();
            StudentMotherGroup history = list.Rows[0].Group;
            HistoryStudentAddResult excluded = HistoryStudentAdd.Read("S553", Table(Row("553", "S553", "排除生", "2", "孝班", "4", "普通科")), list, "不排名", "類別一", "類別二", Tags(), Links("553", true));
            Expect(errors, excluded.Status == HistoryStudentAddStatus.Added, "符合不排名類別的唯一學生仍應加入");
            foreach (string rankType in RankKinds.All)
                Expect(errors, excluded.Student.GetOriginal(rankType).GroupName == excluded.Student.GetWorking(rankType).GroupName && !excluded.Student.GetWorking(rankType).Participate, rankType + "應保留目前母群名稱，且初始參與為不參與");
            Expect(errors, history.GetWorking(RankKinds.Year).GroupName == "舊年" && history.GetWorking(RankKinds.Class).GroupName == "舊班" && history.GetWorking(RankKinds.Department).GroupName == "歷史科", "新增不應修改原本歷史學生的母群名稱");
            return errors;
        }

        private static List<string> CheckMissingScoreStays()
        {
            List<string> errors = new List<string>();
            MotherGroupConfirmList list = HistoryList();
            HistoryStudentAddResult result = HistoryStudentAdd.Read("S554", Table(Row("554", "S554", "無成績", "1", "忠班", "1", "普通科")), list, "", "", "", Tags(), new List<StudentTagLink>());
            list.Add(new ConfirmStudentRow { Group = result.Student, ClassName = result.ClassName, SeatNo = result.SeatNo });
            Expect(errors, HistoryCalculationScope.KeepsStudentWithoutScore(list, "554"), "新增學生沒有本次成績時仍應留在確認清單");
            string selects = "";
            foreach (SemesterRankGradeStudents grade in SemesterRankSql.BuildLegacySelects(list).Grades)
            {
                foreach (string select in grade.Selects)
                    selects += select;
            }
            Expect(errors, selects.IndexOf("'554'::BIGINT") >= 0, "可解析年級的新增學生應進入既有計算清單，由分數條件決定有無明細");
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            Expect(errors, form.IndexOf("subject_origin_score IS NOT NULL") >= 0 && form.IndexOf("entry_score IS NOT NULL") >= 0, "沒有該項成績時仍沿用既有條件，不產生排名明細");
            string adder = File.ReadAllText("MotherGroup\\HistoryStudentAdd.cs", Encoding.UTF8);
            Expect(errors, adder.IndexOf("sems_subj_score") < 0 && adder.IndexOf("INSERT") < 0 && adder.IndexOf("UPDATE") < 0 && adder.IndexOf("DELETE") < 0, "加入學生不應查成績，也不應寫入資料");
            return errors;
        }

        private static List<string> CheckSql()
        {
            List<string> errors = new List<string>();
            string sql = HistoryStudentAdd.SelectSql("O'Brien");
            Expect(errors, sql.IndexOf("student.student_number = 'O''Brien'") >= 0 && sql.IndexOf("class.class_name") >= 0 && sql.IndexOf("class.grade_year") >= 0 && sql.IndexOf("COALESCE(student.ref_dept_id, class.ref_dept_id)") >= 0, "應以學號查目前學生、班級與科別");
            Expect(errors, sql.IndexOf("school_year") < 0 && sql.IndexOf("semester") < 0 && sql.IndexOf("rank_batch") < 0, "查詢不應改寫歷史批次，也不應改用計算期間篩選");
            Expect(errors, sql.IndexOf("INSERT") < 0 && sql.IndexOf("UPDATE") < 0 && sql.IndexOf("DELETE") < 0 && sql.IndexOf(";") < 0 && sql.IndexOf("XMLTABLE") < 0 && sql.IndexOf("MERGE") < 0, "學號查詢應為 PostgreSQL 9.6 到 17.1 的單一唯讀語句");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int add = form.IndexOf("void AddHistoryStudent");
            int clear = form.IndexOf("void ClearLoadedConfirmList");
            string body = add >= 0 && clear > add ? form.Substring(add, clear - add) : "";
            Expect(errors, body.IndexOf("IsHistorySourceSelected()") >= 0 && body.IndexOf("HistoryStudentAdd.SelectSql") >= 0 && body.IndexOf("_confirmList.Add") >= 0, "歷史來源才依學號查詢並加入確認清單");
            Expect(errors, body.IndexOf("INSERT") < 0 && body.IndexOf("UPDATE") < 0 && body.IndexOf("DELETE") < 0 && body.IndexOf("rank_batch") < 0, "加入學生不應更新歷史排名批次");
            Expect(errors, form.IndexOf("\"加入學生\"") >= 0 && form.IndexOf("_btnAddHistoryStudent.Visible = historySource") >= 0, "加入學生入口只在歷史來源顯示");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static MotherGroupConfirmList HistoryList()
        {
            StudentMotherGroup history = new StudentMotherGroup();
            history.StudentId = "500";
            history.StudentNumber = "S500";
            history.StudentName = "歷史生";
            history.SetSourceSlots(new List<RankSlot>
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = "舊年", Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = "歷史科", Participate = true },
                new RankSlot { RankType = RankKinds.Class, GroupName = "舊班", Participate = true },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = "舊類", Participate = true },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = "", Participate = false }
            });
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow> { new ConfirmStudentRow { Group = history, ClassName = "現班" } });
            return list;
        }

        private static DataTable Table(params object[] row)
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id");
            table.Columns.Add("student_number");
            table.Columns.Add("student_name");
            table.Columns.Add("grade_year");
            table.Columns.Add("class_name");
            table.Columns.Add("seat_no");
            table.Columns.Add("dept_name");
            if (row != null && row.Length > 0)
                table.Rows.Add(row);
            return table;
        }

        private static object[] Row(string id, string number, string name, string grade, string className, string seat, string dept)
        {
            return new object[] { id, number, name, grade, className, seat, dept };
        }

        private static List<TagDefinition> Tags()
        {
            return new List<TagDefinition>
            {
                new TagDefinition { Id = "t1", Prefix = "類別一", Name = "甲組" },
                new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" }
            };
        }

        private static List<StudentTagLink> Links(string studentId, bool excluded)
        {
            List<StudentTagLink> links = new List<StudentTagLink>
            {
                new StudentTagLink { StudentId = studentId, TagId = "t1", Name = "甲組" }
            };
            if (excluded)
                links.Add(new StudentTagLink { StudentId = studentId, TagId = "ex", Name = "休學" });
            return links;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
