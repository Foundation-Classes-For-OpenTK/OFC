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
        public Color IconStripBackColor { get { return iconStripBackColor; } set { iconStripBackColor = value; Invalidate(); } }

        public GLMenuStrip(string name, Rectangle location) : base(name, location)
        {
            FlowInZOrder = false;
            Focusable = true;       // allow focus to go to us, so we don't lost focus=null for the gfocus check
            timer.Tick += Timeout;
            FlowDirection = GLFlowLayoutPanel.ControlFlowDirection.Right;
        }

        public GLMenuStrip(string name = "Menu?") : this(name, DefaultWindowRectangle)
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

        // call to pop up the context menu, parent is normally displaycontrol
        // you can double call and the previous one is closed
        public void OpenAsContextMenu(GLBaseControl parent, Point coord)        
        {
            System.Diagnostics.Debug.WriteLine("Open as context menu " + Name);
            if ( Parent != null )                   
                Parent.Detach(this);
            openedascontextmenu = true;
            Location = coord;
            AutoSize = true;
            TopMost = true;
            parent.Add(this);
        }

        #region Implementation

        public override void OnControlAdd(GLBaseControl parent, GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("On control add {0}:{1} {2}:{3}", parent.GetType().Name, parent.Name, child.GetType().Name, child.Name);

            // we only add if we are not getting the parent OnControlAdd call, which you can tell because parent=this if it is that
            // and we are a top level menu. Submenus don't need to hook this as well.
            if (parent != this && parentmenu == null)
            {
                FindDisplay().GlobalMouseClick += GMouseClick;
            }

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

                    if (FlowDirection == ControlFlowDirection.Down)
                    {
                        child.FlowOffsetPosition = new Point(iconareawidth, 0);
                    }

                }

                child.ResumeLayout();
            }

            base.OnControlAdd(parent, child);
        }

        public override void OnControlRemove(GLBaseControl parent, GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("On control remove {0}:{1} {2}:{3}", parent.GetType().Name, parent.Name, child.GetType().Name, child.Name);
            if (parent != this && parentmenu == null)       // unhook GFC from top level menu
            {
                FindDisplay().GlobalMouseClick -= GMouseClick;
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
        private void GMouseClick(GLControlDisplay disp, GLBaseControl item, GLMouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("G Mouse clickFocus {0}, {1}, {2}", item?.GetType()?.Name, e.Button ,e.ScreenCoord);
            if (comboboxchilddropdown == false && (item == null || !IsThisOrChildControl(item)))     // not a drop down, not a child of our tree
            {
                GetTopLevelMenu().CloseSubMenusAndUsIfCM();
            }
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
                GetTopLevelMenu().CloseSubMenusAndUsIfCM();
            }
        }

        private void ComboBoxSelectedIndex(GLBaseControl bc)    // hook, selecting an item closes the menus
        {
            GetTopLevelMenu().CloseSubMenusAndUsIfCM();
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
                    GetTopLevelMenu().CloseSubMenusAndUsIfCM();
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
                    submenu.SuspendLayout();

                    submenu.Font = Font;
                    submenu.ForeColor = this.ForeColor;
                    submenu.BackColor = this.BackColor;
                    submenu.MouseOverBackColor = this.MouseOverBackColor;
                    submenu.FlowDirection = ControlFlowDirection.Down;
                    submenu.AutoSize = true;
                    submenu.parentmenu = this;
                    submenu.TopMost = true;

                    submenu.AddItems(mi.SubMenuItems);

                    submenu.ResumeLayout();

                    FindDisplay().Add(submenu);

                    submenumi = mi;

                    GetTopLevelMenu().SubmenuOpened?.Invoke(mi);                 // call, allowing configuration of the submenu
                }
            }
        }

        private void CloseSubMenusAndUsIfCM()
        {
            System.Diagnostics.Debug.WriteLine("Close menus and us " + Name);
            CloseSubMenus();
            if (openedascontextmenu)
            {
                Parent.Detach(this);
            }
        }

        private void CloseSubMenus()
        {
            System.Diagnostics.Debug.WriteLine("Close menus " + Name);
            if (submenu != null)
            {
                submenu.CloseSubMenus();    // close child submenus first
                submenu.CloseMenu();
                GetTopLevelMenu().SubmenuClosed?.Invoke(submenumi);                 // call, allowing configuration of the submenu
                submenu = null;
                submenumi = null;
            }
        }

        private void CloseMenu()
        {
            System.Diagnostics.Debug.WriteLine("Close menu " + Name);
            SuspendLayout();
            List<GLBaseControl> todelete = new List<GLBaseControl>(ControlsZ);
            foreach (var c in todelete)
            {
                Detach(c);        // detach but do not dispose..
            }

            ResumeLayout();
            Parent.Remove(this);
        }

        public void MenuItemEnter(object c, GLMouseEventArgs e)
        {
            var mi = c as GLMenuItem;

            if (mi != null && mi != submenumi)       // if on a menu item but not the open opened (if submenumi = null then that also counts)
            {
                gotomenuitem = mi;                          // we indicate to change to this one, even if it does not have a submenu
                timer.Start(submenu != null ? 250 : 500);        // slower if opening a new sublayer
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

        protected override void DrawBack(Rectangle area, Graphics gr, Color bc, Color bcgradientalt, int bcgradient)
        {
            base.DrawBack(area, gr, bc, bcgradientalt, bcgradient);

            if (FlowDirection == ControlFlowDirection.Down)
            {
                //IconStripBackColor = Color.Red;
                using (Brush br = new SolidBrush(IconStripBackColor))
                {
                    int iconareawidth = Font.ScalePixels(24);
                    gr.FillRectangle(br, new Rectangle(area.Left, area.Top, iconareawidth, area.Height));
                }
            }
        }

        #endregion

        private Color mouseOverBackColor { get; set; } = DefaultMouseOverButtonColor;
        private Color foreColor { get; set; } = DefaultControlForeColor;
        private Color iconStripBackColor { get; set; } = DefaultMenuIconStripBackColor;

        private GLMenuStrip submenu = null;      // submenu opened
        private GLMenuItem submenumi = null;     // menu item which opened sub menu
        private GLMenuStrip parentmenu = null;   // parent menu, null for top level menu

        private GLMenuItem gotomenuitem = null; // set for timer to go to new menu item

        private OFC.Timers.Timer timer = new Timers.Timer();

        private bool comboboxchilddropdown = false;     // only used in top level menu

        private bool openedascontextmenu = false;
    }
}

