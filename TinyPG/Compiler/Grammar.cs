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
using System.Globalization;
using System.IO;
using System.Text;

namespace TinyPG.Compiler
{
    public class Directives : List<Directive>
    {
        public bool Exists(Directive directive)
        {
            return Exists(d => d.Name == directive.Name);
        }

        public Directive Find(string name)
        {
            return Find(d => d.Name == name);
        }

        public Directive this[string name] => Find(name);
    }

    public class Directive : Dictionary<string, string>
    {
        public Directive(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    public class Grammar
    {
        /// <summary>
        /// represents all terminal and non-terminal symbols in the grammar
        /// </summary>
        public Symbols<Symbol> Symbols { get; set; }

        /// <summary>
        /// corresponds to the symbols that will be skipped during parsing
        /// e.g. commenting code blocks
        /// </summary>
        public Symbols<Symbol> SkipSymbols { get; set; }

        /// <summary>
        /// these are specific directives that should be applied to the grammar
        /// this can be meta data, or information on how code should be generated, e.g.
        /// &lt;%@ Grammar Namespace="TinyPG" %> will generate code with namespace TinyPG.
        /// </summary>
        public Directives Directives { get; set; }

        public Grammar()
        {
            Symbols = new Symbols<Symbol>();
            SkipSymbols = new Symbols<Symbol>();
            Directives = new Directives();
        }

        public Symbols<TerminalSymbol> GetTerminals() => new Symbols<TerminalSymbol>(Symbols);

        public Symbols<NonTerminalSymbol> GetNonTerminals() => new Symbols<NonTerminalSymbol>(Symbols);

        /// <summary>
        /// Once the grammar terminals and non-terminal production rules have been defined
        /// use the Compile method to analyze and check the grammar semantics.
        /// </summary>
        public void Preprocess()
        {
            SetupDirectives();

            DetermineFirsts();

            //LookAheadTree LATree = DetermineLookAheadTree();
            //Symbols nts = GetNonTerminals();
            //NonTerminalSymbol n = (NonTerminalSymbol)nts[0];
            //TerminalSymbol t = (TerminalSymbol) n.FirstTerminals[0];

            //Symbols Follow = new Symbols();
            //t.Rule.DetermineFirstTerminals(Follow, 1);
        }

        /*
        private LookAheadTree DetermineLookAheadTree()
        {
            LookAheadTree tree = new LookAheadTree();
            foreach (NonTerminalSymbol nts in GetNonTerminals())
            {
                tree.NonTerminal = nts;
                nts.DetermineLookAheadTree(tree);
                //nts.DetermineFirstTerminals();
                tree.PrintTree();
            }
            return tree;
        }
        */

        private void DetermineFirsts()
        {
            foreach (var nts in GetNonTerminals())
            {
                nts.DetermineFirstTerminals();
            }
        }

        private void SetupDirectives()
        {

            var d = Directives.Find("TinyPG");
            if (d == null)
            {
                d = new Directive("TinyPG");
                Directives.Insert(0, d);
            }

            if (!d.ContainsKey("Namespace"))
            {
                d["Namespace"] = "TinyPG"; // set default namespace
            }

            if (!d.ContainsKey("OutputPath"))
            {
                d["OutputPath"] = "./"; // write files to current path
            }

            if (!d.ContainsKey("Language"))
            {
                d["Language"] = "C#"; // set default language
            }

            if (!d.ContainsKey("TemplatePath"))
            {
                switch (d["Language"].ToLower(CultureInfo.InvariantCulture))
                {
                    // set the default templates directory
                    case "visualbasic":
                    case "vbnet":
                    case "vb.net":
                    case "vb":
                        d["TemplatePath"] = AppDomain.CurrentDomain.BaseDirectory + @"Templates\VB\";
                        break;
                    default:
                        d["TemplatePath"] = AppDomain.CurrentDomain.BaseDirectory + @"Templates\C#\";
                        break;
                }
            }

            d = Directives.Find("Parser");
            if (d == null)
            {
                d = new Directive("Parser");
                Directives.Insert(1, d);
            }

            if (!d.ContainsKey("Generate"))
            {
                d["Generate"] = "True"; // generate parser by default
            }

            d = Directives.Find("Scanner");
            if (d == null)
            {
                d = new Directive("Scanner");
                Directives.Insert(1, d);
            }

            if (!d.ContainsKey("Generate"))
            {
                d["Generate"] = "True"; // generate scanner by default
            }

            d = Directives.Find("ParseTree");
            if (d == null)
            {
                d = new Directive("ParseTree");
                Directives.Add(d);
            }

            if (!d.ContainsKey("Generate"))
            {
                d["Generate"] = "True"; // generate parse tree by default
            }

            d = Directives.Find("TextHighlighter");
            if (d == null)
            {
                d = new Directive("TextHighlighter");
                Directives.Add(d);
            }

            if (!d.ContainsKey("Generate"))
            {
                d["Generate"] = "False"; // do NOT generate a text highlighter by default
            }
        }

        public string GetTemplatePath()
        {
            var pathOut = Directives["TinyPG"]["TemplatePath"];
            if (Path.IsPathRooted(pathOut))
            {
                var fullPath = Path.GetFullPath(pathOut);
                return Directory.Exists(fullPath) ? fullPath : null;
            }

            var folder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), pathOut));
            if (Directory.Exists(folder))
            {
                return folder;
            }

            folder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pathOut));
            return Directory.Exists(folder) ? folder : null;
        }

        public string GetDtoPath()
        {
            var pathOut = Directives["TinyPG"]["DtoPath"];
            if (Path.IsPathRooted(pathOut))
            {
                var fullPath = Path.GetFullPath(pathOut);
                return Directory.Exists(fullPath) ? fullPath : null;
            }

            var folder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pathOut));
            if (Directory.Exists(folder))
            {
                return folder;
            }

            folder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), pathOut));
            return Directory.Exists(folder) ? folder : null;
        }

        public string GetOutputPath()
        {
            var pathOut = Directives["TinyPG"]["OutputPath"];
            if (Path.IsPathRooted(pathOut))
            {
                var fullPath = Path.GetFullPath(pathOut);
                return Directory.Exists(fullPath) ? fullPath : null;
            }

            var folder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), pathOut));
            return Directory.Exists(folder) ? folder : null;
        }

        public string PrintGrammar()
        {
            var sb = new StringBuilder();
            sb.AppendLine("//Terminals:");
            foreach (var s in GetTerminals())
            {
                var skip = SkipSymbols.Find(s.Name);
                if (skip != null)
                {
                    sb.Append("[Skip] ");
                }

                sb.AppendLine(s.PrintProduction());
            }

            sb.AppendLine("\r\n//Production lines:");
            foreach (var s in GetNonTerminals())
            {
                sb.AppendLine(s.PrintProduction());
            }

            return sb.ToString();
        }

        public string PrintFirsts()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\r\n/*\r\nFirst symbols:");
            foreach (var s in GetNonTerminals())
            {
                var firsts = s.Name + ": ";
                foreach (var t in s.FirstTerminals)
                {
                    // TODO use StringBuilder
                    firsts += t.Name + ' ';
                }

                sb.AppendLine(firsts);
            }

            sb.AppendLine("\r\nSkip symbols: ");
            var skips = "";
            foreach (var s in SkipSymbols)
            {
                skips += s.Name + " ";
            }
            sb.AppendLine(skips);
            sb.AppendLine("*/");
            return sb.ToString();
        }
    }
}
