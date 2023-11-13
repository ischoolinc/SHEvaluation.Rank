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
        public List<string> lstSubject = new List<string>();
        public  List<string> lstSubjectRequire = new List<string>();
        public List<string> lstSubject1 = new List<string>();
        public List<string> lstSubjectRequire1 = new List<string>();
        public SubjectSelect(int SubjectKind, string Dept, string GroupName)
        {
            InitializeComponent();
            chk11.Checked = true;
            chk12.Checked = true;
            chk21.Checked = true;
            chk22.Checked = true;
            chk31.Checked = true;
            cboSubjKind.Items.Add("全部");
            cboSubjKind.Items.Add("部必");
            cboSubjKind.Text = "全部";
            lblDept.Text = GroupName + "(" + Dept + ")";
            lblGroupName.Text = GroupName;
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
            lstSubjectSure.Clear();
            //填入系統內科目設定
            if (GlobalValue.DataDict.ContainsKey(GroupName))
                foreach (udtRegistrationSubjectNew subj in GlobalValue.DataDict[GroupName])
                {
                    switch (SubjectKind)
                    {
                        case 1:
                            if (subj.CalcName == "學業")
                            {
                                SubjectStr = SubjFullName(subj.SubjectName,subj.Level) + "("+ subj.GradeYear + (subj.Semester == "1" ? "上" : "下") + ")";
                                lstSubjectSure.Items.Add(SubjectStr);
                            }
                            break;
                        case 2:
                            if (subj.CalcName == "專業")
                            {
                                SubjectStr = SubjFullName(subj.SubjectName, subj.Level) + "(" + subj.GradeYear + (subj.Semester == "1" ? "上" : "下") + ")";
                                lstSubjectSure.Items.Add(SubjectStr);
                            }
                            break;
                        case 3:
                            if (subj.CalcName == "技能")
                            {
                                SubjectStr = SubjFullName(subj.SubjectName, subj.Level) + "(" + subj.GradeYear + (subj.Semester == "1" ? "上" : "下") + ")";
                                lstSubjectSure.Items.Add(SubjectStr);
                            }
                            break;
                        case 4:
                            if (subj.CalcName == "英文")
                            {
                                SubjectStr = SubjFullName(subj.SubjectName, subj.Level) + "(" + subj.GradeYear + (subj.Semester == "1" ? "上" : "下") + ")";
                                lstSubjectSure.Items.Add(SubjectStr);
                            }
                            break;
                        case 5:
                            if (subj.CalcName == "國文")
                            {
                                SubjectStr = SubjFullName(subj.SubjectName, subj.Level) + "(" + subj.GradeYear + (subj.Semester == "1" ? "上" : "下") + ")";
                                lstSubjectSure.Items.Add(SubjectStr);
                            }
                            break;
                        case 6:
                            if (subj.CalcName == "數學")
                            {
                                SubjectStr = SubjFullName(subj.SubjectName, subj.Level) + "(" + subj.GradeYear + (subj.Semester == "1" ? "上" : "下") + ")";
                                lstSubjectSure.Items.Add(SubjectStr);
                            }
                            break;

                    }
                }
            lblSubjectCount.Text = "目前共有    " + lstSubjectSure.Items.Count + "      科";
            foreach (ClassRecord cr in helper.ClassHelper.GetAllClass())
            {

                if (lstDept.Contains(cr.Department) && cr.GradeYear == "3")
                {
                    lstClass.Add(cr.ClassID);

                }
            }
            if (lstClass.Count < 1)
                return;
            try
            {
                _CClassCourseInfoList = _da.GetCClassCourseInfoList(lstClass);
            }
            catch
            {
                MessageBox.Show("取得課程規劃資料失敗");
            }
            Dictionary<string, List<XElement>> dataDictE = new Dictionary<string, List<XElement>>();
            // 填入資料
            udtRegistrationSubjectNew SubjectRec = new udtRegistrationSubjectNew();
            lstSubject.Clear();
            lstSubjectRequire.Clear();
            lstSubject1.Clear();
            lstSubjectRequire1.Clear();
            foreach (CClassCourseInfo CoursePlan in _CClassCourseInfoList)
            // 資料整理
            {
                if (CoursePlan.RefGPlanXML != null)
                {
                    foreach (XElement elm in CoursePlan.RefGPlanXML.Elements("Subject"))
                    {
                        string idx = elm.Element("Grouping").Attribute("RowIndex").Value;

                        if (!dataDictE.ContainsKey(idx))
                            dataDictE.Add(idx, new List<XElement>());

                        dataDictE[idx].Add(elm);
                    }
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
                                        SubjectStr = SubjFullName(firstElm.Attribute("SubjectName").Value, elmD.Attribute("Level").Value) + "(" + elmD.Attribute("GradeYear").Value + (elmD.Attribute("Semester").Value == "1" ? "上" : "下") + ")";

                                        switch (SubjectKind)
                                        {

                                            case 1:

                                                if (!lstSubject.Contains(SubjectStr))
                                                {
                                                    lstSubject.Add(SubjectStr);
                                                    if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                        if (!lstSubjectRequire.Contains(SubjectStr))
                                                            lstSubjectRequire.Add(SubjectStr);
                                                    SubjectRec = new udtRegistrationSubjectNew();
                                                    SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                    SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                    SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                    SubjectRec.CalcName = "學業";
                                                    SubjectRec.RegGroupName = GroupName;
                                                    SubjectRec.Level = elmD.Attribute("Level").Value;
                                                    if (!dicSubj.ContainsKey(SubjectStr))
                                                        dicSubj.Add(SubjectStr, SubjectRec);
                                                }
                                                break;
                                            case 2:
                                                if ((firstElm.Attribute("Entry").Value == "實習科目" || firstElm.Attribute("Entry").Value == "專業科目"))
                                                {
                                                    if (!lstSubject.Contains(SubjectStr))
                                                    {
                                                        lstSubject.Add(SubjectStr);
                                                        if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                            if (!lstSubjectRequire.Contains(SubjectStr))
                                                                lstSubjectRequire.Add(SubjectStr);
                                                        SubjectRec = new udtRegistrationSubjectNew();
                                                        SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                        SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                        SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                        SubjectRec.CalcName = "專業";
                                                        SubjectRec.RegGroupName = GroupName;
                                                        SubjectRec.Level = elmD.Attribute("Level").Value;
                                                        if (!dicSubj.ContainsKey(SubjectStr))
                                                            dicSubj.Add(SubjectStr, SubjectRec);
                                                    }
                                                }
                                                break;
                                            case 3:
                                                if ((firstElm.Attribute("Entry").Value == "實習科目" || firstElm.Attribute("Entry").Value == "專業科目"))
                                                {
                                                    if (!lstSubject.Contains(SubjectStr))
                                                    {
                                                        lstSubject.Add(SubjectStr);
                                                        if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                            if (!lstSubjectRequire.Contains(SubjectStr))
                                                                lstSubjectRequire.Add(SubjectStr);
                                                        SubjectRec = new udtRegistrationSubjectNew();
                                                        SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                        SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                        SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                        SubjectRec.CalcName = "技能";
                                                        SubjectRec.RegGroupName = GroupName;
                                                        SubjectRec.Level = elmD.Attribute("Level").Value;
                                                        if (!dicSubj.ContainsKey(SubjectStr))
                                                            dicSubj.Add(SubjectStr, SubjectRec);
                                                    }
                                                }
                                                break;
                                            case 4:

                                                if (firstElm.Attribute("課程代碼").Value.Substring(19, 4) == "0102")
                                                {

                                                    if (!lstSubject.Contains(SubjectStr))
                                                    {
                                                        lstSubject.Add(SubjectStr);
                                                        if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                            if (!lstSubjectRequire.Contains(SubjectStr))
                                                                lstSubjectRequire.Add(SubjectStr);
                                                        //SubjectRec = new udtRegistrationSubjectNew();
                                                        //SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                        //SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                        //SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                        //SubjectRec.CalcName = "英文";
                                                        //SubjectRec.RegGroupName = GroupName;
                                                        //SubjectRec.Level = elmD.Attribute("Level").Value;
                                                        //if (!dicSubj.ContainsKey(SubjectStr))
                                                        //    dicSubj.Add(SubjectStr, SubjectRec);
                                                    }
                                                }
                                                if (!lstSubject1.Contains(SubjectStr))
                                                    lstSubject1.Add(SubjectStr);
                                                if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                    if (!lstSubjectRequire1.Contains(SubjectStr))
                                                        lstSubjectRequire1.Add(SubjectStr);
                                                SubjectRec = new udtRegistrationSubjectNew();
                                                SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                SubjectRec.CalcName = "英文";
                                                SubjectRec.RegGroupName = GroupName;
                                                SubjectRec.Level = elmD.Attribute("Level").Value;
                                                if (!dicSubj.ContainsKey(SubjectStr))
                                                    dicSubj.Add(SubjectStr, SubjectRec);
                                                break;
                                            case 5:
                                                if (firstElm.Attribute("課程代碼").Value.Substring(19, 4) == "0101")
                                                {
                                                    if (!lstSubject.Contains(SubjectStr))
                                                    {
                                                        lstSubject.Add(SubjectStr);
                                                        if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                            if (!lstSubjectRequire.Contains(SubjectStr))
                                                                lstSubjectRequire.Add(SubjectStr);
                                                        //SubjectRec = new udtRegistrationSubjectNew();
                                                        //SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                        //SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                        //SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                        //SubjectRec.CalcName = "國文";
                                                        //SubjectRec.RegGroupName = GroupName;
                                                        //SubjectRec.Level = elmD.Attribute("Level").Value;
                                                        //if (!dicSubj.ContainsKey(SubjectStr))
                                                        //    dicSubj.Add(SubjectStr, SubjectRec);
                                                    }
                                                }
                                                if (!lstSubject1.Contains(SubjectStr))
                                                    lstSubject1.Add(SubjectStr);
                                                if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                    if (!lstSubjectRequire1.Contains(SubjectStr))
                                                        lstSubjectRequire1.Add(SubjectStr);
                                                SubjectRec = new udtRegistrationSubjectNew();
                                                SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                SubjectRec.CalcName = "國文";
                                                SubjectRec.RegGroupName = GroupName;
                                                SubjectRec.Level = elmD.Attribute("Level").Value;
                                                if (!dicSubj.ContainsKey(SubjectStr))
                                                    dicSubj.Add(SubjectStr, SubjectRec);
                                                break;
                                            case 6:
                                                if (firstElm.Attribute("Domain").Value == "數學")
                                                {
                                                    if (!lstSubject.Contains(SubjectStr))
                                                    {
                                                        lstSubject.Add(SubjectStr);
                                                        if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                            if (!lstSubjectRequire.Contains(SubjectStr))
                                                                lstSubjectRequire.Add(SubjectStr);
                                                        //SubjectRec = new udtRegistrationSubjectNew();
                                                        //SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                        //SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                        //SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                        //SubjectRec.CalcName = "數學";
                                                        //SubjectRec.RegGroupName = GroupName;
                                                        //SubjectRec.Level = elmD.Attribute("Level").Value;
                                                        //if (!dicSubj.ContainsKey(SubjectStr))
                                                        //    dicSubj.Add(SubjectStr, SubjectRec);
                                                    }
                                                }
                                                if (!lstSubject1.Contains(SubjectStr))
                                                    lstSubject1.Add(SubjectStr);
                                                if (firstElm.Attribute("Required").Value == "必修" && firstElm.Attribute("RequiredBy").Value == "部訂")
                                                    if (!lstSubjectRequire1.Contains(SubjectStr))
                                                        lstSubjectRequire1.Add(SubjectStr);
                                                SubjectRec = new udtRegistrationSubjectNew();
                                                SubjectRec.GradeYear = elmD.Attribute("GradeYear").Value;
                                                SubjectRec.SubjectName = firstElm.Attribute("SubjectName").Value;
                                                SubjectRec.Semester = elmD.Attribute("Semester").Value;
                                                SubjectRec.CalcName = "數學";
                                                SubjectRec.RegGroupName = GroupName;
                                                SubjectRec.Level = elmD.Attribute("Level").Value;
                                                if (!dicSubj.ContainsKey(SubjectStr))
                                                    dicSubj.Add(SubjectStr, SubjectRec);
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
                    dataDictE.Clear();
                }
            }
            ReflashSubjectView();
        }
        private void ReflashSubjectView()
        {
            lstSubjectView.Clear();
            lstSubjectView_B.Clear();
            lstSubject.Sort();
            SubjectList = lstSubject;

            string Level = "";
            if (chk11.Checked)
                Level = Level + "1上";
            if (chk12.Checked)
                Level = Level + "1下";
            if (chk21.Checked)
                Level = Level + "2上";
            if (chk22.Checked)
                Level = Level + "2下";
            if (chk31.Checked)
                Level = Level + "3上";


            foreach (string Subject in lstSubject)
            {
                if (cboSubjKind.Text == "部必" )
                {
                    if (lstSubjectRequire.Contains(Subject))
                    {
                      if (Level.Contains(Subject.Substring(Subject.Length - 3, 2) ))
                        {
                            lstSubjectView_B.Items.Add(Subject);
                        }
                    }
                }
                else
                {
                    if (Level.Contains(Subject.Substring(Subject.Length - 3, 2)))
                    {
                        lstSubjectView_B.Items.Add(Subject);
                    }
                }
            }            
            ListViewItem lv;

            for (int i = 0; i < lstSubjectView_B.Items.Count; i++)
            {
                if (lstSubjectView_B.Items[i].Text.Contains(txtKeyWord.Text))
                {
                    lv = new ListViewItem();
                    lv.Text = lstSubjectView_B.Items[i].Text;
                    lv.Checked = lstSubjectView_B.Items[i].Checked;
                    lstSubjectView.Items.Add(lv);
                }
            }
            Boolean find = false;
            if (txtKeyWord.Text == "")
                return;
            if (lblSubjectKind.Text == "數學" || lblSubjectKind.Text == "國文" || lblSubjectKind.Text == "英文")
            {
                if (cboSubjKind.Text == "部必")
                {
                    foreach (String Subject in lstSubjectRequire1)
                    {
                        if (Subject.Contains(txtKeyWord.Text))
                        {
                            find = false;
                            for (int i = 0; i < lstSubjectView_B.Items.Count; i++)
                                if (lstSubjectView_B.Items[i].Text == Subject)
                                {
                                    find = true;
                                    break;
                                }
                            if (find == false && Level.Contains(Subject.Substring(Subject.Length - 3, 2)))
                            {
                                lstSubjectView.Items.Add(Subject);
                                lstSubjectView_B.Items.Add(Subject);
                            }
                        }
                    }
                }
                else
                {
                    foreach (String Subject in lstSubject1)
                    {
                        if (Subject.Contains(txtKeyWord.Text))
                        {
                            find = false;
                            for (int i = 0; i < lstSubjectView_B.Items.Count; i++)
                                if (lstSubjectView_B.Items[i].Text == Subject)
                                {
                                    find = true;
                                    break;
                                }
                            if (find == false && Level.Contains(Subject.Substring(Subject.Length - 3, 2)))
                            {
                                lstSubjectView.Items.Add(Subject);
                                lstSubjectView_B.Items.Add(Subject);
                            }
                        }
                    }

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
            
            if (GlobalValue.DataDict.ContainsKey(lblGroupName.Text))
                foreach (udtRegistrationSubjectNew subj in GlobalValue.DataDict[lblGroupName.Text])
                    if (subj.CalcName == lblKind.Text)
                    {
                        subj.Deleted = true;
                        subj.Save();
                    }
            for (int i = 0; i < lstSubjectSure.Items.Count; i++)
            {
                if (dicSubj.ContainsKey(lstSubjectSure.Items[i].Text))
                    {
                    if (GlobalValue.DataDict.ContainsKey(lblGroupName.Text))
                    {
                        GlobalValue.DataDict[lblGroupName.Text].Add(dicSubj[lstSubjectSure.Items[i].Text]);
                    }
                    else
                    {
                        GlobalValue.DataDict.Add(lblGroupName.Text, new List<udtRegistrationSubjectNew>());
                        GlobalValue.DataDict[lblGroupName.Text].Add(dicSubj[lstSubjectSure.Items[i].Text]);
                    }
                    dicSubj[lstSubjectSure.Items[i].Text].Save();
                }
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

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ListViewItem lv;
            for (int i = 0; i < lstSubjectView.Items.Count; i++)
            {
                if (lstSubjectView.Items[i].Checked == true)
                {
                    Boolean findflg = false;
                    for (int j = 0; j < lstSubjectSure.Items.Count; j++)
                        if (lstSubjectView.Items[i].Text==lstSubjectSure.Items[j].Text)
                        {
                            findflg = true;
                            break;
                        }
                    if (findflg == false)
                    {
                        lv = new ListViewItem();
                        lv.Text = lstSubjectView.Items[i].Text;
                        lstSubjectSure.Items.Add(lv);
                    }
                }
            }
            lblSubjectCount.Text = "目前共有    " + lstSubjectSure.Items.Count + "      科";
        }

        private void btnMove_Click(object sender, EventArgs e)
        {
            List<string> lstSubject = new List<string>();
            for (int j = 0; j < lstSubjectSure.Items.Count; j++)
                if (lstSubjectSure.Items[j].Checked==false)
                {
                    lstSubject.Add(lstSubjectSure.Items[j].Text);
                }
            lstSubjectSure.Clear();
            ListViewItem lv;
            foreach (string subjstr in lstSubject)
            {
                lv = new ListViewItem();
                lv.Text = subjstr;
                lstSubjectSure.Items.Add(lv);
            }
            lblSubjectCount.Text = "目前共有    " + lstSubjectSure.Items.Count + "      科";
        }

        private void cboSubjKind_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReflashSubjectView();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstSubjectView.Items.Count; i++)
                for (int j = 0; j < lstSubjectView_B.Items.Count; j++)
                {
                    if (txtKeyWord.Text == "")
                    {
                        if (!lstSubject.Contains(lstSubjectView_B.Items[j].Text))
                        {
                            lstSubjectView_B.Items[j].Remove();
                            break;
                        }
                    }
                    if (lstSubjectView_B.Items[j].Text == lstSubjectView.Items[i].Text)
                    {
                        lstSubjectView_B.Items[j].Checked = lstSubjectView.Items[i].Checked;
                        break;
                    }
                }
            lstSubjectView.Items.Clear();
            ListViewItem lv;

            for (int i = 0; i < lstSubjectView_B.Items.Count; i++)
            {
                if (lstSubjectView_B.Items[i].Text.Contains(txtKeyWord.Text))
                {
                    lv = new ListViewItem();
                    lv.Text = lstSubjectView_B.Items[i].Text;
                    lv.Checked = lstSubjectView_B.Items[i].Checked;
                    lstSubjectView.Items.Add(lv);
                }
            }
            Boolean find = false;
            string Level = "";
            if (chk11.Checked)
                Level = Level + "1上";
            if (chk12.Checked)
                Level = Level + "1下";
            if (chk21.Checked)
                Level = Level + "2上";
            if (chk22.Checked)
                Level = Level + "2下";
            if (chk31.Checked)
                Level = Level + "3上";
            if (txtKeyWord.Text == "")
                return;
            if (lblSubjectKind.Text == "數學" || lblSubjectKind.Text == "國文" || lblSubjectKind.Text == "英文")
            {
                if (cboSubjKind.Text == "部必")
                {
                    foreach (String Subject in lstSubjectRequire1)
                    {

                        if (Subject.Contains(txtKeyWord.Text))
                        {
                            find = false;
                            for (int i = 0; i < lstSubjectView_B.Items.Count; i++)
                                if (lstSubjectView_B.Items[i].Text == Subject)
                                {
                                    find = true;
                                    break;
                                }
                            if (find == false && Level.Contains(Subject.Substring(Subject.Length - 3, 2)))
                            {
                                lstSubjectView.Items.Add(Subject);
                                lstSubjectView_B.Items.Add(Subject);
                            }
                        }
                    }
                }
                else
                {
                    foreach (String Subject in lstSubject1)
                    {
                        if (Subject.Contains(txtKeyWord.Text))
                        {
                            find = false;
                            for (int i = 0; i < lstSubjectView_B.Items.Count; i++)
                                if (lstSubjectView_B.Items[i].Text == Subject)
                                {
                                    find = true;
                                    break;
                                }
                            if (find == false && Level.Contains(Subject.Substring(Subject.Length - 3, 2)))
                            {
                                lstSubjectView.Items.Add(Subject);
                                lstSubjectView_B.Items.Add(Subject);
                            }
                        }
                    }

                }
            }
        }

        private void chk11_CheckedChanged(object sender, EventArgs e)
        {
            ReflashSubjectView();
        }

        private void chk21_CheckedChanged(object sender, EventArgs e)
        {
            ReflashSubjectView();
        }

        private void chk31_CheckedChanged(object sender, EventArgs e)
        {
            ReflashSubjectView();
        }

        private void chk12_CheckedChanged(object sender, EventArgs e)
        {
            ReflashSubjectView();
        }

        private void chk22_CheckedChanged(object sender, EventArgs e)
        {
            ReflashSubjectView();
        }
    } 
}
