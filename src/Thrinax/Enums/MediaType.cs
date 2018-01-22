using System;
using System.ComponentModel;

namespace Thrinax.Enums
{
    /// <summary>
    /// 枚举：媒体类型
    /// </summary>

    public enum MediaType
    {
        [Description("其他")]
        Unknown = 0,        //未知
        [Description("报刊杂志")]
        PaperMagazine = 1,  //报刊
        [Description("广播电视")]
        RadioTV = 2,        //电台电视台
        [Description("新闻")]
        WebNews = 3,        //网络媒体(新闻性)
        [Description("论坛")]
        Forum = 4,          //论坛
        [Description("博客")]
        Blog = 5,           //博客
        [Description("微博")]
        Weibo = 6,          //微博
        [Description("社交网站")]
        SNS = 7,            //社交网
        [Description("电子商务")]
        eCommercial = 8,    //电子商务/供求类网站
        [Description("视频网站")]
        Video = 9,          //网络视频
        [Description("百科问答")]
        Wiki = 10,         //百科问答
        [Description("微信")]
        WebChat = 11,
        [Description("新闻项目")]
        NewsItem = 31,
        [Description("组织机构网站")]
        FrontPage = 32
    }
}
