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
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using TinyPG.Compiler;

namespace TinyPG
{
    public class Program
    {
        public enum ExitCode
        {
            Success = 0,
            InvalidFilename = 1,
            UnknownError = 10,
        }

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                var grammarFilePath = Path.GetFullPath(args[0]);
                var output = new StringBuilder(string.Empty);
                if (!File.Exists(grammarFilePath))
                {
                    output.Append("Specified file " + grammarFilePath + " does not exists");
                    Console.WriteLine(output.ToString());
                    return (int)ExitCode.InvalidFilename;
                }

                //As stated in documentation current directory is the one of the TPG file.
                Directory.SetCurrentDirectory(Path.GetDirectoryName(grammarFilePath));

                var startTimer = DateTime.Now;

                var program = new Program(ManageParseError, output);
                var grammar = program.ParseGrammar(File.ReadAllText(grammarFilePath), Path.GetFileName(grammarFilePath));

                if (grammar != null && program.BuildCode(grammar, new Compiler.Compiler()))
                {
                    var span = DateTime.Now.Subtract(startTimer);
                    output.AppendLine("Compilation successful in " + span.TotalMilliseconds + "ms.");
                }

                Console.WriteLine(output.ToString());
            }
            else
            {
                Application.ThreadException += Application_ThreadException;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }

            return (int)ExitCode.Success;
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show("An unhandled exception occured: " + e.Exception.Message);
        }

        public delegate void OnParseErrorDelegate(ParseTree tree, StringBuilder output);
        private readonly OnParseErrorDelegate _parseErrorDelegate;
        public StringBuilder Output { get; }

        public Program(OnParseErrorDelegate parseErrorDelegate, StringBuilder output)
        {
            _parseErrorDelegate = parseErrorDelegate;
            Output = output;
        }

        public Grammar ParseGrammar(string input, string grammarFile)
        {
            Grammar grammar = null;
            var scanner = new Scanner();
            var parser = new Parser(scanner);

            var tree = parser.Parse(input, grammarFile, new GrammarTree());

            if (tree.Errors.Count > 0)
            {
                _parseErrorDelegate(tree, Output);
            }
            else
            {
                grammar = (Grammar)tree.Eval();
                grammar.Preprocess();

                if (tree.Errors.Count == 0)
                {
                    Output.AppendLine(grammar.PrintGrammar());
                    Output.AppendLine(grammar.PrintFirsts());

                    Output.AppendLine("Parse successful!\r\n");
                }
            }
            return grammar;
        }


        public bool BuildCode(Grammar grammar, Compiler.Compiler compiler)
        {

            Output.AppendLine("Building code...");
            compiler.Compile(grammar);
            if (!compiler.IsCompiled)
            {
                foreach (var err in compiler.Errors)
                    Output.AppendLine(err);
                Output.AppendLine("Compilation contains errors, could not compile.");
            }

            new GeneratedFilesWriter(grammar).Generate(false);

            return compiler.IsCompiled;
        }

        protected static void ManageParseError(ParseTree tree, StringBuilder output)
        {
            foreach (var error in tree.Errors)
            {
                output.AppendLine($"({error.Line},{error.Column}): {error.Message}");
            }

            output.AppendLine("Semantic errors in grammar found.");
        }
    }
}
