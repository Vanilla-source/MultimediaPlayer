using System;
using System.Windows.Forms;

namespace FinalTest
{
    /// <summary>
    /// 应用程序入口类
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// 启用视觉样式并启动主窗体 MainForm。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
