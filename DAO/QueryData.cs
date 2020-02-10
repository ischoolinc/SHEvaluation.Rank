using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FISCA.Data;
using System.Data;
using FISCA.Presentation.Controls;
using K12.Data;

namespace SHEvaluation.Rank.DAO
{
    public class QueryData
    {
        /// <summary>
        /// 取得科別
        /// </summary>
        /// <returns></returns>
        public DataTable GetDeptInfoDT()
        {
            try
            {
                QueryHelper qh = new QueryHelper();
                string qry = "SELECT id,name FROM dept ORDER BY code,name;";
                DataTable dt = qh.Select(qry);
                return dt;
            }
            catch (Exception ex)
            {
                MsgBox.Show("取得系統科別失敗，" + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 取得傳入學生ID 的成績年級與學期
        /// </summary>
        /// <param name="GradeYear"></param>
        /// <returns></returns>
        public Dictionary<string, List<GradeYearSemesterInfo>> GetScoreGradeSemesterByGradeYear(List<string> StudentIDList)
        {
            Dictionary<string, List<GradeYearSemesterInfo>> value = new Dictionary<string, List<GradeYearSemesterInfo>>();
            try
            {
                if (StudentIDList.Count > 0)
                {
                    QueryHelper qh = new QueryHelper();
                    string qry = "" +
                       "WITH grade_semester AS( " +
    "SELECT student.id AS student_id,sems_subj_score.school_year,sems_subj_score.semester,sems_subj_score.grade_year FROM student INNER JOIN class ON student.ref_class_id = class.id LEFT JOIN sems_subj_score ON sems_subj_score.ref_student_id = student.id WHERE student.id IN(" + string.Join(",", StudentIDList.ToArray()) + ") " +
    "UNION ALL " +
    "SELECT student.id AS student_id," + K12.Data.School.DefaultSchoolYear + " AS school_year," + K12.Data.School.DefaultSemester + " AS semester,class.grade_year FROM student INNER JOIN class ON student.ref_class_id = class.id WHERE student.id IN(" + string.Join(",", StudentIDList.ToArray()) + ")) " +
    "SELECT student_id,max(school_year) AS school_year,semester,grade_year FROM grade_semester WHERE grade_year IS NOT NULL  GROUP BY student_id,semester,grade_year ORDER BY student_id,grade_year,semester " +
                        "";
                    DataTable dt = qh.Select(qry);

                    foreach (DataRow dr in dt.Rows)
                    {
                        string sid = dr["student_id"].ToString();

                        if (!value.ContainsKey(sid))
                            value.Add(sid, new List<GradeYearSemesterInfo>());

                        int gr, sc, ss;
                        int.TryParse(dr["grade_year"].ToString(), out gr);
                        int.TryParse(dr["semester"].ToString(), out ss);
                        int.TryParse(dr["school_year"].ToString(), out sc);

                        GradeYearSemesterInfo gs = new GradeYearSemesterInfo();
                        gs.GradeYear = gr;
                        gs.Semester = ss;
                        gs.SchoolYear = sc;

                        value[sid].Add(gs);

                    }
                }
            }
            catch (Exception ex)
            {
                MsgBox.Show("取得學生成績年級、學期 失敗，" + ex.Message);
                return value;
            }

            return value;
        }

    }
}
