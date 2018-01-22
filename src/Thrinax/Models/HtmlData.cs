using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Runtime.Serialization;
using System.Drawing;
using Thrinax.Utility;

namespace Thrinax.Models
{
    internal class HtmlCommonPart
    {
        #region public属性

        public string SurfixCommonXPath { get; set; }
        public string PrefixCommonXPath { get; set; }
        public int ItemCount { get; set; }

        #endregion
    }


    internal class HtmlDatePart
    {
        #region public属性

        public double Power { get; set; }
        public string Text { get; set; }
        public HtmlNode Node { get; set; }

        #endregion
    }

    internal class HtmlNumberPart
    {
        #region public属性

        /// <summary>
        ///     权重,数字越大越重要
        /// </summary>
        public double Power { get; set; }

        public string Text { get; set; }
        public HtmlNode Node { get; set; }

        #endregion
    }


    internal class UrlPart
    {
        #region public属性

        public string Format { get; set; }
        public int ValidCount { get; set; }
        public double ValidTitleRate { get; set; }

        #endregion
    }


    /// <summary>
    ///     页面元素自动分析器配置
    /// </summary>
    public static class HtmlNodeExtend
    {
        #region 静态字段

        public const string TagRegex = @"<(?![/!]|\s)[\s\S]*?>";

        #endregion

        #region public静态方法

        public static int TagCount(this HtmlNode node)
        {
            MatchCollection matches = Regex.Matches(node.OuterHtml, TagRegex);
            return matches.Count;
        }

        public static decimal TextDensity(this HtmlNode node)
        {
            return (decimal)node.InnerText.Length / node.OuterHtml.Length;
        }

        #endregion
    }

    [DataContract]
    public class HtmlPatternTraits
    {
        [DataMember]
        public String LCAXPath { get; set; }

        [DataMember]
        public Double VisualScore { get; set; }

        [DataMember]
        public Boolean DOMHeightCollapsed { get; set; }

        public static HtmlPatternTraits FromHtmlPattern(HtmlPattern pattern)
        {
            HtmlPatternTraits traits = new HtmlPatternTraits();
            traits.DOMHeightCollapsed = pattern.DOMHeightCollapsed;
            traits.LCAXPath = pattern.LeastCommonAncestor == null ? null : pattern.LeastCommonAncestor.XPath;
            traits.VisualScore = pattern.VisualScore;
            return traits;
        }
    }

    [DataContract]
    public class GeckoVisualScoreMessageTraits
    {
        [DataMember]
        public List<HtmlPatternTraits> Patterns { get; set; }

        [DataMember]
        public Dictionary<String, Rectangle> Rectangls { get; set; }
    }

    public class HtmlPattern
    {
        /// <summary>
        /// 该Pattern绝对路径
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// 该Pattern单一元素的根节点（父级则覆盖多个Item了），可以向下延展
        /// </summary>
        public string ItemBaseXPath { get; set; }

        /// <summary>
        /// 相对ItemRootXPath节点的路径
        /// </summary>
        public string RelXPath { get; set; }

        public int RelXPathLevel { get { return XPathUtility.CountXPathLevel(RelXPath); } }

        public bool RelXPathUsingName { get { return XPathUtility.isXPathUsingName(RelXPath); } }

        [Obsolete]
        public HtmlNode TopNode { get; set; }
        public HtmlNode LeastCommonAncestor { get; set; }
        public String LCAXPath { get; set; }
        public int ItemCount { get; set; }
        public Double AverageTextLength { get; set; }
        [Obsolete]
        public int ParentCnt { get; set; }
        [Obsolete]
        public double TextDensity { get; set; }
        public int LevelIgnored { get; set; }

        [Obsolete]
        public Boolean XPathMismatch { get; set; }
        [Obsolete]
        public Boolean DOMHeightCollapsed { get; set; }
        public Double VisualScore { get; set; }

        public double Score { get; set; }

        public HtmlPattern()
        { }

        public HtmlPattern(HtmlPattern other)
        {
            XPath = other.XPath;
            ItemBaseXPath = other.ItemBaseXPath;
            RelXPath = other.RelXPath;
            LeastCommonAncestor = other.LeastCommonAncestor;
            LCAXPath = other.LCAXPath;
            ItemCount = other.ItemCount;
            AverageTextLength = other.AverageTextLength;
            LevelIgnored = other.LevelIgnored;
            VisualScore = other.VisualScore;
            Score = other.Score;
        }

        public override string ToString()
        {
            return string.Format("Base: {0} Rel: {1} Count: {2} Score: {3}", ItemBaseXPath, RelXPath, ItemCount, Score);
        }
    }

    public class HtmlPatternXPathComparer : IEqualityComparer<HtmlPattern>
    {
        public bool Equals(HtmlPattern x, HtmlPattern y)
        {
            return x.XPath == y.XPath && x.ItemBaseXPath == y.ItemBaseXPath && x.ItemCount == y.ItemCount;
        }

        public int GetHashCode(HtmlPattern obj)
        {
            return (obj.ItemBaseXPath + obj.XPath).GetHashCode();
        }
    }

    public class HtmlPatternRelXPathComparer : IEqualityComparer<HtmlPattern>
    {
        public bool Equals(HtmlPattern x, HtmlPattern y)
        {
            return x.RelXPath == y.RelXPath;
        }

        public int GetHashCode(HtmlPattern obj)
        {
            return obj.RelXPath.GetHashCode();
        }
    }

    internal class ContentBlockPart
    {
        #region public属性

        public HtmlNode ContentBlock { get; set; }
        public string ChildContentXPath { get; set; }
        public int ChildContentCnt { get; set; }
        public string CleanContent { get; set; }
        public int ParentDepth { get; set; }
        public HtmlNode RawNode { get; set; }
        public int Priority { get; set; }

        #endregion
    }

    internal class ContentBlockPartCompare : IEqualityComparer<ContentBlockPart>
    {
        #region IEqualityComparer<ContentBlockPart>接口成员

        public bool Equals(ContentBlockPart x, ContentBlockPart y)
        {
            return x.CleanContent == y.CleanContent;
        }

        public int GetHashCode(ContentBlockPart obj)
        {
            return obj.CleanContent.GetHashCode();
        }

        #endregion
    }
}
