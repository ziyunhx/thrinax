using System.Windows;
using Thrinax.Extract;
using Thrinax.Http;
using Thrinax.Models;
using System.Linq;
using Thrinax.Utility;

namespace Thrinax.ExtractArticleSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentUrl = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_Extract_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txt_url.Text))
            {
                currentUrl = txt_url.Text;
                string htmlContent = HttpHelper.GetHttpContent(currentUrl);

                Article article = Html2Article.GetArticle(htmlContent);

                if (article != null && !string.IsNullOrWhiteSpace(article.Title))
                {
                    txt_title.Text = article.Title;
                    txt_pubdate.Text = article.PubDate.ToString();

                    if (!string.IsNullOrWhiteSpace(article.HtmlContent))
                        txt_content.Text = HtmlFormattor.FormatHtml(article.HtmlContent, currentUrl);
                }
                else
                {
                    txt_title.Text = "";
                    txt_pubdate.Text = "";
                    txt_content.Text = "";
                }
            }
            else
            {
                MessageBox.Show("Please input your article url to extract.");
            }
        }
    }
}
