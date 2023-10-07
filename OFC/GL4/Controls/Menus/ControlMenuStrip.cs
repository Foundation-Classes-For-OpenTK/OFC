/*
 * Copyright 2019-2023 Robbyxp1 @ github.com
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

using GLOFC.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// A Horizonal or vertical menu strip
    /// </summary>
    public class GLMenuStrip : GLFlowLayoutPanel
    {
        #region Init

        /// <summary> Callback when a top level menu is opening. 
        /// Passes this and the caller opentag given in the Show function
        /// Called before the menu is attached to the parent. You can set size of the menu etc.
        /// You can set menu items visibility and enable state in the callback.
        /// </summary>
        public Action<GLMenuStrip,Object> Opening = null;         // Menu opening due to Show
        /// <summary> Callback, all submenus are requesting to close.  Called on top level menu only. Return true to allow the close to happen. </summary>
        public Func<GLMenuStrip, bool> Closing = null;      

        /// <summary> Callback, called when a submenu is opening </summary>
        public Action<GLMenuItem, GLMenuStrip> SubmenuOpened = null;        
        /// <summary> Callback, called per submenu when it closes</summary>
        public Action<GLMenuItem, GLMenuStrip> SubmenuClosing = null;       

        // Inherited FlowInZOrder, FlowDirection, FlowPadding, BackColor

        /// <summary> Icon strip back color </summary>
        public Color IconStripBackColor { get { return iconStripBackColor; } set { iconStripBackColor = value; Invalidate(); } }

        /// <summary> Icon strip width </summary>
        public int IconAreaWidth { get { return Font.ScalePixels(24); } }

        /// <summary> Sub menu border width</summary>
        public int SubMenuBorderWidth { get; set; } = 0;

        /// <summary> Auto open delay in ms on hover over a submenu. 0 for off </summary>
        public int AutoOpenDelay { get; set; } = 250;

        /// <summary>
        /// Construct a menu strip
        /// </summary>
        /// <param name="name">Name of strip</param>
        /// <param name="location">location of strip</param>
        /// <param name="direction">Direction (Right or Down) </param>
        /// <param name="enablethemer">If to theme </param>
        /// <param name="items">List of menu items</param>
        public GLMenuStrip(string name, Rectangle location, GLFlowLayoutPanel.ControlFlowDirection direction = ControlFlowDirection.Right, 
                                bool enablethemer = true, params GLBaseControl[] items) : base(name, location)
        {
            BackColorGradientAltNI = BackColorNI = DefaultMenuBackColor;
            BorderColorNI = DefaultMenuBorderColor;
            FlowDirection = direction;
            FlowInZOrder = false;
            Focusable = true;       // allow focus to go to us, so we don't lost focus=null for the gfocus check
            EnableThemer = enablethemer;
            timer.Tick += Timeout;
            foreach (var e in items)
                Add(e);
        }

        /// <summary> Default constructor </summary>
        public GLMenuStrip() : this("Menu?", DefaultWindowRectangle)
        {
        }

        /// <summary>
        /// Construct a docking menu strip
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="dock">Docking mode</param>
        /// <param name="dockpercent">Docking percent</param>
        /// <param name="direction">Direction (Right or Down) </param>
        /// <param name="enablethemer">If to theme </param>
        /// <param name="items">List of menu items</param>
        public GLMenuStrip(string name, DockingType dock, float dockpercent, GLFlowLayoutPanel.ControlFlowDirection direction = ControlFlowDirection.Right, 
                                bool enablethemer = true, params GLMenuItem[] items) : 
                            this(name, DefaultWindowRectangle, direction, enablethemer, items)
        {
            Dock = dock;
            DockPercent = dockpercent;
        }

        /// <summary>
        /// Show to pop up a new context menu. Parent is normally displaycontrol.
        /// You can double call and the previous one is closed.
        /// It always makes sure right/bottom is within parent.
        /// Opening is call backed just before being added to the parent. You can adjust size/items at this point
        /// Position may shift afterwards to place on screen.
        /// </summary>
        /// <param name="parent">Parent of menu to attach to. Normally its the control display top level control</param>
        /// <param name="location">Location to place the menu at</param>
        /// <param name="changewidthifrequired">If set, it will reflow the menu to not exceed parent, making the window wider if required, </param>
        /// <param name="opentag">Tag to pass to Opening callback</param>
        public void Show(GLBaseControl parent, Point location, bool changewidthifrequired = false, Object opentag = null)        
        {
            //System.Diagnostics.Debug.WriteLine("Open as context menu " + Name);
            Detach(this);
            openedascontextmenu = true;
            Visible = true;     
            Location = location;
            AutoSize = true;
            TopMost = true;
            KeepWithinParent = changewidthifrequired;
            Opening?.Invoke(this, opentag);
            parent.Add(this);
            int overflowy = this.Bottom - parent.Bottom;
            if (overflowy > 0)
                Top -= overflowy;
            int overflowx = this.Right - parent.Right;
            if (overflowx > 0)
                Left -= overflowx;
            SetFocus();
        }

        #endregion  

        #region Menu Openers

        /// <summary>
        /// Select an item at index
        /// </summary>
        /// <param name="index">Index of item</param>
        /// <param name="focusto">If true, change focus to item</param>
        /// <returns>true if changed to item</returns>
        public bool Select(int index, bool focusto)     
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
                    submenu.Owner = this;                   // associate with us
                    submenu.ScaleWindow = FindScaler();
                    submenu.Font = Font;
                    submenu.BackColor = this.BackColor;
                    submenu.BackColorGradientAlt = this.BackColorGradientAlt;
                    submenu.BackColorGradientDir = this.BackColorGradientDir;
                    submenu.IconStripBackColor = this.IconStripBackColor;
                    submenu.FlowDirection = ControlFlowDirection.Down;
                    submenu.AutoSize = true;
                    submenu.AutoOpenDelay = AutoOpenDelay;
                    submenu.parentmenu = this;
                    submenu.TopMost = true;
                    submenu.BorderWidth = SubMenuBorderWidth;
                    submenu.SubMenuBorderWidth = SubMenuBorderWidth;
                    submenu.AddItems(mi.SubMenuItems);
                    submenu.SetSelected(-1);                                    // release all items for hover highlighting

                    //System.Diagnostics.Debug.WriteLine("Open menu " + submenu.Name + " " + submenu.Bounds);

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

        /// <summary> Move selected item up/left (negative count) or right/down (positive count) </summary>
        public bool Move(int count)                           
        {
            int pos = selected;
            while (true)
            {
                pos += count;
                if (pos < 0 || pos >= ControlsIZ.Count)     // out of range, can't move
                    return false;

                if (ControlsIZ[pos].Enabled && ControlsIZ[pos].Visible && ((GLMenuItemBase)ControlsIZ[pos]).Selectable)     // if on
                {
                    Select(pos, FlowDirection == ControlFlowDirection.Right);   // set focus on if left-right menu
                    return true;
                }
                else if (count == 0)
                    return false;
            }
        }

        /// <summary> Activate the selected item. Opens a submenu, or clicks on a normal item</summary>
        public void ActivateSelected()                  
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

        /// <summary> Find top level menu of this tree </summary>
        public GLMenuStrip GetTopLevelMenu()
        {
            GLMenuStrip m = this;
            while (m.parentmenu != null)
                m = m.parentmenu;
            return m;
        }

        /// <summary> Close all menus, checking on Closing callback for permission
        /// If its a context menu, the menu will be detached from the parent </summary>
        public void CloseMenus()        
        {
            //System.Diagnostics.Debug.WriteLine($"{Name} Close menus");

            var allow = Closing?.Invoke(this) ?? true;
            if (allow)
            {
                CloseSubMenus();
                SetSelected(-1);
                if (openedascontextmenu)
                {
                    //System.Diagnostics.Debug.WriteLine($"{Name} Detach");
                    Detach(this);
                }
            }
        }

        /// <summary> Close submenus of this menu </summary>
        public void CloseSubMenus()     
        {
            if (submenu != null)
            {
                GetTopLevelMenu().SubmenuClosing?.Invoke(ControlsIZ[selected] as GLMenuItem, submenu);                 // call before close
                submenu.CloseSubMenus();    // close child submenus first
                //System.Diagnostics.Debug.WriteLine($"{Name} Close down");
                submenu.Close();
                submenu = null;
                SetFocus();
            }
        }

        #endregion

        #region Implementation

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnControlAdd(GLBaseControl, GLBaseControl)"/>
        protected override void OnControlAdd(GLBaseControl parent, GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("Menu strip On control add {0}:{1} {2}:{3}", parent.GetType().Name, parent.Name, child.GetType().Name, child.Name);

            if (parent is GLMenuStrip )                             // note we get called when the GLMenuStrip is added to the display, we don't want that call
            {
                child.SuspendLayout();

                var item = child as GLMenuItemBase;                    

                if (item == null)       // another type of control.. lets set its flow offset
                {
                    if (FlowDirection == ControlFlowDirection.Down)
                    {
                        child.FlowOffsetPosition = new Point(IconAreaWidth, 0);
                    }

                    child.KeyDown += NonMIKeyDown;
                }
                else
                {
                    // a menu item. all Menu items implement this to allow for icon areas
                    item.IconAreaEnable = FlowDirection == ControlFlowDirection.Down;     

                    var mi = child as GLMenuItem;
                    if (mi != null)                     // these gets hooked to our handlers007
                    {
                        mi.Click += MenuItemClicked;
                        mi.MouseEnter += MenuItemEnter;
                        mi.MouseLeave += MenuItemLeave;
                    }
                }

                child.ResumeLayout();
            }

            base.OnControlAdd(parent, child);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnControlRemove(GLBaseControl, GLBaseControl)"/>
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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnGlobalMouseDown(GLBaseControl, GLMouseEventArgs)"/>
        protected override void OnGlobalMouseDown(GLBaseControl ctrl, GLMouseEventArgs e)
        {
            base.OnGlobalMouseDown(ctrl, e);

            if ( parentmenu == null )       // if top level menu.. top of heirarchy
            {
                bool isthisorchild = ctrl != null && IsThisOrChildOf(ctrl);
                //System.Diagnostics.Debug.WriteLine($"{Name} Top level, click on child {isthisorchild}");

                if (!isthisorchild)
                {
                    GetTopLevelMenu().CloseMenus();
                }
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.IsThisOrChildOf(GLBaseControl)"/>
        public override bool IsThisOrChildOf(GLBaseControl ctrl)        // submenus are us, so its a child
        {
            if (base.IsThisOrChildOf(ctrl))
                return true;
            else if (submenu != null && submenu.IsThisOrChildOf(ctrl))
                return true;
            else
                return false;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.DrawBack(Rectangle, Graphics, Color, Color, int)"/>
        protected override void DrawBack(Rectangle area, Graphics gr, Color bc, Color bcgradientalt, int bcgradient)
        {
            //base.DrawBack(area, gr, Focused ? Color.Green : Color.White , bcgradientalt, bcgradient); // for debugging
            base.DrawBack(area, gr, bc , bcgradientalt, bcgradient);

            if (FlowDirection == ControlFlowDirection.Down)
            {
                using (Brush br = new SolidBrush(IconStripBackColor))
                {
                    gr.FillRectangle(br, new Rectangle(area.Left, area.Top, IconAreaWidth, area.Height));
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

        private void MenuItemEnter(object c, GLMouseEventArgs e)     
        {
            GLBaseControl b = c as GLBaseControl;
            mousehovered = ControlsIZ.IndexOf(b);       // when we move the mouse, the selected is discarded, and mousehover takes over
            SetSelected(-1);

            GLMenuItem mi = ControlsIZ[mousehovered] as GLMenuItem; // if its a menu item, lets do a timer to autoopen
            if (mi != null && AutoOpenDelay>0)
                timer.Start(AutoOpenDelay);
        }

        private void MenuItemLeave(object c, GLMouseEventArgs e)
        {
            timer.Stop();
            mousehovered = -1;
        }

        private void Timeout(PolledTimer t, long tick)
        {
            Select(mousehovered,FlowDirection==ControlFlowDirection.Right);        // if we are a flow right menu, focus changes to below
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyDown(GLKeyEventArgs)"/>
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

        private void NonMIKeyDown(object o, GLKeyEventArgs e)
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

        private Color iconStripBackColor { get; set; } = DefaultMenuIconStripBackColor;

        private int selected = -1;              // open which is highlighted/open
        private int mousehovered = -1;          // if over a menu item

        private GLMenuStrip submenu = null;     // submenu which is opened
        private GLMenuStrip parentmenu = null;  // parent menu, null for top level menu

        private PolledTimer timer = new PolledTimer();

        private bool openedascontextmenu = false;
    }

    /// <summary>
    /// All standard menu items inherit from this
    /// </summary>
    public interface GLMenuItemBase
    {
        /// <summary> Is selectable? </summary>
        bool Selectable { get; set; }
        /// <summary> To enable the icon area on left. Used for sub menu items</summary>
        bool IconAreaEnable { get; set; }
    }

    /// <summary>
    /// Context Menu instance of GLMenuStrip
    /// Use Show() to make it visible and attach to parent.
    /// </summary>
    public class GLContextMenu : GLMenuStrip
    {
        /// <summary> Constructor with name and menu items </summary>
        public GLContextMenu(string name, bool enablethemer = true, params GLBaseControl[] items) : base(name, DefaultWindowRectangle, ControlFlowDirection.Down, enablethemer, items)
        {
        }
    }
}

