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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TinyPG.Controls
{
    public class AutoComplete : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, char wParam, IntPtr lParam);

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private readonly IContainer components = null;


        private const int WM_KEYDOWN = 0x100;
        private readonly RichTextBox _textEditor;

        // suppresses displaying the auto-completion screen while value > 0
        private int _suppress;
        private int _autocompleteStart;

        // word list to show in the auto-completion list
        public ListBox WordList;

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled)
            {
                Visible = false;
            }
        }

        public AutoComplete(NumberedTextBox editor)
        {
            _textEditor = editor.RichTextBox;
            _textEditor.KeyDown += editor_KeyDown;
            _textEditor.KeyUp += textEditor_KeyUp;
            _textEditor.LostFocus += textEditor_LostFocus;

            InitializeComponent();
        }

        private void textEditor_LostFocus(object sender, EventArgs e)
        {
            if (_textEditor.Focused || Focused || WordList.Focused)
            {
                return;
            }

            Visible = false;
        }

        private void editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Enabled)
            {
                return;
            }

            if (e.KeyValue == 32)
            {
                if (e.Control)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }

                if (_suppress > 0)
                {
                    _suppress--;
                }
            }

            if (e.Control && e.KeyValue != 32)
            {
                _suppress = 2;
            }

            if (e.KeyValue == 27 && Visible)
            {
                _suppress = 2;
            }


            if (Visible)
            {
                // PgUp, PgDn, Up, Down
                if (e.KeyValue == 33 || e.KeyValue == 34 || e.KeyValue == 38 || e.KeyValue == 40)
                {
                    SendKey((char)e.KeyValue);
                    e.Handled = true;
                }
            }
        }

        private void textEditor_KeyUp(object sender, KeyEventArgs e)
        {
            if (!Enabled) return;

            try
            {
                if ((e.KeyValue == 32 && !e.Control) || e.KeyValue == 13 || e.KeyValue == 27)
                {
                    Visible = false;
                }
                else if ((e.KeyValue > 64 && e.KeyValue < 91 && !e.Control) || (e.KeyValue == 32 && e.Control))
                {
                    if (!Visible)
                    {
                        var line = _textEditor.GetFirstCharIndexOfCurrentLine();
                        var t = Helper.Reverse(_textEditor.Text.Substring(line, _textEditor.SelectionStart - line));

                        // scan the line of text for any of these characters. these mark the beginning of the word
                        var i = t.IndexOfAny(" \r\n\t.;:\\/?><-=~`[]{}+!#$%^&*()".ToCharArray());
                        if (i < 0)
                        {
                            i = t.Length;
                        }

                        _autocompleteStart = _textEditor.SelectionStart - i;
                        _textEditor.Text.IndexOfAny(" \t\r\n".ToCharArray());
                        var p = _textEditor.GetPositionFromCharIndex(_autocompleteStart);
                        p = _textEditor.PointToScreen(p);
                        p.X -= 8;
                        p.Y += 22;

                        // only show auto-completion dialog if user has typed in the first characters, or if
                        // the user pressed CTRL-Space explicitly
                        if ((_textEditor.SelectionStart - _autocompleteStart > 0 && _suppress <= 0) || (e.KeyValue == 32 && e.Control))
                        {
                            Location = p;
                            Visible = Enabled; // only display if enabled
                            _textEditor.Focus();
                        }
                    }

                    //pre-select a word from the list that begins with the typed characters
                    WordList.SelectedIndex = WordList.FindString(_textEditor.Text.Substring(_autocompleteStart, _textEditor.SelectionStart - _autocompleteStart));

                }
                else if (Visible)
                {
                    if (e.KeyValue == 9 && !e.Alt && !e.Control && !e.Shift) // tab key
                    {
                        SelectCurrentWord();
                        e.Handled = true;
                    }

                    if (_textEditor.SelectionStart < _autocompleteStart)
                    {
                        Visible = false;
                    }
                    if (e.KeyValue == 33 || e.KeyValue == 34 || e.KeyValue == 38 || e.KeyValue == 40)
                    {
                        return;
                    }

                    if (Visible)
                    {
                        WordList.SelectedIndex = WordList.FindString(_textEditor.Text.Substring(_autocompleteStart, _textEditor.SelectionStart - _autocompleteStart));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void SelectCurrentWord()
        {
            Visible = false;
            if (WordList.SelectedItem == null)
            {
                return;
            }

            var temp = _textEditor.SelectionStart;
            _textEditor.Select(_autocompleteStart, temp-_autocompleteStart) ;
            _textEditor.SelectedText = WordList.SelectedItem.ToString();
        }

        private void SendKey(char key)
        {
            SendMessage(WordList.Handle, WM_KEYDOWN, key, IntPtr.Zero);
        }

        private void AutoComplete_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 32 || e.KeyValue == 27 || e.KeyValue == 13 || e.KeyValue == 9)
            {
                Visible = false;
            }

            if (e.KeyValue == 9 || e.KeyValue == 13)
            {
                SelectCurrentWord();
            }
        }

        // user selects a word using double click
        private void WordList_DoubleClick(object sender, EventArgs e)
        {
            SelectCurrentWord();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            WordList = new ListBox();
            SuspendLayout();
            //
            // WordList
            //
            WordList.BorderStyle = BorderStyle.FixedSingle;
            WordList.Dock = DockStyle.Fill;
            WordList.Font = new Font("Segoe UI", 9F);
            WordList.FormattingEnabled = true;
            WordList.ItemHeight = 15;
            WordList.Location = new Point(0, 0);
            WordList.Name = "WordList";
            WordList.Size = new Size(303, 137);
            WordList.Sorted = true;
            WordList.TabIndex = 0;
            WordList.UseTabStops = false;
            WordList.DoubleClick += WordList_DoubleClick;
            //
            // AutoComplete
            //
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(303, 141);
            ControlBox = false;
            Controls.Add(WordList);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            KeyPreview = true;
            Name = "AutoComplete";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            KeyUp += AutoComplete_KeyUp;
            ResumeLayout(false);
        }
    }
}
