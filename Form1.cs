using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;

namespace ChromeSpeechProxy
{
    public partial class Form1 : Form
    {
        const string KEY_CHROME_SPEECH_PROXY = "CHROME_SPEECH_PROXY";
        const string KEY_PROXY_PORT = "PROXY_PORT";

        private HttpListener _mHttpListener = new HttpListener();

        private bool _mWaitForExit = true;

        private Thread _mThread = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtPort.Text = GetProxyPort().ToString();
            btnStart_Click(null, null);
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            int port;
            if (int.TryParse(txtPort.Text, out port))
            {
                if (txtPort.Text != port.ToString())
                {
                    txtPort.Text = port.ToString();
                }
                SetProxyPort(port);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Enabled = false;
            txtPort.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Enabled = true;
            txtPort.Enabled = true;
        }

        private int GetProxyPort()
        {
            int result = 83;

            Microsoft.Win32.RegistryKey key;
            foreach (string name in Microsoft.Win32.Registry.CurrentUser.GetSubKeyNames())
            {
                if (name == KEY_CHROME_SPEECH_PROXY)
                {
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KEY_CHROME_SPEECH_PROXY);
                    if (null != key)
                    {
                        int.TryParse((string)key.GetValue(KEY_PROXY_PORT), out result);
                    }
                }
            }

            return result;
        }

        public void SetProxyPort(int port)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KEY_CHROME_SPEECH_PROXY);
            key.SetValue(KEY_PROXY_PORT, port.ToString());
            key.Close();
        }
    }
}
