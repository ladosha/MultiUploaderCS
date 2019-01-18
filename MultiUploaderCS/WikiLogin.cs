using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;


namespace MultiUploaderCS
{
    public class WikiLogin
    {
        public string Username, Password, Wiki, SupportedExtensions;
        private string Token;
        public CookieContainer Cookies { get; private set; }

        public WikiLogin(string username, string password, string wiki)
        {
            Username = username;
            Password = password;
            Wiki = wiki;
            GetSupportedExtensions();
        }

        public string Login()
        {
            HttpWebRequest loginRequest;
            string URL = Wiki + "/api.php?action=login&lgname=" + Username + "&lgpassword=" + Password + "&format=json";

            try
            {
                loginRequest = HttpWebRequest.CreateHttp(URL);
                loginRequest.Method = "POST";
                loginRequest.UserAgent = "MultiUploader 0.1";
                Cookies = new CookieContainer();
                loginRequest.CookieContainer = Cookies;
                WebResponse response = loginRequest.GetResponse();
                Stream responseStream = response.GetResponseStream();
                var ResponseReader = new StreamReader(responseStream, Encoding.UTF8);
                dynamic responseObj = JsonConvert.DeserializeObject(ResponseReader.ReadToEnd());
                Token = responseObj["login"]["token"].ToString();
            }
            catch
            {
                return "Failure";
            }

            URL = Wiki + "/api.php?action=login&lgname=" + Username + "&lgpassword=" + Password + "&lgtoken=" + Token + "&format=json";
            loginRequest = HttpWebRequest.CreateHttp(URL);
            loginRequest.Method = "POST";
            loginRequest.UserAgent = "MultiUploader 0.1";
            loginRequest.CookieContainer = Cookies;

            try
            {
                var response = loginRequest.GetResponse();
                var responseStream = response.GetResponseStream();
                var ResponseReader = new StreamReader(responseStream, Encoding.UTF8);
                dynamic responseObj = JsonConvert.DeserializeObject(ResponseReader.ReadToEnd());
                var result = responseObj["login"]["result"].ToString();
                return result;
            }
            catch
            {
                return "Failure";
            }
        }

        public bool IsUserAutoConfirmed()
        {
            string URL = Wiki + "/api.php?action=query&list=users&ususers=" + Username + "&usprop=groups&format=json";
            try
            {
                HttpWebRequest groupRequest = HttpWebRequest.CreateHttp(URL);
                groupRequest.Method = "POST";
                groupRequest.UserAgent = "MultiUploader 0.1";
                var responseStream = groupRequest.GetResponse().GetResponseStream();
                var responseReader = new StreamReader(responseStream, Encoding.UTF8);
                dynamic responseObj = JsonConvert.DeserializeObject(responseReader.ReadToEnd());
                string groups = responseObj["query"]["users"][0]["groups"].ToString();
                return groups.Contains("autoconfirmed");
            }
            catch
            {
                return false;
            }
        }

        private void GetSupportedExtensions()
        {
            string URL = Wiki + "/api.php?action=query&meta=siteinfo&siprop=fileextensions&format=json";
            var request = HttpWebRequest.CreateHttp(URL);
            request.Method = "POST";
            request.UserAgent = "MultiUploader 0.1";
            try
            {
                var responseStream = request.GetResponse().GetResponseStream();
                var json = new StreamReader(responseStream, Encoding.UTF8).ReadToEnd();
                dynamic responseObj = JsonConvert.DeserializeObject(json);
                Newtonsoft.Json.Linq.JArray extensions = responseObj["query"]["fileextensions"];
                SupportedExtensions = string.Join(";", extensions.Select(x => $"*.{x["ext"].ToString()}").ToArray());
            }
            catch
            {
                SupportedExtensions = "*.png;*.gif;*.jpg;*.jpeg;*.ico;*.pdf;*.svg;*.odt;*.ods;*.odp;*.odg;*.odc;*.odf;*.odi;*.odm;*.ogg;*.ogv;*.oga";
            }
        }
    }
}
