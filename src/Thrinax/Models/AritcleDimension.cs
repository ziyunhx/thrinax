using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrinax.Models
{
    public class AritcleDimension
    {
        /// <summary>
        /// 出现div元素的次数
        /// </summary>
        public int DivNumber { set; get; }
        /// <summary>
        /// 出现非空div元素的次数
        /// </summary>
        public int NotNullDivNumber { set; get; }
        /// <summary>
        /// 出现A元素的次数
        /// </summary>
        public int ANumber { set; get; }
        /// <summary>
        /// 清洗后正文长度
        /// </summary>
        public int CleanTextLength { set; get; }
        /// <summary>
        /// 出现P元素的次数
        /// </summary>
        public int PNumber { set; get; }
        /// <summary>
        /// P元素中正文长度占总正文的占比
        /// </summary>
        public double PTextLenPerInAll { set; get; }
        /// <summary>
        /// A元素中正文长度占总正文的占比
        /// </summary>
        public double ATextLenPerInAll { set; get; }
        /// <summary>
        /// 正文中连续空行大于等于3行的个数，超过3行的每个数量会加1
        /// </summary>
        public int MoreThan3SpaceLineCount { set; get; }
        /// <summary>
        /// 包含排除词的次数
        /// </summary>
        public int IncludeIgnoreWords { set; get; }
    }
}
