using FISCA.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using FISCA.UDT;
//using FISCA.Data;
//using K12.Data;
using SmartSchool.Customization.Data;
//using SmartSchool.Customization.Data.StudentExtension;
//using SmartSchool.Customization.PlugIn;
using SHEvaluation.Rank.UDT;
using SHCourseGroupCodeAdmin.DAO;
using System.Xml.Linq;
using SHEvaluation.Rank.DAO;
namespace SHEvaluation.Rank
{
    public partial class SubjectSelect : BaseForm
    {
        public Dictionary<string, udtRegistrationSubjectNew> dicSubj = new Dictionary<string, udtRegistrationSubjectNew>();
        public List<string> SubjectList = new List<string>();
        public SubjectSelect(int SubjectKind, string Dept, string GroupName)
        {
            InitializeComponent();
            lblDept.Text = GroupName + "(" + Dept + ")";
            switch (SubjectKind)
            {
                case 1:
                    lblSubjectKind.Text = "學業";
                    lblKind.Text = "學業";
                    break;
                case 2:
                    lblSubjectKind.Text = "專業科目及實習科目";
                    lblKind.Text = "專業";
                    break;
                case 3:
                    lblSubjectKind.Text = "技能領域";
                    lblKind.Text = "技能";
                    break;
                case 4:
                    lblSubjectKind.Text = "英文";
                    lblKind.Text = "英文";
                    break;
                case 5:
                    lblSubjectKind.Text = "國文";
                    lblKind.Text = "國文";
                    break;
                case 6:
                    lblSubjectKind.Text = "數學";
                    lblKind.Text = "數學";
                    break;


            }

            GetData(SubjectKind, Dept, GroupName);
        }

        private void GetData(int SubjectKind, string Dept, string GroupName)
        {
            List<CClassCourseInfo> _CClassCourseInfoList = new List<CClassCourseInfo>();
            Dictionary<string, GPlanInfo> _GPlanInfoDict;
            DataAccess _da = new DataAccess();
            List<string> lstClass = new List<string>();
            List<string> lstCoursePlan = new List<string>();
            _GPlanInfoDict = new Dictionary<string, GPlanInfo>();
            List<string> lstDept = new List<string>();
            string SubjectStr;
            AccessHelper helper = new AccessHelper();

            lstDept = Dept.Split(',').ToList();
            foreach (ClassRecord cr in helper.ClassHelper.GetAllClass())
            {

                if (lstDept.Contains(cr.Department) && cr.GradeYear == "3")
                {
                    lstClass.Add(cr.ClassID);

                }
            }
            _CClassCourseInfoList = _da.GetCClassCourseInfoList(lstClass);
            Dictionary<string, List<XElement>> dataDictE = new Dictionary<string, List<XElement>>();
            foreach (CClassCourseInfo CoursePlan in _CClassCourseInfoList)
            // 資料整理
            {
                foreach (XElement elm in CoursePlan.RefGPlanXML.Elements("Subject"))
                {
                    string idx = elm.Element("Grouping").Attribute("RowIndex").Value;

                    if (!dataDictE.ContainsKey(idx))
                        dataDictE.Add(idx, new List<XElement>());

                    dataDictE[idx].Add(elm);
                }

                // 填入資料
                udtRegistrationSubjectNew SubjectRec = new udtRegistrationSubjectNew();
                List<string> lstSubject = new List<string>();
                foreach (string idx in dataDictE.Keys)
                {
                    XElement firstElm = null;
                    if (dataDictE[idx].Count > 0)
                    {
                        firstElm = dataDictE[idx][0];
                    }
                    foreach (XElement elmD in dataDictE[idx])
                    {
                        SubjectStr = "";
                        try
                        {
                            if ((firstElm.Attribute("NotIncludedInCalc").Value == "False") || (firstElm.Attribute("NotIncludedInCredit").Value == "False"))
                            {
                                if (!(elmD.Attribute("GradeYear").Value == "3" && elmD.Attribute("Semester").Value == "2"))
                                {
                                    switch (SubjectKind)
                                    {
                                        case 1:
                                            SubjectStr = SubjFullName(firstElm.Attribute("SubjectName").Value, elmD.Attribute("Level").Value) + "(" + elmD.Attribute("GradeYear").Value + (elmD.Attribute("Semester").Value == "1" ? "上" : "下") + ")";
                                            if (!lstSubject.Contains(SubjectStr))
                                            {
                                                lstSubject.Add(SubjectStr);
                                                SubjectRec = new udtRegistrationSubjectNew();
                                                SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                SubjectRec.CalcName = "學業";
                                                SubjectRec.RegGroupName = GroupName;
                                                if (!dicSubj.ContainsKey(SubjectStr))
                                                    dicSubj.Add(SubjectStr, SubjectRec);
                                            }
                                            break;
                                        case 2:
                                            if ((firstElm.Attribute("Entry").Value == "實習科目" || firstElm.Attribute("Entry").Value == "專業科目"))
                                            {
                                                SubjectStr = SubjFullName(firstElm.Attribute("SubjectName").Value, elmD.Attribute("Level").Value) + "(" + elmD.Attribute("GradeYear").Value + (elmD.Attribute("Semester").Value == "1" ? "上" : "下") + ")";
                                                if (!lstSubject.Contains(SubjectStr))
                                                {
                                                    lstSubject.Add(SubjectStr);
                                                    SubjectRec = new udtRegistrationSubjectNew();
                                                    SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                    SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                    SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                    SubjectRec.CalcName = "專業";
                                                    SubjectRec.RegGroupName = GroupName;
                                                    if (!dicSubj.ContainsKey(SubjectStr))
                                                        dicSubj.Add(SubjectStr, SubjectRec);
                                                }
                                            }
                                            break;
                                        case 3:
                                            if ((firstElm.Attribute("Entry").Value == "實習科目" || firstElm.Attribute("Entry").Value == "專業科目"))
                                            {
                                                SubjectStr = SubjFullName(firstElm.Attribute("SubjectName").Value, elmD.Attribute("Level").Value) + "(" + elmD.Attribute("GradeYear").Value + (elmD.Attribute("Semester").Value == "1" ? "上" : "下") + ")";
                                                if (!lstSubject.Contains(SubjectStr))
                                                {
                                                    lstSubject.Add(SubjectStr);
                                                    SubjectRec = new udtRegistrationSubjectNew();
                                                    SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                    SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                    SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                    SubjectRec.CalcName = "技能";
                                                    SubjectRec.RegGroupName = GroupName;
                                                    if (!dicSubj.ContainsKey(SubjectStr))
                                                        dicSubj.Add(SubjectStr, SubjectRec);
                                                }
                                            }
                                            break;
                                        case 4:
                                            if (firstElm.Attribute("課程代碼").Value.Substring(19, 4) == "0102")
                                            {
                                                SubjectStr = SubjFullName(firstElm.Attribute("SubjectName").Value, elmD.Attribute("Level").Value) + "(" + elmD.Attribute("GradeYear").Value + (elmD.Attribute("Semester").Value == "1" ? "上" : "下") + ")";
                                                if (!lstSubject.Contains(SubjectStr))
                                                {
                                                    lstSubject.Add(SubjectStr);
                                                    SubjectRec = new udtRegistrationSubjectNew();
                                                    SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                    SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                    SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                    SubjectRec.CalcName = "英文";
                                                    SubjectRec.RegGroupName = GroupName;
                                                    if (!dicSubj.ContainsKey(SubjectStr))
                                                        dicSubj.Add(SubjectStr, SubjectRec);
                                                }
                                            }
                                            break;
                                        case 5:
                                            if (firstElm.Attribute("課程代碼").Value.Substring(19, 4) == "0101")
                                            {
                                                SubjectStr = SubjFullName(firstElm.Attribute("SubjectName").Value, elmD.Attribute("Level").Value) + "(" + elmD.Attribute("GradeYear").Value + (elmD.Attribute("Semester").Value == "1" ? "上" : "下") + ")";
                                                if (!lstSubject.Contains(SubjectStr))
                                                {
                                                    lstSubject.Add(SubjectStr);
                                                    SubjectRec = new udtRegistrationSubjectNew();
                                                    SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                    SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                    SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                    SubjectRec.CalcName = "國文";
                                                    SubjectRec.RegGroupName = GroupName;
                                                    if (!dicSubj.ContainsKey(SubjectStr))
                                                        dicSubj.Add(SubjectStr, SubjectRec);
                                                }
                                            }
                                            break;
                                        case 6:
                                            if (firstElm.Attribute("Domain").Value == "數學")
                                            {
                                                SubjectStr = SubjFullName(firstElm.Attribute("SubjectName").Value, elmD.Attribute("Level").Value) + "(" + elmD.Attribute("GradeYear").Value + (elmD.Attribute("Semester").Value == "1" ? "上" : "下") + ")";
                                                if (!lstSubject.Contains(SubjectStr))
                                                {
                                                    lstSubject.Add(SubjectStr);
                                                    SubjectRec = new udtRegistrationSubjectNew();
                                                    SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                    SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                    SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                    SubjectRec.CalcName = "數學";
                                                    SubjectRec.RegGroupName = GroupName;
                                                    if (!dicSubj.ContainsKey(SubjectStr))
                                                        dicSubj.Add(SubjectStr, SubjectRec);
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // MessageBox.Show("Error");
                            Console.WriteLine(ex.Message);
                        }
                    }


                }
                lstSubjectView.Clear();
                lstSubject.Sort();
                SubjectList = lstSubject;
                foreach (string Subject in lstSubject)
                {
                    lstSubjectView.Items.Add(Subject);
                }
            }
        }
        private void btnSelAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstSubjectView.Items.Count; i++)
            {
                lstSubjectView.Items[i].Checked = true;
            }
        }

        private void btnNoSelAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstSubjectView.Items.Count; i++)
            {
                lstSubjectView.Items[i].Checked = false;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            FISCA.UDT.AccessHelper accessHelper = new FISCA.UDT.AccessHelper();
            List<udtRegistrationSubjectNew> SubjectList = new List<udtRegistrationSubjectNew>();
            for (int i = 0; i < lstSubjectView.Items.Count; i++)
            {
                if (lstSubjectView.Items[i].Checked)
                {
                    if (GlobalValue.DataDict.ContainsKey(dicSubj[lstSubjectView.Items[i].Text].RegGroupName))
                    {
                        Boolean Find = false;
                        foreach (udtRegistrationSubjectNew Subj in GlobalValue.DataDict[dicSubj[lstSubjectView.Items[i].Text].RegGroupName])
                        {
                            if (Subj.SubjectName == dicSubj[lstSubjectView.Items[i].Text].SubjectName && Subj.GradeYear == dicSubj[lstSubjectView.Items[i].Text].GradeYear && Subj.Semester == dicSubj[lstSubjectView.Items[i].Text].Semester && Subj.CalcName == lblKind.Text)
                            {
                                Find = true;
                            }
                        }
                        if (Find == false)
                            SubjectList.Add(dicSubj[lstSubjectView.Items[i].Text]);
                    }
                    else
                    {
                        SubjectList.Add(dicSubj[lstSubjectView.Items[i].Text]);
                    }
                }
            }
            foreach (udtRegistrationSubjectNew Subj in SubjectList)
            {
                if (GlobalValue.DataDict.ContainsKey(Subj.RegGroupName))
                {
                    GlobalValue.DataDict[Subj.RegGroupName].Add(Subj);
                }
                else
                {
                    GlobalValue.DataDict.Add(Subj.RegGroupName, new List<udtRegistrationSubjectNew>());
                    GlobalValue.DataDict[Subj.RegGroupName].Add(Subj);
                }
                Subj.Save();
            }

            this.Close();

        }

        private string SubjFullName(string SubjectName, string level)
        {
            string lev = "";
            if (level == "1")
                lev = " I";

            if (level == "2")
                lev = " II";

            if (level == "3")
                lev = " III";

            if (level == "4")
                lev = " IV";

            if (level == "5")
                lev = " V";

            if (level == "6")
                lev = " VI";

            if (level == "7")
                lev = " VII";

            if (level == "8")
                lev = " VIII";

            if (level == "9")
                lev = " IX";

            if (level == "10")
                lev = " X";

            if (level == "11")
                lev = " 11";

            if (level == "12")
                lev = " 12";

            string value = SubjectName + lev;

            return value;
        }

        private void txtKeyWord_TextChanged(object sender, EventArgs e)
        {
            List<string> lstSubject = new List<string>();
            lstSubjectView.Items.Clear();
            foreach (string Subj in SubjectList)
                if (Subj.Contains(txtKeyWord.Text))
                    lstSubjectView.Items.Add(Subj);
        }

    } 
}
