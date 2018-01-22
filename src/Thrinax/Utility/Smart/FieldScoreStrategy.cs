using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Thrinax.Enums;
using Thrinax.Models;
using Thrinax.Utility;

namespace Thrinax
{
    /// <summary>
    /// 保存需要提取的字段的统计特征，便于提取潜在的Node
    /// </summary>
    public class FieldScoreStrategy
    {
        static string FrequentChars_Dates = @"0123456789分钟前小时秒半天昨年月日期间发表于布稿出:：/-.更新上线星期周";
        static string FrequentChars_MustDate = @"分钟前小时秒半天昨年月日星期周";
        static string FrequentChars_ViewReply = @"0123456789:：/-评论回复点击查看浏览阅读参与人次已有互动围观超过数个第网友条被()（）[]【】";
        static string FrequentChars_MustNotMediaAuthorDate = @"分钟小时秒昨星期评论回复点击查看浏览阅读参与人次已有互动围观超过数个第网友条被";
        static string NoneNameSeperator = @":：/-.";

        static HashSet<char> FrequentCharSet_Dates = new HashSet<char>(FrequentChars_Dates);
        static HashSet<char> FrequentCharSet_MustDate = new HashSet<char>(FrequentChars_MustDate);
        static HashSet<char> FrequentCharSet_ViewReply = new HashSet<char>(FrequentChars_ViewReply);
        static HashSet<char> FrequentCharSet_MustNotMediaAuthorDate = new HashSet<char>(FrequentChars_MustNotMediaAuthorDate);
        static HashSet<char> NoneNameSeperatorSet = new HashSet<char>(NoneNameSeperator);

        static string DateClassNames = @"date time";
        static string ViewClassNames = @"view count num rep click";
        static string ReplyClassNames = @"rep";
        static string MediaClassNames = @"media soure from";
        static string AuthorClassNames = @"author uid user nick by";

        static string[] TitleClassNames_Must = @"content main".Split();
        static string[] TitleClassNames_MustNot = @"right side bar menu nav top10 top15 top20 most phb pic photo hot".Split();
        static string[] DateClassNames_Must = DateClassNames.Split();
        static string[] ViewClassNames_Must = ViewClassNames.Split();
        static string[] ReplyClassNames_Must = ReplyClassNames.Split();
        static string[] MediaClassNames_Must = MediaClassNames.Split();
        static string[] AuthorClassNames_Must = AuthorClassNames.Split();

        static string[] DateClassNames_MustNot = MediaClassNames_Must.Concat(AuthorClassNames_Must).ToArray();
        static string[] ViewClassNames_MustNot = DateClassNames_Must.Concat(MediaClassNames_Must).Concat(AuthorClassNames_Must).ToArray();
        static string[] MediaClassNames_MustNot = DateClassNames_Must.Concat(ViewClassNames_Must).Concat(AuthorClassNames_Must).ToArray();
        static string[] AuthorClassNames_MustNot = DateClassNames_Must.Concat(ViewClassNames_Must).Concat(MediaClassNames_Must).ToArray();

        /// <summary>
        /// 标题、作者、媒体名中的禁用词（排除掉这些词再进行可能性打分）
        /// </summary>
        public static string[] StopWords = @"更多 详细 详情 查看 点击 浏览 阅读 评论 转发 赞 回复 参与 more by from editor view read detail info news reply comment".Split();

        /// <summary>
        /// 屏蔽词，用于从Dom中提取候选节点时过滤（如果有些节点全是这些词，则忽略）
        /// </summary>
        public static string[] BanWords_NodeSelect = StopWords.Union(@"new New 分类 主题 关键词 作者 标题 预览".Split()).ToArray();

        static string[] ReplyWords = @"评论 回复".Split();

        const float FrequentChars_Min = 0.8f;
        const float FrequentChars_Max = 1;

        internal const string AuthorPrefixRegex = @"作\s*者|选\s*稿|编\s*辑|记\s*者|作\s*家";
        internal const string MediaPrefixRegex = @"名\s*称|媒\s*体|来\s*源|来\s*自|来\s*源\s*于|转\s*载|转\s*自|原\s*文|链\s*接";

        /// <summary>
        /// 未知ItemCount情况下的Title模式评分公式（得分越大越好）
        /// </summary>
        /// <param name="Nodes">无用参数</param>
        /// <param name="Pattern"></param>
        /// <param name="Strategy"></param>
        /// <param name="BaseItemCount">无用参数</param>
        /// <returns></returns>
        public double TitleScore_UnkownItemCount(IEnumerable<HtmlNode> Nodes, HtmlPattern Pattern, ListStrategy Strategy, int BaseItemCount = 0)
        {
            double Score = /*model.VisualScore * 100 + */ Pattern.ItemCount * 4 + Pattern.AverageTextLength * 3;

            //根据class和id是否有特殊字样加权
            bool IDClassNameMatched;
            double IDClassScore = IDClassNameScore(Pattern.LeastCommonAncestor, TitleClassNames_Must, null, out IDClassNameMatched);
            //命中Must +20%
            if (IDClassNameMatched) Score *= 1.2;
            IDClassScore = IDClassNameScore(Pattern.LeastCommonAncestor, null, TitleClassNames_MustNot, out IDClassNameMatched);
            //命中MustNot降一半
            if (IDClassNameMatched) Score /= 2;

            return Score;
        }

        /// <summary>
        /// 已知ItemCount情况下的Title模式评分公式（越大越好）
        /// </summary>
        /// <param name="Pattern"></param>
        /// <param name="Strategy"></param>
        /// <param name="BaseItemCount">ItemBase的数量</param>
        /// <returns></returns>
        public double TitleScore_WithItemCount(IEnumerable<HtmlNode> Nodes, HtmlPattern Pattern, ListStrategy Strategy, int BaseItemCount)
        {
            double Score = 0;

            //如果没有指定最佳ItemCount，则根据Strategy提供
            bool NoItemCount = false;
            if (BaseItemCount <= 0)
            {
                BaseItemCount = Strategy.List_BestItemCount;
                NoItemCount = true;
            }

            //Title长度打分10-100，均长跟BestAvg差异1/3或3倍以上的，10分
            double Rate = Pattern.AverageTextLength > Strategy.List_BestAvgTitleLen ? Pattern.AverageTextLength / (double)Strategy.List_BestAvgTitleLen : Strategy.List_BestAvgTitleLen / (double)Pattern.AverageTextLength;
            if (Rate > 3)
                Score = 10;
            else if (Rate > 2)
                //1/2-2倍到3倍之间，从70降到10
                Score = 10 + 60 * (3 - Rate);
            else //1/2-2倍以内的，100-70分
                Score = 70 + 30 * (2 - Rate);

            //先计算数量的可能性，在1/3-3倍之间都可以，可能性等比递减
            //fix 20141216: 基本上还是越多越好（例：两个标题平均长度接近的列表，一个9条，一个50条，都在2倍多，难以分辨）
            Rate = Pattern.ItemCount > BaseItemCount ? Pattern.ItemCount / (double)BaseItemCount : BaseItemCount / (double)Pattern.ItemCount;
            if (!NoItemCount || Pattern.ItemCount > BaseItemCount)
            {
                if (Rate > 3) //3倍以上5分
                    Score *= 5;
                else if (Rate > 2) //2-3倍，5-70分
                    Score *= (5 + 65 * (3 - Rate));
                else //2倍以内，70-100
                    Score *= 70 + 30 * (2 - Rate);
            }
            else //没有指定ItemCount的情况，且ItemCount<最佳值，则使用以下公式
            {
                if (Rate > 3) //3倍以上5分
                    Score *= 3;
                else if (Rate > 2) //2-3倍，5-70分
                    Score *= (2.5 + 30 * (3 - Rate));
                else //2倍以内，70-100
                    Score *= 35 + 15 * (2 - Rate);
            }

            //如果已经提取了RelXPath
            if (!string.IsNullOrEmpty(Pattern.RelXPath))
            {
                //XPath路径级别约小的越好，每多一个级别扣分
                if (Pattern.RelXPathLevel < 4) //小于3个级别，比较好
                    Score *= 1 + 0.3 * (3 - Pattern.RelXPathLevel);
                else
                    Score *= 1 - 0.2 * (Pattern.RelXPathLevel - 4);

                //用ID和class的好于用序号的，2倍得分
                if (Pattern.RelXPathUsingName)
                    Score *= 2;
            }

            //根据class和id是否有特殊字样加权
            bool IDClassNameMatched;
            double IDClassScore = IDClassNameScore(Pattern.LeastCommonAncestor, TitleClassNames_Must, null, out IDClassNameMatched);
            //命中Must +20%
            if (IDClassNameMatched) Score *= 1.2;
            IDClassScore = IDClassNameScore(Pattern.LeastCommonAncestor, null, TitleClassNames_MustNot, out IDClassNameMatched);
            //命中MustNot降一半
            if (IDClassNameMatched) Score /= 2;

            //对Title处理，如果处于h1下面的则加分50%
            Score *= HtmlTagNameScore(string.IsNullOrEmpty(Pattern.RelXPath) ? Pattern.XPath : Pattern.RelXPath);

            return Score;
        }

        /// <summary>
        /// 日期节点Pattern的评估函数（考虑时差和数量，可以理解为0-1之间的可能性数值，越大越好）
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="Pattern"></param> 
        /// <param name="Strategy"></param>
        /// <param name="BaseItemCount"></param>
        /// <returns></returns>
        internal static double DateNodeRelPatternScore(IEnumerable<HtmlNode> Nodes, ListStrategy Strategy, int BaseItemCount, int PathLevel, bool PathUsingName)
        {
            //直接用DateTimeParser.Parser了，所以不用考察字符密度了
            double SumDiff = 0;
            int ParseCount = 0;
            //所有日期和Now的差距综合
            foreach (HtmlNode Node in Nodes)
            {
                string Text = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(Node), true, true, true);
                if (Text.Length > Strategy.MaxLenDate) continue;

                DateTime? Val = DateTimeParser.Parser(Text);
                if (Val != null)
                {
                    ParseCount++;
                    DateTime d = (DateTime)Val;

                    //根据class和id是否有特殊字样加权
                    bool IDClassNameMatched;
                    double IDClassScore = IDClassNameScore(Node, DateClassNames_Must, DateClassNames_MustNot, out IDClassNameMatched);

                    if (d.Hour == 0 && d.Minute == 0 && d.Second == 0)
                        //只有日期，得分为距离Now的时差
                        SumDiff += Math.Abs((DateTime.Now - d).TotalDays / (IDClassNameMatched ? IDClassScore : 1));
                    else
                        //有精确时间，则只计入一半的时差
                        SumDiff += Math.Abs((DateTime.Now - d).TotalDays / 2 / (IDClassNameMatched ? IDClassScore : 1));
                }
            }

            double Possibility = 0;

            //先计算数量的可能性，在1/3-3倍之间都可以，可能性等比递减
            if (ParseCount * 3 < BaseItemCount || BaseItemCount * 3 < ParseCount) return 0;
            if (ParseCount >= BaseItemCount)
                Possibility = 1 / (ParseCount / BaseItemCount); //倍数的倒数
            else
                Possibility = 1 / (BaseItemCount / ParseCount);

            //根据平均时差来计算可能性
            double AvgDiff = SumDiff / ParseCount;
            if (AvgDiff > 180) //180以后统一为30%
                Possibility *= 0.3;
            else if (AvgDiff > 7) //1周以后为30%-100%，等比递减
                Possibility *= (0.3 + 0.004046 * (180 - AvgDiff));
            else if (AvgDiff > 1) //1周以内100%-150%
                Possibility *= (1 + 0.08333 * (7 - AvgDiff));
            else //1天以内的，150%-200%，便于区分原帖和评论的时间
                Possibility *= (1.5 + 0.5 * (1 - AvgDiff));

            //微调1：级别约小的越好，2级以上每多一个级别扣20%可能性
            if (PathLevel > 1)
                Possibility *= (1 - 0.2 * (PathLevel - 1));

            //微调2：用ID和class的好于用序号的，增加20%可能性
            if (PathUsingName)
                Possibility *= 1.2;

            return Possibility;
        }

        /// <summary>
        /// ViewReply节点的打分函数（越大越好）
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="Pattern"></param>
        /// <param name="Strategy"></param>
        /// <param name="BaseItemCount"></param>
        /// <param name="Contain2Numbers">元素中是否包含了2个数字</param>
        /// <param name="MustBeReply">是否一定是评论（class或内容有明确指示的情况）</param>
        /// <param name="AvgNumber">平均数，用于比较是View或Reply;如果在一个Element中有两个数字，则为长度2的数组，均值大的是第一个</param>
        /// <returns></returns>
        internal static double ViewNodeRelPatternScore(IEnumerable<HtmlNode> Nodes, ListStrategy Strategy, int BaseItemCount, int PathLevel, bool PathUsingName, out bool Contain2Numbers, out bool MustBeReply, out double[] AvgNumber)
        {
            Contain2Numbers = false;
            MustBeReply = false;
            AvgNumber = null;

            //平均字符密度的检查
            double AvgCharFreq = 0;
            foreach (HtmlNode Node in Nodes)
            {
                string Text = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(Node), true, true, true, false, true, false);
                if (Text.Length > Strategy.MaxLenView) continue;

                //检查是否命中日期的特殊字符
                if (TextCleaner.CountCharInSet(Text, FrequentCharSet_MustDate, false) > 0) return 0;
                //检查字符集密度， 不合格直接可能性为0
                int CharCount = TextCleaner.CountCharInSet(Text, FrequentCharSet_ViewReply);
                AvgCharFreq += CharCount / (double)Text.Length;
            }
            AvgCharFreq = AvgCharFreq / Nodes.Count();
            if (AvgCharFreq < FrequentChars_Min || AvgCharFreq > FrequentChars_Max) return 0;

            //尝试提取其中的连续数字再解析，统计每个Text有几个数字，及其极值分布
            int TotalCount = 0;
            int ParsedNode = 0;
            List<int> Ints0 = new List<int>(BaseItemCount);
            List<int> Ints1 = new List<int>(BaseItemCount);

            int ViewNameMatched = 0;
            double IDClassScore = 0;

            foreach (HtmlNode Node in Nodes)
            {
                string Text = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(Node), true, true, true, false, true, false);
                if (Text.Length > Strategy.MaxLenView) continue;

                MatchCollection digiText = Regex.Matches(Text, @"\d{1,9}");

                if (digiText.Count == 0) continue;
                //数量超过2则不可能
                if (digiText.Count > 2) return 0;

                TotalCount += digiText.Count;
                //记录每一个Parse出来的int
                int Val0 = int.Parse(digiText[0].Captures[0].Value);
                Ints0.Add(Val0);
                if (digiText.Count > 1)
                    Ints1.Add(int.Parse(digiText[1].Captures[0].Value));

                //根据class和id是否有特殊字样加权
                bool IDClassNameMatched;
                double s = IDClassNameScore(Node, ViewClassNames_Must, ViewClassNames_MustNot, out IDClassNameMatched);
                if (IDClassNameMatched)
                {
                    IDClassScore += s;
                    ViewNameMatched++;

                    if (s > 0) //正面命中的话，看看是不是reply
                        IDClassNameScore(Node, ReplyClassNames_Must, null, out IDClassNameMatched);
                    if (IDClassNameMatched) //有一个命中reply，这个标志就被设定了
                        MustBeReply = true;
                }

                //根据内容关键词来判断是否MustBeReply
                foreach (string ReplyKeyword in ReplyWords)
                    if (Text.Contains(ReplyKeyword))
                        MustBeReply = true;

                ParsedNode++;
            }


            //标志本元素包含两个数字
            if (Ints1.Count > 0 && Ints1.Count == Ints0.Count)
            {
                AvgNumber = new double[2];
                double Sum0 = Ints0.Sum();
                double Sum1 = Ints1.Sum();
                AvgNumber[0] = (Sum0 > Sum1 ? Sum0 : Sum1) / ParsedNode;
                AvgNumber[1] = (Sum0 > Sum1 ? Sum1 : Sum0) / ParsedNode;

                Contain2Numbers = true;
            }
            else if (Ints0.Count > 0 && ParsedNode > 0)
            {
                AvgNumber = new double[1];
                AvgNumber[0] = Ints0.Sum() / ParsedNode;
            }
            else
                AvgCharFreq = 0;

            //如果每个Text只有一个数字，加分20%-50%
            if (TotalCount == Nodes.Count())
                AvgCharFreq *= (Ints0.Max() <= 31) ? 1.2 : 1.5;
            else if (Ints0.Count > 0)
            {
                //有两个数字的检查极值
                if (Ints0.Max() <= 12 && Ints1.Max() <= 31 || Ints0.Max() <= 31 && Ints1.Max() <= 12)
                    AvgCharFreq *= 0.8;
                else //排除日期可能，得分翻倍
                    AvgCharFreq *= 2;
            }

            //根据class和id是否有特殊字样加权
            if (ViewNameMatched > 0)
            {
                IDClassScore /= ViewNameMatched;
                AvgCharFreq = (AvgCharFreq == 0 ? 1 : AvgCharFreq) * IDClassScore;
            }

            //微调1：级别约小的越好，2级以上每多一个级别扣10%可能性
            if (PathLevel > 1)
                AvgCharFreq *= (1 - 0.1 * (PathLevel - 1));

            //微调2：用ID和class的好于用序号的，增加20%可能性
            if (PathUsingName)
                AvgCharFreq *= 1.2;

            return AvgCharFreq;
        }

        /// <summary>
        /// 媒体名称节点的打分函数（越大越好）
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="Pattern"></param>
        /// <param name="Strategy"></param>
        /// <param name="BaseItemCount"></param>
        /// <returns></returns>
        internal static double MediaNodeRelPatternScore(IEnumerable<HtmlNode> Nodes, ListStrategy Strategy, int BaseItemCount, int PathLevel, bool PathUsingName)
        {
            double AvgPossibility = 0;
            foreach (HtmlNode Node in Nodes)
            {
                string Text = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(Node));
                if (Text.Length > Strategy.MaxLenMedia) continue;

                double Posibility = Strategy.MediaType == MediaType.WebNews ? 0.1 : 0;

                //检查是否命中日期或点击数的特殊字符，还不能全部排除，可能有特殊的报纸名称哦,本节点的可能性记为0继续下一个
                if (TextCleaner.CountCharInSet(Text, FrequentCharSet_MustNotMediaAuthorDate, false) > 1)
                    Posibility = 0.2;

                //是否是日期时间
                if (TextCleaner.CountCharInSet(Text, FrequentCharSet_Dates, false) > Text.Length * 0.6)
                    Posibility *= 0.3;

                //检查开头
                Match PrefixMatch = Regex.Match(Text, MediaPrefixRegex);
                if (PrefixMatch.Success)
                {
                    Text = Text.Substring(PrefixMatch.Index + PrefixMatch.Length).TrimStart(':', '：', ']', '】', ' ');
                    Posibility = 1;
                }

                //禁用词替换
                Text = TextCleaner.RemoveStopWords(Text, StopWords);

                //检查结尾
                if (Regex.IsMatch(Text, @"[报台网刊]"))
                    if (Posibility > 0)
                        Posibility = Math.Max(1, Posibility * 1.5);
                    else
                        Posibility = 0.8;

                //根据class和id是否有特殊字样加权
                bool IDClassNameMatched;
                double IDClassScore = IDClassNameScore(Node, MediaClassNames_Must, MediaClassNames_MustNot, out IDClassNameMatched);
                if (IDClassNameMatched)
                    Posibility = (Posibility == 0 ? 1 : Posibility) * IDClassScore;

                //长度检查
                if (Text.Length * 3 < Strategy.List_BestAvgMediaLen || Text.Length > Strategy.List_BestAvgMediaLen * 3)
                    Posibility /= 3; //超过3倍降权到1/3
                else
                {
                    //Rate在1-3之间
                    double Rate = Text.Length >= Strategy.List_BestAvgMediaLen ? Text.Length / (double)Strategy.List_BestAvgMediaLen
                        : Strategy.List_BestAvgMediaLen / (double)Text.Length;
                    //随着Rate提高降低
                    Posibility *= (0.4 + 0.35 * (3 - Rate));
                }

                //非名称分隔符（包含空格）出现，6折
                if (TextCleaner.CountCharInSet(Text, NoneNameSeperatorSet, true) > 0)
                    Posibility *= 0.6;

                //英文出现，再6折
                if (LanguageUtility.DetectedLanguage(Text) == Enums.Language.ENGLISH)
                    Posibility *= 0.6;

                //累加节点的可能性
                AvgPossibility += Posibility;
            }

            //微调1：级别约小的越好，2级以上每多一个级别扣10%可能性
            if (PathLevel > 1)
                AvgPossibility *= (1 - 0.1 * (PathLevel - 1));

            //微调2：用ID和class的好于用序号的，增加20%可能性
            if (PathUsingName)
                AvgPossibility *= 1.2;

            //论坛媒体减分
            if (Strategy.MediaType == Enums.MediaType.Forum)
                AvgPossibility /= 2;

            return AvgPossibility / Nodes.Count();
        }

        /// <summary>
        /// Author节点的打分函数（越大越好）
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="Pattern"></param>
        /// <param name="Strategy"></param>
        /// <param name="BaseItemCount"></param>
        /// <returns></returns>
        internal static double AuthorNodeRelPatternScore(IEnumerable<HtmlNode> Nodes, ListStrategy Strategy, int BaseItemCount, int PathLevel, bool PathUsingName)
        {
            double AvgPossibility = 0;
            foreach (HtmlNode Node in Nodes)
            {
                string Text = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(Node));
                if (Text.Length > Strategy.MaxLenAuthor) continue;

                double Posibility = Strategy.MediaType == Enums.MediaType.Forum ? 0.2 : 0;

                //检查是否命中日期或点击数的特殊字符，还不能全部排除，可能有特殊人名,本节点的可能性记为0继续下一个
                if (TextCleaner.CountCharInSet(Text, FrequentCharSet_MustNotMediaAuthorDate, false) > 0)
                    Posibility = 0.2;

                //是否是日期时间
                if (TextCleaner.CountCharInSet(Text, FrequentCharSet_Dates, false) > Text.Length * 0.6)
                    Posibility *= 0.3;

                //检查开头
                Match PrefixMatch = Regex.Match(Text, @"作\s*者|选\s*稿|编\s*辑|记\s*者");
                if (PrefixMatch.Success)
                {
                    Text = Text.Substring(PrefixMatch.Index + PrefixMatch.Length).TrimStart(':', '：', ']', '】', ' ');

                    //在第一个空格处截断
                    int SpaceIndex = Text.IndexOf(' ');
                    if (SpaceIndex > 0) Text = Text.Substring(0, SpaceIndex);

                    Posibility = 1;
                }

                //禁用词替换
                Text = TextCleaner.RemoveStopWords(Text, StopWords);

                //检查Top10姓氏命中
                if (Regex.IsMatch(Text, @"[李王张刘陈杨赵黄周吴]"))
                    if (Posibility > 0)
                        Posibility *= 1.1;
                    else
                        Posibility = 0.6;

                //根据class和id是否有特殊字样加权
                bool IDClassNameMatched;
                double IDClassScore = IDClassNameScore(Node, AuthorClassNames_Must, AuthorClassNames_MustNot, out IDClassNameMatched);
                if (IDClassNameMatched)
                    Posibility = (Posibility == 0 ? 1 : Posibility) * IDClassScore;

                //ID或Class中出现Reply则为0（只要主贴作者）
                IDClassNameScore(Node, ReplyClassNames_Must, null, out IDClassNameMatched);
                if (IDClassNameMatched) //有一个命中reply，这个标志就被设定了
                    Posibility = 0;

                //长度检查
                if (Text.Length * 3 < Strategy.List_BestAvgAuthorLen || Text.Length > Strategy.List_BestAvgAuthorLen * 3)
                    Posibility /= 3; //超过3倍降权到1/3
                else
                {
                    //Rate在1-3之间
                    double Rate = Text.Length >= Strategy.List_BestAvgAuthorLen ? Text.Length / (double)Strategy.List_BestAvgAuthorLen
                        : Strategy.List_BestAvgAuthorLen / (double)Text.Length;
                    //随着Rate提高降低
                    Posibility *= (0.4 + 0.35 * (3 - Rate));
                }

                //论坛出现英文字母、数字或下划线，+50%可能性
                if (Strategy.MediaType == Enums.MediaType.Forum && Regex.IsMatch(Text, @"[a-z,0-9,_]+", RegexOptions.IgnoreCase))
                    Posibility *= 1.5;

                //非名称分隔符（包含空格）出现，6折
                if (TextCleaner.CountCharInSet(Text, NoneNameSeperatorSet, Strategy.Language == Language.CHINESE) > 0)
                    Posibility *= 0.6;

                //累加节点的可能性
                AvgPossibility += Posibility;
            }

            //微调1：级别约小的越好，2级以上每多一个级别扣10%可能性
            if (PathLevel > 1)
                AvgPossibility *= (1 - 0.1 * (PathLevel - 1));

            //微调2：用ID和class的好于用序号的，增加20%可能性
            if (PathUsingName)
                AvgPossibility *= 1.2;

            return AvgPossibility / Nodes.Count();
        }

        /// <summary>
        /// 对一个Node的ID和ClassName打分,越大越好
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="Must">如果命中则1分</param>
        /// <param name="MustNot">如果命中则0分</param>
        /// <param name="Matched">是否命中任何类名</param>
        /// <param name="UpLevel">向上追溯几个级别（不含自身），上一级得分对折累加在自己的得分上</param>
        /// <returns></returns>
        static double IDClassNameScore(HtmlNode Node, IEnumerable<string> MustKeywords, IEnumerable<string> MustNotKeywords, out bool Matched, int UpLevel = 2)
        {
            Matched = false;
            if (Node == null) return 0;
            int Level = 0;
            double Score = 0;
            double LevelScore = 1;

            do
            {
                bool Must = XPathUtility.IDClassContain(Node, MustKeywords);
                bool MustNot = XPathUtility.IDClassContain(Node, MustNotKeywords);
                if (Must && !MustNot)
                    Score += LevelScore * 1.5;     //命中正面关键词且无负面
                if (Must && MustNot)
                    Score += LevelScore * 0.5;   //正负面都命中
                if (!Must && MustNot)
                    if (Level == 0) //只有负面
                    {
                        Matched = true;
                        return 0;   //第一级则直接返回不可能
                    }
                    else
                        Score -= LevelScore;

                if (Must || MustNot) //任何命中要有标志
                    Matched = true;

                //向上一级
                LevelScore *= 0.8;
                Node = Node.ParentNode;
            } while (!XPathUtility.isTopNode(Node) && Level++ < UpLevel);

            //什么都没有命中保持1
            return Score;
        }

        static HashSet<string> HighLevelTagName = new HashSet<string>("h1 h2 h3".Split());

        /// <summary>
        /// 检查Node的名称标签，对H1之类加权
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="UpLevel">向上追溯几个级别（不含自身）</param>
        /// <returns>1表示没有命中,>1表示命中，及加分</returns>
        static double HtmlTagNameScore(HtmlNode Node, int UpLevel = 2)
        {
            if (Node == null) return 1;
            int Level = 0;

            while (!XPathUtility.isTopNode(Node) && Level++ < UpLevel)
                if (HighLevelTagName.Contains(Node.Name.ToLower()))
                    return 1.5;
                else
                    Node = Node.ParentNode;

            return 1;
        }

        /// <summary>
        /// 检查Node的名称标签，对H1之类加权
        /// </summary>
        /// <param name="Path">根据路径来判定</param>
        /// <returns></returns>
        static double HtmlTagNameScore(string Path)
        {
            if (string.IsNullOrEmpty(Path)) return 1;
            foreach (string segment in Path.Split('/'))
                if (!string.IsNullOrEmpty(segment) && HighLevelTagName.Contains((segment.IndexOf('[') > 0 ? segment.Substring(0, segment.IndexOf('[')) : segment).ToLower()))
                    return 1.5;

            return 1;
        }

        public FieldScoreStrategy(Enums.MediaType MediaType, Enums.Language Language)
        {

        }
    }

}
