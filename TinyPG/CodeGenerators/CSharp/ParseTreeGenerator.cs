using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TinyPG.Compiler;

namespace TinyPG.CodeGenerators.CSharp
{
    public class ParseTreeGenerator : BaseGenerator, ICodeGenerator
    {
        internal ParseTreeGenerator()
            : base("ParseTree.cs")
        {
        }

        public string Generate(Grammar grammar, bool debug)
        {
            if (string.IsNullOrEmpty(grammar.GetTemplatePath()))
            {
                return null;
            }

            // copy the parse tree file (optionally)
            var parseTree = File.ReadAllText(grammar.GetTemplatePath() + TemplateName);

            var evalSymbols = new StringBuilder();
            var evalMethods = new StringBuilder();

            // build non terminal tokens
            foreach (var s in grammar.GetNonTerminals())
            {
                evalSymbols.AppendLine($"                TokenType.{s.Name} => Eval{s.Name}(tree, paramList),");

                evalMethods.AppendLine("        protected virtual object Eval" + s.Name + "(ParseTree tree, params object[] paramList)");
                evalMethods.AppendLine("        {");
                if (s.CodeBlock != null)
                {
                    // paste user code here
                    evalMethods.AppendLine(FormatCodeBlock(s));
                }
                else
                {
                    if (s.Name == "Start") // return a nice warning message from root object.
                    {
                        evalMethods.AppendLine("            return \"Could not interpret input; no semantics implemented.\";");
                    }
                    else
                    {
                        evalMethods.AppendLine("            foreach (var node in Nodes)\r\n" +
                                               "            {\r\n" +
                                               "                node.Eval(tree, paramList);\r\n" +
                                               "            }\r\n" +
                                               "            return null;");
                    }

                    // otherwise simply not implemented!
                }
                evalMethods.AppendLine("        }\r\n");
            }

            if (debug)
            {
                parseTree = parseTree.Replace(@"<%Namespace%>", "TinyPG.Debug");
                parseTree = parseTree.Replace(@"<%ParseError%>", " : TinyPG.Debug.IParseError");
                parseTree = parseTree.Replace(@"<%ParseErrors%>", "List<TinyPG.Debug.IParseError>");
                parseTree = parseTree.Replace(@"<%IParseTree%>", ", TinyPG.Debug.IParseTree");
                parseTree = parseTree.Replace(@"<%IParseNode%>", " : TinyPG.Debug.IParseNode");
                parseTree = parseTree.Replace(@"<%ITokenGet%>", "public IToken IToken { get {return (IToken)Token;} }");

                const string iNodes = "public List<IParseNode> INodes { get { return Nodes.ConvertAll<IParseNode>(new Converter<ParseNode, IParseNode>(n => (IParseNode)n)); }}\r\n\r\n";
                parseTree = parseTree.Replace(@"<%INodesGet%>", iNodes);
            }
            else
            {
                parseTree = parseTree.Replace(@"<%Namespace%>", grammar.Directives["TinyPG"]["Namespace"]);
                parseTree = Regex.Replace(parseTree, @"\s*<%ParseError%>", "");
                parseTree = parseTree.Replace(@"<%ParseErrors%>", "List<ParseError>");
                parseTree = Regex.Replace(parseTree, @"\s*<%IParseTree%>", "");
                parseTree = Regex.Replace(parseTree, @"\s*<%IParseNode%>", "");
                parseTree = Regex.Replace(parseTree, @"\s*<%ITokenGet%>", "");
                parseTree = Regex.Replace(parseTree, @"\s*<%INodesGet%>", "");
            }

            parseTree = Regex.Replace(parseTree, @"[ \t]*<%EvalSymbols%>\r?\n?", evalSymbols.ToString());
            parseTree = parseTree.Replace(@"<%VirtualEvalMethods%>", evalMethods.ToString());

            return parseTree;
        }

        /// <summary>
        /// replaces $ variables with a c# statement
        /// the routine also implements some checks to see if $variables are matching with production symbols
        /// errors are added to the Error object.
        /// </summary>
        /// <param name="nts">non terminal and its production rule</param>
        /// <returns>a formatted codeblock</returns>
        private static string FormatCodeBlock(NonTerminalSymbol nts)
        {
            if (nts == null)
            {
                return "";
            }

            var codeBlock = nts.CodeBlock;

            var varRegex = new Regex(@"\$(?<var>[a-zA-Z_0-9]+)(?:<(?<type>(?><(?<c>)|[^<>]+|>(?<-c>))*(?(c)(?!)))>)?(?<bracket>\[(?<index>[^]]*)\])?", RegexOptions.Compiled);

            var symbols = nts.DetermineProductionSymbols();


            var match = varRegex.Match(codeBlock);
            while (match.Success)
            {
                var s = symbols.Find(match.Groups["var"].Value);
                if (s == null)
                {
                    //TODO: handle error situation
                    //Errors.Add("Variable $" + match.Groups["var"].Value + " cannot be matched.");
                    break; // error situation
                }

                string replacement;
                var variableType = match.Groups["type"].Value;
                var type = variableType.Length > 0 ? $"<{variableType}>" : "";

                // Match list of variables
                if (match.Groups["bracket"].Value == "[]")
                {
                    replacement = $"GetValues{type}(tree, TokenType.{s.Name})";
                }
                else
                {
                    var indexer = match.Groups["index"].Value.Length > 0 ? match.Groups["index"].Value : "0";
                    replacement = $"GetValue{type}(tree, TokenType.{s.Name}, {indexer})";
                }

                codeBlock = codeBlock.Substring(0, match.Captures[0].Index) + replacement + codeBlock.Substring(match.Captures[0].Index + match.Captures[0].Length);
                match = varRegex.Match(codeBlock);
            }

            codeBlock = "            " + codeBlock.Replace("\n", "\r\n        ");
            return codeBlock;
        }
    }
}
