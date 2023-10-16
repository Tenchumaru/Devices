namespace Lad {
	public class Options {
		// Command line parameters
		public readonly string NamespaceName;
		public readonly string[] ClassAccesses;
		public readonly string[] ClassNames;
		public readonly string? InputFilePath;
		public readonly string? OutputFilePath;
		public readonly string? LineDirectivesFilePath;
		public readonly bool DotIncludesNewline;
		public readonly bool IgnoringCase;
		public readonly bool IsDebug;
		public readonly bool WantsLineNumbers;
		public readonly int? TabStop;
		public readonly NewLineOption NewLine;
		public readonly IGenerator Generator;

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
			InputFilePath = commandLineParser.ExtraArguments.FirstOrDefault();
			if (InputFilePath == "-") {
				InputFilePath = null;
			} else if (InputFilePath != null && !File.Exists(InputFilePath)) {
				Usage($"cannot find {InputFilePath}");
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
			NewLine = commandLine.NewLine;
			LineDirectivesFilePath = commandLine.LineDirectivesFilePath;
			if (LineDirectivesFilePath == null && !commandLine.SkippingLineDirectives) {
				LineDirectivesFilePath = InputFilePath;
			}
			DotIncludesNewline = commandLine.DotIncludesNewline;
			IgnoringCase = commandLine.IgnoringCase;
			IsDebug = commandLine.IsDebug;
			WantsLineNumbers = commandLine.WantsLineNumbers;
			TabStop = commandLine.TabStop;
			if (commandLine.ScannerInputType is null) {
				switch (Path.GetExtension(InputFilePath ?? "").ToLowerInvariant()) {
					case ".cs":
						Generator = new InlineGenerator(this);
						break;
					case ".l":
						Generator = new LexGenerator(this);
						break;
					default:
						Usage("cannot auto-determine input type");
						throw new InvalidOperationException();
				}
			} else {
				switch (commandLine.ScannerInputType.ToLowerInvariant()) {
					case "inline":
						Generator = new InlineGenerator(this);
						break;
					case "lex":
						Generator = new LexGenerator(this);
						break;
					default:
						Usage("unknown input type");
						throw new InvalidOperationException();
				}
			}
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
			string name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			Console.WriteLine("{0}: error: {1}", name, message);
			Console.WriteLine(Adrezdi.CommandLine.Usage<CommandLine>());
			Environment.Exit(2);
		}

		[Adrezdi.CommandLine.Usage(Epilog = @"The line-file and no-lines options are incompatible with each other.  The
class-declaration option is for lex input only.")]
		private class CommandLine {
			private const string defaultClassAccess = "public";
			private const string defaultClassName = "Scanner";

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "scanner-input-type", ShortName = 't', Usage = "the type of the scanner; one of inline and lex")]
			public string? ScannerInputType { get; set; }


			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "access", ShortName = 'a', Usage = "the access of the scanner class (default " + defaultClassAccess + ")")]
			public string ClassAccess { get; set; } = defaultClassAccess;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "class", ShortName = 'c', Usage = "the name of the scanner class (default " + defaultClassName + ")")]
			public string ClassName { get; set; } = defaultClassName;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "namespace", ShortName = 'n', Usage = "the namespace to contain the scanner class (default no namespace)")]
			public string Namespace { get; set; } = "";
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "line-file", ShortName = 'f', Usage = "emit line directives for file")]
			public string? LineDirectivesFilePath { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "all-chars", ShortName = 'r', Usage = "'.' includes new line")]
			public bool DotIncludesNewline { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "ignore-case", ShortName = 'i', Usage = "ignore case")]
			public bool IgnoringCase { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "binary", ShortName = 'b', Usage = "create an 8-bit scanner")]
			public bool IsBinary { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "debug", ShortName = 'd', Usage = "enter debug mode")]
			public bool IsDebug { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "no-lines", ShortName = 'l', Usage = "don't emit line directives")]
			public bool SkippingLineDirectives { get; set; }

			[Adrezdi.CommandLine.FlagArgument(LongName = "line-numbers", ShortName = '#', Usage = "track line numbers in a property")]
			public bool WantsLineNumbers { get; set; }
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "tab-stop", ShortName = 'p', Usage = $"the tab stop to use in the output (default none; use spaces)")]
			public int? TabStop { get; set; }
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "new-line", ShortName = 'w', Usage = $"the line separator (default POSIX on POSIX, Either on Windows)")]
			public NewLineOption NewLine { get; set; } = OperatingSystem.IsWindows() ? NewLineOption.Either : NewLineOption.POSIX;
		}

		[Flags]
		public enum NewLineOption { POSIX = 1, Windows, Either }
	}
}
