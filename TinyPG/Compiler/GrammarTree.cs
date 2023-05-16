// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
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
using System.Linq;
using System.Text.RegularExpressions;

namespace TinyPG.Compiler
{

    public class GrammarTree : GrammarNode
    {
    }


    /// <summary>
    /// this class implements the semantics of the parse tree
    /// it will create a Grammar with production rules
    /// </summary>
    public class GrammarNode : ParseTree
    {
        public override ParseNode CreateNode(Token token, string text) =>
            new GrammarNode(token, text) { Parent = this };

        protected GrammarNode()
        {
        }

        protected GrammarNode(Token token, string text)
        {
            Token = token;
            Text = text;
            Nodes = new List<ParseNode>();
        }

        /// <summary>
        /// EvalStart will first do a semantic check to see if symbols are declared correctly
        /// then it will also check for attributes and parse the directives
        /// after that it will complete the transformation to the grammar tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="paramList"></param>
        /// <returns></returns>
        protected override object EvalStart(ParseTree tree, params object[] paramList)
        {
            var startFound = false;
            var g = new Grammar();
            foreach (var n in Nodes)
            {
                switch (n.Token.Type)
                {
                    case TokenType.Directive:
                        EvalDirective(tree, g, n);
                        break;
                    case TokenType.ExtProduction when n.Nodes[n.Nodes.Count - 1].Nodes[2].Nodes[0].Token.Type == TokenType.STRING:
                    {
                        TerminalSymbol terminal;
                        try
                        {
                            terminal = new TerminalSymbol(n.Nodes[n.Nodes.Count - 1].Nodes[0].Token.Text, n.Nodes[n.Nodes.Count - 1].Nodes[2].Nodes[0].Token.Text);
                            for (var i = 0; i < n.Nodes.Count - 1; i++)
                            {
                                if (n.Nodes[i].Token.Type == TokenType.Attribute)
                                {
                                    EvalAttribute(tree, g, terminal, n.Nodes[i]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            tree.Errors.Add(new ParseError(
                                $"regular expression for '{n.Nodes[n.Nodes.Count - 1].Nodes[0].Token.Text}' results in error: {ex.Message}", 0x1020, n.Nodes[0]));
                            continue;
                        }

                        if (terminal.Name == "Start")
                        {
                            tree.Errors.Add(new ParseError("'Start' symbol cannot be a regular expression.", 0x1021, n.Nodes[0]));
                        }

                        if (g.Symbols.Find(terminal.Name) == null)
                        {
                            g.Symbols.Add(terminal);
                        }
                        else
                        {
                            tree.Errors.Add(new ParseError($"Terminal already declared: {terminal.Name}", 0x1022, n.Nodes[0]));
                        }

                        break;
                    }
                    case TokenType.ExtProduction:
                    {
                        var nts = new NonTerminalSymbol(n.Nodes[n.Nodes.Count - 1].Nodes[0].Token.Text);
                        if (g.Symbols.Find(nts.Name) == null)
                        {
                            g.Symbols.Add(nts);
                        }
                        else
                        {
                            tree.Errors.Add(new ParseError($"Non terminal already declared: {nts.Name}", 0x1023, n.Nodes[0]));
                        }

                        for (var i = 0; i < n.Nodes.Count - 1; i++)
                        {
                            if (n.Nodes[i].Token.Type == TokenType.Attribute)
                            {
                                EvalAttribute(tree, g, nts, n.Nodes[i]);
                            }
                        }

                        if (nts.Name == "Start")
                        {
                            startFound = true;
                        }

                        break;
                    }
                }
            }

            if (!startFound)
            {
                tree.Errors.Add(new ParseError("The grammar requires 'Start' to be a production rule.", 0x0024));
                return g;
            }

            foreach (var n in Nodes.Where(n => n.Token.Type == TokenType.ExtProduction))
            {
                n.Eval(tree, g);
            }

            return g;
        }


        protected override object EvalDirective(ParseTree tree, params object[] paramList)
        {
            var g = (Grammar)paramList[0];
            var node = (GrammarNode)paramList[1];
            var name = node.Nodes[1].Token.Text;

            switch (name)
            {
                case "TinyPG":
                case "Parser":
                case "Scanner":
                case "ParseTree":
                case "TextHighlighter":
                    if (g.Directives.Find(name) != null)
                    {
                        tree.Errors.Add(new ParseError($"Directive '{name}' is already defined", 0x1030, node.Nodes[1]));
                        return null; ;
                    }
                    break;
                default:
                    tree.Errors.Add(new ParseError($"Directive '{name}' is not supported", 0x1031, node.Nodes[1]));
                    break;
            }

            var directive = new Directive(name);
            g.Directives.Add(directive);

            foreach (var n in node.Nodes.Where(n => n.Token.Type == TokenType.NameValue))
            {
                EvalNameValue(tree, g, directive, n);
            }

            return null;
        }

        protected override object EvalNameValue(ParseTree tree, params object[] paramList)
        {
            var grammar = (Grammar)paramList[0];
            var directive = (Directive)paramList[1];
            var node = (GrammarNode)paramList[2];

            var key = node.Nodes[0].Token.Text;
            var value = node.Nodes[2].Token.Text.Substring(1, node.Nodes[2].Token.Text.Length - 2);
            if (value.StartsWith("\""))
            {
                value = value.Substring(1);
            }

            directive[key] = value;

            var names = new List<string>(new[] { "Namespace", "OutputPath", "TemplatePath", });
            var languages = new List<string>(new[] { "c#", "cs", "csharp", "vb", "vb.net", "vbnet", "visualbasic" });
            switch (directive.Name)
            {
                case "TinyPG":
                    names.Add("Namespace");
                    names.Add("OutputPath");
                    names.Add("TemplatePath");
                    names.Add("DtoPath");
                    names.Add("Language");

                    switch (key)
                    {
                        case "TemplatePath":
                        {
                            if (grammar.GetTemplatePath() == null)
                            {
                                tree.Errors.Add(new ParseError($"Template path '{value}' does not exist", 0x1060, node.Nodes[2]));
                            }

                            break;
                        }
                        case "OutputPath":
                        {
                            if (grammar.GetOutputPath() == null)
                            {
                                tree.Errors.Add(new ParseError($"Output path '{value}' does not exist", 0x1061, node.Nodes[2]));
                            }

                            break;
                        }
                        case "DtoPath":
                        {
                            if (grammar.GetDtoPath() == null)
                            {
                                tree.Errors.Add(new ParseError($"DTO path '{value}' does not exist", 0x1061, node.Nodes[2]));
                            }

                            break;
                        }
                        case "Language":
                        {
                            if (!languages.Contains(value.ToLower(CultureInfo.InvariantCulture)))
                            {
                                tree.Errors.Add(new ParseError($"Language '{value}' is not supported", 0x1062, node.Nodes[2]));
                            }

                            break;
                        }
                    }

                    break;
                case "Parser":
                case "Scanner":
                case "ParseTree":
                case "TextHighlighter":
                    names.Add("Generate");
                    names.Add("FileName");
                    break;
                default:
                    return null;
            }

            if (!names.Contains(key))
            {
                tree.Errors.Add(new ParseError($"Directive attribute '{key}' is not supported", 0x1034, node.Nodes[0]));
            }

            return null;
        }

        protected override object EvalExtProduction(ParseTree tree, params object[] paramList) =>
            Nodes[Nodes.Count - 1].Eval(tree, paramList);

        protected override object EvalAttribute(ParseTree tree, params object[] paramList)
        {
            var grammar = (Grammar)paramList[0];
            var symbol = (Symbol)paramList[1];
            var node = (GrammarNode)paramList[2];

            if (symbol.Attributes.ContainsKey(node.Nodes[1].Token.Text))
            {
                tree.Errors.Add(new ParseError($"Attribute already defined for this symbol: {node.Nodes[1].Token.Text}", 0x1039, node.Nodes[1]));
                return null;
            }

            symbol.Attributes.Add(node.Nodes[1].Token.Text, (object[])EvalParams(tree, node));
            switch (node.Nodes[1].Token.Text)
            {
                case "Skip":
                    if (symbol is TerminalSymbol)
                    {
                        grammar.SkipSymbols.Add(symbol);
                    }
                    else
                    {
                        tree.Errors.Add(new ParseError(
                            $"Attribute for non-terminal rule not allowed: {node.Nodes[1].Token.Text}", 0x1035, node));
                    }
                    break;
                case "Color":
                    if (symbol is NonTerminalSymbol)
                    {
                        tree.Errors.Add(new ParseError(
                            $"Attribute for non-terminal rule not allowed: {node.Nodes[1].Token.Text}", 0x1035, node));
                    }

                    if (symbol.Attributes["Color"].Length != 1 && symbol.Attributes["Color"].Length != 3)
                    {
                        tree.Errors.Add(new ParseError(
                            $"Attribute {node.Nodes[1].Token.Text} has too many or missing parameters", 0x103A, node.Nodes[1]));
                    }

                    for (var i = 0; i < symbol.Attributes["Color"].Length; i++)
                    {
                        if (!(symbol.Attributes["Color"][i] is string))
                        {
                            continue;
                        }

                        tree.Errors.Add(new ParseError(
                            $"Parameter {node.Nodes[3].Nodes[i * 2].Nodes[0].Token.Text} is of incorrect type", 0x103A, node.Nodes[3].Nodes[i * 2].Nodes[0]));
                        break;
                    }
                    break;
                case "IgnoreCase":
                    if (!(symbol is TerminalSymbol))
                    {
                        tree.Errors.Add(new ParseError(
                            $"Attribute for non-terminal rule not allowed: {node.Nodes[1].Token.Text}", 0x1035, node));
                    }

                    break;
                default:
                    tree.Errors.Add(new ParseError($"Attribute not supported: {node.Nodes[1].Token.Text}", 0x1036, node.Nodes[1]));
                    break;
            }

            return symbol;
        }

        protected override object EvalParams(ParseTree tree, params object[] paramList)
        {
            var node = (GrammarNode)paramList[0];
            if (node.Nodes.Count < 4)
            {
                return null;
            }

            if (node.Nodes[3].Token.Type != TokenType.Params)
            {
                return null;
            }

            var parameters = (GrammarNode)node.Nodes[3];
            var objects = new List<object>();
            for (var i = 0; i < parameters.Nodes.Count; i += 2)
            {
                objects.Add(EvalParam(tree, parameters.Nodes[i]));
            }

            return objects.ToArray();
        }

        protected override object EvalParam(ParseTree tree, params object[] paramList)
        {
            var node = (GrammarNode)paramList[0];
            try
            {
                switch (node.Nodes[0].Token.Type)
                {
                    case TokenType.STRING:
                        return node.Nodes[0].Token.Text;
                    case TokenType.INTEGER:
                        return Convert.ToInt32(node.Nodes[0].Token.Text);
                    case TokenType.HEX:
                        return long.Parse(node.Nodes[0].Token.Text.Substring(2), NumberStyles.HexNumber);
                    default:
                        tree.Errors.Add(new ParseError($"Attribute parameter is not a valid value: {node.Token.Text}", 0x1037, node));
                        break;
                }
            }
            catch (Exception)
            {
                tree.Errors.Add(new ParseError($"Attribute parameter is not a valid value: {node.Token.Text}", 0x1038, node));
            }
            return null;
        }

        protected override object EvalProduction(ParseTree tree, params object[] paramList)
        {
            var g = (Grammar)paramList[0];
            if (Nodes[2].Nodes[0].Token.Type == TokenType.STRING)
            {
                if (!(g.Symbols.Find(Nodes[0].Token.Text) is TerminalSymbol))
                {
                    tree.Errors.Add(new ParseError($"Symbol '{Nodes[0].Token.Text}' is not declared. ", 0x1040, Nodes[0]));
                }
            }
            else
            {
                var nts = g.Symbols.Find(Nodes[0].Token.Text) as NonTerminalSymbol;
                if (nts == null)
                {
                    tree.Errors.Add(new ParseError($"Symbol '{Nodes[0].Token.Text}' is not declared. ", 0x1041, Nodes[0]));
                }

                var r = (Rule)Nodes[2].Eval(tree, g, nts);
                nts?.Rules.Add(r);

                if (Nodes[3].Token.Type == TokenType.CODEBLOCK && nts != null)
                {
                    var codeblock = Nodes[3].Token.Text;
                    nts.CodeBlock = codeblock;
                    ValidateCodeBlock(tree, nts, Nodes[3]);

                    // beautify the codeblock format
                    codeblock = codeblock.Substring(1, codeblock.Length - 3).Trim();
                    nts.CodeBlock = codeblock;
                }
            }

            return g;
        }

        protected override object EvalRule(ParseTree tree, params object[] paramList) =>
            Nodes[0].Eval(tree, paramList);

        protected override object EvalSubrule(ParseTree tree, params object[] paramList)
        {
            if (Nodes.Count == 1) // single symbol
            {
                return Nodes[0].Eval(tree, paramList);
            }

            var choiceRule = new Rule(RuleType.Choice);
            // i+=2 to skip to the | symbols
            for (var i = 0; i < Nodes.Count; i += 2)
            {
                var rule = (Rule)Nodes[i].Eval(tree, paramList);
                choiceRule.Rules.Add(rule);
            }

            return choiceRule;
        }

        protected override object EvalConcatRule(ParseTree tree, params object[] paramList)
        {
            if (Nodes.Count == 1) // single symbol
            {
                return Nodes[0].Eval(tree, paramList);
            }

            var concatRule = new Rule(RuleType.Concat);
            foreach (var rule in Nodes.Select(n => (Rule)n.Eval(tree, paramList)))
            {
                concatRule.Rules.Add(rule);
            }

            return concatRule;
        }


        protected override object EvalSymbol(ParseTree tree, params object[] paramList)
        {
            var last = Nodes[Nodes.Count - 1];
            if (last.Token.Type == TokenType.UNARYOPER)
            {
                Rule unaryRule;
                var oper = last.Token.Text.Trim();
                switch (oper)
                {
                    case "*":
                        unaryRule = new Rule(RuleType.ZeroOrMore);
                        break;
                    case "+":
                        unaryRule = new Rule(RuleType.OneOrMore);
                        break;
                    case "?":
                        unaryRule = new Rule(RuleType.Option);
                        break;
                    default:
                        throw new NotImplementedException("unknown unary operator");
                }

                if (Nodes[0].Token.Type == TokenType.BRACKETOPEN)
                {
                    var rule = (Rule)Nodes[1].Eval(tree, paramList);
                    unaryRule.Rules.Add(rule);
                }
                else
                {
                    var g = (Grammar)paramList[0];
                    if (Nodes[0].Token.Type == TokenType.IDENTIFIER)
                    {
                        var s = g.Symbols.Find(Nodes[0].Token.Text);
                        if (s == null)
                        {
                            tree.Errors.Add(new ParseError($"Symbol '{Nodes[0].Token.Text}' is not declared. ", 0x1042, Nodes[0]));
                        }

                        var r = new Rule(s);
                        unaryRule.Rules.Add(r);
                    }
                }
                return unaryRule;
            }

            if (Nodes[0].Token.Type == TokenType.BRACKETOPEN)
            {
                // create subrule syntax tree
                return Nodes[1].Eval(tree, paramList);
            }

            {
                var g = (Grammar)paramList[0];
                var s = g.Symbols.Find(Nodes[0].Token.Text);
                if (s == null)
                {
                    tree.Errors.Add(new ParseError($"Symbol '{Nodes[0].Token.Text}' is not declared.", 0x1043, Nodes[0]));
                }

                return new Rule(s);
            }
        }

        /// <summary>
        /// validates whether $ variables are corresponding to valid symbols
        /// errors are added to the tree Error object.
        /// </summary>
        /// <param name="nts">non terminal and its production rule</param>
        /// <returns>a formatted codeblock</returns>
        private static void ValidateCodeBlock(ParseTree tree, NonTerminalSymbol nts, ParseNode node)
        {
            if (nts == null)
            {
                return;
            }

            var codeblock = nts.CodeBlock;

            var var = new Regex(@"\$(?<var>[a-zA-Z_0-9]+)(\[(?<index>[^]]+)\])?", RegexOptions.Compiled);

            var symbols = nts.DetermineProductionSymbols();

            var matches = var.Matches(codeblock);
            foreach (Match match in matches)
            {
                var s = symbols.Find(match.Groups["var"].Value);
                if (s != null)
                {
                    continue;
                }

                tree.Errors.Add(new ParseError($"Variable ${match.Groups["var"].Value} cannot be matched.", 0x1016, node.Token.File, node.Token.StartPos + match.Groups["var"].Index, node.Token.StartPos + match.Groups["var"].Index, match.Groups["var"].Length));
                break; // error situation
            }
        }
    }
}
