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


namespace SHEvaluation.Rank
{
    public partial class CalculateTechnologyAssessmentRankStep2 : BaseForm
    {

        List<StudentRecord> StudentFilterList = new List<StudentRecord>();
        List<string> RankStudentIDList = new List<string>();
        List<StudentTagRecord> Tag1Student = new List<StudentTagRecord>();
        List<StudentTagRecord> Tag2Student = new List<StudentTagRecord>();
        Dictionary<string, List<GradeYearSemesterInfo>> StudGradeYearSemsDict = new Dictionary<string, List<GradeYearSemesterInfo>>();

        string SelStudentTag1 = "";
        string SelStudentTag2 = "";
        string SelStudentFilter = "";

        int parseNumber = 0;

        // 學生群別
        Dictionary<string, udtRegistrationDept> StudentGroupDict = new Dictionary<string, udtRegistrationDept>();

        // 群別
        Dictionary<string, udtRegistrationDept> RegistrationDeptDict = new Dictionary<string, udtRegistrationDept>();

        // 科目
        Dictionary<string, udtRegistrationSubject> RegistrationSubjectDict = new Dictionary<string, udtRegistrationSubject>();

        public CalculateTechnologyAssessmentRankStep2()
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
        public void SetRegistrationSubject(Dictionary<string, udtRegistrationSubject> data)
        {
            RegistrationSubjectDict = data;
        }

        public void SetSelStudentTag(string tag1, string tag2, string flt)
        {
            SelStudentTag1 = tag1;
            SelStudentTag2 = tag2;
            SelStudentFilter = flt;
        }

        public void SetStudentDataList(List<StudentRecord> StudList, List<StudentTagRecord> tagList1, List<StudentTagRecord> tagList2, Dictionary<string, string> deptDict)
        {
            StudentFilterList = StudList;
            Tag1Student = tagList1;
            Tag2Student = tagList2;

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
                    row.Cells[colDeptName.Index].Value = deptDict[student.studentID];
                    if (RegistrationDeptDict.ContainsKey(deptDict[student.studentID]))
                    {
                        row.Cells[colRegGroup.Index].Value = RegistrationDeptDict[deptDict[student.studentID]].RegGroupName;

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

            }

            dgvStudentList.Rows.AddRange(rowList.ToArray());
            #endregion

            lblMsg.Text = "共 " + dgvStudentList.Rows.Count + " 位學生";

            btnPrevious.Enabled = true;
            btnCacluate.Enabled = true;
            FISCA.Presentation.MotherForm.SetStatusBarMessage("學生列表讀取完成");
        }

        private void CalculateTechnologyAssessmentRankStep2_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void btnCacluate_Click(object sender, EventArgs e)
        {





            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;


            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                FISCA.Presentation.MotherForm.SetStatusBarMessage("計算排名中", eventArgs.ProgressPercentage);
            };

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
    ";
                    #endregion
                    //把單筆學生資料的SQL加入到List
                    studentSqlList.Add(studentSql);
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


                string calculationSetting = "";

                #region 產生計算規則的SQL
                #region 產生要儲存到rank_batch的setting的Xml
                XmlDocument xdoc = new XmlDocument();
                var settingEle = xdoc.CreateElement("Setting");
                settingEle.SetAttribute("考試名稱", "學期成績");
                settingEle.SetAttribute("不排名學生類別", SelStudentFilter);
                settingEle.SetAttribute("類別一", SelStudentTag1);
                settingEle.SetAttribute("類別二", SelStudentTag2);
                settingEle.SetAttribute("年級", "3");

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
,entry_score_list AS
(
	SELECT		
		student_list.student_id
		, student_list.student_name
        , student_list.rank_grade_year
        , student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, student_list.rank_group_name		
		, array_to_string(xpath('/root/Entry/@分項', xmlparse(content  concat('<root>', entry_score_ele , '</root>'))),'')::TEXT As entry
		, AVG(NULLIF(array_to_string(xpath('/root/Entry/@成績', xmlparse(content   concat('<root>', entry_score_ele , '</root>'))), ''),'')::DECIMAL) AS entry_score
        ,(select parse_number from parse_number)::INT AS parse_number
	FROM
	(
		SELECT
			sems_entry_score.*
			, unnest(xpath('/root/SemesterEntryScore/Entry', xmlparse(content  concat('<root>', sems_entry_score.score_info , '</root>') ))) As entry_score_ele
		FROM
		sems_entry_score
	) AS sems_entry_score_ext
	INNER JOIN student_list
		ON sems_entry_score_ext.ref_student_id = student_list.student_id
	INNER JOIN student_sems
		ON sems_entry_score_ext.school_year = student_sems.sems_school_year::INT
		AND sems_entry_score_ext.ref_student_id = student_sems.student_id::INT
        AND sems_entry_score_ext.semester = student_sems.sems_semester::INT
		AND sems_entry_score_ext.grade_year = student_sems.sems_grade_year::INT 
GROUP BY student_list.student_id
        , student_list.student_name
        , student_list.rank_grade_year
        , student_list.rank_dept_name
        , student_list.rank_class_name
        , student_list.rank_tag1
        , student_list.rank_tag2
        , student_list.rank_group_name
        , entry
)
,entry_score_avg_list AS (
        SELECT 
            student_id
            , student_name
            , rank_grade_year
            , rank_dept_name
            , rank_class_name
            , rank_tag1
            , rank_tag2
            , rank_group_name
            , entry
            , ROUND(entry_score,parse_number) AS entry_score 
        FROM entry_score_list WHERE entry = '學業(原始)'
) 
,entry_rank_list AS
(
	SELECT
		 rank_grade_year
		, rank_dept_name
		, rank_class_name		
		, entry::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
        ,rank_group_name
		, entry_score
		, RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score DESC) AS tag2_rank
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_name, entry ORDER BY entry_score DESC) AS group_rank
        , RANK() OVER(PARTITION BY rank_grade_year, entry ORDER BY entry_score ASC) AS grade_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, entry ORDER BY entry_score ASC) AS dept_rank_reverse
        , RANK() OVER(PARTITION BY rank_class_name, entry ORDER BY entry_score ASC) AS class_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, entry ORDER BY entry_score ASC) AS tag1_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, entry ORDER BY entry_score ASC) AS tag2_rank_reverse
        , RANK() OVER(PARTITION BY rank_grade_year, rank_group_name, entry ORDER BY entry_score ASC) AS group_rank_reverse
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, entry) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, entry) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, entry) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, entry) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, entry) AS tag2_count
        , COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_group_name, entry) AS group_count
	FROM
		entry_score_avg_list
	WHERE
		entry IS NOT NULL
		AND entry_score IS NOT NULL
), entry_rank_expand AS
(
	SELECT
		entry_rank_list.*
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
		entry_rank_list
)

";

                // debug 
                string fiPath = Application.StartupPath + @"\sql1.sql";
                using (System.IO.StreamWriter fi = new System.IO.StreamWriter(fiPath))
                {
                    fi.WriteLine(insertRankSql);
                }

                #endregion






                bkw.ReportProgress(100);
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (bkwException != null)
                {
                    btnCacluate.Enabled = true;
                    btnPrevious.Enabled = true;
                    throw new Exception("計算排名失敗", bkwException);
                }

                MessageBox.Show("計算完成");
                //MotherForm.SetStatusBarMessage("排名計算完成");
                //pbLoading.Visible = false;
                btnCacluate.Enabled = true;
                btnPrevious.Enabled = true;
            };

            bkw.RunWorkerAsync();
        }
    }
}
