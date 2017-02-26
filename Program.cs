﻿using System;
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
                    Form1.SetProxyPort(port);
                }
            }
            string installDir = Directory.GetCurrentDirectory();
            Form1.SetInstallDirectory(installDir);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
