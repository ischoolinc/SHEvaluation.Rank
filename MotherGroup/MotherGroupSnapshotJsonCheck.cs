using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupSnapshotJsonCheck
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckRoundTrip());
                errors.AddRange(CheckUpsert());
                errors.AddRange(CheckRejects());
                errors.AddRange(CheckRestoreOrder());
                errors.AddRange(CheckCatalog());
                errors.AddRange(CheckBatchStudents());
                errors.AddRange(CheckBatchStudentPages());
                errors.AddRange(CheckPublish());
                if (errors.Count == 0)
                {
                    Console.WriteLine("mother-group-snapshot-json-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("postgresql-9.6-17-syntax: ::jsonb only");
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
            MotherGroupConfirmList list = ListOf(
                Student("27893", "510001", "王\"甲", "1年級", true, "普通科", true, "高一甲", true, "", false, "", false),
                Student("27894", "O'Brien", "中文\\生", "未分班", false, "", false, "", false, "", false, "", false));
            string json = MotherGroupSnapshotJson.Compose("[]", list, MotherGroupSourceKind.Current, "999");
            Expect(errors, json.IndexOf("\"source_kind\":\"Current\"") >= 0 && json.IndexOf("\"source_batch_id\":null") >= 0, "目前學生來源不寫來源批次");
            Expect(errors, json.IndexOf("\"participate\":true") >= 0 && json.IndexOf("\"participate\":false") >= 0 && json.IndexOf("\"true\"") < 0 && json.IndexOf("\"false\"") < 0, "參與狀態應是 JSON boolean");
            Expect(errors, json.IndexOf("\"student_name\":\"王\\\"甲\"") >= 0 && json.IndexOf("\"student_number\":\"O'Brien\"") >= 0 && json.IndexOf("中文\\\\生") >= 0, "姓名、學號與特殊字元應可序列化");
            Expect(errors, json.IndexOf("\"grade_year\":1") >= 0 && json.IndexOf("\"grade_year\":null") >= 0, "可解析年級寫數字，無法解析寫 null");
            Expect(errors, json.IndexOf("<MotherGroupSnapshot") < 0, "JSON 不應包成 XML 快照");

            MotherGroupJsonRead read = MotherGroupSnapshotJson.Read(json);
            Expect(errors, read.Restored && read.Students.Count == 2, "JSON 應讀回兩名學生");
            StudentMotherGroup first = Find(read.Students, "27893");
            StudentMotherGroup second = Find(read.Students, "27894");
            Expect(errors, first != null && first.StudentName == "王\"甲" && first.StudentNumber == "510001" && first.GetWorking(RankKinds.Class).GroupName == "高一甲" && first.GetWorking(RankKinds.Class).Participate && !first.GetWorking(RankKinds.Tag1).Participate, "第一名學生的姓名與五種母群應一致");
            Expect(errors, second != null && second.StudentName == "中文\\生" && second.GetWorking(RankKinds.Year).GroupName == "未分班" && !second.GetWorking(RankKinds.Year).Participate, "無法解析年級的學生仍應留在快照");

            string history = MotherGroupSnapshotJson.Compose("[]", list, MotherGroupSourceKind.HistorySnapshot, "145");
            Expect(errors, history.IndexOf("\"source_kind\":\"History\"") >= 0 && history.IndexOf("\"source_batch_id\":145") >= 0 && history.IndexOf("\"source_batch_id\":\"145\"") < 0, "歷史來源批次應是 JSON 數字");
            string excel = MotherGroupSnapshotJson.Compose("[]", list, MotherGroupSourceKind.Excel, "145");
            Expect(errors, excel.IndexOf("\"source_kind\":\"Excel\"") >= 0 && excel.IndexOf("\"source_batch_id\":null") >= 0, "Excel 來源不寫虛構批次");
            return errors;
        }

        private static List<string> CheckUpsert()
        {
            List<string> errors = new List<string>();
            string existing = "[{\"extension_name\":\"OtherExtension\",\"value\":123,\"note\":\"中文\\\"引號\"},{\"extension_name\":\"MotherGroupSnapshot\",\"version\":1,\"students\":[]}]";
            MotherGroupConfirmList list = ListOf(Student("31", "S31", "新生", "2年級", true, "商科", false, "高二乙", true, "甲", false, "", false));
            string json = MotherGroupSnapshotJson.Compose(existing, list, MotherGroupSourceKind.Current, "");
            Expect(errors, Count(json, "\"extension_name\":\"MotherGroupSnapshot\"") == 1 && json.IndexOf("\"extension_name\":\"OtherExtension\"") >= 0 && json.IndexOf("\"value\":123") >= 0 && json.IndexOf("中文\\\"引號") >= 0, "其他 extension 應保留，同名快照只留一筆");
            Expect(errors, json.IndexOf("\"student_id\":31") >= 0 && json.IndexOf("\"students\":[]") < 0, "取代後應是新的學生清單");
            string again = MotherGroupSnapshotJson.Compose(json, list, MotherGroupSourceKind.Current, "");
            Expect(errors, Count(again, "\"extension_name\":\"MotherGroupSnapshot\"") == 1, "重複建立不應再追加快照");
            return errors;
        }

        private static List<string> CheckRejects()
        {
            List<string> errors = new List<string>();
            Expect(errors, MotherGroupSnapshotJson.Read("[]").Present == false && MotherGroupSnapshotJson.Read("[{\"extension_name\":\"OtherExtension\"}]").Present == false, "空陣列或其他 extension 不是母群快照");
            MotherGroupJsonRead broken = MotherGroupSnapshotJson.Read("{");
            Expect(errors, broken.Present && !broken.Restored && broken.Students.Count == 0 && broken.Message == MotherGroupSnapshotJson.InvalidDocumentMessage, "壞掉的 extension 應停止");
            string version = "[{\"extension_name\":\"MotherGroupSnapshot\",\"version\":\"1\",\"source_kind\":\"Current\",\"source_batch_id\":null,\"students\":[]}]";
            MotherGroupJsonRead badVersion = MotherGroupSnapshotJson.Read(version);
            Expect(errors, badVersion.Present && !badVersion.Restored && badVersion.Message == MotherGroupSnapshotJson.UnsupportedVersionMessage, "字串版本不應當成版本 1");
            string duplicate = "[{\"extension_name\":\"MotherGroupSnapshot\",\"version\":1,\"source_kind\":\"Current\",\"source_batch_id\":null,\"students\":[]},{\"extension_name\":\"MotherGroupSnapshot\",\"version\":1,\"source_kind\":\"Current\",\"source_batch_id\":null,\"students\":[]}]";
            MotherGroupJsonRead duplicated = MotherGroupSnapshotJson.Read(duplicate);
            Expect(errors, duplicated.Present && !duplicated.Restored && duplicated.Message == MotherGroupSnapshotJson.DuplicateSnapshotMessage, "重複快照應停止");
            string emptyStudents = SnapshotShell("\"students\":[]");
            Expect(errors, MotherGroupSnapshotJson.Read(emptyStudents).Message == MotherGroupSnapshotJson.EmptyStudentMessage, "空白學生清單應停止");
            string textFlag = SnapshotShell("\"students\":[{\"student_id\":1,\"student_number\":\"1\",\"student_name\":\"甲\",\"grade_year\":1,\"ranks\":{\"year\":{\"name\":\"1年級\",\"participate\":\"true\"},\"dept\":{\"name\":\"\",\"participate\":false},\"class\":{\"name\":\"\",\"participate\":false},\"tag1\":{\"name\":\"\",\"participate\":false},\"tag2\":{\"name\":\"\",\"participate\":false}}}]");
            Expect(errors, MotherGroupSnapshotJson.Read(textFlag).Message == MotherGroupSnapshotJson.UnreadableSnapshotMessage, "字串參與狀態應停止");
            string sameId = SnapshotShell("\"students\":[{\"student_id\":7,\"student_number\":\"7\",\"student_name\":\"甲\",\"grade_year\":1,\"ranks\":{\"year\":{\"name\":\"1年級\",\"participate\":true},\"dept\":{\"name\":\"\",\"participate\":false},\"class\":{\"name\":\"\",\"participate\":false},\"tag1\":{\"name\":\"\",\"participate\":false},\"tag2\":{\"name\":\"\",\"participate\":false}}},{\"student_id\":7,\"student_number\":\"8\",\"student_name\":\"乙\",\"grade_year\":1,\"ranks\":{\"year\":{\"name\":\"1年級\",\"participate\":true},\"dept\":{\"name\":\"\",\"participate\":false},\"class\":{\"name\":\"\",\"participate\":false},\"tag1\":{\"name\":\"\",\"participate\":false},\"tag2\":{\"name\":\"\",\"participate\":false}}}]");
            MotherGroupJsonRead same = MotherGroupSnapshotJson.Read(sameId);
            Expect(errors, same.Present && !same.Restored && same.Students.Count == 0, "重複學生識別不應回傳部分學生");
            return errors;
        }

        private static List<string> CheckRestoreOrder()
        {
            List<string> errors = new List<string>();
            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            document.AppendChild(setting);
            setting.SetAttribute("考試名稱", "學期成績");
            MotherGroupConfirmList xmlList = ListOf(Student("52", "S52", "舊生", "1年級", true, "普通科", true, "忠班", false, "", false, "", false));
            MotherGroupSnapshot.Append(setting, xmlList, MotherGroupSourceKind.HistorySnapshot, "50");
            string xml = setting.OuterXml;

            HistorySnapshotLoad fromXml = HistorySnapshotRestore.LoadStored("91", "[]", xml, "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, fromXml.Restored && fromXml.Students.Count == 1 && fromXml.Students[0].StudentName == "舊生" && fromXml.Students[0].GetWorking(RankKinds.Class).GroupName == "忠班" && !fromXml.Students[0].GetWorking(RankKinds.Class).Participate, "沒有 JSON 時應讀舊 XML");
            Expect(errors, fromXml.SettingXml == xml, "讀取舊 XML 不應改寫設定");

            HistorySnapshotLoad blocked = HistorySnapshotRestore.LoadStored("91", "{", xml, "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, !blocked.Restored && blocked.Students.Count == 0 && blocked.Message == MotherGroupSnapshotJson.InvalidDocumentMessage && blocked.SettingXml == xml, "壞掉的 JSON 不應改讀 XML");

            MotherGroupConfirmList jsonList = ListOf(Student("53", "S53", "新生", "1年級", true, "商科", true, "高一甲", true, "", false, "", false));
            string json = MotherGroupSnapshotJson.Compose("[]", jsonList, MotherGroupSourceKind.HistorySnapshot, "145");
            List<TagDefinition> tags = new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } };
            List<StudentTagLink> links = new List<StudentTagLink> { new StudentTagLink { StudentId = "53", TagId = "ex", Name = "休學" } };
            HistorySnapshotLoad excluded = HistorySnapshotRestore.LoadStored("91", json, xml, "不排名", tags, links);
            Expect(errors, excluded.Restored && excluded.Students.Count == 1 && excluded.Students[0].StudentId == "53" && excluded.Students[0].GetWorking(RankKinds.Department).GroupName == "商科" && !excluded.Students[0].GetWorking(RankKinds.Year).Participate, "有效 JSON 應優先，並套用不排名類別");
            return errors;
        }

        private static List<string> CheckCatalog()
        {
            List<string> errors = new List<string>();
            string xml = "<Setting 考試名稱=\"學期成績\"><年級>1</年級><MotherGroupSnapshot version=\"1\" /></Setting>";
            string plain = "<Setting 考試名稱=\"學期成績\"><年級>2</年級></Setting>";
            string json = "[{\"extension_name\":\"MotherGroupSnapshot\",\"version\":1,\"source_kind\":\"Current\",\"source_batch_id\":null,\"students\":[]}]";
            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("school_year");
            table.Columns.Add("semester");
            table.Columns.Add("calculation_description");
            table.Columns.Add("setting");
            table.Columns.Add("extension");
            table.Rows.Add("3", "113", "1", "舊", xml, "[]");
            table.Rows.Add("20", "113", "1", "新", xml, json);
            table.Rows.Add("8", "113", "1", "無", plain, "");
            table.Rows.Add("11", "113", "1", "壞", plain, "{");
            List<HistoryRankBatch> batches = SemesterHistoryBatchCatalog.Read(table);
            SemesterHistoryBatchCatalog.SortByIdDescending(batches);
            Expect(errors, batches.Count == 4 && batches[0].Id == "20" && batches[1].Id == "11" && batches[2].Id == "8" && batches[3].Id == "3", "歷史批次應依數字識別由大到小");
            Expect(errors, batches[0].SnapshotOrigin == HistorySnapshotOrigin.Json && batches[0].DisplayText.IndexOf("快照有(JSON)") >= 0, "JSON 優先於同列 XML");
            Expect(errors, batches[3].SnapshotOrigin == HistorySnapshotOrigin.Xml && batches[3].DisplayText.IndexOf("快照有(XML)") >= 0 && batches[3].HasVersion1Snapshot, "只有 XML 版本 1 時標成 XML");
            Expect(errors, batches[2].SnapshotOrigin == HistorySnapshotOrigin.None && batches[2].DisplayText.IndexOf("快照無") >= 0 && !batches[2].HasVersion1Snapshot, "兩種都沒有時標成快照無");
            Expect(errors, batches[1].SnapshotOrigin == HistorySnapshotOrigin.Json && batches[1].DisplayText.IndexOf("快照有(JSON)") >= 0, "壞掉的 extension 仍標成 JSON");
            return errors;
        }

        private static List<string> CheckBatchStudents()
        {
            List<string> errors = new List<string>();
            string sql = HistoryRankRestore.SelectBatchStudentSql("145");
            Expect(errors, sql.IndexOf("WHERE rbs.ref_batch_id = 145") >= 0 && sql.IndexOf("matrix_dept") >= 0 && sql.IndexOf("LEFT JOIN student") >= 0 && sql.IndexOf("ORDER BY") >= 0 && sql.IndexOf("rbs.grade_year") >= 0 && sql.IndexOf("rbs.id") >= 0, "無快照查詢應使用所選批次的排名學生列");
            Expect(errors, sql.IndexOf(';') < 0 && sql.IndexOf("INSERT") < 0 && sql.IndexOf("UPDATE") < 0 && sql.IndexOf("DELETE") < 0 && sql.IndexOf("student.status") < 0, "無快照查詢應是唯讀，且不依目前學籍排除");
            bool rejected = false;
            try
            {
                HistoryRankRestore.SelectBatchStudentSql("145 OR 1=1");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "不合法的批次識別不應組成查詢");

            DataTable table = new DataTable();
            string[] columns = new string[] { "id", "ref_batch_id", "ref_student_id", "student_number", "student_name", "grade_year", "matrix_grade", "matrix_dept", "matrix_class", "matrix_tag1", "matrix_tag2" };
            foreach (string column in columns)
                table.Columns.Add(column);
            table.Rows.Add("1", "145", "20", "200", "高年級", "2", "2年級", "普通科", "高二甲", "", "");
            table.Rows.Add("9", "145", "10", "100", "", "1", "1年級", "", "高一甲", "甲", "");
            table.Rows.Add("4", "145", "5", "500", "甲", "1", "1年級", "商科", "忠班", "", "");
            table.Rows.Add("5", "145", "5", "500", "乙", "1", "1年級", "商科", "孝班", "", "");
            List<TagDefinition> tags = new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } };
            List<StudentTagLink> links = new List<StudentTagLink> { new StudentTagLink { StudentId = "10", TagId = "ex", Name = "休學" } };
            HistoryRankLoad load = HistoryRankRestore.ReadBatchStudents("145", table, "不排名", tags, links);
            Expect(errors, load.Restored && load.Students.Count == 3 && load.Students[0].StudentId == "5" && load.Students[1].StudentId == "10" && load.Students[2].StudentId == "20", "應依年級與列識別排序，且留下沒有姓名的學生");
            StudentMotherGroup lower = load.Students[1];
            Expect(errors, lower.GetWorking(RankKinds.Year).GroupName == "1年級" && !lower.GetWorking(RankKinds.Year).Participate && lower.GetWorking(RankKinds.Department).GroupName == "" && lower.StudentName == "", "不排名類別應把有名稱的項目改成不參與，空白科別不推測");
            StudentMotherGroup conflicted = load.Students[0];
            Expect(errors, conflicted.GetWorking(RankKinds.Department).GroupName == "商科" && conflicted.GetWorking(RankKinds.Department).Participate && conflicted.GetWorking(RankKinds.Class).GroupName == "" && !conflicted.GetWorking(RankKinds.Class).Participate, "相同母群保留，衝突班名不任取");
            Expect(errors, load.Notice.IndexOf("重複列") >= 0 && load.Notice.IndexOf("空白") >= 0 && load.Notice.IndexOf("孝班") >= 0, "重複、空白與衝突應出現在提示");

            HistoryRankLoad empty = HistoryRankRestore.ReadBatchStudents("145", table.Clone(), "", new List<TagDefinition>(), new List<StudentTagLink>());
            Expect(errors, !empty.Restored && empty.Students.Count == 0 && empty.Message == HistoryRankRestore.EmptyBatchStudentMessage, "沒有排名學生列時應停止");
            return errors;
        }

        private static List<string> CheckBatchStudentPages()
        {
            List<string> errors = new List<string>();
            string first = HistoryRankRestore.SelectBatchStudentPageSql("91", null, 1);
            string next = HistoryRankRestore.SelectBatchStudentPageSql("91", 10, 1);
            string count = HistoryRankRestore.SelectBatchStudentIdSql("283");
            string groups = HistoryRankRestore.SelectBatchStudentGroupCountSql("91");
            Expect(errors, first.IndexOf("WHERE rbs.ref_batch_id = 91") >= 0 && first.IndexOf("DISTINCT ON") >= 0 && first.IndexOf("ORDER BY collapsed.cursor_id") >= 0 && first.IndexOf("LIMIT 1") >= 0 && first.IndexOf("cursor_id >") < 0 && first.IndexOf("matrix_grade") >= 0 && first.IndexOf("matrix_dept") >= 0 && first.IndexOf("matrix_class") >= 0 && first.IndexOf("matrix_tag1") >= 0 && first.IndexOf("matrix_tag2") >= 0 && first.IndexOf("THEN ''") >= 0 && first.IndexOf("MAX(") < 0, "分頁應在資料庫合併等價列，空白名稱仍留在群組鍵，並以觀察群最小 id 分頁");
            Expect(errors, next.IndexOf("collapsed.cursor_id > 10") >= 0 && next.IndexOf("LIMIT 1") >= 0 && next.IndexOf("ORDER BY collapsed.cursor_id") >= 0 && next.IndexOf("ORDER BY rbs.grade_year") < 0 && next.IndexOf("9223372036854775807") >= 0, "下一頁用觀察群最小 id 當游標，年級只用來選代表列");
            Expect(errors, count.IndexOf("count(DISTINCT rbs.id)") >= 0 && count.IndexOf("WHERE rbs.ref_batch_id = 283") >= 0 && count.IndexOf("matrix_grade") < 0, "應先確認所選批次的 id 唯一");
            Expect(errors, groups.IndexOf("AS group_count") >= 0 && groups.IndexOf("WHERE rbs.ref_batch_id = 91") >= 0 && groups.IndexOf("GROUP BY") >= 0 && groups.IndexOf("LIMIT") < 0 && groups.IndexOf("matrix_dept") >= 0, "合併筆數要與分頁使用同一觀察群");
            Expect(errors, first.IndexOf(';') < 0 && first.IndexOf("INSERT") < 0 && next.IndexOf("UPDATE") < 0 && count.IndexOf("DELETE") < 0 && groups.IndexOf("DELETE") < 0, "分頁查詢應是唯讀單句");
            bool rejected = false;
            try
            {
                HistoryRankRestore.SelectBatchStudentPageSql("91 OR 1=1", null, 1);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Expect(errors, rejected, "分頁查詢不接受不合法的批次識別");

            int pageCalls = 0;
            int pageCount;
            DataTable merged = HistoryRankRestore.ReadBatchStudentTable("91", 1, delegate (string sql)
            {
                if (sql.IndexOf("count(DISTINCT") >= 0)
                    return CountTable("2", "2");
                if (sql.IndexOf("AS group_count") >= 0)
                    return GroupCountTable("2");
                pageCalls++;
                if (pageCalls == 1)
                    return PageRow("10", "91", "5", "500", "甲", "1", "1年級", "商科", "忠班", "", "", "10");
                return PageRow("20", "91", "5", "500", "甲", "1", "1年級", "商科", "孝班", "", "", "20");
            }, out pageCount);
            HistoryRankLoad load = HistoryRankRestore.ReadBatchStudents("91", merged, "", new List<TagDefinition>(), new List<StudentTagLink>());
            SemesterHistoryStudentParticipation.KeepYearDeptClassParticipating(load);
            Expect(errors, pageCalls == 2 && pageCount == 2 && load.Restored && load.Students.Count == 1 && load.Students[0].StudentId == "5", "跨頁的同一學生應在全部頁讀完後合成一列");
            RankSlot classSlot = load.Students[0].GetWorking(RankKinds.Class);
            Expect(errors, classSlot.GroupName.Length == 0 && classSlot.ConflictNames != null && classSlot.ConflictNames.Contains("忠班") && classSlot.ConflictNames.Contains("孝班") && classSlot.Participate, "跨頁班名衝突應保留兩個名稱並沿用無 JSON 的參與預設");

            bool sawPage = false;
            bool failed = false;
            DataTable partial = null;
            try
            {
                partial = HistoryRankRestore.ReadBatchStudentTable("283", 1, delegate (string sql)
                {
                    if (sql.IndexOf("count(DISTINCT") >= 0)
                        return CountTable("2", "2");
                    if (sql.IndexOf("AS group_count") >= 0)
                        return GroupCountTable("2");
                    if (sql.IndexOf("cursor_id >") >= 0)
                        throw new InvalidOperationException("Java heap space");
                    sawPage = true;
                    return PageRow("1", "283", "8", "800", "乙", "1", "1年級", "", "高一甲", "", "", "1");
                }, out pageCount);
            }
            catch (InvalidOperationException ex)
            {
                failed = ex.Message == "Java heap space";
            }
            Expect(errors, sawPage && failed && partial == null, "中途失敗應捨棄已讀頁，不回傳部分母群");
            string message = HistoryRestoreLog.FailureMessage("283", HistoryRestoreLog.StudentStep, new InvalidOperationException("Java heap space"));
            Expect(errors, message.IndexOf("283") >= 0 && message.IndexOf(HistoryRestoreLog.StudentStep) >= 0 && message.IndexOf("確認清單") >= 0 && message.IndexOf("乙") < 0, "失敗訊息應有批次與步驟，且不含學生資料");

            bool paged = false;
            DataTable none = HistoryRankRestore.ReadBatchStudentTable("91", 1, delegate (string sql)
            {
                if (sql.IndexOf("LIMIT") >= 0)
                    paged = true;
                return CountTable("0", "0");
            }, out pageCount);
            Expect(errors, !paged && none.Rows.Count == 0 && pageCount == 0, "沒有排名學生時不送出分頁查詢");

            bool uniqueStopped = false;
            try
            {
                HistoryRankRestore.ReadBatchStudentTable("91", 1, delegate (string sql)
                {
                    if (sql.IndexOf("LIMIT") >= 0)
                        uniqueStopped = true;
                    return CountTable("2", "1");
                }, out pageCount);
            }
            catch (InvalidOperationException ex)
            {
                uniqueStopped = !uniqueStopped && ex.Message.IndexOf("不是唯一") >= 0;
            }
            Expect(errors, uniqueStopped, "id 不唯一時應停止，不以它當游標");

            int sentPages = 0;
            bool discarded = false;
            DataTable kept = HistoryRankRestore.ReadBatchStudentTable("283", 1, delegate (string sql)
            {
                if (sql.IndexOf("count(DISTINCT") >= 0)
                    return CountTable("2", "2");
                if (sql.IndexOf("AS group_count") >= 0)
                    return GroupCountTable("2");
                sentPages++;
                return PageRow("4", "283", "8", "800", "乙", "1", "1年級", "", "高一甲", "", "", "4");
            }, delegate { return sentPages < 1; }, null, out pageCount, out discarded);
            Expect(errors, discarded && kept == null && sentPages == 1, "已送出的查詢返回後應停止後續分頁並丟棄結果");

            DataTable ordered = new DataTable();
            foreach (string column in new string[] { "id", "ref_batch_id", "ref_student_id", "student_number", "student_name", "grade_year", "matrix_grade", "matrix_dept", "matrix_class", "matrix_tag1", "matrix_tag2", "cursor_id" })
                ordered.Columns.Add(column);
            ordered.Rows.Add("30", "91", "5", "500", "甲", "2", "2年級", "", "忠班", "", "", "5");
            ordered.Rows.Add("4", "91", "5", "500", "甲", "1", "1年級", "普通科", "孝班", "", "", "9");
            ordered.Rows.Add("10", "91", "8", "800", "乙", "1", "1年級", "普通科", "高一甲", "", "", "2");
            HistoryRankLoad orderedLoad = HistoryRankRestore.ReadBatchStudents("91", ordered, "", new List<TagDefinition>(), new List<StudentTagLink>());
            SemesterHistoryStudentParticipation.KeepYearDeptClassParticipating(orderedLoad);
            Expect(errors, orderedLoad.Restored && orderedLoad.Students.Count == 2 && orderedLoad.Students[0].StudentId == "5" && orderedLoad.Students[1].StudentId == "8", "學生順序仍用代表列的年級與 id，不用分頁游標");
            RankSlot dept = orderedLoad.Students[0].GetWorking(RankKinds.Department);
            Expect(errors, dept.GroupName.Length == 0 && dept.ConflictNames != null && dept.ConflictNames.Contains("") && dept.ConflictNames.Contains("普通科") && dept.Participate, "空白與非空白科別同時留下，年科班仍預設參與");

            HistoryRestoreLog log = HistoryRestoreLog.Start("91");
            log.Record(HistoryRestoreLog.PeriodStep, 3, 1, "");
            log.Record(HistoryRestoreLog.SettingStep, 4, 1, "設定字元數=12 延伸字元數=0");
            log.Fail(HistoryRestoreLog.StudentStep, 9, new InvalidOperationException("Java heap space"));
            string text = log.Text();
            Expect(errors, text.IndexOf("批次識別=91") >= 0 && text.IndexOf("步驟=" + HistoryRestoreLog.PeriodStep) >= 0 && text.IndexOf("步驟=" + HistoryRestoreLog.SettingStep) >= 0 && text.IndexOf("步驟=" + HistoryRestoreLog.TagStep) < 0 && text.IndexOf("步驟=" + HistoryRestoreLog.StudentStep) >= 0 && text.IndexOf("回傳筆數=1") >= 0 && text.IndexOf("Java heap space") >= 0 && text.IndexOf("學生姓名") < 0, "應分開記錄已完成步驟與失敗步驟");
            HistoryRestoreLog split = HistoryRestoreLog.Start("283");
            split.RecordStudentRead(1200, 40, 3, 1371);
            string splitText = split.Text();
            Expect(errors, splitText.IndexOf("步驟=" + HistoryRestoreLog.StudentStep) >= 0 && splitText.IndexOf("查詢耗時毫秒=1200") >= 0 && splitText.IndexOf("合併耗時毫秒=40") >= 0 && splitText.IndexOf("頁數=3") >= 0 && splitText.IndexOf("回傳筆數=1371") >= 0 && splitText.IndexOf("學號") < 0, "歷史學生應分開記錄查詢、合併、頁數與回傳筆數");

            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int begin = form.IndexOf("void BeginHistorySnapshotRestore");
            int show = form.IndexOf("void ShowConfirmList");
            string history = begin >= 0 && show > begin ? form.Substring(begin, show - begin) : "";
            Expect(errors, history.IndexOf("HistoryRestoreLog.PeriodStep") >= 0 && history.IndexOf("HistoryRestoreLog.SettingStep") > history.IndexOf("HistoryRestoreLog.PeriodStep") && history.IndexOf("HistoryRestoreLog.TagStep") > history.IndexOf("HistoryRestoreLog.SettingStep") && history.IndexOf("HistoryRestoreLog.StudentStep") > history.IndexOf("HistoryRestoreLog.TagStep"), "四個讀取步驟都要分開記錄");
            Expect(errors, history.IndexOf("ReleaseFailedHistoryPick") > history.IndexOf("ReadBatchStudentTable") && history.IndexOf("FailureMessage") > history.IndexOf("ReleaseFailedHistoryPick"), "分頁失敗應清除選取並顯示批次與步驟");
            Expect(errors, history.IndexOf("ClearFailedHistorySelection") < 0 && history.IndexOf("IsHistoryLoadCurrent") >= 0 && history.IndexOf("RecordStudentRead") > history.IndexOf("ReadBatchStudents") && form.IndexOf("void OnFormClosing") >= 0 && form.IndexOf("_settingsPageClosed") >= 0, "關閉設定頁應使當次載入失效，且找不到設定時不清空整份清單");
            int guard = history.IndexOf("IsHistoryLoadCurrent(generation)");
            Expect(errors, guard >= 0 && history.IndexOf("PublishConfirmRequest", guard) > guard && history.LastIndexOf("ShowConfirmList") > history.IndexOf("PublishConfirmRequest", guard), "快照成功與無快照成功都要先確認仍是當次載入");
            int frozen = history.IndexOf("PublishConfirmRequest(_frozenHistoryRequest)");
            Expect(errors, history.IndexOf("_frozenHistoryRequest = FreezeConfirmRequest()") >= 0 && history.IndexOf("LockHistoryLoadInputs()") > history.IndexOf("_frozenHistoryRequest = FreezeConfirmRequest()") && history.IndexOf("LockHistoryLoadInputs()") < history.IndexOf("RunWorkerAsync") && frozen > guard && history.IndexOf("PublishConfirmRequest(_frozenHistoryRequest)", frozen + 1) > frozen, "載入開始應鎖定條件，兩條成功路徑使用同一份確認請求");
            string periodFailure = HistoryRestoreLog.FailureMessage("283", HistoryRestoreLog.PeriodStep, "所選批次的學年度、學期與畫面上的歷史期間不一致，不會進入確認清單，也不會讀取學生。");
            Expect(errors, periodFailure.IndexOf("283") >= 0 && periodFailure.IndexOf(HistoryRestoreLog.PeriodStep) >= 0 && periodFailure.IndexOf("歷史期間不一致") >= 0, "期間不符的訊息應包含批次與步驟");
            return errors;
        }

        private static DataTable CountTable(string rowCount, string idCount)
        {
            DataTable table = new DataTable();
            table.Columns.Add("row_count");
            table.Columns.Add("id_count");
            table.Rows.Add(rowCount, idCount);
            return table;
        }

        private static DataTable GroupCountTable(string groupCount)
        {
            DataTable table = new DataTable();
            table.Columns.Add("group_count");
            table.Rows.Add(groupCount);
            return table;
        }

        private static DataTable PageRow(string id, string batchId, string studentId, string studentNumber, string studentName, string gradeYear, string yearName, string deptName, string className, string tag1, string tag2, string cursorId)
        {
            DataTable table = new DataTable();
            string[] columns = new string[] { "id", "ref_batch_id", "ref_student_id", "student_number", "student_name", "grade_year", "matrix_grade", "matrix_dept", "matrix_class", "matrix_tag1", "matrix_tag2", "cursor_id" };
            foreach (string column in columns)
                table.Columns.Add(column);
            table.Rows.Add(id, batchId, studentId, studentNumber, studentName, gradeYear, yearName, deptName, className, tag1, tag2, cursorId);
            return table;
        }

        private static List<string> CheckPublish()
        {
            List<string> errors = new List<string>();
            string formula = Formula(errors);
            if (formula.Length == 0)
                return errors;
            MotherGroupConfirmList list = ListOf(
                Student("61", "610001", "王\"甲", "1年級", true, "普通科", true, "高一甲", false, "", false, "", false),
                Student("62", "O'Brien", "無年級", "", false, "", false, "", false, "", false, "", false));
            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            document.AppendChild(setting);
            setting.SetAttribute("學年度", "115");
            setting.SetAttribute("學期", "1");
            setting.SetAttribute("考試名稱", "學期成績");
            setting.SetAttribute("不排名學生類別", "");
            setting.SetAttribute("類別一", "");
            setting.SetAttribute("類別二", "");
            XmlElement grade = document.CreateElement("年級");
            grade.InnerText = "1";
            setting.AppendChild(grade);
            XmlElement best = document.CreateElement("擇優採計成績");
            best.InnerText = "原始成績";
            setting.AppendChild(best);
            string extension = MotherGroupSnapshotJson.Compose("[{\"extension_name\":\"OtherExtension\",\"value\":123}]", list, MotherGroupSourceKind.Current, "");
            SemesterRankGradeStudents students = new SemesterRankGradeStudents();
            students.GradeYear = 1;
            students.Selects = new List<string>();
            students.Selects.Add("SELECT\n'61'::BIGINT AS student_id\n");
            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(new SemesterRankGradeStudents[] { students }, "115", "1", setting.OuterXml, extension, formula, false, true);
            string sql = publish.Sql;
            Expect(errors, publish.DatabaseExecuted == false && sql.IndexOf(';') < 0 && Count(sql, "INSERT INTO rank_batch") == 1 && sql.IndexOf("UPDATE rank_batch") < 0, "快照應在單一 INSERT，不另外更新");
            Expect(errors, sql.IndexOf("'::jsonb") >= 0 && sql.IndexOf("jsonb_path") < 0 && sql.IndexOf("JSON_TABLE") < 0 && sql.IndexOf("jsonb_set") < 0 && sql.IndexOf("MERGE") < 0 && sql.IndexOf("XMLTABLE") < 0, "extension 應使用 PostgreSQL 9.6 已有的 jsonb 轉型");
            Expect(errors, sql.IndexOf("<MotherGroupSnapshot") < 0 && sql.IndexOf("學年度=\"115\"") >= 0 && sql.IndexOf("<年級>1</年級>") >= 0 && sql.IndexOf("<擇優採計成績>原始成績</擇優採計成績>") >= 0, "setting 應保留計算設定且不含快照");
            Expect(errors, sql.IndexOf("\"extension_name\":\"OtherExtension\"") >= 0 && sql.IndexOf("\"value\":123") >= 0 && Count(sql, "\"extension_name\":\"MotherGroupSnapshot\"") == 1, "公布語句應保留其他 extension 並寫入一筆快照");
            Expect(errors, sql.IndexOf("\"student_id\":62") >= 0 && sql.IndexOf("王\\\"甲") >= 0 && sql.IndexOf("O''Brien") >= 0 && sql.IndexOf("\"participate\":false") >= 0 && sql.IndexOf("detail_count > 0") >= 0, "無排名年級的學生仍在快照，引號已跳脫，失敗檢查仍在");
            return errors;
        }

        private static string SnapshotShell(string studentsField)
        {
            return "[{\"extension_name\":\"MotherGroupSnapshot\",\"version\":1,\"source_kind\":\"Current\",\"source_batch_id\":null," + studentsField + "}]";
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

        private static MotherGroupConfirmList ListOf(params StudentMotherGroup[] students)
        {
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup student in students)
                rows.Add(new ConfirmStudentRow { Group = student });
            list.Load(rows);
            return list;
        }

        private static StudentMotherGroup Student(string id, string number, string name, string year, bool yearOn, string dept, bool deptOn, string className, bool classOn, string tag1, bool tag1On, string tag2, bool tag2On)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentNumber = number;
            student.StudentName = name;
            student.SetSourceSlots(new RankSlot[]
            {
                Slot(RankKinds.Year, year, yearOn),
                Slot(RankKinds.Department, dept, deptOn),
                Slot(RankKinds.Class, className, classOn),
                Slot(RankKinds.Tag1, tag1, tag1On),
                Slot(RankKinds.Tag2, tag2, tag2On)
            });
            return student;
        }

        private static RankSlot Slot(string rankType, string name, bool participate)
        {
            return new RankSlot { RankType = rankType, GroupName = name, Participate = participate };
        }

        private static StudentMotherGroup Find(List<StudentMotherGroup> students, string id)
        {
            foreach (StudentMotherGroup student in students)
            {
                if (student != null && student.StudentId == id)
                    return student;
            }
            return null;
        }

        private static int Count(string text, string token)
        {
            int count = 0;
            int at = 0;
            while (at >= 0 && at < text.Length)
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
