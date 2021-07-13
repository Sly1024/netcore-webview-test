using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ws_test;

namespace netcore_webview_test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        WebSocketService wsService;
        public App()
        {
            wsService = new WebSocketService();
            wsService.WindowMoveRequest += (x, y) => {
                Dispatcher.Invoke(() => {
                    MainWindow.Left = x;
                    MainWindow.Top = y;
                });
            };
            wsService.Start();
        }
    }
}
