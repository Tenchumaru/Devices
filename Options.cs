using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pard
{
    class Options
    {
        // Command line parameters
        public readonly string NamespaceName;
        public readonly string ParserClassName = "Parser";
        public readonly string ScannerClassName = "IScanner";
        public readonly string InputFilePath;
        public readonly string OutputFilePath;
        public readonly string StateOutputFilePath;
        public readonly IGrammarInput GrammarInput;

        // From the input file
        public readonly List<string> DefineDirectives = new List<string>();
        public readonly List<string> AdditionalUsingDirectives = new List<string>();

        public Options(string[] args)
        {
            var commandLineParser = new Adrezdi.CommandLine();
            var commandLine = commandLineParser.Parse<CommandLine>(args, true);
            if(commandLineParser.ExtraOptions.Any())
                Usage("unexpected options");
            if(commandLineParser.ExtraArguments.Skip(2).Any())
                Usage("too many file specifications");
            InputFilePath = commandLineParser.ExtraArguments.FirstOrDefault();
            if(InputFilePath == "-")
                InputFilePath = null;
            OutputFilePath = commandLineParser.ExtraArguments.Skip(1).FirstOrDefault();
            if(OutputFilePath == "-")
                OutputFilePath = null;
            NamespaceName = commandLine.NamespaceName;
            CheckName(NamespaceName, "namespace");
            ParserClassName = commandLine.ParserClassName ?? "Parser";
            CheckName(ParserClassName, "parser class name");
            ScannerClassName = commandLine.ScannerClassName ?? "IScanner";
            CheckName(ScannerClassName, "scanner class name");
            StateOutputFilePath = commandLine.WantsStates ? commandLine.StateOutputFilePath ?? OutputFilePath + ".txt" : null;
            if(StateOutputFilePath == ".txt")
                Usage("cannot determine states output file path");
            if(String.IsNullOrWhiteSpace(commandLine.GrammarInputType))
            {
                switch(Path.GetExtension(InputFilePath ?? "").ToLowerInvariant())
                {
                case ".xml":
                    GrammarInput = new XmlInput();
                    break;
                case ".y":
                    GrammarInput = new YaccInput();
                    break;
                default:
                    Usage("cannot auto-determine input type");
                    break;
                }
            }
            else
            {
                switch((commandLine.GrammarInputType ?? "").ToLowerInvariant())
                {
                case "xml":
                    GrammarInput = new XmlInput();
                    break;
                case "yacc":
                    GrammarInput = new YaccInput();
                    break;
                default:
                    Usage("unknown input type");
                    break;
                }
            }
        }

        private static void CheckName(string name, string message)
        {
            message = "invalid " + message;
            if(name != null)
            {
                if(name == "")
                    Usage(message);
                if(!Char.IsLetter(name[0]) && name[0] != '_')
                    Usage(message);
                if(name.Any(c => !Char.IsLetterOrDigit(c) && c != '_' && c != '.'))
                    Usage(message);
            }
        }

        private static void Usage(string message)
        {
            if(message != null)
                Console.WriteLine("{0}: error: {1}", Program.Name, message);
            var commandLineParser = new Adrezdi.CommandLine();
            commandLineParser.Usage<CommandLine>();
            Environment.Exit(2);
        }

        class CommandLine
        {
            [Adrezdi.CommandLine.ValueArgument(LongName = "grammar-input-type", ShortName = 't', Usage = "the type of the grammar; one of xml and yacc")]
            public string GrammarInputType { get; set; }

            [Adrezdi.CommandLine.ValueArgument(LongName = "namespace", ShortName = 'n', Usage = "the namespace into which to put the classes")]
            public string NamespaceName { get; set; }

            [Adrezdi.CommandLine.ValueArgument(LongName = "parser-class-name", ShortName = 'p', Usage = "the name of the parser class")]
            public string ParserClassName { get; set; }

            [Adrezdi.CommandLine.ValueArgument(LongName = "scanner-class-name", ShortName = 's', Usage = "the name of the scanner class")]
            public string ScannerClassName { get; set; }

            [Adrezdi.CommandLine.ValueArgument(LongName = "state-output-file", ShortName = 'o', Usage = "the path of the state output file; assumes -v")]
            public string StateOutputFilePath { get; set; }

            [Adrezdi.CommandLine.FlagArgument(LongName = "verbose", ShortName = 'v', Usage = "create a state output file (default outputFilePath.txt)")]
            public bool WantsStates { get; set; }
        }
    }
}
