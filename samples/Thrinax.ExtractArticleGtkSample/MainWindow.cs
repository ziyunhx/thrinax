using System;
using Gtk;
using Thrinax.Extract;
using Thrinax.Http;
using Thrinax.Models;
using Thrinax.Utility;

public partial class MainWindow : Gtk.Window
{
    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
    }

    private string currentUrl = null;

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnButton1Clicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(txtUrl.Text))
        {
            currentUrl = txtUrl.Text;
            string htmlContent = HttpHelper.GetHttpContent(currentUrl);

            Article article = Html2Article.GetArticle(htmlContent);

            if (article != null && !string.IsNullOrWhiteSpace(article.Title))
            {
                txtTitle.Text = article.Title;
                txtPubDate.Text = article.PubDate.ToString();

                if (!string.IsNullOrWhiteSpace(article.HtmlContent))
                {
                    string htmlText = HtmlFormattor.FormatHtml(article.HtmlContent, currentUrl);
                    txtContent.Buffer.Text = htmlText;
                }
            }
            else
            {
                txtTitle.Text = "";
                txtPubDate.Text = "";
                txtContent.Buffer.Text = "";
            }
        }
        else
        {
            //MessageBox.Show("Please input your article url to extract.");
        }
    }
}
