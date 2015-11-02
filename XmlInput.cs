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
            try
            {
                var xml = XDocument.Load(reader).Element("grammar");
                var productions = new List<Production>();
                foreach(var rule in xml.Element("rules").Elements("rule"))
                {
                    var lhs = (string)rule.Attribute("name");
                    var rhs = from s in rule.Elements()
                              where s.Name != "action"
                              let n = (string)s.Attribute("name") ?? (string)s.Attribute("value")
                              select s.Name == "nonterminal" ? (Symbol)new Nonterminal(n) : s.Name == "literal" ? new Terminal(n.Substring(0, 1)) : new Terminal(n);
                    var production = new Production(new Nonterminal(lhs), rhs);
                    productions.Add(production);
                }
                return new Grammar(productions);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
