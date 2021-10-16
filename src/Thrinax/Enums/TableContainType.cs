using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrinax.Enums
{
    /// <summary>
    /// 表格引入方式
    /// </summary>
    public enum TableContainType
    {
        /// <summary>
        /// 使用CSV格式
        /// </summary>
        CSV = 1,
        /// <summary>
        /// 使用Json格式
        /// </summary>
        Json = 2,
        /// <summary>
        /// 使用TSV格式
        /// </summary>
        TSV = 3,
        /// <summary>
        /// 原始格式
        /// </summary>
        None = 4
    }
}