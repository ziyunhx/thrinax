/*
 * Based on Author: StanZhai 翟士丹 (mail@zhaishidan.cn). All rights reserved.
 * 
 * Modify By Carey Tzou at 13/01/2018.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License. 
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and limitations under the License.
*/

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Thrinax.Models;
using Thrinax.Utility;
using System.Linq;

namespace Thrinax.Extract
{
    /// <summary>
    /// 解析Html页面的文章正文内容,基于文本密度的HTML正文提取类
    /// Date:   2012/12/30
    /// Update: 
    ///     2013/7/10   优化文章头部分析算法，优化
    ///     2014/4/25   添加Html代码中注释过滤的正则
    ///         
    /// </summary>
    public class Html2Article
    {
        #region 参数设置

        // 正则表达式过滤：正则表达式，要替换成的文本
        private static readonly string[][] Filters =
        {
            new[] { @"(?is)<script.*?>.*?</script>", "" },
            new[] { @"(?is)<style.*?>.*?</style>", "" },
            new[] { @"(?is)<!--.*?-->", "" },    // 过滤Html代码中的注释
            // 针对链接密集型的网站的处理，主要是门户类的网站，降低链接干扰
            new[] { @"(?is)</a>", "</a>\n"}
        };

        private static bool _appendMode = false;
        /// <summary>
        /// 是否使用追加模式，默认为false
        /// 使用追加模式后，会将符合过滤条件的所有文本提取出来
        /// </summary>
        public static bool AppendMode
        {
            get { return _appendMode; }
            set { _appendMode = value; }
        }

        private static int _depth = 6;
        /// <summary>
        /// 按行分析的深度，默认为6
        /// </summary>
        public static int Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        private static int _limitCount = 180;
        /// <summary>
        /// 字符限定数，当分析的文本数量达到限定数则认为进入正文内容
        /// 默认180个字符数
        /// </summary>
        public static int LimitCount
        {
            get { return _limitCount; }
            set { _limitCount = value; }
        }

        // 确定文章正文头部时，向上查找，连续的空行到达_headEmptyLines，则停止查找
        private static int _headEmptyLines = 2;
        // 用于确定文章结束的字符数
        private static int _endLimitCharCount = 10;

        #endregion

        /// <summary>
        /// 从给定的Html原始文本中获取正文信息
        /// 只有单条链接情况，正文缺少推倒去重复
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static Article GetArticle(string html)
        {
            // 常见的段内标签去除
            if (html.Contains("<h-char unicode="))
            {
                html = Regex.Replace(html, "<h-char unicode=.*?\"><h-inner>([\\s\\S]*?)?</h-inner></h-char>", "$1");
            }

            // 对区块元素进行分行处理
            html = html.Replace("<h1", "\n\n\n\n\n<h1");
            html = html.Replace("h1>", "h1>\n\n\n\n\n");
            html = html.Replace("div>", "div>\n");

            // 获取html，body标签内容
            string body = "";
            string bodyFilter = @"(?is)<body.*?</body>";
            Match m = Regex.Match(html, bodyFilter);
            if (m.Success)
            {
                body = m.ToString();
            }
            // 过滤样式，脚本等不相干标签
            foreach (var filter in Filters)
            {
                body = Regex.Replace(body, filter[0], filter[1]);
            }
            // 标签规整化处理，将标签属性格式化处理到同一行
            // 处理形如以下的标签：
            //  <a 
            //   href='http://www.baidu.com'
            //   class='test'
            // 处理后为
            //  <a href='http://www.baidu.com' class='test'>
            body = Regex.Replace(body, @"(<[^<>]+)\s*\n\s*", FormatTag);

            Article article = new Article();

            article.Title = GetTitle(html);
            string dateTimeStr = GetPublishDate(body);

            article.PubDate =DateTimeParser.Parser(dateTimeStr);
            article.HtmlContent = GetHtmlContent(body, article.Title, dateTimeStr);

            if (!string.IsNullOrWhiteSpace(article.HtmlContent))
                article.Content = HTMLCleaner.CleanHTML(article.HtmlContent, false);

            return article;
        }

        /// <summary>
        /// 格式化标签，剔除匹配标签中的回车符
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private static string FormatTag(Match match)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ch in match.Value)
            {
                if (ch == '\r' || ch == '\n')
                {
                    continue;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取时间
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string GetTitle(string html)
        {
            string titleFilter = @"<title>[\s\S]*?</title>";
            string h1Filter = @"<h1.*?>.*?</h1>";
            string clearFilter = @"<.*?>";

            string title = "";
            Match match = Regex.Match(html, titleFilter, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                title = Regex.Replace(match.Groups[0].Value, clearFilter, "");
            }

            // 正文的标题一般在h1中，比title中的标题更干净
            match = Regex.Match(html, h1Filter, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string h1 = Regex.Replace(match.Groups[0].Value, clearFilter, "");
                if (!String.IsNullOrEmpty(h1) && title.Contains(h1))
                {
                    title = h1;
                }
            }
            return title;
        }

        /// <summary>
        /// 获取文章发布日期
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string GetPublishDate(string html)
        {
            // 过滤html标签，防止标签对日期提取产生影响
            string text = Regex.Replace(html, "(?is)<.*?>", "");
            Match match = Regex.Match(
                text,
                @"((\d{4}|\d{2})(\-|\/)\d{1,2}\3\d{1,2})(\s?\d{2}:\d{2})?|(\d{4}年\d{1,2}月\d{1,2}日)(\s?\d{2}:\d{2})?",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                try
                {
                    string dateStr = "";
                    for (int i = 0; i < match.Groups.Count; i++)
                    {
                        dateStr = match.Groups[i].Value;
                        if (!String.IsNullOrEmpty(dateStr))
                        {
                            break;
                        }
                    }
                    return dateStr;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return "";
        }

        public static string GetHtmlContent(string bodyText, string Title, string dateTimeStr)
        {
            string baseHtmlContent = bodyText;

            //首先通过Html的div标签拆解元素打分
            List<Tuple<string, double>> listNodes = SplitHtmlTextByBlockElement(bodyText);
            if (listNodes != null && listNodes.Count > 0)
                baseHtmlContent = listNodes.OrderByDescending(f => f.Item2).FirstOrDefault().Item1;

            //获取打分最高的元素，去除 H1, 包含 Title 和 dateTimeStr 的行
            if (Regex.IsMatch(baseHtmlContent, @"<\s*h1(\s|>)", RegexOptions.IgnoreCase))
            {
                HtmlNode htmlNode = HtmlUtility.getSafeHtmlRootNode(baseHtmlContent, true, true);
                HtmlNodeCollection Ps = htmlNode.SelectNodes("//h1");
                if (Ps != null && Ps.Count > 0)
                    foreach (HtmlNode node in Ps)
                        try
                        {
                            node.RemoveAll();
                        }
                        catch (Exception ex)
                        {
                        }

                baseHtmlContent = htmlNode.OuterHtml;
            }

            //baseHtmlContent.Replace(Title, "");
            //baseHtmlContent.Replace(dateTimeStr, "");

            return baseHtmlContent;
        }

        /// <summary>
        /// 从body标签文本中分析正文内容
        /// </summary>
        /// <param name="bodyText">只过滤了script和style标签的body文本内容</param>
        /// <param name="content">返回文本正文，不包含标签</param>
        /// <param name="contentWithTags">返回文本正文包含标签</param>
        private static void GetContent(string bodyText, out string content, out string contentWithTags)
        {
            string[] orgLines = null;   // 保存原始内容，按行存储
            string[] lines = null;      // 保存干净的文本内容，不包含标签

            orgLines = bodyText.Split('\n');
            lines = new string[orgLines.Length];
            // 去除每行的空白字符,剔除标签
            for (int i = 0; i < orgLines.Length; i++)
            {
                string lineInfo = orgLines[i];
                // 处理回车，使用[crlf]做为回车标记符，最后统一处理
                lineInfo = Regex.Replace(lineInfo, "(?is)</p>|<br.*?/>", "[crlf]");
                lines[i] = Regex.Replace(lineInfo, "(?is)<.*?>", "").Trim();
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder orgSb = new StringBuilder();

            int preTextLen = 0;         // 记录上一次统计的字符数量
            int startPos = -1;          // 记录文章正文的起始位置
            for (int i = 0; i < lines.Length - _depth; i++)
            {
            reCal:

                int len = 0;
                for (int j = 0; j < _depth; j++)
                {
                    int last = len;
                    len += lines[i + j].Length;

                    if (len == 0) //忽略空行开头的情况
                        break;

                    //寻找突然变化的点，如果某次的数量大于之前所有的字数*2，则以他为起点
                    if (j > 0 && last * 2 < lines[i + j].Length)
                    {
                        i = i + j;
                        goto reCal;
                    }
                }

                if (startPos == -1)     // 还没有找到文章起始位置，需要判断起始位置
                {
                    if (preTextLen > _limitCount && len > 0)    // 如果上次查找的文本数量超过了限定字数，且当前行数字符数不为0，则认为是开始位置
                    {
                        // 查找文章起始位置, 如果向上查找，发现2行连续的空行则认为是头部（这里并不一定呀）
                        int emptyCount = 0;
                        for (int j = i - 1; j > 0; j--)
                        {
                            if (String.IsNullOrEmpty(lines[j]))
                            {
                                emptyCount++;
                            }
                            else
                            {
                                emptyCount = 0;
                            }
                            if (emptyCount == _headEmptyLines)
                            {
                                startPos = j + _headEmptyLines;
                                break;
                            }
                        }
                        // 如果没有定位到文章头，则以当前查找位置作为文章头
                        if (startPos == -1)
                        {
                            startPos = i;
                        }
                        // 填充发现的文章起始部分
                        for (int j = startPos; j <= i; j++)
                        {
                            sb.Append(lines[j]);
                            orgSb.Append(orgLines[j]);
                        }
                    }
                }
                else
                {
                    //if (len == 0 && preTextLen == 0)    // 当前长度为0，且上一个长度也为0，则认为已经结束
                    if (len <= _endLimitCharCount && preTextLen < _endLimitCharCount)    // 当前长度为0，且上一个长度也为0，则认为已经结束
                    {
                        if (!_appendMode)
                        {
                            break;
                        }
                        startPos = -1;
                    }
                    sb.Append(lines[i]);
                    orgSb.Append(orgLines[i]);
                }
                preTextLen = len;
            }

            string result = sb.ToString();
            // 处理回车符，更好的将文本格式化输出
            content = result.Replace("[crlf]", Environment.NewLine);
            content = System.Web.HttpUtility.HtmlDecode(content);
            // 输出带标签文本
            contentWithTags = orgSb.ToString();
        }

        /// <summary>
        /// 通过Html常用的块状元素来拆分数据块，第一阶段先用 div
        /// div 默认会有15分的基础分，子元素出现一次div 扣 3 分
        /// 内部包含的正文字每20个得一分，出现一个 a 标签扣除一分
        /// 正文中连续的3个空行会扣除一分，每多一个多扣0.2分
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
        public static List<Tuple<string, double>> SplitHtmlTextByBlockElement(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return null;

            HtmlNode itempagenode = HtmlUtility.getSafeHtmlRootNode(htmlContent, true, true);
            var itemNodes = itempagenode.SelectNodes("//div");

            if (itemNodes == null || itemNodes.Count == 0)
                return null;

            List<Tuple<string, double>> itemNodeScores = new List<Tuple<string, double>>();
            foreach (var itemNode in itemNodes)
            {
                double baseScore = 15;

                HtmlNode innerNode = HtmlUtility.getSafeHtmlRootNode(itemNode.OuterHtml, true, true);

                //子元素出现一次div且内容不为空的 扣 3 分
                var innerDivs = innerNode.SelectNodes("//div");
                if (innerDivs != null && innerDivs.Count > 1)
                {
                    foreach (var innerDiv in innerDivs)
                    {
                        if(!string.IsNullOrWhiteSpace(innerDiv.InnerText))
                            baseScore -= 3;
                    }
                }

                //子元素出现一次a 扣 1 分
                var inneras = innerNode.SelectNodes("//a");
                if (inneras != null && inneras.Count > 0)
                {
                    baseScore -= inneras.Count;
                }

                //获取正文部分，计算字数
                string innerText = itemNode.InnerText;
                if (!string.IsNullOrWhiteSpace(innerText))
                {
                    string innerTextWithoutBlack = Regex.Replace(innerText, @"\s", "");
                    if (!string.IsNullOrWhiteSpace(innerTextWithoutBlack) && innerTextWithoutBlack.Length > 0)
                        baseScore += (double)innerTextWithoutBlack.Length / 20;
                }
                else
                    continue;

                //计算正文中的空行，连续的3个空行会扣除一分，每多一个多扣0.2分
                string[] orgLines = innerText.Split('\n');
                int currentBlackCount = 0;
                foreach (string orgLine in orgLines)
                {
                    if (string.IsNullOrWhiteSpace(orgLine))
                    {
                        currentBlackCount++;
                        if (currentBlackCount == 3)
                            baseScore--;
                        else if (currentBlackCount > 3)
                            baseScore -= 0.2;
                    }
                    else
                        currentBlackCount = 0;
                }

                Tuple<string, double> tuple = new Tuple<string, double>(itemNode.InnerHtml, baseScore);
                itemNodeScores.Add(tuple);
            }

            return itemNodeScores;
        }
    }
}
