using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Thrinax.Utility
{
    public class HTMLCleaner
    {
        /// <summary>
        /// 去除标签、处理换行等格式字符
        /// </summary>
        /// <param name="HTML">待清洗的HTML</param>
        /// <param name="CleanMedia">是否清洗掉Img标签和视频标签</param>
        /// <returns>清洗出来的文本</returns>
        public static string CleanHTML(string HTML, bool CleanMedia)
        {
            if (string.IsNullOrEmpty(HTML)) return null;

            //去掉HTML标签
            HTML = StripHtml(HTML, CleanMedia);
            HTML = RestoreReplacement(HTML);
            HTML = CleanSpaces(HTML);

            return HTML;
        }

        #region 几种清洗方式

        /// <summary>
        /// 从media字段中获取转载媒体的清洗方式。以TOP20总结的规则
        /// </summary>
        /// <param name="MediaName">带有转载媒体的字段</param>
        /// <returns></returns>
        public static string CleanMediaName(string MediaName)
        {
            MediaName = TextCleaner.FullClean(MediaName);
            if (MediaName.Contains("来源")) MediaName = MediaName.Substring(MediaName.IndexOf("来源") + 3);
            while (!string.IsNullOrWhiteSpace(MediaName) && MediaName.StartsWith(" ")) MediaName = MediaName.Substring(1);
            if (MediaName.Contains(" ")) MediaName = MediaName.Substring(0, MediaName.IndexOf(' '));
            MediaName = MediaName.Replace(")", "").Replace("）", "");
            if (!string.IsNullOrWhiteSpace(MediaName))
                return MediaName;
            else return null;
        }

        /// <summary>
        /// 从media字段中获取作者的清洗方式。以TOP20总结的规则
        /// </summary>
        /// <param name="Author">带有作者的字段</param>
        /// <returns></returns>
        public static string CleanAuthor(string Author)
        {
            Author = TextCleaner.FullClean(Author);
            if (Author.Contains("作者")) Author = Author.Substring(Author.IndexOf("作者") + 3);
            if (Author.Contains("来源")) Author = Author.Substring(0, Author.IndexOf("来源"));
            if (Author.Contains("发布时间")) Author = Author.Substring(0, Author.IndexOf("发布时间"));

            if (!string.IsNullOrWhiteSpace(Author))
                return Author;
            else return null;
        }


        /// <summary>
        /// 对文章内容的清洗。依据TOP20总结出的，较为广泛适用的规则
        /// </summary>
        /// <param name="nodes">通过ItemContentXPath选出的nodes</param>
        /// <param name="Url">该url，用于FormatHtml函数以整理文章格式</param>
        /// <param name="Format">是否运用FormatHtml来进行文章格式的整理。若否，则在后期会清洗掉p、br等标签</param>
        /// <returns></returns>
        public static string CleanContent(HtmlNodeCollection nodes, string Url, bool Format = true)
        {
            string Content = string.Empty;
            foreach (HtmlNode cnode in nodes)
            {
                string temp = HtmlFormattor.FormatHtml(cnode.InnerHtml, Url);
                temp = CleanContent_CleanEditor(temp);
                temp = CleanContent_CleanA(temp);
                if (!Format) temp = TextCleaner.FullClean(temp);
                Content += temp;
            }
            return Content;
        }
        public static string CleanContent(List<HtmlNode> nodes, string Url)
        {
            string Content = string.Empty;
            foreach (HtmlNode cnode in nodes)
            {
                string temp = HtmlFormattor.FormatHtml(cnode.InnerHtml, Url);
                Content += temp;
            }
            return Content;
        }

        /// <summary>
        /// 对文章内容的清洗，可以洗掉“编辑：XXX”类的字段
        /// </summary>
        /// <param name="Content">文章内容</param>
        /// <returns></returns>
        internal static string CleanContent_CleanEditor(string Content, string Title = "空")
        {
            if (string.IsNullOrWhiteSpace(Content)) return Content;
            string resultContent = string.Empty;
            string temp = Content;
            string[] WordstoClean = @"编辑 记者 译者 报道 返回 拨打 公众号".Split();
            if (Title.Length > 5)
            {
                temp = temp.Substring(0, Math.Min(Title.Length * 5, temp.Length));
                if (temp.Contains(Title))
                    Content = Content.Substring(0, Math.Min(Title.Length * 5, temp.Length)).Replace(Title, "") + Content.Substring(Math.Min(Title.Length * 5, temp.Length));
            }
            temp = Content;
            while (Content.Length > 0)
            {
                char[] leftb = new char[1];
                leftb[0] = '('; 
                //leftb[1] = '（';
                char[] rightb = new char[1]; 
                rightb[0] = ')';
                //rightb[1] = '）';
                int indexleftb = Content.IndexOfAny(leftb);
                int indexrightb = Content.IndexOfAny(rightb);

                //如果找不到其中一边的括号则直接交付退出
                if (Math.Min(indexleftb, indexrightb) < 0)
                { resultContent = resultContent + Content; Content = string.Empty; continue; }

                //如果很不规则地先出现了右括号或两个括号连在一起，则无视它放过它
                if (indexrightb < indexleftb + 2)
                { resultContent = resultContent + Content.Substring(0, indexrightb + 1); Content = Content.Substring(indexrightb + 1); continue; }

                if (indexrightb > indexleftb + 1)
                {
                    try
                    {
                        string mid = Content.Substring(indexleftb + 1, indexrightb - indexleftb - 1); bool toclean = false;

                        foreach (string test in WordstoClean)
                            if (mid.Contains(test))
                                toclean = true;
                        if (toclean)
                        {
                            resultContent = indexleftb > 0 ? resultContent + Content.Substring(0, indexleftb - 1) : resultContent;
                            Content = Content.Substring(indexrightb + 1);
                        }
                        else
                        {
                            resultContent = resultContent + Content.Substring(0, indexrightb + 1);
                            Content = Content.Substring(indexrightb + 1);
                        }
                    }
                    catch { }
                }
            }

            if (Content.Length < 50)
                foreach (string text in WordstoClean)
                    if (Content.Contains(text))
                        Content = string.Empty;

            resultContent = resultContent + Content;

            return resultContent;
        }

        /// <summary>
        /// 对文章内容的清洗，可洗掉“返回首页”类的a标签
        /// </summary>
        /// <param name="Content">文章内容</param>
        /// <returns></returns>
        internal static string CleanContent_CleanA(string Content)
        {
            if (string.IsNullOrWhiteSpace(Content)) return Content;
            string resultContent = string.Empty;
            string[] WordstoClean = @"编辑 记者 译者 热线 报道 返回 拨打 公众号 打印 首页 字体".Split();

            //这是清洗的主体部分
            while (Content.Length > 0)
            {
                int aleft = Content.IndexOf("<a");
                int aright = Content.IndexOf("</a>");

                if (Math.Min(aleft, aright) < 0)//如果只有一个或两个都没有那就无视之，全部扔到结果去
                { resultContent = resultContent + Content; Content = string.Empty; continue; }

                if (aright < aleft + 4)
                {//如果这个a标签是空的那就不点它了
                    resultContent = resultContent + Content.Substring(0, aright + 4);
                    Content = Content.Substring(aright + 4);
                    continue;
                }

                if (aright > aleft + 3)
                {
                    try
                    {
                        string mid = Content.Substring(aleft + 3, aright - aleft - 3); bool toclean = false;
                        foreach (string stopword in WordstoClean)
                            if (mid.Contains(stopword))
                            { toclean = true; break; }//检查是否需要清晰这个a标签
                        if (toclean)
                        {
                            resultContent = resultContent + Content.Substring(0, aleft);
                            Content = Content.Substring(aright + 4);
                            continue;
                        }
                        else
                        {
                            resultContent = resultContent + Content.Substring(0, aright + 4);
                            Content = Content.Substring(aright + 4);
                            continue;
                        }
                    }
                    catch { }
                }
            }
            return resultContent;
        }

        #endregion 几种清洗方式

        /// <summary>
        /// 去除处理换行等格式字符
        /// </summary>
        /// <param name="HTML"></param>
        /// <returns></returns>
        public static string CleanSpaces(string HTML)
        {
            //去掉\n \t \r &nbsp; 合并连续的空格
            HTML = nRegex.Replace(HTML,"");
            HTML = trimRegex.Replace(HTML, " ");
            HTML = wRegex.Replace(HTML,  " ");
            HTML = sRegex.Replace(HTML, " ");

            return HTML.Trim();
        }
        private static Regex nRegex = new Regex(@"\n", RegexOptions.Compiled);
        private static Regex trimRegex = new Regex(@"\t|\r|&nbsp;|&nbsp", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex wRegex = new Regex(@"&\w+;", RegexOptions.Compiled);
        private static Regex sRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static Regex nbspRegex = new Regex(@"&nbsp;", RegexOptions.Compiled);
        private static Regex gtRegex = new Regex(@"&gt;", RegexOptions.Compiled);
        private static Regex ltRegex = new Regex(@"&lt;", RegexOptions.Compiled);
        private static Regex ampRegex = new Regex(@"&amp;", RegexOptions.Compiled);
        private static Regex quotRegex = new Regex(@"&quot;", RegexOptions.Compiled);

        public static string RestoreReplacement(string HTML)
        {
            //将所有代换符还原
            HTML = nbspRegex.Replace(HTML, " ");
            HTML = gtRegex.Replace(HTML, ">");
            HTML = ltRegex.Replace(HTML,  "<");
            HTML = ampRegex.Replace(HTML,  "&");
            HTML = quotRegex.Replace(HTML, "\"");

            return HTML;
        }

        private static Regex removeImgRegex = new Regex(@"<(?:.|\n)+?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static Regex retainImgRegex = new Regex(@"<(?!\s*(img|embed|/embed|iframe|/iframe))(?:.|\n)+?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// 仅去除HTML标签
        /// </summary>
        /// <remarks>
        /// Stripping HTML
        /// http://www.4guysfromrolla.com/webtech/042501-1.shtml
        ///
        /// Using regex to find tags without a trailing slash
        /// http://concepts.waetech.com/unclosed_tags/index.cfm
        ///
        /// http://msdn.microsoft.com/library/en-us/script56/html/js56jsgrpregexpsyntax.asp
        /// </remarks>
        public static string StripHtml(string HTML, bool CleanImg)
        {
            string styleless = StripScriptAndStyle(HTML);

            //Strips the HTML tags from the Html
            
            Regex objRegExp = 
                CleanImg ? removeImgRegex : retainImgRegex;

            //Replace all HTML tag matches with the empty string
            string strOutput = objRegExp.Replace(styleless, string.Empty);

            //Replace all < and > with &lt; and &gt;
            //strOutput = strOutput.Replace("<", "&lt;");
            //strOutput = strOutput.Replace(">", "&gt;");

            objRegExp = null;
            return strOutput;
        }
        private static Regex scriptsRegex = new Regex(@"<script[^>.]*>[\s\S]*?</script>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static Regex stylesRegex = new Regex(@"<style[^>.]*>[\s\S]*?</style>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static Regex inlineJavascriptRegex = new Regex(@"javascript:.*?""", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        /// <summary>
        /// 去除脚本和样式
        /// </summary>
        /// <param name="HTML"></param>
        /// <returns></returns>
        public static string StripScriptAndStyle(string HTML)
        {
            //Stripts the <script> tags from the Html
            string cleanedText = scriptsRegex.Replace(HTML, string.Empty);

            //Stripts the <style> tags from the Html
            cleanedText = stylesRegex.Replace(cleanedText, string.Empty);
            cleanedText = inlineJavascriptRegex.Replace(cleanedText, string.Empty);
            return cleanedText;
        }
        private static ConcurrentDictionary<string,Regex> removeTagRegDictionary =new ConcurrentDictionary<string, Regex>();
        private static ConcurrentDictionary<string, Regex> removeInnerRegDictionary = new ConcurrentDictionary<string, Regex>(); 
        /// <summary>
        /// 去除指定的Html标签
        /// </summary>
        /// <param name="HTML"></param>
        /// <param name="Tag"></param>
        /// <param name="RemoveInnerHTML">是否去除内部内容</param>
        /// <returns></returns>
        public static string StripHtmlTag(string HTML, string Tag, bool RemoveInnerHTML)
        {
            if (string.IsNullOrEmpty(Tag) || string.IsNullOrEmpty(HTML)) return HTML;
            Regex replacereg;
            if (RemoveInnerHTML)
            {
                if (!removeInnerRegDictionary.ContainsKey(Tag))
                {
                    string regexStr = string.Format(@"<\s*{0}[^>]*?>[\s\S]*?</\s*{0}\s*>", Tag);
                    Regex regex = new Regex(regexStr,RegexOptions.IgnoreCase|RegexOptions.Compiled);
                    removeInnerRegDictionary.TryAdd(Tag,regex);
                }
                replacereg = removeInnerRegDictionary[Tag];
            }
            else
            {
                if (!removeTagRegDictionary.ContainsKey(Tag))
                {
                    string regexStr = string.Format(@"<(?:/)?\s*{0}[^>]*?>", Tag);
                    Regex regex = new Regex(regexStr,RegexOptions.IgnoreCase|RegexOptions.Compiled);
                    removeTagRegDictionary.TryAdd(Tag, regex);
                }
                replacereg = removeTagRegDictionary[Tag];
            }


            return replacereg.Replace(HTML, string.Empty);
        }

        /// <summary>
        /// 清除Html标签中的属性
        /// </summary>
        /// <param name="HTML"></param>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static string StripHtmlProperty(string HTML, string Tag)
        {
            if (string.IsNullOrEmpty(Tag) || string.IsNullOrEmpty(HTML)) return HTML;
            Regex replacereg;

            if (!removeInnerRegDictionary.ContainsKey(Tag))
            {
                string regexStr = string.Format(@"\s*{0}\s*=""[^""]*?""\s*", Tag);
                Regex regex = new Regex(regexStr, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                removeInnerRegDictionary.TryAdd(Tag, regex);
            }
            replacereg = removeInnerRegDictionary[Tag];

            return replacereg.Replace(HTML, " ");
        }

        /// <summary>
        /// 获取HTML的Title标签内容
        /// </summary>
        /// <param name="HTML"></param>
        /// <returns></returns>
        public static string GetTitle(string HTML)
        {
            HtmlDocument Document = new HtmlDocument();
            Document.LoadHtml(HTML);

            HtmlNode TitleNode = Document.DocumentNode.SelectSingleNode("//title");
            if (TitleNode != null)
                return TitleNode.InnerText;
            else
                return string.Empty;
        }

        /// <summary>
        /// 去除前和后的最长相同子串
        /// </summary>
        /// <param name="Str">待清洗字符串</param>
        /// <param name="CompareStr">供比较字符串</param>
        /// <returns></returns>
        public static string StringCompareClean(string Str, string CompareStr)
        {
            if (string.IsNullOrEmpty(Str) || string.IsNullOrEmpty(CompareStr)) return Str;
            
            string sStr = Str.ToLower();
            CompareStr = CompareStr.ToLower();

            int Start = 0;
            int End = 0; 

            //从前开始数相同字符的个数
            try
            {
                while (Start < sStr.Length & Start < CompareStr.Length)
                    if (sStr[Start] == CompareStr[Start])
                        Start++;
                    else
                        break;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(string.Format("Str={0} CompareStr={1} Start={2} ex={3}", Str, CompareStr, Start, ex.Message));
            }

            //待清洗字符串是比较字符的前缀，则不清洗，保持原样输出
            if (Start == sStr.Length)
            {
                return Str;
            }
            else
            {
                //从后开始数相同字符的个数
                while (End < sStr.Length & End < CompareStr.Length & sStr[sStr.Length - End - 1] == CompareStr[CompareStr.Length - End - 1])
                    End++;

                return Str.Substring(Start, Str.Length - Start - End);
            }
        }

        /// <summary>
        /// 从HTML中清除掉和CompareHTML相同的部分，输出PlainText
        /// </summary>
        /// <param name="HTML"></param>
        /// <param name="CompareHTML"></param>
        /// <param name="Title"></param>
        /// <returns></returns>
        public static void DOMCompareClean(string OriHTML, string CompareHTML, bool CleanText, ref string Title, ref string HTML)
        {
            ///创建HtmlDocument对象
            HtmlDocument Document = new HtmlDocument();
            Document.LoadHtml(OriHTML);
            HtmlDocument CompareDocument = new HtmlDocument();
            CompareDocument.LoadHtml(CompareHTML);

            ///清除相同节点
            DOMCompareClean(Document.DocumentNode, CompareDocument.DocumentNode);

            ///返回剩余叶子节点的文本
            StringBuilder Builder = new StringBuilder(500);
            if (CleanText)
                AppendLeavesText(Builder, Document.DocumentNode);
            else
                AppendLeavesHTML(Builder, Document.DocumentNode, (int)Math.Truncate(TextLengh(Document.DocumentNode)*0.6));
            HTML = Builder.ToString();

            ///返回
            Title = StringCompareClean( GetTitle(HTML), GetTitle(CompareHTML));
            if (CleanText)
                Title = CleanHTML(Title, true);

        }

        /// <summary>
        /// 递归函数：将一个节点和另一个节点比较，删除相同的子节点（用于DOMCompareClean）
        /// </summary>
        private static void DOMCompareClean(HtmlNode Node, HtmlNode CompareNode)
        {
            ///遍历HTML的每一个子节点
            for (int SubNode = 0; Node.HasChildNodes && SubNode < Node.ChildNodes.Count;)
            {
                ///标记是否删除此节点
                bool CutIt = false;
               
                ///对于JS、CSS和备注直接删除
                if (string.Compare(Node.ChildNodes[SubNode].Name, "style", true) == 0
                    || string.Compare(Node.ChildNodes[SubNode].Name, "script", true) == 0
                    || string.Compare(Node.ChildNodes[SubNode].Name, "meta", true) == 0
                    || Node.ChildNodes[SubNode].NodeType == HtmlNodeType.Comment)
                    ///删除此节点
                    CutIt = true;
                else
                    ///遍历CompareHTML的每一个子节点
                    for (int SubCompareNode = 0; CompareNode.HasChildNodes && SubCompareNode < CompareNode.ChildNodes.Count; SubCompareNode++)
                        ///对每一对同名且同Id的子元素，都进行比较
                        ///包含名称或Id不存在的情况
                        ///对于以文章编号命名的元素（id长度较大），忽略id的不一致，仍然进行比较
                        if (string.Compare(Node.ChildNodes[SubNode].Name, CompareNode.ChildNodes[SubCompareNode].Name, true) == 0
                            && (Node.ChildNodes[SubNode].Id != null && Node.ChildNodes[SubNode].Id.Length > 6
                                || string.Compare(Node.ChildNodes[SubNode].Id, CompareNode.ChildNodes[SubCompareNode].Id, true) == 0))
                        {
                            switch (Node.ChildNodes[SubNode].NodeType)
                            {
                                ///文本节点直接比较不递归
                                case HtmlNodeType.Comment:
                                case HtmlNodeType.Text:
                                    string CleanInnerText = CleanSpaces(Node.ChildNodes[SubNode].InnerText);
                                    ///如果文本相同，则删除
                                    if (string.IsNullOrEmpty(CleanInnerText)
                                        || string.Compare(CleanInnerText, CleanSpaces(CompareNode.ChildNodes[SubCompareNode].InnerText), true) == 0)
                                        ///删除此节点
                                        CutIt = true;
                                    break;
                                ///非文本节点需要递归
                                default:
                                    ///如果没有子节点，空标签,则删除
                                    if (!Node.ChildNodes[SubNode].HasChildNodes)
                                        CutIt = true;
                                    ///否则递归
                                    else
                                    {
                                        DOMCompareClean(Node.ChildNodes[SubNode], CompareNode.ChildNodes[SubCompareNode]);
                                        ///如果所有子节点都cutted则母节点也应cutted
                                        if (!Node.ChildNodes[SubNode].HasChildNodes || Node.ChildNodes[SubNode].ChildNodes.Count == 0)
                                            CutIt = true;
                                    }
                                    break;

                            }
                            ///如果已经删除，则不需要再检查CompareHTML的其他节点
                            if (CutIt) break;
                        }

                ///删掉这个节点
                if (CutIt)
                {
                    Node.ChildNodes[SubNode].RemoveAllChildren();
                    Node.RemoveChild(Node.ChildNodes[SubNode]);
                }
                else
                    SubNode++;
            }
        }

        /// <summary>
        /// 递归函数：将该节点下的叶子节点的文本添加到StringBuilder后（用于DOMCompareClean）
        /// </summary>
        /// <param name="Builder"></param>
        /// <param name="Node"></param>
        private static void AppendLeavesText(StringBuilder Builder, HtmlNode Node)
        {
            if (Node.HasChildNodes)
            {
                foreach (HtmlNode SubNode in Node.ChildNodes)
                    AppendLeavesText(Builder, SubNode);
            }
            else
                Builder.Append(CleanSpaces(Node.InnerText)).Append(" ");
        }

        /// <summary>
        /// 递归函数：找到包含指定长度以上有效文本的最远节点，则将其HTML添加到StringBuilder后（用于DOMCompareClean）
        /// </summary>
        /// <param name="Builder"></param>
        /// <param name="Node"></param>
        /// <returns>是否找到符合要求的节点并加入</returns>
        private static bool AppendLeavesHTML(StringBuilder Builder, HtmlNode Node, int Length)
        {
            if (TextLengh(Node) < Length) return false;

            if (Node.HasChildNodes)
            {
                foreach (HtmlNode SubNode in Node.ChildNodes)
                    if (AppendLeavesHTML(Builder, SubNode, Length))
                        return true;
            }

            Builder.Append((string) Node.InnerHtml);
            return true;
        }

        /// <summary>
        /// 递归函数：计算节点下的有效文本长度（用于AppendLeavesHTML）
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        private static int TextLengh(HtmlNode Node)
        {
            int Length = 0;
            if (Node.HasChildNodes)
            {
                foreach (HtmlNode SubNode in Node.ChildNodes)
                    Length += TextLengh(SubNode);
            }
            else
                Length = Node.InnerText.Length;
            
            return Length;
        }

        /// <summary>
        /// 检查Url连接是否正确有效
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public static bool isUrlGood(string Url)
        {
            return !(Url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) || Url.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase)
                || Url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || Url.StartsWith("#")
                || Url.StartsWith("thunder:", StringComparison.OrdinalIgnoreCase)
                || Url.StartsWith("file:", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 清理链接，去掉# js 锚点等等
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public static string CleanUrl(string Url)
        {
            if (string.IsNullOrEmpty(Url)) return null;

            Url = Url.Trim();
            if (!isUrlGood(Url)) return null;

            //去掉所有锚点标记
            Url = Regex.Replace(Url, @"#\w+", string.Empty);

            //去掉末尾的"/"
            if (Url.Length > 1)
                Url.TrimEnd('/');
            return Url;
        }

        /// <summary>
        /// HtmlAgilityPack的InnerText属性会包含Script等，要清除掉才是真正的InnerText
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static string GetCleanInnerText(HtmlNode Node)
        {
            try
            {
                foreach (var script in Node.Descendants("script"))
                    script.Remove();
                foreach (var style in Node.Descendants("style"))
                    style.Remove();
            }
            catch { }
            HtmlNodeCollection comments = Node.SelectNodes("//comment()");
            if (comments != null)
                foreach (var comment in comments)
                    comment.Remove();

            return Node.InnerText;
        }
    }
}
