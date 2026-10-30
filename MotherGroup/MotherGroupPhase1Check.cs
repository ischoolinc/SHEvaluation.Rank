using System;
using System.Collections.Generic;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupPhase1Check
    {
        public static int Main(string[] args)
        {
            List<string> errors = new List<string>();
            errors.AddRange(CheckCurrentNames());
            errors.AddRange(CheckInitialParticipation());
            if (errors.Count == 0)
            {
                Console.WriteLine("phase1-check: ok");
                return 0;
            }
            foreach (string error in errors)
                Console.WriteLine(error);
            return 1;
        }

        public static List<string> CheckCurrentNames()
        {
            List<string> errors = new List<string>();
            string deptSql = CurrentStudentDeptQuery.Create(new string[] { "10", "11" });
            if (deptSql.IndexOf("COALESCE(student.ref_dept_id, class.ref_dept_id)", StringComparison.Ordinal) < 0)
                errors.Add("科別 SQL 未保留 COALESCE(student.ref_dept_id, class.ref_dept_id)");

            List<TagDefinition> tags = new List<TagDefinition>
            {
                new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" },
                new TagDefinition { Id = "a", Prefix = "學程", Name = "第一" },
                new TagDefinition { Id = "b", Prefix = "學程", Name = "第二" },
                new TagDefinition { Id = "c", Prefix = "", Name = "社團" }
            };
            List<StudentTagLink> links = new List<StudentTagLink>
            {
                new StudentTagLink { StudentId = "10", TagId = "b", Name = "第二" },
                new StudentTagLink { StudentId = "10", TagId = "a", Name = "第一" },
                new StudentTagLink { StudentId = "10", TagId = "c", Name = "社團" },
                new StudentTagLink { StudentId = "11", TagId = "ex", Name = "休學" },
                new StudentTagLink { StudentId = "11", TagId = "a", Name = "第一" },
                new StudentTagLink { StudentId = "14", TagId = "a", Name = "第一" }
            };
            List<CurrentStudentRow> students = new List<CurrentStudentRow>
            {
                new CurrentStudentRow { StudentId = "10", StudentNumber = "S10", StudentName = "王", IsRegular = true, RefClassId = "c1", GradeYear = 1, ClassName = "忠" },
                new CurrentStudentRow { StudentId = "11", StudentNumber = "S11", StudentName = "李", IsRegular = true, RefClassId = "c1", GradeYear = 1, ClassName = "忠" },
                new CurrentStudentRow { StudentId = "12", StudentNumber = "S12", StudentName = "張", IsRegular = true, RefClassId = "c2", GradeYear = 2, ClassName = "孝" },
                new CurrentStudentRow { StudentId = "13", StudentNumber = "S13", StudentName = "陳", IsRegular = false, RefClassId = "c1", GradeYear = 1, ClassName = "忠" },
                new CurrentStudentRow { StudentId = "14", StudentNumber = "S14", StudentName = "林", IsRegular = true, RefClassId = "c1", GradeYear = 1, ClassName = "仁" }
            };
            Dictionary<string, string> deptNames = new Dictionary<string, string>
            {
                { "10", "普通科" },
                { "11", "商業科" },
                { "14", "" }
            };

            List<StudentMotherGroup> groups = CurrentStudentMotherGroupBuilder.Build(
                students, tags, links, new int[] { 1 }, "不排名", "學程", "社團", deptNames);

            Expect(errors, groups.Count == 3, "目前學生來源應保留不排名學生，且只含勾選年級的一般在班學生");
            StudentMotherGroup kept = Find(groups, "11");
            Expect(errors, kept != null && kept.MatchesExcludedCategory, "符合不排名類別的學生仍在名單");
            ExpectNames(errors, Find(groups, "10"), "1年級", "普通科", "忠", "第二", "社團");
            ExpectNames(errors, kept, "1年級", "商業科", "忠", "第一", "");
            ExpectNames(errors, Find(groups, "14"), "1年級", "", "仁", "第一", "");
            Expect(errors, Find(groups, "12") == null && Find(groups, "13") == null, "未勾選年級與非一般生不進入目前學生來源");
            return errors;
        }

        public static List<string> CheckInitialParticipation()
        {
            List<string> errors = new List<string>();
            string historyBatchSetting = "<Setting 學年度=\"113\"><MotherGroupSnapshot version=\"1\" /></Setting>";
            string unchangedHistory = historyBatchSetting;

            StudentMotherGroup current = CreateGroup("21", "S21", "現生", true,
                Slot(RankKinds.Year, "1年級", true),
                Slot(RankKinds.Department, "", true),
                Slot(RankKinds.Class, "忠", true),
                Slot(RankKinds.Tag1, "", true),
                Slot(RankKinds.Tag2, "社團", true));
            StudentMotherGroup snapshot = CreateGroup("22", "S22", "快照生", true,
                Slot(RankKinds.Year, "2年級", true),
                Slot(RankKinds.Department, "普通科", true),
                Slot(RankKinds.Class, "孝", true),
                Slot(RankKinds.Tag1, "第一", true),
                Slot(RankKinds.Tag2, "社團", true));
            StudentMotherGroup excel = CreateGroup("23", "S23", "匯入生", true,
                Slot(RankKinds.Year, "3年級", true),
                Slot(RankKinds.Department, "商業科", true),
                Slot(RankKinds.Class, "仁", true),
                Slot(RankKinds.Tag1, "升學", true),
                Slot(RankKinds.Tag2, "競賽", true));

            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, current, true);
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.HistorySnapshot, snapshot, true);
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Excel, excel, true);

            ExpectExcluded(errors, current, "1年級", "", "忠", "", "社團");
            ExpectExcluded(errors, snapshot, "2年級", "普通科", "孝", "第一", "社團");
            ExpectExcluded(errors, excel, "3年級", "商業科", "仁", "升學", "競賽");
            Expect(errors, historyBatchSetting == unchangedHistory, "套用初始參與時舊歷史批次內容被改寫");

            current.GetWorking(RankKinds.Year).Participate = true;
            Expect(errors, current.GetWorking(RankKinds.Year).Participate && current.GetWorking(RankKinds.Year).GroupName == "1年級", "不排名學生改回參與後應保留原母群名稱");

            StudentMotherGroup blankDept = CreateGroup("24", "S24", "空白科", false,
                Slot(RankKinds.Year, "1年級", true),
                Slot(RankKinds.Department, "", false),
                Slot(RankKinds.Class, "忠", true),
                Slot(RankKinds.Tag1, "", true),
                Slot(RankKinds.Tag2, "", true));
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, blankDept, false);
            Expect(errors, blankDept.GetWorking(RankKinds.Department).Participate, "目前學生來源的空白科別仍應參與");
            Expect(errors, blankDept.GetWorking(RankKinds.Department).GroupName == "", "空白科別不應被改成其他名稱");
            Expect(errors, !blankDept.GetWorking(RankKinds.Tag1).Participate && blankDept.GetWorking(RankKinds.Tag1).GroupName == "", "空白類別一應不參與且不建立名稱");
            Expect(errors, !blankDept.GetWorking(RankKinds.Tag2).Participate, "空白類別二應不參與");
            Expect(errors, blankDept.GetOriginal(RankKinds.Tag1).Participate, "來源原值的類別參與不應被初始規則改寫");

            List<StudentMotherGroup> built = CurrentStudentMotherGroupBuilder.Build(
                new List<CurrentStudentRow>
                {
                    new CurrentStudentRow { StudentId = "31", StudentNumber = "S31", StudentName = "現生", IsRegular = true, RefClassId = "c1", GradeYear = 1, ClassName = "忠" }
                },
                new List<TagDefinition> { new TagDefinition { Id = "ex", Prefix = "不排名", Name = "休學" } },
                new List<StudentTagLink> { new StudentTagLink { StudentId = "31", TagId = "ex", Name = "休學" } },
                new int[] { 1 },
                "不排名",
                "",
                "",
                new Dictionary<string, string> { { "31", "普通科" } });
            Expect(errors, built.Count == 1 && built[0].MatchesExcludedCategory, "目前學生來源應保留符合不排名類別的學生");
            MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Current, built[0], true);
            ExpectExcluded(errors, built[0], "1年級", "普通科", "忠", "", "");
            return errors;
        }

        private static void ExpectExcluded(List<string> errors, StudentMotherGroup group, string year, string dept, string className, string tag1, string tag2)
        {
            ExpectNames(errors, group, year, dept, className, tag1, tag2);
            foreach (string rankType in RankKinds.All)
            {
                RankSlot working = group.GetWorking(rankType);
                RankSlot original = group.GetOriginal(rankType);
                Expect(errors, working.GroupName == original.GroupName, group.StudentId + " " + rankType + " 工作名稱應與來源名稱相同");
                Expect(errors, !working.Participate, group.StudentId + " " + rankType + " 初始應不參與");
            }
            Expect(errors, group.GetOriginal(RankKinds.Year).Participate, group.StudentId + " 來源原值的年排名參與應保留");
        }

        private static StudentMotherGroup CreateGroup(string id, string number, string name, bool excluded, params RankSlot[] slots)
        {
            StudentMotherGroup group = new StudentMotherGroup();
            group.StudentId = id;
            group.StudentNumber = number;
            group.StudentName = name;
            group.MatchesExcludedCategory = excluded;
            group.SetSourceSlots(slots);
            return group;
        }

        private static RankSlot Slot(string rankType, string groupName, bool participate)
        {
            return new RankSlot { RankType = rankType, GroupName = groupName, Participate = participate };
        }

        private static void ExpectNames(List<string> errors, StudentMotherGroup group, string year, string dept, string className, string tag1, string tag2)
        {
            if (group == null)
            {
                errors.Add("找不到要比對名稱的學生");
                return;
            }
            Expect(errors, group.GetOriginal(RankKinds.Year).GroupName == year, group.StudentId + " 年排名名稱應為 " + year);
            Expect(errors, group.GetOriginal(RankKinds.Department).GroupName == dept, group.StudentId + " 科排名名稱應為 " + dept);
            Expect(errors, group.GetOriginal(RankKinds.Class).GroupName == className, group.StudentId + " 班排名名稱應為 " + className);
            Expect(errors, group.GetOriginal(RankKinds.Tag1).GroupName == tag1, group.StudentId + " 類別1名稱應為 " + tag1);
            Expect(errors, group.GetOriginal(RankKinds.Tag2).GroupName == tag2, group.StudentId + " 類別2名稱應為 " + tag2);
        }

        private static StudentMotherGroup Find(List<StudentMotherGroup> groups, string studentId)
        {
            foreach (StudentMotherGroup group in groups)
            {
                if (group.StudentId == studentId)
                    return group;
            }
            return null;
        }

        private static void Expect(List<string> errors, bool condition, string message)
        {
            if (!condition)
                errors.Add(message);
        }
    }
}
