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
            Grammar grammar;
            using(var reader = File.OpenText(args[0]))
            {
                var input = new XmlInput();
                grammar = input.Read(reader);
            }
            var table = grammar.Table;
        }
    }
}
