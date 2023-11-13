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
namespace SHEvaluation.Rank.TechnologyAssessmentRank_111
{
    public partial class CalculateTechnologyAccessmentRankSetDeptGroup111 : BaseForm
    {
        QueryData qd = new QueryData();
        Dictionary<string, string> GroupCodeDict = new Dictionary<string, string>();
        Dictionary<string, string> GroupNameDict = new Dictionary<string, string>();
        List<udtRegistrationDept> RegistrationDeptList = new List<udtRegistrationDept>();
        Dictionary<string, List<udtRegistrationDept>> GroupDeptDic = new Dictionary<string, List<udtRegistrationDept>>();
        AccessHelper accessHelper = new AccessHelper();
        Boolean ChangeFlag = false;
        private string strValue;
        public string StrValue
        {
            set
            {
                strValue = value;
            }
        }
        public CalculateTechnologyAccessmentRankSetDeptGroup111()
        {
            InitializeComponent();
        }

        private void CalculateTechnologyAccessmentRankSetDeptGroup_Load(object sender, EventArgs e)
        {
            LoadData();
        }
        private void LoadData()
        {
             btnDel.Enabled = btnAddDept.Enabled = btnRemoveDept.Enabled = false;


            // 取得資料庫內目前科別
            DataTable dt = qd.GetDeptInfoDT();

            ListViewItem lv = new ListViewItem();
            lstDeptView.Items.Clear();
            
            foreach (DataRow dr in dt.Rows)
            {
                string id = dr["id"].ToString();
                string Deptname = dr["name"].ToString(); ;
                lv = new ListViewItem();
                lv.Text = Deptname;
                lv.Tag = id;
                lstDeptView.Items.Add(lv);

            }
            ReflashData();
        }
        // 取得UDT 資料
        private void ReflashData()
        {
            GroupCodeDict.Clear();
            GroupNameDict.Clear();
            GroupDeptDic.Clear();
            RegistrationDeptList.Clear();
            RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();
            foreach (udtRegistrationDept group in RegistrationDeptList)
            {
                if (group.DeptName != "" && group.RegGroupCode != "")
                {
                    if (!GroupCodeDict.ContainsKey(group.RegGroupCode))
                        GroupCodeDict.Add(group.RegGroupCode, group.RegGroupName);
                    if (!GroupNameDict.ContainsKey(group.RegGroupName))
                        GroupNameDict.Add(group.RegGroupName, group.RegGroupCode);
                    if (!GroupDeptDic.ContainsKey(group.RegGroupName))
                    {
                        GroupDeptDic.Add(group.RegGroupName, new List<udtRegistrationDept>());
                        GroupDeptDic[group.RegGroupName].Add(group);
                    }
                    else
                        GroupDeptDic[group.RegGroupName].Add(group);
                    
                }
                else
                {
                    group.Deleted = true;
                    group.Save();
                }
            }
            for (int i = 0; i < lstDeptView.Items.Count; i++)
                lstDeptView.Items[i].Checked = false;
        }
        private void txtGroupID_TextChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < lstDeptView.Items.Count; i++)
                lstDeptView.Items[i].Checked = false;
            lstGroupDept.Items.Clear();
            if (GroupCodeDict.ContainsKey(txtGroupID.Text))
            {
                ChangeFlag = true;
                txtGroupName.Text = GroupCodeDict[txtGroupID.Text].ToString();

                ListViewItem lv = new ListViewItem();
                lstGroupDept.Items.Clear();
                if (GroupDeptDic.ContainsKey(txtGroupName.Text))
                {
                    foreach (udtRegistrationDept group in GroupDeptDic[txtGroupName.Text])
                    {
                        lv = new ListViewItem();
                        lv.Text = group.DeptName;
                        lv.Tag = group;
                        lstGroupDept.Items.Add(lv);
                    }
                }
                btnDel.Enabled = true;
            }
            else
            {
                ChangeFlag = true;
                txtGroupName.Text = "";
            }
            if (txtGroupID.Text != "")
            {
                btnAddDept.Enabled = btnRemoveDept.Enabled = btnDel.Enabled = true;
            }
            else
            {
                btnAddDept.Enabled = btnRemoveDept.Enabled = btnDel.Enabled = false;
            }
            ChangeFlag = false;
        }

        private void btnAddDept_Click(object sender, EventArgs e)
        {
            Boolean find = false;
            ListViewItem lv = new ListViewItem();
            for (int i = 0; i < lstDeptView.Items.Count; i++)
                if (lstDeptView.Items[i].Checked)
                {
                    for (int j = 0; j <lstGroupDept.Items.Count; j++)
                        if (lstDeptView.Items[i].Text == lstGroupDept.Items[j].Text)
                        {
                            find = true;
                        }
                    if (find == false)
                    {
                        udtRegistrationDept group = new udtRegistrationDept();
                        group.DeptName = lstDeptView.Items[i].Text;
                        group.RefDeptID = (string)lstDeptView.Items[i].Tag;
                        group.RegGroupCode = txtGroupID.Text;
                        group.RegGroupName = txtGroupName.Text;
                        group.Save();                                           
                    }
                    find = false;
                }
            ReflashData();
            lstGroupDept.Items.Clear();
            if (GroupDeptDic.ContainsKey(txtGroupName.Text))
            {
                foreach (udtRegistrationDept group in GroupDeptDic[txtGroupName.Text])
                {
                    lv = new ListViewItem();
                    lv.Text = group.DeptName;
                    lv.Tag = group;
                    lstGroupDept.Items.Add(lv);
                }
                
            }
        }

        private void btnRemoveDept_Click(object sender, EventArgs e)
        {
            ListViewItem lv = new ListViewItem();
            for (int i = 0; i <lstGroupDept.Items.Count; i++)
                if (lstGroupDept.Items[i].Checked)
                {
                    udtRegistrationDept group = (udtRegistrationDept)lstGroupDept.Items[i].Tag;
                    group.Deleted = true;
                    group.Save();                    
                }
            ReflashData();
            lstGroupDept.Items.Clear();
            if (GroupDeptDic.ContainsKey(txtGroupName.Text))
            {
                foreach (udtRegistrationDept group in GroupDeptDic[txtGroupName.Text])
                {
                    lv = new ListViewItem();
                    lv.Text = group.DeptName;
                    lv.Tag = group;
                    lstGroupDept.Items.Add(lv);
                }
            }
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
             RegGroupForm RegForm =new RegGroupForm(GroupCodeDict);
             RegForm.Owner = this;
             RegForm.ShowDialog();
             txtGroupID.Text = strValue;
        }

        private void txtGroupName_TextChanged(object sender, EventArgs e)
        {
            //if (GroupNameDict.ContainsKey(txtGroupName.Text))
            //    if (GroupNameDict[txtGroupName.Text] != txtGroupID.Text)
            //    {
            //        MsgBox.Show("群科名稱重覆");
            //        if (lstGroupDept.Items.Count > 0)
            //        {
            //            udtRegistrationDept group = (udtRegistrationDept)lstGroupDept.Items[0].Tag;
            //            txtGroupName.Text = group.RegGroupName;
            //        }
            //        else
            //           txtGroupName.Text = "";                    
            //    }
            
            //if (ChangeFlag == false)
            //{
            //    for (int i = 0; i < lstGroupDept.Items.Count; i++)
            //    {
            //        udtRegistrationDept group = (udtRegistrationDept)lstGroupDept.Items[i].Tag;
            //        group.RegGroupName = txtGroupName.Text;
            //        group.Save();
            //        lstGroupDept.Items[i].Tag = group;

            //    }
            //    ReflashData();
            //}
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lstGroupDept.Items.Count; i++)
            {
                udtRegistrationDept group = (udtRegistrationDept)lstGroupDept.Items[i].Tag;
                group.Deleted = true;
                group.Save();                
            }
            ReflashData();
            txtGroupID.Text = "";
            txtGroupName.Text = "'";
        }

        private void txtGroupName_Leave(object sender, EventArgs e)
        {
            if (GroupNameDict.ContainsKey(txtGroupName.Text))
                if (GroupNameDict[txtGroupName.Text] != txtGroupID.Text)
                {
                    MsgBox.Show("群科名稱重覆");
                    if (lstGroupDept.Items.Count > 0)
                    {
                        udtRegistrationDept group = (udtRegistrationDept)lstGroupDept.Items[0].Tag;
                        txtGroupName.Text = group.RegGroupName;
                    }
                    else
                        txtGroupName.Text = "";
                    ChangeFlag = true;
                }
               else
                    ChangeFlag = false;
            else
                ChangeFlag = false;
            if (ChangeFlag == false)
            {
                for (int i = 0; i < lstGroupDept.Items.Count; i++)
                {
                    udtRegistrationDept group = (udtRegistrationDept)lstGroupDept.Items[i].Tag;
                    group.RegGroupName = txtGroupName.Text;
                    group.Save();
                    lstGroupDept.Items[i].Tag = group;

                }
                ReflashData();
            }
        }

        private void txtGroupID_Leave(object sender, EventArgs e)
        {
            for (int i = 0; i < lstDeptView.Items.Count; i++)
                lstDeptView.Items[i].Checked = false;
            lstGroupDept.Items.Clear();
            if (GroupCodeDict.ContainsKey(txtGroupID.Text))
            {
                ChangeFlag = true;
                txtGroupName.Text = GroupCodeDict[txtGroupID.Text].ToString();

                ListViewItem lv = new ListViewItem();
                lstGroupDept.Items.Clear();
                if (GroupDeptDic.ContainsKey(txtGroupName.Text))
                {
                    foreach (udtRegistrationDept group in GroupDeptDic[txtGroupName.Text])
                    {
                        lv = new ListViewItem();
                        lv.Text = group.DeptName;
                        lv.Tag = group;
                        lstGroupDept.Items.Add(lv);
                    }
                }
                btnDel.Enabled = true;
            }
            else
            {
                ChangeFlag = true;
                txtGroupName.Text = "";
            }
            if (txtGroupID.Text != "")
            {
                btnAddDept.Enabled = btnRemoveDept.Enabled = btnDel.Enabled = true;
            }
            else
            {
                btnAddDept.Enabled = btnRemoveDept.Enabled = btnDel.Enabled = false;
            }
            ChangeFlag = false;
        }
    }

    
}
