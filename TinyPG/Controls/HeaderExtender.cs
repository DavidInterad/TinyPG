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
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TinyPG.Controls
{
    internal class HeaderLabel : Label
    {
        private bool _hasFocus;
        private Control _focusControl;
        private bool _closeButtonPressed;

        public event EventHandler CloseClick;

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            RefreshHeader();
            CloseClick = null;
        }

        private void RefreshHeader()
        {
            ForeColor = _hasFocus ? SystemColors.ControlText : SystemColors.GrayText;
            Invalidate();
        }

        private bool IsHighlighted()
        {
            var box = new Rectangle(Width - 20, (Height - 15) / 2, 16, 14);
            var p = PointToClient(Cursor.Position);
            return new Rectangle(p.X, p.Y, 1, 1).IntersectsWith(box);
        }

        protected void PaintCloseButton(Graphics graphics)
        {
            // paintclosebutton
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.Bicubic;

            var box = new Rectangle(Width - 20, (Height - 15) / 2, 16, 14);
            var pen = new Pen(_hasFocus ? SystemColors.WindowText : SystemColors.GrayText, 2f);
            var p1 = new Point(Width - 16, (Height-8)/2);
            var p2 = new Point(p1.X + 7, p1.Y+7);
            var p3 = new Point(p1.X + 7, p1.Y);
            var p4 = new Point(p1.X, p1.Y + 7);

            if (IsHighlighted())
            {
                if (_closeButtonPressed)
                {
                    graphics.FillRectangle(SystemBrushes.GradientInactiveCaption, box);
                    graphics.DrawRectangle(SystemPens.ActiveCaption, box);
                }
                else
                {
                    graphics.FillRectangle(SystemBrushes.GradientActiveCaption, box);
                    graphics.DrawRectangle(SystemPens.Highlight, box);
                }
            }

            graphics.DrawLine(pen, p1, p2);
            graphics.DrawLine(pen, p3, p4);
            pen.Dispose();
        }

        /// <summary>
        /// will register the parent and child controls and activate the provided control
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public void Activate(Control control)
        {
            _focusControl = control;
            ActivateRecursive(Parent);
        }

        private void ActivateRecursive(Control control)
        {
            ActivatedBy(control);
            foreach (Control c in control.Controls)
            {
                ActivateRecursive(c);
            }
        }

        /// <summary>
        /// register controls that will activate this header. usually these are child or sibling controls as part of the container control
        /// however also the container control will activate the header and should be registered also as ActivatedBy
        /// </summary>
        /// <param name="control"></param>
        public void ActivatedBy(Control control)
        {
            control.GotFocus += control_GotFocus;
            control.MouseDown += control_MouseDown;
            control.LostFocus += control_LostFocus;
        }

        /// <summary>
        /// optional method. If a control gets focus, it will deactivate this caption
        /// </summary>
        /// <param name="control"></param>
        public void DeactivatedBy(Control control)
        {
            control.GotFocus += control_LostFocus;
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            _closeButtonPressed = e.Button == MouseButtons.Left && IsHighlighted();

            Focus();

            if (!IsHighlighted())
            {
                base.OnMouseDown(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_closeButtonPressed && IsHighlighted() && e.Button == MouseButtons.Left)
            {
                // raise close event.
                CloseClick?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                base.OnMouseUp(e);
            }

            _closeButtonPressed = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!IsHighlighted())
            {
                base.OnMouseMove(e);
            }

            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _closeButtonPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (_focusControl != null)
            {
                _focusControl.Focus();
            }
            else
            {
                Parent.Focus();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            var r = new Rectangle(0, 0, Width, Height);

            Brush brush = _hasFocus
                ? new LinearGradientBrush(r, SystemColors.ActiveCaption, SystemColors.GradientActiveCaption,
                    LinearGradientMode.Vertical)
                : new LinearGradientBrush(r, SystemColors.InactiveCaption, SystemColors.GradientInactiveCaption,
                    LinearGradientMode.Vertical);
            pevent.Graphics.FillRectangle(brush, r);
            brush.Dispose();
            r.Height--;
            r.Width--;

            pevent.Graphics.DrawRectangle(SystemPens.ControlDark, r);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // paintclosebutton
            PaintCloseButton(e.Graphics);
        }

        void control_MouseDown(object sender, MouseEventArgs e)
        {
            _hasFocus = true;
            RefreshHeader();
        }

        void control_GotFocus(object sender, EventArgs e)
        {
            _hasFocus = true;
            RefreshHeader();
        }

        void control_LostFocus(object sender, EventArgs e)
        {
            _hasFocus = false;
            RefreshHeader();
        }
    }
}
