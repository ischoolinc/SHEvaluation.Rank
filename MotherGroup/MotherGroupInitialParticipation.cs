using System;
using System.Collections.Generic;

namespace SHEvaluation.Rank.MotherGroup
{
    public enum MotherGroupSourceKind
    {
        Current,
        HistorySnapshot,
        Excel
    }

    public static class MotherGroupInitialParticipation
    {
        public static void Apply(MotherGroupSourceKind sourceKind, StudentMotherGroup student, bool matchesExcluded)
        {
            if (student == null)
                throw new ArgumentNullException("student");

            List<RankSlot> sourceSlots = new List<RankSlot>();
            foreach (RankSlot slot in student.Original)
                sourceSlots.Add(slot.Clone());
            student.SetSourceSlots(sourceSlots);
            student.MatchesExcludedCategory = matchesExcluded;

            foreach (RankSlot slot in student.Working)
            {
                if (IsBlankCategory(slot))
                    slot.Participate = false;
                if (sourceKind == MotherGroupSourceKind.Current && IsBlankDepartment(slot))
                    slot.Participate = true;
            }

            if (matchesExcluded)
            {
                foreach (RankSlot slot in student.Working)
                    slot.Participate = false;
            }

            foreach (RankSlot slot in student.Working)
            {
                if (slot.HasConflict)
                    slot.Participate = false;
            }
        }

        private static bool IsBlankCategory(RankSlot slot)
        {
            return (slot.RankType == RankKinds.Tag1 || slot.RankType == RankKinds.Tag2)
                && string.IsNullOrEmpty(slot.GroupName);
        }

        private static bool IsBlankDepartment(RankSlot slot)
        {
            return slot.RankType == RankKinds.Department && string.IsNullOrEmpty(slot.GroupName);
        }
    }
}
