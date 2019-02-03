using java.io;
using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using technology.tabula;
using technology.tabula.extractors;

namespace Thrinax.Parser.PDFParser
{
    /// <summary>
    /// Pdf文件内容解析
    /// </summary>
    public class PDFParser
    {
        private static string pageEndMark = "--GIMindPageEnd--";
        private static string paragraphEndMark = "--GIMindParagraphEnd--";
        private static string tableStartMark = "--GIMindTableStart--";
        private static string tableEndMark = "--GIMindTableEnd--";
        private static string structStartMark = "--GIMindStructStart--";
        private static string structEndMark = "--GIMindStructEnd--";

        /// <summary>
        /// 通过文件名解析PDF
        /// </summary>
        /// <param name="pdfFileName">PDF文件路径</param>
        /// <param name="tableContainType">表格包含样式</param>
        /// <returns></returns>
        public static PDFModel Parser(string pdfFileName, TableContainType tableContainType)
        {
            if (!System.IO.File.Exists(pdfFileName))
            {
                return null;
            }
            //打开文件
            PDFModel fileContent = null;
            PDDocument reader = null;
            try
            {
                reader = PDDocument.load(new java.io.File(pdfFileName));
                fileContent = Parser(reader, tableContainType);
            }
            catch (Exception ex)
            {
                if (reader != null)
                {
                    reader.close();
                    reader = null;
                }

                return null;
            }
            finally
            {
                reader.close();
                reader = null;
            }

            return fileContent;
        }

        /// <summary>
        /// 通过文件流方式解析PDF
        /// </summary>
        /// <param name="pdfStream">PDF流</param>
        /// <param name="tableContainType">表格包含样式</param>
        /// <returns></returns>
        public static PDFModel Parser(byte[] pdfStream, TableContainType tableContainType)
        {
            PDFModel fileContent = null;
            //打开文件
            PDDocument reader = null;
            try
            {
                InputStream sbs = new ByteArrayInputStream(pdfStream);
                reader = PDDocument.load(sbs);
                fileContent = Parser(reader, tableContainType);
            }
            catch (Exception ex)
            {
                if (reader != null)
                {
                    reader.close();
                    reader = null;
                }

                return null;
            }
            finally
            {
                reader.close();
                reader = null;
            }

            return fileContent;
        }

        /// <summary>
        /// 通过PDF文档对象解析PDF
        /// </summary>
        /// <param name="pdfDocument">PDF文档</param>
        /// <param name="tableContainType">表格包含样式</param>
        /// <returns></returns>
        public static PDFModel Parser(PDDocument pdfDocument, TableContainType tableContainType)
        {
            ObjectExtractor extractor = new ObjectExtractor(pdfDocument);
            PageIterator pageIterator = extractor.extract();
            SpreadsheetExtractionAlgorithm tableExtractor = new SpreadsheetExtractionAlgorithm();

            PDFModel pdfModel = new PDFModel();

            PDFTextStripper pdfStripper = new PDFTextStripper();
            pdfStripper.setPageEnd(pageEndMark);
            pdfStripper.setParagraphEnd(paragraphEndMark);
            string[] strs = Regex.Split(pdfStripper.getText(pdfDocument), pageEndMark, RegexOptions.IgnoreCase);
            if (strs != null && strs.Length > 0)
            {
                
                pdfModel.Pages = new List<PdfPageModel>();
                int cp = 0;

                while (pageIterator.hasNext())
                {
                    PdfPageModel pdfPage = new PdfPageModel();
                    pdfPage.CurrentPage = cp + 1;
                    pdfPage.Text = strs[cp];

                    List<Table> tables = new List<Table>();
                    Page page = pageIterator.next();
                    var pageTables = tableExtractor.extract(page).toArray();
                    if (pageTables != null && pageTables.Length > 0)
                    {
                        for (int i = 0; i < pageTables.Length; i++)
                        {
                            tables.Add(pageTables[i] as Table);
                        }
                    }
                    pdfPage.Tables = tables;
                    pdfModel.Pages.Add(pdfPage);
                    cp++;
                }

                pdfModel.PageNumber = pdfModel.Pages.Count;

                return PdfTextFormater(pdfModel, tableContainType);
            }

            return null;
        }

        /// <summary>
        /// 对PDF解析出的文字进行格式化，去掉页眉，页脚，页码，可识别的表格，并尝试在对分页后的数据进行拼接；
        /// 对常用的指代与冒号分割的进行表格化或Json化；
        /// 表格提供方便显示的CSV格式载入和方便机器计算的Json加载模式；
        /// </summary>
        /// <param name="pdf">结构化后的PDF</param>
        /// <returns></returns>
        protected static PDFModel PdfTextFormater(PDFModel pdf, TableContainType tableContainType)
        {
            StringBuilder sbFileContent = new StringBuilder();
            string fileContent = string.Empty;

            //猜测非空行的最大长度区间, 统计所有行的字数，去除小于5的部分，获取平均数作为可能的最小非换行字数
            int minLineCount = 25;
            List<int> countList = new List<int>();
            if (pdf.PageNumber > 0 && pdf.Pages != null)
            {
                //1. 循环所有的页，提取去除前后空格后的文字，使用分隔符将文字分隔为数组
                Dictionary<string, ContentRemoveTag> maybeBeginText = new Dictionary<string, ContentRemoveTag>();
                Dictionary<string, ContentRemoveTag> maybeEndText = new Dictionary<string, ContentRemoveTag>();
                List<TagPosition> needRemovePage = new List<TagPosition>();

                for (int page = 1; page <= pdf.Pages.Count; page++)
                {
                    PdfPageModel pdfPageModel = pdf.Pages[page - 1];
                    //使用换行符拆分字符串
                    string[] pageTexts = Regex.Split(pdfPageModel.Text, paragraphEndMark, RegexOptions.IgnoreCase);
                    List<int> tempCountList = pageTexts.Select(f => f.Replace(" ", "").Replace("  ", "").Length).Where(f => f > 5).ToList();
                    if (tempCountList != null && tempCountList.Count > 0)
                        countList.AddRange(tempCountList);

                    //获取非空字符串的前三行和后三行的文字部分
                    int beginGetCount = 0;
                    int endGetCount = 0;
                    for (int i = 0; i < pageTexts.Length; i++)
                    {
                        if (beginGetCount < 3)
                        {
                            TagPosition tagPosition = new TagPosition();
                            tagPosition.PageNumber = page;
                            tagPosition.LineNumber = i;

                            string _cleanText = pageTexts[i].Replace(" ", "").Replace("  ", "").Replace("\r", "").Replace("\n", "");

                            if (!string.IsNullOrWhiteSpace(_cleanText))
                            {
                                int numberCount = NumberOfDigits(_cleanText);
                                //去掉单行单个数字的行，同时去除分页前后的换行。
                                if (numberCount == _cleanText.Length && numberCount < 10 && numberCount >= 1)
                                {
                                    needRemovePage.Add(tagPosition);
                                }
                                else
                                {

                                    if (maybeBeginText.ContainsKey(_cleanText))
                                    {
                                        maybeBeginText[_cleanText].OccurCount++;
                                        maybeBeginText[_cleanText].tagPositions.Add(tagPosition);
                                    }
                                    else
                                    {
                                        maybeBeginText[_cleanText] = new ContentRemoveTag();
                                        maybeBeginText[_cleanText].Content = _cleanText;
                                        maybeBeginText[_cleanText].OccurCount++;
                                        maybeBeginText[_cleanText].tagPositions = new List<TagPosition>();
                                        maybeBeginText[_cleanText].tagPositions.Add(tagPosition);
                                    }
                                    beginGetCount++;
                                }
                            }
                            else if (beginGetCount == 0)
                            {
                                needRemovePage.Add(tagPosition);
                            }
                        }

                        if (endGetCount < 3)
                        {
                            TagPosition tagPosition = new TagPosition();
                            tagPosition.PageNumber = page;
                            tagPosition.LineNumber = pageTexts.Length - i - 1;

                            string _cleanText = pageTexts[pageTexts.Length - i - 1].Replace(" ", "").Replace("  ", "").Replace("\r", "").Replace("\n", "");


                            if (!string.IsNullOrWhiteSpace(_cleanText))
                            {
                                int numberCount = NumberOfDigits(_cleanText);
                                //去掉单行单个数字的行，同时去除分页前后的换行。
                                if (numberCount == _cleanText.Length && numberCount < 10 && numberCount >= 1)
                                {
                                    needRemovePage.Add(tagPosition);
                                }
                                else
                                {
                                    if (maybeEndText.ContainsKey(_cleanText))
                                    {
                                        maybeEndText[_cleanText].OccurCount++;
                                        maybeEndText[_cleanText].tagPositions.Add(tagPosition);
                                    }
                                    else
                                    {
                                        maybeEndText[_cleanText] = new ContentRemoveTag();
                                        maybeEndText[_cleanText].Content = _cleanText;
                                        maybeEndText[_cleanText].OccurCount++;
                                        maybeEndText[_cleanText].tagPositions = new List<TagPosition>();
                                        maybeEndText[_cleanText].tagPositions.Add(tagPosition);
                                    }
                                    endGetCount++;
                                }
                            }
                            else if (endGetCount == 0)
                            {
                                needRemovePage.Add(tagPosition);
                            }
                        }
                    }
                }

                //比较和记录出现的频率
                foreach (var _beginItem in maybeBeginText.Values)
                {
                    if (_beginItem.OccurCount > 2 && _beginItem.OccurCount >= (pdf.Pages.Count - 2))
                    {
                        needRemovePage.AddRange(_beginItem.tagPositions);
                    }
                }

                foreach (var _endItem in maybeEndText.Values)
                {
                    if (_endItem.OccurCount > 2 && _endItem.OccurCount >= (pdf.Pages.Count - 2))
                    {
                        needRemovePage.AddRange(_endItem.tagPositions);
                    }
                }

                if (countList != null && countList.Count > 0)
                    minLineCount = countList.Sum() / countList.Count;

                //2. 对段落进行合并和返回
                bool lastIsEnd = true;
                for (int page = 1; page <= pdf.Pages.Count; page++)
                {
                    PdfPageModel pdfPageModel = pdf.Pages[page - 1];
                    string[] pageTexts = Regex.Split(pdfPageModel.Text, paragraphEndMark, RegexOptions.IgnoreCase);


                    //bool needCleanMenu = false;
                    //清理需要清理的行，并进行合并
                    for (int i = 0; i < pageTexts.Length; i++)
                    {
                        //忽略页码行数据
                        if (needRemovePage.Any(f => f.PageNumber == page && f.LineNumber == i))
                            continue;
                        
                        //判断当前页的表格是否包含，存在的情况将表格列替换为表格位置标识的形式，后续替换为CSV或JSON


                        string cleanText = pageTexts[i].TrimEnd('\r','\n');

                        //忽略目录部分的数据
                        string onlyText = cleanText.Replace(" ", "").Replace("  ", "").Replace("\r", "").Replace("\n", "");
                        if (onlyText == "目录" || onlyText.ToUpper() == "MENU")
                        {
                            //needCleanMenu = true;
                            continue;
                        }

                        //if (needCleanMenu)
                        {
                            if (string.IsNullOrWhiteSpace(onlyText) || Regex.IsMatch(onlyText, @".*?(\.{6,}\s*\d+)\s*"))
                                continue;
                            //else
                            //    needCleanMenu = false;
                        }

                        //判断是否以正常中止标点符号结尾
                        bool endWithStopFlag = cleanText.EndsWith("。") || cleanText.EndsWith("！") || cleanText.EndsWith("：") || cleanText.EndsWith("；");

                        //判断该行是否包含正文常见标点符号
                        bool includeNormalFlag = Regex.IsMatch(cleanText, @"[！；，。“]");

                        //统计非空格字数
                        int _lineCount = cleanText.Replace(" ", "").Replace("  ", "").Replace("\r", "").Replace("\n", "").Length;
                        //判断该行字数是否大于最小行字数
                        bool isLenThanMinLineCount = _lineCount >= minLineCount;

                        //特例一：存在明显的排序性质的行，如 ◆，（一），■ 等
                        bool firstException = false;
                        //

                        //情景一：该行是一段的结尾，加上段落的文字后换行
                        if (!firstException && endWithStopFlag)
                        {
                            sbFileContent.Append(cleanText);
                            //sbFileContent.AppendLine("");
                            lastIsEnd = true;
                        }

                        //情景二：该行是普通的一行，并未结束
                        else if (!firstException && !endWithStopFlag && isLenThanMinLineCount)
                        {
                            sbFileContent.Append(cleanText);
                            lastIsEnd = false;
                        }

                        //情景三：该行是独立行
                        else if (firstException || (!isLenThanMinLineCount && !endWithStopFlag && !includeNormalFlag))
                        {
                            if (lastIsEnd)
                            {
                                sbFileContent.Append(cleanText);
                                //sbFileContent.AppendLine("");
                                lastIsEnd = true;
                            }
                            else
                            {
                                sbFileContent.AppendLine(cleanText);
                                //sbFileContent.AppendLine("");
                                lastIsEnd = true;
                            }
                        }
                        else
                        {
                            sbFileContent.AppendLine(cleanText);
                            //sbFileContent.AppendLine("");
                            lastIsEnd = true;
                        }
                    }
                }
            }

            //去掉首尾的换行
            fileContent = sbFileContent.ToString().Trim('\r', '\n', ' ', '\t');
            pdf.Text = fileContent;

            return pdf;
        }

        protected static int NumberOfDigits(string theString)
        {
            int count = 0;
            for (int i = 0; i < theString.Length; i++)
            {
                if (Char.IsDigit(theString[i]))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
