using System;
using System.IO;
using System.Linq;
using System.Text;
using TinyPG.Compiler;

namespace TinyPG.CodeGenerators.VBNet
{
    public class TextHighlighterGenerator : BaseGenerator, ICodeGenerator
    {
        internal TextHighlighterGenerator()
            : base("TextHighlighter.vb")
        {
        }

        public string Generate(Grammar grammar, bool debug)
        {
            if (string.IsNullOrEmpty(grammar.GetTemplatePath()))
            {
                return null;
            }

            var generatedText = File.ReadAllText(grammar.GetTemplatePath() + TemplateName);
            var tokens = new StringBuilder();
            var colors = new StringBuilder();

            var colorIndex = 1;
            foreach (var t in grammar.GetTerminals().Where(t => t.Attributes.ContainsKey("Color")))
            {
                tokens.AppendLine(Helper.Indent(5) + "Case TokenType." + t.Name + ":");
                tokens.AppendLine(Helper.Indent(6) + @"sb.Append(""{{\cf" + colorIndex + @" "")");
                tokens.AppendLine(Helper.Indent(6) + "Exit Select");

                var red = 0;
                var green = 0;
                var blue = 0;
                var len = t.Attributes["Color"].Length;
                switch (len)
                {
                    case 1:
                    {
                        if (t.Attributes["Color"][0] is long)
                        {
                            var v = Convert.ToInt32(t.Attributes["Color"][0]);
                            red = (v >> 16) & 255;
                            green = (v >> 8) & 255;
                            blue = v & 255;
                        }

                        break;
                    }
                    case 3:
                    {
                        if (t.Attributes["Color"][0] is int || t.Attributes["Color"][0] is long)
                            red = Convert.ToInt32(t.Attributes["Color"][0]) & 255;
                        if (t.Attributes["Color"][1] is int || t.Attributes["Color"][1] is long)
                            green = Convert.ToInt32(t.Attributes["Color"][1]) & 255;
                        if (t.Attributes["Color"][2] is int || t.Attributes["Color"][2] is long)
                            blue = Convert.ToInt32(t.Attributes["Color"][2]) & 255;
                        break;
                    }
                }

                colors.Append($@"\red{red}\green{green}\blue{blue};");
                colorIndex++;
            }

            generatedText = generatedText.Replace(@"<%HighlightTokens%>", tokens.ToString());
            generatedText = generatedText.Replace(@"<%RtfColorPalette%>", colors.ToString());

            generatedText = generatedText.Replace(@"<%Namespace%>", debug ? "TinyPG.Debug" : grammar.Directives["TinyPG"]["Namespace"]);

            return generatedText;
        }
    }
}
