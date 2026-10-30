using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase7Task73Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                string form = Read("CalculateSemesterAssessmentRank.cs", errors);
                string formula = LoadFormula(form, errors);
                errors.AddRange(CheckExcelCalculation(formula));
                errors.AddRange(CheckForm(form));
                errors.AddRange(CheckOtherRankFiles());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase7-task73-check: ok");
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

        private static List<string> CheckExcelCalculation(string formula)
        {
            List<string> errors = new List<string>();
            const string scoreYear = "114";
            const string scoreSemester = "1";
            CalculationPeriod period = CalculationPeriod.CreateDefault(scoreYear, scoreSemester);
            period.RememberHistoryBatch("88", "113", "2");
            Expect(errors, period.SchoolYear == scoreYear && period.Semester == scoreSemester, "Excel 計算期間應使用圖1選定值");
            Expect(errors, period.HistorySchoolYear == "113" && period.HistorySemester == "2", "歷史期間應另存，不成為 Excel 的成績期間");

            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(
                RoundTrip(
                    Student("301", "王", "2年級", "Excel忠班", "Excel商科", "Excel甲組", "", true, true, true, true, false),
                    Student("302", "李", "2年級", "Excel孝班", "Excel商科", "Excel甲組", "Excel乙組", true, true, true, true, true),
                    Student("303", "張", "2年級", "Excel仁班", "", "", "Excel乙組", true, true, true, true, true)),
                Lookup(Row("301", "301", "王"), Row("302", "302", "李"), Row("303", "303", "張")));
            Expect(errors, validation.IsValid && validation.Students.Count == 3, "Excel 母群應通過驗證");
            List<StudentMotherGroup> groups = MotherGroupExcelApply.CreateGroups(
                validation,
                "不排名",
                new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } },
                new List<StudentTagLink> { new StudentTagLink { StudentId = "302", TagId = "ex", Name = "休學" } });
            StudentMotherGroup named = Find(groups, "301");
            StudentMotherGroup excluded = Find(groups, "302");
            StudentMotherGroup blank = Find(groups, "303");
            Expect(errors, named != null && named.GetWorking(RankKinds.Year).GroupName == "2年級" && named.GetWorking(RankKinds.Class).GroupName == "Excel忠班" && named.GetWorking(RankKinds.Department).GroupName == "Excel商科" && named.GetWorking(RankKinds.Tag1).GroupName == "Excel甲組", "Excel 的母群名稱應完整保留");
            Expect(errors, named.GetWorking(RankKinds.Year).Participate && named.GetWorking(RankKinds.Class).Participate && named.GetWorking(RankKinds.Department).Participate && named.GetWorking(RankKinds.Tag1).Participate && !named.GetWorking(RankKinds.Tag2).Participate && named.GetWorking(RankKinds.Tag2).GroupName == "", "Excel 參與應保留，空白類別二不參與");
            Expect(errors, excluded != null && !Any(excluded) && excluded.GetWorking(RankKinds.Class).GroupName == "Excel孝班" && excluded.GetWorking(RankKinds.Tag2).GroupName == "Excel乙組", "不排名類別優先套用，名稱仍是 Excel 原值");
            Expect(errors, blank != null && blank.GetWorking(RankKinds.Department).GroupName == "" && blank.GetWorking(RankKinds.Department).Participate && blank.GetWorking(RankKinds.Tag1).GroupName == "" && !blank.GetWorking(RankKinds.Tag1).Participate && blank.GetWorking(RankKinds.Class).GroupName == "Excel仁班", "空白母群不應改用目前班級、科別或類別");
            Expect(errors, named.GetWorking(RankKinds.Class).GroupName != "現在忠班" && named.GetWorking(RankKinds.Department).GroupName != "現在科", "目前學生資料不應覆蓋 Excel 母群");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup group in groups)
                rows.Add(new ConfirmStudentRow { Group = group });
            list.Load(rows);
            Expect(errors, HistoryCalculationScope.KeepsStudentWithoutScore(list, "301") && list.Rows.Count == 3, "沒有本次成績的 Excel 學生仍應留在確認清單");

            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            setting.SetAttribute("學年度", period.SchoolYear);
            setting.SetAttribute("學期", period.Semester);
            setting.SetAttribute("考試名稱", "學期成績");
            document.AppendChild(setting);
            MotherGroupSnapshot.Append(setting, list, MotherGroupSnapshot.KindFromSourceName(CalculationPeriod.ExcelSourceName), "");
            string xml = setting.OuterXml;
            List<StudentMotherGroup> restored;
            string kind;
            string historyBatchId;
            Expect(errors, MotherGroupSnapshot.TryRestore(xml, out restored, out kind, out historyBatchId), "Excel 確認清單應寫入新批次快照");
            Expect(errors, kind == "excel" && historyBatchId == "" && restored.Count == 3, "Excel 快照不應帶歷史批次");
            Expect(errors, xml.IndexOf("學年度=\"114\"") >= 0 && xml.IndexOf("學期=\"1\"") >= 0 && xml.IndexOf("學年度=\"113\"") < 0 && xml.IndexOf("Excel忠班") >= 0, "快照應使用圖1期間並保留 Excel 母群名稱");
            StudentMotherGroup restoredExcluded = Find(restored, "302");
            Expect(errors, restoredExcluded != null && !Any(restoredExcluded) && restoredExcluded.GetWorking(RankKinds.Department).GroupName == "Excel商科", "快照應保留不排名學生與 Excel 名稱");

            SemesterRankStudentSqlResult legacyStudents = SemesterRankSql.BuildLegacySelects(list);
            SemesterRankStudentSqlResult rankStudents = SemesterRankSql.Build(list);
            string selects = "";
            foreach (SemesterRankGradeStudents grade in legacyStudents.Grades)
            {
                foreach (string select in grade.Selects)
                    selects += select + "\n";
            }
            Expect(errors, rankStudents.Grades.Count == 1 && rankStudents.Grades[0].Students.Count == 3 && legacyStudents.Grades[0].Selects.Count == 3, "排名清單應使用同一份 Excel 確認清單");
            Expect(errors, Clause(selects, "301").IndexOf("Excel忠班") >= 0 && Clause(selects, "301").IndexOf("true AS participate_year") >= 0 && Clause(selects, "301").IndexOf("false AS participate_tag2") >= 0, "排名 SQL 應保留 Excel 名稱與參與旗標");
            Expect(errors, Clause(selects, "302").IndexOf("false AS participate_year") >= 0 && Clause(selects, "302").IndexOf("false AS participate_class") >= 0 && Clause(selects, "302").IndexOf("false AS participate_dept") >= 0 && Clause(selects, "302").IndexOf("false AS participate_tag1") >= 0 && Clause(selects, "302").IndexOf("false AS participate_tag2") >= 0, "不排名學生的五種旗標都應是不參與");
            Expect(errors, formula.IndexOf("subject_origin_score IS NOT NULL") >= 0 && formula.IndexOf("entry_score IS NOT NULL") >= 0, "沒有成績時仍沿用既有分數條件，不另產生明細");

            if (formula.Length == 0)
                return errors;
            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(rankStudents.Grades, period.SchoolYear, period.Semester, xml, formula, false);
            string applied = SemesterRankSql.ApplyParticipation(publish.Sql);
            Expect(errors, publish.DatabaseExecuted == false && applied.IndexOf("'114'::TEXT AS rank_school_year") >= 0 && applied.IndexOf("'113'::TEXT AS rank_school_year") < 0, "Excel 公布語句的成績期間應是圖1的 114-1");
            Expect(errors, applied.IndexOf("kind=\"excel\"") >= 0 && applied.IndexOf("historyBatchId=\"\"") >= 0 && applied.IndexOf("Excel忠班") >= 0 && applied.IndexOf("jsonb_to_recordset") >= 0 && applied.IndexOf("\"student_id\":302") >= 0, "快照與排名 SQL 應使用同一份 Excel 確認資料");
            Expect(errors, Branch(applied, "年排名").IndexOf("participate_year") >= 0 && Branch(applied, "科排名").IndexOf("participate_dept") >= 0 && Branch(applied, "班排名").IndexOf("participate_class") >= 0 && Branch(applied, "類別1排名").IndexOf("participate_tag1") >= 0 && Branch(applied, "類別2排名").IndexOf("participate_tag2") >= 0, "五種參與應在對應排名前生效");
            Expect(errors, applied.IndexOf("subject_origin_score IS NOT NULL") >= 0 && applied.IndexOf(';') < 0 && applied.IndexOf("XMLTABLE") < 0 && applied.IndexOf("MERGE") < 0, "公布應維持既有公式、單一語句與 PostgreSQL 9.6 相容寫法");
            return errors;
        }

        private static List<string> CheckForm(string form)
        {
            List<string> errors = new List<string>();
            int excel = form.IndexOf("void BeginExcelImport");
            int history = form.IndexOf("bool IsHistorySourceSelected");
            string body = excel >= 0 && history > excel ? form.Substring(excel, history - excel) : "";
            int label = body.IndexOf("lbSchoolYear.Text = _calculationPeriod.SchoolYear");
            int events = body.IndexOf("EnsureConfirmEvents()");
            int show = body.IndexOf("ShowConfirmList");
            Expect(errors, label >= 0 && events > label && show > events && body.IndexOf("HistorySchoolYear") < 0, "Excel 進入確認清單時應使用圖1計算期間");

            int calculate = form.IndexOf("void btnCacluate_Click");
            int resize = form.IndexOf("void CalculateSemesterAssessmentRank_Resize");
            string calculateBody = calculate >= 0 && resize > calculate ? form.Substring(calculate, resize - calculate) : "";
            int gate = calculateBody.IndexOf("CanWriteRankTables");
            int returned = calculateBody.IndexOf("return;");
            int year = calculateBody.IndexOf("string schoolYear = lbSchoolYear.Text");
            Expect(errors, gate >= 0 && returned > gate && returned < year && calculateBody.IndexOf("HistorySchoolYear") < 0, "Excel 來源的計算仍讀圖2成績期間，閘門返回仍在讀取學年度之前");
            Expect(errors, calculateBody.IndexOf("CopyWorking()", returned) > returned && calculateBody.IndexOf("沒有可計算的年級", returned) > returned && calculateBody.IndexOf("RankCalculationConfirm.Message", returned) > returned && calculateBody.IndexOf("queryHelper.Select(insertRankSql)", returned) > returned && calculateBody.IndexOf("計算排名失敗", returned) > returned, "Excel 來源進入既有計算流程，並保留工作副本、空年級防護、確認框、單次查詢與失敗訊息");
            Expect(errors, calculateBody.IndexOf("motherGroupSourceName == CalculationPeriod.HistorySourceName") >= 0 && calculateBody.IndexOf("CalculationPeriod.ExcelSourceName") < 0, "Excel 來源不應把歷史批次識別寫進快照");
            string gateSource = Read("MotherGroup\\SemesterRankWriteGate.cs", errors);
            Expect(errors, gateSource.IndexOf("return true;") >= 0 && gateSource.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static List<string> CheckOtherRankFiles()
        {
            List<string> errors = new List<string>();
            string[] tokens = new string[] { "MotherGroup", "SemesterRankSql", "SemesterRankPublishSql", "SemesterRankWriteGate" };
            foreach (string path in Directory.GetFiles(".", "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(path);
                if (path.IndexOf("\\bin\\") >= 0 || path.IndexOf("\\obj\\") >= 0 || path.IndexOf("\\MotherGroup\\") >= 0)
                    continue;
                if (name.IndexOf("Regular") < 0 && name.IndexOf("Technology") < 0)
                    continue;
                string text = File.ReadAllText(path, Encoding.UTF8);
                foreach (string token in tokens)
                {
                    if (text.IndexOf(token) >= 0)
                        errors.Add("定期評量或技職繁星檔案不應改接母群計算：" + path);
                }
            }
            return errors;
        }

        private static Workbook RoundTrip(params StudentMotherGroup[] students)
        {
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup student in students)
                rows.Add(new ConfirmStudentRow { Group = student });
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(rows);
            Workbook created = MotherGroupExcelExchange.Create(list);
            MemoryStream stream = new MemoryStream();
            created.Save(stream, SaveFormat.Xlsx);
            stream.Position = 0;
            return MotherGroupExcelExchange.Open(stream);
        }

        private static StudentMotherGroup Student(string id, string name, string year, string className, string department, string tag1, string tag2, bool yearOn, bool classOn, bool departmentOn, bool tag1On, bool tag2On)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentNumber = id;
            student.StudentName = name;
            student.Status = 1;
            student.SetSourceSlots(new List<RankSlot>
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = year, Participate = yearOn },
                new RankSlot { RankType = RankKinds.Department, GroupName = department, Participate = departmentOn },
                new RankSlot { RankType = RankKinds.Class, GroupName = className, Participate = classOn },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = tag1, Participate = tag1On },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = tag2, Participate = tag2On }
            });
            return student;
        }

        private static string Branch(string sql, string rankType)
        {
            string token = "'" + rankType + "'::TEXT AS rank_type";
            int at = sql.IndexOf(token);
            if (at < 0)
                return "";
            int end = sql.IndexOf("UNION ALL", at);
            if (end < 0)
                end = Math.Min(sql.Length, at + 900);
            return sql.Substring(at, end - at);
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

        private static bool Any(StudentMotherGroup student)
        {
            if (student == null)
                return false;
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

        private static DataTable Lookup(params object[][] rows)
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id");
            table.Columns.Add("student_number");
            table.Columns.Add("student_name");
            table.Columns.Add("student_status");
            foreach (object[] row in rows)
                table.Rows.Add(row);
            return table;
        }

        private static object[] Row(string id, string number, string name)
        {
            return new object[] { id, number, name, "1" };
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

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
