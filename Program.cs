using System;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace FinalTest
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序主入口点。
        /// 初始化 VLC 引擎并启动主窗体。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 初始化 VLC 原生库（加载 libvlc.dll）
            Core.Initialize();

            Application.Run(new MainForm());
        }
    }
}
