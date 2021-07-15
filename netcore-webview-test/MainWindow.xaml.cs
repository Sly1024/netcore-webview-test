using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace netcore_webview_test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowManager windowManager;

        public MainWindow()
        {
            InitializeComponent();
            windowManager = new WindowManager(this);
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2InitializationCompleted += (s, e) => {
                webView.CoreWebView2.AddHostObjectToScript("windowManager", windowManager);
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
