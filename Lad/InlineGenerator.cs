using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lad
{
    class InlineGenerator : IGenerator
    {
        public void Generate(Options options, TextReader reader, TextWriter writer)
        {
            bool emittingLineDirectives = options.LineDirectivesFilePath != null;
            if(emittingLineDirectives)
                writer.WriteLine("#line 1 \"{0}\"", options.LineDirectivesFilePath);
            int scannerNumber = 0;
            int lineNumber = 1;
            int acceptingRuleIndex = 0;
            var list = new List<Nfa>();
            var actions = new List<KeyValuePair<int, string>>();
            var action = new KeyValuePair<int, StringBuilder>();
            var namedExpressions = new Dictionary<string, Nfa>();
            for(string line = reader.ReadLine(); line != null; line = reader.ReadLine(), ++lineNumber)
            {
                string trimmedLine = line.TrimStart();
                if(trimmedLine.StartsWith(options.SignalComment))
                {
                    line = trimmedLine.Substring(options.SignalComment.Length).Trim();
                    if(line.StartsWith("let "))
                    {
                        string[] parts = line.Split(new[] { '=' }, 2);
                        if(parts.Length < 2)
                            throw new GeneratorException(lineNumber, "invalid named expression specification");
                        string name = parts[0].Substring(4).TrimEnd();
                        var machine = CreateMachine(lineNumber, parts[1].TrimStart(), options, namedExpressions);
                        namedExpressions.Add(name, machine);
                    }
                    else
                    {
                        if(action.Value != null)
                        {
                            actions.Add(new KeyValuePair<int, string>(action.Key, action.Value.ToString()));
                            action.Value.Length = 0;
                            action = new KeyValuePair<int, StringBuilder>(lineNumber, action.Value);
                        }
                        else
                            action = new KeyValuePair<int, StringBuilder>(lineNumber, new StringBuilder());
                        var machine = CreateMachine(lineNumber, line, options, namedExpressions);
                        if(machine != null)
                        {
                            machine.Finish(acceptingRuleIndex);
                            ++acceptingRuleIndex;
                            list.Add(machine);
                        }
                        else
                        {
                            StringBuilder sb = action.Value;
                            for(int i = 0, count = actions.Count; i < count; ++i)
                            {
                                if(emittingLineDirectives)
                                    sb.AppendFormat("case {0}:{4}#line {3} \"{2}\"{4}{1}{4}#line default{4}break;", i, actions[i].Value, options.LineDirectivesFilePath, actions[i].Key + 1, writer.NewLine);
                                else
                                    sb.AppendFormat("case {0}:{2}{1}{2}break;", i, actions[i].Value, writer.NewLine);
                            }
                            if(emittingLineDirectives)
                                writer.WriteLine("#line default");
                            writer.WriteLine(Properties.Resources.Prologue, scannerNumber, sb);
                            machine = new Nfa(list);
                            ScannerWriter.Write(machine, writer, sb, scannerNumber);
                            if(emittingLineDirectives)
                                writer.WriteLine("#line {1} \"{0}\"", options.LineDirectivesFilePath, lineNumber + 1);
                            ++scannerNumber;
                            acceptingRuleIndex = 0;
                            list = new List<Nfa>();
                            actions = new List<KeyValuePair<int, string>>();
                            action = new KeyValuePair<int, StringBuilder>();
                            namedExpressions.Clear();
                        }
                    }
                }
                else if(action.Value != null)
                    action.Value.AppendLine(line);
                else
                    writer.WriteLine(line);
            }
        }

        private static Nfa CreateMachine(int lineNumber, string line, Options options, Dictionary<string, Nfa> namedExpressions)
        {
            var scanner = new ExpressionScanner(new StringReader(line));
            var parser = new ExpressionParser(scanner, options.DotIncludesNewline, options.IgnoringCase, namedExpressions);
            try
            {
                return parser.CreateMachine();
            }
            catch(Exception ex)
            {
#if DEBUG
                throw new GeneratorException(lineNumber, ex.ToString());
#else
                throw new GeneratorException(lineNumber, ex.Message);
#endif
            }
            throw new GeneratorException(lineNumber, "failed to parse " + line);
        }
    }
}
