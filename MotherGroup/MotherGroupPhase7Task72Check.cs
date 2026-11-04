using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase7Task72Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                string form = Read("CalculateSemesterAssessmentRank.cs", errors);
                string formula = LoadFormula(form, errors);
                errors.AddRange(CheckHistoryCalculation(formula));
                errors.AddRange(CheckForm(form));
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase7-task72-check: ok");
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

        private static List<string> CheckHistoryCalculation(string formula)
        {
            List<string> errors = new List<string>();
            const string scoreYear = "114";
            const string scoreSemester = "1";
            const string motherYear = "113";
            const string motherSemester = "2";
            const string batchId = "88";

            CalculationPeriod period = CalculationPeriod.CreateDefault(scoreYear, scoreSemester);
            period.RememberHistoryBatch(batchId, motherYear, motherSemester);
            Expect(errors, period.SchoolYear == scoreYear && period.Semester == scoreSemester, "計算期間應維持圖1選定的學年度與學期");
            Expect(errors, period.HistorySchoolYear == motherYear && period.HistorySemester == motherSemester && period.HistoryBatchId == batchId, "歷史批次期間應另存，不覆蓋計算期間");

            string restoreSql = HistoryRankRestore.SelectSql(batchId);
            Expect(errors, restoreSql.IndexOf("school_year") < 0 && restoreSql.IndexOf("semester") < 0 && restoreSql.IndexOf("INSERT") < 0 && restoreSql.IndexOf("UPDATE") < 0 && restoreSql.IndexOf("DELETE") < 0, "無快照還原只依批次識別讀取，不寫入也不使用成績期間");

            DataTable table = Table();
            AddStudent(table, "201", "S201", "王", "2年級", "忠班", "甲組", "");
            AddRank(table, "201", "S201", "王", RankKinds.Department, "普通科");
            AddStudent(table, "202", "S202", "李", "2年級", "忠班", "甲組", "");
            AddRank(table, "202", "S202", "李", RankKinds.Department, "普通科");
            HistoryRankLoad load = HistoryRankRestore.Read(
                batchId,
                table,
                "不排名",
                new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } },
                new List<StudentTagLink> { new StudentTagLink { StudentId = "202", TagId = "ex", Name = "休學" } });
            Expect(errors, load.Restored && load.Students.Count == 2, "無快照歷史批次應還原兩名學生");
            StudentMotherGroup scored = Find(load.Students, "201");
            StudentMotherGroup excluded = Find(load.Students, "202");
            Expect(errors, scored != null && scored.GetWorking(RankKinds.Year).GroupName == "2年級" && scored.GetWorking(RankKinds.Year).Participate && scored.GetWorking(RankKinds.Department).GroupName == "普通科" && !scored.GetWorking(RankKinds.Tag2).Participate, "歷史母群名稱應保留，空白類別二不參與");
            Expect(errors, excluded != null && !Any(excluded) && excluded.GetWorking(RankKinds.Class).GroupName == "忠班", "符合不排名類別者留在清單，名稱不變且五種不參與");

            MotherGroupConfirmList list = new MotherGroupConfirmList();
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup student in load.Students)
                rows.Add(new ConfirmStudentRow { Group = student });
            list.Load(rows);
            Expect(errors, HistoryCalculationScope.KeepsStudentWithoutScore(list, "201") && HistoryCalculationScope.KeepsStudentWithoutScore(list, "202"), "沒有本次成績的歷史學生仍應留在確認清單");

            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            setting.SetAttribute("學年度", period.SchoolYear);
            setting.SetAttribute("學期", period.Semester);
            setting.SetAttribute("考試名稱", "學期成績");
            document.AppendChild(setting);
            XmlElement grade = document.CreateElement("年級");
            grade.InnerText = "2";
            setting.AppendChild(grade);
            MotherGroupSnapshot.Append(setting, list, MotherGroupSnapshot.KindFromSourceName(CalculationPeriod.HistorySourceName), period.HistoryBatchId);
            string xml = setting.OuterXml;
            List<StudentMotherGroup> restored;
            string kind;
            string historyBatchId;
            Expect(errors, MotherGroupSnapshot.TryRestore(xml, out restored, out kind, out historyBatchId), "新批次應能還原歷史母群快照");
            Expect(errors, kind == "history" && historyBatchId == batchId && restored.Count == 2, "快照應記錄歷史批次，而不是改寫舊批次");
            Expect(errors, xml.IndexOf("學年度=\"114\"") >= 0 && xml.IndexOf("學期=\"1\"") >= 0 && xml.IndexOf("學年度=\"113\"") < 0, "快照的成績期間應是圖1的 114-1，不是歷史批次的 113-2");
            Expect(errors, Find(restored, "201") != null && Find(restored, "202") != null && !Any(Find(restored, "202")), "沒有本次成績與不參與的學生都應留在快照");

            SemesterRankStudentSqlResult legacyStudents = SemesterRankSql.BuildLegacySelects(list);
            SemesterRankStudentSqlResult rankStudents = SemesterRankSql.Build(list);
            string selects = string.Join("\n", legacyStudents.Grades[0].Selects.ToArray());
            string includedSql = Clause(selects, "201");
            string excludedSql = Clause(selects, "202");
            Expect(errors, rankStudents.Grades.Count == 1 && rankStudents.Grades[0].Students.Count == 2 && legacyStudents.Grades[0].Selects.Count == 2, "排名清單應使用同一份確認清單");
            Expect(errors, includedSql.IndexOf("true AS participate_year") >= 0 && includedSql.IndexOf("false AS participate_tag2") >= 0, "有歷史母群的學生只在該項參與時進入排名");
            Expect(errors, excludedSql.IndexOf("false AS participate_year") >= 0 && excludedSql.IndexOf("false AS participate_class") >= 0, "不參與者的排名旗標應為 false");
            Expect(errors, formula.IndexOf("subject_origin_score IS NOT NULL") >= 0 && formula.IndexOf("entry_score IS NOT NULL") >= 0 && formula.IndexOf("sems_subj_score_ext.school_year = calc_condition.rank_school_year::INT") >= 0, "成績仍依選定學年度與既有分數條件取得，沒有分數不產生明細");

            if (formula.Length == 0)
                return errors;
            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(rankStudents.Grades, period.SchoolYear, period.Semester, xml, formula, false);
            string applied = SemesterRankSql.ApplyParticipation(publish.Sql);
            Expect(errors, publish.DatabaseExecuted == false && applied.IndexOf("'114'::TEXT AS rank_school_year") >= 0 && applied.IndexOf("'113'::TEXT AS rank_school_year") < 0, "公布語句的成績期間應是 114，不是歷史批次的 113");
            Expect(errors, applied.IndexOf("historyBatchId=\"88\"") >= 0 && applied.IndexOf("jsonb_to_recordset") >= 0 && applied.IndexOf("\"student_id\":201") >= 0 && applied.IndexOf("\"student_id\":202") >= 0, "公布語句、學生清單與快照應是同一份確認資料");
            Expect(errors, Branch(applied, "年排名").IndexOf("participate_year") >= 0 && Branch(applied, "類別2排名").IndexOf("participate_tag2") >= 0, "有成績項目的名次應在計算前排除不參與者");
            Expect(errors, applied.IndexOf("subject_origin_score IS NOT NULL") >= 0 && applied.IndexOf(';') < 0, "排名公式仍用既有分數條件，且公布維持單一語句");
            return errors;
        }

        private static List<string> CheckForm(string form)
        {
            List<string> errors = new List<string>();
            int history = form.IndexOf("void BeginHistorySnapshotRestore");
            int show = form.IndexOf("ShowConfirmList(rankLoad.Students", history);
            string body = history >= 0 && show > history ? form.Substring(history, show - history) : "";
            int captured = body.IndexOf("string calculationSchoolYear = _calculationPeriod.SchoolYear");
            int remembered = body.IndexOf("RememberHistoryBatch");
            int label = body.IndexOf("lbSchoolYear.Text = calculationSchoolYear");
            Expect(errors, remembered >= 0 && captured > remembered && label > captured && body.IndexOf("MissingSnapshotMessage") >= 0 && body.IndexOf("HistoryRankRestore.Read") > body.IndexOf("MissingSnapshotMessage"), "無快照還原後，圖2學年度仍應使用計算期間");
            Expect(errors, body.IndexOf("lbSchoolYear.Text = selected.SchoolYear") < 0, "歷史批次學年度不應寫入計算學年度");

            int calculate = form.IndexOf("void btnCacluate_Click");
            int resize = form.IndexOf("void CalculateSemesterAssessmentRank_Resize");
            string calculateBody = calculate >= 0 && resize > calculate ? form.Substring(calculate, resize - calculate) : "";
            int gate = calculateBody.IndexOf("CanWriteRankTables");
            int returned = calculateBody.IndexOf("return;");
            Expect(errors, gate >= 0 && returned > gate && returned < calculateBody.IndexOf("string schoolYear = lbSchoolYear.Text") && calculateBody.IndexOf("HistorySchoolYear") < 0, "歷史來源的計算仍讀圖2成績期間，閘門返回仍在讀取學年度之前");
            Expect(errors, calculateBody.IndexOf("CopyWorking()", returned) > returned && calculateBody.IndexOf("沒有可計算的年級", returned) > returned && calculateBody.IndexOf("RankCalculationConfirm.Message", returned) > returned && calculateBody.IndexOf("queryHelper.Select(insertRankSql)", returned) > returned && calculateBody.IndexOf("計算排名失敗", returned) > returned, "歷史來源進入既有計算流程，並保留工作副本、空年級防護、確認框、單次查詢與失敗訊息");
            string gateSource = Read("MotherGroup\\SemesterRankWriteGate.cs", errors);
            Expect(errors, gateSource.IndexOf("return true;") >= 0 && gateSource.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            return errors;
        }

        private static string Branch(string sql, string rankType)
        {
            string token = "'" + rankType + "'::TEXT AS rank_type";
            int at = sql.IndexOf(token);
            if (at < 0)
                return "";
            int end = sql.IndexOf("UNION ALL", at);
            if (end < 0)
                end = Math.Min(sql.Length, at + 800);
            return sql.Substring(at, end - at);
        }

        private static string LoadFormula(string source, List<string> errors)
        {
            Expect(errors, source.IndexOf("SemesterRankFormulaSql.Build(gradeSelectCopy)") >= 0, "產品畫面應改呼叫共用公式組句");
            string formula = SemesterRankFormulaSql.Build(new string[] { "學年調整成績", "手動調整成績", "補考成績", "重修成績" });
            if (formula.IndexOf(", subject_score AS") < 0 || formula.IndexOf("SELECT count(*) FROM score_list") < 0 || formula.IndexOf(") AS subject_greatest_score") < 0)
            {
                errors.Add("應能從共用公式組句取得年級排名 SQL");
                return "";
            }
            Expect(errors, formula.IndexOf("@學年調整成績") >= 0 && formula.IndexOf("@擇優採計成績") >= 0 && formula.IndexOf("@補考成績") >= 0 && formula.IndexOf("@重修成績") >= 0, "共用公式應含四種擇優 xpath");
            Expect(errors, formula.IndexOf("+ schoolYearAdjScore") < 0 && formula.IndexOf("+ @\"") < 0, "共用公式不可殘留 C# 接縫");
            return formula;
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
