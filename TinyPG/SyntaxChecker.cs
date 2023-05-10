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
using System.Drawing;
using System.Threading;
using TinyPG.Compiler;
using TinyPG.Controls;

namespace TinyPG
{
    public sealed class SyntaxChecker : IDisposable
    {
        private readonly TextMarker _marker;
        private bool _disposing;
        private string _text;
        private bool _textChanged;

        // used by the checker to check the syntax of the grammar while editing
        public ParseTree SyntaxTree { get; set; }

        // contains the runtime compiled grammar
        public Grammar Grammar { get; set; }

        public event EventHandler UpdateSyntax;

        public SyntaxChecker(TextMarker marker)
        {
            UpdateSyntax = null;
            _marker = marker;
            _disposing = false;
        }

        public void Start()
        {
            var scanner = new Scanner();
            var parser = new Parser(scanner);

            while (!_disposing)
            {
                Thread.Sleep(250);
                if (!_textChanged)
                {
                    continue;
                }

                _textChanged = false;

                scanner.Init(_text);
                SyntaxTree = parser.Parse(_text, "", new GrammarTree());
                if (SyntaxTree.Errors.Count > 0)
                {
                    SyntaxTree.Errors.Clear();
                }

                try
                {
                    if (Grammar == null)
                    {
                        Grammar = (Grammar)SyntaxTree.Eval();
                    }
                    else
                    {
                        lock (Grammar)
                        {
                            Grammar = (Grammar)SyntaxTree.Eval();
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                if (_textChanged)
                {
                    continue;
                }

                lock (_marker)
                {
                    _marker.Clear();
                    foreach (var err in SyntaxTree.Errors)
                    {
                        _marker.AddWord(err.Position, err.Length, Color.Red, err.Message);
                    }
                }

                UpdateSyntax?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Check(string text)
        {
            _text = text;
            _textChanged = true;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _disposing = true;
        }

        #endregion
    }
}
