﻿using System;
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
            var knownTerminals = new HashSet<Terminal>();
            var knownNonterminals = new HashSet<Nonterminal>();
            var symbols = xml.Elements("symbols").SelectMany(e => e.Elements()).Select((x, i) => new { Element = x, Precedence = i + 1 });
            foreach(var pair in symbols)
            {
                var symbol = pair.Element;
                var name = (string)symbol.Attribute("name");
                var typeName = (string)symbol.Attribute("type");
                var associativityString = (string)symbol.Attribute("associativity");
                if(associativityString != null)
                    associativityString = associativityString.ToLowerInvariant();
                Grammar.Associativity associativity;
                if(associativityNames.Contains(associativityString))
                    associativity = (Grammar.Associativity)Enum.Parse(typeof(Grammar.Associativity), associativityString, true);
                else
                {
                    Console.Error.WriteLine("warning: unknown associativity '{0}'; using 'none'", symbol.Name.LocalName);
                    associativity = Grammar.Associativity.None;
                }
                switch(symbol.Name.LocalName)
                {
                case "literal":
                    knownTerminals.Add(new Terminal(Terminal.FormatLiteralName(symbol.Attribute("value").Value), typeName, associativity, pair.Precedence));
                    break;
                case "terminal":
                    knownTerminals.Add(new Terminal(name, typeName, associativity, pair.Precedence));
                    break;
                case "nonterminal":
                    knownNonterminals.Add(new Nonterminal(name, typeName));
                    break;
                default:
                    Console.Error.WriteLine("warning: unknown symbol element '{0}'; ignoring", symbol.Name.LocalName);
                    break;
                }
            }
            var terminals = knownTerminals.ToDictionary(t => t.Name);
            var nonterminals = knownNonterminals.ToDictionary(t => t.Name);
            foreach(var rule in xml.Element("rules").Elements("rule"))
            {
                Nonterminal nonterminal;
                Terminal terminal;
                var name = (string)rule.Attribute("name");
                var lhs = nonterminals.TryGetValue(name, out nonterminal) ? nonterminal : new Nonterminal(name, null);
                var rhs = from s in rule.Elements()
                          where s.Name != "action"
                          let n = (string)s.Attribute("name") ?? (string)s.Attribute("value")
                          let l = Terminal.FormatLiteralName(n)
                          select s.Name == "nonterminal" ? nonterminals.TryGetValue(name, out nonterminal) ? (Symbol)nonterminal : new Nonterminal(n, null) :
                          s.Name == "literal" ? terminals.TryGetValue(l, out terminal) ? terminal : new Terminal(l, null, Grammar.Associativity.None, 0) :
                          terminals.TryGetValue(n, out terminal) ? terminal : new Terminal(n, null, Grammar.Associativity.None, 0);
                var actionCode = (string)rule.Element("action");
                Production production;
                var precedenceTokenName = (string)rule.Attribute("precedence") ?? "";
                Terminal precedenceToken;
                if(terminals.TryGetValue(precedenceTokenName, out precedenceToken))
                    production = new Production(lhs, rhs, productions.Count, actionCode, precedenceToken.Associativity, precedenceToken.Precedence);
                else
                {
                    if(precedenceTokenName != "")
                        Console.Error.WriteLine("warning: precedence token '{0}' not found; ignoring", precedenceTokenName);
                    production = new Production(lhs, rhs, productions.Count, actionCode);
                }
                productions.Add(production);
            }
            return new Grammar(productions);
        }
    }
}
