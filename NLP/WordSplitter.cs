using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thrinax.NLP.Segment;

namespace Thrinax.NLP
{
    public class WordSplitter
    {
        /// <summary>
        /// 对传入文本进行分词
        /// </summary>
        /// <param name="Input">文本</param>
        /// <param name="PosTagged">是否标注词性</param>
        /// <returns></returns>
        public static string Splite(string Input, bool PosTagged = true)
        {
            string result = "";

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = ICTCLAS.Splite(Input, PosTagged);
                }
                catch { }
            }

            return result;
        }

        /// <summary>
        /// 把词的数组形式拼接为一个string
        /// </summary>
        /// <param name="Words"></param>
        /// <returns></returns>
        public static string ArrayToString(string[] Words)
        {
            if (Words == null || Words.Length == 0) return null;

            StringBuilder sb = new StringBuilder(Words.Length * 4);
            foreach (string Word in Words)
                if (!string.IsNullOrEmpty(Word))
                    sb.Append(Word).Append(' ');

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 过滤，仅保留指定词性的词，并去掉词性后缀
        /// </summary>
        /// <param name="SpliteTag">"中国/n"，"人民/n"</param>
        /// <param name="RemainPos">保留词性</param>
        /// <param name="StopWords">停用词</param>
        /// <param name="MinLen"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static string[] FilterSpliteTag(string[] SpliteTag, HashSet<string> RemainPos, HashSet<string> StopWords = null, int MinLen = 2, int MaxLen = 10)
        {
            List<string> Words = new List<string>(SpliteTag);
            for (int i = 0; i < Words.Count;)
            {
                int pos = string.IsNullOrEmpty(Words[i]) ? -1 : Words[i].IndexOf('/');
                string Word = pos > 0 ? Words[i].Substring(0, pos) : Words[i];
                if (!string.IsNullOrEmpty(Word) && Word.Length >= MinLen && Word.Length <= MaxLen
                    && (RemainPos == null || pos < 0 || RemainPos.Contains(Words[i].Substring(pos + 1)))
                    && (StopWords == null || !StopWords.Contains(Word.ToLower())))
                {
                    Words[i] = Word; //保留，去掉词性后缀
                    i++;
                }
                else
                    Words.RemoveAt(i); //移除
            }
            return Words.ToArray();
        }

        /// <summary>
        /// 分词函数（返回一个空格分隔的String）
        /// </summary>
        /// <param name="Input">输入字符串</param>
        /// <param name="PosTagged">是否标注词性（如果是增加词性后缀如"/n"）</param>
        /// <param name="RemainPos">仅保留这些词性的词</param>
        /// <param name="StopWords">禁止词列表（小写）</param>
        /// <param name="MinLength">最短词长度</param>
        /// <param name="MaxLength">最长词长度</param>
        /// <returns></returns>
        public static string SpliteFilter(string Input, bool PosTagged = false, HashSet<string> RemainPos = null, HashSet<string> StopWords = null, int MinLength = 0, int MaxLength = 10)
        {
            return ArrayToString(SpliteIntoArray(Input, PosTagged, RemainPos, StopWords, MinLength, MaxLength));
        }

        /// <summary>
        /// 分词函数
        /// </summary>
        /// <param name="Input">输入字符串</param>
        /// <param name="PosTagged">是否标注词性（如果是增加词性后缀如"/n"）</param>
        /// <param name="StopWords">禁止词列表（小写）</param>
        /// <param name="MinLength">最短词长度</param>
        /// <param name="MaxLength">最长词长度</param>
        /// <returns></returns>
        public static string[] SpliteIntoArray(string Input, bool PosTagged = false, HashSet<string> RemainPos = null, HashSet<string> StopWords = null, int MinLength = 0, int MaxLength = 10)
        {
            string str = Splite(Input, true);
            if (string.IsNullOrEmpty(str))
                return null;
            else
                return FilterSpliteTag(str.Split(), RemainPos, StopWords, MinLength, MaxLength);
        }

        /// <summary>
        /// 过滤，仅保留指定词性的词，并去掉词性后缀
        /// </summary>
        /// <param name="SpliteTag">"中国/n"，"人民/n"</param>
        /// <param name="RemainPos"></param>
        /// <param name="StopWords">停用词</param>
        /// <param name="MinLen"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static string[] FilterSpliteTag(string SpliteTag, HashSet<string> RemainPos, HashSet<string> StopWords = null, int MinLen = 2, int MaxLen = 10)
        {
            if (string.IsNullOrEmpty(SpliteTag))
                return null;
            else
                return FilterSpliteTag(SpliteTag.Split(), RemainPos, StopWords, MinLen, MaxLen);
        }
    }
}
