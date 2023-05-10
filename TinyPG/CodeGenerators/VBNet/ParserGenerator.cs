using System.IO;
using System.Linq;
using System.Text;
using TinyPG.Compiler;

namespace TinyPG.CodeGenerators.VBNet
{
    public class ParserGenerator : BaseGenerator, ICodeGenerator
    {
        internal ParserGenerator()
            : base("Parser.vb")
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
            var parser = File.ReadAllText(grammar.GetTemplatePath() + TemplateName);

            // build non terminal tokens
            foreach (var method in grammar.GetNonTerminals().Select(GenerateParseMethod))
            {
                parsers.Append(method);
            }

            if (debug)
            {
                parser = parser.Replace(@"<%Imports%>", "Imports TinyPG.Debug");
                parser = parser.Replace(@"<%Namespace%>", "TinyPG.Debug");
                parser = parser.Replace(@"<%IParser%>", "\r\n        Implements IParser\r\n");
                parser = parser.Replace(@"<%IParseTree%>", "IParseTree");
            }
            else
            {
                parser = parser.Replace(@"<%Imports%>", "");
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
            sb.AppendLine("        Private Sub Parse" + s.Name + "(ByVal parent As ParseNode)" + Helper.AddComment("'", "NonTerminalSymbol: " + s.Name));
            sb.AppendLine("            Dim tok As Token");
            sb.AppendLine("            Dim n As ParseNode");
            sb.AppendLine("            Dim node As ParseNode = parent.CreateNode(m_scanner.GetToken(TokenType." + s.Name + "), \"" + s.Name + "\")");
            sb.AppendLine("            parent.Nodes.Add(node)");
            sb.AppendLine("");

            foreach (var rule in s.Rules)
            {
                sb.AppendLine(GenerateProductionRuleCode(s.Rules[0], 3));
            }

            sb.AppendLine("            parent.Token.UpdateRange(node.Token)");
            sb.AppendLine("        End Sub" + Helper.AddComment("'", "NonTerminalSymbol: " + s.Name));
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
                    sb.AppendLine(tabs + "tok = m_scanner.Scan(TokenType." + r.Symbol.Name + ")" + Helper.AddComment("'", "Terminal Rule: " + r.Symbol.Name));
                    sb.AppendLine(tabs + "n = node.CreateNode(tok, tok.ToString() )");
                    sb.AppendLine(tabs + "node.Token.UpdateRange(tok)");
                    sb.AppendLine(tabs + "node.Nodes.Add(n)");
                    sb.AppendLine(tabs + "If tok.Type <> TokenType." + r.Symbol.Name + " Then");
                    sb.AppendLine(tabs + "    m_tree.Errors.Add(New ParseError(\"Unexpected token '\" + tok.Text.Replace(\"\\n\", \"\") + \"' found. Expected \" + TokenType." + r.Symbol.Name + ".ToString(), &H1001, tok))");
                    sb.AppendLine(tabs + "    Return\r\n");
                    sb.AppendLine(tabs + "End If\r\n");
                    break;
                case RuleType.NonTerminal:
                    sb.AppendLine(tabs + "Parse" + r.Symbol.Name + "(node)" + Helper.AddComment("'", "NonTerminal Rule: " + r.Symbol.Name));
                    break;
                case RuleType.Concat:
                    foreach (var rule in r.Rules)
                    {
                        sb.AppendLine();
                        sb.AppendLine(tabs + Helper.AddComment("'", "Concat Rule"));
                        sb.Append(GenerateProductionRuleCode(rule, indent));
                    }
                    break;
                case RuleType.ZeroOrMore:
                    firsts = r.GetFirstTerminals();
                    i = 0;
                    sb.Append(tabs + "tok = m_scanner.LookAhead(");
                    foreach (var s in firsts)
                    {
                        if (i == 0)
                            sb.Append("TokenType." + s.Name);
                        else
                            sb.Append(", TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine(")" + Helper.AddComment("'", "ZeroOrMore Rule"));

                    i = 0;
                    foreach (var s in firsts)
                    {
                        if (i == 0)
                            sb.Append(tabs + "While tok.Type = TokenType." + s.Name);
                        else
                            sb.Append(" Or tok.Type = TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine("");


                    foreach (var rule in r.Rules)
                    {
                        sb.Append(GenerateProductionRuleCode(rule, indent + 1));
                    }

                    i = 0;
                    sb.Append(tabs + "tok = m_scanner.LookAhead(");
                    foreach (var s in firsts)
                    {
                        if (i == 0)
                            sb.Append("TokenType." + s.Name);
                        else
                            sb.Append(", TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine(")" + Helper.AddComment("'", "ZeroOrMore Rule"));
                    sb.AppendLine(tabs + "End While");
                    break;
                case RuleType.OneOrMore:
                    sb.AppendLine(tabs + "Do" + Helper.AddComment("'", "OneOrMore Rule"));

                    foreach (var rule in r.Rules)
                    {
                        sb.Append(GenerateProductionRuleCode(rule, indent + 1));
                    }

                    i = 0;
                    firsts = r.GetFirstTerminals();
                    sb.Append(tabs + "    tok = m_scanner.LookAhead(");
                    foreach (var s in firsts)
                    {
                        if (i == 0)
                            sb.Append("TokenType." + s.Name);
                        else
                            sb.Append(", TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine(")" + Helper.AddComment("'", "OneOrMore Rule"));

                    i = 0;
                    foreach (var s in r.GetFirstTerminals())
                    {
                        if (i == 0)
                            sb.Append(tabs + "Loop While tok.Type = TokenType." + s.Name);
                        else
                            sb.Append(" Or tok.Type = TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine("" + Helper.AddComment("'", "OneOrMore Rule"));
                    break;
                case RuleType.Option:
                    i = 0;
                    firsts = r.GetFirstTerminals();
                    sb.Append(tabs + "tok = m_scanner.LookAhead(");
                    foreach (var s in firsts)
                    {
                        if (i == 0)
                            sb.Append("TokenType." + s.Name);
                        else
                            sb.Append(", TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine(")" + Helper.AddComment("'", "Option Rule"));

                    i = 0;
                    foreach (var s in r.GetFirstTerminals())
                    {
                        if (i == 0)
                            sb.Append(tabs + "If tok.Type = TokenType." + s.Name);
                        else
                            sb.Append(" Or tok.Type = TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine(" Then");

                    foreach (var rule in r.Rules)
                    {
                        sb.Append(GenerateProductionRuleCode(rule, indent + 1));
                    }
                    sb.AppendLine(tabs + "End If");
                    break;
                case RuleType.Choice:
                    i = 0;
                    firsts = r.GetFirstTerminals();
                    sb.Append(tabs + "tok = m_scanner.LookAhead(");
                    foreach (var s in firsts)
                    {
                        if (i == 0)
                            sb.Append("TokenType." + s.Name);
                        else
                            sb.Append(", TokenType." + s.Name);
                        i++;
                    }
                    sb.AppendLine(")" + Helper.AddComment("'", "Choice Rule"));

                    sb.AppendLine(tabs + "Select Case tok.Type");
                    sb.AppendLine(tabs + "" + Helper.AddComment("'", "Choice Rule"));
                    foreach (var rule in r.Rules)
                    {
                        foreach (var s in rule.GetFirstTerminals())
                        {
                            sb.AppendLine(tabs + "    Case TokenType." + s.Name + "");
                            sb.Append(GenerateProductionRuleCode(rule, indent + 2));
                        }
                    }
                    sb.AppendLine(tabs + "    Case Else");
                    sb.AppendLine(tabs + "        m_tree.Errors.Add(new ParseError(\"Unexpected token '\" + tok.Text.Replace(\"\\n\", \"\") + \"' found.\", &H0002, tok))");
                    sb.AppendLine(tabs + "        Exit Select");
                    sb.AppendLine(tabs + "End Select" + Helper.AddComment("'", "Choice Rule"));
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
