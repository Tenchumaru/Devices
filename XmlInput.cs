using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Pard
{
    class XmlInput : IGrammarInput
    {
        public Grammar Read(TextReader reader)
        {
            var xml = XDocument.Load(reader).Element("grammar");
            var productions = new List<Production>();
            var associativityNames = Enum.GetNames(typeof(Grammar.Associativity)).Select(s => s.ToLowerInvariant()).ToList();
            var terminalSet = new HashSet<Terminal>();
            var nonterminalSet = new HashSet<Nonterminal>();
            foreach(var symbol in xml.Element("symbols").Elements())
            {
                var name = (string)symbol.Attribute("name");
                var typeName = (string)symbol.Attribute("type");
                var associativityString = (string)symbol.Attribute("associativity");
                if(associativityString != null)
                    associativityString = associativityString.ToLowerInvariant();
                var precedenceString = (string)symbol.Attribute("precedence");
                var associativity = associativityNames.Contains(associativityString)
                    ? (Grammar.Associativity)Enum.Parse(typeof(Grammar.Associativity), associativityString, true)
                    : Grammar.Associativity.None;
                int precedence = 0;
                int.TryParse(precedenceString, out precedence);
                switch(symbol.Name.LocalName)
                {
                case "literal":
                    // TODO: perform unescaping on the value.
                    terminalSet.Add(new Terminal(Terminal.FormatLiteralName(symbol.Attribute("value").Value), typeName, associativity, precedence));
                    break;
                case "terminal":
                    terminalSet.Add(new Terminal(name, typeName, associativity, precedence));
                    break;
                case "nonterminal":
                    nonterminalSet.Add(new Nonterminal(name, typeName));
                    break;
                default:
                    throw new Exception();
                }
            }
            var terminals = terminalSet.ToDictionary(t => t.Name);
            var nonterminals = nonterminalSet.ToDictionary(t => t.Name);
            foreach(var rule in xml.Element("rules").Elements("rule"))
            {
                var lhs = nonterminals[(string)rule.Attribute("name")];
                var rhs = from s in rule.Elements()
                          where s.Name != "action"
                          let n = (string)s.Attribute("name") ?? (string)s.Attribute("value")
                          select s.Name == "nonterminal" ? (Symbol)nonterminals[n] : s.Name == "literal" ? terminals[Terminal.FormatLiteralName(n)] : terminals[n];
                var production = new Production(lhs, rhs);
                productions.Add(production);
            }
            return new Grammar(productions);
        }
    }
}
