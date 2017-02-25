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
        const string APP_CMD = @"C:\Windows\System32\cmd.exe";
        const string APP_CHROME = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

        const string KEY_CHROME_SPEECH_PROXY = "CHROME_SPEECH_PROXY";
        const string KEY_PROXY_PORT = "PROXY_PORT";

        const string PATH_ROOT = "/";
        const string PATH_PROXY_DATA = "/ProxyData";

        const string PATH_SPEECH_DETECTION_ABORT = "/SpeechDetectionAbort";
        const string PATH_SPEECH_DETECTION_INIT = "/SpeechDetectionInit";
        const string PATH_SPEECH_DETECTION_GET_LANGUAGES = "/SpeechDetectionGetLanguages";
        const string PATH_SPEECH_DETECTION_GET_RESULT = "/SpeechDetectionGetResult";
        const string PATH_SPEECH_DETECTION_SET_LANGUAGE = "/SpeechDetectionSetLanguage";

        const string PATH_SPEECH_SYNTHESIS_CANCEL = "/SpeechSynthesisCancel";
        const string PATH_SPEECH_SYNTHESIS_CONNECT = "/SpeechSynthesisConnect";
        const string PATH_SPEECH_SYNTHESIS_CREATE_SPEECH_SYNTHESIS_UTTERANCE = "/SpeechSynthesisCreateSpeechSynthesisUtterance";
        const string PATH_SPEECH_SYNTHESIS_GET_VOICES = "/SpeechSynthesisGetVoices";
        const string PATH_SPEECH_SYNTHESIS_PROXY_UTTERANCE = "/SpeechSynthesisProxyUtterance";
        const string PATH_SPEECH_SYNTHESIS_PROXY_VOICES = "/SpeechSynthesisProxyVoices";
        const string PATH_SPEECH_SYNTHESIS_SET_PITCH = "/SpeechSynthesisSetPitch";
        const string PATH_SPEECH_SYNTHESIS_SET_RATE = "/SpeechSynthesisSetRate";
        const string PATH_SPEECH_SYNTHESIS_SET_TEXT = "/SpeechSynthesisSetText";
        const string PATH_SPEECH_SYNTHESIS_SET_VOICE = "/SpeechSynthesisSetVoice";
        const string PATH_SPEECH_SYNTHESIS_SPEAK = "/SpeechSynthesisSpeak";

        const string TOKEN_SPEECH_DETECTION_GET_LANGUAGES = "SpeechDetectionGetLanguages:";
        const string TOKEN_SPEECH_DETECTION_GET_RESULT = "SpeechDetectionGetResult:";
        const string TOKEN_SPEECH_DETECTION_INIT = "SpeechDetectionInit:";

        const string TOKEN_SPEECH_SYNTHESIS_CANCEL = "SpeechSynthesisCancel:";
        const string TOKEN_SPEECH_SYNTHESIS_CREATE_SPEECH_SYNTHESIS_UTTERANCE = "SpeechSynthesisCreateSpeechSynthesisUtterance:";
        const string TOKEN_SPEECH_SYNTHESIS_GET_VOICES = "SpeechSynthesisGetVoices:";
        const string TOKEN_SPEECH_SYNTHESIS_IDLE = "SpeechSynthesisIdle:";
        const string TOKEN_SPEECH_SYNTHESIS_SET_PITCH = "SpeechSynthesisSetPitch:";
        const string TOKEN_SPEECH_SYNTHESIS_SET_RATE = "SpeechSynthesisSetRate:";
        const string TOKEN_SPEECH_SYNTHESIS_SET_TEXT = "SpeechSynthesisSetText:";
        const string TOKEN_SPEECH_SYNTHESIS_SPEAK = "SpeechSynthesisSpeak:";

        private HttpListener _mHttpListener = null;

        private bool _mWaitForExit = true;

        private bool _mClosing = false;

        private Thread _mThread = null;

        private string _mWebGLSpeechDetectionPluginLanguages = null; //start as null

        private List<string> _mWebGLSpeechDetectionPluginResults = new List<string>();

        private List<string> _mPendingJavaScript = new List<string>();

        private StringBuilder _mStringBuilder = new StringBuilder();

        private List<int> _mWebGLSpeechSynthesisPluginUtterances = new List<int>();

        private List<string> _mWebGLSpeechSynthesisPluginVoices = new List<string>();

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
            if (!_mClosing)
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                lblStatus.Enabled = true;
                txtPort.Enabled = true;
            }

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

            if (!_mClosing)
            {
                lblStatus.Text = "Status:";
            }
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

            if (!_mClosing)
            {
                lblChrome.Text = "Chrome: [Connected]";
            }
        }

        private void DetectedUnity()
        {
            if (_mClosing)
            {
                return;
            }

            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { DetectedUnity(); });
                return;
            }

            lblUnity.Text = "Unity: [Connected]";
        }

        private void AppendStatus(string msg, params object[] args)
        {
            if (_mClosing)
            {
                return;
            }

            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { AppendStatus(msg, args); });
                return;
            }

            lblStatus.Text += string.Format(msg, args) + Environment.NewLine;
        }

        /// <summary>
        /// Handle requests with messages from the proxy
        /// </summary>
        /// <param name="request"></param>
        private void HandleProxyData(string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                return;
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_IDLE))
            {
            }

            else if (request.StartsWith(TOKEN_SPEECH_DETECTION_GET_LANGUAGES))
            {
                string jsonData = request.Substring(TOKEN_SPEECH_DETECTION_GET_LANGUAGES.Length);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    string decoded = HttpUtility.UrlDecode(jsonData);
                    _mWebGLSpeechDetectionPluginLanguages = decoded;
                }
            }

            else if (request.StartsWith(TOKEN_SPEECH_DETECTION_GET_RESULT))
            {
                string message = request.Substring(TOKEN_SPEECH_DETECTION_GET_RESULT.Length);
                string decoded = HttpUtility.UrlDecode(message);
                _mWebGLSpeechDetectionPluginResults.Add(decoded);
            }

            else if (request.StartsWith(TOKEN_SPEECH_DETECTION_INIT))
            {
                _mWebGLSpeechDetectionPluginResults.Clear(); //clear previous results
                RunJavaScript("console.log(\"Init Complete\")");
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_CANCEL))
            {
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_CREATE_SPEECH_SYNTHESIS_UTTERANCE))
            {
                string message = request.Substring(TOKEN_SPEECH_SYNTHESIS_CREATE_SPEECH_SYNTHESIS_UTTERANCE.Length);
                int index;
                if (int.TryParse(message, out index))
                {
                    _mWebGLSpeechSynthesisPluginUtterances.Add(index);
                }
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_GET_VOICES))
            {
                string jsonData = request.Substring(TOKEN_SPEECH_SYNTHESIS_GET_VOICES.Length);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    string decoded = HttpUtility.UrlDecode(jsonData);
                    _mWebGLSpeechSynthesisPluginVoices.Add(decoded);
                }
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_SET_PITCH))
            {
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_SET_RATE))
            {
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_SET_TEXT))
            {
            }

            else if (request.StartsWith(TOKEN_SPEECH_SYNTHESIS_SPEAK))
            {
            }
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

                        if (string.IsNullOrEmpty(context.Request.Url.LocalPath))
                        {
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_ROOT))
                        {
                            _mWebGLSpeechDetectionPluginResults.Clear(); //clear previous results
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

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_PROXY_DATA))
                        {
                            try
                            {
                                DetectedChrome();
                                System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                                string encodedString = parameters["message"];
                                if (!string.IsNullOrEmpty(encodedString))
                                {
                                    byte[] data = Convert.FromBase64String(encodedString);
                                    string request = Encoding.UTF8.GetString(data);
                                    HandleProxyData(request);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                            }

                            response = GetPendingJavaScript();
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_DETECTION_GET_RESULT))
                        {
                            DetectedUnity();
                            if (_mWebGLSpeechDetectionPluginResults.Count > 0)
                            {
                                response = _mWebGLSpeechDetectionPluginResults[0];
                                _mWebGLSpeechDetectionPluginResults.RemoveAt(0);
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_DETECTION_INIT))
                        {
                            _mWebGLSpeechDetectionPluginResults.Clear(); //clear previous results
                            DetectedUnity();
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_DETECTION_ABORT))
                        {
                            _mWebGLSpeechDetectionPluginResults.Clear(); //clear previous results
                            DetectedUnity();
                            RunJavaScript("WebGLSpeechDetectionPlugin.Abort()");
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_DETECTION_GET_LANGUAGES))
                        {
                            DetectedUnity();
                            if (null == _mWebGLSpeechDetectionPluginLanguages)
                            {
                                RunJavaScript("WebGLSpeechDetectionPlugin.GetLanguages()");
                            }
                            else
                            {
                                response = _mWebGLSpeechDetectionPluginLanguages;
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_DETECTION_SET_LANGUAGE))
                        {
                            _mWebGLSpeechDetectionPluginResults.Clear(); //clear previous results
                            DetectedUnity();
                            System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                            string lang = parameters["lang"];
                            if (null != lang)
                            {
                                RunJavaScript(string.Format("WebGLSpeechDetectionPlugin.SetLanguage(\"{0}\")", lang));
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_CANCEL))
                        {
                            DetectedUnity();
                            RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.Cancel()"));
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_CONNECT))
                        {
                            DetectedUnity();
                            _mWebGLSpeechSynthesisPluginUtterances.Clear();
                        }                            

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_CREATE_SPEECH_SYNTHESIS_UTTERANCE))
                        {
                            DetectedUnity();
                            RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.CreateSpeechSynthesisUtterance()"));
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_GET_VOICES))
                        {
                            DetectedUnity();
                            RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.GetVoices()"));
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_PROXY_UTTERANCE))
                        {
                            DetectedUnity();
                            if (_mWebGLSpeechSynthesisPluginUtterances.Count > 0)
                            {
                                response = _mWebGLSpeechSynthesisPluginUtterances[0].ToString();
                                _mWebGLSpeechSynthesisPluginUtterances.RemoveAt(0);
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_PROXY_VOICES))
                        {
                            DetectedUnity();
                            if (_mWebGLSpeechSynthesisPluginVoices.Count > 0)
                            {
                                response = _mWebGLSpeechSynthesisPluginVoices[0];
                                _mWebGLSpeechSynthesisPluginVoices.RemoveAt(0);
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_SET_PITCH))
                        {
                            DetectedUnity();
                            RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.SetUtterancePitch(0, 0)"));
                            System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                            string utterance = parameters["utterance"];
                            int index;
                            string strPitch = parameters["pitch"];
                            float pitch;
                            if (int.TryParse(utterance, out index) &&
                                float.TryParse(strPitch, out pitch))
                            {
                                RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.SetUtterancePitch({0}, {1})", index, pitch));
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_SET_RATE))
                        {
                            DetectedUnity();
                            System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                            string utterance = parameters["utterance"];
                            int index;
                            string strRate = parameters["rate"];
                            float rate;
                            if (int.TryParse(utterance, out index) &&
                                float.TryParse(strRate, out rate))
                            {
                                RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.SetUtteranceRate({0}, {1})", index, rate));
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_SET_TEXT))
                        {
                            DetectedUnity();
                            System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                            string utterance = parameters["utterance"];
                            int index;
                            if (int.TryParse(utterance, out index))
                            {
                                string encoded = parameters["text"];
                                string text;
                                if (string.IsNullOrEmpty(encoded))
                                {
                                    text = string.Empty;
                                }
                                else
                                {
                                    byte[] decodedBytes = Convert.FromBase64String(encoded);
                                    text = UTF8Encoding.UTF8.GetString(decodedBytes);
                                }
                                RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.SetUtteranceText({0}, \"{1}\")", index, text));
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_SET_VOICE))
                        {
                            DetectedUnity();
                            System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                            string utterance = parameters["utterance"];
                            int index;
                            if (int.TryParse(utterance, out index))
                            {
                                string encoded = parameters["voice"];
                                string voice;
                                if (string.IsNullOrEmpty(encoded))
                                {
                                    voice = string.Empty;
                                }
                                else
                                {
                                    byte[] decodedBytes = Convert.FromBase64String(encoded);
                                    voice = UTF8Encoding.UTF8.GetString(decodedBytes);
                                }
                                RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.SetUtteranceVoice({0}, \"{1}\")", index, voice));
                            }
                        }

                        else if (context.Request.Url.LocalPath.EndsWith(PATH_SPEECH_SYNTHESIS_SPEAK))
                        {
                            DetectedUnity();
                            System.Collections.Specialized.NameValueCollection parameters = HttpUtility.ParseQueryString(context.Request.Url.Query);
                            string utterance = parameters["utterance"];
                            int index;
                            if (int.TryParse(utterance, out index))
                            {
                                RunJavaScript(string.Format("WebGLSpeechSynthesisPlugin.Speak({0})", index));
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

            if (!_mClosing)
            {
                lblChrome.Text = "Chrome: [Not Connected]";
                lblUnity.Text = "Unity: [Not Connected]";
            }

            AppendStatus("Proxy Stopped!");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _mClosing = true;

            StopProxy();
        }

        private void btnOpenChrome_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            string args = string.Format("/c start \"{0}\" {1}",
                APP_CHROME,
                string.Format("http://localhost:{0}", txtPort.Text));
            process.StartInfo = new System.Diagnostics.ProcessStartInfo(APP_CMD,
                args);
            process.Start();
        }
    }
}
