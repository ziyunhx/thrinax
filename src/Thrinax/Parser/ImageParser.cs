using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tesseract;
using Thrinax.Enums;
using Thrinax.Models;

namespace Thrinax.Parser
{
    public class ImageParser
    {
        public static string Parser(string imagePath)
        {
            string result = "";
            using (var engine = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    using (var page = engine.Process(img))
                    {
                        var output = new StringBuilder();
                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();
                            do
                            {
                                do
                                {
                                    do
                                    {
                                        do
                                        {
                                            do
                                            {
                                                
                                                if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine))
                                                {
                                                    var confidence = iter.GetConfidence(PageIteratorLevel.TextLine) / 100;
                                                    Rect bounds;
                                                    if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out bounds))
                                                    {
                                                        output.AppendFormat(CultureInfo.InvariantCulture, "<line confidence=\"{0:P}\" bounds=\"{1}, {2}, {3}, {4}\">", confidence, bounds.X1, bounds.Y1, bounds.X2, bounds.Y2);
                                                    }
                                                    else
                                                    {
                                                        output.AppendFormat(CultureInfo.InvariantCulture, "<line confidence=\"{0:P}\">", confidence);
                                                    }
                                                }
                                                if (iter.IsAtBeginningOf(PageIteratorLevel.Word))
                                                {
                                                    var confidence = iter.GetConfidence(PageIteratorLevel.Word) / 100;
                                                    Rect bounds;
                                                    if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out bounds))
                                                    {
                                                        output.AppendFormat(CultureInfo.InvariantCulture, "<word confidence=\"{0:P}\" bounds=\"{1}, {2}, {3}, {4}\">", confidence, bounds.X1, bounds.Y1, bounds.X2, bounds.Y2);
                                                    }
                                                    else
                                                    {
                                                        output.AppendFormat(CultureInfo.InvariantCulture, "<word confidence=\"{0:P}\">", confidence);
                                                    }
                                                }

                                                output.Append(iter.GetText(PageIteratorLevel.Symbol));

                                                if (iter.IsAtFinalOf(PageIteratorLevel.Word, PageIteratorLevel.Symbol))
                                                {
                                                    output.Append("</word>");
                                                }
                                            } while (iter.Next(PageIteratorLevel.Word, PageIteratorLevel.Symbol));

                                            if (iter.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                                            {
                                                output.AppendLine("</line>");
                                            }
                                        } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                                        
                                    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                             
                            } while (iter.Next(PageIteratorLevel.Block));
                        }

                        string result2 = output.ToString();

                        PDFModel pdfModel = new PDFModel();
                        pdfModel.PageNumber = 1;
                        pdfModel.Pages = new List<PdfPageModel>();

                        var positions = page.GetSegmentedRegions(PageIteratorLevel.Word);
                        var text = page.GetWordStrBoxText(1);

                        PdfPageModel pdfPageModel = new PdfPageModel();
                        pdfPageModel.CurrentPage = 1;
                        pdfPageModel.Text = page.GetText();
                        pdfModel.Pages.Add(pdfPageModel);

                        var imgResult = PDFParser.PdfTextFormater(pdfModel, TableContainType.None);

                        if (!string.IsNullOrWhiteSpace(imgResult.Text))
                        {
                            result = imgResult.Text;
                        }
                    }
                }
            }
            return result;
        }
    }
}
