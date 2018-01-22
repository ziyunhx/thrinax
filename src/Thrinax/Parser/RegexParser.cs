using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Thrinax.Interface;
using Thrinax.Utility;

namespace Thrinax.Parser
{
    /// <summary>
    /// 正则解析类
    /// </summary>
    public class RegexParser : IParser
    {
         /// <summary>
         /// Parses the list.
         /// </summary>
         /// <returns>The list.</returns>
         /// <param name="Html">Html.</param>
         /// <param name="Pattern">Pattern.</param>
         /// <param name="Url">URL.</param>
         /// <param name="RecogNextPage">If set to <c>true</c> recog next page.</param>
        public ArticleList ParseList(string Html, string Pattern, string Url = null, bool RecogNextPage = true)
        {
            //输入检查
            if (string.IsNullOrWhiteSpace(Html) || string.IsNullOrWhiteSpace(Pattern))
                return null;

            ArticleList articleList = new ArticleList();
            //处理繁体字先
            Html = TextCleaner.ToSimplifyString(Html);
            #region Item集合

            List<Article> Items = null;

            MatchCollection Matches = Regex.Matches(Html, Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, new TimeSpan(0, 0, 10));

            if (Matches == null)
            {
                Logger.Warn(string.Format("正则ParseList失败,Url={0}", Url));
            }

            int MatchesCount = 0;
            try
            {
                MatchesCount = Matches.Count;
            }
            catch (RegexMatchTimeoutException e)
            {
                Logger.Error(string.Format("正则ParseList超时,Url={0}", Url), e);
                Matches = null;
            }

            if (Matches != null && Matches.Count > 0)
            {
                Items = new List<Article>(Matches.Count);
                //去重，唯一ItemID
                HashSet<string> ItemIDs = new HashSet<string>();

                foreach (Match m in Matches)
                {
                    Article Item = new Article();
                    Match2Item(m, ref Item, Url, true);

                    if (Item != null)
                    {
                        Items.Add(Item);
                    }
                }
            }

            articleList.Articles = Items;
            articleList.Count = Items.Count;
            articleList.CurrentPage = 1;
            #endregion Item集合

            return articleList;
        }

        /// <summary>
        /// Parses the item.
        /// </summary>
        /// <param name="Html">Html.</param>
        /// <param name="Pattern">Pattern.</param>
        /// <param name="Url">URL.</param>
        /// <param name="BaseArticle">Base article.</param>
        public bool ParseItem(string Html, string Pattern, string Url, ref Article BaseArticle)
        {
            Match m = Regex.Match(Html, Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline, new TimeSpan(0, 0, 10));
            Match2Item(m, ref BaseArticle, Url, true);
            return true;
        }

        /// <summary>
        /// 从页面上解析Reply页面的Url
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="ReplyUrlString"></param>
        /// <returns></returns>
        public static string ParseItemReplyNextPage(string Content, string ReplyUrlString)
        {
            Match m = Regex.Match(Content, ReplyUrlString, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (!m.Success || !m.Groups["NextPageUrl"].Success || string.IsNullOrEmpty(m.Groups["NextPageUrl"].Value))
                return null;
            else
                return m.Groups["NextPageUrl"].Value;
        }

        /// <summary>
        /// Match2s the item.
        /// </summary>
        /// <param name="m">M.</param>
        /// <param name="Item">Item.</param>
        /// <param name="BaseUrl">Base URL.</param>
        /// <param name="ItemUrlCaseSensitive">If set to <c>true</c> item URL case sensitive.</param>
        public static void Match2Item(Match m, ref Article Item, string BaseUrl, bool ItemUrlCaseSensitive = false)
        {
            //url
            Item.Url = new Uri(new Uri(BaseUrl), RegexUtility.TryGetString(m, "Url", Item.Url, false)).AbsoluteUri;

            //title
            Item.Title = RegexUtility.TryGetString(m, "Title", Item.Title);
            //降低Clean级别
            if (string.IsNullOrEmpty(Item.Title))
                Item.Title = HTMLCleaner.CleanHTML(Item.Title, true);

            //text
            Item.HtmlContent = RegexUtility.TryGetString(m, "Text", Item.HtmlContent, false);

            //Author Info
            Item.Author = RegexUtility.TryGetString(m, "AuthorName", Item.Author);
            Item.Source = RegexUtility.TryGetString(m, "Source", Item.Source);

            if (!String.IsNullOrWhiteSpace(Item.Source))
            {
                Item.Source = TextCleaner.FullClean(Item.Source);
            }

            //Media Info
            Item.MediaName = RegexUtility.TryGetString(m, "MediaName", Item.MediaName);
            //time
 

            if (m.Groups["PubDate"].Success)
            {
                Item.PubDate = DateTimeParser.Parser(HTMLCleaner.CleanHTML(m.Groups["PubDate"].Value, true));
            }

            if (Item.PubDate <= DateTime.MinValue)
            {
                Item.PubDate = DateTime.Now;
            }

            Match2ItemCount(m, Item.ViewDataList);
        }

        /// <summary>
        /// called by Match2Item only
        /// </summary>
        /// <param name="m"></param>
        /// <param name="PreItemCount"></param>
        public static void Match2ItemCount(Match m, List<ViewData> PreItemCount = null)
        {
            if (!m.Groups["View"].Success && !m.Groups["Reply"].Success && !m.Groups["Forward"].Success)
                return;

            ViewData ItemCount = new ViewData();

            ItemCount.FetchTime = DateTime.Now;
            ItemCount.View = RegexUtility.TryGetInt(m, "View", 0);
            ItemCount.Reply = RegexUtility.TryGetInt(m, "Reply", 0);
            ItemCount.Forward = RegexUtility.TryGetInt(m, "Forward", 0);

            if (PreItemCount == null)
                PreItemCount = new List<ViewData>();

            PreItemCount.Add((ItemCount));
        }
    }
}
