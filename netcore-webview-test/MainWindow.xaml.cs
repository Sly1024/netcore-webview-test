using Microsoft.Web.WebView2.Core;
using System;
using System.Text.Json;
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
            webView.CoreWebView2.AddHostObjectToScript("bridge", jsBridge);
            webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.bridge = chrome.webview.hostObjects.bridge;");
            webView.CoreWebView2.WebMessageReceived += MessageReceived;

        }

        async void MessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var message = args.WebMessageAsJson;
            try
            {
                ProcessMessage(message, webView.CoreWebView2.PostWebMessageAsString);
            }
            catch (Exception e) 
            {
                Console.WriteLine("WebView2 postMessage error", e);
            }
        }

        private class PsMessage
        {
            public int msgId;
            public string action;
            public string data;
        }

        private class PsResponse
        {
            public int msgId;
            public bool success;
            public string data;
        }

        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };

        private void ProcessMessage(string message, Action<string> sendMsg)
        {
            var obj = JsonSerializer.Deserialize<PsMessage>(message, JsonSerializerOptions);
            var response = new PsResponse { msgId = obj.msgId };
            try
            {
                switch (obj.action)
                {
                    case "moveWindow":
                        var xyCoord = obj.data.Split(',');
                        if (xyCoord.Length != 2) throw new ArgumentException($"Wrong message format");
                        var x = int.Parse(xyCoord[0]);
                        var y = int.Parse(xyCoord[1]);
                        MoveWindow(x, y);
                        response.data = "OK";
                        break;
                    case "sendMessage":
                        response.data = "OK";
                        break;
                }
                response.success = true;
            }
            catch (Exception e)
            {
                response.success = false;
                response.data = e.ToString();
            }
            sendMsg(JsonSerializer.Serialize(response, JsonSerializerOptions));
        }

        public void MoveWindow(int x, int y)
        {
            Left = x;
            Top = y;
        }
    }
}
