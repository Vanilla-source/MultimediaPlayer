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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.文件FToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.播放PToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.模式MToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.帮助HToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.listView1 = new System.Windows.Forms.ListView();
            this.listView2 = new System.Windows.Forms.ListView();
            this.listView3 = new System.Windows.Forms.ListView();
            this.listView4 = new System.Windows.Forms.ListView();
            this.listView5 = new System.Windows.Forms.ListView();
            this.panel2 = new System.Windows.Forms.Panel();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.进度条 = new System.Windows.Forms.ProgressBar();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件FToolStripMenuItem,
            this.播放PToolStripMenuItem,
            this.模式MToolStripMenuItem,
            this.帮助HToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1794, 48);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // 文件FToolStripMenuItem
            // 
            this.文件FToolStripMenuItem.Name = "文件FToolStripMenuItem";
            this.文件FToolStripMenuItem.Size = new System.Drawing.Size(143, 35);
            this.文件FToolStripMenuItem.Text = "文件（F）";
            // 
            // 播放PToolStripMenuItem
            // 
            this.播放PToolStripMenuItem.Name = "播放PToolStripMenuItem";
            this.播放PToolStripMenuItem.Size = new System.Drawing.Size(145, 35);
            this.播放PToolStripMenuItem.Text = "播放（P）";
            // 
            // 模式MToolStripMenuItem
            // 
            this.模式MToolStripMenuItem.Name = "模式MToolStripMenuItem";
            this.模式MToolStripMenuItem.Size = new System.Drawing.Size(153, 35);
            this.模式MToolStripMenuItem.Text = "模式（M）";
            // 
            // 帮助HToolStripMenuItem
            // 
            this.帮助HToolStripMenuItem.Name = "帮助HToolStripMenuItem";
            this.帮助HToolStripMenuItem.Size = new System.Drawing.Size(149, 35);
            this.帮助HToolStripMenuItem.Text = "帮助（H）";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 48);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            this.splitContainer1.Panel1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 100);
            this.splitContainer1.Panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel1_Paint);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.进度条);
            this.splitContainer1.Panel2.Controls.Add(this.statusStrip1);
            this.splitContainer1.Panel2.Controls.Add(this.listView5);
            this.splitContainer1.Panel2.Controls.Add(this.listView4);
            this.splitContainer1.Panel2.Controls.Add(this.listView3);
            this.splitContainer1.Panel2.Controls.Add(this.listView2);
            this.splitContainer1.Panel2.Controls.Add(this.listView1);
            this.splitContainer1.Size = new System.Drawing.Size(1794, 864);
            this.splitContainer1.SplitterDistance = 598;
            this.splitContainer1.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Location = new System.Drawing.Point(21, 18);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(559, 848);
            this.panel1.TabIndex = 3;
            // 
            // listView1
            // 
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(18, 18);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(1164, 63);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // listView2
            // 
            this.listView2.HideSelection = false;
            this.listView2.Location = new System.Drawing.Point(18, 87);
            this.listView2.Name = "listView2";
            this.listView2.Size = new System.Drawing.Size(1164, 63);
            this.listView2.TabIndex = 1;
            this.listView2.UseCompatibleStateImageBehavior = false;
            // 
            // listView3
            // 
            this.listView3.HideSelection = false;
            this.listView3.Location = new System.Drawing.Point(18, 156);
            this.listView3.Name = "listView3";
            this.listView3.Size = new System.Drawing.Size(1164, 63);
            this.listView3.TabIndex = 2;
            this.listView3.UseCompatibleStateImageBehavior = false;
            // 
            // listView4
            // 
            this.listView4.HideSelection = false;
            this.listView4.Location = new System.Drawing.Point(18, 225);
            this.listView4.Name = "listView4";
            this.listView4.Size = new System.Drawing.Size(1164, 63);
            this.listView4.TabIndex = 3;
            this.listView4.UseCompatibleStateImageBehavior = false;
            // 
            // listView5
            // 
            this.listView5.HideSelection = false;
            this.listView5.Location = new System.Drawing.Point(18, 294);
            this.listView5.Name = "listView5";
            this.listView5.Size = new System.Drawing.Size(1164, 63);
            this.listView5.TabIndex = 4;
            this.listView5.UseCompatibleStateImageBehavior = false;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.trackBar1);
            this.panel2.Controls.Add(this.button3);
            this.panel2.Controls.Add(this.button2);
            this.panel2.Controls.Add(this.button1);
            this.panel2.Location = new System.Drawing.Point(3, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(553, 845);
            this.panel2.TabIndex = 5;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(0, 734);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(115, 108);
            this.button1.TabIndex = 3;
            this.button1.Text = "上一首";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(110, 734);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(115, 108);
            this.button2.TabIndex = 4;
            this.button2.Text = "播放";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(220, 734);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(115, 108);
            this.button3.TabIndex = 5;
            this.button3.Text = "下一首";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripStatusLabel3});
            this.statusStrip1.Location = new System.Drawing.Point(0, 823);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1192, 41);
            this.statusStrip1.TabIndex = 5;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(128, 31);
            this.toolStripStatusLabel1.Text = "+添加文件";
            this.toolStripStatusLabel1.Click += new System.EventHandler(this.toolStripStatusLabel1_Click);
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(120, 31);
            this.toolStripStatusLabel2.Text = "-移除所选";
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(129, 31);
            this.toolStripStatusLabel3.Text = "x 清空列表";
            // 
            // 进度条
            // 
            this.进度条.BackColor = System.Drawing.Color.DodgerBlue;
            this.进度条.ForeColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.进度条.Location = new System.Drawing.Point(556, 831);
            this.进度条.Name = "进度条";
            this.进度条.Size = new System.Drawing.Size(462, 39);
            this.进度条.TabIndex = 6;
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(360, 741);
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(174, 90);
            this.trackBar1.TabIndex = 6;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1794, 912);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load_1);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private MenuStrip menuStrip1;
        private ToolStripMenuItem 文件FToolStripMenuItem;
        private ToolStripMenuItem 播放PToolStripMenuItem;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem 模式MToolStripMenuItem;
        private ToolStripMenuItem 帮助HToolStripMenuItem;
        private SplitContainer splitContainer1;
        private Panel panel1;
        private Panel panel2;
        private Button button1;
        private ListView listView5;
        private ListView listView4;
        private ListView listView3;
        private ListView listView2;
        private ListView listView1;
        private Button button3;
        private Button button2;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private ProgressBar 进度条;
        private TrackBar trackBar1;
    }
}
