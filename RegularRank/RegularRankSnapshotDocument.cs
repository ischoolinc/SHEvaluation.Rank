using SHEvaluation.Rank.MotherGroup;
using System.Globalization;
using System.Text;
using System.Xml;

namespace SHEvaluation.Rank.RegularRank
{
    public static class RegularRankSnapshotDocument
    {
        public static string ComposeExtension(string existingExtensionJson, MotherGroupConfirmList list)
        {
            return MotherGroupSnapshotJson.Compose(existingExtensionJson, list, MotherGroupSourceKind.Current, null);
        }

        public static string ComposeExtension(string existingExtensionJson, MotherGroupConfirmList list, RegularRankConfirmRequest request)
        {
            if (request == null) throw new System.ArgumentNullException("request");
            MotherGroupSourceKind kind = request.SourceKind == RegularRankSourceKind.Excel ? MotherGroupSourceKind.Excel
                : request.SourceKind == RegularRankSourceKind.History ? MotherGroupSourceKind.HistorySnapshot : MotherGroupSourceKind.Current;
            return MotherGroupSnapshotJson.Compose(existingExtensionJson, list, kind,
                request.SourceKind == RegularRankSourceKind.History ? request.SourceBatchId : null);
        }

        public static string BuildSettingXml(RegularRankConfirmRequest request)
        {
            if (request == null)
                return "";
            XmlDocument document = new XmlDocument();
            XmlElement setting = document.CreateElement("Setting");
            setting.SetAttribute("學年度", request.TargetSchoolYear ?? "");
            setting.SetAttribute("學期", request.TargetSemester ?? "");
            setting.SetAttribute("考試名稱", request.TargetExamName ?? "");
            setting.SetAttribute("ref_exam_id", request.TargetExamId ?? "");
            setting.SetAttribute("不排名學生類別", request.ExcludedCategory ?? "");
            setting.SetAttribute("類別一", request.RankTag1 ?? "");
            setting.SetAttribute("類別二", request.RankTag2 ?? "");
            AppendSubjects(document, setting, "採計科目", request.GeneralSubjects);
            AppendSubjects(document, setting, "類別一採計科目", request.Tag1Subjects);
            AppendSubjects(document, setting, "類別二採計科目", request.Tag2Subjects);
            if (request.GradeYears != null)
            {
                foreach (int gradeYear in request.GradeYears)
                {
                    XmlElement grade = document.CreateElement("年級");
                    grade.InnerText = gradeYear.ToString(CultureInfo.InvariantCulture);
                    setting.AppendChild(grade);
                }
            }
            document.AppendChild(setting);
            return setting.OuterXml;
        }

        public static string ConfirmationText(RegularRankConfirmRequest request, int studentCount)
        {
            StringBuilder text = new StringBuilder();
            text.Append("目標 ");
            text.Append(request == null ? "" : request.TargetSchoolYear);
            text.Append("-");
            text.Append(request == null ? "" : request.TargetSemester);
            text.Append(" ");
            text.Append(request == null ? "" : request.TargetExamName);
            text.Append("（exam ID ");
            text.Append(request == null ? "" : request.TargetExamId);
            text.Append("）。來源：目前學生。學生 ");
            text.Append(studentCount.ToString(CultureInfo.InvariantCulture));
            text.Append(" 人。三組科目等學校設定會先保存，排名失敗時不回滾該設定。若發布傳輸檢查未通過，不會保存設定也不會寫入排名。");
            return text.ToString();
        }

        private static void AppendSubjects(XmlDocument document, XmlElement setting, string elementName, System.Collections.Generic.IList<string> subjects)
        {
            if (subjects == null)
                return;
            foreach (string subject in subjects)
            {
                XmlElement element = document.CreateElement(elementName);
                element.InnerText = subject ?? "";
                setting.AppendChild(element);
            }
        }
    }
}
