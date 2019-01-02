using CGA;
using System;
using System.IO;

namespace CGACommandLine
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser();
            string content;
            using (var reader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "test.cga")))
            {
                content = reader.ReadToEnd();
            }
            var parseTree = parser.Parse(content);
            Console.WriteLine(parseTree);
            var interpreter = new Interpreter();
            var shapes = interpreter.Run(parseTree, new Axiom("a", new Box()));
            foreach (var shape in shapes)
            {
                Console.WriteLine(shape);
            }
            Console.ReadKey();
        }
    }
}
