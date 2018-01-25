using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Thrinax.Utility;

namespace Thrinax.Parser
{
    class PageParser
    {
        static int countAll = 0;
        static int successCount = 0;
        static int errorCount = 0;
        static int scriptCount = 0;
        static int exCount = 0;

        public static bool validateNextPage(string Html, string Pattern, ref string Url)
        {
            HtmlNode htmlNode = HtmlUtility.getSafeHtmlRootNode(Html);

            List<HtmlNode> atagHtmlNodes = htmlNode.SelectNodes("//a[@href]").ToList();
            List<testNextUrl> x = new List<testNextUrl>();
            StringBuilder Result = new StringBuilder();

            int intX = 0;

            foreach (HtmlNode tmpNode in atagHtmlNodes)
            {
                intX++;
                if (Regex.Match(tmpNode.InnerText, @".*[一二三四五六七八九十\d页].*").Success)
                {
                    testNextUrl tmp = new testNextUrl();
                    tmp.index = intX;
                    tmp.urlText = HTMLCleaner.CleanHTML(tmpNode.InnerText, true);
                    tmp.urlLink = HtmlUtility.AbsoluteUrl(tmpNode.Attributes["href"].Value, Url, true);
                    x.Add(tmp);
                }
            }
            if (x.Count > 0)
            {
                if (Url.Equals(TrianNextUrl(x).urlLink))
                {
                    return false;
                }
                else
                {
                    Url = TrianNextUrl(x).urlLink;
                }
            }
            if (Url.Length == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public class testNextUrl
        {
            public string CrawlID;
            public int index;
            public string urlText;
            public string urlLink;
            public int score = 0;
        }

        public static testNextUrl TrianNextUrl(List<testNextUrl> urlLinks)
        {
            List<testNextUrl> result = new List<testNextUrl>();

            string deletePattern = @"[^一二三四五六七八九十\d页]";

            #region 开始剔除不需要的Url

            // 一：第一个公理“下一页”的就是我们想要的，直接退出。如果这里出错了，写网站的人脑残。
            testNextUrl tmpaa = urlLinks.Where<testNextUrl>(s => s.urlText.Contains("下一页")).FirstOrDefault();

            if (tmpaa != null)
            {
                if (tmpaa.urlLink.StartsWith("http"))
                {
                    successCount++;
                    Logger.Info("【" + urlLinks[0].CrawlID + urlLinks[0].urlLink + "\r\n" + tmpaa.urlLink + "】");

                    return tmpaa;
                }
                else
                {
                    scriptCount++;
                    Logger.Warn("【" + urlLinks[0].CrawlID + urlLinks[0].urlLink + "\r\n" + tmpaa.urlLink + "】");
                    tmpaa.urlLink = "";
                    return tmpaa;
                }
            }
            // 二:第一个假设，翻页链接<a></a>内不会有乱七八糟的文字。如出错，脑残+1
            List<testNextUrl> level1Links = new List<testNextUrl>();
            for (int i = 0; i < urlLinks.Count - 1; i++)
            {
                string thisUrl = Regex.Replace(urlLinks[i].urlLink, deletePattern, "");
                if (thisUrl.Length > 5)
                {
                    continue;
                }
                else
                {
                    level1Links.Add(urlLinks[i]);
                }
            }
            // 三：第二个假设，两个翻页链接中间不会包含其他的<a>标签。如出错，脑残+1
            List<testNextUrl> level2Links = new List<testNextUrl>();
            for (int i = 1; i < level1Links.Count - 1; i++)
            {
                if (level1Links[i].index == urlLinks[i - 1].index + 1 || level1Links[i].index == level1Links[i + 1].index - 1)
                {
                    level2Links.Add(level1Links[i - 1]);
                    level2Links.Add(level1Links[i]);
                    i++;
                }
                //stopFlag;
            }

            // 四：第三个假设，翻页页码的Url都是相似的，页码都是以数字传递的。较可信。

            for (int i = 0; i < level2Links.Count - 1; i++)
            {
                string thisUrl = Regex.Replace(level2Links[i].urlLink, @"\d", "");
                string nextUrl = Regex.Replace(level2Links[i + 1].urlLink, @"\d", "");
                // 默认相似程度0.9
                if (TextStatisticsUtility.Alike(thisUrl, nextUrl))
                {
                    result.Add(level2Links[i]);
                    result.Add(level2Links[i + 1]);
                    i++;
                }
            }

            #endregion

            if (result.Count > 0)
            {
                if (!result[0].urlLink.StartsWith("http"))
                {
                    scriptCount++;
                    Logger.Warn("【" + urlLinks[0].CrawlID + urlLinks[0].urlLink + "\r\n" + result[0].urlLink + "】");
                }
                else
                {
                    successCount++;
                    Logger.Info("【" + urlLinks[0].CrawlID + urlLinks[0].urlLink + "\r\n" + result[0].urlLink + "】");
                }
                return result[0];
            }
            else
            {
                errorCount++;
                Logger.Error("【" + urlLinks[0].CrawlID + urlLinks[0].urlLink + "\r\n" + "失败】");
                return null;
            }
        }



    }
}
