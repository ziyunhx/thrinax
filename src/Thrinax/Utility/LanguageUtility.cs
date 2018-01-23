using System.Text.RegularExpressions;
using Thrinax.Enums;

namespace Thrinax.Utility
{
    public static class LanguageUtility
    {
        public static Language DetectedLanguage(string content)
        {
            if (string.IsNullOrEmpty(content)) return Language.CHINESE;

            string chineseRegex = @"[\u4E00-\u9FFF]+";

            var containChinese = Regex.IsMatch(content, chineseRegex, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (containChinese)
            {
                return Language.CHINESE;
            }
            else
            {
                return Language.ENGLISH;
            }
        }
    }
}
