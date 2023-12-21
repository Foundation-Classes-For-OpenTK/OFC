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

// Rules - no winforms in Control land except for Keys

using GLOFC.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// This namespace contains a OFC User Interface which allows you to produce a UI for your GL program
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Base class for all controls
    /// </summary>

    [System.Diagnostics.DebuggerDisplay("Control {Name} {window}")]
    public abstract partial class GLBaseControl : IDisposable
    {
        #region Main UI

        /// <summary> Name of control </summary>
        public string Name { get; set; } = "?";

        // bounds of the window - include all margin/padding/borders/
        // co-ords are in offsets from 0,0 being the parent top left corner. See also Set()

        /// <summary> Bounds of the window, in parent co-ordinates </summary>
        public Rectangle Bounds { get { return window; } set { SetPos(value.Left, value.Top, value.Width, value.Height); } }
        /// <summary> Left of window </summary>
        public int Left { get { return window.Left; } set { SetPos(value, window.Top, window.Width, window.Height); } }
        /// <summary> Right of window</summary>
        public int Right { get { return window.Right; } set { SetPos(window.Left, window.Top, value - window.Left, window.Height); } }
        /// <summary> Top of window</summary>
        public int Top { get { return window.Top; } set { SetPos(window.Left, value, window.Width, window.Height); } }
        /// <summary> Bottom of window</summary>
        public int Bottom { get { return window.Bottom; } set { SetPos(window.Left, window.Top, window.Width, value - window.Top); } }
        /// <summary> Width of window</summary>
        public int Width { get { return window.Width; } set { SetPos(window.Left, window.Top, value, window.Height); } }
        /// <summary> Height of window</summary>
        public int Height { get { return window.Height; } set { SetPos(window.Left, window.Top, window.Width, value); } }
        /// <summary> Top left location of window</summary>
        public Point Location { get { return new Point(window.Left, window.Top); } set { SetPos(value.X, value.Y, window.Width, window.Height); } }
        /// <summary> Size of window</summary>
        public Size Size { get { return new Size(window.Width, window.Height); } set { SetPos(window.Left, window.Top, value.Width, value.Height); } }

        /// <summary> Minimum size allowed </summary>
        public Size MinimumSize { get { return minimumsize; } set { if (value != minimumsize) { minimumsize = value; SetPos(window.Left, window.Top, window.Width, window.Height); } } }
        /// <summary> Maximum size allowed </summary>
        public Size MaximumSize { get { return maximumsize; } set { if (value != maximumsize) { maximumsize = value; SetPos(window.Left, window.Top, window.Width, window.Height); } } }

        /// <summary> Only for top level controls, they can be presented scaled 0..1 (normal) ..N</summary>
        public SizeF? ScaleWindow { get { return altscale; } set { altscale = value; TopLevelControlUpdate = true; FindDisplay()?.ReRender(); } }
        /// <summary> Return the size on screen after scaling</summary>
        public Size ScaledSize { get { if (altscale != null) return new Size((int)(Width * ScaleWindow.Value.Width), (int)(Height * ScaleWindow.Value.Height)); else return Size; } }

        // padding/margin and border control (Do not apply to display control)
        /// <summary> Padding: between the border line and client area </summary>
        public PaddingType Padding { get { return padding; } set { if (padding != value) { padding = value; CalcClientRectangle(); InvalidateLayout(); } } }
        /// <summary> Margin: between the windows bounds and the border line</summary>
        public MarginType Margin { get { return margin; } set { if (margin != value) { margin = value; CalcClientRectangle(); InvalidateLayout(); } } }
        /// <summary> Set up margin, border width, and padding quickly </summary>
        public void SetMarginBorderWidth(MarginType m, int borderw, Color borderc, PaddingType p) { margin = m; padding = p; bordercolor = borderc; borderwidth = borderw; CalcClientRectangle(); InvalidateLayout(); }
        /// <summary> Border color </summary>
        public Color BorderColor { get { return bordercolor; } set { if (bordercolor != value) { bordercolor = value; Invalidate(); } } }
        /// <summary> Border Width, 0 = off </summary>
        public int BorderWidth { get { return borderwidth; } set { if (borderwidth != value) { borderwidth = value; CalcClientRectangle(); InvalidateLayout(); } } }

        // this is the client area, inside the margin/padding/border
        /// <summary> Client Left Margin: Includes margin, border line and padding</summary>
        public int ClientLeftMargin { get { return Margin.Left + Padding.Left + BorderWidth; } }
        /// <summary> Client Right Margin: Includes margin, border line and padding</summary>
        public int ClientRightMargin { get { return Margin.Right + Padding.Right + BorderWidth; } }
        /// <summary> Client Total Width of Margin: Includes margin, border line and padding</summary>
        public int ClientWidthMargin { get { return Margin.TotalWidth + Padding.TotalWidth + BorderWidth * 2; } }
        /// <summary> Client Top Margin: Includes margin, border line and padding</summary>
        public int ClientTopMargin { get { return Margin.Top + Padding.Top + BorderWidth; } }
        /// <summary> Client Bottom Margin: Includes margin, border line and padding</summary>
        public int ClientBottomMargin { get { return Margin.Bottom + Padding.Bottom + BorderWidth; } }
        /// <summary> Client Total Height of Margin: Includes margin, border line and padding</summary>
        public int ClientHeightMargin { get { return Margin.TotalHeight + Padding.TotalHeight + BorderWidth * 2; } }
        /// <summary> Client area width</summary>
        public int ClientWidth { get { return ClientRectangle.Width; } set { SetPos(window.Left, window.Top, value + ClientLeftMargin + ClientRightMargin, window.Height); } }
        /// <summary> Client area height</summary>
        public int ClientHeight { get { return ClientRectangle.Height; } set { SetPos(window.Left, window.Top, window.Width, value + ClientTopMargin + ClientBottomMargin); } }
        /// <summary> Client area size</summary>
        public Size ClientSize { get { return ClientRectangle.Size; } set { SetPos(window.Left, window.Top, value.Width + ClientLeftMargin + ClientRightMargin, value.Height + ClientTopMargin + ClientBottomMargin); } }
        /// <summary> Client location top left as a Point</summary>
        public Point ClientLocation { get { return new Point(ClientLeftMargin, ClientTopMargin); } }
        /// <summary> The client rectangle, in terms of the controls bounds</summary>
        public Rectangle ClientRectangle { get; private set; }

        // docking control 
        /// <summary> Docking type</summary>
        public DockingType Dock { get { return docktype; } set { if (docktype != value) { docktype = value; ParentInvalidateLayout(); } } }
        /// <summary> Docking margin, allows docking to be offset from edge, for Left,Right,Bottom,Top  </summary>
        public MarginType DockingMargin { get { return dockingmargin; } set { if (dockingmargin != value) { dockingmargin = value; InvalidateLayout(); } } }
        /// <summary> Docking percent of window, allows docked window width or height to be set to a percentage of the parent bounds, for Left,Right,Bottom,Top </summary>
        public float DockPercent { get { return dockpercent; } set { if (value != dockpercent) { dockpercent = value; ParentInvalidateLayout(); } } }        // % in 0-1 terms used to dock on left,top,right,bottom.  0 means just use width/height

        /// <summary> Anchor, applied when Dock=None, allows the control to be anchored to the Right and/or Bottom of the window</summary>
        public AnchorType Anchor { get { return anchortype; } set { if (value != anchortype) { anchortype = value; ParentInvalidateLayout(); } } }

        /// <summary> Autosize the control if supported</summary>
        public bool AutoSize { get { return autosize; } set { if (autosize != value) { autosize = value; ParentInvalidateLayout(); } } }

        // toggle controls
        /// <summary> Allow reporting and control of enabled state of control</summary>
        public bool Enabled { get { return enabled; } set { if (enabled != value) { SetEnabled(value); Invalidate(); } } }
        /// <summary> Allow reporting and control of visibility of control</summary>
        public bool Visible { get { return visible; } set { if (visible != value) { visible = value; ParentInvalidateLayout(); } } }

        /// <summary> For top level controls only, its Opacity</summary>
        public float Opacity { get { return opacity; } set { if (value != Opacity) { opacity = value; TopLevelControlUpdate = true; FindDisplay()?.ReRender(); } } }

        // Focus
        /// <summary> Is control focused? </summary>
        public virtual bool Focused { get { return focused; } }
        /// <summary> Is the control allowed to be focused? If set, it can get focus. if clear, clicking on it sets focus to null</summary>
        public virtual bool Focusable { get { return focusable; } set { focusable = value; } }
        /// <summary> If set, focus is never given to the control or the focus changed from current by clicking on it </summary>
        public virtual bool RejectFocus { get { return rejectfocus; } set { rejectfocus = value; } }
        /// <summary> If set, focus is passed to parent if present if it does not reject it</summary>
        public virtual bool GiveFocusToParent { get { return givefocustoparent; } set { givefocustoparent = value; } }    // 
        /// <summary> Set focus to this control </summary>
        public virtual bool SetFocus() { return FindDisplay()?.SetFocus(this) ?? false; }

        // colour font

        /// <summary> Font to draw text in </summary>
        public Font Font { get { return font ?? parent?.Font ?? DefaultFont; } set { SetFont(value); InvalidateLayout(); } }    // look back up tree
        /// <summary> Is font defined at this level, or inherited </summary>
        public bool IsFontDefined { get { return font != null; } }
        /// <summary> Back color of control </summary>
        public Color BackColor { get { return backcolor; } set { if (backcolor != value) { backcolor = value; Invalidate(); } } }
        /// <summary> Back color gradient direction </summary>
        public int BackColorGradientDir { get { return backcolorgradientdir; } set { if (backcolorgradientdir != value) { backcolorgradientdir = value; Invalidate(); } } }
        /// <summary> Back color alternate colour when gradient is on </summary>
        public Color BackColorGradientAlt { get { return backcolorgradientalt; } set { if (backcolorgradientalt != value) { backcolorgradientalt = value; Invalidate(); } } }

        // heirarchy
        /// <summary> Parent of control. </summary>
        public GLBaseControl Parent { get { return parent; } }
        /// <summary> Owner of control, if Parent is not the owner.  Normally null
        /// Used for disconnected popups such as list boxes which are placed on the display control. 
        /// taken into consideration in IsThisOrChildOf
        /// </summary>
        public GLBaseControl Owner { get; set; } = null;
        /// <summary> Find the top level display control. </summary>
        public GLControlDisplay FindDisplay() { return this is GLControlDisplay ? this as GLControlDisplay : parent?.FindDisplay(); }
        /// <summary> Find the control under the top level display control which this control belongs to. Null if can't find </summary>
        public GLBaseControl FindControlUnderDisplay() { return Parent is GLControlDisplay ? this : parent?.FindControlUnderDisplay(); }
        /// <summary> Find a form, if the control is part of a form. Null if not </summary>
        public GLForm FindForm() { return this is GLForm ? this as GLForm : parent?.FindForm(); }

        // list of attached animators.
        /// <summary> List of animators for this control </summary>
        public List<IGLControlAnimation> Animators { get; set; } = new List<IGLControlAnimation>();

        // tooltips
        /// <summary> Tool tip text to display for this control </summary>
        public string ToolTipText { get; set; } = null;

        // Is Container 
        /// <summary> Is this a container of other controls (Panels/GroupBoxes etc) </summary>
        public virtual bool IsContainer { get; } = false;

        // Table layout
        /// <summary> For table layouts, this is the row the control is placed in </summary>
        public int Row { get { return row; } set { row = value; ParentInvalidateLayout(); } }
        /// <summary> For table layouts, this is the column the control is placed in </summary>
        public int Column { get { return column; } set { column = value; ParentInvalidateLayout(); } }

        // Flow layout
        /// <summary> For flow layouts, this allows an offset from the chosen flow position to be set. </summary>
        public Point FlowOffsetPosition { get; set; } = Point.Empty;

        // Auto Invalidate
        /// <summary> Invalid control on mouse enter or leave </summary>
        public bool InvalidateOnEnterLeave { get; set; } = false;
        /// <summary> Invalid control on mouse move </summary>
        public bool InvalidateOnMouseMove { get; set; } = false;        
        /// <summary> Invalid control on mouse down or up </summary>
        public bool InvalidateOnMouseDownUp { get; set; } = false;      
        /// <summary> Invalid control on focus gained or lost </summary>
        public bool InvalidateOnFocusChange { get; set; } = false;      

        // State for use during drawing
        /// <summary> Is control currently being hovered over? </summary>
        public bool Hover { get; set; } = false;                      
        /// <summary> When control is hovered over, this is the mouse button state </summary>
        public GLMouseEventArgs.MouseButtons MouseButtonsDown { get; set; } 

        // Bitmap
        /// <summary> Level bitmap. Only for scroll panels and top level controls. For information only. </summary>
        public Bitmap LevelBitmap { get { return levelbmp; } }  

        /// <summary> The scroll offset for scrollable panels</summary>
        public Point ScrollOffset { get { return scrolloffset; } protected set { scrolloffset = value;  } }

        // User properties
        /// <summary> User tag data for control</summary>
        public Object Tag { get; set; }        

        // Tabs
        /// <summary> Tab order number, lowest wins the form focus</summary>
        public int TabOrder { get; set; } = -1;                 

        // Order control
        /// <summary> TopMost, set to force window to top and stay there </summary>
        public bool TopMost { get { return topMost; } set { topMost = value; if (topMost) BringToFront(); } } // set to force top most

        /// <summary> Control themeing of components via the global GLBaseControl.Themer callback </summary>
        public enum ThemeControl {
            /// <summary> Themeing is off for itself and all children </summary>
            Off,
            /// <summary> Themeing is on for itself, children can decide if they want theming</summary>
            On,
            /// <summary> Themeing is off for itself, children can decide if they want theming</summary>
            OffItselfOnly,
        }
        /// <summary> Allow theming of control using the static GLBaseControl.Themer callback </summary>
        public ThemeControl ThemerControl { get; set; } = ThemeControl.On;
        /// <summary> Allow a bool to turn themer on or off</summary>
        public bool EnableThemer { get { return ThemerControl == ThemeControl.On; } set { ThemerControl = value ? ThemeControl.On : ThemeControl.Off; } } 
        /// <summary> Does the control and all its parents allow themeing? </summary>
        public bool IsThemeingAllowed { get { if (ThemerControl == ThemeControl.Off) return false; else if (Parent == null) return ThemerControl == ThemeControl.On; else return Parent.IsThemeingAllowed; } }

        /// <summary> Cursor shape which is displayed when hovering over this control </summary>
        public GLWindowControl.GLCursorType Cursor { get { return cursor; } set { if (value != cursor) { cursor = value; FindDisplay()?.Gc_CursorTo(this, value); } } }

        // children control list

        /// <summary> The control list in inverse Z order. Read only. First is the most background window, last is the foreground window. </summary>
        public virtual IList<GLBaseControl> ControlsIZ { get { return childreniz.AsReadOnly(); } }
        /// <summary> The control list in Z order. Read only. First is the most foreground window, last is the background window. </summary>
        public virtual IList<GLBaseControl> ControlsZ { get { return childrenz.AsReadOnly(); } }    
        /// <summary> Find control by name. Null if not found</summary>
        public GLBaseControl this[string s] { get { return childrenz.Find((x)=>x.Name == s); } }

        // events

        /// <summary> Mouse down action. Passed control and mouse events arguments. </summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseDown { get; set; } = null;
        /// <summary> Mouse up action. Passed control and mouse events arguments. </summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseUp { get; set; } = null;
        /// <summary> Mouse move action. Passed control and mouse events arguments.  </summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseMove { get; set; } = null;
        /// <summary> Mouse mouse click action. Passed control and mouse events arguments.  </summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseClick { get; set; } = null;
        /// <summary> Mouse double click action. Passed control and mouse events arguments. </summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseDoubleClick { get; set; } = null;
        /// <summary> Mouse wheel click action. Passed control and mouse events arguments. </summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseWheel { get; set; } = null;
        /// <summary> Mouse enter action. Passed control and mouse events arguments. </summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseEnter { get; set; } = null;
        /// <summary> Mouse leave action. Passed control and mouse events arguments</summary>
        public Action<GLBaseControl, GLMouseEventArgs> MouseLeave { get; set; } = null;
        /// <summary> Key down action. Passed control and key events arguments</summary>
        public Action<GLBaseControl, GLKeyEventArgs> KeyDown { get; set; } = null;
        /// <summary> Key up action. Passed control and key events arguments</summary>
        public Action<GLBaseControl, GLKeyEventArgs> KeyUp { get; set; } = null;
        /// <summary> Key press action. Passed control and key events arguments</summary>
        public Action<GLBaseControl, GLKeyEventArgs> KeyPress { get; set; } = null;
        
        /// <summary> Focus event type </summary>
        public enum FocusEvent {
            /// <summary> Control has focused</summary>
            Focused,   // 
            /// <summary> Control has deactivated </summary>
            Deactivated,   // 
            /// <summary> Child has focused </summary>
            ChildFocused,   // 
            /// <summary> Child has deactivated </summary>
            ChildDeactivated
        };
        /// <summary>Focus of control or child of control has changed </summary>
        public Action<GLBaseControl, FocusEvent, GLBaseControl> FocusChanged { get; set; } = null;     
        /// <summary>Font has changed</summary>
        public Action<GLBaseControl> FontChanged { get; set; } = null;
        /// <summary>Control has changed size </summary>
        public Action<GLBaseControl> Resize { get; set; } = null;
        /// <summary>Control has moved </summary>
        public Action<GLBaseControl> Moved { get; set; } = null;

        /// <summary> Control has been added. 
        /// Called first on parent with Para1=parent, Para2=child added.  
        /// Called second on child with Para1=parent, Para2=child
        /// </summary>
        public Action<GLBaseControl, GLBaseControl> ControlAdd { get; set; } = null;
        /// <summary> </summary>
        /// <summary> Control has been removed
        /// Called first on child with Para1=parent, Para2=child
        /// Called second on parent with Para1=parent, Para2=child added.  
        /// </summary>
        public Action<GLBaseControl, GLBaseControl> ControlRemove { get; set; } = null;

        // globals
        /// <summary>Called whenever the focus changed, Para1=from, Para2=to. Either may be null.
        /// Only valid to hook on GLControlDisplay</summary>
        public Action<GLBaseControl, GLBaseControl> GlobalFocusChanged { get; set; } = null;
        /// <summary>Called when an control has been clicked, indicating control and mouse arguments
        /// Only valid to hook on GLControlDisplay</summary>
        public Action<GLBaseControl, GLMouseEventArgs> GlobalMouseClick { get; set; } = null;
        /// <summary>Called when an control has had mouse down, indicating control and mouse arguments.  If not over a control, GLBaseControl is null
        /// Only valid to hook on GLControlDisplay</summary>
        public Action<GLBaseControl, GLMouseEventArgs> GlobalMouseDown { get; set; } = null;
        /// <summary>Called when the mouse is moved. Mouse arguments give control its over and location
        /// Only valid to hook on GLControlDisplay</summary>
        public Action<GLMouseEventArgs> GlobalMouseMove { get; set; }

        // global Thermer
        /// <summary>Hook to theme controls as they are added</summary>
        public static Action<GLBaseControl> Themer = null;
        // global No Thermer
        /// <summary>For debug purposes, Hook to show which controls were not themed on add </summary>
        public static Action<GLBaseControl> NoThemer = null;

        /// <summary> Return if this control is us or a child of us.
        /// or if the control is owned by us or one of its parents is owned by us</summary>
        public virtual bool IsThisOrChildOf(GLBaseControl ctrl)         
        {
            //System.Diagnostics.Debug.WriteLine($"Checking {this.Name} For ownership");

            if (ctrl == this)       // if control is this node, its okay
            {
              //  System.Diagnostics.Debug.WriteLine($"Ctrl {ctrl.Name} is child");
                return true;
            }

            if (ctrl.Owner == this) // owner is a child of the tree, its okay
            {
              //  System.Diagnostics.Debug.WriteLine($"Ctrl {ctrl.Name} is directly owned by {this.Name}");
                return true;
            }

            // if we have parents, see if any of them are attached to us via Owner. We need to go up the tree (ie. scrollbar->listbox->owner->us)

            if (ctrl.Parent != null)       
            {
                var node = ctrl.Parent;
                while (node != null)
                {
                    if (node.Owner == this)
                    {
                       // System.Diagnostics.Debug.WriteLine($"Ctrl {ctrl.Name} is directly owned by {this.Name}");
                        return true;
                    }
                    node = node.Parent;
                }
            }

            foreach (var c in childrenz)        // down down tree and see if children have a relationship with ctrl
            {
                if (c.IsThisOrChildOf(ctrl))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary> Return if this control or a child is focused </summary>
        public virtual bool IsThisOrChildrenFocused()
        {
            if (Focused)
                return true;
            foreach (var c in childrenz)
            {
                if (c.IsThisOrChildrenFocused())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Find next tab, either forward or backwards, from this tab number. For certain panels, go into it and find a tab</summary>
        /// <param name="tabno">current tab number, or -1 for find first</param>
        /// <param name="forward">true to go forward, else backwards</param>
        /// <returns></returns>
        public virtual GLBaseControl FindNextTabChild(int tabno, bool forward = true)
        {
            return FindNextTabChild(tabno, int.MaxValue, forward).Item1;
        }

        /// <summary> Return area needed for visible children, with an optional test to reject children from the test</summary>
        public Rectangle VisibleChildArea(Predicate<GLBaseControl> test = null)        
        {
            int left = int.MaxValue, right = int.MinValue, top = int.MaxValue, bottom = int.MinValue;

            foreach (var c in childrenz)
            {
                if (c.visible && (test == null || test(c)))     // must be visible to be part of area, and pass the test
                {
                    if (c.Left < left)
                        left = c.Left;
                    if (c.Right > right)
                        right = c.Right;
                    if (c.Top < top)
                        top = c.Top;
                    if (c.Bottom > bottom)
                        bottom = c.Bottom;
                }
            }

            return left == int.MaxValue ? Rectangle.Empty : new Rectangle(left, top, right - left, bottom - top);
        }

        /// <summary> Invalidate control and repaint.  Children will also repaint. </summary>
        public virtual void Invalidate()    // overridden in controldisplay
        {
            //System.Diagnostics.Debug.WriteLine("Invalidate " + Name);
            needredraw = true;

            if (BackColor == Color.Transparent)   // if we are transparent, we need the parent also to redraw to force it to redraw its background.
            {
                //System.Diagnostics.Debug.WriteLine("Invalidate " + Name + " is transparent, parent needs it too");
                Parent?.Invalidate();
            }

            FindDisplay()?.ReRender(); // and we need to tell the display to redraw
        }

        /// <summary> Invalidate and relayout</summary>
        public void InvalidateLayout()
        {
            InvalidateLayout(this);
        }

        /// <summary> Invalidate and layout the parent, and therefore this control (since it the parent invalidates, all children get redrawn)</summary>
        public void ParentInvalidateLayout()
        {
            parent?.InvalidateLayout(this);
        }

        /// <summary> Perform layout on all children, consisting first of sizing, then of laying out with their sizes</summary>
        public void PerformLayout()
        {
            if (suspendLayoutCount > 0)
            {
                needLayout = true;
                //System.Diagnostics.Debug.WriteLine("Suspended layout on " + Name);
            }
            else
            {
                Size us = Size;

                if (Parent != null)
                    SizeControl(Parent.ClientSize);    // size us

                PerformRecursiveSize();         // size all children

                if (Parent != null)             // and size us post children
                {
                    SizeControlPostChild(Parent.ClientSize);    // And we give ourselves a change to post size to children size

                    if (us != Size)           // if we changed size.. we need to go and let the parent have a full go, since it needs to be layed out
                    {
                        ParentInvalidateLayout();       // Must call this, as its overriden in control display and causes a refresh of the bitmap/texture.
                        return;
                    }
                }

                PerformRecursiveLayout();       // and we layout, recursively
            }
        }

        /// <summary> Call to halt layout. On creation, layout is suspended on the new control.  On Add, layout count is reset to zero and layout can occur</summary>
        public void SuspendLayout()
        {
            suspendLayoutCount++;
            //System.Diagnostics.Debug.WriteLine("Suspend layout on " + Name);
        }

        /// <summary> Return if layout is suspended </summary>
        public bool LayoutSuspended { get { return suspendLayoutCount > 0; } }

        /// <summary> Resume layout. Will perform an immediate layout if required</summary>
        public void ResumeLayout()
        {
            //if ( suspendLayoutSet ) System.Diagnostics.Debug.WriteLine("Resume Layout on " + Name);

            if (suspendLayoutCount == 0 || --suspendLayoutCount == 0)       // if at 0, or counts to 0
            {
                if (needLayout)
                {
                    PerformLayout();
                }
            }
        }

        /// <summary>
        /// Attach control to desktop
        /// </summary>
        /// <param name="child">Control</param>
        /// <param name="atback">False, at front of Z order, True, at back</param>
        /// <returns>True if added</returns>
        public virtual bool AddToDesktop(GLBaseControl child, bool atback = false)
        {
            var f = FindDisplay();
            if (f != null)
            {
                f.Add(child, atback);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Attach control as child of control
        /// </summary>
        /// <param name="child">Child control</param>
        /// <param name="atback">False, at front of Z order, True, at back</param>
        public virtual void Add(GLBaseControl child, bool atback = false)
        {
            System.Diagnostics.Debug.Assert(!childrenz.Contains(child));        // no repeats
            child.parent = this;
            child.suspendLayoutCount = 0;           // we unsuspend - controls are created suspended

            child.ClearFlagsDown();       // in case of reuse, clear all temp flags as child is added

            if (atback)
            {
                childrenz.Add(child);
                childreniz.Insert(0, child);
            }
            else
            {
                int ipos = 0;
                if (!child.TopMost)     // add at end of top list.
                {
                    while (ipos < childrenz.Count && childrenz[ipos].TopMost)     // find first place we can insert
                        ipos++;
                }

                childrenz.Insert(ipos, child);   // in z order.  First is top of z.  insert puts it before existing
                childreniz.Insert(childreniz.Count - ipos, child);       // in inv z order. Last is top of z.  if ipos=0, at end. if ipos=1, inserted just before end
            }

            CheckZOrder();      // verify its okay 

            // if child and parent and optional owner have theme enabled, theme child

            if ( child.IsThemeingAllowed && (child.Owner?.EnableThemer ?? true))
            {
                Themer?.Invoke(child);      // global themer
            }
            else
            {
                NoThemer?.Invoke(child);    // indicate not themeing
                //System.Diagnostics.Debug.WriteLine($"Child {child?.Name} not themed parent {this.Name} owner {this.Owner?.Name}");
            }

            OnControlAdd(this, child);
            child.OnControlAdd(this, child);
            InvalidateLayout(child);        // we are invalidated and layout due to this child
        }

        /// <summary>
        /// Add with tab order set
        /// </summary>
        /// <param name="child">Child control</param>
        /// <param name="tabno">ref to hold tab number</param>
        public virtual void Add(GLBaseControl child, ref int tabno)
        {
            Add(child);
            child.TabOrder = tabno++;
        }

        /// <summary>
        /// Add a range of items to this control as children
        /// </summary>
        /// <param name="list">enumeration of controls to add</param>
        public virtual void AddItems(IEnumerable<GLBaseControl> list)
        {
            SuspendLayout();
            foreach (var i in list)
                Add(i);
            ResumeLayout();
        }

        /// <summary> Remove child control from parent, and disposes of it and all its children. Control is now not reattachable.</summary>
        public static void Remove(GLBaseControl child)
        {
            if (child.Parent != null) // if attached
            {
                GLBaseControl parent = child.Parent;
                parent.RemoveControl(child, true, true);
                parent.InvalidateLayout(null);          // invalidate parent, and indicate null so it knows the child has been removed
                child.needredraw = true;                // next time, it will need to be drawn if reused
            }
        }

        /// <summary> Remove these child control from parent, and disposes of it and all its children. Control is now not reattachable. use Predicate if you want to select</summary>
        public void Remove(Predicate<GLBaseControl> removeit = null)
        {
            var list = new List<GLBaseControl>(childrenz);
            foreach (var control in list)
            {
                if (removeit == null || removeit(control))
                {
                    Remove(control);
                }
            }
        }

        /// <summary> Remove child control from parent, but do not dispose of it or its children, and maintain all child relationship to it. Control can be reattached</summary>
        public static void Detach(GLBaseControl child)    
        {
            if (child.Parent != null) // if attached
            {
                GLBaseControl parent = child.Parent;
                parent.RemoveControl(child, false, false);
                parent.InvalidateLayout(null);
                child.needredraw = true;        // next time, it will need to be drawn
            }
        }

        /// <summary> Bring control to the front of the Z order. True if already at front. </summary>
        public virtual bool BringToFront()      
        {
            return Parent?.BringToFront(this) ?? true;
        }

        /// <summary> Bring child to the front of its parent Z order. True if already at front. </summary>
        public virtual bool BringToFront(GLBaseControl child)   
        {
            //System.Diagnostics.Debug.WriteLine("Bring to front" + child.Name);
            int curpos = childrenz.IndexOf(child);

            if (curpos>=0)
            { 
                int ipos = 0;

                if ( !child.TopMost )
                {
                    while (ipos < childrenz.Count && childrenz[ipos].TopMost)     // find first place we can move to
                        ipos++;
                }

                if ( curpos != ipos )       // if not in first position possible
                {
                    childrenz.Remove(child);
                    childreniz.Remove(child);
                                            // list now has child removed, now insert back into position
                    childrenz.Insert(ipos, child);   // in z order.  First is top of z
                    childreniz.Insert(childreniz.Count-ipos,child);       // in inv z order. Last is top of z

                    CheckZOrder();

                    InvalidateLayout(child);
                    return false;
                }
            }

            return true;
        }

        /// <summary> Apply action to all child controls of this name. 
        /// Wildcards ? * can be used. Action can close the control if required.
        /// </summary>
        public void ApplyToControlOfName(string wildcardname,Action<GLBaseControl> act, bool recurse = false)
        {
            if (recurse)
            {
                foreach (var c in childrenz)
                {
                    c.ApplyToControlOfName(wildcardname, act, recurse);
                }
            }

            List<GLBaseControl> list = childrenz.Where(x => x.Name.WildCardMatch(wildcardname)).ToList();
            foreach (var c in list)
                act(c);
        }

        /// <summary>
        /// Find control over Point
        /// </summary>
        /// <param name="coords">Screen co-ordinates</param>
        /// <param name="offset">Return offset of point within bounds of the control</param>
        /// <returns>Control or null if not over any control</returns>
        public GLBaseControl FindControlOver(Point coords, out Point offset)
        {
            Size sz = ScaledSize;       // get visual size shown to user

            if (coords.X < Left || coords.X >= Left+sz.Width || coords.Y < Top || coords.Y >= Top+sz.Height)       // if outside our bounds, not found
            {
                offset = Point.Empty;
                return null;
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine($"Find {Name} {coords} {ScaleWindow} {sz}");

                coords = new Point(coords.X - Left + ScrollOffset.X, coords.Y - Top + ScrollOffset.Y);            // coords translated to inside the bounds of this control
                //System.Diagnostics.Debug.WriteLine($"-> {coords} ");
                
                if ( ScaleWindow != null )      // we need to match the offset above, in screen pixels, to the internal scale. / because if the ScaleWindow <1, it means the internal scale is bigger than the visual one
                {
                    coords = new Point((int)(coords.X / ScaleWindow.Value.Width), (int)(coords.Y / ScaleWindow.Value.Height));
                    //System.Diagnostics.Debug.WriteLine($"-> {coords} ");
                }
            }

            foreach (GLBaseControl c in childrenz)       // in Z order
            {
                if (c.Visible)      // must be visible to be found..
                {
                    // convert bounds co-ords to client coords by removing client margin, and check

                    var r = c.FindControlOver(new Point(coords.X-ClientLeftMargin,coords.Y-ClientTopMargin), out offset);   
                    if (r != null)
                        return r;
                }
            }

            offset = coords;        // no children, so return bounds offset
            return this;
        }

        /// <summary>
        /// Find screen co-ordinates of control
        /// </summary>
        /// <param name="clientpos">Offset is bounds if false, client if true</param>
        /// <returns>Screen co-ordinate</returns>

        public Point FindScreenCoords(bool clientpos = false)
        {
            return FindScreenCoords(Point.Empty, clientpos);
        }

        /// <summary>
        /// Find screen co-ordinates of control
        /// </summary>
        /// <param name="offset">Offset in control, either bounds or client, dependent on clientpos</param>
        /// <param name="clientpos">Offset is bounds if false, client if true</param>
        /// <returns>Screen co-ordinate</returns>

        public Point FindScreenCoords(Point offset, bool clientpos = false)
        {
            // given a point x in control relative to bounds, in bitmap space (so not scaled), what is its screen coords

            if ( clientpos )
            {
                offset.X += ClientLeftMargin;
                offset.Y += ClientTopMargin;
            }

            PointF p = offset;

            GLBaseControl c = this;

            while (c != null)
            {
                if (c.ScaleWindow != null)
                {
                    p.X *= c.ScaleWindow.Value.Width;         // scale down the X/Y offsets by the window scale to get visual scale
                    p.Y *= c.ScaleWindow.Value.Height;
                }

                p.X += c.Left;
                p.Y += c.Top;

                c = c.Parent;

                if ( c != null )
                {
                    p.X += c.ClientLeftMargin;      // these will be scaled on the above scalar when it loops around
                    p.Y += c.ClientTopMargin;
                }

                //System.Diagnostics.Debug.WriteLine($" -> {p} ");
            }

            return new Point((int)p.X, (int)p.Y);
        }

        /// <summary> Find scale between this control and the desktop</summary>
        public SizeF FindScaler()
        {
            SizeF scale = new SizeF(1, 1);
            GLBaseControl p = this;
            while (p != null)
            {
                if (p.ScaleWindow != null)
                {
                    scale = new SizeF(scale.Width * p.ScaleWindow.Value.Width, scale.Height * p.ScaleWindow.Value.Height);
                }
                p = p.Parent;
            }
            return scale;
        }

        #endregion

     }
}
