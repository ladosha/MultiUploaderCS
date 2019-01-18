using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiUploaderCS
{
    public partial class OptionsForm : Form
    {
        private MainForm Main;
        public OptionsForm(MainForm main)
        {
            Main = main;
            InitializeComponent();
        }

        private void ReLogin(string newWiki)
        {
            buttonSave.Enabled = false;

            if (!newWiki.EndsWith(".wikia.com") && !newWiki.EndsWith(".fandom.com"))
            {
                ShowError("Invalid domain");
                textBoxChangeSite.SelectAll();
                return;
            }
            if (!newWiki.StartsWith("http://") && !newWiki.StartsWith("https://"))
            {
                newWiki = "https://" + newWiki;
            }

            var newLogin = new WikiLogin(Main.login.Username, Main.login.Password, newWiki);
            if (newLogin.Login() == "Success")
            {
                Main.login = newLogin;
                Main.UpdateTitle();
                Main.GetEditToken();
                Main.AddLogMessage($"Logged in as {newLogin.Username}"+Environment.NewLine);
                Main.UpdateExtensionInfo();
                if (Properties.Settings.Default.AutoLogin)
                {
                    Properties.Settings.Default.Wiki = newWiki;
                    Properties.Settings.Default.Save();
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ShowError("Couldn't login, try again");
            }
        }

        private void ShowError(string message)
        {
            labelError.Text = "Error: "+message;
            buttonSave.Enabled = true;
        }

        private void OptionsForm_Load(object sender, EventArgs e)
        {
            textBoxUsername.Text = Properties.Settings.Default.Username;
            textBoxPassword.Text = Properties.Settings.Default.Password;
            textBoxSite.Text = Properties.Settings.Default.Wiki;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(textBoxUsername.Text) &&
                !String.IsNullOrWhiteSpace(textBoxPassword.Text) &&
                !String.IsNullOrWhiteSpace(textBoxSite.Text) &&
                String.IsNullOrWhiteSpace(textBoxChangeSite.Text))
            {
                string newWiki = textBoxSite.Text;
                if (!newWiki.EndsWith(".wikia.com") && !newWiki.EndsWith(".fandom.com"))
                {
                    ShowError("Invalid domain");
                    textBoxSite.SelectAll();
                    return;
                }
                if (!newWiki.StartsWith("http://") && !newWiki.StartsWith("https://"))
                {
                    newWiki = "https://" + newWiki;
                }
                Properties.Settings.Default.Username = textBoxUsername.Text;
                Properties.Settings.Default.Password = textBoxPassword.Text;
                Properties.Settings.Default.Wiki = newWiki;
                Properties.Settings.Default.AutoLogin = true;
                Properties.Settings.Default.Save();
                Main.AddLogMessage("Updated autologin settings" + Environment.NewLine);
                DialogResult = DialogResult.OK;
                Close();
            }
            if (!String.IsNullOrWhiteSpace(textBoxChangeSite.Text))
            {
                ReLogin(textBoxChangeSite.Text);
            }
        }
    }
}
