using System.CodeDom.Compiler;
using System.Globalization;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using TinyPG.CodeGenerators.VBNet;
using VBCodeProvider = Microsoft.VisualBasic.VBCodeProvider;

namespace TinyPG.CodeGenerators
{
    public enum SupportedLanguage
    {
        CSharp = 0, // default
        VBNet = 1,
    }

    public static class CodeGeneratorFactory
    {
        public static SupportedLanguage GetSupportedLanguage(string language)
        {
            switch (language.ToLower(CultureInfo.InvariantCulture))
            {
                // set the default templates directory
                case "visualbasic":
                case "vbnet":
                case "vb.net":
                case "vb":
                    return SupportedLanguage.VBNet;
                default: // c# is default language
                    return SupportedLanguage.CSharp;
            }
        }

        public static ICodeGenerator CreateGenerator(string generator, string language)
        {
            switch (GetSupportedLanguage(language))
            {
                // set the default templates directory
                case SupportedLanguage.VBNet:
                    switch (generator)
                    {
                        case "Parser":
                            return new ParserGenerator();
                        case "Scanner":
                            return new ScannerGenerator();
                        case "ParseTree":
                            return new ParseTreeGenerator();
                        case "TextHighlighter":
                            return new TextHighlighterGenerator();
                    }
                    break;
                default: // c# is default language
                    switch (generator)
                    {
                        case "Parser":
                            return new CSharp.ParserGenerator();
                        case "Scanner":
                            return new CSharp.ScannerGenerator();
                        case "ParseTree":
                            return new CSharp.ParseTreeGenerator();
                        case "TextHighlighter":
                            return new CSharp.TextHighlighterGenerator();
                    }
                    break;
            }
            return null; // code generator was not found
        }

        public static CodeDomProvider CreateCodeDomProvider(string language)
        {
            switch (language.ToLower(CultureInfo.InvariantCulture))
            {
                // set the default templates directory
                case "visualbasic":
                case "vbnet":
                case "vb.net":
                case "vb":
                    return new VBCodeProvider();
                default:
                    return new CSharpCodeProvider();
            }
        }

        public static string GetCodeFileExtension(string language)
        {
            switch (language.ToLower(CultureInfo.InvariantCulture))
            {
                // set the default templates directory
                case "visualbasic":
                case "vbnet":
                case "vb.net":
                case "vb":
                    return ".vb";
                default:
                    return "*.cs";
            }
        }
    }
}
