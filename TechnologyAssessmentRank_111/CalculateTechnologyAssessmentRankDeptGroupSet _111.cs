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
using SHEvaluation.Rank.UDT;
using SHEvaluation.Rank.DAO;
using FISCA.UDT;
using K12.Data;
namespace SHEvaluation.Rank.TechnologyAssessmentRank_111
{
    public partial class CalculateTechnologyAssessmentRankDeptGroupSet__111 : BaseForm
    {
        public CalculateTechnologyAssessmentRankDeptGroupSet__111()
        {
            InitializeComponent();
            RefReshData();
        }
        private void RefReshData()
        {
            AccessHelper accessHelper = new AccessHelper();
            List<udtRegistrationDept> RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();
            Dictionary<string, string> GroupDict = new Dictionary<string, string>();
            lstDeptView.Items.Clear();
            foreach (udtRegistrationDept group in RegistrationDeptList)
            {
                if (group.DeptName != "" && group.RegGroupCode != "")
                {
                    if (!GroupDict.ContainsKey(group.RegGroupCode + group.RegGroupName))
                    {
                        GroupDict.Add(group.RegGroupCode + group.RegGroupName, group.DeptName);
                        ListViewItem lvi = new ListViewItem(group.RegGroupCode);
                        lvi.SubItems.Add(group.RegGroupName);
                        lvi.SubItems.Add(group.DeptName);
                        lstDeptView.Items.Add(lvi);
                    }
                    else
                        GroupDict[group.RegGroupCode + group.RegGroupName] = GroupDict[group.RegGroupCode + group.RegGroupName] + "," + group.DeptName;
                }
                
            }
            //foreach (string Group in GroupDict.Keys)
            //{
            //    ListViewItem lvi = new ListViewItem(Group.Split('+')[0]);
            //    lvi.SubItems.Add(Group.Split('+')[1]);
            //    lvi.SubItems.Add(GroupDict[Group]);                
            //    lstDeptView.Items.Add(lvi);
            //}
            for (int i = 0; i < lstDeptView.Items.Count; i++)
                if (GroupDict.ContainsKey(lstDeptView.Items[i].SubItems[0].Text + lstDeptView.Items[i].SubItems[1].Text))
                {
                    lstDeptView.Items[i].SubItems[2].Text = GroupDict[lstDeptView.Items[i].SubItems[0].Text + lstDeptView.Items[i].SubItems[1].Text];
                }
           lstDeptView.Sort();
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            DeptGroupSet RegForm = new DeptGroupSet("");            
            RegForm.ShowDialog();
            RefReshData();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstDeptView.Items.Count; i++)
                if (lstDeptView.Items[i].Selected)
                {
                    DeptGroupSet RegForm = new DeptGroupSet(lstDeptView.Items[i].SubItems[1].Text);
                    RegForm.ShowDialog();
                    RefReshData();
                }
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstDeptView.Items.Count; i++)
                if (lstDeptView.Items[i].Selected)
                {
                    AccessHelper accessHelper = new AccessHelper();                    
                    List<udtRegistrationDept> RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();
                    ListViewItem lv = new ListViewItem();
                    UpdateHelper uh = new UpdateHelper();
                    string strSQL;
                    foreach (udtRegistrationDept group in RegistrationDeptList)
                    {
                        if (group.RegGroupName == lstDeptView.Items[i].SubItems[1].Text)
                        {
                            group.Deleted = true;
                            group.Save();
                        }
                    }
                    //刪除計算方式
                    strSQL = "DELETE FROM  $campus.technology_star.registration_calc WHERE reg_group_name='" + lstDeptView.Items[i].SubItems[1].Text + "'";
                    uh.Execute(strSQL);
                    //刪除計算科目設定
                    strSQL = "DELETE FROM $campus.technology_star.registration_subjectnew WHERE reg_group_name='" + lstDeptView.Items[i].SubItems[1].Text+"'";
                    uh.Execute(strSQL);
                    
                }
            RefReshData();
        }
    }
}
