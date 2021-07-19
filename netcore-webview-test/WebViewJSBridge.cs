using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace netcore_webview_test
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class WebViewJSBridge
    {
        private MainWindow mainWindow;

        public WebViewJSBridge(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void MoveMainWindow(int x, int y)
        {
            mainWindow.MoveWindow(x, y);
        }

        public string SendMessage(string message)
        {
            return "OK";
        }
    }
}
