using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Thrinax.Http;
using Thrinax.Interface;
using Thrinax.Utility;

namespace Thrinax.Parser
{
    /// <summary>
    /// 通过 Xpath 获取 Article
    /// </summary>
    public class XpathParser : IParser
    {
        private static ParallelOptions _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };
        private static object syncObj = new object();
        private static object patternsObj = new object();

        public ArticleList ParseList(string Html, string Pattern, string Url = null, bool RecogNextPage = true)
        {
            //输入检查
            if (string.IsNullOrWhiteSpace(Html) || string.IsNullOrWhiteSpace(Pattern))
                return null;

            //检查 Pattern 的格式，判断是否符合要求
            XpathPattern xpathPattern = null;
            try {
                xpathPattern = JsonConvert.DeserializeObject<XpathPattern>(Pattern);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Pattern 的格式不符合 Xpath Parser 的定义，请检查！Url:{0}, Pattern:{1}.", Url, Pattern), ex);
            }

            ArticleList articleList = new ArticleList();

            List<Article> articles = new List<Article>();

            #region Article 集合
            HashSet<string> ItemIDs = new HashSet<string>();
            HtmlNode htmlNode = HtmlUtility.getSafeHtmlRootNode(Html, true, true);

            articles = ExtractItemFromList(Url, htmlNode, xpathPattern);
            #endregion Item集合


            articleList.Articles = articles;
            articleList.Count = articleList?.Count ?? 0;

            return articleList;
        }

        public bool ParseItem(string Html, string Pattern, string Url, ref Article BaseArticle)
        {
            //输入检查
            if (string.IsNullOrWhiteSpace(Html) || string.IsNullOrWhiteSpace(Pattern))
                return false;

            //检查 Pattern 的格式，判断是否符合要求
            XpathPattern xpathPattern = null;
            try
            {
                xpathPattern = JsonConvert.DeserializeObject<XpathPattern>(Pattern);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Pattern 的格式不符合 Xpath Parser 的定义，请检查！Url:{0}, Pattern:{1}.", Url, Pattern), ex);
            }

            HtmlNode itempagenode = HtmlUtility.getSafeHtmlRootNode(Html, true, true);

            //提取文章正文
            if (string.IsNullOrEmpty(BaseArticle.HtmlContent) && !string.IsNullOrWhiteSpace(xpathPattern.ItemContentXPath))
            {
                try
                {
                    BaseArticle.HtmlContent = HTMLCleaner.CleanContent(itempagenode.SelectNodes(xpathPattern.ItemContentXPath), Url, true);
                    BaseArticle.Content = HTMLCleaner.CleanHTML(BaseArticle.HtmlContent, false);
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("从详情页解析正文出错，Url:{0}, Pattern:{1}.", Url, xpathPattern.ItemContentXPath), ex);
                }
            }

            //确认标题
            if (string.IsNullOrEmpty(BaseArticle.Title) && !string.IsNullOrWhiteSpace(xpathPattern.ItemTitleXPath))
            {
                try
                {
                    BaseArticle.Title = TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(itempagenode.SelectSingleNode(xpathPattern.ItemTitleXPath)));
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("从详情页解析标题出错，Url:{0}, Pattern:{1}.", Url, xpathPattern.ItemTitleXPath), ex);
                }
            }

            //确认时间
            if (!string.IsNullOrWhiteSpace(xpathPattern.ItemDateXPath))
            {
                try
                {
                    DateTime Pubdate = DateTimeParser.Parser(HTMLCleaner.GetCleanInnerText(itempagenode.SelectSingleNode(xpathPattern.ItemDateXPath)));

                    if (BaseArticle.PubDate <= DateTime.MinValue.AddYears(1) && Pubdate.Year > 2000) //发布时间过旧
                        BaseArticle.PubDate = Pubdate;
                    else if (BaseArticle.PubDate.Hour == 0 && BaseArticle.PubDate.Minute == 0 && (Pubdate.Hour != 0 || Pubdate.Minute != 0) && Pubdate.Year > 2000) //发布时间没有时与分
                        BaseArticle.PubDate = Pubdate;
                    else if (Pubdate.Year > 2000 && (Pubdate.Hour != 0 || Pubdate.Minute != 0) && (BaseArticle.PubDate - Pubdate) > new TimeSpan(0, 1, 59) && BaseArticle.PubDate >= DateTime.Now.AddMinutes(-10)) //发布时间拒当前时间很近且相差较大
                        BaseArticle.PubDate = Pubdate;
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("从详情页解析标题出错，Url:{0}, Pattern:{1}.", Url, xpathPattern.ItemContentXPath), ex);
                }
            }

            //确认媒体
            if (string.IsNullOrEmpty(BaseArticle.MediaName) && !string.IsNullOrWhiteSpace(xpathPattern.ItemMediaNameXPath))
            {
                try
                {
                    BaseArticle.MediaName = TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(itempagenode.SelectSingleNode(xpathPattern.ItemMediaNameXPath)));
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("从详情页解析媒体出错，Url:{0}, Pattern:{1}.", Url, xpathPattern.ItemMediaNameXPath), ex);
                }
            }

            //确认作者
            if (string.IsNullOrEmpty(BaseArticle.Author) && !string.IsNullOrWhiteSpace(xpathPattern.ItemAuthorXPath))
            {
                try
                {
                    BaseArticle.Author = TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(itempagenode.SelectSingleNode(xpathPattern.ItemAuthorXPath)));
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("从详情页解析作者出错，Url:{0}, Pattern:{1}.", Url, xpathPattern.ItemAuthorXPath), ex);
                }
            }

            return true;
        }

        /// <summary>
        /// 从List页面上根据各字段XPath提取内容集合
        /// </summary>
        /// <param name="Url">网址</param>
        /// <param name="RootNode">Document的根节点</param>
        /// <param name="Path">根据此ListPath来提取内容</param>
        /// <param name="List_MinCountItem">至少List几个Item（用于判定旧网站中大量A堆砌在同一个元素下的情况）</param>
        /// <param name="needscalepages">是否需要翻页，默认为否</param>
        /// <returns></returns>
        public static List<Article> ExtractItemFromList(string Url, HtmlNode RootNode, XpathPattern Path)
        {
            List<Article> Content = new List<Article>();

            //fix a null bug by carey. 2014-09-10
            HtmlNodeCollection rootNodes = RootNode.SelectNodes(Path.ItemRootXPath);

            if (rootNodes != null && rootNodes.Count > 0)
            {
                foreach (HtmlNode BaseNode in rootNodes)
                {
                    //正常情况下，每个BaseNode有一个Item，但是某些网站可能存在多个
                    if (string.IsNullOrWhiteSpace(Path.TitleXPath) || BaseNode.SelectNodes(Path.TitleXPath) == null)
                        continue;

                    //如果 BaseNode 的数量小于6，则判断是否存在多个可匹配的 Title 项；如果存在的话则记录数量
                    int singleNodeItemCount = BaseNode.SelectNodes(Path.TitleXPath).Where(n => n.Attributes.Contains("href")).Count();

                    if (singleNodeItemCount <= 0)
                        singleNodeItemCount = 1;

                    List<HtmlNode> nodecollection = BaseNode.SelectNodes(Path.TitleXPath).Where(n => n.Attributes.Contains("href")).ToList();

                    if (nodecollection != null && nodecollection.Count() > 0 && nodecollection.Any(n => !string.IsNullOrEmpty(n.Attributes["href"].Value)))
                    {
                        Article[] articleNodeItems = new Article[singleNodeItemCount];

                        for (int i = 0; i < singleNodeItemCount; i++)
                        {
                            articleNodeItems[i].Title = ExtractInnerTextFromBaseNode(BaseNode, Path.TitleXPath, i);
                            if (articleNodeItems[i].Title != null)
                            {
                                try
                                {
                                    articleNodeItems[i].Url = nodecollection.Where(n => !string.IsNullOrEmpty(n.Attributes["href"].Value)).ElementAt(i).Attributes["href"].Value;
                                    if (articleNodeItems[i].Url.Contains(".pdf"))
                                        continue;
                                    if (articleNodeItems[i].Url.StartsWith("javascript:openArticle"))
                                    {
                                        articleNodeItems[i].Url = articleNodeItems[i].Url.Substring(articleNodeItems[i].Url.IndexOf("('") + 2);
                                        articleNodeItems[i].Url = articleNodeItems[i].Url.Substring(0, articleNodeItems[i].Url.IndexOf("')"));
                                    }
                                    articleNodeItems[i].Url = HtmlUtility.AbsoluteUrl(articleNodeItems[i].Url, Url, true);
                                    string url = HtmlUtility.AbsoluteUrl(articleNodeItems[i].Url, Url, true);
                                    articleNodeItems[i].Url = url;
                                    if (articleNodeItems[i].Url.Contains('@'))
                                        continue;
                                }
                                catch (Exception ex)
                                {
                                    articleNodeItems[i].Url = null;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(Path.MediaNameXPath))
                            {
                                articleNodeItems[i].MediaName = ExtractInnerTextFromBaseNode(BaseNode, Path.MediaNameXPath, i);
                                articleNodeItems[i].MediaName = ExtractSegmentFromInnerText(articleNodeItems[i].MediaName, MediaPrefixRegex);
                                articleNodeItems[i].MediaName = HTMLCleaner.CleanMediaName(articleNodeItems[i].MediaName);//清洗
                            }
                            if (!string.IsNullOrWhiteSpace(Path.AuthorXPath))
                            {
                                articleNodeItems[i].Author = ExtractInnerTextFromBaseNode(BaseNode, Path.AuthorXPath, i);
                                articleNodeItems[i].Author = ExtractSegmentFromInnerText(articleNodeItems[i].Author, AuthorPrefixRegex);
                                articleNodeItems[i].Author = HTMLCleaner.CleanAuthor(articleNodeItems[i].Author);//清洗
                            }
                            if (!string.IsNullOrWhiteSpace(Path.DateXPath))
                            {
                                articleNodeItems[i].PubDate = DateTimeParser.Parser(ExtractInnerTextFromBaseNode(BaseNode, Path.DateXPath, i));
                            }
                            if (!string.IsNullOrWhiteSpace(Path.AbsTractXPath))
                                articleNodeItems[i].AbsTract = ExtractInnerTextFromBaseNode(BaseNode, Path.AbsTractXPath, i);
                            //点击数的提取逻辑                        
                            string ViewString = string.Empty;
                            if (!string.IsNullOrWhiteSpace(Path.ViewXPath) || !string.IsNullOrWhiteSpace(Path.ReplyXPath))
                            {
                                ViewData currentViewData = new ViewData();
                                currentViewData.FetchTime = DateTime.Now;

                                ViewString = ExtractInnerTextFromBaseNode(BaseNode, Path.ViewXPath, i, false);
                                if (!string.IsNullOrEmpty(ViewString))
                                {
                                    MatchCollection digiText = Regex.Matches(ViewString, @"\d{1,9}");
                                    if (digiText.Count == 1)
                                        currentViewData.View = int.Parse(digiText[0].Captures[0].Value);
                                    else if (digiText.Count > 1 && Path.ViewXPath == Path.ReplyXPath) //View和Reply在一个格子里，这里容易出现多个的情况，不建议使用
                                    {
                                        int a = int.Parse(digiText[0].Captures[0].Value);
                                        int b = int.Parse(digiText[1].Captures[0].Value);
                                        currentViewData.View = a >= b ? a : b;
                                        currentViewData.Reply = a >= b ? b : a;
                                    }
                                }

                                //评论数的提取逻辑
                                if (!string.IsNullOrEmpty(Path.ReplyXPath) && Path.ViewXPath != Path.ReplyXPath)
                                {
                                    string ReplyString = ExtractInnerTextFromBaseNode(BaseNode, Path.ReplyXPath, i, false);
                                    if (!string.IsNullOrEmpty(ReplyString))
                                    {
                                        MatchCollection digiText = Regex.Matches(ReplyString, @"\d{1,9}");
                                        if (digiText.Count > 0) //单独的Reply
                                            currentViewData.Reply = int.Parse(digiText[0].Captures[0].Value);
                                    }
                                }
                                if (articleNodeItems[i].ViewDataList == null)
                                    articleNodeItems[i].ViewDataList = new List<ViewData>();

                                articleNodeItems[i].ViewDataList.Add(currentViewData);
                            }
                        }

                        Content.AddRange(articleNodeItems.Where(f => !string.IsNullOrWhiteSpace(f.Url)));
                    }
                }
            }

            return Content;
        }

        /// <summary>
        /// 针对不存在 HtmlNode 时使用，如果 Html 也不存在，将使用默认 Http Helper 获取网页源码
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        public static List<Article> ExtractItemFromList(string Url, XpathPattern Pattern, string Html = null)
        {
            if (string.IsNullOrWhiteSpace(Html)) Html = HttpHelper.GetHttpContent(Url);
            HtmlNode rootNode = HtmlUtility.getSafeHtmlRootNode(Html, true, true);

            return ExtractItemFromList(Url, rootNode, Pattern);
        }

        /// <summary>
        /// 根据相对路径XPath从单一Item的BaseNode节点提取某一个字段的Node的InnerText
        /// </summary>
        /// <param name="BaseNode">一个Item的根节点</param>
        /// <param name="RelXPath">相对XPath路径</param>
        /// <param name="CleanConnectionMark">是否清洗文本</param>
        /// <returns></returns>
        internal static string ExtractInnerTextFromBaseNode(HtmlNode BaseNode, string RelXPath, int postion, bool CleanConnectionMark = true)
        {
            if (BaseNode == null) return null;

            if (string.IsNullOrWhiteSpace(RelXPath) && postion == 0)
            {
                if (CleanConnectionMark)
                    return TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(BaseNode));
                else
                    return TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(BaseNode), true, true, true, false, true, false);
            }

            IEnumerable<HtmlNode> MatchNodes = BaseNode.SelectNodes(RelXPath);
            if (MatchNodes != null)
                MatchNodes = MatchNodes.Where(n => !string.IsNullOrEmpty(XPathUtility.InnerTextNonDescendants(n)));
            if (!string.IsNullOrWhiteSpace(RelXPath) && (MatchNodes == null || MatchNodes.Count() <= postion))
                return null;

            if (CleanConnectionMark)
                return TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(MatchNodes.ElementAt(postion)));
            else
                return TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(MatchNodes.ElementAt(postion)), true, true, true, false, true, false);
        }

        internal const string AuthorPrefixRegex = @"作\s*者|选\s*稿|编\s*辑|记\s*者|作\s*家";
        internal const string MediaPrefixRegex = @"名\s*称|媒\s*体|来\s*源|来\s*自|来\s*源\s*于|转\s*载|转\s*自|原\s*文|链\s*接";

        /// <summary>
        /// 根据可能的前缀集合，从InnerText中提取更精确的文本值
        /// </summary>
        /// <param name="InnerText"></param>
        /// <param name="Prefix"></param>
        /// <returns></returns>
        internal static string ExtractSegmentFromInnerText(string InnerText, string PrefixRegex)
        {
            if (string.IsNullOrEmpty(InnerText) || string.IsNullOrEmpty(PrefixRegex)) return InnerText;

            string Text = InnerText;
            //匹配最尾端的
            Match PrefixMatch = Regex.Match(InnerText, PrefixRegex, RegexOptions.RightToLeft);
            if (PrefixMatch.Success)
            {
                Text = InnerText.Substring(PrefixMatch.Index + PrefixMatch.Length).TrimStart(':', '：', ']', '】', ' ');

                //在第一个空格处截断
                int SpaceIndex = Text.IndexOf(' ');
                if (SpaceIndex > 0) Text = Text.Substring(0, SpaceIndex);
            }

            return Text;
        }
    }
}
