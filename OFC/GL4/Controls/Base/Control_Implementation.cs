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

using System;
using System.Collections.Generic;
using System.Drawing;


namespace GLOFC.GL4.Controls
{
    [System.Diagnostics.DebuggerDisplay("Control {Name} {window}")]
    public abstract partial class GLBaseControl : IDisposable
    {
        #region For Inheritors

        /// <summary>
        /// Create the control. Note controls are created with suspended layout on
        /// </summary>
        /// <param name="name">control name</param>
        /// <param name="bounds">bounds. If width/height = 0 then AutoSize is enabled</param>
        protected GLBaseControl(string name, Rectangle bounds)
        {
            this.Name = name;

            if (bounds.Width == 0 || bounds.Height == 0)
            {
                bounds.Width = bounds.Height = 10;  // nominal
                AutoSize = true;
            }

            lastlocation = Point.Empty;
            lastsize = Size.Empty;
            window = bounds;
            suspendLayoutCount = 1;         // we create suspended

            CalcClientRectangle();
        }

        /// <summary>
        /// The default window to apply 
        /// </summary>
        static protected readonly Rectangle DefaultWindowRectangle = new Rectangle(0, 0, 10, 10);

        /// <summary>
        /// Set Border Color
        /// All inheritors should use these NI functions in constructors of inheritors or for Layout/SizeControl overrides. These change without invalidation or layout 
        /// </summary>
        public Color BorderColorNI { set { bordercolor = value; } }
        /// <summary>
        /// Set Back Color
        /// All inheritors should use these NI functions in constructors of inheritors or for Layout/SizeControl overrides. These change without invalidation or layout 
        /// </summary>
        public Color BackColorNI { set { backcolor = value; } }
        /// <summary>
        /// Set Back Gradient Color
        /// All inheritors should use these NI functions in constructors of inheritors or for Layout/SizeControl overrides. These change without invalidation or layout 
        /// </summary>
        public Color BackColorGradientAltNI { set { backcolorgradientalt = value; } }
        /// <summary>
        /// Set if visible
        /// All inheritors should use these NI functions in constructors of inheritors or for Layout/SizeControl overrides. These change without invalidation or layout 
        /// </summary>
        public bool VisibleNI { set { visible = value; } }
        /// <summary>
        /// Set minimum size of control
        /// All inheritors should use these NI functions in constructors of inheritors or for Layout/SizeControl overrides. These change without invalidation or layout 
        /// </summary>
        /// <param name="size">size</param>
        public void SetMinimumSizeNI(Size size) { minimumsize = size; }
        /// <summary>
        /// Set maximum size of control
        /// All inheritors should use these NI functions in constructors of inheritors or for Layout/SizeControl overrides. These change without invalidation or layout 
        /// </summary>
        /// <param name="size">size</param>
        public void SetMaximumSizeNI(Size size) { maximumsize = size; }

        /// <summary>
        /// Set multiple parameters at once.  If null, don't change this particular parameter
        /// All inheritors should use these NI functions in constructors of inheritors or for Layout/SizeControl overrides. These change without invalidation or layout 
        /// </summary>
        /// <param name="location">change top left</param>
        /// <param name="size">change size</param>
        /// <param name="clientsize">change client size, therefore change bounds</param>
        /// <param name="margin">change margin</param>
        /// <param name="padding">change padding</param>
        /// <param name="borderwidth">change borderwidth</param>
        /// <param name="clipsizetobounds">Size is clipped to the current width/height</param>
        public void SetNI(Point? location = null, Size? size = null, Size? clientsize = null, MarginType? margin = null, PaddingType? padding = null,
                            int? borderwidth = null, bool clipsizetobounds = false)
        {
            Point oldloc = Location;
            Size oldsize = Size;

            if (clipsizetobounds)
            {
                size = new Size(Math.Min(Width, size.Value.Width), Math.Min(Height, size.Value.Height));
            }

            Rectangle pw = window;      // remember previous window position in case it gets changed

            if (margin != null)
                this.margin = margin.Value;
            if (padding != null)
                this.padding = padding.Value;
            if (borderwidth != null)
                this.borderwidth = borderwidth.Value;
            if (location.HasValue)
                window.Location = location.Value;
            if ( size.HasValue || clientsize.HasValue)
            {
                int width = size.HasValue ? size.Value.Width : clientsize.Value.Width + ClientWidthMargin;
                int height = size.HasValue ? size.Value.Height : clientsize.Value.Height + ClientHeightMargin;
                width = Math.Max(width, minimumsize.Width);
                width = Math.Min(width, maximumsize.Width);
                height = Math.Max(height, minimumsize.Height);
                height = Math.Min(height, maximumsize.Height);
                window.Size = new Size(width, height);
            }

            CalcClientRectangle();

            if (window.Location != oldloc)      // if we moved, set previouswindow and call moved
            {
                lastlocation = pw.Location;
                OnMoved();
            }

            if (oldsize != window.Size)         // if we sized, set previouswindow and call sized
            {
                lastsize = pw.Size;
                OnResize();
            }
        }

        /// <summary>
        /// Recursively go thru children, bottom child first, and remove everything 
        /// </summary>
        /// <param name="child">who to remove</param>
        /// <param name="dispose">if to dispose of the child</param>
        /// <param name="removechildren">if to remove children of the control</param>
        internal virtual void RemoveControl(GLBaseControl child, bool dispose, bool removechildren)        
        {
            if (removechildren)
            {
                foreach (var cc in child.childrenz)     // do children of child first
                {
                    RemoveControl(cc, dispose, removechildren);
                }
            }

            //System.Diagnostics.Debug.WriteLine($"Remove control {child.Name} in {Name}");

            child.OnControlRemove(this, child);
            OnControlRemove(this, child);
            //System.Diagnostics.Debug.WriteLine("Remove {0} {1}", child.GetType().Name, child.Name);
            FindDisplay()?.ControlRemoved(child);   // display may be pointing to it

            if (dispose)
                child.Dispose();

            child.parent = null;

            childrenz.Remove(child);
            childreniz.Remove(child);
            CheckZOrder();
        }

        /// <summary>
        /// Make a bitmap for this level of this size - needs to be public due to external derived controls needing it 
        /// Internal use only
        /// </summary>
        internal void MakeLevelBitmap(int width , int height)     
        {
            levelbmp?.Dispose();
            levelbmp = null;
            if (width > 0 && height > 0)
            {
                levelbmp = new Bitmap(width, height);
            }
        }

        /// <summary>
        /// Animate children then itself
        /// </summary>
        /// <param name="ts">timestamp</param>
        internal void Animate(ulong ts)
        {
            if (Visible)
            {
                var controlslist = new List<GLBaseControl>(childreniz); // animators may close/remove the control, so we need to take a copy so we have a collection which does not change.
                foreach (var c in controlslist)
                    c.Animate(ts);
                var animators = new List<IGLControlAnimation>(Animators); // animators may remove themselves from the animation list, so we need to take a copy
                foreach (var a in animators)
                    a.Animate(this, ts);
            }
        }

        #endregion

        #region Internal sizing and layout

        /// <summary>
        /// Called by above giving child reason (or null for remove) for invalidate layout
        /// overriden by display control to pick a better method to just relayout the relevant child
        /// normally we don't care about which child caused it
        /// go thru this function - don't every call invalidate then performlayout as it won't be properly overriden otherwise !
        /// </summary>
        /// <param name="child">reaspm fpr removal, may be null, debug only</param>

        private protected virtual void InvalidateLayout(GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine($"{Name} Invalidate layout due to {child?.Name}");
            Invalidate();
            PerformLayout();
        }
        /// <summary>
        /// Called by displaycontrol on chosen child. Only display control can call this 
        /// </summary>
        internal void PerformLayoutAndSize()
        {
            Size us = Size;

            SizeControl(Parent.ClientSize);    // size us (we obv have a parent, display control)

            PerformRecursiveSize();         // size all children

            SizeControlPostChild(Parent.ClientSize);    // And we give ourselves a chance to post size to children size

            if (us != Size)     // if we changed size, need to do a full layout on parent so all the docking will work
            {
               // System.Diagnostics.Debug.WriteLine($"PerformLayoutSize {Name} changed size, inform parent");
                ParentInvalidateLayout();
            }
            else
            {
                var area = Parent.ClientRectangle;  // else we can just layout ourselves
                Layout(ref area);
                PerformRecursiveLayout();       // and we layout children, recursively
            }
        }

        /// <summary>
        /// Perform a recursive size, on our children. May be overridden by a inheritor
        /// </summary>
        protected void PerformRecursiveSize()   
        {
            //if ( childrenz.Count>0) System.Diagnostics.Debug.WriteLine($"{Name} Perform size of children {size}");

            foreach (var c in childrenz) // in Z order
            {
                if (c.Visible)      // invisible children don't layout
                {
                    c.SizeControl(Size);
                    c.PerformRecursiveSize();
                    c.SizeControlPostChild(Size);
                }
            }
        }

        /// <summary>
        /// Override to auto size before children.  
        /// Only use the NI functions to change size. You can change position as well if you want to
        /// </summary>
        protected virtual void SizeControl(Size parentclientrect)
        {
            //System.Diagnostics.Debug.WriteLine("Size " + Name + " area est is " + parentclientrect);
        }

        /// <summary>
        /// Override to auto size after the children sized themselves.
        /// Only use the NI functions to change size. You can change position as well if you want to
        /// </summary>
        /// <param name="parentclientrect">Parent client rectangle for sizing information</param>
        protected virtual void SizeControlPostChild(Size parentclientrect)
        {
            //System.Diagnostics.Debug.WriteLine("Post Size " + Name + " area est is " + parentclientrect);
        }

        /// <summary>
        /// Performed after sizing, layout children on your control.
        /// Pass our client rectangle to the children and let them layout to it
        /// If you override and do not call base, call ClearLayoutFlags after procedure to clear the layout
        /// </summary>
        protected virtual void PerformRecursiveLayout()     // Layout all the children, and their dependents 
        {
            Rectangle area = ClientRectangle;
            //if ( childrenz.Count>0) System.Diagnostics.Debug.WriteLine($"{Name} Laying out children in {area}");

            foreach (var c in childrenz)     // in z order, top gets first go
            {
                if (c.Visible)      // invisible children don't layout
                {
                    c.Layout(ref area);
                    c.PerformRecursiveLayout();
                }
            }

            ClearLayoutFlags();
        }

        /// <summary>
        /// Clear Layout flags to indicate layout has completed
        /// </summary>
        public void ClearLayoutFlags()
        { 
            suspendLayoutCount = 0;   // we can't be suspended
            needLayout = false;     // we have layed out
        }

        /// <summary>
        /// Because you can't call from an inheritor, even though your the same class, silly, done this way for visibility
        /// </summary>
        internal void CallPerformRecursiveLayout()      
        {
            PerformRecursiveLayout();
        }

        /// <summary>
        /// standard layout function. First layout yourself inside the area then return area left.
        /// You can override this to perform your own specific layout
        /// if so, you must call CalcClientRectangle to update internal variables
        /// </summary>
        /// <param name="parentarea">Area available to you, modify to say what area is left.</param>
        public virtual void Layout(ref Rectangle parentarea)
        {
          //  System.Diagnostics.Debug.WriteLine($"{Name} Layout {parentarea} {docktype} {Anchor}");

            int dockedwidth = DockPercent > 0 ? ((int)(parentarea.Width * DockPercent)) : (window.Width);       // for Left/Right
            int dockedheight = DockPercent > 0 ? ((int)(parentarea.Height * DockPercent)) : (window.Height);    // For Top/Bottom
            int wl = Width;
            int hl = Height;

            Rectangle current = window;

            Rectangle areaout = parentarea;

            if (docktype == DockingType.None)
            {
                // this relies on the previouswindow property to be right, so not multiple resizes before layout. Hopefully this works

                if (Parent != null && Anchor != AnchorType.None)
                {
                    int clientpreviouswidth = (Parent.lastsize.Width - Parent.ClientWidthMargin);
                    int clientpreviousheight = (Parent.lastsize.Height - Parent.ClientHeightMargin);

                    if ((Anchor & AnchorType.Right) != 0)
                    {
                        if (clientpreviouswidth > Right)        // wait till bigger than window size
                        {
                            int rightoffset = clientpreviouswidth - Right;
                            int newright = parentarea.Right - rightoffset;
                            window = new Rectangle(newright - Width, window.Top, Width, Height);
                            //System.Diagnostics.Debug.WriteLine($"Anchor {Name} {clientpreviouswidth} {rightoffset} -> {newright}");
                        }
                    }
                    if ((Anchor & AnchorType.Bottom) != 0)
                    {
                        if (clientpreviousheight > Bottom)
                        {
                            int bottomoffset = clientpreviousheight - Bottom;
                            int newbottom = parentarea.Bottom - bottomoffset;
                            window = new Rectangle(window.Left, newbottom - Height, Width, Height);
                            OnMoved();
                            // System.Diagnostics.Debug.WriteLine($"Anchor {Name} {clientpreviouswidth} {bottomoffset} -> {newbottom}");
                        }
                    }
                }
            }
            else if (docktype == DockingType.Fill)
            {
                window = parentarea;
                areaout = new Rectangle(0, 0, 0, 0);
            }
            else if (docktype == DockingType.Center)
            {
                int xcentre = (parentarea.Left + parentarea.Right) / 2;
                int ycentre = (parentarea.Top + parentarea.Bottom) / 2;
                Width = Math.Min(parentarea.Width, Width);
                Height = Math.Min(parentarea.Height, Height);
                window = new Rectangle(xcentre - Width / 2, ycentre - Height / 2, Width, Height);       // centre in area, bounded by area, no change in area in
            }
            else if (docktype == DockingType.Width)
            {
                window = new Rectangle(0, Top, parentarea.Width, Height);       // dock to full width, but with same Y/Height
            }
            else if (docktype == DockingType.Height)
            {
                window = new Rectangle(Left, 0, Width, parentarea.Height);       // dock to full width, but with same Y/Height
            }
            else if (docktype >= DockingType.Bottom)
            {
                if (docktype == DockingType.Bottom)     // only if we just the whole of the bottom do we modify areaout
                {
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Bottom - dockedheight - dockingmargin.Bottom, parentarea.Width - dockingmargin.TotalWidth, dockedheight);
                    areaout = new Rectangle(parentarea.Left, parentarea.Top, parentarea.Width, parentarea.Height - dockedheight - dockingmargin.TotalHeight);
                }
                else if (docktype == DockingType.BottomCentre)
                    window = new Rectangle(parentarea.Left + parentarea.Width / 2 - wl / 2, parentarea.Bottom - dockedheight - dockingmargin.Bottom, wl, dockedheight);
                else if (docktype == DockingType.BottomLeft)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Bottom - dockedheight - dockingmargin.Bottom, wl, dockedheight);
                else // bottomright
                    window = new Rectangle(parentarea.Right - dockingmargin.Right - wl, parentarea.Bottom - dockedheight - dockingmargin.Bottom, wl, dockedheight);
            }
            else if (docktype >= DockingType.Top)
            {
                if (docktype == DockingType.Top)
                {
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, parentarea.Width - dockingmargin.TotalWidth, dockedheight);
                    areaout = new Rectangle(parentarea.Left, parentarea.Top + dockedheight + dockingmargin.TotalHeight, 
                                                            parentarea.Width, parentarea.Height - dockedheight - dockingmargin.TotalHeight);
                }
                else if (docktype == DockingType.TopCenter)
                    window = new Rectangle(parentarea.Left + parentarea.Width / 2 - wl / 2, parentarea.Top + dockingmargin.Top, wl, dockedheight);
                else if (docktype == DockingType.TopLeft)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, wl, dockedheight);
                else // topright
                    window = new Rectangle(parentarea.Right - dockingmargin.Right - wl, parentarea.Top + dockingmargin.Top, wl, dockedheight);
            }
            else if (docktype >= DockingType.Right)
            {
                if (docktype == DockingType.Right)
                {
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Top + dockingmargin.Top, dockedwidth, parentarea.Height - dockingmargin.TotalHeight);
                    areaout = new Rectangle(parentarea.Left, parentarea.Top, parentarea.Width - window.Width - dockingmargin.TotalWidth, parentarea.Height);
                }
                else if (docktype == DockingType.RightCenter)
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Top + parentarea.Height / 2 - hl / 2, dockedwidth, hl);
                else if (docktype == DockingType.RightTop)
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Top + dockingmargin.Top, dockedwidth, hl);
                else // rightbottom
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Bottom - dockingmargin.Bottom - hl, dockedwidth, hl);
            }
            else // must be left!
            {
                if (docktype == DockingType.Left)
                {
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, dockedwidth, parentarea.Height - dockingmargin.TotalHeight);
                    areaout = new Rectangle(parentarea.Left + dockedwidth + dockingmargin.TotalWidth, 
                                            parentarea.Top, parentarea.Width - dockedwidth - dockingmargin.TotalWidth, parentarea.Height);
                }
                else if (docktype == DockingType.LeftCenter)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + parentarea.Height / 2 - hl / 2, dockedwidth, hl);
                else if (docktype == DockingType.LeftTop)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, dockedwidth, hl);
                else  // leftbottom
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Bottom - dockingmargin.Bottom - hl, dockedwidth, hl);
            }

            // limit size to max/min
            window.Size = new Size(Math.Max(minimumsize.Width,Math.Min(maximumsize.Width,window.Width)),
                                   Math.Max(minimumsize.Height,Math.Min(maximumsize.Width,window.Height)));

            CalcClientRectangle();

         //   System.Diagnostics.Debug.WriteLine($"{Name} dock {Dock} dm {DockingMargin} win {window} Area in {parentarea} Area out {areaout}");

            parentarea = areaout;

            if ( current.Location != window.Location)
            {
                lastlocation = window.Location;
                OnMoved();
            }

            if ( current.Size != window.Size)
            {
               // System.Diagnostics.Debug.WriteLine($"Layout resize {Name}");

                lastsize = window.Size;
                OnResize();
            }
            //  System.Diagnostics.Debug.WriteLine($"{Name} Layout over with {parentarea}");
        }

        /// <summary>
        /// Redraw youself and your children call
        /// </summary>
        /// <param name="parentgr">if null, use the level bitmap of this control, passed down to children to draw into</param>
        /// <param name="bounds">Area that our control occupies on the bitmap, in bitmap co-ords. This may be outside of the clip area below if the child is outside of the client area of its parent control</param>
        /// <param name="cliparea">area that we can draw into, in bitmap co-ords, so we don't exceed the bounds of any parent clip areas above us. clipareas are continually narrowed</param>
        /// <param name="forceredraw">For a redraw even if its NeedRedraw flag is false</param>
        /// <returns>trur if redrawn or any child redrew</returns>
        internal virtual bool Redraw(Graphics parentgr, Rectangle bounds, Rectangle cliparea, bool forceredraw)
        {
            Graphics backgr = parentgr;

            if (parentgr == null)     // top level window, under display control, if this is true
            {
                cliparea = bounds = new Rectangle(0, 0, levelbmp.Width, levelbmp.Height);      // restate area in terms of bitmap, this is the bounds and the clip area

                backgr = Graphics.FromImage(levelbmp);              // get graphics for it
                backgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                backgr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            }

            bool redrawn = false;

            if (needredraw || forceredraw)          // if we need a redraw, or we are forced to draw by a parent redrawing above us.
            {
                //System.Diagnostics.Debug.WriteLine("redraw {0}->{1} Bounds {2} clip {3} client {4} ({5},{6},{7},{8}) nr {9} fr {10}", Parent?.Name, Name, bounds, cliparea, ClientRectangle, ClientLeftMargin, ClientTopMargin, ClientRightMargin, ClientBottomMargin, NeedRedraw, forceredraw);

                forceredraw = true;             // all children, force redraw      
                needredraw = false;             // we have been redrawn
                redrawn = true;                 // and signal up we have been redrawn

                backgr.SetClip(cliparea);           // set graphics to the clip area which includes the border so we can draw the background/border
                backgr.TranslateTransform(bounds.X, bounds.Y);   // move to client 0,0

                if (Width > 0 && Height > 0)        // no point drawing nothing
                {
                    DrawBack(new Rectangle(0, 0, Width, Height), backgr, BackColor, BackColorGradientAlt, BackColorGradientDir);

                    DrawBorder(backgr, BorderColor, BorderWidth);
                }

                backgr.ResetTransform();
            }
            else
            {
             //   System.Diagnostics.Debug.WriteLine($"{Name} does not need draw");
            }

            // now do the children, painting in clientgr
            {
                Rectangle ccliparea = cliparea;     
                Rectangle cbounds = bounds;
                Graphics clientgr = backgr;
                MarginType cmargin;
                Rectangle clientarea;

                if (parentgr != null && levelbmp != null)      // if we have a sub bitmap, which is the bitmap for the client region only
                {
                    // restate area in terms of client rectangle bitmap, this is the bounds and the clip area
                    clientarea = ccliparea = cbounds = new Rectangle(0, 0, levelbmp.Width, levelbmp.Height);      
                    cmargin = new MarginType(0);        // no margins around the bitmap - because its the client bitmap we are dealing with

                    clientgr = Graphics.FromImage(levelbmp);              // get graphics for it
                    clientgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    clientgr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                }
                else
                {
                    // no new bitmap, so we work out the client area of this window (in bitmap co-ords)
                    clientarea = new Rectangle(bounds.Left + ClientLeftMargin, bounds.Top + ClientTopMargin, ClientWidth, ClientHeight);
                    cmargin = new MarginType(ClientLeftMargin, ClientTopMargin, ClientRightMargin, ClientBottomMargin);
                }

                foreach (var c in childreniz)       // in inverse Z order, last is top Z
                {
                    if (c.Visible)
                    {
                        Rectangle childbounds = new Rectangle(clientarea.Left + c.Left,     // not bounded by clip area, in bitmap coords
                                                              clientarea.Top + c.Top,
                                                              c.Width,
                                                              c.Height);

                        // clip area is progressively narrowed as we go down the children
                        // its the minimum of the previous clip area
                        // the child bounds
                        // and the client rectangle

                        int cleft = Math.Max(childbounds.Left, ccliparea.Left);             // first clip to child bounds, or clip left
                        cleft = Math.Max(cleft, cbounds.Left + cmargin.Left);               // then clip to client left

                        int ctop = Math.Max(childbounds.Top, ccliparea.Top);                // clipped to child top or ccliparea top
                        ctop = Math.Max(ctop, cbounds.Top + cmargin.Top);

                        int cright = Math.Min(childbounds.Left + c.Width, ccliparea.Right); // clipped to child left+width or the ccliparea right
                        cright = Math.Min(cright, cbounds.Right - cmargin.Right);           // additionally clipped to our cbounds right less its client margin

                        int cbot = Math.Min(childbounds.Top + c.Height, ccliparea.Bottom);  // clipped to child bottom or ccliparea bottom
                        cbot = Math.Min(cbot, cbounds.Bottom - cmargin.Bottom);             // additionally clipped to cbounds bottom less its client margin

                        Rectangle childcliparea = new Rectangle(cleft, ctop, cright - cleft, cbot - ctop);  // clip area to pass down in bitmap coords

                        redrawn |= c.Redraw(clientgr, childbounds, childcliparea, forceredraw);   // draw, into current gr
                    }
                }

                if (parentgr != null && levelbmp != null)      // if we have a sub bitmap
                    clientgr.Dispose();
            }

            // if we are operating a bitmap, and the children redrew, and we are not a child of control display
            // we must call paint, because thats the thing which throws the now modified bitmap onto the screen
            // note the background has not been repainted (above children) but that is okay, its just the bitmap which needs to be refreshed

            if ( redrawn && levelbmp != null && parentgr != null )
            {
                forceredraw = true; // we must Paint
                //System.Diagnostics.Debug.WriteLine("Child has redrawn in scroll panel but scroll panel is not redrawing");
            }

            // if we need drawing..
            // will be set if NeedRedrawn or forceredrawn.  Draw in the backgr, which is the current bitmap
            // no point drawing nothing either

            if (forceredraw && ClientRectangle.Width>0 && ClientRectangle.Height>0)       
            {
                backgr.SetClip(cliparea);   // set graphics to the clip area, which is the visible area of the ClientRectangle
                    
                backgr.TranslateTransform(bounds.X + ClientLeftMargin, bounds.Y + ClientTopMargin);   // move to client 0,0

                //using (Pen p = new Pen(new SolidBrush(Color.Red))) { gr.DrawLine(p, new Point(0, 0), new Point(1000, 95)); } //for showing where the clip is

                Paint(backgr);
                    
                backgr.ResetTransform();
            }

            if (parentgr == null)
            {
                backgr.Dispose();
            }

            return redrawn;
        }

        /// <summary>
        /// Draw border area, override to draw something different. Co-ords are at 0,0 and clip area set to whole window bounds 
        /// </summary>
        /// <param name="gr">Graphics to draw into</param>
        /// <param name="bordercolor">Border colour</param>
        /// <param name="borderwidth">Border size</param>
        protected virtual void DrawBorder(Graphics gr, Color bordercolor, float borderwidth)
        {
            if (borderwidth > 0)
            {
                int extborder = (int)borderwidth - 1;
                Rectangle rectarea = new Rectangle(Margin.Left+ extborder,
                                                Margin.Top+ extborder,
                                                Width - Margin.TotalWidth - 1 - extborder,
                                                Height - Margin.TotalHeight - 1 - extborder);

                using (var p = new Pen(bordercolor, borderwidth))
                {
                    gr.DrawRectangle(p, rectarea);
                }
            }
        }

        /// <summary>
        /// Draw back of control, override to draw something different. 
        /// </summary>
        /// <param name="area">Area to draw into. Co-ords are at 0,0 and clip area set to whole window bounds </param>
        /// <param name="gr">Graphics to draw into</param>
        /// <param name="backgroundcolor">Color to draw</param>
        /// <param name="bcgradientalt">Alternate gradient color</param>
        /// <param name="bcgradientdir">Gradient direction</param>
        protected virtual void DrawBack(Rectangle area, Graphics gr, Color backgroundcolor, Color bcgradientalt, int bcgradientdir)
        {
            //System.Diagnostics.Debug.WriteLine($"Draw Back {Name} with {bc} in {area}");

            if ( levelbmp != null)                  // if we own a bitmap, reset it all to transparent whatever the color is below
                gr.Clear(Color.Transparent);       

            if (backgroundcolor != Color.Transparent)            // and draw what the back colour is
            {
                if ( levelbmp == null )             // if we are a normal control, we need to start from the pixels inside us being transparent
                    gr.Clear(Color.Transparent);    // erasing anything that we drew before, because if we have half alpha in the colour, it will build up

                if (bcgradientdir != int.MinValue)
                {
                    //System.Diagnostics.Debug.WriteLine("Background " + Name +  " " + bounds + " " + bc + " -> " + bcgradientalt );
                    using (var b = new System.Drawing.Drawing2D.LinearGradientBrush(area, backgroundcolor, bcgradientalt, bcgradientdir))
                        gr.FillRectangle(b, area);       // linear grad brushes do not respect smoothing mode, btw
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Background " + Name + " " + bounds + " " + backcolor);
                    using (Brush b = new SolidBrush(backgroundcolor))     // always fill, so we get back to start
                        gr.FillRectangle(b, area);
                }
            }
        }

        /// <summary>
        /// Paint your foreground. Controls must override this
        /// Called with the clip set to your ClientRectangle or less. 
        /// See Multilinetextbox for an example code if you need to further reduce the clip area
        /// </summary>
        /// <param name="gr">Graphics to draw into</param>
        protected virtual void Paint(Graphics gr)              
        {
            //System.Diagnostics.Debug.WriteLine("Paint {0}", Name);
        }

        #endregion

        #region UI Overrides

        /// <summary>
        /// Mouse has left control. 
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseLeave(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("leave " + Name + " " + e.Location);
            MouseLeave?.Invoke(this, e);

            if (InvalidateOnEnterLeave)
            {
                //System.Diagnostics.Debug.WriteLine($"Invalid on enter {Name}");
                Invalidate();
            }
        }

        /// <summary>
        /// Mouse has entered control. 
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseEnter(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("enter " + Name + " " + e.Location + " " + InvalidateOnEnterLeave);
            MouseEnter?.Invoke(this, e);

            if (InvalidateOnEnterLeave)
            {
                //System.Diagnostics.Debug.WriteLine($"Invalid on enter {Name}");
                Invalidate();
            }
        }

        /// <summary>
        /// Mouse button has been released
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseUp(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("up   " + Name + " " + e.Location + " " + e.Button);
            MouseUp?.Invoke(this, e);

            if (InvalidateOnMouseDownUp)
                Invalidate();
        }

        /// <summary>
        /// Mouse button has been pressed
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseDown(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("down " + Name + " " + e.Location + " " + e.Button + " " + MouseButtonsDown);
            MouseDown?.Invoke(this, e);

            if (InvalidateOnMouseDownUp)
            {
                Invalidate();
            }
        }

        /// <summary>
        /// Mouse was clicked on control
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseClick(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("click " + Name + " " + e.Button + " " + e.Clicks + " " + e.Location);
            MouseClick?.Invoke(this, e);
        }

        /// <summary>
        /// Mouse has double clicked on control. 
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseDoubleClick(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("doubleclick " + Name + " " + e.Button + " " + e.Clicks + " " + e.Location);
            MouseDoubleClick?.Invoke(this, e);
        }

        /// <summary>
        /// Mouse has moved over control
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseMove(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Over " + Name + " " + e.Location);
            MouseMove?.Invoke(this, e);

            if (InvalidateOnMouseMove)
                Invalidate();
        }

        /// <summary>
        /// Mouse wheen has moved while over control
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnMouseWheel(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Over " + Name + " " + e.Location);
            MouseWheel?.Invoke(this, e);
        }

        /// <summary>
        /// Key has been pressed when control is focused
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnKeyDown(GLKeyEventArgs e)     // GLForm above control gets this as well, and can cancel call to control by handling it
        {
            KeyDown?.Invoke(this, e);
        }

        /// <summary>
        /// Key has been released when control is focused
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnKeyUp(GLKeyEventArgs e)       // GLForm above control gets this as well, and can cancel call to control by handling it
        {
            KeyUp?.Invoke(this, e);
        }

        /// <summary>
        /// Keycode has been pressed when control is focused
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnKeyPress(GLKeyEventArgs e)    // GLForm above control gets this as well, and can cancel call to control by handling it
        {
            KeyPress?.Invoke(this, e);
        }

        /// <summary>
        /// Focus has changed to/from the control
        /// Override to implement custom features, you normally call back to this as your first action.
        /// </summary>
        /// <param name="focused">Indicates focus action, either Focused on control, Deactivated, or one of your children has been Focused or Deactivated</param>
        /// <param name="ctrl">Control which focus event has happened on</param>
        protected virtual void OnFocusChanged(FocusEvent focused, GLBaseControl ctrl)  // focused elements or parents up to GLForm gets this as well
        {
            this.focused = focused == FocusEvent.Focused;
            //System.Diagnostics.Debug.WriteLine($"On Focus Changed {Name} to {focused} = {this.focused} fi {InvalidateOnFocusChange}");
            if (InvalidateOnFocusChange)
                Invalidate();
            FocusChanged?.Invoke(this, focused, ctrl);
        }

        /// <summary>
        /// Override this to see all focus events, not just on yourself.
        /// </summary>
        /// <param name="from">Focus changed from this (may be null if no focus)</param>
        /// <param name="to">Focus is now here (may be null if clicked on no controls)</param>
        protected virtual void OnGlobalFocusChanged(GLBaseControl from, GLBaseControl to) // everyone gets this
        {
            GlobalFocusChanged?.Invoke(from, to);
            List<GLBaseControl> list = new List<GLBaseControl>(childrenz); // copy of, in case the caller closes something
            foreach (var c in list)
                c.OnGlobalFocusChanged(from, to);
        }

        /// <summary>
        /// Override this to see all mouse clicks, not just on yourself
        /// </summary>
        /// <param name="ctrl">Control which was clicked</param>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnGlobalMouseClick(GLBaseControl ctrl, GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("In " + Name + " Global click in " + ctrl.Name);
            GlobalMouseClick?.Invoke(ctrl, e);
            List<GLBaseControl> list = new List<GLBaseControl>(childrenz); // copy of, in case the caller closes something
            foreach (var c in list)
                c.OnGlobalMouseClick(ctrl, e);
        }

        /// <summary>
        /// Override this to see all mouse down events, not just on yourself
        /// </summary>
        /// <param name="ctrl">Control which was clicked</param>
        /// <param name="e">Window and mouse event parameters</param>
        protected virtual void OnGlobalMouseDown(GLBaseControl ctrl, GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("In " + Name + " Global click in " + ctrl.Name);
            GlobalMouseDown?.Invoke(ctrl, e);
            List<GLBaseControl> list = new List<GLBaseControl>(childrenz); // copy of, in case the caller closes something
            foreach (var c in list)
                c.OnGlobalMouseDown(ctrl, e);
        }

        /// <summary>
        /// Override this to see font change events on the control
        /// You can also hook to the FontCHanged Action
        /// </summary>
        protected virtual void OnFontChanged()
        {
            FontChanged?.Invoke(this);
        }

        /// <summary>
        /// Override this to see size change events on the control
        /// You can also hook to the Resize Action
        /// </summary>
        protected virtual void OnResize()
        {
            //System.Diagnostics.Debug.WriteLine($"On Resize {Name}");
            Resize?.Invoke(this);
        }

        /// <summary>
        /// Override this to see position change events on the control
        /// You can also hook to the Moved Action
        /// </summary>
        protected virtual void OnMoved()
        {
            Moved?.Invoke(this);
        }

        /// <summary>
        /// Override this to see new controls added as children to the control
        /// You can also hook to the ControlAdd Action
        /// </summary>
        protected virtual void OnControlAdd(GLBaseControl parent, GLBaseControl child)     // fired to both the parent and child
        {
            ControlAdd?.Invoke(parent, child);
        }

        /// <summary>
        /// Override this to see children controls removed to the control
        /// You can also hook to the ControlRemove Action
        /// </summary>
        protected virtual void OnControlRemove(GLBaseControl parent, GLBaseControl ctrlbeingremoved) // fired to both the parent and child
        {
            ControlRemove?.Invoke(parent, ctrlbeingremoved);
        }

        #endregion

        #region Tab Order

        /// <summary>
        /// Find next tab child, from this, down tree.
        /// </summary>
        /// <param name="tabno">Current tab</param>
        /// <param name="mindist">set to int.Max on entry</param>
        /// <param name="forward">direction, true = forward</param>
        /// <returns></returns>
        private Tuple<GLBaseControl, int> FindNextTabChild(int tabno, int mindist, bool forward = true)
        {
            GLBaseControl found = null;

            foreach (var c in ControlsZ)
            {
                if (c.Focusable && c.Visible && c.Enabled)
                {
                    //System.Diagnostics.Debug.WriteLine($"Find tab child {this.Name} {tabno} {mindist} {forward} in child {c.Name} {c.TabOrder}");

                    int dist = c.TabOrder - tabno;

                    if (forward ? dist > 0 : dist < 0)
                    {
                        dist = Math.Abs(dist);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            found = c;
                            //System.Diagnostics.Debug.WriteLine($"Find tab child best so far is {mindist} in child {found.Name} {found.TabOrder}");
                        }
                    }
                }
                else if (c.IsContainer)
                {
                    //System.Diagnostics.Debug.WriteLine($"Find tab child >> {c.Name}");
                    var res = c.FindNextTabChild(tabno, mindist, forward);
                    if (res.Item1 != null)  // if found one better
                    {
                        found = res.Item1;
                        mindist = res.Item2;
                    }
                    //System.Diagnostics.Debug.WriteLine($"Find tab child << {c.Name}");
                }
            }

            return new Tuple<GLBaseControl, int>(found, mindist);
        }

        /// <summary>
        /// Find highest tab child
        /// </summary>
        /// <param name="curmax">set to -1 on entry</param>
        /// <returns></returns>
        private Tuple<GLBaseControl, int> FindHigestTabChild(int curmax = -1)
        {
            GLBaseControl found = null;

            foreach (var c in ControlsZ)
            {
                if (c.Focusable && c.Visible && c.Enabled)
                {
                    if (c.TabOrder > curmax)
                    {
                        found = c;
                        curmax = c.TabOrder;
                    }
                }
                else if (c.IsContainer)
                {
                    var res = c.FindHigestTabChild(curmax);
                    if (res.Item1 != null)  // if found one better
                        curmax = res.Item2;
                }
            }

            return new Tuple<GLBaseControl, int>(found, curmax);
        }

        #endregion
        #region Implementation

        // Set Position, clipped to max/min size, causing an invalidation layout at parent level, only if changed

        internal virtual void SetPos(int left, int top, int width, int height)
        {
            width = Math.Max(width, minimumsize.Width);
            width = Math.Min(width, maximumsize.Width);
            height = Math.Max(height, minimumsize.Height);
            height = Math.Min(height, maximumsize.Height);
            Rectangle w = new Rectangle(left, top, width, height);

            if (w != window)        // if changed
            {
                bool resized = w.Size != window.Size;
                bool moved = w.Location != window.Location;

                if (resized)
                    lastsize = Size;

                if (moved)
                    lastlocation = Location;

                window = w;
                //System.Diagnostics.Debug.WriteLine($"SetPos {Name} {window} prev {previouswindow}");
                
                if (resized)
                    CalcClientRectangle();

                if (moved)
                    OnMoved();

                if (resized)
                    OnResize();

                needredraw = true;

                Parent?.InvalidateLayout(this);
            }
        }

        // client rectangle calc - call if you change the window bounds, margin, padding
        private void CalcClientRectangle()      
        {
            ClientRectangle = new Rectangle(0, 0, Math.Max(0,Width - Margin.TotalWidth - Padding.TotalWidth - BorderWidth * 2), 
                                                  Math.Max(0,Height - Margin.TotalHeight - Padding.TotalHeight - BorderWidth * 2));
        }

        // set enabled, and all children too
        private void SetEnabled(bool v)
        {
            enabled = v;
            foreach (var c in childrenz)
                c.SetEnabled(v);
        }

        private void SetFont(Font f)
        {
            font = f;
            PropergateFontChanged(this);
        }

        private void PropergateFontChanged(GLBaseControl p)
        {
            p.OnFontChanged();
            foreach (var c in p.childrenz)
            {
                if (c.font == null)     // if child does not override font..
                    PropergateFontChanged(c);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckZOrder()
        {
            int pos = childreniz.Count - 1;
            foreach (var c in childrenz)
            {
                System.Diagnostics.Debug.Assert(c == childreniz[pos--]);
            }
        }

        private void ClearFlagsDown()       // ensure this and its children have all flags cleared to default
        {
            Hover = false;
            focused = false;
            MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;
            foreach (var c in childrenz)
                c.ClearFlagsDown();
        }

        /// <summary>
        /// Dispose of resources of this control. 
        /// </summary>
        public virtual void Dispose()
        {
            levelbmp?.Dispose();
            levelbmp = null;
        }

        internal void ClearRedraw()
        {
            needredraw = false;
        }
        internal void ClearSuspendLayout()
        {
            suspendLayoutCount = 0;
        }

        private bool needredraw { get; set; } = true;         // we need to redraw, therefore all children also redraw
        internal bool TopLevelControlUpdate { get; set; } = false;        // Top level windows only, indicate need to recalculate due to Opacity or Scale change

        private Bitmap levelbmp;       // set if the level has a new bitmap.  Controls under Form always does. Other ones may if they scroll
        private Point scrolloffset;     // offset of bit map

        private Font font = null;

        private Point lastlocation;    // setpos/setNI changes these if changed sizes
        private Size lastsize;       

        private Rectangle window;       // total area owned, in parent co-ords
        private PaddingType padding;
        private MarginType margin;
        private Size minimumsize = new Size(0, 0);
        private Size maximumsize = new Size(int.MaxValue, int.MaxValue);

        private bool needLayout  = false;        // need a layout after suspend layout was called
        private int suspendLayoutCount = 0;        // suspend layout is on

        private bool enabled = true;
        private bool visible  = true;

        private DockingType docktype = DockingType.None;
        private float dockpercent  = 0;
        private MarginType dockingmargin;
        private AnchorType anchortype = AnchorType.None;

        private bool autosize;

        private int column = 0;     // for table layouts
        private int row = 0;        // for table layouts

        private Color backcolor  = Color.Red;
        private Color backcolorgradientalt  = Color.Red;
        private int backcolorgradientdir  = int.MinValue;           // in degrees
        private Color bordercolor  = Color.Transparent;         // Margin - border - padding is common to all controls. Area left is control area to draw in
        private int borderwidth  = 0;

        private bool focused  = false;
        private bool focusable  = false;       // if true, clicking on it gets focus.  If not true, clincking on it set focus to null, unless next is set
        private bool rejectfocus  = false;     // if true, clicking on it does nothing to focus.
        private bool givefocustoparent  = false;     // if true, clicking on it tries to focus parent
        private bool topMost  = false;              // if set, always force to top

        private SizeF? altscale = null;
        private Font DefaultFont = new Font("Ms Sans Serif", 8.25f);
        private float opacity = 1.0f;

        private GLWindowControl.GLCursorType cursor = GLWindowControl.GLCursorType.Normal;

        private GLBaseControl parent  = null;       // its parent, or null if not connected or GLDisplayControl

        // z order
        private List<GLBaseControl> childrenz = new List<GLBaseControl>();
        private List<GLBaseControl> childreniz = new List<GLBaseControl>();

        #endregion

    }
}
