using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterRankStudentJson
    {
        public static SemesterRankStudentInput CreateInput(StudentMotherGroup group, int gradeYear)
        {
            if (group == null)
                throw new InvalidOperationException("沒有學生，不建立排名學生清單。");
            string text = group.StudentId == null ? "" : group.StudentId.Trim();
            long studentId;
            if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out studentId) || studentId <= 0 || studentId.ToString(CultureInfo.InvariantCulture) != text)
                throw new InvalidOperationException("學生識別不是正整數，不建立排名學生清單：" + text);
            RankSlot year = group.GetWorking(RankKinds.Year);
            RankSlot dept = group.GetWorking(RankKinds.Department);
            RankSlot classSlot = group.GetWorking(RankKinds.Class);
            RankSlot tag1 = group.GetWorking(RankKinds.Tag1);
            RankSlot tag2 = group.GetWorking(RankKinds.Tag2);
            SemesterRankStudentInput input = new SemesterRankStudentInput();
            input.StudentId = studentId;
            input.StudentName = group.StudentName ?? "";
            input.GradeYear = gradeYear;
            input.DeptName = dept.GroupName ?? "";
            input.ClassName = classSlot.GroupName ?? "";
            input.Tag1 = tag1.GroupName ?? "";
            input.Tag2 = tag2.GroupName ?? "";
            input.ParticipateYear = year.Participate;
            input.ParticipateDept = dept.Participate;
            input.ParticipateClass = classSlot.Participate;
            input.ParticipateTag1 = tag1.Participate;
            input.ParticipateTag2 = tag2.Participate;
            return input;
        }

        public static string TryCompose(IList<SemesterRankGradeStudents> grades)
        {
            if (grades == null)
                return null;
            bool any = false;
            foreach (SemesterRankGradeStudents grade in grades)
            {
                if (grade != null && grade.Students != null && grade.Students.Count > 0)
                    any = true;
            }
            if (!any)
                return null;
            foreach (SemesterRankGradeStudents grade in grades)
            {
                if (grade == null || grade.Students == null || grade.Students.Count == 0)
                    throw new InvalidOperationException("排名學生清單混用 JSON 與舊 SELECT，不建立公布語句。");
            }
            StringBuilder json = new StringBuilder();
            json.Append('[');
            bool first = true;
            foreach (SemesterRankGradeStudents grade in grades)
            {
                foreach (SemesterRankStudentInput student in grade.Students)
                {
                    if (student == null)
                        throw new InvalidOperationException("排名學生是空白，不建立公布語句。");
                    if (!first)
                        json.Append(',');
                    first = false;
                    WriteStudent(json, student);
                }
            }
            json.Append(']');
            return json.ToString();
        }

        public static List<SemesterRankStudentInput> Read(string json)
        {
            Parser parser = new Parser(json ?? "");
            parser.Skip();
            if (!parser.Take('['))
                throw new InvalidOperationException("學生清單 JSON 不是陣列。");
            List<SemesterRankStudentInput> students = new List<SemesterRankStudentInput>();
            parser.Skip();
            if (parser.Take(']'))
                return students;
            while (true)
            {
                students.Add(ReadStudent(parser));
                parser.Skip();
                if (parser.Take(']'))
                    break;
                if (!parser.Take(','))
                    throw new InvalidOperationException("學生清單 JSON 缺少逗號。");
                parser.Skip();
            }
            parser.Skip();
            if (!parser.End)
                throw new InvalidOperationException("學生清單 JSON 後面還有內容。");
            return students;
        }

        private static void WriteStudent(StringBuilder json, SemesterRankStudentInput student)
        {
            json.Append('{');
            WriteNumber(json, "student_id", student.StudentId);
            json.Append(',');
            WriteString(json, "student_name", student.StudentName);
            json.Append(',');
            WriteNumber(json, "rank_grade_year", student.GradeYear);
            json.Append(',');
            WriteString(json, "rank_dept_name", student.DeptName);
            json.Append(',');
            WriteString(json, "rank_class_name", student.ClassName);
            json.Append(',');
            WriteString(json, "rank_tag1", student.Tag1);
            json.Append(',');
            WriteString(json, "rank_tag2", student.Tag2);
            json.Append(',');
            WriteBool(json, "participate_year", student.ParticipateYear);
            json.Append(',');
            WriteBool(json, "participate_dept", student.ParticipateDept);
            json.Append(',');
            WriteBool(json, "participate_class", student.ParticipateClass);
            json.Append(',');
            WriteBool(json, "participate_tag1", student.ParticipateTag1);
            json.Append(',');
            WriteBool(json, "participate_tag2", student.ParticipateTag2);
            json.Append('}');
        }

        private static void WriteNumber(StringBuilder json, string name, long value)
        {
            json.Append('"').Append(name).Append('"').Append(':');
            json.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void WriteBool(StringBuilder json, string name, bool value)
        {
            json.Append('"').Append(name).Append('"').Append(':');
            json.Append(value ? "true" : "false");
        }

        private static void WriteString(StringBuilder json, string name, string value)
        {
            json.Append('"').Append(name).Append('"').Append(':');
            json.Append('"');
            string text = value ?? "";
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '"' || ch == '\\')
                {
                    json.Append('\\').Append(ch);
                }
                else if (ch == '\n')
                {
                    json.Append("\\n");
                }
                else if (ch == '\r')
                {
                    json.Append("\\r");
                }
                else if (ch == '\t')
                {
                    json.Append("\\t");
                }
                else if (ch < ' ')
                {
                    json.Append("\\u").Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                }
                else
                {
                    json.Append(ch);
                }
            }
            json.Append('"');
        }

        private static SemesterRankStudentInput ReadStudent(Parser parser)
        {
            parser.Skip();
            if (!parser.Take('{'))
                throw new InvalidOperationException("學生清單 JSON 的學生不是物件。");
            SemesterRankStudentInput student = new SemesterRankStudentInput();
            bool any = false;
            parser.Skip();
            if (!parser.Take('}'))
            {
                while (true)
                {
                    any = true;
                    string name = parser.ReadString();
                    parser.Skip();
                    if (!parser.Take(':'))
                        throw new InvalidOperationException("學生清單 JSON 缺少冒號。");
                    parser.Skip();
                    Assign(student, name, parser);
                    parser.Skip();
                    if (parser.Take('}'))
                        break;
                    if (!parser.Take(','))
                        throw new InvalidOperationException("學生清單 JSON 缺少逗號。");
                    parser.Skip();
                }
            }
            if (!any)
                throw new InvalidOperationException("學生清單 JSON 沒有欄位。");
            return student;
        }

        private static void Assign(SemesterRankStudentInput student, string name, Parser parser)
        {
            if (name == "student_id")
                student.StudentId = parser.ReadLong();
            else if (name == "student_name")
                student.StudentName = parser.ReadString();
            else if (name == "rank_grade_year")
                student.GradeYear = (int)parser.ReadLong();
            else if (name == "rank_dept_name")
                student.DeptName = parser.ReadString();
            else if (name == "rank_class_name")
                student.ClassName = parser.ReadString();
            else if (name == "rank_tag1")
                student.Tag1 = parser.ReadString();
            else if (name == "rank_tag2")
                student.Tag2 = parser.ReadString();
            else if (name == "participate_year")
                student.ParticipateYear = parser.ReadBool();
            else if (name == "participate_dept")
                student.ParticipateDept = parser.ReadBool();
            else if (name == "participate_class")
                student.ParticipateClass = parser.ReadBool();
            else if (name == "participate_tag1")
                student.ParticipateTag1 = parser.ReadBool();
            else if (name == "participate_tag2")
                student.ParticipateTag2 = parser.ReadBool();
            else
                throw new InvalidOperationException("學生清單 JSON 有未知欄位：" + name);
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _at;

            public Parser(string text)
            {
                _text = text ?? "";
            }

            public bool End
            {
                get { return _at >= _text.Length; }
            }

            public void Skip()
            {
                while (_at < _text.Length && char.IsWhiteSpace(_text[_at]))
                    _at++;
            }

            public bool Take(char ch)
            {
                if (_at < _text.Length && _text[_at] == ch)
                {
                    _at++;
                    return true;
                }
                return false;
            }

            public long ReadLong()
            {
                int start = _at;
                while (_at < _text.Length && _text[_at] >= '0' && _text[_at] <= '9')
                    _at++;
                if (start == _at)
                    throw new InvalidOperationException("學生清單 JSON 的數字無效。");
                long value;
                if (!long.TryParse(_text.Substring(start, _at - start), NumberStyles.None, CultureInfo.InvariantCulture, out value))
                    throw new InvalidOperationException("學生清單 JSON 的數字無效。");
                return value;
            }

            public bool ReadBool()
            {
                if (Match("true"))
                    return true;
                if (Match("false"))
                    return false;
                throw new InvalidOperationException("學生清單 JSON 的參與狀態不是 boolean。");
            }

            public string ReadString()
            {
                if (!Take('"'))
                    throw new InvalidOperationException("學生清單 JSON 的文字無效。");
                StringBuilder value = new StringBuilder();
                while (_at < _text.Length)
                {
                    char ch = _text[_at++];
                    if (ch == '"')
                        return value.ToString();
                    if (ch != '\\')
                    {
                        value.Append(ch);
                        continue;
                    }
                    if (_at >= _text.Length)
                        break;
                    char escaped = _text[_at++];
                    if (escaped == '"' || escaped == '\\' || escaped == '/')
                        value.Append(escaped);
                    else if (escaped == 'n')
                        value.Append('\n');
                    else if (escaped == 'r')
                        value.Append('\r');
                    else if (escaped == 't')
                        value.Append('\t');
                    else if (escaped == 'u' && _at + 4 <= _text.Length)
                    {
                        int code;
                        if (!int.TryParse(_text.Substring(_at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            throw new InvalidOperationException("學生清單 JSON 的跳脫無效。");
                        value.Append((char)code);
                        _at += 4;
                    }
                    else
                    {
                        throw new InvalidOperationException("學生清單 JSON 的跳脫無效。");
                    }
                }
                throw new InvalidOperationException("學生清單 JSON 的文字沒有結束。");
            }

            private bool Match(string token)
            {
                if (_at + token.Length > _text.Length)
                    return false;
                if (_text.Substring(_at, token.Length) != token)
                    return false;
                _at += token.Length;
                return true;
            }
        }
    }
}
