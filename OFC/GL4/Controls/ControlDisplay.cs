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

using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OFC.GL4.Controls
{
    // This control display needs a GLWindowControl to get events from (in constructor)
    // it in turn passes on those events as its a GLWindowControl, adjusted to the controls in the window
    // It render the control display

    public class GLControlDisplay : GLBaseControl, GLWindowControl
    {
        #region Public IF

        public bool RequestRender { get; set; } = false;

        public Point MouseScreenPosition { get { return glwin.MouseScreenPosition; } }

        public override bool Focused { get { return glwin.Focused; } }          // override focused to report if whole window is focused.

        public Action<GLControlDisplay, GLBaseControl, GLBaseControl> GlobalFocusChanged { get; set; } = null;     // subscribe to get any focus changes (from old to new, may be null)
        public Action<GLControlDisplay, GLBaseControl, GLMouseEventArgs> GlobalMouseClick{ get; set; } = null;     // subscribe to get any clicks
        public Action<GLMouseEventArgs> GlobalMouseMove { get; set; } = null;   // subscribe to get any movement changes

        public new Action<Object> Paint { get; set; } = null;                   // override to get a paint event

        public GLMatrixCalc MatrixCalc { get; set; }

        public GLControlDisplay(GLItemsList items, GLWindowControl win, GLMatrixCalc mc) : base("displaycontrol", new Rectangle(0, 0, mc.ScreenCoordMax.Width, mc.ScreenCoordMax.Height))
        {
            glwin = win;
            MatrixCalc = mc;

            vertexes = items.NewBuffer();

            vertexarray = new GLVertexArray();   
            vertexes.Bind(vertexarray,0, 0, vertexesperentry * sizeof(float));             // bind to 0, from 0, 2xfloats. Must bind after vertexarray is made as its bound during construction

            vertexarray.Attribute(0, 0, vertexesperentry, OpenTK.Graphics.OpenGL4.VertexAttribType.Float); // bind 0 on attr 0, 2 components per vertex

            GLRenderControl rc = GLRenderControl.TriStrip();
            rc.PrimitiveRestart = 0xff;
            ri = new GLRenderableItem(rc, 0, vertexarray);     // create a renderable item
            ri.CreateRectangleElementIndexByte(items.NewBuffer(), 255 / 5);
            ri.DrawCount = 0;                               // nothing to draw at this point

            shader = new GLControlShader();

            textures = new Dictionary<GLBaseControl, GLTexture2D>();
            texturebinds = new GLBindlessTextureHandleBlock(arbbufferid);

            glwin.MouseMove += Gc_MouseMove;
            glwin.MouseClick += Gc_MouseClick;
            glwin.MouseDoubleClick += Gc_MouseDoubleClick;
            glwin.MouseDown += Gc_MouseDown;
            glwin.MouseUp += Gc_MouseUp;
            glwin.MouseEnter += Gc_MouseEnter;
            glwin.MouseLeave += Gc_MouseLeave;
            glwin.MouseWheel += Gc_MouseWheel;
            glwin.KeyDown += Gc_KeyDown;
            glwin.KeyUp += Gc_KeyUp;
            glwin.KeyPress += Gc_KeyPress;
            glwin.Resize += Gc_Resize;
            glwin.Paint += Gc_Paint;
        }

        public Rectangle ClientScreenPos { get { return glwin.ClientScreenPos; } }  
        
        public void SetCursor(GLCursorType t)
        {
            glwin.SetCursor(t);
        }

        public override void Add(GLBaseControl other, bool atback = false)           
        {
            System.Diagnostics.Debug.Assert(other is GLVerticalScrollPanel == false, "GLVerticalScrollPanel must not be a child of GLForm");
            textures[other] = new GLTexture2D();                // we make a texture per top level control to render with
            other.MakeLevelBitmap(Math.Max(1,other.Width),Math.Max(1,other.Height));    // ensure we make a bitmap
            base.Add(other,atback);
        }

        public override void Remove(GLBaseControl other)
        {
            if (ControlsZ.Contains(other))
            {
                base.Remove(other);
                textures[other].Dispose();
                textures.Remove(other);
            }
        }

        public bool SetFocus(GLBaseControl newfocus)    // null to clear focus, true if focus taken
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
                    newfocus = null;
            }

            GLBaseControl oldfocus = currentfocus;

            GlobalFocusChanged?.Invoke(this, oldfocus, newfocus);   // global invoker
            System.Diagnostics.Debug.WriteLine("Focus changed from '{0}' to '{1}'", oldfocus?.Name, newfocus?.Name);

            if (currentfocus != null)           // if we have a focus, inform losing it, and cancel it
            {
                currentfocus.OnFocusChanged(FocusEvent.Deactive, newfocus);

                for( var c = currentfocus.Parent; c != null; c = c.Parent)      // inform change up and including the GLForm
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

        public override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();
            UpdateVertexTexturePositions(true);
        }

        public override bool BringToFront(GLBaseControl other)  // if we change the z order, we need to update vertex list, keyed to z order
        {                                                       // and the textures, since the bindless IDs are written in z order        
            if (!base.BringToFront(other))
            {
                UpdateVertexTexturePositions(true);        // we changed z order, update
                return false;
            }
            else
                return true;
        }

        // overriding this indicates all we have to do if child location changes is update the vertex positions, and that we have dealt with it
        protected override bool InvalidateDueToLocationChange(GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("Control display location change");
            UpdateVertexTexturePositions(false);
            return false;
        }

        private void UpdateVertexTexturePositions(bool updatetextures)        // update vertexes, maybe update textures
        {
            if (ControlsZ.Count > 0)            // may end up with nothing to draw, in which case, don't update anything
            {
                vertexes.AllocateBytes(ControlsZ.Count * sizeof(float) * vertexesperentry * 4);
                vertexes.StartWrite(0, vertexes.Length);

                float z = 0.0001f;      // we place it in clip space at a z near 0
                int visible = 0;

                List<IGLTexture> tlist = new List<IGLTexture>();

                foreach (var c in ControlsIZ)       // we paint in IZ order, and we set the Z (bigger is more in the back) from a notional 0.1 to 0 so the depth test works
                {
                    if (c.Visible)  // must be visible to be added to vlist
                    {
                        float[] a = new float[] {       c.Left, c.Top, z, 1,
                                                        c.Left, c.Bottom , z, 1,
                                                        c.Right, c.Top, z, 1,
                                                        c.Right, c.Bottom , z, 1,
                                                 };
                        vertexes.Write(a);
                        z -= 0.0000001f;
                        visible++;

                        if (updatetextures)
                        {
                            if (textures[c].Id == -1 || textures[c].Width != c.LevelBitmap.Width || textures[c].Height != c.LevelBitmap.Height)      // if layout changed bitmap
                            {
                                textures[c].CreateOrUpdateTexture(c.Width, c.Height);   // and make a texture, this will dispose of the old one 
                            }

                            tlist.Add(textures[c]);     // need to have them in the same order as the client rectangles
                        }
                    }
                }

                vertexes.StopReadWrite();
                OFC.GLStatics.Check();

                if ( tlist.Count>0)     // only if we had visible ones
                    texturebinds.WriteHandles(tlist.ToArray()); // write texture handles to the buffer..  written in iz order

                ri.DrawCount = (visible>0) ? (visible * 5 - 1) : 0;    // 4 vertexes per rectangle, 1 restart
            }
            else
                ri.DrawCount = 0;           // and set count to zero.

            RequestRender = true;
        }

        // call this during your Paint to render.

        public void Render(GLRenderControl currentstate)
        {
            //System.Diagnostics.Debug.WriteLine("Render");

            NeedRedraw = false;
            RequestRender = false;

            if (ControlsIZ.Count > 0)       // only action if children present
            {
                foreach (var c in ControlsIZ)
                {
                    if (c.Visible)
                    {
                        //System.Diagnostics.Debug.WriteLine("Draw " + c.Name);
                        bool redrawn = c.Redraw(null, new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 0, 0), null, false);      // see if redraw done

                        if (redrawn)
                        {
                            textures[c].CreateLoadBitmap(c.LevelBitmap);  // and update texture unit with new bitmap
                            //float[] p = textures[c].GetTextureImageAsFloats(end:100);
                        }
                    }
                }

                GLScissors.SetToScreenCoords(0, MatrixCalc);
                shader.Start(MatrixCalc);
                ri.Bind(currentstate, shader, MatrixCalc);        // binds VA AND the element buffer
                ri.Render();                                // draw using primitive restart on element index buffer with bindless textures
                shader.Finish(MatrixCalc);
                GL.UseProgram(0);           // final clean up
                GL.BindProgramPipeline(0);
                GLScissors.Disable(0);
                GLStatics.Check();

                foreach (var c in ControlsIZ)
                {
                    var form = c as GLForm;
                    if (form != null && form.FormShown == false)
                    {
                        form.OnShown();
                        form.FormShown = true;
                    }
                }
            }

            //System.Diagnostics.Debug.WriteLine("Render Finished");
        }

        // called by Control to tell it that a control has been removed

        public void ControlRemoved(GLBaseControl other)
        {
            if (currentfocus == other)
                currentfocus = null;
            if (currentmouseover == other)
                currentmouseover = null;
        }

        #endregion

        #region UI - called from wincontrol - due to windows sending events- translate to control

        private void Gc_MouseLeave(object sender, GLMouseEventArgs e)
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
            }
        }

        private void Gc_MouseEnter(object sender, GLMouseEventArgs e)
        {
            Gc_MouseLeave(sender, e);       // leave current

            SetViewScreenCoord(ref e);

            currentmouseover = FindControlOver(e.ScreenCoord);

            if (currentmouseover != null)
            {
                currentmouseover.Hover = true;

                SetControlLocation(ref e, currentmouseover);

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseEnter(e);
            }
        }


        private void Gc_MouseDown(object sender, GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine("GC Mouse down");
            if (currentmouseover != null)
            {
                currentmouseover.FindControlUnderDisplay()?.BringToFront();     // this brings to the front of the z-order the top level element holding this element and makes it visible.

                SetViewScreenCoord(ref e);
                SetControlLocation(ref e, currentmouseover);

                if (currentmouseover.Enabled)
                {
                    currentmouseover.MouseButtonsDown = e.Button;
                    currentmouseover.OnMouseDown(e);
                }

                mousedowninitialcontrol = currentmouseover;
            }
            else
            {
                if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                    this.OnMouseDown(e);
            }
        }

        private void Gc_MouseMove(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);
            //System.Diagnostics.Debug.WriteLine("WLoc {0} VP {1} SLoc {2}", e.WindowLocation, e.ViewportLocation, e.ScreenCoord);

            GlobalMouseMove?.Invoke(e);         // feed global mouse move - coords are form coords

            GLBaseControl c = FindControlOver(e.ScreenCoord);

            if (c != currentmouseover)      // if different, either going active or inactive
            {
               // System.Diagnostics.Debug.WriteLine("WLoc {0} VP {1} SLoc {2} from {3} to {4}", e.WindowLocation, e.ViewportLocation, e.ScreenCoord, currentmouseover?.Name, c?.Name);
                mousedowninitialcontrol = null;

                if (currentmouseover != null)   // for current, its a leave
                {
                    SetControlLocation(ref e, currentmouseover);

                    if (currentmouseover.MouseButtonsDown != GLMouseEventArgs.MouseButtons.None)   // click and drag, can't change control while mouse is down
                    {
                        if (currentmouseover.Enabled)
                            currentmouseover.OnMouseMove(e);

                        return;
                    }

                    currentmouseover.Hover = false;

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseLeave(e);
                }

                currentmouseover = c;

                if (currentmouseover != null)       // now, are we going over a new one?
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc

                    currentmouseover.Hover = true;

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseEnter(e);
                }
                else
                {
                    if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                        this.OnMouseMove(e);
                }
            }
            else
            {
                if (currentmouseover != null)
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseMove(e);
                }
                else
                {
                    if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                        this.OnMouseMove(e);
                }
            }
        }


        private void Gc_MouseUp(object sender, GLMouseEventArgs e)
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

        private void Gc_MouseClick(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);

            if (mousedowninitialcontrol == currentmouseover && currentmouseover != null)        // clicks only occur if mouse is still over initial control
            {
                e.WasFocusedAtClick = currentmouseover == currentfocus;         // record if clicking on a focused item

                SetFocus(currentmouseover);

                if (currentmouseover != null)     // set focus could have force a loss, thru the global focus hook
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc

                    GlobalMouseClick?.Invoke(this, currentmouseover, e);
                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseClick(e);
                }
            }
            else if (currentmouseover == null)        // not over any control, even control display, but still click, (due to screen coord clip space), so send thru the displaycontrol
            {
                SetFocus(null); // should this not be null? Check tbd, it as prev this

                GlobalMouseClick?.Invoke(this, null, e);

                if (this.Enabled)
                    this.OnMouseClick(e);
            }
        }

        private void Gc_MouseDoubleClick(object sender, GLMouseEventArgs e)
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

        private void Gc_MouseWheel(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null && currentmouseover.Enabled)
            {
                SetViewScreenCoord(ref e);
                SetControlLocation(ref e, currentmouseover);    // reset location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseWheel(e);
            }
        }

        // Set up other locations, control locations, relative location, and area, etc

        private void SetViewScreenCoord(ref GLMouseEventArgs e)
        {
            e.ViewportLocation = MatrixCalc.AdjustWindowCoordToViewPortCoord(e.WindowLocation);
            e.ScreenCoord = MatrixCalc.AdjustWindowCoordToScreenCoord(e.WindowLocation);
        }

        private void SetControlLocation(ref GLMouseEventArgs e, GLBaseControl cur)
        {
            e.ControlClientLocation = cur.DisplayControlCoords(true);     // position of control in screencoords
            e.Location = new Point(e.ScreenCoord.X - e.ControlClientLocation.X, e.ScreenCoord.Y - e.ControlClientLocation.Y);
           // System.Diagnostics.Debug.WriteLine("WLoc {0} VLoc {1} SLoc{2} CLoc {3} Loc {4} Control {5}", e.WindowLocation, e.ViewportLocation, e.ScreenCoord, e.ControlClientLocation, e.Location, cur.Name);

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
        }
               
        private void Gc_KeyUp(object sender, GLKeyEventArgs e)
        {
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyUp(e);            // reflect to form

                if ( !e.Handled)                                    // send to control
                    currentfocus.OnKeyUp(e);

            }
        }

        private void Gc_KeyDown(object sender, GLKeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Control keydown " + e.KeyCode + " on " + currentfocus?.Name);
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyDown(e);          // reflect to form

                if ( !e.Handled)                                    // send to control
                    currentfocus.OnKeyDown(e);

            }
        }

        private void Gc_KeyPress(object sender, GLKeyEventArgs e)
        {
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyPress(e);         // reflect to form

                if ( !e.Handled )
                    currentfocus.OnKeyPress(e);                     // send to control
            }
        }

        private void Gc_Resize(object sender)
        {
            //System.Diagnostics.Debug.WriteLine("Call from glwinform with Resize {0}", glwin.Size);
            MatrixCalc.ResizeViewPort(this,glwin.Size);                 // reset the matrix calc view port size from the window size
            SetLocationSizeNI(bounds: MatrixCalc.ScreenCoordMax);         // calls onresize, so subscribers can see resize as well
            OnResize();                                                 // let base classes know
            InvalidateLayout();                                         // and we need to invalidate layout
        }

        private void Gc_Paint(object sender)
        {
            Paint?.Invoke(sender);
        }

        #endregion

        public class GLControlShader : GLShaderPipeline
        {
            public GLControlShader()
            {
                AddVertexFragment(new GLPLVertexShaderTextureScreenCoordWithTriangleStripCoord(), new GLPLBindlessFragmentShaderTextureTriangleStrip(arbbufferid));
            }
        }

        const int arbbufferid = 10;
        const int vertexesperentry = 4;
        private GLWindowControl glwin;
        private GLBuffer vertexes;
        private GLVertexArray vertexarray;
        private Dictionary<GLBaseControl, GLTexture2D> textures;
        private GLBindlessTextureHandleBlock texturebinds;
        private GLRenderableItem ri;
        private IGLProgramShader shader;
        private GLBaseControl currentmouseover = null;
        private GLBaseControl currentfocus = null;
        private GLBaseControl mousedowninitialcontrol = null;       // track where mouse down occurred

    }
}
