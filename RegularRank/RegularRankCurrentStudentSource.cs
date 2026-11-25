using SHEvaluation.Rank.MotherGroup;
using System.Collections.Generic;

namespace SHEvaluation.Rank.RegularRank
{
    public sealed class RegularRankLoadOutcome
    {
        public string Error = "";
        public string Warning = "";
        public string ValidationError = "";
        public RegularRankDatabaseEvidence DatabaseEvidence;
        public List<ConfirmStudentRow> Rows;
        public List<string> Subjects;
    }

    public static class RegularRankCurrentStudentSource
    {
        public const int ExtendedStatus = 2;
        public const string EmptyPopulationMessage = "沒有符合目前一般與延修、且有班級及年級資格的學生。";
        public const string DuplicateStudentMessage = "目前學生清單有重複的學生識別，不能任取一筆。";

        public static string NormalizeCategory(string text)
        {
            return (text ?? "").Trim().Trim('[', ']');
        }

        public static bool IsEligible(CurrentStudentRow student)
        {
            if (student == null)
                return false;
            bool regularOrExtended = student.IsRegular || student.StatusValue == ExtendedStatus;
            return regularOrExtended
                && !string.IsNullOrEmpty(student.RefClassId)
                && student.GradeYear.HasValue;
        }

        public static string Build(
            IList<CurrentStudentRow> students,
            IDictionary<string, string> seatByStudentId,
            IList<TagDefinition> tagDefinitions,
            IList<StudentTagLink> studentTags,
            IList<int> selectedGradeYears,
            string excludedCategory,
            string tag1Category,
            string tag2Category,
            IDictionary<string, string> deptNameByStudentId,
            out List<ConfirmStudentRow> rows)
        {
            rows = new List<ConfirmStudentRow>();
            if (selectedGradeYears == null || selectedGradeYears.Count == 0)
                return "尚未選擇年級。";

            string excluded = NormalizeCategory(excludedCategory);
            string tag1 = NormalizeCategory(tag1Category);
            string tag2 = NormalizeCategory(tag2Category);
            List<string> excludedTagIds = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, excluded);
            List<string> tag1Ids = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, tag1);
            List<string> tag2Ids = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, tag2);
            List<string> seenIds = new List<string>();

            if (students != null)
            {
                foreach (CurrentStudentRow student in students)
                {
                    if (!IsEligible(student) || !selectedGradeYears.Contains(student.GradeYear.Value))
                        continue;
                    string studentId = student.StudentId ?? "";
                    if (studentId.Length == 0)
                        return "學生識別是空白，不能建立目前母群。";
                    if (seenIds.Contains(studentId))
                        return DuplicateStudentMessage;
                    seenIds.Add(studentId);

                    string tag1Name = CurrentStudentMotherGroupBuilder.FirstTagName(studentTags, studentId, tag1Ids);
                    string tag2Name = CurrentStudentMotherGroupBuilder.FirstTagName(studentTags, studentId, tag2Ids);
                    string deptName = "";
                    if (deptNameByStudentId != null && deptNameByStudentId.ContainsKey(studentId))
                        deptName = deptNameByStudentId[studentId] ?? "";

                    StudentMotherGroup group = new StudentMotherGroup();
                    group.StudentId = studentId;
                    group.StudentNumber = student.StudentNumber ?? "";
                    group.StudentName = student.StudentName ?? "";
                    group.Status = student.StatusValue;
                    group.MatchesExcludedCategory = HasTag(studentTags, studentId, excludedTagIds);
                    group.SetSourceSlots(new RankSlot[]
                    {
                        new RankSlot { RankType = RankKinds.Year, GroupName = student.GradeYear.Value + "年級", Participate = true },
                        new RankSlot { RankType = RankKinds.Department, GroupName = deptName, Participate = true },
                        new RankSlot { RankType = RankKinds.Class, GroupName = student.ClassName ?? "", Participate = true },
                        new RankSlot { RankType = RankKinds.Tag1, GroupName = tag1Name, Participate = tag1Name != "" },
                        new RankSlot { RankType = RankKinds.Tag2, GroupName = tag2Name, Participate = tag2Name != "" }
                    });
                    MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, group, group.MatchesExcludedCategory);

                    string seatNo = "";
                    if (seatByStudentId != null && seatByStudentId.ContainsKey(studentId))
                        seatNo = seatByStudentId[studentId] ?? "";
                    rows.Add(new ConfirmStudentRow
                    {
                        Group = group,
                        ClassName = student.ClassName ?? "",
                        SeatNo = seatNo
                    });
                }
            }

            if (rows.Count == 0)
                return EmptyPopulationMessage;
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
