using System.Collections.Generic;
using System.Linq;
using System.Text;
using FISCA.UDT;

namespace SHEvaluation.Rank.UDT
{
    [TableName("campus.technology_star.registration_Calc")]
    public class udtRegistrationCalc : ActiveRecord
    {
        /// <summary>
        /// 報名群名稱
        /// </summary>
        [Field(Field = "reg_group_name", Indexed = false)]
        public string RegGroupName { get; set; }
        /// <summary>
        /// 比序項目
        /// </summary>
        [Field(Field = "Calc_name", Indexed = false)]
        public string CalcName { get; set; }
        /// <summary>
        /// 計算內容科目或項目
        /// </summary>
        [Field(Field = "reg_Calc_Item", Indexed = false)]
        public string RegCalcItem { get; set; }
        /// <summary>
        /// 計算方式
        /// </summary>
        [Field(Field = "reg_Calc_Kind", Indexed = false)]
        public string RegCalcKind { get; set; }
    }
}
