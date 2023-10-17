#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections

namespace Devices {
	public class OptionsBase {
		// Command line parameters
		public string[] ClassAccesses { get; protected set; }
		public string[] ClassNames { get; protected set; }
		public string? InputFilePath { get; protected set; }
		public string? LineDirectivesFilePath { get; protected set; }
		public string NamespaceName { get; protected set; }
		public string? OutputFilePath { get; protected set; }

		// From the input file
		public List<string> AdditionalUsingDirectives { get; } = new();
		public List<string> DefineDirectives { get; } = new();
		protected Adrezdi.CommandLine commandLineParser = new();
		protected CommandLineBase commandLineBase;

		public OptionsBase(Type type, string[] args) {
			commandLineBase = (CommandLineBase)commandLineParser.Parse(type, args)!;
			if (commandLineParser.ExtraOptions.Any()) {
				Usage(type, "unexpected options: " + string.Join(", ", commandLineParser.ExtraOptions));
			}
			if (commandLineParser.ExtraArguments.Count > 2) {
				Usage(type, "too many file specifications");
			}
			InputFilePath = commandLineParser.ExtraArguments.FirstOrDefault();
			if (InputFilePath == "-") {
				InputFilePath = null;
			} else if (InputFilePath != null && !File.Exists(InputFilePath)) {
				Usage(type, $"cannot find {InputFilePath}");
			}
			OutputFilePath = commandLineParser.ExtraArguments.Skip(1).FirstOrDefault();
			if (OutputFilePath == "-") {
				OutputFilePath = null;
			} else {
				string? directoryPath = Path.GetDirectoryName(OutputFilePath);
				if (directoryPath != null && directoryPath.Any() && !Directory.Exists(directoryPath)) {
					Usage(type, $"cannot find {directoryPath}");
				}
			}
			ClassAccesses = commandLineBase.ClassAccess.Split('.');
			ClassNames = commandLineBase.ClassName.Split('.');
			if (ClassAccesses.Length == 1) {
				ClassAccesses = ClassNames.Select(_ => ClassAccesses.First()).ToArray();
			} else if (ClassAccesses.Length != ClassNames.Length) {
				Usage(type, "unmatched class accesses and class names");
			}
			ClassNames.ToList().ForEach((s) => CheckName(type, s, "class"));
			NamespaceName = commandLineBase.NamespaceName;
			if (NamespaceName.Any()) {
				CheckName(type, NamespaceName, "namespace");
			}
			if (!commandLineBase.SkippingLineDirectives) {
				LineDirectivesFilePath = commandLineBase.LineDirectivesFilePath ?? InputFilePath;
			}
		}

		protected static void CheckName(Type type, string name, string message) {
			if (name != null) {
				message = "invalid " + message;
				if (name == "") {
					Usage(type, message);
				}
				if (!char.IsLetter(name[0]) && name[0] != '_') {
					Usage(type, message);
				}
				if (name.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '.')) {
					Usage(type, message);
				}
			}
		}

		protected static void Usage(Type type, string message) {
			string name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
			Console.WriteLine("{0}: error: {1}", name, message);
			Console.WriteLine(Adrezdi.CommandLine.Usage(type));
			Environment.Exit(2);
		}

		public abstract class CommandLineBase {
			private const string defaultClassAccess = "public";

			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "access", ShortName = 'a', Usage = "the access of the class (default " + defaultClassAccess + ")")]
			public string ClassAccess { get; set; } = defaultClassAccess;
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "line-file", ShortName = 'f', Usage = "emit line directives for file")]
			public string? LineDirectivesFilePath { get; set; }
			[Adrezdi.CommandLine.OptionalValueArgument(LongName = "namespace", ShortName = 'n', Usage = "the namespace to contain the class (default no namespace)")]
			public string NamespaceName { get; set; } = "";
			[Adrezdi.CommandLine.FlagArgument(LongName = "no-lines", ShortName = 'l', Usage = "don't emit line directives")]
			public bool SkippingLineDirectives { get; set; }
			public abstract string ClassName { get; set; }
		}
	}
}
