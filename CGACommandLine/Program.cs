using CGA;
using System;
using System.IO;

namespace CGACommandLine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var parser = new Parser();
            string source;
            using (var reader = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "test.cga")))
            {
                source = reader.ReadToEnd();
            }
            try
            {
                var parseTree = parser.Parse(source);
                Console.WriteLine(parseTree);
                var interpreter = new Interpreter();
                var shapes = interpreter.Run(parseTree, new Axiom(new Symbol("a"), new Box()));
                foreach (var shape in shapes)
                {
                    Console.WriteLine(shape);
                }
            }
            catch (FormatException e)
            {
                var location = (int)e.Data["location"];
                var contextStart = source.Substring(0, location).LastIndexOf('\n') + 1;
                var contextEnd = source.Substring(location).IndexOf('\n');
                contextEnd = contextEnd == -1 ? source.Length - location : contextEnd;
                var contextPrefix = source.Substring(contextStart, location - contextStart);
                var contextSuffix = source.Substring(location, contextEnd);
                Console.WriteLine($"ERROR: {e.Message} (line={e.Data["line"]}, location={location})");
                Console.WriteLine($"{contextPrefix}{contextSuffix}");
                Console.WriteLine($"{new String(' ', contextPrefix.Length)}\u2191{new String(' ', contextSuffix.Length)}");
            }
            Console.ReadKey();
        }
    }
}
