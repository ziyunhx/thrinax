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
using Thrinax.Parser.PDFParser;

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
                this.txtPDFPath.Text = fbd.SelectedPath;
        }

        private void btnStartParser_Click(object sender, RoutedEventArgs e)
        {
            string pdfPath = this.txtPDFPath.Text;

            if (!string.IsNullOrEmpty(pdfPath))
            {
                DirectoryInfo dirInfos = new DirectoryInfo(pdfPath);
                List<FileInfo> itemfiles = new List<FileInfo>();

                DateTime readFileTime = DateTime.Now;

                GetFiles(dirInfos, ref itemfiles);

                if (itemfiles != null && itemfiles.Count > 0)
                {
                    foreach (var dirInfo in itemfiles)
                    {
                        var result = PDFParser.Parser(dirInfo.FullName, TableContainType.CSV);
                        if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                        {
                            File.WriteAllText(System.IO.Path.Combine(Environment.CurrentDirectory, "result/" + dirInfo.Name.Replace(".pdf", "")) + ".txt", result.Text);
                        }
                    }
                }
            }
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
