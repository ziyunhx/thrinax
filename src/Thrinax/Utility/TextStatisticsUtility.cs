using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Thrinax.Utility
{
    /// <summary>
    /// 获取文本统计信息的静态工具类
    /// </summary>
    public static class TextStatisticsUtility
    {
        /// <summary>
        /// 计算 text 中非数字的密度。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static double NonNumericDensity(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int score = text.Length;
            foreach (char c in text)
            {
                if (char.IsNumber(c)) --score;
            }
            return (double)score / text.Length;
        }

        /// <summary>
        /// 返回字串 text 中非字母、数字和标点符号的密度。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static double NonNumericAlphabetPunctuationDensity(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int score = text.Length;
            foreach (char c in text)
            {
                if (char.IsDigit(c) || char.IsPunctuation(c)) --score;
            }
            return (double)score / text.Length;
        }

        /// <summary>
        /// 对 text 给定的一组字串，计算它们的各种前缀出现的数量。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Dictionary<string, int> CountPrefix(string[] text)
        {
            var dict = new Dictionary<string, int>();
            foreach (string s in text)
            {
                for (string t = "" + s[0]; t.Length < s.Length; t += s[t.Length])
                {
                    if (dict.ContainsKey(t)) dict.Add(t, 1); else dict[t]++;
                }
            }
            return dict;
        }

        /// <summary>
        /// 返回字串 a 和 b 的共同前缀长度。若不相同则返回 0。
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int MutualPrefixLength(string a, string b)
        {
            int n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; ++i) if (a[i] != b[i]) return i;
            return n;
        }

        /// <summary>
        /// 返回两个字符串的共同前缀
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string MutualPrefix(string a, string b)
        {
            return a.Substring(0, MutualPrefixLength(a, b));
        }

        /// <summary>
        /// 返回一组字符串的公共前缀
        /// </summary>
        /// <param name="sarry"></param>
        /// <returns></returns>
        public static string MutualPrefix(params string[] sarry)
        {
            sarry = (from x in sarry where x != null select x).ToArray();
            if (sarry.Length < 2) return null;
            string prefix = MutualPrefix(sarry[0], sarry[1]);
            for (int i = 2; i < sarry.Length; ++i)
                prefix = MutualPrefix(prefix, sarry[i]);
            return prefix;
        }

        /// <summary>
        /// 获得中文字符的密度
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static double ChineseCharDensity(string s)
        {
            int counter = 0;
            foreach (char c in s)
            {
                if (Convert.ToInt32(c) > 128) ++counter;
            }
            return (double)counter / s.Length;
        }

        /// <summary>
        /// 检查字符串 s 中是否只包含数字和标点符号
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool ContainsOnlyDigitsAndPunctuations(string s, string interestedChars = null, bool alsoIncludePunctuations = false)
        {
            if (interestedChars == null)
            {
                foreach (var c in s)
                    if (!char.IsDigit(c) && !char.IsPunctuation(c)) return false;
            }
            else
            {
                if (alsoIncludePunctuations)
                {
                    foreach (var c in s)
                        if (!char.IsDigit(c) && !char.IsPunctuation(c) && !char.IsWhiteSpace(c) && !interestedChars.Contains(c)) return false;
                }
                else
                {
                    foreach (var c in s)
                        if (!char.IsDigit(c) && !char.IsWhiteSpace(c) && !interestedChars.Contains(c)) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 寻找字符 c 在字串 s 中第 n 次出现的位置。若没有那么多次出现，则返回 -1。
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int IndexOfNthOccuranceOfChar(string s, char c, int n)
        {
            int counter = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                if (s[i] == c) ++counter;
                if (counter == n) return i;
            }
            return -1;
        }

        /// <summary>
        /// 计算 c 给定的这些字符在字串 s 中出现的数量。
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int CountOccurance(string s, char[] c)
        {
            HashSet<char> sc = new HashSet<char>(c);
            int counter = 0;
            foreach (char x in s) if (sc.Contains(x)) ++counter;
            return counter;
        }

        /// <summary>
        /// 计数字符 c 在字符串 s 中出现的次数
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int CountOccurance(string s, char c)
        {
            int counter = 0;
            foreach (char x in s) if (x == c) ++counter;
            return counter;
        }

        /// <summary>
        /// 寻找两个字符串的最长公共字串
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static int LongestCommonSubstring(string str1, string str2)
        {
            if (String.IsNullOrEmpty(str1) || String.IsNullOrEmpty(str2))
                return 0;

            int[,] num = new int[str1.Length, str2.Length];
            int maxlen = 0;

            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] != str2[j])
                        num[i, j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i, j] = 1;
                        else
                            num[i, j] = 1 + num[i - 1, j - 1];

                        if (num[i, j] > maxlen)
                        {
                            maxlen = num[i, j];
                        }
                    }
                }
            }
            return maxlen;
        }

        /// <summary>
        /// 两个字符串之间的编辑距离
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static int EditDistance(string str1, string str2)
        {
            int[,] mat;
            int n = str1.Length, m = str2.Length;

            if (str1 == str2) return 0;

            int temp = 0;
            char ch1, ch2;
            int i = 0, j = 0;
            if (n == 0) return m;
            if (m == 0) return n;

            mat = new int[n + 1, m + 1];

            for (i = 0; i <= n; i++) mat[i, 0] = i;
            for (j = 0; j <= m; j++) mat[0, j] = j;

            for (i = 1; i <= n; i++)
            {
                ch1 = str1[i - 1];
                for (j = 1; j <= m; j++)
                {
                    ch2 = str2[j - 1];
                    if (ch1.Equals(ch2))
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = 1;
                    }
                    mat[i, j] = Math.Min(mat[i - 1, j] + 1, mat[i, j - 1] + 1);
                    mat[i, j] = Math.Min(mat[i, j], mat[i - 1, j - 1] + temp);
                }
            }

            return mat[n, m];
        }

        /// <summary>
        /// 判断两个字符串是否相似，相似程度由 threshold 决定
        /// 对于比较长的字符串，使用随机算法
        /// </summary>
        /// <param name="compareStr"></param>
        /// <param name="standardStr"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static bool Alike(string compareStr, string standardStr, double threshold = 0.9)
        {
            if (compareStr == standardStr || compareStr.EndsWith(standardStr) || compareStr.StartsWith(standardStr)) return true;
            if (compareStr.Length < threshold * standardStr.Length || standardStr.Length < threshold * compareStr.Length) return false;
            //return (double)LongestCommonSubsequence(compareStr, standardStr) >= (threshold) * standardStr.Length;
            if (compareStr.Length < 1000)
                return (double)EditDistance(compareStr, standardStr) <= (1 - threshold) * standardStr.Length;

            // 随机算法， 5/(1-threshold) 次随机出的序列，长度均为 100
            int succs = 0, samples;
            for (samples = 0; samples < 5 / (1 - threshold); ++samples)
            {
                bool flag = true;
                string seq = "";
                foreach (var idx in HtmlUtility.GenerateSampleIndicies(standardStr.Length, 100).OrderBy(o => o))
                {
                    seq += standardStr[idx];
                }
                // 判定 seq 是否是 compareStr 的一个子序列
                for (int i = 0, j = 0; j < compareStr.Length && i < seq.Length; ++i)
                {
                    while (compareStr[j] != seq[i] && j < compareStr.Length) ++j;
                    if (j >= compareStr.Length) { flag = false; break; }
                }
                if (flag) succs++;
            }
            return succs >= threshold * samples;
        }

        public static int LongestCommonSubsequence(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return 0;
            }

            int n = a.Length, m = b.Length;
            int[,] dist = new int[n + 1, m + 1];

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    if (a[i - 1] == b[j - 1])
                        dist[i, j] = dist[i - 1, j - 1] + 1;
                    else
                        dist[i, j] = Math.Max(dist[i, j - 1], dist[i - 1, j]);
                }
            }

            return dist[n, m];
        }

        public static double Likeliness(string a, string b)
        {
            return 1 - (double)EditDistance(a, b) / Math.Max(a.Length, b.Length);
        }

        /// <summary>
        /// 返回字符串的非空白符数量
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static int NonSpaceLength(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            text = text.Trim();
            int score = text.Length;
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c)) --score;
            }
            return score;
        }

        /// <summary>
        /// 安全获得一个字符串的子串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string GetSafeSubstring(string str, int offset)
        {
            if (offset >= str.Length - 1) return "";
            return str.Substring(offset);
        }

        /// <summary>
        /// 一个英文字符算作半个字长
        /// </summary>
        /// <param name="str"></param>
        /// <param name="StopWords">禁用词将被替换为Empty</param>
        /// <returns></returns>
        public static int GetWeightedLength(string str, IEnumerable<string> StopWords = null)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;
            str = System.Web.HttpUtility.HtmlDecode(str);
            str = Regex.Replace(str, @"\s*", String.Empty);

            //禁用词替换
            if (StopWords != null)
                str = TextCleaner.RemoveStopWords(str, StopWords);

            MatchCollection matches = Regex.Matches(str, @"[a-zA-Z0-9\ \+\-\*\\/]+");

            Int32 alphLen = 0;
            foreach (Match match in matches)
            {
                alphLen += match.Value.Length;
            }

            return str.Length - alphLen + alphLen / 2;
        }
    }
}
