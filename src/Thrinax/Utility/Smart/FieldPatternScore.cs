using System;
using Thrinax.Utility;

namespace Thrinax.Utility.Smart
{
    /// <summary>
    /// 记录一个各字段XPath的组合及其可能性得分
    /// </summary>
    class FieldPatternScore
    {
        public string MediaPath { get; set; }
        public string DatePath { get; set; }
        public string ViewPath { get; set; }
        public string ReplyPath { get; set; }
        public string AuthorPath { get; set; }
        /// <summary>
        /// 五种字段Pattern的可能性乘积
        /// </summary>
        public double Score { get; set; }

        public int CountUsingName { get; set; }

        public int CountLevel { get; set; }

        public FieldPatternScore(string DatePath, string ViewPath, string ReplyPath, string MediaPath, string AuthorPath, double CrossScore)
        {
            this.DatePath = DatePath;
            this.ViewPath = ViewPath;
            this.ReplyPath = ReplyPath;
            this.MediaPath = MediaPath;
            this.AuthorPath = AuthorPath;
            this.Score = CrossScore;

            int CountName = 0, CountLevel = 0;
            if (!string.IsNullOrEmpty(DatePath))
            {
                if (XPathUtility.isXPathUsingName(DatePath)) CountName++;
                CountLevel += XPathUtility.CountXPathLevel(DatePath);
            }
            if (!string.IsNullOrEmpty(ViewPath))
            {
                if (XPathUtility.isXPathUsingName(ViewPath)) CountName++;
                CountLevel += XPathUtility.CountXPathLevel(ViewPath);
            }
            if (!string.IsNullOrEmpty(ReplyPath))
            {
                if (XPathUtility.isXPathUsingName(ReplyPath)) CountName++;
                CountLevel += XPathUtility.CountXPathLevel(ReplyPath);
            }
            if (!string.IsNullOrEmpty(MediaPath))
            {
                if (XPathUtility.isXPathUsingName(MediaPath)) CountName++;
                CountLevel += XPathUtility.CountXPathLevel(MediaPath);
            }
            if (!string.IsNullOrEmpty(AuthorPath))
            {
                if (XPathUtility.isXPathUsingName(AuthorPath)) CountName++;
                CountLevel += XPathUtility.CountXPathLevel(AuthorPath);
            }
            this.CountLevel = CountLevel;
            this.CountUsingName = CountName;
        }
    }

}
