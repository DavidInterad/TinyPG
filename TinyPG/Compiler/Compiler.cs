// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using TinyPG.CodeGenerators;
using TinyPG.Debug;
using CodeDom = System.CodeDom.Compiler;


namespace TinyPG.Compiler
{
    public class Compiler
    {
        private Grammar _grammar;

        /// <summary>
        /// indicates if the grammar was parsed successfully
        /// </summary>
        public bool IsParsed { get; set; }

        /// <summary>
        /// indicates if the grammar was compiled successfully
        /// </summary>
        public bool IsCompiled { get; set; }

        /// <summary>
        /// a string containing the actual generated code for the scanner
        /// </summary>
        public string ScannerCode { get; set; }

        /// <summary>
        /// a string containing the actual generated code for the parser
        /// </summary>
        public string ParserCode { get; set; }

        /// <summary>
        /// a string containing the actual generated code for the Parse tree
        /// </summary>
        public string ParseTreeCode { get; set; }

        /// <summary>
        /// a list of errors that occurred during parsing or compiling
        /// </summary>
        public List<string> Errors { get; set; }

        // the resulting compiled assembly
        private Assembly _assembly;


        public Compiler()
        {
            IsCompiled = false;
            Errors = new List<string>();
        }

        public void Compile(Grammar grammar)
        {
            IsParsed = false;
            IsCompiled = false;
            Errors = new List<string>();

            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar), "Grammar may not be null");
            grammar.Preprocess();
            IsParsed = true;

            BuildCode();
            if (Errors.Count == 0)
            {
                IsCompiled = true;
            }
        }

        /// <summary>
        /// once the grammar compiles correctly, the code can be built.
        /// </summary>
        private void BuildCode()
        {
            var language = _grammar.Directives["TinyPG"]["Language"];
            var provider = CodeGeneratorFactory.CreateCodeDomProvider(language);

            // set KeepFiles to true to debug generated source
            var tempFileCollection = new CodeDom.TempFileCollection();
            var compilerParams = new CodeDom.CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                TempFiles = tempFileCollection,
            };
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            compilerParams.ReferencedAssemblies.Add("System.Drawing.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.dll");

            // reference this assembly to share interfaces (for debugging only)

            var tinyPgFile = Assembly.GetExecutingAssembly().Location;
            compilerParams.ReferencedAssemblies.Add(tinyPgFile);

            // generate the code with debug interface enabled
            var sources = new List<string>();
            foreach (var d in _grammar.Directives)
            {
                var generator = CodeGeneratorFactory.CreateGenerator(d.Name, language);
                if (generator != null && d.TryGetValue("FileName", out var fileName))
                {
                    generator.FileName = fileName;
                }

                if (generator != null && d["Generate"].ToLower() == "true")
                {
                    sources.Add(generator.Generate(_grammar, true));
                }
            }

            AddAdditionalSources(sources, language);

            if (sources.Count <= 0)
            {
                return;
            }

            var result = provider.CompileAssemblyFromSource(compilerParams, sources.ToArray());
            if (result.Errors.Count > 0)
            {
                foreach (CodeDom.CompilerError o in result.Errors)
                {
                    Errors.Add($"{o.ErrorText} on line {o.Line}");
                }
            }
            else
            {
                _assembly = result.CompiledAssembly;
            }
        }

        private void AddAdditionalSources(ICollection<string> sources, string language)
        {
            var dtoFilePath = _grammar.GetDtoPath();
            if (dtoFilePath == null)
            {
                return;
            }

            var hasRecords = false;
            var codeFileExtension = CodeGeneratorFactory.GetCodeFileExtension(language);
            foreach (var file in Directory.EnumerateFiles(dtoFilePath, $"*{codeFileExtension}"))
            {
                var dtoCode = File.ReadAllText(file);
                if (Regex.IsMatch(dtoCode, @"\brecord\b"))
                {
                    hasRecords = true;
                }

                sources.Add(dtoCode);
            }

            if (hasRecords)
            {
                sources.Add(RecordTypeSource);
            }
        }

        /// <summary>
        /// evaluate the input expression
        /// </summary>
        /// <param name="input">the expression to evaluate with the parser</param>
        /// <returns>the output of the parser/compiler</returns>
        public CompilerResult Run(string input) => Run(input, null);

        public CompilerResult Run(string input, RichTextBox textHighlight)
        {
            if (_assembly == null)
            {
                return null;
            }

            var compilerResult = new CompilerResult();
            string output = null;

            var scannerInstance = _assembly.CreateInstance("TinyPG.Debug.Scanner");
            var scanner = scannerInstance.GetType();

            var parserInstance = (IParser)_assembly.CreateInstance("TinyPG.Debug.Parser", true, BindingFlags.CreateInstance, null, new[] { scannerInstance }, null, null);
            var parserType = parserInstance.GetType();

            var treeInstance = parserType.InvokeMember("Parse", BindingFlags.InvokeMethod, null, parserInstance, new object[] { input, string.Empty });
            var iTree = treeInstance as IParseTree;

            compilerResult.ParseTree = iTree;
            var treeType = treeInstance.GetType();

            var errors = (List<IParseError>)treeType.InvokeMember("Errors", BindingFlags.GetField, null, treeInstance, null);

            if (textHighlight != null && errors.Count == 0)
            {
                // try highlight the input text
                var highlighterInstance = _assembly.CreateInstance("TinyPG.Debug.TextHighlighter", true, BindingFlags.CreateInstance, null, new[] { textHighlight, scannerInstance, parserInstance }, null, null);
                if (highlighterInstance != null)
                {
                    output += "Highlighting input..." + "\r\n";
                    var highlighterType = highlighterInstance.GetType();
                    // highlight the input text only once
                    highlighterType.InvokeMember("HighlightText", BindingFlags.InvokeMethod, null, highlighterInstance, null);

                    // let this thread sleep so background thread can highlight the text
                    Thread.Sleep(20);

                    // dispose of the highlighter object
                    highlighterType.InvokeMember("Dispose", BindingFlags.InvokeMethod, null, highlighterInstance, null);
                }
            }

            if (errors.Count > 0)
            {
                output = errors.Aggregate(output, (current, err) =>
                    $"{current}({err.Line},{err.Column}): {err.Message}\r\n");
            }
            else
            {
                output += "Parse was successful." + "\r\n";
                output += "Evaluating...";

                // parsing was successful, now try to evaluate... this should really be done on a separate thread.
                // e.g. if the thread hangs, it will hang the entire application (!)
                try
                {
                    compilerResult.Value = iTree.Eval(null);
                    output += "\r\nResult: " + JsonSerializer.Serialize(compilerResult.Value, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                    });
                }
                catch (Exception exc)
                {
                    output += "\r\nException occurred: " + exc.Message;
                    output += "\r\nStacktrace: " + exc.StackTrace;
                }

            }
            compilerResult.Output = output;
            return compilerResult;
        }

        private const string RecordTypeSource = @"using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This one is required by the compiler to allow records and init setters with older .NET versions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}
";
    }
}
