using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Program
    {
        public static readonly string Name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

        static void Main(string[] args)
        {
            var options = new Options(args);
            IReadOnlyList<Production> productions;
            using(var reader = File.OpenText(options.InputFilePath))
            {
                productions = options.GrammarInput.Read(reader, options);
            }
            var grammar = new Grammar(productions);

            // Write the parser.
            var output = new CodeOutput();
            using(var writer = options.OutputFilePath != null ? new StreamWriter(options.OutputFilePath, false, Encoding.UTF8) : Console.Out)
                output.Write(grammar.Actions, grammar.Gotos, productions, writer, options);
        }
    }
}
