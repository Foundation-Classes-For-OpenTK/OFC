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

using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    // This control display needs a GLWindowControl to get events from
    // it in turn passes on those events as its a GLWindowControl, adjusted to the controls in the window
    // It render the control display

    public class GLControlDisplay : GLBaseControl, GLWindowControl
    {
        #region Public IF

        public bool RequestRender { get; set; } = false;                        // set whenever anything is invalidated by a control.
        public void ReRender() { RequestRender = true; }

        public Point MousePosition { get { return glwin.MousePosition; } }

        public override bool Focused { get { return glwin.Focused; } }          // override focused to report if whole window is focused.

        public new Action<Object, ulong> Paint { get; set; } = null;             // override to get a paint event, ulong is elapsed time in ms

        public GLMatrixCalc MatrixCalc { get; set; }

        public Rectangle GLWindowControlScreenRectangle { get { return glwin.GLWindowControlScreenRectangle; } }

        public Point MouseWindowPosition {  get { return glwin.MouseWindowPosition; } }

        public ulong ElapsedTimems { get { return glwin.ElapsedTimems; } }

        public void EnsureCurrentContext()
        {
            glwin.EnsureCurrentContext();
        }

        // need items, need a window to attach to, need a MC
        public GLControlDisplay(GLItemsList items, GLWindowControl win, GLMatrixCalc mc,
                                    bool depthtest = true,          // do depth testing or not
                                    float startz = 0.001f,          // z for the deepest window (only will apply if depth testing
                                    float deltaz = 0.001f,           // delta betwwen them
                                    int arbbufferid = 10
                                ) : base("displaycontrol", new Rectangle(0, 0, mc.ScreenCoordMax.Width, mc.ScreenCoordMax.Height))
        {
            glwin = win;
            MatrixCalc = mc;

            this.items = items;

            vertexes = items.NewBuffer();

            vertexarray = items.NewVertexArray();   
            vertexes.Bind(vertexarray,0, 0, vertexesperentry * sizeof(float));             // bind to 0, from 0, 2xfloats. Must bind after vertexarray is made as its bound during construction

            vertexarray.Attribute(0, 0, vertexesperentry, OpenTK.Graphics.OpenGL4.VertexAttribType.Float); // bind 0 on attr 0, 2 components per vertex

            GLRenderState rc = GLRenderState.Tri();
            rc.PrimitiveRestart = 0xff;
            rc.DepthTest = depthtest;

            this.startz = startz;
            this.deltaz = deltaz;

            ri = new GLRenderableItem(PrimitiveType.TriangleStrip,rc, 0, vertexarray);     // create a renderable item
            ri.CreateRectangleElementIndexByte(items.NewBuffer(), 255 / 5);             // note this limits top level controls number to 255/5.
            ri.DrawCount = 0;                               // nothing to draw at this point

            shader = new GLShaderPipeline( new GLPLVertexShaderTextureScreenCoordWithTriangleStripCoord(), new GLPLFragmentShaderBindlessTexture(arbbufferid,true, discardiftransparent:true));
            items.Add(shader);

            textures = new Dictionary<GLBaseControl, GLTexture2D>();
            texturebinds = items.NewBindlessTextureHandleBlock(arbbufferid);

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

        public void SetCursor(GLCursorType t)
        {
            glwin.SetCursor(t);
        }

        // On control add, to display, we need to do more work to set textures up

        public override void Add(GLBaseControl other, bool atback = false)           
        {
            System.Diagnostics.Debug.Assert(other is GLVerticalScrollPanel == false, "GLVerticalScrollPanel must not be a child of GLForm");
            textures[other] = items.NewTexture2D(null);                // we make a texture per top level control to render with
            other.MakeLevelBitmap(Math.Max(1,other.Width),Math.Max(1,other.Height));    // ensure we make a bitmap
            base.Add(other,atback);
        }

        // on a recursive layout, we need to adjust the texture positions of the sub controls
        protected override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();
            UpdateVertexTexturePositions(true);
        }

        // if we change the z order, we need to update vertex list, keyed to z order
        // and the textures, since the bindless IDs are written in z order        
        public override bool BringToFront(GLBaseControl other)  
        {                                                       
            if (!base.BringToFront(other))                  // if not already at front
            {
                UpdateVertexTexturePositions(true);         // we changed z order, update
                return false;
            }
            else
                return true;
        }

        // call this during your Paint to render.

        public void Render(GLRenderState currentstate, ulong ts)
        {
            //System.Diagnostics.Debug.WriteLine("Render");

            NeedRedraw = false;
            RequestRender = false;

            if (ControlsIZ.Count > 0)       // only action if children present
            {
                bool altscalechanged = false;

                foreach (var c in ControlsIZ)
                {
                    if (c.Visible)
                    {
                        //System.Diagnostics.Debug.WriteLine("Draw " + c.Name);
                        bool redrawn = c.Redraw(null, new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 0, 0), false);      // see if redraw done

                        if (redrawn)
                        {
                            textures[c].CreateLoadBitmap(c.LevelBitmap, SizedInternalFormat.Rgba8);  // and update texture unit with new bitmap
                            //float[] p = textures[c].GetTextureImageAsFloats(end:100);
                        }

                        if ( c.TopLevelControlUpdate )
                        {
                            c.TopLevelControlUpdate = false;
                            altscalechanged = true;
                        }
                    }
                }

                if (altscalechanged)
                    UpdateVertexTexturePositions(false);        // we need to update..

                GLScissors.SetToScreenCoords(0, MatrixCalc);
                shader.Start(MatrixCalc);
                ri.Bind(currentstate, shader, MatrixCalc);        // binds VA AND the element buffer
                ri.Render();                                // draw using primitive restart on element index buffer with bindless textures
                shader.Finish();
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

        // call this during system tick to run the animations

        public new void Animate(ulong ts)
        {
            var controls = new List<GLBaseControl>(ControlsIZ);     // animators may close/remove the control, so we need to take a copy so we have a collection which does not change.
            foreach (var c in controls)
            {
                c.Animate(ts);
            }
        }

        #endregion

        #region Implementation

        // override remove control since we need to know if to remove texture

        protected override void RemoveControl(GLBaseControl child, bool dispose, bool removechildren)
        {
            base.RemoveControl(child, dispose, removechildren); // remove

            if (ControlsZ.Contains(child))      // if its our child
            {
                items.Dispose(textures[child]);  // and delete textures and remove from item list
                textures.Remove(child);
            }
        }

        // override base control invalidate, and call it, and also pass the invalidate to the gl window control we have

        public override void Invalidate()           
        {
            base.Invalidate();
            glwin.Invalidate();
        }

        // overriding this indicates all we have to do if child location changes is update the vertex positions, and that we have dealt with it

        protected override bool InvalidateDueToLocationChange(GLBaseControl child)
        {
            //System.Diagnostics.Debug.WriteLine("Control display location change");
            UpdateVertexTexturePositions(false);
            return false;       // we don't need to invalidate due to just a location change, we can handle that without it
        }

        // override this to provide translation between form co-ords and viewport/screen coords

        protected override void SetViewScreenCoord(ref GLMouseEventArgs e)
        {
            e.ViewportLocation = MatrixCalc.AdjustWindowCoordToViewPortCoord(e.WindowLocation);
            e.ScreenCoord = MatrixCalc.AdjustWindowCoordToScreenCoord(e.WindowLocation);
        }

        // update vertexes, maybe update textures

        private void UpdateVertexTexturePositions(bool updatetextures)        
        {
            if (ControlsZ.Count > 0)            // may end up with nothing to draw, in which case, don't update anything
            {
                vertexes.AllocateBytes(ControlsZ.Count * sizeof(float) * vertexesperentry * 4);
                vertexes.StartWrite(0, vertexes.Length);

                float z = startz;      // we place it in clip space at a z near
                int visible = 0;

                List<IGLTexture> tlist = new List<IGLTexture>();

                foreach (var c in ControlsIZ)       // we paint in IZ order, and we set the Z (bigger is more in the back) from a notional X to 0 so the depth test works
                {
                    if (c.Visible)  // must be visible to be added to vlist
                    {
                      //  System.Diagnostics.Debug.WriteLine($"Update texture {c.Name} with {c.Opacity}");
                        float[] a;
                        float w = c.Opacity;

                        if (c.ScaleWindow == null)
                        {
                         
                            a = new float[] {   c.Left, c.Top, z, w,
                                                        c.Left, c.Bottom , z, w,
                                                        c.Right, c.Top, z, w,
                                                        c.Right, c.Bottom , z, w,
                                                 };
                        }
                        else
                        {
                            float right = c.Left + c.Width * c.ScaleWindow.Value.Width;
                            float bot = c.Top + c.Height * c.ScaleWindow.Value.Height;
                            a = new float[] { c.Left, c.Top, z, w,
                                              c.Left, bot, z, w,
                                              right, c.Top, z, w,
                                              right, bot, z, w
                            };
                        }

                        vertexes.Write(a);
                        z -= deltaz;
                        visible++;

                        if (updatetextures)
                        {
                            if (textures[c].Id < 0 || textures[c].Width != c.LevelBitmap.Width || textures[c].Height != c.LevelBitmap.Height)      // if layout changed bitmap
                            {
                                textures[c].CreateOrUpdateTexture(c.Width, c.Height, SizedInternalFormat.Rgba8);   // and make a texture, this will dispose of the old one 
                            }

                          //  System.Diagnostics.Debug.WriteLine($"Update tlist for {c.Name} with {c.Opacity}");
                            tlist.Add(textures[c]);     // need to have them in the same order as the client rectangles
                        }
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine($"{c.Name} not visible so not adding");
                    }
                }

                vertexes.StopReadWrite();
                GLOFC.GLStatics.Check();

                if (tlist.Count > 0)     // only if we had visible ones
                {
                   // System.Diagnostics.Debug.WriteLine($"texture binds write");
                    texturebinds.WriteHandles(tlist.ToArray()); // write texture handles to the buffer..  written in iz order
                    GLMemoryBarrier.All();
                }

                ri.DrawCount = (visible>0) ? (visible * 5 - 1) : 0;    // 4 vertexes per rectangle, 1 restart
            }
            else
                ri.DrawCount = 0;           // and set count to zero.

            RequestRender = true;
        }

        // window is resizing
       
        private void Gc_Resize(object sender)
        {
            //System.Diagnostics.Debug.WriteLine("Call from glwinform with Resize {0}", glwin.Size);
            MatrixCalc.ResizeViewPort(this,glwin.Size);                 // reset the matrix calc view port size from the window size
            SetNI(size: MatrixCalc.ScreenCoordMax);         // calls onresize, so subscribers can see resize as well
            OnResize();                                                 // let base classes know
            InvalidateLayout();                                         // and we need to invalidate layout
        }

        // window is painting - hooked up to GLWindowControl Paint function. ts is elapsed time in ms.
        private void Gc_Paint(object sender,ulong ts)
        {
            Paint?.Invoke(sender,ts);
        }

        const int vertexesperentry = 4;
        private GLItemsList items;
        private GLWindowControl glwin;
        private GLBuffer vertexes;
        private GLVertexArray vertexarray;
        private Dictionary<GLBaseControl, GLTexture2D> textures;
        private GLBindlessTextureHandleBlock texturebinds;
        private GLRenderableItem ri;
        private IGLProgramShader shader;
        private float startz, deltaz;

        #endregion

    }
}
