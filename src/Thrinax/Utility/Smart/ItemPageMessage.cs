using System;
using HtmlAgilityPack;
using Thrinax.Utility;

namespace Thrinax
{
    public class ItemPageMessage
    {
        public string Html { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public HtmlNode RootNode { get; set; }
        public int NodeCount { get; set; }

        public ItemPageMessage(string url, string html)
        {
            this.Url = url;
            this.Html = html;
            this.RootNode = HtmlUtility.getSafeHtmlRootNode(html, true, true);
        }
        public ItemPageMessage(ItemPageMessage itpm)
        {
            this.Html = itpm.Html;
            this.Title = itpm.Title;
            this.Url = itpm.Url;
            this.RootNode = null;

            this.NodeCount = itpm.NodeCount;
            this.RootNode = HtmlUtility.getSafeHtmlRootNode(itpm.Html, true, true);
        }
        public ItemPageMessage()
        { }
        public override string ToString()
        {
            return string.Format("NodeCount: {0} url: {1}", NodeCount, Url);
        }

    }
}
