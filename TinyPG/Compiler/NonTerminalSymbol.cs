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

using System.Linq;
using System.Text;

namespace TinyPG.Compiler
{
    public class NonTerminalSymbol : Symbol
    {
        public Rules Rules;

        // indicates if Non-terminal can evaluate to an empty terminal (e.g. in case T -> a? or T -> a*)
        // in which case the parent rule should continue scanning after this non-terminal for Firsts.
        private bool _containsEmpty;
        private int _visitCount;
        public Symbols<TerminalSymbol> FirstTerminals;

        public NonTerminalSymbol()
            : this("NTS_" + ++Counter)
        {
        }

        public NonTerminalSymbol(string name)
        {
            FirstTerminals = new Symbols<TerminalSymbol>();
            Rules = new Rules();
            Name = name;
            _containsEmpty = false;
            _visitCount = 0;
        }

        /*
        internal void DetermineLookAheadTree(LookAheadNode node)
        {
            //recursion here
            foreach (Rule rule in Rules)
            {
                rule.DetermineLookAheadTree(node);
            }
        }
        */
        internal bool DetermineFirstTerminals()
        {
            // check if non-terminal has already been visited x times
            // only determine firsts x times to allow for recursion of depth x, otherwise may wind up in endless loop
            if (_visitCount > 10)
            {
                return _containsEmpty;
            }

            _visitCount++;

            // reset terminals
            FirstTerminals = new Symbols<TerminalSymbol>();

            //recursion here

            foreach (var rule in Rules)
            {
                _containsEmpty |= rule.DetermineFirstTerminals(FirstTerminals);
            }

            return _containsEmpty;
        }

        /// <summary>
        /// returns a list of symbols used by this production
        /// </summary>
        public Symbols<Symbol> DetermineProductionSymbols()
        {
            var symbols = new Symbols<Symbol>();
            foreach (var rule in Rules)
            {
                rule.DetermineProductionSymbols(symbols);
            }
            return symbols;
        }

        public override string PrintProduction()
        {
            var p = Rules.Aggregate("", (current, r) => $"{current}{r.PrintRule()};");
            return Helper.Outline(Name, 0, $" -> {p}", 4);
        }

    }
}
