/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
    [System.Diagnostics.DebuggerDisplay("Control {Name} {window}")]
    public abstract partial class GLBaseControl : IDisposable
    {
        #region Main UI
        public string Name { get; set; } = "?";

        // bounds of the window - include all margin/padding/borders/
        // co-ords are in offsets from 0,0 being the parent top left corner. See also Set()

        public Rectangle Bounds { get { return window; } set { SetPos(value.Left, value.Top, value.Width, value.Height); } } 
        public int Left { get { return window.Left; } set { SetPos(value, window.Top, window.Width, window.Height); } }
        public int Right { get { return window.Right; } set { SetPos(window.Left, window.Top, value - window.Left, window.Height); } }
        public int Top { get { return window.Top; } set { SetPos(window.Left, value, window.Width, window.Height); } }
        public int Bottom { get { return window.Bottom; } set { SetPos(window.Left, window.Top, window.Width, value - window.Top); } }
        public int Width { get { return window.Width; } set { SetPos(window.Left, window.Top, value, window.Height); } }
        public int Height { get { return window.Height; } set { SetPos(window.Left, window.Top, window.Width, value); } }
        public Point Location { get { return new Point(window.Left, window.Top); } set { SetPos(value.X, value.Y, window.Width, window.Height); } }
        public Size Size { get { return new Size(window.Width, window.Height); } set { SetPos(window.Left, window.Top, value.Width, value.Height); } }

        public Size MinimumSize { get { return minimumsize; } set { if (value != minimumsize) { minimumsize = value; SetPos(window.Left, window.Top, window.Width, window.Height); } } }
        public Size MaximumSize { get { return maximumsize; } set { if (value != maximumsize) { maximumsize = value; SetPos(window.Left, window.Top, window.Width, window.Height); } } }

        // only for top level windows at the moment, we can throw them on the screen scaled..  <1 smaller, >1 bigger
        public SizeF? ScaleWindow { get { return altscale; } set { altscale = value; TopLevelControlUpdate = true; FindDisplay()?.ReRender(); } }
        public Size ScaledSize { get { if (altscale != null) return new Size((int)(Width * ScaleWindow.Value.Width), (int)(Height * ScaleWindow.Value.Height)); else return Size; } }

        // padding/margin and border control (Do not apply to display control)
        public Padding Padding { get { return padding; } set { if (padding != value) { padding = value; CalcClientRectangle(); InvalidateLayout(); } } }
        public Margin Margin { get { return margin; } set { if (margin != value) { margin = value; CalcClientRectangle(); InvalidateLayout(); } } }
        public void SetMarginBorderWidth(Margin m, int borderw, Color borderc, Padding p) { margin = m; padding = p; bordercolor = borderc; borderwidth = borderw; CalcClientRectangle(); InvalidateLayout(); }
        public Color BorderColor { get { return bordercolor; } set { if (bordercolor != value) { bordercolor = value; Invalidate(); } } }
        public int BorderWidth { get { return borderwidth; } set { if (borderwidth != value) { borderwidth = value; CalcClientRectangle(); InvalidateLayout(); } } }

        // this is the client area, inside the margin/padding/border
        public int ClientLeftMargin { get { return Margin.Left + Padding.Left + BorderWidth; } }
        public int ClientRightMargin { get { return Margin.Right + Padding.Right + BorderWidth; } }
        public int ClientWidthMargin { get { return Margin.TotalWidth + Padding.TotalWidth + BorderWidth * 2; } }
        public int ClientTopMargin { get { return Margin.Top + Padding.Top + BorderWidth; } }
        public int ClientBottomMargin { get { return Margin.Bottom + Padding.Bottom + BorderWidth; } }
        public int ClientHeightMargin { get { return Margin.TotalHeight + Padding.TotalHeight + BorderWidth * 2; } }
        public int ClientWidth { get { return ClientRectangle.Width; } set { SetPos(window.Left, window.Top, value + ClientLeftMargin + ClientRightMargin, window.Height); } }
        public int ClientHeight { get { return ClientRectangle.Height; } set { SetPos(window.Left, window.Top, window.Width, value + ClientTopMargin + ClientBottomMargin); } }
        public Size ClientSize { get { return ClientRectangle.Size; } set { SetPos(window.Left, window.Top, value.Width + ClientLeftMargin + ClientRightMargin, value.Height + ClientTopMargin + ClientBottomMargin); } }
        public Point ClientLocation { get { return new Point(ClientLeftMargin, ClientTopMargin); } }
        public Rectangle ClientRectangle { get; private set; }

        // docking control 
        public DockingType Dock { get { return docktype; } set { if (docktype != value) { docktype = value; ParentInvalidateLayout(); } } }
        // applies to all Left,Right,Bottom,Top dockings
        public Margin DockingMargin { get { return dockingmargin; } set { if (dockingmargin != value) { dockingmargin = value; InvalidateLayout(); } } }
        // applies to all Left,Right,Bottom,Top dockings, if >0 , sets the percentage of the parent area to set width/height to (so for Bottom/Top, the height, for Left/Right, the Width)
        public float DockPercent { get { return dockpercent; } set { if (value != dockpercent) { dockpercent = value; ParentInvalidateLayout(); } } }        // % in 0-1 terms used to dock on left,top,right,bottom.  0 means just use width/height

        public AnchorType Anchor { get { return anchortype; } set { if (value != anchortype) { anchortype = value; ParentInvalidateLayout(); } } }
        
        // Autosize
        public bool AutoSize { get { return autosize; } set { if (autosize != value) { autosize = value; ParentInvalidateLayout(); } } }

        // toggle controls
        public bool Enabled { get { return enabled; } set { if (enabled != value) { SetEnabled(value); Invalidate(); } } }
        public bool Visible { get { return visible; } set { if (visible != value) { visible = value; ParentInvalidateLayout(); } } }

        // Top level windows only, control Opacity
        public float Opacity { get { return opacity; } set { if (value != Opacity) { opacity = value; TopLevelControlUpdate = true; FindDisplay()?.ReRender(); } } }

        // Focus
        public virtual bool Focused { get { return focused; } }
        public virtual bool Focusable { get { return focusable; } set { focusable = value; } }          // if set, it can get focus. if clear, clicking on it sets focus to null
        public virtual bool RejectFocus { get { return rejectfocus; } set { rejectfocus = value; } }    // if set, focus is never given or changed by clicking on it.
        public virtual bool GiveFocusToParent { get { return givefocustoparent; } set { givefocustoparent = value; } }    // if set, focus is passed to parent if present, and it does not reject it
        public virtual bool SetFocus() { return FindDisplay()?.SetFocus(this) ?? false; }

        // colour font
        public Font Font { get { return font ?? parent?.Font ?? DefaultFont; } set { SetFont(value); InvalidateLayout(); } }    // look back up tree
        public bool IsFontDefined { get { return font != null; } }
        public Color BackColor { get { return backcolor; } set { if (backcolor != value) { backcolor = value; Invalidate(); } } }
        public int BackColorGradientDir { get { return backcolorgradientdir; } set { if (backcolorgradientdir != value) { backcolorgradientdir = value; Invalidate(); } } }
        public Color BackColorGradientAlt { get { return backcolorgradientalt; } set { if (backcolorgradientalt != value) { backcolorgradientalt = value; Invalidate(); } } }

        // heirarchy
        public GLBaseControl Parent { get { return parent; } }
        public GLControlDisplay FindDisplay() { return this is GLControlDisplay ? this as GLControlDisplay : parent?.FindDisplay(); }
        public GLBaseControl FindControlUnderDisplay() { return Parent is GLControlDisplay ? this : parent?.FindControlUnderDisplay(); }
        public GLForm FindForm() { return this is GLForm ? this as GLForm : parent?.FindForm(); }

        // list of attached animators.
        public List<IControlAnimation> Animators { get; set; } = new List<IControlAnimation>();

        // tooltips
        public string ToolTipText { get; set; } = null;

        // Table layout
        public int Row { get { return row; } set { row = value; ParentInvalidateLayout(); } }       // for table layouts
        public int Column { get { return column; } set { column = value; ParentInvalidateLayout(); } } // for table layouts

        // Flow layout
        public Point FlowOffsetPosition { get; set; } = Point.Empty;        // optionally offset this control from its flow position by this value

        // Auto Invalidate
        public bool InvalidateOnEnterLeave { get; set; } = false;       // if set, invalidate on enter/leave to force a redraw
        public bool InvalidateOnMouseMove { get; set; } = false;        // if set, invalidate on mouse move in control
        public bool InvalidateOnMouseDownUp { get; set; } = false;      // if set, invalidate on mouse button down/up to force a redraw
        public bool InvalidateOnFocusChange { get; set; } = false;      // if set, invalidate on focus change

        // State for use during drawing
        public bool Hover { get; set; } = false;                        // mouse is over control
        public GLMouseEventArgs.MouseButtons MouseButtonsDown { get; set; } // set if mouse buttons down over control

        // Bitmap
        public Bitmap LevelBitmap { get { return levelbmp; } }  // return level bitmap, null if does not have a level bitmap 

        // if has a bitmap, its scroll offset.  Only derived classes can set this
        public Point ScrollOffset { get { return scrolloffset; } protected set { scrolloffset = value;  } }

        // User properties
        public Object Tag { get; set; }                         // control tag, user controlled

        // Tabs
        public int TabOrder { get; set; } = -1;                 // set, the lowest tab order wins the form focus

        // Order control
        public bool TopMost { get { return topMost; } set { topMost = value; if (topMost) BringToFront(); } } // set to force top most

        // Global themer enable - applied at Add. Do we apply it to this control?
        public bool EnableThemer { get; set; } = true;

        // Cursor shape
        public GLCursorType Cursor { get { return cursor; } set { if (value != cursor) { cursor = value; FindDisplay()?.Gc_CursorTo(this, value); } } }


        // children control list

        public virtual IList<GLBaseControl> ControlsIZ { get { return childreniz.AsReadOnly(); } }      // read only, in inv zorder, so 0 = last layout first drawn
        public virtual IList<GLBaseControl> ControlsZ { get { return childrenz.AsReadOnly(); } }        // read only, in zorder, so 0 = first layout last painted
        public GLBaseControl this[string s] { get { return childrenz.Find((x)=>x.Name == s); } }    // null if not

        // events

        public Action<Object, GLMouseEventArgs> MouseDown { get; set; } = null;  // location in client terms, NonClientArea set if on border with negative/too big x/y for clients
        public Action<Object, GLMouseEventArgs> MouseUp { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseMove { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseClick { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseDoubleClick { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseWheel { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseEnter { get; set; } = null;  // location in terms of whole window
        public Action<Object, GLMouseEventArgs> MouseLeave { get; set; } = null;  // location in terms of whole window
        public Action<Object, GLKeyEventArgs> KeyDown { get; set; } = null;
        public Action<Object, GLKeyEventArgs> KeyUp { get; set; } = null;
        public Action<Object, GLKeyEventArgs> KeyPress { get; set; } = null;
        public enum FocusEvent { Focused,   // OnFocusChange - you get the old focus
                                Deactive,   // you get the new one focused
                                ChildFocused,   // you get the new one focused
                                ChildDeactive }; // you get the new one focused
        public Action<Object, FocusEvent, GLBaseControl> FocusChanged { get; set; } = null;     // send to control gaining/losing focus, and to its parents
        public Action<Object> FontChanged { get; set; } = null;
        public Action<Object> Resize { get; set; } = null;
        public Action<Object> Moved { get; set; } = null;
        public Action<GLBaseControl, GLBaseControl> ControlAdd { get; set; } = null;
        public Action<GLBaseControl, GLBaseControl> ControlRemove { get; set; } = null;
        // globals
        public Action<GLBaseControl, GLBaseControl> GlobalFocusChanged { get; set; } = null;        // sent to all controls on a focus change. Either may be null
        public Action<GLBaseControl, GLMouseEventArgs> GlobalMouseClick { get; set; } = null;       // sent to all controls on a click
        public Action<GLBaseControl, GLMouseEventArgs> GlobalMouseDown { get; set; } = null;       // sent to all controls on a click. GLBaseControl may be null
        public Action<GLMouseEventArgs> GlobalMouseMove { get; set; }       // only hook on GLControlDisplay.  Has all the GLMouseEventArgs fields filled out including control ones

        // global Thermer

        public static Action<GLBaseControl> Themer = null;                 // set this up, will be called when the control is added for you to theme the colours/options

        // is ctrl us, or one of our children?  may want to override to associate other controls as us
        public virtual bool IsThisOrChildOf(GLBaseControl ctrl)         
        {
            if (ctrl == this)
                return true;
            foreach( var c in childrenz)
            {
                if (c.IsThisOrChildOf(ctrl))
                    return true;
            }
            return false;
        }

        // is us or child of us focused
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

        // next tab, from tabno, either forward or back
        public GLBaseControl FindNextTabChild(int tabno, bool forward = true)       
        {
            GLBaseControl found = null;
            int mindist = int.MaxValue;

            foreach (var c in childrenz)
            {
                if (c.Focusable && c.Visible && c.Enabled )
                {
                    int dist = c.TabOrder - tabno;

                    if (forward ? dist > 0 : dist < 0)
                    {
                        dist = Math.Abs(dist);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            found = c;
                        }
                    }
                }
            }

            return found;
        }

        // Area needed for children controls. empty if none
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

        // Invalidate us - our children will also repaint. Overriden in ControlDisplay 
        public virtual void Invalidate()
        {
            //System.Diagnostics.Debug.WriteLine("Invalidate " + Name);
            NeedRedraw = true;

            if (BackColor == Color.Transparent)   // if we are transparent, we need the parent also to redraw to force it to redraw its background.
            {
                //System.Diagnostics.Debug.WriteLine("Invalidate " + Name + " is transparent, parent needs it too");
                Parent?.Invalidate();
            }

            FindDisplay()?.ReRender(); // and we need to tell the display to redraw
        }

        // Invalidate and relayout us
        public void InvalidateLayout()
        {
            InvalidateLayout(this);
        }

        // Invalidate and layout the parent, and therefore us (since it the parent invalidates, all children get redrawn)
        public void ParentInvalidateLayout()
        {
            parent?.InvalidateLayout(this);
        }

        // perform layout on all children, consisting first of sizing, then of laying out with their sizes
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

        // Call to halt layout. On creation, layout is suspended on the new control.  On Add, layout count is reset to zero and layout can occur
        public void SuspendLayout()
        {
            suspendLayoutCount++;
            //System.Diagnostics.Debug.WriteLine("Suspend layout on " + Name);
        }

        public bool LayoutSuspended { get { return suspendLayoutCount > 0; } }
        // call to resume layout
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

        // attach control to desktop
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

        // attach control to us as child.  atback allows insertion in bottom z order
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

            if ( EnableThemer)
                Themer?.Invoke(child);      // global themer

            OnControlAdd(this, child);
            child.OnControlAdd(this, child);
            InvalidateLayout(child);        // we are invalidated and layout due to this child
        }

        // add a list of controls
        public virtual void AddItems(IEnumerable<GLBaseControl> list)
        {
            SuspendLayout();
            foreach (var i in list)
                Add(i);
            ResumeLayout();
        }

        // remove closes down and disposes of the child and all its children
        public static void Remove(GLBaseControl child)    
        {                                                  
            if (child.Parent != null) // if attached
            {
                GLBaseControl parent = child.Parent;
                parent.RemoveControl(child, true, true);
                parent.InvalidateLayout(null);          // invalidate parent, and indicate null so it knows the child has been removed
                child.NeedRedraw = true;                // next time, it will need to be drawn if reused
            }
        }

        // a detach keeps the child and its children alive and connected together, but detached from parent
        public static void Detach(GLBaseControl child)    
        {
            if (child.Parent != null) // if attached
            {
                GLBaseControl parent = child.Parent;
                parent.RemoveControl(child, false, false);
                parent.InvalidateLayout(null);
                child.NeedRedraw = true;        // next time, it will need to be drawn
            }
        }

        // bring to the front, true if it was at the front
        public virtual bool BringToFront()      
        {
            return Parent?.BringToFront(this) ?? true;
        }

        // bring child to front, true if already in front
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

        // Using naming to address controls. wildcards ? *. Will work with closing/removing items since we take a list first
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

        // p = co-coords finds including margin/padding/border area, so inside bounds
        // if control found, return offset within bounds left
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


        // given a point x in control relative to bounds, in bitmap space (so not scaled), what is its screen coords
        public Point FindScreenCoords(Point pin, bool clientpos = false)
        {
            if ( clientpos )
            {
                pin.X += ClientLeftMargin;
                pin.Y += ClientTopMargin;
            }

            PointF p = pin;

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

        // what is the scale between this control and the desktop
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
