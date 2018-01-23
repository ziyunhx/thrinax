using System;
using System.Collections.Generic;

namespace Thrinax.Models
{
    /// <summary>
    /// Article list.
    /// </summary>
    public class ArticleList
    {
        /// <summary>
        /// Gets or sets the article count.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; set; }
        /// <summary>
        /// Gets or sets the articles.
        /// </summary>
        /// <value>The articles.</value>
        public List<Article> Articles { get; set; }
        /// <summary>
        /// Gets or sets the current page.
        /// </summary>
        /// <value>The current page.</value>
        public int CurrentPage { get; set; }
        /// <summary>
        /// Gets or sets the total count.
        /// </summary>
        /// <value>The total count.</value>
        public long TotalCount { get; set; }
        /// <summary>
        /// Gets or sets the total page.
        /// </summary>
        /// <value>The total page.</value>
        public int TotalPage { get; set; }
        /// <summary>
        /// Gets or sets the page urls.
        /// </summary>
        /// <value>The page urls.</value>
        public List<string> PageUrls { get; set; }
        /// <summary>
        /// Gets or sets the next page URL.
        /// </summary>
        /// <value>The next page URL.</value>
        public string NextPageUrl { get; set; }
    }
}
