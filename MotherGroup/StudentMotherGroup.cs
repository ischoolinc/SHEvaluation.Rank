using System;
using System.Collections.Generic;
using System.Linq;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class StudentMotherGroup
    {
        public string StudentId { get; set; }
        public string StudentNumber { get; set; }
        public string StudentName { get; set; }
        public int? Status { get; set; }
        public bool MatchesExcludedCategory { get; set; }
        public List<RankSlot> Original { get; private set; }
        public List<RankSlot> Working { get; private set; }

        public StudentMotherGroup()
        {
            Original = CreateEmptySlots();
            Working = CreateEmptySlots();
        }

        public void SetSourceSlots(IEnumerable<RankSlot> sourceSlots)
        {
            if (sourceSlots == null)
                throw new ArgumentNullException("sourceSlots");

            Original = CloneSlots(sourceSlots);
            Working = CloneSlots(sourceSlots);
        }

        public RankSlot GetOriginal(string rankType)
        {
            return FindSlot(Original, rankType);
        }

        public RankSlot GetWorking(string rankType)
        {
            return FindSlot(Working, rankType);
        }

        private static List<RankSlot> CreateEmptySlots()
        {
            List<RankSlot> slots = new List<RankSlot>();
            foreach (string rankType in RankKinds.All)
            {
                slots.Add(new RankSlot
                {
                    RankType = rankType,
                    GroupName = "",
                    Participate = false
                });
            }
            return slots;
        }

        private static List<RankSlot> CloneSlots(IEnumerable<RankSlot> sourceSlots)
        {
            Dictionary<string, RankSlot> byType = new Dictionary<string, RankSlot>();
            foreach (RankSlot slot in sourceSlots)
            {
                if (slot == null || string.IsNullOrEmpty(slot.RankType))
                    continue;
                if (!RankKinds.All.Contains(slot.RankType))
                    throw new ArgumentException("未知的排名類型：" + slot.RankType);
                if (byType.ContainsKey(slot.RankType))
                    throw new ArgumentException("同一排名類型只能有一個母群：" + slot.RankType);
                byType.Add(slot.RankType, slot.Clone());
            }

            List<RankSlot> cloned = new List<RankSlot>();
            foreach (string rankType in RankKinds.All)
            {
                RankSlot slot;
                if (byType.TryGetValue(rankType, out slot))
                {
                    if (slot.GroupName == null)
                        slot.GroupName = "";
                    cloned.Add(slot);
                }
                else
                {
                    cloned.Add(new RankSlot
                    {
                        RankType = rankType,
                        GroupName = "",
                        Participate = false
                    });
                }
            }
            return cloned;
        }

        private static RankSlot FindSlot(List<RankSlot> slots, string rankType)
        {
            foreach (RankSlot slot in slots)
            {
                if (slot.RankType == rankType)
                    return slot;
            }
            throw new ArgumentException("未知的排名類型：" + rankType);
        }
    }
}
