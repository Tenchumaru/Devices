using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lad {
	interface IGenerator {
		void Generate(Options options, TextReader reader, TextWriter writer);
	}

	public class GeneratorException : Exception {
		public readonly int LineNumber;

		public GeneratorException(int lineNumber, string message)
				: base(message) {
			LineNumber = lineNumber;
		}
	}
}
