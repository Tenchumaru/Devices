using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad
{
    class Program
    {
        public static readonly string Name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

        static void Main(string[] args)
        {
#if !DEBUG
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
#endif
            try
            {
                // Parse the options and input and output file paths.
                var options = new Options(args);

                // Read the input file text.
                string inputFileText = options.InputFilePath == null
                    ? Console.In.ReadToEnd()
                    : File.ReadAllText(options.InputFilePath);

                using(var reader = new StringReader(inputFileText))
                {
                    using(TextWriter writer = options.OutputFilePath != null ? new StreamWriter(options.OutputFilePath, false, Encoding.UTF8) : Console.Out)
                        options.Generator.Generate(options, reader, writer);
                }
            }
            catch(GeneratorException gex)
            {
#if DEBUG
                Console.Error.WriteLine("{0}({1}): error: {2}", Program.Name, gex.LineNumber, gex);
#else
                Console.Error.WriteLine("{0}({1}): error: {2}", Program.Name, gex.LineNumber, gex.Message);
#endif
                Environment.ExitCode = 1;
            }
            catch(Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine("{0}: error: {1}", Program.Name, ex);
#else
                Console.Error.WriteLine("{0}: error: {1}", Program.Name, ex.Message);
#endif
                Environment.ExitCode = 1;
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
