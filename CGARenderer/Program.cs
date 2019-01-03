using CGA;
using System;
using System.IO;
using System.Windows.Forms;

namespace CGARenderer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var parser = new Parser();
            string content;
            using (var reader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "test.cga")))
            {
                content = reader.ReadToEnd();
            }
            var parseTree = parser.Parse(content);
            var interpreter = new Interpreter();
            var shapes = interpreter.Run(parseTree, new Axiom(new Symbol("a"), new Box()));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var renderer = new CGARendererApp("Renderer", 640, 480, false, shapes);
            renderer.Start();
        }
    }
}

