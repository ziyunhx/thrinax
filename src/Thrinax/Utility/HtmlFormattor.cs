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
        public static string HtmlRemoveTags_RemoveContent = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveTags_RemoveContent"] ?? @"script obj object param embed map input";

        /// <summary>
        /// 清洗时去掉的Html标签(其中内容保留)
        /// </summary>
        public static string HtmlRemoveTags_RemainContent = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveTags_RemainContent"] ?? @"span font param pre a";

        /// <summary>
        /// 清洗时去掉的Html标签中的属性
        /// </summary>
        public static string HtmlRemoveProperty = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveProperty"] ?? @"style class id";

        /// <summary>
        /// 图片的alt属性文字
        /// </summary>
        public static string HtmlImgAltText = ConfigurationManager.AppSettings["Thrinax.HtmlImgAltText"] ?? @"Thrinax";

        /// <summary>
        /// 如果内部内容为空zhe
        /// </summary>
        public static string HtmlRemoveNullTags = ConfigurationManager.AppSettings["Thrinax.HtmlRemoveNullTags"] ?? @"div span table center iframe";

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
            if (string.IsNullOrEmpty(OriHtml)) return OriHtml;

            #region 去除不允许的html标签

            foreach (string RemoveTag in HtmlRemoveTags_RemoveContent.Split())
                OriHtml = HTMLCleaner.StripHtmlTag(OriHtml, RemoveTag, true);
            foreach (string RemoveTag in HtmlRemoveTags_RemainContent.Split())
                OriHtml = HTMLCleaner.StripHtmlTag(OriHtml, RemoveTag, false);
            foreach (string RemoveTag in HtmlRemoveProperty.Split())
                OriHtml = HTMLCleaner.StripHtmlProperty(OriHtml, RemoveTag);

            #endregion 去除不允许的html标签

            #region some cleanning

            OriHtml = Regex.Replace(OriHtml, @"\n|\r", string.Empty, RegexOptions.None);
            OriHtml = Regex.Replace(OriHtml, @"\t", " ", RegexOptions.None);
            OriHtml = Regex.Replace(OriHtml, @"\s*onload=(""|')?\S*(""|')?\s*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            OriHtml = Regex.Replace(OriHtml, @"\s*onclick=(""|')?\S*(""|')?\s*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            OriHtml = Regex.Replace(OriHtml, @"\s*onmouse\S*=(""|')?\S*(""|')?\s*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            OriHtml = HTMLCleaner.CleanSpaces(OriHtml);


            if (!OriHtml.Contains("<p>"))
            {
                //将<br> <br/> <br /> <br>替换为<p>分隔
                OriHtml = Regex.Replace(OriHtml, @"<br>|<br />|<br>", "<br/>", RegexOptions.None);

                string[] htmls = OriHtml.Split(new string[] { "<br/>" }, StringSplitOptions.RemoveEmptyEntries);
                OriHtml = "<p>" + String.Join("</p><p>", htmls) + "</p>";
            }
            #endregion some cleanning

            #region 加载Doc对象

            //加载HtmlDocument（内容为Html片段，可能异常）
            HtmlDocument doc = new HtmlDocument();
            bool LoadFail = false;
            string FailMsg = null;
            try
            {
                doc.LoadHtml(OriHtml);
            }
            catch(Exception ex)
            {
                LoadFail = true;
                FailMsg = ex.Message;
            }

            if (LoadFail || doc.DocumentNode == null)
            {
                Logger.Warn(string.Format("FormatHtml加载HtmlDocument异常:{0},Url={1}", FailMsg, Url));
                return OriHtml;
            }

            #endregion 加载Doc对象

            #region P整理
            //如果有p标签
            if (Regex.IsMatch(OriHtml, @"<\s*p(\s|>)", RegexOptions.IgnoreCase))
            {
                HtmlNodeCollection Ps = doc.DocumentNode.SelectNodes("//p");
                if (Ps != null && Ps.Count > 0)
                {
                    foreach (HtmlNode node in Ps)
                    {
                        try
                        {
                            //清理无内容的P
                            if (string.IsNullOrEmpty(TextCleaner.FullClean(node.InnerHtml, false)))
                            {
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
            }

            #endregion P整理

            //清理空内容的标签
            foreach (string RemoveNullTag in HtmlRemoveNullTags.Split())
            {
                HtmlNodeCollection NullTags = doc.DocumentNode.SelectNodes("//" + RemoveNullTag);
                if (NullTags != null && NullTags.Count > 0)
                {
                    foreach (HtmlNode node in NullTags)
                    {
                        try
                        {
                            //清理无内容的P
                            if (string.IsNullOrWhiteSpace(TextCleaner.FullClean(node.InnerHtml, false)))
                            {
                                doc.DocumentNode.RemoveChild(node);
                                //node.ParentNode.Remove();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(string.Format("调整内容中标签时出错:{0},Url={1},P={2}", ex.Message, Url, node.OuterHtml));
                        }
                    }
                }
            }

            OriHtml = doc.DocumentNode.InnerHtml;

            #region Img整理

            try
            {
                //如果有Img标签
                if (Regex.IsMatch(OriHtml, @"<\s*img(\s|>)", RegexOptions.IgnoreCase))
                {
                    HtmlNodeCollection Imgs = doc.DocumentNode.SelectNodes("//img");
                    if (Imgs != null && Imgs.Count > 0)
                        foreach (HtmlNode node in Imgs)
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
                            else if(node.Attributes["data-src"] != null && !string.IsNullOrEmpty(node.Attributes["data-src"].Value)) // wechat
                            {
                                if (node.Attributes["src"] == null)
                                    node.Attributes.Append("src", node.Attributes["data-src"].Value);
                                else
                                    node.SetAttributeValue("src", node.Attributes["data-src"].Value);
                            }

                            //src绝对路径
                            if (node.Attributes["src"] == null || string.IsNullOrEmpty(node.Attributes["src"].Value) )
                                node.RemoveAll();
                            else
                                try
                                {
                                    if (!node.Attributes["src"].Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) 
                                        && !node.Attributes["src"].Value.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                                        && !node.Attributes["src"].Value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                                        && !node.Attributes["src"].Value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
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

                //删除空的img标签
                OriHtml = OriHtml.Replace("<img>", "").Replace("<img/>", "");
            }
            catch
            { }
            #endregion Img整理

            #region 视频标签处理
            //常见视频网站：
            //优酷网/爱奇艺/土豆网/搜狐视频/迅雷看看/凤凰视频/腾讯视频/新浪视频/56网/CNTV视频/酷6网/暴风影音/乐视网/PPS/风行/PPTV
            //百度视频/糖豆网/芒果TV/激动网/第一视频/爆米花视频/华数TV/爱拍原创/百度影音/熊猫频道/YY直播/播视网/A站/B站 
            //目前仅考虑 优酷 腾讯视频
            string youkuRegex = @"youku\.com/player\.php/sid/(?<youkuId>[^/]*)/";
            string youkuFmt = "<iframe width=\"100%\" src=\"http://player.youku.com/embed/{0}\" frameborder=0 allowfullscreen></iframe>";

            string qqRegex = @"video\.qq\.com.{5,15}vid=(?<qqId>[^&]*)&";
            string qqFmt = "<iframe width=\"100%\" src=\"http://v.qq.com/iframe/player.html?vid={0}&tiny=0&auto=0\" frameborder=0 allowfullscreen></iframe>";

            //视频格式 avi,rmvb,rm,mkv,mp4,3gp,flv,swf

            //TODO: 对支持的几种格式替换为iframe形式以实现多平台访问

            //取出已知的几种视频格式，仅保留src与type，allowscriptaccess，allowfullscreen，wmode
            if (Regex.IsMatch(OriHtml, @"<\s*embed[^>]+\.(avi|rmvb|rm|mkv|mp4|3gp|flv|swf)", RegexOptions.IgnoreCase))
            {                
                HtmlNodeCollection As = doc.DocumentNode.SelectNodes("//embed");
                if (As != null && As.Count > 0)
                {
                    foreach (HtmlNode node in As)
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

                            if(!string.IsNullOrEmpty(tempsrc))
                                node.Attributes.Append("src", tempsrc);
                            if(!string.IsNullOrEmpty(temptype))
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
                HtmlNodeCollection Iframe = doc.DocumentNode.SelectNodes("//iframe");
                if (Iframe != null && Iframe.Count > 0)
                {
                    foreach (HtmlNode node in Iframe)
                    {
                        try
                        {
                            //保存下src和type的值
                            string tempsrc = "", temptype = "";
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
            }

            #endregion

            #region a整理

            //如果有a标签
            if (Regex.IsMatch(OriHtml, @"<\s*a[^>]+href[^>]+", RegexOptions.IgnoreCase))
            {
                try
                {
                    HtmlNodeCollection As = doc.DocumentNode.SelectNodes("//a[@href]");
                    if (As != null && As.Count > 0)
                        foreach (HtmlNode node in As)
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
                catch (Exception ex)
                {
                    Logger.Warn(string.Format("FormatHtml选择内容中a标签时出错:{1}, Url={0}", Url, ex.Message));
                }
            }

            #endregion a整理

            return doc.DocumentNode.OuterHtml;
        }
    }
}
