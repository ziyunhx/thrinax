using System;

namespace Thrinax.Utility
{
    public class TimeUtility
    {
        /// <summary>
		/// change Unix timestamp to csharp DateTime
        /// </summary>
		/// <param name="d">timestamp</param>
        /// <returns>DateTime</returns>
        public static DateTime ConvertIntDateTime(long d)
        {
            return GetTime(d.ToString());
        }

        /// <summary>
		/// change csharp DateTime to Unix timestamp
        /// </summary>
		/// <param name="time">DateTime</param>
        /// <returns>13位时间戳</returns>
        public static long ConvertDateTimeInt(DateTime time)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000;  //除10000调整为13位
            return t;
        }

        /// <summary>
        /// 获取时间戳(10位)
        /// </summary>
        /// <returns></returns>
        public static long GetUnixTimeStamp(DateTime argDateTime)
        {
            return ConvertDateTimeInt(argDateTime) / 1000;
        }

        /// <summary>
        /// 时间戳转为C#格式时间，无法转换时返回当前时间
        /// </summary>
        /// <param name="timeStamp">Unix时间戳格式</param>
        /// <returns>C#格式时间</returns>
        public static DateTime GetTime(string timeStamp)
        {
            //根据字符串长度判断是否符合规则，对于不符合规则的返回当前时间
            if (timeStamp.Length != 10 && timeStamp.Length != 13 && timeStamp.Length != 17)
                return DateTime.Now;

            //根据字符串长度自动在右边补0，需要17位数字
            timeStamp = timeStamp.PadRight(17, '0');

            try
            {
                DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                long lTime = long.Parse(timeStamp);
                TimeSpan toNow = new TimeSpan(lTime);
                return dtStart.Add(toNow);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        /// <summary>
        /// 时间戳转为C#格式时间，无法转换时返回null
        /// </summary>
        /// <param name="timeStamp">Unix时间戳格式</param>
        /// <returns>C#格式时间</returns>
        public static DateTime? GetDateTime(string timeStamp)
        {
            //根据字符串长度判断是否符合规则，对于不符合规则的返回当前时间
            if (timeStamp.Length != 10 && timeStamp.Length != 13 && timeStamp.Length != 17)
                return null;

            //根据字符串长度自动在右边补0，需要17位数字
            timeStamp = timeStamp.PadRight(17, '0');

            try
            {
                DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                long lTime = long.Parse(timeStamp);
                TimeSpan toNow = new TimeSpan(lTime);
                return dtStart.Add(toNow);
            }
            catch
            {
                return null;
            }
        }
    }
}
