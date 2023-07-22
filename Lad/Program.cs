using Lad;

var options = new Options(args);
if (!options.Generator.Generate()) {
	Environment.Exit(1);
}
