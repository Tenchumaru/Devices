using Devices;

namespace Pard {
	public class Options : OptionsBase {
		public IGrammarInput GrammarInput { get; }
		public bool WantsTokenClass { get; }
		public bool WantsWarnings { get; }
		public string ScannerClassName { get; }
		public string? StateOutputFilePath { get; }

		public Options(string[] args) : base(typeof(CommandLine), args) {
			var commandLine = (CommandLine)commandLineBase;
			ScannerClassName = commandLine.ScannerClassName;
			CheckName(typeof(CommandLine), ScannerClassName, "scanner class");
			if (commandLine.WantsStates) {
				if (commandLine.StateOutputFilePath != null) {
					StateOutputFilePath = commandLine.StateOutputFilePath;
				} else if (OutputFilePath != null) {
					StateOutputFilePath = OutputFilePath + ".txt";
				} else {
					Usage(typeof(CommandLine), "cannot determine states output file path");
				}
			} else {
				StateOutputFilePath = commandLine.StateOutputFilePath;
			}
			WantsTokenClass = commandLine.WantsTokenClass;
			WantsWarnings = commandLine.WantsWarnings;
			string grammarType = string.IsNullOrWhiteSpace(commandLine.GrammarType) ?
				Path.GetExtension(InputFilePath ?? "").ToLowerInvariant() :
				(commandLine.GrammarType ?? "").ToLowerInvariant();
			switch (grammarType) {
				case ".xml":
				case "xml":
					GrammarInput = new XmlInput(this);
					LineDirectivesFilePath = null;
					break;
				case ".y":
				case "yacc":
					GrammarInput = new YaccInput(this);
					break;
				default:
					Usage(typeof(CommandLine), "unknown input type");
					throw new NotImplementedException();
			}
		}

		[Adrezdi.CommandLine.Usage(Epilog = @"The line-file and no-lines options are incompatible with each other.  Line directives are
not available for XML grammars.  To specify an inner class for the parser, concatenate the class names with a dot.  Do the same for
the namespaces and, if they differ, the accesses of the classes.")]
		class CommandLine : CommandLineBase {
			private const string defaultClassName = "Parser";
			private const string defaultScannerClassName = "Scanner";

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "class", ShortName = 'c', Usage = "the name of the parser class (default " + defaultClassName + ")")]
			public override string ClassName { get; set; } = defaultClassName;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "grammar-type", ShortName = 't', Usage = "the type of the grammar; one of xml and yacc")]
			public string? GrammarType { get; set; }
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "scanner-class", ShortName = 's', Usage = "the name of the scanner class (default " + defaultScannerClassName + ")")]
			public string ScannerClassName { get; set; } = defaultScannerClassName;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "state-output-file", ShortName = 'o', Usage = "the path of the state output file; assumes -v")]
			public string? StateOutputFilePath { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "verbose", ShortName = 'v', Usage = "create a state output file (default outputFilePath.txt)")]
			public bool WantsStates { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "generate-token", ShortName = 'g', Usage = "create a Token class")]
			public bool WantsTokenClass { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "warnings", ShortName = 'w', Usage = "provide all warnings (noisy)")]
			public bool WantsWarnings { get; set; }
		}
	}
}
