using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using TNIdea.Common.Data;

namespace TNIdea.Common.Helper
{
    public class HttpHelper
    {
        public const string CharsetReg = @"(meta.*?charset=""?(?<Charset>[^\s""'>]+)""?)|(xml.*?encoding=""?(?<Charset>[^\s"">]+)""?)";

        /// <summary>
        /// 使用Http Request获取网页信息
        /// </summary>
        /// <param name="url">Url</param>
        /// <param name="postData">Post的信息</param>
        /// <param name="cookies">Cookies</param>
        /// <param name="userAgent">浏览器标识</param>
        /// <param name="referer">来源页</param>
        /// <param name="cookiesDomain">Cookies的Domian参数，配合cookies使用；为空则取url的Host</param>
        /// <param name="encode">编码方式，用于解析html</param>
        /// <returns></returns>
        public static HttpResponse HttpRequest(string url, string postData = null, CookieContainer cookies = null, string userAgent = "", string referer = "", string cookiesDomain = "", Encoding encode = null)
        {
            HttpResponse httpResponse = new HttpResponse();

            try
            {
                HttpWebResponse httpWebResponse = null;
                if (!string.IsNullOrWhiteSpace(postData))
                    httpWebResponse = CreatePostHttpResponse(url, postData, cookies: cookies, userAgent: userAgent, referer: referer);
                else
                    httpWebResponse = CreateGetHttpResponse(url, cookies: cookies, userAgent: userAgent, referer: referer);

                httpResponse.Url = httpWebResponse.ResponseUri.ToString();
                httpResponse.HttpCode = (int)httpWebResponse.StatusCode;
                httpResponse.LastModified = TimeHelper.ConvertDateTimeInt(httpWebResponse.LastModified);

                #region 根据Html头判断
                string Content = null;
                //缓冲区长度
                const int N_CacheLength = 10000;
                //头部预读取缓冲区，字节形式
                var bytes = new List<byte>();
                int count = 0;
                //头部预读取缓冲区，字符串
                String cache = string.Empty;

                //创建流对象并解码
                Stream ResponseStream;
                switch (httpWebResponse.ContentEncoding.ToUpperInvariant())
                {
                    case "GZIP":
                        ResponseStream = new GZipStream(
                            httpWebResponse.GetResponseStream(), CompressionMode.Decompress);
                        break;
                    case "DEFLATE":
                        ResponseStream = new DeflateStream(
                            httpWebResponse.GetResponseStream(), CompressionMode.Decompress);
                        break;
                    default:
                        ResponseStream = httpWebResponse.GetResponseStream();
                        break;
                }

                try
                {
                    while (!(cache.EndsWith("</head>", StringComparison.OrdinalIgnoreCase) || count >= N_CacheLength))
                    {
                        var b = (byte)ResponseStream.ReadByte();
                        if (b < 0) //end of stream
                            break;
                        bytes.Add(b);

                        count++;
                        cache += (char)b;
                    }

                    if (encode == null)
                    {
                        try
                        {
                            if (httpWebResponse.CharacterSet == "ISO-8859-1" || httpWebResponse.CharacterSet == "zh-cn")
                            {
                                Match match = Regex.Match(cache, CharsetReg, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                                if (match.Success)
                                {
                                    try
                                    {
                                        string charset = match.Groups["Charset"].Value;
                                        encode = Encoding.GetEncoding(charset);
                                    }
                                    catch { }
                                }
                                else
                                    encode = Encoding.GetEncoding("GB2312");
                            }
                            else if (httpWebResponse.CharacterSet != null)
                                encode = Encoding.GetEncoding(httpWebResponse.CharacterSet);
                            else
                                encode = Encoding.GetEncoding("GB2312");
                        }
                        catch {
                            encode = Encoding.GetEncoding("GB2312");
                        }
                    }

                    //缓冲字节重新编码，然后再把流读完
                    var Reader = new StreamReader(ResponseStream, encode);
                    Content = encode.GetString(bytes.ToArray(), 0, count) + Reader.ReadToEnd();
                    Reader.Close();
                }
                catch (Exception ex)
                {
                    httpResponse.Content = ex.ToString();
                    return httpResponse;
                }
                finally
                {
                    httpWebResponse.Close();
                }
                #endregion 根据Html头判断
                //获取返回的Cookies，支持httponly
                if (string.IsNullOrWhiteSpace(cookiesDomain))
                    cookiesDomain = httpWebResponse.ResponseUri.Host;

                cookies = new CookieContainer();
                CookieCollection httpHeaderCookies = SetCookie(httpWebResponse, cookiesDomain);
                cookies.Add(httpHeaderCookies ?? httpWebResponse.Cookies);

                httpResponse.Content = Content;
            }
            catch
            {
                httpResponse.Content = string.Empty;
            }
            return httpResponse;
        }

        /// <summary>
        /// 获取网页的内容
        /// </summary>
        /// <param name="url">Url</param>
        /// <param name="postData">Post的信息</param>
        /// <param name="cookies">Cookies</param>
        /// <param name="userAgent">浏览器标识</param>
        /// <param name="referer">来源页</param>
        /// <param name="cookiesDomain">Cookies的Domian参数，配合cookies使用；为空则取url的Host</param>
        /// <param name="encode">编码方式，用于解析html</param>
        /// <returns></returns>
        public static string GetHttpContent(string url, string postData = null, CookieContainer cookies = null, string userAgent = "", string referer = "", string cookiesDomain = "", Encoding encode = null)
        {
            return HttpHelper.HttpRequest(url, postData, cookies, userAgent, referer, cookiesDomain, encode).Content;
        }

        /// <summary>
        /// 创建GET方式的HTTP请求 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="timeout"></param>
        /// <param name="userAgent"></param>
        /// <param name="cookies"></param>
        /// <param name="referer"></param>
        /// <returns></returns>
        public static HttpWebResponse CreateGetHttpResponse(string url, int timeout = 60000, string userAgent = "", CookieContainer cookies = null, string referer = "")
        {
            HttpWebRequest request = null;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                //对服务端证书进行有效性校验（非第三方权威机构颁发的证书，如自己生成的，不进行验证，这里返回true）
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                //request.ProtocolVersion = HttpVersion.Version10;    //http版本，默认是1.1,这里设置为1.0
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }

            request.Referer = referer;
            request.Method = "GET";

            //设置代理UserAgent和超时
            if (string.IsNullOrWhiteSpace(userAgent))
                userAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";

            request.UserAgent = userAgent;
            request.Timeout = timeout;
            request.KeepAlive = true;
            request.AllowAutoRedirect = true;

            if (cookies == null)
                cookies = new CookieContainer();
            request.CookieContainer = cookies;

            return request.GetResponse() as HttpWebResponse;
        }

        /// <summary>
        /// 创建POST方式的HTTP请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="timeout"></param>
        /// <param name="userAgent"></param>
        /// <param name="cookies"></param>
        /// <param name="referer"></param>
        /// <returns></returns>
        public static HttpWebResponse CreatePostHttpResponse(string url, string postData, int timeout = 60000, string userAgent = "", CookieContainer cookies = null, string referer = "")
        {
            HttpWebRequest request = null;
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                //request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            request.Referer = referer;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            //设置代理UserAgent和超时
            if (string.IsNullOrWhiteSpace(userAgent))
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.125 Safari/537.36";
            else
                request.UserAgent = userAgent;
            request.Timeout = timeout;
            request.KeepAlive = true;
            request.AllowAutoRedirect = true;

            if (cookies == null)
                cookies = new CookieContainer();
            request.CookieContainer = cookies;

            //发送POST数据  
            if (!string.IsNullOrWhiteSpace(postData))
            {
                byte[] data = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = data.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            //string[] values = request.Headers.GetValues("Content-Type");
            return request.GetResponse() as HttpWebResponse;
        }

        /// <summary>
        /// 验证证书
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns>是否验证通过</returns>
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            return false;
        }

        /// <summary>
        /// 根据response中头部的set-cookie对request中的cookie进行设置
        /// </summary>
        /// <param name="setCookie">The set cookie.</param>
        /// <param name="defaultDomain">The default domain.</param>
        /// <returns></returns>
        private static CookieCollection SetCookie(HttpWebResponse response, string defaultDomain)
        {
            try
            {
                string[] setCookie = response.Headers.GetValues("Set-Cookie");

                // there is bug in it,the datetime in "set-cookie" will be sepreated in two pieces.
                List<string> a = new List<string>(setCookie);
                for (int i = setCookie.Length - 1; i > 0; i--)
                {
                    if (a[i].Substring(a[i].Length - 3) == "GMT")
                    {
                        a[i - 1] = a[i - 1] + ", " + a[i];
                        a.RemoveAt(i);
                        i--;
                    }
                }
                setCookie = a.ToArray<string>();
                CookieCollection cookies = new CookieCollection();
                foreach (string str in setCookie)
                {
                    NameValueCollection hs = new NameValueCollection();
                    foreach (string i in str.Split(';'))
                    {
                        int index = i.IndexOf("=");
                        if (index > 0)
                            hs.Add(i.Substring(0, index).Trim(), i.Substring(index + 1).Trim());
                        else
                            switch (i)
                            {
                                case "HttpOnly":
                                    hs.Add("HttpOnly", "True");
                                    break;
                                case "Secure":
                                    hs.Add("Secure", "True");
                                    break;
                            }
                    }
                    Cookie ck = new Cookie();
                    foreach (string Key in hs.AllKeys)
                    {
                        switch (Key.ToLower().Trim())
                        {
                            case "path":
                                ck.Path = hs[Key];
                                break;
                            case "expires":
                                ck.Expires = DateTime.Parse(hs[Key]);
                                break;
                            case "domain":
                                ck.Domain = hs[Key];
                                break;
                            case "httpOnly":
                                ck.HttpOnly = true;
                                break;
                            case "secure":
                                ck.Secure = true;
                                break;
                            default:
                                ck.Name = Key;
                                ck.Value = hs[Key];
                                break;
                        }
                    }
                    if (ck.Domain == "") ck.Domain = defaultDomain;
                    if (ck.Name != "") cookies.Add(ck);
                }
                return cookies;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 遍历CookieContainer
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <returns>List of cookie</returns>
        public static Dictionary<string, string> GetAllCookies(CookieContainer cookieContainer)
        {
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
        /// <param name="cookies"></param>
        /// <returns></returns>
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
                }
            }
            return cookieContainer;
        }
    }
}
