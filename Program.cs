using System;
using System.IO;
using System.Windows.Forms;

namespace ChromeSpeechProxy
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string strPort = args[0];
                int port;
                if (int.TryParse(strPort, out port))
                {
                    Server.SetProxyPort(port);
                }
            }
            string[] commandArgs = Environment.GetCommandLineArgs();
            if (commandArgs.Length > 0)
            {
                FileInfo fi = new FileInfo(commandArgs[0]);
                string installDir = fi.DirectoryName;
                Server.SetInstallDirectory(installDir);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
