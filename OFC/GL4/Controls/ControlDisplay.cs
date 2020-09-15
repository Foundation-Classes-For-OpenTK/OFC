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

        public override bool Focused { get { return glwin.Focused; } }          // override focused to report if whole window is focused.

        public Action<GLControlDisplay, GLBaseControl, GLBaseControl> GlobalFocusChanged { get; set; } = null;     // subscribe to get any focus changes (from old to new, may be null)

        public Action<GLMouseEventArgs> GlobalMouseMove { get; set; } = null;     // subscribe to get any movement changes

        // from Control, override the Mouse* and Key* events

        public new Action<Object> Paint { get; set; } = null;                   //override to get a paint event

        // items is for store, win is the driving window to which we hook to get events

        public GLControlDisplay(GLItemsList items, GLWindowControl win) : base("displaycontrol", new Rectangle(0, 0, win.Width, win.Height))
        {
            glwin = win;

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

        public override void Add(GLBaseControl other)           
        {
            System.Diagnostics.Debug.Assert(other is GLVerticalScrollPanel == false, "GLVerticalScrollPanel must not be a child of GLForm");
            textures[other] = new GLTexture2D();                // we make a texture per top level control to render with
            other.MakeLevelBitmap(Math.Max(1,other.Width),Math.Max(1,other.Height));    // ensure we make a bitmap
            base.Add(other);
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

        public void SetFocus(GLBaseControl ctrl)    // null to clear focus
        {
            System.Diagnostics.Debug.WriteLine("Focus to " + ctrl?.Name);

            if (ctrl == currentfocus)
                return;

            GLBaseControl oldfocus = currentfocus;
            GLBaseControl newfocus = (ctrl != null && ctrl.Enabled && ctrl.Focusable) ? ctrl : null;

            GlobalFocusChanged?.Invoke(this, oldfocus, newfocus);

            if (currentfocus != null)
            {
                currentfocus.OnFocusChanged(false, newfocus);
                currentfocus = null;
            }
            
            if (newfocus != null)
            {
                currentfocus = ctrl;
                currentfocus.OnFocusChanged(true, oldfocus);
            }
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
        protected override bool ChildLocationChanged(GLBaseControl child)
        {
            UpdateVertexTexturePositions(false);
            return true;
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
            //System.Diagnostics.Debug.WriteLine("Form redraw start");
            //DebugWhoWantsRedraw();

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
                            textures[c].LoadBitmap(c.LevelBitmap);  // and update texture unit with new bitmap
                            //float[] p = textures[c].GetTextureImageAsFloats(end:100);
                        }
                    }
                }

                shader.Start();
                ri.Bind(currentstate, shader, null);        // binds VA AND the element buffer
                ri.Render();                                // draw using primitive restart on element index buffer with bindless textures
                shader.Finish();
                GL.UseProgram(0);           // final clean up
                GL.BindProgramPipeline(0);
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

            //System.Diagnostics.Debug.WriteLine("Form redraw end");
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

                var mouseleaveev = new GLMouseEventArgs(e.Location);

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseLeave(mouseleaveev);

                currentmouseover = null;
            }
        }

        private void Gc_MouseEnter(object sender, GLMouseEventArgs e)
        {
            Gc_MouseLeave(sender, e);       // leave current

            currentmouseover = FindControlOver(e.Location);

            if (currentmouseover != null)
            {
                currentmouseover.Hover = true;

                AdjustLocationToControl(ref e, currentmouseover, e.Location);

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseEnter(e);
            }
        }


        private void Gc_MouseDown(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null)
            {
                currentmouseover.FindControlUnderDisplay()?.BringToFront();     // this brings to the front of the z-order the top level element holding this element and makes it visible.

                AdjustLocationToControl(ref e, currentmouseover, e.Location);

                if (currentmouseover.Enabled)
                {
                    currentmouseover.MouseButtonsDown = e.Button;
                    currentmouseover.OnMouseDown(e);
                }

                mousedowninitialcontrol = currentmouseover;
            }
        }

        private void Gc_MouseMove(object sender, GLMouseEventArgs e)
        {
            GLBaseControl c = FindControlOver(e.Location);      // e.location are form co-ords

            Point orgxy = e.Location;

            GlobalMouseMove?.Invoke(e);         // feed global mouse move - coords are form coords

            if (c != currentmouseover)      // if different, either going active or inactive
            {
                mousedowninitialcontrol = null;

                if (currentmouseover != null)   // for current, its a leave
                {
                    AdjustLocationToControl(ref e, currentmouseover, orgxy);

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
                    AdjustLocationToControl(ref e, currentmouseover, orgxy);    // reset location etc

                    currentmouseover.Hover = true;

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseEnter(e);
                }
            }
            else
            {
                if (currentmouseover != null)
                {
                    AdjustLocationToControl(ref e, currentmouseover, orgxy);    // reset location etc

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseMove(e);
                }
            }
        }


        private void Gc_MouseUp(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null)
            {
                currentmouseover.MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;

                AdjustLocationToControl(ref e, currentmouseover, e.Location);    // reset location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseUp(e);
            }

            mousedowninitialcontrol = null;
        }

        private void Gc_MouseClick(object sender, GLMouseEventArgs e)
        {
            if (mousedowninitialcontrol == currentmouseover && currentmouseover != null )        // clicks only occur if mouse is still over initial control
            {
                SetFocus(currentmouseover);
                AdjustLocationToControl(ref e, currentmouseover, e.Location);    // reset location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseClick(e);
            }
        }

        private void Gc_MouseWheel(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null && currentmouseover.Enabled)
            {
                AdjustLocationToControl(ref e, currentmouseover, e.Location);    // reset location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseWheel(e);
            }
        }

        private void AdjustLocationToControl(ref GLMouseEventArgs e, GLBaseControl cur, Point mouseloc)
        {
            e.ControlLocation = cur.DisplayControlCoords(true);
            e.Location = new Point(mouseloc.X - e.ControlLocation.X, mouseloc.Y - e.ControlLocation.Y);
            //            System.Diagnostics.Debug.WriteLine("Control " + cur.Name + " " + e.ControlLocation + " " + e.Location);

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
                    currentfocus.FindForm()?.OnKeyUp(e);        // reflect to form

                if ( !e.Handled)
                    currentfocus.OnKeyUp(e);

            }
        }

        private void Gc_KeyDown(object sender, GLKeyEventArgs e)
        {
        //    System.Diagnostics.Debug.WriteLine("Control keydown on " + currentfocus?.Name);
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyDown(e);        // reflect to form

                if ( !e.Handled )
                    currentfocus.OnKeyDown(e);

            }
        }

        private void Gc_KeyPress(object sender, GLKeyEventArgs e)
        {
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyPress(e);        // reflect to form

                if ( !e.Handled )
                    currentfocus.OnKeyPress(e);
            }
        }

        private void Gc_Resize(object sender)
        {
            SetLocationSizeNI(size: glwin.Size);        // calls onresize, so subscribers can see resize as well
            InvalidateLayout(); // and we need to invalidate layout
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
