using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FISCA.Data;
using System.Data;
using FISCA.Presentation.Controls;

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
    }
}
