namespace Thrinax.Data
{
    /// <summary>
    /// The result of http request
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// the http status code.
        /// </summary>
        public int HttpCode { set; get; }
        /// <summary>
        /// the html content.
        /// </summary>
        public string Content { set; get; }
        /// <summary>
        /// the response url.
        /// </summary>
        public string Url { set; get; }
        /// <summary>
        /// the last unix time stamp of result modify.
        /// </summary>
        public long LastModified { set; get; }
    }
}
