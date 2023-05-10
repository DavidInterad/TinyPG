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
using System.Linq;
using System.Windows.Forms;

namespace TinyPG.Controls
{
    [ProvideProperty("Dockable", typeof(Panel))]

    public sealed class DockExtender : Component, IExtenderProvider, ISupportInitialize
    {
        private readonly Control _dockHost;


        // this is the blue overlay that presents a preview how the control will be docked
        internal Overlay Overlay = new Overlay();


        public bool Dockable { get; set; }

        public Floaties Floaties { get; }

        public DockExtender()
        {
            _dockHost = null;
            Floaties = new Floaties();
        }

        public DockExtender(Control dockHost)
        {
            _dockHost = dockHost;
            Floaties = new Floaties();
        }

        /// <summary>
        /// display the container control that is either floating or docked
        /// </summary>
        /// <param name="container"></param>
        public void Show(Control container)
        {
            var f = Floaties.Find(container);
            f?.Show();
        }

        /// <summary>
        /// this will gracefully hide the container control
        /// making sure that the floating window is also closed
        /// </summary>
        /// <param name="container"></param>
        public void Hide(Control container)
        {
            var f = Floaties.Find(container);
            f?.Hide();
        }

        /// <summary>
        /// Attach a container control and use it as a grip handle. The container must support mouse move events.
        /// </summary>
        /// <param name="container">container to make dockable/floatable</param>
        /// <returns>the floaty that manages the container's behaviour</returns>
        public IFloaty Attach(Control container)
        {
            return Attach(container, container, null);
        }

        /// <summary>
        /// Attach a container and a grip handle. The handle must support mouse move events.
        /// </summary>
        /// <param name="container">container to make dockable/floatable</param>
        /// <param name="handle">grip handle used to drag the container</param>
        /// <returns>the floaty that manages the container's behaviour</returns>
        public IFloaty Attach(Control container, Control handle)
        {
            return Attach(container, handle, null);
        }

        /// <summary>
        /// attach this class to any dockable type of container control
        /// to make it dockable.
        /// Attach a container control and use it as a grip handle. The handle must support mouse move events.
        /// Supply a splitter control to allow resizing of the docked container
        /// </summary>
        /// <param name="container">control to be dockable</param>
        /// <param name="handle">handle to be used to track the mouse movement (e.g. caption of the container)</param>
        /// <param name="splitter">splitter to resize the docked container (optional)</param>
        public IFloaty Attach(Control container, Control handle, Splitter splitter)
        {
            var dockState = new DockState
            {
                Container = container ?? throw new ArgumentException("container cannot be null"),
                Handle = handle ?? throw new ArgumentException("handle cannot be null"),
                OrgDockHost = _dockHost,
                Splitter = splitter,
            };

            var floaty = new Floaty(this);
            floaty.Attach(dockState);
            Floaties.Add(floaty);
            return floaty;
        }

        // finds the potential dockhost control at the specified location
        internal Control FindDockHost(Floaty floaty , Point pt)
        {
            Control c = null;
            if (FormIsHit(floaty.DockState.OrgDockHost, pt))
            {
                c = floaty.DockState.OrgDockHost; //assume top level control
            }

            if (floaty.DockOnHostOnly)
            {
                return c;
            }

            foreach (var f in Floaties.Cast<Floaty>().Where(f => f.DockState.Container.Visible && FormIsHit(f.DockState.Container, pt)))
            {
                // add this line to disallow docking inside floaties
                //if (f.Visible) continue;

                c = f.DockState.Container; // found suitable floating form
                break;
            }

            return c;
        }

        // finds the potential dockhost control at the specified location
        internal bool FormIsHit(Control c, Point pt)
        {
            if (c == null)
            {
                return false;
            }

            var pc = c.PointToClient(pt);
            var hit = c.ClientRectangle.IntersectsWith(new Rectangle(pc, new Size(1, 1))); //.TopLevelControl; // this is tricky
            return hit;
        }

        #region IExtenderProvider Members

        public bool CanExtend(object extendee)
        {
            return (extendee is Control);
        }

        #endregion

        #region ISupportInitialize Members

        public void BeginInit()
        {
            Console.WriteLine("DockExtender_BeginInit");
        }

        public void EndInit()
        {
            Console.WriteLine("DockExtender_EndInit");
        }

        #endregion
    }

    internal struct DockState
    {
        /// <summary>
        /// the docking control (usually a container class, e.g Panel)
        /// </summary>
        public Control Container;
        /// <summary>
        /// handle of the container that the user can use to select and move the container
        /// </summary>
        public Control Handle;

        /// <summary>
        /// splitter that is attached to this panel for resizing.
        /// this is optional
        /// </summary>
        public Splitter Splitter;

        /// <summary>
        /// the parent of the container
        /// </summary>
        public Control OrgDockingParent;

        /// <summary>
        /// the base docking host that contains all docking panels
        /// </summary>
        public Control OrgDockHost;

        /// <summary>
        /// the original docking style, stored in order to reset the state
        /// </summary>
        public DockStyle OrgDockStyle;

        /// <summary>
        /// the original bounds of the container
        /// </summary>
        public Rectangle OrgBounds;
    }
}
