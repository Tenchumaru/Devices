using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lad
{
    static class ScannerWriter
    {
        public static void Write(Nfa machine, TextWriter writer, StringBuilder sb, int scannerNumber)
        {
            DfaState startState = machine.CreateDfa();
            var states = new List<DfaState> { startState };
            writer.WriteLine("#pragma warning disable 164");
            var defaultSb = new StringBuilder();
            for(int i = 0; i < states.Count; ++i)
            {
                DfaState state = states[i];
                sb.Length = 0;
                defaultSb.Length = 0;
                string defaultElse = "goto done_;";
                foreach(var transition in state.Transitions)
                {
                    ConcreteSymbol symbol = transition.Key;
                    DfaState target = transition.Value;
                    int targetIndex = states.IndexOf(target);
                    if(targetIndex < 0)
                    {
                        targetIndex = states.Count;
                        states.Add(target);
                    }
                    switch(symbol.GetType().Name)
                    {
                    case "SimpleSymbol":
                        sb.AppendFormat("case '{1}':goto L{2}i{0}_;", scannerNumber, ConcreteSymbol.Escape(((SimpleSymbol)symbol).Value), targetIndex);
                        break;
                    case "AnySymbol":
                        if(symbol == AnySymbol.WithoutNewLine)
                            defaultElse = String.Format("if(ch_!='\\n')goto L{1}i{0}_;goto done_;", scannerNumber, targetIndex);
                        else
                            defaultElse = String.Format("goto L{1}i{0}_;", scannerNumber, targetIndex);
                        break;
                    case "RangeSymbol":
                        var subRanges = ((RangeSymbol)symbol).ComposeSubRanges();
                        defaultSb.Append("if(");
                        foreach(var subRange in subRanges)
                        {
                            if(subRange.Key != subRange.Value)
                            {
                                defaultSb.AppendFormat("(ch_>='{0}'&&ch_<='{1}')||",
                                    ConcreteSymbol.Escape(subRange.Key), ConcreteSymbol.Escape(subRange.Value));
                            }
                            else
                                defaultSb.AppendFormat("ch_=='{0}'||", ConcreteSymbol.Escape(subRange.Key));
                        }
                        defaultSb.Length -= 2;
                        defaultSb.AppendFormat(")goto L{1}i{0}_;", scannerNumber, targetIndex);
                        break;
                    default:
                        throw new Exception("unexpected symbol type: " + symbol.GetType());
                    }
                }
                bool isFinal = sb.Length == 0 && defaultSb.Length == 0 && defaultElse.StartsWith("goto");
                sb.AppendFormat("default:{0}{1}", defaultSb, defaultElse);
                if(state.AcceptingRuleIndices.Count == 0)
                {
                    if(isFinal)
                        writer.WriteLine(Properties.Resources.NonAcceptingFinalState, scannerNumber, i, defaultElse);
                    else
                        writer.WriteLine(Properties.Resources.NonAcceptingState, scannerNumber, i, sb);
                }
                else
                {
                    if(isFinal)
                        writer.WriteLine(Properties.Resources.AcceptingFinalState, scannerNumber, i, state.AcceptingRuleIndices.Min(), defaultElse);
                    else
                        writer.WriteLine(Properties.Resources.AcceptingState, scannerNumber, i, state.AcceptingRuleIndices.Min(), sb);
                }
            }
            writer.WriteLine("X{0}_:;", scannerNumber);
            writer.WriteLine("#pragma warning restore 164");
        }
    }
}
