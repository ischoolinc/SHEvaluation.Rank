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
using SHEvaluation.Rank.DAO;
using FISCA.Data;
using FISCA.UDT;
using SHEvaluation.Rank.UDT;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankStep1_111 : BaseForm
    {
        List<TagConfigRecord> _TagList = new List<TagConfigRecord>();
        
        List<StudentRecord> _StudentList = new List<StudentRecord>();

        // 不排名學生ID
        List<string> studentFilterIDList = new List<string>();


        List<string> StudentIDListAll = new List<string>();
        List<string> studentFilterTagIDs = new List<string>();
        List<string> scoreErrIDList = new List<string>();
        DataTable deptDataTable = new DataTable();
        Dictionary<string, string> StudentDeptNameDict = new Dictionary<string, string>();
        Exception bkwException = null;
        List<StudentRecord> _StudentFilterList = new List<StudentRecord>();

        // Step1NextResult: 用於從 BackgroundWorker DoWork 傳遞資料到 RunWorkerCompleted
        private class Step1NextResult
        {
            public Dictionary<string, udtRegistrationDept> DeptDict { get; set; }
            public Dictionary<string, List<udtRegistrationSubjectNew>> SubjDict { get; set; }
            public Dictionary<string, string> DeptNameDict { get; set; }

            // 新增：把 Tag 過濾的 StudentTagRecord 也放進 Result
            public List<StudentTagRecord> Tag1Student { get; set; }
            public List<StudentTagRecord> Tag2Student { get; set; }
        }
        BackgroundWorker _bgWorkerLoadDefaultData = new BackgroundWorker();
        BackgroundWorker _bgWorkerStep1Next = new BackgroundWorker();
        Dictionary<string, List<GradeYearSemesterInfo>> StudGradeYearSemsDict = new Dictionary<string, List<GradeYearSemesterInfo>>();
        string studentFilter = "";
        string studentTag1 = "";
        string studentTag2 = "";
        int parseNumber = 0;


        CalculateTechnologyAssessmentRankStep2_111 ctrr2 = new CalculateTechnologyAssessmentRankStep2_111();

        public CalculateTechnologyAssessmentRankStep1_111()
        {
            InitializeComponent();
            _bgWorkerLoadDefaultData.DoWork += _bgWorkerLoadDefaultData_DoWork;
            _bgWorkerLoadDefaultData.RunWorkerCompleted += _bgWorkerLoadDefaultData_RunWorkerCompleted;
            _bgWorkerLoadDefaultData.ProgressChanged += _bgWorkerLoadDefaultData_ProgressChanged;
            _bgWorkerLoadDefaultData.WorkerReportsProgress = true;

            _bgWorkerStep1Next.DoWork += _bgWorkerStep1Next_DoWork;
            _bgWorkerStep1Next.RunWorkerCompleted += _bgWorkerStep1Next_RunWorkerCompleted;
            _bgWorkerStep1Next.ProgressChanged += _bgWorkerStep1Next_ProgressChanged;
            _bgWorkerStep1Next.WorkerReportsProgress = true;
        }

        private void _bgWorkerStep1Next_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FISCA.Presentation.MotherForm.SetStatusBarMessage("讀取學生清單", e.ProgressPercentage);
        }

        private void _bgWorkerStep1Next_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // 處理錯誤情況
            if (bkwException != null)
            {
                MessageBox.Show("學生列表讀取失敗：" + bkwException.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Visible = true;
                this.UserControlEnale(true);
                FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                bkwException = null; // 清除例外，避免下次誤判
                return;
            }

            if (_StudentFilterList.Count == 0)
            {
                MessageBox.Show("沒有找到符合條件的學生");
                this.Visible = true;
                this.UserControlEnale(true);
                FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                return;
            }

            // 從 DoWork 的結果取得資料
            Step1NextResult result = e.Result as Step1NextResult;
            if (result == null)
            {
                // 如果沒有結果資料，表示發生錯誤
                MessageBox.Show("資料處理發生錯誤", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Visible = true;
                this.UserControlEnale(true);
                FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                return;
            }

            // 從 Result 取得在背景執行中已處理好的 Tag 資料
            List<StudentTagRecord> tag1Student = result.Tag1Student ?? new List<StudentTagRecord>();
            List<StudentTagRecord> tag2Student = result.Tag2Student ?? new List<StudentTagRecord>();

            // 在 UI thread 設定 Step2 的資料
            ctrr2.SetRegistrationDept(result.DeptDict);
            ctrr2.SetRegistrationSubject(result.SubjDict);
            ctrr2.SetStudentDataList(_StudentFilterList, tag1Student, tag2Student, result.DeptNameDict, scoreErrIDList, _StudentList, studentFilterIDList);

            // 只呼叫一次 ShowDialog()
            ctrr2.StartPosition = FormStartPosition.CenterScreen;
            var dialogResult = ctrr2.ShowDialog();

            if (dialogResult == DialogResult.Cancel)
            {
                // 回到 Step1：重新啟用 Step1 的 UI
                FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                this.Visible = true;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.UserControlEnale(true);
            }
            else if (dialogResult == DialogResult.OK)
            {
                // 直接關閉 Step1，代表使用者在 Step2 完成整個流程
                this.Close();
            }

        }

        private void _bgWorkerStep1Next_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(studentFilter))
                {
                    studentFilterTagIDs = _TagList.Where(x => x.Prefix == studentFilter).Select(x => x.ID).ToList();
                    if (studentFilterTagIDs.Count == 0)
                    {
                        studentFilterTagIDs = _TagList.Where(x => x.Name == studentFilter).Select(x => x.ID).ToList();
                    }
                    studentFilterIDList = StudentTag.SelectAll().Where(x => studentFilterTagIDs.Contains(x.RefTagID)).Select(x => x.RefStudentID).ToList();
                    _StudentFilterList = _StudentFilterList.Where(x => !studentFilterIDList.Contains(x.ID)).ToList();
                }


                AccessHelper accessHelper = new AccessHelper();

                Dictionary<string, udtRegistrationDept> tmpDeptDict = new Dictionary<string, udtRegistrationDept>();
                // 設定群別 key: DeptName
                List<udtRegistrationDept> udtDeptList = accessHelper.Select<udtRegistrationDept>();
                foreach (udtRegistrationDept data in udtDeptList)
                {
                    if (!tmpDeptDict.ContainsKey(data.DeptName))
                        tmpDeptDict.Add(data.DeptName, data);
                }
                // 不在 DoWork 中呼叫 Step2 的 setter，改為放入 Result

                // 設定科目 key: SubjectName                
                List<udtRegistrationSubjectNew> SubjectList = new List<udtRegistrationSubjectNew>();
                SubjectList = accessHelper.Select<udtRegistrationSubjectNew>();

                Dictionary<string, List<udtRegistrationSubjectNew>> tmpSubjDict = new Dictionary<string, List<udtRegistrationSubjectNew>>();


                foreach (udtRegistrationSubjectNew data in SubjectList)
                {
                    if (!tmpSubjDict.ContainsKey(data.RegGroupName))
                    {
                        tmpSubjDict.Add(data.RegGroupName, new List<udtRegistrationSubjectNew>());
                        tmpSubjDict[data.RegGroupName].Add(data);
                    }
                    else
                        tmpSubjDict[data.RegGroupName].Add(data);

                }
                // 不在 DoWork 中呼叫 Step2 的 setter，改為放入 Result



                #region 取得篩選學生的科別
                string studentIDs = string.Join(",", _StudentFilterList.Select(x => x.ID).ToList());

                #region 取得科別的SQL
                string queryDeptSQL = @"
SELECT
student.id AS student_id
, COALESCE(dept.name, '') AS deptname
FROM
student
LEFT OUTER JOIN class
ON class.id = student.ref_class_id
LEFT OUTER JOIN dept
ON dept.id = COALESCE(student.ref_dept_id, class.ref_dept_id)
WHERE
student.id IN
(
" + studentIDs + @"
)
";
                #endregion
                if (studentIDs.Count() > 0)
                {
                    StudentDeptNameDict.Clear();
                    QueryHelper queryHelper = new QueryHelper();
                    deptDataTable = queryHelper.Select(queryDeptSQL);
                    foreach (DataRow dr in deptDataTable.Rows)
                    {
                        string sid = dr["student_id"].ToString();
                        if (!StudentDeptNameDict.ContainsKey(sid))
                        {
                            StudentDeptNameDict.Add(sid, dr["deptname"].ToString());
                        }
                    }
                }


                #endregion

                // ===================== 整理學生類別（背景執行） =====================
                List<StudentTagRecord> tag1Student = new List<StudentTagRecord>();
                List<StudentTagRecord> tag2Student = new List<StudentTagRecord>();

                // 一次抓全部 StudentTag
                List<StudentTagRecord> allStudentTags = StudentTag.SelectAll();
                var tagIdToStudentTags = allStudentTags.ToLookup(t => t.RefTagID);

                // 共用方法：依 Prefix/Name 找 TagID
                Func<string, List<string>> getTagIdsByPrefixOrName = (tagName) =>
                {
                    if (string.IsNullOrEmpty(tagName))
                        return new List<string>();

                    var ids = _TagList
                        .Where(x => x.Prefix == tagName)
                        .Select(x => x.ID)
                        .ToList();

                    if (ids.Count == 0)
                    {
                        ids = _TagList
                            .Where(x => x.Name == tagName)
                            .Select(x => x.ID)
                            .ToList();
                    }

                    return ids;
                };

                if (!string.IsNullOrEmpty(studentTag1))
                {
                    var tag1IDs = new HashSet<string>(getTagIdsByPrefixOrName(studentTag1));
                    tag1Student = tag1IDs
                        .SelectMany(id => tagIdToStudentTags[id])
                        .ToList();
                }

                if (!string.IsNullOrEmpty(studentTag2))
                {
                    var tag2IDs = new HashSet<string>(getTagIdsByPrefixOrName(studentTag2));
                    tag2Student = tag2IDs
                        .SelectMany(id => tagIdToStudentTags[id])
                        .ToList();
                }
                // ===================== 整理學生類別（結束） =====================

                // 將資料放入 Result，在 RunWorkerCompleted 中再設定給 Step2
                e.Result = new Step1NextResult
                {
                    DeptDict = tmpDeptDict,
                    SubjDict = tmpSubjDict,
                    DeptNameDict = StudentDeptNameDict,

                    // 新增這兩個回傳值
                    Tag1Student = tag1Student,
                    Tag2Student = tag2Student
                };

            }
            catch (Exception ex)
            {
                bkwException = ex;
            }
        }

        private void _bgWorkerLoadDefaultData_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FISCA.Presentation.MotherForm.SetStatusBarMessage("預設資料讀取中...", e.ProgressPercentage);
        }

        private void _bgWorkerLoadDefaultData_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            cboStudentFilter.Items.Clear();
            cboStudentTag1.Items.Clear();
            cboStudentTag2.Items.Clear();
            cboStudentFilter.Items.Add("");
            cboStudentTag1.Items.Add("");
            cboStudentTag2.Items.Add("");
            foreach (string tagName in _TagList.Select(x => x.Prefix).Distinct().ToList())
            {
                if (!string.IsNullOrEmpty(tagName))
                {
                    cboStudentFilter.Items.Add("[" + tagName + "]");
                    cboStudentTag1.Items.Add("[" + tagName + "]");
                    cboStudentTag2.Items.Add("[" + tagName + "]");
                }
            }
            foreach (string tagName in _TagList.Where(x => string.IsNullOrEmpty(x.Prefix)).Select(x => x.Name).ToList())
            {
                cboStudentFilter.Items.Add(tagName);
                cboStudentTag1.Items.Add(tagName);
                cboStudentTag2.Items.Add(tagName);
            }

            UserControlEnale(true);
            FISCA.Presentation.MotherForm.SetStatusBarMessage("");
        }

        private void _bgWorkerLoadDefaultData_DoWork(object sender, DoWorkEventArgs e)
        {
            LoadDefaultData();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            btnNext.Enabled = false;
            FISCA.Presentation.MotherForm.SetStatusBarMessage("資料處理中...");

            studentFilter = cboStudentFilter.Text.Trim('[', ']');
            studentTag1 = cboStudentTag1.Text.Trim('[', ']');
            studentTag2 = cboStudentTag2.Text.Trim('[', ']');

            ctrr2.SetButtonEnable(false);
            ctrr2.SetSelStudentTag(studentTag1, studentTag2, studentFilter);
            ctrr2.SetParseNumber(parseNumber);

            ctrr2.SetStudGradeYearSems(StudGradeYearSemsDict);

            _bgWorkerStep1Next.RunWorkerAsync();


        }

        private void btnSetDeptGroup_Click(object sender, EventArgs e)
        {
            btnSetDeptGroup.Enabled = false;
            SHEvaluation.Rank.TechnologyAssessmentRank_111.CalculateTechnologyAssessmentRankDeptGroupSet__111 ctrdg = new SHEvaluation.Rank.TechnologyAssessmentRank_111.CalculateTechnologyAssessmentRankDeptGroupSet__111();

            ctrdg.ShowDialog();
            btnSetDeptGroup.Enabled = true;
        }

        private void btnSetSubject_Click(object sender, EventArgs e)
        {
            btnSetSubject.Enabled = false;
            SortCalcSet ctrss = new SortCalcSet();
            ctrss.ShowDialog();
            btnSetSubject.Enabled = true;
        }

        private void CalculateTechnologyAssessmentRankStep1_Load(object sender, EventArgs e)
        {
            this.MaximumSize = this.MinimumSize = this.Size;
            this.StartPosition = FormStartPosition.CenterScreen;

            UserControlEnale(false);

            _bgWorkerLoadDefaultData.RunWorkerAsync();



        }

        private void LoadDefaultData()
        {
            _bgWorkerLoadDefaultData.ReportProgress(1);

            StudentIDListAll.Clear();
            // 過濾不排名名單與成績不滿5學期
            _StudentFilterList.Clear();
            scoreErrIDList.Clear();
            QueryData qd = new QueryData();

            //// 三年級一般生
            //_StudentList = K12.Data.Student.SelectAll().Where(x => (x.Status == StudentRecord.StudentStatus.一般) && !string.IsNullOrEmpty(x.RefClassID) && x.Class.GradeYear != null && x.Class.GradeYear.Value == 3).ToList();

            // 取得三年級一般狀態學生 ID
            StudentIDListAll = qd.GetStudentIDByGradeYearStatus1(3);

            _bgWorkerLoadDefaultData.ReportProgress(30);
            _StudentList = K12.Data.Student.SelectByIDs(StudentIDListAll);

            _TagList = K12.Data.TagConfig.SelectByCategory(TagCategory.Student);

            _bgWorkerLoadDefaultData.ReportProgress(50);

            //foreach (StudentRecord rec in _StudentList)
            //    StudentIDListAll.Add(rec.ID);         

            // 取得學生成績年級學生
            StudGradeYearSemsDict = qd.GetScoreGradeSemesterByGradeYear(StudentIDListAll);

            _bgWorkerLoadDefaultData.ReportProgress(70);

            // 完全沒成績
            foreach (string id in StudentIDListAll)
            {
                if (!StudGradeYearSemsDict.ContainsKey(id))
                    scoreErrIDList.Add(id);
            }

            // 未滿五學期還是可以使用
            //// 學期成績未滿5學期不能使用
            //foreach (string id in StudGradeYearSemsDict.Keys)
            //{
            //    if (StudGradeYearSemsDict[id].Count < 5)
            //        scoreErrIDList.Add(id);
            //}

            foreach (StudentRecord rec in _StudentList)
            {
                if (!scoreErrIDList.Contains(rec.ID))
                    _StudentFilterList.Add(rec);
            }


            _bgWorkerLoadDefaultData.ReportProgress(100);

        }

        private void UserControlEnale(bool value)
        {
            btnNext.Enabled = value;
            btnSetDeptGroup.Enabled = value;
            btnSetSubject.Enabled = value;
            cboStudentFilter.Enabled = value;
            cboStudentTag1.Enabled = value;
            cboStudentTag2.Enabled = value;
            iptParseNum.Enabled = value;

        }

        private void cboStudentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentFilter.Text == cboStudentTag1.Text || cboStudentFilter.Text == cboStudentTag2.Text)
            {
                cboStudentFilter.Text = "";
            }
        }

        private void cboStudentTag1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag1.Text == cboStudentFilter.Text || cboStudentTag1.Text == cboStudentTag2.Text)
            {
                cboStudentTag1.Text = "";
            }
            if (cboStudentTag1.Text != "")
            {
                cboStudentTag2.Enabled = true;
            }
            else
            {
                cboStudentTag2.Text = "";
                cboStudentTag2.Enabled = false;
            }
        }

        private void cboStudentTag2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag2.Text == cboStudentFilter.Text || cboStudentTag2.Text == cboStudentTag1.Text)
            {
                cboStudentTag2.Text = "";
            }
        }

        private void iptParseNum_ValueChanged(object sender, EventArgs e)
        {
            parseNumber = iptParseNum.Value;
        }

        //private void button1_Click(object sender, EventArgs e)
        //{
        //    List<udtRegistrationCalc> GroupCalc = new List<udtRegistrationCalc>();
        //    AccessHelper accessHelper = new AccessHelper();
        //    GroupCalc = accessHelper.Select<udtRegistrationCalc>();
        //    foreach (udtRegistrationCalc Calc in GroupCalc)
        //    {
        //        Calc.Deleted = true;
        //        Calc.Save();
        //    }
        //}
    }
}
