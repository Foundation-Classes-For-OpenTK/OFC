/*
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
using System.Drawing.Drawing2D;

namespace OFC.GL4.Controls
{
    public class GLMenuStrip : GLFlowLayoutPanel
    {
        public Action<GLMenuItem> SubmenuOpened = null;         // SubMenu opening, called if a top level menu
        public Action<GLMenuItem> SubmenuClosed = null;         // SubMenu closing, called if a top level menu

        // Inherited FlowInZOrder, FlowDirection, FlowPadding, BackColor

        public Color ForeColor { get { return foreColor; } set { foreColor = value; Invalidate(); } }       // of text.  Set to Color.Empty for no override
        public Color MouseOverBackColor { get { return mouseOverBackColor; } set { mouseOverBackColor = value; Invalidate(); } }    // Set Color.Empty for no override

        public GLMenuStrip(string name, Rectangle location) : base(name, location)
        {
            FlowInZOrder = false;
            Focusable = true;       // allow focus to go to us, so we don't lost focus=null for the gfocus check
            timer.Tick += Timeout;
        }

        public GLMenuStrip() : this("Menu?", DefaultWindowRectangle)
        {
        }

        public GLMenuStrip(string name, DockingType type, float dockpercent) : this(name, DefaultWindowRectangle)
        {
            Dock = type;
            DockPercent = dockpercent;
        }

        public GLMenuStrip(string name, Size sizep, DockingType type, float dockpercentage) : this(name, DefaultWindowRectangle)
        {
            Dock = type;
            DockPercent = dockpercentage;
            SetLocationSizeNI(bounds: sizep);
        }

        public override void OnControlAdd(GLBaseControl parent, GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("On control add {0}:{1} {2}:{3}", parent.GetType().Name, parent.Name, child.GetType().Name, child.Name);

            // we only add if we are not getting the parent OnControlAdd call, which you can tell because parent=this if it is that
            // and we are a top level menu. Submenus don't need to hook this as well.
            if ( parent != this && parentmenu == null)      
            {
                //System.Diagnostics.Debug.WriteLine("Add G Focus to " + this.GetType().Name + " " + Name);
                FindDisplay().GlobalFocusChanged += GFocusChanged;
            }

            child.BackColor = BackColor;

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
            }
            var cb = child as GLComboBox;
            if (cb != null)
            {
                cb.DropDownStateChanged += DropDownChild;
                cb.SelectedIndexChanged += ComboBoxSelectedIndex;
            }

            var chbox = child as GLCheckBox;
            if (chbox != null)
            {
                chbox.CheckChanged += CheckBoxChanged;
            }

            base.OnControlAdd(parent, child);
        }

        public override void OnControlRemove(GLBaseControl parent, GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("On control remove {0}:{1} {2}:{3}", parent.GetType().Name, parent.Name, child.GetType().Name, child.Name);
            if (parent != this && parentmenu == null)       // unhook GFC from top level menu
            {
                //System.Diagnostics.Debug.WriteLine("Remove G Focus to " + this.GetType().Name + " " + Name);
                FindDisplay().GlobalFocusChanged -= GFocusChanged;
            }

            var mi = child as GLMenuItem;
            if (mi != null)                     // make sure we unhook!  this caught me out.
            {
                mi.Click -= MenuItemClicked;
                mi.MouseEnter -= MenuItemEnter;
                mi.MouseLeave -= MenuItemLeave;
            }

            var cb = child as GLComboBox;
            if (cb != null)
            {
                cb.DropDownStateChanged -= DropDownChild;
                cb.SelectedIndexChanged -= ComboBoxSelectedIndex;
            }

            var chbox = child as GLCheckBox;
            if (chbox != null)
            {
                chbox.CheckChanged -= CheckBoxChanged;
            }

            base.OnControlRemove(parent, child);
        }

        public GLMenuStrip GetTopLevelMenu()
        {
            GLMenuStrip m = this;
            while (m.parentmenu != null)
                m = m.parentmenu;
            return m;
        }

        // hooked only to primary top level parent menu
        private void GFocusChanged(GLControlDisplay disp, GLBaseControl from , GLBaseControl to)
        {
            System.Diagnostics.Debug.WriteLine("G Focus {0}, {1}, {2}", from?.GetType()?.Name, to?.GetType()?.Name, comboboxchilddropdown);

            if (comboboxchilddropdown == false && (to == null || !IsThisOrChildControl(to)))     // not a drop down, not a child of our tree
                GetTopLevelMenu().CloseSubMenus();
        }

        private bool IsThisOrChildControl(GLBaseControl to)        // find out if to is this or a child in the tree
        {
            if (to == this || ControlsIZ.Contains(to))
                return true;
            else
                return submenu != null ? submenu.IsThisOrChildControl(to) : false;      // check out submenus
        }

        private void DropDownChild(GLBaseControl cb, bool state)
        {
            GetTopLevelMenu().comboboxchilddropdown = state;    // need to tell top level menu, which GFocusChanged is called on, its combo boxed
        }

        private void CheckBoxChanged(GLBaseControl child)       // hook check changed on checkbox, if autocheck, close menus
        {
            var chbox = child as GLCheckBox;
            if (chbox.CheckOnClick)
            {
                GetTopLevelMenu().CloseSubMenus();
            }
        }

        private void ComboBoxSelectedIndex(GLBaseControl bc)    // hook, selecting an item closes the menus
        {
            GetTopLevelMenu().CloseSubMenus();
        }

        private void MenuItemClicked(GLBaseControl c, GLMouseEventArgs e)
        {
            System.Diagnostics.Debug.Assert(Parent != null);        // double check we are not on a zombie control, due to the add/remove stuff

            var mi = c as GLMenuItem;
            if (mi != null)               // menu items are handled by us
            {
                if (mi.SubMenuItems != null)
                {
                    OpenItem(mi);
                }
                else
                {
                    GetTopLevelMenu().CloseSubMenus();      // clickable item , close all
                }
            }
        }

        private void OpenItem(GLMenuItem mi)     // open this item 
        {
            if (submenumi != mi)      // don't double open
            {
                CloseSubMenus();        // close any current ones

                if (mi.SubMenuItems != null)
                {
                    Point p = this.DisplayControlCoords(false);

                    if (FlowDirection == ControlFlowDirection.Right)       // pick co-ords based on flow
                    {
                        p.X += mi.Left - this.ClientLeftMargin;
                        p.Y += this.Height;
                    }
                    else
                    {
                        p.X += this.Width;
                        p.Y += mi.Top;
                    }

                    submenu = new GLMenuStrip(Name + "." + (Environment.TickCount % 100), new Rectangle(p.X, p.Y, 200, 200));        // size is immaterial , autosize both
                    submenu.Font = Font;
                    submenu.ForeColor = this.ForeColor;
                    submenu.BackColor = this.BackColor;
                    submenu.MouseOverBackColor = this.MouseOverBackColor;
                    submenu.FlowDirection = ControlFlowDirection.Down;
                    submenu.AutoSize = true;
                    submenu.SuspendLayout();
                    submenu.parentmenu = this;
                    submenu.TopMost = true;

                    int iconareawidth = Font.ScalePixels(24);

                    foreach (var m in mi.SubMenuItems)
                    {
                        var mis = m as GLMenuItem;
                        if (mis != null)
                        {
                            mis.TextAlign = ContentAlignment.MiddleLeft;
                            mis.Padding = new Padding(2);                           // fix padding here
                            mis.IconTickAreaWidth = iconareawidth;                  // set button icon width for check/images
                        }
                        else
                            m.Margin = new Margin(iconareawidth + 2, 0, 0, 0);      // compensate for padding MI

                        submenu.Add(m);
                    }

                    submenu.ResumeLayout();
                    FindDisplay().Add(submenu);

                    submenumi = mi;

                    GetTopLevelMenu().SubmenuOpened?.Invoke(mi);                 // call, allowing configuration of the submenu
                }
            }
        }

        private void CloseSubMenus()
        {
            if (submenu != null)                
            {
                submenu.CloseSubMenus();    // close child submenus first

                System.Diagnostics.Debug.WriteLine("Close submenu " + submenu.Name);
                submenu.SuspendLayout();
                List<GLBaseControl> todelete = new List<GLBaseControl>(submenu.ControlsZ);
                foreach (var c in todelete)
                {
                    submenu.Remove(c, false);        // do not dispose of them, we want to be able to reuse
                }

                FindDisplay().Remove(submenu);

                GetTopLevelMenu().SubmenuClosed?.Invoke(submenumi);                 // call after action

                submenu = null;
                submenumi = null;
            }
        }

        public void MenuItemEnter(object c, GLMouseEventArgs e)
        {
            var mi = c as GLMenuItem;

            if ( mi != null && mi != submenumi )       // if on a menu item but not the open opened (if submenumi = null then that also counts)
            {
                gotomenuitem = mi;                          // we indicate to change to this one, even if it does not have a submenu
                timer.Start(submenu!= null ? 500 : 1000);        // slower if opening a new sublayer
            }
        }

        public void MenuItemLeave(object c, GLMouseEventArgs e)
        {
            timer.Stop();
        }

        public void Timeout(OFC.Timers.Timer t, long tick)
        {
            OpenItem(gotomenuitem);
        }

        private Color mouseOverBackColor { get; set; } = DefaultMouseOverButtonColor;
        private Color foreColor { get; set; } = DefaultControlForeColor;

        private GLMenuStrip submenu = null;      // submenu opened
        private GLMenuItem submenumi = null;     // menu item which opened sub menu
        private GLMenuStrip parentmenu = null;   // parent menu, null for top level menu

        private GLMenuItem gotomenuitem = null; // set for timer to go to new menu item

        private OFC.Timers.Timer timer = new Timers.Timer();

        private bool comboboxchilddropdown = false;     // only used in top level menu

    }
}

