namespace Pard {
	public class Options {
		// Command line parameters
		public readonly string NamespaceName;
		public readonly string[] ClassAccesses;
		public readonly string[] ClassNames;
		public readonly string ScannerClassName;
		public readonly string? OutputFilePath;
		public readonly string? StateOutputFilePath;
		public readonly string? LineDirectivesFilePath;
		public readonly bool WantsTokenClass;
		public readonly bool WantsWarnings;
		public readonly IGrammarInput GrammarInput;
		private readonly string? inputFilePath;

		// From the input file
		public readonly List<string> DefineDirectives = new();
		public readonly List<string> AdditionalUsingDirectives = new();

		public Options(string[] args) {
			Adrezdi.CommandLine commandLineParser = new();
			CommandLine commandLine = commandLineParser.Parse<CommandLine>(args, Adrezdi.CommandLine.FailureBehavior.ExitWithUsage)!;
			if (commandLineParser.ExtraOptions.Any()) {
				Usage("unexpected options: " + string.Join(", ", commandLineParser.ExtraOptions));
			}
			if (commandLineParser.ExtraArguments.Skip(2).Any()) {
				Usage("too many file specifications");
			}
			inputFilePath = commandLineParser.ExtraArguments.FirstOrDefault();
			if (inputFilePath == "-") {
				inputFilePath = null;
			} else if (inputFilePath != null && !File.Exists(inputFilePath)) {
				Usage($"cannot find {inputFilePath}");
			}
			OutputFilePath = commandLineParser.ExtraArguments.Skip(1).FirstOrDefault();
			if (OutputFilePath == "-") {
				OutputFilePath = null;
			} else {
				string? directoryPath = Path.GetDirectoryName(OutputFilePath);
				if (directoryPath != null && directoryPath.Any() && !Directory.Exists(directoryPath)) {
					Usage($"cannot find {directoryPath}");
				}
			}
			ClassAccesses = commandLine.ClassAccess.Split('.');
			ClassNames = commandLine.ClassName.Split('.');
			if (ClassAccesses.Length == 1) {
				ClassAccesses = ClassNames.Select(_ => ClassAccesses.First()).ToArray();
			} else if (ClassAccesses.Length != ClassNames.Length) {
				Usage("unmatched class accesses and class names");
			}
			ClassNames.ToList().ForEach((s) => CheckName(s, "class"));
			NamespaceName = commandLine.Namespace;
			if (NamespaceName.Any()) {
				CheckName(NamespaceName, "namespace");
			}
			ScannerClassName = commandLine.ScannerClassName;
			CheckName(ScannerClassName, "scanner class");
			if (commandLine.WantsStates) {
				if (commandLine.StateOutputFilePath != null) {
					StateOutputFilePath = commandLine.StateOutputFilePath;
				} else if (OutputFilePath != null) {
					StateOutputFilePath = OutputFilePath + ".txt";
				} else {
					Usage("cannot determine states output file path");
				}
			} else {
				StateOutputFilePath = commandLine.StateOutputFilePath;
			}
			LineDirectivesFilePath = commandLine.LineDirectivesFilePath;
			if (LineDirectivesFilePath == null && !commandLine.SkippingLineDirectives) {
				LineDirectivesFilePath = inputFilePath;
			}
			WantsTokenClass = commandLine.WantsTokenClass;
			WantsWarnings = commandLine.WantsWarnings;
			string grammarType = string.IsNullOrWhiteSpace(commandLine.GrammarType) ?
				Path.GetExtension(inputFilePath ?? "").ToLowerInvariant() :
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
					Usage("unknown input type");
					throw new NotImplementedException();
			}
		}

		public TextReader OpenReader() {
			if (inputFilePath == null) {
				return Console.In;
			}
			return new StreamReader(inputFilePath);
		}

		private static void CheckName(string name, string message) {
			if (name != null) {
				message = "invalid " + message;
				if (name == "") {
					Usage(message);
				}
				if (!char.IsLetter(name[0]) && name[0] != '_') {
					Usage(message);
				}
				if (name.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '.')) {
					Usage(message);
				}
			}
		}

		private static void Usage(string message) {
			Console.WriteLine("{0}: error: {1}", Program.Name, message);
			Console.WriteLine();
			Console.WriteLine(Adrezdi.CommandLine.Usage<CommandLine>());
			Environment.Exit(2);
		}

		[Adrezdi.CommandLine.Usage(Epilog = @"The line-file and no-lines options are incompatible with each other.  Line directives are
not available for XML grammars.  To specify an inner class for the parser, concatenate the class names with a dot.  Do the same for
the accesses of the classes if they differ.")]
		class CommandLine {
			private const string defaultClassAccess = "public";
			private const string defaultClassName = "Parser";
			private const string defaultScannerClassName = "Scanner";

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "grammar-type", ShortName = 't', Usage = "the type of the grammar; one of xml and yacc")]
			public string? GrammarType { get; set; }
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "access", ShortName = 'a', Usage = "the access of the parser class (default " + defaultClassAccess + ")")]
			public string ClassAccess { get; set; } = defaultClassAccess;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "class", ShortName = 'c', Usage = "the name of the parser class (default " + defaultClassName + ")")]
			public string ClassName { get; set; } = defaultClassName;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "namespace", ShortName = 'n', Usage = "the namespace to contain the parser class (default no namespace)")]
			public string Namespace { get; set; } = "";

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "scanner-class", ShortName = 's', Usage = "the name of the scanner class (default " + defaultScannerClassName + ")")]
			public string ScannerClassName { get; set; } = defaultScannerClassName;

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "state-output-file", ShortName = 'o', Usage = "the path of the state output file; assumes -v")]
			public string? StateOutputFilePath { get; set; }

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "line-file", ShortName = 'f', Usage = "emit line directives for file")]
			public string? LineDirectivesFilePath { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "no-lines", ShortName = 'l', Usage = "don't emit line directives")]
			public bool SkippingLineDirectives { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "generate-token", ShortName = 'g', Usage = "create a Token class")]
			public bool WantsTokenClass { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "verbose", ShortName = 'v', Usage = "create a state output file (default outputFilePath.txt)")]
			public bool WantsStates { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "warnings", ShortName = 'w', Usage = "provide all warnings (noisy)")]
			public bool WantsWarnings { get; set; }
		}
	}
}
