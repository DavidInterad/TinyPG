using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Source: https://stackoverflow.com/a/17973520/1634286

namespace TinyPG.Controls
{
	public partial class NumberedTextBox : UserControl
	{
		private int _lines = 0;

		public RichTextBox RichTextBox => editBox;

		[Browsable(true),
		 Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", "System.Drawing.Design.UITypeEditor")]
		public new string Text
		{
			get => editBox.Text;
			set
			{
				editBox.Text = value;
				Invalidate();
			}
		}

		private Color _lineNumberColor = Color.LightSeaGreen;

		[Browsable(true), DefaultValue(typeof(Color), "LightSeaGreen")]
		public Color LineNumberColor
		{
			get => _lineNumberColor;
			set
			{
				_lineNumberColor = value;
				Invalidate();
			}
		}

		public NumberedTextBox()
		{
			InitializeComponent();

			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			editBox.SelectionChanged += selectionChanged;
			editBox.VScroll += OnVScroll;
		}

		private void selectionChanged(object sender, EventArgs args)
		{
			Invalidate();
		}

		private void DrawLines(Graphics g)
		{
			g.Clear(BackColor);
			var max = (int)g.MeasureString((_lines + 1).ToString(), editBox.Font).Width + 6;
			for (var i = 1; i < _lines + 1; i++)
			{
				var y = editBox.GetPositionFromCharIndex(editBox.GetFirstCharIndexFromLine(i - 1)).Y;
				var size = g.MeasureString(i.ToString(), editBox.Font);
				g.DrawString(i.ToString(), editBox.Font, new SolidBrush(LineNumberColor), new Point(max - 3 - (int)size.Width, y));
			}
			editBox.Location = new Point(max, 0);
			editBox.Size = new Size(ClientRectangle.Width - max, ClientRectangle.Height);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			_lines = editBox.Lines.Length;
			DrawLines(e.Graphics);
			e.Graphics.TranslateTransform(50, 0);
			editBox.Invalidate();
			base.OnPaint(e);
		}

		private void OnVScroll(object sender, EventArgs e)
		{
			Invalidate();
		}

		public void Select(int start, int length)
		{
			editBox.Select(start, length);
		}

		public void ScrollToCaret()
		{
			editBox.ScrollToCaret();
		}

		private void editBox_TextChanged(object sender, EventArgs e)
		{
			Invalidate();
		}
	}

	public class RichTextBoxEx : RichTextBox
	{
		private double _Yfactor = 1.0d;

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, ref Point lParam);

		private enum WindowsMessages
		{
			WM_USER = 0x400,
			EM_GETSCROLLPOS = WM_USER + 221,
			EM_SETSCROLLPOS = WM_USER + 222
		}

		public Point ScrollPos
		{
			get
			{
				var scrollPoint = new Point();
				SendMessage(Handle, (int)WindowsMessages.EM_GETSCROLLPOS, 0, ref scrollPoint);
				return scrollPoint;
			}
			set
			{
				var original = value;
				if (original.Y < 0)
					original.Y = 0;
				if (original.X < 0)
					original.X = 0;

				var factored = value;
				factored.Y = (int)((double)original.Y * _Yfactor);

				var result = value;

				SendMessage(Handle, (int)WindowsMessages.EM_SETSCROLLPOS, 0, ref factored);
				SendMessage(Handle, (int)WindowsMessages.EM_GETSCROLLPOS, 0, ref result);

				var loopcount = 0;
				var maxloop = 100;
				while (result.Y != original.Y)
				{
					// Adjust the input.
					if (result.Y > original.Y)
						factored.Y -= (result.Y - original.Y) / 2 - 1;
					else if (result.Y < original.Y)
						factored.Y += (original.Y - result.Y) / 2 + 1;

					// test the new input.
					SendMessage(Handle, (int)WindowsMessages.EM_SETSCROLLPOS, 0, ref factored);
					SendMessage(Handle, (int)WindowsMessages.EM_GETSCROLLPOS, 0, ref result);

					// save new factor, test for exit.
					loopcount++;
					if (loopcount >= maxloop || result.Y == original.Y)
					{
						_Yfactor = (double)factored.Y / (double)original.Y;
						break;
					}
				}
			}
		}
	}
}
