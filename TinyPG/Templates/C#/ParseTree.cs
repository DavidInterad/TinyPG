﻿// <auto-generated>
// Generated by TinyPG v1.4
// </auto-generated>

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace <%Namespace%>
{
    #region ParseTree

    [GeneratedCode("TinyPG", "1.4")]
    [Serializable]
    public class ParseErrors : <%ParseErrors%>
    {
    }

    [GeneratedCode("TinyPG", "1.4")]
    [Serializable]
    public class ParseError<%ParseError%>
    {
        public string File { get; }
        public int Code { get; }
        public int Line { get; }
        public int Column { get; }
        public int Position { get; }
        public int Length { get; }
        public string Message { get; }

        // just for the sake of serialization
        public ParseError()
        {
        }

        public ParseError(string message, int code, ParseNode node)
            : this(message, code, node.Token)
        {
        }

        public ParseError(string message, int code, Token token)
            : this(message, code, token.File, token.Line, token.Column, token.StartPos, token.Length)
        {
        }

        public ParseError(string message, int code)
            : this(message, code, string.Empty, 0, 0, 0, 0)
        {
        }

        public ParseError(string message, int code, string file, int line, int col, int pos, int length)
        {
            File = file;
            Message = message;
            Code = code;
            Line = line;
            Column = col;
            Position = pos;
            Length = length;
        }
    }

    // rootlevel of the node tree
    [GeneratedCode("TinyPG", "1.4")]
    [Serializable]
    public partial class ParseTree : ParseNode<%IParseTree%>
    {
        public ParseErrors Errors;

        public List<Token> Skipped;

        public ParseTree() : base(new Token(), "ParseTree")
        {
            Token.Type = TokenType.Start;
            Token.Text = "Root";
            Errors = new ParseErrors();
        }

        public string PrintTree()
        {
            StringBuilder sb = new StringBuilder();
            PrintNode(sb, this, 0);
            return sb.ToString();
        }

        private void PrintNode(StringBuilder sb, ParseNode node, int indent)
        {
            string space = "".PadLeft(indent, ' ');

            sb.Append(space);
            sb.AppendLine(node.Text);

            foreach (ParseNode n in node.Nodes)
            {
                PrintNode(sb, n, indent + 2);
            }
        }

        /// <summary>
        /// this is the entry point for executing and evaluating the parse tree.
        /// </summary>
        /// <param name="paramList">additional optional input parameters</param>
        /// <returns>the output of the evaluation function</returns>
        public object Eval(params object[] paramList) => Nodes[0].Eval(this, paramList);
    }

    [GeneratedCode("TinyPG", "1.4")]
    [Serializable]
    [XmlInclude(typeof(ParseTree))]
    public partial class ParseNode<%IParseNode%>
    {
        <%ITokenGet%>
        public List<ParseNode> Nodes { get; }
        <%INodesGet%>
        [XmlIgnore] // avoid circular references when serializing
        public ParseNode Parent;
        public Token Token; // the token/rule

        /// <summary>
        /// Text to display in parse tree.
        /// </summary>
        [XmlIgnore] // skip redundant text (is part of Token)
        public string Text { get; set; }

        public virtual ParseNode CreateNode(Token token, string text) => new ParseNode(token, text) { Parent = this };

        protected ParseNode(Token token, string text)
        {
            Token = token;
            Text = text;
            Nodes = new List<ParseNode>();
        }

        protected object GetValue(ParseTree tree, TokenType type, int index)
        {
            return GetValue(tree, type, ref index);
        }

        protected object GetValue(ParseTree tree, TokenType type, ref int index)
        {
            object o = null;
            if (index < 0) return o;

            // left to right
            foreach (ParseNode node in nodes)
            {
                if (node.Token.Type == type)
                {
                    index--;
                    if (index < 0)
                    {
                        o = node.Eval(tree);
                        break;
                    }
                }
            }
            return o;
        }

        /// <summary>
        /// this implements the evaluation functionality, cannot be used directly
        /// </summary>
        /// <param name="tree">the parsetree itself</param>
        /// <param name="paramList">optional input parameters</param>
        /// <returns>a partial result of the evaluation</returns>
        internal object Eval(ParseTree tree, params object[] paramList) =>
            Token.Type switch
            {
<%EvalSymbols%>
                _ => Token.Text,
            };

<%VirtualEvalMethods%>
    }

    #endregion ParseTree
}
