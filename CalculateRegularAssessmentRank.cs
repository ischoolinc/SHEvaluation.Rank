using FISCA.Data;
using FISCA.Presentation;
using FISCA.Presentation.Controls;
using K12.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace SHEvaluation.Rank
{
    public partial class CalculateRegularAssessmentRank : BaseForm
    {
        string _DefaultSchoolYear = "";
        string _DefaultSemester = "";
        List<ExamRecord> _ExamList = new List<ExamRecord>();
        List<TagConfigRecord> _TagList = new List<TagConfigRecord>();
        List<StudentRecord> _StudentList = new List<StudentRecord>();
        List<int> _GradeYearList = new List<int>();

        public CalculateRegularAssessmentRank()
        {
            InitializeComponent();

            #region 讀取及篩選所需資料
            _DefaultSchoolYear = K12.Data.School.DefaultSchoolYear;
            _DefaultSemester = K12.Data.School.DefaultSemester;
            _ExamList = K12.Data.Exam.SelectAll();
            _TagList = K12.Data.TagConfig.SelectByCategory(TagCategory.Student);
            _StudentList = K12.Data.Student.SelectAll().Where(x => x.Status == StudentRecord.StudentStatus.一般
                                                                && !string.IsNullOrEmpty(x.RefClassID)
                                                                && x.Class.GradeYear != null).ToList();
            #endregion
        }

        private void CalculateRegularAssessmentRank_Load(object sender, EventArgs e)
        {
            plStudentView.Visible = false;

            #region 填資料進ComboBox
            cboSchoolYear.Items.Clear();
            cboSemester.Items.Clear();
            cboExamName.Items.Clear();
            cboStudentFilter.Items.Clear();
            cboStudentTag1.Items.Clear();
            cboStudentTag2.Items.Clear();

            cboSchoolYear.Items.Add(_DefaultSchoolYear);//加入預設的學年度
            cboSemester.Items.Add(_DefaultSemester);//加入預設的學年度

            cboSchoolYear.SelectedIndex = 0;
            if (cboSemester.Items.Contains(_DefaultSemester))
            {
                cboSemester.SelectedIndex = cboSemester.Items.IndexOf(_DefaultSemester);
            }
            else
            {
                cboSemester.SelectedIndex = 0;
            }

            foreach (string exam in _ExamList.Select(x => x.Name).Distinct())
            {
                cboExamName.Items.Add(exam);
            }
            cboExamName.SelectedIndex = 0;

            cboStudentFilter.Items.Add("");
            cboStudentTag1.Items.Add("");
            cboStudentTag2.Items.Add("");
            foreach (string tagName in _TagList.Select(x => x.Prefix).Distinct())
            {
                cboStudentFilter.Items.Add("[" + tagName + "]");
                cboStudentTag1.Items.Add("[" + tagName + "]");
                cboStudentTag2.Items.Add("[" + tagName + "]");
            }
            cboStudentFilter.SelectedIndex = 0;
            cboStudentTag1.SelectedIndex = 0;
            cboStudentTag2.SelectedIndex = 0;
            #endregion

            #region 將年級資料填入ListView
            foreach (int gradeYear in _StudentList.Select(x => x.Class.GradeYear).Distinct().OrderBy(x => x).ToList())
            {
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = gradeYear + "年級";
                listViewItem.Checked = true;
                listGradeYear.Items.Add(listViewItem);
            }
            #endregion
        }

        private void cboStudentFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentFilter.Text == cboStudentTag1.Text || cboStudentFilter.Text == cboStudentTag2.Text)
            {
                cboStudentFilter.SelectedIndex = 0;
            }
        }

        private void cboStudentTag1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag1.Text == cboStudentFilter.Text || cboStudentTag1.Text == cboStudentTag2.Text)
            {
                cboStudentTag1.SelectedIndex = 0;
            }
            if (cboStudentTag1.Text == "")
            {
                cboStudentTag2.SelectedIndex = 0;
                cboStudentTag2.Enabled = false;
            }
            else
            {
                cboStudentTag2.Enabled = true;
            }
        }

        private void cboStudentTag2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboStudentTag2.Text == cboStudentFilter.Text || cboStudentTag2.Text == cboStudentTag1.Text)
            {
                cboStudentTag2.SelectedIndex = 0;
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            plStudentView.Visible = true;

            btnPrevious.Enabled = false;
            btnCalculate.Enabled = false;

            lbSchoolYear.Text = cboSchoolYear.Text;
            lbSemester.Text = cboSemester.Text;
            lbExam.Text = cboExamName.Text;

            string studentfilter = cboStudentFilter.Text.Trim('[', ']');
            string studenttag1 = cboStudentTag1.Text.Trim('[', ']');
            string studenttag2 = cboStudentTag2.Text.Trim('[', ']');

            _GradeYearList = new List<int>();
            foreach (ListViewItem listViewItem in listGradeYear.Items)
            {
                if (listViewItem.Checked == true)
                {
                    int gradeYear = Convert.ToInt32(listViewItem.Text.Trim('年', '級'));
                    _GradeYearList.Add(gradeYear);
                }
            }

            #region 讀取學生清單
            List<StudentRecord> filterStudentList = new List<StudentRecord>();
            DataTable deptDataTable = new DataTable();
            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;

            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                MotherForm.SetStatusBarMessage("讀取學生清單", eventArgs.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    #region 依據篩選條件篩選出學生清單
                    bkw.ReportProgress(0);

                    foreach (int gradeYear in _GradeYearList)
                    {
                        filterStudentList.AddRange(_StudentList.Where(x => x.Class.GradeYear == gradeYear).ToList());
                    }

                    bkw.ReportProgress(35);

                    if (!string.IsNullOrEmpty(studentfilter))
                    {
                        List<string> filterTagIDs = _TagList.Where(x => x.Prefix == studentfilter).Select(x => x.ID).ToList();
                        List<string> studentIDList = K12.Data.StudentTag.SelectAll().Where(x => filterTagIDs.Contains(x.RefTagID)).Select(x => x.RefStudentID).ToList();
                        filterStudentList = filterStudentList.Where(x => !studentIDList.Contains(x.ID)).ToList();
                    }

                    bkw.ReportProgress(70);
                    #endregion

                    #region 取得篩選學生的科別
                    string studentIDs = string.Join(",", filterStudentList.Select(x => x.ID).ToList());

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

                    QueryHelper queryHelper = new QueryHelper();
                    deptDataTable = queryHelper.Select(queryDeptSQL);

                    bkw.ReportProgress(100);
                    #endregion

                }
                catch (Exception ex)
                {
                    bkwException = ex;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (bkwException != null)
                {
                    throw new Exception("學生清單讀取失敗", bkwException);
                }

                if (filterStudentList.Count() == 0)
                {
                    MessageBox.Show("沒有找到符合條件的學生");
                    btnPrevious.Enabled = true;
                    return;
                }

                #region 整理學生基本資料
                var studentViewList = (from student in filterStudentList
                                       select new
                                       {
                                           studentID = student.ID,
                                           studentClass = student.Class.Name,
                                           studentSeatNo = student.SeatNo,
                                           studentNumber = student.StudentNumber,
                                           studentName = student.Name,
                                           RankGradeYear = student.Class.GradeYear + "年級",
                                           RankClassName = student.Class.Name
                                       }).ToList();
                #endregion

                #region 整理學生類別
                List<StudentTagRecord> tag1StudentList = new List<StudentTagRecord>();
                List<StudentTagRecord> tag2StudentList = new List<StudentTagRecord>();
                if (!string.IsNullOrEmpty(studenttag1))
                {
                    List<string> tag1IDs = _TagList.Where(x => x.Prefix == studenttag1).Select(x => x.ID).ToList();
                    tag1StudentList = K12.Data.StudentTag.SelectAll().Where(x => tag1IDs.Contains(x.RefTagID)).ToList();
                }
                if (!string.IsNullOrEmpty(studenttag2))
                {
                    List<string> tag2IDs = _TagList.Where(x => x.Prefix == studenttag2).Select(x => x.ID).ToList();
                    tag2StudentList = K12.Data.StudentTag.SelectAll().Where(x => tag2IDs.Contains(x.RefTagID)).ToList();
                }
                #endregion

                #region 將資料填入dataGridView
                List<DataGridViewRow> rowList = new List<DataGridViewRow>();
                foreach (var student in studentViewList)
                {
                    string tag1 = "", tag2 = "";
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(dgvStudentList);

                    row.Tag = student.studentID;
                    row.Cells[0].Value = "" + student.studentClass;
                    row.Cells[1].Value = "" + student.studentSeatNo;
                    row.Cells[2].Value = "" + student.studentNumber;
                    row.Cells[3].Value = "" + student.studentName;
                    row.Cells[4].Value = "" + student.RankGradeYear;
                    row.Cells[6].Value = "" + student.RankClassName;
                    DataRow[] dataRow = deptDataTable.Select("student_id = '" + student.studentID + "'");
                    row.Cells[5].Value = "" + dataRow.First()["deptname"];
                    if (tag1StudentList.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                    {
                        tag1 = tag1StudentList.First(x => x.RefStudentID == student.studentID).Name;
                        row.Cells[7].Value = tag1;
                    }
                    if (tag2StudentList.Where(x => x.RefStudentID == student.studentID).Count() > 0)
                    {
                        tag2 = tag2StudentList.First(x => x.RefStudentID == student.studentID).Name;
                        row.Cells[8].Value = tag2;
                    }

                    rowList.Add(row);
                }

                dgvStudentList.Rows.AddRange(rowList.ToArray());
                #endregion

                btnPrevious.Enabled = true;
                btnCalculate.Enabled = true;
                MotherForm.SetStatusBarMessage("學生清單讀取完成");
            };

            bkw.RunWorkerAsync();
            #endregion
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            plStudentView.Visible = false;
            btnCalculate.Enabled = true;
            if (dgvStudentList.Rows.Count > 0)
            {
                dgvStudentList.Rows.Clear();
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            #region 產生學生清單的SQL
            List<string> studentSQLList = new List<string>();
            foreach (DataGridViewRow row in dgvStudentList.Rows)
            {
                #region 將單筆學生SQL加入到List
                studentSQLList.Add(@"    
    SELECT
        '" + row.Tag + @"'::BIGINT AS student_id
        ,'" + row.Cells[3].Value + @"'::TEXT AS student_name
        ,'" + ("" + row.Cells[4].Value).Trim('年', '級') + @"'::INT AS rank_grade_year
        ,'" + "" + row.Cells[5].Value + @"'::TEXT AS rank_dept_name
        ,'" + "" + row.Cells[6].Value + @"'::TEXT AS rank_class_name
        ,'" + "" + row.Cells[7].Value + @"'::TEXT AS rank_tag1
        ,'" + "" + row.Cells[8].Value + @"'::TEXT AS rank_tag2
    ");
                #endregion
            }

            #region 所有學生資料的SQL
            string studentListSQL = @"
WITH student_list AS
(
    " + string.Join(@"
    UNION ALL", studentSQLList) + @"
)
";
            #endregion 
            #endregion

            btnCalculate.Enabled = false;
            btnPrevious.Enabled = false;
            string schoolYear = lbSchoolYear.Text;
            string semester = lbSemester.Text;
            string examName = lbExam.Text;
            string examId = _ExamList.First(x => x.Name == examName).ID;
            string studentFilter = cboStudentFilter.Text.Trim('[', ']');
            string studentTag1 = cboStudentTag1.Text.Trim('[', ']');
            string studentTag2 = cboStudentTag2.Text.Trim('[', ']');
            string calculateSetting = "";

            #region 產生計算規則的SQL
            #region 產生要儲存到rank_batch的setting的Xml
            XmlDocument doc = new XmlDocument();
            var settingEle = doc.CreateElement("Setting");
            settingEle.SetAttribute("學年度", "" + schoolYear);
            settingEle.SetAttribute("學期", "" + semester);
            settingEle.SetAttribute("考試名稱", "" + examName);
            settingEle.SetAttribute("不排名學生類別", "" + studentFilter);
            settingEle.SetAttribute("類別一", "" + studentTag1);
            settingEle.SetAttribute("類別二", "" + studentTag2);
            foreach (int gradeYear in _GradeYearList)
            {
                var gradeYearEle = doc.CreateElement("年級");
                gradeYearEle.InnerText = "" + gradeYear;
                settingEle.AppendChild(gradeYearEle);
            }
            calculateSetting = settingEle.OuterXml;
            #endregion

            List<string> calcSettingSQLList = new List<string>();
            foreach (int gradeYear in _GradeYearList)
            {
                #region 將單筆計算規則的SQL加到List
                calcSettingSQLList.Add(@"
	SELECT
		'" + gradeYear + @"'::TEXT  AS rank_grade_year
		, '" + schoolYear + @"'::TEXT AS rank_school_year
		, '" + semester + @"'::TEXT AS rank_semester
		, '" + examName + @"'::TEXT AS rank_exam_name
        , '" + calculateSetting + @"'::TEXT AS calculation_setting
    ");
                #endregion
            }

            #region 計算規則的SQL
            string calculationSetting = @"
, calc_condition AS
(
    " + string.Join(@"
    UNION ALL", calcSettingSQLList) + @"
)
";
            #endregion 
            #endregion

            BackgroundWorker bkw = new BackgroundWorker();
            bkw.WorkerReportsProgress = true;
            Exception bkwException = null;
            pbLoading.Visible = true;

            bkw.ProgressChanged += delegate (object obj, ProgressChangedEventArgs eventArgs)
            {
                MotherForm.SetStatusBarMessage("計算排名中", eventArgs.ProgressPercentage);
            };

            bkw.DoWork += delegate
            {
                try
                {
                    bkw.ReportProgress(1);

                    #region 計算排名SQL
                    string insertRankSQL = @"
" + studentListSQL + @"
" + calculationSetting + @"
, subject_rank_score AS
(
	SELECT
		student_list.student_id
		, student_list.rank_grade_year
		, student_list.rank_dept_name
		, student_list.rank_class_name
		, student_list.rank_tag1
		, student_list.rank_tag2
		, course.school_year AS rank_school_year
		, course.semester AS rank_semester
		, course.score_type
		, course.subject
		, course.credit
		, exam.id AS ref_exam_id
		, exam.exam_name
		, sce_take.score
	FROM
		sce_take
	LEFT JOIN sc_attend
		ON sce_take.ref_sc_attend_id = sc_attend.id
	LEFT JOIn exam
		ON sce_take.ref_exam_id = exam.id
	LEFT JOIN course
		ON sc_attend.ref_course_id = course.id
	INNER JOIN student_list
		ON sc_attend.ref_student_id = student_list.student_id
	INNER JOIN calc_condition
		ON calc_condition.rank_grade_year::INT = student_list.rank_grade_year
		AND calc_condition.rank_school_year::INT = course.school_year
		AND calc_condition.rank_semester::INT = course.semester
		AND calc_condition.rank_exam_name = exam.exam_name
)
, weighted_rank_score AS
(
	SELECT
		student_id
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
		, rank_school_year
		, rank_semester
		, ref_exam_id
		, exam_name
		, SUM
		(
			score::DECIMAL * credit::DECIMAL
		)
		/
		SUM
		(
			CASE
				WHEN credit = 0
				THEN 1
				ELSE credit::DECIMAL
			END
		) AS weighted_score
	FROM
		subject_rank_score
	WHERE
		score IS NOT NULL
	GROUP BY
		rank_school_year
		, rank_semester
		, ref_exam_id
		, exam_name
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
		, student_id
)
, subject_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, '定期評量/科目成績'::TEXT AS item_type
		, ref_exam_id
		, subject AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, score
		, RANK() OVER(PARTITION BY rank_grade_year, subject ORDER BY score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name, subject ORDER BY score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name, subject ORDER BY score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1, subject ORDER BY score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2, subject ORDER BY score DESC) AS tag2_rank
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, subject) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name, subject) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name, subject) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1, subject) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2, subject) AS tag2_count
	FROM
		subject_rank_score
	WHERE
		subject IS NOT NULL
		AND subject <> ''
)
, subject_rank_expand AS
(
	SELECT
		subject_rank.*
		, FLOOR((grade_rank::DECIMAL - 1) * 100 / grade_count) + 1 AS grade_rank_percentage
		, FLOOR((dept_rank::DECIMAL - 1) * 100 / dept_count) + 1 AS dept_rank_percentage
		, FLOOR((class_rank::DECIMAL - 1) * 100 / class_count) + 1 AS class_rank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1) * 100 / tag1_count) + 1 AS tag1_rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1) * 100 / tag2_count) + 1 AS tag2_rank_percentage
	FROM
		subject_rank
)
, weighted_rank AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, rank_dept_name
		, rank_class_name
		, '定期評量/總計成績'::TEXT AS item_type
		, ref_exam_id
		, '加權平均'::TEXT AS item_name
		, student_id
		, rank_tag1
		, rank_tag2
		, weighted_score AS score
		, RANK() OVER(PARTITION BY rank_grade_year ORDER BY weighted_score DESC) AS grade_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_dept_name ORDER BY weighted_score DESC) AS dept_rank
		, RANK() OVER(PARTITION BY rank_class_name ORDER BY weighted_score DESC) AS class_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag1 ORDER BY weighted_score DESC) AS tag1_rank
		, RANK() OVER(PARTITION BY rank_grade_year, rank_tag2 ORDER BY weighted_score DESC) AS tag2_rank
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year) AS grade_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_dept_name) AS dept_count
		, COUNT(student_id) OVER(PARTITION BY rank_class_name) AS class_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag1) AS tag1_count
		, COUNT(student_id) OVER(PARTITION BY rank_grade_year, rank_tag2) AS tag2_count
	FROM
		weighted_rank_score
)
, weighted_rank_expand AS
(
	SELECT
		weighted_rank.*
		, FLOOR((grade_rank::DECIMAL - 1) * 100 / grade_count) + 1 AS grade_rank_percentage
		, FLOOR((dept_rank::DECIMAL - 1) * 100 / dept_count) + 1 AS dept_rank_percentage
		, FLOOR((class_rank::DECIMAL - 1) * 100 / class_count) + 1 AS class_rank_percentage
		, FLOOR((tag1_rank::DECIMAL - 1) * 100 / tag1_count) + 1 AS tag1_rank_percentage
		, FLOOR((tag2_rank::DECIMAL - 1) * 100 / tag2_count) + 1 AS tag2_rank_percentage
	FROM
		weighted_rank
)
, score_list AS
(
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, ''|| rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, grade_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank <= TRUNC(dept_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank <= TRUNC(dept_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank >= TRUNC(dept_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank >= TRUNC(dept_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10
		, student_id
		, score
		, dept_rank AS rank
		, dept_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank_expand
	WHERE
		rank_dept_name IS NOT NULL
		AND rank_dept_name <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10
		, student_id
		, score
		, class_rank AS rank
		, class_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		subject_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '年排名'::TEXT AS rank_type
		, ''|| rank_grade_year || '年級'::TEXT AS rank_name
		, true AS is_alive
		, grade_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.25)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank <= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.5)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE grade_rank >= TRUNC(grade_count * 0.75)) OVER(PARTITION BY rank_grade_year, item_name) AS avg_bottom_75
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, item_name) AS level_lt10
		, student_id
		, score
		, grade_rank AS rank
		, grade_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weighted_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '科排名'::TEXT AS rank_type
		, rank_dept_name AS rank_name
		, true AS is_alive
		, dept_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank <= TRUNC(dept_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank <= TRUNC(dept_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank >= TRUNC(dept_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE dept_rank >= TRUNC(dept_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_dept_name, item_name) AS level_lt10
		, student_id
		, score
		, dept_rank AS rank
		, dept_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weighted_rank_expand
	WHERE
		rank_dept_name IS NOT NULL
		AND rank_dept_name <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '班排名'::TEXT AS rank_type
		, rank_class_name AS rank_name
		, true AS is_alive
		, class_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE class_rank <= TRUNC(class_count * 0.25)) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE class_rank <= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE class_rank >= TRUNC(class_count * 0.5)) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE class_rank >= TRUNC(class_count * 0.75)) OVER(PARTITION BY rank_class_name, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_class_name, item_name) AS level_lt10
		, student_id
		, score
		, class_rank AS rank
		, class_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weighted_rank_expand

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '類別1排名'::TEXT AS rank_type
		, rank_tag1 AS rank_name
		, true AS is_alive
		, tag1_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank <= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE tag1_rank >= TRUNC(tag1_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag1, item_name) AS level_lt10
		, student_id
		, score
		, tag1_rank AS rank
		, tag1_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weighted_rank_expand
	WHERE
		rank_tag1 IS NOT NULL
		AND rank_tag1 <> ''

	UNION ALL
	SELECT
		rank_school_year
		, rank_semester
		, rank_grade_year
		, item_type
		, ref_exam_id
		, item_name
		, '類別2排名'::TEXT AS rank_type
		, rank_tag2 AS rank_name
		, true AS is_alive
		, tag2_count AS matrix_count
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.25)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_25
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank <= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_top_50
		, AVG(score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.5)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_50
		, AVG(score::DECIMAL) FILTER(WHERE tag2_rank >= TRUNC(tag2_count * 0.75)) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS avg_bottom_25
		, COUNT(*) FILTER(WHERE 100::DECIMAL <= score::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_gte100
		, COUNT(*) FILTER(WHERE 90::DECIMAL <= score::DECIMAL AND score < 100::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_90
		, COUNT(*) FILTER(WHERE 80::DECIMAL <= score::DECIMAL AND score < 90::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_80
		, COUNT(*) FILTER(WHERE 70::DECIMAL <= score::DECIMAL AND score < 80::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_70
		, COUNT(*) FILTER(WHERE 60::DECIMAL <= score::DECIMAL AND score < 70::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_60
		, COUNT(*) FILTER(WHERE 50::DECIMAL <= score::DECIMAL AND score < 60::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_50
		, COUNT(*) FILTER(WHERE 40::DECIMAL <= score::DECIMAL AND score < 50::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_40
		, COUNT(*) FILTER(WHERE 30::DECIMAL <= score::DECIMAL AND score < 40::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_30
		, COUNT(*) FILTER(WHERE 20::DECIMAL <= score::DECIMAL AND score < 30::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_20
		, COUNT(*) FILTER(WHERE 10::DECIMAL <= score::DECIMAL AND score < 20::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_10
		, COUNT(*) FILTER(WHERE score::DECIMAL < 10::DECIMAL) OVER(PARTITION BY rank_grade_year, rank_tag2, item_name) AS level_lt10
		, student_id
		, score
		, tag2_rank AS rank
		, tag2_rank_percentage AS percentile
		, rank_dept_name
		, rank_class_name
		, rank_tag1
		, rank_tag2
	FROM
		weighted_rank_expand
	WHERE
		rank_tag2 IS NOT NULL
		AND rank_tag2 <> ''
)
, update_data AS
(
	UPDATE
		rank_matrix
	SET
		is_alive = null
	FROM
		score_list
	WHERE
		rank_matrix.is_alive = true
		AND rank_matrix.school_year = score_list.rank_school_year
		AND rank_matrix.semester = score_list.rank_semester
		AND rank_matrix.grade_year = score_list.rank_grade_year
		AND rank_matrix.ref_exam_id = score_list.ref_exam_id

	RETURNING rank_matrix.*
)
, insert_batch AS
(
	INSERT INTO
		rank_batch
		(
			school_year
			, semester
			, calculation_description
			, setting
		)
		SELECT
			DISTINCT
			rank_school_year::INT
			, rank_semester::INT
			, rank_school_year || ' ' || rank_semester || ' 計算' || rank_exam_name || '排名'::TEXT AS calculation_description
			, calculation_setting
		FROM
			calc_condition

	RETURNING *	
)
, insert_matrix AS
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
			, avg_top_25
			, avg_top_50
			, avg
			, avg_bottom_50
			, avg_bottom_25
			, level_gte100
			, level_90
			, level_80
			, level_70
			, level_60
			, level_50
			, level_40
			, level_30
			, level_20
			, level_10
			, level_lt10			
		)
		SELECT
			insert_batch.id AS ref_batch_id
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
			, score_list.avg_top_25
			, score_list.avg_top_50
			, score_list.avg
			, score_list.avg_bottom_50
			, score_list.avg_bottom_25
			, score_list.level_gte100
			, score_list.level_90
			, score_list.level_80
			, score_list.level_70
			, score_list.level_60
			, score_list.level_50
			, score_list.level_40
			, score_list.level_30
			, score_list.level_20
			, score_list.level_10
			, score_list.level_lt10
		FROM
			score_list
			LEFT OUTER JOIN update_data
				ON update_data.id < 0
			CROSS JOIN insert_batch
		GROUP BY
			insert_batch.id
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
			, score_list.avg_top_25
			, score_list.avg_top_50
			, score_list.avg
			, score_list.avg_bottom_50
			, score_list.avg_bottom_25
			, score_list.level_gte100
			, score_list.level_90
			, score_list.level_80
			, score_list.level_70
			, score_list.level_60
			, score_list.level_50
			, score_list.level_40
			, score_list.level_30
			, score_list.level_20
			, score_list.level_10
			, score_list.level_lt10

	RETURNING *			
)
, insert_batch_student AS
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
			insert_batch.id AS ref_batch_id
			, score_list.student_id
			, score_list.rank_grade_year
			, score_list.rank_grade_year || '年級' AS matrix_grade
			, score_list.rank_class_name
			, score_list.rank_dept_name
			, score_list.rank_tag1
			, score_list.rank_tag2
		FROM
			score_list
			CROSS JOIN insert_batch
)
, insert_detail AS
(
	INSERT INTO
		rank_detail
		(
			ref_matrix_id
			, ref_student_id
			, score
			, rank
			, percentile
		)
		SELECT
			insert_matrix.id AS ref_matrix_id
			, score_list.student_id
			, score_list.score
			, score_list.rank
			, score_list.percentile
		FROM
			score_list
			LEFT OUTER JOIN insert_matrix
				ON Insert_matrix.school_year = score_list.rank_school_year
				AND insert_matrix.semester = score_list.rank_semester
				AND insert_matrix.grade_year = score_list.rank_grade_year
				AND insert_matrix.item_type = score_list.item_type
				AND insert_matrix.ref_exam_id = score_list.ref_exam_id
				AND insert_matrix.item_name = score_list.item_name
				AND insert_matrix.rank_type = score_list.rank_type
				AND insert_matrix.rank_name = score_list.rank_name
)
SELECT
	score_list.rank_school_year
	, score_list.rank_semester
	, score_list.rank_grade_year
	, score_list.item_type
	, score_list.ref_exam_id
	, score_list.item_name
	, score_list.rank_type
	, score_list.rank_name
	, score_list.student_id
FROM 
	score_list
	LEFT OUTER JOIN insert_matrix
		ON insert_matrix.school_year = score_list.rank_school_year
		AND insert_matrix.semester = score_list.rank_semester
		AND insert_matrix.grade_year = score_list.rank_grade_year
		AND insert_matrix.item_type = score_list.item_type
		AND insert_matrix.ref_exam_id = score_list.ref_exam_id
		AND insert_matrix.item_name = score_list.item_name
		AND insert_matrix.rank_type = score_list.rank_type
		AND insert_matrix.rank_name = score_list.rank_name
";
                    #endregion

                    bkw.ReportProgress(50);

                    QueryHelper queryHelper = new QueryHelper();
                    queryHelper.Select(insertRankSQL);

                    bkw.ReportProgress(100);
                }
                catch (Exception ex)
                {
                    bkwException = ex;
                }
            };

            bkw.RunWorkerCompleted += delegate
            {
                if (bkwException != null)
                {
                    btnCalculate.Enabled = true;
                    btnPrevious.Enabled = true;
                    throw new Exception("計算排名失敗", bkwException);
                }

                MessageBox.Show("計算完成");
                pbLoading.Visible = false;
                btnCalculate.Enabled = true;
                btnPrevious.Enabled = true;
            };

            bkw.RunWorkerAsync();
        }
    }
}
