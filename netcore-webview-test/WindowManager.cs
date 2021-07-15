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
    class WindowManager
    {
        private MainWindow mainWindow;

        public WindowManager(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void MoveMainWindow(int x, int y)
        {
            mainWindow.MoveWindow(x, y);
        }
    }
}
