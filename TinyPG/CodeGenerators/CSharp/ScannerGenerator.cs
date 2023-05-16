using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TinyPG.Compiler;

namespace TinyPG.CodeGenerators.CSharp
{
    public class ScannerGenerator : BaseGenerator, ICodeGenerator
    {
        internal ScannerGenerator()
            : base("Scanner.cs")
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
                skipList.AppendLine("            _skipList.Add(TokenType." + s.Name + ");");
            }

            // build system tokens
            tokenType.AppendLine("\r\n            //Non terminal tokens:");
            tokenType.AppendLine(Helper.Outline("_NONE_", 3, "= 0,", 5));
            tokenType.AppendLine(Helper.Outline("_UNDETERMINED_", 3, "= 1,", 5));

            // build non terminal tokens
            tokenType.AppendLine("\r\n            //Non terminal tokens:");
            foreach (var s in grammar.GetNonTerminals())
            {
                tokenType.AppendLine(Helper.Outline(s.Name, 3, $"= {counter:d},", 5));
                counter++;
            }

            // build terminal tokens
            tokenType.AppendLine("\r\n            //Terminal tokens:");
            foreach (var s in grammar.GetTerminals())
            {
                regexps.Append($"            regex = new Regex({s.Expression}, RegexOptions.Compiled");

                if (s.Attributes.ContainsKey("IgnoreCase"))
                {
                    regexps.Append(" | RegexOptions.IgnoreCase");
                }

                regexps.Append(");\r\n");

                regexps.Append($"            Patterns.Add(TokenType.{s.Name}, regex);\r\n");
                regexps.Append($"            _tokens.Add(TokenType.{s.Name});\r\n\r\n");

                tokenType.Append(Helper.Outline(s.Name, 3, $"= {counter:d}", 5)).AppendLine(",");
                counter++;
            }

            scanner = scanner.Replace(@"<%SkipList%>", skipList.ToString());
            scanner = scanner.Replace(@"<%RegExps%>", regexps.ToString().TrimEnd());
            scanner = scanner.Replace(@"<%TokenType%>", tokenType.ToString().TrimEnd());

            if (debug)
            {
                scanner = scanner.Replace(@"<%Namespace%>", "TinyPG.Debug");
                scanner = scanner.Replace(@"<%IToken%>", " : TinyPG.Debug.IToken");
            }
            else
            {
                scanner = scanner.Replace(@"<%Namespace%>", grammar.Directives["TinyPG"]["Namespace"]);
                scanner = Regex.Replace(scanner, @"\s*<%IToken%>", "");
            }

            return scanner;
        }
    }
}
