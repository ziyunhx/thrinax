using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrinax.NLP
{
    /// <summary>
    /// 提取关键词，通过参数区分已分词和未分词两种调用模式
    /// </summary>
    public class KeywordExtracter
    {
        const string StopWordsPath = @"StopWords";

        /// <summary>
        /// 阻尼系数（ＤａｍｐｉｎｇＦａｃｔｏｒ），一般取值为0.85
        /// </summary>
        private static float d = 0.85f;
        /// <summary>
        /// 最大迭代次数
        /// </summary>
        private static int max_iter = 200;
        private static float min_diff = 0.001f;

        /// <summary>
        /// 提取关键词（单词）
        /// </summary>
        /// <param name="Texts">文本集，未进行分词</param>
        /// <param name="MinSupport">所选词的最小支持度</param>
        /// <param name="MaxSupport">所选词的最大支持度</param>
        /// <param name="MinTxtLength">最小分词长度</param>
        /// <param name="isSplitter">是否已分词</param>
        /// <returns></returns>
        public static KeywordSupport[] ExtractSingleKeyword(string[] Texts, int MinSupport = 5, int MaxSupport = 50, int MinTxtLength = 33, bool isSplitter = false)
        {
            //设定保留词性
            HashSet<string> RemainPos = new HashSet<string>(("a ag an ad e b d f j n ng nr ns nt nx nz r s p q v vd vg vn y z").Split());

            //加载停用词
            HashSet<string> StopWords = new HashSet<string>();
            foreach (string FN in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StopWordsPath)))
                foreach (string w in File.ReadLines(FN, Encoding.Default))
                    StopWords.Add(w.ToLower());

            List<string> keywords = new List<string>();
            //分词
            foreach (string Text in Texts)
                if (!string.IsNullOrEmpty(Text) && Text.Length >= MinTxtLength)
                {
                    //过滤后的分词
                    string spliteTag = "";
                    if (!isSplitter)
                        spliteTag = WordSplitter.Splite(Text, true);
                    else
                        spliteTag = Text;

                    string[] Split = WordSplitter.FilterSpliteTag(spliteTag, RemainPos, StopWords, 2, 6);

                    keywords.AddRange(Split.Distinct());
                }

            if (keywords == null || keywords.Count < 1)
                return null;

            Dictionary<string, int> TermCount = new Dictionary<string, int>();
            if (keywords.Count >= 300)
                TermCount = WordCountExtract(keywords.ToArray());
            else
                TermCount = TextRankExtract(keywords.ToArray());

            //Term过滤并排序
            return TermCount.Where(t => t.Value >= MinSupport && t.Value <= MaxSupport)
                .OrderByDescending(t => t.Value)
                .Select(t => new KeywordSupport(t.Key, t.Value)).ToArray();
        }

        /// <summary>
        /// 对单篇文章进行关键词提取，当词的数量小于300时使用TextRank，否则使用WordCount
        /// </summary>
        /// <param name="paper">文本或者分词后结果</param>
        /// <param name="MinSupport">最小得分</param>
        /// <param name="MaxSupport">最大得分</param>
        /// <param name="isSplitter">是否分词</param>
        /// <returns></returns>
        public static KeywordSupport[] ExtractKeyword(string paper, int MinSupport = 5, int MaxSupport = 50, bool isSplitter = false)
        {
            //设定保留词性
            HashSet<string> RemainPos = new HashSet<string>(("a ag an ad e b d f j n ng nr ns nt nx nz r s p q v vd vg vn y z").Split());

            //加载停用词
            HashSet<string> StopWords = new HashSet<string>();
            foreach (string FN in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StopWordsPath)))
                foreach (string w in File.ReadLines(FN, Encoding.Default))
                    StopWords.Add(w.ToLower());

            //分词
            if (string.IsNullOrEmpty(paper))
                return null;

            //过滤后的分词
            string spliteTag = "";
            if (!isSplitter)
                spliteTag = WordSplitter.Splite(paper, true);
            else
                spliteTag = paper;

            string[] Split = WordSplitter.FilterSpliteTag(spliteTag, RemainPos, StopWords, 2, 6);

            Dictionary<string, int> TermCount = new Dictionary<string, int>();
            if (Split.Length >= 300)
                TermCount = WordCountExtract(Split);
            else
                TermCount = TextRankExtract(Split);

            //Term过滤并排序
            return TermCount.Where(t => t.Value >= MinSupport && t.Value <= MaxSupport)
                .OrderByDescending(t => t.Value)
                .Select(t => new KeywordSupport(t.Key, t.Value)).ToArray();
        }

        /// <summary>
        /// 针对词频进行统计来对关键词进行排序
        /// </summary>
        /// <param name="spliteWords">分词后的单个词组，无词性</param>
        /// <returns>词与得分</returns>
        private static Dictionary<string, int> WordCountExtract(string[] spliteWords)
        {
            if (spliteWords == null || spliteWords.Length < 1)
                return null;

            Dictionary<string, int> TermCount = new Dictionary<string, int>();
            //单篇内去重，然后对Term计数
            foreach (string Word in spliteWords)
            {
                if (TermCount.ContainsKey(Word))
                    TermCount[Word] = TermCount[Word] + 1;
                else
                    TermCount.Add(Word, 1);
            }

            return TermCount;
        }

        /// <summary>
        /// 使用TextRank算法来对关键词打分，在原有算法的得分上乘以13来和统计得分相接近
        /// </summary>
        /// <param name="spliteWords"></param>
        /// <returns></returns>
        private static Dictionary<string, int> TextRankExtract(string[] spliteWords)
        {
            if (spliteWords == null || spliteWords.Length < 1)
                return null;

            Dictionary<string, List<string>> words = new Dictionary<string, List<string>>();
            Queue<String> que = new Queue<string>();

            foreach (string w in spliteWords)
            {
                if (!words.ContainsKey(w))
                    words.Add(w, new List<string>());

                que.Enqueue(w);
                if (que.Count > 5)
                    que.Dequeue();

                foreach (string w1 in que)
                {
                    foreach (string w2 in que)
                    {
                        if (w1.Equals(w2))
                            continue;

                        words[w1].Add(w2);
                        words[w2].Add(w1);
                    }
                }
            }

            Dictionary<string, float> score = new Dictionary<string, float>();
            for (int i = 0; i < max_iter; ++i)
            {
                Dictionary<string, float> m = new Dictionary<string, float>();
                float max_diff = 0;
                foreach (var entry in words)
                {
                    String key = entry.Key;
                    List<String> value = entry.Value;
                    m.Add(key, 1 - d);
                    foreach (string element in value)
                    {
                        int size = words[element].Count;
                        if (key.Equals(element) || size == 0)
                            continue;
                        m[key] = m[key] + d / size * (score.ContainsKey(element) ? score[element] : 0);
                    }
                    max_diff = Math.Max(max_diff, Math.Abs(m[key] - (score.ContainsKey(key) ? score[key] : 0)));
                }
                score = m;
                if (max_diff <= min_diff)
                    break;
            }

            //针对结果进行排序，去除停用词，得分*13以适配原有的得分，最好不要混合使用
            return score.OrderByDescending(t => t.Value).Select(t =>
                new KeyValuePair<string, int>(t.Key, Convert.ToInt32(t.Value * 13))).ToDictionary(f => f.Key, f => f.Value);
        }

        /// <summary>
        /// 关键词及其支持度
        /// </summary>
        public class KeywordSupport
        {
            /// <summary>
            /// 关键词
            /// </summary>
            public string Keyword { set; get; }
            /// <summary>
            /// 得分
            /// </summary>
            public int Support { set; get; }

            public KeywordSupport(string Keyword, int Support)
            {
                this.Keyword = Keyword;
                this.Support = Support;
            }
        }
    }
}