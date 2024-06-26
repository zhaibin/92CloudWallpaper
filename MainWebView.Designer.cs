using System.Drawing;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace _92CloudWallpaper
{
    partial class MainWebView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainWebView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable; 
            this.MaximizeBox = true; // 启用最大化按钮
            this.ControlBox = true; // 显示窗口图标与关闭按钮
            this.MinimumSize = new System.Drawing.Size(1600, 900);
            this.Name = "MainWebView";
            this.Text = InfoHelper.SoftwareInfo.NameCN; // 不显示窗口名称
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.MainWebView_Load);
            //this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainWebView_MouseDown);
            this.ResumeLayout(false);
            this.Icon = Icon.FromHandle(new Bitmap(Properties.Resources.logo).GetHicon());
            // 添加关闭按钮
            //Button closeButton = new Button();
            //closeButton.Text = "关闭";
            //closeButton.Size = new Size(100, 40);
            //closeButton.Location = new Point(this.ClientSize.Width - closeButton.Width - 20, 20); // 放置在右上角
            //closeButton.Click += new EventHandler(CloseButton_Click);
            //this.Controls.Add(closeButton);

        }

        #endregion


        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
