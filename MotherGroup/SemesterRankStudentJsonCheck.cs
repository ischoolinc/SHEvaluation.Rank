using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankStudentJsonCheck
    {
        public static int Main()
        {
            List<string> errors = new List<string>();
            try
            {
                CheckRoundTrip(errors);
                CheckDuplicateAndIdentity(errors);
                CheckPublish(errors);
                CheckSizes(errors);
            }
            catch (Exception ex)
            {
                errors.Add(ex.GetType().Name + ": " + ex.Message);
            }
            if (errors.Count > 0)
            {
                foreach (string error in errors)
                    Console.WriteLine(error);
                return 1;
            }
            Console.WriteLine("semester-rank-student-json-check ok");
            return 0;
        }

        private static void CheckRoundTrip(List<string> errors)
        {
            StudentMotherGroup student = Student("1001", "測試'學生\\\"A\n甲", "2年級", true, "普通科", true, "二年甲班", true, "一般生", true, "", false);
            MotherGroupConfirmList list = ListOf(student);
            SemesterRankStudentSqlResult built = SemesterRankSql.Build(list);
            SemesterRankStudentSqlResult legacy = SemesterRankSql.BuildLegacySelects(list);
            Expect(errors, built.Grades.Count == 1 && built.Grades[0].Students.Count == 1 && built.Grades[0].Selects.Count == 0, "正式清單只保留學生物件，不建立 SELECT 字串");
            Expect(errors, legacy.Grades[0].Selects.Count == 1, "舊版比較基準仍產生一列 SELECT");
            string legacySql = legacy.Grades[0].Selects[0];
            Expect(errors, legacySql.IndexOf("false AS participate_tag2") >= 0 && legacySql.IndexOf("true AS participate_year") >= 0 && legacySql.IndexOf("''::TEXT AS rank_tag2") >= 0, "舊版空白類別是空字串，不參與是 false");
            string json = SemesterRankStudentJson.TryCompose(built.Grades);
            Expect(errors, json.IndexOf("\"rank_tag2\":\"\"") >= 0 && json.IndexOf("\"participate_tag2\":false") >= 0 && json.IndexOf("\"participate_year\":true") >= 0 && json.IndexOf(":null") < 0, "JSON 空白是空字串，參與是 boolean，不是 null");
            List<SemesterRankStudentInput> read = SemesterRankStudentJson.Read(json);
            SemesterRankStudentInput input = built.Grades[0].Students[0];
            Expect(errors, read.Count == 1 && Same(read[0], input) && input.StudentId == 1001 && input.StudentName == student.StudentName && input.Tag2 == "" && input.ParticipateTag2 == false && input.ParticipateClass, "讀回的學生欄位與確認清單相同");
        }

        private static void CheckDuplicateAndIdentity(List<string> errors)
        {
            MotherGroupConfirmList duplicated = ListOf(
                Student("1001", "甲", "1年級", true, "普通科", true, "甲班", true, "", false, "", false),
                Student("1001", "甲", "1年級", true, "普通科", true, "甲班", true, "", false, "", false));
            Expect(errors, Throws(delegate { SemesterRankSql.Build(duplicated); }, "重複"), "同一學生重複時應阻擋，不取第一筆");
            Expect(errors, Throws(delegate { SemesterRankSql.Build(ListOf(Student("0101", "甲", "1年級", true, "普通科", false, "", false, "", false, "", false))); }, "正整數"), "前導零的學生識別應阻擋，避免轉成另一個數字");
            Expect(errors, Throws(delegate { SemesterRankSql.Build(ListOf(Student("A1", "甲", "1年級", true, "普通科", false, "", false, "", false, "", false))); }, "正整數"), "非整數學生識別應阻擋");
            SemesterRankStudentSqlResult legacy = SemesterRankSql.BuildLegacySelects(duplicated);
            Expect(errors, legacy.Grades.Count == 1 && legacy.Grades[0].Selects.Count == 2, "舊版比較基準保留原本兩列，不在測試路徑改成靜默合併");
        }

        private static void CheckPublish(List<string> errors)
        {
            string formula = Formula(errors);
            if (formula.Length == 0)
                return;
            Expect(errors, File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8).IndexOf("jsonb_to_recordset") < 0, "成績公式檔不應改成 JSONB 學生清單");
            MotherGroupConfirmList list = ListOf(
                Student("1001", "測試'學生\\\"A\n甲", "1年級", true, "普通科", true, "一年甲班", true, "一般生", true, "", false),
                Student("1002", "乙", "2年級", false, "", false, "二年乙班", true, "", false, "社團", true));
            SemesterRankStudentSqlResult built = SemesterRankSql.Build(list);
            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(built.Grades, "114", "2", "<Setting />", formula, false);
            string applied = SemesterRankSql.ApplyParticipation(publish.Sql);
            string studentList = Between(applied, "WITH student_list AS", ", insert_batch_data AS");
            string calc = Between(applied, ", calc_condition AS", ", subject_score AS");
            string scores = applied.Substring(applied.IndexOf(", subject_score AS"));
            Expect(errors, studentList.IndexOf("jsonb_to_recordset") >= 0 && studentList.IndexOf("'::jsonb") >= 0 && studentList.IndexOf("UNION ALL") < 0, "學生清單應是一份 jsonb_to_recordset，沒有學生 UNION ALL");
            Expect(errors, studentList.IndexOf("participate_year") >= 0 && studentList.IndexOf("participate_tag2") >= 0, "學生清單應帶出五種參與欄位");
            Expect(errors, calc.IndexOf("UNION ALL") >= 0 && calc.IndexOf("'1'::TEXT AS rank_grade_year") >= 0 && calc.IndexOf("'2'::TEXT AS rank_grade_year") >= 0, "年級條件仍是一列一年級，年級之間維持 UNION ALL");
            Expect(errors, applied.IndexOf("PARTITION BY rank_grade_year, subject, participate_year") >= 0, "ApplyParticipation 仍把年排名參與旗標加進 PARTITION BY");
            Expect(errors, scores.IndexOf("UNION ALL") >= 0, "成績 CTE 仍保留分數的 UNION ALL");
            Expect(errors, scores.IndexOf("subject_origin_score IS NOT NULL") >= 0, "原始成績的空值條件仍在");
            Expect(errors, scores.IndexOf("GREATEST") >= 0, "擇優仍使用 GREATEST");
            Expect(errors, applied.IndexOf("MATERIALIZED") < 0 && applied.IndexOf("jsonb_path") < 0 && applied.IndexOf(';') < 0, "公布語句不使用新版才有的 JSON 語法，且維持單一敘述");
            Expect(errors, applied.IndexOf("測試''學生\\\\\\\"A\\n甲") >= 0, "單引號、反斜線、換行與中文應先做 JSON 跳脫，再做 SQL 跳脫");

            SemesterRankGradeStudents hand = new SemesterRankGradeStudents();
            hand.GradeYear = 1;
            hand.Selects = new List<string>();
            hand.Selects.Add("SELECT '7'::BIGINT AS student_id");
            Expect(errors, Throws(delegate { SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { hand }, "114", "2", "", formula, false); }, "JSONB 學生清單"), "正式公布入口不得改走舊的學生 UNION ALL");
            SemesterRankPublishStatement legacyPublish = SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { hand }, "114", "2", "", formula, false, true);
            string legacyList = Between(legacyPublish.Sql, "WITH student_list AS", ", insert_batch_data AS");
            Expect(errors, legacyList.IndexOf("jsonb_to_recordset") < 0 && legacyList.IndexOf("'7'::BIGINT AS student_id") >= 0, "既有手寫 SELECT 測試仍走舊學生清單");

            MotherGroupConfirmList semicolon = ListOf(Student("1001", "甲;乙", "1年級", true, "普通科", true, "甲班", true, "", false, "", false));
            SemesterRankStudentSqlResult semicolonStudents = SemesterRankSql.Build(semicolon);
            Expect(errors, Throws(delegate { SemesterRankPublishSql.Build(semicolonStudents.Grades, "114", "2", "", formula, false); }, "多個敘述"), "姓名中的分號仍會使單一敘述檢查失敗");
        }

        private static void CheckSizes(List<string> errors)
        {
            int[] sizes = new int[] { 1, 100, 500, 1000, 2000, 5000 };
            StringBuilder report = new StringBuilder();
            foreach (int size in sizes)
            {
                MotherGroupConfirmList list = new MotherGroupConfirmList();
                for (int i = 0; i < size; i++)
                {
                    int grade = (i % 3) + 1;
                    list.Rows.Add(Row(Student((100000 + i).ToString(), "學生" + i.ToString(), grade.ToString() + "年級", true, "普通科", true, grade.ToString() + "年甲班", i % 5 != 0, i % 7 == 0 ? "" : "一般生", i % 7 != 0, "", false)));
                }
                SemesterRankStudentSqlResult built = SemesterRankSql.Build(list);
                int listed = 0;
                foreach (SemesterRankGradeStudents grade in built.Grades)
                    listed += grade.Students.Count;
                Expect(errors, listed == size && built.Grades.Count == (size == 1 ? 1 : 3), size.ToString() + " 人應全數進入清單且不重複");
                long before = GC.GetTotalMemory(false);
                Stopwatch watch = Stopwatch.StartNew();
                string json = SemesterRankStudentJson.TryCompose(built.Grades);
                watch.Stop();
                long after = GC.GetTotalMemory(false);
                Expect(errors, json != null && json.StartsWith("[") && json.EndsWith("]") && json.IndexOf("\"student_id\":") >= 0, size.ToString() + " 人的 JSON 應是陣列");
                if (report.Length > 0)
                    report.Append("; ");
                report.Append(size.ToString()).Append("=").Append(json.Length.ToString()).Append("chars/").Append(watch.ElapsedMilliseconds.ToString()).Append("ms/memDelta ").Append((after - before).ToString());
            }
            Console.WriteLine("json-compose " + report.ToString());
        }

        private static StudentMotherGroup Student(string id, string name, string year, bool yearOn, string dept, bool deptOn, string className, bool classOn, string tag1, bool tag1On, string tag2, bool tag2On)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentName = name;
            student.GetWorking(RankKinds.Year).GroupName = year;
            student.GetWorking(RankKinds.Year).Participate = yearOn;
            student.GetWorking(RankKinds.Department).GroupName = dept;
            student.GetWorking(RankKinds.Department).Participate = deptOn;
            student.GetWorking(RankKinds.Class).GroupName = className;
            student.GetWorking(RankKinds.Class).Participate = classOn;
            student.GetWorking(RankKinds.Tag1).GroupName = tag1;
            student.GetWorking(RankKinds.Tag1).Participate = tag1On;
            student.GetWorking(RankKinds.Tag2).GroupName = tag2;
            student.GetWorking(RankKinds.Tag2).Participate = tag2On;
            return student;
        }

        private static MotherGroupConfirmList ListOf(params StudentMotherGroup[] students)
        {
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            foreach (StudentMotherGroup student in students)
                list.Rows.Add(Row(student));
            return list;
        }

        private static ConfirmStudentRow Row(StudentMotherGroup student)
        {
            ConfirmStudentRow row = new ConfirmStudentRow();
            row.Group = student;
            return row;
        }

        private static bool Same(SemesterRankStudentInput left, SemesterRankStudentInput right)
        {
            return left.StudentId == right.StudentId
                && left.StudentName == right.StudentName
                && left.GradeYear == right.GradeYear
                && left.DeptName == right.DeptName
                && left.ClassName == right.ClassName
                && left.Tag1 == right.Tag1
                && left.Tag2 == right.Tag2
                && left.ParticipateYear == right.ParticipateYear
                && left.ParticipateDept == right.ParticipateDept
                && left.ParticipateClass == right.ParticipateClass
                && left.ParticipateTag1 == right.ParticipateTag1
                && left.ParticipateTag2 == right.ParticipateTag2;
        }

        private static bool Throws(Action action, string messagePart)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message.IndexOf(messagePart) >= 0;
            }
            return false;
        }

        private static string Formula(List<string> errors)
        {
            string formula = SemesterRankFormulaSql.BuildOriginOnly();
            if (formula.IndexOf(", subject_score AS") < 0 || formula.IndexOf("SELECT count(*) FROM score_list") < 0)
            {
                errors.Add("找不到成績公式");
                return "";
            }
            return formula;
        }

        private static string Between(string text, string start, string end)
        {
            int at = text.IndexOf(start);
            int to = at < 0 ? -1 : text.IndexOf(end, at);
            if (at < 0 || to < 0)
                return "";
            return text.Substring(at, to - at);
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
