using System;
using System.ComponentModel;

namespace Thrinax.Enums
{
    /// <summary>
    /// Crawl result.
    /// </summary>
    public enum CrawlResult
    {
        [Description("成功")]
        Succ = 0,
        [Description("304 Not Modified")]
        NotModified = 1,
        [Description("审核通过")]
        Investigated = 2,

        [Description("匹配到空的Pattern，不再继续翻页")]
        NoItem = 3,

        /**********以下都是错误********/
        [Description("普通错误")]
        GeneralError = 5,

        //网络相关
        [Description("一般连接错误")]
        GeneralConnectError = 10,
        [Description("网络超时")]
        Timeout = 11,
        [Description("DNS出错")]
        DNSProblem = 12,
        [Description("代理出错")]
        ProxyError = 13,
        [Description("404 错误")]
        HTTP404 = 14,
        [Description("50X 错误")]
        HTTP50X = 15,
        // 页面返回字节空，区别于“真的没有结果”
        [Description("返回空字节")]
        Empty = 16,
        [Description("Url匹配错误")]
        UrlMatchError = 17,
        [Description("Url调转回首页")]
        IndexUrl = 18,
        [Description("返回内容过短")]
        ContentTooShort = 19,

        //与服务器交互过程出错
        [Description("登录失败")]
        LoginFail = 21,
        [Description("用户被禁止")]
        UserDeny = 22,
        [Description("IP被禁止")]
        IPDeny = 23,
        [Description("其它禁止访问")]
        OtherDeny = 24,
        [Description("页码被删除")]
        Deleted = 25,
        [Description("没有账户")]
        NoAccount = 26,
        [Description("验证码错误")]
        IdentifyCodeFail = 27,

        //数据抽取错误
        [Description("数据抽取错误")]
        MatchError = 30,
        [Description("获取Item Url错误")]
        UrlError = 31,  //List拿到的ItemUrl格式不正确
        [Description("获取DOM错误")]
        DOMError = 32,  //DOM模型无法解析

        //Crawl任务相关
        [Description("爬虫任务超时")]
        CrawlJobTimeout = 41,
        [Description("验证列表失败")]
        InvalidList = 42,
        [Description("不是列表")]
        NotList = 51,
        [Description("不是文章列表，文章页Url后缀以pdf等结尾")]
        NotArtical = 52,
        [Description("语言标注错误")]
        LanguageError = 53,
        [Description("网页包含iframe")]
        InIframe = 54,
        [Description("列表出现错误")]
        ListReviewFailed = 55,
        [Description("文章页出现错误")]
        ItemReviewFailed = 56,
        [Description("没有模式")]
        NullModel = 57,
        [Description("发布时间太旧过期了")]
        PubDateTooOld = 58,
    }
}
