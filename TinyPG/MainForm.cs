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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using TinyPG.CodeGenerators;
using TinyPG.Compiler;
using TinyPG.Controls;
using TinyPG.Debug;
using TinyPG.Highlighter;
using Timer = System.Windows.Forms.Timer;

namespace TinyPG
{
    public partial class MainForm : Form
    {
        #region member declarations

        // the compiler used to evaluate the input
        private Compiler.Compiler _compiler;
        private Grammar _grammar;


        // indicates if text/grammar has changed
        private bool _isDirty;

        // the current file the user is editing
        private string _grammarFile;

        // manages docking and floating of panels
        private DockExtender _dockExtender;
        // used to make the input pane floating/draggable
        private IFloaty _inputFloaty;
        // used to make the output pane floating/draggable
        private IFloaty _outputFloaty;

        // marks erroneous text with little waves
        // this is used in combination with the checker
        private TextMarker _marker;
        // checks the syntax/semantics while editing on a separate thread
        private SyntaxChecker _checker;

        // timer that will fire if the changed text requires evaluating
        private Timer _textChangedTimer;

        // responsible for text highlighting
        private TextHighlighter _textHighlighter;

        // scanner to be used by the highlighter, declare here
        // so we can modify the scanner properties at runtime if needed
        private Highlighter.Scanner _highlighterScanner;

        // autocomplete popup form
        private AutoComplete _codeComplete;
        private AutoComplete _directiveComplete;

        // keep this event handler reference in a separate object, so it can be
        // unregistered on closing. this is required because the checker runs on a separate thread
        private EventHandler _syntaxUpdateChecker;

        #endregion

        #region Initialization

        public MainForm()
        {
            InitializeComponent();
            _isDirty = false;
            _compiler = null;
            _grammarFile = null;

            Disposed += MainForm_Disposed;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            headerEvaluator.Activate(textInput);
            headerEvaluator.CloseClick += headerEvaluator_CloseClick;
            headerOutput.Activate(tabOutput);
            headerOutput.CloseClick += headerOutput_CloseClick;

            _dockExtender = new DockExtender(this);
            _inputFloaty = _dockExtender.Attach(panelInput, headerEvaluator, splitterBottom);
            _outputFloaty = _dockExtender.Attach(panelOutput, headerOutput, splitterRight);

            _inputFloaty.Docking += inputFloaty_Docking;
            _outputFloaty.Docking += inputFloaty_Docking;
            _inputFloaty.Hide();
            _outputFloaty.Hide();

            textOutput.Text = $"{AssemblyInfo.ProductName} v{Application.ProductVersion}\r\n";
            textOutput.Text += $"{AssemblyInfo.CopyRightsDetail}\r\n\r\n";


            _marker = new TextMarker(textEditor);
            _checker = new SyntaxChecker(_marker); // run the syntax checker on separate thread

            // create the syntax update checker event handler and remember its reference
            _syntaxUpdateChecker = checker_UpdateSyntax;
            _checker.UpdateSyntax += _syntaxUpdateChecker; // listen for events
            var thread = new Thread(_checker.Start);
            thread.Start();

            _textChangedTimer = new Timer();
            _textChangedTimer.Tick += TextChangedTimer_Tick;

            // assign the auto completion function to this editor
            // autocomplete form will take care of the rest
            _codeComplete = new AutoComplete(textEditor)
            {
                Enabled = false,
            };
            _directiveComplete = new AutoComplete(textEditor)
            {
                Enabled = false,
            };
            _directiveComplete.WordList.Items.Add("@ParseTree");
            _directiveComplete.WordList.Items.Add("@Parser");
            _directiveComplete.WordList.Items.Add("@Scanner");
            _directiveComplete.WordList.Items.Add("@TextHighlighter");
            _directiveComplete.WordList.Items.Add("@TinyPG");
            _directiveComplete.WordList.Items.Add("Generate");
            _directiveComplete.WordList.Items.Add("Language");
            _directiveComplete.WordList.Items.Add("Namespace");
            _directiveComplete.WordList.Items.Add("OutputPath");
            _directiveComplete.WordList.Items.Add("TemplatePath");

            // setup the text highlighter (= text coloring)
            _highlighterScanner = new Highlighter.Scanner();
            _textHighlighter = new TextHighlighter(textEditor, _highlighterScanner, new Highlighter.Parser(_highlighterScanner));
            _textHighlighter.SwitchContext += TextHighlighter_SwitchContext;

            LoadConfig();

            if (_grammarFile == null)
            {
                NewGrammar();
            }

        }
        #endregion Initialization

        #region Control events
        /// <summary>
        /// a context switch is raised when the caret of the editor moves from one section to another.
        /// the sections are defined by the highlighter parser. E.g. if the caret moves from the COMMENTBLOCK to
        /// a CODEBLOCK token, the contextswitch is raised.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextHighlighter_SwitchContext(object sender, ContextSwitchEventArgs e)
        {

            if (e.NewContext.Token.Type == Highlighter.TokenType.DOTNET_COMMENTBLOCK
                || e.NewContext.Token.Type == Highlighter.TokenType.DOTNET_COMMENTLINE
                || e.NewContext.Token.Type == Highlighter.TokenType.DOTNET_STRING
                || e.NewContext.Token.Type == Highlighter.TokenType.GRAMMARSTRING
                || e.NewContext.Token.Type == Highlighter.TokenType.DIRECTIVESTRING
                || e.NewContext.Token.Type == Highlighter.TokenType.GRAMMARCOMMENTBLOCK
                || e.NewContext.Token.Type == Highlighter.TokenType.GRAMMARCOMMENTLINE)
            {
                _codeComplete.Enabled = false; // disable auto-completion if user is editing in any of these blocks
                _directiveComplete.Enabled = false;
            }
            else switch (e.NewContext.Parent.Token.Type)
            {
                case Highlighter.TokenType.GrammarBlock:
                    _directiveComplete.Enabled = false;
                    _codeComplete.Enabled = true; //allow auto-completion
                    break;
                case Highlighter.TokenType.DirectiveBlock:
                    _codeComplete.Enabled = false;
                    _directiveComplete.Enabled = true; //allow directives auto-completion
                    break;
                default:
                    _codeComplete.Enabled = false;
                    _directiveComplete.Enabled = false;
                    break;
            }

        }

        private void checker_UpdateSyntax(object sender, EventArgs e)
        {
            if (InvokeRequired && !IsDisposed)
            {
                Invoke(new EventHandler(checker_UpdateSyntax), sender, e);
                return;
            }

            _marker.MarkWords();

            if (_checker.Grammar == null)
            {
                return;
            }

            if (_codeComplete.Visible)
            {
                return;
            }

            lock (_checker.Grammar)
            {
                var startAdded = false;
                _codeComplete.WordList.Items.Clear();
                foreach (var s in _checker.Grammar.Symbols)
                {
                    _codeComplete.WordList.Items.Add(s.Name);
                    if (s.Name == "Start")
                    {
                        startAdded = true;
                    }
                }

                if (!startAdded)
                {
                    _codeComplete.WordList.Items.Add("Start");
                }
            }
        }

        private void inputFloaty_Docking(object sender, EventArgs e)
        {
            textEditor.BringToFront();
        }

        #endregion Control events

        #region Form events

        private void MainForm_Disposed(object sender, EventArgs e)
        {
            // unregister event handler.
            _checker.UpdateSyntax -= _syntaxUpdateChecker; // listen for events

            _checker.Dispose();
            _marker.Dispose();
        }

        private void headerOutput_CloseClick(object sender, EventArgs e)
        {
            _outputFloaty.Hide();
        }

        private void headerEvaluator_CloseClick(object sender, EventArgs e)
        {
            _inputFloaty.Hide();
        }

        private void menuToolsGenerate_Click(object sender, EventArgs e)
        {
            _outputFloaty.Show();
            tabOutput.SelectedIndex = 0;

            CompileGrammar();

            if (_compiler != null && _compiler.Errors.Count == 0)
            {
                // save the grammar when compilation was successful
                SaveGrammar(_grammarFile);
            }

        }

        private void parseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _inputFloaty.Show();
            _outputFloaty.Show();
            if (tabOutput.SelectedIndex != 0 && tabOutput.SelectedIndex != 1)
            {
                tabOutput.SelectedIndex = 0;
            }

            EvaluateExpression();
        }


        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            if (_textHighlighter.IsHighlighting)
            {
                return;
            }

            _marker.Clear();
            _textChangedTimer.Stop();
            _textChangedTimer.Interval = 3000;
            _textChangedTimer.Start();

            if (!_isDirty)
            {
                _isDirty = true;
                SetFormCaption();
            }
        }

        private void TextChangedTimer_Tick(object sender, EventArgs e)
        {
            _textChangedTimer.Stop();

            textEditor.Invalidate();
            _checker.Check(textEditor.Text);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isDirty)
            {
                SaveGrammarAs();
            }

            NewGrammar();
        }


        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var newGrammarFile = OpenGrammar();
            if (newGrammarFile == null)
            {
                return;
            }

            if (_isDirty && _grammarFile != null)
            {
                var r = MessageBox.Show(this, "You will lose current changes, continue?", "Lose changes", MessageBoxButtons.OKCancel);
                if (r == DialogResult.Cancel)
                {
                    return;
                }
            }

            _grammarFile = newGrammarFile;
            LoadGrammarFile();
            SaveConfig();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_grammarFile))
            {
                SaveGrammarAs();
            }
            else
            {
                SaveGrammar(_grammarFile);
            }

            SaveConfig();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveGrammarAs();
            SaveConfig();
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
            Application.Exit();
        }

        private void tvParsetree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!(e.Node?.Tag is IParseNode ipn))
            {
                return;
            }

            textInput.Select(ipn.IToken.StartPos, ipn.IToken.EndPos - ipn.IToken.StartPos);
            textInput.ScrollToCaret();
        }

        private void expressionEvaluatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _inputFloaty.Show();
            textInput.Focus();
        }

        private void outputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _outputFloaty.Show();
            tabOutput.SelectedIndex = 0;
        }

        private void parsetreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _outputFloaty.Show();
            tabOutput.SelectedIndex = 1;
        }

        private void regexToolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _outputFloaty.Show();
            tabOutput.SelectedIndex = 2;
        }

        private void tabOutput_Selected(object sender, TabControlEventArgs e)
        {
            headerOutput.Text = e.TabPage.Text;
        }

        private void textEditor_SelectionChanged(object sender, EventArgs e)
        {
            SetStatusbar();
        }

        private void textInput_SelectionChanged(object sender, EventArgs e)
        {
            SetStatusbar();
        }

        private void textInput_Enter(object sender, EventArgs e)
        {
            SetStatusbar();
        }

        private void textInput_Leave(object sender, EventArgs e)
        {
            SetStatusbar();
        }

        private void textEditor_Enter(object sender, EventArgs e)
        {
            SetStatusbar();
        }

        private void textEditor_Leave(object sender, EventArgs e)
        {
            SetStatusbar();
        }

        private void aboutTinyParserGeneratorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutTinyPG();
        }

        private void viewParserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewFile("Parser");
        }

        private void viewScannerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewFile("Scanner");
        }

        private void viewParseTreeCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewFile("ParseTree");
        }

        private void expressionEvaluatorToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\simple expression1.tpg");
        }

        private void codeblocksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\simple expression2.tpg");
        }

        private void theTinyPGGrammarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\BNFGrammar v1.1.tpg");
        }

        private void theTinyPGGrammarV10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\BNFGrammar v1.0.tpg");
        }

        private void theTinyPGGrammarHighlighterV12ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NotepadViewFile(AppDomain.CurrentDomain.BaseDirectory + @"Examples\GrammarHighlighter.tpg");
        }

        private void textOutput_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                if (e.LinkText == "www.codeproject.com")
                {
                    Process.Start("http://www.codeproject.com/script/Articles/MemberArticles.aspx?amid=2192187");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion Form events

        #region Processing functions

        private static void NotepadViewFile(string filename)
        {
            try
            {
                Process.Start("Notepad.exe", filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ViewFile(string fileType)
        {
            try
            {
                if (_isDirty || _compiler == null || !_compiler.IsCompiled)
                {
                    CompileGrammar();
                }

                if (_grammar == null)
                {
                    return;
                }

                var generator = CodeGeneratorFactory.CreateGenerator(fileType, _grammar.Directives["TinyPG"]["Language"]);
                var folder = _grammar.GetOutputPath() + generator.FileName;
                Process.Start(folder);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void EvaluateExpression()
        {
            textOutput.Text = "Parsing expression...\r\n";
            try
            {

                if (_isDirty || _compiler == null || !_compiler.IsCompiled)
                {
                    CompileGrammar();
                }

                if (string.IsNullOrEmpty(_grammarFile))
                {
                    return;
                }

                // save the grammar when compilation was successful
                if (_compiler != null && _compiler.Errors.Count == 0)
                {
                    SaveGrammar(_grammarFile);
                }

                if (_compiler?.IsCompiled == true)
                {
                    var result = _compiler.Run(textInput.Text, textInput.RichTextBox);

                    //textOutput.Text = result.ParseTree.PrintTree();
                    textOutput.Text += result.Output;
                    ParseTreeViewer.Populate(tvParsetree, result.ParseTree);
                }
            }
            catch (Exception exc)
            {
                textOutput.Text +=
                    $"An exception occured compiling the assembly: \r\n{exc.Message}\r\n{exc.StackTrace}";
            }

        }

        /// <summary>
        /// this is where some of the magic happens
        /// to highlight specific C# code or VB code, the language specific keywords are swapped
        /// that is, the DOTNET regexps are overwritten by either the c# or VB regexps
        /// </summary>
        /// <param name="language"></param>
        private void SetHighlighterLanguage(string language)
        {
            lock (TextHighlighter.TreeLock)
            {
                switch (CodeGeneratorFactory.GetSupportedLanguage(language))
                {
                    case SupportedLanguage.VBNet:
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_STRING] = _highlighterScanner.Patterns[Highlighter.TokenType.VB_STRING];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_SYMBOL] = _highlighterScanner.Patterns[Highlighter.TokenType.VB_SYMBOL];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTBLOCK] = _highlighterScanner.Patterns[Highlighter.TokenType.VB_COMMENTBLOCK];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTLINE] = _highlighterScanner.Patterns[Highlighter.TokenType.VB_COMMENTLINE];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_KEYWORD] = _highlighterScanner.Patterns[Highlighter.TokenType.VB_KEYWORD];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_NONKEYWORD] = _highlighterScanner.Patterns[Highlighter.TokenType.VB_NONKEYWORD];
                        break;
                    default:
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_STRING] = _highlighterScanner.Patterns[Highlighter.TokenType.CS_STRING];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_SYMBOL] = _highlighterScanner.Patterns[Highlighter.TokenType.CS_SYMBOL];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTBLOCK] = _highlighterScanner.Patterns[Highlighter.TokenType.CS_COMMENTBLOCK];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_COMMENTLINE] = _highlighterScanner.Patterns[Highlighter.TokenType.CS_COMMENTLINE];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_KEYWORD] = _highlighterScanner.Patterns[Highlighter.TokenType.CS_KEYWORD];
                        _highlighterScanner.Patterns[Highlighter.TokenType.DOTNET_NONKEYWORD] = _highlighterScanner.Patterns[Highlighter.TokenType.CS_NONKEYWORD];
                        break;
                }
                _textHighlighter.HighlightText();
            }

        }

        private void ManageParseError(ParseTree tree, StringBuilder output)
        {
            foreach (var error in tree.Errors)
            {
                output.AppendLine($"({error.Line},{error.Column}): {error.Message}");
            }

            output.AppendLine("Semantic errors in grammar found.");
            textEditor.Select(tree.Errors[0].Position, tree.Errors[0].Length > 0 ? tree.Errors[0].Length : 1);
        }

        private void CompileGrammar()
        {
            if (string.IsNullOrEmpty(_grammarFile))
            {
                SaveGrammarAs();
            }

            if (string.IsNullOrEmpty(_grammarFile))
            {
                return;
            }

            _compiler = new Compiler.Compiler();
            var output = new StringBuilder();

            // clear tree
            tvParsetree.Nodes.Clear();

            var prog = new Program(ManageParseError, output);
            var startTimer = DateTime.Now;
            _grammar = prog.ParseGrammar(textEditor.Text, _grammarFile);

            if (_grammar != null)
            {
                SetHighlighterLanguage(_grammar.Directives["TinyPG"]["Language"]);

                if (prog.BuildCode(_grammar, _compiler))
                {
                    var span = DateTime.Now.Subtract(startTimer);
                    output.AppendLine("Compilation successful in " + span.TotalMilliseconds + "ms.");
                }
            }

            textOutput.Text = output.ToString();
            textOutput.Select(textOutput.Text.Length, 0);
            textOutput.ScrollToCaret();
        }

        private void AboutTinyPG()
        {
            var about = new StringBuilder();

            //http://www.codeproject.com/script/Articles/MemberArticles.aspx?amid=2192187

            about.AppendLine($"{AssemblyInfo.ProductName} v{Application.ProductVersion}");
            about.AppendLine(AssemblyInfo.CopyRightsDetail);
            about.AppendLine();
            about.AppendLine("For more information about the author");
            about.AppendLine("or TinyPG visit www.codeproject.com");

            _outputFloaty.Show();
            tabOutput.SelectedIndex = 0;
            textOutput.Text = about.ToString();

        }

        private void SetFormCaption()
        {
            Text = "@TinyPG - a Tiny Parser Generator .Net";
            if (_grammarFile == null || !File.Exists(_grammarFile))
            {
                if (_isDirty)
                {
                    Text += " *";
                }

                return;
            }

            var name = new FileInfo(_grammarFile).Name;
            Text += $" [{name}]";
            if (_isDirty)
            {
                Text += " *";
            }
        }

        private void NewGrammar()
        {
            _grammarFile = null;
            _isDirty = false;

            var text = $"//{AssemblyInfo.ProductName} v{Application.ProductVersion}\r\n";
            text += $"//{AssemblyInfo.CopyRightsDetail}\r\n\r\n";
            textEditor.Text = text;
            textEditor.RichTextBox.ClearUndo();

            textOutput.Text = $"{AssemblyInfo.ProductName} v{Application.ProductVersion}\r\n";
            textOutput.Text += $"{AssemblyInfo.CopyRightsDetail}\r\n\r\n";

            SetFormCaption();
            SaveConfig();

            textEditor.Select(textEditor.Text.Length, 0);

            _isDirty = false;
            _textHighlighter.ClearUndo();
            SetFormCaption();
            SetStatusbar();

        }
        private void LoadGrammarFile()
        {
            if (_grammarFile == null)
            {
                return;
            }

            if (!File.Exists(_grammarFile))
            {
                _grammarFile = null; // file does not exist anymore
                return;
            }

            var folder = new FileInfo(_grammarFile).DirectoryName;
            Directory.SetCurrentDirectory(folder);

            textEditor.Text = File.ReadAllText(_grammarFile);
            textEditor.RichTextBox.ClearUndo();
            CompileGrammar();
            textOutput.Text = "";
            textEditor.Focus();
            SetStatusbar();
            _textHighlighter.ClearUndo();
            _isDirty = false;
            SetFormCaption();
            textEditor.Select(0, 0);
            _checker.Check(textEditor.Text);
        }

        private void SaveGrammarAs()
        {
            var r = saveFileDialog.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                SaveGrammar(saveFileDialog.FileName);
            }
        }

        private string OpenGrammar()
        {
            var r = openFileDialog.ShowDialog(this);
            return r == DialogResult.OK ? openFileDialog.FileName : null;
        }

        private void SaveGrammar(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return;
            }

            _grammarFile = filename;

            var folder = new FileInfo(_grammarFile).DirectoryName;
            Directory.SetCurrentDirectory(folder);

            var text = textEditor.Text.Replace("\n", "\r\n");
            File.WriteAllText(filename, text);
            _isDirty = false;
            SetFormCaption();
        }

        private void LoadConfig()
        {
            try
            {
                var configFile = AppDomain.CurrentDomain.BaseDirectory + "TinyPG.config";

                if (!File.Exists(configFile))
                {
                    return;
                }

                var doc = new XmlDocument();
                doc.Load(configFile);
                openFileDialog.InitialDirectory = doc["AppSettings"]["OpenFilePath"].Attributes[0].Value;
                saveFileDialog.InitialDirectory = doc["AppSettings"]["SaveFilePath"].Attributes[0].Value;
                _grammarFile = doc["AppSettings"]["GrammarFile"].Attributes[0].Value;

                if (string.IsNullOrEmpty(_grammarFile))
                {
                    NewGrammar();
                }
                else
                {
                    LoadGrammarFile();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void SaveConfig()
        {
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TinyPG.config");
            var doc = new XmlDocument();
            XmlNode settings = doc.CreateElement("AppSettings", "TinyPG");
            doc.AppendChild(settings);

            XmlNode node = doc.CreateElement("OpenFilePath", "TinyPG");
            settings.AppendChild(node);
            node = doc.CreateElement("SaveFilePath", "TinyPG");
            settings.AppendChild(node);
            node = doc.CreateElement("GrammarFile", "TinyPG");
            settings.AppendChild(node);

            var attr = doc.CreateAttribute("Value");
            settings["OpenFilePath"].Attributes.Append(attr);
            if (File.Exists(openFileDialog.FileName))
            {
                attr.Value = new FileInfo(openFileDialog.FileName).Directory.FullName;
            }

            attr = doc.CreateAttribute("Value");
            settings["SaveFilePath"].Attributes.Append(attr);
            if (File.Exists(saveFileDialog.FileName))
            {
                attr.Value = new FileInfo(saveFileDialog.FileName).Directory.FullName;
            }

            attr = doc.CreateAttribute("Value");
            attr.Value = _grammarFile;
            settings["GrammarFile"].Attributes.Append(attr);

            doc.Save(configFile);
        }

        private void SetStatusbar()
        {
            if (textEditor.Focused)
            {
                var pos = textEditor.RichTextBox.SelectionStart;
                statusPos.Text = pos.ToString(CultureInfo.InvariantCulture);
                statusCol.Text = (pos - textEditor.RichTextBox.GetFirstCharIndexOfCurrentLine() + 1).ToString(CultureInfo.InvariantCulture);
                statusLine.Text = (textEditor.RichTextBox.GetLineFromCharIndex(pos) + 1).ToString(CultureInfo.InvariantCulture);

            }
            else if (textInput.Focused)
            {
                var pos = textInput.RichTextBox.SelectionStart;
                statusPos.Text = pos.ToString(CultureInfo.InvariantCulture);
                statusCol.Text = (pos - textInput.RichTextBox.GetFirstCharIndexOfCurrentLine() + 1).ToString(CultureInfo.InvariantCulture);
                statusLine.Text = (textInput.RichTextBox.GetLineFromCharIndex(pos) + 1).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                statusPos.Text = "-";
                statusCol.Text = "-";
                statusLine.Text = "-";
            }
        }

        #endregion
    }
}
