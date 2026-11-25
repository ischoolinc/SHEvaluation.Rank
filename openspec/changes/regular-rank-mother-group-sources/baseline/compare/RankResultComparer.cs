using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RegularRankBaseline
{
    internal sealed class RankResultRow
    {
        public string Key;
        public string KeyText;
        public int SourceIndex;
        public int SourceLine;
        public Dictionary<string, ExactNumber> Values;
    }

    internal sealed class FieldDifference
    {
        public string KeyText;
        public string CandidateKeyText;
        public string Field;
        public string BaselineValue;
        public string CandidateValue;
        public int BaselineSourceIndex;
        public int CandidateSourceIndex;
        public int BaselineSourceLine;
        public int CandidateSourceLine;
    }

    internal sealed class UnmatchedRow
    {
        public string KeyText;
        public int SourceIndex;
        public int SourceLine;
    }

    internal sealed class DuplicateKey
    {
        public string KeyText;
        public int BaselineCount;
        public int CandidateCount;
        public List<int> BaselineSourceIndexes = new List<int>();
        public List<int> CandidateSourceIndexes = new List<int>();
        public List<int> BaselineSourceLines = new List<int>();
        public List<int> CandidateSourceLines = new List<int>();
    }

    internal sealed class CompareReport
    {
        public int BaselineCount;
        public int CandidateCount;
        public int EqualPairs;
        public List<UnmatchedRow> Missing = new List<UnmatchedRow>();
        public List<UnmatchedRow> Extra = new List<UnmatchedRow>();
        public List<FieldDifference> ValueDifferences = new List<FieldDifference>();
        public List<FieldDifference> NullDifferences = new List<FieldDifference>();
        public List<DuplicateKey> Duplicates = new List<DuplicateKey>();
        public bool Passed;
    }

    internal sealed class ExactNumber
    {
        internal readonly bool IsNull;
        internal readonly string Canonical;
        internal readonly string Original;

        private ExactNumber(bool isNull, string canonical, string original)
        {
            IsNull = isNull;
            Canonical = canonical;
            Original = original;
        }

        internal static ExactNumber Null()
        {
            return new ExactNumber(true, null, null);
        }

        internal static ExactNumber FromRaw(string raw)
        {
            return new ExactNumber(false, Canonicalize(raw), raw);
        }

        internal string Display()
        {
            if (IsNull)
            {
                return "NULL";
            }
            return Original;
        }

        internal bool Same(ExactNumber other)
        {
            if (IsNull || other.IsNull)
            {
                return IsNull && other.IsNull;
            }
            return string.Equals(Canonical, other.Canonical, StringComparison.Ordinal);
        }

        // 保留 JSON 數字字面的全部有效位數。不轉 double／decimal，也不使用容差。
        internal static string Canonicalize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                throw new InvalidOperationException("數字格式不正確");
            }

            int index = 0;
            bool negative = false;
            if (raw[index] == '-')
            {
                negative = true;
                index++;
            }
            if (index >= raw.Length || !IsDigit(raw[index]))
            {
                throw new InvalidOperationException("數字格式不正確");
            }

            int integerStart = index;
            if (raw[index] == '0')
            {
                index++;
            }
            else
            {
                while (index < raw.Length && IsDigit(raw[index]))
                {
                    index++;
                }
            }
            string integerDigits = raw.Substring(integerStart, index - integerStart);

            string fractionDigits = string.Empty;
            if (index < raw.Length && raw[index] == '.')
            {
                index++;
                int fractionStart = index;
                if (index >= raw.Length || !IsDigit(raw[index]))
                {
                    throw new InvalidOperationException("數字格式不正確");
                }
                while (index < raw.Length && IsDigit(raw[index]))
                {
                    index++;
                }
                fractionDigits = raw.Substring(fractionStart, index - fractionStart);
            }

            long exponent = 0;
            if (index < raw.Length && (raw[index] == 'e' || raw[index] == 'E'))
            {
                index++;
                exponent = ReadExponent(raw, ref index);
            }
            if (index != raw.Length)
            {
                throw new InvalidOperationException("數字格式不正確");
            }

            string digits = integerDigits + fractionDigits;
            long scale = ScaleAfterFraction(exponent, fractionDigits.Length);
            int start = 0;
            int end = digits.Length;
            while (start < end && digits[start] == '0')
            {
                start++;
            }
            if (start == end)
            {
                return "0";
            }
            while (end > start && digits[end - 1] == '0')
            {
                if (scale == long.MaxValue)
                {
                    throw new InvalidOperationException("數字指數超出可比較範圍");
                }
                scale++;
                end--;
            }

            string significant = digits.Substring(start, end - start);
            string text = significant + "e" + scale.ToString(CultureInfo.InvariantCulture);
            if (negative)
            {
                return "-" + text;
            }
            return text;
        }

        private static long ReadExponent(string raw, ref int index)
        {
            int sign = 1;
            if (index < raw.Length && (raw[index] == '+' || raw[index] == '-'))
            {
                if (raw[index] == '-')
                {
                    sign = -1;
                }
                index++;
            }
            if (index >= raw.Length || !IsDigit(raw[index]))
            {
                throw new InvalidOperationException("數字格式不正確");
            }

            long value = 0;
            while (index < raw.Length && IsDigit(raw[index]))
            {
                int digit = raw[index] - '0';
                if (value > (long.MaxValue - digit) / 10)
                {
                    throw new InvalidOperationException("數字指數超出可比較範圍");
                }
                value = (value * 10) + digit;
                index++;
            }
            if (sign < 0)
            {
                return -value;
            }
            return value;
        }

        private static long ScaleAfterFraction(long exponent, int fractionLength)
        {
            if (exponent >= 0)
            {
                return exponent - fractionLength;
            }
            if (exponent < long.MinValue + fractionLength)
            {
                throw new InvalidOperationException("數字指數超出可比較範圍");
            }
            return exponent - fractionLength;
        }

        private static bool IsDigit(char value)
        {
            return value >= '0' && value <= '9';
        }
    }

    internal static class RankResultComparer
    {
        internal static readonly string[] KeyFields = new string[]
        {
            "school_year",
            "semester",
            "grade_year",
            "item_type",
            "ref_exam_id",
            "item_name",
            "rank_type",
            "rank_name",
            "student_id"
        };

        internal static readonly string[] ValueFields = new string[]
        {
            "score",
            "rank",
            "pr",
            "percentile",
            "matrix_count",
            "avg_top_25",
            "avg_top_50",
            "avg",
            "avg_bottom_50",
            "avg_bottom_25",
            "level_gte100",
            "level_90",
            "level_80",
            "level_70",
            "level_60",
            "level_50",
            "level_40",
            "level_30",
            "level_20",
            "level_10",
            "level_lt10",
            "pr_88",
            "pr_75",
            "pr_50",
            "pr_25",
            "pr_12",
            "std_dev_pop"
        };

        internal static CompareReport Compare(IList<RankResultRow> baseline, IList<RankResultRow> candidate)
        {
            Dictionary<string, List<RankResultRow>> baselineGroups = Group(baseline);
            Dictionary<string, List<RankResultRow>> candidateGroups = Group(candidate);
            List<string> keys = new List<string>(baselineGroups.Keys);
            foreach (string key in candidateGroups.Keys)
            {
                if (!baselineGroups.ContainsKey(key))
                {
                    keys.Add(key);
                }
            }
            keys.Sort(StringComparer.Ordinal);

            CompareReport report = new CompareReport();
            report.BaselineCount = baseline.Count;
            report.CandidateCount = candidate.Count;

            foreach (string key in keys)
            {
                List<RankResultRow> baselineRows = GetGroup(baselineGroups, key);
                List<RankResultRow> candidateRows = GetGroup(candidateGroups, key);
                List<RankResultRow> orderedBaseline = OrderForPairing(baselineRows);
                List<RankResultRow> orderedCandidate = OrderForPairing(candidateRows);
                int paired = Math.Min(orderedBaseline.Count, orderedCandidate.Count);

                if (orderedBaseline.Count > 1 || orderedCandidate.Count > 1)
                {
                    DuplicateKey duplicate = new DuplicateKey();
                    duplicate.KeyText = KeyTextOf(orderedBaseline, orderedCandidate);
                    duplicate.BaselineCount = orderedBaseline.Count;
                    duplicate.CandidateCount = orderedCandidate.Count;
                    CollectSources(baselineRows, duplicate.BaselineSourceIndexes, duplicate.BaselineSourceLines);
                    CollectSources(candidateRows, duplicate.CandidateSourceIndexes, duplicate.CandidateSourceLines);
                    report.Duplicates.Add(duplicate);
                }

                for (int index = 0; index < paired; index++)
                {
                    if (PairEquals(report, orderedBaseline[index], orderedCandidate[index]))
                    {
                        report.EqualPairs++;
                    }
                }

                for (int index = paired; index < orderedBaseline.Count; index++)
                {
                    report.Missing.Add(ToUnmatched(orderedBaseline[index]));
                }

                for (int index = paired; index < orderedCandidate.Count; index++)
                {
                    report.Extra.Add(ToUnmatched(orderedCandidate[index]));
                }
            }

            report.Passed = report.Missing.Count == 0
                && report.Extra.Count == 0
                && report.ValueDifferences.Count == 0
                && report.NullDifferences.Count == 0;
            return report;
        }

        private static bool PairEquals(CompareReport report, RankResultRow baseline, RankResultRow candidate)
        {
            bool equal = true;
            for (int fieldIndex = 0; fieldIndex < ValueFields.Length; fieldIndex++)
            {
                string field = ValueFields[fieldIndex];
                ExactNumber baselineValue = baseline.Values[field];
                ExactNumber candidateValue = candidate.Values[field];
                if (baselineValue.Same(candidateValue))
                {
                    continue;
                }

                equal = false;
                FieldDifference difference = new FieldDifference();
                difference.KeyText = baseline.KeyText;
                difference.CandidateKeyText = candidate.KeyText;
                difference.Field = field;
                difference.BaselineValue = baselineValue.Display();
                difference.CandidateValue = candidateValue.Display();
                difference.BaselineSourceIndex = baseline.SourceIndex;
                difference.CandidateSourceIndex = candidate.SourceIndex;
                difference.BaselineSourceLine = baseline.SourceLine;
                difference.CandidateSourceLine = candidate.SourceLine;
                if (baselineValue.IsNull || candidateValue.IsNull)
                {
                    report.NullDifferences.Add(difference);
                }
                else
                {
                    report.ValueDifferences.Add(difference);
                }
            }
            return equal;
        }

        private static List<RankResultRow> OrderForPairing(List<RankResultRow> rows)
        {
            return rows
                .Select(delegate(RankResultRow row, int index) { return new IndexedRow(row, index); })
                .OrderBy(delegate(IndexedRow item) { return ValueSignature(item.Row); }, StringComparer.Ordinal)
                .ThenBy(delegate(IndexedRow item) { return item.Index; })
                .Select(delegate(IndexedRow item) { return item.Row; })
                .ToList();
        }

        private static string ValueSignature(RankResultRow row)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < ValueFields.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append('\u001f');
                }
                ExactNumber value = row.Values[ValueFields[index]];
                if (value.IsNull)
                {
                    builder.Append('\u0000');
                }
                else
                {
                    builder.Append('1');
                    builder.Append(value.Canonical);
                }
            }
            return builder.ToString();
        }

        private static Dictionary<string, List<RankResultRow>> Group(IList<RankResultRow> rows)
        {
            Dictionary<string, List<RankResultRow>> groups = new Dictionary<string, List<RankResultRow>>(StringComparer.Ordinal);
            for (int index = 0; index < rows.Count; index++)
            {
                RankResultRow row = rows[index];
                List<RankResultRow> group;
                if (!groups.TryGetValue(row.Key, out group))
                {
                    group = new List<RankResultRow>();
                    groups.Add(row.Key, group);
                }
                group.Add(row);
            }
            return groups;
        }

        private static List<RankResultRow> GetGroup(Dictionary<string, List<RankResultRow>> groups, string key)
        {
            List<RankResultRow> group;
            if (!groups.TryGetValue(key, out group))
            {
                return new List<RankResultRow>();
            }
            return group;
        }

        private static string KeyTextOf(List<RankResultRow> baseline, List<RankResultRow> candidate)
        {
            if (baseline.Count > 0)
            {
                return baseline[0].KeyText;
            }
            return candidate[0].KeyText;
        }

        private static UnmatchedRow ToUnmatched(RankResultRow row)
        {
            UnmatchedRow unmatched = new UnmatchedRow();
            unmatched.KeyText = row.KeyText;
            unmatched.SourceIndex = row.SourceIndex;
            unmatched.SourceLine = row.SourceLine;
            return unmatched;
        }

        private static void CollectSources(List<RankResultRow> rows, List<int> indexes, List<int> lines)
        {
            List<RankResultRow> ordered = new List<RankResultRow>(rows);
            ordered.Sort(delegate(RankResultRow left, RankResultRow right)
            {
                return left.SourceIndex.CompareTo(right.SourceIndex);
            });
            for (int index = 0; index < ordered.Count; index++)
            {
                indexes.Add(ordered[index].SourceIndex);
                lines.Add(ordered[index].SourceLine);
            }
        }
    }

    internal sealed class IndexedRow
    {
        internal readonly RankResultRow Row;
        internal readonly int Index;

        internal IndexedRow(RankResultRow row, int index)
        {
            Row = row;
            Index = index;
        }
    }

    internal sealed class JsonNumber
    {
        internal readonly string Raw;

        internal JsonNumber(string raw)
        {
            Raw = raw;
        }
    }

    internal sealed class JsonDocument
    {
        internal readonly object Root;
        internal readonly Dictionary<Dictionary<string, object>, int> ObjectLines;

        internal JsonDocument(object root, Dictionary<Dictionary<string, object>, int> objectLines)
        {
            Root = root;
            ObjectLines = objectLines;
        }

        internal int LineOf(Dictionary<string, object> value)
        {
            int line;
            if (value != null && ObjectLines.TryGetValue(value, out line))
            {
                return line;
            }
            return 0;
        }
    }

    internal sealed class JsonTextParser
    {
        private readonly string _text;
        private readonly Dictionary<Dictionary<string, object>, int> _lines;
        private int _index;
        private int _line;

        private JsonTextParser(string text)
        {
            _text = text ?? string.Empty;
            _lines = new Dictionary<Dictionary<string, object>, int>();
            _line = 1;
        }

        internal static JsonDocument Parse(string text)
        {
            JsonTextParser parser = new JsonTextParser(text);
            if (parser._text.Length > 0 && parser._text[0] == '\uFEFF')
            {
                parser._index = 1;
            }
            parser.SkipWhitespace();
            if (parser._index >= parser._text.Length)
            {
                throw parser.Error("JSON 是空的");
            }
            object root = parser.ParseValue();
            parser.SkipWhitespace();
            if (parser._index != parser._text.Length)
            {
                throw parser.Error("JSON 結尾有多餘內容");
            }
            return new JsonDocument(root, parser._lines);
        }

        private object ParseValue()
        {
            if (_index >= _text.Length)
            {
                throw Error("JSON 意外結束");
            }
            char current = _text[_index];
            if (current == '{')
            {
                return ParseObject();
            }
            if (current == '[')
            {
                return ParseArray();
            }
            if (current == '"')
            {
                return ParseString();
            }
            if (current == 't')
            {
                return ParseLiteral("true", true);
            }
            if (current == 'f')
            {
                return ParseLiteral("false", false);
            }
            if (current == 'n')
            {
                return ParseLiteral("null", null);
            }
            if (current == '-' || IsDigit(current))
            {
                return ParseNumber();
            }
            throw Error("JSON 格式不正確");
        }

        private Dictionary<string, object> ParseObject()
        {
            int line = _line;
            _index++;
            Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.Ordinal);
            _lines.Add(result, line);
            SkipWhitespace();
            if (Consume('}'))
            {
                return result;
            }
            while (true)
            {
                if (_index >= _text.Length || _text[_index] != '"')
                {
                    throw Error("JSON 物件鍵必須是字串");
                }
                string key = ParseString();
                SkipWhitespace();
                if (!Consume(':'))
                {
                    throw Error("JSON 物件缺少冒號");
                }
                SkipWhitespace();
                result[key] = ParseValue();
                SkipWhitespace();
                if (Consume('}'))
                {
                    return result;
                }
                if (!Consume(','))
                {
                    throw Error("JSON 物件缺少逗號或結尾");
                }
                SkipWhitespace();
                if (Peek('}'))
                {
                    throw Error("JSON 物件結尾有多餘逗號");
                }
            }
        }

        private List<object> ParseArray()
        {
            _index++;
            List<object> result = new List<object>();
            SkipWhitespace();
            if (Consume(']'))
            {
                return result;
            }
            while (true)
            {
                result.Add(ParseValue());
                SkipWhitespace();
                if (Consume(']'))
                {
                    return result;
                }
                if (!Consume(','))
                {
                    throw Error("JSON 陣列缺少逗號或結尾");
                }
                SkipWhitespace();
                if (Peek(']'))
                {
                    throw Error("JSON 陣列結尾有多餘逗號");
                }
            }
        }

        private string ParseString()
        {
            _index++;
            StringBuilder builder = new StringBuilder();
            while (_index < _text.Length)
            {
                char current = _text[_index++];
                if (current == '"')
                {
                    return builder.ToString();
                }
                if (current == '\\')
                {
                    builder.Append(ParseEscape());
                    continue;
                }
                if (current < ' ')
                {
                    throw Error("JSON 字串含有未跳脫的控制字元");
                }
                builder.Append(current);
            }
            throw Error("JSON 字串沒有結束");
        }

        private char ParseEscape()
        {
            if (_index >= _text.Length)
            {
                throw Error("JSON 字串跳脫不完整");
            }
            char current = _text[_index++];
            if (current == '"' || current == '\\' || current == '/')
            {
                return current;
            }
            if (current == 'b')
            {
                return '\b';
            }
            if (current == 'f')
            {
                return '\f';
            }
            if (current == 'n')
            {
                return '\n';
            }
            if (current == 'r')
            {
                return '\r';
            }
            if (current == 't')
            {
                return '\t';
            }
            if (current == 'u')
            {
                int code = 0;
                for (int index = 0; index < 4; index++)
                {
                    if (_index >= _text.Length)
                    {
                        throw Error("JSON Unicode 跳脫不完整");
                    }
                    int digit = HexValue(_text[_index++]);
                    if (digit < 0)
                    {
                        throw Error("JSON Unicode 跳脫不正確");
                    }
                    code = (code << 4) + digit;
                }
                return (char)code;
            }
            throw Error("JSON 字串跳脫不正確");
        }

        private object ParseLiteral(string word, object value)
        {
            for (int index = 0; index < word.Length; index++)
            {
                if (_index >= _text.Length || _text[_index] != word[index])
                {
                    throw Error("JSON 格式不正確");
                }
                _index++;
            }
            if (_index < _text.Length && (IsDigit(_text[_index]) || IsLetter(_text[_index]) || _text[_index] == '_'))
            {
                throw Error("JSON 格式不正確");
            }
            return value;
        }

        private JsonNumber ParseNumber()
        {
            int start = _index;
            if (_text[_index] == '-')
            {
                _index++;
            }
            if (_index >= _text.Length || !IsDigit(_text[_index]))
            {
                throw Error("數字格式不正確");
            }
            if (_text[_index] == '0')
            {
                _index++;
            }
            else
            {
                while (_index < _text.Length && IsDigit(_text[_index]))
                {
                    _index++;
                }
            }
            if (_index < _text.Length && _text[_index] == '.')
            {
                _index++;
                if (_index >= _text.Length || !IsDigit(_text[_index]))
                {
                    throw Error("數字格式不正確");
                }
                while (_index < _text.Length && IsDigit(_text[_index]))
                {
                    _index++;
                }
            }
            if (_index < _text.Length && (_text[_index] == 'e' || _text[_index] == 'E'))
            {
                _index++;
                if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-'))
                {
                    _index++;
                }
                if (_index >= _text.Length || !IsDigit(_text[_index]))
                {
                    throw Error("數字格式不正確");
                }
                while (_index < _text.Length && IsDigit(_text[_index]))
                {
                    _index++;
                }
            }
            return new JsonNumber(_text.Substring(start, _index - start));
        }

        private void SkipWhitespace()
        {
            while (_index < _text.Length)
            {
                char current = _text[_index];
                if (current == ' ' || current == '\t')
                {
                    _index++;
                    continue;
                }
                if (current == '\n')
                {
                    _index++;
                    _line++;
                    continue;
                }
                if (current == '\r')
                {
                    _index++;
                    if (_index < _text.Length && _text[_index] == '\n')
                    {
                        _index++;
                    }
                    _line++;
                    continue;
                }
                return;
            }
        }

        private bool Consume(char expected)
        {
            if (!Peek(expected))
            {
                return false;
            }
            _index++;
            return true;
        }

        private bool Peek(char expected)
        {
            return _index < _text.Length && _text[_index] == expected;
        }

        private static bool IsDigit(char value)
        {
            return value >= '0' && value <= '9';
        }

        private static bool IsLetter(char value)
        {
            return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
        }

        private static int HexValue(char value)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }
            if (value >= 'a' && value <= 'f')
            {
                return value - 'a' + 10;
            }
            if (value >= 'A' && value <= 'F')
            {
                return value - 'A' + 10;
            }
            return -1;
        }

        private InvalidOperationException Error(string message)
        {
            return new InvalidOperationException(message + "，第 " + _line.ToString(CultureInfo.InvariantCulture) + " 行");
        }
    }

    internal static class Program
    {
        private const int LargeRowMinimum = 3000;
        private const int LargeCharMinimumExclusive = 2115001;
        private const string HighScore = "90.12345678901234567890123456781";
        private const string HighScoreNext = "90.12345678901234567890123456782";

        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                if (HasOption(args, "--large-check"))
                {
                    RunLargeCheck();
                    return 0;
                }
                if (HasOption(args, "--baseline"))
                {
                    string reportPath = null;
                    if (HasOption(args, "--report"))
                    {
                        reportPath = RequiredOption(args, "--report");
                    }
                    CompareReport report;
                    return ExecuteCompare(
                        RequiredOption(args, "--baseline"),
                        RequiredOption(args, "--candidate"),
                        reportPath,
                        out report);
                }

                string casesDirectory = RequiredOption(args, "--cases");
                return RunCases(casesDirectory) ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static int ExecuteCompare(string baselinePath, string candidatePath, string reportPath, out CompareReport report)
        {
            report = RankResultComparer.Compare(
                LoadCompleteRows(ReadJson(baselinePath)),
                LoadCompleteRows(ReadJson(candidatePath)));
            string text = RenderReport(report);
            Console.Write(text);
            if (!string.IsNullOrEmpty(reportPath))
            {
                File.WriteAllText(reportPath, text, new UTF8Encoding(true));
            }
            return report.Passed ? 0 : 1;
        }

        private static bool RunCases(string casesDirectory)
        {
            JsonDocument defaultsDocument = ReadJson(Path.Combine(casesDirectory, "defaults.json"));
            Dictionary<string, object> defaults = AsObject(defaultsDocument.Root);
            string[] caseFiles = Directory.GetFiles(casesDirectory, "*.json");
            Array.Sort(caseFiles, StringComparer.Ordinal);
            bool allPassed = true;
            int ran = 0;
            for (int index = 0; index < caseFiles.Length; index++)
            {
                string fileName = Path.GetFileName(caseFiles[index]);
                if (string.Equals(fileName, "defaults.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                ran++;
                JsonDocument caseDocument = ReadJson(caseFiles[index]);
                Dictionary<string, object> caseFile = AsObject(caseDocument.Root);
                string name = Convert.ToString(caseFile["name"], CultureInfo.InvariantCulture);
                CompareReport report = RankResultComparer.Compare(
                    LoadRows(defaults, AsList(caseFile["baseline"]), caseDocument),
                    LoadRows(defaults, AsList(caseFile["candidate"]), caseDocument));
                string failure = ExpectationFailure(AsObject(caseFile["expect"]), report);
                if (failure == null)
                {
                    Console.WriteLine("PASS " + name);
                }
                else
                {
                    allPassed = false;
                    Console.WriteLine("FAIL " + name + " " + failure);
                    Console.Write(RenderReport(report));
                }
            }
            if (ran == 0)
            {
                throw new InvalidOperationException("沒有找到驗證案例。");
            }
            Console.WriteLine(allPassed ? "ALL PASSED " + ran.ToString(CultureInfo.InvariantCulture) : "FAILED");
            return allPassed;
        }

        private static void RunLargeCheck()
        {
            string directory = Path.Combine(Path.GetTempPath(), "regular-rank-compare-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                RunLargeCheckIn(directory);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
            Console.WriteLine("tempDeleted=True");
            Console.WriteLine("largeCheck=PASS");
        }

        private static void RunLargeCheckIn(string directory)
        {
            string baselinePath = Path.Combine(directory, "baseline.json");
            string identicalPath = Path.Combine(directory, "identical.json");
            string mutatedPath = Path.Combine(directory, "mutated.json");
            string reportPath = Path.Combine(directory, "mutated-report.txt");
            int rows = LargeRowMinimum;
            int chars = 0;
            while (rows <= 20000)
            {
                WriteLargeArray(baselinePath, rows, -1, HighScore);
                chars = File.ReadAllText(baselinePath, Encoding.UTF8).Length;
                if (chars > LargeCharMinimumExclusive)
                {
                    break;
                }
                int average = chars / rows;
                if (average < 1)
                {
                    throw new InvalidOperationException("大量案例內容異常");
                }
                int next = (LargeCharMinimumExclusive / average) + 2;
                if (next <= rows)
                {
                    next = rows + 100;
                }
                rows = next;
            }
            if (chars <= LargeCharMinimumExclusive || rows < LargeRowMinimum)
            {
                throw new InvalidOperationException("無法產生超過讀取限制的完整結果");
            }

            long bytes = new FileInfo(baselinePath).Length;
            File.Copy(baselinePath, identicalPath, true);
            int mutateIndex = rows / 2;
            WriteLargeArray(mutatedPath, rows, mutateIndex, HighScoreNext);

            Console.WriteLine("--- large identical ---");
            CompareReport identicalReport;
            int identicalExit = ExecuteCompare(baselinePath, identicalPath, null, out identicalReport);
            if (identicalExit != 0 || !identicalReport.Passed)
            {
                throw new InvalidOperationException("大量相同結果應回傳 0");
            }
            if (identicalReport.BaselineCount != rows || identicalReport.CandidateCount != rows || identicalReport.EqualPairs != rows)
            {
                throw new InvalidOperationException("大量相同結果的列數或相等配對不正確");
            }

            Console.WriteLine("--- large mutated ---");
            CompareReport mutatedReport;
            int mutatedExit = ExecuteCompare(baselinePath, mutatedPath, reportPath, out mutatedReport);
            if (mutatedExit != 1 || mutatedReport.Passed)
            {
                throw new InvalidOperationException("大量末位差異不得回傳成功");
            }
            if (mutatedReport.ValueDifferences.Count != 1
                || mutatedReport.NullDifferences.Count != 0
                || mutatedReport.Missing.Count != 0
                || mutatedReport.Extra.Count != 0
                || mutatedReport.EqualPairs != rows - 1)
            {
                throw new InvalidOperationException("大量末位差異的分類不正確");
            }

            FieldDifference difference = mutatedReport.ValueDifferences[0];
            string studentId = (100000 + mutateIndex).ToString(CultureInfo.InvariantCulture);
            string expectedKey = "school_year=113 | semester=2 | grade_year=1 | item_type="
                + "\u5b9a\u671f\u8a55\u91cf/\u79d1\u76ee\u6210\u7e3e"
                + " | ref_exam_id=15 | item_name=\u570b\u6587 | rank_type=\u5e74\u6392\u540d | rank_name=1\u5e74\u7d1a | student_id="
                + studentId;
            if (!string.Equals(difference.Field, "score", StringComparison.Ordinal)
                || !string.Equals(difference.BaselineValue, HighScore, StringComparison.Ordinal)
                || !string.Equals(difference.CandidateValue, HighScoreNext, StringComparison.Ordinal)
                || !string.Equals(difference.KeyText, expectedKey, StringComparison.Ordinal)
                || difference.BaselineSourceIndex != mutateIndex
                || difference.CandidateSourceIndex != mutateIndex
                || difference.BaselineSourceLine != mutateIndex + 2
                || difference.CandidateSourceLine != mutateIndex + 2)
            {
                throw new InvalidOperationException("大量末位差異無法定位到指定列");
            }

            string saved = File.ReadAllText(reportPath, Encoding.UTF8);
            if (saved.IndexOf(HighScore, StringComparison.Ordinal) < 0
                || saved.IndexOf(HighScoreNext, StringComparison.Ordinal) < 0
                || saved.IndexOf(expectedKey, StringComparison.Ordinal) < 0
                || saved.IndexOf("passed=False", StringComparison.Ordinal) < 0
                || saved.IndexOf("\u6b04\u4f4d: score", StringComparison.Ordinal) < 0
                || saved.IndexOf("baseline\u4f86\u6e90\u5217: " + (mutateIndex + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) < 0
                || saved.IndexOf("candidate\u4f86\u6e90\u5217: " + (mutateIndex + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("存檔的差異報告缺少定位資訊");
            }

            Console.WriteLine("--- large-check ---");
            Console.WriteLine("largeRows=" + rows.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("largeChars=" + chars.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("largeBytes=" + bytes.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("largeRowMinimum=" + LargeRowMinimum.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("largeCharMinimumExclusive=" + LargeCharMinimumExclusive.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("identicalExit=" + identicalExit.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("identicalEqualPairs=" + identicalReport.EqualPairs.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedExit=" + mutatedExit.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedEqualPairs=" + mutatedReport.EqualPairs.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedValueDifferences=" + mutatedReport.ValueDifferences.Count.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedField=" + difference.Field);
            Console.WriteLine("mutatedBaselineValue=" + difference.BaselineValue);
            Console.WriteLine("mutatedCandidateValue=" + difference.CandidateValue);
            Console.WriteLine("mutatedBaselineSourceRow=" + (difference.BaselineSourceIndex + 1).ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedCandidateSourceRow=" + (difference.CandidateSourceIndex + 1).ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedBaselineLine=" + difference.BaselineSourceLine.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedCandidateLine=" + difference.CandidateSourceLine.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("mutatedStudentId=" + studentId);
            Console.WriteLine("reportRoundTrip=True");
        }

        private static void WriteLargeArray(string path, int rowCount, int mutateIndex, string mutateScore)
        {
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                writer.NewLine = "\n";
                writer.WriteLine("[");
                for (int index = 0; index < rowCount; index++)
                {
                    string score = HighScore;
                    if (index == mutateIndex)
                    {
                        score = mutateScore;
                    }
                    WriteLargeRow(writer, index, score);
                    if (index + 1 < rowCount)
                    {
                        writer.WriteLine(",");
                    }
                    else
                    {
                        writer.WriteLine();
                    }
                }
                writer.WriteLine("]");
            }
        }

        private static void WriteLargeRow(StreamWriter writer, int index, string scoreLiteral)
        {
            writer.Write("{\"school_year\":113,\"semester\":2,\"grade_year\":1,\"item_type\":\"");
            writer.Write("\u5b9a\u671f\u8a55\u91cf/\u79d1\u76ee\u6210\u7e3e");
            writer.Write("\",\"ref_exam_id\":15,\"item_name\":\"");
            writer.Write("\u570b\u6587");
            writer.Write("\",\"rank_type\":\"");
            writer.Write("\u5e74\u6392\u540d");
            writer.Write("\",\"rank_name\":\"1");
            writer.Write("\u5e74\u7d1a");
            writer.Write("\",\"student_id\":");
            writer.Write((100000 + index).ToString(CultureInfo.InvariantCulture));
            writer.Write(",\"score\":");
            writer.Write(scoreLiteral);
            writer.Write(",\"rank\":1,\"pr\":0,\"percentile\":1,\"matrix_count\":10,\"avg_top_25\":95,\"avg_top_50\":92,\"avg\":80,\"avg_bottom_50\":70,\"avg_bottom_25\":60,\"level_gte100\":0,\"level_90\":1,\"level_80\":2,\"level_70\":3,\"level_60\":1,\"level_50\":1,\"level_40\":1,\"level_30\":1,\"level_20\":0,\"level_10\":0,\"level_lt10\":0,\"pr_88\":88,\"pr_75\":75,\"pr_50\":50,\"pr_25\":25,\"pr_12\":12,\"std_dev_pop\":null}");
        }

        private static string ExpectationFailure(Dictionary<string, object> expect, CompareReport report)
        {
            if (GetBool(expect, "passed") != report.Passed)
            {
                return "passed";
            }
            if (GetInt(expect, "baselineCount") != report.BaselineCount)
            {
                return "baselineCount";
            }
            if (GetInt(expect, "candidateCount") != report.CandidateCount)
            {
                return "candidateCount";
            }
            if (GetInt(expect, "equalPairs") != report.EqualPairs)
            {
                return "equalPairs";
            }
            if (GetInt(expect, "missing") != report.Missing.Count)
            {
                return "missing";
            }
            if (GetInt(expect, "extra") != report.Extra.Count)
            {
                return "extra";
            }
            if (GetInt(expect, "valueDifferences") != report.ValueDifferences.Count)
            {
                return "valueDifferences";
            }
            if (GetInt(expect, "nullDifferences") != report.NullDifferences.Count)
            {
                return "nullDifferences";
            }
            if (expect.ContainsKey("valueField") && !ContainsField(report.ValueDifferences, Convert.ToString(expect["valueField"], CultureInfo.InvariantCulture)))
            {
                return "valueField";
            }
            if (expect.ContainsKey("nullField") && !ContainsField(report.NullDifferences, Convert.ToString(expect["nullField"], CultureInfo.InvariantCulture)))
            {
                return "nullField";
            }
            if (expect.ContainsKey("duplicateBaseline"))
            {
                if (report.Duplicates.Count != 1)
                {
                    return "duplicateCount";
                }
                if (report.Duplicates[0].BaselineCount != GetInt(expect, "duplicateBaseline")
                    || report.Duplicates[0].CandidateCount != GetInt(expect, "duplicateCandidate"))
                {
                    return "duplicateMultiplicity";
                }
            }
            else if (report.Duplicates.Count != 0)
            {
                return "unexpectedDuplicate";
            }
            return null;
        }

        private static bool ContainsField(List<FieldDifference> differences, string field)
        {
            for (int index = 0; index < differences.Count; index++)
            {
                if (differences[index].Field == field)
                {
                    return true;
                }
            }
            return false;
        }

        private static List<RankResultRow> LoadRows(Dictionary<string, object> defaults, IList rows, JsonDocument document)
        {
            List<RankResultRow> result = new List<RankResultRow>();
            for (int index = 0; index < rows.Count; index++)
            {
                Dictionary<string, object> original = AsObject(rows[index]);
                result.Add(BuildRow(Merge(defaults, original), index, document.LineOf(original)));
            }
            return result;
        }

        private static List<RankResultRow> LoadCompleteRows(JsonDocument document)
        {
            return LoadRows(new Dictionary<string, object>(StringComparer.Ordinal), AsList(document.Root), document);
        }

        private static RankResultRow BuildRow(Dictionary<string, object> source, int sourceIndex, int sourceLine)
        {
            RankResultRow row = new RankResultRow();
            row.SourceIndex = sourceIndex;
            row.SourceLine = sourceLine;
            row.Values = new Dictionary<string, ExactNumber>(StringComparer.Ordinal);
            StringBuilder key = new StringBuilder();
            StringBuilder keyText = new StringBuilder();
            for (int index = 0; index < RankResultComparer.KeyFields.Length; index++)
            {
                string field = RankResultComparer.KeyFields[index];
                if (!source.ContainsKey(field))
                {
                    throw new InvalidOperationException("缺少識別欄位 " + field);
                }
                if (index > 0)
                {
                    key.Append('\u001f');
                    keyText.Append(" | ");
                }
                key.Append(CanonicalKeyPart(source[field]));
                keyText.Append(field);
                keyText.Append('=');
                keyText.Append(DisplayKeyPart(source[field]));
            }
            for (int index = 0; index < RankResultComparer.ValueFields.Length; index++)
            {
                string field = RankResultComparer.ValueFields[index];
                if (!source.ContainsKey(field))
                {
                    throw new InvalidOperationException("缺少比較欄位 " + field);
                }
                row.Values.Add(field, CanonicalValue(source[field], field));
            }
            row.Key = key.ToString();
            row.KeyText = keyText.ToString();
            return row;
        }

        private static Dictionary<string, object> Merge(Dictionary<string, object> defaults, Dictionary<string, object> row)
        {
            Dictionary<string, object> merged = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> pair in defaults)
            {
                merged[pair.Key] = pair.Value;
            }
            foreach (KeyValuePair<string, object> pair in row)
            {
                merged[pair.Key] = pair.Value;
            }
            return merged;
        }

        private static string CanonicalKeyPart(object value)
        {
            if (value == null)
            {
                return "\u0000";
            }
            string text = value as string;
            if (text != null)
            {
                return text;
            }
            JsonNumber number = value as JsonNumber;
            if (number == null)
            {
                throw new InvalidOperationException("識別欄位型別不正確");
            }
            return ExactNumber.Canonicalize(number.Raw);
        }

        private static string DisplayKeyPart(object value)
        {
            if (value == null)
            {
                return "NULL";
            }
            string text = value as string;
            if (text != null)
            {
                return text;
            }
            JsonNumber number = value as JsonNumber;
            if (number == null)
            {
                throw new InvalidOperationException("識別欄位型別不正確");
            }
            return number.Raw;
        }

        private static ExactNumber CanonicalValue(object value, string field)
        {
            if (value == null)
            {
                return ExactNumber.Null();
            }
            if (value is string)
            {
                throw new InvalidOperationException(field + " 必須是數字或 null");
            }
            JsonNumber number = value as JsonNumber;
            if (number == null)
            {
                throw new InvalidOperationException(field + " 必須是數字或 null");
            }
            return ExactNumber.FromRaw(number.Raw);
        }

        private static JsonDocument ReadJson(string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            try
            {
                return JsonTextParser.Parse(text);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(path + "：" + ex.Message);
            }
        }

        private static Dictionary<string, object> AsObject(object value)
        {
            Dictionary<string, object> dictionary = value as Dictionary<string, object>;
            if (dictionary == null)
            {
                throw new InvalidOperationException("JSON 物件格式不正確");
            }
            return dictionary;
        }

        private static IList AsList(object value)
        {
            IList list = value as IList;
            if (list == null || value is string)
            {
                throw new InvalidOperationException("JSON 陣列格式不正確");
            }
            return list;
        }

        private static bool GetBool(Dictionary<string, object> source, string name)
        {
            object value = source[name];
            if (value is bool)
            {
                return (bool)value;
            }
            throw new InvalidOperationException(name + " 必須是布林值");
        }

        private static int GetInt(Dictionary<string, object> source, string name)
        {
            object value = source[name];
            JsonNumber number = value as JsonNumber;
            if (number == null)
            {
                throw new InvalidOperationException(name + " 必須是整數");
            }
            return ParseJsonInt(number.Raw, name);
        }

        private static int ParseJsonInt(string raw, string name)
        {
            string canonical = ExactNumber.Canonicalize(raw);
            if (canonical == "0")
            {
                return 0;
            }
            bool negative = canonical[0] == '-';
            string body = negative ? canonical.Substring(1) : canonical;
            int marker = body.IndexOf('e');
            if (marker <= 0)
            {
                throw new InvalidOperationException(name + " 必須是整數");
            }
            string digits = body.Substring(0, marker);
            long scale = long.Parse(body.Substring(marker + 1), CultureInfo.InvariantCulture);
            if (scale < 0)
            {
                throw new InvalidOperationException(name + " 必須是整數");
            }
            if (digits.Length + scale > 10)
            {
                throw new InvalidOperationException(name + " 超出整數範圍");
            }
            string plain = digits + new string('0', (int)scale);
            if (negative)
            {
                plain = "-" + plain;
            }
            int parsed;
            if (!int.TryParse(plain, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                throw new InvalidOperationException(name + " 超出整數範圍");
            }
            return parsed;
        }

        private static bool HasOption(string[] args, string name)
        {
            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] == name)
                {
                    return true;
                }
            }
            return false;
        }

        private static string RequiredOption(string[] args, string name)
        {
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (args[index] == name)
                {
                    return args[index + 1];
                }
            }
            throw new InvalidOperationException("缺少參數 " + name);
        }

        private static string RenderReport(CompareReport report)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("baseline=").Append(report.BaselineCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(" candidate=").Append(report.CandidateCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(" equalPairs=").Append(report.EqualPairs.ToString(CultureInfo.InvariantCulture));
            builder.Append(" missing=").Append(report.Missing.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(" extra=").Append(report.Extra.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(" valueDifferences=").Append(report.ValueDifferences.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(" nullDifferences=").Append(report.NullDifferences.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(" duplicates=").Append(report.Duplicates.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append(" passed=").Append(report.Passed ? "True" : "False");
            builder.AppendLine();
            AppendDifferences(builder, "數值差異", report.ValueDifferences);
            AppendDifferences(builder, "NULL 差異", report.NullDifferences);
            AppendUnmatched(builder, "缺漏", "baseline", report.Missing);
            AppendUnmatched(builder, "多出", "candidate", report.Extra);
            AppendDuplicates(builder, report.Duplicates);
            return builder.ToString();
        }

        private static void AppendDifferences(StringBuilder builder, string title, List<FieldDifference> items)
        {
            if (items.Count == 0)
            {
                return;
            }
            builder.Append(title);
            builder.Append(' ');
            builder.AppendLine(items.Count.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < items.Count; index++)
            {
                FieldDifference item = items[index];
                builder.Append('[');
                builder.Append((index + 1).ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("]");
                builder.Append("識別鍵: ");
                builder.AppendLine(item.KeyText);
                if (!string.Equals(item.KeyText, item.CandidateKeyText, StringComparison.Ordinal))
                {
                    builder.Append("candidate識別鍵: ");
                    builder.AppendLine(item.CandidateKeyText);
                }
                builder.Append("欄位: ");
                builder.AppendLine(item.Field);
                builder.Append("baseline: ");
                builder.AppendLine(item.BaselineValue);
                builder.Append("candidate: ");
                builder.AppendLine(item.CandidateValue);
                builder.Append("baseline來源列: ");
                builder.AppendLine((item.BaselineSourceIndex + 1).ToString(CultureInfo.InvariantCulture));
                builder.Append("baseline檔案行: ");
                builder.AppendLine(FormatLine(item.BaselineSourceLine));
                builder.Append("candidate來源列: ");
                builder.AppendLine((item.CandidateSourceIndex + 1).ToString(CultureInfo.InvariantCulture));
                builder.Append("candidate檔案行: ");
                builder.AppendLine(FormatLine(item.CandidateSourceLine));
            }
        }

        private static void AppendUnmatched(StringBuilder builder, string title, string side, List<UnmatchedRow> items)
        {
            if (items.Count == 0)
            {
                return;
            }
            builder.Append(title);
            builder.Append(' ');
            builder.AppendLine(items.Count.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < items.Count; index++)
            {
                UnmatchedRow item = items[index];
                builder.Append('[');
                builder.Append((index + 1).ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("]");
                builder.Append("識別鍵: ");
                builder.AppendLine(item.KeyText);
                builder.Append(side);
                builder.Append("來源列: ");
                builder.AppendLine((item.SourceIndex + 1).ToString(CultureInfo.InvariantCulture));
                builder.Append(side);
                builder.Append("檔案行: ");
                builder.AppendLine(FormatLine(item.SourceLine));
            }
        }

        private static void AppendDuplicates(StringBuilder builder, List<DuplicateKey> items)
        {
            if (items.Count == 0)
            {
                return;
            }
            builder.Append("重複 ");
            builder.AppendLine(items.Count.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < items.Count; index++)
            {
                DuplicateKey item = items[index];
                builder.Append('[');
                builder.Append((index + 1).ToString(CultureInfo.InvariantCulture));
                builder.AppendLine("]");
                builder.Append("識別鍵: ");
                builder.AppendLine(item.KeyText);
                builder.Append("baseline筆數: ");
                builder.AppendLine(item.BaselineCount.ToString(CultureInfo.InvariantCulture));
                builder.Append("candidate筆數: ");
                builder.AppendLine(item.CandidateCount.ToString(CultureInfo.InvariantCulture));
                builder.Append("baseline來源列: ");
                builder.AppendLine(FormatSourceRows(item.BaselineSourceIndexes));
                builder.Append("candidate來源列: ");
                builder.AppendLine(FormatSourceRows(item.CandidateSourceIndexes));
                builder.Append("baseline檔案行: ");
                builder.AppendLine(FormatSourceLines(item.BaselineSourceLines));
                builder.Append("candidate檔案行: ");
                builder.AppendLine(FormatSourceLines(item.CandidateSourceLines));
            }
        }

        private static string FormatSourceRows(List<int> indexes)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < indexes.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }
                builder.Append((indexes[index] + 1).ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static string FormatSourceLines(List<int> lines)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < lines.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(FormatLine(lines[index]));
            }
            return builder.ToString();
        }

        private static string FormatLine(int line)
        {
            if (line <= 0)
            {
                return "未知";
            }
            return line.ToString(CultureInfo.InvariantCulture);
        }
    }
}
