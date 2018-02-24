using Gecko;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Thrinax.Enums;
using Thrinax.Http;
using Thrinax.Models;
using Thrinax.Parser;
using Thrinax.Utility.Smart;

namespace Thrinax.HtmlListParserSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GeckoWebBrowser geckoWebBrowser = new GeckoWebBrowser { Dock = System.Windows.Forms.DockStyle.Fill };
        private bool iscmbLoading = false;
        private List<ListPagePattern> listPagePatterns = null;
        private ListPagePattern selectedPattern = null;
        private string lastTitleXPath = null;
        private string channelUrl = null;
        private HttpResult httpResult = null;

        private string selectedModel = "btnModel1";

        public MainWindow()
        {
            InitializeComponent();

            Xpcom.Initialize("Firefox");

            #region 频闭某些无用的部分
            GeckoPreferences.Default["permissions.default.stylesheet"] = 1;       //启用css
            GeckoPreferences.Default["browser.xul.error_pages.enabled"] = true; //不弹出错误窗口
            GeckoPreferences.Default["browser.link.open_newwindow.restriction"] = 1; //不打开任何新的窗口
            GeckoPreferences.Default["browser.history_expire_days"] = 1;          //浏览历史过期天数
            GeckoPreferences.Default["browser.history_expire_days_min"] = 3;
            GeckoPreferences.Default["browser.cache.memory.capacity"] = 65536;    //FF的内存缓存
            GeckoPreferences.Default["browser.sessionhistory.max_total_viewers"] = 0;    //禁用“上一页”
            GeckoPreferences.Default["privacy.popups.disable_from_plugins"] = 3;//禁用所有弹出框
            GeckoPreferences.Default["privacy.popups.showBrowserMessage"] = false;//禁用弹出框提示
            GeckoPreferences.Default["privacy.popups.policy"] = 2;//禁用所有弹出框
            GeckoPreferences.Default["browser.popups.showPopupBlocker"] = false;
            GeckoPreferences.Default["dom.popup_maximum"] = 0;
            GeckoPreferences.Default["network.http.pipelining"] = true;           //启用Pipeline
            GeckoPreferences.Default["dom.disable_open_during_load"] = true; //禁用页面载入时弹出框
            GeckoPreferences.Default["dom.successive_dialog_time_limit"] = 0;
            GeckoPreferences.Default["gfx.font_rendering.graphite.enabled"] = true;
            #endregion

            geckoWebBrowser.CreateWindow += GeckoWebBrowser_CreateWindow;
            geckoFormHost.Child = geckoWebBrowser;
            geckoWebBrowser.DocumentCompleted += GeckoWebBrowser_DocumentCompleted;

            geckoWebBrowser.Navigate("about:blank");
        }

        private void btn_Parser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txt_url.Text))
            {
                MessageBox.Show("Pleasr input the url to parser.");
                return;
            }

            listPagePatterns = new List<ListPagePattern>();
            selectedPattern = null;
            lastTitleXPath = null;

            channelUrl = txt_url.Text;

            if (checkUseBrowser.IsChecked ?? false)
            {
                httpResult = SeleniumHelper.HttpRequest(channelUrl);
            }
            else
            {
                httpResult = HttpHelper.HttpRequest(channelUrl);
            }

            if (httpResult != null && httpResult.HttpCode == 200)
            {
                listPagePatterns = SmartParser.Extract_Patterns(channelUrl, httpResult.Content, MediaType.WebNews, Enums.Language.CHINESE);
                RefreshUI();
            }
        }

        private void btnModel_Checked(object sender, RoutedEventArgs e)
        {
            string checkedButton = ((RadioButton)sender).Name;
            if (checkedButton.Equals(selectedModel))
                return;

            if (checkedButton.Equals("btnModel1") && listPagePatterns.Count > 0)
                selectedPattern = listPagePatterns[0];
            else if (checkedButton.Equals("btnModel2") && listPagePatterns.Count > 1)
                selectedPattern = listPagePatterns[1];
            else if (checkedButton.Equals("btnModel3") && listPagePatterns.Count > 2)
                selectedPattern = listPagePatterns[2];
            else if (checkedButton.Equals("btnModel4") && listPagePatterns.Count > 3)
                selectedPattern = listPagePatterns[3];
            else if (checkedButton.Equals("btnModel5") && listPagePatterns.Count > 4)
                selectedPattern = listPagePatterns[4];

            txtBaseXpath.Text = selectedPattern.Path?.ItemRootXPath;
            ChangeSelectedXpath();
            ChangeDisplayGecko(selectedPattern);

            selectedModel = checkedButton;
        }

        private void cmbTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var senderCombox = ((ComboBox)sender).SelectedValue;

            if (iscmbLoading || senderCombox == null || (selectedPattern.Path.DateXPath != null && selectedPattern.Path.DateXPath.Equals(senderCombox.ToString())))
                return;

            selectedPattern.Path.DateXPath = senderCombox.ToString();
            var contents = XpathParser.ParseList(httpResult.Content, JsonConvert.SerializeObject(selectedPattern.Path), channelUrl);
            selectedPattern.Contents = contents.Articles.ToArray();
            ChangeSelectedXpath();
        }

        private void cmbAuthor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var senderCombox = ((ComboBox)sender).SelectedValue;

            if (iscmbLoading || senderCombox == null || (selectedPattern.Path.AuthorXPath != null && selectedPattern.Path.AuthorXPath.Equals(senderCombox.ToString())))
                return;

            selectedPattern.Path.AuthorXPath = senderCombox.ToString();
            var contents = XpathParser.ParseList(httpResult.Content, JsonConvert.SerializeObject(selectedPattern.Path), channelUrl);
            selectedPattern.Contents = contents.Articles.ToArray();
            ChangeSelectedXpath();
        }

        private void cmbTitleXpath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var senderCombox = ((ComboBox)sender).SelectedValue;

            if (iscmbLoading || senderCombox == null || (selectedPattern.Path.TitleXPath != null && selectedPattern.Path.TitleXPath.Equals(senderCombox.ToString())))
                return;

            selectedPattern.Path.TitleXPath = senderCombox.ToString();
            var contents = XpathParser.ParseList(httpResult.Content, JsonConvert.SerializeObject(selectedPattern.Path), channelUrl);
            selectedPattern.Contents = contents.Articles.ToArray();
            ChangeSelectedXpath();
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            string successPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "success.txt");

            File.AppendAllText(successPath, JsonConvert.SerializeObject(new KeyValuePair<string, ListPagePattern>(channelUrl, selectedPattern)) + Environment.NewLine);

            MessageBox.Show("Finish!");
        }

        private void btnWrong_Click(object sender, RoutedEventArgs e)
        {
            string failPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "fail.txt");

            File.AppendAllText(failPath, channelUrl + Environment.NewLine);

            MessageBox.Show("Finish!");
        }

        private void GeckoWebBrowser_CreateWindow(object sender, GeckoCreateWindowEventArgs e)
        {
            e.Cancel = true;
        }

        private void GeckoWebBrowser_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {
            if (selectedPattern != null)
            {
                try
                {
                    ChangeDisplayGecko(selectedPattern);
                }
                catch { }
            }
        }

        private void RefreshUI()
        {
            selectedPattern = null;

            if (string.IsNullOrEmpty(channelUrl) || listPagePatterns == null)
            {
                this.Dispatcher.Invoke(new Action(() => {
                    //控制浏览区与判断区样式
                    geckoFormHost.IsEnabled = false;
                    geckoWebBrowser.Navigate("about:blank");
                    geckoWebBrowser.IsAccessible = false;
                    btnRight.IsEnabled = false;
                    btnWrong.IsEnabled = false;

                    //清除表格和下拉框样式与数据
                    dataGridItems.IsEnabled = false;
                    cmbUrlXpath.IsEnabled = false;
                    cmbTitleXpath.IsEnabled = false;
                    cmbTimeXpath.IsEnabled = false;
                    cmbAuthorXpath.IsEnabled = false;

                    cmbUrlXpath.Items.Clear();
                    cmbTitleXpath.Items.Clear();
                    cmbTimeXpath.Items.Clear();
                    cmbAuthorXpath.Items.Clear();

                    //清除模式选择按钮状态
                    btnModel1.IsChecked = false;
                    btnModel2.IsChecked = false;
                    btnModel3.IsChecked = false;
                    btnModel4.IsChecked = false;
                    btnModel5.IsChecked = false;

                    btnModel1.IsEnabled = false;
                    btnModel2.IsEnabled = false;
                    btnModel3.IsEnabled = false;
                    btnModel4.IsEnabled = false;
                    btnModel5.IsEnabled = false;
                }));
            }
            else
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    //启用浏览区域与操作区域，加载缓存的网页
                    geckoFormHost.IsEnabled = true;
                    try
                    {
                        if (geckoWebBrowser.Url != new Uri(channelUrl))
                            geckoWebBrowser.Navigate(channelUrl);
                    }
                    catch { }
                    geckoWebBrowser.IsAccessible = true;
                    btnRight.IsEnabled = true;
                    btnWrong.IsEnabled = true;

                    cmbUrlXpath.Items.Clear();
                    cmbTitleXpath.Items.Clear();
                    cmbTimeXpath.Items.Clear();
                    cmbAuthorXpath.Items.Clear();

                    //在 listPatterns 不为 null 的情况下绑定第一个模式的数据
                    if (listPagePatterns != null && listPagePatterns.Count > 0)
                    {
                        selectedPattern = listPagePatterns[0];
                        cmbUrlXpath.Items.Add(selectedPattern.Path.UrlXPath);
                        cmbTitleXpath.Items.Add(selectedPattern.Path.TitleXPath);

                        cmbTimeXpath.Items.Add(selectedPattern.Path.DateXPath ?? "");
                        //获取备选的时间模式
                        if (selectedPattern.BackUpPaths.ContainsKey(PatternType.Date))
                        {
                            List<string> tempPaths = selectedPattern.BackUpPaths[PatternType.Date];
                            //判断是否为空，以及是否和当选的Xpath相同
                            if (tempPaths != null)
                            {
                                foreach (string tempPath in tempPaths)
                                {
                                    if (!string.IsNullOrEmpty(tempPath) && !tempPath.Equals(selectedPattern.Path.DateXPath))
                                    {
                                        cmbTimeXpath.Items.Add(tempPath);
                                    }
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(selectedPattern.Path.DateXPath))
                            cmbTimeXpath.Items.Add("");

                        cmbAuthorXpath.Items.Add(selectedPattern.Path.AuthorXPath ?? "");
                        //获取备选的作者模式
                        if (selectedPattern.BackUpPaths.ContainsKey(PatternType.Author))
                        {
                            List<string> tempPaths = selectedPattern.BackUpPaths[PatternType.Author];
                            //判断是否为空，以及是否和当选的Xpath相同
                            if (tempPaths != null)
                            {
                                foreach (string tempPath in tempPaths)
                                {
                                    if (!string.IsNullOrEmpty(tempPath) && !tempPath.Equals(selectedPattern.Path.AuthorXPath))
                                    {
                                        cmbAuthorXpath.Items.Add(tempPath);
                                    }
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(selectedPattern.Path.AuthorXPath))
                            cmbAuthorXpath.Items.Add("");

                        cmbUrlXpath.SelectedIndex = 0;
                        cmbTitleXpath.SelectedIndex = 0;
                        cmbTimeXpath.SelectedIndex = 0;
                        cmbAuthorXpath.SelectedIndex = 0;
                        txtBaseXpath.Text = selectedPattern.Path?.ItemRootXPath;

                        var contents = XpathParser.ParseList(httpResult.Content, JsonConvert.SerializeObject(selectedPattern.Path), channelUrl);
                        //绑定出事DataGrid
                        dataGridItems.ItemsSource = contents.Articles;
                    }

                    //启用表格和下拉框
                    dataGridItems.IsEnabled = true;
                    cmbUrlXpath.IsEnabled = false;
                    cmbTitleXpath.IsEnabled = true;
                    cmbTimeXpath.IsEnabled = true;
                    cmbAuthorXpath.IsEnabled = true;

                    //根据数据情况确定模式按钮的可用性
                    btnModel1.IsEnabled = listPagePatterns.Count > 0;
                    btnModel2.IsEnabled = listPagePatterns.Count > 1;
                    btnModel3.IsEnabled = listPagePatterns.Count > 2;
                    btnModel4.IsEnabled = listPagePatterns.Count > 3;
                    btnModel5.IsEnabled = listPagePatterns.Count > 4;

                    btnModel1.IsChecked = true;
                    btnModel2.IsChecked = false;
                    btnModel3.IsChecked = false;
                    btnModel4.IsChecked = false;
                    btnModel5.IsChecked = false;

                    ChangeDisplayGecko(selectedPattern);
                }));
            }
        }

        private void ChangeDisplayGecko(ListPagePattern currentPattern)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(lastTitleXPath))
                {
                    Gecko.DOM.XPathResult oldTemp = geckoWebBrowser.Document.EvaluateXPath(lastTitleXPath);
                    GeckoNode[] oldNodes = oldTemp.GetNodes().ToArray();
                    for (int i = 0; i < oldNodes.Length; i++)
                    {
                        GeckoElement toNoGreen = oldNodes[i] as GeckoElement;
                        toNoGreen.SetAttribute("style", ""); //将大框标为无色
                    }
                }

                Gecko.DOM.XPathResult temp = geckoWebBrowser.Document.EvaluateXPath(currentPattern.Path.ItemRootXPath);
                GeckoNode[] nodes = temp.GetNodes().ToArray();
                for (int i = 0; i < nodes.Length; i++)
                {
                    GeckoElement toGreen = nodes[i] as GeckoElement;
                    toGreen.SetAttribute("style", "border:1px solid #F00;background-color:#87CEEB;"); //将大框标为红框蓝底
                }
                lastTitleXPath = currentPattern.Path.ItemRootXPath;
            }
            catch { }
        }

        private void ChangeSelectedXpath()
        {
            iscmbLoading = true;

            btnRight.IsEnabled = false;
            btnWrong.IsEnabled = false;

            cmbUrlXpath.IsEnabled = false;
            cmbTitleXpath.IsEnabled = false;
            cmbTimeXpath.IsEnabled = false;
            cmbAuthorXpath.IsEnabled = false;

            cmbUrlXpath.Items.Clear();
            cmbTitleXpath.Items.Clear();
            cmbTimeXpath.Items.Clear();
            cmbAuthorXpath.Items.Clear();

            cmbUrlXpath.Items.Add(selectedPattern.Path.UrlXPath);
            //在 listPatterns 不为 null 的情况下绑定第一个模式的数据
            cmbTitleXpath.Items.Add(selectedPattern.Path.TitleXPath);

            cmbTimeXpath.Items.Add(selectedPattern.Path.DateXPath ?? "");
            //获取备选的时间模式
            if (selectedPattern.BackUpPaths.ContainsKey(PatternType.Date))
            {
                List<string> tempPaths = selectedPattern.BackUpPaths[PatternType.Date];
                //判断是否为空，以及是否和当选的Xpath相同
                if (tempPaths != null)
                {
                    foreach (string tempPath in tempPaths)
                    {
                        if (!string.IsNullOrEmpty(tempPath) && !tempPath.Equals(selectedPattern.Path.DateXPath))
                        {
                            cmbTimeXpath.Items.Add(tempPath);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(selectedPattern.Path.DateXPath))
                cmbTimeXpath.Items.Add("");

            cmbAuthorXpath.Items.Add(selectedPattern.Path.AuthorXPath ?? "");
            //获取备选的作者模式
            if (selectedPattern.BackUpPaths.ContainsKey(PatternType.Author))
            {
                List<string> tempPaths = selectedPattern.BackUpPaths[PatternType.Author];
                //判断是否为空，以及是否和当选的Xpath相同
                if (tempPaths != null)
                {
                    foreach (string tempPath in tempPaths)
                    {
                        if (!string.IsNullOrEmpty(tempPath) && !tempPath.Equals(selectedPattern.Path.AuthorXPath))
                        {
                            cmbAuthorXpath.Items.Add(tempPath);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(selectedPattern.Path.AuthorXPath))
                cmbAuthorXpath.Items.Add("");

            cmbUrlXpath.SelectedIndex = 0;
            cmbTitleXpath.SelectedIndex = 0;
            cmbTimeXpath.SelectedIndex = 0;
            cmbAuthorXpath.SelectedIndex = 0;

            var contents = XpathParser.ParseList(httpResult.Content, JsonConvert.SerializeObject(selectedPattern.Path), channelUrl);
            //绑定出事DataGrid
            dataGridItems.ItemsSource = contents.Articles;

            btnRight.IsEnabled = true;
            btnWrong.IsEnabled = true;

            cmbTitleXpath.IsEnabled = true;
            cmbTimeXpath.IsEnabled = true;
            cmbAuthorXpath.IsEnabled = true;

            iscmbLoading = false;
        }

        private void OpenUrl_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            Uri uri = hyperlink.NavigateUri;
            System.Diagnostics.Process.Start("explorer.exe", uri.AbsoluteUri);
        }
    }
}
