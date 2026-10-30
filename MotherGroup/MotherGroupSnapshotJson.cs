using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class MotherGroupJsonRead
    {
        public bool Present { get; private set; }
        public bool Restored { get; private set; }
        public string Message { get; private set; }
        public List<StudentMotherGroup> Students { get; private set; }

        internal static MotherGroupJsonRead NotPresent()
        {
            MotherGroupJsonRead read = new MotherGroupJsonRead();
            read.Present = false;
            read.Restored = false;
            read.Message = "";
            read.Students = new List<StudentMotherGroup>();
            return read;
        }

        internal static MotherGroupJsonRead Fail(string message)
        {
            MotherGroupJsonRead read = new MotherGroupJsonRead();
            read.Present = true;
            read.Restored = false;
            read.Message = message ?? "";
            read.Students = new List<StudentMotherGroup>();
            return read;
        }

        internal static MotherGroupJsonRead Ok(List<StudentMotherGroup> students)
        {
            MotherGroupJsonRead read = new MotherGroupJsonRead();
            read.Present = true;
            read.Restored = true;
            read.Message = "";
            read.Students = students ?? new List<StudentMotherGroup>();
            return read;
        }
    }

    public sealed class MotherGroupExtensionMark
    {
        public bool InvalidDocument { get; private set; }
        public int SnapshotCount { get; private set; }
        public bool AnyVersion1 { get; private set; }

        internal static MotherGroupExtensionMark Create(bool invalidDocument, int snapshotCount, bool anyVersion1)
        {
            MotherGroupExtensionMark mark = new MotherGroupExtensionMark();
            mark.InvalidDocument = invalidDocument;
            mark.SnapshotCount = snapshotCount;
            mark.AnyVersion1 = anyVersion1;
            return mark;
        }
    }

    public static class MotherGroupSnapshotJson
    {
        public const string ExtensionName = "MotherGroupSnapshot";
        public const string InvalidDocumentMessage = "這筆歷史批次的 extension 不是有效的 JSON 陣列，無法還原母群快照。本次不會改用舊 XML 或排名學生資料。";
        public const string DuplicateSnapshotMessage = "這筆歷史批次的 extension 有多筆 MotherGroupSnapshot，無法還原。本次不會改用舊 XML 或排名學生資料。";
        public const string UnsupportedVersionMessage = "這筆歷史批次的母群快照版本不是 1，無法還原。本次不會改用舊 XML 或排名學生資料。";
        public const string UnreadableSnapshotMessage = "這筆歷史批次的母群快照內容不完整或無法還原。本次不會改用舊 XML 或排名學生資料。";
        public const string EmptyStudentMessage = "這筆歷史批次的母群快照沒有學生，無法還原。本次不會改用舊 XML 或排名學生資料。";

        private static readonly string[] SnapshotFields = new string[] { "extension_name", "version", "source_kind", "source_batch_id", "students" };
        private static readonly string[] StudentFields = new string[] { "student_id", "student_number", "student_name", "grade_year", "ranks" };
        private static readonly string[] RankFields = new string[] { "year", "dept", "class", "tag1", "tag2" };
        private static readonly string[] SlotFields = new string[] { "name", "participate" };

        public static string Compose(string existingExtensionJson, MotherGroupConfirmList list, MotherGroupSourceKind sourceKind, string historyBatchId)
        {
            if (list == null)
                throw new InvalidOperationException("沒有確認清單，不建立母群快照。");
            JsonNode root = ParseExistingArray(existingExtensionJson);
            JsonNode snapshot = BuildSnapshot(list, sourceKind, historyBatchId);
            List<JsonNode> items = new List<JsonNode>();
            bool replaced = false;
            foreach (JsonNode item in root.Items)
            {
                if (IsSnapshot(item))
                {
                    if (!replaced)
                    {
                        items.Add(snapshot);
                        replaced = true;
                    }
                    continue;
                }
                items.Add(item);
            }
            if (!replaced)
                items.Add(snapshot);
            root.Items = items;
            return Write(root);
        }

        public static MotherGroupJsonRead Read(string extensionJson)
        {
            if (string.IsNullOrWhiteSpace(extensionJson))
                return MotherGroupJsonRead.NotPresent();
            JsonNode root;
            if (!TryParse(extensionJson, out root) || root == null || root.Kind != "array")
                return MotherGroupJsonRead.Fail(InvalidDocumentMessage);

            List<JsonNode> snapshots = new List<JsonNode>();
            foreach (JsonNode item in root.Items)
            {
                if (IsSnapshot(item))
                    snapshots.Add(item);
            }
            if (snapshots.Count == 0)
                return MotherGroupJsonRead.NotPresent();
            if (snapshots.Count != 1)
                return MotherGroupJsonRead.Fail(DuplicateSnapshotMessage);

            JsonNode snapshot = snapshots[0];
            if (snapshot.DuplicateName || !HasExactFields(snapshot, SnapshotFields))
                return MotherGroupJsonRead.Fail(UnreadableSnapshotMessage);
            JsonNode version = Member(snapshot, "version");
            long versionNumber;
            if (version == null || !TryCanonicalInteger(version, out versionNumber))
                return MotherGroupJsonRead.Fail(UnsupportedVersionMessage);
            if (versionNumber != 1)
                return MotherGroupJsonRead.Fail(UnsupportedVersionMessage);

            JsonNode sourceKind = Member(snapshot, "source_kind");
            JsonNode sourceBatch = Member(snapshot, "source_batch_id");
            if (sourceKind == null || sourceKind.Kind != "string" || sourceBatch == null)
                return MotherGroupJsonRead.Fail(UnreadableSnapshotMessage);
            if (sourceKind.Text != "Current" && sourceKind.Text != "History" && sourceKind.Text != "Excel")
                return MotherGroupJsonRead.Fail(UnreadableSnapshotMessage);
            long batchId;
            bool batchIsNumber = TryCanonicalInteger(sourceBatch, out batchId);
            if (sourceKind.Text == "History")
            {
                if (!batchIsNumber || batchId <= 0)
                    return MotherGroupJsonRead.Fail(UnreadableSnapshotMessage);
            }
            else if (sourceBatch.Kind != "null")
            {
                return MotherGroupJsonRead.Fail(UnreadableSnapshotMessage);
            }

            JsonNode students = Member(snapshot, "students");
            if (students == null || students.Kind != "array")
                return MotherGroupJsonRead.Fail(UnreadableSnapshotMessage);
            if (students.Items.Count == 0)
                return MotherGroupJsonRead.Fail(EmptyStudentMessage);

            List<StudentMotherGroup> parsed = new List<StudentMotherGroup>();
            Dictionary<string, bool> seen = new Dictionary<string, bool>();
            foreach (JsonNode studentNode in students.Items)
            {
                StudentMotherGroup student = ReadStudent(studentNode);
                if (student == null || seen.ContainsKey(student.StudentId))
                    return MotherGroupJsonRead.Fail(UnreadableSnapshotMessage);
                seen.Add(student.StudentId, true);
                parsed.Add(student);
            }
            return MotherGroupJsonRead.Ok(parsed);
        }

        public static MotherGroupExtensionMark Describe(string extensionJson)
        {
            if (string.IsNullOrWhiteSpace(extensionJson))
                return MotherGroupExtensionMark.Create(false, 0, false);
            JsonNode root;
            if (!TryParse(extensionJson, out root) || root == null || root.Kind != "array")
                return MotherGroupExtensionMark.Create(true, 0, false);
            int count = 0;
            bool version1 = false;
            foreach (JsonNode item in root.Items)
            {
                if (!IsSnapshot(item))
                    continue;
                count++;
                JsonNode version = Member(item, "version");
                long versionNumber;
                if (version != null && TryCanonicalInteger(version, out versionNumber) && versionNumber == 1)
                    version1 = true;
            }
            return MotherGroupExtensionMark.Create(false, count, version1);
        }

        private static JsonNode BuildSnapshot(MotherGroupConfirmList list, MotherGroupSourceKind sourceKind, string historyBatchId)
        {
            string kind = SourceKindText(sourceKind);
            JsonNode batchValue = NullNode();
            if (sourceKind == MotherGroupSourceKind.HistorySnapshot)
            {
                long batchId;
                if (!TryCanonicalIntegerText(historyBatchId == null ? "" : historyBatchId.Trim(), out batchId))
                    throw new InvalidOperationException("歷史母群快照缺少有效的來源批次識別，不建立母群快照。");
                batchValue = NumberNode(batchId.ToString(CultureInfo.InvariantCulture));
            }

            JsonNode students = ArrayNode();
            Dictionary<string, bool> seen = new Dictionary<string, bool>();
            foreach (ConfirmStudentRow row in list.Rows)
            {
                if (row == null || row.Group == null)
                    throw new InvalidOperationException("確認清單有缺少學生識別的列，不建立母群快照。");
                long studentId;
                string idText = row.Group.StudentId == null ? "" : row.Group.StudentId.Trim();
                if (!TryCanonicalIntegerText(idText, out studentId))
                    throw new InvalidOperationException("確認清單的學生識別不是正整數，不建立母群快照：" + row.Group.StudentId);
                if (seen.ContainsKey(studentId.ToString(CultureInfo.InvariantCulture)))
                    throw new InvalidOperationException("確認清單的學生識別重複，不建立母群快照：" + row.Group.StudentId);
                foreach (string rankType in RankKinds.All)
                {
                    if (row.Group.GetWorking(rankType) == null)
                        throw new InvalidOperationException("確認清單缺少排名母群，不建立母群快照：" + row.Group.StudentId);
                }
                seen.Add(studentId.ToString(CultureInfo.InvariantCulture), true);
                students.Items.Add(StudentNode(row.Group, studentId));
            }
            if (students.Items.Count == 0)
                throw new InvalidOperationException("確認清單沒有學生，不建立母群快照。");

            JsonNode snapshot = ObjectNode();
            Add(snapshot, "extension_name", StringNode(ExtensionName));
            Add(snapshot, "version", NumberNode("1"));
            Add(snapshot, "source_kind", StringNode(kind));
            Add(snapshot, "source_batch_id", batchValue);
            Add(snapshot, "students", students);
            return snapshot;
        }

        private static JsonNode StudentNode(StudentMotherGroup group, long studentId)
        {
            string yearName = group.GetWorking(RankKinds.Year).GroupName ?? "";
            int gradeYear;
            JsonNode grade = SemesterRankSql.TryParseGradeYear(yearName, out gradeYear)
                ? NumberNode(gradeYear.ToString(CultureInfo.InvariantCulture))
                : NullNode();
            JsonNode ranks = ObjectNode();
            Add(ranks, "year", SlotNode(group.GetWorking(RankKinds.Year)));
            Add(ranks, "dept", SlotNode(group.GetWorking(RankKinds.Department)));
            Add(ranks, "class", SlotNode(group.GetWorking(RankKinds.Class)));
            Add(ranks, "tag1", SlotNode(group.GetWorking(RankKinds.Tag1)));
            Add(ranks, "tag2", SlotNode(group.GetWorking(RankKinds.Tag2)));

            JsonNode student = ObjectNode();
            Add(student, "student_id", NumberNode(studentId.ToString(CultureInfo.InvariantCulture)));
            Add(student, "student_number", StringNode(group.StudentNumber ?? ""));
            Add(student, "student_name", StringNode(group.StudentName ?? ""));
            Add(student, "grade_year", grade);
            Add(student, "ranks", ranks);
            return student;
        }

        private static JsonNode SlotNode(RankSlot slot)
        {
            JsonNode node = ObjectNode();
            Add(node, "name", StringNode(slot.GroupName ?? ""));
            Add(node, "participate", BoolNode(slot.Participate));
            return node;
        }

        private static StudentMotherGroup ReadStudent(JsonNode node)
        {
            if (node == null || node.Kind != "object" || node.DuplicateName || !HasExactFields(node, StudentFields))
                return null;
            JsonNode idNode = Member(node, "student_id");
            long studentId;
            if (!TryCanonicalInteger(idNode, out studentId) || studentId <= 0)
                return null;
            JsonNode number = Member(node, "student_number");
            JsonNode name = Member(node, "student_name");
            JsonNode grade = Member(node, "grade_year");
            JsonNode ranks = Member(node, "ranks");
            if (number == null || number.Kind != "string" || name == null || name.Kind != "string" || grade == null || ranks == null)
                return null;
            long gradeYear;
            if (grade.Kind != "null" && (!TryCanonicalInteger(grade, out gradeYear) || gradeYear <= 0 || gradeYear > int.MaxValue))
                return null;
            if (ranks.Kind != "object" || ranks.DuplicateName || !HasExactFields(ranks, RankFields))
                return null;

            List<RankSlot> slots = new List<RankSlot>();
            if (!TrySlot(ranks, "year", RankKinds.Year, slots))
                return null;
            if (!TrySlot(ranks, "dept", RankKinds.Department, slots))
                return null;
            if (!TrySlot(ranks, "class", RankKinds.Class, slots))
                return null;
            if (!TrySlot(ranks, "tag1", RankKinds.Tag1, slots))
                return null;
            if (!TrySlot(ranks, "tag2", RankKinds.Tag2, slots))
                return null;

            StudentMotherGroup student = new StudentMotherGroup();
            student.StudentId = studentId.ToString(CultureInfo.InvariantCulture);
            student.StudentNumber = number.Text ?? "";
            student.StudentName = name.Text ?? "";
            student.SetSourceSlots(slots);
            return student;
        }

        private static bool TrySlot(JsonNode ranks, string key, string rankType, List<RankSlot> slots)
        {
            JsonNode slot = Member(ranks, key);
            if (slot == null || slot.Kind != "object" || slot.DuplicateName || !HasExactFields(slot, SlotFields))
                return false;
            JsonNode name = Member(slot, "name");
            JsonNode participate = Member(slot, "participate");
            if (name == null || name.Kind != "string" || participate == null || participate.Kind != "bool")
                return false;
            slots.Add(new RankSlot
            {
                RankType = rankType,
                GroupName = name.Text ?? "",
                Participate = participate.Boolean
            });
            return true;
        }

        private static string SourceKindText(MotherGroupSourceKind sourceKind)
        {
            if (sourceKind == MotherGroupSourceKind.Current)
                return "Current";
            if (sourceKind == MotherGroupSourceKind.HistorySnapshot)
                return "History";
            if (sourceKind == MotherGroupSourceKind.Excel)
                return "Excel";
            throw new ArgumentException("未知的母群來源：" + sourceKind);
        }

        private static JsonNode ParseExistingArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return ArrayNode();
            JsonNode root;
            if (!TryParse(json, out root) || root == null || root.Kind != "array")
                throw new InvalidOperationException("既有 extension 不是 JSON 陣列，不建立母群快照。");
            return root;
        }

        private static bool IsSnapshot(JsonNode node)
        {
            if (node == null || node.Kind != "object")
                return false;
            foreach (JsonMember member in node.Members)
            {
                if (member.Name == "extension_name" && member.Value != null && member.Value.Kind == "string" && member.Value.Text == ExtensionName)
                    return true;
            }
            return false;
        }

        private static bool HasExactFields(JsonNode node, string[] fields)
        {
            if (node == null || node.Members == null || node.Members.Count != fields.Length)
                return false;
            foreach (string field in fields)
            {
                if (CountName(node, field) != 1)
                    return false;
            }
            return true;
        }

        private static int CountName(JsonNode node, string name)
        {
            int count = 0;
            foreach (JsonMember member in node.Members)
            {
                if (member.Name == name)
                    count++;
            }
            return count;
        }

        private static JsonNode Member(JsonNode node, string name)
        {
            if (node == null || node.Members == null)
                return null;
            foreach (JsonMember member in node.Members)
            {
                if (member.Name == name)
                    return member.Value;
            }
            return null;
        }

        private static bool TryCanonicalInteger(JsonNode node, out long value)
        {
            value = 0;
            if (node == null || node.Kind != "number")
                return false;
            return TryCanonicalIntegerText(node.Text, out value);
        }

        private static bool TryCanonicalIntegerText(string text, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text))
                return false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                    return false;
            }
            if (text.Length > 1 && text[0] == '0')
                return false;
            if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value))
                return false;
            return value > 0 && value.ToString(CultureInfo.InvariantCulture) == text;
        }

        private static bool TryParse(string json, out JsonNode node)
        {
            node = null;
            try
            {
                JsonCursor cursor = new JsonCursor(json);
                node = cursor.ParseValue();
                cursor.SkipSpace();
                if (!cursor.End)
                    return false;
                return node != null;
            }
            catch (JsonParseException)
            {
                node = null;
                return false;
            }
        }

        private static string Write(JsonNode node)
        {
            StringBuilder text = new StringBuilder();
            WriteNode(text, node);
            return text.ToString();
        }

        private static void WriteNode(StringBuilder text, JsonNode node)
        {
            if (node == null || node.Kind == "null")
            {
                text.Append("null");
                return;
            }
            if (node.Kind == "bool")
            {
                text.Append(node.Boolean ? "true" : "false");
                return;
            }
            if (node.Kind == "number")
            {
                text.Append(node.Text);
                return;
            }
            if (node.Kind == "string")
            {
                WriteString(text, node.Text ?? "");
                return;
            }
            if (node.Kind == "array")
            {
                text.Append('[');
                for (int i = 0; i < node.Items.Count; i++)
                {
                    if (i > 0)
                        text.Append(',');
                    WriteNode(text, node.Items[i]);
                }
                text.Append(']');
                return;
            }
            text.Append('{');
            for (int i = 0; i < node.Members.Count; i++)
            {
                if (i > 0)
                    text.Append(',');
                WriteString(text, node.Members[i].Name ?? "");
                text.Append(':');
                WriteNode(text, node.Members[i].Value);
            }
            text.Append('}');
        }

        private static void WriteString(StringBuilder text, string value)
        {
            text.Append('"');
            foreach (char ch in value)
            {
                if (ch == '"' || ch == '\\')
                {
                    text.Append('\\');
                    text.Append(ch);
                }
                else if (ch == '\b')
                {
                    text.Append("\\b");
                }
                else if (ch == '\f')
                {
                    text.Append("\\f");
                }
                else if (ch == '\n')
                {
                    text.Append("\\n");
                }
                else if (ch == '\r')
                {
                    text.Append("\\r");
                }
                else if (ch == '\t')
                {
                    text.Append("\\t");
                }
                else if (ch < ' ')
                {
                    text.Append("\\u");
                    text.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    text.Append(ch);
                }
            }
            text.Append('"');
        }

        private static JsonNode ArrayNode()
        {
            JsonNode node = new JsonNode();
            node.Kind = "array";
            node.Items = new List<JsonNode>();
            node.Members = new List<JsonMember>();
            return node;
        }

        private static JsonNode ObjectNode()
        {
            JsonNode node = new JsonNode();
            node.Kind = "object";
            node.Items = new List<JsonNode>();
            node.Members = new List<JsonMember>();
            return node;
        }

        private static JsonNode StringNode(string value)
        {
            JsonNode node = new JsonNode();
            node.Kind = "string";
            node.Text = value ?? "";
            return node;
        }

        private static JsonNode NumberNode(string lexeme)
        {
            JsonNode node = new JsonNode();
            node.Kind = "number";
            node.Text = lexeme;
            return node;
        }

        private static JsonNode BoolNode(bool value)
        {
            JsonNode node = new JsonNode();
            node.Kind = "bool";
            node.Boolean = value;
            return node;
        }

        private static JsonNode NullNode()
        {
            JsonNode node = new JsonNode();
            node.Kind = "null";
            return node;
        }

        private static void Add(JsonNode node, string name, JsonNode value)
        {
            node.Members.Add(new JsonMember { Name = name, Value = value });
        }

        private sealed class JsonNode
        {
            public string Kind;
            public bool Boolean;
            public string Text;
            public List<JsonNode> Items;
            public List<JsonMember> Members;
            public bool DuplicateName;
        }

        private sealed class JsonMember
        {
            public string Name;
            public JsonNode Value;
        }

        private sealed class JsonParseException : Exception
        {
        }

        private sealed class JsonCursor
        {
            private readonly string _text;
            private int _index;

            public JsonCursor(string text)
            {
                _text = text ?? "";
            }

            public bool End
            {
                get { return _index >= _text.Length; }
            }

            public JsonNode ParseValue()
            {
                SkipSpace();
                if (End)
                    throw new JsonParseException();
                char ch = _text[_index];
                if (ch == '{')
                    return ParseObject();
                if (ch == '[')
                    return ParseArray();
                if (ch == '"')
                    return StringNode(ParseString());
                if (ch == 't')
                    return BoolNode(ParseLiteral("true"));
                if (ch == 'f')
                    return BoolNode(!ParseFalse());
                if (ch == 'n')
                {
                    ParseLiteral("null");
                    return NullNode();
                }
                return NumberNode(ParseNumber());
            }

            public void SkipSpace()
            {
                while (!End)
                {
                    char ch = _text[_index];
                    if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n')
                        return;
                    _index++;
                }
            }

            private JsonNode ParseObject()
            {
                Expect('{');
                JsonNode node = ObjectNode();
                SkipSpace();
                if (Peek('}'))
                {
                    _index++;
                    return node;
                }
                while (true)
                {
                    SkipSpace();
                    string name = ParseString();
                    SkipSpace();
                    Expect(':');
                    JsonNode value = ParseValue();
                    if (CountName(node, name) > 0)
                        node.DuplicateName = true;
                    Add(node, name, value);
                    SkipSpace();
                    if (Peek('}'))
                    {
                        _index++;
                        return node;
                    }
                    Expect(',');
                }
            }

            private JsonNode ParseArray()
            {
                Expect('[');
                JsonNode node = ArrayNode();
                SkipSpace();
                if (Peek(']'))
                {
                    _index++;
                    return node;
                }
                while (true)
                {
                    node.Items.Add(ParseValue());
                    SkipSpace();
                    if (Peek(']'))
                    {
                        _index++;
                        return node;
                    }
                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                StringBuilder text = new StringBuilder();
                while (!End)
                {
                    char ch = _text[_index++];
                    if (ch == '"')
                        return text.ToString();
                    if (ch == '\\')
                    {
                        if (End)
                            throw new JsonParseException();
                        char escaped = _text[_index++];
                        if (escaped == '"' || escaped == '\\' || escaped == '/')
                            text.Append(escaped);
                        else if (escaped == 'b')
                            text.Append('\b');
                        else if (escaped == 'f')
                            text.Append('\f');
                        else if (escaped == 'n')
                            text.Append('\n');
                        else if (escaped == 'r')
                            text.Append('\r');
                        else if (escaped == 't')
                            text.Append('\t');
                        else if (escaped == 'u')
                            text.Append(ParseHex());
                        else
                            throw new JsonParseException();
                    }
                    else if (ch < ' ')
                    {
                        throw new JsonParseException();
                    }
                    else
                    {
                        text.Append(ch);
                    }
                }
                throw new JsonParseException();
            }

            private char ParseHex()
            {
                if (_index + 4 > _text.Length)
                    throw new JsonParseException();
                int value = 0;
                for (int i = 0; i < 4; i++)
                {
                    char ch = _text[_index++];
                    int digit;
                    if (ch >= '0' && ch <= '9')
                        digit = ch - '0';
                    else if (ch >= 'a' && ch <= 'f')
                        digit = ch - 'a' + 10;
                    else if (ch >= 'A' && ch <= 'F')
                        digit = ch - 'A' + 10;
                    else
                        throw new JsonParseException();
                    value = (value << 4) + digit;
                }
                return (char)value;
            }

            private string ParseNumber()
            {
                int start = _index;
                if (Peek('-'))
                    _index++;
                if (End || _text[_index] < '0' || _text[_index] > '9')
                    throw new JsonParseException();
                if (_text[_index] == '0')
                {
                    _index++;
                }
                else
                {
                    while (!End && _text[_index] >= '0' && _text[_index] <= '9')
                        _index++;
                }
                if (!End && _text[_index] == '.')
                {
                    _index++;
                    if (End || _text[_index] < '0' || _text[_index] > '9')
                        throw new JsonParseException();
                    while (!End && _text[_index] >= '0' && _text[_index] <= '9')
                        _index++;
                }
                if (!End && (_text[_index] == 'e' || _text[_index] == 'E'))
                {
                    _index++;
                    if (!End && (_text[_index] == '+' || _text[_index] == '-'))
                        _index++;
                    if (End || _text[_index] < '0' || _text[_index] > '9')
                        throw new JsonParseException();
                    while (!End && _text[_index] >= '0' && _text[_index] <= '9')
                        _index++;
                }
                return _text.Substring(start, _index - start);
            }

            private bool ParseFalse()
            {
                ParseLiteral("false");
                return true;
            }

            private bool ParseLiteral(string word)
            {
                if (_index + word.Length > _text.Length || _text.Substring(_index, word.Length) != word)
                    throw new JsonParseException();
                _index += word.Length;
                return true;
            }

            private void Expect(char ch)
            {
                if (End || _text[_index] != ch)
                    throw new JsonParseException();
                _index++;
            }

            private bool Peek(char ch)
            {
                return !End && _text[_index] == ch;
            }
        }
    }
}
