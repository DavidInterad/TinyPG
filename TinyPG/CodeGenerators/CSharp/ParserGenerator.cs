using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TinyPG.Compiler;

namespace TinyPG.CodeGenerators.CSharp
{
	public class ParserGenerator : BaseGenerator, ICodeGenerator
	{
		internal ParserGenerator()
			: base("Parser.cs")
		{
		}

		public string Generate(Grammar grammar, bool debug)
		{
			if (string.IsNullOrEmpty(grammar.GetTemplatePath()))
			{
				return null;
			}

			// generate the parser file
			var parsers = new StringBuilder();
			var parser = File.ReadAllText(Path.Combine(grammar.GetTemplatePath(), TemplateName));

			// build non terminal tokens
			foreach (var method in grammar.GetNonTerminals().Select(GenerateParseMethod))
			{
				parsers.Append(method);
			}

			if (debug)
			{
				parser = parser.Replace(@"<%Namespace%>", "TinyPG.Debug");
				parser = parser.Replace(@"<%IParser%>", " : TinyPG.Debug.IParser");
				parser = parser.Replace(@"<%IParseTree%>", "TinyPG.Debug.IParseTree");

			}
			else
			{
				parser = parser.Replace(@"<%Namespace%>", grammar.Directives["TinyPG"]["Namespace"]);
				parser = parser.Replace(@"<%IParser%>", "");
				parser = parser.Replace(@"<%IParseTree%>", "ParseTree");
			}

			parser = parser.Replace(@"<%ParseNonTerminals%>", parsers.ToString());
			return parser;
		}

		// generates the method header and body
		private static string GenerateParseMethod(NonTerminalSymbol s)
		{
			var sb = new StringBuilder();
			sb.AppendLine("        private void Parse" + s.Name + "(ParseNode parent)" + Helper.AddComment("NonTerminalSymbol: " + s.Name));
			sb.AppendLine("        {");
			sb.AppendLine("            Token tok;");
			sb.AppendLine("            ParseNode n;");
			sb.AppendLine("            ParseNode node = parent.CreateNode(scanner.GetToken(TokenType." + s.Name + "), \"" + s.Name + "\");");
			sb.AppendLine("            parent.Nodes.Add(node);");
			sb.AppendLine("");

			foreach (var rule in s.Rules)
			{
				// TODO check if s.Rules[0] should actually be rule
				sb.AppendLine(GenerateProductionRuleCode(s.Rules[0], 3));
			}

			sb.AppendLine("            parent.Token.UpdateRange(node.Token);");
			sb.AppendLine("        }" + Helper.AddComment("NonTerminalSymbol: " + s.Name));
			sb.AppendLine();
			return sb.ToString();
		}

		// generates the rule logic inside the method body
		private static string GenerateProductionRuleCode(Rule r, int indent)
		{
			int i;
			Symbols<TerminalSymbol> firsts;
			var sb = new StringBuilder();
			var tabs = IndentTabs(indent);

			switch (r.Type)
			{
				case RuleType.Terminal:
					// expecting terminal, so scan it.
					sb.AppendLine($"{tabs}tok = scanner.Scan(TokenType.{r.Symbol.Name});{Helper.AddComment("Terminal Rule: " + r.Symbol.Name)}");
					sb.AppendLine($"{tabs}n = node.CreateNode(tok, tok.ToString() );");
					sb.AppendLine($"{tabs}node.Token.UpdateRange(tok);");
					sb.AppendLine($"{tabs}node.Nodes.Add(n);");
					sb.AppendLine($"{tabs}if (tok.Type != TokenType.{r.Symbol.Name}) {{");
					sb.AppendLine($"{tabs}    tree.Errors.Add(new ParseError(\"Unexpected token '\" + tok.Text.Replace(\"\\n\", \"\") + \"' found. Expected \" + TokenType.{r.Symbol.Name}.ToString(), 0x1001, tok));");
					sb.AppendLine($"{tabs}    return;");
					sb.AppendLine($"{tabs}}}");
					break;
				case RuleType.NonTerminal:
					sb.AppendLine($"{tabs}Parse{r.Symbol.Name}(node);{Helper.AddComment("NonTerminal Rule: " + r.Symbol.Name)}");
					break;
				case RuleType.Concat:
					foreach (var rule in r.Rules)
					{
						sb.AppendLine();
						sb.AppendLine(tabs + Helper.AddComment("Concat Rule"));
						sb.Append(GenerateProductionRuleCode(rule, indent));
					}
					break;
				case RuleType.ZeroOrMore:
					firsts = r.GetFirstTerminals();
					i = 0;
					sb.Append($"{tabs}tok = scanner.LookAhead(");
					foreach (var s in firsts)
					{
						sb.Append(i == 0 ? $"TokenType.{s.Name}" : $", TokenType.{s.Name}");
						i++;
					}
					sb.AppendLine(");" + Helper.AddComment("ZeroOrMore Rule"));

					i = 0;
					foreach (var s in firsts)
					{
						sb.Append(i == 0
							? $"{tabs}while (tok.Type == TokenType.{s.Name}"
							: $"\r\n{tabs}    || tok.Type == TokenType.{s.Name}");
						i++;
					}
					sb.AppendLine(")");
					sb.AppendLine(tabs + "{");

					foreach (var rule in r.Rules)
					{
						sb.Append(GenerateProductionRuleCode(rule, indent + 1));
					}

					i = 0;
					sb.Append(tabs + "tok = scanner.LookAhead(");
					foreach (var s in firsts)
					{
						sb.Append(i == 0 ? $"TokenType.{s.Name}" : $", TokenType.{s.Name}");
						i++;
					}
					sb.AppendLine(");" + Helper.AddComment("ZeroOrMore Rule"));
					sb.AppendLine(tabs + "}");
					break;
				case RuleType.OneOrMore:
					sb.AppendLine(tabs + "do {" + Helper.AddComment("OneOrMore Rule"));

					foreach (var rule in r.Rules)
					{
						sb.Append(GenerateProductionRuleCode(rule, indent + 1));
					}

					i = 0;
					firsts = r.GetFirstTerminals();
					sb.Append(tabs + "    tok = scanner.LookAhead(");
					foreach (var s in firsts)
					{
						sb.Append(i == 0 ? $"TokenType.{s.Name}" : $", TokenType.{s.Name}");
						i++;
					}
					sb.AppendLine(");" + Helper.AddComment("OneOrMore Rule"));

					i = 0;
					foreach (var s in firsts)
					{
						sb.Append(i == 0 ? $"{tabs}}} while (tok.Type == TokenType.{s.Name}" : $"\r\n{tabs}    || tok.Type == TokenType.{s.Name}");
						i++;
					}
					sb.AppendLine(");" + Helper.AddComment("OneOrMore Rule"));
					break;
				case RuleType.Option:
					i = 0;
					firsts = r.GetFirstTerminals();
					sb.Append(tabs + "tok = scanner.LookAhead(");
					foreach (var s in firsts)
					{
						sb.Append(i == 0 ? $"TokenType.{s.Name}" : $", TokenType.{s.Name}");
						i++;
					}
					sb.AppendLine(");" + Helper.AddComment("Option Rule"));

					i = 0;
					foreach (var s in firsts)
					{
						sb.Append(i == 0
							? $"{tabs}if (tok.Type == TokenType.{s.Name}"
							: $"\r\n{tabs}    || tok.Type == TokenType.{s.Name}");
						i++;
					}
					sb.AppendLine(")");
					sb.AppendLine(tabs + "{");

					foreach (var rule in r.Rules)
					{
						sb.Append(GenerateProductionRuleCode(rule, indent + 1));
					}
					sb.AppendLine(tabs + "}");
					break;
				case RuleType.Choice:
					i = 0;
					firsts = r.GetFirstTerminals();
					sb.Append($"{tabs}tok = scanner.LookAhead(");
					var tokens = new List<string>();
					foreach (var s in firsts)
					{
						sb.Append(i == 0 ? $"TokenType.{s.Name}" : $", TokenType.{s.Name}");
						i++;
						tokens.Add(s.Name);
					}
					string expectedTokens;
					switch (tokens.Count)
					{
						case 1:
							expectedTokens = tokens[0];
							break;
						case 2:
							expectedTokens = $"{tokens[0]} or {tokens[1]}";
							break;
						default:
							expectedTokens = string.Join(", ", tokens.GetRange(0, tokens.Count - 1).ToArray());
							expectedTokens += ", or " + tokens[tokens.Count - 1];
							break;
					}
					sb.AppendLine(");" + Helper.AddComment("Choice Rule"));

					sb.AppendLine(tabs + "switch (tok.Type)");
					sb.AppendLine(tabs + "{" + Helper.AddComment("Choice Rule"));
					foreach (var rule in r.Rules)
					{
						foreach (var s in rule.GetFirstTerminals())
						{
							sb.AppendLine($"{tabs}    case TokenType.{s.Name}:");
						}
						sb.Append(GenerateProductionRuleCode(rule, indent + 2));
						sb.AppendLine(tabs + "        break;");
					}
					sb.AppendLine(tabs + "    default:");
					sb.AppendLine(tabs + "        tree.Errors.Add(new ParseError(\"Unexpected token '\" + tok.Text.Replace(\"\\n\", \"\") + \"' found. Expected " + expectedTokens + ".\", 0x0002, tok));");
					sb.AppendLine(tabs + "        break;");
					sb.AppendLine(tabs + "}" + Helper.AddComment("Choice Rule"));
					break;
			}
			return sb.ToString();
		}

		// replaces tabs by spaces, so outlining is more consistent
		public static string IndentTabs(int indent)
		{
			var t = "";
			for (var i = 0; i < indent; i++)
			{
				t += "    ";
			}

			return t;
		}
	}
}
