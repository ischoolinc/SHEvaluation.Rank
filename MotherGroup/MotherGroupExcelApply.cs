using System;
using System.Collections.Generic;
using System.Text;

namespace SHEvaluation.Rank.MotherGroup
{
    public static class MotherGroupExcelApply
    {
        public static string IssueText(MotherGroupExcelValidation validation)
        {
            if (validation == null || validation.Issues == null || validation.Issues.Count == 0)
                return "Excel 母群無法匯入。";
            StringBuilder text = new StringBuilder();
            foreach (MotherGroupExcelIssue issue in validation.Issues)
            {
                if (text.Length > 0)
                    text.Append("\n");
                text.Append(FormatIssue(issue));
            }
            return text.ToString();
        }

        public static string FormatIssue(MotherGroupExcelIssue issue)
        {
            if (issue == null)
                return "";
            string message = issue.Message ?? "";
            if (issue.RowNumber <= 0)
                return message;
            string label = "第 " + issue.RowNumber + " 列";
            if (message.IndexOf(label) >= 0)
                return message;
            return label + "：" + message;
        }

        public static string WarningText(MotherGroupExcelValidation validation)
        {
            if (validation == null || validation.Students == null)
                return "";
            StringBuilder text = new StringBuilder();
            foreach (MotherGroupExcelStudent student in validation.Students)
            {
                if (student == null || string.IsNullOrEmpty(student.NameWarning))
                    continue;
                if (text.Length > 0)
                    text.Append("\n");
                text.Append(student.NameWarning);
            }
            return text.ToString();
        }

        public static List<StudentMotherGroup> CreateGroups(MotherGroupExcelValidation validation, string excludedCategory, IList<TagDefinition> tagDefinitions, IList<StudentTagLink> studentTags)
        {
            if (validation == null || !validation.IsValid)
                throw new InvalidOperationException("Excel 尚未通過驗證，不能建立確認清單。");
            List<string> excludedIds = CurrentStudentMotherGroupBuilder.ResolveTagIds(tagDefinitions, excludedCategory);
            List<StudentMotherGroup> groups = new List<StudentMotherGroup>();
            foreach (MotherGroupExcelStudent student in validation.Students)
            {
                if (student == null)
                    continue;
                StudentMotherGroup group = new StudentMotherGroup();
                group.StudentId = student.StudentId;
                group.StudentNumber = student.StudentNumber;
                group.StudentName = string.IsNullOrEmpty(student.NameWarning) ? student.ExcelName : student.DatabaseName;
                group.Status = student.Status;
                group.SetSourceSlots(student.Slots);
                MotherGroupInitialParticipation.Apply(MotherGroupSourceKind.Excel, group, HasTag(studentTags, student.StudentId, excludedIds));
                groups.Add(group);
            }
            return groups;
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
