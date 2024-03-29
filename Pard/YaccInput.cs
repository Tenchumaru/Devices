﻿namespace Pard {
	public partial class YaccInput : IGrammarInput {
		public Nonterminal? StartingSymbol { get; private set; }
		private readonly List<Production> productions = new();
		private readonly List<ActionCode> codeBlocks = new();
		private Grammar.Associativity terminalAssociativity;
		private string? terminalTypeName;
		private int? precedence;
		private int subruleNumber;
		private readonly Options options;
		private readonly Dictionary<string, Terminal> terminals = new();
		private readonly Dictionary<string, Nonterminal> nonterminals = new();

		public YaccInput(Options options) => this.options = options;

		public (Nonterminal, IEnumerable<(string, int)>, IReadOnlyList<Production>, IReadOnlyList<ActionCode>) Read(TextReader reader) {
			precedence = 0;
			terminals.Clear();
			nonterminals.Clear();
			YaccInputParser parser = new(this, new YaccInputScanner(reader));
			if (parser.Parse()) {
				StartingSymbol = parser.StartingRuleName == null ?
					productions[0].Lhs :
					productions.FirstOrDefault(p => p.Lhs.Name == parser.StartingRuleName)?.Lhs ?? throw new ApplicationException("start symbol undefined");
				return (StartingSymbol, terminals.Values.Select((t) => (t.Name, t.Value)), productions, codeBlocks);
			} else {
				throw new ApplicationException("syntax error");
			}
		}

		private void CreateNonterminals(string? typeName, List<string> names) => names.ForEach(s => nonterminals[s] = new Nonterminal(s, typeName));

		private void SetTerminalParameters(Grammar.Associativity associativity, int? precedence, string? typeName) {
			terminalAssociativity = associativity;
			this.precedence = precedence;
			terminalTypeName = typeName;
		}

		private void AddTerminal(string name) {
			if (terminals.ContainsKey(name)) {
				throw new ApplicationException($"Terminal '{name}' already declared");
			}
			terminals.Add(name, new Terminal(name, terminalTypeName, terminalAssociativity, precedence));
		}

		private void AddLiteral(char ch) => terminals.Add(Terminal.FormatLiteralName(ch), new Terminal(Terminal.FormatLiteralName(ch), terminalTypeName, terminalAssociativity, precedence, ch));

		private void AddProduction(string ruleName, List<Symbol> rhs, Terminal? terminal, int lineNumber) {
			// Replace code blocks with synthesized non-terminals for rules using those code blocks.
			for (int i = 0, count = rhs.Count - 1; i < count; ++i) {
				var innerCodeBlock = rhs[i] as CodeBlockSymbol;
				if (innerCodeBlock != null) {
					string subruleName = string.Format("{0}.{1}", ruleName, ++subruleNumber);
					var subruleSymbol = new Nonterminal(subruleName, null);
					var subruleProduction = new Production(subruleSymbol, Array.Empty<Symbol>(), productions.Count, innerCodeBlock.ActionCode, lineNumber);
					productions.Add(subruleProduction);
					rhs[i] = subruleSymbol;
				}
			}
			var lastCodeBlock = rhs.LastOrDefault() as CodeBlockSymbol;
			ActionCode? actionCode = null;
			if (lastCodeBlock != null) {
				rhs.RemoveAt(rhs.Count - 1);
				actionCode = lastCodeBlock.ActionCode;
			}
			if (!nonterminals.TryGetValue(ruleName, out Nonterminal? nonterminal)) {
				nonterminal = new Nonterminal(ruleName, null);
				nonterminals.Add(ruleName, nonterminal);
			}
			productions.Add(new Production(nonterminal, rhs, productions.Count, actionCode,
				terminal != null ? terminal.Associativity : Grammar.Associativity.None,
				terminal != null ? terminal.Precedence : 0, lineNumber));
		}

		private Terminal GetTerminal(string name) {
			if (!terminals.TryGetValue(name, out Terminal? terminal)) {
				terminal = new Terminal(name, null, Grammar.Associativity.None, 0);
				terminals.Add(name, terminal);
			}
			return terminal;
		}

		private Symbol GetSymbol(string name) {
			if (terminals.TryGetValue(name, out Terminal? terminal)) {
				return terminal;
			} else if (nonterminals.TryGetValue(name, out Nonterminal? nonterminal)) {
				return nonterminal;
			} else {
				nonterminal = new Nonterminal(name, null);
				nonterminals.Add(name, nonterminal);
				return nonterminal;
			}
		}

		private Terminal GetLiteral(char ch) {
			string name = Terminal.FormatLiteralName(ch);
			if (!terminals.TryGetValue(name, out Terminal? terminal)) {
				terminal = new Terminal(name, null, Grammar.Associativity.None, 0, ch);
				terminals.Add(name, terminal);
			}
			return terminal;
		}

		private class CodeBlockSymbol : Symbol {
			public readonly ActionCode ActionCode;

			public CodeBlockSymbol(ActionCode actionCode) : base(Guid.NewGuid().ToString(), null) {
				ActionCode = actionCode;
			}
		}

		public class Token {
			public int Symbol;
			public object? Value;
			public static readonly Token End = new() { Symbol = -1 };
		}

		private enum InputState { Section1, Section2Declaration, Section2Definition }
	}
}
