using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrinax.Models;
using Thrinax.Utility;

namespace Thrinax.Http
{

    /// <summary>
    /// Selenium 浏览器实例
    /// </summary>
    public class SeleniumDriverInfo : IDisposable
    {
        /// <summary>
        /// Hash ID
        /// </summary>
        public string hashId { set; get; }
        /// <summary>
        /// 实际的浏览器实例
        /// </summary>
        public RemoteWebDriver client { set; get; }
        /// <summary>
        /// 超时时间
        /// </summary>
        public DateTime delayTime { set; get; }
        /// <summary>
        /// 最后激活时间
        /// </summary>
        public DateTime lastActiveTime { set; get; }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            client.Close();
            client.Dispose();
        }
    }

    /// <summary>
    /// 浏览器池
    /// </summary>
    public class SeleniumPool : IDisposable
    {
        private static string _browserClientCount = ConfigurationManager.AppSettings["Thrinax.browserClientCount"];

        private static int browserClientCount {
            get {
                int defaultCount = -1;
                Int32.TryParse(_browserClientCount, out defaultCount);
                if (defaultCount <= 0)
                    defaultCount = 10;

                return defaultCount;
            }
        }

        private static object _redisConnectLock = new object();

        private static SeleniumPool _instance;

        /// <summary>
        /// 浏览器实例
        /// </summary>
        public static SeleniumPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_redisConnectLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SeleniumPool();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// the chrome driver pool.
        /// </summary>
        private ConcurrentStack<SeleniumDriverInfo> objectPool = new ConcurrentStack<SeleniumDriverInfo>();
        /// <summary>
        /// auto reset event.
        /// </summary>
        private AutoResetEvent resetEvent = new AutoResetEvent(false);
        /// <summary>
        /// actived object count.
        /// </summary>
        //private volatile int activedCount = 0;

        private ConcurrentDictionary<string, SeleniumDriverInfo> activedClients = new ConcurrentDictionary<string, SeleniumDriverInfo>();

        /// <summary>
        /// 获取一个SeleniumDriver
        /// </summary>
        /// <returns></returns>
        public SeleniumDriverInfo BorrowInstance()
        {
            Selenium:

            if (activedClients.Count > 0)
            {
                var delayKeys = activedClients.Where(f => f.Value.delayTime <= DateTime.Now).Select(f => f.Key);
                foreach (string delayKey in delayKeys)
                {
                    SeleniumDriverInfo clientInfo;
                    if (activedClients.TryRemove(delayKey, out clientInfo))
                        clientInfo.Dispose();
                }
            }

            if (objectPool.Count == 0)
            {
                if (activedClients.Count >= browserClientCount)
                {
                    Logger.Warn("SeleniumDriverInfo pool is full.");
                    resetEvent.WaitOne();
                }
                else
                    PushObject(CreateInstance());
            }

            SeleniumDriverInfo seleniumClient = null;
            bool result = objectPool.TryPop(out seleniumClient);
            if (!result)
            {
                goto Selenium;
            }

            //每借出一个client均需要将它缓存，并根据配置标识其最后超时时间
            seleniumClient.lastActiveTime = DateTime.Now;
            seleniumClient.delayTime = seleniumClient.lastActiveTime.AddHours(3);

            activedClients[seleniumClient.hashId] = seleniumClient;

            return seleniumClient;
        }

        /// <summary>
        /// 还回一个链接
        /// </summary>
        /// <param name="instance"></param>
        public void ReturnInstance(SeleniumDriverInfo instance)
        {
            if (objectPool.Count >= browserClientCount)
                DestoryInstance(instance);
            else
            {
                //归还后需要移除掉缓存中的部分
                SeleniumDriverInfo clientInfo;
                activedClients.TryRemove(instance.hashId, out clientInfo);
                clientInfo.client.SwitchTo().Window(clientInfo.client.WindowHandles[1]).Close();
                clientInfo.client.SwitchTo().Window(clientInfo.client.WindowHandles[0]);

                PushObject(instance);

                resetEvent.Set();
            }
        }

        /// <summary>
        /// create an auto reset event.
        /// </summary>
        private void CreateResetEvent()
        {
            if (resetEvent == null)
            {
                resetEvent = new AutoResetEvent(false);
            }
        }

        /// <summary>
        /// create the thrift pool.
        /// </summary>
        private void CreateThriftPool()
        {
            if (objectPool == null)
            {
                objectPool = new ConcurrentStack<SeleniumDriverInfo>();
            }
        }

        /// <summary>
        /// push an instance to object pool.
        /// </summary>
        /// <param name="transport"></param>
        private void PushObject(SeleniumDriverInfo transport)
        {
            objectPool.Push(transport);
        }

        /// <summary>
        /// create an instance
        /// </summary>
        /// <returns></returns>
        private SeleniumDriverInfo CreateInstance()
        {
            try
            {
                SeleniumDriverInfo seleniumDriverInfo = new SeleniumDriverInfo();

                seleniumDriverInfo.lastActiveTime = DateTime.Now;
                seleniumDriverInfo.hashId = Guid.NewGuid().ToString();

                var options = new ChromeOptions();
                options.AddArgument("--window-position=-32000,-32000");
                options.AddArgument("headless");
                options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
                options.AddUserProfilePreference("profile.default_content_settings.popups", 0);

                ChromeDriver instance = new ChromeDriver(GetChromeDriverService(), options);
                seleniumDriverInfo.client = instance;

                return seleniumDriverInfo;
            }
            catch (Exception ex)
            {
                Logger.Error("打开浏览器错误：" + ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// destory the instance.
        /// </summary>
        /// <param name="instance"></param>
        private void DestoryInstance(SeleniumDriverInfo instance)
        {
            instance.Dispose();
        }

        private static ChromeDriverService GetChromeDriverService()
        {
            ChromeDriverService pds = ChromeDriverService.CreateDefaultService();
            pds.HideCommandPromptWindow = true;
            //设置代理服务器地址
            //pds.Proxy = $"{ip}:{port}";  
            //设置代理服务器认证信息
            //pds.ProxyAuthentication = GetProxyAuthorization();
            return pds;
        }

        public void Dispose()
        {
            foreach (var client in objectPool)
                client.Dispose();

            foreach (var client in activedClients)
                client.Value.Dispose();
        }
    }

    /// <summary>
    /// 浏览器管理
    /// </summary>
    public class SeleniumHelper
    {
        /// <summary>
        /// 通过 PhantomJS 获取网站源码
        /// </summary>
        /// <param name="url"></param>
        /// <param name="responseUrl"></param>
        /// <param name="Status"></param>
        /// <returns></returns>
        public static string GetHttpContent(string url)
        {
            return HttpRequest(url).Content;
        }

        public static HttpResult HttpRequest(string url, string postData = null, CookieContainer cookies = null, string userAgent = null, string referer = null, string cookiesDomain = null, Encoding encode = null, string method = null, IWebProxy proxy = null, string encoding = null, string contentType = null, int timeout = 8000, Dictionary<string, string> headers = null)
        {
            HttpResult httpResponse = new HttpResult();

            SeleniumDriverInfo seleniumDriverInfo = null;
            try
            {
                seleniumDriverInfo = SeleniumPool.Instance.BorrowInstance();

                // open a new tab and set the context
                ((ChromeDriver)seleniumDriverInfo.client).ExecuteScript("window.open('_blank', 'worktab');");
                seleniumDriverInfo.client.SwitchTo().Window("worktab");
                seleniumDriverInfo.client.Navigate().GoToUrl(url);
                Thread.Sleep(3000); //执行成功后仍然等待3S，用于接收后加载数据

                httpResponse.Url = seleniumDriverInfo.client.Url;
                httpResponse.HttpCode = 200; //todo, 根据返回的内容解析错误代码
                httpResponse.LastModified = TimeUtility.ConvertDateTimeInt(DateTime.Now);

                httpResponse.Content = seleniumDriverInfo.client.PageSource;
            }
            catch (Exception ex)
            {
                httpResponse.Url = url;
                httpResponse.HttpCode = 500;
            }
            finally
            {
                if (seleniumDriverInfo != null)
                    SeleniumPool.Instance.ReturnInstance(seleniumDriverInfo);
            }

            return httpResponse;
        }
    }
}
