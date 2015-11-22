using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pard
{
    public partial class YaccInput : IGrammarInput
    {
        private List<string> defineDirectives;
        private List<string> usingStatements;
        private List<Production> productions = new List<Production>();
        private Grammar.Associativity terminalAssociativity;
        private string terminalTypeName;
        private int precedence;
        private HashSet<Terminal> knownTerminals = new HashSet<Terminal>();
        private HashSet<Nonterminal> knownNonterminals = new HashSet<Nonterminal>();

        public YaccInput()
            : this(null)
        {
        }

        public IReadOnlyList<Production> Read(TextReader reader, Options options)
        {
            defineDirectives = options.DefineDirectives;
            usingStatements = options.AdditionalUsingDirectives;
            precedence = 0;
            knownTerminals.Clear();
            knownNonterminals.Clear();
            scanner = new Scanner(reader);
            return Parse() ? productions : null;
        }

        private void CreateNonterminals(string typeName, List<string> names)
        {
            knownNonterminals.UnionWith(names.Select(s => new Nonterminal(s, typeName)));
        }

        private void SetTerminalParameters(Grammar.Associativity associativity, string typeName)
        {
            terminalAssociativity = associativity;
            terminalTypeName = typeName;
        }

        private void AddTerminal(string name)
        {
            knownTerminals.Add(new Terminal(name, terminalTypeName, terminalAssociativity, precedence));
        }

        private void AddLiteral(char ch)
        {
            knownTerminals.Add(new Terminal("'" + ch + "'", terminalTypeName, terminalAssociativity, precedence, ch));
        }

        private void AddProduction(string ruleName, List<Symbol> rhs, Terminal terminal)
        {
            // Replace code blocks with synthesized non-terminals for rules
            // using those code blocks.
            for(int i = 0, count = rhs.Count - 1; i < count; ++i)
            {
                var innerCodeBlock = rhs[i] as CodeBlockSymbol;
                if(innerCodeBlock != null)
                {
                    string subruleName = string.Format("{0}.{1}", ruleName, i + 1);
                    var subruleSymbol = new Nonterminal(subruleName, null);
                    var subruleProduction = new Production(subruleSymbol, new Symbol[0], productions.Count, innerCodeBlock.ActionCode);
                    productions.Add(subruleProduction);
                    rhs[i] = subruleSymbol;
                }
            }
            var lastCodeBlock = rhs.LastOrDefault() as CodeBlockSymbol;
            ActionCode actionCode = null;
            if(lastCodeBlock != null)
            {
                rhs.RemoveAt(rhs.Count - 1);
                actionCode = lastCodeBlock.ActionCode;
            }
            var nonterminal = new Nonterminal(ruleName, null);
            if(!knownNonterminals.Add(nonterminal))
                nonterminal = knownNonterminals.First(n => n == nonterminal);
            productions.Add(new Production(nonterminal, rhs, productions.Count, actionCode,
                terminal != null ? terminal.Associativity : Grammar.Associativity.None,
                terminal != null ? terminal.Precedence : 0));
        }

        private Terminal GetTerminal(string name)
        {
            var terminal = new Terminal(name, null, Grammar.Associativity.None, 0);
            if(!knownTerminals.Add(terminal))
                terminal = knownTerminals.First(t => t == terminal);
            return terminal;
        }

        private Symbol GetSymbol(string name)
        {
            var terminal = new Terminal(name, null, Grammar.Associativity.None, 0);
            Symbol symbol = knownTerminals.FirstOrDefault(t => t == terminal);
            if(symbol == null)
            {
                var nonterminal = new Nonterminal(name, null);
                symbol = knownNonterminals.Add(nonterminal) ? nonterminal : knownNonterminals.First(n => n == nonterminal);
            }
            return symbol;
        }

        private Symbol GetLiteral(char ch)
        {
            // TODO:  escape character.
            return new Terminal("'" + ch + "'", null, Grammar.Associativity.None, 0, ch);
        }

        private class CodeBlockSymbol : Symbol
        {
            internal readonly ActionCode ActionCode;

            internal CodeBlockSymbol(ActionCode actionCode)
                : base(Guid.NewGuid().ToString(), null)
            {
                ActionCode = actionCode;
            }
        }

        public class Token
        {
            public int Symbol;
            public object Value;
            public static readonly Token End = new Token { Symbol = -1 };
        }

        private enum InputState { Section1, Section2Declaration, Section2Definition }

        public partial class Scanner
        {
            public int LineNumber { get { return yy.LineNumber; } }
            private readonly YY yy;
            private readonly StringBuilder currentAction = new StringBuilder();
            private ScannerMode mode = ScannerMode.SectionOne;

            public Scanner(TextReader reader)
            {
                yy = new YY(reader);
            }

            public Token Read()
            {
                switch(mode)
                {
                case ScannerMode.SectionOne:
                    return ReadSectionOne();
                case ScannerMode.SectionTwo:
                    return ReadSectionTwo();
                }
                throw new Exception("unexpected scanner mode " + mode);
            }

            private Token ReadRestOfLine(int tokenSymbol)
            {
                // Read the rest of the line as the token value.
                var sb = new StringBuilder();
                for(int ch = yy.Get(); ch >= 0 && ch != '\n'; ch = yy.Get())
                {
                    sb.Append((char)ch);
                }
                return new Token { Symbol = tokenSymbol, Value = sb.ToString() };
            }

            private Token MakeLiteral(char value)
            {
                if(yy.Get() != '\'')
                {
                    ReportError("unterminated literal: " + value);
                    return Token.End;
                }
                return new Token { Symbol = Literal, Value = value };
            }

            private void ReportError(string message)
            {
                Console.Error.WriteLine("{0} in line {1}", message, LineNumber);
            }

            private void ReportError(string format, params object[] args)
            {
                ReportError(String.Format(format, args));
            }

            private class YY
            {
                public int LineNumber { get { return lineNumber; } }
                public int ScanValue { get; private set; }
                public string TokenValue { get; private set; }
                private int marker, position, lineNumber = 1;
                private readonly TextReader reader;
                private readonly StringBuilder buffer = new StringBuilder();

                public YY(TextReader reader)
                {
                    this.reader = reader;
                }

                public void Save()
                {
                    marker = position;
                }

                public void Restore()
                {
                    TokenValue = buffer.ToString(0, marker);
                    buffer.Remove(position = 0, marker);
                    marker = 0;
                }

                public int Get()
                {
                    if(position >= buffer.Length)
                    {
                        if(ScanValue < 0)
                            return ScanValue;
                        int ch = reader.Read();
                        if(ch == '\r')
                            ch = reader.Read();
                        if(ch == '\n')
                            ++lineNumber;
                        else if(ch < 0)
                            return ScanValue = -1;
                        buffer.Append((char)ch);
                    }
                    ++position;
                    return ScanValue = buffer[position - 1];
                }
            }

            private enum ScannerMode { SectionOne, SectionTwo }
        }
    }
}
