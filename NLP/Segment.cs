using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrinax.NLP
{
    public class Segment
    {
    }

    /// <summary>
    /// 基于多层隐马模型
    /// </summary>
    /// <remarks>PC单核测试
    /// 原词库：分词速度新闻文档300+每分钟
    /// 增加搜狗2006，人名、地名、成语、网络新名词库后，分词速度新闻文档300+每分钟
    /// </remarks>
    public class ICTCLASSharp
    {
        static string DataPath = @"data_ws";
        const int nKind = 10; //分词精度1-10，越大精度越高,原始为5

        #region 单件模式

        public WordSegment wordSegment;

        private ICTCLASSharp()
        {
            wordSegment = new WordSegment();
            string ErrMsg;
            if (!wordSegment.InitWordSegment(Path.Combine(RuntimeInfo.BaseDirectory, DataPath) + Path.DirectorySeparatorChar, out ErrMsg))
            {
                throw new Exception("SharpICTCLAS分词器初始化失败:" + ErrMsg);
            }
        }

        private static ICTCLASSharp _Instance = null;

        static ICTCLASSharp Instance
        {
            get
            {
                if ((_Instance) == null)
                    lock (typeof(ICTCLASSharp))
                    {
                        if ((_Instance) == null)
                            _Instance = new ICTCLASSharp();
                    }
                return _Instance;
            }
        }

        #endregion 单件模式

        /*        
        1 Ag 形语素 形容词性语素。形容词代码为a，语素代码ｇ前面置以A。
        2 a 形容词 取英语形容词adjective的第1个字母。
        3 ad 副形词 直接作状语的形容词。形容词代码a和副词代码d并在一起。
        4 an 名形词 具有名词功能的形容词。形容词代码a和名词代码n并在一起。
        5 b 区别词 取汉字“别”的声母。
        6 c 连词 取英语连词conjunction的第1个字母。
        7 Dg 副语素 副词性语素。副词代码为 d，语素代码ｇ前面置以D。
        8 d 副词 取adverb的第2个字母，因其第1个字母已用于形容词。
        9 e 叹词 取英语叹词 exclamation的第1个字母。
        10 f 方位词 取汉字“方”
        11 g 语素 绝大多数语素都能作为合成词的“词根”，取汉字 “根”的声母。
        12 h 前接成分 取英语head的第1个字母。
        13 i 成语 取英语成语idiom的第1个字母。
        14 j 简称略语 取汉字“简”的声母。
        15 k 后接成分 　
        16 l 习用语 习用语尚未成为成语，有点“临时性”，取“临”的声母。
        17 m 数词 取英语numeral的第3个字母，n，u已有他用。
        18 Ng 名语素 名词性语素。名词代码为n，语素代码ｇ前面置以N。
        19 n 名词 取英语名词noun的第1个字母。
        20 nr 人名 名词代码n和“人(ren)”的声母并在一起。
        21 ns 地名 名词代码n和处所词代码s并在一起。
        22 nt 机构团体 “团”的声母为t，名词代码n和t并在一起。
        23 nz 其他专名 “专”的声母的第1个字母为z，名词代码n和z并在一起。
        24 o 拟声词 取英语拟声词onomatopoeia的第1个字母。
        25 p 介词 取英语介词 prepositional的第1个字母。
        26 q 量词 取英语quantity的第1个字母。
        27 r 代词 取英语代词 pronoun的第2个字母,因p已用于介词。
        28 s 处所词 取英语space的第1个字母。
        29 Tg 时语素 时间词性语素。时间词代码为t,在语素的代码g前面置以T。
        30 t 时间词 取英语time的第1个字母。
        31 u 助词 取英语助词 auxiliary
        32 Vg 动语素 动词性语素。动词代码为v。在语素的代码g前面置以V。
        33 v 动词 取英语动词verb的第一个字母。
        34 vd 副动词 直接作状语的动词。动词和副词的代码并在一起。
        35 vn 名动词 指具有名词功能的动词。动词和名词的代码并在一起。
        36 w 标点符号 　
        37 x 非语素字 非语素字只是一个符号，字母x通常用于代表未知数、符号。
        38 y 语气词 取汉字“语”的声母。
        39 z 状态词 取汉字“状”的声母的前一个字母。
        */

        /*词频示例
          词长  频率  词性   词
            0    56    h   (挨)
            2     1    j   (挨)次
            2    19    n   (挨)打
            2     3    ns  (挨)冻
            2     1    n   (挨)斗
            2     9    ns  (挨)饿
            2     4    ns  (挨)个
            4     2    ns  (挨)个儿
            6    17    nr  (挨)家挨户
            2     1    nz  (挨)近
            2     0    n   (挨)骂
            6     1    ns  (挨)门挨户
            2     1    ns  (挨)批
            2     0    ns  (挨)整
            2    12    ns  (挨)着
            2     0    nr  (挨)揍          

            0    10    h   (哎)
            2     3    j   (哎)呀
            2     2    n   (哎)哟          
         */
        /// <summary>
        /// 将源字符串分割成两两分割的Tag
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>分割后的数组，每个数组均含两个字符</returns>
        public static string[] Generate2WordsTag(string source)
        {

            const string sep = @"([`~!@#$%^&*\)-_=+|]{};':,.<>/?《》，。？；‘’“”：、—￥！…（） .""·";
            List<string> terms = new List<string>();
            var knownSplit = source.Split(sep.ToArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in knownSplit)
            {
                if (item.Length == 1)
                {
                    terms.Add(item);
                    continue;

                }
                for (int i = 0; i < item.Length - 1; i++)
                {
                    terms.Add(item[i].ToString() + item[i + 1].ToString());
                }
            }
            var result = terms.Distinct().ToArray();
            return result;

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
        public static string Splite(string Input, bool PosTagged = false, HashSet<string> RemainPos = null, HashSet<string> StopWords = null, int MinLength = 0, int MaxLength = 100)
        {
            if (string.IsNullOrEmpty(Input)) return Input;

            //warn:截断，防止内存溢出
            if (Input.Length > 10000) Input = Input.Substring(0, 10000);

            List<WordResult[]> Words = null;

            try
            {
                Words = Instance.wordSegment.Segment(Input, nKind);
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Splite Err:{0} Input:{1}\n{2}", e.Message, Input, e.StackTrace));
                throw e;
            }

            StringBuilder sb = new StringBuilder(Words.Count * 3);
            foreach (WordResult[] s in Words)
                foreach (WordResult w in s)
                {
                    //跳过开始和结束符号
                    if (w.nPOS == 1 || w.nPOS == 4) continue;

                    string Pos = SharpICTCLAS.Utility.GetPOSString(w.nPOS).TrimEnd('\0');

                    //进行词性过滤
                    if (RemainPos != null)
                        if (!RemainPos.Contains(Pos.ToLower()))
                            continue;

                    //停止词过滤
                    if (StopWords != null)
                        if (StopWords.Contains(w.sWord.ToLower()))
                            continue;

                    //长度过滤
                    if (w.sWord.Length < MinLength || w.sWord.Length > MaxLength)
                        continue;

                    sb.Append(string.Format(PosTagged ? "{0}/{1} " : "{0} ", w.sWord, Pos));
                }

            return sb.ToString();
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
        public static string[] SpliteIntoArray(string Input, HashSet<string> RemainPos, HashSet<string> StopWords, int MinLength, int MaxLength, bool PosTagged = false)
        {
            if (string.IsNullOrEmpty(Input)) return null;

            //warn:截断，防止内存溢出
            if (Input.Length > 10000) Input = Input.Substring(0, 10000);

            List<WordResult[]> Words = Instance.wordSegment.Segment(Input, nKind);

            List<string> Result = new List<string>(Words.Count);
            foreach (WordResult[] s in Words)
                foreach (WordResult w in s)
                {
                    //跳过开始和结束符号
                    if (w.nPOS == 1 || w.nPOS == 4) continue;

                    string Pos = SharpICTCLAS.Utility.GetPOSString(w.nPOS).TrimEnd('\0');

                    //进行词性过滤
                    if (RemainPos != null)
                        if (!RemainPos.Contains(Pos.ToLower()))
                            continue;

                    //停止词过滤
                    if (StopWords != null)
                        if (StopWords.Contains(w.sWord.ToLower()))
                            continue;

                    //长度过滤
                    if (w.sWord.Length < MinLength || w.sWord.Length > MaxLength)
                        continue;

                    if (PosTagged)
                        Result.Add(w.sWord + "/" + Pos);
                    else
                        Result.Add(w.sWord);
                }

            return Result.ToArray();
        }



        #region 词库维护

        /// <summary>
        /// 当前实例的字典
        /// </summary>
        public static WordDictionary WordDictionary
        {
            get { return Instance.wordSegment.CoreDic; }
        }

        /// <summary>
        /// 运行时重新加载词库，初始化分词器
        /// </summary>
        /// <param name="DataPath">分词器所需文件的文件夹，空则加载默认</param>
        public static void Reload(string DataPath = null)
        {
            if (!string.IsNullOrEmpty(DataPath))
                DataPath = DataPath;
            if (_Instance != null)
                WordDictionary.ReleaseDict();
            _Instance = null; //which will cause reinit 
        }

        /// <summary>
        /// 导入词库的类型
        /// </summary>
        public enum DictionaryFormat
        {
            /// <summary>
            /// 词条/t词频/t词性(逗号分隔)
            /// </summary>
            SogouW2006 = 0,

            /// <summary>
            /// 词条,词频,"词性(逗号分隔)"   引号可能有
            /// </summary>
            ExcelCSV = 1
        }

        /// <summary>
        /// 从SogouW2006词性转换到系统词性,vi后加用于人工标注无词性的词时用
        /// </summary>      
        const string SogouW2006PosTrans = "N-n V-v ADJ-a ADV-d CLAS-q ECHO-o AUX-u STRU-u COOR-c CONJ-c SUFFIX-k PREFIX-h PREP-p PRON-r QUES-y NUM-m IDIOM-i vi-vi";

        /// <summary>
        /// 导入外部词库，词频按照重合词频比例平均值
        /// </summary>
        /// <param name="ImportDicFile">外部词库文件名</param>
        /// <param name="ImportEncoding">外部词库文件编码</param>
        /// <param name="SourceDicFile">源dct文件名</param>
        /// <param name="DestDicFile">目标dct文件名</param>
        /// <param name="DicFormat">外部词库类型</param>
        /// <param name="OddLines">导入的库中无效且不在源库中的数据</param>
        /// <param name="ImportFrqRate">设置固定的导入文件频度比例（除以此数字后入库,小于等于0则按照AvgFrqRate入库）</param>
        /// <param name="AvgFrqRate">导入文件的平均频度比例</param>
        /// <returns>导入的条数</returns>
        public static int ImportDictionary(string ImportDicFile, Encoding ImportEncoding, string SourceDicFile, string DestDicFile, DictionaryFormat DicFormat, out string[] OddLines, out double AvgFrqRate, double ImportFrqRate = 0)
        {
            //初始化
            double MaxFrqRate, MinFrqRate;
            WordDictionary.DicWordInfo[] NewWords;
            WordDictionary.DicWordInfo[] ExistWords;
            FindDifferent(ImportDicFile, ImportEncoding, DicFormat, SourceDicFile, out OddLines, out NewWords, out ExistWords, out MaxFrqRate, out MinFrqRate, out AvgFrqRate);

            //加载词库
            WordDictionary dict = new WordDictionary();
            if (!dict.Load(SourceDicFile))
                throw new Exception("load source dic file fail");

            //加入新词
            foreach (WordDictionary.DicWordInfo Word in NewWords)
            {
                int Frq = Convert.ToInt32(ImportFrqRate <= 0 ? Word.Frequence / AvgFrqRate : Word.Frequence / ImportFrqRate);
                dict.AddWord(Word.Word, Word.Pos, Frq);
            }

            //保存
            dict.Save(DestDicFile);
            dict.ReleaseDict();
            return NewWords.Length;
        }

        /// <summary>
        /// 找到导入库和现有库的不同
        /// </summary>
        /// <param name="NewDicFile">导入库文件</param>
        /// <param name="Encoding">导入库文件编码</param>
        /// <param name="DicFormat">导入库文件格式</param>
        /// <param name="SourceDictFileName">原库文件</param>
        /// <param name="OddLines">输出没有词性标注且现有库中也没有的词行</param>
        /// <param name="NewWords">输出新词或现有词的新词性</param>
        /// <param name="ExistWords">输出重复词，且词性也相同</param>
        /// <param name="MaxFrqRate">重复词的最大词频比例</param>
        /// <param name="MinFrqRate">重复词的最小词频比例</param>
        /// <param name="AvgFrqRate">重复词的平均词频比例</param>
        public static void FindDifferent(string NewDicFile, Encoding Encoding, DictionaryFormat DicFormat, string SourceDictFileName,
            out string[] OddLines, out WordDictionary.DicWordInfo[] NewWords, out WordDictionary.DicWordInfo[] ExistWords,
            out double MaxFrqRate, out double MinFrqRate, out double AvgFrqRate)
        {
            WordDictionary SourceDict = new WordDictionary();
            if (!SourceDict.Load(SourceDictFileName))
                throw new Exception("load source dic file fail");
            FindDifferent(NewDicFile, Encoding, DicFormat, SourceDict, out OddLines, out NewWords, out ExistWords, out MaxFrqRate, out MinFrqRate, out AvgFrqRate);
            SourceDict.ReleaseDict();
        }

        /// <summary>
        /// 找到导入库和现有库的不同
        /// </summary>
        /// <param name="NewDicFile">导入库文件</param>
        /// <param name="Encoding">导入库文件编码</param>
        /// <param name="DicFormat">导入库文件格式</param>
        /// <param name="SourceDict">原库对象</param>
        /// <param name="OddLines">输出没有词性标注且现有库中也没有的词行</param>
        /// <param name="NewWords">输出新词或现有词的新词性</param>
        /// <param name="ExistWords">输出重复词，且词性也相同</param>
        /// <param name="MaxFrqRate">重复词的最大词频比例</param>
        /// <param name="MinFrqRate">重复词的最小词频比例</param>
        /// <param name="AvgFrqRate">重复词的平均词频比例</param>
        public static void FindDifferent(string NewDicFile, Encoding Encoding, DictionaryFormat DicFormat, WordDictionary SourceDict,
            out string[] OddLines, out WordDictionary.DicWordInfo[] NewWords, out WordDictionary.DicWordInfo[] ExistWords,
            out double MaxFrqRate, out double MinFrqRate, out double AvgFrqRate)
        {
            //初始化
            MaxFrqRate = double.MinValue; MinFrqRate = double.MaxValue; decimal SumFrqRate = 0;
            //const string[] CheckPos = new string[] { "n", "ns", "nr", "ng", "v", "j", "m", "vn", "a", "q" };

            //准备词性转换
            Dictionary<string, string> PosTrans = getPosTransformMap(DicFormat);

            //加载词库
            Dictionary<string, WordDictionary.DicWordInfo> OldWords = SourceDict.ToWordDictionary(); ;

            //内存词组
            List<string> Odds = new List<string>(OldWords.Count / 2);
            List<WordDictionary.DicWordInfo> Exists = new List<SharpICTCLAS.WordDictionary.DicWordInfo>(OldWords.Count / 2);
            List<WordDictionary.DicWordInfo> News = new List<WordDictionary.DicWordInfo>(OldWords.Count / 2);

            //加载词库并统计库内有的词的词频，以估算词频转换的比例关系
            foreach (string Line in File.ReadAllLines(NewDicFile, Encoding))
            {
                string Word;
                int Frq;
                string Poses;

                switch (DicFormat)
                {
                    case DictionaryFormat.SogouW2006:
                        string[] s = Line.Split('\t', ' ');
                        Word = s[0];
                        Frq = s.Length == 1 ? -1 : int.Parse(s[1]);
                        Poses = s.Length < 2 ? null : s[2];
                        break;

                    case DictionaryFormat.ExcelCSV:
                    default:
                        int p1 = Line.IndexOf(',');
                        int p2 = Line.IndexOf(',', p1 + 1);
                        Word = Line.Substring(0, p1);
                        Frq = int.Parse(Line.Substring(p1 + 1, p2 - p1 - 1));
                        Poses = Line.Substring(p2 + 1).Trim('"').Trim();
                        break;
                }

                if (string.IsNullOrEmpty(Poses))
                {
                    if (!OldWords.ContainsKey(Word.ToLower())) Odds.Add(Line);
                    continue;
                }

                foreach (string InputPos in Poses.TrimEnd(',').Split(','))
                {
                    if (string.IsNullOrEmpty(InputPos)) continue;
                    //如果映射表中没有，则保留原始词性字母
                    string Pos = PosTrans.ContainsKey(InputPos.ToLower()) ? PosTrans[InputPos.ToLower()] : InputPos.ToLower();

                    //是否存在
                    if (OldWords.ContainsKey(Word.ToLower()) && OldWords[Word.ToLower()].Pos.Contains(Pos))
                    {
                        int SourceFrq = OldWords[Word.ToLower()].Frequence;
                        double FrqR = SourceFrq == 0 ? Frq : (double)Frq / SourceFrq;
                        if (FrqR > MaxFrqRate) MaxFrqRate = FrqR;
                        if (FrqR < MinFrqRate) MinFrqRate = FrqR;
                        SumFrqRate += (decimal)FrqR;
                        Exists.Add(new WordDictionary.DicWordInfo(Word, Pos, Frq));
                    }
                    else //新词或新词性
                    {
                        News.Add(new WordDictionary.DicWordInfo(Word, Pos, Frq));
                    }
                }
            }

            //平均频度转换倍数
            AvgFrqRate = Exists.Count > 0 ? Convert.ToDouble(SumFrqRate / Exists.Count) : 0;

            OddLines = Odds.ToArray();
            NewWords = News.ToArray();
            ExistWords = Exists.ToArray();
        }

        /// <summary>
        /// 返回特定词典格式的词性转换映射
        /// </summary>
        /// <param name="DicFormat"></param>
        /// <returns></returns>
        private static Dictionary<string, string> getPosTransformMap(DictionaryFormat DicFormat)
        {
            Dictionary<string, string> PosTrans = new Dictionary<string, string>(50);
            string PosTransString = null;
            switch (DicFormat)
            {
                case DictionaryFormat.SogouW2006:
                case DictionaryFormat.ExcelCSV:
                    PosTransString = SogouW2006PosTrans;
                    break;
            }
            foreach (string PosT in PosTransString.Split())
            {
                string[] s = PosT.Split('-');
                PosTrans.Add(s[0].ToLower(), s[1].ToLower());
            }
            return PosTrans;
        }
        #endregion 词库维护
    }
}
