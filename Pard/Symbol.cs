using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard {
	abstract class Symbol : NamedObject {
		public readonly string TypeName;

		public Symbol(string name, string typeName)
				: base(name) {
			TypeName = typeName;
		}
	}
}
