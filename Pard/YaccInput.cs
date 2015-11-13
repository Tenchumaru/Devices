﻿using System;
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
            scanner = new YaccScanner(reader);
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
                    var subruleProduction = new Production(subruleSymbol, new Symbol[0], 0, innerCodeBlock.ToString());
                    productions.Add(subruleProduction);
                    rhs[i] = subruleSymbol;
                }
            }
            var lastCodeBlock = rhs.LastOrDefault() as CodeBlockSymbol;
            string actionCode = null;
            if(lastCodeBlock != null)
            {
                rhs.RemoveAt(rhs.Count - 1);
                actionCode = lastCodeBlock.TypeName;
            }
            var nonterminal = new Nonterminal(ruleName, null);
            if(!knownNonterminals.Add(nonterminal))
                nonterminal = knownNonterminals.First(n => n == nonterminal);
            productions.Add(new Production(nonterminal, rhs, 0, actionCode,
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
            Symbol symbol;
            var terminal = new Terminal(name, null, Grammar.Associativity.None, 0);
            if(knownTerminals.Contains(terminal))
                symbol = terminal;
            else
            {
                var nonterminal = new Nonterminal(name, null);
                symbol = knownNonterminals.Add(nonterminal) ? nonterminal : knownNonterminals.First(n => n == nonterminal);
            }
            return symbol;
        }

        private Symbol GetLiteral(char ch)
        {
            // TODO:  escape character.
            return new Terminal("'" + ch + "'", null, Grammar.Associativity.None, 0);
        }

        private class CodeBlockSymbol : Symbol
        {
            internal CodeBlockSymbol(string text)
                : base(Guid.NewGuid().ToString(), text)
            {
            }
        }

        public interface IScanner
        {
            ScannerToken Read();
        }

        public class ScannerToken
        {
            public int Symbol;
            public object Value;
            public static readonly ScannerToken End = new ScannerToken { Symbol = -1 };
        }

        private enum InputState { Section1, Section2Declaration, Section2Definition }

        public partial class YaccScanner : IScanner
        {
            private YY yy;
            private ScannerMode mode;
            private StringBuilder currentAction = new StringBuilder();

            public YaccScanner(TextReader reader)
            {
                yy = new YY(reader);
            }

            public ScannerToken Read()
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

            private ScannerToken ReadRestOfLine(int tokenSymbol)
            {
                // Read the rest of the line as the token value.
                var sb = new StringBuilder();
                for(int ch; ; )
                {
                    ch = yy.Get();
                    if(ch < 0 || ch == '\r' || ch == '\n')
                        break;
                    sb.Append((char)ch);
                }
                return new ScannerToken { Symbol = tokenSymbol, Value = sb.ToString() };
            }

            private ScannerToken MakeLiteral(char value)
            {
                if(yy.Get() != '\'')
                {
                    ReportError("unterminated literal: " + value);
                    return ScannerToken.End;
                }
                return new ScannerToken { Symbol = Literal, Value = value };
            }

            private void ReportError(string message)
            {
                Console.Error.WriteLine(message);
            }

            private void ReportError(string format, params object[] args)
            {
                Console.Error.WriteLine(format, args);
            }

            private enum ScannerMode { SectionOne, SectionTwo }

            private class YY
            {
                public int ScanValue { get; private set; }
                public string TokenValue { get; private set; }
                private StringBuilder buffer = new StringBuilder();
                private int marker, position;
                private TextReader reader;

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
                        if(ch < 0)
                            return ScanValue = -1;
                        buffer.Append((char)ch);
                    }
                    ++position;
                    return ScanValue = buffer[position - 1];
                }
            }
        }
    }
}