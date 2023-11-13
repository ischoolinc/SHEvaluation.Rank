using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using K12.Data;
using SHEvaluation.Rank.UDT;
using System.Xml;
using SHEvaluation.Rank.DAO;
using FISCA.Data;
using FISCA.UDT;

namespace SHEvaluation.Rank
{

    public partial class SortCalcSet : BaseForm
    {
        
        Dictionary<string, List<string>> RegistrationDeptDic = new Dictionary<string, List<string>>();
        List<udtRegistrationDept> RegistrationDeptList = new List<udtRegistrationDept>();
        AccessHelper accessHelper = new AccessHelper();
        List<udtRegistrationSubjectNew> SubjectList = new List<udtRegistrationSubjectNew>();
        Boolean ChangeFlag = false;
        public SortCalcSet()
        {
            InitializeComponent();
        }

        private void SortCalcSet_Load(object sender, EventArgs e)
        {
            btnChineseSel.Enabled = false;
            btnGeneralSel.Enabled = false;
            btnEnglishSel.Enabled = false;
            btnMathSel.Enabled = false;
            btnSkillSel.Enabled = false;
            btnSpecialSel.Enabled = false;           
            cboChineseCalc.Items.Add("加權平均");
            cboChineseCalc.Items.Add("算術平均");
            cboGeneralCalc.Items.Add("加權平均");
            cboGeneralCalc.Items.Add("算術平均");
            cboEnglishCalc.Items.Add("加權平均");
            cboEnglishCalc.Items.Add("算術平均");
            cboSkillCalc.Items.Add("加權平均");
            cboSkillCalc.Items.Add("算術平均");
            cboMathCalc.Items.Add("加權平均");
            cboMathCalc.Items.Add("算術平均");
            cboSpecialCalc.Items.Add("加權平均");
            cboSpecialCalc.Items.Add("算術平均");
            cboGeneralContain.Items.Add("依所選科目");
            cboGeneralContain.Items.Add("依學業原始");
            cboChineseCalc.Enabled = false;
            cboGeneralContain.Enabled = false;
            cboEnglishCalc.Enabled = false;
            cboMathCalc.Enabled = false;
            cboSkillCalc.Enabled = false;
            cboSpecialCalc.Enabled = false;
            cboGeneralCalc.Enabled = false;
            // 取得群科設定UDT 資料
            RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();
            List<string> tmpName = new List<string>();

            foreach (udtRegistrationDept data in RegistrationDeptList)
                if (RegistrationDeptDic.ContainsKey(data.RegGroupName))
                    RegistrationDeptDic[data.RegGroupName].Add(data.DeptName);
                else
                {
                    RegistrationDeptDic.Add(data.RegGroupName, new List<string>());
                    RegistrationDeptDic[data.RegGroupName].Add(data.DeptName);
                }

            listDeptGroup.Items.Clear();
            foreach (string GroupName in RegistrationDeptDic.Keys)
            {
                if (GroupName != "")
                    listDeptGroup.Items.Add(GroupName);
            }
            
            ReflashDataDict();
            }


        private void ReflashDataDict()
        {
            // 讀取 科目設定UDT 資料
            SubjectList = accessHelper.Select<udtRegistrationSubjectNew>();
            //Dictionary<string, List<udtRegistrationSubjectNew>> dataDict = new Dictionary<string, List<udtRegistrationSubjectNew>>();
            GlobalValue.DataDict.Clear();
            foreach (udtRegistrationSubjectNew data in SubjectList)
            {
                if (!GlobalValue.DataDict.ContainsKey(data.RegGroupName))
                {
                    GlobalValue.DataDict.Add(data.RegGroupName, new List<udtRegistrationSubjectNew>());
                    GlobalValue.DataDict[data.RegGroupName].Add(data);
                }
                else
                {
                    GlobalValue.DataDict[data.RegGroupName].Add(data);

                }
            }
        }
        private void listDeptGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblGroupDept.Text = "";
            string GroupDept = "";
            cboGeneralCalc.Text = "";
            cboSpecialCalc.Text = "";
            cboSkillCalc.Text = "";
            cboEnglishCalc.Text = "";
            cboChineseCalc.Text = "";
            cboMathCalc.Text = "";
            cboGeneralCalc.Tag = null;
            cboSpecialCalc.Tag = null;
            cboSkillCalc.Tag = null;
            cboEnglishCalc.Tag = null;
            cboChineseCalc.Tag = null;
            cboMathCalc.Tag = null;
            List<udtRegistrationCalc> GroupCalc = new List<udtRegistrationCalc>();            
            
            GroupCalc = accessHelper.Select<udtRegistrationCalc>();
            if (listDeptGroup.SelectedItem!=null && RegistrationDeptDic.ContainsKey(listDeptGroup.SelectedItem.ToString()))
            {
                foreach (string DeptName in RegistrationDeptDic[listDeptGroup.SelectedItem.ToString()])
                {
                    GroupDept = GroupDept + DeptName + ",";
                }
            }
            if (GroupDept != "")
            {
                ChangeFlag = true;
                foreach (udtRegistrationCalc Calc in GroupCalc)                    
                    if (Calc.RegGroupName== listDeptGroup.SelectedItem.ToString())
                    {
                        switch (Calc.CalcName)
                        {
                            case "學業":
                                cboGeneralCalc.Text=Calc.RegCalcKind;
                                cboGeneralContain.Text = Calc.RegCalcItem;
                                cboGeneralCalc.Tag = Calc;
                                break;
                            case "專業":
                                cboSpecialCalc.Text = Calc.RegCalcKind;
                                cboSpecialCalc.Tag = Calc;
                                break;
                            case "技能":
                                cboSkillCalc.Text = Calc.RegCalcKind;
                                cboSkillCalc.Tag = Calc;
                                break;
                            case "英文":
                                cboEnglishCalc.Text = Calc.RegCalcKind;
                                cboEnglishCalc.Tag = Calc;
                                break;
                            case "國文":
                                cboChineseCalc.Text = Calc.RegCalcKind;
                                cboChineseCalc.Tag = Calc;
                                break;
                            case "數學":
                                cboMathCalc.Text = Calc.RegCalcKind;
                                cboMathCalc.Tag = Calc;
                                break;
                        }
                    }    
                    lblGroupDept.Text = GroupDept.Substring(0, GroupDept.Length - 1);
                // 取得群科設定UDT 資料
                RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();

                ReflashView();
                btnChineseSel.Enabled = true;
                btnGeneralSel.Enabled = true;
                btnEnglishSel.Enabled = true;
                btnMathSel.Enabled = true;
                btnSkillSel.Enabled = true;
                btnSpecialSel.Enabled = true;                
                cboChineseCalc.Enabled = true;
                cboGeneralContain.Enabled = true;
                cboEnglishCalc.Enabled = true;
                cboMathCalc.Enabled = true;
                cboSkillCalc.Enabled = true;
                cboSpecialCalc.Enabled = true;
                cboGeneralCalc.Enabled = true;                
            }
            else
            {
                btnChineseSel.Enabled = false;
                btnGeneralSel.Enabled = false;
                btnEnglishSel.Enabled = false;
                btnMathSel.Enabled = false;
                btnSkillSel.Enabled = false;
                btnSpecialSel.Enabled = false;                
                cboChineseCalc.Enabled = false;
                cboGeneralContain.Enabled = false;
                cboEnglishCalc.Enabled = false;
                cboMathCalc.Enabled = false;
                cboSkillCalc.Enabled = false;
                cboSpecialCalc.Enabled = false;
                cboGeneralCalc.Enabled = false;
            }
            ChangeFlag = false;
            if (cboGeneralContain.Text == "依學業原始")
            {
                lstGeneralSubj.Enabled = false;                
                btnGeneralSel.Enabled = false;
                
            }
            else
            {
                lstGeneralSubj.Enabled = true;                
                btnGeneralSel.Enabled = true;
                
            }
        }
        private void ReflashView()
        {
            lstChineseSubj.Clear();
            lstGeneralSubj.Clear();
            lstSpecialSubj.Clear();
            lstSkillSubj.Clear();
            lstEnglishSubj.Clear();
            lstMathSubj.Clear();
            ListViewItem lv = new ListViewItem();
            lblChinese.Text = "0";
            lblGeneral.Text=  "0";
            lblEnglish.Text = "0";
            lblSpecial.Text = "0";
            lblMath.Text=  "0";
            lblSkill.Text = "0";
            if (GlobalValue.DataDict.ContainsKey(listDeptGroup.SelectedItem.ToString()))
            {
                foreach (udtRegistrationSubjectNew data in GlobalValue.DataDict[listDeptGroup.SelectedItem.ToString()])
                {
                    switch (data.CalcName)
                    {
                        case "學業":
                            lv = new ListViewItem();
                            lv.Text = SubjFullName(data.SubjectName,data.Level) + "("+data.GradeYear + (data.Semester == "1" ? "上" : "下")+")";
                            lv.Tag = data;
                            lstGeneralSubj.Items.Add(lv);                                                    
                            break;
                        case "專業":
                            lv = new ListViewItem();
                            lv.Text = SubjFullName(data.SubjectName, data.Level) + "(" + data.GradeYear + (data.Semester == "1" ? "上" : "下") + ")";
                            lv.Tag = data;
                            lstSpecialSubj.Items.Add(lv);                            
                            break;
                        case "技能":
                            lv = new ListViewItem();
                            lv.Text = SubjFullName(data.SubjectName, data.Level) + "(" + data.GradeYear + (data.Semester == "1" ? "上" : "下") + ")";
                            lv.Tag = data;
                            lstSkillSubj.Items.Add(lv);
                            break;
                        case "英文":
                            lv = new ListViewItem();
                            lv.Text = SubjFullName(data.SubjectName, data.Level) + "(" + data.GradeYear + (data.Semester == "1" ? "上" : "下") + ")";
                            lv.Tag = data;
                            lstEnglishSubj.Items.Add(lv);
                            break;
                        case "國文":
                            lv = new ListViewItem();
                            lv.Text = SubjFullName(data.SubjectName, data.Level) + "(" + data.GradeYear + (data.Semester == "1" ? "上" : "下") + ")";
                            lv.Tag = data;
                            lstChineseSubj.Items.Add(lv);
                            break;
                        case "數學":
                            lv = new ListViewItem();
                            lv.Text = SubjFullName(data.SubjectName, data.Level) + "(" + data.GradeYear + (data.Semester == "1" ? "上" : "下") + ")";
                            lv.Tag = data;
                            lstMathSubj.Items.Add(lv);
                            break;
                    }
                }

            }
           
            lblChinese.Text = lstChineseSubj.Items.Count.ToString();
            lblGeneral.Text = lstGeneralSubj.Items.Count.ToString();
            lblEnglish.Text = lstEnglishSubj.Items.Count.ToString();
            lblSpecial.Text = lstSpecialSubj.Items.Count.ToString();
            lblMath.Text = lstMathSubj.Items.Count.ToString();
            lblSkill.Text = lstSkillSubj.Items.Count.ToString();
        }
        private void btnGeneralSel_Click(object sender, EventArgs e)
        {
            new SubjectSelect(1, lblGroupDept.Text.ToString(), listDeptGroup.SelectedItem.ToString()).ShowDialog();
            ReflashDataDict();
            ReflashView();
        }

        private void btnSpecialSel_Click(object sender, EventArgs e)
        {
            new SubjectSelect(2, lblGroupDept.Text.ToString(), listDeptGroup.SelectedItem.ToString()).ShowDialog();
            ReflashDataDict();
            ReflashView();
        }

        private void btnSkillSel_Click(object sender, EventArgs e)
        {
            new SubjectSelect(3, lblGroupDept.Text.ToString(), listDeptGroup.SelectedItem.ToString()).ShowDialog();
            ReflashDataDict();
            ReflashView();
        }

        private void btnEnglishSel_Click(object sender, EventArgs e)
        {
            new SubjectSelect(4, lblGroupDept.Text.ToString(), listDeptGroup.SelectedItem.ToString()).ShowDialog();
            ReflashDataDict();
            ReflashView();
        }

        private void btnChineseSel_Click(object sender, EventArgs e)
        {
            new SubjectSelect(5, lblGroupDept.Text.ToString(), listDeptGroup.SelectedItem.ToString()).ShowDialog();
            ReflashDataDict();
            ReflashView();
        }

        private void btnMathSel_Click(object sender, EventArgs e)
        {
            new SubjectSelect(6, lblGroupDept.Text.ToString(), listDeptGroup.SelectedItem.ToString()).ShowDialog();
            ReflashDataDict();
            ReflashView();
        }

        private void cboGeneralCalc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeFlag == false)
            {
                if (cboGeneralContain.Text == "依學業原始")                
                    cboGeneralCalc.Text = "算術平均";
                if (cboGeneralCalc.Tag == null)
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc.CalcName = "學業";
                    Calc.RegCalcKind = cboGeneralCalc.Text;
                    Calc.RegCalcItem = cboGeneralContain.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.Save();
                    cboGeneralCalc.Tag = Calc;
                }
                else
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc = (udtRegistrationCalc)cboGeneralCalc.Tag;
                    if (Calc == null)
                    {
                        Calc = new udtRegistrationCalc();
                        Calc.CalcName = "學業";
                        Calc.RegCalcKind = cboGeneralCalc.Text;
                        Calc.RegCalcItem = cboGeneralContain.Text;
                        Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    }
                    Calc.RegCalcItem = cboGeneralContain.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.RegCalcKind = cboGeneralCalc.Text;
                    Calc.Save();
                    cboGeneralCalc.Tag = Calc;
                }
            }
        }

        private void cboSpecialCalc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeFlag == false)
            {
                if (cboSpecialCalc.Tag == null)
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc.CalcName = "專業";
                    Calc.RegCalcKind = cboSpecialCalc.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.Save();
                    cboSpecialCalc.Tag = Calc;
                }
                else
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc = (udtRegistrationCalc)cboSpecialCalc.Tag;
                    if (Calc == null)
                    {
                        Calc = new udtRegistrationCalc();
                        Calc.CalcName = "專業";
                        Calc.RegCalcKind = cboSpecialCalc.Text;
                        Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    }
                    Calc.RegCalcKind = cboSpecialCalc.Text;
                    Calc.Save();
                    cboSpecialCalc.Tag = Calc;
                }
            }
        }

       

        private void cboEnglishCalc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeFlag == false)
            {
                if (cboEnglishCalc.Tag == null)
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc.CalcName = "英文";
                    Calc.RegCalcKind = cboEnglishCalc.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.Save();
                    cboEnglishCalc.Tag = Calc;
                }
                else
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc = (udtRegistrationCalc)cboEnglishCalc.Tag;
                    if (Calc == null)
                    {
                        Calc = new udtRegistrationCalc();
                        Calc.CalcName = "英文";
                        Calc.RegCalcKind = cboEnglishCalc.Text;
                        Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    }
                    Calc.RegCalcKind = cboEnglishCalc.Text;
                    Calc.Save();
                    cboEnglishCalc.Tag = Calc;
                }
            }
        }

        private void cboChineseCalc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeFlag == false)
            {
                if (cboChineseCalc.Tag == null)
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc.CalcName = "國文";
                    Calc.RegCalcKind = cboChineseCalc.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.Save();
                    cboChineseCalc.Tag = Calc;
                }
                else
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc = (udtRegistrationCalc)cboChineseCalc.Tag;
                    if (Calc == null)
                    {
                        Calc = new udtRegistrationCalc();
                        Calc.CalcName = "國文";
                        Calc.RegCalcKind = cboChineseCalc.Text;
                        Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    }
                    Calc.RegCalcKind = cboChineseCalc.Text;
                    Calc.Save();
                    cboChineseCalc.Tag = Calc;
                }
            }
        }

        private void cboMathCalc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeFlag == false)
            {
                if (cboMathCalc.Tag == null)
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc.CalcName = "數學";
                    Calc.RegCalcKind = cboMathCalc.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.Save();
                    cboMathCalc.Tag = Calc;
                }
                else
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc = (udtRegistrationCalc)cboMathCalc.Tag;
                    if (Calc == null)
                    {
                        Calc = new udtRegistrationCalc();
                        Calc.CalcName = "數學";
                        Calc.RegCalcKind = cboMathCalc.Text;
                        Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    }
                    Calc.RegCalcKind = cboMathCalc.Text;
                    Calc.Save();
                    cboMathCalc.Tag = Calc;
                }
            }
        }



        private void cboSkillCalc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeFlag == false)
            {
                if (cboSkillCalc.Tag == null)
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc.CalcName = "技能";
                    Calc.RegCalcKind = cboSkillCalc.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.Save();
                    cboSkillCalc.Tag = Calc;
                }
                else
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc = (udtRegistrationCalc)cboSkillCalc.Tag;
                    if (Calc == null)
                    {
                        Calc = new udtRegistrationCalc();
                        Calc.CalcName = "技能";
                        Calc.RegCalcKind = cboSkillCalc.Text;
                        Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    }
                    Calc.RegCalcKind = cboSkillCalc.Text;
                    Calc.Save();
                    cboSkillCalc.Tag = Calc;
                }
            }
        }

        private void cboGeneralContain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ChangeFlag == false)
            {
                if (cboGeneralContain.Text == "依學業原始")
                {
                    cboGeneralCalc.Text = "算術平均";
                    lstGeneralSubj.Enabled = false;                    
                    btnGeneralSel.Enabled = false;
                    
                }
                else
                {
                    lstGeneralSubj.Enabled = true;                    
                    btnGeneralSel.Enabled = true;
                    
                }
                    if (cboGeneralCalc.Tag == null)
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc.CalcName = "學業";                    
                    Calc.RegCalcKind = cboGeneralCalc.Text;
                    Calc.RegCalcItem = cboGeneralContain.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.Save();                    
                    cboGeneralCalc.Tag = Calc;
                }
                else
                {
                    udtRegistrationCalc Calc = new udtRegistrationCalc();
                    Calc = (udtRegistrationCalc)cboGeneralCalc.Tag;
                    if (Calc == null)
                    {
                        Calc = new udtRegistrationCalc();
                        Calc.CalcName = "學業";
                        Calc.RegCalcKind = cboGeneralCalc.Text;
                        Calc.RegCalcItem = cboGeneralContain.Text;
                        Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    }
                    Calc.RegCalcItem = cboGeneralContain.Text;
                    Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                    Calc.RegCalcKind = cboGeneralCalc.Text;
                    Calc.Save();
                    cboGeneralCalc.Tag = Calc;
                }
            }
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
    }
}
