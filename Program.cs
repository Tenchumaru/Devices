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
            Grammar grammar;
            using(var reader = File.OpenText(options.InputFilePath))
            {
                grammar = options.GrammarInput.Read(reader, options);
            }
            var table = grammar.Table;
        }
    }
}
