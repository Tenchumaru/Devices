using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pard
{
    class CodeOutput : IGrammarOutput
    {
        private static readonly string[] requiredUsingDirectives =
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
        };

        public void Write(IReadOnlyList<Grammar.ActionEntry> actions, IReadOnlyList<Grammar.GotoEntry> gotos, IReadOnlyList<Production> productions, TextWriter writer, Options options)
        {
            // Emit the define directives.
            foreach(string defineDirective in options.DefineDirectives)
                writer.WriteLine("#define " + defineDirective);

            // Get the output skeleton.
            var skeleton = new StringReader(Properties.Resources.Skeleton);
            EmitSection(skeleton, writer);

            // Add unique additional using directives.
            var uniqueUsingDirectives = options.AdditionalUsingDirectives.Except(requiredUsingDirectives).ToArray();
            if(uniqueUsingDirectives.Length > 0)
                writer.WriteLine("using {0};", string.Join(";using ", uniqueUsingDirectives));

            // Emit the namespace, class name, and constructor values.
            string classDeclaration = options.NamespaceName != null
                ? string.Format("namespace {0} {{ public partial class {1}", options.NamespaceName, options.ParserClassName)
                : "public class " + options.ParserClassName;
            writer.WriteLine(classDeclaration);
            EmitSection(skeleton, writer);
            writer.WriteLine("private {0} scanner;", options.ScannerClassName);
            EmitSection(skeleton, writer);
            writer.WriteLine("public {0}({1} scanner)", options.ParserClassName, options.ScannerClassName);
            EmitSection(skeleton, writer);

            // Map the non-terminal symbols to go-to and reduction table indices.
            var nonTerminalIndices = gotos.Select(e => e.Nonterminal).Distinct().Select((n, i) => new { N = n, I = i }).ToDictionary(a => a.N, a => a.I);

            // Construct the go-to table and replace it in the skeleton.
            string goTos = ConstructGoTos(gotos, nonTerminalIndices);
            writer.WriteLine(goTos);
            EmitSection(skeleton, writer);

            // Map the productions to reduction table entries.
            var reducedProductions = from e in actions
                                     where e.Action == Grammar.Action.Reduce
                                     select productions[e.Value - 1];
            var productionIndices = reducedProductions.Distinct().Select((p, i) => new { P = p, I = i }).ToDictionary(a => a.P, a => a.I);

            // Construct the reduction table and replace it in the skeleton.
            string reductions = ConstructReductions(nonTerminalIndices, productionIndices);
            writer.WriteLine(reductions);
            EmitSection(skeleton, writer);

            int rowCount = actions.Max(e => e.StateIndex) + 1;
            var stateRowLists = new List<string>[rowCount];
            for(int i = 0; i < rowCount; ++i)
                stateRowLists[i] = new List<string>();

            foreach(var entry in actions)
            {
                var terminal = entry.Terminal;
                switch(entry.Action)
                {
                case Grammar.Action.Accept:
                    stateRowLists[entry.StateIndex].Add("case -1:return true;");
                    break;
                case Grammar.Action.Reduce:
                    stateRowLists[entry.StateIndex].Add(string.Format("case {0}:state_={1};goto reduce1;", terminal.Value, productionIndices[productions[entry.Value - 1]]));
                    break;
                case Grammar.Action.Shift:
                    stateRowLists[entry.StateIndex].Add(string.Format("case {0}:state_={1};goto shift;", terminal.Value, entry.Value));
                    break;
                }
            }

            // Collapse inner cases.
            var stateRows = new List<string>();
            foreach(var stateRowList in stateRowLists)
            {
                var inners = from s in stateRowList
                             let a = s.Split(':')
                             group a[0] by a[1] into g
                             select string.Format("{0}:{1}", string.Join(":", g.Distinct().ToArray()), g.Key);
                var joined = string.Join("", inners.ToArray());
                stateRows.Add(joined);
            }

            // Collapse outer cases and emit the transitions.
            var outers = stateRows.Select((s, i) => new { S = s, I = i }).GroupBy(a => a.S).Select(g => string.Format("case {0}:switch(token_.Symbol){{ {1} }}break;", string.Join(":case ", g.Select(a => a.I.ToString()).Distinct().ToArray()), g.Key));
            writer.WriteLine(string.Join(Environment.NewLine, outers.ToArray()));
            EmitSection(skeleton, writer);

            // Emit the actions.
            int actionIndex = -1;
            foreach(var production in productionIndices.Keys.Where(p => p.ActionCode != null))
                writer.WriteLine("case {1}:{0}{2}{0}goto reduce2;", writer.NewLine, --actionIndex, ConstructAction(production));
            EmitSection(skeleton, writer);

            // Emit any terminal definitions.
            var terminals = from e in actions
                            let t = e.Terminal
                            where t.Name[0] != '\'' && t.Name != "(end)"
                            select t;
            foreach(var terminal in terminals.Distinct().Select((t, i) => new { Name = t.Name, Value = i + Char.MaxValue + 1 }))
                writer.WriteLine("public const int {0}= {1};", terminal.Name, terminal.Value);
            EmitSection(skeleton, writer);

            if(options.NamespaceName != null)
                writer.WriteLine('}');
        }

        private static string ConstructAction(Production production)
        {
            string code = string.Join("reductionValue_", production.ActionCode.Split(new[] { "$$" }, StringSplitOptions.None));
            string[] parts = code.Split('$');

            for(int i = 1; i < parts.Length; ++i)
            {
                string part = parts[i];
                if(part.Length < 1)
                    throw new MalformedSubstitutionException(production);

                // Parse any explicit type name.
                string typeName = null;
                int j = 0;
                if(part[0] == '<')
                {
                    j = part.IndexOf('>');
                    if(j < 2)
                        throw new MalformedSubstitutionException(production);
                    typeName = part.Substring(1, j - 1);
                    ++j;
                }

                // Parse the symbol number.
                if(j >= part.Length)
                    throw new MalformedSubstitutionException(production);
                if(part[j] == '$')
                {
                    if(typeName != null)
                        parts[i] = string.Format("(({0})reductionValue_{1})", typeName, part.Substring(j + 1));
                    else
                        parts[i] = "reductionValue_" + part.Substring(j + 1);
                    continue;
                }
                var sb = new StringBuilder();
                if(part[j] == '-')
                {
                    sb.Append('-');
                    ++j;
                }
                for(; j < part.Length && char.IsDigit(part[j]); ++j)
                    sb.Append(part[j]);
                int symbolNumber;
                if(!int.TryParse(sb.ToString(), out symbolNumber))
                    throw new MalformedSubstitutionException(production);
                int symbolIndex = symbolNumber - 1;
                if(symbolIndex >= production.Rhs.Count)
                    throw new MalformedSubstitutionException("invalid substitution in action for " + production);

                // Replace the specifier with the stack accessor.
                if(typeName == null)
                    typeName = production.Rhs[symbolIndex].TypeName;
                if(typeName == null)
                {
                    Console.WriteLine("warning: type name not specified; using object");
                    typeName = "object";
                }
                int stackIndex = production.Rhs.Count - symbolIndex;
                var s = string.Format("(({0})(stack_[stack_.Count - {1}].Value))", typeName, stackIndex);
                parts[i] = s + part.Substring(j);
            }

            return string.Join("", parts);
        }

        private static void EmitSection(StringReader skeleton, TextWriter output)
        {
            for(string line = skeleton.ReadLine(); line != null && !line.EndsWith("$"); line = skeleton.ReadLine())
                output.WriteLine(line);
        }

        private static string ConstructReductions(Dictionary<Nonterminal, int> nonTerminalIndices, Dictionary<Production, int> productionIndices)
        {
            var sb = new StringBuilder();
            int actionIndex = -1;
            foreach(var production in productionIndices.Keys)
            {
                if(production.ActionCode != null)
                    sb.AppendFormat("new R_({0},{1},{2}),", nonTerminalIndices[production.Lhs], production.Rhs.Count, --actionIndex);
                else
                {
                    string rhsTypeName = production.Rhs.Count == 0 ? null : production.Rhs[0].TypeName;
                    if(production.Lhs.TypeName != rhsTypeName)
                        Console.WriteLine("warning: default action type mismatch; assigning '{0}' from '{1}'", production.Lhs.TypeName, rhsTypeName);
                    sb.AppendFormat("new R_({0},{1}),", nonTerminalIndices[production.Lhs], production.Rhs.Count);
                }
            }
            return sb.ToString();
        }

        private static string ConstructGoTos(IReadOnlyList<Grammar.GotoEntry> gotos, Dictionary<Nonterminal, int> nonTerminalIndices)
        {
            var goTos = from g in gotos
                        let r = g.StateIndex
                        let i = nonTerminalIndices[g.Nonterminal]
                        orderby r, i
                        select new { Row = r, Column = i, Target = g.TargetStateIndex };
            if(!goTos.Any())
                return "";
            int goToRowCount = goTos.Max(g => g.Row) + 1;
            var goToArray = new int[goToRowCount, nonTerminalIndices.Count];
            foreach(var g in goTos)
                goToArray[g.Row, g.Column] = g.Target;
            var sb = new StringBuilder();
            for(int i = 0; i < goToRowCount; ++i)
            {
                sb.Append('{');
                for(int j = 0; j < nonTerminalIndices.Count; ++j)
                    sb.AppendFormat("{0},", goToArray[i, j]);
                --sb.Length;
                sb.Append("},");
            }
            --sb.Length;
            return sb.ToString();
        }
    }

    public class MalformedSubstitutionException : Exception
    {
        internal MalformedSubstitutionException(Production production)
            : base("malformed substitution in " + production)
        {
        }

        public MalformedSubstitutionException(string message)
            : base(message)
        {
        }
    }
}
