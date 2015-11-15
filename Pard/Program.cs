using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Pard
{
    class Program
    {
        public static readonly string Name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

        static void Main(string[] args)
        {
#if !DEBUG
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
#endif
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

            if(options.StateOutputFilePath != null)
            {
                using(var writer = new StreamWriter(options.StateOutputFilePath, false, Encoding.UTF8))
                {
                    var states = grammar.States.Select((s, i) => new { Set = s, Index = i }).ToDictionary(p => p.Set, p => p.Index);
                    foreach(var item in states)
                    {
                        writer.WriteLine();
                        writer.WriteLine("state {0}:", item.Value);
                        writer.WriteLine(item.Key.ToString(productions));
                        writer.WriteLine();
                        foreach(var pair in item.Key.Gotos)
                        {
                            writer.WriteLine("\t{0} -> {1}", pair.Key, states[pair.Value]);
                        }
                    }
                }
            }
        }

#if !DEBUG
        static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            var resourceName = typeof(Program).Namespace + '.' + new AssemblyName(args.Name).Name + ".dll";
            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                var assemblyData = new Byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }
#endif
    }
}
