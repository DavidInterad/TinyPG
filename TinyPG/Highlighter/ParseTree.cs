// Generated by TinyPG v1.4

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TinyPG.Highlighter
{
    #region ParseTree
    [Serializable]
    public class ParseErrors : List<ParseError>
    {
    }

    [Serializable]
    public class ParseError
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

        public ParseError(string message, int code, ParseNode node) : this(message, code, node.Token)
        {
        }

        public ParseError(string message, int code, Token token) : this(message, code, token.File, token.Line, token.Column, token.StartPos, token.Length)
        {
        }

        public ParseError(string message, int code, string file = "", int line = 0, int col = 0, int pos = 0, int length = 0)
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

    // root level of the node tree
    [Serializable]
    public class ParseTree : ParseNode
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
            var sb = new StringBuilder();
            const int indent = 0;
            PrintNode(sb, this, indent);
            return sb.ToString();
        }

        private void PrintNode(StringBuilder sb, ParseNode node, int indent)
        {
            var space = "".PadLeft(indent, ' ');

            sb.Append(space);
            sb.AppendLine(node.Text);

            foreach (var n in node.Nodes)
            {
                PrintNode(sb, n, indent + 2);
            }
        }

        /// <summary>
        /// this is the entry point for executing and evaluating the parse tree.
        /// </summary>
        /// <param name="paramList">additional optional input parameters</param>
        /// <returns>the output of the evaluation function</returns>
        public object Eval(params object[] paramList)
        {
            return Nodes[0].Eval(this, paramList);
        }
    }

    [Serializable]
    [XmlInclude(typeof(ParseTree))]
    public class ParseNode
    {
        protected string text;
        protected List<ParseNode> nodes;

        public List<ParseNode> Nodes => nodes;

        [XmlIgnore] // avoid circular references when serializing
        public ParseNode Parent;
        public Token Token; // the token/rule

        [XmlIgnore] // skip redundant text (is part of Token)
        public string Text { // text to display in parse tree
            get => text;
            set => text = value;
        }

        public virtual ParseNode CreateNode(Token token, string text)
        {
            var node = new ParseNode(token, text)
            {
                Parent = this,
            };
            return node;
        }

        protected ParseNode(Token token, string text)
        {
            Token = token;
            this.text = text;
            nodes = new List<ParseNode>();
        }

        protected object GetValue(ParseTree tree, TokenType type, int index) =>
            GetValue(tree, type, ref index);

        protected object GetValue(ParseTree tree, TokenType type, ref int index)
        {
            if (index < 0)
            {
                return null;
            }

            // left to right
            object o = null;
            foreach (var node in nodes.Where(node => node.Token.Type == type))
            {
                index--;
                if (index >= 0)
                {
                    continue;
                }

                o = node.Eval(tree);
                break;
            }

            return o;
        }

        /// <summary>
        /// this implements the evaluation functionality, cannot be used directly
        /// </summary>
        /// <param name="tree">the parse tree itself</param>
        /// <param name="paramList">optional input parameters</param>
        /// <returns>a partial result of the evaluation</returns>
        internal object Eval(ParseTree tree, params object[] paramList)
        {
            object value;
            switch (Token.Type)
            {
                case TokenType.Start:
                    value = EvalStart(tree, paramList);
                    break;
                case TokenType.CommentBlock:
                    value = EvalCommentBlock(tree, paramList);
                    break;
                case TokenType.DirectiveBlock:
                    value = EvalDirectiveBlock(tree, paramList);
                    break;
                case TokenType.GrammarBlock:
                    value = EvalGrammarBlock(tree, paramList);
                    break;
                case TokenType.AttributeBlock:
                    value = EvalAttributeBlock(tree, paramList);
                    break;
                case TokenType.CodeBlock:
                    value = EvalCodeBlock(tree, paramList);
                    break;

                default:
                    value = Token.Text;
                    break;
            }
            return value;
        }

        protected virtual object EvalStart(ParseTree tree, params object[] paramList)
        {
            return "Could not interpret input; no semantics implemented.";
        }

        protected virtual object EvalCommentBlock(ParseTree tree, params object[] paramList)
        {
            foreach (var node in Nodes)
            {
                node.Eval(tree, paramList);
            }
            return null;
        }

        protected virtual object EvalDirectiveBlock(ParseTree tree, params object[] paramList)
        {
            foreach (var node in Nodes)
            {
                node.Eval(tree, paramList);
            }
            return null;
        }

        protected virtual object EvalGrammarBlock(ParseTree tree, params object[] paramList)
        {
            foreach (var node in Nodes)
            {
                node.Eval(tree, paramList);
            }
            return null;
        }

        protected virtual object EvalAttributeBlock(ParseTree tree, params object[] paramList)
        {
            foreach (var node in Nodes)
            {
                node.Eval(tree, paramList);
            }
            return null;
        }

        protected virtual object EvalCodeBlock(ParseTree tree, params object[] paramList)
        {
            foreach (var node in Nodes)
            {
                node.Eval(tree, paramList);
            }
            return null;
        }
    }

    #endregion ParseTree
}
