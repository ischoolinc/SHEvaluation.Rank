using SHEvaluation.Rank.MotherGroup;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankAtomicPublishSql
    {
        // A single statement owns the whole ranking publication, including every selected grade.
        public static string Build(RegularRankConfirmRequest request,
            MotherGroupConfirmList population, string operationId, bool injectFailure)
        {
            if (!SemesterRankPublishSql.IsOperationId(operationId)) throw new ArgumentException("操作識別無效。");
            string candidate = RegularRankCandidateSql.Build(request, population);
            List<int> grades = RegularRankCandidateSql.ActualGrades(request, population);
            List<string> fullStudents = new List<string>();
            foreach (ConfirmStudentRow student in population.Rows)
            {
                int grade;
                if (SemesterRankSql.TryParseGradeYear(student.Group.GetWorking(RankKinds.Year).GroupName, out grade) && grades.Contains(grade))
                    fullStudents.Add(RegularRankCandidateSql.StudentSql(student.Group, grade));
            }
            XmlDocument document = new XmlDocument { XmlResolver = null };
            document.LoadXml(RegularRankSnapshotDocument.BuildSettingXml(request));
            document.DocumentElement.SetAttribute("操作識別", operationId);
            string setting = RegularRankCandidateSql.Literal(document.OuterXml);
            string extension = RegularRankCandidateSql.Literal(RegularRankSnapshotDocument.ComposeExtension("[]", population, request));
            string year = request.TargetSchoolYear;
            string term = request.TargetSemester;
            string exam = request.TargetExamId;
            int key1, key2;
            // The scope contains no student data and is stable across clients.
            using (System.Security.Cryptography.SHA256 hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes("regular-rank:" + year + ":" + term + ":" + exam));
                key1 = ReadInt32(bytes, 0); key2 = ReadInt32(bytes, 4);
            }
            string scopeLock = "pg_try_advisory_xact_lock(" + key1.ToString(CultureInfo.InvariantCulture) + "," + key2.ToString(CultureInfo.InvariantCulture) + ")";
            List<string> gradeText = new List<string>();
            foreach (int grade in grades) gradeText.Add(grade.ToString(CultureInfo.InvariantCulture));
            string actual = string.Join(",", gradeText.ToArray());
            string matrix = RegularRankMatrixInsertSql.Cte.Replace("CROSS JOIN row", "INNER JOIN row ON row.rank_grade_year::INT = score_list.rank_grade_year");
            return "WITH regular_lock AS (SELECT " + scopeLock + " AS acquired, "
                + SemesterRankPublishSql.AdvisoryLockCall(operationId) + " AS operation_lock),\n"
                + "score_list AS (" + candidate + "),\n"
                + "regular_full_students AS (" + string.Join("\nUNION ALL\n", fullStudents.ToArray()) + "),\n"
                + "regular_guard AS (SELECT 1 / CASE WHEN acquired AND (SELECT count(DISTINCT rank_grade_year) FROM score_list) = " + grades.Count
                + " AND NOT EXISTS (SELECT 1 FROM rank_batch WHERE strpos(setting, " + RegularRankCandidateSql.Literal(SemesterRankPublishSql.OperationAttribute(operationId)) + ") > 0) THEN 1 ELSE 0 END AS ok FROM regular_lock),\n"
                + "insert_batch_data AS (INSERT INTO rank_batch(school_year,semester,calculation_description,setting,extension) SELECT "
                + year + "," + term + "," + RegularRankCandidateSql.Literal(year + " " + term + " 計算" + request.TargetExamName + "排名")
                + "," + setting + "," + extension + " FROM regular_guard WHERE ok=1 RETURNING *),\n"
                + "row AS (SELECT " + year + "::INT AS rank_school_year," + term + "::INT AS rank_semester," + exam + "::INT AS ref_exam_id,"
                + "g AS rank_grade_year,b.id AS batch_id FROM insert_batch_data b CROSS JOIN unnest(ARRAY[" + actual + "]) g),\n"
                + "previous_matrices AS (SELECT m.id FROM rank_matrix m INNER JOIN row ON m.school_year=row.rank_school_year"
                + " AND m.semester=row.rank_semester AND m.grade_year=row.rank_grade_year AND m.ref_exam_id=row.ref_exam_id"
                + " WHERE m.is_alive=true AND m.item_type LIKE '定期評量/%'),\n"
                + "update_data AS (UPDATE rank_matrix m SET is_alive=NULL FROM previous_matrices p WHERE m.id=p.id AND m.is_alive=true RETURNING m.id),\n"
                + matrix + ",\n"
                + "insert_batch_student_data AS (INSERT INTO rank_batch_student(ref_batch_id,ref_student_id,grade_year,matrix_grade,matrix_dept,matrix_class,matrix_tag1,matrix_tag2)"
                + " SELECT b.id,s.student_id,s.rank_grade_year,s.rank_grade_year || '年級',s.rank_dept_name,s.rank_class_name,s.rank_tag1,s.rank_tag2 FROM regular_full_students s CROSS JOIN insert_batch_data b RETURNING *),\n"
                + "insert_detail_data AS (INSERT INTO rank_detail(ref_matrix_id,ref_student_id,score,rank,percentile,pr) SELECT m.id,s.student_id,s.score,s.rank,s.percentile,s.pr"
                + " FROM score_list s INNER JOIN insert_matrix_data m ON m.school_year=s.rank_school_year AND m.semester=s.rank_semester AND m.grade_year=s.rank_grade_year"
                + " AND m.item_type=s.item_type AND m.ref_exam_id=s.ref_exam_id AND m.item_name=s.item_name AND m.rank_type=s.rank_type AND m.rank_name=s.rank_name RETURNING *),\n"
                + "result_guard AS (SELECT 1 / CASE WHEN " + (injectFailure ? "(SELECT count(*) FROM insert_detail_data) < 0" : "true")
                + " AND (SELECT count(*) FROM insert_detail_data) = (SELECT count(*) FROM score_list)"
                + " AND (SELECT count(*) FROM insert_matrix_data) > 0 AND (SELECT count(*) FROM insert_detail_data) > 0"
                + " AND (SELECT count(*) FROM insert_batch_student_data) = (SELECT count(*) FROM regular_full_students)"
                + " AND (SELECT count(*) FROM update_data) = (SELECT count(*) FROM previous_matrices)"
                + " THEN 1 ELSE 0 END AS ok)\n"
                + "SELECT b.id AS batch_id,(SELECT count(*) FROM insert_detail_data) AS detail_count,(SELECT count(*) FROM insert_matrix_data) AS matrix_count"
                + " FROM insert_batch_data b CROSS JOIN result_guard WHERE result_guard.ok=1";
        }

        private static int ReadInt32(byte[] bytes, int at)
        {
            return unchecked((int)((uint)bytes[at] << 24 | (uint)bytes[at + 1] << 16 | (uint)bytes[at + 2] << 8 | bytes[at + 3]));
        }
    }
}
