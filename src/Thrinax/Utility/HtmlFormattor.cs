using HtmlAgilityPack;
using System;
using System.Text.RegularExpressions;
using System.Configuration;

namespace Thrinax.Utility
{
    /// <summary>
    /// 对Html进行格式重整
    /// </summary>
    public class HtmlFormattor
    {
        #region 配置项

        /// <summary>
        /// 清洗时去掉的Html标签(其中内容也去掉)
        /// </summary>
        public static string HtmlRemoveTags_RemoveContent = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveTags_RemoveContent"] ?? @"script obj object param map input";

        /// <summary>
        /// 清洗时去掉的Html标签(其中内容保留)
        /// </summary>
        public static string HtmlRemoveTags_RemainContent = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveTags_RemainContent"] ?? @"span font param pre a";

        /// <summary>
        /// 清洗时去掉的Html标签中的属性
        /// </summary>
        public static string HtmlRemoveProperty = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveProperty"] ?? @"style class id align";

        /// <summary>
        /// 图片的alt属性文字
        /// </summary>
        public static string HtmlImgAltText = ConfigurationManager.AppSettings["Thrinax.HtmlImgAltText"] ?? @"Thrinax";

        /// <summary>
        /// 如果内部内容为空则去掉
        /// </summary>
        public static string HtmlRemoveNullTags = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveNullTags"] ?? @"div span table center iframe input";

        #endregion 配置项

        /// <summary>
        /// 对Html进行格式重整（以便发布）
        /// </summary>
        /// <remarks>整理内容：
        /// 0.去除不允许的html标签
        /// 1.P元素整理：删除空P，前后空格，段首俩空格
        /// 2.img标签整理：src变为绝对路径，alt属性设置
        /// 3.a标签整理：href变为绝对路径
        /// 4.清楚class以及style样式
        /// 5.整理图片样式
        /// 6.整理视频样式
        /// </remarks>
        /// <param name="OriHtml">原始Html片段</param>
        /// <param name="Url">文章的Url</param>
        /// <returns>整理后的Html(如失败则返回原始串)</returns>
        public static string FormatHtml(string OriHtml, string Url)
        {
            if (string.IsNullOrEmpty(OriHtml))
                return OriHtml;

            //将文本替换与HtmlNode的部分分离，减少建立Dom树的次数
            #region 加载Doc对象

            //加载HtmlDocument（内容为Html片段，可能异常）
            HtmlNode oriHtmlNode = HtmlUtility.getSafeHtmlRootNode(OriHtml, true, true);

            if (oriHtmlNode == null || string.IsNullOrWhiteSpace(oriHtmlNode.InnerText))
                return OriHtml;

            oriHtmlNode = oriHtmlNode.SelectSingleNode("//body");

            #endregion 加载Doc对象

            #region P整理
            HtmlNodeCollection PNodes = oriHtmlNode.SelectNodes("//p");
            if (PNodes != null && PNodes.Count > 0)
            {
                foreach (HtmlNode node in PNodes)
                {
                    try
                    {
                        //清理无内容的P
                        if (string.IsNullOrEmpty(TextCleaner.FullClean(node.InnerHtml, false)))
                        {
                            //可能是为了空一行.
                            node.RemoveAll();
                        }
                        else
                        {
                            while (node.InnerHtml.TrimStart().StartsWith("&nbsp;", StringComparison.OrdinalIgnoreCase))
                                node.InnerHtml = node.InnerHtml.TrimStart().Substring(6);

                            while (node.InnerHtml.TrimStart().StartsWith(" ", StringComparison.OrdinalIgnoreCase))
                                node.InnerHtml = node.InnerHtml.TrimStart(' ');
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(string.Format("调整内容中p标签时出错:{0},Url={1},P={2}", ex.Message, Url, node.OuterHtml));
                    }
                }
            }
            #endregion P整理

            #region 清理空内容标签
            //清理空内容的标签
            foreach (string RemoveNullTag in HtmlRemoveNullTags.Split())
            {
                HtmlNodeCollection NullTags = oriHtmlNode.SelectNodes("//" + RemoveNullTag);
                if (NullTags != null && NullTags.Count > 0)
                {
                    foreach (HtmlNode node in NullTags)
                    {
                        try
                        {
                            //清理无内容的P
                            if (string.IsNullOrWhiteSpace(TextCleaner.FullClean(node.InnerHtml, false)))
                            {
                                node.ParentNode.RemoveChild(node);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(string.Format("调整内容中标签时出错:{0},Url={1},P={2}", ex.Message, Url, node.OuterHtml));
                        }
                    }
                }
            }
            #endregion

            #region Img整理

            HtmlNodeCollection ImgNodes = oriHtmlNode.SelectNodes("//img");
            if (ImgNodes != null && ImgNodes.Count > 0)
            {
                foreach (HtmlNode node in ImgNodes)
                {
                    //替换alt标签
                    try
                    {
                        if (node.Attributes["alt"] == null)
                            node.Attributes.Append("alt", HtmlImgAltText);
                        else
                            node.SetAttributeValue("alt", HtmlImgAltText);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(string.Format("替换内容中Img标签的alt属性时出错, Img={0}", node.OuterHtml), ex);
                    }

                    //如果包含real_src，则替换src的值；新浪财经特殊处理逻辑
                    if (node.Attributes["real_src"] != null && !string.IsNullOrEmpty(node.Attributes["real_src"].Value))
                    {
                        if (node.Attributes["src"] == null)
                            node.Attributes.Append("src", node.Attributes["real_src"].Value);
                        else
                            node.SetAttributeValue("src", node.Attributes["real_src"].Value);
                    }
                    else if (node.Attributes["data-src"] != null && !string.IsNullOrEmpty(node.Attributes["data-src"].Value)) // wechat
                    {
                        if (node.Attributes["src"] == null)
                            node.Attributes.Append("src", node.Attributes["data-src"].Value);
                        else
                            node.SetAttributeValue("src", node.Attributes["data-src"].Value);
                    }

                    //src绝对路径
                    if (node.Attributes["src"] == null || string.IsNullOrEmpty(node.Attributes["src"].Value))
                        node.RemoveAll();
                    else
                    {
                        try
                        {
                            if (!node.Attributes["src"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                && !node.Attributes["src"].Value.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                                && !node.Attributes["src"].Value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                                && !node.Attributes["src"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                                && !node.Attributes["src"].Value.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                                node.Attributes["src"].Value = new Uri(new Uri(Url), node.Attributes["src"].Value).AbsoluteUri;

                            //粗暴的认为在非主域名上的Url包含 icon 的都是表情，移除掉
                            if (new Uri(node.Attributes["src"].Value).PathAndQuery.Contains("icon"))
                                node.RemoveAll();
                            else
                            {
                                //去掉图片名为各大社交分享平台的图标 （wechat,weibo,qq,qzone,sina,renren,kaixin,baidu,tieba,fetion,fbook,facebook,twitter,linkedin,sohu）
                                string chatImage = @"(wechat\.|weibo\.|qq\.|qzone\.|sina\.|renren\.|kaixin\.|baidu\.|tieba\.|fetion\.|fbook\.|facebook\.|twitter\.|linkedin\.|sohu\.)";

                                if (Regex.IsMatch(node.Attributes["src"].Value, chatImage, RegexOptions.IgnoreCase))
                                    node.RemoveAll();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(string.Format("FormatHtml替换内容中Img标签的src属性时出错, Url={0} Img={1}\n", Url, node.OuterHtml), ex);
                        }
                    }
                }
            }
            #endregion Img整理

            #region 视频标签处理
            //常见视频网站：需进一步完善
            //优酷网/爱奇艺/土豆网/搜狐视频/迅雷看看/凤凰视频/腾讯视频/新浪视频/56网/CNTV视频/酷6网/暴风影音/乐视网/PPS/风行/PPTV
            //百度视频/糖豆网/芒果TV/激动网/第一视频/爆米花视频/华数TV/爱拍原创/百度影音/熊猫频道/YY直播/播视网/A站/B站 
            //取出已知的几种视频格式，仅保留src与type，allowscriptaccess，allowfullscreen，wmode
            HtmlNodeCollection embedNodes = oriHtmlNode.SelectNodes("//embed");
            if (embedNodes != null && embedNodes.Count > 0)
            {
                foreach (HtmlNode node in embedNodes)
                {
                    try
                    {
                        //保存下src和type的值
                        string tempsrc = "", temptype = "";
                        if (node.Attributes["src"] != null)
                            tempsrc = node.Attributes["src"].Value;
                        if (!Regex.IsMatch(tempsrc, @"\.(avi|rmvb|rm|mkv|mp4|3gp|flv|swf)", RegexOptions.IgnoreCase))
                            continue;

                        if (node.Attributes["type"] != null)
                            temptype = node.Attributes["type"].Value;
                        //清除其它的attribute
                        node.Attributes.RemoveAll();

                        if (!string.IsNullOrEmpty(tempsrc))
                            node.Attributes.Append("src", tempsrc);
                        if (!string.IsNullOrEmpty(temptype))
                            node.Attributes.Append("type", temptype);

                        node.Attributes.Append("width", "always");
                        node.Attributes.Append("allowfullscreen", "true");
                        node.Attributes.Append("wmode", "opaque");
                        //node.Attributes.Append("allowscriptaccess", "always");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(string.Format("替换视频标签属性时出错, film={0}", node.OuterHtml), ex);
                    }
                }
            }

            //针对Iframe的视频地址去除高宽
            HtmlNodeCollection IframeNodes = oriHtmlNode.SelectNodes("//iframe");
            if (IframeNodes != null && IframeNodes.Count > 0)
            {
                foreach (HtmlNode node in IframeNodes)
                {
                    try
                    {
                        //保存下src和type的值
                        string tempsrc = "";
                        if (node.Attributes["src"] != null)
                            tempsrc = node.Attributes["src"].Value;
                        if (!Regex.IsMatch(tempsrc, @"\.(avi|rmvb|rm|mkv|mp4|3gp|flv|swf)", RegexOptions.IgnoreCase))
                            continue;
                        //清除其它的attribute
                        node.Attributes.RemoveAll();

                        if (!string.IsNullOrEmpty(tempsrc))
                            node.Attributes.Append("src", tempsrc);

                        node.Attributes.Append("allowscriptaccess", "100%");
                        node.Attributes.Append("allowfullscreen", "true");
                        node.Attributes.Append("frameborder", "0");
                        //node.Attributes.Append("allowscriptaccess", "always");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(string.Format("替换视频标签属性时出错, film={0}", node.OuterHtml), ex);
                    }
                }
            }

            #endregion

            #region a整理
            HtmlNodeCollection ANodes = oriHtmlNode.SelectNodes("//a[@href]");
            if (ANodes != null && ANodes.Count > 0)
            {
                foreach (HtmlNode node in ANodes)
                {
                    try
                    {
                        string href = HTMLCleaner.CleanUrl(node.Attributes["href"].Value);
                        if (!string.IsNullOrEmpty(href) && !node.Attributes["href"].Value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            node.Attributes["href"].Value = new Uri(new Uri(Url), node.Attributes["href"].Value).AbsoluteUri;
                        else
                            node.Attributes["href"].Value = href;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(string.Format("FormatHtml替换内容中a标签的href属性时出错, href={0}", node.Attributes["href"].Value), ex);
                    }
                }
            }
            #endregion a整理

            string outHtml = oriHtmlNode.InnerHtml;
            //下面开始字符的替换操作
            #region 去除不允许的html标签
            foreach (string RemoveTag in HtmlRemoveTags_RemoveContent.Split())
                outHtml = HTMLCleaner.StripHtmlTag(outHtml, RemoveTag, true);
            foreach (string RemoveTag in HtmlRemoveTags_RemainContent.Split())
                outHtml = HTMLCleaner.StripHtmlTag(outHtml, RemoveTag, false);
            foreach (string RemoveTag in HtmlRemoveProperty.Split())
                outHtml = HTMLCleaner.StripHtmlProperty(outHtml, RemoveTag);
            #endregion 去除不允许的html标签

            #region some cleanning
            outHtml = Regex.Replace(outHtml, @"\n|\r", string.Empty, RegexOptions.None);
            outHtml = Regex.Replace(outHtml, @"\t", " ", RegexOptions.None);
            outHtml = Regex.Replace(outHtml, @"\s*onload=(""|')?\S*(""|')?\s*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            outHtml = Regex.Replace(outHtml, @"\s*onclick=(""|')?\S*(""|')?\s*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            outHtml = Regex.Replace(outHtml, @"\s*onmouse\S*=(""|')?\S*(""|')?\s*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            outHtml = HTMLCleaner.CleanSpaces(outHtml);
            #endregion some cleanning

            //清理空的<>;《》;（）;();{};[];""
            outHtml = Regex.Replace(outHtml, @"<\s*?>", "", RegexOptions.None);
            outHtml = Regex.Replace(outHtml, @"《\s*?》", "", RegexOptions.None);
            outHtml = Regex.Replace(outHtml, @"（\s*?）", "", RegexOptions.None);
            outHtml = Regex.Replace(outHtml, @"\(\s*?\)", "", RegexOptions.None);
            outHtml = Regex.Replace(outHtml, @"{\s*?}", "", RegexOptions.None);
            outHtml = Regex.Replace(outHtml, @"\[\s*?\]", "", RegexOptions.None);
            outHtml = Regex.Replace(outHtml, "\"\\s*?\"", "", RegexOptions.None);

            //删除空的img标签
            outHtml = outHtml.Replace("<img>", "").Replace("<img/>", "");

            return outHtml;
        }
    }
}
