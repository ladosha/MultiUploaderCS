using System;
using System.Windows.Forms;

namespace MultiUploaderCS
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private string Username, Password, Wiki;
        public WikiLogin login;

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            Username = textBoxUsername.Text;
            Password = textBoxPassword.Text;
            Wiki = textBoxSite.Text;

            if(string.IsNullOrWhiteSpace(Username))
            {
                ShowError("Username can not be empty");
                return;
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Password can not be empty");
                return;
            }

            if (!Wiki.EndsWith(".wikia.com") && !Wiki.EndsWith(".fandom.com"))
            {
                ShowError("Invalid domain");
                return;
            }
            if(!Wiki.StartsWith("http://") && !Wiki.StartsWith("https://"))
            {
                Wiki = "https://" + Wiki;
            }

            login = new WikiLogin(Username, Password, Wiki);

            if (checkBoxSaveCreds.Checked)
            {
                Properties.Settings.Default.Username = Username;
                Properties.Settings.Default.Password = Password;
                Properties.Settings.Default.Wiki = Wiki;
                Properties.Settings.Default.AutoLogin = true;
                Properties.Settings.Default.Save();
            }
            
            buttonLogin.Enabled = false;
            buttonLogin.Text = "Logging in...";
            bool isAutoConfirmed = login.IsUserAutoConfirmed();
            if (!isAutoConfirmed)
            {
                ShowError("Account is not autoconfirmed");
                return;
            }

            string loginResult = login.Login();
            if (loginResult == "Success")
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ShowError("Couldn't login, try again");
            }
        }

        public void ShowError(string message)
        {
            labelError.Text = "Error: " + message;
            buttonLogin.Text = "Login";
            buttonLogin.Enabled = true;
        }

        public void FillIn(string user, string pass, string wiki)
        {
            textBoxUsername.Text = user;
            textBoxPassword.Text = pass;
            textBoxSite.Text = wiki;
        }
    }
}
