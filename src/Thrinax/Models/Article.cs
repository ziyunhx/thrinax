using System;
using System.Collections.Generic;

namespace Thrinax.Models
{
    /// <summary>
    /// Article.
    /// </summary>
    public class Article
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        public string Content { get; set; }
        /// <summary>
        /// Gets or sets the content of the html.
        /// </summary>
        /// <value>The content of the html.</value>
        public string HtmlContent { get; set; }
        /// <summary>
        /// Gets or sets the AbsTract.
        /// </summary>
        public string AbsTract { get; set; }
        /// <summary>
        /// Gets or sets the publish date.
        /// </summary>
        /// <value>The pub date.</value>
        public DateTime PubDate { get; set; }
        /// <summary>
        /// Gets or sets the name of the media.
        /// </summary>
        /// <value>The name of the media.</value>
        public string MediaName { get; set; }
        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        /// <value>The channel.</value>
        public string Channel { get; set; }
        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        public string Source { get; set; }
        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        /// <value>The author.</value>
        public string Author { get; set; }
        /// <summary>
        /// Gets or sets the view data list.
        /// </summary>
        /// <value>The view data list.</value>
        public List<ViewData> ViewDataList { get; set; }
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
        /// Gets or sets the page contents.
        /// </summary>
        /// <value>The page contents.</value>
        public List<string> PageContents { get; set; }
        /// <summary>
        /// Gets or sets the page html contents.
        /// </summary>
        /// <value>The page html contents.</value>
        public List<string> PageHtmlContents { get; set; }
    }

    /// <summary>
    /// View data.
    /// </summary>
    public class ViewData
    {
        /// <summary>
        /// Gets or sets the fetch time.
        /// </summary>
        /// <value>The fetch time.</value>
        public DateTime FetchTime { get; set; }
        /// <summary>
        /// Gets or sets the view count.
        /// </summary>
        /// <value>The view.</value>
        public int View { get; set; }
        /// <summary>
        /// Gets or sets the reply count.
        /// </summary>
        /// <value>The reply.</value>
        public int Reply { get; set; }
        /// <summary>
        /// Gets or sets the favorite count.
        /// </summary>
        /// <value>The favorite.</value>
        public int Favorite { get; set; }
        /// <summary>
        /// Gets or sets the like count.
        /// </summary>
        /// <value>The like.</value>
        public int Like { get; set; }
        public int Forward { get; set; }
    }
}
