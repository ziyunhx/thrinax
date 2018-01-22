using System;
using HtmlAgilityPack;
using Thrinax.Utility;

namespace Thrinax
{
    /// <summary>
    /// 各类网站模式识别的全部参数及相关方法
    /// </summary>
    public class ListStrategy
    {
        /// <summary>
        /// 本策略适用的MediaType
        /// </summary>
        public readonly Enums.MediaType MediaType;

        /// <summary>
        /// 本策略适用的Language
        /// </summary>
        public readonly Enums.Language Language;

        /// <summary>
        /// List页最小匹配链接数量
        /// </summary>
        public readonly int List_MinCountItem = 3;

        /// <summary>
        /// 最短标题长度
        /// </summary>
        public readonly int MinLenTitle = 5;

        /// <summary>
        /// 标题最短单词数
        /// </summary>
        public readonly int MinWordCountTitle = 5;

        /// <summary>
        /// 标题中最多的数字字符占比
        /// </summary>
        public readonly double MaxRateTitleDigits = 0.5;

        public readonly int MaxLenDate = 25;

        public readonly int MaxLenView = 12;

        public readonly int MaxLenMedia = 25;

        public readonly int MaxLenAuthor = 25;

        /// <summary>
        /// 最佳单页文章数量
        /// </summary>
        public readonly int List_BestItemCount;

        /// <summary>
        /// 列表页最小平均标题长度
        /// </summary>
        public readonly double List_MinAvgTitleLen;

        /// <summary>
        /// 列表页最小平均标题长度
        /// </summary>
        public readonly double List_BestAvgTitleLen;

        /// <summary>
        /// 列表页最小平均媒体名称长度
        /// </summary>
        public readonly double List_BestAvgMediaLen;

        /// <summary>
        /// 列表页最小平均作者名长度
        /// </summary>
        public readonly double List_BestAvgAuthorLen;

        /// <summary>
        /// 忽略xpath路径上数字序号的层级数初始值
        /// </summary>
        public readonly int List_IgnoreOrderNumber_UpLevel_Init;

        /// <summary>
        /// 忽略xpath路径上数字序号的层级数最大值
        /// </summary>
        public readonly int List_IgnoreOrderNumber_UpLevel_Max = 8;

        /// <summary>
        /// 合并相似的兄弟Pattern时，沿XPath向上追溯几级
        /// </summary>
        public readonly int List_CombinSiblingPattern_MaxUpLevel;

        public readonly FieldScoreStrategy FieldScore;

        public ListStrategy(Enums.MediaType MediaType, Enums.Language Language)
        {
            this.MediaType = MediaType;
            this.Language = Language;

            switch (MediaType)
            {
                default:
                case Enums.MediaType.WebNews:
                    List_IgnoreOrderNumber_UpLevel_Init = 1;
                    List_BestItemCount = 20;
                    List_MinAvgTitleLen = Language == Enums.Language.CHINESE ? 4.9 : 10;
                    List_BestAvgTitleLen = Language == Enums.Language.CHINESE ? 18 : 70;
                    List_BestAvgMediaLen = Language == Enums.Language.CHINESE ? 4 : 12;
                    List_BestAvgAuthorLen = Language == Enums.Language.CHINESE ? 3 : 15;
                    List_CombinSiblingPattern_MaxUpLevel = 2;
                    break;
                case Enums.MediaType.Forum:
                    List_IgnoreOrderNumber_UpLevel_Init = 1;
                    List_BestItemCount = 40;
                    List_MinAvgTitleLen = Language == Enums.Language.CHINESE ? 7 : 12;
                    List_BestAvgTitleLen = Language == Enums.Language.CHINESE ? 16 : 60;
                    List_BestAvgMediaLen = Language == Enums.Language.CHINESE ? 4 : 12;
                    List_BestAvgAuthorLen = Language == Enums.Language.CHINESE ? 6 : 17;
                    List_CombinSiblingPattern_MaxUpLevel = 2;
                    break;
                case Enums.MediaType.FrontPage:
                    List_IgnoreOrderNumber_UpLevel_Init = 1;
                    List_BestItemCount = 10;
                    List_MinAvgTitleLen = Language == Enums.Language.CHINESE ? 7 : 12;
                    List_BestAvgTitleLen = Language == Enums.Language.CHINESE ? 12 : 70;
                    List_BestAvgMediaLen = Language == Enums.Language.CHINESE ? 4 : 12;
                    List_BestAvgAuthorLen = Language == Enums.Language.CHINESE ? 4 : 17;
                    List_CombinSiblingPattern_MaxUpLevel = 2;
                    break;
            }

            this.FieldScore = new FieldScoreStrategy(MediaType, Language);
        }

        /// <summary>
        /// 验证标题是否合法
        /// </summary>
        /// <param name="Title"></param>
        /// <returns></returns>
        public bool ValidateTitle(string Title)
        {
            if (string.IsNullOrWhiteSpace(Title)) return false;
            string CleanTitle = TextCleaner.FullClean(Title);

            switch (Language)
            {
                default:
                case Enums.Language.CHINESE:
                    //中文：标题长度够长，且数字字符占比不超
                    return ((MinLenTitle <= 0 || CleanTitle.Length >= MinLenTitle)
                        && (MaxRateTitleDigits >= 1 || CleanTitle.Length * MaxRateTitleDigits > TextCleaner.CountDigitChars(CleanTitle)));

                case Enums.Language.ENGLISH:
                    //英文：标题单词够多，且数字字符占比不超
                    return (MinWordCountTitle <= 0 || CleanTitle.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > MinWordCountTitle
                        && (MaxRateTitleDigits >= 1 || CleanTitle.Length * MaxRateTitleDigits > TextCleaner.CountDigitChars(CleanTitle)));
            }
        }

        /// <summary>
        /// 验证标题是否合法
        /// </summary>
        /// <param name="Title"></param>
        /// <returns></returns>
        public bool ValidateTitle(HtmlNode Title)
        {
            return ValidateTitle(XPathUtility.InnerTextNonDescendants(Title)) && !(Title.Attributes["href"] == null) && HTMLCleaner.isUrlGood(Title.Attributes["href"].Value);
        }

        ///// <summary>
        ///// 验证一个List的模式是否能应用于某一个页面（只是检查是否明显不可能）
        ///// </summary>
        ///// <param name="Url"></param>
        ///// <param name="HTML"></param>
        ///// <param name="XPath"></param>
        ///// <param name="MediaType"></param>
        ///// <param name="Language"></param>
        ///// <returns></returns>
        //public bool ValidateListXPath(string Url, string HTML, ListPagePattern listPagePattern)
        //{
        //    //获取root节点（有些网站页面不带html标签的，直接从head开始写）
        //    HtmlNode rootNode = HtmlUtility.getSafeHtmlRootNode(HTML);
        //    if (rootNode == null)
        //        return false;

        //    return ValidateListXPath(Url, rootNode, listPagePattern);
        //}

        ///// <summary>
        ///// 验证一个List的模式是否能应用于某一个页面（只是检查是否明显不可能）
        ///// </summary>
        ///// <param name="Url"></param>
        ///// <param name="RootNode"></param>
        ///// <param name="XPath"></param>
        ///// <param name="MediaType"></param>
        ///// <param name="Language"></param>
        ///// <returns></returns>
        //public bool ValidateListXPath(string Url, HtmlNode RootNode, ListPagePattern listPagePattern)
        //{
        //    if (string.IsNullOrEmpty(Url) || RootNode == null || listPagePattern == null) return false;

        //    Article[] Content = ExtractContent(Url, RootNode, listPagePattern.Path);
        //    if (Content == null || Content.Length < 3) return false;

        //    int TitleCount = 0, DateCount = 0, ViewCount = 0, ReplyCount = 0, MediaCount = 0, AuthorCount = 0;
        //    foreach (Article ele in Content)
        //    {
        //        if (!string.IsNullOrEmpty(ele.Title) && !string.IsNullOrEmpty(ele.Url)) TitleCount++;
        //        if (!string.IsNullOrEmpty(listPagePattern.Path.DateXPath) && ele.Pubdate != null) DateCount++;
        //        if (!string.IsNullOrEmpty(listPagePattern.Path.ViewXPath) && ele.View >= 0) ViewCount++;
        //        if (!string.IsNullOrEmpty(listPagePattern.Path.ReplyXPath) && ele.Reply >= 0) ReplyCount++;
        //        if (!string.IsNullOrEmpty(listPagePattern.Path.MediaNameXPath) && !string.IsNullOrEmpty(ele.MediaName)) MediaCount++;
        //        if (!string.IsNullOrEmpty(listPagePattern.Path.AuthorXPath) && !string.IsNullOrEmpty(ele.Author)) AuthorCount++;
        //    }

        //    if (TitleCount < Content.Length * 0.9) return false;
        //    if (!string.IsNullOrEmpty(listPagePattern.Path.DateXPath) && DateCount < Content.Length * 0.9) return false;
        //    if (!string.IsNullOrEmpty(listPagePattern.Path.ViewXPath) && ViewCount < Content.Length * 0.9) return false;
        //    if (!string.IsNullOrEmpty(listPagePattern.Path.ReplyXPath) && ReplyCount < Content.Length * 0.9) return false;
        //    if (!string.IsNullOrEmpty(listPagePattern.Path.MediaNameXPath) && MediaCount < Content.Length * 0.9) return false;
        //    if (!string.IsNullOrEmpty(listPagePattern.Path.AuthorXPath) && AuthorCount < Content.Length * 0.9) return false;

        //    return true;
        //}

        public override string ToString()
        {
            return "MediaType:" + MediaType;
        }
    }
}
