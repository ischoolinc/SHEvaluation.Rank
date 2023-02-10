using System;
using System.Collections.Generic;
using System.Text;
using SmartSchool.Evaluation.GraduationPlan;
using System.Xml;

namespace SHEvaluation.Rank
{
    // 取得班級課程規劃
    class ClassCoursePlan
    {
        private string _ClassID;
        private List<subject> _SubjecList;
        private List<subject> _tmpSubjList;


        // getCoursePlan
        public ClassCoursePlan(string ClassID, bool RequiredTrue)
        {
            this._SubjecList=new List<subject> ();
            this._tmpSubjList = new List<subject>();
            this._ClassID = ClassID;
            GraduationPlanInfo info = GraduationPlan.Instance.GetClassGraduationPlan(ClassID);
            if (info != null)
            {
                foreach (GraduationPlanSubject Subj in info.Subjects)
                {
                    subject subj1 = new subject();
                    subj1.Gategory = Subj.Category;
                    subj1.Domain = Subj.Domain;
                    subj1.SubjectCode = Subj.SubjectCode;
                    if (!Subj.NotIncludedInCalc && !Subj.NotIncludedInCredit)
                        {
                            subj1.SubjectName = Subj.SubjectName;
                            subj1.Required = Subj.Required;
                            subj1.SubjectLevel = Subj.Level;
                            subj1.Credit = int.Parse(Subj.Credit);
                            this._tmpSubjList.Add(subj1);
                            subj1 = null;
                        }
                    } 
                }

                // 解析年級與學期                                   
                foreach (XmlElement xe in info.GraduationPlanElement.SelectNodes("Subject"))
                {
                    foreach (subject s1 in this._tmpSubjList)
                    {
                        if (s1.SubjectName == xe.GetAttribute("SubjectName") && s1.SubjectLevel == xe.GetAttribute("Level") && s1.Required == xe.GetAttribute("Required"))
                        {
                            if (xe.GetAttribute("GradeYear") != "")
                                s1.GradeYear = int.Parse(xe.GetAttribute("GradeYear"));
                            if (xe.GetAttribute("Semester") != "")
                                s1.Semester = int.Parse(xe.GetAttribute("Semester"));
                        }
                    }
                }

                // 依年級學期放入
                for (int gr = 1; gr <= 3; gr++)
                    for (int sem = 1; sem <= 2; sem++)
                        foreach (subject subj in this._tmpSubjList)
                            if (subj.GradeYear == gr && subj.Semester == sem)
                                this._SubjecList.Add(subj);

                // 將有疑問放在最後
                if (this._SubjecList.Count != this._tmpSubjList.Count)
                    foreach (subject subj in this._tmpSubjList)
                        if (subj.Semester == 0 || subj.GradeYear == 0)
                            this._SubjecList.Add(subj);
            }

        public List<subject> SubjectList
        { get { return _SubjecList; } }

        public string CPClassID
        { get { return this._ClassID; } }
    }
}
