using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class HistoryRankConflict
    {
        public string StudentId { get; private set; }
        public string RankType { get; private set; }
        public List<string> Names { get; private set; }

        internal static HistoryRankConflict Create(string studentId, string rankType, List<string> names)
        {
            HistoryRankConflict conflict = new HistoryRankConflict();
            conflict.StudentId = studentId ?? "";
            conflict.RankType = rankType ?? "";
            conflict.Names = names ?? new List<string>();
            return conflict;
        }
    }

    public sealed class HistoryRankWarning
    {
        public string StudentId { get; private set; }
        public string RankType { get; private set; }
        public string Message { get; private set; }

        internal static HistoryRankWarning Create(string studentId, string rankType, string message)
        {
            HistoryRankWarning warning = new HistoryRankWarning();
            warning.StudentId = studentId ?? "";
            warning.RankType = rankType ?? "";
            warning.Message = message ?? "";
            return warning;
        }
    }

    public sealed class HistoryRankLoad
    {
        public bool Restored { get; private set; }
        public string Message { get; private set; }
        public string Notice { get; private set; }
        public List<StudentMotherGroup> Students { get; private set; }
        public List<HistoryRankConflict> Conflicts { get; private set; }
        public List<HistoryRankWarning> Warnings { get; private set; }
        public string SelectedBatchId { get; private set; }

        internal static HistoryRankLoad Fail(string selectedBatchId, string message)
        {
            HistoryRankLoad load = new HistoryRankLoad();
            load.Restored = false;
            load.Message = message;
            load.Notice = "";
            load.Students = new List<StudentMotherGroup>();
            load.Conflicts = new List<HistoryRankConflict>();
            load.Warnings = new List<HistoryRankWarning>();
            load.SelectedBatchId = selectedBatchId ?? "";
            return load;
        }

        internal static HistoryRankLoad Success(string selectedBatchId, List<StudentMotherGroup> students, List<HistoryRankConflict> conflicts, List<HistoryRankWarning> warnings, string notice)
        {
            HistoryRankLoad load = new HistoryRankLoad();
            load.Restored = true;
            load.Message = "";
            load.Notice = notice ?? "";
            load.Students = students;
            load.Conflicts = conflicts;
            load.Warnings = warnings;
            load.SelectedBatchId = selectedBatchId ?? "";
            return load;
        }
    }

    public static class HistoryRankRestore
    {
        public const string EmptyBatchMessage = "這筆歷史批次沒有可還原的排名學生。完全沒有排名資料的學生不會出現。本次不會改用目前學生資料。";

        public static string SelectSql(string batchId)
        {
            long id;
            if (!long.TryParse(batchId == null ? "" : batchId.Trim(), out id) || id <= 0 || id.ToString() != batchId.Trim())
                throw new ArgumentException("歷史批次識別不是有效的數字，不查詢排名資料。");
            string idText = id.ToString();
            return "SELECT\n"
                + "\tstudent_id\n"
                + "\t, student_number\n"
                + "\t, student_name\n"
                + "\t, rank_type\n"
                + "\t, group_name\n"
                + "\t, name_origin\n"
                + "FROM (\n"
                + StudentNames(idText)
                + "\tUNION ALL\n"
                + RankNames(idText)
                + ") AS history_name";
        }

        public const string EmptyBatchStudentMessage = "這筆歷史批次沒有排名學生。本次不會使用目前學生名單，也不會進入確認清單。";

        public static string SelectBatchStudentSql(string batchId)
        {
            long id;
            if (!long.TryParse(batchId == null ? "" : batchId.Trim(), out id) || id <= 0 || id.ToString() != batchId.Trim())
                throw new ArgumentException("歷史批次識別不是有效的數字，不查詢排名學生。");
            string idText = id.ToString();
            return "SELECT\n"
                + "\trbs.id\n"
                + "\t, rbs.ref_batch_id\n"
                + "\t, rbs.ref_student_id\n"
                + "\t, s.student_number\n"
                + "\t, s.name AS student_name\n"
                + "\t, rbs.grade_year\n"
                + "\t, rbs.matrix_grade\n"
                + "\t, rbs.matrix_dept\n"
                + "\t, rbs.matrix_class\n"
                + "\t, rbs.matrix_tag1\n"
                + "\t, rbs.matrix_tag2\n"
                + "FROM rank_batch_student rbs\n"
                + "LEFT JOIN student s\n"
                + "\tON s.id = rbs.ref_student_id\n"
                + "WHERE rbs.ref_batch_id = " + idText + "\n"
                + "ORDER BY\n"
                + "\trbs.grade_year\n"
                + "\t, rbs.id";
        }

        public const int BatchStudentPageSize = 500;

        public static string SelectBatchStudentIdSql(string batchId)
        {
            return "SELECT\n"
                + "\tcount(rbs.id) AS row_count\n"
                + "\t, count(DISTINCT rbs.id) AS id_count\n"
                + "FROM rank_batch_student rbs\n"
                + "WHERE rbs.ref_batch_id = " + BatchIdText(batchId);
        }

        public static string SelectBatchStudentGroupCountSql(string batchId)
        {
            string idText = BatchIdText(batchId);
            return "SELECT count(*) AS group_count\n"
                + "FROM (\n"
                + "\tSELECT 1\n"
                + BatchStudentFromSql(idText)
                + "\tGROUP BY\n"
                + "\t\t" + CollapsedKeySql() + "\n"
                + ") AS collapsed";
        }

        public static string SelectBatchStudentPageSql(string batchId, long? afterCursor, int pageSize)
        {
            ValidatePageSize(pageSize);
            if (afterCursor.HasValue && afterCursor.Value < 0)
                throw new ArgumentException("歷史排名學生的 id 游標不是有效的數字。");
            string idText = BatchIdText(batchId);
            string keys = CollapsedKeySql();
            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT\n");
            sql.Append("\tcollapsed.id\n");
            sql.Append("\t, collapsed.ref_batch_id\n");
            sql.Append("\t, collapsed.ref_student_id\n");
            sql.Append("\t, collapsed.student_number\n");
            sql.Append("\t, collapsed.student_name\n");
            sql.Append("\t, collapsed.grade_year\n");
            sql.Append("\t, collapsed.matrix_grade\n");
            sql.Append("\t, collapsed.matrix_dept\n");
            sql.Append("\t, collapsed.matrix_class\n");
            sql.Append("\t, collapsed.matrix_tag1\n");
            sql.Append("\t, collapsed.matrix_tag2\n");
            sql.Append("\t, collapsed.cursor_id\n");
            sql.Append("FROM (\n");
            sql.Append("\tSELECT DISTINCT ON (\n");
            sql.Append("\t\t").Append(keys).Append("\n");
            sql.Append("\t)\n");
            sql.Append("\t\trbs.id\n");
            sql.Append("\t\t, rbs.ref_batch_id\n");
            sql.Append("\t\t, rbs.ref_student_id\n");
            sql.Append("\t\t, coalesce(s.student_number, '') AS student_number\n");
            sql.Append("\t\t, coalesce(s.name, '') AS student_name\n");
            sql.Append("\t\t, rbs.grade_year\n");
            sql.Append("\t\t, ").Append(CleanColumnSql("rbs.matrix_grade")).Append(" AS matrix_grade\n");
            sql.Append("\t\t, ").Append(CleanColumnSql("rbs.matrix_dept")).Append(" AS matrix_dept\n");
            sql.Append("\t\t, ").Append(CleanColumnSql("rbs.matrix_class")).Append(" AS matrix_class\n");
            sql.Append("\t\t, ").Append(CleanColumnSql("rbs.matrix_tag1")).Append(" AS matrix_tag1\n");
            sql.Append("\t\t, ").Append(CleanColumnSql("rbs.matrix_tag2")).Append(" AS matrix_tag2\n");
            sql.Append("\t\t, MIN(rbs.id) OVER (\n");
            sql.Append("\t\t\tPARTITION BY\n");
            sql.Append("\t\t\t\t").Append(keys).Append("\n");
            sql.Append("\t\t) AS cursor_id\n");
            sql.Append("\t").Append(BatchStudentFromSql(idText));
            sql.Append("\tORDER BY\n");
            sql.Append("\t\t").Append(keys).Append("\n");
            sql.Append("\t\t, ").Append(GradeSortSql("rbs.grade_year")).Append("\n");
            sql.Append("\t\t, rbs.id\n");
            sql.Append(") AS collapsed\n");
            if (afterCursor.HasValue)
                sql.Append("WHERE collapsed.cursor_id > ").Append(afterCursor.Value.ToString()).Append("\n");
            sql.Append("ORDER BY collapsed.cursor_id\n");
            sql.Append("LIMIT ").Append(pageSize.ToString());
            return sql.ToString();
        }

        public static DataTable ReadBatchStudentTable(string batchId, int pageSize, Func<string, DataTable> query, out int pageCount)
        {
            bool discarded;
            return ReadBatchStudentTable(batchId, pageSize, query, null, null, out pageCount, out discarded);
        }

        public static DataTable ReadBatchStudentTable(string batchId, int pageSize, Func<string, DataTable> query, Func<bool> stillCurrent, Action<int, int> pageAccepted, out int pageCount, out bool discarded)
        {
            pageCount = 0;
            discarded = false;
            if (query == null)
                throw new ArgumentNullException("query");
            ValidatePageSize(pageSize);
            DataTable countTable = query(SelectBatchStudentIdSql(batchId));
            if (StopAfterQuery(stillCurrent))
            {
                discarded = true;
                return null;
            }
            long rowCount;
            long idCount;
            if (!TryReadIdCounts(countTable, out rowCount, out idCount))
                throw new InvalidOperationException("歷史排名學生的筆數無法讀取。");
            if (rowCount != idCount)
                throw new InvalidOperationException("歷史排名學生的 id 不是唯一，無法分頁讀取。");
            if (rowCount == 0)
                return EmptyBatchStudentTable();

            DataTable groupTable = query(SelectBatchStudentGroupCountSql(batchId));
            if (StopAfterQuery(stillCurrent))
            {
                discarded = true;
                return null;
            }
            long groupCount;
            if (!TryReadGroupCount(groupTable, out groupCount))
                throw new InvalidOperationException("歷史排名學生的合併筆數無法讀取。");
            if (groupCount <= 0 || groupCount > rowCount)
                throw new InvalidOperationException("歷史排名學生的合併筆數不正確。");

            int totalPages = (int)((groupCount + pageSize - 1) / pageSize);
            DataTable all = null;
            long? after = null;
            long seen = 0;
            while (seen < groupCount)
            {
                if (StopAfterQuery(stillCurrent))
                {
                    discarded = true;
                    return null;
                }
                DataTable page = query(SelectBatchStudentPageSql(batchId, after, pageSize));
                if (StopAfterQuery(stillCurrent))
                {
                    discarded = true;
                    return null;
                }
                if (page == null)
                    throw new InvalidOperationException("歷史排名學生分頁沒有回傳。");
                int count = page.Rows.Count;
                if (count == 0 || count > pageSize)
                    throw new InvalidOperationException("歷史排名學生分頁筆數不正確。");
                if (all == null)
                    all = page.Clone();
                long previous = 0;
                bool hasPrevious = after.HasValue;
                if (hasPrevious)
                    previous = after.Value;
                long last = 0;
                foreach (DataRow row in page.Rows)
                {
                    long id;
                    if (!TryRowId(row, out id))
                        throw new InvalidOperationException("歷史排名學生分頁的 id 無法讀取。");
                    long cursor;
                    if (!TryCursor(row, out cursor))
                        throw new InvalidOperationException("歷史排名學生分頁的游標無法讀取。");
                    if (hasPrevious && cursor <= previous)
                        throw new InvalidOperationException("歷史排名學生分頁的游標沒有前進。");
                    hasPrevious = true;
                    previous = cursor;
                    last = cursor;
                    all.ImportRow(row);
                }
                pageCount++;
                seen += count;
                if (seen > groupCount)
                    throw new InvalidOperationException("歷史排名學生分頁筆數超過合併後筆數。");
                after = last;
                if (pageAccepted != null)
                    pageAccepted(pageCount, totalPages);
                if (count < pageSize)
                    break;
            }
            if (all == null || seen != groupCount)
                throw new InvalidOperationException("歷史排名學生分頁筆數與合併後筆數不一致。");
            return all;
        }

        private static bool StopAfterQuery(Func<bool> stillCurrent)
        {
            return stillCurrent != null && !stillCurrent();
        }

        private static string BatchStudentFromSql(string idText)
        {
            return "\tFROM rank_batch_student rbs\n"
                + "\tLEFT JOIN student s\n"
                + "\t\tON s.id = rbs.ref_student_id\n"
                + "\tWHERE rbs.ref_batch_id = " + idText + "\n";
        }

        private static string CollapsedKeySql()
        {
            return "rbs.ref_student_id\n"
                + "\t\t, coalesce(s.student_number, '')\n"
                + "\t\t, coalesce(s.name, '')\n"
                + "\t\t, " + CleanColumnSql("rbs.matrix_grade") + "\n"
                + "\t\t, " + CleanColumnSql("rbs.matrix_dept") + "\n"
                + "\t\t, " + CleanColumnSql("rbs.matrix_class") + "\n"
                + "\t\t, " + CleanColumnSql("rbs.matrix_tag1") + "\n"
                + "\t\t, " + CleanColumnSql("rbs.matrix_tag2");
        }

        private static string CleanColumnSql(string column)
        {
            return "CASE WHEN " + column + " IS NULL OR btrim(" + column + "::text) = '' THEN '' ELSE " + column + "::text END";
        }

        private static string GradeSortSql(string column)
        {
            return "CASE WHEN btrim(coalesce(" + column + "::text, '')) ~ '^[+-]?[0-9]{1,18}$' THEN btrim(" + column + "::text)::bigint ELSE 9223372036854775807 END";
        }

        private static string BatchIdText(string batchId)
        {
            long id;
            if (!long.TryParse(batchId == null ? "" : batchId.Trim(), out id) || id <= 0 || id.ToString() != batchId.Trim())
                throw new ArgumentException("歷史批次識別不是有效的數字，不查詢排名學生。");
            return id.ToString();
        }

        private static void ValidatePageSize(int pageSize)
        {
            if (pageSize < 1 || pageSize > 5000)
                throw new ArgumentException("歷史排名學生的分頁大小不正確。");
        }

        private static bool TryReadIdCounts(DataTable table, out long rowCount, out long idCount)
        {
            rowCount = 0;
            idCount = 0;
            if (table == null || table.Rows.Count != 1)
                return false;
            if (!table.Columns.Contains("row_count") || !table.Columns.Contains("id_count"))
                return false;
            return TryLong(table.Rows[0]["row_count"], out rowCount) && TryLong(table.Rows[0]["id_count"], out idCount);
        }

        private static bool TryReadGroupCount(DataTable table, out long groupCount)
        {
            groupCount = 0;
            if (table == null || table.Rows.Count != 1 || !table.Columns.Contains("group_count"))
                return false;
            return TryLong(table.Rows[0]["group_count"], out groupCount) && groupCount >= 0;
        }

        private static bool TryRowId(DataRow row, out long id)
        {
            id = 0;
            if (row == null || row.Table == null || !row.Table.Columns.Contains("id"))
                return false;
            return TryLong(row["id"], out id);
        }

        private static bool TryCursor(DataRow row, out long cursor)
        {
            cursor = 0;
            if (row == null || row.Table == null || !row.Table.Columns.Contains("cursor_id"))
                return false;
            return TryLong(row["cursor_id"], out cursor);
        }

        private static bool TryLong(object value, out long parsed)
        {
            parsed = 0;
            if (value == null || value == DBNull.Value)
                return false;
            return long.TryParse(value.ToString().Trim(), out parsed) && parsed >= 0;
        }

        private static DataTable EmptyBatchStudentTable()
        {
            DataTable table = new DataTable();
            string[] columns = new string[] { "id", "ref_batch_id", "ref_student_id", "student_number", "student_name", "grade_year", "matrix_grade", "matrix_dept", "matrix_class", "matrix_tag1", "matrix_tag2" };
            foreach (string column in columns)
                table.Columns.Add(column);
            return table;
        }

        public static HistoryRankLoad ReadBatchStudents(string selectedBatchId, DataTable table, string excludedCategory, IList<TagDefinition> tagDefinitions, IList<StudentTagLink> studentTags)
        {
            string batchId = selectedBatchId ?? "";
            if (table == null)
                return HistoryRankLoad.Fail(batchId, "沒有讀到歷史排名學生。本次不會使用目前學生名單。");
            string missing = MissingBatchColumn(table);
            if (missing.Length > 0)
                return HistoryRankLoad.Fail(batchId, "歷史排名學生查詢缺少欄位：" + missing + "。本次不會使用目前學生名單。");
            if (table.Rows.Count == 0)
                return HistoryRankLoad.Fail(batchId, EmptyBatchStudentMessage);

            List<BatchStudentRow> rows = new List<BatchStudentRow>();
            List<HistoryRankWarning> warnings = new List<HistoryRankWarning>();
            foreach (DataRow row in table.Rows)
            {
                string rowBatch = Cell(row, "ref_batch_id").Trim();
                if (rowBatch.Length > 0 && rowBatch != batchId)
                {
                    warnings.Add(HistoryRankWarning.Create("", "", "排名學生列的批次識別不符，已略過。"));
                    continue;
                }
                string studentId = Cell(row, "ref_student_id").Trim();
                if (studentId.Length == 0)
                {
                    warnings.Add(HistoryRankWarning.Create("", "", "排名學生列缺少學生識別，已略過。"));
                    continue;
                }
                rows.Add(new BatchStudentRow
                {
                    RowId = ParseSort(Cell(row, "id")),
                    GradeSort = ParseSort(Cell(row, "grade_year")),
                    StudentId = studentId,
                    StudentNumber = Cell(row, "student_number"),
                    StudentName = Cell(row, "student_name"),
                    YearName = CleanName(Cell(row, "matrix_grade")),
                    DeptName = CleanName(Cell(row, "matrix_dept")),
                    ClassName = CleanName(Cell(row, "matrix_class")),
                    Tag1Name = CleanName(Cell(row, "matrix_tag1")),
                    Tag2Name = CleanName(Cell(row, "matrix_tag2"))
                });
            }
            if (rows.Count == 0)
                return HistoryRankLoad.Fail(batchId, warnings.Count == 0 ? EmptyBatchStudentMessage : warnings[0].Message);

            Dictionary<string, List<BatchStudentRow>> groups = new Dictionary<string, List<BatchStudentRow>>();
            List<string> order = new List<string>();
            foreach (BatchStudentRow row in rows)
            {
                List<BatchStudentRow> group;
                if (!groups.TryGetValue(row.StudentId, out group))
                {
                    group = new List<BatchStudentRow>();
                    groups.Add(row.StudentId, group);
                    order.Add(row.StudentId);
                }
                group.Add(row);
            }
            order.Sort(delegate (string left, string right)
            {
                BatchStudentRow leftRow = FirstRow(groups[left]);
                BatchStudentRow rightRow = FirstRow(groups[right]);
                int grade = leftRow.GradeSort.CompareTo(rightRow.GradeSort);
                if (grade != 0)
                    return grade;
                return leftRow.RowId.CompareTo(rightRow.RowId);
            });

            List<string> excludedTagIds = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, excludedCategory);
            List<StudentMotherGroup> students = new List<StudentMotherGroup>();
            List<HistoryRankConflict> conflicts = new List<HistoryRankConflict>();
            foreach (string studentId in order)
            {
                List<BatchStudentRow> group = groups[studentId];
                if (group.Count > 1)
                    warnings.Add(HistoryRankWarning.Create(studentId, "", "學生 " + studentId + " 在排名學生資料有重複列。"));
                string number = Agree(group, delegate (BatchStudentRow row) { return row.StudentNumber; }, studentId, "學號", warnings);
                string name = Agree(group, delegate (BatchStudentRow row) { return row.StudentName; }, studentId, "姓名", warnings);
                List<RankSlot> slots = new List<RankSlot>();
                slots.Add(MergeSlot(group, RankKinds.Year, delegate (BatchStudentRow row) { return row.YearName; }, conflicts, warnings));
                slots.Add(MergeSlot(group, RankKinds.Department, delegate (BatchStudentRow row) { return row.DeptName; }, conflicts, warnings));
                slots.Add(MergeSlot(group, RankKinds.Class, delegate (BatchStudentRow row) { return row.ClassName; }, conflicts, warnings));
                slots.Add(MergeSlot(group, RankKinds.Tag1, delegate (BatchStudentRow row) { return row.Tag1Name; }, conflicts, warnings));
                slots.Add(MergeSlot(group, RankKinds.Tag2, delegate (BatchStudentRow row) { return row.Tag2Name; }, conflicts, warnings));

                StudentMotherGroup student = new StudentMotherGroup();
                student.StudentId = studentId;
                student.StudentNumber = number;
                student.StudentName = name;
                student.SetSourceSlots(slots);
                bool excluded = HasTag(studentTags, studentId, excludedTagIds);
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.HistorySnapshot, student, excluded);
                students.Add(student);
            }
            return HistoryRankLoad.Success(batchId, students, conflicts, warnings, BuildBatchNotice(warnings, conflicts));
        }

        public static HistoryRankLoad Read(string selectedBatchId, DataTable table, string excludedCategory, IList<TagDefinition> tagDefinitions, IList<StudentTagLink> studentTags)
        {
            string batchId = selectedBatchId ?? "";
            if (table == null)
                return HistoryRankLoad.Fail(batchId, "沒有讀到歷史排名資料。本次不會改用目前學生資料。");
            string missing = MissingColumn(table);
            if (missing.Length > 0)
                return HistoryRankLoad.Fail(batchId, "歷史排名查詢缺少欄位：" + missing + "。本次不會改用目前學生資料。");

            Dictionary<string, StudentBucket> buckets = new Dictionary<string, StudentBucket>();
            foreach (DataRow row in table.Rows)
            {
                string studentId = Cell(row, "student_id").Trim();
                if (studentId.Length == 0)
                    continue;
                string rankType = Cell(row, "rank_type");
                if (!IsRankType(rankType))
                    continue;
                StudentBucket bucket;
                if (!buckets.TryGetValue(studentId, out bucket))
                {
                    bucket = new StudentBucket();
                    bucket.StudentId = studentId;
                    buckets.Add(studentId, bucket);
                }
                bucket.AddIdentity(Cell(row, "student_number"), Cell(row, "student_name"));
                bucket.Observations.Add(new NameObservation
                {
                    RankType = rankType,
                    GroupName = Cell(row, "group_name"),
                    FromStudentRow = Cell(row, "name_origin") == "student"
                });
            }

            if (buckets.Count == 0)
                return HistoryRankLoad.Fail(batchId, EmptyBatchMessage);

            List<string> excludedTagIds = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, excludedCategory);
            List<StudentMotherGroup> students = new List<StudentMotherGroup>();
            List<HistoryRankConflict> conflicts = new List<HistoryRankConflict>();
            List<HistoryRankWarning> warnings = new List<HistoryRankWarning>();
            foreach (StudentBucket bucket in buckets.Values)
            {
                List<RankSlot> slots = new List<RankSlot>();
                foreach (string rankType in RankKinds.All)
                    slots.Add(RestoreSlot(bucket, rankType, conflicts, warnings));

                StudentMotherGroup student = new StudentMotherGroup();
                student.StudentId = bucket.StudentId;
                student.StudentNumber = bucket.StudentNumber;
                student.StudentName = bucket.StudentName;
                student.SetSourceSlots(slots);
                bool excluded = HasTag(studentTags, bucket.StudentId, excludedTagIds);
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.HistorySnapshot, student, excluded);
                students.Add(student);
            }

            return HistoryRankLoad.Success(batchId, students, conflicts, warnings, BuildNotice(conflicts, warnings));
        }

        private static string StudentNames(string idText)
        {
            return "\tSELECT\n"
                + "\t\trank_source.ref_student_id::TEXT AS student_id\n"
                + "\t\t, COALESCE(student.student_number, '') AS student_number\n"
                + "\t\t, COALESCE(student.name, '') AS student_name\n"
                + "\t\t, rank_source.rank_type\n"
                + "\t\t, COALESCE(rank_source.group_name, '') AS group_name\n"
                + "\t\t, 'student' AS name_origin\n"
                + "\tFROM (\n"
                + StudentRank(idText, RankKinds.Year, "matrix_grade")
                + "\t\tUNION ALL\n"
                + StudentRank(idText, RankKinds.Class, "matrix_class")
                + "\t\tUNION ALL\n"
                + StudentRank(idText, RankKinds.Tag1, "matrix_tag1")
                + "\t\tUNION ALL\n"
                + StudentRank(idText, RankKinds.Tag2, "matrix_tag2")
                + "\t) AS rank_source\n"
                + "\tLEFT OUTER JOIN student\n"
                + "\t\tON student.id = rank_source.ref_student_id\n";
        }

        private static string StudentRank(string idText, string rankType, string column)
        {
            return "\t\tSELECT ref_batch_id, ref_student_id, '" + rankType + "'::TEXT AS rank_type, " + column + " AS group_name\n"
                + "\t\tFROM rank_batch_student\n"
                + "\t\tWHERE ref_batch_id = " + idText + "\n";
        }

        private static string RankNames(string idText)
        {
            return "\tSELECT DISTINCT\n"
                + "\t\trank_detail.ref_student_id::TEXT AS student_id\n"
                + "\t\t, COALESCE(student.student_number, '') AS student_number\n"
                + "\t\t, COALESCE(student.name, '') AS student_name\n"
                + "\t\t, rank_matrix.rank_type\n"
                + "\t\t, COALESCE(rank_matrix.rank_name, '') AS group_name\n"
                + "\t\t, 'rank' AS name_origin\n"
                + "\tFROM rank_matrix\n"
                + "\tINNER JOIN rank_detail\n"
                + "\t\tON rank_detail.ref_matrix_id = rank_matrix.id\n"
                + "\tLEFT OUTER JOIN student\n"
                + "\t\tON student.id = rank_detail.ref_student_id\n"
                + "\tWHERE rank_matrix.ref_batch_id = " + idText + "\n"
                + "\t\tAND rank_matrix.rank_type IN ('年排名', '科排名', '班排名', '類別1排名', '類別2排名')\n";
        }

        private static RankSlot RestoreSlot(StudentBucket bucket, string rankType, List<HistoryRankConflict> conflicts, List<HistoryRankWarning> warnings)
        {
            List<string> names = DistinctNames(bucket, rankType);
            RankSlot slot = new RankSlot();
            slot.RankType = rankType;
            slot.GroupName = "";
            slot.Participate = false;
            if (names.Count > 1)
            {
                slot.ConflictNames = new List<string>();
                foreach (string name in names)
                    slot.ConflictNames.Add(name);
                conflicts.Add(HistoryRankConflict.Create(bucket.StudentId, rankType, slot.ConflictNames));
                return slot;
            }
            if (names.Count == 1)
            {
                slot.GroupName = names[0];
                slot.Participate = RankParticipates(rankType, names[0]);
                return slot;
            }
            if (rankType == RankKinds.Department || rankType == RankKinds.Year || rankType == RankKinds.Class)
            {
                warnings.Add(HistoryRankWarning.Create(bucket.StudentId, rankType, MissingMessage(bucket.StudentId, rankType)));
            }
            return slot;
        }

        private static bool RankParticipates(string rankType, string name)
        {
            if (rankType == RankKinds.Department)
                return true;
            return !string.IsNullOrEmpty(name);
        }

        private static List<string> DistinctNames(StudentBucket bucket, string rankType)
        {
            bool hasStudentRow = false;
            foreach (NameObservation observation in bucket.Observations)
            {
                if (observation.FromStudentRow)
                    hasStudentRow = true;
            }
            List<string> names = new List<string>();
            foreach (NameObservation observation in bucket.Observations)
            {
                if (observation.RankType != rankType)
                    continue;
                if (rankType == RankKinds.Department)
                {
                    if (observation.FromStudentRow)
                        continue;
                }
                else if (hasStudentRow && !observation.FromStudentRow)
                {
                    continue;
                }
                else if (!hasStudentRow && observation.FromStudentRow)
                {
                    continue;
                }
                string name = observation.GroupName ?? "";
                if (!names.Contains(name))
                    names.Add(name);
            }
            return names;
        }

        private static string MissingMessage(string studentId, string rankType)
        {
            return "學生 " + studentId + " 沒有" + rankType + "紀錄，名稱留白、不參與。";
        }

        private static string BuildNotice(List<HistoryRankConflict> conflicts, List<HistoryRankWarning> warnings)
        {
            StringBuilder text = new StringBuilder();
            text.Append("沒有版本 1 快照，已依這筆歷史排名還原。完全沒有排名資料的學生不會出現。");
            foreach (HistoryRankWarning warning in warnings)
            {
                text.Append("\n");
                text.Append(warning.Message);
            }
            foreach (HistoryRankConflict conflict in conflicts)
            {
                text.Append("\n學生 ");
                text.Append(conflict.StudentId);
                text.Append(" 的");
                text.Append(conflict.RankType);
                text.Append("同時有");
                for (int i = 0; i < conflict.Names.Count; i++)
                {
                    if (i > 0)
                        text.Append("、");
                    text.Append("「");
                    text.Append(conflict.Names[i]);
                    text.Append("」");
                }
                text.Append("，不任取其中一筆。");
            }
            return text.ToString();
        }

        private delegate string BatchField(BatchStudentRow row);

        private static string MissingBatchColumn(DataTable table)
        {
            string[] columns = new string[] { "id", "ref_batch_id", "ref_student_id", "student_number", "student_name", "grade_year", "matrix_grade", "matrix_dept", "matrix_class", "matrix_tag1", "matrix_tag2" };
            foreach (string column in columns)
            {
                if (!table.Columns.Contains(column))
                    return column;
            }
            return "";
        }

        private static BatchStudentRow FirstRow(List<BatchStudentRow> rows)
        {
            BatchStudentRow best = rows[0];
            for (int i = 1; i < rows.Count; i++)
            {
                int grade = rows[i].GradeSort.CompareTo(best.GradeSort);
                if (grade < 0 || (grade == 0 && rows[i].RowId < best.RowId))
                    best = rows[i];
            }
            return best;
        }

        private static string Agree(List<BatchStudentRow> rows, BatchField field, string studentId, string label, List<HistoryRankWarning> warnings)
        {
            string chosen = null;
            bool conflict = false;
            foreach (BatchStudentRow row in rows)
            {
                string value = field(row) ?? "";
                if (value.Length == 0)
                    continue;
                if (chosen == null)
                    chosen = value;
                else if (chosen != value)
                    conflict = true;
            }
            if (conflict)
            {
                warnings.Add(HistoryRankWarning.Create(studentId, "", "學生 " + studentId + " 的" + label + "不一致，確認清單不選定" + label + "。"));
                return "";
            }
            return chosen ?? "";
        }

        private static RankSlot MergeSlot(List<BatchStudentRow> rows, string rankType, BatchField field, List<HistoryRankConflict> conflicts, List<HistoryRankWarning> warnings)
        {
            List<string> names = new List<string>();
            foreach (BatchStudentRow row in rows)
            {
                string name = field(row) ?? "";
                if (!names.Contains(name))
                    names.Add(name);
            }
            RankSlot slot = new RankSlot();
            slot.RankType = rankType;
            slot.GroupName = "";
            slot.Participate = false;
            if (names.Count > 1)
            {
                slot.ConflictNames = new List<string>();
                foreach (string name in names)
                    slot.ConflictNames.Add(name);
                conflicts.Add(HistoryRankConflict.Create(rows[0].StudentId, rankType, slot.ConflictNames));
                return slot;
            }
            string only = names.Count == 0 ? "" : names[0];
            slot.GroupName = only;
            if (only.Length > 0)
            {
                slot.Participate = true;
                return slot;
            }
            warnings.Add(HistoryRankWarning.Create(rows[0].StudentId, rankType, "學生 " + rows[0].StudentId + " 的" + rankType + "母群是空白，預設不參與。"));
            return slot;
        }

        private static string BuildBatchNotice(List<HistoryRankWarning> warnings, List<HistoryRankConflict> conflicts)
        {
            StringBuilder text = new StringBuilder();
            text.Append("這筆歷史批次沒有母群快照，已依排名學生資料還原。有名稱的母群預設參與，空白母群不參與。");
            foreach (HistoryRankWarning warning in warnings)
            {
                if (warning.Message.Length == 0)
                    continue;
                text.Append("\n");
                text.Append(warning.Message);
            }
            foreach (HistoryRankConflict conflict in conflicts)
            {
                text.Append("\n學生 ");
                text.Append(conflict.StudentId);
                text.Append(" 的");
                text.Append(conflict.RankType);
                text.Append("同時有");
                for (int i = 0; i < conflict.Names.Count; i++)
                {
                    if (i > 0)
                        text.Append("、");
                    text.Append("「");
                    text.Append(conflict.Names[i]);
                    text.Append("」");
                }
                text.Append("，不任取其中一筆。");
            }
            return text.ToString();
        }

        private static long ParseSort(string text)
        {
            long value;
            if (!long.TryParse(text == null ? "" : text.Trim(), out value))
                return long.MaxValue;
            return value;
        }

        private static string CleanName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            return value;
        }

        private sealed class BatchStudentRow
        {
            public long RowId;
            public long GradeSort;
            public string StudentId;
            public string StudentNumber;
            public string StudentName;
            public string YearName;
            public string DeptName;
            public string ClassName;
            public string Tag1Name;
            public string Tag2Name;
        }

        private static string MissingColumn(DataTable table)
        {
            string[] columns = new string[] { "student_id", "student_number", "student_name", "rank_type", "group_name", "name_origin" };
            foreach (string column in columns)
            {
                if (!table.Columns.Contains(column))
                    return column;
            }
            return "";
        }

        private static string Cell(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value)
                return "";
            return value.ToString();
        }

        private static bool IsRankType(string rankType)
        {
            foreach (string known in RankKinds.All)
            {
                if (known == rankType)
                    return true;
            }
            return false;
        }

        private static bool HasTag(IList<StudentTagLink> studentTags, string studentId, IList<string> tagIds)
        {
            if (studentTags == null || tagIds == null || tagIds.Count == 0)
                return false;
            foreach (StudentTagLink link in studentTags)
            {
                if (link != null && link.StudentId == studentId && tagIds.Contains(link.TagId))
                    return true;
            }
            return false;
        }

        private sealed class StudentBucket
        {
            public string StudentId;
            public string StudentNumber = "";
            public string StudentName = "";
            public List<NameObservation> Observations = new List<NameObservation>();
            private string _number;
            private string _name;
            private bool _numberConflict;
            private bool _nameConflict;

            public void AddIdentity(string number, string name)
            {
                if (!string.IsNullOrEmpty(number))
                {
                    if (_number == null)
                        _number = number;
                    else if (_number != number)
                        _numberConflict = true;
                }
                if (!string.IsNullOrEmpty(name))
                {
                    if (_name == null)
                        _name = name;
                    else if (_name != name)
                        _nameConflict = true;
                }
                StudentNumber = _numberConflict ? "" : (_number ?? "");
                StudentName = _nameConflict ? "" : (_name ?? "");
            }
        }

        private sealed class NameObservation
        {
            public string RankType;
            public string GroupName;
            public bool FromStudentRow;
        }
    }
}
