using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Thrinax.Enums;
using Thrinax.Extract;
using Thrinax.Http;
using Thrinax.Interface;
using Thrinax.Models;
using Thrinax.Utility;
using Thrinax.Utility.Smart;

namespace Thrinax.Parser
{

    public class SmartParser : IParser
    {
        public ArticleList ParseList(string Html, string Pattern, string Url = null, bool RecogNextPage = true)
        {
            //输入检查
            if (string.IsNullOrWhiteSpace(Html))
                return null;

            //检查 Pattern 是否为空，不为空时直接使用对应Parser
            XpathPattern xpathPattern = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(Pattern))
                {
                    xpathPattern = JsonConvert.DeserializeObject<XpathPattern>(Pattern);
                    if (xpathPattern != null)
                        return new XpathParser().ParseList(Html, Pattern, Url, RecogNextPage);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Pattern 的格式不符合 Xpath Parser 的定义，请检查！Url:{0}, Pattern:{1}.", Url, Pattern), ex);
            }

            //TODO: Pattern为空时使用训练模型获取得分最高的数组返回
            return null;
        }

        public bool ParseItem(string Html, string Pattern, string Url, ref Article BaseArticle)
        {
            throw new NotImplementedException();
        }

        #region 模式训练流程

        #region List部分

        static Random rmd = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// 从List页面中分析出模式，再随机提取若干Item页面
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="HTML"></param>
        /// <param name="MediaType"></param>
        /// <param name="Language"></param>
        /// <param name="RandomItemCount"></param>
        /// <returns></returns>
        public static List<ListPagePattern> List_ExtractPattern_n_CrawlItem(string Url, string HTML, Enums.MediaType MediaType, Enums.Language Language, int RandomItemCount)
        {
            List<ListPagePattern> XPaths = List_ExtractPattern(Url, HTML, MediaType, Language, true);
            //每一个可能的模式都要试取几个Item
            foreach (ListPagePattern XPath in XPaths)
                foreach (Article Item in XPath.Contents.OrderBy(x => rmd.Next(10000)).Take(RandomItemCount))
                {
                    if (string.IsNullOrWhiteSpace(Item.Url) || !Uri.IsWellFormedUriString(Item.Url, UriKind.Absolute)) continue;

                    //下载
                    string ItemHTML = HttpHelper.GetHttpContent(Item.Url);
                    if (string.IsNullOrWhiteSpace(ItemHTML))
                    {
                        Item.Content = "Download Error";
                        continue;
                    }

                    //直接用基于行文字密度的简单算法提取
                    Article article = HtmlToArticle.GetArticle(ItemHTML);
                    if (article == null || string.IsNullOrWhiteSpace(article.HtmlContent))
                    {
                        Item.HtmlContent = "ItemPage Parse Error";
                        continue;
                    }
                    Item.HtmlContent = article.HtmlContent;
                    Item.Content = article.Content;
                    if (string.IsNullOrEmpty(Item.Title)) Item.Title = article.Title;
                    if (Item.PubDate == null && article.PubDate.Year > 2000)
                        Item.PubDate = article.PubDate;
                }
            return XPaths;
        }

        /// <summary>
        /// 分析出List和Item中的模式
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="HTML"></param>
        /// <param name="MediaType"></param>
        /// <param name="Language"></param>
        /// <returns></returns>
        public static List<ListPagePattern> Extract_Patterns(string Url, string HTML, Enums.MediaType MediaType, Enums.Language Language)
        {
            //完成list页面模式的提取和分析，经过补充还保留了部分字段用来检查item的结果。
            List<ListPagePattern> ListXPaths = List_ExtractPattern(Url, HTML, MediaType, Language, true);

            return ListXPaths;
        }

        /// <summary>
        /// 从List页面中分析出模式
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="HTML"></param>
        /// <param name="MediaType"></param>
        /// <param name="Language"></param>
        /// <param name="NeedExtractContent">是否提取内容</param>
        /// <param name="NeedScore">是否需要打分</param>
        /// <returns>多个候选，按照可能性降序</returns>
        public static List<ListPagePattern> List_ExtractPattern(string Url, string HTML, Enums.MediaType MediaType, Enums.Language Language, bool NeedExtractContent = true, bool NeedScore = true)
        {
            //获取root节点（有些网站页面不带html标签的，直接从head开始写）
            HtmlNode rootNode = HtmlUtility.getSafeHtmlRootNode(HTML, true, true);
            if (rootNode == null) return null;

            //todo:如果命中了论坛类型，则进入论坛识别
            if (MediaType == Enums.MediaType.Forum)
            {

            }

            SoftStrategy Strategy = new SoftStrategy(MediaType, Language);

            //提取候选标题的Pattern
            List<HtmlPattern> LinkPatterns = List_LinkPattern_getCandidateByATag(Url, rootNode, MediaType, Language, Strategy, NeedScore);
            if (LinkPatterns == null || LinkPatterns.Count == 0) return null;

            //提取时由于候选较多，已经过了一步筛选，提取不当造成的低分Pattern找不回来了
            //一种思路是所有的识别都必须经过视觉打分，跟上述算法共同决定最佳的Pattern
            //另一种思路是，有个打分和评判机制，只有当不确定上述密度算法的第一结果可用时，才启用视觉算法

            //接下去，开始进行其他字段的Xpath识别                 
            List<ListPagePattern> AllFieldsPath = new List<ListPagePattern>();
            ListPagePattern XPath = new ListPagePattern();


            //把每一个候选的LinkPattern都生成全字段匹配加入总的候选
            foreach (HtmlPattern LinkPattern in LinkPatterns)
                AllFieldsPath.Add(List_getAllFieldsXPath(Url, rootNode, LinkPattern, NeedExtractContent, Strategy));


            //全局重新排序，截断太差的
            AllFieldsPath = AllFieldsPath.OrderByDescending(x => x.TotalScore).ToList();

            return AllFieldsPath;
        }

        /// <summary>
        /// 根据List页Dom模型提取A标签，来获取候选的ListPattern
        /// </summary>
        /// <param name="Url">链接</param>
        /// <param name="RootNode">根节点</param>
        /// <param name="MediaType">媒体类型</param>
        /// <param name="Language">语言</param>
        /// <param name="Strategy">SVM模型</param>
        /// <param name="NeedScore">是否需要得分</param>
        /// <returns>按可能性排序的Pattern集合</returns>
        public static List<HtmlPattern> List_LinkPattern_getCandidateByATag(string Url, HtmlNode RootNode, Enums.MediaType MediaType, Enums.Language Language, SoftStrategy Strategy, bool NeedScore = true)
        {
            //提取所有A标签
            HtmlNodeCollection allHref = RootNode.SelectNodes("//a[@href]");
            if (allHref == null) return null;

            //过滤A标签：检查标题及链接是否合规
            List<HtmlNode> filterHref = allHref.Where(a => !string.IsNullOrWhiteSpace(a.InnerText) && HTMLCleaner.isUrlGood(a.Attributes["href"].Value)).ToList();
            filterHref = filterHref.Where(a => TextStatisticsUtility.GetWeightedLength(HTMLCleaner.GetCleanInnerText(a), HardThreshold.StopWords) > 0).ToList();

            //统计共有多少种不同结构的链接，列出候选者并排序
            List<HtmlPattern> Patterns = List_HtmlPattern_DiscoverPattern(filterHref, RootNode, Strategy, Url, NeedScore);
            if (Patterns == null) return null;


            //这里是删减部分
            if (NeedScore)
            {
                //出现了一种全部都为负的情况，此时绝大多数情况是没有合适的模式，返回前5个进行下一步计算。得分拉开的有点大，只要大于1/8且小于5个就好
                if (Patterns.FirstOrDefault().Score > 0)
                    Patterns = Patterns.Where(p => p.Score >= Patterns.First(t => true).Score / 8).Take(5).ToList();
                else if (Patterns.Count() > 5)
                    Patterns = Patterns.OrderByDescending(p => p.Score).Take(5).OrderByDescending(p => p.Score).ToList();
            }
            return Patterns;
        }

        /// <summary>
        /// 修改过的Pattern识别主体。共六步，在region:合并生成pattern的新思路 中
        /// </summary>
        /// <param name="Nodes">所有A Nodes</param>
        /// <param name="RootNode">DOM树的根节点</param>
        /// <param name="Strategy">策略文件，包含打分模型和边界参数</param>
        /// <param name="URL">URL</param>
        /// <param name="NeedScore">是否需要打分。在训练时对一些无关的模式可以不用打分，节省计算</param>
        /// <returns></returns>
        internal static List<HtmlPattern> List_HtmlPattern_DiscoverPattern(List<HtmlNode> Nodes, HtmlNode RootNode, SoftStrategy Strategy, string URL, bool NeedScore = true)
        {
            //第一步，先全部Pattern化，即算好子节点
            List<PatternAnaly> BasicNodesAnalies = List_HtmlPattern_FormingPatterns(Nodes);

            //第二步，平级检查是否有同样的Pattern，全中则列为Pattern列入表，否则向上并检查同样父节点的组合成一个Pattern列入表。
            List<PatternAnaly> BasicPatternsAnalies = List_HtmlPattern_FormingtruePatterns(BasicNodesAnalies, Strategy);

            //第三步，对每个Pattern，向上检索父点是否具有相同的范式，再向上检查，N次之后确定一个最大的值。取消中间层的数字序号。 
            List<PatternAnaly> CombinedPatternsAnalies = List_HtmlPattern_CombinTruePatterns(BasicPatternsAnalies, Strategy);

            //第四步，精简多种path（xpath,lcaxpath,itbxpath等等）并生成LevelIgnored留作后用
            List<PatternAnaly> ShortenAnalies = List_HtmlPattern_ShortenAnalies(CombinedPatternsAnalies, RootNode, URL);

            //第五步，将生成的patternanali类转化成pattern类
            List<HtmlPattern> Patterns = new List<HtmlPattern>();
            ShortenAnalies.ForEach(s => Patterns.Add(s.WritePattern()));

            //第六步，生成标题的relxpath并对比去重 (这一步对信息出现了丢失)
            List<HtmlPattern> ResultPatterns = List_HtmlPattern_FinishPatterns(Patterns, Strategy, RootNode, URL, NeedScore);

            //ResultPatterns.ForEach(r => r.XPath = getRawITBXPath(r.ItemBaseXPath, r.LCAXPath, r.ItemCount, RootNode, Strategy));
            //六步完成
            return ResultPatterns;
        }

        /// <summary>
        /// 简单小函数，用来检查是否可以直接在ItemBaseXPath进行简化。有时候lcaxpath是没法简化的。这样做可以有一点小进步
        /// </summary>
        /// <param name="ItemBaseXPath"></param>
        /// <param name="LeastAncestorXPath"></param>
        /// <param name="ItemCount"></param>
        /// <param name="RootNode"></param>
        /// <param name="Strategy"></param>
        /// <returns></returns>
        public static string getRawITBXPath(string ItemBaseXPath, string LeastAncestorXPath, int ItemCount, HtmlNode RootNode, SoftStrategy Strategy)
        {

            //如果这样就说明itbxp已经挺简化的了，不必再做这些处理
            if (Regex.Matches(ItemBaseXPath, @"/").Count <= 4)
                return ItemBaseXPath;

            List<string> rawitbxpaths = new List<string>();
            rawitbxpaths.Add(ItemBaseXPath);

            IEnumerable<HtmlNode> ItemBaseNodes = RootNode.SelectNodes(ItemBaseXPath);
            HtmlPattern TempPattern = new HtmlPattern();
            TempPattern.ItemBaseXPath = LeastAncestorXPath;
            TempPattern.ItemCount = ItemCount;
            List<HtmlPattern> rawitbpatterns = List_HtmlPattern_getRelXPath(ItemBaseNodes, RootNode, TempPattern, Strategy, null, null, ItemCount, PatternType.Title, false);

            if (rawitbpatterns == null) return null;
            foreach (HtmlPattern ITBPattern in rawitbpatterns)
            {
                rawitbxpaths.Add(ITBPattern.RelXPath);
                rawitbxpaths.Add(ITBPattern.LCAXPath + '/' + ITBPattern.RelXPath);
            }
            rawitbxpaths = rawitbxpaths.OrderBy(r => Regex.Matches(r, @"/").Count).ToList();

            return rawitbxpaths.First(t => isthesameXpath(RootNode, ItemBaseXPath, t));
        }

        /// <summary>
        /// Node有效审核的评估接口
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        internal delegate bool NodeValidFilter(HtmlNode Node);

        /// <summary>
        /// Node评分的接口
        /// </summary>
        /// <param name="Pattern"></param>
        /// <param name="Strategy"></param>
        /// <param name="BaseItemCount"></param>
        /// <returns></returns>
        internal delegate double NodePatternScore(IEnumerable<HtmlNode> Nodes);

        /// <summary>
        /// 从ItemBaseNode开始根据相对路径RelXPath提取目标Node
        /// </summary>
        /// <param name="ItemBaseNode"></param>
        /// <param name="RelPattern"></param>
        /// <param name="ExceptTitleNodes"></param>
        /// <returns></returns>
        private static IEnumerable<HtmlNode> SelectNode_ByRelXPath_FromBaseNode(HtmlNode ItemBaseNode, HtmlPattern RelPattern, IEnumerable<HtmlNode> ExceptTitleNodes, IEnumerable<string> StopWords)
        {
            HtmlNodeCollection selected = ItemBaseNode.SelectNodes(RelPattern.RelXPath);
            if (selected == null) return null;
            IEnumerable<HtmlNode> Nodes = selected.Where(n => !string.IsNullOrEmpty(TextCleaner.RemoveStopWords(TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(n)), StopWords)));

            //必须正好选出1个节点
            if (Nodes.Count() != 1) return null;

            if (ExceptTitleNodes != null)
            {
                //如果Path指向了Title则忽略
                if (Nodes.Where(n => ExceptTitleNodes.Contains(n) || ExceptTitleNodes.Contains(n.ParentNode)).Count() > 0) return null;
                //如果Path选中的节点包含了Title，则忽略
                bool ContainTitle = false;
                foreach (HtmlNode n in Nodes)
                    foreach (HtmlNode t in ExceptTitleNodes)
                        if (n.ChildNodes.Contains(t))
                        {
                            ContainTitle = true;
                            break;
                        }
                if (ContainTitle) return null;
            }

            return Nodes;
        }

        #region 合并生成pattern的新思路


        /// <summary>
        /// 第一步，先全部Pattern化，即算好子节点
        /// 把所有的点都分析化。因为都是最基础的点所以ContainList不设置都为Null
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static List<PatternAnaly> List_HtmlPattern_FormingPatterns(List<HtmlNode> Nodes)
        {
            List<PatternAnaly> ReplyPatternAnalies = new List<PatternAnaly>();
            foreach (HtmlNode node in Nodes)
            {
                PatternAnaly newanali = new PatternAnaly
                {
                    BasicNode = node,
                    CurrentNode = node,

                    BasicXPath = node.XPath,

                    ItemCount = 1,
                    AverageTextLength = TextStatisticsUtility.GetWeightedLength(HTMLCleaner.GetCleanInnerText(node)),
                    ContainList = formContainList(node),
                    useless = false
                };
                ReplyPatternAnalies.Add(newanali);
            }
            return ReplyPatternAnalies;
        }

        /// <summary>
        /// 完成之后对ContainList进行描写的部分
        /// </summary>
        /// <param name="SiblingPatterns"></param>
        /// <returns></returns>
        //public static Dictionary<string, int> formContainList(IEnumerable<PatternAnaly> SiblingPatterns)
        //{
        //    Dictionary<string, int> ContainList = new Dictionary<string, int>();
        //    foreach (PatternAnaly Sibling in SiblingPatterns)
        //    {
        //        if (ContainList.ContainsKey(Sibling.node.Name))
        //            ContainList[Sibling.node.Name] += 1;
        //        else
        //            ContainList.Add(Sibling.node.Name, 1);
        //    }
        //    return ContainList;
        //}
        public static Dictionary<string, int> formContainList(HtmlNode Node)
        {
            if (Node == null) return null;
            if (!Node.HasChildNodes || Node.ChildNodes.Count == 0)
                return null;

            Dictionary<string, int> ContainList = new Dictionary<string, int>();
            foreach (HtmlNode node in Node.ChildNodes)
                if (!node.Name.Contains("#"))
                {
                    if (ContainList.ContainsKey(node.Name))
                        ContainList[node.Name] += 1;
                    else
                        ContainList.Add(node.Name, 1);
                }
            return ContainList;
        }

        public static Dictionary<string, HashSet<string>> formAttributelist(HtmlNode RootNode, string xpath)
        {
            if (RootNode == null || string.IsNullOrWhiteSpace(xpath)) return null;
            if (xpath.EndsWith("]")) xpath = xpath.Remove(xpath.LastIndexOf("["));
            try
            {
                HtmlNodeCollection Nodes = RootNode.SelectNodes(xpath);
                if (Nodes == null) return null;

                Dictionary<string, HashSet<string>> Attributelist = new Dictionary<string, HashSet<string>>();
                foreach (HtmlNode Node in Nodes)
                    if (Node.HasAttributes && Node.Attributes != null)
                        foreach (HtmlAttribute a in Node.Attributes)
                        {
                            if (!Attributelist.ContainsKey(a.Name)) Attributelist.Add(a.Name, new HashSet<string>());
                            Attributelist[a.Name].Add(a.Value);
                        }
                return Attributelist;
            }
            catch
            {
                //todo:检查掉这个错误之后去除try catch。原来是table会带有符号标签，实际上应该去除。
                return null;
            }

        }

        /// <summary>
        /// 第二步，平级检查是否有同样的Pattern，全中则列为Pattern列入表，否则向上并检查同样父节点的组合成一个Pattern列入表。
        /// 这部分是整理合并从basic中找到的信息，整理为确实可以被认定是pattern的部分
        /// TODO: 合并时一批pattern可能存在多种合并情况，为了保证能获取到全部可能情况，需要全部保留
        /// </summary>
        /// <param name="strategy">打分模型</param>
        /// <param name="BasicPatternAnalies"></param>
        /// <returns></returns>
        public static List<PatternAnaly> List_HtmlPattern_FormingtruePatterns(List<PatternAnaly> BasicPatternAnalies, SoftStrategy strategy)
        {
            List<PatternAnaly> ResultAnalies = new List<PatternAnaly>();
            List<PatternAnaly> RawAnalies = BasicPatternAnalies;
            List<PatternAnaly> NewRawAnalies = new List<PatternAnaly>();

            //这里仅记录在某一层用过的Pattern，同一个Xpath可以被多层使用
            Dictionary<int, List<PatternAnaly>> usedPattern = new Dictionary<int, List<PatternAnaly>>();

            for (int startfloor = 1; startfloor <= strategy.Threshold.LevelUpCelling_TitleABasePattern; startfloor++)
            {
                NewRawAnalies = new List<PatternAnaly>();
                foreach (PatternAnaly Anali in RawAnalies)
                {
                    if (XPathUtility.isTopNode(Anali.CurrentNode.ParentNode) || (usedPattern.ContainsKey(startfloor) && usedPattern[startfloor].Contains(Anali)))//如果已经到顶点了就不要了
                    {
                        continue;
                    }

                    List<PatternAnaly> Siblings = new List<PatternAnaly>();

                    foreach (PatternAnaly rawanali in RawAnalies)
                    {
                        //并不是ParentNode一样，就可以归为一组的，还要考虑
                        if (GetItemBaseXpath(Anali, startfloor) == GetItemBaseXpath(rawanali, startfloor)
                            && IsthesameDic(rawanali.ContainList, Anali.ContainList)
                            && rawanali.CurrentNode.Name == Anali.CurrentNode.Name
                            && (!usedPattern.ContainsKey(startfloor) || !usedPattern[startfloor].Contains(rawanali)))
                        {
                            Siblings.Add(rawanali);
                            if (!usedPattern.ContainsKey(startfloor))
                                usedPattern[startfloor] = new List<PatternAnaly>();

                            usedPattern[startfloor].Add(rawanali);

                            rawanali.useless = true;
                        }
                    }
                    if (Siblings.Count > 1)//如果检查成功，在父节点下有多个与自己相同的，那么形成单元并删去已存内容
                    {
                        double AvgTtLth = 0;
                        foreach (PatternAnaly Sibling in Siblings)
                            AvgTtLth += Sibling.AverageTextLength;
                        AvgTtLth /= Siblings.Count;

                        PatternAnaly newresultAnali = new PatternAnaly
                        {
                            BasicNode = Anali.BasicNode,
                            ItemBaseNode = Anali.CurrentNode,
                            CurrentNode = Anali.CurrentNode,

                            BasicXPath = Anali.BasicXPath,
                            ItemBaseXPath = Anali.CurrentNode.XPath,

                            ItemCount = Siblings.Count,
                            AverageTextLength = AvgTtLth,
                            ContainList = formContainList(Anali.ItemBaseNode),
                            useless = false
                        };
                        //检查是否有数字序号的简并，并对xpath和ibtxpath进行调整
                        if (newresultAnali.ItemBaseXPath.EndsWith("]") && Siblings.Count > 1)
                            newresultAnali.ItemBaseXPath = newresultAnali.ItemBaseXPath.Substring(0, newresultAnali.ItemBaseXPath.LastIndexOf("["));
                        if (newresultAnali.BasicXPath.Substring(newresultAnali.ItemBaseXPath.Length).StartsWith("["))
                        {
                            string sub = Anali.BasicXPath.Substring(newresultAnali.ItemBaseXPath.Length);
                            newresultAnali.BasicXPath = newresultAnali.ItemBaseXPath + sub.Substring(sub.IndexOf("]") + 1);
                        }

                        ResultAnalies.Add(newresultAnali);
                        //从这里形成更高级的pattern
                    }
                    if (Siblings.Count < 4)
                    {
                        //如果sibling格式并不统一，Pattern格式独立，则向上一层定义新的Pattern来代替之前的
                        foreach (PatternAnaly Sibling in Siblings)
                        {
                            PatternAnaly newrawAnali = new PatternAnaly
                            {
                                BasicNode = Sibling.BasicNode,
                                CurrentNode = Anali.CurrentNode.ParentNode,

                                BasicXPath = Sibling.BasicXPath,

                                ItemCount = 1,
                                AverageTextLength = Sibling.AverageTextLength,
                                ContainList = formContainList(Sibling.CurrentNode.ParentNode),
                                useless = false
                            };
                            //从这里把basicpattern的单元向上一级
                            if (!NewRawAnalies.Contains(newrawAnali)) NewRawAnalies.Add(newrawAnali);
                        }
                    }
                }
                RawAnalies = RawAnalies.Where(a => a.useless == false).ToList();//其实不必，反正肯定已经空了
                RawAnalies.AddRange(NewRawAnalies);
            }
            ResultAnalies = ResultAnalies.OrderByDescending(r => r.ItemCount).ToList();
            return ResultAnalies;
        }

        private static string GetItemBaseXpath(PatternAnaly rawPattern, int level)
        {
            string baseXpath = rawPattern.BasicXPath;

            if (string.IsNullOrEmpty(baseXpath))
                return baseXpath;

            int charTime = baseXpath.Count(f => f.Equals('[')) - level;
            int startIndex = -1;
            int endIndex = -1;

            for (int j = 0; j <= charTime; j++)
            {
                startIndex = baseXpath.IndexOf('[', startIndex + 1);
                endIndex = baseXpath.IndexOf(']', endIndex + 1);
            }

            if (startIndex > 0 && endIndex > 0 && startIndex < endIndex)
            {
                string resultXpath = baseXpath.Substring(0, startIndex) + baseXpath.Substring(endIndex + 1);
                return resultXpath;
            }
            return baseXpath;
        }

        /// <summary>
        /// 第三步，对每个Pattern，向上检索父点是否具有相同的范式，再向上检查，N次之后确定一个最大的值。取消中间层的数字序号。
        /// 将确实可以被认定是pattern的内容向上升级，检查是否可以继续合并
        /// </summary>
        /// <param name="RawAnalies"></param>
        /// <param name="strategy"></param>
        /// <returns></returns>
        public static List<PatternAnaly> List_HtmlPattern_CombinTruePatterns(List<PatternAnaly> RawAnalies, SoftStrategy strategy)
        {
            List<PatternAnaly> ResultAnalies = RawAnalies;
            List<PatternAnaly> NewRawAnalies = new List<PatternAnaly>();
            for (int startfloor = 1; startfloor <= strategy.Threshold.LevelUpCelling_TitleAAncestorPattern; startfloor++)
            {
                NewRawAnalies = new List<PatternAnaly>();
                foreach (PatternAnaly Anali in ResultAnalies)
                {
                    //列入待审核名单，
                    if (XPathUtility.isTopNode(Anali.CurrentNode.ParentNode) && !NewRawAnalies.Contains(Anali))//如果已经到顶点了就不要再升级下去了
                    {
                        NewRawAnalies.Add(Anali);
                        Anali.useless = true;
                    }
                    if (Anali.useless == true) continue;

                    List<PatternAnaly> Siblings = new List<PatternAnaly>();

                    foreach (PatternAnaly Sibling in ResultAnalies)
                        if (!Sibling.useless &&
                            Sibling.CurrentNode.ParentNode == Anali.CurrentNode.ParentNode &&
                            Sibling.CurrentNode.Name == Anali.CurrentNode.Name &&
                            IsthesameDic(Sibling.ContainList, Anali.ContainList) &&
                            Sibling.BasicXPath.Substring(Sibling.ItemBaseXPath.Length) == Anali.BasicXPath.Substring(Anali.ItemBaseXPath.Length))
                        {
                            Siblings.Add(Sibling);
                            Sibling.useless = true;
                        }

                    //保证在某些sibling所包含信息不全的情况下，仍然能得到正确路径
                    //虽然不影响大多数情况，这里还是有Bug的。见http://www.bmlink.com/news/list-26.html
                    //右侧广告里12个标题的一个列表和10个标题的列表没有被合并，然而再向上一级它们又合并了
                    //只能说暂且这么凑合着用了，毕竟基本上不会遇到这样无聊的意外
                    Siblings = Siblings.OrderByDescending(s => s.ItemCount).ToList();
                    double AVTL = 0;
                    int IC = 0;
                    foreach (PatternAnaly Sibling in Siblings)//只要不是被舍弃的顶部节点，在做完整理之后要么进入true要么向上形成新basic返回basic内部，不会再用到
                    {
                        AVTL += Sibling.AverageTextLength * Sibling.ItemCount;
                        IC += Sibling.ItemCount;
                    }
                    AVTL = AVTL / IC;

                    PatternAnaly NewtrueAnali = new PatternAnaly
                    {
                        CurrentNode = Anali.CurrentNode.ParentNode,//对于正常结束的，这里不再需要，所以不准确也无所谓
                        BasicNode = Anali.BasicNode,
                        ItemBaseNode = Anali.ItemBaseNode,
                        LeastAncestorNode = Anali.LeastAncestorNode,

                        BasicXPath = Anali.BasicXPath,
                        ItemBaseXPath = Anali.ItemBaseXPath,
                        LeastAncestorXPath = Anali.LeastAncestorNode == null ? string.Empty : Anali.LeastAncestorNode.XPath,

                        ItemCount = IC,
                        AverageTextLength = AVTL,
                        ContainList = formContainList(Anali.CurrentNode.ParentNode),
                        useless = false,
                    };
                    //如果是合并的，则修正一下LCANode
                    if (Siblings.Count > 1 || NewtrueAnali.LeastAncestorNode == null)
                    {
                        NewtrueAnali.LeastAncestorNode = Anali.CurrentNode.ParentNode;
                        NewtrueAnali.LeastAncestorXPath = Anali.CurrentNode.ParentNode.XPath;
                    }
                    if (Anali.CurrentNode.XPath.EndsWith("]") && Siblings.Count > 1)
                    {
                        string sub = NewtrueAnali.BasicXPath.Substring(Anali.CurrentNode.XPath.Length);
                        if (sub.IndexOf("[") == -1 && sub.Contains("]") || sub.IndexOf("[") > sub.IndexOf("]")) sub = "[" + sub;
                        NewtrueAnali.BasicXPath = Anali.CurrentNode.XPath.Substring(0, Anali.CurrentNode.XPath.LastIndexOf("[")) + sub;
                        if (Anali.CurrentNode.XPath.Length < NewtrueAnali.ItemBaseXPath.Length)
                        {
                            sub = NewtrueAnali.ItemBaseXPath.Substring(Anali.CurrentNode.XPath.Length);
                            if (sub.IndexOf("[") == -1 && sub.Contains("]") || sub.IndexOf("[") > sub.IndexOf("]")) sub = "[" + sub;
                            NewtrueAnali.ItemBaseXPath = Anali.CurrentNode.XPath.Substring(0, Anali.CurrentNode.XPath.LastIndexOf("[")) + sub;
                        }
                    }

                    if (!NewRawAnalies.Contains(NewtrueAnali)) NewRawAnalies.Add(NewtrueAnali);

                }
                if (NewRawAnalies.Count != 0)
                    ResultAnalies = NewRawAnalies;
            }
            ResultAnalies = ResultAnalies.OrderByDescending(t => t.ItemCount).ToList();
            return ResultAnalies;
        }

        /// <summary>
        /// 第四步，精简多种path（xpath,lcaxpath,itbxpath等等）并生成LevelIgnored留作后用
        /// </summary>
        /// <param name="CombinedAnalies"></param>
        /// <returns></returns>
        public static List<PatternAnaly> List_HtmlPattern_ShortenAnalies(List<PatternAnaly> CombinedAnalies, HtmlNode RootNode, string Url)
        {
            foreach (PatternAnaly anali in CombinedAnalies)
            {
                int LVI = Regex.Matches(anali.LeastAncestorXPath, @"/").Count;
                string shortenLCAXPath = XPathUtility.GetRawXPath(anali.LeastAncestorNode, RootNode, LVI, Url, true);
                if (shortenLCAXPath != anali.LeastAncestorXPath && !string.IsNullOrWhiteSpace(shortenLCAXPath))
                {
                    anali.BasicXPath = shortenLCAXPath + anali.BasicXPath.Substring(anali.LeastAncestorXPath.Length);
                    anali.ItemBaseXPath = shortenLCAXPath + anali.ItemBaseXPath.Substring(anali.LeastAncestorXPath.Length);
                    anali.LeastAncestorXPath = shortenLCAXPath;
                }
                anali.LVI = Regex.Matches(anali.ItemBaseXPath.Substring(anali.LeastAncestorXPath.Length), @"/").Count;
            }
            return CombinedAnalies;
        }

        /// <summary>
        /// 第六步，生成标题的relxpath并对比去重
        /// </summary>
        /// <param name="Patterns"></param>
        /// <param name="strategy"></param>
        /// <param name="RootNode"></param>
        /// <param name="Url"></param>
        /// <param name="NeedScore"></param>
        /// <returns></returns>
        public static List<HtmlPattern> List_HtmlPattern_FinishPatterns(List<HtmlPattern> Patterns, SoftStrategy strategy, HtmlNode RootNode, string Url, bool NeedScore = false)
        {
            List<HtmlPattern> ResultPatterns = new List<HtmlPattern>();

            List<HtmlPattern> RelPatterns = new List<HtmlPattern>();
            int BestBaseItemCount = Patterns.Count == 0 ? 0 : Patterns[0].ItemCount;

            foreach (HtmlPattern AbsPattern in Patterns)
            {
                //随机抽取至多3个叶子节点
                if (RootNode.SelectNodes(AbsPattern.XPath) == null) continue;
                int num = RootNode.SelectNodes(AbsPattern.XPath).Count();
                if (num > 3) num = 3;
                IEnumerable<HtmlNode> SeedNodes = RootNode.SelectNodes(AbsPattern.XPath).OrderBy(x => rmd.Next()).Take(num);
                RelPatterns.AddRange(List_HtmlPattern_getRelXPath(SeedNodes, RootNode, AbsPattern, strategy, null, null, BestBaseItemCount, PatternType.Title, false));
            }


            //去重
            foreach (HtmlPattern hp in RelPatterns)
            {
                bool repeat = false;
                foreach (HtmlPattern rthp in ResultPatterns)
                    if (isthesameXpath(RootNode, hp.ItemBaseXPath, rthp.ItemBaseXPath))
                    {
                        List<HtmlNode> BaseNodes = RootNode.SelectNodes(hp.ItemBaseXPath).ToList();
                        if (isthesameRelXpath(BaseNodes, hp.RelXPath, rthp.RelXPath))
                        { repeat = true; break; }
                    }

                if (!repeat)
                {
                    string path = string.IsNullOrWhiteSpace(hp.RelXPath) ? hp.ItemBaseXPath : hp.ItemBaseXPath + "/" + hp.RelXPath;
                    List<HtmlNode> filterNodes = RootNode.SelectNodes(path).ToList().Where(a => !string.IsNullOrEmpty(a.InnerText)).ToList();
                    if (filterNodes != null && filterNodes.Count() > 0)
                    {
                        if (NeedScore)
                            hp.Score = strategy.ScoreforListTitle(filterNodes);
                        ResultPatterns.Add(hp);
                    }
                }
            }

            ResultPatterns = ResultPatterns.OrderByDescending(p => p.Score).ThenByDescending(p => p.RelXPathUsingName).ThenBy(p => p.RelXPathLevel).ToList();


            return ResultPatterns;
        }

        #endregion 合并生成pattern的新思路

        /// <summary>
        /// 打分与评判的主体，Title的打分需要单独存储，这是因为其它部分的打分会破坏掉其含义
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="RootNode"></param>
        /// <param name="LinkPattern"></param>
        /// <param name="ExtractContent"></param>
        /// <param name="Strategy"></param>
        /// <returns></returns>
        public static ListPagePattern List_getAllFieldsXPath_SVM(string Url, HtmlNode RootNode, HtmlPattern LinkPattern, bool ExtractContent, SoftStrategy Strategy)
        {
            List<HtmlNode> BaseNodes = RootNode.SelectNodes(LinkPattern.ItemBaseXPath).ToList();
            if (BaseNodes == null) return null;


            if (string.IsNullOrWhiteSpace(LinkPattern.RelXPath))
            {
                ListPagePattern lpxp = new ListPagePattern();
                lpxp.Path.ItemRootXPath = LinkPattern.ItemBaseXPath;
                lpxp.TitleScore = LinkPattern.Score;
                lpxp.TotalScore = LinkPattern.Score + 400;//toto:用hardthreshold来解决这个问题。别忘了下面也有一个
                lpxp.Path.TitleXPath = LinkPattern.RelXPath;
                lpxp.Path.UrlXPath = LinkPattern.RelXPath;

                return lpxp;
            }

            //对ItemBaseNode随机抽样3个
            IEnumerable<HtmlNode> SampleItemBaseNodes = RootNode.SelectNodes(LinkPattern.ItemBaseXPath).OrderBy(x => rmd.Next(10000)).Take(3);

            //包含两个数字的Path集合
            HashSet<string> Pattern_Contain2Numbers = new HashSet<string>();

            Dictionary<PatternType, Dictionary<string, double>> Scores = new Dictionary<PatternType, Dictionary<string, double>>();
            Dictionary<string, double> AvgNumber = new Dictionary<string, double>();
            Scores.Add(PatternType.Author, new Dictionary<string, double>());
            Scores.Add(PatternType.AbsTract, new Dictionary<string, double>());
            Scores.Add(PatternType.Date, new Dictionary<string, double>());
            Scores.Add(PatternType.MediaName, new Dictionary<string, double>());
            Scores.Add(PatternType.ViewnReply, new Dictionary<string, double>());

            List<string> TextPatternsOriginal = new List<string>();
            //这一段对scores的初始化也打包放到hardthreshold里去好了，都是每次过后必须调整的。
            //对每一个采样的BaseNode
            foreach (HtmlNode SampleBaseNode in SampleItemBaseNodes)

                if (SampleBaseNode.SelectNodes(LinkPattern.RelXPath) != null)
                {
                    //取出这个BaseNode下的全部Text类型Node（排除掉Title）

                    List<HtmlNode> TitleNode = SampleBaseNode.SelectNodes(LinkPattern.RelXPath).ToList();
                    List<HtmlNode> BasicTextNodes = SampleBaseNode.Descendants().Where(n => n.NodeType == HtmlNodeType.Text && n.ParentNode.Name != "script" && !TitleNode.Contains(n.ParentNode) && !string.IsNullOrEmpty(TextCleaner.FullClean(n.InnerText))).ToList();
                    List<HtmlNode> TextNodes = new List<HtmlNode>();
                    foreach (HtmlNode textnode in BasicTextNodes)
                    {
                        TextNodes.Add(textnode);
                        if (textnode.ParentNode.ChildNodes.Count == 1)
                            TextNodes.Add(textnode.ParentNode);
                    }

                    //提权这个BaseNode下的候选RelXPath，不过滤不打分

                    List<HtmlPattern> TextPattern = List_HtmlPattern_getRelXPath(TextNodes, RootNode, LinkPattern, Strategy, null, null, LinkPattern.ItemCount, PatternType.Unknown);
                    if (TextPattern != null)
                        foreach (HtmlPattern Pattern in TextPattern)
                            if (!string.IsNullOrWhiteSpace(Pattern.RelXPath))
                                if (!TextPatternsOriginal.Contains(Pattern.RelXPath))
                                    TextPatternsOriginal.Add(Pattern.RelXPath);

                }//end foreach ItemBaseNode

            //去重，不然太多变了
            List<string> TextPatterns = new List<string>();

            foreach (string relxpath in TextPatternsOriginal)
            {
                //先检查是不是有重复的
                bool notrepeated = true;
                foreach (string comparexpath in TextPatterns)
                    if (isthesameRelXpath(BaseNodes, relxpath, comparexpath))
                    { notrepeated = false; break; }
                if (!notrepeated)
                    continue;
                //再检查分数是不是有超出的。因为排列组合最多需要8个，所以只取前八个
                List<HtmlNode> Nodes = new List<HtmlNode>();

                foreach (HtmlNode basenode in BaseNodes)
                {
                    IEnumerable<HtmlNode> nodes = basenode.SelectNodes(relxpath);
                    if (nodes != null) Nodes.AddRange(nodes);
                }
                if (Nodes != null)
                {
                    double avgnumber = 0;
                    Dictionary<PatternType, double> scores = Strategy.ScoreforRel(Nodes, LinkPattern.ItemCount, ref avgnumber, Strategy);
                    AvgNumber.Add(relxpath, avgnumber);
                    foreach (PatternType pt in scores.Keys)
                        if (Scores[pt].Keys.Count() < 8)
                        {
                            Scores[pt].Add(relxpath, scores[pt]);
                            TextPatterns.Add(relxpath);
                        }
                        else if (Scores[pt].Values.Where(n => n > scores[pt]).Count() < 8)
                        {
                            Scores[pt].Add(relxpath, scores[pt]);
                            TextPatterns.Add(relxpath);

                            string path = Scores[pt].Keys.FirstOrDefault(s => Scores[pt][s] < scores[pt]);
                            if (!string.IsNullOrWhiteSpace(path) && Scores[pt].ContainsKey(path))
                            {
                                Scores[pt].Remove(path);
                                //然后检查这种Path是否已经完全被抛弃了，是则丢掉不再参与迭代与比较
                                bool con = false;
                                foreach (PatternType spt in Scores.Keys)
                                    if (Scores[pt].ContainsKey(path))
                                        con = true;
                                if (!con)
                                {
                                    TextPatterns.Remove(path);
                                    //AvgNumber.Remove(path);
                                }
                            }
                        }
                }
            }

            //打分步骤

            //补充一个空值给分
            foreach (PatternType kvp in Scores.Keys)
                Scores[kvp].Add(string.Empty, 200);
            AvgNumber.Add(string.Empty, 0);

            //多层级遍历寻找最合适的解
            ListPagePattern ResultPageXPath = SelectCompositions_SVM(Scores, AvgNumber, LinkPattern, Strategy);

            return ResultPageXPath;
        }


        /// <summary>
        /// 打分与评判的主体，Title的打分需要单独存储，这是因为其它部分的打分会破坏掉其含义
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="RootNode"></param>
        /// <param name="LinkPattern"></param>
        /// <param name="ExtractContent"></param>
        /// <param name="Strategy"></param>
        /// <returns></returns>
        public static ListPagePattern List_getAllFieldsXPath(string Url, HtmlNode RootNode, HtmlPattern LinkPattern, bool ExtractContent, SoftStrategy Strategy)
        {
            List<HtmlNode> BaseNodes = RootNode.SelectNodes(LinkPattern.ItemBaseXPath).ToList();
            if (BaseNodes == null) return null;


            if (string.IsNullOrWhiteSpace(LinkPattern.RelXPath))
            {
                ListPagePattern lpxp = new ListPagePattern();
                lpxp.Path.ItemRootXPath = LinkPattern.ItemBaseXPath;
                lpxp.TitleScore = LinkPattern.Score;
                lpxp.TotalScore = LinkPattern.Score + 400;//toto:用hardthreshold来解决这个问题。别忘了下面也有一个
                lpxp.Path.TitleXPath = LinkPattern.RelXPath;
                lpxp.Path.UrlXPath = LinkPattern.RelXPath;

                return lpxp;
            }

            //对ItemBaseNode随机抽样3个
            IEnumerable<HtmlNode> SampleItemBaseNodes = RootNode.SelectNodes(LinkPattern.ItemBaseXPath).OrderBy(x => rmd.Next(10000)).Take(3);

            //包含两个数字的Path集合
            HashSet<string> Pattern_Contain2Numbers = new HashSet<string>();

            Dictionary<PatternType, Dictionary<string, double>> Scores = new Dictionary<PatternType, Dictionary<string, double>>();
            //Dictionary<string, double> AvgNumber = new Dictionary<string, double>();
            Scores.Add(PatternType.Author, new Dictionary<string, double>());
            Scores.Add(PatternType.AbsTract, new Dictionary<string, double>());
            Scores.Add(PatternType.Date, new Dictionary<string, double>());
            Scores.Add(PatternType.MediaName, new Dictionary<string, double>());
            Scores.Add(PatternType.View, new Dictionary<string, double>());
            Scores.Add(PatternType.Reply, new Dictionary<string, double>());

            #region BEGIN
            ListStrategy strategy = new ListStrategy(Strategy.MediaType, Strategy.Language);
            //格子内的平均数字
            Dictionary<string, double> Avg_View = new Dictionary<string, double>();
            Dictionary<string, double> Avg_Reply = new Dictionary<string, double>();

            //对每一个采样的BaseNode
            foreach (HtmlNode SampleBaseNode in SampleItemBaseNodes)
                if (SampleBaseNode.SelectNodes(LinkPattern.RelXPath) != null)
                {
                    //取出这个BaseNode下的全部Text类型Node（排除掉Title）
                    List<HtmlNode> TitleNode = SampleBaseNode.SelectNodes(LinkPattern.RelXPath).ToList();
                    List<HtmlNode> TextNodes = SampleBaseNode.Descendants().Where(n => n.NodeType == HtmlNodeType.Text && n.ParentNode.Name != "script" && !TitleNode.Contains(n.ParentNode) && !string.IsNullOrEmpty(TextCleaner.FullClean(n.InnerText))).ToList();

                    //提权这个BaseNode下的候选RelXPath，不过滤不打分
                    List<HtmlPattern> TextPatterns = List_HtmlPattern_getRelXPath(TextNodes, RootNode, LinkPattern, Strategy, null, null, LinkPattern.ItemCount);

                    //对每个Text类型导出的Pattern，可能性进行打分（并标识Must/MustNot）
                    foreach (HtmlPattern Pattern in TextPatterns)
                    {
                        //选出该Pattern下的节点
                        IEnumerable<HtmlNode> Nodes = SelectNode_ByRelXPath_FromBaseNode(SampleBaseNode, Pattern, TitleNode, FieldScoreStrategy.StopWords);
                        if (Nodes == null) continue;

                        //日期打分
                        double DateScore = FieldScoreStrategy.DateNodeRelPatternScore(Nodes, strategy, Nodes.Count(), Pattern.RelXPathLevel, Pattern.RelXPathUsingName);
                        if (Scores[PatternType.Date].ContainsKey(Pattern.RelXPath))
                            //如果已经存在则累计得分（全部BaseNode下的分数加在一起）
                            Scores[PatternType.Date][Pattern.RelXPath] += DateScore;
                        else if (DateScore > 0)
                            //这里最好要去重一次，如果实际采集的Node集合一样的话(当然重合可能性比较小)
                            Scores[PatternType.Date].Add(Pattern.RelXPath, DateScore);

                        //view/reply
                        bool Contain2Numbers, MustBeReply;
                        double[] AvgValue;
                        double ViewDiscount = 0, ReplyDiscount = 0;
                        double ViewScore = FieldScoreStrategy.ViewNodeRelPatternScore(Nodes, strategy, Nodes.Count(), Pattern.RelXPathLevel, Pattern.RelXPathUsingName, out Contain2Numbers, out MustBeReply, out AvgValue);
                        if (ViewScore > 0)
                        {
                            //根据MediaType有不同的策略加入:如果新闻默认是view 论坛默认是reply，有两个数字则都加，如果MustBeReply则加入Reply
                            switch (Strategy.MediaType)
                            {
                                default:
                                case Enums.MediaType.WebNews: //新闻，数字倾向于是点击，评论8折
                                    ViewDiscount = MustBeReply ? 0 : 1;
                                    ReplyDiscount = Contain2Numbers ? 1 : (MustBeReply ? 1 : 0.8);
                                    break;
                                case Enums.MediaType.Forum:     //论坛，数字倾向于评论，点击8折
                                    ViewDiscount = Contain2Numbers ? 1 : (MustBeReply ? 0 : 0.8);
                                    ReplyDiscount = 1;
                                    break;
                            }

                            //加入View
                            if (ViewDiscount > 0)
                            {
                                if (Scores[PatternType.View].ContainsKey(Pattern.RelXPath))
                                    //如果已经存在则累计得分（全部BaseNode下的分数加在一起）
                                    Scores[PatternType.View][Pattern.RelXPath] += ViewScore * ViewDiscount;
                                else if (ViewScore > 0)
                                    //这里最好要去重一次，如果实际采集的Node集合一样的话(当然重合可能性比较小)
                                    Scores[PatternType.View].Add(Pattern.RelXPath, ViewScore * ViewDiscount);

                                //记录数字均值，用于区分View/Reply
                                if (Avg_View.ContainsKey(Pattern.RelXPath))
                                    Avg_View[Pattern.RelXPath] += AvgValue[0];
                                else
                                    Avg_View.Add(Pattern.RelXPath, AvgValue[0]);
                            }

                            //加入Reply
                            if (ReplyDiscount > 0)
                            {
                                if (Scores[PatternType.Reply].ContainsKey(Pattern.RelXPath))
                                    //如果已经存在则累计得分（全部BaseNode下的分数加在一起）
                                    Scores[PatternType.Reply][Pattern.RelXPath] += ViewScore * ReplyDiscount;
                                else if (ViewScore > 0)
                                    //这里最好要去重一次，如果实际采集的Node集合一样的话(当然重合可能性比较小)
                                    Scores[PatternType.Reply].Add(Pattern.RelXPath, ViewScore * ReplyDiscount);

                                //记录数字均值，用于区分View/Reply
                                if (Avg_Reply.ContainsKey(Pattern.RelXPath))
                                    Avg_Reply[Pattern.RelXPath] += Contain2Numbers ? AvgValue[1] : AvgValue[0];
                                else
                                    Avg_Reply.Add(Pattern.RelXPath, Contain2Numbers ? AvgValue[1] : AvgValue[0]);
                            }

                            //记录Pattern_Contain2Numbers
                            if (Contain2Numbers)
                                Pattern_Contain2Numbers.Add(Pattern.RelXPath);
                        }

                        //媒体名
                        double MediaScore = FieldScoreStrategy.MediaNodeRelPatternScore(Nodes, strategy, Nodes.Count(), Pattern.RelXPathLevel, Pattern.RelXPathUsingName);
                        if (Scores[PatternType.MediaName].ContainsKey(Pattern.RelXPath))
                            //如果已经存在则累计得分（全部BaseNode下的分数加在一起）
                            Scores[PatternType.MediaName][Pattern.RelXPath] += MediaScore;
                        else if (MediaScore > 0)
                            //这里最好要去重一次，如果实际采集的Node集合一样的话(当然重合可能性比较小)
                            Scores[PatternType.MediaName].Add(Pattern.RelXPath, MediaScore);

                        //作者
                        double AuthorScore = FieldScoreStrategy.AuthorNodeRelPatternScore(Nodes, strategy, Nodes.Count(), Pattern.RelXPathLevel, Pattern.RelXPathUsingName);
                        if (Scores[PatternType.Author].ContainsKey(Pattern.RelXPath))
                            //如果已经存在则累计得分（全部BaseNode下的分数加在一起）
                            Scores[PatternType.Author][Pattern.RelXPath] += AuthorScore;
                        else if (AuthorScore > 0)
                            //这里最好要去重一次，如果实际采集的Node集合一样的话(当然重合可能性比较小)
                            Scores[PatternType.Author].Add(Pattern.RelXPath, AuthorScore);
                    }//end foreach Patten under this ItemBaseNode
                }//end foreach ItemBaseNode

            //规则1：将Date中可能性高于85%的XPath，在其他字段中1/4
            double Threshhold = 0.85 * SampleItemBaseNodes.Count();
            foreach (string Key in Scores[PatternType.Date].Keys.Where(k => Scores[PatternType.Date][k] > Threshhold))
            {
                if (Scores[PatternType.View].ContainsKey(Key))
                    Scores[PatternType.View][Key] /= 4;
                if (Scores[PatternType.Reply].ContainsKey(Key))
                    Scores[PatternType.Reply][Key] /= 4;
                if (Scores[PatternType.MediaName].ContainsKey(Key))
                    Scores[PatternType.MediaName][Key] /= 4;
                if (Scores[PatternType.Author].ContainsKey(Key))
                    Scores[PatternType.Author][Key] /= 4;
            }

            //规则2：将View、Reply中可能性高于85%的XPath，在Media、Author字段中降低一半
            foreach (string Key in Scores[PatternType.View].Keys.Where(k => Scores[PatternType.View][k] > Threshhold)
                .Concat(Scores[PatternType.Reply].Keys.Where(k => Scores[PatternType.Reply][k] > Threshhold)).Distinct().ToArray())
            {
                if (Scores[PatternType.MediaName].ContainsKey(Key))
                    Scores[PatternType.MediaName][Key] /= 2;
                if (Scores[PatternType.Author].ContainsKey(Key))
                    Scores[PatternType.Author][Key] /= 2;
            }

            //规则3：去掉平均可能性<8%的
            Threshhold = 0.08 * SampleItemBaseNodes.Count();
            foreach (string Key in Scores[PatternType.Date].Keys.Where(k => Scores[PatternType.Date][k] < Threshhold).ToArray()) Scores[PatternType.Date].Remove(Key);
            foreach (string Key in Scores[PatternType.View].Keys.Where(k => Scores[PatternType.View][k] < Threshhold).ToArray()) Scores[PatternType.View].Remove(Key);
            foreach (string Key in Scores[PatternType.Reply].Keys.Where(k => Scores[PatternType.Reply][k] < Threshhold).ToArray()) Scores[PatternType.Reply].Remove(Key);
            foreach (string Key in Scores[PatternType.MediaName].Keys.Where(k => Scores[PatternType.MediaName][k] < Threshhold).ToArray()) Scores[PatternType.MediaName].Remove(Key);
            foreach (string Key in Scores[PatternType.Author].Keys.Where(k => Scores[PatternType.Author][k] < Threshhold).ToArray()) Scores[PatternType.Author].Remove(Key);

            //特殊情况1：date和view或reply冲突，后两者只有一个候选，且与date的一个pattern相同，则只能保留一个
            if (Scores[PatternType.View].Count == 1 && Scores[PatternType.Date].ContainsKey(Scores[PatternType.View].Keys.First())
                && Scores[PatternType.View].First().Value < Scores[PatternType.Date][Scores[PatternType.View].Keys.First()] * 2)
                Scores[PatternType.View].Clear();
            if (Scores[PatternType.Reply].Count == 1 && Scores[PatternType.Date].ContainsKey(Scores[PatternType.Reply].Keys.First())
                && Scores[PatternType.Reply].First().Value < Scores[PatternType.Date][Scores[PatternType.Reply].Keys.First()] * 2)
                Scores[PatternType.Reply].Clear();

            //特殊情况2：view和reply冲突，两者指向同一个单一数字的节点，且view和reply都只有一个候选节点，则只保留1个
            if (Scores[PatternType.View].Count == 1 && Scores[PatternType.Reply].Count == 1 && Scores[PatternType.View].Keys.First() == Scores[PatternType.Reply].Keys.First()
                && !Pattern_Contain2Numbers.Contains(Scores[PatternType.View].Keys.First()))
            {
                string SamePattern = Scores[PatternType.View].Keys.First();
                if (Scores[PatternType.View][SamePattern] > Scores[PatternType.Reply][SamePattern])
                    Scores[PatternType.Reply].Clear();
                else if (Scores[PatternType.View][SamePattern] < Scores[PatternType.Reply][SamePattern])
                    Scores[PatternType.View].Clear();
                else //相等的情况
                    if (Strategy.MediaType != Enums.MediaType.Forum)
                    Scores[PatternType.Reply].Clear();
                else
                    Scores[PatternType.View].Clear();
            }

            //特殊情况3：media和author冲突，两者相同且只有一个
            if (Scores[PatternType.MediaName].Count == 1 && Scores[PatternType.Author].Count == 1 && Scores[PatternType.MediaName].Keys.First() == Scores[PatternType.Author].Keys.First())
            {
                if (Scores[PatternType.MediaName].First().Value > Scores[PatternType.Author].First().Value)
                    Scores[PatternType.Author].Clear();
                else
                    Scores[PatternType.MediaName].Clear();
            }
            #endregion

            List<PatternType> scoreKeyList = new List<PatternType>();
            scoreKeyList.AddRange(Scores.Keys);

            //补充一个空值给分
            foreach (PatternType kvp in scoreKeyList)
            {
                Scores[kvp].Add(string.Empty, 0.4);

                List<string> pathList = new List<string>();
                pathList.AddRange(Scores[kvp].Keys);

                foreach (string _path in pathList)
                    Scores[kvp][_path] = 500 * Scores[kvp][_path];
            }
            Avg_View.Add(string.Empty, 0);
            Avg_Reply.Add(string.Empty, 0);

            ListPagePattern ResultPageXPath = SelectCompositions(Scores, Avg_View, Avg_Reply, LinkPattern, Strategy);

            return ResultPageXPath;
        }

        /// <summary>
        /// 多重遍历寻找最优解，顺便把其他可选择的项目都拿出来了。为了能够跳出多重循环，单独拿出来放入一个函数中
        /// </summary>
        /// <param name="Scores"></param>
        /// <param name="AvgNumber"></param>
        /// <param name="LinkPattern"></param>
        /// <param name="Strategy"></param>
        /// <returns></returns>
        public static ListPagePattern SelectCompositions_SVM(Dictionary<PatternType, Dictionary<string, double>> Scores, Dictionary<string, double> AvgNumber, HtmlPattern LinkPattern, SoftStrategy Strategy)
        {
            List<ListPagePattern> All = new List<ListPagePattern>();

            ListPagePattern HighestPattern = new ListPagePattern();

            List<string> allpath = new List<string>();
            foreach (string path in AvgNumber.Keys)
            {
                bool con = false;
                foreach (PatternType pt in Scores.Keys)
                    if (Scores[pt].ContainsKey(path))
                        con = true;
                if (con) allpath.Add(path);
            }
            IEnumerable<PatternType> allpt = Scores.Keys;


            //加一步验证确保每种只有八个路径进入迭代
            foreach (PatternType pt in allpt)
                foreach (string path in allpath)
                {
                    if (Scores[pt].Count() < 8) continue;
                    if (Scores[pt].ContainsKey(path) && Scores[pt].Values.Where(n => n > Scores[pt][path]).Count() > 7)
                        Scores[pt].Remove(path);
                }

            foreach (string abs in Scores[PatternType.AbsTract].Keys)
                if (Scores[PatternType.AbsTract][abs] != 0)
                    foreach (string reply in Scores[PatternType.ViewnReply].Keys)
                        if (Scores[PatternType.ViewnReply][reply] != 0)
                            foreach (string view in Scores[PatternType.ViewnReply].Keys)
                                if (Scores[PatternType.ViewnReply][view] != 0)
                                    foreach (string author in Scores[PatternType.Author].Keys)
                                        if (Scores[PatternType.Author][author] != 0)
                                            foreach (string media in Scores[PatternType.MediaName].Keys)
                                                if (Scores[PatternType.MediaName][media] != 0)
                                                    foreach (string date in Scores[PatternType.Date].Keys)
                                                        if (Scores[PatternType.Date][date] != 0)
                                                        {
                                                            if (!string.IsNullOrWhiteSpace(media) && (Strategy.MediaType == Enums.MediaType.Forum && media == date)) continue;
                                                            if (!string.IsNullOrWhiteSpace(author) && (Strategy.MediaType == Enums.MediaType.Forum && (author == media || author == date))) continue;
                                                            if ((!string.IsNullOrWhiteSpace(view) && (view == date || view == media || view == author)) || (!string.IsNullOrWhiteSpace(reply) && (reply == date || reply == media || reply == author))) continue;
                                                            if (!string.IsNullOrWhiteSpace(abs) && (abs == date || abs == media || abs == author || abs == view || abs == reply)) continue;
                                                            if (!string.IsNullOrWhiteSpace(view) && !string.IsNullOrWhiteSpace(reply) && AvgNumber[view] < AvgNumber[reply]) continue;

                                                            ListPagePattern pattern = new ListPagePattern();
                                                            //分项分数
                                                            pattern.TitleScore = LinkPattern.Score;

                                                            pattern.DataTimeScore = Scores[PatternType.Date][date];
                                                            pattern.MediaScore = Scores[PatternType.MediaName][media];
                                                            pattern.AuthorScore = Scores[PatternType.Author][author];
                                                            pattern.ViewScore = Scores[PatternType.ViewnReply][view];
                                                            pattern.ReplyScore = Scores[PatternType.ViewnReply][reply];
                                                            pattern.AbstractScore = Scores[PatternType.AbsTract][abs];

                                                            //总分
                                                            pattern.TotalScore = LinkPattern.Score;
                                                            pattern.TotalScore += Scores[PatternType.Date][date];
                                                            pattern.TotalScore += Scores[PatternType.MediaName][media];
                                                            pattern.TotalScore += Scores[PatternType.Author][author];
                                                            pattern.TotalScore += Scores[PatternType.ViewnReply][view];
                                                            pattern.TotalScore += Scores[PatternType.ViewnReply][reply];
                                                            pattern.TotalScore += Scores[PatternType.AbsTract][abs];

                                                            if (HighestPattern != null && HighestPattern.TotalScore != null && HighestPattern.TotalScore > pattern.TotalScore)
                                                                continue;

                                                            pattern.Path.ItemRootXPath = LinkPattern.ItemBaseXPath;
                                                            pattern.Path.TitleXPath = LinkPattern.RelXPath;
                                                            pattern.Path.UrlXPath = LinkPattern.RelXPath;

                                                            pattern.Path.DateXPath = date;
                                                            pattern.Path.MediaNameXPath = media;
                                                            pattern.Path.AuthorXPath = author;
                                                            pattern.Path.ViewXPath = view;
                                                            pattern.Path.ReplyXPath = reply;
                                                            pattern.Path.AbsTractXPath = abs;

                                                            HighestPattern = pattern;
                                                        }

            if (HighestPattern.TotalScore != null && HighestPattern.TotalScore > 0)
                All.Add(HighestPattern);

            //foreach (PatternType pt in allpt)
            //    foreach (string path in allpath)
            //        if (Scores[pt].ContainsKey(path) && Scores[pt].Values.Where(n => n > Scores[pt][path]).Count() > 3)
            //            Scores[pt].Remove(path);

            foreach (PatternType pt in allpt)
            {
                //int takenumber = Scores[pt].Count() - 1; //3;
                //if (Scores[pt].Count() < 4 && Scores[pt].Count() > 1) takenumber = Scores[pt].Count() - 1;
                if (Scores[pt].Count() == 1)
                {
                    continue;
                }
                HighestPattern.BackUpPaths.Add(pt, new List<string>());
                foreach (string relpath in Scores[pt].Keys.OrderByDescending(s => Scores[pt][s]))//.Take(takenumber))
                    HighestPattern.BackUpPaths[pt].Add(relpath);
            }

            return (HighestPattern);
        }


        /// <summary>
        /// 多重遍历寻找最优解，顺便把其他可选择的项目都拿出来了。为了能够跳出多重循环，单独拿出来放入一个函数中
        /// </summary>
        /// <param name="Scores"></param>
        /// <param name="AvgViewNumber"></param>
        /// <param name="AvgReplyNumber"></param>
        /// <param name="LinkPattern"></param>
        /// <param name="Strategy"></param>
        /// <returns></returns>
        public static ListPagePattern SelectCompositions(Dictionary<PatternType, Dictionary<string, double>> Scores, Dictionary<string, double> AvgViewNumber, Dictionary<string, double> AvgReplyNumber, HtmlPattern LinkPattern, SoftStrategy Strategy)
        {
            List<ListPagePattern> All = new List<ListPagePattern>();

            ListPagePattern HighestPattern = new ListPagePattern();

            List<string> allpath = new List<string>();
            foreach (string path in AvgViewNumber.Keys)
            {
                bool con = false;
                foreach (PatternType pt in Scores.Keys)
                    if (Scores[pt].ContainsKey(path))
                        con = true;
                if (con) allpath.Add(path);
            }
            IEnumerable<PatternType> allpt = Scores.Keys;


            //加一步验证确保每种只有八个路径进入迭代
            foreach (PatternType pt in allpt)
                foreach (string path in allpath)
                {
                    if (Scores[pt].Count() < 8) continue;
                    if (Scores[pt].ContainsKey(path) && Scores[pt].Values.Where(n => n > Scores[pt][path]).Count() > 7)
                        Scores[pt].Remove(path);
                }

            foreach (string abs in Scores[PatternType.AbsTract].Keys)
                if (Scores[PatternType.AbsTract][abs] != 0)
                    foreach (string reply in Scores[PatternType.Reply].Keys)
                        if (Scores[PatternType.Reply][reply] != 0)
                            foreach (string view in Scores[PatternType.View].Keys)
                                if (Scores[PatternType.View][view] != 0)
                                    foreach (string author in Scores[PatternType.Author].Keys)
                                        if (Scores[PatternType.Author][author] != 0)
                                            foreach (string media in Scores[PatternType.MediaName].Keys)
                                                if (Scores[PatternType.MediaName][media] != 0)
                                                    foreach (string date in Scores[PatternType.Date].Keys)
                                                        if (Scores[PatternType.Date][date] != 0)
                                                        {
                                                            if (!string.IsNullOrWhiteSpace(media) && (Strategy.MediaType == Enums.MediaType.Forum && media == date)) continue;
                                                            if (!string.IsNullOrWhiteSpace(author) && (Strategy.MediaType == Enums.MediaType.Forum && (author == media || author == date))) continue;
                                                            if ((!string.IsNullOrWhiteSpace(view) && (view == date || view == media || view == author)) || (!string.IsNullOrWhiteSpace(reply) && (reply == date || reply == media || reply == author))) continue;
                                                            if (!string.IsNullOrWhiteSpace(abs) && (abs == date || abs == media || abs == author || abs == view || abs == reply)) continue;
                                                            if (!string.IsNullOrWhiteSpace(view) && !string.IsNullOrWhiteSpace(reply) && AvgViewNumber[view] <= AvgReplyNumber[reply]) continue;
                                                            if (!string.IsNullOrWhiteSpace(reply) && reply == view) continue;

                                                            ListPagePattern pattern = new ListPagePattern();
                                                            //分项分数
                                                            pattern.TitleScore = LinkPattern.Score;

                                                            pattern.DataTimeScore = Scores[PatternType.Date][date];
                                                            pattern.MediaScore = Scores[PatternType.MediaName][media];
                                                            pattern.AuthorScore = Scores[PatternType.Author][author];
                                                            pattern.ViewScore = Scores[PatternType.View][view];
                                                            pattern.ReplyScore = Scores[PatternType.Reply][reply];
                                                            pattern.AbstractScore = Scores[PatternType.AbsTract][abs];

                                                            //总分
                                                            pattern.TotalScore = LinkPattern.Score;
                                                            pattern.TotalScore += Scores[PatternType.Date][date];
                                                            pattern.TotalScore += Scores[PatternType.MediaName][media];
                                                            pattern.TotalScore += Scores[PatternType.Author][author];
                                                            pattern.TotalScore += Scores[PatternType.View][view];
                                                            pattern.TotalScore += Scores[PatternType.Reply][reply];
                                                            pattern.TotalScore += Scores[PatternType.AbsTract][abs];

                                                            if (HighestPattern != null && HighestPattern.TotalScore != null && HighestPattern.TotalScore > pattern.TotalScore)
                                                                continue;

                                                            pattern.Path.ItemRootXPath = LinkPattern.ItemBaseXPath;
                                                            pattern.Path.TitleXPath = LinkPattern.RelXPath;
                                                            pattern.Path.UrlXPath = LinkPattern.RelXPath;

                                                            pattern.Path.DateXPath = date;
                                                            pattern.Path.MediaNameXPath = media;
                                                            pattern.Path.AuthorXPath = author;
                                                            pattern.Path.ViewXPath = view;
                                                            pattern.Path.ReplyXPath = reply;
                                                            pattern.Path.AbsTractXPath = abs;

                                                            HighestPattern = pattern;
                                                        }

            if (HighestPattern.TotalScore != null && HighestPattern.TotalScore > 0)
                All.Add(HighestPattern);

            //foreach (PatternType pt in allpt)
            //    foreach (string path in allpath)
            //        if (Scores[pt].ContainsKey(path) && Scores[pt].Values.Where(n => n > Scores[pt][path]).Count() > 3)
            //            Scores[pt].Remove(path);

            foreach (PatternType pt in allpt)
            {
                HighestPattern.BackUpPaths.Add(pt, new List<string>());
                foreach (string relpath in Scores[pt].Keys.OrderByDescending(s => Scores[pt][s]))//.Take(takenumber))
                    HighestPattern.BackUpPaths[pt].Add(relpath);
            }

            return HighestPattern;
        }


        #region 寻找简化路径的方法及众接口
        /// <summary>
        /// getrelxpath，一看就知道是什么了吧
        /// 但是泛用性太狭窄了，觉得可以升级一下。放到xpathutility里最好了。
        /// 最好能改成：针对一组同层或不同层的nodes和一个rootnode，可以给出相对路径；
        /// 针对一组node和一组basenode，可以给出相对路径；
        /// 针对一个node和一个rootnode，也可以给出相对路径。这是明天的主要工作。
        /// todo:进程将暂时往后调
        /// </summary>
        /// <param name="SeedNodes"></param>
        /// <param name="RootNode"></param>
        /// <param name="LinkPattern"></param>
        /// <param name="NodeFilter"></param>
        /// <param name="NodeScore"></param>
        /// <returns></returns>
        internal static List<HtmlPattern> GetShorternXPaths(IEnumerable<HtmlNode> BasicNodes, HtmlNode RootNode, int CellingLevel, SoftStrategy Strategy, NodeValidFilter NodeFilter, NodePatternScore NodeScore, int BestBaseItemCount, PatternType pt = PatternType.Unknown, bool NeedScore = true)
        {


            //存放临时候选的RelXPath
            HashSet<string> NewRelXPaths = new HashSet<string>();
            List<HtmlNode> ItemBaseNodes = RootNode.SelectNodes("").ToList();
            //第一步，检查从叶子到ItemRoot之间的所有ID和Class是否可用，生成全部候选的RelXPath
            foreach (HtmlNode Node in BasicNodes)
            {
                HtmlNode current = Node;
                if (Node.NodeType != HtmlNodeType.Element && pt == PatternType.Title) current = current.ParentNode;
                if ((Node.NodeType != HtmlNodeType.Element && Node.NodeType != HtmlNodeType.Text && pt != PatternType.Title) || current.ParentNode.Name == "a") current = current.ParentNode;
                if (current.ParentNode.ChildNodes.Count() == 1 && current.Name == "#text") current = current.ParentNode;
                if (current.Name == "#text") current.Name = "text()";
                //A标签要测试下其父级是否是否可以代表之，以避免某些媒体或作者名字没有链接的情况
                //这里的逻辑有问题
                //if (ATagTryParent && current.Name == "a" && TextCleaner.FullClean(current.InnerText) == TextCleaner.FullClean(current.ParentNode.InnerText))
                //    current = current.ParentNode;

                //找到真正的ItemBaseNode
                HtmlNode ItemBaseNode = current;
                while (!ItemBaseNodes.Contains(ItemBaseNode) && !XPathUtility.isTopNode(ItemBaseNode)) ItemBaseNode = ItemBaseNode.ParentNode;
                //路径前缀：从叶子到ItemBase之间的(去下标，去掉开头'/')
                string RelXPathPrefix;
                if (ItemBaseNode.XPath.Length == current.XPath.Length) RelXPathPrefix = string.Empty;
                else RelXPathPrefix = XPathUtility.removeXPathOrderNumber(current.XPath.Substring(ItemBaseNode.XPath.Length + 1), CellingLevel);
                //路径后缀：加在当前节点的命名XPath之后来指向叶子节点
                string RelXPathSuffix = string.Empty;

                //从每个Node开始向上,直到ItemRoot级别
                int ClimbLevel = 0;
                while (ClimbLevel < CellingLevel && !ItemBaseNodes.Contains(current))
                {
                    //有ID，则根据ID生成一个候选的相对路径
                    if (!string.IsNullOrWhiteSpace(current.Id))
                        NewRelXPaths.Add(string.Format(@".//{0}[@id={1}]{2}", current.Name, current.Id, RelXPathSuffix));

                    //有Class，也生成一个相对路径
                    if (current.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(current.Attributes["class"].Value) && !current.Attributes["class"].Value.Contains('$'))
                        foreach (string className in current.Attributes["class"].Value.Split())
                            NewRelXPaths.Add(string.Format(@".//{0}[contains(@class,'{1}')]{2}", current.Name, className, RelXPathSuffix));

                    int index = 0;
                    //同一级有多个同类型的元素，则需要根据每个序号生成一个候选相对路径(例如bbs一个div中两个a,后一个是用户名)
                    if ((current.NodeType == HtmlNodeType.Element || (current.NodeType == HtmlNodeType.Text && pt == PatternType.Unknown)) && !XPathUtility.isTopNode(current))
                        //如果按此RelPath在每个BaseNode下可得到多个，则应该用序号来选出唯一一个；
                        //除非特殊情况，所有的Item都在同一个BaseNode下（例如某些狗屎网站，很多a写在一个div里，用br分开）
                        if (current.ParentNode.SelectNodes(@"./" + current.Name).Count > 1)
                        {

                            for (int i = 0; i < Math.Min(6, current.ParentNode.SelectNodes(@"./" + current.Name).Count); i++)
                            {
                                if (current.ParentNode.SelectNodes(@"./" + current.Name + "[" + (i + 1).ToString() + "]").Contains(current)) index = i + 1;
                                NewRelXPaths.Add(string.Format("{0}[{1}]{2}", RelXPathPrefix, i + 1, RelXPathSuffix));
                            }
                        }
                        else
                            NewRelXPaths.Add(string.Format("{0}{1}", RelXPathPrefix, RelXPathSuffix));

                    //向上
                    if (current.ParentNode.SelectNodes(@"./" + current.Name).Count > 1 && index != 0)
                        RelXPathSuffix = string.Format("/{0}{1}{2}{3}{4}", current.Name, RelXPathSuffix, '[', index.ToString(), ']');
                    else
                        RelXPathSuffix = string.Format("/{0}{1}", current.Name, RelXPathSuffix);
                    if (!string.IsNullOrWhiteSpace(RelXPathPrefix))
                        if (RelXPathPrefix.IndexOf('/') >= 0)
                            RelXPathPrefix = RelXPathPrefix.Substring(0, RelXPathPrefix.LastIndexOf('/'));
                        else
                            RelXPathPrefix = string.Empty;
                    current = current.ParentNode;
                    ClimbLevel++;
                }

            }//end foreach node

            //第二步，过滤和打分RelXPath


            List<HtmlPattern> NewPatterns = new List<HtmlPattern>(NewRelXPaths.Count);
            Dictionary<string, HashSet<string>> PatternsTextSet = new Dictionary<string, HashSet<string>>(NewRelXPaths.Count);
            foreach (string RelXPath in NewRelXPaths.Distinct())
            {
                //从每一个Base节点去提取Rel对应的节点
                List<HtmlNode> Nodes = new List<HtmlNode>(BestBaseItemCount + 2);
                HashSet<string> NodesTextSet_ThisPattern = new HashSet<string>();
                foreach (HtmlNode OneBaseNode in ItemBaseNodes)
                {
                    List<HtmlNode> leaveNodes = new List<HtmlNode>();
                    if (NewRelXPaths.Count == 1 && NewRelXPaths.First(n => true) == string.Empty)
                        leaveNodes.Add(OneBaseNode);
                    else
                    {

                        HtmlNodeCollection leaveNodescollection = OneBaseNode.SelectNodes(RelXPath);
                        if (leaveNodescollection != null) leaveNodes = leaveNodescollection.ToList();
                        else leaveNodes = null;
                    }
                    if (leaveNodes != null)
                    {
                        //过滤无效Nodes

                        IEnumerable<HtmlNode> filterNodes = leaveNodes.Where(a => NodeFilter == null && !string.IsNullOrEmpty(a.InnerText) || NodeFilter != null && NodeFilter(a));
                        Nodes.AddRange(filterNodes);
                        //提取文本集合
                        foreach (HtmlNode node in filterNodes)
                        {
                            string Text = XPathUtility.InnerTextNonDescendants(node);
                            if (!string.IsNullOrEmpty(Text))
                            {
                                if (NodesTextSet_ThisPattern.Contains(Text))
                                {
                                    int i = 0;
                                    Text = Text + "000" + i.ToString();
                                    while (NodesTextSet_ThisPattern.Contains(Text))
                                    {
                                        Text = Text.Remove(Text.Length - i++.ToString().Length);
                                        Text = Text + i.ToString();
                                    }
                                }
                                NodesTextSet_ThisPattern.Add(Text);
                            }
                        }
                    }
                }
                if (Nodes == null) continue;

                //特殊处理：对于节点内的文本内容是无意义序号的，发现后直接忽略(top3可能特殊格式，因此从4开始)
                if (Nodes.Count > 3 && Nodes.Count >= ItemBaseNodes.Count - 4 && Nodes.Count <= ItemBaseNodes.Count
                    && NodesTextSet_ThisPattern.Contains("4") && NodesTextSet_ThisPattern.Contains("5"))
                {
                    bool GotEveryNumber = true;
                    //检查一遍是不是每个数字都有
                    for (int i = 4; i < ItemBaseNodes.Count - 3; i++)
                        if (!NodesTextSet_ThisPattern.Contains(i.ToString()))
                        {
                            GotEveryNumber = false;
                            break;
                        }

                    //全部是序号数字，忽略本Pattern
                    if (GotEveryNumber) continue;
                }

                //文本集合是否命中禁用词
                if (NodesTextSet_ThisPattern.Count == 0 || (NodesTextSet_ThisPattern.Count == 1 && HardThreshold.BanWords_NodeSelect.Contains(NodesTextSet_ThisPattern.First().ToLower()))) continue;

                //保存文本集合用于去重
                PatternsTextSet.Add(RelXPath, NodesTextSet_ThisPattern);


                if (pt == PatternType.Title && (NewRelXPaths.Count == 0 || NewRelXPaths == null))
                    NewRelXPaths.Add(string.Empty);

                //加入候选列表
                HtmlPattern NewPattern = new HtmlPattern();
                NewPattern.RelXPath = RelXPath;
                NewPattern.ItemCount = Nodes.Count;
                int SumTextLen = 0;
                Nodes.ForEach(a => SumTextLen += TextStatisticsUtility.GetWeightedLength(HTMLCleaner.GetCleanInnerText(a), HardThreshold.StopWords));
                NewPattern.AverageTextLength = SumTextLen / (double)Nodes.Count;
                //HtmlNodeCollection NodesChooseDirectly = RootNode.SelectNodes("//div[contains(@class,'pannel-inner')]/ul/li/a");
                //int e = Nodes.Where(n => !NodesChooseDirectly.Contains(n)).Count();
                //int f = NodesChooseDirectly.Where(n => !Nodes.Contains(n)).Count();
                //int g = e + f;
                //SoftStrategy.Feature newfeature = Strategy.GetFeature(Nodes, new AIParser.SoftStrategy.Feature(1));
                //g = newfeature.IdClassnameRecord["left"];
                if (NeedScore)
                    NewPattern.Score = NodeScore == null ? 0 : NodeScore(Nodes);
                NewPatterns.Add(NewPattern);
            }

            //第三步，去重，把选出同样Node文本集合的排除掉
            List<HtmlPattern> ResultPatterns = new List<HtmlPattern>(NewPatterns.Count);
            List<HashSet<string>> ResultPatternNodeTextSet = new List<HashSet<string>>(NewPatterns.Count);

            //按照排序,排在后面的重合Pattern会被忽略掉.这里也考虑一下，未来可能会要求修改为在前面也要求计算title的relpattern的模式
            foreach (HtmlPattern RelPattern in NewPatterns.OrderByDescending(p => NodeScore != null ? p.Score : -Math.Abs(p.ItemCount - BestBaseItemCount)).ThenByDescending(p => p.RelXPathUsingName).ThenBy(p => p.RelXPathLevel))
            {
                HashSet<string> NodesSetThisPattern = PatternsTextSet[RelPattern.RelXPath];
                bool FoundSameSet = false;
                //如果这个节点集合和已有的集合一摸一样，则忽略;
                foreach (HashSet<string> NodesSet in ResultPatternNodeTextSet)
                {
                    int MaxDiff = 3;
                    if (NodesSet.Count < 10) MaxDiff = 2;
                    if (NodesSet.Count < 3) MaxDiff = 1;

                    if (Math.Abs(NodesSet.Count - NodesSetThisPattern.Count) < MaxDiff && NodesSet.Except(NodesSetThisPattern).Count() < MaxDiff)
                    {
                        FoundSameSet = true;
                        break; ;
                    }
                }
                if (FoundSameSet) continue;

                ResultPatterns.Add(RelPattern);
                ResultPatternNodeTextSet.Add(NodesSetThisPattern);
            }

            if (ResultPatterns.Count() == 0)
                if (RootNode.SelectNodes("").Contains(RootNode.SelectSingleNode("")))
                {
                }


            //无重复且排序好了，再返回
            return ResultPatterns;
        }


        #endregion

        internal static List<HtmlPattern> List_HtmlPattern_getRelXPath(IEnumerable<HtmlNode> SeedNodes, HtmlNode RootNode, HtmlPattern LinkPattern, SoftStrategy Strategy, NodeValidFilter NodeFilter, NodePatternScore NodeScore, int BestBaseItemCount, PatternType pt = PatternType.Unknown, bool NeedScore = true)
        {
            if (string.IsNullOrWhiteSpace(LinkPattern.ItemBaseXPath)) return null;

            //存放临时候选的RelXPath
            HashSet<string> NewRelXPaths = new HashSet<string>();
            List<HtmlNode> ItemBaseNodes = RootNode.SelectNodes(LinkPattern.ItemBaseXPath).ToList();
            //第一步，检查从叶子到ItemRoot之间的所有ID和Class是否可用，生成全部候选的RelXPath
            foreach (HtmlNode Node in SeedNodes)
            {
                HtmlNode current = Node;
                if (Node.NodeType != HtmlNodeType.Element && pt == PatternType.Title) current = current.ParentNode;
                if ((Node.NodeType != HtmlNodeType.Element && Node.NodeType != HtmlNodeType.Text && pt != PatternType.Title) || current.ParentNode.Name == "a") current = current.ParentNode;
                if (current.ParentNode.ChildNodes.Count() == 1 && current.Name == "#text") current = current.ParentNode;
                if (current.Name == "#text") current.Name = "text()";
                //A标签要测试下其父级是否是否可以代表之，以避免某些媒体或作者名字没有链接的情况
                //这里的逻辑有问题
                //if (ATagTryParent && current.Name == "a" && TextCleaner.FullClean(current.InnerText) == TextCleaner.FullClean(current.ParentNode.InnerText))
                //    current = current.ParentNode;

                //找到真正的ItemBaseNode
                HtmlNode ItemBaseNode = current;
                while (!ItemBaseNodes.Contains(ItemBaseNode) && !XPathUtility.isTopNode(ItemBaseNode)) ItemBaseNode = ItemBaseNode.ParentNode;
                //路径前缀：从叶子到ItemBase之间的(去下标，去掉开头'/')
                string RelXPathPrefix;
                if (ItemBaseNode.XPath.Length == current.XPath.Length) RelXPathPrefix = string.Empty;
                else RelXPathPrefix = XPathUtility.removeXPathOrderNumber(current.XPath.Substring(ItemBaseNode.XPath.Length + 1), LinkPattern.LevelIgnored);
                //路径后缀：加在当前节点的命名XPath之后来指向叶子节点
                string RelXPathSuffix = string.Empty;

                //从每个Node开始向上,直到ItemRoot级别
                int ClimbLevel = 0;
                while (ClimbLevel < LinkPattern.LevelIgnored && !ItemBaseNodes.Contains(current))
                {
                    //有ID，则根据ID生成一个候选的相对路径
                    if (!string.IsNullOrWhiteSpace(current.Id))
                        NewRelXPaths.Add(string.Format(@".//{0}[@id={1}]{2}", current.Name, current.Id, RelXPathSuffix));

                    //有Class，也生成一个相对路径
                    if (current.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(current.Attributes["class"].Value) && !current.Attributes["class"].Value.Contains('$'))
                        foreach (string className in current.Attributes["class"].Value.Split())
                            NewRelXPaths.Add(string.Format(@".//{0}[contains(@class,'{1}')]{2}", current.Name, className, RelXPathSuffix));

                    int index = 0;
                    //同一级有多个同类型的元素，则需要根据每个序号生成一个候选相对路径(例如bbs一个div中两个a,后一个是用户名)
                    if ((current.NodeType == HtmlNodeType.Element || (current.NodeType == HtmlNodeType.Text && pt == PatternType.Unknown)) && !XPathUtility.isTopNode(current))
                        //如果按此RelPath在每个BaseNode下可得到多个，则应该用序号来选出唯一一个；
                        //除非特殊情况，所有的Item都在同一个BaseNode下（例如某些狗屎网站，很多a写在一个div里，用br分开）
                        if (current.ParentNode.SelectNodes(@"./" + current.Name).Count > 1)
                        {

                            for (int i = 0; i < Math.Min(6, current.ParentNode.SelectNodes(@"./" + current.Name).Count); i++)
                            {
                                if (current.ParentNode.SelectNodes(@"./" + current.Name + "[" + (i + 1).ToString() + "]").Contains(current)) index = i + 1;
                                NewRelXPaths.Add(string.Format("{0}[{1}]{2}", RelXPathPrefix, i + 1, RelXPathSuffix));
                            }
                        }
                        else
                            NewRelXPaths.Add(string.Format("{0}{1}", RelXPathPrefix, RelXPathSuffix));

                    //向上
                    if (current.ParentNode.SelectNodes(@"./" + current.Name).Count > 1 && index != 0)
                        RelXPathSuffix = string.Format("/{0}{1}{2}{3}{4}", current.Name, RelXPathSuffix, '[', index.ToString(), ']');
                    else
                        RelXPathSuffix = string.Format("/{0}{1}", current.Name, RelXPathSuffix);
                    if (!string.IsNullOrWhiteSpace(RelXPathPrefix))
                        if (RelXPathPrefix.IndexOf('/') >= 0)
                            RelXPathPrefix = RelXPathPrefix.Substring(0, RelXPathPrefix.LastIndexOf('/'));
                        else
                            RelXPathPrefix = string.Empty;
                    current = current.ParentNode;
                    ClimbLevel++;
                }

            }//end foreach node

            //第二步，过滤和打分RelXPath
            List<HtmlPattern> NewPatterns = new List<HtmlPattern>(NewRelXPaths.Count);
            Dictionary<string, HashSet<string>> PatternsTextSet = new Dictionary<string, HashSet<string>>(NewRelXPaths.Count);
            foreach (string RelXPath in NewRelXPaths.Distinct())
            {
                //从每一个Base节点去提取Rel对应的节点
                List<HtmlNode> Nodes = new List<HtmlNode>(LinkPattern.ItemCount + 2);
                HashSet<string> NodesTextSet_ThisPattern = new HashSet<string>();
                foreach (HtmlNode OneBaseNode in ItemBaseNodes)
                {
                    List<HtmlNode> leaveNodes = new List<HtmlNode>();
                    if (NewRelXPaths.Count == 1 && NewRelXPaths.First(n => true) == string.Empty)
                        leaveNodes.Add(OneBaseNode);
                    else
                    {

                        HtmlNodeCollection leaveNodescollection = OneBaseNode.SelectNodes(RelXPath);
                        if (leaveNodescollection != null) leaveNodes = leaveNodescollection.ToList();
                        else leaveNodes = null;
                    }
                    if (leaveNodes != null)
                    {
                        //过滤无效Nodes

                        IEnumerable<HtmlNode> filterNodes = leaveNodes.Where(a => NodeFilter == null && !string.IsNullOrEmpty(a.InnerText) || NodeFilter != null && NodeFilter(a));
                        Nodes.AddRange(filterNodes);
                        //提取文本集合
                        foreach (HtmlNode node in filterNodes)
                        {
                            string Text = XPathUtility.InnerTextNonDescendants(node);
                            if (!string.IsNullOrEmpty(Text))
                            {
                                if (NodesTextSet_ThisPattern.Contains(Text))
                                {
                                    int i = 0;
                                    Text = Text + "000" + i.ToString();
                                    while (NodesTextSet_ThisPattern.Contains(Text))
                                    {
                                        Text = Text.Remove(Text.Length - i++.ToString().Length);
                                        Text = Text + i.ToString();
                                    }
                                }
                                NodesTextSet_ThisPattern.Add(Text);
                            }
                        }
                    }
                }
                if (Nodes == null) continue;

                //特殊处理：对于节点内的文本内容是无意义序号的，发现后直接忽略(top3可能特殊格式，因此从4开始)
                if (Nodes.Count > 3 && Nodes.Count >= ItemBaseNodes.Count - 4 && Nodes.Count <= ItemBaseNodes.Count
                    && NodesTextSet_ThisPattern.Contains("4") && NodesTextSet_ThisPattern.Contains("5"))
                {
                    bool GotEveryNumber = true;
                    //检查一遍是不是每个数字都有
                    for (int i = 4; i < ItemBaseNodes.Count - 3; i++)
                        if (!NodesTextSet_ThisPattern.Contains(i.ToString()))
                        {
                            GotEveryNumber = false;
                            break;
                        }

                    //全部是序号数字，忽略本Pattern
                    if (GotEveryNumber) continue;
                }

                //文本集合是否命中禁用词
                if (NodesTextSet_ThisPattern.Count == 0 || (NodesTextSet_ThisPattern.Count == 1 && HardThreshold.BanWords_NodeSelect.Contains(NodesTextSet_ThisPattern.First().ToLower()))) continue;

                //保存文本集合用于去重
                PatternsTextSet.Add(RelXPath, NodesTextSet_ThisPattern);


                if (pt == PatternType.Title && (NewRelXPaths.Count == 0 || NewRelXPaths == null))
                    NewRelXPaths.Add(string.Empty);

                //加入候选列表
                HtmlPattern NewPattern = new HtmlPattern(LinkPattern);
                NewPattern.RelXPath = RelXPath;
                NewPattern.ItemCount = Nodes.Count;
                int SumTextLen = 0;
                Nodes.ForEach(a => SumTextLen += TextStatisticsUtility.GetWeightedLength(HTMLCleaner.GetCleanInnerText(a), HardThreshold.StopWords));
                NewPattern.AverageTextLength = SumTextLen / (double)Nodes.Count;
                //HtmlNodeCollection NodesChooseDirectly = RootNode.SelectNodes("//div[contains(@class,'pannel-inner')]/ul/li/a");
                //int e = Nodes.Where(n => !NodesChooseDirectly.Contains(n)).Count();
                //int f = NodesChooseDirectly.Where(n => !Nodes.Contains(n)).Count();
                //int g = e + f;
                //SoftStrategy.Feature newfeature = Strategy.GetFeature(Nodes, new AIParser.SoftStrategy.Feature(1));
                //g = newfeature.IdClassnameRecord["left"];
                if (NeedScore)
                    NewPattern.Score = NodeScore == null ? 0 : NodeScore(Nodes);
                NewPatterns.Add(NewPattern);
            }

            //第三步，去重，把选出同样Node文本集合的排除掉
            List<HtmlPattern> ResultPatterns = new List<HtmlPattern>(NewPatterns.Count);
            List<HashSet<string>> ResultPatternNodeTextSet = new List<HashSet<string>>(NewPatterns.Count);

            //按照排序,排在后面的重合Pattern会被忽略掉.这里也考虑一下，未来可能会要求修改为在前面也要求计算title的relpattern的模式
            foreach (HtmlPattern RelPattern in NewPatterns.OrderByDescending(p => NodeScore != null ? p.Score : -Math.Abs(p.ItemCount - BestBaseItemCount)).ThenByDescending(p => p.RelXPathUsingName).ThenBy(p => p.RelXPathLevel))
            {
                HashSet<string> NodesSetThisPattern = PatternsTextSet[RelPattern.RelXPath];
                bool FoundSameSet = false;
                //如果这个节点集合和已有的集合一摸一样，则忽略;
                foreach (HashSet<string> NodesSet in ResultPatternNodeTextSet)
                {
                    int MaxDiff = 3;
                    if (NodesSet.Count < 10) MaxDiff = 2;
                    if (NodesSet.Count < 3) MaxDiff = 1;

                    if (Math.Abs(NodesSet.Count - NodesSetThisPattern.Count) < MaxDiff && NodesSet.Except(NodesSetThisPattern).Count() < MaxDiff)
                    {
                        FoundSameSet = true;
                        break; ;
                    }
                }
                if (FoundSameSet) continue;

                ResultPatterns.Add(RelPattern);
                ResultPatternNodeTextSet.Add(NodesSetThisPattern);
            }

            if (ResultPatterns.Count() == 0)
                if (RootNode.SelectNodes(LinkPattern.ItemBaseXPath).Contains(RootNode.SelectSingleNode(LinkPattern.XPath)))
                {
                    LinkPattern.RelXPath = "";
                    ResultPatterns.Add(LinkPattern);
                }


            //无重复且排序好了，再返回
            return ResultPatterns;
        }

        /// <summary>
        /// 判断Dictionary是否相同，用于ContainList
        /// </summary>
        /// <param name="DicA"></param>
        /// <param name="DicB"></param>
        /// <returns></returns>
        public static bool IsthesameDic(Dictionary<string, int> DicA, Dictionary<string, int> DicB)
        {
            if (DicA == null && DicB == null) return true;
            if ((DicA == null && DicB != null) || (DicA != null && DicB == null)) return false;
            if (DicA.Keys.Count() != DicB.Keys.Count() || DicA.Keys.Except(DicB.Keys).Count() != 0) return false;
            foreach (string Key in DicA.Keys)
            {
                if (DicB[Key] != DicA[Key]) return false;
            }
            return true;
        }
        public static bool IsthesameDic(Dictionary<string, HashSet<string>> DicA, Dictionary<string, HashSet<string>> DicB)
        {
            if (DicA == null && DicB == null) return true;
            if ((DicA == null && DicB != null) || (DicA != null && DicB == null)) return false;
            if (DicA.Keys.Count() != DicB.Keys.Count() || DicA.Keys.Except(DicB.Keys).Count() != 0) return false;
            foreach (string Key in DicA.Keys)
            {
                if (!DicB.ContainsKey(Key)) return false;
                if (DicB[Key].Except(DicA[Key]).Count() != 0 || DicA[Key].Except(DicB[Key]).Count() != 0) return false;
            }
            return true;
        }

        /// <summary>
        /// 用来取出干净无标签的xpath，为进行比较做准备
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public static string RemoveallMarks(string A)
        {
            if (A.Contains('[') && A.Contains(']'))
                A = A.Substring(0, A.IndexOf('[')) + A.Substring(A.IndexOf(']'));
            return A;
        }

        /// <summary>
        /// getrelxpath在dombaseddicover中用到的一个接口。因为有可能面对的那个TitlePattern不是自动生成的而是由审核结果指出的，所以要重新生成几个参数方便选取
        /// 最后修改 2015-12-02 Sandy
        /// </summary>
        /// <param name="TextNodes"></param>
        /// <param name="RootNode"></param>
        /// <param name="TitlePattern"></param>
        /// <param name="Strategy"></param>
        /// <param name="IC"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static List<HtmlPattern> List_HtmlPattern_getRelXPathoutside(IEnumerable<HtmlNode> TextNodes, HtmlNode RootNode, HtmlPattern TitlePattern, SoftStrategy Strategy, int IC, PatternType pt = PatternType.Unknown)
        {
            HtmlPattern LinkPattern = new HtmlPattern(TitlePattern);
            List<HtmlNode> BaseNodes = RootNode.SelectNodes(TitlePattern.ItemBaseXPath).ToList();
            List<HtmlNode> Nodes = RootNode.SelectNodes(TitlePattern.ItemBaseXPath + '/' + TitlePattern.RelXPath).ToList();
            if (BaseNodes == null || Nodes == null) return null;

            List<HtmlNode> UpperNodes = new List<HtmlNode>();
            int MaxLVI = Regex.Matches(BaseNodes.First(n => true).XPath, @"/").Count;
            for (int i = 1; i < MaxLVI; i++)
            {
                foreach (HtmlNode basenode in BaseNodes)
                    if (!UpperNodes.Contains(basenode.ParentNode))
                        UpperNodes.Add(basenode.ParentNode);
                BaseNodes = UpperNodes;
                UpperNodes = new List<HtmlNode>();
                if (BaseNodes.Count() <= 1)
                    break;
            }
            int min = Regex.Matches(BaseNodes.First(n => true).XPath, @"/").Count;
            int max = Regex.Matches(Nodes.First(n => true).XPath, @"/").Count;
            if (max <= min) return null;


            LinkPattern.LevelIgnored = max - min;
            LinkPattern.ItemCount = Nodes.Count();

            return List_HtmlPattern_getRelXPath(TextNodes, RootNode, LinkPattern, Strategy, null, null, IC, pt);
        }

        /// <summary>
        /// 用来判断基于一个rootnode的两个xpath是否相同
        /// </summary>
        /// <param name="CompareRootNode"></param>
        /// <param name="XPath1"></param>
        /// <param name="XPath2"></param>
        /// <returns></returns>
        public static bool isthesameXpath(HtmlNode CompareRootNode, string XPath1, string XPath2)
        {
            //首先，如果两者相同则不必再比直接返回正确
            if (XPath1 == XPath2)
                return true;

            //然后判断能否选出同样的一些node
            HashSet<HtmlNode> NodeSetXPath1 = new HashSet<HtmlNode>();
            HashSet<HtmlNode> NodeSetXPath2 = new HashSet<HtmlNode>();



            if (CompareRootNode.SelectNodes(XPath1) == null && CompareRootNode.SelectNodes(XPath2) == null)
                return true;
            if (CompareRootNode.SelectNodes(XPath1) == null || CompareRootNode.SelectNodes(XPath2) == null)
                return false;

            foreach (HtmlNode node in CompareRootNode.SelectNodes(XPath1))
                NodeSetXPath1.Add(node);
            foreach (HtmlNode node in CompareRootNode.SelectNodes(XPath2))
                NodeSetXPath2.Add(node);

            if (NodeSetXPath1.Count == NodeSetXPath2.Count && NodeSetXPath1.Except(NodeSetXPath2).Count() == 0 && NodeSetXPath2.Except(NodeSetXPath1).Count() == 0)
                return true;
            else return false;
        }

        public static bool isthesameRelXpath(List<HtmlNode> BaseNodes, string XPath1, string XPath2)
        {
            bool samerelxpath = true;
            foreach (HtmlNode basenode in BaseNodes)
                try
                {
                    if (!isthesameXpath(basenode, XPath1, XPath2))
                    { samerelxpath = false; break; }
                }
                catch { }
            return samerelxpath;

        }

        #endregion List部分

        #region Item页面部分

        /// <summary>
        /// 根据是否能取到相同的点进行分类
        /// </summary>
        /// <param name="Html"></param>
        /// <param name="ItemXPaths"></param>
        /// <returns></returns>
        public static List<List<string>> Item_DevideintoGroups(string Html, List<string> ItemXPaths, bool iscontentxpath = false)
        {
            HtmlNode rootNode = HtmlUtility.getSafeHtmlRootNode(Html, true, true);
            if (rootNode == null)
                return null;
            List<ItemPatternAnaly> ContentPatterns = new List<ItemPatternAnaly>();
            foreach (string xpath in ItemXPaths)
            {
                ItemPatternAnaly newon = new ItemPatternAnaly();
                newon.XPath = xpath;
                newon.TextLenCount = 0;
                newon.useless = false;
                HtmlNodeCollection nodes = rootNode.SelectNodes(xpath);
                if (nodes == null) continue;
                foreach (HtmlNode node in nodes)
                    newon.TextLenCount += TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(node)).Length;
                ContentPatterns.Add(newon);
            }
            ContentPatterns = ContentPatterns.OrderByDescending(c => c.TextLenCount).ToList();

            List<List<string>> Groups = new List<List<string>>();

            foreach (ItemPatternAnaly itpa in ContentPatterns)
            {
                if (itpa.useless) continue;
                List<ItemPatternAnaly> siblings = new List<ItemPatternAnaly>();
                try
                {
                    siblings = ContentPatterns.Where(c => c.useless == false && c.TextLenCount == itpa.TextLenCount
                        && rootNode.SelectNodes(c.XPath).Except(rootNode.SelectNodes(itpa.XPath)).Count() == 0
                        && rootNode.SelectNodes(itpa.XPath).Except(rootNode.SelectNodes(c.XPath)).Count() == 0).ToList();
                }
                catch
                {//这是啥毛病？说“first值不能为null”
                    continue;
                }
                if (iscontentxpath)
                    siblings = ContentPatterns.Where(c => c.useless == false && c.TextLenCount == itpa.TextLenCount).ToList();
                List<string> siblingxpaths = new List<string>();
                foreach (ItemPatternAnaly ipainsib in siblings)
                {
                    ipainsib.useless = true;
                    siblingxpaths.Add(ipainsib.XPath);
                }
                Groups.Add(siblingxpaths);
            }

            return Groups;
        }




        /// <summary>
        /// 主体步骤。现在果然还是通过文字密度来判决谁才是真正的文章主体。还好有选择机制
        /// </summary>
        /// <param name="ItemPages"></param>
        /// <param name="MediaType"></param>
        /// <param name="Language"></param>
        /// <returns></returns>
        public static ItemXPathPattern Item_LinkPattern_getCandidate_WebNews(IEnumerable<ItemPageMessage> ItemPages, List<Article> PageElements, Dictionary<string, HtmlNode> ComPareNodes, SoftStrategy strategy, bool NeedScore = false)
        {
            Dictionary<string, HtmlNode> RootNodes = new Dictionary<string, HtmlNode>();
            foreach (ItemPageMessage itpm in ItemPages)
                if (!RootNodes.Keys.Contains(itpm.Url))
                    RootNodes.Add(itpm.Url, itpm.RootNode);

            ItemXPathPattern Result = new ItemXPathPattern();
            Dictionary<string, List<ItemPatternAnaly>> BasicPatterns = new Dictionary<string, List<ItemPatternAnaly>>();
            //先寻找正文路径
            Result.ContentXPath = Item_ContentCandidates_WebNewsAndTheOthers(RootNodes, ComPareNodes, strategy);

            ItemXPathPattern itmpx = new ItemXPathPattern();
            itmpx = Result;

            //用那边得到的Innertext做印证寻找这里的relxpath

            Dictionary<PatternType, Dictionary<string, List<ItemPatternAnaly>>> ItmpaForRelXPaths = new Dictionary<PatternType, Dictionary<string, List<ItemPatternAnaly>>>();
            Dictionary<PatternType, Dictionary<string, List<string>>> ShortRelXPaths = new Dictionary<PatternType, Dictionary<string, List<string>>>();

            List<string> PubDateXPath = new List<string>();
            List<string> AuthorXPath = new List<string>();
            List<string> MediaNameXPath = new List<string>();
            List<string> ReplyXPath = new List<string>();
            List<string> TitleXPath = new List<string>();
            List<string> ViewXPath = new List<string>();
            Result.PubDateXPath = new List<string>();
            Result.AuthorXPath = new List<string>();
            Result.MediaNameXPath = new List<string>();
            Result.ReplyXPath = new List<string>();
            Result.TitleXPath = new List<string>();
            Result.ViewXPath = new List<string>();

            foreach (string url in BasicPatterns.Keys)
            {
                Article content = PageElements.FirstOrDefault(c => c.Url == url);
                if (content == null) return null;

                TitleXPath = Item_GetStringXPaths(BasicPatterns[url], content.Title, RootNodes[url], Result.TitleXPath, PatternType.Title);
                //if (TitleXPath != null)
                //    TitleXPath = TitleXPath.OrderByDescending(t => Regex.Matches(t, "/h"+ @"\d{1,9}").Count).ThenByDescending(t => Regex.Matches(t, "tit").Count).ToList();
                //if (TitleXPath.Exists(t => Regex.Matches(t, "/h" + @"\d{1,9}").Count > 0 || Regex.Matches(t, "tit").Count > 0))
                TitleXPath = TitleXPath.OrderByDescending(t => Regex.Matches(t, "/h" + @"\d{1,9}").Count > 0 || Regex.Matches(t, "tit").Count > 0).ToList();


                bool DateinListXPath = content.PubDate != null && content.PubDate.Year > 2000 && (content.PubDate.Hour != 0 || content.PubDate.Minute != 0 || content.PubDate.Second != 0);
                PubDateXPath = Item_GetDateXPaths(BasicPatterns[url], content.PubDate, RootNodes[url], Result.PubDateXPath, DateinListXPath);

                //if (!string.IsNullOrWhiteSpace(ListXPath.Path.AuthorXPath) || !string.IsNullOrWhiteSpace(content.Author))
                AuthorXPath = Item_GetStringXPaths(BasicPatterns[url], content.Author, RootNodes[url], Result.AuthorXPath, PatternType.Author).OrderByDescending(t => Regex.Match(t, "aut").Length).ToList();

                //if (!string.IsNullOrWhiteSpace(ListXPath.Path.MediaNameXPath) || !string.IsNullOrWhiteSpace(content.MediaName))
                MediaNameXPath = Item_GetStringXPaths(BasicPatterns[url], content.MediaName, RootNodes[url], Result.MediaNameXPath, PatternType.MediaName);

                string reply = content.ViewDataList?[0]?.Reply > 0 ? string.Empty : content.ViewDataList?[0]?.Reply.ToString();
                ReplyXPath = Item_GetStringXPaths(BasicPatterns[url], reply, RootNodes[url], Result.ReplyXPath, PatternType.Reply);

                string view = content.ViewDataList?[0]?.View > 0 ? string.Empty : content.ViewDataList?[0]?.View.ToString();
                ViewXPath = Item_GetStringXPaths(BasicPatterns[url], view, RootNodes[url], Result.ViewXPath, PatternType.View);
            }

            if (PubDateXPath != null && PubDateXPath.Count() != 0)
            {
                Result.PubDateXPath = new List<string>(); Result.PubDateXPath.Add(PubDateXPath.First(t => true));
                foreach (string xpath in PubDateXPath)
                {
                    bool alreadyhave = false;
                    foreach (string epxpath in Result.PubDateXPath)
                        if (isthesameRelXpath(RootNodes.Values.ToList(), xpath, epxpath))
                        { alreadyhave = true; break; }
                    if (!alreadyhave)
                        Result.PubDateXPath.Add(xpath);
                }
            }

            if (AuthorXPath != null && AuthorXPath.Count() != 0)
            {
                Result.AuthorXPath = new List<string>(); Result.AuthorXPath.Add(AuthorXPath.First(t => true));
                foreach (string xpath in AuthorXPath)
                {
                    bool alreadyhave = false;
                    foreach (string epxpath in Result.AuthorXPath)
                        if (isthesameRelXpath(RootNodes.Values.ToList(), xpath, epxpath))
                        { alreadyhave = true; break; }
                    if (!alreadyhave)
                        Result.AuthorXPath.Add(xpath);
                }
            }

            if (MediaNameXPath != null && MediaNameXPath.Count() != 0)
            {
                Result.MediaNameXPath = new List<string>(); Result.MediaNameXPath.Add(MediaNameXPath.First(t => true));
                foreach (string xpath in MediaNameXPath)
                {
                    bool alreadyhave = false;
                    foreach (string epxpath in Result.MediaNameXPath)
                        if (isthesameRelXpath(RootNodes.Values.ToList(), xpath, epxpath))
                        { alreadyhave = true; break; }
                    if (!alreadyhave)
                        Result.MediaNameXPath.Add(xpath);
                }
            }

            if (ReplyXPath != null && ReplyXPath.Count() != 0)
            {
                Result.ReplyXPath = new List<string>(); Result.ReplyXPath.Add(ReplyXPath.First(t => true));
                foreach (string xpath in ReplyXPath)
                {
                    bool alreadyhave = false;
                    foreach (string epxpath in Result.ReplyXPath)
                        if (isthesameRelXpath(RootNodes.Values.ToList(), xpath, epxpath))
                        { alreadyhave = true; break; }
                    if (!alreadyhave)
                        Result.ReplyXPath.Add(xpath);
                }
            }

            if (TitleXPath != null && TitleXPath.Count() != 0)
            {
                Result.TitleXPath = new List<string>(); Result.TitleXPath.Add(TitleXPath.First(t => true));
                foreach (string xpath in TitleXPath)
                {
                    bool alreadyhave = false;
                    foreach (string epxpath in Result.TitleXPath)
                        if (isthesameRelXpath(RootNodes.Values.ToList(), xpath, epxpath))
                        { alreadyhave = true; break; }
                    if (!alreadyhave)
                        Result.TitleXPath.Add(xpath);
                }
            }

            if (ViewXPath != null && ViewXPath.Count() != 0)
            {
                Result.ViewXPath = new List<string>(); Result.ViewXPath.Add(ViewXPath.First(t => true));
                foreach (string xpath in ViewXPath)
                {
                    bool alreadyhave = false;
                    foreach (string epxpath in Result.ViewXPath)
                        if (isthesameRelXpath(RootNodes.Values.ToList(), xpath, epxpath))
                        { alreadyhave = true; break; }
                    if (!alreadyhave)
                        Result.ViewXPath.Add(xpath);
                }
            }

            return Result;
        }

        public static List<string> Item_GetDateXPaths(List<ItemPatternAnaly> BasicPatterns, DateTime ContentPubdate, HtmlNode RootNode, List<string> RelXPath, bool dateinlistxpath = false)
        {
            List<string> subxpath = new List<string>();//补充的，万一列表页没取出来，可以在文章页补
            if (dateinlistxpath)
                foreach (ItemPatternAnaly itmpa in BasicPatterns)
                {
                    DateTime? Val = new DateTime();
                    try//todo:这也能出问题，你到底输进去了什么鬼东西
                    {
                        Val = DateTimeParser.Parser(TextCleaner.FullClean(itmpa.Content));
                    }
                    catch
                    {
                        continue;
                    }
                    if (Val != null && Val.Value.Year > 2000 && itmpa.Content.Length < 100)
                    {
                        DateTime ItemPubDate = (DateTime)Val;
                        if (ContentPubdate.Equals(ItemPubDate))
                        {
                            string ShortXPath = XPathUtility.GetRawXPath(itmpa.SampleNode, RootNode, 0, string.Empty);
                            if (!RelXPath.Contains(Item_FormalizeTextNodeXPath(ShortXPath)))
                                RelXPath.Add(Item_FormalizeTextNodeXPath(ShortXPath));
                        }
                        else
                        {
                            string ShortXPath = XPathUtility.GetRawXPath(itmpa.SampleNode, RootNode, 0, string.Empty, true);
                            if (!subxpath.Contains(Item_FormalizeTextNodeXPath(ShortXPath)))
                                subxpath.Add(Item_FormalizeTextNodeXPath(ShortXPath));
                        }
                    }
                }
            else
                foreach (ItemPatternAnaly itmpa in BasicPatterns)
                {
                    DateTime? Val = DateTimeParser.Parser(itmpa.Content);
                    if (Val != null && Val.Value.Year > 2000 && itmpa.Content.Length < 100)
                    {
                        string ShortXPath = XPathUtility.GetRawXPath(itmpa.SampleNode, RootNode, 0, string.Empty, true);
                        if (!RelXPath.Contains(Item_FormalizeTextNodeXPath(ShortXPath)))
                            RelXPath.Add(Item_FormalizeTextNodeXPath(ShortXPath));
                    }
                }
            RelXPath.AddRange(subxpath);
            return RelXPath;
        }
        public static List<string> Item_GetStringXPaths(List<ItemPatternAnaly> BasicPatterns, string Content, HtmlNode RootNode, List<string> RelXPath, PatternType ptt)
        {
            foreach (ItemPatternAnaly itmpa in BasicPatterns)
            {
                bool valuable = itmpa.Content != null;
                if (!valuable) continue;
                string itmpacontent = TextCleaner.FullClean(itmpa.Content);
                valuable = false;
                string content = TextCleaner.FullClean(Content);
                if (!string.IsNullOrWhiteSpace(Content))
                    valuable = (itmpacontent.Contains(content) || content.Contains(itmpacontent));
                switch (ptt)
                {
                    case PatternType.Author:
                        valuable = valuable || (itmpacontent.Length < 40 && Regex.Matches(itmpacontent, @"[作者|编辑|记撰|稿|人|播|主|报]").Count > 0);
                        break;
                    case PatternType.MediaName:
                        valuable = valuable || (itmpacontent.Length < 40 && Regex.Matches(itmpacontent, @"[报|台|网|刊|来源|媒体|新闻|论坛|智库]").Count > 0);
                        break;
                    case PatternType.Reply:
                        valuable = valuable || (itmpacontent.Length < 40 && Regex.Matches(itmpacontent, @"[评论|回复|留言]").Count > 0 || Regex.Matches(itmpacontent, @"\d{1,9}").Count == itmpacontent.Length);
                        break;
                    case PatternType.View:
                        valuable = valuable || (itmpacontent.Length < 40 && Regex.Matches(itmpacontent, @"[点击|浏览|阅读]").Count > 0 || Regex.Matches(itmpacontent, @"\d{1,9}").Count == itmpacontent.Length);
                        break;
                    default:
                        break;
                }

                if (valuable)
                {
                    string ShortXPath = XPathUtility.GetRawXPath(itmpa.SampleNode, RootNode, 0, string.Empty, true);
                    if (itmpa.SampleNode.Name.Contains("text()"))
                        if (itmpa.SampleNode.ParentNode.SelectNodes("text()").Count() == 1)
                            ShortXPath = ShortXPath.Substring(0, ShortXPath.LastIndexOf("/"));
                    if (itmpa.SampleNode.Name.Contains("content()"))
                        if (itmpa.SampleNode.ParentNode.SelectNodes("content()").Count() == 1)
                            ShortXPath = ShortXPath.Substring(0, ShortXPath.LastIndexOf("/"));

                    if (!RelXPath.Contains(Item_FormalizeTextNodeXPath(ShortXPath)))
                        RelXPath.Add(Item_FormalizeTextNodeXPath(ShortXPath));
                }
            }
            return RelXPath;
        }

        /// <summary>
        /// 除了Forum之外其他形式媒体的后续步骤
        /// </summary>
        /// <param name="ItemPages"></param>
        /// <param name="ContentChars"></param>
        /// <param name="strategy"></param>
        /// <param name="RootNodes"></param>
        /// <param name="BasicPatterns"></param>
        /// <returns></returns>
        public static List<string> Item_ContentCandidates_WebNewsAndTheOthers(Dictionary<string, HtmlNode> RootNodes, Dictionary<string, HtmlNode> ComPareNodes, SoftStrategy strategy)
        {
            Dictionary<string, List<ItemPatternAnaly>> PatternsReadytoCombine = new Dictionary<string, List<ItemPatternAnaly>>();

            //进行在文章页分别操作的步骤
            foreach (string thisurl in RootNodes.Keys)
            {
                //第一步，生成基本的patterns
                List<ItemPatternAnaly> BasicPatterns = Item_FormBasicAnalies(Item_CollectNode(RootNodes[thisurl]), RootNodes[thisurl]);

                //第二步，向上合并
                List<ItemPatternAnaly> CombinedPatterns = Item_CombinePatterns_WebNews(BasicPatterns, strategy.Threshold.itemclimb, RootNodes[thisurl]);

                //第三步，进行路径的简化
                List<ItemPatternAnaly> ShorternPatterns = Item_ShortPatterns_WebNews(CombinedPatterns, RootNodes[thisurl], thisurl);
                PatternsReadytoCombine.Add(thisurl, ShorternPatterns);
            }

            //第四步，在文章页之间进行合并
            List<ItemPatternAnaly> IntergratedShortedCombinedPatterns = Item_IntergratePatterns_WebNews(PatternsReadytoCombine);



            //通过文字密度进行打分。可以换成使用模型打分的方式
            List<ItemPatternAnaly> patterns = new List<ItemPatternAnaly>();// IntergratedShortedCombinedPatterns.Where(i => i.NodesCount > 1).OrderByDescending(i => i.TextLenCount).ToList();

            foreach (ItemPatternAnaly itmpa in IntergratedShortedCombinedPatterns)
            {
                if (itmpa.ShortXPath.Contains("#"))
                {
                    itmpa.ShortXPath = itmpa.ShortXPath.Replace("#text", "text()");
                    itmpa.ShortXPath = itmpa.ShortXPath.Replace("#content", "content()");
                }
                if (isValidItemContentXPath(itmpa, ComPareNodes))
                    patterns.Add(itmpa);
            }

            patterns = patterns.OrderByDescending(i => i.TextLenCount).ToList();
            patterns = patterns.Where(p => p.TextLenCount > patterns.First(a => true).TextLenCount * strategy.Threshold.ItemSave).ToList();

            List<string> ResultItemContentXPath = new List<string>();
            foreach (ItemPatternAnaly itmpa in patterns)
            {
                ResultItemContentXPath.Add(itmpa.ShortXPath);
            }
            return ResultItemContentXPath;
        }

        public static bool isValidItemContentXPath(ItemPatternAnaly itmpa, Dictionary<string, HtmlNode> ComPareNodes)
        {

            if (itmpa.NodesCount > 0)
            {
                IEnumerable<HtmlNode> SelectedNodes = null;
                foreach (HtmlNode rootnode in ComPareNodes.Values)
                {
                    try
                    {
                        if (rootnode.SelectNodes(itmpa.ShortXPath) == null)
                            return false;
                    }
                    catch
                    {
                        itmpa.XPath = itmpa.ShortXPath;
                    }
                    try
                    {
                        if (rootnode.SelectNodes(itmpa.ShortXPath).Count() == 0)
                            return false;
                    }
                    catch
                    {//todo:怎么到处出错，什么鬼
                        return false;
                    }
                    if (SelectedNodes == null || SelectedNodes.Count() == 0)
                    {
                        SelectedNodes = rootnode.SelectNodes(itmpa.ShortXPath);
                    }//todo:这里有逻辑问题。搞什么鬼。。。
                    else
                    {
                        List<HtmlNode> thisSelectedNodes = rootnode.SelectNodes(itmpa.ShortXPath).ToList();
                        if (SelectedNodes.Except(thisSelectedNodes).Count() == SelectedNodes.Count())
                            //如果某两次选取的点完全不一样，可以直接回复正确
                            return true;
                        SelectedNodes = SelectedNodes.Except(SelectedNodes.Except(thisSelectedNodes));
                        //这样，如果两次选取的点都不一样则通过，如果都不一样则保留都被取出的点。经过三个比较通过之后，如果一起的总字数大于某个值则判定不合理
                    }
                }
                //此时如果没有说明没有什么点是在所有比较页中都被重复选取的，所以可以被放入备选
                if (SelectedNodes == null || SelectedNodes.Count() == 0) return true;

                int txtlen = 0;
                foreach (HtmlNode node in SelectedNodes)
                    txtlen += TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(node)).Length;
                if (txtlen < 100) return true;
                else return false;
            }
            else return false;
        }



        /// <summary>
        /// 检查所有的BasicNodes把它们写成ItemPatternAnaly
        /// </summary>
        /// <param name="Nodes">生成的基本Nodes，生成方式及要求见Item_CollectNode</param>
        /// <param name="RootNode">在生成过程中用于生成AttributeList</param>
        /// <returns></returns>
        public static List<ItemPatternAnaly> Item_FormBasicAnalies(List<HtmlNode> Nodes, HtmlNode RootNode)
        {
            List<ItemPatternAnaly> ItemPatterns = new List<ItemPatternAnaly>();
            foreach (HtmlNode Node in Nodes)
            {
                if (XPathUtility.isTopNode(Node.ParentNode)) continue;

                HtmlNode node = Node;
                List<ItemPatternAnaly> newItemAnalies = new List<ItemPatternAnaly>();
                //结合之前形成Node时的筛选条件，这里所遍历的就是所有文本点，#text或#content。
                if (node.HasChildNodes && node.ChildNodes.Where(c => !c.Attributes.Contains("cutting")).Count() > 0)
                    try
                    {
                        foreach (HtmlNode chdnod in node.ChildNodes.Where(c => !c.Attributes.Contains("cutting") && c.Name.Contains('#')))
                        {
                            string xp = chdnod.XPath.Replace("#text", "text()").Replace("#content", "content()");
                            ItemPatternAnaly pattern = new ItemPatternAnaly(chdnod, formContainList(chdnod), formAttributelist(RootNode, xp));
                            if (!string.IsNullOrWhiteSpace(HTMLCleaner.GetCleanInnerText(chdnod)))
                                pattern.Content = TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(chdnod));
                            newItemAnalies.Add(pattern);
                        }
                    }
                    catch//若失败，则将该点本身加入
                    {
                        newItemAnalies = new List<ItemPatternAnaly>();
                        string xpp = node.XPath.Replace("#text", "text()").Replace("#content", "content()");
                        ItemPatternAnaly pattern = new ItemPatternAnaly(node, formContainList(node), formAttributelist(RootNode, xpp));
                        if (!string.IsNullOrWhiteSpace(HTMLCleaner.GetCleanInnerText(node)))
                            pattern.Content = TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(node));
                        newItemAnalies.Add(pattern);
                    }
                else//若其下没有文本点则将该点本身加入
                {
                    string xpp = node.XPath.Replace("#text", "text()").Replace("#content", "content()");
                    ItemPatternAnaly pattern = new ItemPatternAnaly(node, formContainList(node), formAttributelist(RootNode, xpp));
                    if (!string.IsNullOrWhiteSpace(HTMLCleaner.GetCleanInnerText(node)))
                        pattern.Content = TextCleaner.FullClean(HTMLCleaner.GetCleanInnerText(node));
                    newItemAnalies.Add(pattern);
                }
                ItemPatterns.AddRange(newItemAnalies);
            }
            return ItemPatterns;
        }

        /// <summary>
        /// 统计点数
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static int Item_CountChildNodes(HtmlNode node)
        {
            int count = 0;
            if (node.ChildNodes.Count() == 0 && !node.Attributes.Contains("cutting"))
                count = 1;
            else
                foreach (HtmlNode childnode in node.ChildNodes)
                    count += Item_CountChildNodes(childnode);
            return count;
        }

        /// <summary>
        /// 下方函数的一个重载，实际目的是为了调用下方函数
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static List<HtmlNode> Item_CollectNode(HtmlNode RootNode)
        {
            List<HtmlNode> Collection = new List<HtmlNode>();
            foreach (HtmlNode node in RootNode.ChildNodes)
                Collection = Item_CollectNodes(Collection, node);
            return Collection;
        }

        /// <summary>
        /// 这是用来收集所有最基点的函数。所有没有子节点的，或者子节点均为文本节点的，都会被选入。
        /// 这样得到的所有node都是直接下辖一个文本点的、没被剪枝的node
        /// </summary>
        /// <param name="Collection"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static List<HtmlNode> Item_CollectNodes(List<HtmlNode> Collection, HtmlNode Node)
        {
            if (Node.Attributes.Contains("cutting"))
                return Collection;
            if (!Node.HasChildNodes || (Node.ChildNodes.Where(c => !c.Name.Contains("#") && !(c.Attributes != null && c.Attributes.Count() > 0 && c.Attributes.Contains("cutting"))).Count() == 0))
            {
                Collection.Add(Node);
                return Collection;
            }
            foreach (HtmlNode node in Node.ChildNodes)
                Collection = Item_CollectNodes(Collection, node);
            return Collection;

        }

        /// <summary>
        /// 旧第二步，对得到的点进行合并。主要基础结构。
        /// 同时也是新规则下细化路径的第一步
        /// 11-03仍未修改过它的向上升级规则，不过至少a的问题解决了。修改的时候可以同时改进itempatternanaly的两种初始化方法设置，同样的原理也可以运用于listpatternanaly
        /// </summary>
        /// <param name="ItemPatterns"></param>
        /// <param name="leveltoclimb"></param>
        /// <param name="RootNode"></param>
        /// <returns></returns>
        public static List<ItemPatternAnaly> Item_CombinePatterns_WebNews(List<ItemPatternAnaly> ItemPatterns, int leveltoclimb, HtmlNode RootNode)
        {
            List<ItemPatternAnaly> ResultPatterns = new List<ItemPatternAnaly>();
            List<ItemPatternAnaly> currentpatternlist = ItemPatterns;
            while (currentpatternlist.Exists(p => p.useless == false))
            {
                List<ItemPatternAnaly> newpatternlist = new List<ItemPatternAnaly>();
                foreach (ItemPatternAnaly itmp in currentpatternlist.Where(c => !c.useless))
                {
                    if (itmp.ContainList == null || itmp.ContainList.Count() == 0) itmp.ContainList = formContainList(itmp.CNode);

                    //寻找sibling的方法，是对起父节点和下属包含类型进行比较
                    //寻找规则：上级节点相同，containlist相同，attributelist相同
                    List<ItemPatternAnaly> Siblings = currentpatternlist.Where(p => p.CNode.ParentNode == itmp.CNode.ParentNode &&
                        (IsthesameDic(p.ContainList, itmp.ContainList) ||
                        IsthesameDic(itmp.Attributelist, p.Attributelist))).ToList();

                    //统计新模式的各项属性。节点的包含数目，所包含的文字类节点数，在合并为这一级时获得的文字增幅
                    int countnode = 0;
                    int counttextnode = 0;
                    double gaintextrate = 0;
                    string content = string.Empty;
                    foreach (ItemPatternAnaly itmpa in Siblings)
                    {
                        itmpa.usedtime++;
                        if (Siblings.Exists(s => s.useless && s.PreXPath == itmpa.PreXPath &&
                            ((string.IsNullOrWhiteSpace(s.Content) && string.IsNullOrWhiteSpace(itmpa.Content)) ||
                            ((!string.IsNullOrWhiteSpace(s.Content) && !string.IsNullOrWhiteSpace(itmpa.Content)) &&
                            s.Content.Length == itmpa.Content.Length))))
                        { itmpa.useless = true; continue; }
                        itmpa.useless = true;
                        countnode += itmpa.NodesCount;
                        //加一个判断剔除可能混进去的a标签
                        if (itmpa.SampleNode.Name == "a") counttextnode += 1;
                        else counttextnode += itmpa.TextNodesCount;
                        content = content + itmpa.Content;
                    }

                    gaintextrate = Siblings.Where(s => !string.IsNullOrWhiteSpace(s.Content) && s.Content.Length >= 5).Count();


                    //存储直接向上得到的路径
                    ItemPatternAnaly pattern = new ItemPatternAnaly(itmp.CNode, itmp.CNode.ParentNode, formContainList(itmp.CNode.ParentNode), formAttributelist(RootNode, itmp.CNode.ParentNode.XPath), countnode, counttextnode);
                    string tem = itmp.PreXPath.Substring(itmp.PreXPath.LastIndexOf('/'));
                    if (!string.IsNullOrWhiteSpace(pattern.SubXPath)) pattern.SubXPath = tem + itmp.SubXPath;
                    pattern.PreXPath = itmp.PreXPath.Substring(0, itmp.PreXPath.Length - tem.Length);
                    pattern.XPath = pattern.PreXPath + pattern.SubXPath;
                    pattern.Content = content;
                    pattern.GainTextRate = gaintextrate;
                    if (!string.IsNullOrWhiteSpace(pattern.SubXPath))
                        pattern.SampleNode = itmp.SampleNode;
                    if (!currentpatternlist.Exists(p => p.XPath == pattern.XPath && p.PreXPath == pattern.PreXPath))
                        newpatternlist.Add(pattern);

                    //若containlist中包含多种下层类型，需要都加入
                    if (formContainList(itmp.CNode.ParentNode).Keys.Count() > 1)
                    {
                        ItemPatternAnaly pattern_sameparentnode = new ItemPatternAnaly(itmp.SampleNode, itmp.CNode.ParentNode, formContainList(itmp.CNode.ParentNode), formAttributelist(RootNode, itmp.CNode.ParentNode.XPath), countnode, counttextnode);
                        tem = itmp.PreXPath.Substring(itmp.PreXPath.LastIndexOf('/'));
                        pattern_sameparentnode.SubXPath = "/" + itmp.CNode.Name + itmp.SubXPath;
                        pattern_sameparentnode.PreXPath = itmp.PreXPath.Substring(0, itmp.PreXPath.Length - tem.Length);
                        pattern_sameparentnode.XPath = pattern_sameparentnode.PreXPath + pattern_sameparentnode.SubXPath;
                        pattern_sameparentnode.Content = content;
                        pattern_sameparentnode.GainTextRate = gaintextrate;
                        if (!currentpatternlist.Exists(p => p.XPath == pattern_sameparentnode.XPath && p.PreXPath == pattern_sameparentnode.XPath))
                            newpatternlist.Add(pattern_sameparentnode);
                    }

                    //如果同层含有多种attribute，需要分别加入标签注释以及not标签注释
                    if (itmp.Attributelist != null && itmp.Attributelist.Keys.Count() > 0)
                        foreach (string name in itmp.Attributelist.Keys)
                            if (name != "cutting" && (name == "id" || name == "class" || name == "align"))
                                foreach (string value in itmp.Attributelist[name])
                                {
                                    ItemPatternAnaly pattern_containattribute = new ItemPatternAnaly(itmp.SampleNode, itmp.CNode.ParentNode, formContainList(itmp.CNode.ParentNode), formAttributelist(RootNode, itmp.CNode.ParentNode.XPath), countnode, counttextnode);
                                    tem = itmp.PreXPath.Substring(itmp.PreXPath.LastIndexOf('/'));
                                    if (name != "class")
                                        pattern_containattribute.SubXPath = "/" + itmp.CNode.Name + "[@" + name + "='" + value + "']" + itmp.SubXPath;
                                    else pattern_containattribute.SubXPath = "/" + itmp.CNode.Name + "[contains(@class,'" + value + "')]" + itmp.SubXPath;
                                    pattern_containattribute.PreXPath = itmp.PreXPath.Substring(0, itmp.PreXPath.Length - tem.Length);
                                    pattern_containattribute.XPath = pattern_containattribute.PreXPath + pattern_containattribute.SubXPath;
                                    pattern_containattribute.Content = content;
                                    pattern_containattribute.GainTextRate = gaintextrate;
                                    if (!currentpatternlist.Exists(p => p.XPath == pattern_containattribute.XPath && p.PreXPath == pattern_containattribute.PreXPath))
                                        newpatternlist.Add(pattern_containattribute);

                                    ItemPatternAnaly pattern_ncontainattribute = new ItemPatternAnaly(itmp.SampleNode, itmp.CNode.ParentNode, formContainList(itmp.CNode.ParentNode), formAttributelist(RootNode, itmp.CNode.ParentNode.XPath), countnode, counttextnode);
                                    if (name != "class")
                                        pattern_ncontainattribute.SubXPath = "/" + itmp.CNode.Name + "[not(@" + name + "='" + value + "')]" + itmp.SubXPath;
                                    else
                                        pattern_ncontainattribute.SubXPath = "/" + itmp.CNode.Name + "[not(contains(@class,'" + value + "'))]" + itmp.SubXPath;
                                    pattern_ncontainattribute.PreXPath = itmp.PreXPath.Substring(0, itmp.PreXPath.Length - tem.Length);
                                    pattern_ncontainattribute.XPath = pattern_ncontainattribute.PreXPath + pattern_ncontainattribute.SubXPath;
                                    pattern_ncontainattribute.Content = content;
                                    pattern_ncontainattribute.GainTextRate = gaintextrate;
                                    if (!currentpatternlist.Exists(p => p.XPath == pattern_ncontainattribute.XPath && p.PreXPath == pattern_ncontainattribute.PreXPath))
                                        newpatternlist.Add(pattern_ncontainattribute);
                                }

                }
                foreach (ItemPatternAnaly newit in newpatternlist)
                    if (!currentpatternlist.Exists(p => p.XPath == newit.XPath && p.PreXPath == newit.PreXPath))
                        currentpatternlist.Add(newit);

                //已经到达顶点，或已经参与过三轮的使用，则不再参与合并
                currentpatternlist.ForEach(p => p.useless = (XPathUtility.isTopNode(p.CNode.ParentNode) || p.usedtime >= 3));
                newpatternlist.Clear();

            }

            ResultPatterns = currentpatternlist;
            ResultPatterns.ForEach(r => r.TextLenCount = TextCleaner.FullClean(r.Content).Length);

            ResultPatterns = ResultPatterns.OrderByDescending(r => r.GainTextRate).ToList();
            ResultPatterns = ResultPatterns.Where(r => r.TextLenCount >= ResultPatterns.FirstOrDefault(t => true).TextLenCount * 2 / 3).ToList();

            return ResultPatterns;

        }

        /// <summary>
        /// 新思路的第二步
        /// </summary>
        /// <param name="UnshortedPatterns"></param>
        /// <param name="RootNode"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<ItemPatternAnaly> Item_ShortPatterns_WebNews(List<ItemPatternAnaly> UnshortedPatterns, HtmlNode RootNode, string url)
        {
            List<ItemPatternAnaly> ShortPatterns = new List<ItemPatternAnaly>();

            foreach (ItemPatternAnaly itpma in UnshortedPatterns)
            {

                string xpath = itpma.XPath;

                string prestring = xpath, substring = string.Empty;
                int nofat = 200, nonot = 200, nonon = 200;

                substring = xpath.Substring(xpath.IndexOf("/") + 1);
                while (substring.Contains("/"))
                {
                    string first = substring.Substring(0, substring.IndexOf("/"));
                    if (first.EndsWith("]") || first.EndsWith("l") || first.EndsWith("y"))
                        substring = substring.Substring(substring.IndexOf("/") + 1);
                    else
                        break;
                }
                nonon = xpath.Length - substring.Length + 1;
                if (string.IsNullOrWhiteSpace(substring) || (!substring.Contains("/") && substring.Contains("]")))
                { substring = string.Empty; nonon = 200; }

                if (xpath.Contains("@")) nofat = xpath.IndexOf("@");
                if (xpath.Contains("not")) nonot = xpath.IndexOf("not");
                nofat = Math.Min(nofat, nonon);

                if (Math.Min(nonot, nofat) < 200)
                {
                    prestring = xpath.Substring(0, Math.Min(nonot, nofat) - 1);
                    prestring = prestring.Substring(0, prestring.LastIndexOf("/"));
                    substring = xpath.Substring(prestring.Length);
                }



                try
                {
                    if (RootNode.SelectSingleNode(prestring) == null) continue;
                }
                catch
                {//todo:差掉这个错误，什么鬼东西
                    continue;
                }
                HtmlNode node = RootNode.SelectSingleNode(prestring);
                prestring = XPathUtility.GetrawXPath(node, RootNode, 0, url, true);

                if (!string.IsNullOrEmpty(substring)) itpma.ShortXPath = prestring + substring;
                else itpma.ShortXPath = prestring;

                ShortPatterns.Add(itpma);
                if (itpma.ShortXPath.Contains("@class="))
                    continue;
            }
            return ShortPatterns;
        }

        /// <summary>
        /// 重构后的样子，在几个url之间进行合并，但是并没有保存在url中的数量
        /// </summary>
        /// <param name="UnintergratedPatterns"></param>
        /// <returns></returns>
        public static List<ItemPatternAnaly> Item_IntergratePatterns_WebNews(Dictionary<string, List<ItemPatternAnaly>> UnintergratedPatternsWithUrl)
        {
            List<ItemPatternAnaly> ResultPatternAnaly = new List<ItemPatternAnaly>();
            foreach (string url in UnintergratedPatternsWithUrl.Keys)
                foreach (ItemPatternAnaly itpma in UnintergratedPatternsWithUrl[url])
                {
                    if (!ResultPatternAnaly.Exists(r => r.ShortXPath == itpma.ShortXPath))
                        ResultPatternAnaly.Add(itpma);
                    else
                    {
                        ResultPatternAnaly.First(r => r.ShortXPath == itpma.ShortXPath).TextLenCount += itpma.TextLenCount;
                        ResultPatternAnaly.First(r => r.ShortXPath == itpma.ShortXPath).TextNodesCount += itpma.TextNodesCount;
                        ResultPatternAnaly.First(r => r.ShortXPath == itpma.ShortXPath).Content += itpma.Content;
                    }
                }
            return ResultPatternAnaly;
        }


        /// <summary>
        /// 这里是用之前保留的信息来检查的
        /// </summary>
        /// <param name="UncheckedPatterns"></param>
        /// <param name="RelChars"></param>
        /// <returns></returns>
        public static Dictionary<PatternType, List<ItemPatternAnaly>> Item_CheckNodesForRelXPath(List<ItemPatternAnaly> UncheckedPatterns, string url, Dictionary<PatternType, Dictionary<string, string>> RelString)
        {
            Dictionary<PatternType, List<ItemPatternAnaly>> CheckedPatterns = new Dictionary<PatternType, List<ItemPatternAnaly>>();
            List<HtmlNode> Nodes = new List<HtmlNode>();

            int i = 1;
            foreach (PatternType pt in RelString.Keys)
            {
                string rel = RelString[pt][url];
                List<ItemPatternAnaly> CheckedPatternsinCertainPt = new List<ItemPatternAnaly>();
                foreach (ItemPatternAnaly itmpa in UncheckedPatterns)
                {
                    CheckedPatternsinCertainPt = Item_CheckNodesForRelXPath_ifContains(itmpa, pt, rel, CheckedPatternsinCertainPt);
                }
                CheckedPatterns.Add(pt, CheckedPatternsinCertainPt);
            }
            return CheckedPatterns;
        }

        /// <summary>
        /// 其实没什么必要写出来，发现是因为innertext的问题之后就不必要单独拿出来了
        /// </summary>
        /// <param name="itmpa"></param>
        /// <param name="pt"></param>
        /// <param name="rel"></param>
        /// <param name="CheckedPatterns"></param>
        /// <returns></returns>
        public static List<ItemPatternAnaly> Item_CheckNodesForRelXPath_ifContains(ItemPatternAnaly itmpa, PatternType pt, string rel, List<ItemPatternAnaly> CheckedPatterns)
        {
            if (!string.IsNullOrWhiteSpace(itmpa.Content) && itmpa.Content.Contains(rel))
                CheckedPatterns.Add(itmpa);
            return CheckedPatterns;
        }

        /// <summary>
        /// 统计字符密度
        /// </summary>
        /// <param name="xpath"></param>
        /// <param name="RootNodes"></param>
        /// <returns></returns>
        public static int Item_CountText(string xpath, List<HtmlNode> RootNodes)
        {
            int count = 0;
            foreach (HtmlNode rootnode in RootNodes)
            {
                List<HtmlNode> Nodes = rootnode.SelectNodes(xpath).ToList();
                foreach (HtmlNode node in Nodes)
                    count += Item_CountTextLength(node);
            }
            return count;
        }

        /// <summary>
        /// 递归检查所有点下面所有的字符数量
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static int Item_CountTextLength(HtmlNode Node)
        {
            int Count = 0;
            if (!Node.HasChildNodes)
                return HTMLCleaner.GetCleanInnerText(Node).Length;
            else
                if (Node.ChildNodes.Count > 0)
                foreach (HtmlNode childnode in Node.ChildNodes)
                    Count += Item_CountTextLength(childnode);
            return Count;
        }

        /// <summary>
        /// 决定复制弄一个出来。首先保存一个阶层的数据，即每个pattern需要向上多少级才能到顶。后面一堆参数都可以不用了。然而还是有点担心啊，这样会不会基本无法得到什么结果，大部分都返回empty？
        /// 
        /// </summary>
        /// <param name="Node">要提取XPath的节点</param>
        /// <param name="RootNode">页面根节点</param>
        /// <param name="ShortXPaths">该页面已缓存过的短路径，用于加速节点查询</param>
        /// <param name="AddToCache">是否将结果加入ShortXPaths缓存数组</param>
        /// <param name="IgnoreOrderNumber_UpLevel">到ItemRoot的最长层级</param>
        /// <param name="PatternMinCountItem">最少多少Item才是合法的</param>
        /// <returns></returns>
        public static string Item_GetshortXPath(Dictionary<string, HtmlNode> Nodes, Dictionary<string, HtmlNode> RootNodes)
        {

            if (Nodes == null) return null;
            string Shortest = null;
            foreach (HtmlNode node in Nodes.Values)
                if (XPathUtility.isTopNode(node))
                    return node.XPath;
            string urll = Nodes.Keys.First(t => true);//这个是为了方便随机取一个作为代表来完成计算，获取xpath之后会对所有的点进行验证
            Dictionary<string, HtmlNode> ParentNodes = new Dictionary<string, HtmlNode>();
            foreach (string url in Nodes.Keys)
                ParentNodes.Add(url, Nodes[url].ParentNode);
            bool jud0;


            jud0 = true;
            //第一招，如果有ID且全局唯一，则可以缩短
            foreach (string url in Nodes.Keys)
                if (string.IsNullOrWhiteSpace(Nodes[url].Id)
                    || (RootNodes[url].SelectNodes(string.Format("//{0}[@id=\"{1}\"]", Nodes[url].Name, Nodes[url].Id)).Count != 1)
                    || XPathUtility.isID_RandomGUID(Nodes[url], RootNodes[url]))
                    jud0 = false;
            if (jud0)
            {
                string Name_NoNumber;

                //如果没有序号的话
                if (!XPathUtility.isID_SeqNumber(Nodes[urll], RootNodes[urll], out Name_NoNumber))//这里似乎有一个上限选择。设置为1吧。其实应该认为最后一招是用不到的
                {
                    Shortest = string.Format("//{0}[@id=\"{1}\"]", Nodes[urll].Name, Nodes[urll].Id);
                    //理论上来讲这些都可以通用的，但还是确认一下比较好
                    bool jud1 = true;
                    foreach (string url in Nodes.Keys)
                        if (RootNodes[url].SelectNodes(Shortest) == null || RootNodes[url].SelectNodes(Shortest).Count > 1)
                            jud1 = false;
                    if (!jud1)
                        Shortest = string.Empty;
                }
                else
                {
                    string GroupPath = string.Format("//{0}[contains(@id,'{1}')]", Nodes[urll].Name, Name_NoNumber);
                    Dictionary<string, int> Indexs = new Dictionary<string, int>();
                    foreach (string url in Nodes.Keys)
                    {
                        HtmlNodeCollection Siblings = RootNodes[urll].SelectNodes(GroupPath);
                        Indexs.Add(url, 0);
                        for (; Indexs[url] < Siblings.Count; Indexs[url]++)
                            if (Siblings[Indexs[url]] == Nodes[url]) break;
                    }
                    bool jud2 = true;
                    foreach (string url in Indexs.Keys)
                        if (Indexs[url] != Indexs.Values.First(t => true))
                            jud2 = false;
                    if (jud2)
                        Shortest = string.Format("{0}[{1}]", GroupPath, Indexs[urll] + 1);
                }
            }

            //第二招
            else if (Nodes[urll].Attributes.Contains("class") && !string.IsNullOrWhiteSpace(Nodes[urll].Attributes["class"].Value))
            {
                string ParentNoClassPath = Item_GetshortXPath(ParentNodes, RootNodes);
                //如果向上的节点可以用Id搞定，就不考虑class了
                if (ParentNoClassPath.Contains("@id"))
                {
                    Shortest = ParentNoClassPath + Nodes[urll].XPath.Substring(Nodes[urll].XPath.LastIndexOf('/'));
                    bool jud3 = true;
                    foreach (string url in Nodes.Keys)
                        if (RootNodes[url].SelectNodes(Shortest).Count != 1)
                            jud3 = false;
                    if (!jud3)
                        Shortest = string.Empty;
                }
                string ClassPath = string.Empty;
                foreach (string className in Nodes[urll].Attributes["class"].Value.Split())
                {
                    if (className.Contains("$")) continue;
                    ClassPath = string.Format(@"//{0}[contains(@class,'{1}')]", Nodes[urll].Name, className);

                    //首先你得到的结果不能为空吧
                    if (RootNodes[urll].SelectNodes(ClassPath) == null) continue;

                    bool jud4 = true;//jud4来判断是否全局唯一
                    bool jud5 = true;//jud5来判断是否单层唯一
                    foreach (string url in Nodes.Keys)
                    {
                        if (RootNodes[url].SelectNodes(ClassPath) == null || RootNodes[url].SelectNodes(ClassPath).Count != 1)
                            jud4 = false;
                        if (ParentNodes[url].SelectNodes(ClassPath) == null || RootNodes[url].SelectNodes(ClassPath).Count != 1)
                            jud5 = false;
                    }
                    //然后如果全局唯一的话就可以这样结束了
                    if (jud4)
                    {
                        Shortest = ClassPath;
                        break;
                    }
                    //如果不是全局唯一，而是单层唯一的话就向上递归吧。如果单层唯一也不是那就继续吧
                    else if (jud5)
                    {
                        Shortest = Item_GetshortXPath(ParentNodes, RootNodes) + ClassPath.Substring(1);
                    }
                }
            }
            //第三招，通过本级元素数量来限定上级元素（如果递归全部都是序号标识，才被迫尝试这步，险棋。这一步难改）
            if (string.IsNullOrWhiteSpace(Shortest))
            {
                string LongXPath = Nodes[urll].XPath;
                Shortest = Item_GetshortXPath(ParentNodes, RootNodes) + LongXPath.Substring(LongXPath.LastIndexOf('/'));
                if (!Shortest.Contains("@id") && !Shortest.Contains("@class") && !Shortest.Contains("last()"))
                {
                    string Suffix = string.Empty;
                    for (int i = 0; i < Regex.Matches(Nodes[urll].XPath, @"\/").Count; i++)
                    {
                        HtmlNodeCollection Siblings = ParentNodes[urll].SelectNodes("./" + Nodes[urll].Name);
                        int CountSibling = Siblings.Count;
                        if (CountSibling >= 1)
                        {
                            //是为了减少数量限制，避免限制过死
                            int CountLimit = CountSibling;
                            if (CountLimit > 15) CountLimit--;
                            if (CountLimit > 5) CountLimit--;
                            //现在寻找序号
                            int Index = 0;
                            for (; Index < Siblings.Count; Index++)
                                if (Siblings[Index] == Nodes[urll]) break;
                            //能否唯一定位Current所处的节点？（注意XPath的Index是从1开始的）
                            string NodeCountPath = string.Format("//{0}/{1}[last()>={2}][{3}]", ParentNodes[urll].Name, Nodes[urll].Name, CountLimit, Index + 1);
                            if ((!string.IsNullOrEmpty(Suffix) && (LongXPath.Length == Suffix.Length || Suffix.Length == Shortest.Length))) break;// 这里是一个终点所以条件不是那些什么#之类的
                            Suffix = i == 0 ? string.Empty : Shortest.Substring(LongXPath.LastIndexOf('/', Suffix.Length));
                            if (NodeCountPath.Contains("#")) continue;
                            bool jud5 = true;
                            foreach (string url in Nodes.Keys)
                                if (RootNodes[url].SelectNodes(NodeCountPath) != null)
                                    if (RootNodes[url].SelectNodes(NodeCountPath).Count != 1)
                                    {
                                        jud5 = false;
                                    }
                            if (jud5)
                            {
                                Shortest = NodeCountPath + Suffix;
                                break;
                            }

                        }
                    }
                }
            }
            return Shortest;
        }

        public static string Item_GetShortXPath(Dictionary<string, HtmlNode> Nodes, Dictionary<string, HtmlNode> RootNodes)
        {
            return Item_GetshortXPath(Nodes, RootNodes);
        }

        public static string Item_FormalizeTextNodeXPath(string xpath)
        {
            if (xpath.Contains("#text"))
                xpath = xpath.Replace("#text", "text()");
            if (xpath.Contains("#content"))
                xpath = xpath.Replace("#content", "content()");
            return xpath;
        }

        #endregion Item页面部分

        #region 临时总结论坛内容

        public static void Item_ExtractForumPatterns(List<string> PageUrls)
        {
            List<string> PossibleFloorRootXPaths = new List<string>();


            foreach (string url in PageUrls)
            {
                string HtmlContent = HttpHelper.GetHttpContent(url);
                HtmlNode rootNode = HtmlUtility.getSafeHtmlRootNode(HtmlContent, true, true);
                //先进行楼层的识别，得到一系列大概的楼层节点

                List<HtmlNode> basenodes = GetBaseNodes(rootNode);

                string shortxpath = XPathUtility.GetrawXPath(basenodes.First(t => true).ParentNode, rootNode, 0, url);
                string RootXPath = shortxpath + "/" + basenodes.First(t => true).Name;
                //考虑如何去除杂志广告div？
                List<string> possiblerootxpaths = GetPreciseRootxpath(basenodes, rootNode.SelectSingleNode(basenodes.First(t => true).ParentNode.XPath), RootXPath);

                foreach (string path in possiblerootxpaths)
                    if (!PossibleFloorRootXPaths.Contains(path))
                        PossibleFloorRootXPaths.Add(path);

                List<string> PossibleAuthorXPaths = GetPreciseAuthorRelXPaths(basenodes, rootNode);
                List<string> PossibleDateXPaths = getPreciseDateRelXPaths(basenodes, rootNode);
                List<string> PossibleFloorXPaths = getPreciseFloorRelXPaths(basenodes, rootNode);
                //List<string> PossibleContentXPaths = getPreciseContentRelXPaths(basenodes, rootNode);

            }





        }

        /// <summary>
        /// 从一堆node中整合出一组格式相近而且层级相同的xpath
        /// </summary>
        /// <param name="texts"></param>
        /// <returns></returns>
        internal static List<string> CombinedXPaths(List<HtmlNode> texts)
        {
            if (texts == null || texts.Count() == 0) return new List<string>();
            List<string> OriXPaths = new List<string>(); List<string> XPaths = new List<string>();
            foreach (HtmlNode node in texts)//先把所有的xpath放到一起
                OriXPaths.Add(node.XPath);
            foreach (string orixpath in OriXPaths)//再直接剪掉长度不整齐的
                if (OriXPaths.Where(x => Math.Abs(x.Length - orixpath.Length) < 2).Count() > OriXPaths.Count() / 3)
                    XPaths.Add(orixpath);
            OriXPaths = XPaths;
            Dictionary<string, int> CombinedXPaths = new Dictionary<string, int>();
            foreach (string xpath in OriXPaths)
                CombinedXPaths.Add(xpath, 1);

            //开始循环向上寻找最大的
            for (int i = 1; i < 40; i++)
            {
                Dictionary<string, int> ParentCombinedXPaths = new Dictionary<string, int>();
                foreach (string xpath in CombinedXPaths.Keys)
                {
                    string parentxpath = xpath.Remove(xpath.LastIndexOf("/"));
                    if (ParentCombinedXPaths.ContainsKey(parentxpath))
                        ParentCombinedXPaths[parentxpath] += CombinedXPaths[xpath];
                    else
                        ParentCombinedXPaths.Add(parentxpath, CombinedXPaths[xpath]);
                }
                CombinedXPaths = ParentCombinedXPaths;
                if (CombinedXPaths.Values.ToList().Exists(n => n > OriXPaths.Count() / 2))
                    break;
            }
            string ancesterxpath = CombinedXPaths.Keys.First(n => CombinedXPaths[n] > OriXPaths.Count() / 2);

            XPaths = new List<string>();
            foreach (string path in OriXPaths)
                if (path.Contains(ancesterxpath))
                {
                    string newpath = path.Substring(0, path.IndexOf("/", ancesterxpath.Length + 1));
                    if (!XPaths.Contains(newpath))
                        XPaths.Add(newpath);
                }
            return XPaths;
        }


        /// <summary>
        /// 从一个页面的根节点生成合适的楼层基本node
        /// </summary>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        internal static List<HtmlNode> GetBaseNodes(HtmlNode rootNode)
        {
            List<HtmlNode> textsp = new List<HtmlNode>();
            List<HtmlNode> textsdiv = new List<HtmlNode>();
            List<HtmlNode> textsspan = new List<HtmlNode>();
            List<HtmlNode> textsstrong = new List<HtmlNode>();
            try
            { textsp = rootNode.SelectNodes(".//p").Where(p => p.InnerText.Length < 40 && Regex.Matches(p.InnerText, @"\d{1,9}").Count > 0 && (p.InnerText.Contains("#") || p.InnerText.Contains("楼"))).ToList(); }
            catch { }
            try
            { textsdiv = rootNode.SelectNodes(".//div").Where(p => p.InnerText.Length < 40 && Regex.Matches(p.InnerText, @"\d{1,9}").Count > 0 && (p.InnerText.Contains("#") || p.InnerText.Contains("楼"))).ToList(); }
            catch { }
            try
            { textsspan = rootNode.SelectNodes(".//span").Where(p => p.InnerText.Length < 40 && Regex.Matches(p.InnerText, @"\d{1,9}").Count > 0 && (p.InnerText.Contains("#") || p.InnerText.Contains("楼"))).ToList(); }
            catch { }
            try
            { textsstrong = rootNode.SelectNodes(".//strong").Where(p => p.InnerText.Length < 40 && Regex.Matches(p.InnerText, @"\d{1,9}").Count > 0 && (p.InnerText.Contains("#") || p.InnerText.Contains("楼"))).ToList(); }
            catch { }
            List<string> levelrootxpath = new List<string>();
            levelrootxpath.AddRange(CombinedXPaths(textsp));
            levelrootxpath.AddRange(CombinedXPaths(textsdiv));
            levelrootxpath.AddRange(CombinedXPaths(textsspan));
            levelrootxpath.AddRange(CombinedXPaths(textsstrong));

            List<string> rootxpath = new List<string>();
            foreach (string lrxpath in levelrootxpath)
                if (!rootxpath.Contains(lrxpath))
                    rootxpath.Add(lrxpath);
            List<HtmlNode> basenodes = new List<HtmlNode>();
            foreach (string rxpath in rootxpath)
                basenodes.Add(rootNode.SelectSingleNode(rxpath));

            return basenodes;
        }

        /// <summary>
        /// 对楼层基本点的路径进行精准化，使用所有可能的标签
        /// </summary>
        /// <param name="BaseNodes"></param>
        /// <param name="ParentNode"></param>
        /// <param name="RootXPath"></param>
        /// <param name="mastif"></param>
        /// <returns></returns>
        internal static List<string> GetPreciseRootxpath(List<HtmlNode> BaseNodes, HtmlNode ParentNode, string RootXPath, int mastif = 2)
        {
            if (BaseNodes == null || BaseNodes.Count() == 0) return null;
            string nodename = BaseNodes.First(t => true).Name;
            List<string> PossiblePreciseRootXPath = new List<string>();
            Dictionary<string, List<string>> PossibleAttributes = new Dictionary<string, List<string>>();
            foreach (HtmlNode node in BaseNodes)
            {
                if (!node.HasAttributes || node.Attributes.Count() == 0) continue;
                foreach (HtmlAttribute a in node.Attributes)
                {
                    if (!PossibleAttributes.ContainsKey(a.Name)) PossibleAttributes.Add(a.Name, new List<string>());
                    if (!PossibleAttributes[a.Name].Contains(a.Value)) PossibleAttributes[a.Name].Add(a.Value);
                }
            }
            foreach (string name in PossibleAttributes.Keys)
                foreach (string value in PossibleAttributes[name])
                {
                    string adrule = "[contains(@" + name + ",'" + value + "')]";
                    List<HtmlNode> TheseNodes = ParentNode.SelectNodes(nodename + adrule).ToList();
                    if (TheseNodes.Except(BaseNodes).Count() < mastif && BaseNodes.Except(TheseNodes).Count() < mastif)
                        PossiblePreciseRootXPath.Add(RootXPath + adrule);

                    adrule = "[@" + name + "='" + value + "']";
                    TheseNodes = ParentNode.SelectNodes(nodename + adrule).ToList();
                    if (TheseNodes.Except(BaseNodes).Count() < mastif && BaseNodes.Except(TheseNodes).Count() < mastif)
                        PossiblePreciseRootXPath.Add(RootXPath + adrule);

                }


            return PossiblePreciseRootXPath;
        }

        /// <summary>
        /// 通过是否含有url、是否有文字、是否有特殊标签来选择楼层作者的地址
        /// </summary>
        /// <param name="BaseNodes"></param>
        /// <returns></returns>
        internal static List<string> GetPreciseAuthorRelXPaths(List<HtmlNode> BaseNodes, HtmlNode RootNode)
        {
            Dictionary<string, int> AuthorXPaths = new Dictionary<string, int>();
            foreach (HtmlNode basenode in BaseNodes)
            {
                List<HtmlNode> anodes = basenode.SelectNodes(".//a").Where(a => !string.IsNullOrWhiteSpace(a.InnerText)).ToList();
                anodes = anodes.Where(a => a.HasAttributes && a.Attributes.Count(t => t.Name == "href") > 0).ToList();
                anodes = anodes.Where(a => a.Attributes.First(t => t.Name == "href").Value.Contains("http") || a.Attributes.First(t => t.Name == "href").Value.Contains("/home/")).ToList();
                if (anodes == null || anodes.Count() == 0) continue;

                foreach (HtmlNode anode in anodes)
                {
                    string rawxpath = getRelXPath(anode, basenode, RootNode);
                    if (!AuthorXPaths.ContainsKey(rawxpath)) AuthorXPaths.Add(rawxpath, 0);

                    int plus = ParentswithAttributes(anode, basenode, "aut");
                    AuthorXPaths[rawxpath] += (plus + 1);
                }
            }
            List<string> AuthorXpath = AuthorXPaths.Keys.ToList();
            AuthorXpath = AuthorXpath.OrderByDescending(p => AuthorXPaths[p]).ToList();
            return AuthorXpath;
        }


        /// <summary>
        /// 通过是否能成功识别为日期、是否有特殊标签来选择日期地址
        /// </summary>
        /// <param name="BaseNodes"></param>
        /// <returns></returns>
        internal static List<string> getPreciseDateRelXPaths(List<HtmlNode> BaseNodes, HtmlNode RootNode)
        {
            Dictionary<string, int> DateXPaths = new Dictionary<string, int>();
            foreach (HtmlNode basenode in BaseNodes)
            {
                List<HtmlNode> anodes = basenode.SelectNodes(".//text()").Where(a => !string.IsNullOrWhiteSpace(a.InnerText)).ToList();
                anodes = anodes.Where(a => DateTimeParser.Parser(a.InnerText) != null && DateTimeParser.Parser(a.InnerText).Year > 2000).ToList();
                if (anodes == null || anodes.Count() == 0) continue;

                foreach (HtmlNode anode in anodes)
                {

                    string rawxpath = getRelXPath(anode, basenode, RootNode);
                    if (!DateXPaths.ContainsKey(rawxpath)) DateXPaths.Add(rawxpath, 0);

                    int plustime = ParentswithAttributes(anode, basenode, "time");
                    int plusdate = ParentswithAttributes(anode, basenode, "time");
                    DateXPaths[rawxpath] += (plusdate + plustime + 1);
                }
            }
            List<string> DateXpath = DateXPaths.Keys.ToList();
            DateXpath = DateXpath.OrderByDescending(p => DateXPaths[p]).ToList();
            return DateXpath;
        }

        /// <summary>
        /// 通过是否含有楼层信息来选择楼层
        /// </summary>
        /// <param name="BaseNodes"></param>
        /// <returns></returns>
        internal static List<string> getPreciseFloorRelXPaths(List<HtmlNode> BaseNodes, HtmlNode RootNode)
        {
            Dictionary<string, int> FloorXPaths = new Dictionary<string, int>();
            foreach (HtmlNode basenode in BaseNodes)
            {
                List<HtmlNode> anodes = basenode.SelectNodes(".//text()").Where(a => !string.IsNullOrWhiteSpace(a.InnerText)).ToList();
                anodes = anodes.Where(a => a.InnerText.Length < 40 && a.InnerText.Contains("楼")).ToList();
                if (anodes == null || anodes.Count() == 0) continue;

                foreach (HtmlNode anode in anodes)
                {
                    string rawxpath = getRelXPath(anode, basenode, RootNode);
                    if (!FloorXPaths.ContainsKey(rawxpath)) FloorXPaths.Add(rawxpath, 0);
                    int plusinfo = ParentswithAttributes(anode, basenode, "info");

                    FloorXPaths[rawxpath] += (plusinfo + 1);
                }
            }
            List<string> FloorXpath = FloorXPaths.Keys.ToList();
            FloorXpath = FloorXpath.OrderByDescending(p => FloorXPaths[p]).ToList();
            return FloorXpath;
        }

        /// <summary>
        /// 用来检查是否含有带某个字段的标签
        /// </summary>
        /// <param name="node"></param>
        /// <param name="TopNode"></param>
        /// <param name="specialvalue"></param>
        /// <returns></returns>
        internal static int ParentswithAttributes(HtmlNode node, HtmlNode TopNode, string specialvalue)
        {
            if (node == null || TopNode == null || node == TopNode) return 0;
            int score = 0;
            while (node != TopNode)
            {
                if (node.HasAttributes && node.Attributes.Count() > 0)
                    score += node.Attributes.Where(n => n.Value.Contains(specialvalue)).Count();
                node = node.ParentNode;
            }
            return score;
        }

        /// <summary>
        /// 简化的取相对路径的方式
        /// </summary>
        /// <param name="SeedNodes"></param>
        /// <param name="RootNode"></param>
        /// <param name="LinkPattern"></param>
        /// <param name="NodeFilter"></param>
        /// <param name="NodeScore"></param>
        /// <returns></returns>
        internal static string getRelXPath(HtmlNode LeaveNode, HtmlNode BaseNode, HtmlNode RootNode)
        {
            if (LeaveNode == RootNode) return string.Empty;

            //存放临时候选的RelXPath
            HashSet<string> NewRelXPaths = new HashSet<string>();
            //List<HtmlNode> ItemBaseNodes = RootNode.SelectNodes(LinkPattern.ItemBaseXPath).ToList();
            //第一步，检查从叶子到ItemRoot之间的所有ID和Class是否可用，生成全部候选的RelXPath

            HtmlNode current = LeaveNode;
            if (LeaveNode.NodeType != HtmlNodeType.Element) current = current.ParentNode;

            if (current.ParentNode.ChildNodes.Count() == 1 && current.Name == "#text") current = current.ParentNode;
            //if (current.Name == "#text") current.Name = "text()";
            //A标签要测试下其父级是否是否可以代表之，以避免某些媒体或作者名字没有链接的情况


            //找到真正的ItemBaseNode
            HtmlNode ItemBaseNode = current;
            while (ItemBaseNode != BaseNode && ItemBaseNode != RootNode) ItemBaseNode = ItemBaseNode.ParentNode;
            if (ItemBaseNode == RootNode) return string.Empty;

            //路径前缀：从叶子到ItemBase之间的(去下标，去掉开头'/')
            string RelXPathPrefix;
            if (ItemBaseNode.XPath.Length == current.XPath.Length) RelXPathPrefix = string.Empty;
            else RelXPathPrefix = XPathUtility.removeXPathOrderNumber(current.XPath.Substring(ItemBaseNode.XPath.Length + 1), 0);
            //路径后缀：加在当前节点的命名XPath之后来指向叶子节点
            string RelXPathSuffix = string.Empty;

            //从每个Node开始向上,直到ItemRoot级别
            int ClimbLevel = 0;
            while (ItemBaseNode != current)
            {
                //有ID，则根据ID生成一个候选的相对路径
                if (!string.IsNullOrWhiteSpace(current.Id))
                    NewRelXPaths.Add(string.Format(@".//{0}[@id={1}]{2}", current.Name, current.Id, RelXPathSuffix));

                //有Class，也生成一个相对路径
                if (current.Attributes.Contains("class") && !string.IsNullOrWhiteSpace(current.Attributes["class"].Value) && !current.Attributes["class"].Value.Contains('$'))
                    foreach (string className in current.Attributes["class"].Value.Split())
                        NewRelXPaths.Add(string.Format(@".//{0}[contains(@class,'{1}')]{2}", current.Name, className, RelXPathSuffix));

                int index = 0;
                //同一级有多个同类型的元素，则需要根据每个序号生成一个候选相对路径(例如bbs一个div中两个a,后一个是用户名)
                if ((current.NodeType == HtmlNodeType.Element || (current.NodeType == HtmlNodeType.Text)) && !XPathUtility.isTopNode(current))
                    //如果按此RelPath在每个BaseNode下可得到多个，则应该用序号来选出唯一一个；
                    //除非特殊情况，所有的Item都在同一个BaseNode下（例如某些狗屎网站，很多a写在一个div里，用br分开）
                    if (current.ParentNode.SelectNodes(@"./" + current.Name).Count > 1)
                    {

                        for (int i = 0; i < Math.Min(6, current.ParentNode.SelectNodes(@"./" + current.Name).Count); i++)
                        {
                            if (current.ParentNode.SelectNodes(@"./" + current.Name + "[" + (i + 1).ToString() + "]").Contains(current)) index = i + 1;
                            NewRelXPaths.Add(string.Format("{0}[{1}]{2}", RelXPathPrefix, i + 1, RelXPathSuffix));
                        }
                    }
                    else
                        NewRelXPaths.Add(string.Format("{0}{1}", RelXPathPrefix, RelXPathSuffix));

                //向上
                if (current.ParentNode.SelectNodes(@"./" + current.Name).Count > 1 && index != 0)
                    RelXPathSuffix = string.Format("/{0}{1}{2}{3}{4}", current.Name, RelXPathSuffix, '[', index.ToString(), ']');
                else
                    RelXPathSuffix = string.Format("/{0}{1}", current.Name, RelXPathSuffix);
                if (!string.IsNullOrWhiteSpace(RelXPathPrefix))
                    if (RelXPathPrefix.IndexOf('/') >= 0)
                        RelXPathPrefix = RelXPathPrefix.Substring(0, RelXPathPrefix.LastIndexOf('/'));
                    else
                        RelXPathPrefix = string.Empty;
                current = current.ParentNode;
                ClimbLevel++;
            }

            //end foreach node
            string returnstring = NewRelXPaths.OrderByDescending(t => Regex.Match(t, "/").Length).First(t => true);
            //无重复且排序好了，再返回
            return returnstring;
        }




        #endregion 临时总结论坛内容

        #endregion 模式训练流程

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
            if (MediaName.Contains(' ')) MediaName = MediaName.Substring(0, MediaName.IndexOf(' '));
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
                char[] leftb = new char[2]; leftb[0] = '（'; leftb[1] = '(';
                char[] rightb = new char[2]; rightb[0] = '）'; rightb[1] = ')';
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
    }
}