using System;
using System.Collections.Generic;
using Thrinax.Enums;
using Thrinax.Models;

namespace Thrinax.Utility.Smart
{
    /// <summary>
    /// List页各字段的XPath
    /// </summary>
    public class ListPagePattern
    {
        public XpathPattern Path { get; set; }

        public Dictionary<PatternType, List<string>> BackUpPaths { get; set; }

        /// <summary>
        /// 总得分
        /// </summary>
        public double? TotalScore { get; set; }
        /// <summary>
        /// 标题得分
        /// </summary>
        public double? TitleScore { set; get; }
        /// <summary>
        /// 发布时间得分
        /// </summary>
        public double? DataTimeScore { set; get; }
        /// <summary>
        /// 媒体得分
        /// </summary>
        public double? MediaScore { set; get; }
        /// <summary>
        /// 作者得分
        /// </summary>
        public double? AuthorScore { set; get; }
        /// <summary>
        /// 查看数得分
        /// </summary>
        public double? ViewScore { set; get; }
        /// <summary>
        /// 回复数得分
        /// </summary>
        public double? ReplyScore { set; get; }
        /// <summary>
        /// 摘要得分
        /// </summary>
        public double? AbstractScore { set; get; }

        /// <summary>
        /// 提取的各字段内容数组
        /// </summary>
        public Article[] Contents { get; set; }

        public Dictionary<PatternType, Dictionary<string, string>> RelString { get; set; }

        public ListPagePattern()
        {
            this.Path = new XpathPattern();
            this.BackUpPaths = new Dictionary<PatternType, List<string>>();

        }

        public override string ToString()
        {
            return string.Format("Base: {0} Title: {1} Count: {2} Score: {3}", Path.ItemRootXPath, Path.TitleXPath, Contents?.Length ?? 0, TotalScore);
        }
    }
}
