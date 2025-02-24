﻿// <auto-generated>
// Generated by TinyPG v1.4
// </auto-generated>

using System.CodeDom.Compiler;

// Disable unused variable warnings which
// can happen during the parser generation.
#pragma warning disable 168

namespace <%Namespace%>
{
    #region Parser

    [GeneratedCode("TinyPG", "1.4")]
    public partial class Parser <%IParser%>
    {
        private readonly Scanner _scanner;
        private ParseTree _tree;

        public Parser(Scanner scanner) => _scanner = scanner;

        public <%IParseTree%> Parse(string input) => Parse(input, "", new ParseTree());

        public <%IParseTree%> Parse(string input, string fileName) => Parse(input, fileName, new ParseTree());

        public <%IParseTree%> Parse(string input, string fileName, ParseTree tree)
        {
            _scanner.Init(input, fileName);

            _tree = tree;
            ParseStart(tree);
            tree.Skipped = _scanner.Skipped;

            return tree;
        }

<%ParseNonTerminals%>
    }

    #endregion Parser
}
