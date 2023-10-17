using Devices;

namespace Lad {
	public class Options : OptionsBase {
		public IGenerator Generator { get; }
		public NewLineOption NewLine { get; }
		public bool DotIncludesNewline { get; }
		public bool IgnoringCase { get; }
		public bool IsDebug { get; }
		public bool WantsLineNumbers { get; }

		public Options(string[] args) : base(typeof(CommandLine), args) {
			var commandLine = (CommandLine)commandLineBase;
			NewLine = commandLine.NewLine;
			DotIncludesNewline = commandLine.DotIncludesNewline;
			IgnoringCase = commandLine.IgnoringCase;
			IsDebug = commandLine.IsDebug;
			WantsLineNumbers = commandLine.WantsLineNumbers;
			if (commandLine.ScannerInputType is null) {
				switch (Path.GetExtension(InputFilePath ?? "").ToLowerInvariant()) {
					case ".cs":
						Generator = new InlineGenerator(this);
						break;
					case ".l":
						Generator = new LexGenerator(this);
						break;
					default:
						Usage(typeof(CommandLine), "cannot auto-determine input type");
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
						Usage(typeof(CommandLine), "unknown input type");
						throw new InvalidOperationException();
				}
			}
		}

		[Adrezdi.CommandLine.Usage(Epilog = @"The line-file and no-lines options are incompatible with each other.  The
class-declaration option is for lex input only.")]
		private class CommandLine : CommandLineBase {
			private const string defaultClassName = "Scanner";


			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "class", ShortName = 'c', Usage = "the name of the scanner class (default " + defaultClassName + ")")]
			public override string ClassName { get; set; } = defaultClassName;
			[Adrezdi.CommandLine.FlagArgument(LongName = "all-chars", ShortName = 'r', Usage = "'.' includes new line")]
			public bool DotIncludesNewline { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "ignore-case", ShortName = 'i', Usage = "ignore case")]
			public bool IgnoringCase { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "binary", ShortName = 'b', Usage = "create an 8-bit scanner")]
			public bool IsBinary { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "debug", ShortName = 'd', Usage = "enter debug mode")]
			public bool IsDebug { get; set; }
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "new-line", ShortName = 'w', Usage = $"the line separator (default POSIX on POSIX, Either on Windows)")]
			public NewLineOption NewLine { get; set; } = OperatingSystem.IsWindows() ? NewLineOption.Either : NewLineOption.POSIX;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "scanner-input-type", ShortName = 't', Usage = "the type of the scanner; one of inline and lex")]
			public string? ScannerInputType { get; set; }
			[Adrezdi.CommandLine.FlagArgument(LongName = "line-numbers", ShortName = '#', Usage = "track line numbers in a property")]
			public bool WantsLineNumbers { get; set; }
		}

		[Flags]
		public enum NewLineOption { POSIX = 1, Windows, Either }
	}
}
