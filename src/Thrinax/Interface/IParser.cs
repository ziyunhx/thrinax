using System;
namespace Thrinax.Interface
{
    public interface IParser
    {
        /// <summary>
        /// Parses the list.
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="Html">Html.</param>
        /// <param name="Pattern">Pattern.</param>
        /// <param name="Url">URL.</param>
        /// <param name="RecogNextPage">If set to <c>true</c> recog next page.</param>
        ArticleList ParseList(string Html, string Pattern, string Url = null, bool RecogNextPage = true);
        /// <summary>
        /// Parses the item.
        /// </summary>
        /// <param name="Html">Html.</param>
        /// <param name="Pattern">Pattern.</param>
        /// <param name="BaseArticle">Base article.</param>
        /// <returns>Is Success</returns>
        bool ParseItem(string Html, string Pattern, string Url, ref Article BaseArticle);
    }
}
