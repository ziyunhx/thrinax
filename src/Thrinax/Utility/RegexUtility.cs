using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace Thrinax.Utility
{
    /// <summary>
    /// Regex utility.
    /// </summary>
    public class RegexUtility
    {
        /// <summary>
        /// Matches the specified input, pattern, options, CrawlID and timeoutSecs.
        /// </summary>
        /// <returns>The matches.</returns>
        /// <param name="input">Input.</param>
        /// <param name="pattern">Pattern.</param>
        /// <param name="options">Options.</param>
        /// <param name="CrawlID">Crawl identifier.</param>
        /// <param name="timeoutSecs">Timeout secs.</param>
        public static MatchCollection Matches(string input, string pattern, RegexOptions options, string CrawlID, int timeoutSecs = 200)
        {
            MatchCollection matchResult = null;

            Thread thread = new Thread(() =>
                {
                    try
                    {
                        matchResult = Regex.Matches(input, pattern, options, new TimeSpan(0, 0, timeoutSecs));
                    }
                    catch (Exception ex)
                    {
                        matchResult = null;
                        Logger.Error("Regex match error.", ex);
                    }
                });
            thread.Name = Thread.CurrentThread.Name + " DoCrawl";
            try
            {
                thread.Start();
            }
            catch (RegexMatchTimeoutException e)
            {
                Logger.Warn("正则解析时间超长Collection CrawlID:" + CrawlID, e);
                return null;
            }
            bool timeout = !thread.Join(new TimeSpan(0, 0, timeoutSecs));

            if (timeout)
            {
                #region 任务超时
                thread.Abort();
                matchResult = null;
                #endregion
            }

            return matchResult;
        }

        /// <summary>
        /// Match the specified input, pattern, options, CrawlID and timeoutSecs.
        /// </summary>
        /// <returns>The match.</returns>
        /// <param name="input">Input.</param>
        /// <param name="pattern">Pattern.</param>
        /// <param name="options">Options.</param>
        /// <param name="CrawlID">Crawl identifier.</param>
        /// <param name="timeoutSecs">Timeout secs.</param>
        public static Match Match(string input, string pattern, RegexOptions options, string CrawlID, int timeoutSecs = 10)
        {
            try
            {
                return Regex.Match(input, pattern, options, new TimeSpan(0, 0, timeoutSecs));
            }
            catch (RegexMatchTimeoutException e)
            {
                Logger.Warn("正则解析时间超长Single match CrawlID:" + CrawlID, e);
                return null;
            }
        }

        /// <summary>
        /// Tries the get string.
        /// </summary>
        /// <returns>The get string.</returns>
        /// <param name="m">M.</param>
        /// <param name="MatchGroupName">Match group name.</param>
        /// <param name="DefaultValue">Default value.</param>
        /// <param name="Clean">If set to <c>true</c> clean.</param>
        /// <param name="weibo">If set to <c>true</c> weibo.</param>
        public static string TryGetString(Match m, string MatchGroupName, string DefaultValue, bool Clean = true)
        {
            if (Clean)
            {
                return m.Groups[MatchGroupName].Success ? TextCleaner.FullClean(m.Groups[MatchGroupName].Value) : DefaultValue;
            }
            else
            {
                return m.Groups[MatchGroupName].Success ? m.Groups[MatchGroupName].Value : DefaultValue;
            }
        }

        /// <summary>
        /// Tries the get int.
        /// </summary>
        /// <returns>The get int.</returns>
        /// <param name="m">M.</param>
        /// <param name="MatchGroupName">Match group name.</param>
        /// <param name="DefaultValue">Default value.</param>
        public static int TryGetInt(Match m, string MatchGroupName, int DefaultValue)
        {
            try
            {
                return m.Groups[MatchGroupName].Success ? int.Parse(m.Groups[MatchGroupName].Value.Replace(",", string.Empty)) : DefaultValue;
            }
            catch
            {
                return DefaultValue;
            }
        }
    }
}
