using System.IO;
using System.Text;
using TinyPG.Compiler;

namespace TinyPG.CodeGenerators.VBNet
{
    public class ScannerGenerator : BaseGenerator, ICodeGenerator
    {
        internal ScannerGenerator()
            : base("Scanner.vb")
        {
        }

        public string Generate(Grammar grammar, bool debug)
        {
            if (string.IsNullOrEmpty(grammar.GetTemplatePath()))
            {
                return null;
            }

            var scanner = File.ReadAllText(grammar.GetTemplatePath() + TemplateName);

            var counter = 2;
            var tokenType = new StringBuilder();
            var regexps = new StringBuilder();
            var skipList = new StringBuilder();

            foreach (var s in grammar.SkipSymbols)
            {
                skipList.AppendLine($"            SkipList.Add(TokenType.{s.Name})");
            }

            // build system tokens
            tokenType.AppendLine("\r\n        'Non terminal tokens:");
            tokenType.AppendLine(Helper.Outline("_NONE_", 2, "= 0", 5));
            tokenType.AppendLine(Helper.Outline("_UNDETERMINED_", 2, "= 1", 5));

            // build non terminal tokens
            tokenType.AppendLine("\r\n        'Non terminal tokens:");
            foreach (var s in grammar.GetNonTerminals())
            {
                tokenType.AppendLine(Helper.Outline(s.Name, 2, $"= {counter:d}", 5));
                counter++;
            }

            // build terminal tokens
            tokenType.AppendLine("\r\n        'Terminal tokens:");
            var first = true;
            foreach (var s in grammar.GetTerminals())
            {
                var vbexpr = s.Expression.ToString();
                if (vbexpr.StartsWith("@"))
                {
                    vbexpr = vbexpr.Substring(1);
                }

                regexps.Append($"            regex = new Regex({vbexpr}, RegexOptions.Compiled)\r\n");
                regexps.Append($"            Patterns.Add(TokenType.{s.Name}, regex)\r\n");
                regexps.Append($"            Tokens.Add(TokenType.{s.Name})\r\n\r\n");

                if (first)
                {
                    first = false;
                }
                else
                {
                    tokenType.AppendLine("");
                }

                tokenType.Append(Helper.Outline(s.Name, 2, $"= {counter:d}", 5));
                counter++;
            }

            scanner = scanner.Replace(@"<%SkipList%>", skipList.ToString());
            scanner = scanner.Replace(@"<%RegExps%>", regexps.ToString());
            scanner = scanner.Replace(@"<%TokenType%>", tokenType.ToString());

            if (debug)
            {
                scanner = scanner.Replace(@"<%Imports%>", "Imports TinyPG.Debug");
                scanner = scanner.Replace(@"<%Namespace%>", "TinyPG.Debug");
                scanner = scanner.Replace(@"<%IToken%>", "\r\n        Implements IToken");
                scanner = scanner.Replace(@"<%ImplementsITokenStartPos%>", " Implements IToken.StartPos");
                scanner = scanner.Replace(@"<%ImplementsITokenEndPos%>", " Implements IToken.EndPos");
                scanner = scanner.Replace(@"<%ImplementsITokenLength%>", " Implements IToken.Length");
                scanner = scanner.Replace(@"<%ImplementsITokenText%>", " Implements IToken.Text");
                scanner = scanner.Replace(@"<%ImplementsITokenToString%>", " Implements IToken.ToString");

            }
            else
            {
                scanner = scanner.Replace(@"<%Imports%>", "");
                scanner = scanner.Replace(@"<%Namespace%>", grammar.Directives["TinyPG"]["Namespace"]);
                scanner = scanner.Replace(@"<%IToken%>", "");
                scanner = scanner.Replace(@"<%ImplementsITokenStartPos%>", "");
                scanner = scanner.Replace(@"<%ImplementsITokenEndPos%>", "");
                scanner = scanner.Replace(@"<%ImplementsITokenLength%>", "");
                scanner = scanner.Replace(@"<%ImplementsITokenText%>", "");
                scanner = scanner.Replace(@"<%ImplementsITokenToString%>", "");
            }

            return scanner;
        }
    }
}
