using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace MultiUploaderCS
{
    
    public partial class MainForm : Form
    {
        private string EditToken;
        public const string Boundary = "------------------\r\n";
        public WikiLogin login;

        public MainForm()
        {
            InitializeComponent();
        }

        private async void buttonUpload_Click(object sender, EventArgs e)
        {
            buttonUpload.Enabled = false;
            string description = textBoxDescription.Text;
            string[] paths = listBoxFileNames.Items.OfType<string>().ToArray();
            foreach (string path in paths)
            {
                AddLogMessage(Boundary);
                AddLogMessage($"Uploading file: {path}...");
                string uploadResult = await Upload(path, description);
                if (uploadResult == "Success")
                {
                    AddLogMessage("OK" + Environment.NewLine); // Add confirming log message
                    AddLogMessage(login.Wiki + "/w/File:" + Path.GetFileName(path).Replace(" ", "_") + Environment.NewLine);
                    listBoxFileNames.Items.Remove(path);
                }
                else
                {
                    AddLogMessage("FAIL" + Environment.NewLine);
                    AddLogMessage(uploadResult+Environment.NewLine);
                }
            }
            buttonUpload.Enabled = true;
        }

        public void AddLogMessage(string msg)
        {
            textBoxLog.Text += msg;
        }

        public void UpdateTitle()
        {
            Text = $"MultiUploader v{Program.GetApplicationVersionStr()} [{login.Username} at {login.Wiki.Substring(login.Wiki.IndexOf("://")+3)}]";
        }

        public void UpdateExtensionInfo()
        {
            AddLogMessage("Supported file extensions: " + login.SupportedExtensions + Environment.NewLine);
            openFileDialog1.Filter = "Supported files|" + login.SupportedExtensions;

        }

        public void GetEditToken()
        {
            string URL = login.Wiki + "/api.php?action=query&prop=info&intoken=edit&titles=Foo&format=json&indexpageids=1";
            var tokenRequest = WebRequest.CreateHttp(URL);
            tokenRequest.UserAgent = "MultiUploader 0.1";
            tokenRequest.Method = "GET";
            tokenRequest.CookieContainer = login.Cookies;
            try
            {
                var response = new StreamReader(tokenRequest.GetResponse().GetResponseStream(), Encoding.UTF8).ReadToEnd();
                dynamic responseObj = JsonConvert.DeserializeObject(response);
                var pageID = responseObj["query"]["pageids"][0].ToString();
                EditToken = responseObj["query"]["pages"][pageID]["edittoken"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task<string> Upload(string path, string description) // Manually build the request
        {
            string URL = login.Wiki + "/api.php"; // Point data to /api.php
            byte[] fileBytes = File.ReadAllBytes(path);
            if (fileBytes.Length > 1048576)
            {
                return "Error: File too large";
            }
            string Boundary = "-----------------------" + DateTime.Now.Ticks.ToString("x"); // Generate boundary
            Dictionary<string, string> APIParams = new Dictionary<string, string>(); // Dictionary of API parameters
            string filename = Path.GetFileName(path);
            APIParams.Add("action", "upload"); // Add API parameters
            APIParams.Add("filename", filename);
            APIParams.Add("comment", description);
            APIParams.Add("token", EditToken);
            APIParams.Add("ignorewarnings", "1");
            APIParams.Add("format", "json");
            HttpWebRequest UploadImageRequest = WebRequest.CreateHttp(URL); // Setup request
            UploadImageRequest.Method = "POST";
            UploadImageRequest.UserAgent = "MultiUploader 0.1";
            UploadImageRequest.CookieContainer = login.Cookies; // Set cookies to those received at login
            UploadImageRequest.ContentType = "multipart/form-data; boundary=" + Boundary;
            Stream UploadImageRequestStream = await UploadImageRequest.GetRequestStreamAsync(); // Begin writing to request stream
            foreach (KeyValuePair<string, string> Entry in APIParams) // Generate binary stream for API parameters
            {
                string Data = string.Format("--{0}" + Environment.NewLine + "Content-Disposition: form-data; name=\"{1}\"" + Environment.NewLine + Environment.NewLine + "{2}" + Environment.NewLine, Boundary, Entry.Key, Entry.Value);
                byte[] DataBytes = Encoding.UTF8.GetBytes(Data); // Get binary stream for API parameter
                await UploadImageRequestStream.WriteAsync(DataBytes, 0, DataBytes.Length); // Write API parameter to the request stream
            }
            string FileType = "";
            try
            {
                FileType = MimeTypes[Path.GetExtension(path).ToLower()];
            }
            catch
            {
                FileType = "application/octet-stream";
            }
            string FileStr = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n", Boundary, "file", filename, FileType); // Header for imagedata
            byte[] FileStrBytes = Encoding.UTF8.GetBytes(FileStr); // Get binary stream for image data header
            await UploadImageRequestStream.WriteAsync(FileStrBytes, 0, FileStrBytes.Length); // Write imagedata header to the request stream

            await UploadImageRequestStream.WriteAsync(fileBytes, 0, fileBytes.Length); // Write binary stream for image to request stream
            string Ending = Environment.NewLine + "--" + Boundary + "--"; // Create end boundary
            byte[] EndingBytes = Encoding.UTF8.GetBytes(Ending); // Get binary stream for end boundary
            await UploadImageRequestStream.WriteAsync(EndingBytes, 0, EndingBytes.Length); // Write binary stream for end boundary to request stream
            UploadImageRequestStream.Close(); // Finished creating request stream
            WebResponse UploadImageRequestResponse; // Setup response
            try
            {
                UploadImageRequestResponse = UploadImageRequest.GetResponse(); // Get response
                
                StreamReader UploadImageRequestStreamReader = new StreamReader(UploadImageRequestResponse.GetResponseStream());
                string UploadImageRequestResponseString = await UploadImageRequestStreamReader.ReadToEndAsync();
                dynamic responseObj = JsonConvert.DeserializeObject(UploadImageRequestResponseString);
                string result = responseObj["upload"]?["result"]?.ToString() ??
                    "Error: "+ responseObj["error"]["info"].ToString();
                UploadImageRequestStreamReader.Close();
                UploadImageRequestResponse.Close();
                return result;
            }
            catch (Exception ex)
            {
                return "Exception: " + ex.Message;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.AutoLogin)
            {
                login = new WikiLogin(Properties.Settings.Default.Username,
                                      Properties.Settings.Default.Password,
                                      Properties.Settings.Default.Wiki);
                if (!login.IsUserAutoConfirmed() || login.Login() != "Success")
                {
                    LoginForm loginForm = new LoginForm();
                    loginForm.FillIn(login.Username, login.Password, login.Wiki);
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        login = loginForm.login;
                    }
                    else
                    {
                        Close();
                        return;
                    }
                }
                UpdateTitle();
                GetEditToken();
                AddLogMessage($"Logged in as {login.Username}" + Environment.NewLine);
                UpdateExtensionInfo();
            }
            else
            {
                LoginForm loginForm = new LoginForm();
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    login = loginForm.login;
                    UpdateTitle();
                    GetEditToken();
                    AddLogMessage($"Logged in as {login.Username}" + Environment.NewLine);
                    UpdateExtensionInfo();
                }
                else
                {
                    Close();
                }
            } 
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxFileNames.Items.Clear();
            textBoxDescription.Text = "";
            textBoxLog.Text = "Cleared all";
        }

        private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxLog.Text = "Cleared log";
        }

        private void editOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsForm optionsForm = new OptionsForm(this);
            optionsForm.ShowDialog();
        }

        private void clearOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.HelpText, "Help", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(String.Format(Properties.Resources.AboutText, Program.GetApplicationVersionStr()), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void listBoxFileNames_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                var selectedItems = listBoxFileNames.SelectedItems.OfType<string>().ToArray();
                foreach (var item in selectedItems)
                {
                    listBoxFileNames.Items.Remove(item);
                }
            }
        }

        private void buttonAddFiles_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach(var name in openFileDialog1.FileNames)
                {
                    if (!listBoxFileNames.Items.Contains(name))
                    {
                        listBoxFileNames.Items.Add(name);
                    }
                }
            }
        }

        private void buttonClearFileList_Click(object sender, EventArgs e)
        {
            listBoxFileNames.Items.Clear();
        }

        private void textBoxLog_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
        {
            {".png", "image/png"},
            {".gif", "image/gif"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".ico", "image/x-icon"},
            {".pdf", "application/pdf"},
            {".svg", "image/svg+xml"},
            {".odt", "application/vnd.oasis.opendocument.text"},
            {".ods", "application/vnd.oasis.opendocument.spreadsheet"},
            {".odp", "application/vnd.oasis.opendocument.presentation"},
            {".odg", "application/vnd.oasis.opendocument.graphics"},
            {".odc", "application/vnd.oasis.opendocument.chart"},
            {".odf", "application/vnd.oasis.opendocument.formula"},
            {".odi", "application/vnd.oasis.opendocument.image"},
            {".odm", "application/vnd.oasis.opendocument.text-master"},
            {".ogg", "audio/ogg"},
            {".ogv", "video/ogg"},
            {".oga", "audio/ogg"},
            {".txt", "text/plain"},
            {".ttf", "application/x-font-ttf"},
            {".woff", "application/x-font-woff"},
            {".woff2", "application/x-font-woff2" }
        };

        private void listBoxFileNames_DragDrop(object sender, DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string path in paths)
            {
                if (!listBoxFileNames.Items.Contains(path) && login.SupportedExtensions.Contains(Path.GetExtension(path).ToLower()))
                {
                    listBoxFileNames.Items.Add(path);
                }
            }
        }

        private void listBoxFileNames_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        
    }
}
