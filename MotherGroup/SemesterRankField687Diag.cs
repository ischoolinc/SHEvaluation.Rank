using FISCA.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class SemesterRankField687Plan
    {
        public int StudentCount { get; set; }
        public int ParticipateYear { get; set; }
        public int ParticipateDept { get; set; }
        public int ParticipateClass { get; set; }
        public int ParticipateTag1 { get; set; }
        public int ParticipateTag2 { get; set; }
        public string PublishSql { get; set; }
        public string ReadOnlyCandidateSql { get; set; }
        public string FullPublishExplainSql { get; set; }
        public int PublishSqlBytes { get; set; }
    }

    /// <summary>
    /// 依現場 114-2／目前學生／三年級／原始成績／指定類別，組出與產品相同的公布語句與唯讀診斷。
    /// </summary>
    public static class SemesterRankField687Diag
    {
        public const string SchoolYear = "114";
        public const string Semester = "2";
        public const int GradeYear = 3;
        public const int RefExamId = -1;
        public const string ExcludedCategory = "不排名學生";
        public const string Tag1Category = "類組排名";
        public const string Tag2Category = "全校排名";
        public const string SourceName = "依目前學生資料產生";

        public static SemesterRankField687Plan Build(QueryHelper helper)
        {
            if (helper == null)
                throw new ArgumentNullException("helper");

            List<CurrentStudentRow> students = LoadStudents(helper);
            List<TagDefinition> tags = LoadTags(helper);
            List<StudentTagLink> links = LoadStudentTags(helper);
            List<string> studentIds = new List<string>();
            foreach (CurrentStudentRow student in students)
            {
                if (CurrentStudentMotherGroupBuilder.IsCurrentPopulation(student) && student.GradeYear == GradeYear)
                    studentIds.Add(student.StudentId);
            }
            Dictionary<string, string> depts = LoadDepts(helper, studentIds);
            List<StudentMotherGroup> groups = CurrentStudentMotherGroupBuilder.Build(
                students,
                tags,
                links,
                new int[] { GradeYear },
                ExcludedCategory,
                Tag1Category,
                Tag2Category,
                depts);
            foreach (StudentMotherGroup group in groups)
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, group, group.MatchesExcludedCategory);

            MotherGroupConfirmList confirm = new MotherGroupConfirmList();
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (StudentMotherGroup group in groups)
            {
                ConfirmStudentRow row = new ConfirmStudentRow();
                row.Group = group;
                rows.Add(row);
            }
            confirm.Load(rows);
            if (confirm.Rows.Count == 0)
                throw new InvalidOperationException("沒有載入到三年級目前學生。");

            SemesterRankStudentSqlResult rankStudents = SemesterRankSql.Build(confirm);
            string formula = SemesterRankFormulaSql.BuildOriginOnly();
            AssertExecutableFormula(formula);
            XmlDocument setting = new XmlDocument();
            setting.LoadXml("<Setting />");
            XmlElement root = setting.DocumentElement;
            root.SetAttribute("學年度", SchoolYear);
            root.SetAttribute("學期", Semester);
            root.SetAttribute("考試名稱", "學期成績");
            root.SetAttribute("不排名學生類別", ExcludedCategory);
            root.SetAttribute("類別一", Tag1Category);
            root.SetAttribute("類別二", Tag2Category);
            root.SetAttribute("成績選項", "原始成績");
            string extension = MotherGroupSnapshotJson.Compose("[]", confirm, MotherGroupSourceKind.Current, "");
            SemesterRankPublishStatement publish = SemesterRankPublishSql.Build(
                rankStudents.Grades,
                SchoolYear,
                Semester,
                setting.OuterXml,
                extension,
                formula,
                false);
            string applied = SemesterRankSql.ApplyParticipation(publish.Sql);

            SemesterRankField687Plan plan = new SemesterRankField687Plan();
            plan.StudentCount = confirm.Rows.Count;
            CountParticipation(confirm, plan);
            plan.PublishSql = applied;
            plan.PublishSqlBytes = Encoding.UTF8.GetByteCount(applied);
            plan.ReadOnlyCandidateSql = SemesterRankMatrixDiagSql.ToReadOnlyCandidateSql(applied, int.Parse(SchoolYear), int.Parse(Semester), GradeYear, RefExamId);
            plan.FullPublishExplainSql = SemesterRankMatrixDiagSql.ToFullPublishExplain(applied);
            return plan;
        }

        private static void AssertExecutableFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula) || formula.IndexOf(", subject_score AS") < 0)
                throw new InvalidOperationException("執行時公式缺少 subject_score。");
            string[] forbidden = new string[] { "+ schoolYearAdjScore", "+ @\"", "string insertRankSql", "//,", "ExtractOriginOnlyFormula" };
            for (int i = 0; i < forbidden.Length; i++)
            {
                if (formula.IndexOf(forbidden[i]) >= 0)
                    throw new InvalidOperationException("執行時公式殘留 C# 原始碼：" + forbidden[i]);
            }
        }

        private static void CountParticipation(MotherGroupConfirmList confirm, SemesterRankField687Plan plan)
        {
            foreach (ConfirmStudentRow row in confirm.Rows)
            {
                if (row == null || row.Group == null)
                    continue;
                if (row.Group.GetWorking(RankKinds.Year).Participate)
                    plan.ParticipateYear++;
                if (row.Group.GetWorking(RankKinds.Department).Participate)
                    plan.ParticipateDept++;
                if (row.Group.GetWorking(RankKinds.Class).Participate)
                    plan.ParticipateClass++;
                if (row.Group.GetWorking(RankKinds.Tag1).Participate)
                    plan.ParticipateTag1++;
                if (row.Group.GetWorking(RankKinds.Tag2).Participate)
                    plan.ParticipateTag2++;
            }
        }

        private static List<CurrentStudentRow> LoadStudents(QueryHelper helper)
        {
            string sql = @"
SELECT
	student.id::TEXT AS student_id
	, COALESCE(student.student_number, '') AS student_number
	, COALESCE(student.name, '') AS student_name
	, student.status AS status_value
	, student.ref_class_id::TEXT AS ref_class_id
	, class.grade_year AS grade_year
	, COALESCE(class.class_name, '') AS class_name
FROM
	student
	INNER JOIN class
		ON class.id = student.ref_class_id
WHERE
	student.status = 1
	AND class.grade_year = " + GradeYear + @"
";
            DataTable table = helper.Select(sql);
            List<CurrentStudentRow> rows = new List<CurrentStudentRow>();
            if (table == null)
                return rows;
            foreach (DataRow dataRow in table.Rows)
            {
                CurrentStudentRow row = new CurrentStudentRow();
                row.StudentId = Convert.ToString(dataRow["student_id"]);
                row.StudentNumber = Convert.ToString(dataRow["student_number"]);
                row.StudentName = Convert.ToString(dataRow["student_name"]);
                row.StatusValue = Convert.ToInt32(dataRow["status_value"]);
                row.IsRegular = row.StatusValue == 1;
                row.RefClassId = Convert.ToString(dataRow["ref_class_id"]);
                row.GradeYear = Convert.ToInt32(dataRow["grade_year"]);
                row.ClassName = Convert.ToString(dataRow["class_name"]);
                rows.Add(row);
            }
            return rows;
        }

        private static List<TagDefinition> LoadTags(QueryHelper helper)
        {
            DataTable table = helper.Select("SELECT id::TEXT AS id, COALESCE(prefix, '') AS prefix, COALESCE(name, '') AS name FROM tag");
            List<TagDefinition> tags = new List<TagDefinition>();
            if (table == null)
                return tags;
            foreach (DataRow dataRow in table.Rows)
            {
                tags.Add(new TagDefinition
                {
                    Id = Convert.ToString(dataRow["id"]),
                    Prefix = Convert.ToString(dataRow["prefix"]),
                    Name = Convert.ToString(dataRow["name"])
                });
            }
            return tags;
        }

        private static List<StudentTagLink> LoadStudentTags(QueryHelper helper)
        {
            DataTable table = helper.Select(@"
SELECT
	tag_student.ref_student_id::TEXT AS student_id
	, tag_student.ref_tag_id::TEXT AS tag_id
	, COALESCE(tag.name, '') AS name
FROM
	tag_student
	INNER JOIN tag
		ON tag.id = tag_student.ref_tag_id
");
            List<StudentTagLink> links = new List<StudentTagLink>();
            if (table == null)
                return links;
            foreach (DataRow dataRow in table.Rows)
            {
                links.Add(new StudentTagLink
                {
                    StudentId = Convert.ToString(dataRow["student_id"]),
                    TagId = Convert.ToString(dataRow["tag_id"]),
                    Name = Convert.ToString(dataRow["name"])
                });
            }
            return links;
        }

        private static Dictionary<string, string> LoadDepts(QueryHelper helper, IList<string> studentIds)
        {
            Dictionary<string, string> depts = new Dictionary<string, string>();
            if (studentIds == null || studentIds.Count == 0)
                return depts;
            DataTable table = helper.Select(CurrentStudentDeptQuery.Create(studentIds));
            if (table == null)
                return depts;
            foreach (DataRow dataRow in table.Rows)
                depts[Convert.ToString(dataRow["student_id"])] = Convert.ToString(dataRow["deptname"]);
            return depts;
        }
    }
}
