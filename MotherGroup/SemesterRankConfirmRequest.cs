using System.Collections.Generic;
using K12.Data;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class SemesterRankConfirmRequest
    {
        public string SchoolYear { get; set; }
        public string Semester { get; set; }
        public string SourceName { get; set; }
        public string HistoryBatchId { get; set; }
        public string HistorySchoolYear { get; set; }
        public string HistorySemester { get; set; }
        public List<StudentRecord> Students { get; set; }
        public List<TagConfigRecord> Tags { get; set; }
        public List<int> GradeYears { get; set; }
        public List<string> GradeSelections { get; set; }
        public string ExcludedCategory { get; set; }
        public string RankTag1 { get; set; }
        public string RankTag2 { get; set; }
        public IList<string> RankTypes { get; set; }
    }
}
