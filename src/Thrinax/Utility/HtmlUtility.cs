using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Thrinax.Utility
{
    /// <summary>
    /// 操作 HTML document 或 node 的工具
    /// </summary>
    public static class HtmlUtility
    {
        /// <summary>
        /// 返回 HtmlDocument
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static HtmlDocument CreateHtmlDoc(string html)
        {
            if (html == null) return new HtmlDocument();
            HtmlDocument h = new HtmlDocument(); h.LoadHtml(html);
            return h;
        }

        /// <summary>
        /// 返回 collection 中评分最高的全部元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="valueFunc"></param>
        /// <returns></returns>
        public static IEnumerable<T> ArgMaxAll<T>(ICollection<T> collection, Func<T, double> valueFunc)
        {
            if (collection == null || collection.Count == 0) yield break;
            var m = collection.Max(valueFunc);
            foreach (var x in collection)
            {
                double vx = valueFunc(x);
                if (vx == m)
                {
                    yield return x;
                }
            }
        }

        /// <summary>
        /// 如果传来的 collection 为 null 或零元素，则返回 T 类型的默认值，如 null。
        /// 要找 argmin，只要让 valueFunc 返回原来函数的负值即可。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="valueFunc"></param>
        /// <returns></returns>
        public static T ArgMax<T>(IEnumerable<T> collection, Func<T, double> valueFunc)
        {
            if (collection == null) return default(T);
            T element = collection.FirstOrDefault();
            double m = valueFunc(element);
            foreach (var x in collection)
            {
                double vx = valueFunc(x);
                if (m < vx)
                {
                    element = x;
                    m = vx;
                }
            }
            return element;
        }

        /// <summary>
        /// 防止访问 null 的 HTML 节点 n 的属性
        /// </summary>
        /// <param name="n"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetSafeNodeAttribute(HtmlNode n, string name)
        {
            if (n == null) return null;
            if (n.Attributes.Contains(name)) return n.Attributes[name].Value;
            return null;
        }

        /// <summary>
        /// 去除 HTML 字串中的注释和多余的空白符。
        /// 注意：有时候注释中包含了可以帮助进行定位的信息，因此不一定要用它来做。
        /// 但是在进行密度计算的时候，进行清理会得到比较有用的数值。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string CleanseHtmlAnnotations(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            Regex r = new Regex(@"<!--.*?-->|<style.*?>.*?</style>|<script.*?</script>|<.*?>|\s|&.*?;", RegexOptions.Multiline | RegexOptions.Compiled);
            Regex rs = new Regex(@"\s+", RegexOptions.Multiline | RegexOptions.Compiled);
            string t0 = text;
            while (true)
            {
                text = r.Replace(text, " ");
                if (t0 == text) break;
                t0 = text;
            }
            return rs.Replace(text, " ").Trim();
        }

        /// <summary>
        /// 判断这个 Tag 是否可能内含列表、链接或回复项
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public static bool IsValidContainerTag(string tagName)
        {
            if (tagName.StartsWith("#")) return false;
            switch (tagName)
            {
                case "ul":
                case "td":
                case "th":
                case "tr":
                case "div":
                case "form":
                case "table":
                case "tbody":
                default:
                    return true;
                case "a":
                case "span":
                case "input":
                case "script":
                case "head":
                case "link":
                case "meta":
                case "title":
                case "style":
                case "br":
                    return false;
            }
        }

        /// <summary>
        /// 以 class 和 id 的方式，而不是数字下标，来构造节点的 XPath
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string BuildXPathWithClassAndId(HtmlNode n)
        {
            string res = "";

            while (n != null && n.Name != "#document")
            {
                string id = "", className = "";
                if (!string.IsNullOrWhiteSpace(n.Id))
                {
                    if (!Regex.IsMatch(n.Id, @"\d{2,}"))
                        id = "[@id='" + n.Id + "']";
                }
                if (!string.IsNullOrWhiteSpace(GetSafeNodeAttribute(n, "class"))) className = "[@class='" + HtmlUtility.GetSafeNodeAttribute(n, "class") + "']";
                res = "/" + n.Name + id + className
                    + res;
                n = n.ParentNode;
            }

            return res;
        }

        /// <summary>
        /// 返回父节点的 XPath
        /// </summary>
        /// <param name="xpath"></param>
        /// <param name="generations"></param>
        /// <returns></returns>
        public static string GetParentXPath(string xpath, int generations = 1)
        {
            while (generations-- > 0)
            {
                int slash = xpath.LastIndexOf('/');
                if (slash < 0) return "";
                xpath = xpath.Substring(0, slash);
            }
            return xpath;
        }

        /// <summary>
        /// 返回 XPath 中当前节点的标签名称
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        private static string getLastTagName(string xpath)
        {
            xpath = xpath.Substring(xpath.LastIndexOf('/') + 1);
            if (xpath.Contains('[')) xpath = xpath.Substring(0, xpath.IndexOf('['));
            return xpath;
        }

        /// <summary>
        /// 寻找两个 XPath 的共同祖先
        /// </summary>
        /// <param name="xpatha"></param>
        /// <param name="xpathb"></param>
        /// <param name="checkTagNamesOnly"></param>
        /// <returns></returns>
        public static string GetXPathCommonAnscendents(string xpatha, string xpathb, bool checkTagNamesOnly = false)
        {
            if (xpatha == null) return xpathb;
            if (xpathb == null) return xpatha;

            int ca = TextStatisticsUtility.CountOccurance(xpatha, '/'),
                cb = TextStatisticsUtility.CountOccurance(xpathb, '/');
            if (ca > cb) xpatha = GetParentXPath(xpatha, ca - cb);
            else xpathb = GetParentXPath(xpathb, cb - ca);

            while ((!checkTagNamesOnly && xpatha != xpathb) ||
                (checkTagNamesOnly && getLastTagName(xpatha) != getLastTagName(xpathb)))
            {
                xpatha = GetParentXPath(xpatha);
                xpathb = GetParentXPath(xpathb);
            }
            return xpatha;
        }

        /// <summary>
        /// 返回一组 XPath 中大多数节点的共同祖先
        /// </summary>
        /// <param name="xpaths"></param>
        /// <returns></returns>
        public static string GetXPathMostCommonAnscendent(params string[] xpaths)
        {
            xpaths = xpaths.Where(x => x != null).ToArray();
            KeyCounter<string> kc = new KeyCounter<string>();
            foreach (string a in xpaths)
                foreach (string b in xpaths)
                {
                    //if (a == b) continue;
                    kc[GetXPathCommonAnscendents(a, b)]++;
                }
            return kc.ArgMax;
        }

        /// <summary>
        /// 返回相对的 XPath
        /// </summary>
        /// <param name="xpath"></param>
        /// <param name="relto"></param>
        /// <param name="ignoreSharps"></param>
        /// <param name="checkTagNamesOnly"></param>
        /// <returns></returns>
        public static string GetRelativeXPath(string xpath, string relto, bool ignoreSharps = true, bool checkTagNamesOnly = false)
        {
            if (xpath == null) return null;
            string commonAnscendent = GetXPathCommonAnscendents(xpath, relto, checkTagNamesOnly);
            string result = "";
            while (relto != commonAnscendent)
            {
                result += "../";
                relto = GetParentXPath(relto);
            }
            result += xpath.Substring(commonAnscendent.Length);

            if (ignoreSharps && result.Contains('#'))
                result = result.Substring(0, result.IndexOf('#'));

            if (result.EndsWith("/")) result = result.Substring(0, result.Length - 1);

            return result;
        }

        /// <summary>
        /// 对相对 XPath 进行展开
        /// </summary>
        /// <param name="segs"></param>
        /// <returns></returns>
        public static string CombineXPaths(params string[] segs)
        {
            string s = "";
            foreach (string seg in segs)
            {
                string sgm = seg;
                while (sgm.StartsWith("../"))
                {
                    s = GetParentXPath(s);
                    sgm = sgm.Substring(3);
                }
                s = s + sgm;
            }
            s = s.Replace("//", "/");
            return s;
        }

        /// <summary>
        /// 对相对路径进行展开
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static string ExpandRelativePath(string baseUrl, string relativePath)
        {
            relativePath = System.Web.HttpUtility.HtmlDecode(relativePath);
            baseUrl = System.Web.HttpUtility.HtmlDecode(baseUrl);
            if (string.IsNullOrWhiteSpace(relativePath)) return baseUrl;
            if (relativePath.StartsWith("http://")) return relativePath;
            return new Uri(new Uri(baseUrl), relativePath).ToString();
        }

        /// <summary>
        /// 标准化Url
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="BaseUrl"></param>
        /// <returns></returns>
        public static string AbsoluteUrl(string Url, string BaseUrl, bool caseSensitive = false)
        {
            if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(BaseUrl)) return Url;
            try
            {
                string NewUrl = ExpandRelativePath(HttpUtility.UrlDecode(BaseUrl), HttpUtility.UrlDecode(Url));
                if (caseSensitive)
                {
                    NewUrl = NewUrl.Replace("&amp;", "&");
                }
                else
                {
                    NewUrl = NewUrl.ToLower().Replace("&amp;", "&");
                }


                //todo:处理Redirect
                return NewUrl;
            }
            catch
            {
                return Url;
            }
        }

        /// <summary>
        /// 安全获取某个 XPath 的节点，如果出错或不存在，则返回 anyNode
        /// </summary>
        /// <param name="anyNode"></param>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public static HtmlNode GetSafeHtmlNode(HtmlNode anyNode, string xpath)
        {
            if (xpath == null) return anyNode;
            if (xpath.Contains("#")) xpath = xpath.Substring(0, xpath.IndexOf('#'));
            if (xpath.EndsWith("/")) xpath = xpath.Substring(0, xpath.Length - 1);
            try
            {
                return anyNode.SelectSingleNode(xpath) ?? anyNode;
            }
            catch { }
            return anyNode;
        }

        /// <summary>
        /// 获取 Html 节点（有些网站直接从head开始写，脑残）
        /// </summary>
        /// <param name="HTML"></param>
        /// <param name="TryFixHTML">是否修正Html</param>
        /// <returns></returns>
        public static HtmlNode getSafeHtmlRootNode(string HTML, bool TryFixHTML = true)
        {
            HtmlDocument doc = new HtmlDocument();

            string _html = HTML;

            //We require a custom configuration
            var config = Configuration.Default;
            //Let's create a new parser using this configuration
            var parser = new HtmlParser(config);

            IDocument document = null;

            if (TryFixHTML)
            {
                try
                {
                    //加载DOM, 修正Html
                    document = parser.Parse(HTML);

                    if (document != null && !string.IsNullOrEmpty(document.DocumentElement.OuterHtml))
                        _html = document.DocumentElement.OuterHtml;
                }
                catch { }
            }

            try
            {
                doc.LoadHtml(_html);
            }
            catch (Exception ex)
            {
                return null;
            }

            //获取root节点（有些网站页面不带html标签的，直接从head开始写）
            HtmlNode rootNode = null;
            try
            {
                rootNode = doc.DocumentNode.SelectSingleNode("//html");
                if (rootNode == null)
                    rootNode = doc.DocumentNode;
            }
            catch
            {
                rootNode = doc.DocumentNode;
            }

            return rootNode;
        }

        /// <summary>
        /// 获取安全的HtmlNode，放弃原有的HtmlValidator
        /// </summary>
        /// <param name="HTML"></param>
        /// <param name="ignoreEx"></param>
        /// <param name="TryFixHTML"></param>
        /// <returns></returns>
        public static HtmlNode getSafeHtmlRootNode(string HTML, bool ignoreEx, bool TryFixHTML = true)
        {
            HtmlDocument doc = new HtmlDocument();

            string _html = HTML;

            //We require a custom configuration
            var config = Configuration.Default;
            //Let's create a new parser using this configuration
            var parser = new HtmlParser(config);

            IDocument document = null;

            if (TryFixHTML)
            {
                try
                {
                    //加载DOM, 修正Html
                    document = parser.Parse(HTML);

                    if (document != null && !string.IsNullOrEmpty(document.DocumentElement.OuterHtml))
                        _html = document.DocumentElement.OuterHtml;
                }
                catch { }
            }

            try
            {
                doc.LoadHtml(_html);
            }
            catch (Exception ex)
            {
                if (ignoreEx)
                {
                    return null;
                }
                else
                    throw ex;
            }

            //获取root节点（有些网站页面不带html标签的，直接从head开始写）
            HtmlNode rootNode = null;
            try
            {
                rootNode = doc.DocumentNode.SelectSingleNode("//html");
                if (rootNode == null)
                    rootNode = doc.DocumentNode;
            }
            catch
            {
                rootNode = doc.DocumentNode;
            }

            return rootNode;
        }

        private static Random rnd = new Random();

        /// <summary>
        /// 在 0 ~ count-1 范围内获取至多 maxCount 个下标以进行抽样，当 count 小于等于 maxCount 时则返回全部 
        /// </summary>
        /// <param name="count"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        public static IEnumerable<int> GenerateSampleIndicies(int count, int maxCount)
        {
            if (count <= maxCount)
            {
                for (int i = 0; i < count; ++i)
                    yield return i;
            }
            else
            {
                int[] indicies = GenerateSampleIndicies(count, count).ToArray();
                for (; maxCount > 0; maxCount--)
                {
                    int idx = rnd.Next(count);
                    yield return indicies[idx];
                    indicies[idx] = indicies[count - 1];
                }
            }
        }

        public static int SafeIntParse(string p)
        {
            Regex digits = new Regex(@"\d+", RegexOptions.Compiled);
            var m = digits.Match(p);
            if (m != null && !string.IsNullOrEmpty(m.Value))
                return int.Parse(m.Value);
            return 0;
        }
    }
}
