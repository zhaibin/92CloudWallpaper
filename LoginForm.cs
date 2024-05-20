using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace _92CloudWallpaper
{
    public partial class LoginForm : Form
    {
        private Label phoneLabel;
        private TextBox phoneTextBox;
        private Label codeLabel;
        private TextBox codeTextBox;
        private Button getCodeButton;
        private Button loginButton;
        private Label errorMessageLabel;
        private Timer countdownTimer;
        private int countdown = 60;

        public LoginForm()
        {
            InitializeComponent();
            CreateUI();
        }

        private void CreateUI()
        {
            // 设置窗口大小和背景颜色
            this.Size = new Size(350, 200);
            this.BackColor = Color.White;

            // 创建并设置标签和文本框
            phoneLabel = new Label { Text = "手机号", Left = 20, Top = 20, Width = 50 };
            phoneTextBox = new TextBox { Left = 80, Top = 20, Width = 200 };
            codeLabel = new Label { Text = "验证码", Left = 20, Top = 50, Width = 50 };
            codeTextBox = new TextBox { Left = 80, Top = 50, Width = 80 };
            getCodeButton = new Button { Text = "获取验证码", Left = 180, Top = 50, Width = 100};
            loginButton = new Button { Text = "登录", Left = 120, Top = 80, Width = 100};
            errorMessageLabel = new Label { Left = 20, Top = 110, Width = 300, Height = 40, ForeColor = Color.Red };

            getCodeButton.Click += new EventHandler(GetCodeButton_Click);
            loginButton.Click += new EventHandler(LoginButton_Click);

            Controls.Add(phoneLabel);
            Controls.Add(phoneTextBox);
            Controls.Add(codeLabel);
            Controls.Add(codeTextBox);
            Controls.Add(getCodeButton);
            Controls.Add(loginButton);
            Controls.Add(errorMessageLabel);

            countdownTimer = new Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += new EventHandler(CountdownTimer_Tick);
        }

        private void GetCodeButton_Click(object sender, EventArgs e)
        {
            string phoneNumber = phoneTextBox.Text.Trim();

            if (!IsValidChinesePhoneNumber(phoneNumber))
            {
                errorMessageLabel.Text = "请输入有效的手机号。";
                return;
            }

            getCodeButton.Enabled = false;
            countdown = 60;
            countdownTimer.Start();
            // 发送验证码请求
            _ = GetVerificationCode(phoneNumber);
        }

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            string phoneNumber = phoneTextBox.Text.Trim();
            string code = codeTextBox.Text.Trim();

            if (!IsValidChinesePhoneNumber(phoneNumber))
            {
                errorMessageLabel.Text = "请输入有效的手机号。";
                return;
            }

            if (!IsValidVerificationCode(code))
            {
                errorMessageLabel.Text = "请输入4位数字验证码。";
                return;
            }

            // 发送登录请求
            int userId = await Login(phoneNumber, code);
            if (userId > 0)
            {
                this.DialogResult = DialogResult.OK;
                GlobalData.UserId = userId;
                Properties.Settings.Default.UserId = userId;
                Properties.Settings.Default.Save();

                this.Close();
            }
            else
            {
                errorMessageLabel.Text = "登录失败，请检查您的凭据。";
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdown--;
            getCodeButton.Text = $"{countdown}秒";
            if (countdown == 0)
            {
                countdownTimer.Stop();
                getCodeButton.Text = "获取验证码";
                getCodeButton.Enabled = true;
            }
        }

        private async Task GetVerificationCode(string phoneNumber)
        {
            var apiHandler = new ApiRequestHandler();
            var body = new SortedDictionary<string, object>
            {
                { "flag", 4 },
                { "phone", "+86" + phoneNumber }
            };

            var response = await apiHandler.SendApiRequestAsync("https://cnapi.levect.com/social/sms", body);
            //Console.WriteLine($"验证码返回 \n{response}");
            errorMessageLabel.Text = "请输入短信验证码";
        }

        private async Task<int> Login(string phoneNumber, string code)
        {
            var apiHandler = new ApiRequestHandler();
            var body = new SortedDictionary<string, object>
            {
                { "flag", 1 },
                { "loginType", 4 },
                { "name", phoneNumber },
                { "phoneCode", "+86" },
                { "userType", 0 },
                { "valiCode", code },
                { "wechatId", phoneNumber },
            };

            var response = await apiHandler.SendApiRequestAsync("https://cnapi.levect.com/social/loginV2", body);
            Console.WriteLine(response);

            using (JsonDocument doc = JsonDocument.Parse(response))
            {
                JsonElement root = doc.RootElement;
                JsonElement bodyElement = root.GetProperty("body");
                string status = bodyElement.GetProperty("status").ToString();
                Console.WriteLine($"status :{status}");
                if (status == "0")
                {
                    int userId = bodyElement.GetProperty("userId").GetInt32();
                    Properties.Settings.Default.UserId = userId;
                    Properties.Settings.Default.Save();
                    GlobalData.UserId = userId;

                    return GlobalData.UserId;
                }
                else
                {
                    return 0;
                }
            }
        }

        private bool IsValidChinesePhoneNumber(string phoneNumber)
        {
            return Regex.IsMatch(phoneNumber, @"^1[3-9]\d{9}$");
        }

        private bool IsValidVerificationCode(string code)
        {
            return Regex.IsMatch(code, @"^\d{4}$");
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
        }
    }
}
