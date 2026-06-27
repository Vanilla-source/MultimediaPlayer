using System.ComponentModel;

namespace FinalTest.Models
{
    /// <summary>
    /// 播放模式枚举
    /// 定义播放器支持的四种播放模式
    /// </summary>
    public enum PlayMode
    {
        /// <summary>
        /// 顺序播放：按列表顺序依次播放，播完最后一首后停止
        /// </summary>
        [Description("顺序播放")]
        Sequential,

        /// <summary>
        /// 单曲循环：重复播放当前曲目
        /// </summary>
        [Description("单曲循环")]
        SingleLoop,

        /// <summary>
        /// 列表循环：列表播完后自动回到第一首继续播放
        /// </summary>
        [Description("列表循环")]
        ListLoop,

        /// <summary>
        /// 随机播放：随机选择下一首播放
        /// </summary>
        [Description("随机播放")]
        Shuffle
    }
}
