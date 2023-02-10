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
            btnChineseDel.Enabled = false;
            btnGeneralDel.Enabled = false;
            btnEnglishDel.Enabled = false;
            btnMathDel.Enabled = false;
            btnSkillDel.Enabled = false;
            btnSpecialDel.Enabled = false;
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
            List<udtRegistrationCalc> GroupCalc = new List<udtRegistrationCalc>();
            

            foreach (string DeptName in RegistrationDeptDic[listDeptGroup.SelectedItem.ToString()])
            {
                GroupDept = GroupDept + DeptName + ",";
            }
            if (GroupDept != "")
            {
                foreach (udtRegistrationCalc Calc in GroupCalc)
                    if (Calc.RegGroupName== listDeptGroup.SelectedItem.ToString())
                    {
                        switch (Calc.CalcName)
                        {
                            case "學業":
                                cboGeneralCalc.Text=Calc.RegCalcKind;
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
                btnChineseDel.Enabled = true;
                btnGeneralDel.Enabled = true;
                btnEnglishDel.Enabled = true;
                btnMathDel.Enabled = true;
                btnSkillDel.Enabled = true;
                btnSpecialDel.Enabled = true;
            }
            else
            {
                btnChineseSel.Enabled = false;
                btnGeneralSel.Enabled = false;
                btnEnglishSel.Enabled = false;
                btnMathSel.Enabled = false;
                btnSkillSel.Enabled = false;
                btnSpecialSel.Enabled = false;
                btnChineseDel.Enabled = false;
                btnGeneralDel.Enabled = false;
                btnEnglishDel.Enabled = false;
                btnMathDel.Enabled = false;
                btnSkillDel.Enabled = false;
                btnSpecialDel.Enabled = false;
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

            if (GlobalValue.DataDict.ContainsKey(listDeptGroup.SelectedItem.ToString()))
            {
                foreach (udtRegistrationSubjectNew data in GlobalValue.DataDict[listDeptGroup.SelectedItem.ToString()])
                {
                    switch (data.CalcName)
                    {
                        case "學業":
                            lv = new ListViewItem();
                            lv.Text = data.SubjectName + data.GradeYear + (data.Semester == "1" ? "上" : "下");
                            lv.Tag = data;
                            lstGeneralSubj.Items.Add(lv);
                                                    
                            break;
                        case "專業":
                            lv = new ListViewItem();
                            lv.Text = data.SubjectName + data.GradeYear + (data.Semester == "1" ? "上" : "下");
                            lv.Tag = data;
                            lstSpecialSubj.Items.Add(lv);                            
                            break;
                        case "技能":
                            lv = new ListViewItem();
                            lv.Text = data.SubjectName + data.GradeYear + (data.Semester == "1" ? "上" : "下");
                            lv.Tag = data;
                            lstSkillSubj.Items.Add(lv);
                            break;
                        case "英文":
                            lv = new ListViewItem();
                            lv.Text = data.SubjectName + data.GradeYear + (data.Semester == "1" ? "上" : "下");
                            lv.Tag = data;
                            lstEnglishSubj.Items.Add(lv);
                            break;
                        case "國文":
                            lv = new ListViewItem();
                            lv.Text = data.SubjectName + data.GradeYear + (data.Semester == "1" ? "上" : "下");
                            lv.Tag = data;
                            lstChineseSubj.Items.Add(lv);
                            break;
                        case "數學":
                            lv = new ListViewItem();
                            lv.Text = data.SubjectName + data.GradeYear + (data.Semester == "1" ? "上" : "下");
                            lv.Tag = data;
                            lstMathSubj.Items.Add(lv);
                            break;
                    }
                }

            }
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

        private void btnGeneralDel_Click(object sender, EventArgs e)
        {
            udtRegistrationSubjectNew subj = new udtRegistrationSubjectNew();
            for (int i = 0; i < lstGeneralSubj.Items.Count; i++)
            {
                if (lstGeneralSubj.Items[i].Checked)
                {
                    subj = (udtRegistrationSubjectNew)lstGeneralSubj.Items[i].Tag;
                    subj.Deleted = true;
                    subj.Save();

                }
            }
            ReflashDataDict();
            ReflashView();
        }

        private void btnSpecialDel_Click(object sender, EventArgs e)
        {
            udtRegistrationSubjectNew subj = new udtRegistrationSubjectNew();
            for (int i = 0; i < lstSpecialSubj.Items.Count; i++)
            {
                if (lstSpecialSubj.Items[i].Checked)
                {
                    subj = (udtRegistrationSubjectNew)lstSpecialSubj.Items[i].Tag;
                    subj.Deleted = true;
                    subj.Save();

                }
            }
            ReflashDataDict();
            ReflashView();
        }

        private void btnSkillDel_Click(object sender, EventArgs e)
        {
            udtRegistrationSubjectNew subj = new udtRegistrationSubjectNew();
            for (int i = 0; i < lstSkillSubj.Items.Count; i++)
            {
                if (lstSkillSubj.Items[i].Checked)
                {
                    subj = (udtRegistrationSubjectNew)lstSkillSubj.Items[i].Tag;
                    subj.Deleted = true;
                    subj.Save();

                }
            }
            ReflashDataDict();
            ReflashView();
        }

        private void btnEnglishDel_Click(object sender, EventArgs e)
        {
            udtRegistrationSubjectNew subj = new udtRegistrationSubjectNew();
            for (int i = 0; i < lstEnglishSubj.Items.Count; i++)
            {
                if (lstEnglishSubj.Items[i].Checked)
                {
                    subj = (udtRegistrationSubjectNew)lstEnglishSubj.Items[i].Tag;
                    subj.Deleted = true;
                    subj.Save();

                }
            }
            ReflashDataDict();
            ReflashView();
        }

        private void btnChineseDel_Click(object sender, EventArgs e)
        {
            udtRegistrationSubjectNew subj = new udtRegistrationSubjectNew();
            for (int i = 0; i < lstChineseSubj.Items.Count; i++)
            {
                if (lstChineseSubj.Items[i].Checked)
                {
                    subj = (udtRegistrationSubjectNew)lstChineseSubj.Items[i].Tag;
                    subj.Deleted = true;
                    subj.Save();

                }
            }
            ReflashDataDict();
            ReflashView();
        }

        private void btnMathDel_Click(object sender, EventArgs e)
        {
            udtRegistrationSubjectNew subj = new udtRegistrationSubjectNew();
            for (int i = 0; i < lstMathSubj.Items.Count; i++)
            {
                if (lstMathSubj.Items[i].Checked)
                {
                    subj = (udtRegistrationSubjectNew)lstMathSubj.Items[i].Tag;
                    subj.Deleted = true;
                    subj.Save();

                }
            }
            ReflashDataDict();
            ReflashView();
        }

        private void cboGeneralCalc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboGeneralCalc.Tag != null)
            {
                udtRegistrationCalc Calc = new udtRegistrationCalc();
                Calc.CalcName = "學業";
                Calc.RegCalcKind = cboGeneralCalc.Text;
                Calc.RegGroupName = listDeptGroup.SelectedItem.ToString();
                Calc.Save();
                cboGeneralCalc.Tag = Calc;
            }
            else
            {
                udtRegistrationCalc Calc = new udtRegistrationCalc();
                Calc = (udtRegistrationCalc)cboGeneralCalc.Tag;
                if (Calc == null)
                    Calc = new udtRegistrationCalc();
                Calc.RegCalcKind = cboGeneralCalc.Text;
                Calc.Save();
            }
        }
    }
}
