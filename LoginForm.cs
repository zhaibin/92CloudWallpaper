using System;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json;
using static _92CloudWallpaper.ApiRequestHandler;

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
            // 创建并设置标签和文本框
            phoneLabel = new Label { Text = "手机号", Left = 20, Top = 20, Width = 50 };
            phoneTextBox = new TextBox { Left = 80, Top = 20, Width = 200 };
            codeLabel = new Label { Text = "验证码", Left = 20, Top = 50, Width = 50 };
            codeTextBox = new TextBox { Left = 80, Top = 50, Width = 120 };
            getCodeButton = new Button { Text = "获取验证码", Left = 210, Top = 50, Width = 70 };
            loginButton = new Button { Text = "登录", Left = 120, Top = 80, Width = 100 };
            errorMessageLabel = new Label { Left = 20, Top = 110, Width = 260, Height = 40, ForeColor = Color.Red };

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

        private async void GetCodeButton_Click(object sender, EventArgs e)
        {
            getCodeButton.Enabled = false;
            countdown = 60;
            countdownTimer.Start();
            // 发送验证码请求
            await GetVerificationCode(phoneTextBox.Text);
        }

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            // 发送登录请求
            int userId = await Login(phoneTextBox.Text, codeTextBox.Text);
            if (userId > 0)
            {
                this.DialogResult = DialogResult.OK;
                GlobalData.UserId = userId;
                Properties.Settings.Default.UserId = userId;
                Properties.Settings.Default.Save();

                this.Close();
                //GlobalData.LoginFlag = 1;
                //Form1.loginMenuItem.Text = "登出";

            }
            else
            {
                errorMessageLabel.Text = "Login failed. Check your credentials.";
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdown--;
            getCodeButton.Text = $"{countdown} seconds";
            if (countdown == 0)
            {
                countdownTimer.Stop();
                getCodeButton.Text = "Get Code";
                getCodeButton.Enabled = true;
            }
        }

        private async Task GetVerificationCode(string phoneNumber)
        {
            var apiHandler = new ApiRequestHandler();
            var body = new 
            {
                flag = 4,
                phone =  "+86"+phoneNumber,
            };
            
            var response = await apiHandler.SendApiRequestAsync("https://cnapi.levect.com/social/sms", body);
            //Console.WriteLine(response);
            errorMessageLabel.Text = "请输入短信验证码";
        }

        private async Task<int> Login(string phoneNumber, string code)
        {
            var apiHandler = new ApiRequestHandler();
            var body = new
            {
                flag = 1,
                loginType = 4,
                name = phoneNumber,
                phoneCode = "+86",
                userType = 0,
                valiCode = code,
                wechatId = phoneNumber,
            };
            var response = await apiHandler.SendApiRequestAsync("https://cnapi.levect.com/social/loginV2", body);
            Console.WriteLine(response);
            //Response res = JsonConvert.DeserializeObject<Response>(response);
            var res = JsonConvert.DeserializeObject<Response>(response);
            //Console.WriteLine(response);
            if (res.Body.Status == "0")
            {
                Properties.Settings.Default.UserId = res.Body.UserId;
                Properties.Settings.Default.Save();
                GlobalData.UserId = res.Body.UserId;
                //Console.WriteLine(GlobalData.UserId);
                return GlobalData.UserId;
            }
            else
            {
                return 0;
            }
            
            
            
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}
