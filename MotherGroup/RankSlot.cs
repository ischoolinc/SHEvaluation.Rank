using System.Collections.Generic;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class RankKinds
    {
        public const string Year = "年排名";
        public const string Department = "科排名";
        public const string Class = "班排名";
        public const string Tag1 = "類別1排名";
        public const string Tag2 = "類別2排名";

        public static readonly string[] All = new string[]
        {
            Year,
            Department,
            Class,
            Tag1,
            Tag2
        };
    }

    public sealed class RankSlot
    {
        public string RankType { get; set; }
        public string GroupName { get; set; }
        public bool Participate { get; set; }
        public bool AllowParticipationDespiteConflict { get; set; }
        public List<string> ConflictNames { get; set; }

        public bool HasConflict
        {
            get { return ConflictNames != null && ConflictNames.Count > 1; }
        }

        public RankSlot Clone()
        {
            RankSlot clone = new RankSlot();
            clone.RankType = RankType;
            clone.GroupName = GroupName;
            clone.Participate = Participate;
            clone.AllowParticipationDespiteConflict = AllowParticipationDespiteConflict;
            if (ConflictNames != null)
            {
                clone.ConflictNames = new List<string>();
                foreach (string name in ConflictNames)
                    clone.ConflictNames.Add(name);
            }
            return clone;
        }
    }
}
