// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
// Updated 2023 David Prem - <david.prem@interad.at>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace TinyPG.Compiler
{
    #region RuleType
    public enum RuleType
    {
        //Production = 0, // production rule
        /// <summary>
        /// represents a terminal symbol
        /// </summary>
        Terminal = 1,
        /// <summary>
        /// represents a non terminal symbol
        /// </summary>
        NonTerminal = 2,
        /// <summary>
        /// represents the | symbol, choose between one or the other symbol or sub-rule (OR)
        /// </summary>
        Choice = 3, // |
        /// <summary>
        /// puts two symbols or sub-rules in sequential order (AND)
        /// </summary>
        Concat = 4, // <whitespace>
        /// <summary>
        /// represents the ? symbol
        /// </summary>
        Option = 5, // ?
        /// <summary>
        /// represents the * symbol
        /// </summary>
        ZeroOrMore = 6, // *
        /// <summary>
        /// represents the + symbol
        /// </summary>
        OneOrMore = 7, // +
    }
    #endregion RuleType

    public class Rules : List<Rule>
    {
    }

    public class Rule
    {
        public Symbol Symbol;
        public Rules Rules;
        public RuleType Type;

        public Rule()
            : this(null, RuleType.Choice)
        {
        }

        public Rule(Symbol s) : this(s, s is TerminalSymbol ? RuleType.Terminal : RuleType.NonTerminal)
        {
        }

        public Rule(RuleType type) : this(null, type)
        {
        }

        public Rule(Symbol s, RuleType type)
        {
            Type = type;
            Symbol = s;
            Rules = new Rules();
        }
        public Symbols<TerminalSymbol> GetFirstTerminals()
        {
            var firstTerminals = new Symbols<TerminalSymbol>();
            DetermineFirstTerminals(firstTerminals);
            return firstTerminals;
        }


        public void DetermineProductionSymbols(Symbols<Symbol> symbols)
        {
            if (Type == RuleType.Terminal || Type == RuleType.NonTerminal)
            {
                symbols.Add(Symbol);
            }
            else
            {
                foreach (var rule in Rules)
                {
                    rule.DetermineProductionSymbols(symbols);
                }
            }

        }

        /*
        internal void DetermineLookAheadTree(LookAheadNode node)
        {
            switch (Type)
            {
                case RuleType.Terminal:
                    LookAheadNode f = node.Nodes.Find(Symbol.Name);
                    if (f == null)
                    {
                        LookAheadNode n = new LookAheadNode();
                        n.LookAheadTerminal = (TerminalSymbol) Symbol;
                        node.Nodes.Add(n);
                    }
                    else
                        Console.WriteLine("throw new Exception(\"Terminal already exists\");");
                    break;
                case RuleType.NonTerminal:
                    NonTerminalSymbol nts = Symbol as NonTerminalSymbol;

                    break;
                //case RuleType.Production:
                case RuleType.Concat:
                    break;
                case RuleType.OneOrMore:
                    break;
                case RuleType.Option:
                case RuleType.Choice:
                case RuleType.ZeroOrMore:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        */

        internal bool DetermineFirstTerminals(Symbols<TerminalSymbol> firstTerminals) => DetermineFirstTerminals(firstTerminals, 0);

        internal bool DetermineFirstTerminals(Symbols<TerminalSymbol> firstTerminals, int index)
        {
            // indicates if Non-terminal can evaluate to an empty terminal (e.g. in case T -> a? or T -> a*)
            // in which case the parent rule should continue scanning after this non-terminal for Firsts.
            var containsEmpty = false; // assume terminal is found
            switch (Type)
            {
                case RuleType.Terminal:
                    if (!(Symbol is TerminalSymbol terminalSymbol))
                    {
                        return true;
                    }

                    if (!firstTerminals.Exists(terminalSymbol))
                    {
                        firstTerminals.Add(terminalSymbol);
                    }
                    else
                    {
                        Console.WriteLine("throw new Exception(\"Terminal already exists\");");
                    }

                    break;
                case RuleType.NonTerminal:
                    if (Symbol == null)
                    {
                        return true;
                    }

                    var nts = (NonTerminalSymbol)Symbol;
                    containsEmpty = nts.DetermineFirstTerminals();

                    // add first symbols of the non-terminal if not already added
                    foreach (var t in nts.FirstTerminals)
                    {
                        if (!firstTerminals.Exists(t))
                        {
                            firstTerminals.Add(t);
                        }
                        else
                        {
                            Console.WriteLine("throw new Exception(\"Terminal already exists\");");
                        }
                    }
                    break;
                case RuleType.Choice:
                    {
                        // all sub-rules must be evaluated to determine if they contain first terminals
                        // if any sub-rule contains an empty, then this rule also contains an empty
                        foreach (var r in Rules)
                        {
                            containsEmpty |= r.DetermineFirstTerminals(firstTerminals);
                        }
                        break;
                    }
                case RuleType.OneOrMore:
                    {
                        // if a non-empty sub-rule was found, then stop further parsing.
                        foreach (var r in Rules)
                        {
                            containsEmpty = r.DetermineFirstTerminals(firstTerminals);
                            if (!containsEmpty) // found the final set of first terminals
                            {
                                break;
                            }
                        }
                        break;
                    }
                case RuleType.Concat:
                    {
                        // if a non-empty sub-rule was found, then stop further parsing.
                        // start scanning from Index

                        for (var i = index; i < Rules.Count; i++)
                        {
                            containsEmpty = Rules[i].DetermineFirstTerminals(firstTerminals);
                            if (!containsEmpty) // found the final set of first terminals
                            {
                                break;
                            }
                        }

                        // assign this concat rule to each terminal
                        foreach (var t in firstTerminals)
                        {
                            t.Rule = this;
                        }

                        break;
                    }
                case RuleType.Option:
                case RuleType.ZeroOrMore:
                    {
                        // empty due to the nature of this rule (A? or A* can always be empty)
                        containsEmpty = true;

                        // This check does not work (containsEmpty stays always true).
                        // if a non-empty sub-rule was found, then stop further parsing.
                        foreach (var r in Rules)
                        {
                            containsEmpty |= r.DetermineFirstTerminals(firstTerminals);
                            if (!containsEmpty) // found the final set of first terminals
                            {
                                break;
                            }
                        }
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
            return containsEmpty;
        }

        public string PrintRule()
        {
            var r = "";

            switch (Type)
            {
                case RuleType.Terminal:
                case RuleType.NonTerminal:
                    if (Symbol != null)
                    {
                        r = Symbol.Name;
                    }
                    break;
                case RuleType.Concat:
                    foreach (var rule in Rules)
                    {
                        // continue recursively parsing all sub-rules
                        r += $"{rule.PrintRule()} ";
                    }
                    if (Rules.Count < 1)
                        r += " <- WARNING: ConcatRule contains no sub-rules";
                    break;
                case RuleType.Choice:
                    r += "(";
                    foreach (var rule in Rules)
                    {
                        if (r.Length > 1)
                        {
                            r += " | ";
                        }

                        // continue recursively parsing all sub-rules
                        r += rule.PrintRule();
                    }
                    r += ")";
                    if (Rules.Count < 1)
                    {
                        r += " <- WARNING: ChoiceRule contains no sub-rules";
                    }

                    break;
                case RuleType.ZeroOrMore:
                    if (Rules.Count >= 1)
                    {
                        r += "(" + Rules[0].PrintRule() + ")*";
                    }

                    if (Rules.Count > 1)
                    {
                        r += " <- WARNING: ZeroOrMoreRule contains more than 1 sub-rule";
                    }

                    if (Rules.Count < 1)
                    {
                        r += " <- WARNING: ZeroOrMoreRule contains no sub-rule";
                    }

                    break;
                case RuleType.OneOrMore:
                    if (Rules.Count >= 1)
                    {
                        r += "(" + Rules[0].PrintRule() + ")+";
                    }

                    if (Rules.Count > 1)
                    {
                        r += " <- WARNING: OneOrMoreRule contains more than 1 sub-rule";
                    }

                    if (Rules.Count < 1)
                    {
                        r += " <- WARNING: OneOrMoreRule contains no sub-rule";
                    }

                    break;
                case RuleType.Option:
                    if (Rules.Count >= 1)
                    {
                        r += "(" + Rules[0].PrintRule() + ")?";
                    }

                    if (Rules.Count > 1)
                    {
                        r += " <- WARNING: OptionRule contains more than 1 sub-rule";
                    }

                    if (Rules.Count < 1)
                    {
                        r += " <- WARNING: OptionRule contains no sub-rule";
                    }

                    break;
                default:
                    r = Symbol.Name;
                    break;
            }
            return r;
        }
    }
}
