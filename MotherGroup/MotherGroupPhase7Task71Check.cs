using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase7Task71Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                string form = Read("CalculateSemesterAssessmentRank.cs", errors);
                string viewer = Read("SemesterAssessmentRankSelect.cs", errors);
                string formula = LoadFormula(form, errors);
                errors.AddRange(CheckCurrentCalculation(formula));
                errors.AddRange(CheckViewer(viewer));
                errors.AddRange(CheckGate(form));
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase7-task71-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("school-ui-opened: false");
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

        private static List<string> CheckCurrentCalculation(string formula)
        {
            List<string> errors = new List<string>();
            const string schoolYear = "113";
            const string semester = "1";
            List<StudentMotherGroup> groups = CurrentStudentMotherGroupBuilder.Build(
                new List<CurrentStudentRow>
                {
                    Row("101", "S101", "王", true, "c1", 1, "忠"),
                    Row("102", "S102", "李", true, "c1", 1, "忠"),
                    Row("103", "S103", "張", true, "c2", 1, "孝"),
                    Row("104", "S104", "趙", true, "c3", 2, "仁"),
                    Row("105", "S105", "周", false, "c1", 1, "忠")
                },
                new List<TagDefinition>
                {
                    new TagDefinition { Id = "t1", Prefix = "類別一", Name = "甲組" },
                    new TagDefinition { Id = "t2", Prefix = "類別二", Name = "乙組" },
                    new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" }
                },
                new List<StudentTagLink>
                {
                    Link("101", "t1", "甲組"),
                    Link("101", "t2", "乙組"),
                    Link("102", "t1", "甲組"),
                    Link("102", "t2", "乙組"),
                    Link("102", "ex", "休學"),
                    Link("103", "t1", "甲組")
                },
                new List<int> { 1 },
                "不排名",
                "類別一",
                "類別二",
                new Dictionary<string, string> { { "101", "普通科" }, { "102", "普通科" }, { "103", "" } });
            foreach (StudentMotherGroup group in groups)
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, group, group.MatchesExcludedCategory);

            StudentMotherGroup included = Find(groups, "101");
            StudentMotherGroup excluded = Find(groups, "102");
            StudentMotherGroup blankDept = Find(groups, "103");
            Expect(errors, included != null && excluded != null && blankDept != null && Find(groups, "104") == null && Find(groups, "105") == null, "目前學生來源只留下勾選年級中的一般有班級學生");
            Expect(errors, included != null && All(included, true) && included.GetWorking(RankKinds.Year).GroupName == "1年級" && included.GetWorking(RankKinds.Class).GroupName == "忠" && included.GetWorking(RankKinds.Department).GroupName == "普通科", "預設參與應保留目前學生的五種母群且全部參與");
            Expect(errors, excluded != null && !Any(excluded) && excluded.GetWorking(RankKinds.Year).GroupName == "1年級" && excluded.GetWorking(RankKinds.Tag1).GroupName == "甲組", "符合不排名類別者留在清單，名稱不變且五種初始不參與");
            Expect(errors, blankDept != null && blankDept.GetWorking(RankKinds.Department).GroupName == "" && blankDept.GetWorking(RankKinds.Department).Participate && !blankDept.GetWorking(RankKinds.Tag2).Participate, "目前學生的空白科別仍參與，空白類別二不參與");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup group in groups)
                rows.Add(new ConfirmStudentRow { Group = group });
            list.Load(rows);
            Expect(errors, HistoryCalculationScope.UnparsedYearMessage(list).Length == 0, "目前學生的年排名名稱應能解析，不需改用其他年級");

            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            setting.SetAttribute("學年度", schoolYear);
            setting.SetAttribute("學期", semester);
            setting.SetAttribute("考試名稱", "學期成績");
            setting.SetAttribute("不排名學生類別", "不排名");
            setting.SetAttribute("類別一", "類別一");
            setting.SetAttribute("類別二", "類別二");
            document.AppendChild(setting);
            XmlElement grade = document.CreateElement("年級");
            grade.InnerText = "1";
            setting.AppendChild(grade);
            XmlElement best = document.CreateElement("擇優採計成績");
            best.InnerText = "原始成績";
            setting.AppendChild(best);
            MotherGroupSnapshot.Append(setting, list, MotherGroupSnapshot.KindFromSourceName(CalculationPeriod.CurrentSourceName), "");
            string xml = setting.OuterXml;

            List<StudentMotherGroup> restored;
            string kind;
            string historyBatchId;
            Expect(errors, MotherGroupSnapshot.TryRestore(xml, out restored, out kind, out historyBatchId), "新批次設定應能還原目前學生快照");
            Expect(errors, kind == "current" && historyBatchId == "" && restored.Count == 3, "目前學生來源的快照不帶歷史批次");
            Expect(errors, xml.IndexOf("學年度=\"113\"") >= 0 && xml.IndexOf("學期=\"1\"") >= 0 && xml.IndexOf("<年級>1</年級>") >= 0 && xml.IndexOf("<擇優採計成績>原始成績</擇優採計成績>") >= 0, "快照不應蓋掉學年度、學期、年級與擇優節點");
            StudentMotherGroup restoredIncluded = Find(restored, "101");
            StudentMotherGroup restoredExcluded = Find(restored, "102");
            Expect(errors, restoredIncluded != null && restoredIncluded.GetWorking(RankKinds.Year).Participate && restoredIncluded.GetWorking(RankKinds.Year).GroupName == "1年級", "快照應保存預設參與與目前母群名稱");
            Expect(errors, restoredExcluded != null && !Any(restoredExcluded) && restoredExcluded.GetWorking(RankKinds.Department).GroupName == "普通科", "快照應保留不排名學生與原本母群名稱");

            SemesterRankStudentSqlResult legacyStudents = SemesterRankSql.BuildLegacySelects(list);
            SemesterRankStudentSqlResult rankStudents = SemesterRankSql.Build(list);
            Expect(errors, rankStudents.Grades.Count == 1 && rankStudents.Grades[0].GradeYear == 1 && rankStudents.Grades[0].Students.Count == 3 && legacyStudents.Grades[0].Selects.Count == 3, "選定年級的三名學生都進入同一份排名清單");
            string includedSql = Clause(string.Join("\n", legacyStudents.Grades[0].Selects.ToArray()), "101");
            string excludedSql = Clause(string.Join("\n", legacyStudents.Grades[0].Selects.ToArray()), "102");
            string blankSql = Clause(string.Join("\n", legacyStudents.Grades[0].Selects.ToArray()), "103");
            Expect(errors, includedSql.IndexOf("true AS participate_year") >= 0 && includedSql.IndexOf("true AS participate_tag2") >= 0, "預設全參與學生的五種旗標應為參與");
            Expect(errors, excludedSql.IndexOf("false AS participate_year") >= 0 && excludedSql.IndexOf("false AS participate_dept") >= 0 && excludedSql.IndexOf("false AS participate_class") >= 0 && excludedSql.IndexOf("false AS participate_tag1") >= 0 && excludedSql.IndexOf("false AS participate_tag2") >= 0, "不排名學生不進入五種排名");
            Expect(errors, blankSql.IndexOf("''::TEXT AS rank_dept_name") >= 0 && blankSql.IndexOf("true AS participate_dept") >= 0 && blankSql.IndexOf("false AS participate_tag2") >= 0, "空白科別仍參與，空白類別不參與");
            Expect(errors, formula.IndexOf("subject_origin_score IS NOT NULL") >= 0 && formula.IndexOf("entry_score IS NOT NULL") >= 0, "沒有成績時仍沿用既有分數條件，不另產生明細");

            if (formula.Length == 0)
                return errors;
            string composed = SemesterRankCompatibility.Compose(legacyStudents.Grades[0].Selects, 1, schoolYear, semester, formula);
            SemesterRankComparePlan plan = SemesterRankCompatibility.Build(composed, "current-default-113-1");
            Expect(errors, plan.DatabaseExecuted == false && plan.Sql.IndexOf("database_executed: false") >= 0, "舊公式比對不應在資料庫執行");
            Expect(errors, !ContainsWrite(plan.Sql), "比對語句不應寫入排名資料");
            Expect(errors, Count(plan.Sql, "'113'::TEXT AS rank_school_year") == 2 && Count(plan.Sql, "'1'::TEXT AS rank_semester") == 2, "比對兩側都使用選定的學年度與學期");
            string legacy = Half(plan.Sql, "/* begin-legacy */", "/* end-legacy */");
            string revised = Half(plan.Sql, "/* begin-revised */", "/* end-revised */");
            Expect(errors, legacy.IndexOf("PARTITION BY rank_grade_year, subject, participate_year") < 0 && revised.IndexOf("PARTITION BY rank_grade_year, subject, participate_year") >= 0, "新側才在排名前套用參與旗標，舊側保留原公式");
            Expect(errors, SameFragment(legacy, revised, "GREATEST", "AS subject_greatest_score"), "兩側擇優公式應相同");
            Expect(errors, legacy.IndexOf("'101'::BIGINT AS student_id") >= 0 && revised.IndexOf("'101'::BIGINT AS student_id") >= 0 && legacy.IndexOf("'102'::BIGINT AS student_id") >= 0, "比對與快照使用同一份目前學生");

            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(rankStudents.Grades, schoolYear, semester, xml, formula, false);
            Expect(errors, publish.DatabaseExecuted == false && SemesterRankPublishSql.DatabaseExecuted == false, "公布語句不應標記為已執行");
            Expect(errors, publish.Sql.IndexOf(';') < 0 && publish.Sql.IndexOf("0 AS batch_id") < 0, "公布應是單一語句，且不使用批次識別 0");
            Expect(errors, publish.Sql.IndexOf("113") >= 0 && publish.Sql.IndexOf("MotherGroupSnapshot") >= 0 && publish.Sql.IndexOf("kind=\"current\"") >= 0, "公布語句應帶選定期間與目前學生快照");
            Expect(errors, publish.Sql.IndexOf("is_alive = NULL") >= 0 && publish.Sql.IndexOf("true AS is_alive") >= 0, "停用舊排名與啟用新排名應在同一句");
            Expect(errors, publish.Sql.IndexOf("jsonb_to_recordset") >= 0 && publish.Sql.IndexOf("\"student_id\":101") >= 0 && publish.Sql.IndexOf("\"student_id\":102") >= 0, "有效排名與快照使用同一份學生");
            return errors;
        }

        private static List<string> CheckViewer(string viewer)
        {
            List<string> errors = new List<string>();
            Expect(errors, Count(viewer, "is_alive = true") >= 2, "檢索畫面應只讀有效排名");
            Expect(errors, viewer.IndexOf("is_alive IS NULL") < 0 && viewer.IndexOf("is_alive = false") < 0 && viewer.IndexOf("is_alive=NULL") < 0, "未公布的排名不應出現在檢索畫面");
            return errors;
        }

        private static List<string> CheckGate(string form)
        {
            List<string> errors = new List<string>();
            int calculate = form.IndexOf("void btnCacluate_Click");
            int gate = form.IndexOf("CanWriteRankTables", calculate);
            int returned = form.IndexOf("return;", gate);
            int year = form.IndexOf("string schoolYear = lbSchoolYear.Text", calculate);
            int append = form.IndexOf("MotherGroupSnapshotJson.Compose", calculate);
            int publish = form.IndexOf("SemesterRankPublishSql.Build(", calculate);
            Expect(errors, calculate >= 0 && gate > calculate && returned > gate && returned < year && year < append && append < publish, "閘門返回仍在讀取學年度之前，允許寫入後同一條路徑繼續到快照與公布");
            Expect(errors, form.IndexOf("CopyWorking()", returned) > returned && form.IndexOf("沒有可計算的年級", returned) > returned && form.IndexOf("RankCalculationConfirm.Message", returned) > returned && form.IndexOf("queryHelper.Select(insertRankSql)", returned) > returned && form.IndexOf("計算排名失敗", returned) > returned && form.IndexOf("CalculationPeriod.CurrentSourceName", returned) > returned && form.IndexOf("CalculationPeriod.HistorySourceName", returned) > returned, "目前學生、歷史與 Excel 進入同一條計算流程，並保留工作副本、空年級防護、確認框、單次查詢與失敗訊息");
            string gateSource = Read("MotherGroup\\SemesterRankWriteGate.cs", errors);
            Expect(errors, gateSource.IndexOf("return true;") >= 0 && gateSource.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
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
            StringBuilder bestOf = new StringBuilder();
            string[] attributes = new string[] { "學年調整成績", "擇優採計成績", "補考成績", "重修成績" };
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
            return formula.Insert(greatest, bestOf.ToString()) + "SELECT count(*) FROM score_list";
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

        private static string Clause(string sql, string studentId)
        {
            string token = "'" + studentId + "'::BIGINT AS student_id";
            int at = sql.IndexOf(token);
            if (at < 0)
                return "";
            int end = sql.IndexOf("UNION ALL", at);
            if (end < 0)
                end = sql.Length;
            return sql.Substring(at, end - at);
        }

        private static string Half(string sql, string start, string end)
        {
            int from = sql.IndexOf(start);
            int to = sql.IndexOf(end, from < 0 ? 0 : from);
            if (from < 0 || to <= from)
                return "";
            return sql.Substring(from, to - from);
        }

        private static bool SameFragment(string left, string right, string start, string end)
        {
            int leftAt = left.IndexOf(start);
            int leftEnd = leftAt < 0 ? -1 : left.IndexOf(end, leftAt);
            int rightAt = right.IndexOf(start);
            int rightEnd = rightAt < 0 ? -1 : right.IndexOf(end, rightAt);
            if (leftAt < 0 || leftEnd <= leftAt || rightAt < 0 || rightEnd <= rightAt)
                return false;
            return left.Substring(leftAt, leftEnd - leftAt) == right.Substring(rightAt, rightEnd - rightAt);
        }

        private static bool ContainsWrite(string sql)
        {
            string[] forbidden = new string[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE" };
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

        private static bool All(StudentMotherGroup student, bool expected)
        {
            foreach (string rankType in RankKinds.All)
            {
                if (student.GetWorking(rankType).Participate != expected)
                    return false;
            }
            return true;
        }

        private static bool Any(StudentMotherGroup student)
        {
            foreach (string rankType in RankKinds.All)
            {
                if (student.GetWorking(rankType).Participate)
                    return true;
            }
            return false;
        }

        private static StudentMotherGroup Find(IList<StudentMotherGroup> groups, string studentId)
        {
            if (groups == null)
                return null;
            foreach (StudentMotherGroup group in groups)
            {
                if (group != null && group.StudentId == studentId)
                    return group;
            }
            return null;
        }

        private static CurrentStudentRow Row(string id, string number, string name, bool regular, string classId, int grade, string className)
        {
            return new CurrentStudentRow
            {
                StudentId = id,
                StudentNumber = number,
                StudentName = name,
                IsRegular = regular,
                RefClassId = classId,
                GradeYear = grade,
                ClassName = className
            };
        }

        private static StudentTagLink Link(string studentId, string tagId, string name)
        {
            return new StudentTagLink { StudentId = studentId, TagId = tagId, Name = name };
        }

        private static string Read(string path, List<string> errors)
        {
            if (!File.Exists(path))
            {
                errors.Add("找不到檔案：" + path);
                return "";
            }
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static int Count(string text, string token)
        {
            int count = 0;
            int index = 0;
            while (text != null && (index = text.IndexOf(token, index)) >= 0)
            {
                count++;
                index += token.Length;
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
