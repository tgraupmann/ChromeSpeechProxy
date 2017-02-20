using System;
using System.Collections.Generic;
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

        private HttpListener _mHttpListener = null;

        private bool _mWaitForExit = true;

        private Thread _mThread = null;

        private List<string> _mWebGLSpeechDetectionPluginResults = new List<string>();

        private List<string> _mPendingJavaScript = new List<string>();

        private StringBuilder _mStringBuilder = new StringBuilder();

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

            StartProxy();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Enabled = true;
            txtPort.Enabled = true;

            StopProxy();
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

        private void SetProxyPort(int port)
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KEY_CHROME_SPEECH_PROXY);
            key.SetValue(KEY_PROXY_PORT, port.ToString());
            key.Close();
        }

        private void StartProxy()
        {
            SetStatus("Starting Proxy...");

            try
            {
                _mWaitForExit = true;

                int port;
                if (int.TryParse(txtPort.Text, out port))
                {
                    _mHttpListener = new HttpListener();

                    _mHttpListener.Prefixes.Add(string.Format("http://*:{0}/", port));
                    _mHttpListener.Start();

                    ThreadStart ts = new ThreadStart(WorkerThread);
                    _mThread = new Thread(ts);
                    _mThread.Start();

                    AppendStatus("Proxy Started!");
                }
                else
                {
                    AppendStatus("Port is Invalid!");
                }
            }
            catch (Exception ex)
            {
                AppendStatus("Failed to start listener: {0}", ex);
            }
        }

        private void SetStatus(string msg, params object[] args)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { SetStatus(msg, args); });
                return;
            }

            lblStatus.Text = "Status:";
            AppendStatus(Environment.NewLine);
            AppendStatus(msg, args);
            AppendStatus(Environment.NewLine);
        }

        private void DetectedChrome()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { DetectedChrome(); });
                return;
            }

            lblChrome.Text = "Chrome: [Connected]";
        }

        private void DetectedUnity()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { DetectedUnity(); });
                return;
            }

            lblUnity.Text = "Unity: [Connected]";
        }

        private void AppendStatus(string msg, params object[] args)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { AppendStatus(msg, args); });
                return;
            }

            lblStatus.Text += string.Format(msg, args) + Environment.NewLine;
        }

        private void WorkerThread()
        {
            // keep listening and send a response
            while (_mWaitForExit)
            {
                if (null != _mHttpListener)
                {
                    HttpListenerContext context = null;
                    try
                    {
                        context = _mHttpListener.GetContext();

                        string response = "";

                        if (context.Request.Url.LocalPath.EndsWith("/GetResult"))
                        {
                            DetectedUnity();
                            if (_mWebGLSpeechDetectionPluginResults.Count > 0)
                            {
                                response = _mWebGLSpeechDetectionPluginResults[0];
                                _mWebGLSpeechDetectionPluginResults.RemoveAt(0);
                            }
                        }
                        else if (context.Request.Url.LocalPath.EndsWith("/ProxyData"))
                        {
                            try
                            {
                                DetectedChrome();
                                System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                                string encodedString = parameters["message"];
                                if (null != encodedString)
                                {
                                    byte[] data = Convert.FromBase64String(encodedString);
                                    string decodedString = Encoding.UTF8.GetString(data);
                                    if (!string.IsNullOrEmpty(decodedString))
                                    {

                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }

                            response = GetPendingJavaScript();
                        }
                        else if (context.Request.Url.LocalPath.EndsWith("/Abort"))
                        {
                            DetectedUnity();
                            RunJavaScript("WebGLSpeechDetectionPlugin.Abort()");
                        }
                        else if (context.Request.Url.LocalPath.EndsWith("/SetLanguage"))
                        {
                            DetectedUnity();
                            System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                            string lang = parameters["lang"];
                            if (null != lang)
                            {
                                RunJavaScript(string.Format("WebGLSpeechDetectionPlugin.SetLanguage(\"{0}\")", lang));
                            }
                        }
                        else
                        {
                            DetectedChrome();
                            try
                            {
                                using (System.IO.StreamReader sr = new System.IO.StreamReader("proxy.html"))
                                {
                                    response = sr.ReadToEnd().Replace("__PROXY_PORT__", txtPort.Text);
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }

                        byte[] bytes = UTF8Encoding.UTF8.GetBytes(response);
                        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        context.Response.OutputStream.Flush();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("WorkerThread: HTTPListener exception={0}", ex);
                    }
                    finally
                    {
                        try
                        {
                            // close the connection
                            if (null != context)
                            {
                                context.Response.Close();
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
                Thread.Sleep(0);
            }

            AppendStatus("Worker Exited!");
        }

        private void RunJavaScript(string js)
        {
            _mPendingJavaScript.Add(js);
        }

        private string GetPendingJavaScript()
        {
            while (_mPendingJavaScript.Count > 0)
            {
                _mStringBuilder.AppendLine(_mPendingJavaScript[0]);
                _mPendingJavaScript.RemoveAt(0);
            }

            string result = _mStringBuilder.ToString();

            if (_mStringBuilder.Length > 0)
            {
                _mStringBuilder.Remove(0, _mStringBuilder.Length);
            }

            return result;
        }

        private void StopProxy()
        {
            SetStatus("Stopping Proxy...");

            _mWaitForExit = false;

            _mHttpListener.Abort();

            lblChrome.Text = "Chrome: [Not Connected]";
            lblUnity.Text = "Unity: [Not Connected]";

            AppendStatus("Proxy Stopped!");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            StopProxy();
        }
    }
}
