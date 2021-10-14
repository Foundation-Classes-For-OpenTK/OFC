﻿/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    public class GLMenuStrip : GLFlowLayoutPanel
    {
        #region Init

        // all Menu Items

        public Action<GLMenuStrip> Opening = null;         // Menu opening

        // Called on top level menu item only.
        public Func<GLMenuStrip, bool> Closing = null;      // All Submenus closing (and if context menu, the top level is detaching), return true to allow the close to happen
        public Action<GLMenuItem, GLMenuStrip> SubmenuOpened = null;        // from GLMenuItem the GLMenuStrip submenu is opening
        public Action<GLMenuItem, GLMenuStrip> SubmenuClosing = null;       // from GLMenuItem the GLMenuStrip submenu is closing

        // Inherited FlowInZOrder, FlowDirection, FlowPadding, BackColor

        public Color ForeColor { get { return foreColor; } set { foreColor = value; Invalidate(); } }       // of text.  Set to Color.Empty for no override
        public Color MouseOverBackColor { get { return mouseOverBackColor; } set { mouseOverBackColor = value; Invalidate(); } }    // Set Color.Empty for no override
        public Color IconStripBackColor { get { return iconStripBackColor; } set { iconStripBackColor = value; Invalidate(); } }

        public int AutoOpenDelay { get; set; } = 250;     // open after a delay, 0 for off

        // if you specify items, make sure the direction is right also.

        public GLMenuStrip(string name, Rectangle location, GLFlowLayoutPanel.ControlFlowDirection direction = ControlFlowDirection.Right, params GLMenuItem[] items) : base(name, location)
        {
            FlowDirection = direction;
            FlowInZOrder = false;
            Focusable = true;       // allow focus to go to us, so we don't lost focus=null for the gfocus check
            timer.Tick += Timeout;
            foreach (var e in items)
                Add(e);
        }

        public GLMenuStrip(string name = "Menu?") : this(name, DefaultWindowRectangle)
        {
        }

        public GLMenuStrip(string name, DockingType type, float dockpercent, GLFlowLayoutPanel.ControlFlowDirection direction = ControlFlowDirection.Right, params GLMenuItem[] items) : 
                            this(name, DefaultWindowRectangle, direction, items)
        {
            Dock = type;
            DockPercent = dockpercent;
        }

        public GLMenuStrip(string name, Size sizep, DockingType type, float dockpercentage, GLFlowLayoutPanel.ControlFlowDirection direction = ControlFlowDirection.Right, params GLMenuItem[] items) 
                        : this(name, DefaultWindowRectangle, direction, items)
        {
            Dock = type;
            DockPercent = dockpercentage;
            SetNI(size: sizep);
        }

        // call to pop up the context menu, parent is normally displaycontrol
        // you can double call and the previous one is closed
        public void Show(GLBaseControl parent, Point coord)        
        {
            //System.Diagnostics.Debug.WriteLine("Open as context menu " + Name);
            Detach(this);
            openedascontextmenu = true;
            Location = coord;
            AutoSize = true;
            TopMost = true;
            parent.Add(this);
            SetFocus();
            Opening?.Invoke(this);
        }

        #endregion  

        #region Menu Openers

        private bool Select(int index, bool focusto)     // select this item, optionally transfer focus to it
        {
            if ( submenu != null )      // if submenu is activated..
            {
                if (index == selected)  // if already selected, its open
                    return true;

                CloseSubMenus();        // close any current ones
            }

            if (index >= 0 && index < ControlsIZ.Count )
            {
                var mi = ControlsIZ[index] as GLMenuItem;

                //System.Diagnostics.Debug.WriteLine("Selected " + index + " in " + Name);

                if (mi != null && mi.SubMenuItems != null)      // actually a submenu..
                {
                    Point offset;
                    if (FlowDirection == ControlFlowDirection.Right)       // pick co-ords based on flow
                        offset = new Point(mi.Left - this.ClientLeftMargin, this.Height);
                    else
                        offset = new Point(Width, mi.Top);

                    Point p = FindScreenCoords(offset);     // find this point in bounds on screen

                    submenu = new GLMenuStrip(Name + "." + mi.Name, new Rectangle(p.X, p.Y, 200, 200));        // size is immaterial , autosize both
                    submenu.ScaleWindow = FindScaler();
                    submenu.SuspendLayout();

                    System.Diagnostics.Debug.WriteLine("Open menu " + submenu.Name + " " + submenu.Bounds);

                    submenu.Font = Font;
                    submenu.ForeColor = this.ForeColor;
                    submenu.BackColor = this.BackColor;
                    submenu.MouseOverBackColor = this.MouseOverBackColor;
                    submenu.FlowDirection = ControlFlowDirection.Down;
                    submenu.AutoSize = true;
                    submenu.AutoOpenDelay = AutoOpenDelay;
                    submenu.parentmenu = this;
                    submenu.TopMost = true;

                    submenu.AddItems(mi.SubMenuItems);
                    submenu.SetSelected(-1);                                    // release all items for hover highlighting

                    submenu.ResumeLayout();

                    AddToDesktop(submenu);

                    SetSelected(index);                                         // set selected thus fixing highlight on this one

                    GetTopLevelMenu().SubmenuOpened?.Invoke(mi,submenu);                // call, allowing configuration of the submenu

                    if (focusto)                                                // used to transfer focus to submenu.  Note submenu with focus has up/down keys
                        submenu.SetFocus();
                    else
                        SetFocus();                                             // else ensure we have the focus, in case due to keyboard hit

                    return true;
                }
                else 
                {
                    SetSelected(index);                     // not a menu or other, select it
                    if (mi == null)
                    {
                        ControlsIZ[index].SetFocus();       // not a menu item, it gets focus.
                    }
                    return true;
                }
            }
            else
                return false;
        }

        public bool Move(int dir)                           // move highlight left/up or right/down
        {
            int pos = selected;
            while (true)
            {
                pos += dir;
                if (pos < 0 || pos >= ControlsIZ.Count)     // out of range, can't move
                    return false;

                if (ControlsIZ[pos].Enabled && ControlsIZ[pos].Visible)     // if on
                {
                    Select(pos, FlowDirection == ControlFlowDirection.Right);   // set focus on if left-right menu
                    return true;
                }
                else if (dir == 0)
                    return false;
            }
        }

        public void ActivateSelected()                  // activate, either selected or hoverover item
        {
            if (submenu != null)                        // if a submenu is up, activate always transfers to it
            {
                submenu.SetFocus();                     // it becomes the focus
                submenu.Move(1);                        // autoselect first item
            }
            else
            {
                int clickon = selected >= 0 ? selected : mousehovered;      // prefer selected..

                if (clickon >= 0)   // if valid, click it
                {
                    GLMenuItem mi = ControlsIZ[clickon] as GLMenuItem;
                    if (mi != null)
                    {
                        if (mi.SubMenuItems != null)        // it may be closed up, due to going down, then backspacing, so click on it, transfer focus
                        {
                            Select(clickon, true);
                        }
                        else
                        {
                            //System.Diagnostics.Debug.WriteLine("Activate {0} {1}", mi.Name, clickon);
                            GetTopLevelMenu().CloseMenus();
                            mi.OnClick();
                        }
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("Not MI");
                    }
                }
            }
        }

        public GLMenuStrip GetTopLevelMenu()
        {
            GLMenuStrip m = this;
            while (m.parentmenu != null)
                m = m.parentmenu;
            return m;
        }

        public void CloseMenus()        // all menus close down
        {
            System.Diagnostics.Debug.WriteLine($"{Name} Close menus");

            var allow = Closing?.Invoke(this) ?? true;
            if (allow)
            {
                CloseSubMenus();
                SetSelected(-1);
                if (openedascontextmenu)
                {
                    System.Diagnostics.Debug.WriteLine($"{Name} Detach");
                    Detach(this);
                }
            }
        }

        public void CloseSubMenus()     // all submenus shut down
        {
            if (submenu != null)
            {
                GetTopLevelMenu().SubmenuClosing?.Invoke(ControlsIZ[selected] as GLMenuItem, submenu);                 // call before close
                submenu.CloseSubMenus();    // close child submenus first
                System.Diagnostics.Debug.WriteLine($"{Name} Close down");
                submenu.Close();
                submenu = null;
                SetFocus();
            }
        }

        #endregion

        #region Implementation

        protected override void OnControlAdd(GLBaseControl parent, GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("On control add {0}:{1} {2}:{3}", parent.GetType().Name, parent.Name, child.GetType().Name, child.Name);

            if (parent is GLMenuStrip)      // note we get called when the GLMenuStrip is added to the display, we don't want that call
            {
                child.BackColor = BackColor;
                child.SuspendLayout();

                int iconareawidth = Font.ScalePixels(24);

                var mi = child as GLMenuItem;
                if (mi != null)                     // MenuItems get coloured and hooked
                {
                    if (ForeColor != Color.Empty)
                        mi.ForeColor = ForeColor;

                    mi.ButtonBackColor = BackColor;

                    if (MouseOverBackColor != Color.Empty)
                    {
                        mi.MouseOverBackColor = MouseOverBackColor;
                        mi.BackColorScaling = 1;
                    }

                    mi.Click += MenuItemClicked;
                    mi.MouseEnter += MenuItemEnter;
                    mi.MouseLeave += MenuItemLeave;

                    if (FlowDirection == ControlFlowDirection.Down)
                    {
                        mi.IconTickAreaWidth = iconareawidth;
                    }
                }
                else
                {
                    if (FlowDirection == ControlFlowDirection.Down)
                    {
                        child.FlowOffsetPosition = new Point(iconareawidth, 0);
                    }

                    child.KeyDown += NonMIKeyDown;
                }

                child.ResumeLayout();
            }

            base.OnControlAdd(parent, child);
        }

        protected override void OnControlRemove(GLBaseControl parent, GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("On control remove {0}:{1} {2}:{3}", parent.GetType().Name, parent.Name, child.GetType().Name, child.Name);

            var mi = child as GLMenuItem;
            if (mi != null)                     // make sure we unhook!  this caught me out.
            {
                mi.Click -= MenuItemClicked;
                mi.MouseEnter -= MenuItemEnter;
                mi.MouseLeave -= MenuItemLeave;
            }
            else
            {
                child.KeyDown -= NonMIKeyDown;
            }

            base.OnControlRemove(parent, child);
        }

        // intercept mouse clicks, and if not clicked on us, close us

        protected override void OnGlobalMouseDown(GLBaseControl ctrl, GLMouseEventArgs e)
        {
            base.OnGlobalMouseDown(ctrl, e);

            if ( parentmenu == null )       // if top level menu.. top of heirarchy
            {
                bool isthisorchild = ctrl != null && IsThisOrChildOf(ctrl);
                System.Diagnostics.Debug.WriteLine($"{Name} Top level, click on child {isthisorchild}");

                if (!isthisorchild)
                {
                    GetTopLevelMenu().CloseMenus();
                }
            }
        }

        public override bool IsThisOrChildOf(GLBaseControl ctrl)        // submenus are us, so its a child
        {
            if (base.IsThisOrChildOf(ctrl))
                return true;
            else if (submenu != null && submenu.IsThisOrChildOf(ctrl))
                return true;
            else
                return false;
        }

        protected override void DrawBack(Rectangle area, Graphics gr, Color bc, Color bcgradientalt, int bcgradient)
        {
            //base.DrawBack(area, gr, Focused ? Color.Green : Color.White , bcgradientalt, bcgradient); // for debugging
            base.DrawBack(area, gr, bc , bcgradientalt, bcgradient);

            if (FlowDirection == ControlFlowDirection.Down)
            {
                using (Brush br = new SolidBrush(IconStripBackColor))
                {
                    int iconareawidth = Font.ScalePixels(24);
                    gr.FillRectangle(br, new Rectangle(area.Left, area.Top, iconareawidth, area.Height));
                }
            }
        }

        private void SetSelected(int sel)       // select item, or -1 select none
        {
            selected = sel;

            for (int i = 0; i < ControlsIZ.Count; i++)
            {
                var c = ControlsIZ[i] as GLMenuItem;
                if (c != null)
                {
                    c.Highlighted = selected == i;
                    c.DisableHoverHighlight = selected != -1;
                    //System.Diagnostics.Debug.WriteLine("  Item {0} {1} {2}", c.Name, c.Highlighted, c.DisableHoverHighlight);
                }
            }
        }

        private void Close()
        {
            //System.Diagnostics.Debug.WriteLine("Close menu " + Name);
            SuspendLayout();
            List<GLBaseControl> todelete = new List<GLBaseControl>(ControlsZ);
            foreach (var c in todelete)
            {
                Detach(c);        // detach but do not dispose..
            }

            ResumeLayout();
            Remove(this);
        }

        #endregion

        #region UI

        private void MenuItemClicked(GLBaseControl c)
        {
            System.Diagnostics.Debug.Assert(Parent != null);        // double check we are not on a zombie control, due to the add/remove stuff

            var mi = c as GLMenuItem;
            if (mi != null)                             // menu items are handled by us
            {
                if (mi.SubMenuItems != null)
                {
                    Select(ControlsIZ.IndexOf(mi), FlowDirection == ControlFlowDirection.Right);
                }
                else
                {
                    GetTopLevelMenu().CloseMenus();
                }
            }
        }

        public void MenuItemEnter(object c, GLMouseEventArgs e)     
        {
            GLBaseControl b = c as GLBaseControl;
            mousehovered = ControlsIZ.IndexOf(b);       // when we move the mouse, the selected is discarded, and mousehover takes over
            SetSelected(-1);

            GLMenuItem mi = ControlsIZ[mousehovered] as GLMenuItem; // if its a menu item, lets do a timer to autoopen
            if (mi != null && AutoOpenDelay>0)
                timer.Start(AutoOpenDelay);
        }

        public void MenuItemLeave(object c, GLMouseEventArgs e)
        {
            timer.Stop();
            mousehovered = -1;
        }

        public void Timeout(GLOFC.Timers.Timer t, long tick)
        {
            Select(mousehovered,FlowDirection==ControlFlowDirection.Right);        // if we are a flow right menu, focus changes to below
        }

        protected override void OnKeyDown(GLKeyEventArgs e)
        {
            base.OnKeyPress(e);
            //System.Diagnostics.Debug.WriteLine("Keydown in {0} {1}", Name, e.KeyCode);

            if (e.KeyCode == System.Windows.Forms.Keys.Left)
            {
                if (FlowDirection == ControlFlowDirection.Right)                // if we are in a flow right menu, its a negative to us
                    Move(-1);
                else if (parentmenu?.FlowDirection == ControlFlowDirection.Right)   // else see if the parent is flow right..
                    parentmenu.Move(-1);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Right)
            {
                if (FlowDirection == ControlFlowDirection.Right)
                    Move(1);
                else if (parentmenu?.FlowDirection == ControlFlowDirection.Right)
                    parentmenu.Move(1);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Up)
            {
                if (FlowDirection == ControlFlowDirection.Down)
                    Move(-1);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Down)
            {
                if (FlowDirection == ControlFlowDirection.Down)
                    Move(1);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Return)
            {
                if (FlowDirection == ControlFlowDirection.Right && selected == -1)      // if menu bar, and unselected, its like a right
                    Move(1);
                else
                    ActivateSelected();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Back)
            {
                if (parentmenu?.FlowDirection != ControlFlowDirection.Right)        // if in a up/down, it means close this menu from parent
                    parentmenu?.CloseSubMenus();
                else
                    parentmenu?.CloseMenus();
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Escape)     // full shut down
            {
                GetTopLevelMenu().CloseMenus();
            }

        }

        public void NonMIKeyDown(object o, GLKeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Up)
            {
                Move(-1);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Down)
            {
                Move(1);
            }

        }

        #endregion

        private Color mouseOverBackColor { get; set; } = DefaultMouseOverButtonColor;
        private Color foreColor { get; set; } = DefaultControlForeColor;
        private Color iconStripBackColor { get; set; } = DefaultMenuIconStripBackColor;

        private int selected = -1;              // open which is highlighted/open
        private int mousehovered = -1;          // if over a menu item

        private GLMenuStrip submenu = null;     // submenu which is opened
        private GLMenuStrip parentmenu = null;  // parent menu, null for top level menu

        private GLOFC.Timers.Timer timer = new Timers.Timer();

        private bool openedascontextmenu = false;
    }

    // Helper class - use Show() to make it visible. Do not attach to anything at creation. Note they always flow Down
    public class GLContextMenu : GLMenuStrip
    {
        public GLContextMenu(string name, params GLMenuItem[] items) : base(name, DefaultWindowRectangle, ControlFlowDirection.Down, items)
        {
        }
    }
}

