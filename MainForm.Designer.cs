using System;
using System.Drawing;
using System.Windows.Forms;
using FinalTest.Models;
using LibVLCSharp.WinForms;

namespace FinalTest
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ===== 构建所有控件（设计器和运行时共用） =====
            CreateAllControls();
        }
    }
}
