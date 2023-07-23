using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad {
	public class Options {
		// Command line parameters
		public readonly string NamespaceName;
		public readonly string ScannerClassName;
		public readonly string InputFilePath;
		public readonly string OutputFilePath;
		public readonly string SignalComment;
		public readonly string LineDirectivesFilePath;
		public readonly bool DotIncludesNewline;
		public readonly bool IgnoringCase;
		public readonly bool WantsLineNumberTracking;
		internal readonly IGenerator Generator;
		private const string defaultSignalComment = "//**";

		// From the input file
		public readonly List<string> DefineDirectives = new List<string>();
		public readonly List<string> AdditionalUsingDirectives = new List<string>();

		public Options(string[] args) {
			var commandLineParser = new Adrezdi.CommandLine();
			var commandLine = commandLineParser.Parse<CommandLine>(args, Adrezdi.CommandLine.FailureBehavior.ExitWithUsage);
			if (commandLineParser.ExtraOptions.Any())
				Usage("unexpected options: " + String.Join(", ", commandLineParser.ExtraOptions));
			if (commandLineParser.ExtraArguments.Skip(2).Any())
				Usage("too many file specifications");
			InputFilePath = commandLineParser.ExtraArguments.FirstOrDefault();
			if (InputFilePath == "-")
				InputFilePath = null;
			OutputFilePath = commandLineParser.ExtraArguments.Skip(1).FirstOrDefault();
			if (OutputFilePath == "-")
				OutputFilePath = null;
			NamespaceName = commandLine.NamespaceName;
			CheckName(NamespaceName, "namespace");
			ScannerClassName = commandLine.ScannerClassName ?? "Scanner";
			CheckName(ScannerClassName, "scanner class name");
			SignalComment = commandLine.SignalComment ?? defaultSignalComment;
			LineDirectivesFilePath = commandLine.LineDirectivesFilePath;
			if (LineDirectivesFilePath == null && !commandLine.SkippingLineDirectives)
				LineDirectivesFilePath = InputFilePath;
			DotIncludesNewline = commandLine.DotIncludesNewline;
			IgnoringCase = commandLine.IgnoringCase;
			WantsLineNumberTracking = commandLine.WantsLineNumberTracking;
			if (String.IsNullOrWhiteSpace(commandLine.ScannerInputType)) {
				switch (Path.GetExtension(InputFilePath ?? "").ToLowerInvariant()) {
					case ".cs":
						Generator = new InlineGenerator();
						break;
					case ".l":
						Generator = new LexGenerator();
						break;
					default:
						Usage("cannot auto-determine input type");
						break;
				}
			} else {
				switch ((commandLine.ScannerInputType ?? "").ToLowerInvariant()) {
					case "inline":
						Generator = new InlineGenerator();
						break;
					case "lex":
						Generator = new LexGenerator();
						break;
					default:
						Usage("unknown input type");
						break;
				}
			}
		}

		private static void CheckName(string name, string message) {
			message = "invalid " + message;
			if (name != null) {
				if (name == "")
					Usage(message);
				if (!Char.IsLetter(name[0]) && name[0] != '_')
					Usage(message);
				if (name.Any(c => !Char.IsLetterOrDigit(c) && c != '_' && c != '.'))
					Usage(message);
			}
		}

		private static void Usage(string message) {
			if (message != null)
				Console.WriteLine("{0}: error: {1}", Program.Name, message);
			Adrezdi.CommandLine.Usage<CommandLine>();
			Environment.Exit(2);
		}

		[Adrezdi.CommandLine.Usage(Epilog = @"The line-file and no-lines options are incompatible with each other.  The
namespace and class-name options are for lex input only.  The signal-comment
option is for inline input only.")]
		class CommandLine {
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "scanner-input-type", ShortName = 't', Usage = "the type of the scanner; one of inline and lex")]
			public string ScannerInputType { get; set; }

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "namespace", ShortName = 'n', Usage = "the namespace into which to put the class")]
			public string NamespaceName { get; set; }

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "class-name", ShortName = 'c', Usage = "the name of the scanner class")]
			public string ScannerClassName { get; set; }

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "line-file", ShortName = 'f', Usage = "emit line directives for file")]
			public string LineDirectivesFilePath { get; set; }

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "signal-comment", ShortName = 's', Usage = "lexical section signal comment (default " + defaultSignalComment + ")")]
			public string SignalComment { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "all-chars", ShortName = 'a', Usage = "'.' includes newline")]
			public bool DotIncludesNewline { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "ignore-case", ShortName = 'i', Usage = "ignore case")]
			public bool IgnoringCase { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "no-lines", ShortName = 'l', Usage = "don't emit line directives")]
			public bool SkippingLineDirectives { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "line-numbers", ShortName = '#', Usage = "track line numbers in a property")]
			public bool WantsLineNumberTracking { get; set; }
		}
	}
}
