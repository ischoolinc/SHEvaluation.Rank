using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase3Task31Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckStudentList());
                errors.AddRange(CheckGeneratedSql());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase3-task31-check: ok");
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

        public static List<string> CheckStudentList()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup mixed = Student("101", "王O'Hara", "1年級", true, "普通科", false, "忠", true, "甲", false, "社團", true, false);
            StudentMotherGroup none = Student("102", "李", "1年級", false, "普通科", false, "忠", false, "乙", false, "", false, false);
            StudentMotherGroup classOnly = Student("103", "張", "1年級", false, "", true, "孝", true, "甲", false, "社團", false, true);
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, classOnly, true);
            StudentMotherGroup otherGrade = Student("201", "陳", "2年級", true, "商科", true, "仁", true, "甲", true, "社團", true, false);
            StudentMotherGroup noGrade = Student("301", "林", "", true, "普通科", true, "忠", true, "甲", true, "社團", true, false);

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[]
            {
                new ConfirmStudentRow { Group = mixed, ClassName = "忠", SeatNo = "1" },
                new ConfirmStudentRow { Group = none, ClassName = "忠", SeatNo = "2" },
                new ConfirmStudentRow { Group = classOnly, ClassName = "孝", SeatNo = "3" },
                new ConfirmStudentRow { Group = otherGrade, ClassName = "仁", SeatNo = "1" },
                new ConfirmStudentRow { Group = noGrade, ClassName = "忠", SeatNo = "4" }
            });
            list.SetParticipate(new string[] { "103" }, RankKinds.Class, true);
            list.SetParticipate(new string[] { "101" }, RankKinds.Year, false);

            Expect(errors, list.VisibleStudentIds("不會命中").Count == 0, "搜尋條件只用於這個對照");
            SemesterRankStudentSqlResult sql = SemesterRankSql.BuildLegacySelects(list);
            Expect(errors, sql.SkippedWithoutGrade.Count == 1 && sql.SkippedWithoutGrade[0] == "301", "年排名名稱無法解析時不臆測年級");
            Expect(errors, sql.Grades.Count == 2, "應依年排名名稱分成兩個年級");

            string grade1 = JoinGrade(sql, 1);
            string grade2 = JoinGrade(sql, 2);
            Expect(errors, CountOf(grade1, "AS student_id") == 3, "一年級應包含參與狀態不同的三名學生");
            Expect(errors, grade1.IndexOf("'101'::BIGINT") >= 0 && grade1.IndexOf("'102'::BIGINT") >= 0 && grade1.IndexOf("'103'::BIGINT") >= 0, "student_list 應包含完整工作副本，不只搜尋結果");
            Expect(errors, grade1.IndexOf("王O''Hara") >= 0, "姓名單引號應跳脫");
            Expect(errors, grade1.IndexOf("'普通科'::TEXT AS rank_dept_name") >= 0 && grade1.IndexOf("'忠'::TEXT AS rank_class_name") >= 0 && grade1.IndexOf("'甲'::TEXT AS rank_tag1") >= 0 && grade1.IndexOf("'社團'::TEXT AS rank_tag2") >= 0, "每名學生應帶入五種母群名稱");
            Expect(errors, Slice(grade1, "'101'::BIGINT", "'102'::BIGINT").IndexOf("false AS participate_year") >= 0 && Slice(grade1, "'101'::BIGINT", "'102'::BIGINT").IndexOf("false AS participate_dept") >= 0 && Slice(grade1, "'101'::BIGINT", "'102'::BIGINT").IndexOf("true AS participate_class") >= 0, "個別改參與後應讀工作副本，且年排名不參與仍保留班排名");
            string noneSql = Slice(grade1, "'102'::BIGINT", "'103'::BIGINT");
            Expect(errors, noneSql.IndexOf("false AS participate_year") >= 0 && noneSql.IndexOf("false AS participate_dept") >= 0 && noneSql.IndexOf("false AS participate_class") >= 0 && noneSql.IndexOf("false AS participate_tag1") >= 0 && noneSql.IndexOf("false AS participate_tag2") >= 0, "全不參與的學生仍應留在 student_list");
            string classSql = grade1.Substring(grade1.IndexOf("'103'::BIGINT"));
            Expect(errors, classSql.IndexOf("false AS participate_year") >= 0 && classSql.IndexOf("true AS participate_class") >= 0 && classSql.IndexOf("false AS participate_dept") >= 0, "圖1排除只影響初始值，計算應採用圖2最終旗標");
            Expect(errors, grade1.IndexOf("WHERE") < 0 && grade1.IndexOf("Cells[") < 0, "student_list 不從畫面儲存格產生，也不在清單層整筆排除");
            Expect(errors, grade2.IndexOf("'201'::BIGINT") >= 0 && grade2.IndexOf("true AS participate_year") >= 0 && grade2.IndexOf("'301'") < 0, "其他年級與無法解析年級應分開");
            return errors;
        }

        public static List<string> CheckGeneratedSql()
        {
            List<string> errors = new List<string>();
            string path = "CalculateSemesterAssessmentRank.cs";
            if (!File.Exists(path))
            {
                errors.Add("找不到排名 SQL 來源檔：" + path);
                return errors;
            }
            string source = File.ReadAllText(path, Encoding.UTF8);
            int start = source.IndexOf(", subject_score AS");
            int end = source.IndexOf("SELECT count(*) FROM score_list");
            Expect(errors, start >= 0 && end > start, "應能從表單取出年級排名 SQL");
            if (start < 0 || end <= start)
                return errors;
            string template = source.Substring(start, end - start);
            string sql = SemesterRankSql.ApplyParticipation(template);
            Expect(errors, sql.IndexOf("PARTITION BY rank_grade_year, subject ORDER") < 0, "年排名 RANK 分組應已帶參與旗標");
            Expect(errors, sql.IndexOf("PARTITION BY rank_grade_year, subject, participate_year ORDER BY subject_origin_score DESC") >= 0, "原始成績年排名應在 RANK 前以參與旗標分組");
            Expect(errors, sql.IndexOf("COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject, participate_year)") >= 0, "年排名 COUNT 應與參與者同一分組");
            Expect(errors, sql.IndexOf("STDDEV_POP(origin_score) OVER(PARTITION BY rank_grade_year, item_name, participate_year)") >= 0, "原始成績標準差應在同一參與分組");
            Expect(errors, sql.IndexOf("PARTITION BY rank_grade_year, rank_dept_name, subject, participate_dept") >= 0, "科排名窗口應使用科參與旗標");
            Expect(errors, sql.IndexOf("PARTITION BY rank_class_name, subject, participate_class") >= 0, "班排名窗口應使用班參與旗標");
            Expect(errors, sql.IndexOf("PARTITION BY rank_grade_year, rank_tag1, item_name, participate_tag1") >= 0, "類別一統計應使用類別一旗標");
            Expect(errors, sql.IndexOf("PARTITION BY rank_grade_year, rank_tag2, entry, participate_tag2") >= 0, "類別二分項窗口應使用類別二旗標");
            Expect(errors, sql.IndexOf("student_list.participate_year") >= 0 && sql.IndexOf("\t\t, participate_year\n") >= 0, "成績列應帶出五個參與旗標");
            Expect(errors, sql.IndexOf("GREATEST") >= 0 && sql.IndexOf("/root/Subject/@原始成績") >= 0, "擇優與原始成績公式應保留");
            Expect(errors, sql.IndexOf("XMLTABLE") < 0 && sql.IndexOf("MERGE") < 0 && sql.IndexOf("PROCEDURE") < 0, "SQL 不使用 PostgreSQL 9.6 沒有的語法");
            Expect(errors, sql.IndexOf("DELETE FROM rank_detail") < 0 && sql.IndexOf("DELETE FROM\n\t\trank_detail") < 0, "不參與者不是先算出再刪除排名明細");

            string scoreList = Between(sql, ", score_list AS", ", update_data AS");
            string[] branches = scoreList.Split(new string[] { "UNION ALL" }, StringSplitOptions.None);
            Expect(errors, branches.Length == 20, "四種成績乘五種排名應有 20 個寫入分支");
            int[] seen = new int[5];
            string[] types = new string[] { "年排名", "科排名", "班排名", "類別1排名", "類別2排名" };
            string[] flags = new string[] { "participate_year", "participate_dept", "participate_class", "participate_tag1", "participate_tag2" };
            foreach (string branch in branches)
            {
                int fromAt = branch.LastIndexOf("FROM");
                Expect(errors, fromAt >= 0, "score_list 分支應有 FROM");
                if (fromAt < 0)
                    continue;
                string after = branch.Substring(fromAt);
                for (int i = 0; i < types.Length; i++)
                {
                    if (branch.IndexOf("'" + types[i] + "'::TEXT AS rank_type") < 0)
                        continue;
                    seen[i]++;
                    Expect(errors, after.IndexOf(flags[i]) >= 0, types[i] + " 寫入列應只保留該項參與者");
                    for (int other = 0; other < flags.Length; other++)
                    {
                        if (other != i)
                            Expect(errors, after.IndexOf(flags[other]) < 0, types[i] + " 不應被其他排名的旗標整筆排除");
                    }
                }
            }
            for (int i = 0; i < seen.Length; i++)
                Expect(errors, seen[i] == 4, types[i] + " 應涵蓋四種成績");

            int gate = source.IndexOf("CanWriteRankTables");
            int apply = source.IndexOf("SemesterRankSql.ApplyParticipation");
            int select = source.IndexOf("queryHelper.Select(insertRankSql)");
            int build = source.IndexOf("SemesterRankSql.Build(");
            Expect(errors, gate >= 0 && build > gate && apply > gate && select > apply, "寫入閘門仍在產生 SQL 與資料庫更新之前");
            Expect(errors, CountOf(source, "foreach (DataGridViewRow row in dgvStudentList.Rows)") == 1, "作用中的學生清單不應再讀取畫面列");
            return errors;
        }

        private static StudentMotherGroup Student(string id, string name, string year, bool yearParticipate, string dept, bool deptParticipate, string className, bool classParticipate, string tag1, bool tag1Participate, string tag2, bool tag2Participate, bool excluded)
        {
            StudentMotherGroup group = new StudentMotherGroup();
            group.StudentId = id;
            group.StudentName = name;
            group.MatchesExcludedCategory = excluded;
            group.SetSourceSlots(new RankSlot[]
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = year, Participate = yearParticipate },
                new RankSlot { RankType = RankKinds.Department, GroupName = dept, Participate = deptParticipate },
                new RankSlot { RankType = RankKinds.Class, GroupName = className, Participate = classParticipate },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = tag1, Participate = tag1Participate },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = tag2, Participate = tag2Participate }
            });
            return group;
        }

        private static string JoinGrade(SemesterRankStudentSqlResult sql, int gradeYear)
        {
            foreach (SemesterRankGradeStudents grade in sql.Grades)
            {
                if (grade.GradeYear == gradeYear)
                    return string.Join("\nUNION ALL\n", grade.Selects.ToArray());
            }
            return "";
        }

        private static string Slice(string text, string start, string end)
        {
            int from = text.IndexOf(start);
            int to = text.IndexOf(end, from < 0 ? 0 : from);
            if (from < 0 || to < from)
                return "";
            return text.Substring(from, to - from);
        }

        private static string Between(string text, string start, string end)
        {
            int from = text.IndexOf(start);
            int to = text.IndexOf(end, from < 0 ? 0 : from);
            if (from < 0 || to < from)
                return "";
            return text.Substring(from, to - from);
        }

        private static int CountOf(string text, string value)
        {
            int count = 0;
            int at = 0;
            while (at >= 0 && at < text.Length)
            {
                at = text.IndexOf(value, at);
                if (at < 0)
                    break;
                count++;
                at += value.Length;
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
