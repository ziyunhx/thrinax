using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Thrinax.Enums;
using Thrinax.Parser;
using Thrinax.Utility;
using SVM;
using Thrinax.Models;

namespace Thrinax.Utility.Smart
{
    /// <summary>
    /// 软规则，因为日后需要用统计数据对其进行不断修正和更新
    /// </summary>
    public class SoftStrategy
    {

        public readonly SVM.Model model_title;

        public readonly SVM.Model model_rel;

        public readonly MediaType MediaType;

        public readonly Language Language;

        public readonly HardThreshold Threshold;

        public readonly Feature Stencil_ListTitle;

        public readonly Feature Stencil_ListRel;

        public readonly Feature Stencil_ItemContent;

        public readonly Feature Stencil_ItemOthers;

        /// <summary>
        /// 最佳单页文章数量，保存的唯一原因是在没有打分方式时暂时充当打分标准
        /// </summary>
        public readonly int List_BestItemCount;

        /// <summary>
        /// 初始化函数
        /// </summary>
        /// <param name="MediaType"></param>
        /// <param name="Language"></param>
        /// <param name="modelpath_title"></param>
        /// <param name="modelpath_rel"></param>
        /// <param name="stencilfeature"></param>
        public SoftStrategy(Enums.MediaType MediaType, Enums.Language Language)
        {
            this.MediaType = MediaType;
            this.Language = Language;
 
            this.model_title = Model.Read("SVMmodel/model_title_WebNews");
            this.model_rel = Model.Read("SVMmodel/model_rel_WebNews");
             //目前在用比较笨的方法来赋值，以后将直接调用文件资源对这些评分标准进行赋值

            this.Threshold = new HardThreshold(MediaType, Language);

            #region stencil定义部分

            string FileAdd = Path.Combine("Stencil");
            if (File.Exists(FileAdd + "/Stencil_ListTitle"))
                this.Stencil_ListTitle = (Feature)JsonConvert.DeserializeObject(File.ReadAllText(FileAdd + "/Stencil_ListTitle"), typeof(Feature));
            else
            {
                #region listTitle模版定义部分
                this.Stencil_ListTitle = new Feature(1);
                Feature temp = new Feature(0);
                foreach (string key in temp.FigureFeatures.Keys)
                    if (key == "DateParseCount" || key == "AvgNumber" || key == "AvgDateDistance" || key == "DigitCountRate" || key == "DateCountRate")
                        this.Stencil_ListTitle.FigureFeatures[key] = 0;

                foreach (string key in temp.CharRecordf.Keys)
                    this.Stencil_ListTitle.CharRecordf[key] = 0;

                foreach (string key in temp.CharRecords.Keys)
                    this.Stencil_ListTitle.CharRecords[key] = 0;

                foreach (string key in temp.BoolFeatures.Keys)
                    this.Stencil_ListTitle.BoolFeatures[key] = 0;
                #endregion listtitle模版定义部分
            }

            if (File.Exists(FileAdd + "/Stencil_ListRel"))
                this.Stencil_ListRel = (Feature)JsonConvert.DeserializeObject(File.ReadAllText(FileAdd + "/Stencil_ListRel"), typeof(Feature));
            else
            {
                #region listRel模版定义部分
                this.Stencil_ListRel = new Feature(1);
                Feature temp = new Feature(0);
                foreach (string key in temp.FigureFeatures.Keys)
                    if (key == "DateParseCount" || key == "AvgNumber" || key == "AvgDateDistance" || key == "ItemCount" || key == "AvgDateDistance")
                        this.Stencil_ListRel.FigureFeatures[key] = 0;

                foreach (string key in temp.BoolFeatures.Keys)
                    this.Stencil_ListRel.BoolFeatures[key] = 0;

                foreach (string key in temp.IdClassnameRecord.Keys)
                    this.Stencil_ListRel.IdClassnameRecord[key] = 0;
                #endregion listRel模版定义部分
            }

            if (File.Exists(FileAdd + "/Stencil_ItemContent"))
                this.Stencil_ItemContent = (Feature)JsonConvert.DeserializeObject(File.ReadAllText(FileAdd + "/Stencil_ItemContent"), typeof(Feature));
            else
            {
                #region itemcontent模版定义部分
                this.Stencil_ItemContent = new Feature(1);
                Feature temp = new Feature(0);
                foreach (string key in temp.FigureFeatures.Keys)
                    if (key == "AllTextLen") this.Stencil_ItemContent.FigureFeatures[key] = 1;
                    else this.Stencil_ItemContent.FigureFeatures[key] = 0;
                foreach (string key in temp.CharRecordf.Keys)
                    this.Stencil_ItemContent.CharRecordf[key] = 1;
                foreach (string key in temp.CharRecords.Keys)
                    this.Stencil_ItemContent.CharRecords[key] = 1;
                foreach (string key in temp.IdClassnameRecord.Keys)
                    this.Stencil_ItemContent.IdClassnameRecord[key] = 1;
                foreach (string key in temp.BoolFeatures.Keys)
                    this.Stencil_ItemContent.BoolFeatures[key] = 0;
                #endregion itemcontent模版定义部分
            }

            if (File.Exists(FileAdd + "/Stencil_ItemOthers"))
                this.Stencil_ItemOthers = (Feature)JsonConvert.DeserializeObject(File.ReadAllText(FileAdd + "/Stencil_ItemOthers"), typeof(Feature));
            else
            {
                #region itemcontent模版定义部分
                this.Stencil_ItemOthers = new Feature(1);
                Feature temp = new Feature(0);
                foreach (string key in temp.FigureFeatures.Keys)
                    if (key == "AllTextLen" || key == "DateParseCount") this.Stencil_ItemOthers.FigureFeatures[key] = 1;
                    else this.Stencil_ItemOthers.FigureFeatures[key] = 0;
                foreach (string key in temp.CharRecordf.Keys)
                    this.Stencil_ItemOthers.CharRecordf[key] = 1;
                foreach (string key in temp.CharRecords.Keys)
                    this.Stencil_ItemOthers.CharRecords[key] = 1;
                foreach (string key in temp.IdClassnameRecord.Keys)
                    this.Stencil_ItemOthers.IdClassnameRecord[key] = 1;
                foreach (string key in temp.BoolFeatures.Keys)
                    this.Stencil_ItemOthers.BoolFeatures[key] = 0;
                #endregion itemcontent模版定义部分
            }

            #endregion stencil定义部分

        }

        /// <summary>
        /// 简单的初始化方式
        /// </summary>
        public SoftStrategy()
        {
            this.Threshold = new HardThreshold();
        }

        /// <summary>
        /// 读取model的说明文件，指示哪些特征是这个model选用了的。暂时还没用上
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal Feature ReadStencil(string address)
        {
            string[] Lines = File.ReadAllLines(address);
            Feature stencilfeature = new Feature(0);
            for (int i = 0; i < 200; i++)
            {
                string key;
                int value;
                if (Lines[0].Contains(','))
                {
                    key = Lines[0].Substring(0, Lines[0].IndexOf(','));
                    Lines[0] = Lines[0].Substring(Lines[0].IndexOf(',') + 1);
                    value = int.Parse(Lines[1].Substring(0, Lines[1].IndexOf(',')));
                    Lines[1] = Lines[1].Substring(Lines[1].IndexOf(',') + 1);
                }
                else
                {
                    key = Lines[0];
                    value = int.Parse(Lines[1]);
                }
                //检查五部分特征的分布
                if (stencilfeature.FigureFeatures.Keys.Contains(key)) stencilfeature.FigureFeatures[key] = value;
                if (stencilfeature.CharRecordf.Keys.Contains(key)) stencilfeature.CharRecords[key] = value;
                if (stencilfeature.CharRecords.Keys.Contains(key)) stencilfeature.CharRecords[key] = value;
                if (stencilfeature.IdClassnameRecord.Keys.Contains(key)) stencilfeature.IdClassnameRecord[key] = value;
                if (stencilfeature.BoolFeatures.Keys.Contains(key)) stencilfeature.BoolFeatures[key] = value;
            }
            return stencilfeature;
        }

        /// <summary>///html/body/div[4]/div[1]/div[6]/div[1]/ul/div[1]/div[2]/ol/li/a
        /// 验证一个List的模式是否能应用于某一个页面（只是检查是否明显不可能）
        /// 与下面的函数都是直接从ListStrategy里copy过来的
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="HTML"></param>
        /// <param name="XPath"></param>
        /// <returns></returns>
        public bool ValidateListXPath(string Url, string HTML, XpathPattern XPath)
        {
            //获取root节点（有些网站页面不带html标签的，直接从head开始写）
            HtmlNode rootNode = HtmlUtility.getSafeHtmlRootNode(HTML, true, true);
            if (rootNode == null)
                return false;

            HtmlNodeCollection rootNodes = rootNode.SelectNodes(XPath.ItemRootXPath);

            if (rootNodes == null)
                return false;

            var TitleNode = rootNodes.Select(f => f.SelectSingleNode(XPath.TitleXPath)).Where(f => f != null);
            if (TitleNode == null || TitleNode.Count() == 0 || (TitleNode.Count() == 1 && TitleNode.FirstOrDefault() == null))
                return false;

            //获取时有可能第一个为空
            TitleNode = TitleNode.Where(f => f != null);

            List<HtmlNode> TitleNodes = TitleNode.Where(a => !string.IsNullOrEmpty(a.InnerText)).ToList();
            double Score = ScoreforListTitle(TitleNodes);

            return ((Score > Threshold.LeastTitleScore || (Url.Contains("tieba.baidu.com") && Score > 100)) && ValidateListXPath(Url, rootNode, XPath));
        }

        /// <summary>
        /// 验证一个List的模式是否能应用于某一个页面（只是检查是否明显不可能）
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="RootNode"></param>
        /// <param name="XPath"></param>
        /// <returns></returns>
        public bool ValidateListXPath(string Url, HtmlNode RootNode, XpathPattern XPath)
        {
            if (string.IsNullOrEmpty(Url) || RootNode == null || XPath == null) return false;

            List<Article> Content = XpathParser.ExtractItemFromList(Url, RootNode, XPath);
            if (Content == null || Content.Count < 3) return false;

            int TitleCount = 0, DateCount = 0, ViewCount = 0, ReplyCount = 0, MediaCount = 0, AuthorCount = 0;
            foreach (Article ele in Content)
            {
                if (!string.IsNullOrEmpty(ele.Title) && !string.IsNullOrEmpty(ele.Url)) TitleCount++;
                if (!string.IsNullOrEmpty(XPath.DateXPath) && ele.PubDate != null) DateCount++;
                if (!string.IsNullOrEmpty(XPath.ViewXPath) && ele.ViewDataList?.FirstOrDefault()?.View >= 0) ViewCount++;
                if (!string.IsNullOrEmpty(XPath.ReplyXPath) && ele.ViewDataList?.FirstOrDefault()?.Reply >= 0) ReplyCount++;
                if (!string.IsNullOrEmpty(XPath.MediaNameXPath) && !string.IsNullOrEmpty(ele.MediaName)) MediaCount++;
                if (!string.IsNullOrEmpty(XPath.AuthorXPath) && !string.IsNullOrEmpty(ele.Author)) AuthorCount++;
            }

            if (TitleCount < Content.Count * 0.9) return false;
            if (!string.IsNullOrEmpty(XPath.DateXPath) && DateCount < Content.Count * 0.9) return false;
            if (!string.IsNullOrEmpty(XPath.ViewXPath) && ViewCount < Content.Count * 0.4) return false;
            if (!string.IsNullOrEmpty(XPath.ReplyXPath) && ReplyCount < Content.Count * 0.1) return false;
            if (!string.IsNullOrEmpty(XPath.MediaNameXPath) && MediaCount < Content.Count * 0.9) return false;
            if (!string.IsNullOrEmpty(XPath.AuthorXPath) && AuthorCount < Content.Count * 0.9) return false;

            return true;
        }

        /// <summary>
        /// 检查两个Url是否相近，即是否只存在最多两个层级的不同。这样可以增加精准性
        /// </summary>
        /// <param name="url1"></param>
        /// <param name="url2"></param>
        /// <returns></returns>
        public bool IsUrlClose(string url1, string url2)
        {
            //首先，对全空、一个为空、层级数不同的url做了规定。同时把逗号换成了斜杠。
            if (string.IsNullOrWhiteSpace(url1) && !string.IsNullOrWhiteSpace(url2)) return false;
            if (!string.IsNullOrWhiteSpace(url1) && string.IsNullOrWhiteSpace(url2)) return false;
            if (string.IsNullOrWhiteSpace(url1) && string.IsNullOrWhiteSpace(url2)) return true;
            url1 = url1.Replace(",", "/"); url2 = url2.Replace(",", "/");
            if (Regex.Match(url1, @"/").Length != Regex.Match(url2, @"/").Length) return false;

            //这里检查是否有两处不同
            int wrongpart = 0;
            url1 += "/"; url2 += "/";
            while (url1.Contains("/"))
            {
                string temp = url1.Substring(0, url1.IndexOf("/"));
                if (temp != url2.Substring(0, url2.IndexOf("/")))
                    wrongpart++;
                if (wrongpart > 2) return false;
                if (url1.IndexOf("/") > url1.Length - 1)
                {
                    url1 = url1.Substring(url1.IndexOf("/") + 1);
                    url2 = url2.Substring(url2.IndexOf("/") + 1);
                }
                else
                    url1 = string.Empty; url2 = string.Empty;
            }
            return true;
        }


        #region 一些常用函数

        /// <summary>
        /// 给title进行打分的函数
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public double ScoreforListTitle(IEnumerable<HtmlNode> Nodes, ref double ScoreSVM, ref double AvgLen)
        {
            if (Nodes == null || Nodes.Count() == 0) return 0;
            double[] score = new double[2];
            Feature featureofthispattern = GetFeature_ListPage(Nodes, Nodes.Count(), Stencil_ListTitle);
            SVM.Node[] nodes = Getnode(featureofthispattern, Stencil_ListTitle);
            int i = 0;
            if (model_title.ClassLabels[i] == 1) i = 1;
            score = Prediction.PredictProbability(model_title, nodes);
            double s = score[i] * 1000;
            double len = featureofthispattern.FigureFeatures["AvgTextLen"];
            double linadd = 0;
            if (len != 0)
                linadd = Math.Abs(Math.Log(len) - Math.Log(25)) * 200;
            if (Math.Abs(len - 25) > 5) linadd -= (len - 25) * 20;
            double countadd = Math.Abs(Math.Log(Nodes.Count()) - Math.Log(50)) * 200;

            s = s + 500 - linadd - countadd;
            ScoreSVM = score[i] * 1000;
            AvgLen = len;
            return s;// core[i] * 1000;
        }
        public double ScoreforListTitle(IEnumerable<HtmlNode> Nodes)
        {
            if (Nodes == null || Nodes.Count() == 0) return 0;
            double[] score = new double[2];
            Feature featureofthispattern = GetFeature_ListPage(Nodes, Nodes.Count(), Stencil_ListTitle);
            SVM.Node[] nodes = Getnode(featureofthispattern, Stencil_ListTitle);
            int i = 0;
            if (model_title.ClassLabels[i] == 1) i = 1;
            score = Prediction.PredictProbability(model_title, nodes);
            double s = score[i] * 1000;
            double len = featureofthispattern.FigureFeatures["AvgTextLen"];
            double linadd = Math.Abs(Math.Log(len) - Math.Log(25)) * 200;
            if (Math.Abs(len - 25) > 5) linadd -= (len - 25) * 20;
            double countadd = Math.Abs(Math.Log(Nodes.Count()) - Math.Log(50)) * 200;

            s = s + 500 - linadd - countadd;
            return s;// score[i] * 1000;
        }
        /// <summary>
        /// 给字段进行打分的函数
        /// </summary>
        /// <param name="Scores"></param>
        /// <param name="Nodes"></param>
        /// <param name="Pattern"></param>
        public Dictionary<PatternType, double> ScoreforRel(List<HtmlNode> Nodes, int ItemCount, ref double Number, SoftStrategy Strategy)
        {
            Dictionary<PatternType, double> Scores = new Dictionary<PatternType, double>();

            Feature TempStencil = Stencil_ListRel;
            TempStencil.BoolFeatures["twonuminregularshape"] = 1;

            Feature featureofthispattern = GetFeature_ListPage(Nodes, ItemCount, TempStencil);
            Number = featureofthispattern.FigureFeatures["AvgNumber"];

            //一些简单规则，虽然不符合科学精神但是实用为上。这东西还不知道能用几年呢，编程语言更新换代辣么快，说不定三年后就用不着了
            //简化规则一：15-11-25：加入对平均长度的审查。如果等于1甚至小于1就无视，因为有可能是一些[或者·之类的内容，网站摆着它纯粹为了美观
            if (featureofthispattern.FigureFeatures["AvgTextLen"] <= 1 && featureofthispattern.FigureFeatures["DigitCountRate"] < 1)
            {
                for (int i = 0; i < model_rel.ClassLabels.Count(); i++)
                    Scores.Add((PatternType)Enum.ToObject(typeof(PatternType), model_rel.ClassLabels[i]), 0);
                return Scores;
            }
            //结束

            SVM.Node[] nodes = Getnode(featureofthispattern, Stencil_ListRel);
            double[] score = SVM.Prediction.PredictProbability(model_rel, nodes);
            for (int i = 0; i < model_rel.ClassLabels.Count(); i++)
            {
                PatternType pt = (PatternType)Enum.ToObject(typeof(PatternType), model_rel.ClassLabels[i]);
                if (score[i] < Threshold.Relthreshold) score[i] = 0;
                Scores.Add(pt, score[i] * 1000);
            }
            //简化规则二：15-11-27:如果数字匹配大于二则无视它的reply\view得分
            if (featureofthispattern.FigureFeatures["DigitCountRate"] < 1)
            {
                Scores[PatternType.ViewnReply] = 0;
            }
            //简化规则三：15-11-27：如果日期匹配比率小于1则无视其日期匹配得分
            if (featureofthispattern.FigureFeatures["DateCountRate"] < 9)
                Scores[PatternType.Date] = 0;
            //结束
            //简化规则四：15-12-02：加入对twoinregularshape的审查，如果合格则强行认为它是满分的view&reply路径
            if (featureofthispattern.BoolFeatures["twonuminregularshape"] == 0 && featureofthispattern.FigureFeatures["RateTitleDigits"] > 9)
                Scores[PatternType.ViewnReply] = 1000;
            //结束

            return Scores;
        }

        /// <summary>
        /// 保存特征值的类，包括一个以单位数字设置值的初始化方法，用于简化模版特征的初始化步骤
        /// </summary>
        public class Feature
        {
            //存储的是某些连续数字型特征的值，可能会进行归一化处理
            public Dictionary<string, double> FigureFeatures = new Dictionary<string, double> { { "AvgTextLen", 0 }, { "TotleTextLen" ,0},
                { "AvgDateDistance", 0 }, { "AvgNumber", 0 }, { "RateTitleDigits", 0 }, { "ItemCount", 0 }, { "DateParseCount", 0 } ,
                { "DateCountRate" , 0 }, { "DigitCountRate" , 0 }, { "AllTextLen",0} };

            //存储的是直接搜索统计的字符的频次
            public Dictionary<string, int> CharRecordf = new Dictionary<string, int> { { "分钟", 0 }, { "前", 0 }, { "小时", 0 },
                { "秒", 0 }, { "半", 0 }, { "天", 0 }, { "昨", 0 }, { "年", 0 }, { "月", 0 }, { "日", 0 }, { "期间", 0 }, { "发表", 0 },
                { "于", 0 }, { "布稿", 0 }, { "出", 0 }, { ":", 0 }, { "：", 0 }, { "/", 0 }, { "-", 0 }, { ".", 0 }, { "更新", 0 },
                { "上线", 0 }, { "星期", 0 }, { "周", 0 }, { "评论", 0 }, { "回复", 0 }, { "点击", 0 }, { "查看", 0 }, { "浏览", 0 },
                { "阅读", 0 }, { "参与", 0 }, { "人次", 0 }, { "已有", 0 }, { "互动", 0 }, { "围观", 0 }, { "超过", 0 }, { "数个", 0 },
                { "第", 0 }, { "网友", 0 }, { "条", 0 }, { "被", 0 }, { "(", 0 }, { ")", 0 }, { "（", 0 }, { "）", 0 }, { "[", 0 },
                { "]", 0 }, { "【", 0 }, { "】", 0 }, { "更多", 0 }, { "详细", 0 }, { "详情", 0 }, { "转发", 0 }, { "赞", 0 }, { "more", 0 },
                { "by", 0 }, { "from", 0 }, { "editor", 0 }, { "view", 0 }, { "read", 0 }, { "detail", 0 }, { "info", 0 }, { "news", 0 },
                { "reply", 0 }, { "comment", 0 }, { "new", 0 }, { "New", 0 }, { "分类", 0 }, { "主题", 0 }, { "关键词", 0 },
                { "标题", 0 }, { "预览", 0 } };

            //存储的是要用regex统计的字符的频次，因为判断方法不一样所以设置另一个字典
            public Dictionary<string, int> CharRecords = new Dictionary<string, int> { { "数字", 0 }, { "报台网刊", 0 },
                { "李王张刘陈杨赵黄周吴", 0 }, { "作者", 0 }, { "选稿", 0 }, { "编辑", 0 }, { "记者", 0 } };

            //存储的是ID与ClassName的统计信息，不过为了便于计算可能会做归一化调整
            public Dictionary<string, int> IdClassnameRecord = new Dictionary<string, int> { { "date", 0 }, { "time", 0 }, { "view", 0 },
                { "count", 0 }, { "num", 0 }, { "click", 0 }, { "rep", 0 }, { "media", 0 }, { "soure", 0 }, { "from", 0 }, { "author", 0 },
                { "uid", 0 }, { "user", 0 }, { "nick", 0 }, { "by", 0 }, { "content", 0 }, { "main", 0 }, { "right", 0 }, { "side", 0 },
                { "bar", 0 }, { "menu", 0 }, { "nav", 0 }, { "top10", 0 }, { "top15", 0 }, { "top20", 0 }, { "most", 0 }, { "phb", 0 },
                { "pic", 0 }, { "photo", 0 }, { "hot", 0 },{"left",0} , {"list",0},{"remark",0},{"message",0},{"leftlist",0},{"topic",0}};

            //这里保存的是某些不方便用数字统计的结果，在此用数字存储。0表示是，1表示否，2表示未知
            public Dictionary<string, int> BoolFeatures = new Dictionary<string, int> { { "twonuminregularshape", 0 } };

            /// <summary>
            /// 一种初始化方法，可以直接确定初始为什么数字，很好用
            /// </summary>
            /// <param name="def"></param>
            public Feature(int def)
            {
                Dictionary<string, double> temp = new Dictionary<string, double>();
                foreach (string key in this.FigureFeatures.Keys)
                    temp.Add(key, def);
                this.FigureFeatures = temp;

                Dictionary<string, int> tep = new Dictionary<string, int>();
                foreach (string key in this.CharRecordf.Keys)
                    tep.Add(key, def);
                this.CharRecordf = tep;

                tep = new Dictionary<string, int>();
                foreach (string key in this.CharRecords.Keys)
                    tep.Add(key, def);
                this.CharRecords = tep;

                tep = new Dictionary<string, int>();
                foreach (string key in this.IdClassnameRecord.Keys)
                    tep.Add(key, def);
                this.IdClassnameRecord = tep;

                tep = new Dictionary<string, int>();
                foreach (string key in this.BoolFeatures.Keys)
                    tep.Add(key, def);
                this.BoolFeatures = tep;
            }
        }

        #region 计算特征值部分


        /// <summary>
        /// 计算特征值的主体部分。为了提升速度决定把stencilfeature的指引作用也放到这里来.
        /// Nodes不得为空，ItemCount也不得为0。
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="ItemCount"></param>
        /// <param name="stencilfeature"></param>
        /// <returns></returns>
        public Feature GetFeature_ListPage(IEnumerable<HtmlNode> Nodes, int ItemCount, Feature stencilfeature)
        {
            if (Nodes == null || ItemCount == 0) return null;
            Feature feature = new Feature(0);
            feature.FigureFeatures["ItemCount"] = Nodes.Count();
            int[] TextLen = new int[Nodes.Count()];
            int[] DigiLen = new int[Nodes.Count()];
            double[] Diff = new double[Nodes.Count()];
            int i = 0;
            int[] intone = new int[Nodes.Count()];
            bool havetwonums = true;
            int DigitCount = 0;
            foreach (HtmlNode node in Nodes)
            {
                string Text = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(node));
                TextLen[i] = Text.Length;
                DigiLen[i] = TextCleaner.CountDigitChars(Text);
                if (Nodes.Count() >= ItemCount * 0.8 && Nodes.Count() <= ItemCount * 1.2 && (stencilfeature.FigureFeatures["AvgDateDistance"] == 1 || stencilfeature.FigureFeatures["DateParseCount"] == 1 || stencilfeature.FigureFeatures["DateCountRate"] == 1) && Text.Length > 1)
                {
                    if (Text.Contains("秒前") && Text.Length < 5)
                        Text = "昨日";
                    DateTime? Val = DateTimeParser.Parser(Text);
                    if (Val != null)
                    {
                        double diff = Math.Abs((DateTime.Now - (DateTime)Val).TotalDays);
                        if (diff < 4096 && Text.Length < Threshold.MaxDateLength)
                        {
                            Diff[i] = diff;
                            feature.FigureFeatures["DateParseCount"] += 1;
                        }
                    }
                }
                string Textfordigit = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(node), true, true, true, false, true, false);
                MatchCollection digiText = Regex.Matches(Textfordigit, @"\d{1,9}");
                switch (digiText.Count)
                {
                    case 1:
                        DigitCount++;
                        intone[i] = int.Parse(digiText[0].Captures[0].Value);
                        havetwonums = false;
                        break;
                    case 2:
                        DigitCount++;
                        intone[i] = int.Parse(digiText[0].Captures[0].Value) - int.Parse(digiText[1].Captures[0].Value);
                        break;
                    default:
                        havetwonums = false;
                        break;
                }

                feature = CheckforChars(feature, Text, stencilfeature, true);
                i += 1;
            }

            //ID和CLASS NAME的识别
            feature = CheckforIdorClassName(feature, Nodes, stencilfeature, true);
            if (stencilfeature.FigureFeatures["DigitCountRate"] == 1)
                feature.FigureFeatures["DigitCountRate"] = 10 * DigitCount / ItemCount;
            if (stencilfeature.FigureFeatures["AvgTextLen"] == 1)
                feature.FigureFeatures["AvgTextLen"] = TextLen.Average();
            if (stencilfeature.FigureFeatures["AllTextLen"] == 1)
                feature.FigureFeatures["AllTextLen"] = TextLen.Sum();
            if (stencilfeature.FigureFeatures["AvgDateDistance"] == 1)
                feature.FigureFeatures["AvgDateDistance"] = Diff.Average();
            intone = intone.Where(inton => inton > 0).ToArray();
            if (stencilfeature.FigureFeatures["AvgNumber"] == 1)
                feature.FigureFeatures["AvgNumber"] = intone.Count() == 0 ? 0 : Math.Log(intone.Where(inton => inton > 0).Average(), 2);
            if (stencilfeature.FigureFeatures["DateCountRate"] == 1 && feature.FigureFeatures["ItemCount"] != 0)
                feature.FigureFeatures["DateCountRate"] = 10 * feature.FigureFeatures["DateParseCount"] / ItemCount;
            if (stencilfeature.FigureFeatures["RateTitleDigits"] == 1)
                feature.FigureFeatures["RateTitleDigits"] = TextLen.Sum() + DigiLen.Sum() == 0 ? 0 : 10 * (double)(DigiLen.Sum()) / (double)(TextLen.Sum() + DigiLen.Sum());
            if (stencilfeature.BoolFeatures["twonuminregularshape"] == 1)
                feature.BoolFeatures["twonuminregularshape"] = (havetwonums && (intone.Where(k => k > 0).Count() == 0 || intone.Where(k => k < 0).Count() == 0)) ? 0 : 1;

            //曾经考虑过把数字特征的方差也统计进来，或者把标准差与平均值之比放进来。有用吗
            return feature;
        }


        public Feature GetFeature_ItemPage(IEnumerable<HtmlNode> Nodes, int ItemCount, Feature stencilfeature)
        {
            if (Nodes == null || ItemCount == 0) return null;
            Feature feature = new Feature(0);
            feature.FigureFeatures["ItemCount"] = Nodes.Count();
            int[] TextLen = new int[Nodes.Count()];
            int[] DigiLen = new int[Nodes.Count()];
            double[] Diff = new double[Nodes.Count()];
            int i = 0;
            int[] intone = new int[Nodes.Count()];
            bool havetwonums = true;
            int DigitCount = 0;
            foreach (HtmlNode node in Nodes)
            {
                string Text = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(node));
                TextLen[i] = Text.Length;
                DigiLen[i] = TextCleaner.CountDigitChars(Text);
                if (Nodes.Count() >= ItemCount * 0.8 && Nodes.Count() <= ItemCount * 1.2 && (stencilfeature.FigureFeatures["AvgDateDistance"] == 1 || stencilfeature.FigureFeatures["DateParseCount"] == 1 || stencilfeature.FigureFeatures["DateCountRate"] == 1) && Text.Length > 1)
                {
                    if (Text.Contains("秒前") && Text.Length < 5)
                        Text = "昨日";
                    DateTime? Val = DateTimeParser.Parser(Text);
                    if (Val != null)
                    {
                        double diff = Math.Abs((DateTime.Now - (DateTime)Val).TotalDays);
                        if (diff < 4096 && Text.Length < Threshold.MaxDateLength)
                        {
                            Diff[i] = diff;
                            feature.FigureFeatures["DateParseCount"] += 1;
                        }
                    }
                }
                string Textfordigit = TextCleaner.FullClean(XPathUtility.InnerTextNonDescendants(node), true, true, true, false, true, false);
                MatchCollection digiText = Regex.Matches(Textfordigit, @"\d{1,9}");
                switch (digiText.Count)
                {
                    case 1:
                        DigitCount++;
                        intone[i] = int.Parse(digiText[0].Captures[0].Value);
                        havetwonums = false;
                        break;
                    case 2:
                        DigitCount++;
                        intone[i] = int.Parse(digiText[0].Captures[0].Value) - int.Parse(digiText[1].Captures[0].Value);
                        break;
                    default:
                        havetwonums = false;
                        break;
                }
                if (TextLen.Sum() < 50)
                    feature = CheckforChars(feature, Text, stencilfeature, false);
                i += 1;
            }

            //ID和CLASS NAME的识别
            feature = CheckforIdorClassName(feature, Nodes, stencilfeature, false);
            if (stencilfeature.FigureFeatures["DigitCountRate"] == 1)
                feature.FigureFeatures["DigitCountRate"] = 10 * DigitCount / ItemCount;
            if (stencilfeature.FigureFeatures["AvgTextLen"] == 1)
                feature.FigureFeatures["AvgTextLen"] = TextLen.Average();
            if (stencilfeature.FigureFeatures["AllTextLen"] == 1)
                feature.FigureFeatures["AllTextLen"] = TextLen.Sum();
            if (stencilfeature.FigureFeatures["AvgDateDistance"] == 1)
                feature.FigureFeatures["AvgDateDistance"] = Diff.Average();
            intone = intone.Where(inton => inton > 0).ToArray();
            if (stencilfeature.FigureFeatures["AvgNumber"] == 1)
                feature.FigureFeatures["AvgNumber"] = intone.Count() == 0 ? 0 : Math.Log(intone.Where(inton => inton > 0).Average(), 2);
            if (stencilfeature.FigureFeatures["DateCountRate"] == 1 && feature.FigureFeatures["ItemCount"] != 0)
                feature.FigureFeatures["DateCountRate"] = 10 * feature.FigureFeatures["DateParseCount"] / ItemCount;
            if (stencilfeature.FigureFeatures["RateTitleDigits"] == 1)
                feature.FigureFeatures["RateTitleDigits"] = TextLen.Sum() + DigiLen.Sum() == 0 ? 0 : 10 * (double)(DigiLen.Sum()) / (double)(TextLen.Sum() + DigiLen.Sum());
            if (stencilfeature.BoolFeatures["twonuminregularshape"] == 1)
                feature.BoolFeatures["twonuminregularshape"] = (havetwonums && (intone.Where(k => k > 0).Count() == 0 || intone.Where(k => k < 0).Count() == 0)) ? 0 : 1;

            //曾经考虑过把数字特征的方差也统计进来，或者把标准差与平均值之比放进来。有用吗
            return feature;
        }

        /// <summary>
        /// 进行匹配判断字符频次的函数，内部使用
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="Text"></param>
        /// <returns></returns>
        internal static Feature CheckforChars(Feature feature, string Text, Feature stencilfeature, bool forListPage)
        {
            //CharRecordf部分
            foreach (string key in stencilfeature.CharRecordf.Keys)
                if (stencilfeature.CharRecordf[key] == 1 && Text.Contains(key)) feature.CharRecordf[key] += 1;

            //CharRecords部分
            if (stencilfeature.CharRecords["数字"] == 1) feature.CharRecords["数字"] += Regex.Matches(Text, @"\d{1,9}").Count;
            if (stencilfeature.CharRecords["报台网刊"] == 1) feature.CharRecords["报台网刊"] += Regex.Matches(Text, @"[报台网刊]").Count;
            if (stencilfeature.CharRecords["李王张刘陈杨赵黄周吴"] == 1) feature.CharRecords["李王张刘陈杨赵黄周吴"] += Regex.Matches(Text, @"[李王张刘陈杨赵黄周吴]").Count;
            if (stencilfeature.CharRecords["作者"] == 1) feature.CharRecords["作者"] += Regex.Matches(Text, @"作\s*者").Count;
            if (stencilfeature.CharRecords["选稿"] == 1) feature.CharRecords["选稿"] += Regex.Matches(Text, @"选\s*稿").Count;
            if (stencilfeature.CharRecords["编辑"] == 1) feature.CharRecords["编辑"] += Regex.Matches(Text, @"编\s*辑").Count;
            if (stencilfeature.CharRecords["记者"] == 1) feature.CharRecords["记者"] += Regex.Matches(Text, @"记\s*者").Count;

            return feature;
        }

        /// <summary>
        /// 进行匹配判断Id或ClassName的函数，内部使用
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        internal static Feature CheckforIdorClassName(Feature feature, IEnumerable<HtmlNode> Nodes, Feature stencilfeature, bool forListPage)
        {
            int nodestested = 0;
            Dictionary<string, int> temp = new Dictionary<string, int>();
            foreach (HtmlNode node in Nodes)
            {
                HtmlNode Node = node;
                if (XPathUtility.isTopNode(Node)) break;

                temp = new Dictionary<string, int>();
                foreach (string key in stencilfeature.IdClassnameRecord.Keys)
                    if (stencilfeature.IdClassnameRecord[key] == 1) temp.Add(key, 0);

                for (int i = 1; i < 16; i++)
                {
                    if (XPathUtility.isTopNode(Node.ParentNode)) break;
                    foreach (string key in stencilfeature.IdClassnameRecord.Keys)
                    {
                        if (stencilfeature.IdClassnameRecord[key] == 1 && temp[key] == 0 && XPathUtility.IDClassContain(Node, key)) temp[key] = i;
                    }
                    Node = Node.ParentNode;
                }
                foreach (string key in temp.Keys)
                {
                    if (nodestested != 0 && feature.IdClassnameRecord[key] != temp[key])
                        feature.IdClassnameRecord[key] = 0;
                    feature.IdClassnameRecord[key] = temp[key];
                }
                nodestested++;
            }
            foreach (string key in temp.Keys)
                if (feature.IdClassnameRecord[key] == 0)
                    feature.IdClassnameRecord[key] = 15;
            return feature;
        }


        /// <summary>
        /// 获得Node用来进行评分的。stencilfeature用来判断哪些特征被选择用来进行
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private SVM.Node[] Getnode(Feature feature, Feature stencilfeature)
        {
            int count = stencilfeature.BoolFeatures.Where(b => b.Value == 1).Count() + stencilfeature.CharRecordf.Where(b => b.Value == 1).Count() + stencilfeature.CharRecords.Where(b => b.Value == 1).Count() + stencilfeature.FigureFeatures.Where(b => b.Value == 1).Count() + stencilfeature.IdClassnameRecord.Where(b => b.Value == 1).Count() + 1;

            int i = 1;
            SVM.Node[] nodes = new SVM.Node[count];
            nodes[0] = new SVM.Node();
            nodes[0].Index = 0;
            nodes[0].Value = 0;
            if (feature.FigureFeatures.Count > 0)
                foreach (string key in feature.FigureFeatures.Keys)
                    if (stencilfeature.FigureFeatures[key] == 1)
                    {
                        nodes[i] = new SVM.Node();
                        nodes[i].Index = i;
                        nodes[i].Value = feature.FigureFeatures[key];
                        i++;
                    }

            if (feature.CharRecordf.Count > 0)
                foreach (string key in feature.CharRecordf.Keys)
                    if (stencilfeature.CharRecordf[key] == 1)
                    {
                        nodes[i] = new SVM.Node();
                        nodes[i].Index = i;
                        nodes[i].Value = feature.CharRecordf[key];
                        i++;
                    }

            if (feature.CharRecords.Count > 0)
                foreach (string key in feature.CharRecords.Keys)
                    if (stencilfeature.CharRecords[key] == 1)
                    {
                        nodes[i] = new SVM.Node();
                        nodes[i].Index = i;
                        nodes[i].Value = feature.CharRecords[key];
                        i++;
                    }

            if (feature.IdClassnameRecord.Count > 0)
                foreach (string key in feature.IdClassnameRecord.Keys)
                    if (stencilfeature.IdClassnameRecord[key] == 1)
                    {
                        nodes[i] = new SVM.Node();
                        nodes[i].Index = i;
                        nodes[i].Value = feature.IdClassnameRecord[key];
                        i++;
                    }

            if (feature.BoolFeatures.Count > 0)
                foreach (string key in feature.BoolFeatures.Keys)
                    if (stencilfeature.BoolFeatures[key] == 1)
                    {
                        nodes[i] = new SVM.Node();
                        nodes[i].Index = i;
                        nodes[i].Value = feature.BoolFeatures[key];
                        i++;
                    }

            return nodes;
        }

        #endregion 计算特征值部分

        #endregion 一些常用函数

    }
}
