using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Tesseract;
using Thrinax.Enums;
using Thrinax.Models;
using Thrinax.Parser;

namespace Thrinax.PDFParserDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnChosePDF_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                this.txtPDFPath.Text = dialog.SelectedPath;
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
                        var resultPath = Path.Combine(pdfPath, "parser_result");
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
                                    File.WriteAllText(Path.Combine(resultPath, dirInfo.Name.Replace(".pdf", "")) + ".txt", result.Text);
                                }
                            }
                            else if (dirInfo.FullName.ToLower().EndsWith(".jpg"))
                            {
                                var imgText = ImageParser.Parser(dirInfo.FullName);

                                
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
