using Microsoft.VisualStudio.TestTools.UnitTesting;
using Thrinax.Utility;

namespace ThrinaxUnitTest
{
    [TestClass]
    public class HtmlFormatterUnitTest
    {
        [TestMethod]
        public void TestSimpleImgSrcHtmlFormatter()
        {
            string _url = "http://hk.eastmoney.com/news/1535,20180323847472445.html";
            string _html = "<p><img src=\"https://pifm.eastmoney.com/EM_Finance2014PictureInterface/Index.aspx?id=007005&imageType=knews&token=65e339ba36773878639f360ec823415f\" border=\"0\" alt=\"Kͼ 00700_21\" data-code=\"K 00700 | 116 | 5\" style=\"\" width=\"530\" height=\"276\">/></p>";
            string resultHtml = HtmlFormatter.FormatHtml(_html, _url);
            Assert.IsNotNull(resultHtml);
        }
    }
}
