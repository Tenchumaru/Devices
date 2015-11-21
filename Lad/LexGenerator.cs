﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lad
{
    partial class LexGenerator : IGenerator
    {
        private bool dotIncludesNewline;
        private bool ignoringCase;
        private Dictionary<string, Nfa> namedExpressions = new Dictionary<string, Nfa>();
        private bool usesBol, usesTrailingContext;
        private List<string> definitions = new List<string>();
        private List<string> usingDirectives = new List<string>();
        private List<string> activeRuleGroupNames = new List<string>();
        private List<string> actions = new List<string>();
        private string finalCodeBlock;
        private Nfa machine;
        private static readonly string[] requiredUsingDirectives =
        {
            "System",
            "System.Collections.Generic",
            "System.IO",
            "System.Text",
        };

        public void Generate(Options options, TextReader reader, TextWriter writer)
        {
            throw new NotImplementedException();
        }

        public LexGenerator()
        {
        }

        internal void Parse(Options options, TextReader reader, TextWriter writer)
        {
            dotIncludesNewline = options.DotIncludesNewline;
            ignoringCase = options.IgnoringCase;
            scanner = new Scanner(reader);
            if(!Parse())
                throw new Exception("syntax error");
            var sb = new StringBuilder();
            using(var skeleton = new StringReader(Properties.Resources.ResourceManager.GetString("Skeleton")))
            {
                // Emit the definitions.
                foreach(string definition in definitions)
                    writer.WriteLine("#define " + definition);

                // Emit the first part of the skeleton.
                for(string line = skeleton.ReadLine(); !line.EndsWith("$"); line = skeleton.ReadLine())
                    writer.WriteLine(line);

                // Emit unique additional using directives.
                var uniqueUsingDirectives = usingDirectives.Except(requiredUsingDirectives).ToArray();
                if(uniqueUsingDirectives.Length > 0)
                    writer.WriteLine("using {0};", string.Join(";using ", uniqueUsingDirectives));

                // Emit the namespace and class declarations.
                if(options.NamespaceName != null)
                    writer.WriteLine("namespace {0} {{", options.NamespaceName);
                writer.WriteLine("public partial class {0}", options.ScannerClassName);

                // Emit the next part of the skeleton.
                for(string line = skeleton.ReadLine(); !line.EndsWith("$"); line = skeleton.ReadLine())
                    writer.WriteLine(line);

                // Emit the constructor.
                writer.WriteLine("public {0}(TextReader reader)", options.ScannerClassName);

                // Emit the next part of the skeleton.
                for(string line = skeleton.ReadLine(); !line.EndsWith("$"); line = skeleton.ReadLine())
                    writer.WriteLine(line);

                // Emit the state machine and rule actions.
                string actionText = string.Join(Environment.NewLine, actions.Select((s, i) => string.Format("case {0}: {1};break;", i, s)).ToArray());
                writer.WriteLine(Properties.Resources.Prologue, 0, actionText);
                ScannerWriter.Write(machine, writer, sb, 0);

                // Emit the next part of the skeleton.
                for(string line = skeleton.ReadLine(); !line.EndsWith("$"); line = skeleton.ReadLine())
                    writer.WriteLine(line);

                // Emit the final code block.
                writer.WriteLine(finalCodeBlock);

                // Emit the last part of the skeleton.
                for(string line = skeleton.ReadLine(); line != null; line = skeleton.ReadLine())
                    writer.WriteLine(line);

                // Close the namespace.
                if(options.NamespaceName != null)
                    writer.WriteLine('}');
            }
        }

        private void ReportError(int lineNumber, string message)
        {
            throw new GeneratorException(lineNumber, message);
        }

        private void ConstructCompositeNfa(List<Nfa> list)
        {
            machine = new Nfa(list);
        }

        private void ParseOption(int lineNumber, string optionText)
        {
            throw new NotImplementedException();
        }

        private void SetRuleActions(List<Nfa> list, string codeBlock)
        {
            list.ForEach(n => n.Finish(actions.Count));
            actions.Add(codeBlock);
        }

        private void ParseDefaultOption(int lineNumber, string optionText)
        {
            throw new NotImplementedException();
        }

        private void ActivateDefaultRuleGroups()
        {
            throw new NotImplementedException();
        }

        private void ActivateAllRuleGroups()
        {
            throw new NotImplementedException();
        }

        private void ActivateRuleGroups(List<string> list)
        {
            throw new NotImplementedException();
        }

        public partial class Scanner
        {
            public int LineNumber
            {
                get { return lineNumber; }
            }

            private TextReader reader;
            private StringBuilder buffer = new StringBuilder();
            private int marker, position, lineNumber;
            private string tokenValue;
            private Scanner yy;
            private int ScanValue;
            private ScannerMode mode = ScannerMode.SectionOne;

            public Scanner(TextReader reader)
            {
                this.reader = reader;
                yy = this;
            }

            internal Token Read()
            {
                switch(mode)
                {
                case ScannerMode.SectionOne:
                    return ReadSectionOne();
                case ScannerMode.SectionTwoExpression:
                    return ReadSectionTwoExpression();
                case ScannerMode.SectionTwoClass:
                    return ReadSectionTwoClass();
                case ScannerMode.SectionTwoQuotedExpression:
                    return ReadSectionTwoQuotedExpression();
                }
                throw new Exception("unexpected scanner mode " + mode);
            }

            private Token ReadRestOfLine(int tokenSymbol)
            {
                // Read the rest of the line as the token value.
                var sb = new StringBuilder();
                for(int ch; ; )
                {
                    ch = Get();
                    if(ch < 0 || ch == '\r' || ch == '\n')
                        break;
                    sb.Append((char)ch);
                }
                return new Token { Symbol = tokenSymbol, Value = sb.ToString() };
            }

            private Token HandleNoMatch()
            {
                return new Token { Symbol = Take() };
            }

            private void Save()
            {
                marker = position;
            }

            private void Restore()
            {
                tokenValue = buffer.ToString(0, marker);
                buffer.Remove(position = 0, marker);
                marker = 0;
            }

            private int Get()
            {
                if(position >= buffer.Length)
                {
                    if(ScanValue < 0)
                        return ScanValue;
                    int ch = reader.Read();
                    if(ch < 0)
                        return ScanValue = -1;
                    if(ch == '\n')
                        ++lineNumber;
                    buffer.Append((char)ch);
                }
                ++position;
                return ScanValue = buffer[position - 1];
            }

            private int Take()
            {
                int ch = Get();
                Save();
                Restore();
                return ch;
            }

            private void ReportError(string message)
            {
                Console.Error.WriteLine(message);
            }

            private void ReportError(string format, params object[] args)
            {
                Console.Error.WriteLine(format, args);
            }

            private enum ScannerMode { SectionOne, SectionTwoExpression, SectionTwoClass, SectionTwoQuotedExpression }
        }
    }
}
