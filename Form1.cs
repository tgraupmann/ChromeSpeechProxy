using System;
using System.Windows.Forms;

namespace ChromeSpeechProxy
{
    public partial class Form1 : Form, IForm
    {
        /// <summary>
        /// Proxy server logic
        /// </summary>
        private Server _mServer = null;

        public Form1()
        {
            InitializeComponent();
        }

        public void AppendStatusText(string text)
        {
            RunOnMainThread(() =>
            {
                lblStatus.Text += text;
            });
        }

        public void CloseForm()
        {
            RunOnMainThread(() =>
            {
                Close();
            });
        }

        public void DisplayUIStartProxy()
        {
            RunOnMainThread(() =>
            {
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                lblStatus.Enabled = false;
                txtPort.Enabled = false;
            });
        }

        public void DisplayUIStopProxy()
        {
            RunOnMainThread(() =>
            {
                if (!_mServer.IsClosing())
                {
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    lblStatus.Enabled = true;
                    txtPort.Enabled = true;
                }
            });
        }

        private void OnClickButtonCloseChrome(object sender, EventArgs e)
        {
            _mServer.CloseChrome();
        }

        private void OnClickButtonOpenChrome(object sender, EventArgs e)
        {
            _mServer.OpenChrome();
        }

        private void OnClickButtonStart(object sender, EventArgs e)
        {
            DisplayUIStartProxy();
            _mServer.StartProxy();
        }

        private void OnClickButtonStop(object sender, EventArgs e)
        {
            DisplayUIStopProxy();
            _mServer.StopProxy();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _mServer.SetClosing();

            _mServer.StopProxy();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _mServer = new Server(this);

            txtPort.Text = Server.GetProxyPort().ToString();
            OnClickButtonStart(null, null);
        }

        private void OnTextChangedTxtPort(object sender, EventArgs e)
        {
            int port;
            if (int.TryParse(txtPort.Text, out port))
            {
                if (txtPort.Text != port.ToString())
                {
                    txtPort.Text = port.ToString();
                }
                Server.SetProxyPort(port);
            }
        }

        public void RunOnMainThread(Action action)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { RunOnMainThread(action); });
                return;
            }
            action.Invoke();
        }

        public void SetChromeConnectedText(string text)
        {
            RunOnMainThread(() =>
            {
                lblChrome.Text = text;
            });
        }

        public void SetPortText(string text)
        {
            RunOnMainThread(() =>
            {
                txtPort.Text = text;
            });
        }

        public void SetStatusText(string text)
        {
            RunOnMainThread(() =>
            {
                lblStatus.Text = text;
            });
        }

        public void SetUnityConnectedText(string text)
        {
            RunOnMainThread(() =>
            {
                lblUnity.Text = text;
            });
        }
       
    }
}
