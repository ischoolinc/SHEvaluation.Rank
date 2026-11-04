using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase3Task3233Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckBlankNames());
                errors.AddRange(CheckMissingScores());
                errors.AddRange(CheckTransformer());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase3-task3233-check: ok");
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

        public static List<string> CheckBlankNames()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup blank = Student("501", "空白母群", "1年級", true, "", true, "忠", true, "", true, "", true);
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, blank, false);
            StudentMotherGroup named = Student("502", "有類別", "1年級", true, "普通科", true, "孝", true, "甲", true, "社團", false);
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[]
            {
                new ConfirmStudentRow { Group = blank },
                new ConfirmStudentRow { Group = named }
            });

            Expect(errors, !blank.GetWorking(RankKinds.Tag1).Participate && blank.GetWorking(RankKinds.Tag1).GroupName == "", "空白類別一應為不參與且名稱維持空白");
            Expect(errors, !blank.GetWorking(RankKinds.Tag2).Participate && blank.GetWorking(RankKinds.Tag2).GroupName == "", "空白類別二應為不參與且名稱維持空白");
            Expect(errors, blank.GetWorking(RankKinds.Department).Participate && blank.GetWorking(RankKinds.Department).GroupName == "", "目前學生的空白科別應維持參與");
            Expect(errors, named.GetWorking(RankKinds.Tag1).Participate && !named.GetWorking(RankKinds.Tag2).Participate, "兩名學生的類別參與旗標應各自獨立");

            SemesterRankStudentSqlResult built = SemesterRankSql.BuildLegacySelects(list);
            string gradeSql = string.Join("\nUNION ALL\n", built.Grades[0].Selects.ToArray());
            string blankSql = Slice(gradeSql, "'501'::BIGINT", "'502'::BIGINT");
            string namedSql = gradeSql.Substring(gradeSql.IndexOf("'502'::BIGINT"));
            Expect(errors, blankSql.IndexOf("''::TEXT AS rank_dept_name") >= 0 && blankSql.IndexOf("true AS participate_dept") >= 0, "空白科別列應帶空名稱與參與 true");
            Expect(errors, blankSql.IndexOf("''::TEXT AS rank_tag1") >= 0 && blankSql.IndexOf("false AS participate_tag1") >= 0, "空白類別一列應帶空名稱與不參與");
            Expect(errors, blankSql.IndexOf("''::TEXT AS rank_tag2") >= 0 && blankSql.IndexOf("false AS participate_tag2") >= 0, "空白類別二列應帶空名稱與不參與");
            Expect(errors, blankSql.IndexOf("true AS participate_year") >= 0 && blankSql.IndexOf("true AS participate_class") >= 0, "空白類別不應改掉年排名與班排名");
            Expect(errors, namedSql.IndexOf("'甲'::TEXT AS rank_tag1") >= 0 && namedSql.IndexOf("true AS participate_tag1") >= 0 && namedSql.IndexOf("false AS participate_tag2") >= 0, "另一名學生的類別旗標不受空白類別影響");

            string sql = TransformedSql(errors);
            if (sql.Length == 0)
                return errors;
            string[] branches = Branches(sql);
            Expect(errors, branches.Length == 20, "寫入分支應維持四種成績乘五種排名");
            int tag1 = 0;
            int tag2 = 0;
            int dept = 0;
            foreach (string branch in branches)
            {
                string after = AfterFrom(branch);
                if (branch.IndexOf("'類別1排名'::TEXT AS rank_type") >= 0)
                {
                    tag1++;
                    Expect(errors, CountOf(after, "rank_tag1 <> ''") == 1 && CountOf(after, "participate_tag1") == 1, "類別一寫入應同時排除空白名稱與不參與者");
                    Expect(errors, branch.IndexOf("rank_tag1 AS rank_name") >= 0 && branch.IndexOf("tag1_count AS matrix_count") >= 0, "類別一的 rank_name 與 matrix_count 應沿用原欄位");
                    Expect(errors, after.IndexOf("participate_year") < 0 && after.IndexOf("participate_dept") < 0 && after.IndexOf("participate_class") < 0 && after.IndexOf("participate_tag2") < 0, "類別一條件不應帶入其他排名旗標");
                }
                if (branch.IndexOf("'類別2排名'::TEXT AS rank_type") >= 0)
                {
                    tag2++;
                    Expect(errors, CountOf(after, "rank_tag2 <> ''") == 1 && CountOf(after, "participate_tag2") == 1, "類別二寫入應同時排除空白名稱與不參與者");
                    Expect(errors, branch.IndexOf("rank_tag2 AS rank_name") >= 0 && branch.IndexOf("tag2_count AS matrix_count") >= 0, "類別二的 rank_name 與 matrix_count 應沿用原欄位");
                }
                if (branch.IndexOf("'科排名'::TEXT AS rank_type") >= 0)
                {
                    dept++;
                    Expect(errors, after.IndexOf("rank_dept_name <> ''") < 0 && after.IndexOf("rank_dept_name IS NOT NULL") < 0, "空白科別不應被名稱條件排除");
                    Expect(errors, CountOf(after, "participate_dept") == 1, "科排名只應加上自己的參與條件");
                    Expect(errors, branch.IndexOf("rank_dept_name AS rank_name") >= 0 && branch.IndexOf("dept_count AS matrix_count") >= 0, "科排名的 rank_name 可為空白，matrix_count 使用科人數");
                    Expect(errors, after.IndexOf("participate_tag1") < 0 && after.IndexOf("participate_tag2") < 0, "科排名條件不應受類別旗標影響");
                }
                if (branch.IndexOf("'年排名'::TEXT AS rank_type") >= 0)
                {
                    Expect(errors, branch.IndexOf("'' || rank_grade_year || '年級'::TEXT AS rank_name") >= 0 && branch.IndexOf("grade_count AS matrix_count") >= 0, "年排名名稱與人數欄位應維持原寫法");
                    Expect(errors, AfterFrom(branch).IndexOf("participate_dept") < 0, "年排名不應因空白科別被排除");
                }
                if (branch.IndexOf("'班排名'::TEXT AS rank_type") >= 0)
                    Expect(errors, branch.IndexOf("rank_class_name AS rank_name") >= 0 && branch.IndexOf("class_count AS matrix_count") >= 0, "班排名名稱與人數欄位應維持原寫法");
            }
            Expect(errors, tag1 == 4 && tag2 == 4 && dept == 4, "類別與科排名都應涵蓋四種成績");
            return errors;
        }

        public static List<string> CheckMissingScores()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup noSubject = Student("601", "缺科目", "1年級", true, "普通科", true, "忠", true, "甲", true, "社團", true);
            StudentMotherGroup noEntry = Student("602", "缺分項", "1年級", true, "普通科", true, "忠", true, "甲", true, "社團", true);
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new ConfirmStudentRow[]
            {
                new ConfirmStudentRow { Group = noSubject },
                new ConfirmStudentRow { Group = noEntry }
            });
            SemesterRankSql.BuildLegacySelects(list);
            Expect(errors, list.Rows.Count == 2 && list.Rows[0].Group.StudentId == "601" && list.Rows[1].Group.StudentId == "602", "缺少成績不應從確認清單移除學生");

            string sql = TransformedSql(errors);
            if (sql.Length == 0)
                return errors;
            Expect(errors, CountOf(sql, "subject IS NOT NULL") >= 2 && CountOf(sql, "subject_origin_score IS NOT NULL") == 1 && CountOf(sql, "subject_greatest_score IS NOT NULL") == 1, "科目原始與擇優仍先排除沒有分數的列");
            Expect(errors, CountOf(sql, "entry IS NOT NULL") == 1 && CountOf(sql, "entry_score IS NOT NULL") == 1, "分項成績仍先排除沒有分數的列");
            string origin = Between(sql, ", subj_origin_rank AS", ", subj_greatest_rank AS");
            string greatest = Between(sql, ", subj_greatest_rank AS", ", subj_origin_rank_expand AS");
            string entry = Between(sql, ", entry_rank_list AS", ", entry_rank_expand AS");
            Expect(errors, origin.IndexOf("COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject, participate_year)") >= 0 && origin.IndexOf("subject_origin_score IS NOT NULL") > origin.IndexOf("FROM"), "科目原始人數應在分數過濾後的同一查詢計算");
            Expect(errors, greatest.IndexOf("subject_greatest_score IS NOT NULL") > greatest.IndexOf("FROM") && greatest.IndexOf("GREATEST") < 0, "擇優排名使用已算出的分數，不改 GREATEST 公式");
            Expect(errors, entry.IndexOf("entry_score IS NOT NULL") > entry.IndexOf("FROM") && entry.IndexOf("COUNT(student_id) OVER(PARTITION BY rank_grade_year, entry, participate_year)") >= 0, "分項人數應在分數過濾後的同一查詢計算");
            int detailRank = sql.IndexOf("rank_detail");
            Expect(errors, detailRank > sql.IndexOf(", update_data AS"), "rank_detail 插入仍在原寫入區");
            string detailSql = sql.Substring(detailRank);
            Expect(errors, detailSql.IndexOf("score_list") >= 0 && detailSql.IndexOf("student_list") < 0, "rank_detail 只來自已有分數的 score_list");
            Expect(errors, sql.IndexOf("GREATEST") >= 0 && sql.IndexOf("/root/Subject/@原始成績") >= 0 && sql.IndexOf("/root/Entry/@分項") >= 0, "原始、擇優與分項的分數取得應保留");
            return errors;
        }

        public static List<string> CheckTransformer()
        {
            List<string> errors = new List<string>();
            string template = Template(errors);
            if (template.Length == 0)
                return errors;
            string once = SemesterRankSql.ApplyParticipation(template);
            string twice = SemesterRankSql.ApplyParticipation(once);
            Expect(errors, once == twice, "同一段 SQL 套用兩次不應重複插入旗標或條件");

            List<string> before = Partitions(template);
            List<string> after = Partitions(once);
            Expect(errors, before.Count == after.Count && before.Count > 0, "窗口數量應與轉換前相同");
            for (int i = 0; i < before.Count && i < after.Count; i++)
            {
                string expected = ExpectedFlag(before[i]);
                Expect(errors, expected.Length > 0, "無法判斷窗口所屬排名：" + before[i]);
                Expect(errors, after[i].IndexOf(expected) >= 0, "窗口應加上對應旗標：" + before[i]);
                Expect(errors, CountOf(after[i], "participate_") == 1, "一個窗口只應有一個參與旗標：" + after[i]);
            }
            Expect(errors, once.IndexOf("participate_year, participate_year") < 0 && once.IndexOf("participate_dept, participate_dept") < 0, "參與旗標不應重複接在同一分組");
            Expect(errors, CountOf(once, "rank_tag1 <> ''") == CountOf(template, "rank_tag1 <> ''") && CountOf(template, "rank_tag1 <> ''") == 4, "類別一空白名稱條件不應被漏掉或複製");
            Expect(errors, CountOf(once, "rank_tag2 <> ''") == CountOf(template, "rank_tag2 <> ''") && CountOf(template, "rank_tag2 <> ''") == 4, "類別二空白名稱條件不應被漏掉或複製");
            Expect(errors, once.IndexOf("rank_dept_name <> ''") < 0 && template.IndexOf("rank_dept_name <> ''") < 0, "轉換不應新增空白科別排除");
            Expect(errors, CountOf(once, "subject_origin_score IS NOT NULL") == CountOf(template, "subject_origin_score IS NOT NULL"), "轉換不應改動科目分數過濾");
            Expect(errors, CountOf(once, "entry_score IS NOT NULL") == CountOf(template, "entry_score IS NOT NULL"), "轉換不應改動分項分數過濾");

            string originalTail = Tail(template.Replace("\r\n", "\n").Replace("\r", "\n"));
            string convertedTail = Tail(once);
            Expect(errors, originalTail == convertedTail && originalTail.IndexOf("INSERT INTO") >= 0 && originalTail.IndexOf("rank_matrix") >= 0 && originalTail.IndexOf("rank_detail") >= 0, "rank_* 寫入語句應與轉換前相同");
            Expect(errors, once.IndexOf("XMLTABLE") < 0 && once.IndexOf("MERGE") < 0 && once.IndexOf("PROCEDURE") < 0, "轉換不應引入 PostgreSQL 9.6 沒有的語法");

            string source = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int gate = source.IndexOf("CanWriteRankTables");
            int select = source.IndexOf("queryHelper.Select(insertRankSql)");
            Expect(errors, gate >= 0 && select > gate, "寫入閘門仍位於資料庫更新之前");
            return errors;
        }

        private static string ExpectedFlag(string partition)
        {
            if (partition.IndexOf("rank_dept_name") >= 0)
                return "participate_dept";
            if (partition.IndexOf("rank_tag1") >= 0)
                return "participate_tag1";
            if (partition.IndexOf("rank_tag2") >= 0)
                return "participate_tag2";
            if (partition.IndexOf("rank_class_name") >= 0)
                return "participate_class";
            if (partition.IndexOf("rank_grade_year") >= 0)
                return "participate_year";
            return "";
        }

        private static List<string> Partitions(string sql)
        {
            List<string> found = new List<string>();
            int at = 0;
            const string mark = "PARTITION BY ";
            while (at >= 0 && at < sql.Length)
            {
                at = sql.IndexOf(mark, at);
                if (at < 0)
                    break;
                int start = at + mark.Length;
                int end = start;
                while (end < sql.Length && sql[end] != ')' && !(sql[end] == ' ' && sql.Substring(end).StartsWith(" ORDER")))
                    end++;
                found.Add(sql.Substring(start, end - start).Trim());
                at = end;
            }
            return found;
        }

        private static string TransformedSql(List<string> errors)
        {
            string template = Template(errors);
            if (template.Length == 0)
                return "";
            return SemesterRankSql.ApplyParticipation(template);
        }

        private static string Template(List<string> errors)
        {
            string path = "CalculateSemesterAssessmentRank.cs";
            if (!File.Exists(path))
            {
                errors.Add("找不到排名 SQL 來源檔：" + path);
                return "";
            }
            string source = File.ReadAllText(path, Encoding.UTF8);
            Expect(errors, source.IndexOf("SemesterRankFormulaSql.Build(gradeSelectCopy)") >= 0, "產品畫面應改呼叫共用公式組句");
            string template = SemesterRankFormulaSql.BuildOriginOnly();
            if (template.IndexOf(", subject_score AS") < 0 || template.IndexOf("SELECT count(*) FROM score_list") < 0)
            {
                errors.Add("應能從共用公式組句取得年級排名 SQL");
                return "";
            }
            return template;
        }

        private static string[] Branches(string sql)
        {
            return Between(sql, ", score_list AS", ", update_data AS").Split(new string[] { "UNION ALL" }, StringSplitOptions.None);
        }

        private static string AfterFrom(string branch)
        {
            int fromAt = branch.LastIndexOf("FROM");
            if (fromAt < 0)
                return "";
            return branch.Substring(fromAt);
        }

        private static string Tail(string sql)
        {
            int at = sql.IndexOf(", update_data AS");
            if (at < 0)
                return "";
            return sql.Substring(at);
        }

        private static string Between(string text, string start, string end)
        {
            int from = text.IndexOf(start);
            int to = text.IndexOf(end, from < 0 ? 0 : from);
            if (from < 0 || to < from)
                return "";
            return text.Substring(from, to - from);
        }

        private static string Slice(string text, string start, string end)
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

        private static StudentMotherGroup Student(string id, string name, string year, bool yearParticipate, string dept, bool deptParticipate, string className, bool classParticipate, string tag1, bool tag1Participate, string tag2, bool tag2Participate)
        {
            StudentMotherGroup group = new StudentMotherGroup();
            group.StudentId = id;
            group.StudentName = name;
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

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
