using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Thrinax.Http
{
    public class HttpWebClient
    {
        protected HttpClient http;
        protected HttpClientHandler handler;

        public HttpResponseMessage HttpGet(string api, object parameters)
        {
            HttpResponseMessage result = HttpGetAsync(api, parameters).Result;

            return result;
        }

        private Task<HttpResponseMessage> HttpGetAsync(string api, object parameters)
        {
            var paramType = parameters.GetType();
            if (!(paramType.Name.Contains("AnonymousType") && paramType.Namespace == null))
            {
                throw new ArgumentException("Only anonymous type parameters are supported.");
            }

            var dict = paramType.GetProperties().ToDictionary(k => k.Name, v => string.Format("{0}", v.GetValue(parameters, null)));

            return HttpGetAsync(api, dict);
        }


        public HttpResponseMessage HttpGet(string api, Dictionary<string, object> parameters = null)
        {
            HttpResponseMessage result = HttpGetAsync(api, parameters).Result;

            return result;
        }

        private Task<HttpResponseMessage> HttpGetAsync(string api, Dictionary<string, object> parameters = null)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            var queryString = string.Join("&", parameters.Select(p => string.Format("{0}={1}", Uri.EscapeDataString(p.Key), Uri.EscapeDataString(string.Format("{0}", p.Value)))));

            if (!api.Contains("?"))
            {
                api = string.Format("{0}?{1}", api, queryString);
            }
            else
            {
                api = string.Format("{0}&{1}", api, queryString);
            }

            api = api.Trim('&', '?');

            //If the result is 
            return http.GetAsync(api);
        }

        public HttpResponseMessage HttpPost(string api, object parameters)
        {
            HttpResponseMessage result = HttpPostAsync(api, parameters).Result;

            return result;
        }

        private Task<HttpResponseMessage> HttpPostAsync(string api, object parameters)
        {
            var paramType = parameters.GetType();
            if (!(paramType.Name.Contains("AnonymousType") && paramType.Namespace == null))
            {
                throw new ArgumentException("Only anonymous type parameters are supported.");
            }

            var dict = paramType.GetProperties().ToDictionary(k => k.Name, v => v.GetValue(parameters, null));

            return HttpPostAsync(api, dict);
        }

        public HttpResponseMessage HttpPost(string api, Dictionary<string, object> parameters)
        {
            HttpResponseMessage result = HttpPostAsync(api, parameters).Result;

            return result;
        }

        private Task<HttpResponseMessage> HttpPostAsync(string api, Dictionary<string, object> parameters, bool needAuthorized = true)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            var dict = new Dictionary<string, object>(parameters.ToDictionary(k => k.Key, v => v.Value));

            HttpContent httpContent = null;

            if (dict.Count(p => p.Value.GetType() == typeof(byte[]) || p.Value.GetType() == typeof(System.IO.FileInfo)) > 0)
            {
                var content = new MultipartFormDataContent();

                foreach (var param in dict)
                {
                    var dataType = param.Value.GetType();
                    if (dataType == typeof(byte[])) //byte[]
                    {
                        content.Add(new ByteArrayContent((byte[])param.Value), param.Key, GetNonceString());
                    }
                    else if (dataType == typeof(System.IO.FileInfo))
                    {
                        var file = (System.IO.FileInfo)param.Value;
                        content.Add(new ByteArrayContent(System.IO.File.ReadAllBytes(file.FullName)), param.Key, file.Name);
                    }
                    else
                    {
                        content.Add(new StringContent(string.Format("{0}", param.Value)), param.Key);
                    }
                }

                httpContent = content;
            }
            else
            {
                var content = new FormUrlEncodedContent(dict.ToDictionary(k => k.Key, v => string.Format("{0}", v.Value)));
                httpContent = content;
            }

            return http.PostAsync(api, httpContent);
        }

        /// <summary>
        /// Init method
        /// </summary>
        /// <param name="baseUrl">Base Url</param>
        /// <param name="timeout">time out ticks</param>
        public HttpWebClient(string baseUrl = "", long timeout = 30000)
        {
            handler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip
            };

            http = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = new TimeSpan(timeout)
            };
        }

        /// <summary>
        /// Get the Nonce String
        /// </summary>
        /// <param name="length">length</param>
        /// <returns></returns>
        private string GetNonceString(int length = 8)
        {
            var sb = new StringBuilder();

            var rnd = new Random();
            for (var i = 0; i < length; i++)
            {
                sb.Append((char)rnd.Next(97, 123));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 设置cookies
        /// </summary>
        /// <param name="cookieContainer">The set cookie.</param>
        /// <returns></returns>
        private void SetCookie(CookieContainer cookieContainer)
        {
            handler.CookieContainer = cookieContainer;
        }

        /// <summary>
        /// Get all cookies from CookieContainer.
        /// </summary>
        /// <returns>List of cookie</returns>
        public Dictionary<string, string> GetAllCookies()
        {
            CookieContainer cookieContainer = handler.CookieContainer;
            Dictionary<string, string> cookies = new Dictionary<string, string>();

            Hashtable table = (Hashtable)cookieContainer.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance, null, cookieContainer, new object[] { });

            foreach (string pathList in table.Keys)
            {
                StringBuilder _cookie = new StringBuilder();
                SortedList cookieColList = (SortedList)table[pathList].GetType().InvokeMember("m_list",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.Instance, null, table[pathList], new object[] { });
                foreach (CookieCollection colCookies in cookieColList.Values)
                    foreach (Cookie c in colCookies)
                        _cookie.Append(c.Name + "=" + c.Value + ";");

                cookies.Add(pathList, _cookie.ToString().TrimEnd(';'));
            }
            return cookies;
        }

        /// <summary>
        /// convert cookies string to CookieContainer
        /// </summary>
        /// <param name="cookies">cookies dictionary.</param>
        /// <returns>the CookieContainer</returns>
        public static CookieContainer ConvertToCookieContainer(Dictionary<string, string> cookies)
        {
            CookieContainer cookieContainer = new CookieContainer();

            foreach (var cookie in cookies)
            {
                string[] strEachCookParts = cookie.Value.Split(';');
                int intEachCookPartsCount = strEachCookParts.Length;

                foreach (string strCNameAndCValue in strEachCookParts)
                {
                    if (!string.IsNullOrEmpty(strCNameAndCValue))
                    {
                        try
                        {
                            Cookie cookTemp = new Cookie();
                            int firstEqual = strCNameAndCValue.IndexOf("=");
                            string firstName = strCNameAndCValue.Substring(0, firstEqual);
                            string allValue = strCNameAndCValue.Substring(firstEqual + 1, strCNameAndCValue.Length - (firstEqual + 1));
                            cookTemp.Name = firstName;
                            cookTemp.Value = allValue;
                            cookTemp.Path = "/";
                            cookTemp.Domain = cookie.Key;
                            cookieContainer.Add(cookTemp);
                        }
                        catch (Exception ex) 
                        {
                            //do nothing.
                        }
                    }
                }
            }
            return cookieContainer;
        }
    }
}