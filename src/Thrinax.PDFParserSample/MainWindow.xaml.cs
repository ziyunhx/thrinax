using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Thrinax.PDFParserSample
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnChosePDF_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowDialog();
            if (fbd.SelectedPath != string.Empty)
            {
                this.txtPDFPath.Text = fbd.SelectedPath;
                this.btnStartParser.Content = "开始解析";
                this.btnStartParser.IsEnabled = true;
            }

        }

        private async void btnStartParser_Click(object sender, RoutedEventArgs e)
        {
            string pdfPath = this.txtPDFPath.Text;
            this.btnStartParser.Content = "解析中...";
            this.btnStartParser.IsEnabled = false;
            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(pdfPath))
                {
                    DirectoryInfo dirInfos = new DirectoryInfo(pdfPath);
                    List<FileInfo> itemfiles = new List<FileInfo>();

                    DateTime readFileTime = DateTime.Now;

                    GetFiles(dirInfos, ref itemfiles);

                    if (itemfiles != null && itemfiles.Count > 0)
                    {
                        var resultPath = System.IO.Path.Combine(pdfPath, "parser_result");
                        if (!Directory.Exists(resultPath))
                        {
                            Directory.CreateDirectory(resultPath);
                        }

                        foreach (var dirInfo in itemfiles)
                        {
                            if (dirInfo.FullName.ToLower().EndsWith(".pdf"))
                            {
                                var result = PDFParser.Parser(dirInfo.FullName, TableContainType.CSV);
                                if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                                {
                                    File.WriteAllText(System.IO.Path.Combine(resultPath, dirInfo.Name.Replace(".pdf", "")) + ".txt", result.Text);
                                }
                            }
                            else if(dirInfo.FullName.ToLower().EndsWith(".jpg"))
                            {
                                using (var engine = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default))
                                {
                                    using (var img = Pix.LoadFromFile(dirInfo.FullName))
                                    {
                                        using (var page = engine.Process(img))
                                        {
                                            string text = page.GetText();

                                            if (!string.IsNullOrWhiteSpace(text))
                                            {
                                                File.WriteAllText(System.IO.Path.Combine(resultPath, dirInfo.Name.Replace(".pdf", "")) + ".1.txt", text);
                                            }
                                        }
                                    }
                                }


                                //AspriseOCR.SetUp();
                                //AspriseOCR ocr = new AspriseOCR();
                                //ocr.StartEngine("chi", AspriseOCR.SPEED_FASTEST);
                                ////string file = "C:\\YOUR_FILE.jpg"; // ☜ jpg, gif, tif, pdf, etc.
                                //string s = ocr.Recognize(dirInfo.FullName, -1, -1, -1, -1, -1, AspriseOCR.RECOGNIZE_TYPE_ALL, AspriseOCR.OUTPUT_FORMAT_PLAINTEXT);

                                //if (!string.IsNullOrWhiteSpace(s))
                                //{
                                //    File.WriteAllText(System.IO.Path.Combine(resultPath, dirInfo.Name.Replace(".pdf", "")) + ".2.txt", s);
                                //}

                                ////Console.WriteLine("Result: " + s);
                                //ocr.StopEngine();
                            }
                        }
                    }
                }
            });

            this.btnStartParser.Content = "解析完成";
            this.btnStartParser.IsEnabled = false;
        }

        public static void GetFiles(DirectoryInfo floder, ref List<FileInfo> files)
        {
            FileInfo[] fileInfo = floder.GetFiles("*.*");

            files.AddRange(fileInfo);

            DirectoryInfo[] dirInfo = floder.GetDirectories();
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                GetFiles(NextFolder, ref files);
            }
        }
    }
}
