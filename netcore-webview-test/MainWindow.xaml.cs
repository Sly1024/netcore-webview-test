using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;


namespace netcore_webview_test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebViewJSBridge jsBridge;

        public MainWindow()
        {
            InitializeComponent();
            jsBridge = new WebViewJSBridge(this);
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.NavigationCompleted += (s, e) => {
                webView.CoreWebView2.AddHostObjectToScript("bridge", jsBridge);
            };
            webView.CoreWebView2.WebMessageReceived += MessageReceived;
        }

        void MessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var message = args.TryGetWebMessageAsString();
            try
            {
                ProcessMessage(message);
            }
            catch (Exception) { }

            webView.CoreWebView2.PostWebMessageAsString("OK");
        }

        public void ProcessMessage(string message)
        {
            if (message.StartsWith("move"))
            {
                var xyCoord = message[4..].Split(',');
                if (xyCoord.Length != 2) throw new ArgumentException($"Wrong message format");
                var x = int.Parse(xyCoord[0]);
                var y = int.Parse(xyCoord[1]);
                MoveWindow(x, y);
            }
        }

        public void MoveWindow(int x, int y)
        {
            Left = x;
            Top = y;
        }
    }
}
