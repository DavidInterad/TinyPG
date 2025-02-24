﻿// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
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
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TinyPG.Controls
{
	internal class TabControlEx : TabControl
	{
		protected override void OnCreateControl()
		{
			SetStyle(ControlStyles.DoubleBuffer |
			   ControlStyles.UserPaint |
			   ControlStyles.AllPaintingInWmPaint |
			   ControlStyles.SupportsTransparentBackColor, true);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			Invalidate();
			base.OnMouseEnter(e);

		}

		protected override void OnMouseLeave(EventArgs e)
		{
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnSystemColorsChanged(EventArgs e)
		{
			Invalidate();
			base.OnSystemColorsChanged(e);
		}
		protected virtual void DrawTabPage(Graphics graphics, int index, TabPage page)
		{
			Brush brush;
			var textWidth = (int)graphics.MeasureString(page.Text, Font).Width;

			var r = GetTabRect(index);
			var p = PointToClient(Cursor.Position);

			var highlight = new Rectangle(p.X, p.Y, 1, 1).IntersectsWith(r);

			if (index == SelectedIndex)
			{
				brush = SystemBrushes.ControlLightLight;
				if (Alignment == TabAlignment.Top)
				{
					r.X -= 2;
					r.Y -= 2;
					r.Width += 2;
					r.Height += 5;
				}
				else
				{
					r.X -= 2;
					r.Y -= 2;
					r.Width += 2;
					r.Height += 4;
				}
			}
			else
			{
				if (Alignment == TabAlignment.Top)
				{
					r.Y += 0;
					r.Height += 1;
				}
				else
				{
					r.Y -= 2;
					r.Height += 2;
				}

				brush = highlight
					? new LinearGradientBrush(r, ControlPaint.LightLight(SystemColors.Highlight),
						SystemColors.ButtonHighlight, LinearGradientMode.Vertical)
					: new LinearGradientBrush(r, SystemColors.ControlLight, SystemColors.ButtonHighlight,
						LinearGradientMode.Vertical);
			}

			switch (Alignment)
			{
				case TabAlignment.Top:
					{
						graphics.FillRectangle(brush, r);
						var points = new Point[4];
						points[0] = new Point(r.Left, r.Top + r.Height - 1);
						points[1] = new Point(r.Left, r.Top);
						points[2] = new Point(r.Left + r.Width, r.Top);
						points[3] = new Point(r.Left + r.Width, r.Top + r.Height - 1);

						graphics.DrawLines(Pens.Gray, points);
						graphics.DrawString(page.Text, Font, Brushes.Black, r.Left + (r.Width - textWidth) / 2, r.Top + 2);
						break;
					}
				case TabAlignment.Bottom:
					{
						graphics.FillRectangle(brush, r.Left + 1, r.Top + 1, r.Width - 1, r.Height - 1);
						var points = new Point[4];
						points[0] = new Point(r.Left, r.Top);
						points[1] = new Point(r.Left, r.Top + r.Height - 1);
						points[2] = new Point(r.Left + r.Width, r.Top + r.Height - 1);
						points[3] = new Point(r.Left + r.Width, r.Top);
						graphics.DrawLines(Pens.Gray, points);
						graphics.DrawString(page.Text, Font, Brushes.Black, r.Left + (r.Width - textWidth) / 2, r.Top + 2);
						if (index == SelectedIndex)
						{
							graphics.DrawLine(Pens.White, r.Left + 1, r.Top, r.Left + r.Width - 1, r.Top);
						}

						break;
					}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			switch (Alignment)
			{
				case TabAlignment.Top:
					e.Graphics.FillRectangle(SystemBrushes.ControlLightLight, new Rectangle(0, 23, Width - 2, Height - 2));
					e.Graphics.DrawRectangle(SystemPens.ControlDarkDark, new Rectangle(0, 21, Width - 2, Height - 23));
					break;
				case TabAlignment.Bottom:
					e.Graphics.FillRectangle(SystemBrushes.ControlLightLight, new Rectangle(0, 0, Width, Height - 20));
					e.Graphics.DrawRectangle(SystemPens.ControlDarkDark, new Rectangle(0, 0, Width - 2, Height - 22));
					break;
			}

			for (var i = 0; i < TabPages.Count; i++)
			{
				if (i == SelectedIndex)
				{
					continue;
				}

				var page = TabPages[i];
				DrawTabPage(e.Graphics, i, page);
			}

			if (SelectedIndex >= 0)
			{
				DrawTabPage(e.Graphics, SelectedIndex, TabPages[SelectedIndex]);
			}
		}
	}
}
