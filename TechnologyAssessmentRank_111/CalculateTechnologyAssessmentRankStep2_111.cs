﻿using System;
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
using Aspose.Cells;
using System.IO;
using FISCA.UDT;

namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankStep2_111 : BaseForm
    {

        // 全部被選學生
        List<StudentRecord> StudentAllList = new List<StudentRecord>();

        List<StudentRecord> StudentFilterList = new List<StudentRecord>();
        List<string> RankStudentIDList = new List<string>();
        List<StudentTagRecord> Tag1Student = new List<StudentTagRecord>();
        List<StudentTagRecord> Tag2Student = new List<StudentTagRecord>();

        // 不被列入排名清單
        Dictionary<string, string> StudentNotRankDict = new Dictionary<string, string>();


        Dictionary<string, List<GradeYearSemesterInfo>> StudGradeYearSemsDict = new Dictionary<string, List<GradeYearSemesterInfo>>();

        List<GradeYearSemesterInfo> calSemesterList = new List<GradeYearSemesterInfo>();

        Dictionary<string, Dictionary<string, string>> SubjectNameMapDict = new Dictionary<string, Dictionary<string, string>>();

        string SelStudentTag1 = "";
        string SelStudentTag2 = "";
        string SelStudentFilter = "";

        int parseNumber = 0;

        // 學生群別
        Dictionary<string, udtRegistrationDept> StudentGroupDict = new Dictionary<string, udtRegistrationDept>();

        // 群別
        Dictionary<string, udtRegistrationDept> RegistrationDeptDict = new Dictionary<string, udtRegistrationDept>();

        // 科目
        Dictionary<string, List<udtRegistrationSubjectNew>> RegistrationSubjectDict = new Dictionary<string, List<udtRegistrationSubjectNew>>();

        public CalculateTechnologyAssessmentRankStep2_111()
        {
            InitializeComponent();
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        public void SetButtonEnable(bool value)
        {
            this.btnPrevious.Enabled = value;
            this.btnCacluate.Enabled = value;
        }

        public void SetParseNumber(int num)
        {
            parseNumber = num;
        }

        public void SetStudGradeYearSems(Dictionary<string, List<GradeYearSemesterInfo>> data)
        {
            StudGradeYearSemsDict = data;
        }

        public void SetRegistrationDept(Dictionary<string, udtRegistrationDept> data)
        {
            RegistrationDeptDict = data;
        }
        public void SetRegistrationSubject(Dictionary<string, List<udtRegistrationSubjectNew> > data)
        {

            RegistrationSubjectDict = data;
        }

        private string parseString1(string str)
        {
            string val = "";
            str = str.Trim();
            string[] aa = str.Split(',');
            if (aa.Count() > 0)
            {
                val = "'" + string.Join("','", aa.ToArray()) + "'";
            }

            return val;
        }


        public void SetSelStudentTag(string tag1, string tag2, string flt)
        {
            SelStudentTag1 = tag1;
            SelStudentTag2 = tag2;
            SelStudentFilter = flt;
        }

        public void SetStudentDataList(List<StudentRecord> StudList, List<StudentTagRecord> tagList1, List<StudentTagRecord> tagList2, Dictionary<string, string> deptDict, List<string> scoreErrorIDs, List<StudentRecord> studentAllList, List<string> studentFilterIDs)
        {
            StudentFilterList = StudList;
            StudentAllList = studentAllList;
            Tag1Student = tagList1;
            Tag2Student = tagList2;
            studentFilterIDs = studentFilterIDs.Distinct().ToList();

            // 沒有群別
            List<string> noGroupIDList = new List<string>();

            #region 整理學生基本資料
            var studentViewList = (from student in StudentFilterList
                                   select new
                                   {
                                       studentID = student.ID,
                                       studentClass = student.Class.Name,
                                       studentSeatNo = student.SeatNo,
                                       studentNumber = student.StudentNumber,
                                       studentName = student.Name,
                                       RankGradeYear = "" + student.Class.GradeYear + "年級",
                                       RankClassName = student.Class.Name
                                   }).ToList();
            #endregion

            #region 將資料填入dataGridView
            List<DataGridViewRow> rowList = new List<DataGridViewRow>();

            // 學生群索引
            StudentGroupDict.Clear();
            // 畫面上學生
            dgvStudentList.Rows.Clear();
            // 需要排名學生ID
            RankStudentIDList.Clear();
            // 不排名
            StudentNotRankDict.Clear();

            bool hasGroup = false;
            foreach (var student in studentViewList)
            {
                hasGroup = false;
                string tag1 = "", tag2 = "";
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(dgvStudentList);
                row.Tag = student.studentID;
                row.Cells[colClass.Index].Value = student.studentClass;
                row.Cells[colSeatNum.Index].Value = student.studentSeatNo;
                row.Cells[colStudentNum.Index].Value = student.studentNumber;
                row.Cells[colStudentName.Index].Value = student.studentName;
                row.Cells[colSchoolRank.Index].Value = student.RankGradeYear;

                if (deptDict.ContainsKey(student.studentID))
                {

                    if (RegistrationDeptDict.ContainsKey(deptDict[student.studentID]))
                    {
                        row.Cells[colRegGroup.Index].Value = RegistrationDeptDict[deptDict[student.studentID]].RegGroupName;

                        row.Cells[colDeptName.Index].Value = RegistrationDeptDict[deptDict[student.studentID]].DeptName;
                        
                        row.Cells[colRegGroupCode.Index].Value = RegistrationDeptDict[deptDict[student.studentID]].RegGroupCode;
                        if (!string.IsNullOrEmpty(RegistrationDeptDict[deptDict[student.studentID]].RegGroupName))
                            hasGroup = true;

                        if (!StudentGroupDict.ContainsKey(student.studentID))
                            StudentGroupDict.Add(student.studentID, RegistrationDeptDict[deptDict[student.studentID]]);
                    }
                }

                row.Cells[colClassRank.Index].Value = student.RankClassName;
                if (Tag1Student.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                {
                    tag1 = Tag1Student.First(x => x.RefStudentID == student.studentID).Name;
                    row.Cells[colRankType1.Index].Value = tag1;
                }
                if (Tag2Student.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                {
                    tag2 = Tag2Student.First(x => x.RefStudentID == student.studentID).Name;
                    row.Cells[colRankType2.Index].Value = tag2;
                }

                // 有群別再加入
                if (hasGroup)
                {
                    RankStudentIDList.Add(student.studentID);
                    rowList.Add(row);
                }
                else
                {
                    if (!noGroupIDList.Contains(student.studentID))
                        noGroupIDList.Add(student.studentID);
                }

            }

            dgvStudentList.Rows.AddRange(rowList.ToArray());
            #endregion

            lblMsg.Text = "共 " + dgvStudentList.Rows.Count + " 位學生";

            // 整理不排名清單
            // 先暫時註解未滿5學期限制
            //foreach (string id in scoreErrorIDs)
            //{
            //    if (!StudentNotRankDict.ContainsKey(id))
            //        StudentNotRankDict.Add(id, "學期科目成績未滿5學期");
            //}
            foreach (string id in noGroupIDList)
            {
                if (!StudentNotRankDict.ContainsKey(id))
                    StudentNotRankDict.Add(id, "沒有報名群");
                else
                    StudentNotRankDict[id] += ",沒有報名群";
            }


            foreach (string id in studentFilterIDs)
            {
                if (!StudentNotRankDict.ContainsKey(id))
                    StudentNotRankDict.Add(id, "設定不排名學生");
                else
                    StudentNotRankDict[id] += ",設定不排名學生";
            }

            btnPrevious.Enabled = true;
            btnCacluate.Enabled = true;
            FISCA.Presentation.MotherForm.SetStatusBarMessage("學生列表讀取完成");
        }

        private void CalculateTechnologyAssessmentRankStep2_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            pbLoading.Visible = false;
            calSemesterList.Clear();
            for (int i = 1; i <= 3; i++)
            {
                for (int j = 1; j <= 2; j++)
                {
                    if (i == 3 && j == 2)
                        continue;

                    GradeYearSemesterInfo gs = new GradeYearSemesterInfo();
                    gs.GradeYear = i;
                    gs.Semester = j;
                    calSemesterList.Add(gs);
                }
            }
        }

        private void btnCacluate_Click(object sender, EventArgs e)
        {
            btnCacluate.Enabled = false;
            btnPrevious.Enabled = false;
            Dictionary<string, Dictionary<string, List<string>>> RegGroupSubjMapDic =new Dictionary<string, Dictionary<string, List<string>>>();
            Dictionary<string, Dictionary<string, List<string>>> RegGroupCalcMapDic = new Dictionary<string, Dictionary<string, List<string>>>();
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
            pbLoading.Visible = true;
            
            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                FISCA.Presentation.MotherForm.SetStatusBarMessage("計算排名中", eventArgs.ProgressPercentage);
            };
            // 讀取 科目設定UDT 資料
            AccessHelper accessHelper = new AccessHelper();
            List<udtRegistrationSubjectNew> SubjectList = new List<udtRegistrationSubjectNew>();
            SubjectList = accessHelper.Select<udtRegistrationSubjectNew>();
            Dictionary<string, Dictionary<string, List<udtRegistrationSubjectNew>>> DataDict = new Dictionary<string, Dictionary<string, List<udtRegistrationSubjectNew>>>();
            foreach (udtRegistrationSubjectNew data in SubjectList)
            {
                if (!DataDict.ContainsKey(data.RegGroupName))
                {
                    DataDict.Add(data.RegGroupName, new Dictionary<string, List<udtRegistrationSubjectNew>>());
                    if (!DataDict[data.RegGroupName].ContainsKey(data.CalcName))
                        {
                        DataDict[data.RegGroupName].Add(data.CalcName, new List<udtRegistrationSubjectNew>());
                        DataDict[data.RegGroupName][data.CalcName].Add(data);
                    }
                    else
                        DataDict[data.RegGroupName][data.CalcName].Add(data);

                }
                else
                {
                    if (!DataDict[data.RegGroupName].ContainsKey(data.CalcName))
                        {
                        DataDict[data.RegGroupName].Add(data.CalcName, new List<udtRegistrationSubjectNew>());
                        DataDict[data.RegGroupName][data.CalcName].Add(data);
                    }
                    else
                        DataDict[data.RegGroupName][data.CalcName].Add(data);

                }
            }
            

            bkw.DoWork += delegate
            {
                bkw.ReportProgress(1);
                #region 產生學生清單的SQL
                List<string> studentSqlList = new List<string>();
                foreach (DataGridViewRow row in dgvStudentList.Rows)
                {
              
                    #region 單筆學生資料的SQL
                    string studentSql = @"
    SELECT
        '" + row.Tag + @"'::BIGINT AS student_id
        , '" + "" + row.Cells[colStudentName.Index].Value + @"'::TEXT AS student_name
        , '" + ("" + row.Cells[colSchoolRank.Index].Value).Trim('年', '級') + @"'::INT AS rank_grade_year
        , '" + "" + row.Cells[colDeptName.Index].Value + @"'::TEXT AS rank_dept_name
        , '" + "" + row.Cells[colClassRank.Index].Value + @"'::TEXT AS rank_class_name
        , '" + "" + row.Cells[colRankType1.Index].Value + @"'::TEXT AS rank_tag1
        , '" + "" + row.Cells[colRankType2.Index].Value + @"'::TEXT AS rank_tag2
        , '" + "" + row.Cells[colRegGroup.Index].Value + @"'::TEXT AS rank_group_name
        , '" + "" + row.Cells[colRegGroupCode.Index].Value + @"'::TEXT AS rank_group_code
    ";
                    #endregion
                    //將各群組學生加入科目對照表
                    if (!RegGroupSubjMapDic.ContainsKey("" + row.Cells[colRegGroup.Index].Value))
                    {
                        RegGroupSubjMapDic.Add("" + row.Cells[colRegGroup.Index].Value, new Dictionary<string, List<string>>());
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value].Add("學業", new List<string>());
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["學業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value].Add("專業", new List<string>());
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value].Add("技能", new List<string>());
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value].Add("英文", new List<string>());
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value].Add("國文", new List<string>());
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value].Add("數學", new List<string>());
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic["" + row.Cells[colRegGroup.Index].Value]["數學"].Add("'沒有設定科目'");
                    }
                    //將各群組學生加入成綪計算方式對照表
                    if (!RegGroupCalcMapDic.ContainsKey("" + row.Cells[colRegGroup.Index].Value))
                    {
                        RegGroupCalcMapDic.Add("" + row.Cells[colRegGroup.Index].Value, new Dictionary<string, List<string>>());
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value].Add("學業", new List<string>());
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["學業"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["學業"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value].Add("專業", new List<string>());
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["專業"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["專業"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value].Add("技能", new List<string>());
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["技能"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["技能"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value].Add("英文", new List<string>());
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["英文"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["英文"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value].Add("國文", new List<string>());
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["國文"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["國文"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value].Add("數學", new List<string>());
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["數學"].Add("");
                        RegGroupCalcMapDic["" + row.Cells[colRegGroup.Index].Value]["數學"].Add("");

                    }

                    //把單筆學生資料的SQL加入到List                    
                    studentSqlList.Add(studentSql);
                }
                //填入群組計算方式
                List<udtRegistrationCalc> GroupCalc = new List<udtRegistrationCalc>();

                GroupCalc = accessHelper.Select<udtRegistrationCalc>();
                foreach (udtRegistrationCalc Calc in GroupCalc)
                { 
                    if (!RegGroupCalcMapDic.ContainsKey(Calc.RegGroupName))
                    {
                        RegGroupCalcMapDic.Add(Calc.RegGroupName, new Dictionary<string, List<string>>());
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("學業", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["學業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["學業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("專業", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["專業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["專業"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("技能", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["技能"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["技能"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("英文", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["英文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["英文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("國文", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["國文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["國文"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName].Add("數學", new List<string>());
                        RegGroupCalcMapDic[Calc.RegGroupName]["數學"].Add("");
                        RegGroupCalcMapDic[Calc.RegGroupName]["數學"].Add("");
                    }
                    RegGroupCalcMapDic[Calc.RegGroupName][Calc.CalcName][0] = Calc.RegCalcKind;
                    RegGroupCalcMapDic[Calc.RegGroupName][Calc.CalcName][1] = Calc.RegCalcItem;
                    //若該群組只有設定學業原始，則只會有學業成績
                    if (RegGroupCalcMapDic[Calc.RegGroupName]["學業"][1] == "依學業原始")
                    {
                        if (!RegGroupSubjMapDic.ContainsKey(Calc.RegGroupName))
                        {
                            RegGroupSubjMapDic.Add(Calc.RegGroupName, new Dictionary<string, List<string>>());
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("學業", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("專業", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["專業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("技能", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["技能"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("英文", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["英文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("國文", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["國文"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName].Add("數學", new List<string>());
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[Calc.RegGroupName]["數學"].Add("'沒有設定科目'");
                        }
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][0] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][1] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][2] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][3] = "'依學業原始'";
                        RegGroupSubjMapDic[Calc.RegGroupName]["學業"][4] = "'依學業原始'";
                    }
                }
                //填入科目
                List<string> SubjectC = new List<string>();
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                SubjectC.Add("'沒有設定科目',");
                //依群組填入各項目科目
                foreach (string group in DataDict.Keys)
                {
                    if (!RegGroupCalcMapDic.ContainsKey(group))
                    {
                        RegGroupCalcMapDic.Add(group, new Dictionary<string, List<string>>());
                        RegGroupCalcMapDic[group].Add("學業", new List<string>());
                        RegGroupCalcMapDic[group]["學業"].Add("");
                        RegGroupCalcMapDic[group]["學業"].Add("");
                        RegGroupCalcMapDic[group].Add("專業", new List<string>());
                        RegGroupCalcMapDic[group]["專業"].Add("");
                        RegGroupCalcMapDic[group]["專業"].Add("");
                        RegGroupCalcMapDic[group].Add("技能", new List<string>());
                        RegGroupCalcMapDic[group]["技能"].Add("");
                        RegGroupCalcMapDic[group]["技能"].Add("");
                        RegGroupCalcMapDic[group].Add("英文", new List<string>());
                        RegGroupCalcMapDic[group]["英文"].Add("");
                        RegGroupCalcMapDic[group]["英文"].Add("");
                        RegGroupCalcMapDic[group].Add("國文", new List<string>());
                        RegGroupCalcMapDic[group]["國文"].Add("");
                        RegGroupCalcMapDic[group]["國文"].Add("");
                        RegGroupCalcMapDic[group].Add("數學", new List<string>());
                        RegGroupCalcMapDic[group]["數學"].Add("");
                        RegGroupCalcMapDic[group]["數學"].Add("");
                    }
                    if (!RegGroupSubjMapDic.ContainsKey(group))
                    {
                        RegGroupSubjMapDic.Add(group, new Dictionary<string, List<string>>());
                        
                        if (RegGroupCalcMapDic[group]["學業"][1] == "依學業原始")
                        {
                            RegGroupSubjMapDic[group].Add("學業", new List<string>());
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                            RegGroupSubjMapDic[group]["學業"].Add("'依學業原始'");
                        }
                        else
                        {
                            RegGroupSubjMapDic[group].Add("學業", new List<string>());
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                            RegGroupSubjMapDic[group]["學業"].Add("'沒有設定科目'");
                        }
                        RegGroupSubjMapDic[group].Add("專業", new List<string>());
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["專業"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("技能", new List<string>());
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["技能"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("英文", new List<string>());
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["英文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("國文", new List<string>());
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["國文"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group].Add("數學", new List<string>());
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                        RegGroupSubjMapDic[group]["數學"].Add("'沒有設定科目'");
                    }
                    List<string> CalcString = new List<string>();
                    //CalcString.Add("學業");
                    //CalcString.Add("專業");
                    //CalcString.Add("技能");
                    //CalcString.Add("英文");
                    //CalcString.Add("國文");
                    //CalcString.Add("數學");
                    //依計算項目加入科目
                    foreach (string Calc in DataDict[group].Keys)
                    {
                        if (Calc == "學業" && RegGroupCalcMapDic[group]["學業"][1] == "依學業原始")
                        {
                            RegGroupSubjMapDic[group][Calc][0] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][1] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][2] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][3] = "'依學業原始'";
                            RegGroupSubjMapDic[group][Calc][4] = "'依學業原始'";
                        }
                        else
                        {
                            //組合科目名稱
                            if (DataDict[group].ContainsKey(Calc))
                            { 
                                foreach (udtRegistrationSubjectNew data in DataDict[group][Calc])
                                {
                                    SubjectC[((int.Parse(data.GradeYear) - 1) * 2 + int.Parse(data.Semester)) - 1] = SubjectC[((int.Parse(data.GradeYear) - 1) * 2 + int.Parse(data.Semester)) - 1] + "'" + data.SubjectName + "',";
                                }
                            }
                            //放入5學期科目
                            RegGroupSubjMapDic[group][Calc][0] = SubjectC[0].Substring(0, SubjectC[0].Length - 1);
                            RegGroupSubjMapDic[group][Calc][1] = SubjectC[1].Substring(0, SubjectC[1].Length - 1);
                            RegGroupSubjMapDic[group][Calc][2] = SubjectC[2].Substring(0, SubjectC[2].Length - 1);
                            RegGroupSubjMapDic[group][Calc][3] = SubjectC[3].Substring(0, SubjectC[3].Length - 1);
                            RegGroupSubjMapDic[group][Calc][4] = SubjectC[4].Substring(0, SubjectC[4].Length - 1);
                        }
                        SubjectC[0]="'沒有設定科目',";
                        SubjectC[1] = "'沒有設定科目',";
                        SubjectC[2] = "'沒有設定科目',";
                        SubjectC[3] = "'沒有設定科目',";
                        SubjectC[4] = "'沒有設定科目',";
                   }
                }
                        //把剛剛組好的學生資料的SQL的List拆開
                        #region 所有學生資料的SQL
                        string studentListSql = @"
WITH student_list AS
(
    " + string.Join(@"
    UNION ALL", studentSqlList) + @"
)
";
                #endregion
                #endregion

                //// debug 
                //string fiPath = Application.StartupPath + @"\sql1.txt";
                //using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                //{
                //    fi.WriteLine(studentListSql);
                //}

                //< Subject 不計學分 = "否" 不需評分 = "否" 修課必選修 = "必修" 修課校部訂 = "校訂" 原始成績 = "82" 學年調整成績 = "" 擇優採計成績 = "" 是否取得學分 = "是" 是否補修成績 = "否" 科目 = "英文文法" 科目級別 = "2" 補考成績 = "" 重修學年度 = "" 重修學期 = "" 重修成績 = "" 開課分項類別 = "學業" 開課學分數 = "1" />

                string calculationSetting = "";

                #region 產生計算規則的SQL
                #region 產生要儲存到rank_batch的setting的Xml
                XmlDocument xdoc = new XmlDocument();
                var settingEle = xdoc.CreateElement("Setting");
                settingEle.SetAttribute("不排名學生類別", SelStudentFilter);
                settingEle.SetAttribute("類別一", SelStudentTag1);
                //settingEle.SetAttribute("類別二", SelStudentTag2);
                settingEle.SetAttribute("年級", "3");
                settingEle.SetAttribute("成績四捨五入小數位數", parseNumber.ToString());

                calculationSetting = settingEle.OuterXml;
                #endregion
                #endregion

                #region 產生學生學期成績對照表
                List<string> StudSemsSQLList = new List<string>();

                foreach (string sid in RankStudentIDList)
                {
                    if (StudGradeYearSemsDict.ContainsKey(sid))
                    {
                        foreach (GradeYearSemesterInfo gs in StudGradeYearSemsDict[sid])
                        {
                            // 取五學期 3 年級第2學期不取
                            if (gs.GradeYear == 3 && gs.Semester == 2)
                                continue;
                            
                            // todo 處理重讀--Cyn

                            string qry = @"
SELECT " + sid + @" ::BIGINT AS student_id
, " + gs.GradeYear + @" ::INT AS sems_grade_year
, " + gs.Semester + @" ::INT AS sems_semester 
, " + gs.SchoolYear + @" ::INT AS sems_school_year";

                            StudSemsSQLList.Add(qry);
                        }
                    }
                }

                string StudSemsSQL = @"
, student_sems AS
(
    " + string.Join(@"
    UNION ALL", StudSemsSQLList) + @"
)";

                string insertRankSql = @"
" + studentListSql + @"
" + StudSemsSQL + @"
,parse_number AS
(
    SELECT " + parseNumber + @" ::INT AS parse_number
)              
, subject_score_data AS
(
	SELECT
		 student_list.student_id
        , sems_subj_score_ext.school_year
        , sems_subj_score_ext.semester
        , sems_subj_score_ext,grade_year
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, student_list.rank_group_name
        , student_list.rank_group_code
		, array_to_string(xpath('/root/Subject/@科目', xmlparse(content  concat('<root>', subj_score_ele , '</root>'))), '')::TEXT As subject
		, NULLIF(array_to_string(xpath('/root/Subject/@原始成績', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_origin_score
		, NULLIF(array_to_string(xpath('/root/Subject/@開課學分數', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::DECIMAL As subject_credit
		, NULLIF(array_to_string(xpath('/root/Subject/@開課分項類別', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As subject_entry
        , NULLIF(array_to_string(xpath('/root/Subject/@修課必選修', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As subject_required
        , NULLIF(array_to_string(xpath('/root/Subject/@修課校部訂', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As subject_requiredby
         , NULLIF(array_to_string(xpath('/root/Subject/@不計學分', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As ns1
         , NULLIF(array_to_string(xpath('/root/Subject/@不需評分', xmlparse(content concat('<root>', subj_score_ele , '</root>'))), ''),'')::TEXT As ns2
	FROM 
	(
		SELECT 
			sems_subj_score.*
			, unnest(xpath('/root/SemesterSubjectScoreInfo/Subject', xmlparse(content concat('<root>', score_info , '</root>') ))) AS subj_score_ele
		FROM 
			sems_subj_score
	) AS sems_subj_score_ext
	INNER JOIN student_list
		ON sems_subj_score_ext.ref_student_id = student_list.student_id
	INNER JOIN student_sems
		ON sems_subj_score_ext.school_year = student_sems.sems_school_year::INT
		AND sems_subj_score_ext.ref_student_id = student_sems.student_id::INT
        AND sems_subj_score_ext.semester = student_sems.sems_semester::INT
		AND sems_subj_score_ext.grade_year = student_sems.sems_grade_year::INT   
)
,sems_entry_Score_data AS
(SELECT
		 student_list.student_id
        , sems_entry_score_ext.school_year
        , sems_entry_score_ext.semester
        ,sems_entry_score_ext,grade_year
		, student_list.student_name
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, student_list.rank_group_name
        , student_list.rank_group_code
		, '依學業原始'::TEXT As subject
		,nullif(score,'')::DECIMAL AS subject_origin_score
		,'1'::DECIMAL As subject_credit
		,''::TEXT As subject_entry
        ,''::TEXT As subject_required
        ,''::TEXT As subject_requiredby
        ,''::TEXT As ns1
        ,''::TEXT As ns2
	FROM 
        (SELECT sems_entry_score.*
	,array_to_string(xpath('//SemesterEntryScore/Entry[@分項=''學業(原始)'']/@成績', xmlparse(content score_info)), '')::TEXT AS score
	  FROM  sems_entry_score WHERE entry_group = 1 ) AS sems_entry_score_ext	   
	INNER JOIN student_list
		ON sems_entry_score_ext.ref_student_id = student_list.student_id	
  	INNER JOIN student_sems
		ON sems_entry_score_ext.school_year = student_sems.sems_school_year::INT
		AND sems_entry_score_ext.ref_student_id = student_sems.student_id::INT
        AND sems_entry_score_ext.semester = student_sems.sems_semester::INT
		AND sems_entry_score_ext.grade_year = student_sems.sems_grade_year::INT      
)
,subject_score AS
(
    SELECT * FROM subject_score_data WHERE ns1 = '否' AND ns2 ='否' 
    UNION ALL
    SELECT * FROM sems_entry_score_data
)
"; // 過濾不需評分與不計學分 

    // 多學期學業加權平均
    StringBuilder sbSubjectEntrySore = new StringBuilder();
    string semstr = "";
    List<String> SubjectSql = new List<string>();    
//依考試群組分群計算成績
    foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
        {
            if (RegGroupCalcMapDic[REGGroupName]["學業"][0] == "加權平均")
                semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score " + @"
    ,SUM(subject_credit) AS sum_credit ";
            else
                semstr = ",SUM(subject_origin_score) AS sum_score " + @"
    ,Count(subject_credit) AS sum_credit "; 
      foreach (GradeYearSemesterInfo gs in calSemesterList)
        {
            string ss = "a";
            if (gs.Semester == 2)
                ss = "b";
            int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
             SubjectSql.Add("subject_entry_score_avg_" + REGGroupName + gs.GradeYear + ss);
            string str = @",subject_entry_score_avg_" + REGGroupName + gs.GradeYear + ss + @" AS (
	SELECT
		student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name 
        ,student_list.rank_group_code " + semstr + @"
	FROM 
		subject_score
	INNER JOIN student_list
		ON subject_score.student_id = student_list.student_id
	WHERE 
		subject_score.grade_year = " + gs.GradeYear + @" AND subject_score.semester = " + gs.Semester + @" AND" + @"
        student_list.rank_group_name = '" + REGGroupName + @"' AND subject IN (" + RegGroupSubjMapDic[REGGroupName]["學業"][subjectNum] + @") " + @"
    GROUP BY 
	student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code

)
";
                        sbSubjectEntrySore.AppendLine(str);
                        }
                    }
                       
                string Strsubject_entry_score_avg_list = @"
,subject_entry_score_avg_list AS
(

SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,round(sum_score_s/sum_credit_s,parse_number) AS avg_score
    FROM
    (
SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,SUM(sum_score) AS sum_score_s
        ,SUM(sum_credit) AS sum_credit_s
        ,(select parse_number from parse_number)::INT AS parse_number
        FROM ( ";
foreach (string TableName in SubjectSql)
                {
                    Strsubject_entry_score_avg_list = Strsubject_entry_score_avg_list + @"
                    SELECT
                        student_id
                        ,student_name
                        ,rank_grade_year
                        ,rank_dept_name
                        ,rank_class_name
                        ,rank_tag1
                        ,rank_tag2
                        ,rank_group_name
                        ,rank_group_code
                        ,sum_score
                        ,sum_credit
                        FROM  " + TableName + " UNION ALL " ;
                }                
                Strsubject_entry_score_avg_list = Strsubject_entry_score_avg_list.Substring(0, Strsubject_entry_score_avg_list.Length-10) + @"
) as subject_entry_score_avg5
group BY
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
    ) AS subject_entry_score_avg_round
)
,subject_entry_score_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'學業'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_entry_score_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_entry_score_avg_expand AS
(
    SELECT
        subject_entry_score_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_entry_score_rank_list
)

";

                    // 多學期專業及實習加權平均
                StringBuilder sbSubjectProSore = new StringBuilder();
                SubjectSql.Clear();
                //依考試群組分群計算成績
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    if (RegGroupCalcMapDic[REGGroupName]["專業"][0] == "加權平均")
                        semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score " + @"
    ,SUM(subject_credit) AS sum_credit ";
                    else
                        semstr = ",SUM(subject_origin_score) AS sum_score " + @"
    ,Count(subject_credit) AS sum_credit ";
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        string ss = "a";
                        if (gs.Semester == 2)
                            ss = "b";
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        SubjectSql.Add("subject_score_pro_avg_" + REGGroupName + gs.GradeYear + ss);
                        string str = @",subject_score_pro_avg_" + REGGroupName + gs.GradeYear + ss + @" AS (
               
	SELECT
		student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code " + semstr + @"
	FROM 
		subject_score
	INNER JOIN student_list
		ON subject_score.student_id = student_list.student_id
	WHERE 
        subject_score.grade_year = " + gs.GradeYear + @" AND subject_score.semester = " + gs.Semester + @" AND" + @"
        student_list.rank_group_name = '" + REGGroupName + @"' AND subject IN (" + RegGroupSubjMapDic[REGGroupName]["專業"][subjectNum] + @") " + @"		
	GROUP BY 
	student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code
)
";
                        sbSubjectProSore.AppendLine(str);
                    }
                }
                    string Strsubject_score_pro_avg_list = @"
,subject_score_pro_avg_list AS
(

SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
       ,round(sum_score_s/sum_credit_s,parse_number) AS avg_score
    FROM
    (
SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,SUM(sum_score) AS sum_score_s
        ,SUM(sum_credit) AS sum_credit_s
        ,(select parse_number from parse_number)::INT AS parse_number
        FROM  ( ";
                foreach (string TableName in SubjectSql)
                {
                    Strsubject_score_pro_avg_list = Strsubject_score_pro_avg_list + @"
                    SELECT
                        student_id
                        , student_name
                        , rank_grade_year
                        , rank_dept_name
                        , rank_class_name
                        , rank_tag1
                        , rank_tag2
                        , rank_group_name
                        , rank_group_code
                        , sum_score
                        , sum_credit
                        FROM  " + TableName + " UNION ALL " ;
                }
                Strsubject_score_pro_avg_list = Strsubject_score_pro_avg_list.Substring(0, Strsubject_score_pro_avg_list.Length - 10) + @"
            ) as subject_score_pro_avg5
group BY
student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
    ) AS subject_score_pro_avg_round
)
,subject_score_pro_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'專業及實習'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_pro_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_pro_avg_expand AS
(
    SELECT
        subject_score_pro_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_pro_rank_list
)
";
                // 多學期技能
                StringBuilder sbSubjectSoreSkill = new StringBuilder();
                SubjectSql.Clear();
                //依考試群組分群計算成績
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    if (RegGroupCalcMapDic[REGGroupName]["技能"][0] == "加權平均")
                        semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score " + @"
    ,SUM(subject_credit) AS sum_credit ";
                    else
                        semstr = ",SUM(subject_origin_score) AS sum_score " + @"
    ,Count(subject_credit) AS sum_credit ";
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        string ss = "a";
                        if (gs.Semester == 2)
                            ss = "b";
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        SubjectSql.Add("subject_score_SkillDomain_" + REGGroupName + gs.GradeYear + ss);
                        string str = @",subject_score_SkillDomain_" + REGGroupName + gs.GradeYear + ss + @" AS (
               
	SELECT
		student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code " + semstr + @"
	FROM 
		subject_score
	INNER JOIN student_list
		ON subject_score.student_id = student_list.student_id
	WHERE 
        subject_score.grade_year = " + gs.GradeYear + @" AND subject_score.semester = " + gs.Semester + @" AND" + @"
        student_list.rank_group_name = '" + REGGroupName + @"' AND subject IN (" + RegGroupSubjMapDic[REGGroupName]["技能"][subjectNum] + @") " + @"		
	GROUP BY
	    student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code
)
";
                        sbSubjectSoreSkill.AppendLine(str);
                    }
                }
                    string Strsubject_score_SkillDomain_avg_list = @"
,subject_score_SkillDomain_avg_list AS
(

SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,round(sum_score_s/sum_credit_s,parse_number) AS avg_score
    FROM
    (
SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,SUM(sum_score) AS sum_score_s
        ,SUM(sum_credit) AS sum_credit_s
        ,(select parse_number from parse_number)::INT AS parse_number
       FROM  ( ";
                foreach (string TableName in SubjectSql)
                {
                    Strsubject_score_SkillDomain_avg_list = Strsubject_score_SkillDomain_avg_list + @"
                    SELECT
                        student_id
                        , student_name
                        , rank_grade_year
                        , rank_dept_name
                        , rank_class_name
                        , rank_tag1
                        , rank_tag2
                        , rank_group_name
                        , rank_group_code
                        , sum_score
                        , sum_credit
                        FROM  " + TableName + " UNION ALL ";
                }
                Strsubject_score_SkillDomain_avg_list = Strsubject_score_SkillDomain_avg_list.Substring(0, Strsubject_score_SkillDomain_avg_list.Length - 10) + @"
           ) as subject_SkillDomain_avg_avg5
group BY
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name  
        ,rank_group_code
    ) AS subject_score_SkillDomain_avg_round
)
,subject_score_SkillDomain_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'技能領域'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year,rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year,rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_SkillDomain_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_SkillDomain_avg_expand AS
(
    SELECT
        subject_score_SkillDomain_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_SkillDomain_rank_list
)
";

                // 處理國英數動態對照算成績
                // 多學期國文平均
                //依考試群組分群計算成績
                StringBuilder sbSubjectChineseScore = new StringBuilder();
                SubjectSql.Clear();
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    if (RegGroupCalcMapDic[REGGroupName]["國文"][0] == "加權平均")
                        semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score " + @"
    ,SUM(subject_credit) AS sum_credit ";
                    else
                        semstr = ",SUM(subject_origin_score) AS sum_score " + @"
    ,Count(subject_credit) AS sum_credit ";
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        string ss = "a";
                        if (gs.Semester == 2)
                            ss = "b";
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        SubjectSql.Add("subject_score_chinese_avg_" + REGGroupName + gs.GradeYear + ss);
                        string str = @",subject_score_chinese_avg_" + REGGroupName + gs.GradeYear + ss + @" AS (
               
	SELECT
		student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code " + semstr + @"
	FROM 
		subject_score
	INNER JOIN student_list
		ON subject_score.student_id = student_list.student_id
	WHERE 
        subject_score.grade_year = " + gs.GradeYear + @" AND subject_score.semester = " + gs.Semester + @" AND" + @"
        student_list.rank_group_name = '" + REGGroupName + @"' AND subject IN (" + RegGroupSubjMapDic[REGGroupName]["國文"][subjectNum] + @") " + @"		

    GROUP BY 
    student_list.student_id
        ,student_list.student_name
        ,student_list.rank_grade_year
        ,student_list.rank_dept_name
        ,student_list.rank_class_name
        ,student_list.rank_tag1
        ,student_list.rank_tag2
        ,student_list.rank_group_name
        ,student_list.rank_group_code
)
";
                            sbSubjectChineseScore.AppendLine(str);

                        }
                    }

                    string Strsubject_score_chinese_avg_list = @"
,subject_score_chinese_avg_list AS
(

SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,round(sum_score_s/sum_credit_s,parse_number) AS avg_score
    FROM
    (
SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,SUM(sum_score) AS sum_score_s
        ,SUM(sum_credit) AS sum_credit_s
        ,(select parse_number from parse_number)::INT AS parse_number
        FROM   ( ";
                foreach (string TableName in SubjectSql)
                {
                    Strsubject_score_chinese_avg_list = Strsubject_score_chinese_avg_list + @"
                    SELECT
                        student_id
                        , student_name
                        , rank_grade_year
                        , rank_dept_name
                        , rank_class_name
                        , rank_tag1
                        , rank_tag2
                        , rank_group_name
                        , rank_group_code
                        , sum_score
                        , sum_credit
                        FROM  " + TableName + " UNION ALL ";
                }
                Strsubject_score_chinese_avg_list = Strsubject_score_chinese_avg_list.Substring(0, Strsubject_score_chinese_avg_list.Length - 10) + @"
       
            ) as subject_score_chinese_avg5
group BY
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
    ) AS subject_score_chinese_avg_round
)
,subject_score_chinese_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'國文'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year,rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_chinese_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_chinese_avg_expand AS
(
    SELECT
        subject_score_chinese_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_chinese_rank_list
)

";
 
                // 多學期英文
                //依考試群組分群計算成績
                StringBuilder sbSubjectEnglishScore = new StringBuilder();
                SubjectSql.Clear();
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    if (RegGroupCalcMapDic[REGGroupName]["英文"][0] == "加權平均")
                        semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score " + @"
    ,SUM(subject_credit) AS sum_credit ";
                    else
                        semstr = ",SUM(subject_origin_score) AS sum_score " + @"
    ,Count(subject_credit) AS sum_credit ";
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        string ss = "a";
                        if (gs.Semester == 2)
                            ss = "b";
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        SubjectSql.Add("subject_score_english_avg_" + REGGroupName + gs.GradeYear + ss);
                        string str = @",subject_score_english_avg_" + REGGroupName + gs.GradeYear + ss + @" AS (
 
SELECT
        student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code " + semstr + @"
	FROM 
		subject_score
	INNER JOIN student_list
		ON subject_score.student_id = student_list.student_id
	WHERE 
        subject_score.grade_year = " + gs.GradeYear + @" AND subject_score.semester = " + gs.Semester + @" AND" + @"
        student_list.rank_group_name = '" + REGGroupName + @"' AND subject IN (" + RegGroupSubjMapDic[REGGroupName]["英文"][subjectNum] + @") " + @"	
    GROUP BY 
    student_list.student_id
        ,student_list.student_name
        ,student_list.rank_grade_year
        ,student_list.rank_dept_name
        ,student_list.rank_class_name
        ,student_list.rank_tag1
        ,student_list.rank_tag2
        ,student_list.rank_group_name
        ,student_list.rank_group_code
)
";
                            sbSubjectEnglishScore.AppendLine(str);

                        }
                    }

                    string Strsubject_score_english_avg_list = @"
,subject_score_english_avg_list AS
(

SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,round(sum_score_s/sum_credit_s,parse_number) AS avg_score
    FROM
    (
SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,SUM(sum_score) AS sum_score_s
        ,SUM(sum_credit) AS sum_credit_s
        ,(select parse_number from parse_number)::INT AS parse_number
        FROM  ( ";
                foreach (string TableName in SubjectSql)
                {
                    Strsubject_score_english_avg_list = Strsubject_score_english_avg_list + @"
                    SELECT
                        student_id
                        , student_name
                        , rank_grade_year
                        , rank_dept_name
                        , rank_class_name
                        , rank_tag1
                        , rank_tag2
                        , rank_group_name
                        ,rank_group_code
                        , sum_score
                        , sum_credit
                        FROM  " + TableName + " UNION ALL ";
                }
                Strsubject_score_english_avg_list = Strsubject_score_english_avg_list.Substring(0, Strsubject_score_english_avg_list.Length - 10) + @"
       
            ) as subject_score_english_avg5
group BY
student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
    ) AS subject_score_english_avg_round
)
,subject_score_english_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'英文'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code        
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_english_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_english_avg_expand AS
(
    SELECT
        subject_score_english_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_english_rank_list
)

";

                // 多學期數學
                //依考試群組分群計算成績
                StringBuilder sbSubjectMathScore = new StringBuilder();
                SubjectSql.Clear();
                foreach (string REGGroupName in RegGroupSubjMapDic.Keys)
                {
                    if (RegGroupCalcMapDic[REGGroupName]["數學"][0] == "加權平均")
                        semstr = ",SUM(subject_origin_score * subject_credit) AS sum_score " + @"
    ,SUM(subject_credit) AS sum_credit ";
                    else
                        semstr = ",SUM(subject_origin_score) AS sum_score " + @"
    ,Count(subject_credit) AS sum_credit ";
                    foreach (GradeYearSemesterInfo gs in calSemesterList)
                    {
                        string ss = "a";
                        if (gs.Semester == 2)
                            ss = "b";
                        int subjectNum = (gs.GradeYear - 1) * 2 + gs.Semester - 1;
                        SubjectSql.Add("subject_score_math_avg_" + REGGroupName + gs.GradeYear + ss);
                        string str = @",subject_score_math_avg_" + REGGroupName + gs.GradeYear + ss + @" AS (
 
SELECT
        student_list.student_id
		,student_list.student_name
		,student_list.rank_grade_year
		,student_list.rank_dept_name
		,student_list.rank_class_name
		,student_list.rank_tag1
		,student_list.rank_tag2
		,student_list.rank_group_name
        ,student_list.rank_group_code " + semstr + @"
	FROM 
		subject_score
	INNER JOIN student_list
		ON subject_score.student_id = student_list.student_id
	WHERE 
        subject_score.grade_year = " + gs.GradeYear + @" AND subject_score.semester = " + gs.Semester + @" AND" + @"
        student_list.rank_group_name = '" + REGGroupName + @"' AND subject IN (" + RegGroupSubjMapDic[REGGroupName]["數學"][subjectNum] + @") " + @"	
    GROUP BY 
    student_list.student_id
        ,student_list.student_name
        ,student_list.rank_grade_year
        ,student_list.rank_dept_name
        ,student_list.rank_class_name
        ,student_list.rank_tag1
        ,student_list.rank_tag2
        ,student_list.rank_group_name
        ,student_list.rank_group_code
)
";
                            sbSubjectMathScore.AppendLine(str);

                        }
                    }


                    string Strsubject_score_math_avg_list = @"
,subject_score_math_avg_list AS
(

SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,round(sum_score_s/sum_credit_s,parse_number) AS avg_score
    FROM
    (
SELECT
        student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        ,SUM(sum_score) AS sum_score_s
        ,SUM(sum_credit) AS sum_credit_s
        ,(select parse_number from parse_number)::INT AS parse_number
        FROM ( ";
                foreach (string TableName in SubjectSql)
                {
                    Strsubject_score_math_avg_list = Strsubject_score_math_avg_list + @"
                    SELECT
                        student_id
                        , student_name
                        , rank_grade_year
                        , rank_dept_name
                        , rank_class_name
                        , rank_tag1
                        , rank_tag2
                        , rank_group_name
                        , rank_group_code
                        , sum_score
                        , sum_credit
                        FROM  " + TableName + " UNION ALL ";
                }
                Strsubject_score_math_avg_list = Strsubject_score_math_avg_list.Substring(0, Strsubject_score_math_avg_list.Length - 10) + @"
  ) as subject_score_math_avg5
group BY
student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
    ) AS subject_score_math_avg_round
)
,subject_score_math_rank_list AS
(
    SELECT
         student_id
        ,student_name
        ,rank_grade_year
        ,rank_dept_name
        ,'數學'::TEXT AS item_name
        ,rank_class_name
        ,rank_tag1
        ,rank_tag2
        ,rank_group_name
        ,rank_group_code
        , avg_score
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score DESC) AS grade_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score DESC) AS dept_rank
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score DESC) AS class_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score DESC) AS tag1_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year ORDER BY avg_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY avg_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name ORDER BY avg_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY avg_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY avg_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_code ORDER BY avg_score ASC) AS group_rank_reverse
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
        , COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_code) AS group_count
    FROM
        subject_score_math_avg_list
    WHERE       
        avg_score IS NOT NULL
)
, subject_score_math_avg_expand AS
(
    SELECT
        subject_score_math_rank_list.*
        , FLOOR((grade_rank::DECIMAL - 1)*100::DECIMAL / grade_count) + 1 AS graderank_percentage
        , FLOOR((dept_rank::DECIMAL - 1)*100::DECIMAL / dept_count) + 1 AS deptrank_percentage
        , FLOOR((class_rank::DECIMAL - 1)*100::DECIMAL / class_count) + 1 AS classrank_percentage
        , FLOOR((tag1_rank::DECIMAL - 1)*100::DECIMAL / tag1_count) + 1 AS tag1rank_percentage
        , FLOOR((tag2_rank::DECIMAL - 1)*100::DECIMAL / tag2_count) + 1 AS tag2rank_percentage
        , FLOOR((group_rank::DECIMAL - 1)*100::DECIMAL / group_count) + 1 AS grouprank_percentage
        , FLOOR((grade_rank_reverse::DECIMAL-1)*100::DECIMAL/grade_count) AS graderank_pr
        , FLOOR((class_rank_reverse::DECIMAL-1)*100::DECIMAL/class_count) AS classrank_pr
        , FLOOR((dept_rank_reverse::DECIMAL-1)*100::DECIMAL/dept_count) AS deptrank_pr
        , FLOOR((tag1_rank_reverse::DECIMAL-1)*100::DECIMAL/tag1_count) AS tag1ank_pr
        , FLOOR((tag2_rank_reverse::DECIMAL-1)*100::DECIMAL/tag2_count) AS tag2rank_pr
        , FLOOR((group_rank_reverse::DECIMAL-1)*100::DECIMAL/group_count) AS grouprank_pr
    FROM
        subject_score_math_rank_list
)

";

                StringBuilder sbScoreListSQL = new StringBuilder();
                List<string> scNameList = new List<string>();
                scNameList.Add("subject_entry_score_avg_expand");  //學業
                scNameList.Add("subject_score_pro_avg_expand");    //專業/實習科目
                scNameList.Add("subject_score_SkillDomain_avg_expand");    //技能領域            
                scNameList.Add("subject_score_chinese_avg_expand");//國文
                scNameList.Add("subject_score_english_avg_expand");//英文
                scNameList.Add("subject_score_math_avg_expand");   //數學             

                sbScoreListSQL.AppendLine(", score_list AS(");
                int sbScoreItemNum = 1;
                foreach (string name in scNameList)
                    {
                        // 成績總表
                        string scoreListSQL = @"
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, '' || rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count		
		, student_id
		, avg_score AS score
		, grade_rank AS rank
		, graderank_percentage AS percentile
        , graderank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
     UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name::TEXT AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count		
		, student_id
		, avg_score AS score
		, dept_rank AS rank
		, deptrank_percentage AS percentile
        , deptrank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        ,rank_group_code
	FROM
		" + name + @"
UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name::TEXT AS rank_name
		, true AS is_alive
		, class_count AS matrix_count		
		, student_id
		, avg_score AS score
		, class_rank AS rank
		, classrank_percentage AS percentile
        , classrank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1::TEXT AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count		
		, student_id
		, avg_score AS score
		, tag1_rank AS rank
		, tag1rank_percentage AS percentile
        , tag1ank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

 UNION ALL
    SELECT
		-1 :: INT AS rank_school_year
		, -1 :: INT AS rank_semester
		, rank_grade_year
		, '5學期/技職繁星比序(111年學年度適用)'::TEXT AS item_type
		, -1 :: INT AS ref_exam_id
		, item_name
		, '學群排名'::TEXT AS rank_type
		, rank_group_code::TEXT AS rank_name
		, true AS is_alive
		, group_count AS matrix_count		
		, student_id
		, avg_score AS score
		, group_rank AS rank
		, grouprank_percentage AS percentile
        , grouprank_pr AS pr
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
        , rank_group_name
        , rank_group_code
	FROM
		" + name + @"
";
                        sbScoreListSQL.AppendLine(scoreListSQL);

                        if (sbScoreItemNum > 0 && sbScoreItemNum <6)  //6個比序
                            sbScoreListSQL.AppendLine(" UNION ALL");
                        sbScoreItemNum++;

                    }
                    sbScoreListSQL.AppendLine(")");
                    //sbScoreListSQL.AppendLine("select * from score_list");


                    //select * from score_list
                    //";






                    string insertUpdateSQL = @"
, update_data AS
(
	UPDATE
		rank_matrix
	SET
		is_alive = NULL	
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = -1
		AND rank_matrix.semester = -1
		AND rank_matrix.grade_year = 3		
		AND rank_matrix.item_type = '5學期/技職繁星比序(111年學年度適用)'

	RETURNING rank_matrix.*
), insert_batch_data AS
(
	INSERT INTO
		rank_batch
		(
			school_year
			, semester
			, calculation_description
			, setting
		)
		VALUES
        (" + K12.Data.School.DefaultSchoolYear + @"
            ," + K12.Data.School.DefaultSemester + @"
            ,'" + K12.Data.School.DefaultSchoolYear + "學年度 第" + K12.Data.School.DefaultSemester + @"學期 技職繁星比序(111年學年度適用)'
            ,'" + calculationSetting + @"'
        )

	RETURNING *
)
, insert_matrix_data AS
(
	INSERT INTO
		rank_matrix
		(
			ref_batch_id
			, school_year
			, semester
			, grade_year
			, item_type
			, ref_exam_id
			, item_name
			, rank_type
			, rank_name
			, is_alive
			, matrix_count			
		)
		SELECT
			insert_batch_data.id AS ref_batch_id
			, score_list.rank_school_year::INT
			, score_list.rank_semester::INT
			, score_list.rank_grade_year::INT
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count			
		FROM
			score_list
			LEFT OUTER JOIN update_data
				ON update_data.id  < 0 --永遠為false，只是為了讓insert等待update執行完
			CROSS JOIN insert_batch_data
		GROUP BY
			insert_batch_data.id
			, score_list.rank_school_year
			, score_list.rank_semester
			, score_list.rank_grade_year
			, score_list.item_type
			, score_list.ref_exam_id
			, score_list.item_name
			, score_list.rank_type
			, score_list.rank_name
			, score_list.is_alive
			, score_list.matrix_count			

	RETURNING *
)
, insert_batch_student_data AS
(
	INSERT INTO
		rank_batch_student
		(
			ref_batch_id
			, ref_student_id
			, grade_year
			, matrix_grade
			, matrix_class
            , matrix_dept
			, matrix_tag1
			, matrix_tag2
		)
		SELECT
			insert_batch_data.id AS ref_batch_id
			, score_list.student_id
			, score_list.rank_grade_year::INT
			, score_list.rank_grade_year||'年級' AS matrix_grade
			, score_list.rank_class_name
            , score_list.rank_dept_name
			, score_list.rank_tag1 
            , score_list.rank_group_code
		FROM
			score_list
			CROSS JOIN insert_batch_data
)
, insert_detail_data AS
(
	INSERT INTO
		rank_detail
		(
			ref_matrix_id
			, ref_student_id
			, score
			, rank
            ,pr
			, percentile
		)
		SELECT
			insert_matrix_data.id AS ref_matrix_id
			, score_list.student_id AS ref_student_id
			, score_list.score AS score
			, score_list.rank AS rank
            , score_list.pr AS pr
			, score_list.percentile AS percentile
		FROM
			score_list
			LEFT OUTER JOIN
				insert_matrix_data
					ON insert_matrix_data.school_year = score_list.rank_school_year::INT
					AND insert_matrix_data.semester = score_list.rank_semester::INT
					AND insert_matrix_data.grade_year = score_list.rank_grade_year::INT
					AND insert_matrix_data.item_type = score_list.item_type
					AND insert_matrix_data.ref_exam_id = score_list.ref_exam_id
					AND insert_matrix_data.item_name = score_list.item_name
					AND insert_matrix_data.rank_type = score_list.rank_type
					AND insert_matrix_data.rank_name = score_list.rank_name
)
SELECT * from score_list limit 1
	

                ";

                insertRankSql += sbSubjectEntrySore.ToString() + Strsubject_entry_score_avg_list   //學業成績             
                + sbSubjectProSore.ToString() + Strsubject_score_pro_avg_list +//專業成績   
                sbSubjectSoreSkill.ToString()+ Strsubject_score_SkillDomain_avg_list + //技能領域
                 sbSubjectChineseScore.ToString() + Strsubject_score_chinese_avg_list//國文成績               
                +sbSubjectEnglishScore.ToString() + Strsubject_score_english_avg_list +//英文成績                
               sbSubjectMathScore.ToString() + Strsubject_score_math_avg_list +//數學成績
                
               sbScoreListSQL.ToString() + insertUpdateSQL;


                ////debug
                //string fiPath = Application.StartupPath + @"\sql1.sql";
                //using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                //{
                //    fi.WriteLine(insertRankSql);
                //}

                #endregion


                bkw.ReportProgress(50);

                    QueryHelper queryHelper = new QueryHelper();
                try
                {                    
                     queryHelper.Select(insertRankSql);
                }
                catch
                {
                    MsgBox.Show("計算失敗");
                }
                // Error: Use of unassigned local variable 'n'.
               // Console.Write(n);
                


                bkw.ReportProgress(100);
            };

            bkw.RunWorkerCompleted += delegate
            {
                pbLoading.Visible = false;
                if (bkwException != null)
                {
                    btnCacluate.Enabled = true;
                    btnPrevious.Enabled = true;
                    throw new Exception("計算排名失敗", bkwException);                    
                }

                MessageBox.Show("計算完成");
                FISCA.Presentation.MotherForm.SetStatusBarMessage("");
                //MotherForm.SetStatusBarMessage("排名計算完成");
                //pbLoading.Visible = false;
                btnCacluate.Enabled = true;
                btnPrevious.Enabled = true;
                this.DialogResult = DialogResult.OK;
            };

            bkw.RunWorkerAsync();
        }

        private void CalculateTechnologyAssessmentRankStep2_Resize(object sender, EventArgs e)
        {
            pbLoading.Location = new Point(this.Width / 2 - 20, this.Height / 2 - 20);
        }

        private void CalculateTechnologyAssessmentRankStep2_FormClosing(object sender, FormClosingEventArgs e)
        {
            //this.DialogResult = DialogResult.Abort;
        }

        private void btnExportNoRank_Click(object sender, EventArgs e)
        {
            if (StudentNotRankDict.Count > 0)
            {
                try
                {
                    btnExportNoRank.Enabled = false;


                    Workbook wb = new Workbook();
                    wb.Worksheets[0].Name = "不排名學生";
                    wb.Worksheets[0].Cells[0, 0].PutValue("學號");
                    wb.Worksheets[0].Cells[0, 1].PutValue("班級");
                    wb.Worksheets[0].Cells[0, 2].PutValue("座號");
                    wb.Worksheets[0].Cells[0, 3].PutValue("姓名");
                    wb.Worksheets[0].Cells[0, 4].PutValue("不排名原因");
                    int rIdx = 1;
                    foreach (StudentRecord studRec in StudentAllList)
                    {
                        if (StudentNotRankDict.ContainsKey(studRec.ID))
                        {
                            wb.Worksheets[0].Cells[rIdx, 0].PutValue(studRec.StudentNumber);
                            if (studRec.Class != null)
                                wb.Worksheets[0].Cells[rIdx, 1].PutValue(studRec.Class.Name);
                            if (studRec.SeatNo.HasValue)
                                wb.Worksheets[0].Cells[rIdx, 2].PutValue(studRec.SeatNo.Value);
                            wb.Worksheets[0].Cells[rIdx, 3].PutValue(studRec.Name);
                            wb.Worksheets[0].Cells[rIdx, 4].PutValue(StudentNotRankDict[studRec.ID]);
                            rIdx++;
                        }
                    }

                    if (wb != null)
                    {
                        #region 儲存檔案
                        string reportName = "技職繁星不排名學生";

                        string path = Path.Combine(System.Windows.Forms.Application.StartupPath, "Reports");
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        path = Path.Combine(path, reportName + ".xls");

                        try
                        {


                            if (File.Exists(path))
                            {
                                int i = 1;
                                while (true)
                                {
                                    string newPath = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + (i++) + Path.GetExtension(path);
                                    if (!File.Exists(newPath))
                                    {
                                        path = newPath;
                                        break;
                                    }
                                }
                            }
                            wb.Save(path, SaveFormat.Excel97To2003);
                            System.Diagnostics.Process.Start(path);
                        }
                        catch
                        {
                            System.Windows.Forms.SaveFileDialog sd = new System.Windows.Forms.SaveFileDialog();
                            sd.Title = "另存新檔";
                            sd.FileName = reportName + ".xls";
                            sd.Filter = "Excel檔案 (*.xls)|*.xls|所有檔案 (*.*)|*.*";
                            if (sd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                try
                                {
                                    wb.Save(sd.FileName, SaveFormat.Excel97To2003);

                                }
                                catch
                                {
                                    FISCA.Presentation.Controls.MsgBox.Show("指定路徑無法存取。", "建立檔案失敗", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        MsgBox.Show("Excel 檔案無法產生。");
                    }
                }
                catch (Exception ex)
                {
                    MsgBox.Show("產生不排名學生錯誤," + ex.Message);
                    btnExportNoRank.Enabled = true;
                }


                btnExportNoRank.Enabled = true;
            }
        }
    }
}
