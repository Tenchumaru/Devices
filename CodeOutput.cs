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

        public void Write(IEnumerable<Grammar.Entry> table, TextWriter writer, Options options)
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

#if false
            // Map the non-terminal symbols to go-to and reduction table indices.
            var nonTerminalIndices = table.Where(e => e is GoingEntry).Select(e => e.Symbol).Distinct().Select((n, i) => new { N = n, I = i }).ToDictionary(a => a.N, a => a.I);

            // Construct the go-to table and replace it in the skeleton.
            string goTos = ConstructGoTos(table, nonTerminalIndices);
            writer.WriteLine(goTos);
            EmitSection(skeleton, writer);

            // Map the productions to reduction table entries.
            var productions = from e in table
                              let r = e as ReducingEntry
                              where r != null
                              select r.Production;
            var productionIndices = productions.Distinct().Select((p, i) => new { P = p, I = i }).ToDictionary(a => a.P, a => a.I);

            // Construct the reduction table and replace it in the skeleton.
            string reductions = ConstructReductions(nonTerminalIndices, productionIndices);
            writer.WriteLine(reductions);
            EmitSection(skeleton, writer);

            int rowCount = table.Max(e => e.Row) + 1;
            var stateRowLists = new List<string>[rowCount];
            for(int i = 0; i < rowCount; ++i)
                stateRowLists[i] = new List<string>();

            foreach(var entry in table)
            {
                var terminal = entry.Symbol as TerminalSymbol;
                switch(entry.GetType().Name)
                {
                case "AcceptingEntry":
                    stateRowLists[entry.Row].Add("case -1:return true;");
                    break;
                case "GoingEntry":
                    break;
                case "ReducingEntry":
                    var reducingEntry = (ReducingEntry)entry;
                    stateRowLists[entry.Row].Add(string.Format("case {0}:state_={1};goto reduce1;", terminal.Value, productionIndices[reducingEntry.Production]));
                    break;
                case "ShiftingEntry":
                    var shiftingEntry = (ShiftingEntry)entry;
                    stateRowLists[entry.Row].Add(string.Format("case {0}:state_={1};goto shift;", terminal.Value, shiftingEntry.Target));
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
            var terminals = from e in table
                            let t = e.Symbol as TerminalSymbol
                            where t != null && t.Value >= char.MaxValue
                            select t;
            foreach(var terminal in terminals.Distinct())
                writer.WriteLine("public const int {0}= {1};", terminal.ToString(), terminal.Value);
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
                if(symbolIndex >= production.RHS.Count)
                    throw new MalformedSubstitutionException("invalid substitution in action for " + production);

                // Replace the specifier with the stack accessor.
                if(typeName == null)
                    typeName = production.RHS[symbolIndex].TypeName;
                if(typeName == null)
                {
                    Console.WriteLine("warning: type name not specified; using object");
                    typeName = "object";
                }
                int stackIndex = production.RHS.Count - symbolIndex;
                var s = string.Format("(({0})(stack_[stack_.Count - {1}].Value))", typeName, stackIndex);
                parts[i] = s + part.Substring(j);
            }
            return string.Join("", parts);
#endif
        }

        private static void EmitSection(StringReader skeleton, TextWriter output)
        {
            for(string line = skeleton.ReadLine(); line != null && !line.EndsWith("$"); line = skeleton.ReadLine())
                output.WriteLine(line);
#if false
        }

        private static string ConstructReductions(Dictionary<Symbol, int> nonTerminalIndices, Dictionary<Production, int> productionIndices)
        {
            var sb = new StringBuilder();
            int actionIndex = -1;
            foreach(var production in productionIndices.Keys)
            {
                if(production.ActionCode != null)
                    sb.AppendFormat("new R_({0},{1},{2}),", nonTerminalIndices[production.LHS], production.RHS.Count, --actionIndex);
                else
                {
                    string rhsTypeName = production.RHS.Count == 0 ? null : production.RHS[0].TypeName;
                    if(production.LHS.TypeName != rhsTypeName)
                        Console.WriteLine("warning: default action type mismatch; assigning '{0}' from '{1}'", production.LHS.TypeName, rhsTypeName);
                    sb.AppendFormat("new R_({0},{1}),", nonTerminalIndices[production.LHS], production.RHS.Count);
                }
            }
            return sb.ToString();
        }

        private static string ConstructGoTos(IEnumerable<Entry> table, Dictionary<Symbol, int> nonTerminalIndices)
        {
            var goTos = from e in table
                        let g = e as GoingEntry
                        where g != null
                        let r = g.Row
                        let i = nonTerminalIndices[g.Symbol]
                        orderby r, i
                        select new { Row = r, Column = i, Target = g.Target };
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
#endif
        }
    }
}
