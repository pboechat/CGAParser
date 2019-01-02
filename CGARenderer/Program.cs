using System;
using System.Windows.Forms;

namespace CGARenderer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var renderer = new CGARendererApp("Window", 640, 480, false);
            renderer.Start();
        }
    }
}

