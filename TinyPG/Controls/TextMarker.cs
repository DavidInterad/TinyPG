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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TinyPG.Controls
{
    /// <summary>
    /// the text marker is responsible for underlining erroneous text (= marked words) with a wavy line
    /// the text marker also handles the display of the tooltip
    /// </summary>
    public sealed class TextMarker : NativeWindow, IDisposable
    {
        public RichTextBox TextBox;
        private List<Word> _markedWords;
        private readonly ToolTip _toolTip;
        private Point _lastMousePos;

        private struct Word
        {
            public int Start;
            public int Length;
            public Color Color;
            public string ToolTip;
        }

        public TextMarker(NumberedTextBox textBox)
        {
            TextBox = textBox.RichTextBox;
            TextBox.MouseMove += TextBoxMouseMove;
            AssignHandle(TextBox.Handle);
            _toolTip = new ToolTip();
            Clear();
            _lastMousePos = new Point();
        }

        void TextBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (_lastMousePos.X == e.X || _lastMousePos.Y == e.Y)
            {
                return;
            }

            _lastMousePos = new Point(e.X, e.Y);
            var i = TextBox.GetCharIndexFromPosition(_lastMousePos);

            var found = false;
            foreach (var w in _markedWords)
            {
                if (w.Start > i || w.Start + w.Length <= i)
                {
                    continue;
                }

                var p = TextBox.GetPositionFromCharIndex(w.Start);
                p.Y += 18;

                _toolTip.Show(w.ToolTip, TextBox, p);
                found = true;
            }

            if (!found)
            {
                _toolTip.Hide(TextBox);
            }
        }

        protected override void WndProc(ref Message m)
        {
            // pre-process the text control's messages
            switch (m.Msg)
            {
                case 0x14: // WM_ERASEBKGND
                    base.WndProc(ref m);
                    MarkWords();
                    break;
                case 0x114: // WM_HSCROLL
                    base.WndProc(ref m);
                    MarkWords();
                    break;
                case 0x115: // WM_VSCROLL
                    base.WndProc(ref m);
                    MarkWords();
                    break;
                case 0x0101: // WM_KEYUP
                    base.WndProc(ref m);
                    MarkWords();
                    break;
                case 0x113: // WM_TIMER
                    base.WndProc(ref m);
                    MarkWords();
                    break;
                default:
                    //Console.WriteLine(m.Msg);
                    base.WndProc(ref m);
                    break;
            }
        }

        public void AddWord(int wordStart, int wordLen, Color color)
        {
            AddWord(wordStart, wordLen, color, "");
        }

        public void AddWord(int wordStart, int wordLen, Color color, string toolTip)
        {
            var word = new Word
            {
                Start = wordStart,
                Length = wordLen,
                Color = color,
                ToolTip = toolTip,
            };
            _markedWords.Add(word);
        }

        public void Clear()
        {
            _markedWords = new List<Word>();
        }

        public void MarkWords()
        {
            if (TextBox.IsDisposed || !TextBox.Enabled || !TextBox.Visible) return;

            var graphics = TextBox.CreateGraphics();

            var minPos = TextBox.GetCharIndexFromPosition(new Point(0, 0));
            var maxPos = TextBox.GetCharIndexFromPosition(new Point(TextBox.Width, TextBox.Height));
            foreach (var w in _markedWords.Where(w => w.Start + w.Length >= minPos && w.Start <= maxPos))
            {
                MarkWord(w, graphics);
            }
            graphics.Dispose();
        }

        private void MarkWord(Word word, Graphics graphics)
        {
            var path = new GraphicsPath();

            var points = new List<Point>();
            var p1 = TextBox.GetPositionFromCharIndex(word.Start);
            var p2 = TextBox.GetPositionFromCharIndex(word.Start + word.Length);

            if (word.Length == 0)
            {
                p1.X -= 5;
                p2.X += 5;
            }

            p1.Y += TextBox.Font.Height - 2;
            points.Add(p1);
            var up = true;
            for (var x = p1.X + 2; x < p2.X + 2; x += 2)
            {
                var p = up ? new Point(x, p1.Y + 2) : new Point(x, p1.Y);
                points.Add(p);
                up = !up;
            }
            if (points.Count > 1)
            {
                path.StartFigure();
                path.AddLines(points.ToArray());
            }

            var pen = new Pen(word.Color);
            graphics.DrawPath(pen, path);
            pen.Dispose();
            path.Dispose();
        }

        #region IDisposable Members

        public void Dispose()
        {
            ReleaseHandle();
        }

        #endregion
    }
}
