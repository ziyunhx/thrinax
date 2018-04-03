using System;
using Thrinax.Extract;
using Thrinax.Http;
using Thrinax.Models;
using Thrinax.Utility;

namespace Thrinax
{
    /// <summary>
    /// Thrinax Helper
    /// </summary>
    public class ThrinaxHelper
    {
        /// <summary>
        /// Get the article from url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Article GetArticleFromUrl(string url)
        {
            string htmlContent = HttpHelper.GetHttpContent(url);

            return GetArticleFromHtml(htmlContent);
        }

        /// <summary>
        /// Get the article from html
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static Article GetArticleFromHtml(string html)
        {
            return HtmlToArticle.GetArticle(html);
        }

        /// <summary>
        /// Get the article and format the html content.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="html"></param>
        /// <returns></returns>
        public static Article GetArticleAndFormatter(string url, string html = null)
        {
            if (string.IsNullOrWhiteSpace(html))
                html = HttpHelper.GetHttpContent(url);

            Article article = HtmlToArticle.GetArticle(html);

            if (article != null && !string.IsNullOrWhiteSpace(article.HtmlContent))
            {
                article.HtmlContent = HtmlFormatter.FormatHtml(article.HtmlContent, url);
            }

            return article;
        }
    }
}
