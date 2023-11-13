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
    public partial class DeptGroupSet : BaseForm
    {
        
        public DeptGroupSet(string GroupName)
        {
            InitializeComponent();
            txtGroupID.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            LoadData();
            if (GroupName != "")
            {
                txtGroupName.Text = GroupName;
                txtGroupID.Enabled = false;
                txtGroupName.Enabled = false;
                lblKind.Text = "修改群組科別設定";
                ReflashData(GroupName);
            }
            else
            {
                txtGroupID.Enabled = true;
                txtGroupName.Enabled = true;
                lblKind.Text = "新增群組科別設定";
            }
        }

        private void LoadData()
        {
            
            QueryData qd = new QueryData();

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
        }
        // 取得UDT 資料
        private void ReflashData(string GroupName)
        {
            AccessHelper accessHelper = new AccessHelper();
            List<udtRegistrationDept> RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();
            ListViewItem lv = new ListViewItem();
            foreach (udtRegistrationDept group in RegistrationDeptList)
            {
                if (group.RegGroupName == GroupName)
                {
                    txtGroupID.Text = group.RegGroupCode;
                    lv = new ListViewItem();
                    lv.Text = group.DeptName;
                    lv.Tag = group;
                    lstGroupDept.Items.Add(lv);
                }
            }
            
            
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
                        lv = new ListViewItem();
                        lv.Text = lstDeptView.Items[i].Text;
                        
                        lstGroupDept.Items.Add(lv);
                    }
                    find = false;
                }
           

        }

        private void btnRemoveDept_Click(object sender, EventArgs e)
        {
            ListViewItem lv = new ListViewItem();
            List<string> lstDept = new List<string>();
            for (int i = 0; i <lstGroupDept.Items.Count; i++)
                if (!lstGroupDept.Items[i].Checked)
                {
                    lstDept.Add(lstGroupDept.Items[i].Text);                  
                }           
            lstGroupDept.Items.Clear();
            foreach (string  group in lstDept)
            {
                lv = new ListViewItem();
                lv.Text = group;                
                lstGroupDept.Items.Add(lv);
            }
            
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

       

        private void btnSave_Click(object sender, EventArgs e)
        { 
            if (txtGroupName.Text == "")
                {
                MsgBox.Show("群組名稱空白，不可儲存");
                return;
            }
            if (txtGroupID.Text == "")
            {
                MsgBox.Show("群組代碼空白，不可儲存");
                return;
            }
            if (lblKind.Text == "新增群組科別設定")
            {
                if (lstGroupDept.Items.Count < 1)
                {
                    MsgBox.Show("沒有設定科別，無法儲存");
                    return;
                }
            }
            else
            {
                if (lstGroupDept.Items.Count < 1)
                {
                    MsgBox.Show("沒有設定科別，設群組會被刪除");
                    return;
                }
            }
            //將全型代碼轉成半型
            char[] c = txtGroupID.Text.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                    c[i] = (char)(c[i] - 65248);
            }
            txtGroupID.Text = new string(c);
            AccessHelper accessHelper = new AccessHelper();
            List<udtRegistrationDept> RegistrationDeptList = new List<udtRegistrationDept>();
            if (lblKind.Text == "新增群組科別設定")
            {
                RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();                
                foreach (udtRegistrationDept group in RegistrationDeptList)
                {
                    if (group.RegGroupName == txtGroupName.Text)
                    {
                        MsgBox.Show("群組名稱已存在，無法新增");
                        return;
                    }
                }
            }
            
            RegistrationDeptList = accessHelper.Select<udtRegistrationDept>();
            List<string> lstDept = new List<string>();
            List<string> haslstDept = new List<string>();
            for (int i = 0; i < lstGroupDept.Items.Count; i++)
                lstDept.Add(lstGroupDept.Items[i].Text);
            foreach (udtRegistrationDept group in RegistrationDeptList)
            {               
                if (group.RegGroupName==txtGroupName.Text)
                {
                    //判斷是否原來有後來沒有
                    if (!lstDept.Contains(group.DeptName))
                    {
                        group.Deleted = true;
                        group.Save();
                    }
                    else
                    {
                        //記錄原有科別
                        haslstDept.Add(group.DeptName);
                    }
                }
            }
            //新增科別
            foreach (string deptstr in lstDept)
            {
                if (!haslstDept.Contains(deptstr))
                    { 
                    udtRegistrationDept deptrec = new udtRegistrationDept();
                    deptrec.DeptName = deptstr;
                    deptrec.RegGroupCode = txtGroupID.Text;
                    deptrec.RegGroupName = txtGroupName.Text;
                    deptrec.RegDeptName = deptstr;
                    deptrec.Save();
                   }                
            }
            this.Close();
        }

        private void txtGroupID_ImeModeChanged(object sender, EventArgs e)
        {
            txtGroupID.ImeMode = System.Windows.Forms.ImeMode.Off;
            
        }
    }

    
}
