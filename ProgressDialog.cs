using System;
using System.Drawing;
using System.Windows.Forms;

public class ProgressDialog : Form
{
    public Label InfoLabel { get; private set; }
    public ProgressBar ProgressBar { get; private set; }

    public ProgressDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.InfoLabel = new Label
        {
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 50
        };

        this.ProgressBar = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Minimum = 0,
            Maximum = 100,
            Height = 50
        };

        this.Controls.Add(this.InfoLabel);
        this.Controls.Add(this.ProgressBar);
        this.ClientSize = new Size(400, 100);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "下载进度";
    }
}
