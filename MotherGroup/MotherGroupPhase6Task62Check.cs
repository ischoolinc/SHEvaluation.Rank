using Aspose.Cells;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase6Task62Check
    {
        public static int Main(string[] args)
        {
            try
            {
                List<string> errors = new List<string>();
                errors.AddRange(CheckValidFile());
                errors.AddRange(CheckStructure());
                errors.AddRange(CheckEmptyFile());
                errors.AddRange(CheckExtraColumn());
                errors.AddRange(CheckIdentity());
                errors.AddRange(CheckStatusIdentity());
                errors.AddRange(CheckExistingListUntouched());
                errors.AddRange(CheckSqlAndBinding());
                errors.AddRange(CheckDisplayedRowNumbers());
                if (!SemesterRankWriteGate.CanWriteRankTables())
                    errors.Add("SemesterRankWriteGate 應允許寫入");
                if (errors.Count == 0)
                {
                    Console.WriteLine("phase6-task62-check: ok");
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

        private static List<string> CheckValidFile()
        {
            List<string> errors = new List<string>();
            int code;
            string name;
            Expect(errors, MotherGroupExcelExchange.TryStatusCode("休學", out code) && code == 4 && MotherGroupExcelExchange.TryStatusName(4, out name) && name == "休學", "匯入與匯出應共用同一組狀態對照");
            StudentMotherGroup source = Student("00123", "王&李<甲>", "2年級", "", "普通科", "", "A&B", false, true, true, false, true);
            MotherGroupExcelValidation validation = MotherGroupExcelImport.Validate(RoundTrip(source), Lookup(Row("91", "00123", "主檔姓名", 1)));
            Expect(errors, validation.IsValid && validation.Students.Count == 1 && validation.Issues.Count == 0, "13 欄匯出檔應可通過驗證");
            MotherGroupExcelStudent student = validation.Students[0];
            Expect(errors, student.StudentId == "91" && student.StudentNumber == "00123" && student.Status == 1 && student.ExcelName == "王&李<甲>", "學號應保留前導零，身分以學號加狀態對到的學生為準");
            Expect(errors, student.DatabaseName == "主檔姓名" && student.NameWarning.IndexOf("主檔") >= 0 && student.ExcelName != student.DatabaseName, "姓名不同時採用主檔姓名，並保留警告");
            RankSlot year = Slot(student, RankKinds.Year);
            RankSlot classSlot = Slot(student, RankKinds.Class);
            RankSlot tag1 = Slot(student, RankKinds.Tag1);
            Expect(errors, year.GroupName == "2年級" && !year.Participate, "已調整的年排名應分開讀出名稱與不參與");
            Expect(errors, classSlot.GroupName == "" && classSlot.Participate, "空白班排名應保留空白，不用其他名稱填入");
            Expect(errors, tag1.GroupName == "" && !tag1.Participate, "空白類別與不參與應保留");
            Expect(errors, Slot(student, RankKinds.Tag2).GroupName == "A&B" && Slot(student, RankKinds.Tag2).Participate, "特殊字元的類別二應原樣讀回");
            return errors;
        }

        private static List<string> CheckStructure()
        {
            List<string> errors = new List<string>();
            Workbook missingSheet = new Workbook();
            missingSheet.Worksheets[0].Name = "其他";
            MotherGroupExcelValidation noSheet = MotherGroupExcelImport.Validate(missingSheet, Lookup());
            Expect(errors, !noSheet.IsValid && noSheet.Students.Count == 0 && noSheet.Issues[0].Message.IndexOf("母群") >= 0, "找不到母群工作表時不應套用");

            Workbook shortHeader = WorkbookOf("學號", "姓名", "學生狀態");
            MotherGroupExcelValidation missing = MotherGroupExcelImport.Validate(shortHeader, Lookup());
            Expect(errors, !missing.IsValid && missing.Students.Count == 0 && missing.Issues[0].Message.IndexOf("欄位不符") >= 0, "缺欄時不應套用");

            string[] swapped = (string[])MotherGroupExcelExchange.Headers.Clone();
            string swap = swapped[2];
            swapped[2] = swapped[3];
            swapped[3] = swap;
            MotherGroupExcelValidation wrong = MotherGroupExcelImport.Validate(WorkbookOf(swapped), Lookup());
            Expect(errors, !wrong.IsValid && wrong.Students.Count == 0, "欄位順序錯誤時不應套用");

            Workbook blankNumber = ValidWorkbook();
            blankNumber.Worksheets[0].Cells[1, 0].PutValue("");
            MotherGroupExcelValidation blank = MotherGroupExcelImport.Validate(blankNumber, Lookup(Row("91", "00123", "主檔姓名", 1)));
            Expect(errors, !blank.IsValid && blank.Students.Count == 0 && HasIssue(blank, "學號是空白"), "空白學號應列出錯誤且不套用其他列");

            Workbook badParticipation = ValidWorkbook();
            badParticipation.Worksheets[0].Cells[1, 8].PutValue("是");
            MotherGroupExcelValidation participation = MotherGroupExcelImport.Validate(badParticipation, Lookup(Row("91", "00123", "主檔姓名", 1)));
            Expect(errors, !participation.IsValid && participation.Students.Count == 0 && HasIssue(participation, "不是「參與」或「不參與」"), "非法參與值應列出錯誤");
            return errors;
        }

        private static List<string> CheckEmptyFile()
        {
            List<string> errors = new List<string>();
            MotherGroupExcelValidation headersOnly = MotherGroupExcelImport.Validate(WorkbookOf(MotherGroupExcelExchange.Headers), Lookup());
            Expect(errors, !headersOnly.IsValid && headersOnly.Students.Count == 0 && HasIssue(headersOnly, "Excel 沒有可匯入的學生資料。"), "只有 13 欄標題時不應視為可匯入");

            Workbook blankRows = WorkbookOf(MotherGroupExcelExchange.Headers);
            for (int column = 0; column < 13; column++)
                blankRows.Worksheets[0].Cells[1, column].PutValue(" ");
            MotherGroupExcelValidation blank = MotherGroupExcelImport.Validate(blankRows, Lookup());
            Expect(errors, !blank.IsValid && blank.Students.Count == 0 && HasIssue(blank, "Excel 沒有可匯入的學生資料。"), "學生資料列全部空白時不應匯入");
            return errors;
        }

        private static List<string> CheckExtraColumn()
        {
            List<string> errors = new List<string>();
            Workbook headerExtra = ValidWorkbook();
            headerExtra.Worksheets[0].Cells[0, 13].PutValue("備註");
            MotherGroupExcelValidation header = MotherGroupExcelImport.Validate(headerExtra, Lookup(Row("91", "00123", "主檔姓名", 1)));
            Expect(errors, !header.IsValid && header.Students.Count == 0 && HasIssue(header, "第 1 列第 14 欄有額外資料。"), "第 14 欄標題有值時應指出位置且不套用");

            Workbook dataExtra = ValidWorkbook();
            dataExtra.Worksheets[0].Cells[1, 13].PutValue("多餘");
            MotherGroupExcelValidation data = MotherGroupExcelImport.Validate(dataExtra, Lookup(Row("91", "00123", "主檔姓名", 1)));
            Expect(errors, !data.IsValid && data.Students.Count == 0 && HasIssue(data, "第 2 列第 14 欄有額外資料。"), "第 14 欄學生資料有值時應指出位置且不套用");

            Workbook blankExtra = ValidWorkbook();
            blankExtra.Worksheets[0].Cells[0, 13].PutValue("");
            blankExtra.Worksheets[0].Cells[1, 13].PutValue(" ");
            MotherGroupExcelValidation ignored = MotherGroupExcelImport.Validate(blankExtra, Lookup(Row("91", "00123", "主檔姓名", 1)));
            Expect(errors, ignored.IsValid && ignored.Students.Count == 1 && ignored.Students[0].Slots.Count == 5, "完全空白的多餘欄位應忽略，且不讀成母群欄位");
            return errors;
        }

        private static List<string> CheckIdentity()
        {
            List<string> errors = new List<string>();
            Workbook duplicated = ValidWorkbook();
            duplicated.Worksheets[0].Cells[2, 0].PutValue("00123");
            duplicated.Worksheets[0].Cells[2, 1].PutValue("另一列");
            duplicated.Worksheets[0].Cells[2, 2].PutValue("一般");
            for (int column = 8; column <= 12; column++)
                duplicated.Worksheets[0].Cells[2, column].PutValue("不參與");
            MotherGroupExcelValidation duplicate = MotherGroupExcelImport.Validate(duplicated, Lookup(Row("91", "00123", "主檔姓名", 1)));
            Expect(errors, !duplicate.IsValid && duplicate.Students.Count == 0 && HasIssue(duplicate, "第 2 列與第 3 列重複"), "相同學號與相同狀態應列出兩個列號，不任選一列");

            MotherGroupExcelValidation missing = MotherGroupExcelImport.Validate(ValidWorkbook(), Lookup());
            Expect(errors, !missing.IsValid && missing.Students.Count == 0 && HasIssue(missing, "找不到學號"), "找不到學生應列出錯誤");

            DataTable many = Lookup(Row("91", "00123", "甲", 1), Row("92", "00123", "乙", 1));
            MotherGroupExcelValidation ambiguous = MotherGroupExcelImport.Validate(ValidWorkbook(), many);
            Expect(errors, !ambiguous.IsValid && ambiguous.Students.Count == 0 && HasIssue(ambiguous, "多位學生") && !HasStudentId(ambiguous, "91") && !HasStudentId(ambiguous, "92"), "同一學號與同一狀態有多位學生時不應任選");
            return errors;
        }

        private static List<string> CheckStatusIdentity()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup regular = Student("101", "一般生", "1年級", "忠班", "普通科", "", "", true, true, true, false, false);
            regular.Status = 1;
            StudentMotherGroup suspended = Student("101", "休學生", "1年級", "忠班", "普通科", "", "", true, true, true, false, false);
            suspended.Status = 4;
            MotherGroupExcelValidation both = MotherGroupExcelImport.Validate(
                RoundTrip(regular, suspended),
                Lookup(Row("1", "101", "一般生", 1), Row("2", "101", "休學生", 4)));
            Expect(errors, both.IsValid && both.Students.Count == 2 && both.Students[0].StudentId == "1" && both.Students[0].Status == 1 && both.Students[1].StudentId == "2" && both.Students[1].Status == 4, "相同學號、不同狀態應分別對到學生識別");

            MotherGroupExcelValidation onlyRegular = MotherGroupExcelImport.Validate(RoundTrip(regular), Lookup(Row("1", "101", "一般生", 1), Row("2", "101", "休學生", 4)));
            Expect(errors, onlyRegular.IsValid && onlyRegular.Students.Count == 1 && onlyRegular.Students[0].StudentId == "1", "學號 101、一般應對到 student.id=1");
            MotherGroupExcelValidation onlySuspended = MotherGroupExcelImport.Validate(RoundTrip(suspended), Lookup(Row("1", "101", "一般生", 1), Row("2", "101", "休學生", 4)));
            Expect(errors, onlySuspended.IsValid && onlySuspended.Students.Count == 1 && onlySuspended.Students[0].StudentId == "2", "學號 101、休學應對到 student.id=2");

            Workbook repeated = RoundTrip(regular);
            repeated.Worksheets[0].Cells[2, 0].PutValue("101");
            repeated.Worksheets[0].Cells[2, 1].PutValue("一般生");
            repeated.Worksheets[0].Cells[2, 2].PutValue("一般");
            for (int column = 8; column <= 12; column++)
                repeated.Worksheets[0].Cells[2, column].PutValue("參與");
            MotherGroupExcelValidation repeatedValidation = MotherGroupExcelImport.Validate(repeated, Lookup(Row("1", "101", "一般生", 1)));
            Expect(errors, !repeatedValidation.IsValid && repeatedValidation.Students.Count == 0 && HasIssue(repeatedValidation, "重複"), "學號 101、一般出現兩次時整份失敗");

            MotherGroupExcelValidation absent = MotherGroupExcelImport.Validate(RoundTrip(regular), Lookup(Row("2", "101", "休學生", 4)));
            Expect(errors, !absent.IsValid && absent.Students.Count == 0 && HasIssue(absent, "找不到學號"), "學號加狀態查不到學生時失敗");

            Workbook blankStatus = RoundTrip(regular);
            blankStatus.Worksheets[0].Cells[1, 2].PutValue("");
            MotherGroupExcelValidation blank = MotherGroupExcelImport.Validate(blankStatus, null);
            Expect(errors, !blank.IsValid && blank.Students.Count == 0 && HasIssue(blank, "學生狀態是空白") && !HasIssue(blank, "沒有讀到學生資料"), "學生狀態空白時應回報該列");

            Workbook unknown = RoundTrip(regular);
            unknown.Worksheets[0].Cells[1, 2].PutValue("在學");
            MotherGroupExcelValidation unknownValidation = MotherGroupExcelImport.Validate(unknown, null);
            Expect(errors, !unknownValidation.IsValid && unknownValidation.Students.Count == 0 && HasIssue(unknownValidation, "第 2 列") && HasIssue(unknownValidation, "不在對照表"), "未知的學生狀態應回報列號與內容");

            StudentMotherGroup other = Student("102", "延修生", "1年級", "忠班", "普通科", "", "", true, true, true, false, false);
            other.Status = 2;
            MotherGroupExcelValidation sameId = MotherGroupExcelImport.Validate(
                RoundTrip(regular, other),
                Lookup(Row("7", "101", "一般生", 1), Row("7", "102", "延修生", 2)));
            Expect(errors, !sameId.IsValid && sameId.Students.Count == 0 && HasIssue(sameId, "學生識別「7」"), "同一學生識別出現兩次時不建立清單");

            Workbook mixed = RoundTrip(regular, suspended);
            mixed.Worksheets[0].Cells[2, 8].PutValue("是");
            MotherGroupExcelValidation partial = MotherGroupExcelImport.Validate(mixed, Lookup(Row("1", "101", "一般生", 1), Row("2", "101", "休學生", 4)));
            Expect(errors, !partial.IsValid && partial.Students.Count == 0 && !HasStudentId(partial, "1") && HasIssue(partial, "不是「參與」或「不參與」"), "一筆有效、一筆錯誤時整份失敗，不產生部分清單");
            return errors;
        }

        private static List<string> CheckExistingListUntouched()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup existing = Student("S500", "歷史生", "舊年", "舊班", "歷史科", "舊類", "", true, true, true, true, false);
            existing.GetWorking(RankKinds.Class).ConflictNames = new List<string> { "舊班", "另一班" };
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(new List<ConfirmStudentRow> { new ConfirmStudentRow { Group = existing } });
            string before = Signature(list);
            MotherGroupExcelImport.Validate(ValidWorkbook(), Lookup());
            Expect(errors, Signature(list) == before, "匯入驗證不應修改既有確認清單或歷史母群");
            return errors;
        }

        private static List<string> CheckSqlAndBinding()
        {
            List<string> errors = new List<string>();
            string sql = MotherGroupExcelImport.SelectSql(new MotherGroupExcelIdentity[]
            {
                MotherGroupExcelIdentity.Create("00123", 1),
                MotherGroupExcelIdentity.Create("O'Brien", 4)
            });
            Expect(errors, sql.IndexOf("SELECT") == 0 && sql.LastIndexOf("SELECT") == 0 && sql.IndexOf("WHERE (student.student_number, student.status) IN (('00123', 1), ('O''Brien', 4))") >= 0, "應以學號加狀態一批查詢，並保留前導零");
            Expect(errors, sql.IndexOf("student.status AS student_status") >= 0 && sql.IndexOf("student.id::TEXT AS student_id") >= 0, "查詢應回傳學生識別、學號、姓名與狀態");
            Expect(errors, sql.IndexOf("class.class_name") < 0 && sql.IndexOf("dept.name") < 0 && sql.IndexOf("school_year") < 0, "驗證查詢不應讀取目前班級、科別或計算期間");
            Expect(errors, sql.IndexOf("INSERT") < 0 && sql.IndexOf("UPDATE") < 0 && sql.IndexOf("DELETE") < 0 && sql.IndexOf(";") < 0, "學生比對應為單一 SELECT");
            string form = File.ReadAllText("CalculateSemesterAssessmentRank.cs", Encoding.UTF8);
            string designer = File.ReadAllText("CalculateSemesterAssessmentRank.Designer.cs", Encoding.UTF8);
            int bindings = 0;
            int from = 0;
            while (true)
            {
                int at = form.IndexOf("btnExport.Click", from);
                if (at < 0)
                    break;
                bindings++;
                from = at + 1;
            }
            Expect(errors, bindings == 1 && designer.IndexOf("btnExport.Click") < 0, "學期成績排名的匯出按鈕只在建構式綁定一次，Designer 沒有再綁");
            int excel = form.IndexOf("void BeginExcelImport");
            int next = form.IndexOf("bool IsHistorySourceSelected", excel);
            string body = excel >= 0 && next > excel ? form.Substring(excel, next - excel) : "";
            int shown = body.IndexOf("ShowConfirmList");
            int failed = body.IndexOf("!validation.IsValid");
            Expect(errors, failed >= 0 && shown > failed && Count(body, "queryHelper.Select") == 1 && body.IndexOf("IdentityKeys") >= 0, "驗證失敗應先回報；學號加狀態只查一次");
            string gate = File.ReadAllText("MotherGroup\\SemesterRankWriteGate.cs", Encoding.UTF8);
            Expect(errors, gate.IndexOf("return true;") >= 0 && gate.IndexOf("return false;") < 0, "SemesterRankWriteGate 應回傳 true");
            string import = File.ReadAllText("MotherGroup\\MotherGroupExcelImport.cs", Encoding.UTF8);
            Expect(errors, import.IndexOf("INSERT") < 0 && import.IndexOf("UPDATE") < 0 && import.IndexOf("DELETE") < 0, "匯入驗證不應寫入資料庫");
            return errors;
        }

        private static List<string> CheckDisplayedRowNumbers()
        {
            List<string> errors = new List<string>();
            StudentMotherGroup first = Student("90001", "甲", "1年級", "忠班", "普通科", "", "", true, true, true, false, false);
            StudentMotherGroup second = Student("90002", "乙", "1年級", "忠班", "普通科", "", "", true, true, true, false, false);
            Workbook missingRows = RoundTrip(first, second);
            MotherGroupExcelValidation missing = MotherGroupExcelImport.Validate(missingRows, Lookup());
            string missingText = MotherGroupExcelApply.IssueText(missing);
            Expect(errors, !missing.IsValid && missing.Students.Count == 0, "查無學生時不建立可套用清單");
            Expect(errors, missingText.IndexOf("第 2 列") >= 0 && missingText.IndexOf("第 3 列") >= 0 && missingText.IndexOf("90001") >= 0 && missingText.IndexOf("90002") >= 0 && missingText.IndexOf("一般") >= 0, "查無學生的每一列都要有列號、學號與狀態");
            Expect(errors, missingText.IndexOf("第 0 列") < 0 && Count(missingText, "第 2 列") == 1 && Count(missingText, "第 3 列") == 1, "列號不可重複，也不可出現第 0 列");
            bool applied = false;
            try
            {
                MotherGroupExcelApply.CreateGroups(missing, "", new List<TagDefinition>(), new List<StudentTagLink>());
                applied = true;
            }
            catch (InvalidOperationException)
            {
            }
            Expect(errors, !applied, "驗證失敗時不得套用匯入結果");

            MotherGroupExcelValidation many = MotherGroupExcelImport.Validate(ValidWorkbook(), Lookup(Row("91", "00123", "甲", 1), Row("92", "00123", "乙", 1)));
            string manyText = MotherGroupExcelApply.IssueText(many);
            Expect(errors, !many.IsValid && many.Students.Count == 0 && manyText.IndexOf("第 2 列") >= 0 && manyText.IndexOf("對應多位學生") >= 0 && manyText.IndexOf("00123") >= 0 && Count(manyText, "第 2 列") == 1, "對應多位學生要顯示列號且不重複");

            MotherGroupExcelValidation noId = MotherGroupExcelImport.Validate(ValidWorkbook(), Lookup(Row("", "00123", "甲", 1)));
            string noIdText = MotherGroupExcelApply.IssueText(noId);
            Expect(errors, !noId.IsValid && noId.Students.Count == 0 && noIdText.IndexOf("第 2 列") >= 0 && noIdText.IndexOf("沒有識別") >= 0 && noIdText.IndexOf("00123") >= 0, "缺少學生識別要顯示列號、學號與狀態");

            Workbook other = new Workbook();
            other.Worksheets[0].Name = "其他";
            string fileText = MotherGroupExcelApply.IssueText(MotherGroupExcelImport.Validate(other, Lookup()));
            Expect(errors, fileText.IndexOf("第 0 列") < 0 && fileText.IndexOf("母群") >= 0, "沒有資料列的檔案錯誤不顯示第 0 列");

            Workbook badParticipation = RoundTrip(first);
            badParticipation.Worksheets[0].Cells[1, 8].PutValue("可能");
            MotherGroupExcelValidation participation = MotherGroupExcelImport.Validate(badParticipation, Lookup(Row("1", "90001", "甲", 1)));
            string participationText = MotherGroupExcelApply.IssueText(participation);
            Expect(errors, participationText.IndexOf("第 2 列") >= 0 && Count(participationText, "第 2 列") == 1, "已含列號的訊息不要再加一次");
            return errors;
        }

        private static Workbook RoundTrip(params StudentMotherGroup[] students)
        {
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup student in students)
                rows.Add(new ConfirmStudentRow { Group = student });
            list.Load(rows);
            Workbook created = MotherGroupExcelExchange.Create(list);
            MemoryStream stream = new MemoryStream();
            created.Save(stream, SaveFormat.Xlsx);
            stream.Position = 0;
            return MotherGroupExcelExchange.Open(stream);
        }

        private static Workbook ValidWorkbook()
        {
            return RoundTrip(Student("00123", "王&李<甲>", "2年級", "", "普通科", "", "A&B", false, true, true, false, true));
        }

        private static Workbook WorkbookOf(params string[] headers)
        {
            Workbook workbook = new Workbook();
            workbook.Worksheets[0].Name = MotherGroupExcelImport.SheetName;
            for (int i = 0; i < headers.Length; i++)
                workbook.Worksheets[0].Cells[0, i].PutValue(headers[i]);
            return workbook;
        }

        private static StudentMotherGroup Student(string number, string name, string year, string className, string department, string tag1, string tag2, bool yearOn, bool classOn, bool departmentOn, bool tag1On, bool tag2On)
        {
            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = "local";
            student.StudentNumber = number;
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

        private static RankSlot Slot(MotherGroupExcelStudent student, string rankType)
        {
            foreach (RankSlot slot in student.Slots)
            {
                if (slot.RankType == rankType)
                    return slot;
            }
            return new RankSlot();
        }

        private static DataTable Lookup(params object[][] rows)
        {
            DataTable table = new DataTable();
            table.Columns.Add("student_id");
            table.Columns.Add("student_number");
            table.Columns.Add("student_name");
            table.Columns.Add("student_status");
            if (rows != null)
            {
                foreach (object[] row in rows)
                    table.Rows.Add(row);
            }
            return table;
        }

        private static object[] Row(string id, string number, string name, int status)
        {
            return new object[] { id, number, name, status };
        }

        private static bool HasIssue(MotherGroupExcelValidation validation, string text)
        {
            foreach (MotherGroupExcelIssue issue in validation.Issues)
            {
                if (issue.Message.IndexOf(text) >= 0)
                    return true;
            }
            return false;
        }

        private static bool HasStudentId(MotherGroupExcelValidation validation, string studentId)
        {
            foreach (MotherGroupExcelStudent student in validation.Students)
            {
                if (student.StudentId == studentId)
                    return true;
            }
            return false;
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
                text.Append(row.Group.StudentNumber).Append('|').Append(row.Group.StudentName);
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
