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
        static void Main(string[] args)
        {
            var grammar = new Grammar();
            using(var reader = File.OpenText(args[0]))
            {
                var input = new XmlInput();
                input.Read(reader, grammar);
            }
            var table = grammar.ConstructTable();
        }
    }
}
