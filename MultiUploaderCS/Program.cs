using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.Reflection;

namespace MultiUploaderCS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public static string GetApplicationVersionStr()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var sb = new StringBuilder();
            sb.Append($"{version.Major}.{version.Minor}");
            if (version.Build != 0)
            {
                sb.Append($".{version.Build}");
            }
            return sb.ToString();
        }
    }
}
