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


using System;
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    // used by GLControlDisplay only, Lower controls do not use these functions
    // here so it can call protected members of this class.  

    public abstract partial class GLBaseControl : IDisposable
    {
        private GLBaseControl currentmouseover = null;
        private GLBaseControl currentfocus = null;                  
        private GLBaseControl mousedowninitialcontrol = null;       // track where mouse down occurred

        private bool SetFocus(GLBaseControl newfocus)    // null to clear focus, true if focus taken
        {
            if (newfocus == currentfocus)       // no action if the same
                return true;

            if (newfocus != null)
            {
                if (newfocus.GiveFocusToParent && newfocus.Parent != null && newfocus.Parent.RejectFocus == false)
                    newfocus = newfocus.Parent;     // see if we want to give it to parent

                if (newfocus.RejectFocus)       // if reject focus change when clicked, abort, do not change focus
                    return false;

                if (!newfocus.Enabled || !newfocus.Focusable)       // if its not enabled or not focusable, change to no focus
                {
                    //System.Diagnostics.Debug.WriteLine("Focus target not enabled/focusable " + newfocus.Name);
                    newfocus = null;
                }
            }

            GLBaseControl oldfocus = currentfocus;

            OnGlobalFocusChanged(oldfocus, newfocus);

            //            System.Diagnostics.Debug.WriteLine("Focus changed from '{0}' to '{1}' {2}", oldfocus?.Name, newfocus?.Name, Environment.StackTrace);

            if (currentfocus != null)           // if we have a focus, inform losing it, and cancel it
            {
                currentfocus.OnFocusChanged(FocusEvent.Deactive, newfocus);

                for (var c = currentfocus.Parent; c != null; c = c.Parent)      // inform change up and including the GLForm
                {
                    c.OnFocusChanged(FocusEvent.ChildDeactive, newfocus);
                    if (c is GLForm)
                        break;
                }

                currentfocus = null;
            }

            if (newfocus != null)               // if we have a new focus, set and tell it
            {
                currentfocus = newfocus;

                currentfocus.OnFocusChanged(FocusEvent.Focused, oldfocus);

                for (var c = currentfocus.Parent; c != null; c = c.Parent)      // inform change up and including the GLForm
                {
                    c.OnFocusChanged(FocusEvent.ChildFocused, currentfocus);
                    if (c is GLForm)
                        break;
                }
            }

            return true;
        }

        protected void ControlRemoved(GLBaseControl other)     // called on ControlDisplay, to inform it that a control has been removed
        {
            if (currentfocus == other)
                currentfocus = null;
            if (currentmouseover == other)
                currentmouseover = null;
        }

        protected void Gc_MouseEnter(object sender, GLMouseEventArgs e)
        {
            Gc_MouseLeave(sender, e);       // leave current

            SetViewScreenCoord(ref e);

            currentmouseover = FindControlOver(e.ScreenCoord, out Point leftover);

            if (currentmouseover != null)
            {
                currentmouseover.Hover = true;

                SetControlLocation(ref e, currentmouseover);

                ((GLControlDisplay)this).SetCursor(currentmouseover.Cursor);

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseEnter(e);
            }
        }
        protected void Gc_MouseLeave(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null)
            {
                currentmouseover.MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;
                currentmouseover.Hover = false;

                var mouseleaveev = new GLMouseEventArgs(e.WindowLocation);
                SetViewScreenCoord(ref e);

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseLeave(mouseleaveev);

                currentmouseover = null;

                ((GLControlDisplay)this).SetCursor(GLCursorType.Normal);
            }
        }

        protected void Gc_MouseMove(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);
            //System.Diagnostics.Debug.WriteLine("WLoc {0} VP {1} SLoc {2} MousePos {3}", e.WindowLocation, e.ViewportLocation, e.ScreenCoord, FindDisplay().MouseWindowPosition);

            GLBaseControl c = FindControlOver(e.ScreenCoord, out Point leftover); // overcontrol ,or over display, or maybe outside display

            if (c != currentmouseover)      // if different, either going active or inactive
            {
                mousedowninitialcontrol = null;     // because we moved away from mouse down control, its now null

                if (currentmouseover != null)   // for current, its a leave or its a drag..
                {
                    SetControlLocation(ref e, currentmouseover);

                    if (currentmouseover.MouseButtonsDown != GLMouseEventArgs.MouseButtons.None)   // click and drag, can't change control while mouse is down
                    {
                       // System.Diagnostics.Debug.WriteLine($"Captured WLoc {e.WindowLocation} VP {e.ViewportLocation} SLoc {e.ScreenCoord} Bnd {e.BoundsLocation} {currentmouseover?.Name} @ {currentmouseoverscreenpos}");
                        GlobalMouseMove?.Invoke(e);     // we move, with the currentmouseover

                        if (currentmouseover.Enabled)       // and send to control if enabled
                            currentmouseover.OnMouseMove(e);

                        return;
                    }

                    currentmouseover.Hover = false;     // we are leaving this one

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseLeave(e);
                }

                currentmouseover = c;   // change to new value

                if (currentmouseover != null)       // now, are we going over a new one?
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc
                   // System.Diagnostics.Debug.WriteLine($"Changed to WLoc {e.WindowLocation} VP {e.ViewportLocation} SLoc {e.ScreenCoord} bnd {e.BoundsLocation} {currentmouseover?.Name} @ {currentmouseoverscreenpos}");

                    currentmouseover.Hover = true;

                    GlobalMouseMove?.Invoke(e);     // we move, with the new currentmouseover

                    ((GLControlDisplay)this).SetCursor(currentmouseover.Cursor);

                    if (currentmouseover.Enabled)       // and send to control if enabled
                        currentmouseover.OnMouseEnter(e);
                }
                else
                {
                    GlobalMouseMove?.Invoke(e);     // we move, with no mouse over

                    ((GLControlDisplay)this).SetCursor(GLCursorType.Normal);

                    if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                        this.OnMouseMove(e);
                }
            }
            else
            {                                                       // over same control
                if (currentmouseover != null)
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc
                    //System.Diagnostics.Debug.WriteLine($"move WLoc {e.WindowLocation} VP {e.ViewportLocation} SLoc {e.ScreenCoord} Loc {e.Location} Bnd {e.BoundsLocation} {currentmouseover?.Name}");

                    GlobalMouseMove?.Invoke(e);     // we move, with the new currentmouseover

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseMove(e);
                }
                else
                {
                    GlobalMouseMove?.Invoke(e);     // we move, with no mouse over

                    if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                        this.OnMouseMove(e);
                }
            }
        }

        protected void Gc_MouseDown(object sender, GLMouseEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine("GC Mouse down");
            if (currentmouseover != null)
            {
                currentmouseover.FindControlUnderDisplay()?.BringToFront();     // this brings to the front of the z-order the top level element holding this element and makes it visible.

                SetViewScreenCoord(ref e);
                SetControlLocation(ref e, currentmouseover);

                OnGlobalMouseDown(currentmouseover, e);

                if (currentmouseover.Enabled)
                {
                    currentmouseover.MouseButtonsDown = e.Button;
                    currentmouseover.OnMouseDown(e);
                }

                mousedowninitialcontrol = currentmouseover;
            }
            else
            {
                OnGlobalMouseDown(null, e);

                if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                    this.OnMouseDown(e);
            }
        }
        protected void Gc_MouseUp(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);

            if (currentmouseover != null)
            {
                currentmouseover.MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;

                SetControlLocation(ref e, currentmouseover);    // reset location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseUp(e);
            }
            else
            {
                if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                    this.OnMouseUp(e);
            }

            mousedowninitialcontrol = null;
        }

        protected void Gc_MouseClick(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);

            if (mousedowninitialcontrol == currentmouseover && currentmouseover != null)        // clicks only occur if mouse is still over initial control
            {
                e.WasFocusedAtClick = currentmouseover == currentfocus;         // record if clicking on a focused item

                SetFocus(currentmouseover);

                if (currentmouseover != null)     // set focus could have force a loss, thru the global focus hook
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc

                    OnGlobalMouseClick(currentmouseover, e);

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseClick(e);
               }
            }
            else if (currentmouseover == null)        // not over any control, even control display, but still click, (due to screen coord clip space), so send thru the displaycontrol
            {
                SetFocus(null);
                OnGlobalMouseClick(null, e);
                this.OnMouseClick(e);
            }
        }

        protected void Gc_MouseDoubleClick(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);

            if (mousedowninitialcontrol == currentmouseover && currentmouseover != null)        // clicks only occur if mouse is still over initial control
            {
                e.WasFocusedAtClick = currentmouseover == currentfocus;         // record if clicking on a focused item

                SetFocus(currentmouseover);

                if (currentmouseover != null)     // set focus could have force a loss, thru the global focus hook
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseDoubleClick(e);
                }
            }
            else if (currentmouseover == null)        // not over any control, even control display, but still click, (due to screen coord clip space), so send thru the displaycontrol
            {
                SetFocus(null);

                if (this.Enabled)
                    this.OnMouseDoubleClick(e);
            }
        }

        protected void Gc_MouseWheel(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null && currentmouseover.Enabled)
            {
                SetViewScreenCoord(ref e);
                SetControlLocation(ref e, currentmouseover);    // set location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseWheel(e);
            }
        }

        // provides a means to calculate what the GLMouseEventArgs for windowlocation, viewportlocation, etc is from a window point inside the GL Control window
        public GLMouseEventArgs MouseEventArgsFromPoint(Point windowlocation)
        {
            GLMouseEventArgs e = new GLMouseEventArgs(windowlocation);
            SetViewScreenCoord(ref e);
            GLBaseControl c = FindControlOver(e.ScreenCoord, out Point leftover); // overcontrol ,or over display, or maybe outside display
            if (c != null)
            {
                SetControlLocation(ref e, c);
            }

            return e;
        }

        // overriden by GLControlDisplay. Translate WindowsLocation into ViewPortLocation and ScreenCoord
        protected virtual void SetViewScreenCoord(ref GLMouseEventArgs e)       // overridden in control class to provide co-ords
        {
        }

        // pass in current control and its upper left screen location, and it will set up the mouse events args for you
        private void SetControlLocation(ref GLMouseEventArgs e, GLBaseControl cur)
        {
            // record control, bounds, and client location
            e.Control = cur;
            
            Point ctrlloc = cur.FindScreenCoords(new Point(0, 0));      // we need to find this each time, the control may have been dragged, can't cache it like previous goes at this code!

            e.Bounds = cur.Bounds;  // in parent co-ords
            e.BoundsLocation = new Point(e.ScreenCoord.X - ctrlloc.X, e.ScreenCoord.Y - ctrlloc.Y);     // to location in bounds
            e.Location = new Point(e.BoundsLocation.X-cur.ClientLeftMargin, e.BoundsLocation.Y-cur.ClientTopMargin);      // to location in client rectangle co-ords

            // determine logical area
            if (e.Location.X < 0)
                e.Area = GLMouseEventArgs.AreaType.Left;
            else if (e.Location.X >= cur.ClientWidth)
            {
                if (e.Location.Y >= cur.ClientHeight)
                    e.Area = GLMouseEventArgs.AreaType.NWSE;
                else
                    e.Area = GLMouseEventArgs.AreaType.Right;
            }
            else if (e.Location.Y < 0)
                e.Area = GLMouseEventArgs.AreaType.Top;
            else if (e.Location.Y >= cur.ClientHeight)
                e.Area = GLMouseEventArgs.AreaType.Bottom;
            else
                e.Area = GLMouseEventArgs.AreaType.Client;

         //   System.Diagnostics.Debug.WriteLine($"Pos {e.WindowLocation} VP {e.ViewportLocation} SC {e.ScreenCoord} BL {e.BoundsLocation} loc {e.Location} {e.Area} {cur.Name} {cur.Bounds}");
        }

        // feed keys to focus is present and enabled, else the control display gets them

        protected void Gc_KeyUp(object sender, GLKeyEventArgs e)
        {
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyUp(e);            // reflect to form

                if (!e.Handled)                                    // send to control
                    currentfocus.OnKeyUp(e);
            }
            else
                OnKeyUp(e);
        }

        protected void Gc_KeyDown(object sender, GLKeyEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Control keydown " + e.KeyCode + " on " + currentfocus?.Name);
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyDown(e);          // reflect to form

                if (!e.Handled)                                    // send to control
                    currentfocus.OnKeyDown(e);
            }
            else
                OnKeyDown(e);
        }

        protected void Gc_KeyPress(object sender, GLKeyEventArgs e)
        {
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyPress(e);         // reflect to form

                if (!e.Handled)
                    currentfocus.OnKeyPress(e);                     // send to control
            }
            else
                OnKeyPress(e);
        }

        // we are in control display, and if control change the cursor is the current control mouse over, set it
        protected void Gc_CursorTo(GLBaseControl c, GLCursorType ct)       
        {
            if (currentmouseover == c)                              
                ((GLControlDisplay)this).SetCursor(ct);
        }
    }
}
