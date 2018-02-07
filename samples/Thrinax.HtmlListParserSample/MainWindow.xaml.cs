using Gecko;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Thrinax.HtmlListParserSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GeckoWebBrowser geckoWebBrowser = new GeckoWebBrowser { Dock = System.Windows.Forms.DockStyle.Fill };
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

        }

        private void btnModel_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void cmbTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void cmbAuthor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnWrong_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GeckoWebBrowser_CreateWindow(object sender, GeckoCreateWindowEventArgs e)
        {
            e.Cancel = true;
        }

        private void GeckoWebBrowser_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {

        }
    }
}
