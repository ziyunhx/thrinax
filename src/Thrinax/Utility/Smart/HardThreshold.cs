using System;
using System.Linq;

namespace Thrinax
{
    /// <summary>
    /// 准入规则。不过不再需要这些杂七杂八的东西了，只要给我留下前面怎么筛选的数字就好。比如floor
    /// 这里保留的都是一些暂时无法用机器优化的参数，基本上全员都是人工设置定死的
    /// </summary>
    public class HardThreshold
    {

        /// <summary>
        /// 本策略适用的MediaType
        /// </summary>
        public readonly Enums.MediaType MediaType;

        /// <summary>
        /// 本策略适用的Language
        /// </summary>
        public readonly Enums.Language Language;

        /// <summary>
        /// 第一次用a标签总结初级模式的时候，允许向上的层级数
        /// </summary>
        public readonly int LevelUpCelling_TitleABasePattern;
        public readonly int LevelUpCelling_TitleAAncestorPattern;
        public readonly double Relthreshold;
        public readonly int itemclimb;
        public readonly int itempageminvalidvalue;//最少需要多少个item页面才可以总结
        public readonly int itempagemaxvalue;//最多选取来进行归纳的item页面数
        public readonly double ListSave;//为了节省消耗在List页面归纳Pattern时保留前几个？或分数最高者的几分之几？
        public readonly double ItemSave;//同listsave
        public readonly int MaxDateLength;//规定死是因为摘要中的日期不应当被识别，而如果要加入日期距离的标准差，则计算量又太多
        public readonly int List_MinCountItem;
        public readonly double LeastTitleScore;//用于工具中，分数高于此可以不用审核

        /// <summary>
        /// 标题、作者、媒体名中的禁用词（排除掉这些词再进行可能性打分）
        /// </summary>
        public static string[] StopWords = @"更多 详细 详情 查看 点击 浏览 阅读 评论 转发 赞 回复 参与 more by from editor view read detail info news reply comment".Split();

        /// <summary>
        /// 屏蔽词，用于从Dom中提取候选节点时过滤（如果有些节点全是这些词，则忽略）
        /// </summary>
        public static string[] BanWords_NodeSelect = StopWords.Union(@"new New 分类 主题 关键词 作者 标题 预览".Split()).ToArray();

        public HardThreshold(Enums.MediaType MediaType, Enums.Language Language)
        {
            this.MediaType = MediaType;
            this.Language = Language;
            this.LevelUpCelling_TitleABasePattern = 15;
            this.LevelUpCelling_TitleAAncestorPattern = 8;
            this.Relthreshold = 0.05;//rel打分时低于此值的统一无视，记为零分
            this.itemclimb = 10;
            this.itempageminvalidvalue = 3;
            this.itempagemaxvalue = 5;
            this.ListSave = 5;
            this.ItemSave = 1 / 3;
            this.MaxDateLength = 50;
            this.List_MinCountItem = 3;
            this.LeastTitleScore = 200;
        }
        public HardThreshold()
        {
            this.LevelUpCelling_TitleABasePattern = 15;
            this.LevelUpCelling_TitleAAncestorPattern = 8;
            this.Relthreshold = 0.05;
            this.itemclimb = 10;
            this.itempageminvalidvalue = 3;
            this.itempagemaxvalue = 5;
            this.ListSave = 5;
            this.ItemSave = 2 / 3;
            this.MaxDateLength = 50;
            this.List_MinCountItem = 3;
            this.LeastTitleScore = 200;
        }
        //准入规则结束
    }
}
