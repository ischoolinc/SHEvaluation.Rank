using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase4Task43Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckRoundTrip());
                errors.AddRange(CheckSpecialCharacters());
                errors.AddRange(CheckBlankAllOffAndNoScore());
                errors.AddRange(CheckSameInsert());
                errors.AddRange(CheckForm());
                errors.AddRange(CheckDataIntegrity());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
                Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
                string snapshotSource = File.ReadAllText("MotherGroup\\MotherGroupSnapshot.cs", Encoding.UTF8);
                Expect(errors, snapshotSource.IndexOf("score_list") < 0 && snapshotSource.IndexOf("rank_batch_student") < 0 && snapshotSource.IndexOf("GetOriginal") < 0, "快照應讀確認清單的工作副本，不讀成績列、批次學生或來源原值");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase4-task43-check: ok");
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

        private static List<string> CheckRoundTrip()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup edited = Student("430001", "S001", "王小明", "1年級", true, "普通科", true, "忠", true, "甲", true, "社團", false);
            edited.GetOriginal(RankKinds.Year).Participate = true;
            edited.GetWorking(RankKinds.Year).Participate = false;
            StudentMotherGroup excluded = Student("430002", "S002", "李小華", "1年級", false, "普通科", false, "忠", false, "甲", false, "社團", false);
            excluded.MatchesExcludedCategory = true;
            MotherGroupConfirmList list = ListOf(edited, excluded);
            string xml = SettingXml(list, MotherGroupSourceKind.Current, "");
            List<StudentMotherGroup> restored;
            string kind;
            string historyBatchId;
            Expect(errors, MotherGroupSnapshot.TryRestore(xml, out restored, out kind, out historyBatchId), "版本 1 快照應可讀回");
            Expect(errors, kind == "current" && historyBatchId == "" && restored.Count == 2, "應還原來源種類與全部學生");
            Expect(errors, SameStudent(restored[0], edited), "讀回應保留圖2最終參與值，而不是來源原值");
            Expect(errors, SameStudent(restored[1], excluded), "符合不排名類別的學生仍應留在快照");
            string unsupported = xml.Replace("version=\"1\"", "version=\"2\"");
            List<StudentMotherGroup> ignored;
            Expect(errors, !MotherGroupSnapshot.TryRestore(unsupported, out ignored) && ignored.Count == 0, "不是版本 1 的快照不應當成可讀快照");
            return errors;
        }

        private static List<string> CheckSpecialCharacters()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup special = Student("430010", "S&1", "王&小<明>\"甲'乙", "1年級&A", true, "普通科<乙>", true, "忠\"班", true, "甲&乙", false, "社'團", true);
            string xml = SettingXml(ListOf(special), MotherGroupSourceKind.Excel, "");
            string recovered = xml.Replace("'", "''").Replace("''", "'");
            List<StudentMotherGroup> restored;
            string kind;
            string historyBatchId;
            Expect(errors, MotherGroupSnapshot.TryRestore(recovered, out restored, out kind, out historyBatchId), "含特殊字元的快照應可讀回");
            Expect(errors, restored.Count == 1 && kind == "excel" && SameStudent(restored[0], special), "特殊字元的學號、姓名、母群名稱與參與值應完整還原");
            return errors;
        }

        private static List<string> CheckBlankAllOffAndNoScore()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup blank = Student("430020", "", "空白名稱", "", false, "", true, "", false, "", false, "", false);
            StudentMotherGroup none = Student("430021", "S021", "全不參與", "2年級", false, "商業科", false, "孝", false, "", false, "", false);
            StudentMotherGroup noScore = Student("430022", "S022", "無成績", "未分班", true, "普通科", true, "仁", true, "甲", true, "社團", true);
            MotherGroupConfirmList list = ListOf(blank, none, noScore);
            SemesterRankStudentSqlResult rankStudents = SemesterRankSql.Build(list);
            Expect(errors, rankStudents.SkippedWithoutGrade.Contains("430022") || !HasStudent(rankStudents, "430022"), "無法解析年級的學生不進入排名學生清單");
            string xml = SettingXml(list, MotherGroupSourceKind.HistorySnapshot, "");
            List<StudentMotherGroup> restored;
            Expect(errors, MotherGroupSnapshot.TryRestore(xml, out restored) && restored.Count == 3, "空白名稱、全不參與與無成績學生都應寫入快照");
            Expect(errors, SameStudent(Find(restored, "430020"), blank), "空白母群名稱應照工作副本還原");
            Expect(errors, SameStudent(Find(restored, "430021"), none), "五種都不參與的學生應保留名稱與 false");
            Expect(errors, SameStudent(Find(restored, "430022"), noScore), "沒有進入排名清單的學生仍應在快照");
            return errors;
        }

        private static List<string> CheckSameInsert()
        {
            List<string> errors = new List<string>();
            string formula = Formula(errors);
            if (formula.Length == 0)
                return errors;
            StudentMotherGroup ranked = Student("430030", "S030", "有成績", "1年級", true, "普通科", true, "忠", true, "甲", true, "社團", true);
            StudentMotherGroup noScore = Student("430031", "S031", "無成績", "未分班", false, "", false, "", false, "", false, "", false);
            MotherGroupConfirmList list = ListOf(ranked, noScore);
            string setting = SettingXml(list, MotherGroupSourceKind.Current, "");
            SemesterRankGradeStudents grade = new SemesterRankGradeStudents();
            grade.GradeYear = 1;
            grade.Selects = new List<string>();
            grade.Selects.Add("SELECT\n'430030'::BIGINT AS student_id\n, '有成績'::TEXT AS student_name\n, '1'::INT AS rank_grade_year\n");
            SemesterRankPublishStatement statement = SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { grade }, "114", "1", setting, formula, false, true);
            string sql = statement.Sql;
            Expect(errors, statement.DatabaseExecuted == false && sql.IndexOf(';') < 0, "快照應留在單一公布語句");
            Expect(errors, CountOf(sql, "INSERT INTO rank_batch") == 1 && sql.IndexOf("UPDATE rank_batch") < 0 && sql.IndexOf("UPDATE\n\t\trank_batch") < 0, "快照與新批次在同一個 INSERT，不更新既有批次");
            string embedded = setting.Replace("'", "''");
            Expect(errors, sql.IndexOf(embedded) >= 0, "新批次的 setting 應含完整快照 XML");
            string studentList = Between(sql, "WITH student_list AS", ", insert_batch_data AS");
            Expect(errors, studentList.IndexOf("430031") < 0 && sql.IndexOf("430031") >= 0, "無成績學生只出現在快照，不由排名學生清單產生");
            List<StudentMotherGroup> restored;
            Expect(errors, MotherGroupSnapshot.TryRestore(embedded.Replace("''", "'"), out restored) && restored.Count == 2, "從公布語句使用的設定文字應能還原兩名學生");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string source = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int gate = source.IndexOf("CanWriteRankTables");
            int compose = source.IndexOf("MotherGroupSnapshotJson.Compose");
            int setting = source.IndexOf("calculationSetting = settingEle.OuterXml");
            int build = source.IndexOf("SemesterRankPublishSql.Build(");
            int select = source.IndexOf("queryHelper.Select(insertRankSql)");
            Expect(errors, gate >= 0 && setting > gate && compose > setting && build > compose && select > build, "快照應在設定 XML 之後組成，再放進唯一一次公布查詢");
            Expect(errors, source.IndexOf("MotherGroupSnapshot.Append") < 0, "新批次不應再把母群快照寫進 setting");
            Expect(errors, CountOf(source, "queryHelper.Select(insertRankSql)") == 1 && source.IndexOf("UPDATE rank_batch") < 0, "表單不應另外更新既有 rank_batch");
            return errors;
        }

        private static List<string> CheckDataIntegrity()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup first = Student("430040", "S040", "第一位", "1年級", true, "普通科", true, "忠", true, "甲", true, "乙", false);
            XmlElement setting = FreshSetting();
            MotherGroupSnapshot.Append(setting, ListOf(first), MotherGroupSourceKind.Current, "");
            string before = setting.OuterXml;

            StudentMotherGroup duplicate = Student("430040", "S041", "重複", "1年級", true, "普通科", true, "孝", true, "甲", true, "乙", false);
            string duplicateMessage = AppendError(setting, ListOf(first, duplicate));
            Expect(errors, duplicateMessage.IndexOf("重複") >= 0 && duplicateMessage.IndexOf("430040") >= 0, "重複學生識別應拋出明確例外");
            Expect(errors, setting.OuterXml == before, "學生識別重複時不應修改 Setting");

            MotherGroupConfirmList blank = ListOf(first);
            blank.Rows.Add(new ConfirmStudentRow { Group = Student("", "S042", "空白識別", "1年級", true, "普通科", true, "忠", true, "甲", true, "乙", false) });
            string blankMessage = AppendError(setting, blank);
            Expect(errors, blankMessage.IndexOf("缺少學生識別") >= 0 && setting.OuterXml == before, "空白學生識別應拒絕建立快照，且不修改 Setting");

            MotherGroupConfirmList whitespace = ListOf(first);
            whitespace.Rows.Add(new ConfirmStudentRow { Group = Student("   ", "S043", "空白識別", "1年級", true, "普通科", true, "忠", true, "甲", true, "乙", false) });
            string whitespaceMessage = AppendError(setting, whitespace);
            Expect(errors, whitespaceMessage.IndexOf("缺少學生識別") >= 0 && setting.OuterXml == before, "只有空白字元的學生識別應拒絕建立快照，且不修改 Setting");

            List<StudentMotherGroup> restored;
            string kind;
            string historyBatchId;
            string laterMissing = SnapshotXml(StudentXml("430040", "S040", "第一位", FiveRanks()) + StudentXml("430041", "S041", "後段", FourRanks()));
            Expect(errors, !MotherGroupSnapshot.TryRestore(laterMissing, out restored, out kind, out historyBatchId) && restored.Count == 0 && kind == "" && historyBatchId == "", "後段學生缺少排名類型時不應回傳前面的學生");

            string laterDuplicateRank = SnapshotXml(StudentXml("430040", "S040", "第一位", FiveRanks()) + StudentXml("430041", "S041", "後段", DuplicateYearRanks()));
            Expect(errors, !MotherGroupSnapshot.TryRestore(laterDuplicateRank, out restored, out kind, out historyBatchId) && restored.Count == 0 && kind == "", "後段學生的排名類型重複時不應回傳部分學生");

            string duplicateIds = SnapshotXml(StudentXml("430040", "S040", "第一位", FiveRanks()) + StudentXml("430040", "S041", "重複", FiveRanks()));
            Expect(errors, !MotherGroupSnapshot.TryRestore(duplicateIds, out restored) && restored.Count == 0, "快照內學生識別重複時不應回傳任何學生");

            string blankId = SnapshotXml(StudentXml("430040", "S040", "第一位", FiveRanks()) + StudentXml("", "S042", "空白", FiveRanks()));
            Expect(errors, !MotherGroupSnapshot.TryRestore(blankId, out restored) && restored.Count == 0, "快照內空白學生識別不應回傳部分學生");

            string badKind = SnapshotXml(StudentXml("430040", "S040", "第一位", FiveRanks())).Replace("kind=\"current\"", "kind=\"unknown\"");
            Expect(errors, !MotherGroupSnapshot.TryRestore(badKind, out restored, out kind, out historyBatchId) && restored.Count == 0 && kind == "", "來源種類錯誤時應無法還原");

            string illegal = "<Setting><MotherGroupSnapshot version=\"1\"";
            Expect(errors, !MotherGroupSnapshot.TryRestore(illegal, out restored, out kind, out historyBatchId) && restored.Count == 0 && kind == "", "非法 XML 應無法還原，且不丟出未處理例外");
            return errors;
        }

        private static string AppendError(XmlElement setting, MotherGroupConfirmList list)
        {
            try
            {
                MotherGroupSnapshot.Append(setting, list, MotherGroupSourceKind.Current, "");
                return "";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        private static XmlElement FreshSetting()
        {
            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            document.AppendChild(setting);
            setting.SetAttribute("學年度", "114");
            setting.SetAttribute("學期", "1");
            setting.SetAttribute("考試名稱", "學期成績");
            return setting;
        }

        private static string SnapshotXml(string students)
        {
            return "<Setting 學年度=\"114\" 學期=\"1\" 考試名稱=\"學期成績\"><MotherGroupSnapshot version=\"1\"><Source kind=\"current\" historyBatchId=\"\" />" + students + "</MotherGroupSnapshot></Setting>";
        }

        private static string StudentXml(string id, string number, string name, string ranks)
        {
            return "<Student id=\"" + id + "\" number=\"" + number + "\" name=\"" + name + "\">" + ranks + "</Student>";
        }

        private static string FiveRanks()
        {
            return RankXml("年排名", "1年級", "true") + RankXml("科排名", "普通科", "true") + RankXml("班排名", "忠", "true") + RankXml("類別1排名", "甲", "false") + RankXml("類別2排名", "乙", "false");
        }

        private static string FourRanks()
        {
            return RankXml("年排名", "1年級", "true") + RankXml("科排名", "普通科", "true") + RankXml("班排名", "忠", "true") + RankXml("類別1排名", "甲", "false");
        }

        private static string DuplicateYearRanks()
        {
            return RankXml("年排名", "1年級", "true") + RankXml("年排名", "2年級", "false") + RankXml("科排名", "普通科", "true") + RankXml("班排名", "忠", "true") + RankXml("類別1排名", "甲", "false") + RankXml("類別2排名", "乙", "false");
        }

        private static string RankXml(string type, string name, string participate)
        {
            return "<Rank type=\"" + type + "\" name=\"" + name + "\" participate=\"" + participate + "\" />";
        }

        private static string SettingXml(MotherGroupConfirmList list, MotherGroupSourceKind kind, string historyBatchId)
        {
            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            document.AppendChild(setting);
            setting.SetAttribute("學年度", "114");
            setting.SetAttribute("學期", "1");
            setting.SetAttribute("考試名稱", "學期成績");
            setting.SetAttribute("不排名學生類別", "休學");
            setting.SetAttribute("類別一", "甲");
            setting.SetAttribute("類別二", "乙");
            XmlElement grade = document.CreateElement("年級");
            grade.InnerText = "1";
            setting.AppendChild(grade);
            XmlElement best = document.CreateElement("擇優採計成績");
            best.InnerText = "學年調整成績";
            setting.AppendChild(best);
            MotherGroupSnapshot.Append(setting, list, kind, historyBatchId);
            if (setting.GetAttribute("學年度") != "114" || setting.GetAttribute("考試名稱") != "學期成績" || setting.GetAttribute("不排名學生類別") != "休學" || setting.GetAttribute("類別一") != "甲" || setting.GetAttribute("類別二") != "乙")
                throw new InvalidOperationException("快照不應覆蓋原設定屬性。");
            if (setting.GetElementsByTagName("年級").Count != 1 || setting.GetElementsByTagName("擇優採計成績").Count != 1)
                throw new InvalidOperationException("快照不應移除年級或擇優節點。");
            return setting.OuterXml;
        }

        private static StudentMotherGroup Student(string id, string number, string name, string year, bool yearOn, string dept, bool deptOn, string className, bool classOn, string tag1, bool tag1On, string tag2, bool tag2On)
        {
            StudentMotherGroup group = new StudentMotherGroup();
            group.StudentId = id;
            group.StudentNumber = number;
            group.StudentName = name;
            group.SetSourceSlots(new RankSlot[]
            {
                Slot(RankKinds.Year, year, yearOn),
                Slot(RankKinds.Department, dept, deptOn),
                Slot(RankKinds.Class, className, classOn),
                Slot(RankKinds.Tag1, tag1, tag1On),
                Slot(RankKinds.Tag2, tag2, tag2On)
            });
            return group;
        }

        private static RankSlot Slot(string rankType, string groupName, bool participate)
        {
            return new RankSlot { RankType = rankType, GroupName = groupName, Participate = participate };
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

        private static bool SameStudent(StudentMotherGroup actual, StudentMotherGroup expected)
        {
            if (actual == null || expected == null)
                return false;
            if (actual.StudentId != expected.StudentId || actual.StudentNumber != expected.StudentNumber || actual.StudentName != expected.StudentName)
                return false;
            foreach (string rankType in RankKinds.All)
            {
                RankSlot left = actual.GetWorking(rankType);
                RankSlot right = expected.GetWorking(rankType);
                if (left.GroupName != right.GroupName || left.Participate != right.Participate)
                    return false;
            }
            return true;
        }

        private static StudentMotherGroup Find(List<StudentMotherGroup> students, string studentId)
        {
            foreach (StudentMotherGroup student in students)
            {
                if (student.StudentId == studentId)
                    return student;
            }
            return null;
        }

        private static bool HasStudent(SemesterRankStudentSqlResult result, string studentId)
        {
            foreach (SemesterRankGradeStudents grade in result.Grades)
            {
                if (grade.Students != null)
                {
                    foreach (SemesterRankStudentInput student in grade.Students)
                    {
                        if (student != null && student.StudentId.ToString() == studentId)
                            return true;
                    }
                }
                if (grade.Selects == null)
                    continue;
                foreach (string select in grade.Selects)
                {
                    if (select.IndexOf("'" + studentId + "'") >= 0)
                        return true;
                }
            }
            return false;
        }

        private static string Formula(List<string> errors)
        {
            string formula = SemesterRankFormulaSql.BuildOriginOnly();
            if (formula.IndexOf(", subject_score AS") < 0 || formula.IndexOf("SELECT count(*) FROM score_list") < 0)
            {
                errors.Add("應能從共用公式組句取得排名公式");
                return "";
            }
            return formula;
        }

        private static string Between(string text, string start, string end)
        {
            int from = text.IndexOf(start);
            int to = text.IndexOf(end, from < 0 ? 0 : from + start.Length);
            if (from < 0 || to < from)
                return "";
            return text.Substring(from, to - from);
        }

        private static int CountOf(string text, string token)
        {
            int count = 0;
            int at = 0;
            while (text != null && at >= 0 && at < text.Length)
            {
                at = text.IndexOf(token, at);
                if (at < 0)
                    break;
                count++;
                at += token.Length;
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
