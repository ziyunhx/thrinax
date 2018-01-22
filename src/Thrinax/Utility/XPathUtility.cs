using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thrinax.Utility
{
    public class XPathUtility
    {
        public static String GetCommonPrefix(String source, String target)
        {
            Int32 minLength = Math.Min(source.Length, target.Length);
            int i = 0;
            for (; i < minLength; i++)
            {
                if (source[i] != target[i])
                {
                    break;
                }
            }
            return i > 0 ? source.Substring(0, i - 1) : String.Empty;
        }

        public static String GetLCAXPath(IEnumerable<String> xpaths)
        {
            if (xpaths == null || xpaths.Count() == 0)
            {
                return String.Empty;
            }

            String bench = xpaths.First();
            Int32 minLength = xpaths.Min(o => o.Length);
            int i = 0;
            for (; i < minLength; i++)
            {
                Boolean flag = false;
                foreach (String s in xpaths)
                {
                    if (s[i] != bench[i])
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    break;
                }
            }

            if (i == 0)
            {
                return String.Empty;
            }

            String raw = bench.Substring(0, i - 1);
            return raw.Substring(0, raw.LastIndexOf('/'));
        }

        public static String GetLCAXPath(HtmlNodeCollection nodes)
        {
            List<String> xpaths = new List<string>();
            foreach (HtmlNode node in nodes)
            {
                xpaths.Add(node.XPath);
            }

            return GetLCAXPath(xpaths);
        }

        /// <summary>
        /// 递归提取Node的短XPath(通过ID,Class,或元素数量限定)，这是带一个Dic的多次提取方法，还有另一个版本的单次提取方法
        /// </summary>
        /// <param name="Node">要提取XPath的节点</param>
        /// <param name="RootNode">页面根节点</param>
        /// <param name="ShortXPaths">该页面已缓存过的短路径，用于加速节点查询</param>
        /// <param name="AddToCache">是否将结果加入ShortXPaths缓存数组</param>
        /// <param name="IgnoreOrderNumber_UpLevel">到ItemRoot的最长层级</param>
        /// <param name="PatternMinCountItem">最少多少Item才是合法的</param>
        /// <returns></returns>
        public static string ExtracShortXPath_WithIDClassCount(HtmlNode Node, HtmlNode RootNode, ref Dictionary<HtmlNode, string> ShortXPaths, int IgnoreOrderNumber_UpLevel, bool UsingClassNameOrCount = false, bool AddToCache = true, int PatternMinCountItem = 5)
        {
            if (Node == null) return null;
            //缓存
            if (ShortXPaths != null && ShortXPaths.ContainsKey(Node))
                return ShortXPaths[Node];

            string Shortest = null;
            if (isTopNode(Node))
                //顶级节点处理
                Shortest = Node.XPath;
            else
            {
                //第一招，如果有ID且全局唯一，则可以缩短
                if (!string.IsNullOrWhiteSpace(Node.Id)
                    && RootNode.SelectNodes(string.Format("//{0}[@id=\"{1}\"]", Node.Name, Node.Id)).Count == 1
                    //且ID不是随机数
                    && !isID_RandomGUID(Node,RootNode))
                {
                    string Name_NoNumber;
                    //正常ID命名，直接拼出Path
                    if (!isID_SeqNumber(Node, RootNode, out Name_NoNumber))
                        Shortest = string.Format("//{0}[@id=\"{1}\"]", Node.Name, Node.Id);
                    else
                    //带数字序列的ID命名，要特殊处理
                    {
                        string GroupPath = string.Format("//{0}[contains(@id,'{1}')]", Node.Name, Name_NoNumber);
                        HtmlNodeCollection Siblings = RootNode.SelectNodes(GroupPath);
                    
                        //找到兄弟中的序号
                        int Index = 0;
                        for (; Index < Siblings.Count; Index++)
                            if (Siblings[Index] == Node) break;

                        Shortest = string.Format("{0}[{1}]", GroupPath, Index + 1);
                    }
                }
                //第二招，如果有class且唯一
                else if (UsingClassNameOrCount && Node.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(Node.Attributes["class"].Value))
                {
                    //如果祖先可以用id搞定的，就不要class啦
                    string ParentNoClassPath = ExtracShortXPath_WithIDClassCount(Node.ParentNode, RootNode, ref ShortXPaths, IgnoreOrderNumber_UpLevel, false, false, PatternMinCountItem);
                    if (ParentNoClassPath.Contains("@id"))
                        Shortest = ParentNoClassPath + Node.XPath.Substring(Node.XPath.LastIndexOf('/'));

                    //如果这个classname是全局或局部唯一（即从ItemBaseRoot向下），应是更好的path，则覆盖Shortest
                    string ClassPath = null;
                    foreach (string className in Node.Attributes["class"].Value.Split())
                    {
                        ClassPath = string.Format(@"//{0}[contains(@class,'{1}')]", Node.Name, className);
                        if (RootNode.SelectNodes(ClassPath) == null) continue;

                        if (RootNode.SelectNodes(ClassPath).Count == 1)
                        {
                            //全局唯一，就它了，跳出
                            Shortest = ClassPath;
                            break;
                        }
                        else //局部唯一只要检查上一级下面是否是唯一的即可（再往上就不大可能唯一了）
                            if (string.IsNullOrEmpty(Shortest) && Node.ParentNode.SelectNodes('.' + ClassPath.Substring(1)).Count == 1)
                            {
                                Shortest = ExtracShortXPath_WithIDClassCount(Node.ParentNode, RootNode, ref ShortXPaths, IgnoreOrderNumber_UpLevel, UsingClassNameOrCount, AddToCache, PatternMinCountItem) + ClassPath.Substring(1); //本级只保留一个"/"
                                //break;//不跳出，继续循环其他classname，万一其他的是全局唯一
                            }
                    }
                }

                //前两招都失败
                if (string.IsNullOrWhiteSpace(Shortest))
                {
                    //递归到上一级，本级以序号标识
                    string LongXPath = Node.XPath;
                    Shortest = ExtracShortXPath_WithIDClassCount(Node.ParentNode, RootNode, ref ShortXPaths, IgnoreOrderNumber_UpLevel, UsingClassNameOrCount, AddToCache, PatternMinCountItem) + LongXPath.Substring(LongXPath.LastIndexOf('/'));

                    //第三招，通过本级元素数量来限定上级元素（如果递归全部都是序号标识，才被迫尝试这步，险棋）
                    if (UsingClassNameOrCount && !Shortest.Contains("@id") && !Shortest.Contains("@class") && !Shortest.Contains("last()"))
                    {
                        HtmlNode current = Node;
                        string Suffix = null;
                        for (int i = 0; i < IgnoreOrderNumber_UpLevel && !isTopNode(current); i++)
                        {
                            HtmlNodeCollection Siblings = Node.ParentNode.SelectNodes("./" + Node.Name);
                            int CountSibling = Siblings.Count;
                            if (CountSibling >= PatternMinCountItem)
                            {
                                //适当减少点数量限制，避免限制过死
                                int CountLimit = CountSibling;
                                if (CountLimit > 15) CountLimit--;
                                if (CountLimit > 5) CountLimit--;

                                //找到兄弟中的序号
                                int Index = 0;
                                for (; Index < Siblings.Count; Index++)
                                    if (Siblings[Index] == Node) break;
                                  
                                //是否能唯一定位Current所处的节点？(注意XPath的Index是从1开始的)
                                string NodeCountPath = string.Format("//{0}/{1}[last()>={2}][{3}]", Node.ParentNode.Name, Node.Name, CountLimit, Index + 1);
                                Suffix = i == 0 ? string.Empty : Shortest.Substring(LongXPath.LastIndexOf('/', Suffix.Length));
                                if (RootNode.SelectNodes(NodeCountPath).Count == 1)
                                {
                                    Shortest = NodeCountPath + Suffix;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //最短XPath存进Dic
            if (ShortXPaths != null && AddToCache) ShortXPaths[Node] = Shortest;
            return Shortest;
        }

        /// <summary>
        /// 递归提取Node的短XPath(通过ID,Class,或元素数量限定)，这是带一个Dic的多次提取方法，还有另一个版本的单次提取方法
        /// </summary>
        /// <param name="Node">要提取XPath的节点</param>
        /// <param name="RootNode">页面根节点</param>
        /// <param name="ShortXPaths">该页面已缓存过的短路径，用于加速节点查询</param>
        /// <param name="AddToCache">是否将结果加入ShortXPaths缓存数组</param>
        /// <param name="IgnoreOrderNumber_UpLevel">到ItemRoot的最长层级</param>
        /// <param name="PatternMinCountItem">最少多少Item才是合法的</param>
        /// <returns></returns>
        public static string GetRawXPath(HtmlNode Node, HtmlNode RootNode, int IgnoreOrderNumber_UpLevel, string URL, bool UsingClassNameOrCount = false, int PatternMinCountItem = 3)
        {

            if (Node == null) return null;
            string Shortest = null;
            if (isTopNode(Node))
                //顶级节点处理
                Shortest = Node.XPath;
            else
            {
                //第一招，如果有ID且全局唯一，则可以缩短
                if (!string.IsNullOrWhiteSpace(Node.Id)
                    && RootNode.SelectNodes(string.Format("//{0}[@id=\"{1}\"]", Node.Name, Node.Id)).Count == 1
                    //且ID不是随机数
                    && !isID_RandomGUID(Node, RootNode))
                {
                    string Name_NoNumber;
                    //正常ID命名，直接拼出Path
                    if (!isID_SeqNumber(Node, RootNode, out Name_NoNumber))
                        Shortest = string.Format("//{0}[@id=\"{1}\"]", Node.Name, Node.Id);
                    else
                    //带数字序列的ID命名，要特殊处理
                    {
                        string GroupPath = string.Format("//{0}[contains(@id,'{1}')]", Node.Name, Name_NoNumber);
                        HtmlNodeCollection Siblings = RootNode.SelectNodes(GroupPath);

                        //找到兄弟中的序号
                        int Index = 0;
                        for (; Index < Siblings.Count; Index++)
                            if (Siblings[Index] == Node) break;

                        Shortest = string.Format("{0}[{1}]", GroupPath, Index + 1);
                    }

                }
                //第二招，如果有class且唯一
                else if (UsingClassNameOrCount && Node.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(Node.Attributes["class"].Value))
                {
                    //如果祖先可以用id搞定的，就不要class啦
                    string ParentNoClassPath = GetrawXPath(Node.ParentNode, RootNode, IgnoreOrderNumber_UpLevel, URL, false, PatternMinCountItem);
                    if (ParentNoClassPath.Contains("@id"))
                        Shortest = ParentNoClassPath + Node.XPath.Substring(Node.XPath.LastIndexOf('/'));

                    //如果这个classname是全局或局部唯一（即从ItemBaseRoot向下），应是更好的path，则覆盖Shortest
                    string ClassPath = null;
                    foreach (string className in Node.Attributes["class"].Value.Split())
                    {

                        if (className.Contains("$")) 
                            continue;

                        //ClassPath = string.Format("//{0}[@class=\"{1}\"]", Node.Name, className);
                        ClassPath = string.Format(@"//{0}[contains(@class,'{1}')]", Node.Name, className);
                        if (RootNode.SelectNodes(ClassPath) == null) continue;

                        if (RootNode.SelectNodes(ClassPath).Count == 1)
                        {
                            //全局唯一，就它了，跳出

                            Shortest = ClassPath;
                            //ClassPath = string.Format(@"//{0}[contains(@class,'{1}')]", Node.Name, className);
                            //if (RootNode.SelectNodes(ClassPath).Count == 1) Shortest = ClassPath;
                            break;
                        }
                        else //局部唯一只要检查上一级下面是否是唯一的即可（再往上就不大可能唯一了）
                            if (string.IsNullOrEmpty(Shortest) && Node.ParentNode.SelectNodes('.' + ClassPath.Substring(1)).Count == 1)
                            {
                                Shortest = GetrawXPath(Node.ParentNode, RootNode, IgnoreOrderNumber_UpLevel, URL, false, PatternMinCountItem) + ClassPath.Substring(1); //本级只保留一个"/"
                                //break;//不跳出，继续循环其他classname，万一其他的是全局唯一
                            }
                    }

                }

                //前两招都失败
                if (string.IsNullOrWhiteSpace(Shortest))
                {
                    //递归到上一级，本级以序号标识
                    string LongXPath = Node.XPath;
                    Shortest = GetrawXPath(Node.ParentNode, RootNode, IgnoreOrderNumber_UpLevel, URL, UsingClassNameOrCount, PatternMinCountItem) + LongXPath.Substring(LongXPath.LastIndexOf('/'));

                    //第三招，通过本级元素数量来限定上级元素（如果递归全部都是序号标识，才被迫尝试这步，险棋）
                    if (UsingClassNameOrCount && !Shortest.Contains("@id") && !Shortest.Contains("@class") && !Shortest.Contains("last()"))
                    {
                        HtmlNode current = Node;
                        string Suffix = null;
                        for (int i = 0; i < IgnoreOrderNumber_UpLevel && !isTopNode(current); i++)
                        {
                            HtmlNodeCollection Siblings = Node.ParentNode.SelectNodes("./" + Node.Name);
                            int CountSibling = Siblings.Count;
                            if (CountSibling >= PatternMinCountItem)
                            {
                                //适当减少点数量限制，避免限制过死
                                int CountLimit = CountSibling;
                                if (CountLimit > 15) CountLimit--;
                                if (CountLimit > 5) CountLimit--;

                                //找到兄弟中的序号
                                int Index = 0;
                                for (; Index < Siblings.Count; Index++)
                                    if (Siblings[Index] == Node) break;

                                //是否能唯一定位Current所处的节点？(注意XPath的Index是从1开始的)
                                string NodeCountPath = string.Format("//{0}/{1}[last()>={2}][{3}]", Node.ParentNode.Name, Node.Name, CountLimit, Index + 1);
                                if ((!string.IsNullOrEmpty(Suffix) && (LongXPath.Length == Suffix.Length || Suffix.Length == Shortest.Length))   ) break;// || NodeCountPath.Contains("#")) break;//这里改得很大胆哦
                                Suffix = i == 0 ? string.Empty : Shortest.Substring(LongXPath.LastIndexOf('/', Suffix.Length));
                                if (NodeCountPath.Contains("#")) continue;
                                if (RootNode.SelectNodes(NodeCountPath).Count == 1)
                                {
                                    Shortest = NodeCountPath + Suffix;
                                    break;
                                }
                            }
                        }
                    }
                }
            }


            return Shortest;
        }
        
        public static string GetrawXPath(HtmlNode Node, HtmlNode RootNode, int IgnoreOrderNumber_UpLevel, string URL, bool UsingClassNameOrCount = false, int PatternMinCountItem = 3)
        {
            return GetRawXPath(Node, RootNode, IgnoreOrderNumber_UpLevel, URL, UsingClassNameOrCount, PatternMinCountItem);
        }


        /// <summary>
        /// 递归提取Node的短XPath(通过ID命名),如果要在同一个页面上多次提取，请使用另一个带Dic缓存的版本
        /// </summary>
        /// <param name="Node">要提取XPath的节点</param>
        /// <param name="RootNode">页面根节点</param>
        /// <param name="IgnoreOrderNumber_UpLevel">到ItemRoot的最长层级</param>
        /// <param name="PatternMinCountItem">最少多少Item才是合法的</param>
        /// <returns></returns>
        public static string ExtracShortXPath_WithIDClassCount(HtmlNode Node, HtmlNode RootNode, int IgnoreOrderNumber_UpLevel, bool UsingClassNameOrCount = false, int PatternMinCountItem = 5)
        {
            Dictionary<HtmlNode, string> ShortXPaths = null;
            return ExtracShortXPath_WithIDClassCount(Node, RootNode, ref ShortXPaths, IgnoreOrderNumber_UpLevel, UsingClassNameOrCount, true, PatternMinCountItem);
        }

        /// <summary>
        /// 是否ID形如“abcd_1234”编号，如是则应当忽略编号
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="RootNode"></param>
        /// <param name="Name_NoNumber">去除数字序列后的名称</param>
        /// <param name="MinCountItem">去掉数字编号以后至少多少个Item则认为是一组序列</param>
        /// <returns>true表示应该去掉后面的数字来提取真正的命名</returns>
        public static bool isID_SeqNumber(HtmlNode Node, HtmlNode RootNode, out string Name_NoNumber, int MinCountItem = 10)
        {
            Name_NoNumber = null;
            if (string.IsNullOrEmpty(Node.Id)) return false;

            List<string> Names_NoNumber = new List<string>();
            foreach (Match m in Regex.Matches(Node.Id, @"\d{3,}"))
            {
                //提取无数字序列的新名称(考虑数字序列在头尾的情况)
                Name_NoNumber = (m.Index + m.Length == Node.Id.Length) ? Node.Id.Substring(0, m.Index) : Node.Id.Substring(m.Index + m.Length);
                if (!Names_NoNumber.Contains(Name_NoNumber))
                {
                    //至少达到全局MinCountItem个
                    if (RootNode.SelectNodes(string.Format("//{0}[contains(@id,'{1}')]", Node.Name, Name_NoNumber)).Count < MinCountItem)
                        break;

                    Names_NoNumber.Add(Name_NoNumber);
                }
            }

            return Names_NoNumber.Count > 0;
        }

        /// <summary>
        /// 是否ID是由类似GUID的随机数构成的
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="RootNode"></param>
        /// <param name="MinCountItem">有多少兄弟是类似的才认为是随机的</param>
        /// <returns>true则表示应该忽略此ID定位</returns>
        public static bool isID_RandomGUID(HtmlNode Node, HtmlNode RootNode, int MinCountItem = 10)
        {
            if (string.IsNullOrEmpty(Node.Id) || Node.Id.Length < 9) return false;

            int BeforeCount = 0, AfterCount = 0;
            HtmlNode p = PrevSameLabelNode(Node);
            while (p != null && p.Id.Length == Node.Id.Length)
            {
                BeforeCount++;
                p = PrevSameLabelNode(p);
            }

            p = NextSameLabelNode(Node);
            while (p != null && p.Id.Length == Node.Id.Length)
            {
                AfterCount++;
                p = NextSameLabelNode(p);
            }

            return (BeforeCount + AfterCount + 1) >= MinCountItem;
        }

        static HtmlNode PrevSameLabelNode(HtmlNode Node)
        {
            HtmlNode p = Node.PreviousSibling;
            while (p != null && p.Name != Node.Name)
                p = p.PreviousSibling;
            return p;
        }

        static HtmlNode NextSameLabelNode(HtmlNode Node)
        {
            HtmlNode p = Node.NextSibling;
            while (p != null && p.Name != Node.Name)
                p = p.NextSibling;
            return p;
        }

        /// <summary>
        /// 当前节点的ID或Class中是否包含关键词
        /// </summary>
        /// <param name="Node">要检查的Node</param>
        /// <param name="Keywords">全部小写哦</param>
        /// <returns></returns>
        public static bool IDClassContain(HtmlNode Node, IEnumerable<string> Keywords)
        {
            if (Node == null || Keywords == null) return false;

            List<string> Str2Check = new List<string>();

            if (!string.IsNullOrEmpty(Node.Id))
                Str2Check.Add(Node.Id.ToLower());
            if (Node.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(Node.Attributes["class"].Value))
                foreach (string ClassName in Node.Attributes["class"].Value.Split())
                    Str2Check.Add(ClassName.ToLower());

            foreach (string Str in Str2Check)
                foreach (string Keyword in Keywords)
                    if (Str.Contains(Keyword))
                        return true;

            return false;
        }
        public static bool IDClassContain(HtmlNode Node, string Keyword)
        {
            //被十级郁气了，为何找不到方法把一个单一的string放到IEnumerable<string>里面去！算了我自己重新写一遍
            if (Node == null || string.IsNullOrEmpty(Keyword)) return false;

            List<string> Str2Check = new List<string>();

            if (!string.IsNullOrEmpty(Node.Id))
                Str2Check.Add(Node.Id.ToLower());
            if (Node.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(Node.Attributes["class"].Value))
                foreach (string ClassName in Node.Attributes["class"].Value.Split())
                    Str2Check.Add(ClassName.ToLower());

            foreach (string Str in Str2Check)
                    if (Str.Contains(Keyword))
                        return true;

            return false;
        }

        /// <summary>
        /// 当前节点的ID或Class中是否包含关键词
        /// </summary>
        /// <param name="Node">要检查的Node</param>
        /// <param name="Keywords">全部小写哦</param>
        /// <returns></returns>
        public static double ContainIDorClass(HtmlNode Node, Dictionary<string,double> Keywords)
        {
            double Score = 0;
            if (Node == null || Keywords == null) return Score;

            List<string> Str2Check = new List<string>();

            if (!string.IsNullOrEmpty(Node.Id))
                Str2Check.Add(Node.Id.ToLower());
            if (Node.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(Node.Attributes["class"].Value))
                foreach (string ClassName in Node.Attributes["class"].Value.Split())
                    Str2Check.Add(ClassName.ToLower());

            foreach (string Str in Str2Check)
                foreach (string Key in Keywords.Keys)
                    if (Str.Contains(Key))
                        Score += Keywords[Key];
            return Score;
        }


        /// <summary>
        /// XPath中是否使用了ID或Class来定位
        /// </summary>
        /// <param name="XPath"></param>
        /// <returns></returns>
        public static bool isXPathUsingName(string XPath)
        {
            if (string.IsNullOrWhiteSpace(XPath)) return false;
            return XPath.Contains("@id") || XPath.Contains("@class") || XPath.Contains("last()");
        }

        /// <summary>
        /// 计算XPath中包含了几个上级(叶子节点不算)
        /// </summary>
        /// <param name="XPath"></param>
        /// <returns></returns>
        public static int CountXPathLevel(string XPath)
        {
            if (string.IsNullOrWhiteSpace(XPath)) return 0;
            return XPath.Replace("//", "/").TrimStart('.').Trim('/').Where(c => c == '/').Count();
        }

        /// <summary>
        /// 去除XPath路径上的序号，从根节点向上数
        /// 这里的去除其实没有必要去掉向上几级的序号，上面几级序号相同时保留可以避免发生差错
        /// </summary>
        /// <param name="rawXPath"></param>
        /// <param name="IgnoreOrderNumber_UpLevel"></param>
        /// <returns></returns>
        /// <example>
        /// 如下xpath
        /// /html/body/div[3]/div/div[3]/ul[2]/li[4]/a
        /// IgnoreOrderNumber_UpLevel=2的情况下，输出如下
        /// /html/body/div[3]/div/div[3]/ul/li/a
        /// </example>
        public static string removeXPathOrderNumber(string rawXPath, int IgnoreOrderNumber_UpLevel)
        {
            if (string.IsNullOrEmpty(rawXPath) || IgnoreOrderNumber_UpLevel < 0)
                return rawXPath;

            //首先去掉最后一个元素的[]序号
            if (rawXPath.EndsWith("]") && char.IsDigit(rawXPath[rawXPath.LastIndexOf('[')+1]))
                rawXPath = rawXPath.Substring(0, rawXPath.LastIndexOf('['));

            return rawXPath;
        }

        /// <summary>
        /// 把最后几个被忽略级别的xpath去掉，获得共同父级的xpath
        /// </summary>
        /// <param name="rawXPath"></param>
        /// <param name="IgnoreOrderNumber_UpLevel"></param>
        /// <returns></returns>
        public static string removeIgnoreLayer(string rawXPath, int IgnoreOrderNumber_UpLevel)
        {
            if (string.IsNullOrEmpty(rawXPath) || IgnoreOrderNumber_UpLevel < 0)
                return rawXPath;

            // 算上最后一级自己这个[]，所以要加上1
            for (int i = 0; i < IgnoreOrderNumber_UpLevel + 1; i++)
            {
                int LastIndex = rawXPath.LastIndexOf('/');

                //没有/了，则结束本函数
                if (LastIndex <= 0) break;

                //如果有数字序号，则处理，没有则下一个循环
                rawXPath = rawXPath.Remove(LastIndex);
            }

            return rawXPath;
        }

        /// <summary>
        /// 监测一个HtmlNode是否是Body或Html标签
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static bool isTopNode(HtmlNode Node)
        {
            if (Node == null) return true;

            return (Node.ParentNode == null || Node == Node.OwnerDocument.DocumentNode || Node.Name.ToLower() == "body" || Node.Name.ToLower() == "html");
        }

        /// <summary>
        /// 如果出现下列标签，则不分解子节点，直接提取InnerText
        /// </summary>
        static HashSet<string> HtmlTag_DirectInnerText = new HashSet<string>("a".Split());

        /// <summary>
        /// 本节点直属的文本内容，不含子Element的内容，如果有多段用空格隔开
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static string InnerTextNonDescendants(HtmlNode Node)
        {
            switch (Node.NodeType)
            {
                default:
                case HtmlNodeType.Comment:
                    return null;
                case HtmlNodeType.Text:
                    return Node.InnerText;
                case HtmlNodeType.Document:
                case HtmlNodeType.Element:
                    if (Node.ChildNodes == null || Node.ChildNodes.Count == 0)
                        return null;
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (HtmlNode son in Node.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Text || HtmlTag_DirectInnerText.Contains(Node.Name.ToLower())))
                            sb.Append(son.InnerText.Trim()).Append(' ');
                        return sb.ToString().TrimEnd(' ');
                    }
            }
        }

        /// <summary>
        /// 用于验证节点的取舍
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public delegate bool ValidateNode(HtmlNode node);

        /// <summary>
        /// 通过XQuery查询到候选结果，再用Validate函数过滤，返回结果
        /// </summary>
        /// <param name="root"></param>
        /// <param name="XQuery"></param>
        /// <param name="ValidateFunction"></param>
        /// <returns></returns>
        public static List<HtmlNode> FilterNodes(HtmlNode root, string XQuery, ValidateNode ValidateFunction = null)
        {
            if (root == null || string.IsNullOrEmpty(XQuery)) return null;
            HtmlNodeCollection nodes = root.SelectNodes(XQuery);
            if (nodes == null)
                return null;
            if (ValidateFunction == null)
                return nodes.ToList();
            else
                return nodes.Where(n => ValidateFunction(n)).ToList();
        }

        /// <summary>
        /// 计算两个的XPath的距离（从头部和尾部分别排除掉相同字符，中间部分的长度即为距离）
        /// </summary>
        /// <param name="Path1"></param>
        /// <param name="Path2"></param>
        /// <param name="FirstDiffCharIndex">从左侧开始第一个不同字符的位置</param>
        public static int XPathDifference(string Path1, string Path2, out int FirstDiffCharIndex)
        {
            if (string.IsNullOrEmpty(Path1) || string.IsNullOrEmpty(Path2))
            {
                FirstDiffCharIndex = -1;
                return int.MaxValue;
            }

            int pl = 0, pr = Path1.Length - 1; //左右两个指针，找到两边的第一个不同字符
            while (pl < Path1.Length && pl < Path2.Length && Path1[pl] == Path2[pl]) pl++;
            while (pr >= 0 && Path1[pr] == Path2[Path2.Length - (Path1.Length - pr)]) pr--;

            FirstDiffCharIndex = pl;
            if (pr < pl)
                return 0;
            else
                return pr - pl + 1;
        }

        /// <summary>
        /// 将两个XPath出去左右两边相同部分的剩余部分中的[]全部去掉
        /// </summary>
        /// <param name="Path1"></param>
        /// <param name="Path2"></param>
        /// <returns>是否成功合并</returns>
        public static string CombinePattern(string Path1, string Path2)
        {
            int pl = 0, pr = Path1.Length - 1; //左右两个指针，找到两边的第一个不同字符
            while (pl < Path1.Length && pl < Path2.Length && Path1[pl] == Path2[pl]) pl++;
            while (pr >= 0 && Path1[pr] == Path2[Path2.Length - (Path1.Length - pr)]) pr--;

            if (pr < pl) return Path1;

            //pl和pr分别移动到[和]的位置
            while (pl >= 0 && Path1[pl] != '[') pl--;
            while (pr < Path1.Length && Path1[pr] != ']') pr++;

            string Middle1 = Regex.Replace(Path1.Substring(pl, pr - pl + 1), @"\[[^\]]*?\]", "");
            string Middle2 = Regex.Replace(Path2.Substring(pl, Path2.Length - pl - (Path1.Length - pr) + 1), @"\[[^\]]*?\]", "");

            if (Middle1 != Middle2) return null;

            return Path1.Substring(0, pl) + Middle1 + Path1.Substring(pr + 1);
        }
    }
}
