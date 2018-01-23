using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Thrinax.Utility;

namespace Thrinax.Utility.Smart
{
    public class ItemPatternAnaly
    {
        /// <summary>
        /// 路径名称，这是最重要的特点
        /// </summary>
        public string XPath { get; set; }

        //以下几条是为了整理方便而产生的
        /// <summary>
        /// 合并时所用的前提字段
        /// </summary>
        public string PreXPath { get; set; }

        /// <summary>
        /// 合并时所用的后备字段
        /// </summary>
        public string SubXPath { get; set; }

        /// <summary>
        /// 简化后的路径名称
        /// </summary>
        public string ShortXPath { get; set; }


        //以下是为了评估优劣

        /// <summary>
        /// 当前的路径所取到的点的一个代表
        /// </summary>
        public HtmlNode SampleNode { get; set; }

        /// <summary>
        ///最初的一个点 
        /// </summary>
        public HtmlNode CNode { get; set; }

        /// <summary>
        /// 所包含的点的数量统计
        /// </summary>
        public int NodesCount { get; set; }

        /// <summary>
        /// 所包含的文字长度统计
        /// </summary>
        public int TextLenCount { get; set; }

        /// <summary>
        /// 包含的文字节点统计 
        /// </summary>
        public int TextNodesCount { get; set; }

        /// <summary>
        /// 正文内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 合并到该层带来的文字变化
        /// </summary>
        public double GainTextRate { get; set; }

        //以下是为了方便生成
        /// <summary>
        /// 提取次数，超过三次则放过
        /// </summary>
        public int usedtime { get; set; }

        /// <summary>
        /// 是否还有留作提取的必要
        /// </summary>
        public bool useless { get; set; }

        /// <summary>
        /// 所包含下级节点的种类和个数
        /// </summary>
        public Dictionary<string, int> ContainList { get; set; }

        /// <summary>
        /// 所包含的标签种类和内容
        /// </summary>
        public Dictionary<string, HashSet<string>> Attributelist { get; set; }


        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(this.ShortXPath))
                return string.Format("c: {0} tl: {1} tc: {2} XPth: {3} nodetype: {4}", NodesCount, TextLenCount, GainTextRate, XPath, SampleNode.NodeType);
            else
                return string.Format("c: {0} tl: {1} tc: {2} XPth: {3} nodetype: {4}", NodesCount, TextLenCount, GainTextRate, ShortXPath, SampleNode.NodeType);
        }

        public ItemPatternAnaly()
        { }

        /// <summary>
        /// 虽然理论上确实可以把所有的步骤都放到这里来，但是暂且别这样做，到时候要改动的部分很大
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="ContainList"></param>
        /// <param name="basic"></param>
        public ItemPatternAnaly(HtmlNode Node, Dictionary<string, int> ContainList, Dictionary<string, HashSet<string>> Attributelist)
        {
            this.SampleNode = Node;

            this.XPath = Node.XPath;
            this.PreXPath = Node.XPath;
            this.SubXPath = string.Empty;
            this.ContainList = ContainList;
            this.usedtime = 0;
            this.GainTextRate = 0;
            //this.Level = Regex.Matches(XPath, @"/").Count;
            //如果是从基点生成一个，则balabala。实际上可以把这个函数做城重载。

            //this.SampleNode = Node;
            //this.BasicNode = Node;
            this.NodesCount = CountNodecontainsText(Node);
            this.TextLenCount = 0;//CountTextofNode(Node);
            this.TextNodesCount = 1;
            this.CNode = Node;
            this.Attributelist = Attributelist;

            //最后再照顾一下文字内容的格式问题
            if (Node.Name.Contains("#"))
            {
                Node.Name = Node.Name.Substring(1);
                Node.Name = Node.Name + "()";
                this.SampleNode = Node;
                this.CNode = Node;
                this.XPath = Node.XPath.Replace("#text", "text()");
                this.XPath = this.XPath.Replace("#content", "content()");
                this.PreXPath = this.XPath;
            }
        }


        public ItemPatternAnaly(HtmlNode SampleNode, HtmlNode CNode, Dictionary<string, int> ContainList, Dictionary<string, HashSet<string>> Attributelist, int nodescount, int textnodescount)
        {
            this.SampleNode = SampleNode;
            //this.XPath = itmp.XPath;
            this.CNode = CNode;
            //this.SubXPath = string.Empty;
            this.ContainList = ContainList;
            this.usedtime = 0;
            this.GainTextRate = 0;

            this.Attributelist = Attributelist;
            //this.PreXPath = CNode.XPath;
            //this.SubXPath = ;
            //this.Level = Regex.Matches(XPath, @"/").Count;
            //如果是从基点生成一个，则balabala。实际上可以把这个函数做城重载。

            this.NodesCount = nodescount;
            this.TextLenCount = 0;
            this.TextNodesCount = textnodescount;
        }



        internal int CountTextofNode(HtmlNode Node)
        {
            if (Node.XPath.Contains("/script") || Node.XPath.Contains("/style") || Node.XPath.Contains("/meta")) return 0;
            if (Node.Name.Contains("#") && !Node.Attributes.Contains("cutting")) return HTMLCleaner.GetCleanInnerText(Node).Length;
            if (Node.ChildNodes.Count == 0) return 0;
            int count = 0;
            foreach (HtmlNode node in Node.ChildNodes)
            {
                if (node.Attributes.Contains("cutting")) continue;
                if (node.Name.Contains("#"))
                    count += HTMLCleaner.GetCleanInnerText(node).Length;
                else count += CountTextofNode(node);
            }
            return count;
        }
        internal int CountNodecontainsText(HtmlNode Node)
        {
            if (Node.XPath.Contains("/script") || Node.XPath.Contains("/style") || Node.XPath.Contains("/meta")) return 0;
            if (Node.Name.Contains("#") && !Node.Attributes.Contains("cutting")) return 1;
            if (Node.ChildNodes.Count == 0) return 0;
            int count = 0;
            foreach (HtmlNode node in Node.ChildNodes)
            {
                if (node.Attributes.Contains("cutting")) continue;
                if (node.Name.Contains("#"))
                    count += 1;
                else count += CountNodecontainsText(node);
            }
            return count;
        }
    }

}
