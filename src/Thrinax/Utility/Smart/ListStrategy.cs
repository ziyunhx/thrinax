using System;
using HtmlAgilityPack;
using Thrinax.Utility;

namespace Thrinax.Utility.Smart
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

        public override string ToString()
        {
            return "MediaType:" + MediaType;
        }
    }
}
