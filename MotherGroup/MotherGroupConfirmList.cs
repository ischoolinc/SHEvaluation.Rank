using System;
using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class ConfirmStudentRow
    {
        public StudentMotherGroup Group { get; set; }
        public string ClassName { get; set; }
        public string SeatNo { get; set; }
    }

    public sealed class NamedGroupCount
    {
        public string GroupName { get; set; }
        public int TotalCount { get; set; }
        public int ParticipateCount { get; set; }
        public int ExcludeCount { get; set; }
    }

    public sealed class RankPopulationCount
    {
        public string RankType { get; set; }
        public int ParticipateCount { get; set; }
        public int ExcludeCount { get; set; }
        public List<NamedGroupCount> NamedGroups { get; set; }
    }

    public sealed class MotherGroupCountSummary
    {
        public int StudentCount { get; set; }
        public List<RankPopulationCount> Ranks { get; set; }
    }

    public sealed class MotherGroupConfirmList
    {
        private readonly List<ConfirmStudentRow> _rows = new List<ConfirmStudentRow>();
        private readonly Dictionary<string, Dictionary<string, bool>> _initialParticipate = new Dictionary<string, Dictionary<string, bool>>();

        public IList<ConfirmStudentRow> Rows
        {
            get { return _rows; }
        }

        public void Load(IList<ConfirmStudentRow> rows)
        {
            _rows.Clear();
            _initialParticipate.Clear();
            if (rows == null)
                return;
            foreach (ConfirmStudentRow row in rows)
            {
                if (row == null || row.Group == null || string.IsNullOrEmpty(row.Group.StudentId))
                    continue;
                _rows.Add(row);
                Dictionary<string, bool> flags = new Dictionary<string, bool>();
                foreach (string rankType in RankKinds.All)
                    flags[rankType] = row.Group.GetWorking(rankType).Participate;
                _initialParticipate[row.Group.StudentId] = flags;
            }
        }

        public MotherGroupConfirmList CopyWorking()
        {
            List<ConfirmStudentRow> rows = new List<ConfirmStudentRow>();
            foreach (ConfirmStudentRow row in _rows)
            {
                if (row == null || row.Group == null)
                    continue;
                StudentMotherGroup group = new StudentMotherGroup();
                group.StudentId = row.Group.StudentId;
                group.StudentNumber = row.Group.StudentNumber;
                group.StudentName = row.Group.StudentName;
                group.Status = row.Group.Status;
                group.MatchesExcludedCategory = row.Group.MatchesExcludedCategory;
                group.SetSourceSlots(row.Group.Working);
                ConfirmStudentRow copy = new ConfirmStudentRow();
                copy.Group = group;
                copy.ClassName = row.ClassName;
                copy.SeatNo = row.SeatNo;
                rows.Add(copy);
            }
            MotherGroupConfirmList list = new MotherGroupConfirmList();
            list.Load(rows);
            return list;
        }

        public bool ContainsIdentity(string studentId, string studentNumber)
        {
            string number = studentNumber == null ? "" : studentNumber.Trim();
            foreach (ConfirmStudentRow row in _rows)
            {
                if (row == null || row.Group == null)
                    continue;
                if (!string.IsNullOrEmpty(studentId) && row.Group.StudentId == studentId)
                    return true;
                string existing = row.Group.StudentNumber == null ? "" : row.Group.StudentNumber.Trim();
                if (number.Length > 0 && existing == number)
                    return true;
            }
            return false;
        }

        public void Add(ConfirmStudentRow row)
        {
            if (row == null || row.Group == null || string.IsNullOrEmpty(row.Group.StudentId))
                throw new ArgumentException("新增學生缺少學生識別。");
            if (ContainsIdentity(row.Group.StudentId, row.Group.StudentNumber))
                throw new InvalidOperationException("這位學生已在確認清單。");
            _rows.Add(row);
            Dictionary<string, bool> flags = new Dictionary<string, bool>();
            foreach (string rankType in RankKinds.All)
                flags[rankType] = row.Group.GetWorking(rankType).Participate;
            _initialParticipate[row.Group.StudentId] = flags;
        }

        public void SetParticipate(IEnumerable<string> studentIds, string rankType, bool participate)
        {
            if (studentIds == null)
                return;
            Dictionary<string, bool> selected = new Dictionary<string, bool>();
            foreach (string studentId in studentIds)
            {
                if (!string.IsNullOrEmpty(studentId))
                    selected[studentId] = true;
            }
            foreach (ConfirmStudentRow row in _rows)
            {
                if (!selected.ContainsKey(row.Group.StudentId))
                    continue;
                RankSlot slot = row.Group.GetWorking(rankType);
                if (slot.HasConflict && !slot.AllowParticipationDespiteConflict)
                    slot.Participate = false;
                else
                    slot.Participate = participate;
            }
        }

        public void Restore()
        {
            foreach (ConfirmStudentRow row in _rows)
            {
                Dictionary<string, bool> flags;
                if (!_initialParticipate.TryGetValue(row.Group.StudentId, out flags))
                    continue;
                foreach (string rankType in RankKinds.All)
                {
                    RankSlot slot = row.Group.GetWorking(rankType);
                    if (slot.HasConflict && !slot.AllowParticipationDespiteConflict)
                    {
                        slot.Participate = false;
                        continue;
                    }
                    bool participate;
                    if (flags.TryGetValue(rankType, out participate))
                        slot.Participate = participate;
                }
            }
        }

        public List<string> VisibleStudentIds(string keyword)
        {
            List<string> ids = new List<string>();
            string text = keyword == null ? "" : keyword.Trim();
            foreach (ConfirmStudentRow row in _rows)
            {
                if (text.Length == 0 || Matches(row, text))
                    ids.Add(row.Group.StudentId);
            }
            return ids;
        }

        public MotherGroupCountSummary Summarize()
        {
            MotherGroupCountSummary summary = new MotherGroupCountSummary();
            summary.StudentCount = _rows.Count;
            summary.Ranks = new List<RankPopulationCount>();
            foreach (string rankType in RankKinds.All)
            {
                RankPopulationCount rank = new RankPopulationCount();
                rank.RankType = rankType;
                rank.NamedGroups = new List<NamedGroupCount>();
                Dictionary<string, NamedGroupCount> named = new Dictionary<string, NamedGroupCount>();
                foreach (ConfirmStudentRow row in _rows)
                {
                    RankSlot slot = row.Group.GetWorking(rankType);
                    if (slot.Participate)
                        rank.ParticipateCount++;
                    else
                        rank.ExcludeCount++;
                    if (string.IsNullOrEmpty(slot.GroupName))
                        continue;
                    NamedGroupCount namedCount;
                    if (!named.TryGetValue(slot.GroupName, out namedCount))
                    {
                        namedCount = new NamedGroupCount();
                        namedCount.GroupName = slot.GroupName;
                        named.Add(slot.GroupName, namedCount);
                        rank.NamedGroups.Add(namedCount);
                    }
                    namedCount.TotalCount++;
                    if (slot.Participate)
                        namedCount.ParticipateCount++;
                    else
                        namedCount.ExcludeCount++;
                }
                summary.Ranks.Add(rank);
            }
            return summary;
        }

        public static string FormatFixedSummary(MotherGroupCountSummary summary)
        {
            StringBuilder text = new StringBuilder();
            text.Append("學生總人數 ").Append(summary.StudentCount);
            if (summary.Ranks != null)
            {
                foreach (RankPopulationCount rank in summary.Ranks)
                {
                    text.AppendLine();
                    text.Append(RankCaption(rank.RankType));
                    text.Append(" 參與 ").Append(rank.ParticipateCount);
                    text.Append("／排除 ").Append(rank.ExcludeCount);
                }
            }
            return text.ToString();
        }

        public static string RankCaption(string rankType)
        {
            if (rankType == RankKinds.Tag1)
                return "類別一";
            if (rankType == RankKinds.Tag2)
                return "類別二";
            return rankType;
        }

        private static bool Matches(ConfirmStudentRow row, string keyword)
        {
            if (Contains(row.ClassName, keyword) || Contains(row.SeatNo, keyword))
                return true;
            if (Contains(row.Group.StudentNumber, keyword) || Contains(row.Group.StudentName, keyword))
                return true;
            foreach (string rankType in RankKinds.All)
            {
                RankSlot slot = row.Group.GetWorking(rankType);
                if (Contains(slot.GroupName, keyword))
                    return true;
                if (slot.ConflictNames == null)
                    continue;
                foreach (string name in slot.ConflictNames)
                {
                    if (Contains(name, keyword))
                        return true;
                }
            }
            return false;
        }

        private static bool Contains(string value, string keyword)
        {
            return value != null && value.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
