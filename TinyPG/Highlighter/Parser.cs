// Generated by TinyPG v1.4

namespace TinyPG.Highlighter
{
	#region Parser

	public class Parser
	{
		private readonly Scanner _scanner;
		private ParseTree _tree;

		public Parser(Scanner scanner)
		{
			_scanner = scanner;
		}

		public ParseTree Parse(string input, string fileName)
		{
			_tree = new ParseTree();
			return Parse(input, fileName, _tree);
		}

		public ParseTree Parse(string input, string fileName, ParseTree tree)
		{
			_scanner.Init(input, fileName);

			_tree = tree;
			ParseStart(tree);
			tree.Skipped = _scanner.Skipped;

			return tree;
		}

		private void ParseStart(ParseNode parent)
		{
			var node = parent.CreateNode(_scanner.GetToken(TokenType.Start), "Start");
			parent.Nodes.Add(node);

			var tok = _scanner.LookAhead(TokenType.GRAMMARCOMMENTLINE, TokenType.GRAMMARCOMMENTBLOCK, TokenType.ATTRIBUTEOPEN, TokenType.GRAMMARSTRING, TokenType.GRAMMARARROW, TokenType.GRAMMARNONKEYWORD, TokenType.GRAMMARKEYWORD, TokenType.GRAMMARSYMBOL, TokenType.CODEBLOCKOPEN, TokenType.DIRECTIVEOPEN);
			while (tok.Type == TokenType.GRAMMARCOMMENTLINE
				|| tok.Type == TokenType.GRAMMARCOMMENTBLOCK
				|| tok.Type == TokenType.ATTRIBUTEOPEN
				|| tok.Type == TokenType.GRAMMARSTRING
				|| tok.Type == TokenType.GRAMMARARROW
				|| tok.Type == TokenType.GRAMMARNONKEYWORD
				|| tok.Type == TokenType.GRAMMARKEYWORD
				|| tok.Type == TokenType.GRAMMARSYMBOL
				|| tok.Type == TokenType.CODEBLOCKOPEN
				|| tok.Type == TokenType.DIRECTIVEOPEN)
			{
				tok = _scanner.LookAhead(TokenType.GRAMMARCOMMENTLINE, TokenType.GRAMMARCOMMENTBLOCK, TokenType.ATTRIBUTEOPEN, TokenType.GRAMMARSTRING, TokenType.GRAMMARARROW, TokenType.GRAMMARNONKEYWORD, TokenType.GRAMMARKEYWORD, TokenType.GRAMMARSYMBOL, TokenType.CODEBLOCKOPEN, TokenType.DIRECTIVEOPEN);
				switch (tok.Type)
				{
					case TokenType.GRAMMARCOMMENTLINE:
					case TokenType.GRAMMARCOMMENTBLOCK:
						ParseCommentBlock(node);
						break;
					case TokenType.ATTRIBUTEOPEN:
						ParseAttributeBlock(node);
						break;
					case TokenType.GRAMMARSTRING:
					case TokenType.GRAMMARARROW:
					case TokenType.GRAMMARNONKEYWORD:
					case TokenType.GRAMMARKEYWORD:
					case TokenType.GRAMMARSYMBOL:
						ParseGrammarBlock(node);
						break;
					case TokenType.CODEBLOCKOPEN:
						ParseCodeBlock(node);
						break;
					case TokenType.DIRECTIVEOPEN:
						ParseDirectiveBlock(node);
						break;
					default:
						_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found.", 0x0002, tok));
						break;
				}
				tok = _scanner.LookAhead(TokenType.GRAMMARCOMMENTLINE, TokenType.GRAMMARCOMMENTBLOCK, TokenType.ATTRIBUTEOPEN, TokenType.GRAMMARSTRING, TokenType.GRAMMARARROW, TokenType.GRAMMARNONKEYWORD, TokenType.GRAMMARKEYWORD, TokenType.GRAMMARSYMBOL, TokenType.CODEBLOCKOPEN, TokenType.DIRECTIVEOPEN);
			}


			tok = _scanner.Scan(TokenType.EOF);
			var n = node.CreateNode(tok, tok.ToString());
			node.Token.UpdateRange(tok);
			node.Nodes.Add(n);
			if (tok.Type != TokenType.EOF)
			{
				_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.EOF, 0x1001, tok));
				return;
			}

			parent.Token.UpdateRange(node.Token);
		}

		private void ParseCommentBlock(ParseNode parent)
		{
			Token tok;
			var node = parent.CreateNode(_scanner.GetToken(TokenType.CommentBlock), "CommentBlock");
			parent.Nodes.Add(node);

			do
			{
				tok = _scanner.LookAhead(TokenType.GRAMMARCOMMENTLINE, TokenType.GRAMMARCOMMENTBLOCK);
				ParseNode n;
				switch (tok.Type)
				{
					case TokenType.GRAMMARCOMMENTLINE:
						tok = _scanner.Scan(TokenType.GRAMMARCOMMENTLINE);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.GRAMMARCOMMENTLINE)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.GRAMMARCOMMENTLINE, 0x1001, tok));
							return;
						}
						break;
					case TokenType.GRAMMARCOMMENTBLOCK:
						tok = _scanner.Scan(TokenType.GRAMMARCOMMENTBLOCK);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.GRAMMARCOMMENTBLOCK)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.GRAMMARCOMMENTBLOCK, 0x1001, tok));
							return;
						}
						break;
					default:
						_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found.", 0x0002, tok));
						break;
				}
				tok = _scanner.LookAhead(TokenType.GRAMMARCOMMENTLINE, TokenType.GRAMMARCOMMENTBLOCK);
			} while (tok.Type == TokenType.GRAMMARCOMMENTLINE || tok.Type == TokenType.GRAMMARCOMMENTBLOCK);

			parent.Token.UpdateRange(node.Token);
		}

		private void ParseDirectiveBlock(ParseNode parent)
		{
			var node = parent.CreateNode(_scanner.GetToken(TokenType.DirectiveBlock), "DirectiveBlock");
			parent.Nodes.Add(node);

			var tok = _scanner.Scan(TokenType.DIRECTIVEOPEN);
			var n = node.CreateNode(tok, tok.ToString());
			node.Token.UpdateRange(tok);
			node.Nodes.Add(n);
			if (tok.Type != TokenType.DIRECTIVEOPEN)
			{
				_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DIRECTIVEOPEN, 0x1001, tok));
				return;
			}


			tok = _scanner.LookAhead(TokenType.WHITESPACE, TokenType.DIRECTIVEKEYWORD, TokenType.DIRECTIVESYMBOL, TokenType.DIRECTIVENONKEYWORD, TokenType.DIRECTIVESTRING);
			while (tok.Type == TokenType.WHITESPACE
				|| tok.Type == TokenType.DIRECTIVEKEYWORD
				|| tok.Type == TokenType.DIRECTIVESYMBOL
				|| tok.Type == TokenType.DIRECTIVENONKEYWORD
				|| tok.Type == TokenType.DIRECTIVESTRING)
			{
				tok = _scanner.LookAhead(TokenType.WHITESPACE, TokenType.DIRECTIVEKEYWORD, TokenType.DIRECTIVESYMBOL, TokenType.DIRECTIVENONKEYWORD, TokenType.DIRECTIVESTRING);
				switch (tok.Type)
				{
					case TokenType.WHITESPACE:
						tok = _scanner.Scan(TokenType.WHITESPACE);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.WHITESPACE)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.WHITESPACE, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DIRECTIVEKEYWORD:
						tok = _scanner.Scan(TokenType.DIRECTIVEKEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DIRECTIVEKEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DIRECTIVEKEYWORD, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DIRECTIVESYMBOL:
						tok = _scanner.Scan(TokenType.DIRECTIVESYMBOL);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DIRECTIVESYMBOL)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DIRECTIVESYMBOL, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DIRECTIVENONKEYWORD:
						tok = _scanner.Scan(TokenType.DIRECTIVENONKEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DIRECTIVENONKEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DIRECTIVENONKEYWORD, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DIRECTIVESTRING:
						tok = _scanner.Scan(TokenType.DIRECTIVESTRING);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DIRECTIVESTRING)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DIRECTIVESTRING, 0x1001, tok));
							return;
						}
						break;
					default:
						_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found.", 0x0002, tok));
						break;
				}
				tok = _scanner.LookAhead(TokenType.WHITESPACE, TokenType.DIRECTIVEKEYWORD, TokenType.DIRECTIVESYMBOL, TokenType.DIRECTIVENONKEYWORD, TokenType.DIRECTIVESTRING);
			}


			tok = _scanner.LookAhead(TokenType.DIRECTIVECLOSE);
			if (tok.Type == TokenType.DIRECTIVECLOSE)
			{
				tok = _scanner.Scan(TokenType.DIRECTIVECLOSE);
				n = node.CreateNode(tok, tok.ToString());
				node.Token.UpdateRange(tok);
				node.Nodes.Add(n);
				if (tok.Type != TokenType.DIRECTIVECLOSE)
				{
					_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DIRECTIVECLOSE, 0x1001, tok));
					return;
				}
			}

			parent.Token.UpdateRange(node.Token);
		}

		private void ParseGrammarBlock(ParseNode parent)
		{
			Token tok;
			var node = parent.CreateNode(_scanner.GetToken(TokenType.GrammarBlock), "GrammarBlock");
			parent.Nodes.Add(node);

			do
			{
				tok = _scanner.LookAhead(TokenType.GRAMMARSTRING, TokenType.GRAMMARARROW, TokenType.GRAMMARNONKEYWORD, TokenType.GRAMMARKEYWORD, TokenType.GRAMMARSYMBOL);
				ParseNode n;
				switch (tok.Type)
				{
					case TokenType.GRAMMARSTRING:
						tok = _scanner.Scan(TokenType.GRAMMARSTRING);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.GRAMMARSTRING)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.GRAMMARSTRING, 0x1001, tok));
							return;
						}
						break;
					case TokenType.GRAMMARARROW:
						tok = _scanner.Scan(TokenType.GRAMMARARROW);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.GRAMMARARROW)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.GRAMMARARROW, 0x1001, tok));
							return;
						}
						break;
					case TokenType.GRAMMARNONKEYWORD:
						tok = _scanner.Scan(TokenType.GRAMMARNONKEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.GRAMMARNONKEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.GRAMMARNONKEYWORD, 0x1001, tok));
							return;
						}
						break;
					case TokenType.GRAMMARKEYWORD:
						tok = _scanner.Scan(TokenType.GRAMMARKEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.GRAMMARKEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.GRAMMARKEYWORD, 0x1001, tok));
							return;
						}
						break;
					case TokenType.GRAMMARSYMBOL:
						tok = _scanner.Scan(TokenType.GRAMMARSYMBOL);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.GRAMMARSYMBOL)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.GRAMMARSYMBOL, 0x1001, tok));
							return;
						}
						break;
					default:
						_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found.", 0x0002, tok));
						break;
				}
				tok = _scanner.LookAhead(TokenType.GRAMMARSTRING, TokenType.GRAMMARARROW, TokenType.GRAMMARNONKEYWORD, TokenType.GRAMMARKEYWORD, TokenType.GRAMMARSYMBOL);
			} while (tok.Type == TokenType.GRAMMARSTRING
				|| tok.Type == TokenType.GRAMMARARROW
				|| tok.Type == TokenType.GRAMMARNONKEYWORD
				|| tok.Type == TokenType.GRAMMARKEYWORD
				|| tok.Type == TokenType.GRAMMARSYMBOL);

			parent.Token.UpdateRange(node.Token);
		}

		private void ParseAttributeBlock(ParseNode parent)
		{
			var node = parent.CreateNode(_scanner.GetToken(TokenType.AttributeBlock), "AttributeBlock");
			parent.Nodes.Add(node);

			var tok = _scanner.Scan(TokenType.ATTRIBUTEOPEN);
			var n = node.CreateNode(tok, tok.ToString());
			node.Token.UpdateRange(tok);
			node.Nodes.Add(n);
			if (tok.Type != TokenType.ATTRIBUTEOPEN)
			{
				_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.ATTRIBUTEOPEN, 0x1001, tok));
				return;
			}


			tok = _scanner.LookAhead(TokenType.ATTRIBUTEKEYWORD, TokenType.ATTRIBUTENONKEYWORD, TokenType.ATTRIBUTESYMBOL);
			while (tok.Type == TokenType.ATTRIBUTEKEYWORD
				|| tok.Type == TokenType.ATTRIBUTENONKEYWORD
				|| tok.Type == TokenType.ATTRIBUTESYMBOL)
			{
				tok = _scanner.LookAhead(TokenType.ATTRIBUTEKEYWORD, TokenType.ATTRIBUTENONKEYWORD, TokenType.ATTRIBUTESYMBOL);
				switch (tok.Type)
				{
					case TokenType.ATTRIBUTEKEYWORD:
						tok = _scanner.Scan(TokenType.ATTRIBUTEKEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.ATTRIBUTEKEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.ATTRIBUTEKEYWORD, 0x1001, tok));
							return;
						}
						break;
					case TokenType.ATTRIBUTENONKEYWORD:
						tok = _scanner.Scan(TokenType.ATTRIBUTENONKEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.ATTRIBUTENONKEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.ATTRIBUTENONKEYWORD, 0x1001, tok));
							return;
						}
						break;
					case TokenType.ATTRIBUTESYMBOL:
						tok = _scanner.Scan(TokenType.ATTRIBUTESYMBOL);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.ATTRIBUTESYMBOL)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.ATTRIBUTESYMBOL, 0x1001, tok));
							return;
						}
						break;
					default:
						_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found.", 0x0002, tok));
						break;
				}
				tok = _scanner.LookAhead(TokenType.ATTRIBUTEKEYWORD, TokenType.ATTRIBUTENONKEYWORD, TokenType.ATTRIBUTESYMBOL);
			}


			tok = _scanner.LookAhead(TokenType.ATTRIBUTECLOSE);
			if (tok.Type == TokenType.ATTRIBUTECLOSE)
			{
				tok = _scanner.Scan(TokenType.ATTRIBUTECLOSE);
				n = node.CreateNode(tok, tok.ToString());
				node.Token.UpdateRange(tok);
				node.Nodes.Add(n);
				if (tok.Type != TokenType.ATTRIBUTECLOSE)
				{
					_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.ATTRIBUTECLOSE, 0x1001, tok));
					return;
				}
			}

			parent.Token.UpdateRange(node.Token);
		}

		private void ParseCodeBlock(ParseNode parent)
		{
			var node = parent.CreateNode(_scanner.GetToken(TokenType.CodeBlock), "CodeBlock");
			parent.Nodes.Add(node);

			var tok = _scanner.Scan(TokenType.CODEBLOCKOPEN);
			var n = node.CreateNode(tok, tok.ToString());
			node.Token.UpdateRange(tok);
			node.Nodes.Add(n);
			if (tok.Type != TokenType.CODEBLOCKOPEN)
			{
				_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.CODEBLOCKOPEN, 0x1001, tok));
				return;
			}


			tok = _scanner.LookAhead(TokenType.DOTNET_COMMENTLINE, TokenType.DOTNET_COMMENTBLOCK, TokenType.DOTNET_TYPES, TokenType.DOTNET_KEYWORD, TokenType.DOTNET_SYMBOL, TokenType.DOTNET_STRING, TokenType.DOTNET_NONKEYWORD);
			while (tok.Type == TokenType.DOTNET_COMMENTLINE
				|| tok.Type == TokenType.DOTNET_COMMENTBLOCK
				|| tok.Type == TokenType.DOTNET_TYPES
				|| tok.Type == TokenType.DOTNET_KEYWORD
				|| tok.Type == TokenType.DOTNET_SYMBOL
				|| tok.Type == TokenType.DOTNET_STRING
				|| tok.Type == TokenType.DOTNET_NONKEYWORD)
			{
				tok = _scanner.LookAhead(TokenType.DOTNET_COMMENTLINE, TokenType.DOTNET_COMMENTBLOCK, TokenType.DOTNET_TYPES, TokenType.DOTNET_KEYWORD, TokenType.DOTNET_SYMBOL, TokenType.DOTNET_STRING, TokenType.DOTNET_NONKEYWORD);
				switch (tok.Type)
				{
					case TokenType.DOTNET_COMMENTLINE:
						tok = _scanner.Scan(TokenType.DOTNET_COMMENTLINE);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DOTNET_COMMENTLINE)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DOTNET_COMMENTLINE, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DOTNET_COMMENTBLOCK:
						tok = _scanner.Scan(TokenType.DOTNET_COMMENTBLOCK);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DOTNET_COMMENTBLOCK)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DOTNET_COMMENTBLOCK, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DOTNET_TYPES:
						tok = _scanner.Scan(TokenType.DOTNET_TYPES);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DOTNET_TYPES)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DOTNET_TYPES, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DOTNET_KEYWORD:
						tok = _scanner.Scan(TokenType.DOTNET_KEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DOTNET_KEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DOTNET_KEYWORD, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DOTNET_SYMBOL:
						tok = _scanner.Scan(TokenType.DOTNET_SYMBOL);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DOTNET_SYMBOL)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DOTNET_SYMBOL, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DOTNET_STRING:
						tok = _scanner.Scan(TokenType.DOTNET_STRING);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DOTNET_STRING)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DOTNET_STRING, 0x1001, tok));
							return;
						}
						break;
					case TokenType.DOTNET_NONKEYWORD:
						tok = _scanner.Scan(TokenType.DOTNET_NONKEYWORD);
						n = node.CreateNode(tok, tok.ToString());
						node.Token.UpdateRange(tok);
						node.Nodes.Add(n);
						if (tok.Type != TokenType.DOTNET_NONKEYWORD)
						{
							_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.DOTNET_NONKEYWORD, 0x1001, tok));
							return;
						}
						break;
					default:
						_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found.", 0x0002, tok));
						break;
				}
				tok = _scanner.LookAhead(TokenType.DOTNET_COMMENTLINE, TokenType.DOTNET_COMMENTBLOCK, TokenType.DOTNET_TYPES, TokenType.DOTNET_KEYWORD, TokenType.DOTNET_SYMBOL, TokenType.DOTNET_STRING, TokenType.DOTNET_NONKEYWORD);
			}


			tok = _scanner.LookAhead(TokenType.CODEBLOCKCLOSE);
			if (tok.Type == TokenType.CODEBLOCKCLOSE)
			{
				tok = _scanner.Scan(TokenType.CODEBLOCKCLOSE);
				n = node.CreateNode(tok, tok.ToString());
				node.Token.UpdateRange(tok);
				node.Nodes.Add(n);
				if (tok.Type != TokenType.CODEBLOCKCLOSE)
				{
					_tree.Errors.Add(new ParseError("Unexpected token '" + tok.Text.Replace("\n", "") + "' found. Expected " + TokenType.CODEBLOCKCLOSE, 0x1001, tok));
					return;
				}
			}

			parent.Token.UpdateRange(node.Token);
		}
	}

	#endregion Parser
}
