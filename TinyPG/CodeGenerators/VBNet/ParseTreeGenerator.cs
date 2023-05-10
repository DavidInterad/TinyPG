using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TinyPG.Compiler;

namespace TinyPG.CodeGenerators.VBNet
{
    public class ParseTreeGenerator : BaseGenerator, ICodeGenerator
    {
        internal ParseTreeGenerator()
            : base("ParseTree.vb")
        {
        }

        public string Generate(Grammar grammar, bool debug)
        {
            if (string.IsNullOrEmpty(grammar.GetTemplatePath()))
                return null;

            // copy the parse tree file (optionally)
            var parseTree = File.ReadAllText(grammar.GetTemplatePath() + TemplateName);

            var evalSymbols = new StringBuilder();
            var evalMethods = new StringBuilder();

            // build non terminal tokens
            foreach (var s in grammar.GetNonTerminals())
            {
                evalSymbols.AppendLine("                Case TokenType." + s.Name + "");
                evalSymbols.AppendLine("                    Value = Eval" + s.Name + "(tree, paramList)");
                evalSymbols.AppendLine("                    Exit Select");

                evalMethods.AppendLine("        Protected Overridable Function Eval" + s.Name + "(ByVal tree As ParseTree, ByVal ParamArray paramList As Object()) As Object");
                if (s.CodeBlock != null)
                {
                    // paste user code here
                    evalMethods.AppendLine(FormatCodeBlock(s));
                }
                else
                {
                    evalMethods.AppendLine(s.Name == "Start" // return a nice warning message from root object.
                        ? "            Return \"Could not interpret input; no semantics implemented.\""
                        : "            Throw New NotImplementedException()");

                    // otherwise simply not implemented!
                }
                evalMethods.AppendLine("        End Function\r\n");
            }

            if (debug)
            {
                parseTree = parseTree.Replace(@"<%Imports%>", "Imports TinyPG.Debug");
                parseTree = parseTree.Replace(@"<%Namespace%>", "TinyPG.Debug");
                parseTree = parseTree.Replace(@"<%IParseTree%>", "\r\n        Implements IParseTree");
                parseTree = parseTree.Replace(@"<%IParseNode%>", "\r\n        Implements IParseNode\r\n");
                parseTree = parseTree.Replace(@"<%ParseError%>", "\r\n        Implements IParseError\r\n");
                parseTree = parseTree.Replace(@"<%ParseErrors%>", "List(Of IParseError)");

                const string iToken = "        Public ReadOnly Property IToken() As IToken Implements IParseNode.IToken\r\n"
                                      + "            Get\r\n"
                                      + "                Return DirectCast(Token, IToken)\r\n"
                                      + "            End Get\r\n"
                                      + "        End Property\r\n";

                parseTree = parseTree.Replace(@"<%ITokenGet%>", iToken);


                parseTree = parseTree.Replace(@"<%ImplementsIParseTreePrintTree%>", " Implements IParseTree.PrintTree");
                parseTree = parseTree.Replace(@"<%ImplementsIParseTreeEval%>", " Implements IParseTree.Eval");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorCode%>", " Implements IParseError.Code");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorLine%>", " Implements IParseError.Line");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorColumn%>", " Implements IParseError.Column");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorPosition%>", " Implements IParseError.Position");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorLength%>", " Implements IParseError.Length");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorMessage%>", " Implements IParseError.Message");

                const string iNodes = "        Public Shared Function Node2INode(ByVal node As ParseNode) As IParseNode\r\n"
                                      + "            Return DirectCast(node, IParseNode)\r\n"
                                      + "        End Function\r\n\r\n"
                                      + "        Public ReadOnly Property INodes() As List(Of IParseNode) Implements IParseNode.INodes\r\n"
                                      + "            Get\r\n"
                                      + "                Return Nodes.ConvertAll(Of IParseNode)(New Converter(Of ParseNode, IParseNode)(AddressOf Node2INode))\r\n"
                                      + "            End Get\r\n"
                                      + "        End Property\r\n";
                parseTree = parseTree.Replace(@"<%INodesGet%>", iNodes);
                parseTree = parseTree.Replace(@"<%ImplementsIParseNodeText%>", " Implements IParseNode.Text");

            }
            else
            {
                parseTree = parseTree.Replace(@"<%Imports%>", "");
                parseTree = parseTree.Replace(@"<%Namespace%>", grammar.Directives["TinyPG"]["Namespace"]);
                parseTree = parseTree.Replace(@"<%ParseError%>", "");
                parseTree = parseTree.Replace(@"<%ParseErrors%>", "List(Of ParseError)");
                parseTree = parseTree.Replace(@"<%IParseTree%>", "");
                parseTree = parseTree.Replace(@"<%IParseNode%>", "");
                parseTree = parseTree.Replace(@"<%ITokenGet%>", "");
                parseTree = parseTree.Replace(@"<%INodesGet%>", "");

                parseTree = parseTree.Replace(@"<%ImplementsIParseTreePrintTree%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseTreeEval%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorCode%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorLine%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorColumn%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorPosition%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorLength%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseErrorMessage%>", "");
                parseTree = parseTree.Replace(@"<%ImplementsIParseNodeText%>", "");
            }

            parseTree = parseTree.Replace(@"<%EvalSymbols%>", evalSymbols.ToString());
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

            var codeblock = nts.CodeBlock;

            var var = new Regex(@"\$(?<var>[a-zA-Z_0-9]+)(\[(?<index>[^]]+)\])?", RegexOptions.Compiled);

            var symbols = nts.DetermineProductionSymbols();


            var match = var.Match(codeblock);
            while (match.Success)
            {
                var s = symbols.Find(match.Groups["var"].Value);
                if (s == null)
                {
                    break; // error situation
                }

                var indexer = "0";
                if (match.Groups["index"].Value.Length > 0)
                {
                    indexer = match.Groups["index"].Value;
                }

                var replacement = "Me.GetValue(tree, TokenType." + s.Name + ", " + indexer + ")";

                codeblock = codeblock.Substring(0, match.Captures[0].Index) + replacement + codeblock.Substring(match.Captures[0].Index + match.Captures[0].Length);
                match = var.Match(codeblock);
            }

            codeblock = "            " + codeblock.Replace("\n", "\r\n        ");
            return codeblock;
        }
    }
}
