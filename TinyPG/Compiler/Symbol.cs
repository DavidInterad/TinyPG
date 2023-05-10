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

using System.Collections.Generic;
using System.Linq;

namespace TinyPG.Compiler
{
    public class Symbols<T> : List<T> where T : Symbol
    {
        public Symbols() { }

        public Symbols(IEnumerable<T> symbols) => AddRange(symbols);

        public Symbols(IEnumerable<Symbol> symbols) => AddRange(symbols.OfType<T>());

        public bool Exists(Symbol symbol) => Exists(s => s.Name == symbol.Name);

        public Symbol Find(string name) => Find(s => s != null && s.Name == name);
    }

    // allows assigning attributes to the node
    public class SymbolAttributes : Dictionary<string, object[]>
    {
    }

    public abstract class Symbol
    {
        public SymbolAttributes Attributes;

        protected static int Counter = 0;

        // the name of the symbol
        public string Name;

        // an attached piece of sourcecode
        public string CodeBlock;

        public Rule Rule; // the rule this symbol is used in.

        public abstract string PrintProduction();

        protected Symbol()
        {
            Attributes = new SymbolAttributes();
        }
    }
}
