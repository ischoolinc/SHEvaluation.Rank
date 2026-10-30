using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase6Task61Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckHeadersAndValues());
                errors.AddRange(CheckStatus());
                errors.AddRange(CheckListUnchanged());
                errors.AddRange(CheckForm());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase6-task61-check: ok");
                    Console.WriteLine("database-executed: false");
                    Console.WriteLine("excel-ui-opened: false");
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

        private static List<string> CheckHeadersAndValues()
        {
            List<string> errors = new List<string>();
            Expect(errors, MotherGroupExcelExchange.ExportHeaders.Length == 13, "匯出必須是規格中的 13 欄");
            Expect(errors, MotherGroupExcelExchange.Headers.Length == 13 && object.ReferenceEquals(MotherGroupExcelExchange.ExportHeaders, MotherGroupExcelExchange.Headers), "匯入與匯出使用同一組 13 欄");
            string[] expected = new string[]
            {
                "學號", "姓名", "學生狀態", "年排名母群", "班排名母群", "科排名母群", "類別一母群", "類別二母群",
                "年排名參與", "班排名參與", "科排名參與", "類別一參與", "類別二參與"
            };
            for (int i = 0; i < expected.Length; i++)
                Expect(errors, MotherGroupExcelExchange.ExportHeaders[i] == expected[i], "欄位順序應為規格順序，第 " + (i + 1) + " 欄是 " + expected[i]);

            StudentMotherGroup adjusted = Student("6101", "00123", "王&李<甲>", "2年級", "忠\"班\"", "", "甲組", "A&B");
            adjusted.Status = 1;
            adjusted.GetWorking(RankKinds.Year).Participate = false;
            StudentMotherGroup excluded = Student("6102", "S611", "空白生", "", "", "普通科", "", "");
            excluded.Status = 16;
            foreach (string rankType in RankKinds.All)
                excluded.GetWorking(rankType).Participate = false;
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow>
            {
                new ConfirmStudentRow { Group = adjusted },
                new ConfirmStudentRow { Group = excluded }
            });

            Workbook opened = RoundTrip(list);
            Worksheet sheet = opened.Worksheets[0];
            Expect(errors, sheet.Cells.MaxDataColumn == 12, "匯出檔應只有 13 欄");
            for (int column = 0; column < expected.Length; column++)
            {
                Cell header = sheet.Cells[0, column];
                Expect(errors, header.StringValue == expected[column] && header.Type == CellValueType.IsString, "標題 " + expected[column] + " 應為文字");
            }

            Expect(errors, Text(sheet, 1, 0) == "00123" && sheet.Cells[1, 0].Type == CellValueType.IsString, "學號應以文字保留前導零");
            Expect(errors, Text(sheet, 1, 1) == "王&李<甲>" && Text(sheet, 1, 2) == "一般", "姓名與一般狀態應完整寫出");
            Expect(errors, Text(sheet, 1, 3) == "2年級" && Text(sheet, 1, 4) == "忠\"班\"" && Text(sheet, 1, 5) == "" && Text(sheet, 1, 6) == "甲組" && Text(sheet, 1, 7) == "A&B", "五種母群名稱應分開寫出，空白科別維持空白");
            Expect(errors, Text(sheet, 1, 8) == "不參與" && Text(sheet, 1, 9) == "參與" && Text(sheet, 1, 10) == "參與" && Text(sheet, 1, 11) == "參與" && Text(sheet, 1, 12) == "參與", "已調整的年排名應匯出不參與，其餘匯出參與");
            Expect(errors, Text(sheet, 2, 2) == "畢業或離校", "畢業或離校應寫中文，不寫代碼");
            Expect(errors, Text(sheet, 2, 3) == "" && Text(sheet, 2, 4) == "" && Text(sheet, 2, 5) == "普通科", "不參與學生的空白母群應保留");
            for (int column = 8; column <= 12; column++)
                Expect(errors, Text(sheet, 2, column) == "不參與", "不參與學生的五種參與都應寫成不參與");
            for (int row = 1; row <= 2; row++)
            {
                for (int column = 0; column <= 12; column++)
                {
                    Cell cell = sheet.Cells[row, column];
                    string value = cell.StringValue ?? "";
                    bool textType = cell.Type == CellValueType.IsString || (value.Length == 0 && cell.Type == CellValueType.IsNull);
                    Expect(errors, textType && (value == "參與" || value == "不參與" || column < 8), "儲存格應為文字；參與欄只允許參與或不參與");
                }
            }
            return errors;
        }

        private static List<string> CheckStatus()
        {
            List<string> errors = new List<string>();
            int[] codes = new int[] { 1, 2, 4, 8, 16, 256 };
            string[] names = new string[] { "一般", "延修", "休學", "輟學", "畢業或離校", "刪除" };
            for (int i = 0; i < codes.Length; i++)
            {
                string mapped;
                Expect(errors, MotherGroupExcelExchange.TryStatusName(codes[i], out mapped) && mapped == names[i], "狀態 " + codes[i] + " 應寫成" + names[i]);
                StudentMotherGroup student = Student("70" + i, "S" + i, names[i], "1年級", "忠班", "普通科", "", "");
                student.Status = codes[i];
                MotherGroupConfirmList list = ListOf(student);
                Expect(errors, Text(RoundTrip(list).Worksheets[0], 1, 2) == names[i], "匯出第 3 欄應是" + names[i]);
            }
            string unused;
            Expect(errors, !MotherGroupExcelExchange.TryStatusName(3, out unused) && !MotherGroupExcelExchange.TryStatusName(0, out unused), "對照表以外的狀態不應有中文名稱");

            StudentMotherGroup missing = Student("801", "0801", "缺狀態", "1年級", "忠班", "普通科", "", "");
            missing.Status = null;
            MotherGroupConfirmList missingList = ListOf(missing);
            string before = Signature(missingList);
            string blockedPath = Path.Combine(Path.GetTempPath(), "sh-eval-rank-task61-incomplete.xlsx");
            if (File.Exists(blockedPath))
                File.Delete(blockedPath);
            bool blocked = false;
            try
            {
                MotherGroupExcelExchange.Save(blockedPath, missingList, null);
            }
            catch (InvalidOperationException ex)
            {
                blocked = ex.Message.IndexOf("801") >= 0 && ex.Message.IndexOf("0801") >= 0 && ex.Message.IndexOf("一般") < 0;
            }
            Expect(errors, blocked && !File.Exists(blockedPath) && Signature(missingList) == before && missing.Status == null, "查不到狀態時不產生檔案，也不把狀態改成一般或寫回清單");

            Dictionary<string, int> lookedUp = new Dictionary<string, int>();
            lookedUp["801"] = 4;
            Workbook filled = RoundTrip(missingList, lookedUp);
            Expect(errors, Text(filled.Worksheets[0], 1, 2) == "休學" && missing.Status == null && Signature(missingList) == before, "缺值狀態只用於這一張匯出，不改確認清單");

            missing.Status = 99;
            bool illegal = false;
            try
            {
                MotherGroupExcelExchange.Create(ListOf(missing));
            }
            catch (InvalidOperationException ex)
            {
                illegal = ex.Message.IndexOf("99") >= 0 && ex.Message.IndexOf("一般") < 0;
            }
            Expect(errors, illegal, "不在六種值內的狀態應回報代碼，不寫成一般");

            string sql = MotherGroupExcelExchange.SelectCurrentStatusSql(new List<string> { "10", "11" });
            Expect(errors, sql.IndexOf("SELECT") == 0 && sql.LastIndexOf("SELECT") == 0 && sql.IndexOf(" IN (") >= 0 && sql.IndexOf(";") < 0, "缺值狀態應合成一句以學生識別查詢");
            Expect(errors, sql.IndexOf("student.id") >= 0 && sql.IndexOf("student.status") >= 0 && sql.IndexOf("student_number") < 0 && sql.IndexOf("'10'") >= 0 && sql.IndexOf("'11'") >= 0, "查詢只使用 student.id，不使用學號");

            DataTable table = StatusTable();
            table.Rows.Add("10", 8);
            table.Rows.Add("11", 2);
            Dictionary<string, int> parsed = MotherGroupExcelExchange.ReadCurrentStatuses(new List<string> { "10", "11" }, table);
            Expect(errors, parsed["10"] == 8 && parsed["11"] == 2, "同一句查詢結果應依學生識別對上狀態");

            DataTable duplicate = StatusTable();
            duplicate.Rows.Add("10", 1);
            duplicate.Rows.Add("10", 2);
            Expect(errors, ReadFails(new List<string> { "10" }, duplicate, "超過一筆"), "同一學生識別超過一筆時應回報");

            DataTable absent = StatusTable();
            absent.Rows.Add("10", 1);
            Expect(errors, ReadFails(new List<string> { "10", "12" }, absent, "12"), "查詢沒有該學生識別時應回報");

            DataTable bad = StatusTable();
            bad.Rows.Add("10", 3);
            Expect(errors, ReadFails(new List<string> { "10" }, bad, "3"), "查到對照表以外的狀態時應回報");
            Expect(errors, ReadFails(new List<string> { "10" }, null, "查詢失敗"), "沒有查詢結果時應回報失敗");

            CurrentStudentRow current = new CurrentStudentRow();
            current.StudentId = "1";
            current.StudentNumber = "00123";
            current.StudentName = "甲";
            current.IsRegular = true;
            current.RefClassId = "c1";
            current.GradeYear = 1;
            current.ClassName = "忠班";
            current.StatusValue = 1;
            List<StudentMotherGroup> built = CurrentStudentMotherGroupBuilder.Build(
                new List<CurrentStudentRow> { current },
                new List<TagDefinition>(),
                new List<StudentTagLink>(),
                new List<int> { 1 },
                "",
                "",
                "",
                new Dictionary<string, string>());
            Expect(errors, built.Count == 1 && built[0].Status == 1 && built[0].GetWorking(RankKinds.Year).GroupName == "1年級", "目前學生來源應帶入既有狀態，母群名稱仍依原本規則");

            StudentMotherGroup copied = Student("1", "00123", "甲", "1年級", "忠班", "普通科", "", "");
            copied.Status = 8;
            MotherGroupConfirmList source = ListOf(copied);
            MotherGroupConfirmList copy = source.CopyWorking();
            Expect(errors, copy.Rows[0].Group.Status == 8 && source.Rows[0].Group.Status == 8, "工作副本應保留學生狀態");

            string sourceText = File.ReadAllText("MotherGroup\\CurrentStudentMotherGroupSource.cs", Encoding.UTF8);
            Expect(errors, sourceText.IndexOf("StudentStatus.一般") >= 0 && sourceText.IndexOf("StatusValue = DatabaseStatus(student.Status)") >= 0 && sourceText.IndexOf("return 1;") >= 0 && sourceText.IndexOf("return 256;") >= 0, "目前學生來源仍只收一般學生，並把列舉轉成 student.status 代碼");
            string snapshot = File.ReadAllText("MotherGroup\\MotherGroupSnapshot.cs", Encoding.UTF8);
            Expect(errors, snapshot.IndexOf("Status") < 0, "快照不應新增學生狀態");
            return errors;
        }

        private static List<string> CheckListUnchanged()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup student = Student("6101", "S610", "王", "1年級", "忠班", "普通科", "甲", "");
            student.Status = 2;
            student.GetWorking(RankKinds.Class).ConflictNames = new List<string> { "忠班", "孝班" };
            student.GetWorking(RankKinds.Class).GroupName = "";
            student.GetWorking(RankKinds.Class).Participate = false;
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow> { new ConfirmStudentRow { Group = student } });
            string before = Signature(list);
            RoundTrip(list);
            Expect(errors, Signature(list) == before && list.Rows.Count == 1 && student.Status == 2, "匯出不應修改確認清單的母群名稱、參與、衝突名稱或狀態");
            Expect(errors, Text(RoundTrip(list).Worksheets[0], 1, 4) == "" && Text(RoundTrip(list).Worksheets[0], 1, 9) == "不參與", "未選定的衝突母群應匯出空白名稱與不參與，不另加欄位");
            return errors;
        }

        private static List<string> CheckForm()
        {
            List<string> errors = new List<string>();
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            int export = form.IndexOf("void ExportMotherGroup");
            int next = form.IndexOf("void EnsureFigureControls");
            string body = export >= 0 && next > export ? form.Substring(export, next - export) : "";
            Expect(errors, form.IndexOf("btnExport.Click") >= 0 && body.IndexOf("MotherGroupExcelExchange.Save") >= 0 && body.IndexOf("Excel (*.xlsx)|*.xlsx") >= 0, "匯出應沿用既有的 Excel 儲存方式");
            Expect(errors, body.IndexOf("INSERT") < 0 && body.IndexOf("UPDATE") < 0 && body.IndexOf("DELETE") < 0 && body.IndexOf("_confirmList = null") < 0 && body.IndexOf("_confirmList = new") < 0, "匯出不應改確認清單，也不應寫入資料庫");
            Expect(errors, Count(body, "queryHelper.Select") == 1 && body.IndexOf("SelectCurrentStatusSql") >= 0 && body.IndexOf("ReadCurrentStatuses") >= 0, "缺值狀態應只查一次，並在失敗時不儲存檔案");
            Expect(errors, body.IndexOf(".Participate") < 0 && body.IndexOf(".GroupName") < 0 && body.IndexOf(".Status =") < 0, "匯出與狀態讀取不應改母群名稱、參與或清單上的狀態");
            Expect(errors, form.IndexOf("btnImport.Click") < 0, "本次不應實作 Excel 匯入");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            string exchange = File.ReadAllText("MotherGroup\\MotherGroupExcelExchange.cs", Encoding.UTF8);
            Expect(errors, exchange.IndexOf("INSERT") < 0 && exchange.IndexOf("UPDATE") < 0 && exchange.IndexOf("DELETE") < 0 && exchange.IndexOf("rank_batch") < 0, "Excel 匯出不應含排名資料寫入");
            string publish = File.ReadAllText("MotherGroup\\SemesterRankPublishSql.cs", Encoding.UTF8);
            Expect(errors, publish.IndexOf("student.status") < 0, "公布語句不應讀取學生狀態");
            return errors;
        }

        private static Workbook RoundTrip(MotherGroupConfirmList list)
        {
            return RoundTrip(list, null);
        }

        private static Workbook RoundTrip(MotherGroupConfirmList list, IDictionary<string, int> currentStatusByStudentId)
        {
            Workbook workbook = MotherGroupExcelExchange.Create(list, currentStatusByStudentId);
            MemoryStream stream = new MemoryStream();
            workbook.Save(stream, SaveFormat.Xlsx);
            stream.Position = 0;
            return MotherGroupExcelExchange.Open(stream);
        }

        private static string Text(Worksheet sheet, int row, int column)
        {
            return sheet.Cells[row, column].StringValue ?? "";
        }

        private static StudentMotherGroup Student(string id, string number, string name, string year, string className, string department, string tag1, string tag2)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = id;
            student.StudentNumber = number;
            student.StudentName = name;
            student.SetSourceSlots(new List<RankSlot>
            {
                new RankSlot { RankType = RankKinds.Year, GroupName = year, Participate = true },
                new RankSlot { RankType = RankKinds.Department, GroupName = department, Participate = true },
                new RankSlot { RankType = RankKinds.Class, GroupName = className, Participate = true },
                new RankSlot { RankType = RankKinds.Tag1, GroupName = tag1, Participate = tag1 != "" },
                new RankSlot { RankType = RankKinds.Tag2, GroupName = tag2, Participate = tag2 != "" }
            });
            return student;
        }

        private static MotherGroupConfirmList ListOf(StudentMotherGroup student)
        {
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow> { new ConfirmStudentRow { Group = student } });
            return list;
        }

        private static DataTable StatusTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id", typeof(string));
            table.Columns.Add("student_status", typeof(int));
            return table;
        }

        private static bool ReadFails(IList<string> ids, DataTable table, string expected)
        {
            try
            {
                MotherGroupExcelExchange.ReadCurrentStatuses(ids, table);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message.IndexOf(expected) >= 0;
            }
        }

        private static int Count(string text, string value)
        {
            int count = 0;
            int from = 0;
            while (from < text.Length)
            {
                int at = text.IndexOf(value, from);
                if (at < 0)
                    break;
                count++;
                from = at + value.Length;
            }
            return count;
        }

        private static string Signature(MotherGroupConfirmList list)
        {
            StringBuilder text = new StringBuilder();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                text.Append(row.Group.StudentId).Append('|').Append(row.Group.StudentNumber).Append('|').Append(row.Group.StudentName);
                text.Append('|').Append(row.Group.Status.HasValue ? row.Group.Status.Value.ToString() : "");
                foreach (string rankType in RankKinds.All)
                {
                    RankSlot slot = row.Group.GetWorking(rankType);
                    text.Append('|').Append(slot.GroupName).Append('|').Append(slot.Participate);
                    if (slot.ConflictNames != null)
                    {
                        foreach (string conflict in slot.ConflictNames)
                            text.Append('|').Append(conflict);
                    }
                }
            }
            return text.ToString();
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
