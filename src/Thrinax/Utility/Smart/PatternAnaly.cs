using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Thrinax.Models;

namespace Thrinax.Utility.Smart
{
    public class PatternAnaly
    {
        public HtmlNode CurrentNode { get; set; }
        /// <summary>
        /// 标题node
        /// </summary>
        public HtmlNode BasicNode { get; set; }
        /// <summary>
        /// 一级的basenode
        /// </summary>
        public HtmlNode ItemBaseNode { get; set; }
        /// <summary>
        /// LeastAncestorNode
        /// </summary>
        public HtmlNode LeastAncestorNode { get; set; }
        /// <summary>
        /// basenode级别的xpath
        /// </summary>
        public string BasicXPath { get; set; }
        /// <summary>
        /// ItemBaseNode级别的xpath
        /// </summary>
        public string ItemBaseXPath { get; set; }
        /// <summary>
        /// LeastAncestorNode级别的XPath
        /// </summary>
        public string LeastAncestorXPath { get; set; }
        /// <summary>
        /// 是否还有用可以留下
        /// </summary>
        public bool useless { get; set; }
        /// <summary>
        /// 所包含的Node清单
        /// </summary>
        public Dictionary<string, int> ContainList { get; set; }
        ///// <summary>
        ///// ItemBaseNode向上跨越的层级数
        ///// </summary>
        //public int skipfloor { get; set; }
        /// <summary>
        /// 包含的anode个数
        /// </summary>
        public int ItemCount { get; set; }
        /// <summary>
        /// 平均text长度
        /// </summary>
        public double AverageTextLength { get; set; }
        /// <summary>
        /// LevelIgnored，总想找办法把这个属性删掉
        /// </summary>
        public int LVI { get; set; }

        public HtmlPattern WritePattern()
        {
            return new HtmlPattern
            {
                LCAXPath = this.LeastAncestorXPath,
                ItemBaseXPath = this.ItemBaseXPath,
                ItemCount = this.ItemCount,
                AverageTextLength = this.AverageTextLength,
                LeastCommonAncestor = this.LeastAncestorNode,
                LevelIgnored = this.LVI,
                XPath = this.BasicXPath
            };
        }

        public override string ToString()
        {
            return string.Format("usls: {0} c: {1} itbxph: {2} xph: {3}", useless, ItemCount, ItemBaseXPath, BasicXPath);
        }
    }
}
