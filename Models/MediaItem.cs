using System.IO;

namespace FinalTest.Models
{
    /// <summary>
    /// 媒体文件信息模型
    /// 封装单个媒体文件的核心属性：路径、标题、时长
    /// </summary>
    public class MediaItem
    {
        /// <summary>
        /// 媒体文件的完整路径（绝对路径）
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 媒体文件的显示标题（默认使用不带扩展名的文件名）
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 媒体文件的时长字符串（格式：mm:ss），由播放器在加载后更新
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// 文件扩展名（如 .mp3、.mp4），用于判断媒体类型
        /// </summary>
        public string Extension => Path.GetExtension(FilePath)?.ToLower();

        /// <summary>
        /// 判断当前文件是否为视频文件
        /// 根据常见视频扩展名进行匹配
        /// </summary>
        public bool IsVideo
        {
            get
            {
                string ext = Extension;
                return ext == ".mp4" || ext == ".avi" || ext == ".wmv" ||
                       ext == ".mov" || ext == ".mkv" || ext == ".flv" ||
                       ext == ".mpeg" || ext == ".mpg" || ext == ".webm";
            }
        }

        /// <summary>
        /// 判断当前文件是否为音频文件
        /// </summary>
        public bool IsAudio
        {
            get
            {
                string ext = Extension;
                return ext == ".mp3" || ext == ".wav" || ext == ".wma" ||
                       ext == ".flac" || ext == ".aac" || ext == ".ogg" ||
                       ext == ".m4a";
            }
        }

        /// <summary>
        /// 返回 Title 作为默认显示文本，用于 ListView/ListBox 绑定
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Title)
                ? Path.GetFileName(FilePath)
                : Title;
        }

        /// <summary>
        /// 构造函数：从文件路径创建 MediaItem
        /// 自动提取文件名称（不含扩展名）作为默认标题
        /// </summary>
        /// <param name="filePath">文件的完整路径</param>
        public MediaItem(string filePath)
        {
            FilePath = filePath;
            Title = Path.GetFileNameWithoutExtension(filePath);
            Duration = "00:00";
        }

        /// <summary>
        /// 无参构造函数（用于列表序列化等场景）
        /// </summary>
        public MediaItem() { }
    }
}
