using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Thrinax.Utility
{
    public class DateTimeParser
    {
        /// <summary>
        /// 尝试解析日期+时间字符串，失败则返回最小时间
        /// </summary>
        /// <param name="DateTimeString"></param>
        /// <returns></returns>
        public static DateTime Parser(string DateTimeString)
        {
            if (string.IsNullOrWhiteSpace(DateTimeString)) return DateTime.MinValue;

            DateTimeString = DateTimeString.Trim();
            if ((DateTimeString.StartsWith("(", StringComparison.OrdinalIgnoreCase) && DateTimeString.EndsWith(")", StringComparison.OrdinalIgnoreCase)) ||
                (DateTimeString.StartsWith("[", StringComparison.OrdinalIgnoreCase) && DateTimeString.EndsWith("]", StringComparison.OrdinalIgnoreCase)))
                DateTimeString = DateTimeString.Substring(1, DateTimeString.Length - 2);

            //日期与时间中间包含html，去掉html后无空格
            if (Regex.IsMatch(DateTimeString, @"/\d{5,6}:"))
            {
                try
                {
                    int spacePos = 0;

                    spacePos = DateTimeString.LastIndexOf('/');
                    if(spacePos <=0)
                        spacePos = DateTimeString.LastIndexOf('-');

                    if (spacePos > 0 && DateTimeString.Length > spacePos + 4)
                    {
                        DateTimeString = DateTimeString.Substring(0, spacePos + 4 + 1) + " " 
                            + DateTimeString.Substring(spacePos + 4 + 1, DateTimeString.Length - (spacePos + 4 + 1));
                    }
                }
                catch
                { }
            }


            //Unix timestamp
            if (Regex.IsMatch(DateTimeString, @"\d{10,17}"))
            {
                try
                {
                    while (DateTimeString.Length < 17) DateTimeString = DateTimeString + "0";
                    TimeSpan span = new TimeSpan(long.Parse(DateTimeString));
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
                    return epoch.Add(span);
                }
                catch (Exception)
                {
                    return DateTime.MinValue;
                }

            }

            //html
            DateTimeString = DateTimeString.Replace("&nbsp;", "");

            //今天 昨天 前天
            if (DateTimeString.Contains("昨天"))
                DateTimeString = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd ") + DateTimeString.Replace("昨天", string.Empty);
            if (DateTimeString.Contains("前天"))
                DateTimeString = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd ") + DateTimeString.Replace("前天", string.Empty);
            if (DateTimeString.Contains("今天"))
                DateTimeString = DateTime.Now.ToString("yyyy-MM-dd ") + DateTimeString.Replace("今天", string.Empty);

            //上午下午
            DateTimeString = DateTimeString.Replace("上午", "AM");
            DateTimeString = DateTimeString.Replace("下午", "PM");
            DateTimeString = DateTimeString.Replace("发表于", String.Empty).Trim();

            //时间：
            DateTimeString = Regex.Replace(DateTimeString, @"(时间|time|日期|发布|date)[\s\D]*", string.Empty, RegexOptions.IgnoreCase);

            //x分钟前/小时前/天前
            Match m = Regex.Match(DateTimeString, @"(?<mins>\d+)\s*分钟前");
            if (m.Success)
                return DateTime.Now.AddMinutes(-int.Parse(m.Groups["mins"].Value));
            m = Regex.Match(DateTimeString, @"(?<hours>\d+)\s*小时前");
            if (m.Success)
                return DateTime.Now.AddHours(-int.Parse(m.Groups["hours"].Value));
            m = Regex.Match(DateTimeString, @"半小时前");
            if (m.Success)
                return DateTime.Now.AddMinutes(30);
            m = Regex.Match(DateTimeString, @"(?<days>\d+)\s*天前");
            if (m.Success)
                return DateTime.Now.AddDays(-int.Parse(m.Groups["days"].Value));

            //“(x年)x月x日”
            if (Regex.IsMatch(DateTimeString, @"\d+\s*月\d+\s*日"))
            {
                Match match = Regex.Match(DateTimeString, @"\s*(?:(?<Year>\d+)\s*年)?\s*(?:(?<Month>\d+)\s*月)\s*(?:(?<Day>\d+)\s*日)\s*(?<Rest>.+)?");
                if (match.Success)
                {
                    int Year = match.Groups["Year"].Success ? int.Parse(match.Groups["Year"].Value) : DateTime.Now.Year;
                    int Month = int.Parse(match.Groups["Month"].Value);
                    int Day = int.Parse(match.Groups["Day"].Value);
                    string Rest = match.Groups["Rest"].Value.Trim();

                    DateTimeString = string.Format("{0}-{1}-{2} {3}", Year, Month, Day, Rest);
                }
            }

            //08-26 15:29 缺少年
            if (Regex.IsMatch(DateTimeString, @"^\d+[-/]\d+\s+\d+:\d+"))
            {
                DateTimeString = DateTime.Now.Year + (DateTimeString.Contains("-") ? "-" : "/") + DateTimeString;
            }

            //标准格式 09-1-1  09/1/1 
            DateTime t;
            if (DateTime.TryParse(DateTimeString, null, DateTimeStyles.AssumeLocal, out t))
                return AdjustDateYear(t);

            // copied from ReplyParseStrategy.cs

            Regex regexPubDateFinder1 = new Regex(@"([^\d]|^)((\d{2}|\d{4})[\-/年－\.]([01]*\d)[\-/月－\.]([0123]*\d)[日]?(\s*(\d{1,2}):(\d{2})(:(\d{2}))?))?", RegexOptions.Compiled);
            foreach (Match match in regexPubDateFinder1.Matches(DateTimeString))
            {
                DateTime matchdate = DateTime.Now;
                if (match != null && match.Groups.Count >= 3)
                {
                    string sDate = match.Groups[2].Value;
                    if (sDate.Length < 4) continue;
                    if (!char.IsDigit(sDate[2])) sDate = "20" + match.Value;
                    if (DateTime.TryParse(sDate, null, DateTimeStyles.AssumeLocal, out matchdate) && matchdate < DateTime.Now)
                    {
                        return matchdate;
                    }
                }
            }

            //////////////

            Regex regexPubDateFinder2 = new Regex(@"([^\d]|^)(([01]?\d)[\-/月－\.]([0123]*\d)[日]?\s*((\d{1,2}):(\d{2})(:(\d{2}))?)?)", RegexOptions.Compiled);
            foreach (Match match in regexPubDateFinder2.Matches(DateTimeString))
            {
                DateTime matchdate = DateTime.Now;
                if (match != null && match.Groups.Count >= 4)
                {
                    string sDate = match.Groups[2].Value;
                    if (sDate.Length < 4) continue; 
                    if (sDate.Contains("/")) sDate = DateTime.Now.Year.ToString() + "/" + sDate;
                    else if (sDate.Contains("-")) sDate = DateTime.Now.Year.ToString() + "-" + sDate;
                    else if (sDate.Contains("月")) sDate = DateTime.Now.Year.ToString() + "年" + sDate;
                    if (DateTime.TryParse(sDate, null, DateTimeStyles.AssumeLocal, out matchdate) && matchdate < DateTime.Now)
                    {
                        return matchdate;
                    }
                }
            }



            // 试试看 X分钟/小时/天 前
            Regex regexPubDateFinder4 = new Regex(@"(\d+)\s*(天|小时|分钟|秒|秒钟)前");
            foreach (Match match in regexPubDateFinder4.Matches(DateTimeString))
            {
                if (match.Groups.Count < 3) continue;
                int num = int.Parse(m.Groups[1].Value);

				if(match.Groups[2].Value == "天")
					return (DateTime.Today.AddDays(-num));
				else if(match.Groups[2].Value == "小时")
					return (DateTime.Now.AddHours(-num));
				else if(match.Groups[2].Value == "分钟")
					return (DateTime.Now.AddMinutes(-num));
				else if(match.Groups[2].Value.Contains("秒"))
					return (DateTime.Now.AddSeconds(-num));
            }

            // 今天 昨天 前天
            Regex regexPubDateFinder5 = new Regex(@"(今天|昨天|前天)");
            foreach (Match match in regexPubDateFinder4.Matches(DateTimeString))
            {
                if (match.Groups.Count < 2) continue;

				if(match.Groups[1].Value == "今天")
					return (DateTime.Today);
				else if(match.Groups[1].Value == "昨天")
					return (DateTime.Today.AddDays (-1));
				else if(match.Groups[1].Value == "前天")
					return (DateTime.Today.AddDays (-2));
            }

            //////////////


            Regex regexPubDateFinder3 = new Regex(@"[^\d]((\d{2}|\d{4})[\-/年－\.])?([01]*\d)[\-/月－\.]([0123]*\d)[日]?\s*((\d{1,2}):(\d{2})(:(\d{2}))?)?", RegexOptions.Compiled);
            foreach (Match match in regexPubDateFinder3.Matches(DateTimeString))
            {
                DateTime matchdate = DateTime.Now;
                if (match != null && match.Groups.Count >= 3)
                {
                    string sDate = match.Value;
                    if (!char.IsDigit(match.Value[2])) sDate = "20" + match.Value;
                    if (DateTime.TryParse(sDate, null, DateTimeStyles.AssumeLocal, out matchdate) && matchdate < DateTime.Now)
                    {
                        return matchdate;
                    }
                }
            }


            try
            {
                DateTime r = Parse(DateTimeString, CultureInfo.GetCultureInfo("en-US"));
                if (r == null) r = Parse(DateTimeString, CultureInfo.GetCultureInfo("en-GB"));
                if (r != null)
                {
                    r = AdjustDateYear((DateTime)r);
                }
                return r;
            }
            catch (Exception ex)
            {
                Logger.Warn(string.Format("时间识别错误:{0},String={1}", ex.Message, DateTimeString));
                return DateTime.MinValue;
            }
        }
        private static DateTime AdjustDateYear(DateTime dateTime)
        {
            if (dateTime > DateTime.Now)
            {
                return dateTime.AddYears(-1);
            }
            return dateTime;
        }
        private static Regex rfc2822 = new Regex(@"\s*(?:(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun)\s*,\s*)?(\d{1,2})\s+(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{2,})\s+(\d{2})\s*:\s*(\d{2})\s*(?::\s*(\d{2}))?\s+([+\-]\d{4}|UT|GMT|EST|EDT|CST|CDT|MST|MDT|PST|PDT|[A-IK-Z])", RegexOptions.Compiled);
            
        /// <summary>
        /// Parse is able to parse RFC2822/RFC822 formatted dates.
        /// It has a fallback mechanism: if the string does not match,
        /// the normal DateTime.Parse() function is called.
        /// 
        /// Copyright of RssBandit.org
        /// Author - t_rendelmann
        /// </summary>
        /// <param name="dateTime">Date Time to parse</param>
        /// <returns>DateTime instance</returns>
        /// <exception cref="FormatException">On format errors parsing the DateTime</exception>
        
        private static DateTime Parse(string dateTime, CultureInfo culture)
        {
            ArrayList months = new ArrayList(new string[] { "ZeroIndex", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" });

            if (string.IsNullOrEmpty(dateTime))
                return DateTime.Now;

            Match m = rfc2822.Match(dateTime);
            if (m.Success)
            {
                try
                {
                    int dd = Int32.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    int mth = months.IndexOf(m.Groups[2].Value);
                    int yy = Int32.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                    //// following year completion is compliant with RFC 2822.
                    yy = (yy < 50 ? 2000 + yy : (yy < 1000 ? 1900 + yy : yy));
                    int hh = Int32.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
                    int mm = Int32.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture);
                    int ss = Int32.Parse(m.Groups[6].Value, CultureInfo.InvariantCulture);
                    string zone = m.Groups[7].Value;

                    DateTime xd = new DateTime(yy, mth, dd, hh, mm, ss);
                    return xd.AddHours(RFCTimeZoneToGMTBias(zone) * -1);
                }
                catch (FormatException e)
                {
                    //throw new FormatException(string.Format(Resources.RssToolkit.Culture, Resources.RssToolkit.RssText, e.GetType().Name), e);
                    return DateTime.MinValue;
                }
            }
            else
            {
                // fall-back, if regex does not match:
                try
                {
                    return DateTime.Parse(dateTime, culture);
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// Changes Time zone based on GMT
        /// 
        /// Copyright of RssBandit.org
        /// Author - t_rendelmann
        /// </summary>
        /// <param name="zone">The zone.</param>
        /// <returns>RFCTimeZoneToGMTBias</returns>
        private static double RFCTimeZoneToGMTBias(string zone)
        {
            Dictionary<string, int> timeZones = null;

            //if (HttpContext.Current != null)
            //{
            //    timeZones = HttpContext.Current.Cache[TimeZoneCacheKey] as Dictionary<string, int>;
            //}

            if (timeZones == null)
            {
                timeZones = new Dictionary<string, int>();
                timeZones.Add("GMT", 0);
                timeZones.Add("UT", 0);
                timeZones.Add("EST", -5 * 60);
                timeZones.Add("EDT", -4 * 60);
                timeZones.Add("CST", -6 * 60);
                timeZones.Add("CDT", -5 * 60);
                timeZones.Add("MST", -7 * 60);
                timeZones.Add("MDT", -6 * 60);
                timeZones.Add("PST", -8 * 60);
                timeZones.Add("PDT", -7 * 60);
                timeZones.Add("Z", 0);
                timeZones.Add("A", -1 * 60);
                timeZones.Add("B", -2 * 60);
                timeZones.Add("C", -3 * 60);
                timeZones.Add("D", -4 * 60);
                timeZones.Add("E", -5 * 60);
                timeZones.Add("F", -6 * 60);
                timeZones.Add("G", -7 * 60);
                timeZones.Add("H", -8 * 60);
                timeZones.Add("I", -9 * 60);
                timeZones.Add("K", -10 * 60);
                timeZones.Add("L", -11 * 60);
                timeZones.Add("M", -12 * 60);
                timeZones.Add("N", 1 * 60);
                timeZones.Add("O", 2 * 60);
                timeZones.Add("P", 3 * 60);
                timeZones.Add("Q", 4 * 60);
                timeZones.Add("R", 3 * 60);
                timeZones.Add("S", 6 * 60);
                timeZones.Add("T", 3 * 60);
                timeZones.Add("U", 8 * 60);
                timeZones.Add("V", 3 * 60);
                timeZones.Add("W", 10 * 60);
                timeZones.Add("X", 3 * 60);
                timeZones.Add("Y", 12 * 60);

                //if (HttpContext.Current != null)
                //{
                //    HttpContext.Current.Cache.Insert(TimeZoneCacheKey, timeZones);
                //}
            }

            if (zone.IndexOfAny(new char[] { '+', '-' }) == 0)  // +hhmm format
            {
                int sign = (zone[0] == '-' ? -1 : 1);
                string s = zone.Substring(1).TrimEnd();
                int hh = Math.Min(23, Int32.Parse(s.Substring(0, 2), CultureInfo.InvariantCulture));
                int mm = Math.Min(59, Int32.Parse(s.Substring(2, 2), CultureInfo.InvariantCulture));
                return sign * (hh + (mm / 60.0));
            }
            else
            { // named format
                string s = zone.ToUpper(CultureInfo.InvariantCulture).Trim();
                foreach (string key in timeZones.Keys)
                {
                    if (key.Equals(s))
                    {
                        return timeZones[key] / 60.0;
                    }
                }
            }

            return 0.0;
        }
    }
}
