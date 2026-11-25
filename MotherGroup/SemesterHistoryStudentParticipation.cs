namespace SHEvaluation.Rank.MotherGroup
{
    public static class SemesterHistoryStudentParticipation
    {
        public static void KeepYearDeptClassParticipating(HistoryRankLoad load)
        {
            if (load == null || !load.Restored)
                return;
            ApplyNoJsonYearDeptClass(load.Students);
        }

        public static void ApplyNoJsonYearDeptClass(System.Collections.Generic.IList<StudentMotherGroup> students)
        {
            if (students == null)
                return;
            foreach (StudentMotherGroup student in students)
            {
                if (student == null || student.MatchesExcludedCategory)
                    continue;
                Keep(student, RankKinds.Year);
                Keep(student, RankKinds.Department);
                Keep(student, RankKinds.Class);
            }
        }

        private static void Keep(StudentMotherGroup student, string rankType)
        {
            RankSlot slot = student.GetWorking(rankType);
            slot.Participate = true;
            if (slot.HasConflict)
                slot.AllowParticipationDespiteConflict = true;
        }
    }
}
