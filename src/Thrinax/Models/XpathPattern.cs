using System;
namespace Thrinax
{
    public class XpathPattern
    {
        #region 列表页部分
        /// <summary>
        /// list中每个Item的根节点（正常人类都用li或tr）
        /// </summary>
        public string ItemRootXPath { get; set; }
        /// <summary>
        /// 识别Url的XPath
        /// </summary>
        public string UrlXPath { get; set; }
        /// <summary>
        /// 识别标题的XPath
        /// </summary>
        public string TitleXPath { get; set; }
        /// <summary>
        /// 识别内容摘要的XPath
        /// </summary>
        public string AbsTractXPath { get; set; }
        /// <summary>
        /// 识别点击量的XPath
        /// </summary>
        public string ViewXPath { get; set; }
        /// <summary>
        /// 识别回复量的XPath
        /// </summary>
        public string ReplyXPath { get; set; }
        /// <summary>
        /// 识别作者的XPath
        /// </summary>
        public string AuthorXPath { get; set; }
        /// <summary>
        /// 识别媒体来源的XPath
        /// </summary>
        public string MediaNameXPath { get; set; }
        /// <summary>
        /// 识别发布日期的XPath
        /// </summary>
        public string DateXPath { get; set; }

        /// <summary>
        /// 列表上一页的Xpath
        /// </summary>
        public string ListLastPage { get; set; }
        /// <summary>
        /// 列表下一页的Xpath
        /// </summary>
        public string ListNextPage { get; set; }
        #endregion

        #region 文章页部分
        /// <summary>
        /// 文章页发布时间的Xpath
        /// </summary>
        public string ItemDateXPath { get; set; }
        /// <summary>
        /// 文章页媒体名称的Xpath
        /// </summary>
        public string ItemMediaNameXPath { get; set; }
        /// <summary>
        /// 文章页阅读数的Xpath
        /// </summary>
        public string ItemViewXPath { get; set; }
        /// <summary>
        /// 文章页回复数的Xpath
        /// </summary>
        public string ItemReplyXPath { get; set; }
        /// <summary>
        /// 文章页标题的Xpath
        /// </summary>
        public string ItemTitleXPath { get; set; }
        /// <summary>
        /// 文章页内容的Xpath
        /// </summary>
        public string ItemContentXPath { get; set; }
        /// <summary>
        /// 文章页作者的Xpath
        /// </summary>
        public string ItemAuthorXPath { get; set; }
        /// <summary>
        /// 文章上一页的Xpath
        /// </summary>
        public string ItemLastPaper { get; set; }
        /// <summary>
        /// 文章下一页的Xpath
        /// </summary>
        public string ItemNextPaper { get; set; }
        #endregion

        /// <summary>
        /// Html是否需要经过验证
        /// </summary>
        public bool HtmlNeedValidation { get; set; }
    }
}
