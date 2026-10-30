using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase3Task34Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                string source = ReadForm(errors);
                string formula = LoadFormula(source, errors);
                if (formula.Length > 0)
                {
                    errors.AddRange(CheckBaseline(formula));
                    errors.AddRange(CheckYearOffClassOn(formula));
                    errors.AddRange(CheckNone(formula));
                    errors.AddRange(CheckBlankNames(formula));
                    errors.AddRange(CheckMissingScores(formula));
                    errors.AddRange(CheckRejectedInputs(formula));
                }
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                int gate = source.IndexOf("CanWriteRankTables");
                int select = source.IndexOf("queryHelper.Select(insertRankSql)");
                Expect(errors, gate >= 0 && select > gate, "寫入閘門仍位於排名 SQL 執行之前");
                string gateSource = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                Expect(errors, gateSource.IndexOf("return true;") >= 0 && gateSource.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase3-task34-check: ok");
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

        private static List<string> CheckBaseline(string formula)
        {
            List<string> errors = new List<string>();
            StudentMotherGroup student = Named("701", "全參與", "1年級", "普通科", "忠", "甲", "社團");
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, student, false);
            Expect(errors, AllParticipate(student, true), "未改初始參與且名稱齊全時，五種參與應維持 true");
            SemesterRankComparePlan plan = Plan(formula, "unmodified-current", new StudentMotherGroup[] { student });
            errors.AddRange(CheckPlan(plan, "114", "1"));
            string studentSql = StudentSql(plan, "701");
            Expect(errors, CountOf(studentSql, "true AS participate_") == 5, "基準案例的學生列應為五種都參與");
            return errors;
        }

        private static List<string> CheckYearOffClassOn(string formula)
        {
            List<string> errors = new List<string>();
            StudentMotherGroup student = Named("702", "年不班參", "1年級", "普通科", "忠", "甲", "社團");
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, student, false);
            student.GetWorking(RankKinds.Year).Participate = false;
            Expect(errors, !student.GetWorking(RankKinds.Year).Participate && student.GetWorking(RankKinds.Class).Participate, "年排名不參與時班排名應仍參與");
            Expect(errors, student.GetWorking(RankKinds.Department).Participate && student.GetWorking(RankKinds.Tag1).Participate && student.GetWorking(RankKinds.Tag2).Participate, "只關閉年排名時，科別與兩個類別應維持參與");
            SemesterRankComparePlan plan = Plan(formula, "year-off-class-on", new StudentMotherGroup[] { student });
            errors.AddRange(CheckPlan(plan, "114", "1"));
            string studentSql = StudentSql(plan, "702");
            Expect(errors, studentSql.IndexOf("false AS participate_year") >= 0 && studentSql.IndexOf("true AS participate_class") >= 0, "比對語句應同時帶年排名 false 與班排名 true");
            string revised = Half(plan.Sql, "/* begin-revised */", "/* end-revised */");
            string yearBranch = Branch(revised, "'年排名'::TEXT AS rank_type");
            string classBranch = Branch(revised, "'班排名'::TEXT AS rank_type");
            Expect(errors, AfterFrom(yearBranch).IndexOf("participate_year") >= 0 && AfterFrom(yearBranch).IndexOf("participate_class") < 0, "年排名差異條件只使用年排名旗標");
            Expect(errors, AfterFrom(classBranch).IndexOf("participate_class") >= 0 && AfterFrom(classBranch).IndexOf("participate_year") < 0, "班排名差異條件只使用班排名旗標");
            return errors;
        }

        private static List<string> CheckNone(string formula)
        {
            List<string> errors = new List<string>();
            StudentMotherGroup student = Named("703", "全不參與", "1年級", "普通科", "忠", "甲", "社團");
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, student, false);
            foreach (string rankType in RankKinds.All)
                student.GetWorking(rankType).Participate = false;
            MotherGroupConfirmList list = ListOf(student);
            SemesterRankSql.BuildLegacySelects(list);
            Expect(errors, list.Rows.Count == 1 && list.Rows[0].Group.StudentId == "703", "五種都不參與仍應留在確認清單");
            SemesterRankComparePlan plan = Plan(formula, "participate-none", new StudentMotherGroup[] { student });
            errors.AddRange(CheckPlan(plan, "114", "1"));
            string studentSql = StudentSql(plan, "703");
            Expect(errors, CountOf(studentSql, "false AS participate_") == 5, "五種都不參與時，學生列的五個旗標都應為 false");
            Expect(errors, Half(plan.Sql, "/* begin-legacy */", "/* end-legacy */").IndexOf("'703'::BIGINT") >= 0, "舊公式側仍應包含這名學生");
            Expect(errors, Half(plan.Sql, "/* begin-revised */", "/* end-revised */").IndexOf("'703'::BIGINT") >= 0, "新公式側仍應包含這名學生");
            return errors;
        }

        private static List<string> CheckBlankNames(string formula)
        {
            List<string> errors = new List<string>();
            StudentMotherGroup student = Student("704", "空白名稱", "1年級", true, "", true, "忠", true, "", true, "", true);
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, student, false);
            Expect(errors, student.GetWorking(RankKinds.Department).Participate && student.GetWorking(RankKinds.Department).GroupName == "", "目前學生的空白科別應維持參與");
            Expect(errors, !student.GetWorking(RankKinds.Tag1).Participate && student.GetWorking(RankKinds.Tag1).GroupName == "", "空白類別一應不參與且名稱維持空白");
            Expect(errors, !student.GetWorking(RankKinds.Tag2).Participate && student.GetWorking(RankKinds.Tag2).GroupName == "", "空白類別二應不參與且名稱維持空白");
            Expect(errors, student.GetWorking(RankKinds.Year).Participate && student.GetWorking(RankKinds.Class).Participate, "空白類別與空白科別不應關閉年排名或班排名");
            SemesterRankComparePlan plan = Plan(formula, "blank-category-and-blank-dept", new StudentMotherGroup[] { student });
            errors.AddRange(CheckPlan(plan, "114", "1"));
            string studentSql = StudentSql(plan, "704");
            Expect(errors, studentSql.IndexOf("''::TEXT AS rank_dept_name") >= 0 && studentSql.IndexOf("true AS participate_dept") >= 0, "空白科別應以空名稱與參與 true 進入兩側");
            Expect(errors, studentSql.IndexOf("''::TEXT AS rank_tag1") >= 0 && studentSql.IndexOf("false AS participate_tag1") >= 0, "空白類別一應以空名稱與不參與進入兩側");
            foreach (string marker in new string[] { "/* begin-legacy */", "/* begin-revised */" })
            {
                string half = Half(plan.Sql, marker, marker.Replace("begin", "end"));
                Expect(errors, CountOf(half, "rank_tag1 <> ''") == 4 && CountOf(half, "rank_tag2 <> ''") == 4, "新舊兩側都應保留四段空白類別排除");
                Expect(errors, half.IndexOf("rank_dept_name <> ''") < 0 && half.IndexOf("rank_dept_name IS NOT NULL") < 0, "新舊兩側都不應排除空白科別");
            }
            return errors;
        }

        private static List<string> CheckMissingScores(string formula)
        {
            List<string> errors = new List<string>();
            StudentMotherGroup noSubject = Named("705", "缺科目", "1年級", "普通科", "忠", "甲", "社團");
            StudentMotherGroup noEntry = Named("706", "缺分項", "1年級", "普通科", "忠", "甲", "社團");
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, noSubject, false);
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, noEntry, false);
            MotherGroupConfirmList list = ListOf(noSubject, noEntry);
            SemesterRankSql.BuildLegacySelects(list);
            Expect(errors, list.Rows.Count == 2, "缺少科目或分項成績不應從確認清單移除學生");
            SemesterRankComparePlan plan = Plan(formula, "missing-subject-and-entry", new StudentMotherGroup[] { noSubject, noEntry });
            errors.AddRange(CheckPlan(plan, "114", "1"));
            Expect(errors, plan.Sql.IndexOf("'705'::BIGINT") >= 0 && plan.Sql.IndexOf("'706'::BIGINT") >= 0, "缺成績的兩名學生都應留在比對語句");
            foreach (string marker in new string[] { "/* begin-legacy */", "/* begin-revised */" })
            {
                string half = Half(plan.Sql, marker, marker.Replace("begin", "end"));
                string origin = Between(half, ", subj_origin_rank AS", ", subj_greatest_rank AS");
                string entry = Between(half, ", entry_rank_list AS", ", entry_rank_expand AS");
                Expect(errors, origin.IndexOf("subject IS NOT NULL") >= 0 && origin.IndexOf("subject_origin_score IS NOT NULL") > origin.IndexOf("FROM"), "科目分數過濾應在排名窗口之前");
                Expect(errors, entry.IndexOf("entry IS NOT NULL") >= 0 && entry.IndexOf("entry_score IS NOT NULL") > entry.IndexOf("FROM"), "分項分數過濾應在排名窗口之前");
            }
            string legacy = Half(plan.Sql, "/* begin-legacy */", "/* end-legacy */");
            string revised = Half(plan.Sql, "/* begin-revised */", "/* end-revised */");
            Expect(errors, CountOf(legacy, "subject_origin_score IS NOT NULL") == CountOf(revised, "subject_origin_score IS NOT NULL"), "新舊兩側的科目分數條件數量應相同");
            Expect(errors, CountOf(legacy, "entry_score IS NOT NULL") == CountOf(revised, "entry_score IS NOT NULL"), "新舊兩側的分項分數條件數量應相同");
            return errors;
        }

        private static List<string> CheckRejectedInputs(string formula)
        {
            List<string> errors = new List<string>();
            StudentMotherGroup student = Named("701", "全參與", "1年級", "普通科", "忠", "甲", "社團");
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, student, false);
            string composed = Composed(formula, new StudentMotherGroup[] { student });
            string applied = SemesterRankSql.ApplyParticipation(composed);
            bool rejected = false;
            try
            {
                SemesterRankCompatibility.Build(applied, "already-applied");
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "已經套用參與旗標的語句不應再當成舊公式");
            Expect(errors, composed.IndexOf("INSERT INTO") >= 0 && composed.IndexOf("UPDATE") >= 0, "原排名語句仍含寫入，供比對工具切除");
            return errors;
        }

        private static List<string> CheckPlan(SemesterRankComparePlan plan, string schoolYear, string semester)
        {
            List<string> errors = new List<string>();
            Expect(errors, plan != null && plan.DatabaseExecuted == false, "比對計畫不應標記為已在資料庫執行");
            Expect(errors, plan.Sql.IndexOf("database_executed: false") >= 0 && plan.Sql.IndexOf("false AS database_executed") >= 0, "比對語句應自帶尚未執行的標記");
            Expect(errors, !ContainsWrite(plan.Sql), "比對語句不應含寫入、停用或建立資料庫物件的關鍵字");
            string compact = plan.Sql.Replace(" ", "").Replace("\n", "").Replace("\t", "");
            Expect(errors, compact.IndexOf("is_alive=NULL") < 0, "比對語句不應停用既有排名");
            string legacy = Half(plan.Sql, "/* begin-legacy */", "/* end-legacy */");
            string revised = Half(plan.Sql, "/* begin-revised */", "/* end-revised */");
            Expect(errors, legacy.Length > 0 && revised.Length > 0, "比對語句應同時含舊公式與新公式");
            Expect(errors, legacy.IndexOf(", participate_") < 0, "舊公式的窗口不應先被加上參與旗標");
            Expect(errors, revised.IndexOf("PARTITION BY rank_grade_year, subject, participate_year") >= 0, "新公式的年排名窗口應含參與旗標");
            Expect(errors, SameFragment(legacy, revised, "GREATEST", "AS subject_greatest_score"), "兩側的 GREATEST 擇優公式應相同");
            Expect(errors, CountOf(legacy, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") == 1 && CountOf(revised, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") == 1, "兩側都應保留一次科目展開");
            Expect(errors, CountOf(legacy, "unnest(xpath('/root/SemesterEntryScore/Entry'") == 1 && CountOf(revised, "unnest(xpath('/root/SemesterEntryScore/Entry'") == 1, "兩側都應保留一次分項展開");
            Expect(errors, !HasDistinctBefore(legacy, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'") && !HasDistinctBefore(revised, "unnest(xpath('/root/SemesterSubjectScoreInfo/Subject'"), "科目展開不應被加上 DISTINCT");
            Expect(errors, !HasDistinctBefore(legacy, "unnest(xpath('/root/SemesterEntryScore/Entry'") && !HasDistinctBefore(revised, "unnest(xpath('/root/SemesterEntryScore/Entry'"), "分項展開不應被加上 DISTINCT");
            Expect(errors, CountOf(plan.Sql, "'" + schoolYear + "'::TEXT AS rank_school_year") == 2, "新舊兩側應使用同一個學年度");
            Expect(errors, CountOf(plan.Sql, "'" + semester + "'::TEXT AS rank_semester") == 2, "新舊兩側應使用同一個學期");
            Expect(errors, plan.Sql.IndexOf("0 AS batch_id") < 0 && CountOf(plan.Sql, "NULL::INT AS batch_id") == 2, "唯讀比對不應帶批次編號 0");
            foreach (string rankType in RankKinds.All)
            {
                string token = "'" + rankType + "'::TEXT AS rank_type";
                Expect(errors, CountOf(legacy, token) == 4 && CountOf(revised, token) == 4, "每一側的 " + rankType + " 都應涵蓋四種成績");
            }
            string[] itemTypes = new string[] { "學期/科目成績(原始)", "學期/科目成績", "學期/分項成績(原始)", "學期/分項成績" };
            foreach (string itemType in itemTypes)
            {
                string token = "'" + itemType + "'::TEXT AS item_type";
                Expect(errors, CountOf(legacy, token) == 5 && CountOf(revised, token) == 5, "每一側的 " + itemType + " 都應涵蓋五種排名");
            }
            foreach (string column in SemesterRankCompatibility.JoinColumns)
                Expect(errors, plan.Sql.IndexOf("legacy_row." + column + " IS NOT DISTINCT FROM revised_row." + column) >= 0, "比對鍵應包含 " + column);
            foreach (string column in SemesterRankCompatibility.ValueColumns)
                Expect(errors, plan.Sql.IndexOf("legacy_row." + column + " IS DISTINCT FROM revised_row." + column) >= 0, "比對值應包含 " + column);
            Expect(errors, plan.Sql.IndexOf("ROW_NUMBER() OVER") >= 0 && CountOf(plan.Sql, "AS duplicate_index") >= 2, "重複科目應以相同排序編號後再比對，不先去除重複");
            Expect(errors, plan.Sql.IndexOf("XMLTABLE") < 0 && plan.Sql.IndexOf("ON CONFLICT") < 0, "比對語句不應使用 PostgreSQL 9.6 沒有的語法");
            return errors;
        }

        private static SemesterRankComparePlan Plan(string formula, string scenario, StudentMotherGroup[] students)
        {
            return SemesterRankCompatibility.Build(Composed(formula, students), scenario);
        }

        private static string Composed(string formula, StudentMotherGroup[] students)
        {
            MotherGroupConfirmList list = ListOf(students);
            SemesterRankStudentSqlResult built = SemesterRankSql.BuildLegacySelects(list);
            return SemesterRankCompatibility.Compose(built.Grades[0].Selects, 1, "114", "1", formula);
        }

        private static string LoadFormula(string source, List<string> errors)
        {
            int start = source.IndexOf(", subject_score AS");
            int end = source.IndexOf("SELECT count(*) FROM score_list");
            if (start < 0 || end <= start)
            {
                errors.Add("應能從表單取出年級排名 SQL");
                return "";
            }
            string formula = source.Substring(start, end - start);
            int broken = formula.IndexOf("::DECIMAL\"");
            int resume = broken < 0 ? -1 : formula.IndexOf("+ @\"", broken);
            if (broken < 0 || resume < 0)
            {
                errors.Add("應能辨識表單裡 GREATEST 的字串接縫");
                return "";
            }
            formula = formula.Substring(0, broken + "::DECIMAL".Length) + formula.Substring(resume + "+ @\"".Length);
            string[] attributes = new string[] { "學年調整成績", "擇優採計成績", "補考成績", "重修成績" };
            StringBuilder bestOf = new StringBuilder();
            foreach (string attribute in attributes)
            {
                string fragment = AssignedFragment(source, attribute);
                if (fragment.Length == 0)
                {
                    errors.Add("表單缺少擇優片段：" + attribute);
                    return "";
                }
                bestOf.Append("\n\t\t\t").Append(fragment);
            }
            int greatest = formula.IndexOf(") AS subject_greatest_score");
            if (greatest < 0)
            {
                errors.Add("GREATEST 結果欄位不存在");
                return "";
            }
            formula = formula.Insert(greatest, bestOf.ToString());
            Expect(errors, formula.IndexOf(", update_data AS") > greatest && formula.IndexOf("INSERT INTO") > greatest, "原寫入語句應仍留在公式尾端，供比對時切除");
            return formula;
        }

        private static string AssignedFragment(string source, string attribute)
        {
            string token = "xpath('/root/Subject/@" + attribute + "'";
            int at = source.IndexOf(token);
            if (at < 0)
                return "";
            int start = source.LastIndexOf("\"", at);
            int end = source.IndexOf("\"", at);
            if (start < 0 || end <= start)
                return "";
            return source.Substring(start + 1, end - start - 1);
        }

        private static string ReadForm(List<string> errors)
        {
            string path = "CalculateSemesterAssessmentRank.cs";
            if (!File.Exists(path))
            {
                errors.Add("找不到排名 SQL 來源檔：" + path);
                return "";
            }
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static bool AllParticipate(StudentMotherGroup student, bool expected)
        {
            foreach (string rankType in RankKinds.All)
            {
                if (student.GetWorking(rankType).Participate != expected)
                    return false;
            }
            return true;
        }

        private static bool HasDistinctBefore(string sql, string marker)
        {
            int at = sql.IndexOf(marker);
            if (at < 0)
                return true;
            int from = Math.Max(0, at - 80);
            return sql.Substring(from, at - from).IndexOf("DISTINCT") >= 0;
        }

        private static bool SameFragment(string left, string right, string start, string end)
        {
            string leftPart = Between(left, start, end);
            string rightPart = Between(right, start, end);
            return leftPart.Length > 0 && leftPart == rightPart;
        }

        private static bool ContainsWrite(string sql)
        {
            string[] forbidden = new string[]
            {
                "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "INTO", "RETURNING", "CALL", "COPY", "ALTER", "DROP", "CREATE"
            };
            foreach (string keyword in forbidden)
            {
                if (ContainsKeyword(sql, keyword))
                    return true;
            }
            return false;
        }

        private static bool ContainsKeyword(string sql, string keyword)
        {
            int at = 0;
            while (at <= sql.Length - keyword.Length)
            {
                at = sql.IndexOf(keyword, at, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return false;
                bool left = at == 0 || !IsIdent(sql[at - 1]);
                int end = at + keyword.Length;
                bool right = end >= sql.Length || !IsIdent(sql[end]);
                if (left && right)
                    return true;
                at = end;
            }
            return false;
        }

        private static bool IsIdent(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private static string StudentSql(SemesterRankComparePlan plan, string studentId)
        {
            string token = "'" + studentId + "'::BIGINT";
            int at = plan.Sql.IndexOf(token);
            if (at < 0)
                return "";
            int end = plan.Sql.IndexOf("UNION ALL", at);
            int legacyEnd = plan.Sql.IndexOf("/* end-legacy */", at);
            if (end < 0 || (legacyEnd >= 0 && legacyEnd < end))
                end = legacyEnd;
            if (end < 0)
                return plan.Sql.Substring(at);
            return plan.Sql.Substring(at, end - at);
        }

        private static string Branch(string sql, string rankToken)
        {
            int from = sql.IndexOf(", score_list AS");
            string body = from < 0 ? sql : sql.Substring(from);
            string[] parts = body.Split(new string[] { "UNION ALL" }, StringSplitOptions.None);
            foreach (string part in parts)
            {
                if (part.IndexOf(rankToken) >= 0)
                    return part;
            }
            return "";
        }

        private static string AfterFrom(string branch)
        {
            int fromAt = branch.LastIndexOf("FROM");
            if (fromAt < 0)
                return "";
            return branch.Substring(fromAt);
        }

        private static string Half(string sql, string start, string end)
        {
            return Between(sql, start, end);
        }

        private static string Between(string text, string start, string end)
        {
            int from = text.IndexOf(start);
            if (from < 0)
                return "";
            from += start.Length;
            int to = text.IndexOf(end, from);
            if (to < from)
                return "";
            return text.Substring(from, to - from);
        }

        private static int CountOf(string text, string value)
        {
            if (text == null || value.Length == 0)
                return 0;
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

        private static MotherGroupConfirmList ListOf(params StudentMotherGroup[] students)
        {
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup student in students)
                rows.Add(new ConfirmStudentRow { Group = student });
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(rows);
            return list;
        }

        private static StudentMotherGroup Named(string id, string name, string year, string dept, string className, string tag1, string tag2)
        {
            return Student(id, name, year, true, dept, true, className, true, tag1, true, tag2, true);
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
