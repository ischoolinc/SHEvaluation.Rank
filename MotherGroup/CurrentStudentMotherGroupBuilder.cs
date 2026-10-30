using System.Collections.Generic;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class CurrentStudentRow
    {
        public string StudentId { get; set; }
        public string StudentNumber { get; set; }
        public string StudentName { get; set; }
        public bool IsRegular { get; set; }
        public int? StatusValue { get; set; }
        public string RefClassId { get; set; }
        public int? GradeYear { get; set; }
        public string ClassName { get; set; }
    }

    public sealed class TagDefinition
    {
        public string Id { get; set; }
        public string Prefix { get; set; }
        public string Name { get; set; }
    }

    public sealed class StudentTagLink
    {
        public string StudentId { get; set; }
        public string TagId { get; set; }
        public string Name { get; set; }
    }

    public static class CurrentStudentDeptQuery
    {
        public static string Create(IEnumerable<string> studentIds)
        {
            string studentIDs = string.Join(",", studentIds);
            return @"
SELECT
	student.id AS student_id
	, COALESCE(dept.name, '') AS deptname
FROM
	student
	LEFT OUTER JOIN class
		ON class.id = student.ref_class_id
	LEFT OUTER JOIN dept
		ON dept.id = COALESCE(student.ref_dept_id, class.ref_dept_id)
WHERE
	student.id IN
	(
		" + studentIDs + @"
	)
";
        }
    }

    public static class CurrentStudentMotherGroupBuilder
    {
        public static List<StudentMotherGroup> Build(
            IList<CurrentStudentRow> students,
            IList<TagDefinition> tagDefinitions,
            IList<StudentTagLink> studentTags,
            IList<int> selectedGradeYears,
            string excludedCategory,
            string tag1Category,
            string tag2Category,
            IDictionary<string, string> deptNameByStudentId)
        {
            List<StudentMotherGroup> groups = new List<StudentMotherGroup>();
            if (students == null)
                return groups;

            List<string> excludedTagIds = ResolveTagIds(tagDefinitions, excludedCategory);
            List<string> tag1Ids = ResolveTagIds(tagDefinitions, tag1Category);
            List<string> tag2Ids = ResolveTagIds(tagDefinitions, tag2Category);

            foreach (CurrentStudentRow student in students)
            {
                if (student == null || !IsCurrentPopulation(student))
                    continue;
                if (selectedGradeYears == null || !selectedGradeYears.Contains(student.GradeYear.Value))
                    continue;

                string tag1Name = FirstTagName(studentTags, student.StudentId, tag1Ids);
                string tag2Name = FirstTagName(studentTags, student.StudentId, tag2Ids);
                string deptName = "";
                if (deptNameByStudentId != null && deptNameByStudentId.ContainsKey(student.StudentId))
                    deptName = deptNameByStudentId[student.StudentId] ?? "";

                StudentMotherGroup group = new StudentMotherGroup();
                group.StudentId = student.StudentId;
                group.StudentNumber = student.StudentNumber;
                group.StudentName = student.StudentName;
                group.Status = student.StatusValue;
                group.MatchesExcludedCategory = HasTag(studentTags, student.StudentId, excludedTagIds);
                group.SetSourceSlots(new RankSlot[]
                {
                    new RankSlot { RankType = RankKinds.Year, GroupName = student.GradeYear.Value + "年級", Participate = true },
                    new RankSlot { RankType = RankKinds.Department, GroupName = deptName, Participate = true },
                    new RankSlot { RankType = RankKinds.Class, GroupName = student.ClassName ?? "", Participate = true },
                    new RankSlot { RankType = RankKinds.Tag1, GroupName = tag1Name, Participate = tag1Name != "" },
                    new RankSlot { RankType = RankKinds.Tag2, GroupName = tag2Name, Participate = tag2Name != "" }
                });
                groups.Add(group);
            }
            return groups;
        }

        public static bool IsCurrentPopulation(CurrentStudentRow student)
        {
            return student.IsRegular
                && !string.IsNullOrEmpty(student.RefClassId)
                && student.GradeYear.HasValue;
        }

        public static List<string> ResolveTagIds(IList<TagDefinition> tagDefinitions, string category)
        {
            List<string> ids = new List<string>();
            if (string.IsNullOrEmpty(category) || tagDefinitions == null)
                return ids;

            foreach (TagDefinition tag in tagDefinitions)
            {
                if (tag != null && tag.Prefix == category)
                    ids.Add(tag.Id);
            }
            if (ids.Count > 0)
                return ids;

            foreach (TagDefinition tag in tagDefinitions)
            {
                if (tag != null && tag.Name == category)
                    ids.Add(tag.Id);
            }
            return ids;
        }

        public static string FirstTagName(IList<StudentTagLink> studentTags, string studentId, IList<string> tagIds)
        {
            if (studentTags == null || tagIds == null || tagIds.Count == 0)
                return "";
            foreach (StudentTagLink link in studentTags)
            {
                if (link != null && link.StudentId == studentId && tagIds.Contains(link.TagId))
                    return link.Name ?? "";
            }
            return "";
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
    }
}
