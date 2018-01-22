using System;
using System.Collections.Generic;

namespace Thrinax.Models
{
    public class ItemXPathPattern
    {
        /// <summary>
        /// 识别标题的XPath
        /// </summary>
        public List<string> TitleXPath { get; set; }
        /// <summary>
        /// 识别内容的XPath
        /// </summary>
        public List<string> ContentXPath { get; set; }
        /// <summary>
        /// 识别点击量的XPath
        /// </summary>
        public List<string> ViewXPath { get; set; }
        /// <summary>
        /// 识别回复量的XPath
        /// </summary>
        public List<string> ReplyXPath { get; set; }
        /// <summary>
        /// 识别作者的XPath
        /// </summary>
        public List<string> AuthorXPath { get; set; }
        /// <summary>
        /// 识别媒体来源的XPath
        /// </summary>
        public List<string> MediaNameXPath { get; set; }
        /// <summary>
        /// 识别发布日期的XPath
        /// </summary>
        public List<string> PubDateXPath { get; set; }
        /// <summary>
        /// 识别下一页的XPath(暂时未用)
        /// </summary>
        public List<string> NextPageXPath { get; set; }
        /// <summary>
        /// 识别跟帖发布日期的XPath
        /// </summary>
        public List<string> SubItemPubDateXPath { get; set; }
        /// <summary>
        /// 识别跟帖作者的XPath
        /// </summary>
        public List<string> SubItemAuthorXPath { get; set; }
        /// <summary>
        /// 识别跟帖内容的XPath
        /// </summary>
        public List<string> SubItemContentXPath { get; set; }

        public override string ToString()
        {
            return string.Format("Title: {0} Content: {1}", TitleXPath, ContentXPath);
        }
    }
}
