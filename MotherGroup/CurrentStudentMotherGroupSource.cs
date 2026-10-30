using FISCA.Data;
using K12.Data;
using System.Collections.Generic;
using System.Data;

namespace SHEvaluation.Rank.MotherGroup
{
    public sealed class CurrentStudentMotherGroupSource
    {
        public List<StudentMotherGroup> Load(
            IList<StudentRecord> students,
            IList<TagConfigRecord> tagConfigs,
            IList<int> selectedGradeYears,
            string excludedCategory,
            string tag1Category,
            string tag2Category)
        {
            List<CurrentStudentRow> rows = new List<CurrentStudentRow>();
            if (students != null)
            {
                foreach (StudentRecord student in students)
                {
                    if (student == null)
                        continue;
                    int? gradeYear = null;
                    string className = "";
                    if (student.Class != null)
                    {
                        gradeYear = student.Class.GradeYear;
                        className = student.Class.Name;
                    }
                    rows.Add(new CurrentStudentRow
                    {
                        StudentId = student.ID,
                        StudentNumber = student.StudentNumber,
                        StudentName = student.Name,
                        IsRegular = student.Status == StudentRecord.StudentStatus.一般,
                        StatusValue = DatabaseStatus(student.Status),
                        RefClassId = student.RefClassID,
                        GradeYear = gradeYear,
                        ClassName = className
                    });
                }
            }

            List<TagDefinition> definitions = new List<TagDefinition>();
            if (tagConfigs != null)
            {
                foreach (TagConfigRecord tag in tagConfigs)
                {
                    if (tag == null)
                        continue;
                    definitions.Add(new TagDefinition
                    {
                        Id = tag.ID,
                        Prefix = tag.Prefix,
                        Name = tag.Name
                    });
                }
            }

            List<StudentTagLink> links = new List<StudentTagLink>();
            foreach (StudentTagRecord tag in StudentTag.SelectAll())
            {
                if (tag == null)
                    continue;
                links.Add(new StudentTagLink
                {
                    StudentId = tag.RefStudentID,
                    TagId = tag.RefTagID,
                    Name = tag.Name
                });
            }

            List<string> studentIds = new List<string>();
            foreach (CurrentStudentRow row in rows)
            {
                if (!CurrentStudentMotherGroupBuilder.IsCurrentPopulation(row))
                    continue;
                if (selectedGradeYears == null || !selectedGradeYears.Contains(row.GradeYear.Value))
                    continue;
                studentIds.Add(row.StudentId);
            }

            Dictionary<string, string> deptNames = new Dictionary<string, string>();
            if (studentIds.Count > 0)
            {
                QueryHelper queryHelper = new QueryHelper();
                DataTable table = queryHelper.Select(CurrentStudentDeptQuery.Create(studentIds));
                foreach (DataRow dataRow in table.Rows)
                {
                    string studentId = "" + dataRow["student_id"];
                    deptNames[studentId] = "" + dataRow["deptname"];
                }
            }

            return CurrentStudentMotherGroupBuilder.Build(
                rows,
                definitions,
                links,
                selectedGradeYears,
                excludedCategory,
                tag1Category,
                tag2Category,
                deptNames);
        }

        private static int? DatabaseStatus(StudentRecord.StudentStatus status)
        {
            if (status == StudentRecord.StudentStatus.一般)
                return 1;
            if (status == StudentRecord.StudentStatus.延修)
                return 2;
            if (status == StudentRecord.StudentStatus.休學)
                return 4;
            if (status == StudentRecord.StudentStatus.輟學)
                return 8;
            if (status == StudentRecord.StudentStatus.畢業或離校)
                return 16;
            if (status == StudentRecord.StudentStatus.刪除)
                return 256;
            return null;
        }
    }
}
