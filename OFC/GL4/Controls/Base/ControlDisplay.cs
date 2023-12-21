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

using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Fragment;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GLOFC.GL4.Textures;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Control display is the top level windows covering the open gl surface
    /// Holds all other controls as children. See control examples for how to instance the class
    /// </summary>

    public class GLControlDisplay : GLBaseControl
    {
        #region Implement similar interface to GLWindowControl 

        /// <inheritdoc cref="GLOFC.GLWindowControl.GLWindowControlScreenRectangle"/>
        public Rectangle GLWindowControlScreenRectangle { get { return glwin.GLWindowControlScreenRectangle; } }
        /// <inheritdoc cref="GLOFC.GLWindowControl.MousePosition"/>
        public Point MousePosition { get { return glwin.MousePosition; } }
        /// <inheritdoc cref="GLOFC.GLWindowControl.MouseWindowPosition"/>
        public Point MouseWindowPosition { get { return glwin.MouseWindowPosition; } }

        // Width,Height,Size,Focused implemented by GLBaseControl

        /// <summary> Is context current to opengl </summary>
        public bool IsContextCurrent()  {  return glwin.IsContextCurrent();  }

        // Resize implemented by GLBaseControl, as is Key/Mouse events

        /// <summary> Paint call back. ulong is elapsed time in ms </summary>
        public new Action<ulong> Paint { get; set; } = null;             // override to get a paint event, ulong is elapsed time in ms

        /// <summary> Invalidate the window </summary>
        public override void Invalidate()   {base.Invalidate(); glwin.Invalidate(); }

        /// <summary> Ensure this is the current context </summary>
        public void EnsureCurrentContext() { glwin.EnsureCurrentContext();  }

        /// <summary> Get elapsed time in ms </summary>
        public ulong ElapsedTimems { get { return glwin.ElapsedTimems; } }

        #endregion

        #region Public IF

        /// <summary> Request a render.  Set whenever anything is invalidated by a control.</summary>
        public bool RequestRender { get; private set; } = false;
        /// <summary> Request a render.  Set whenever anything is invalidated by a control.</summary>
        public void ReRender() { RequestRender = true; }

        /// <summary> Is the control display gl window focused? </summary>
        public override bool Focused { get { return glwin.Focused; } }

        /// <summary> Matrix Calc for the system </summary>
        public GLMatrixCalc MatrixCalc { get; set; }

        /// <summary>
        /// Construct a control display
        /// </summary>
        /// <param name="items">Items to store GL data to</param>
        /// <param name="win">GLWindowControl to hook to</param>
        /// <param name="mc">Matrix Calc to use</param>
        /// <param name="depthtest">Enable depth test</param>
        /// <param name="startz">Start Z for nearest top level window</param>
        /// <param name="deltaz">Delta Z between each top level window</param>
        /// <param name="arbbufferid">ARB buffer to use for texture bindless storage</param>
        public GLControlDisplay(GLItemsList items, GLWindowControl win, GLMatrixCalc mc,
                                    bool depthtest = true,          // do depth testing or not
                                    float startz = 0.001f,          // z for the deepest window (only will apply if depth testing
                                    float deltaz = 0.001f,           // delta betwwen them
                                    int arbbufferid = 10
                                ) : base("displaycontrol", new Rectangle(0, 0, mc.ScreenCoordMax.Width, mc.ScreenCoordMax.Height))
        {
            glwin = win;
            MatrixCalc = mc;
            context = GLStatics.GetContext();

            this.items = items;

            vertexes = items.NewBuffer();

            vertexarray = items.NewVertexArray();
            vertexes.Bind(vertexarray, 0, 0, vertexesperentry * sizeof(float));             // bind to 0, from 0, 2xfloats. Must bind after vertexarray is made as its bound during construction

            vertexarray.Attribute(0, 0, vertexesperentry, OpenTK.Graphics.OpenGL4.VertexAttribType.Float); // bind 0 on attr 0, 2 components per vertex

            GLRenderState rc = GLRenderState.Tri();
            rc.PrimitiveRestart = 0xff;
            rc.DepthTest = depthtest;

            this.startz = startz;
            this.deltaz = deltaz;

            ri = new GLRenderableItem(PrimitiveType.TriangleStrip, rc, 0, vertexarray);     // create a renderable item
            ri.CreateRectangleElementIndexByte(items.NewBuffer(), 255 / 5);             // note this limits top level controls number to 255/5.
            ri.DrawCount = 0;                               // nothing to draw at this point

            shader = new GLShaderPipeline(new GLPLVertexShaderScreenTexture(), new GLPLFragmentShaderBindlessTexture(arbbufferid, true, discardiftransparent: true));
            items.Add(shader);

            textures = new Dictionary<GLBaseControl, GLTexture2D>();
            size = new Dictionary<GLBaseControl, Size>();
            visible = new Dictionary<GLBaseControl, bool>();

            texturebinds = items.NewBindlessTextureHandleBlock(arbbufferid);
        }

        /// <summary> Hook to GLWindowsControl </summary>
        public void Hook()
        { 
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

            ClearSuspendLayout();
        }

        /// <summary>
        /// Render controls. Called by an override to GLWinFormControl Paint or other method.
        /// </summary>
        /// <param name="currentstate">Render state</param>
        /// <param name="ts">Time stamp from Paint</param>
        public void Render(GLRenderState currentstate, ulong ts)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext() && IsContextCurrent(), "Context incorrect");

            //System.Diagnostics.Debug.WriteLine("Render");

            ClearRedraw();
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
                           // System.Diagnostics.Debug.WriteLine($"texture changed refresh GL {c.Name} {c.Size} {c.LevelBitmap.Size}");
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
                    UpdateVertexPositionsTextures();        // we need to update..

                GLScissors.SetToScreenCoords(0, MatrixCalc);
                shader.Start(MatrixCalc);
                ri.Bind(currentstate, shader, MatrixCalc);        // binds VA AND the element buffer
                ri.Render();                                // draw using primitive restart on element index buffer with bindless textures
                shader.Finish();
                GL.UseProgram(0);           // final clean up
                GL.BindProgramPipeline(0);
                GLScissors.Disable(0);
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);

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

        /// <summary>
        /// Performs window animation. Call during a system tick or paint callback
        /// </summary>
        /// <param name="ts">Timestamp, obtained from window control</param>
        public new void Animate(ulong ts)
        {
            var controls = new List<GLBaseControl>(ControlsIZ);     // animators may close/remove the control, so we need to take a copy so we have a collection which does not change.
            foreach (var c in controls)
            {
                c.Animate(ts);
            }
        }

        #endregion

        #region Overrides

        // override base control invalidate, and call it, and also pass the invalidate to the gl window control
        // override this, so that we see all invalidations layouts to us and what child required it.
        // then we just layout and size the child only, so the rest of them, unaffected by the way displaycontrol handles textures, do not get invalidated
        // unless we see there is compound docking on, in which case we need to use a full PerformLayout
        // may be called with null child, meaning its a remove/detach
        // it may be called due to a property in displaycontrol changing (Font),
        // and we check the vertex/positions/sizes to make sure everything is okay
        private protected override void InvalidateLayout(GLBaseControl dueto)
        {
            //System.Diagnostics.Debug.WriteLine($"Display control invalidate layout due to {dueto?.Name} {suspendLayoutCount}");

            glwin.Invalidate();

            int docked = ControlsZ.Where(x => x.Dock >= DockingType.Left).Count();      // how many have a compound docking type

            if (dueto == this || docked>0)   // if change due to display control property, or we have a docking sitation
            {
                PerformLayout();    // full layout on all children
            }
            else
            {
                if (dueto != null)  // if not a remove, layout and size on child only
                    dueto.PerformLayoutAndSize();
            }

            UpdateVertexPositionsTextures();        // need to at least update vertexes, maybe textures 
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.BringToFront(GLBaseControl)"/>
        public override bool BringToFront(GLBaseControl other)
        {
            // if we change the z order, we need to update vertex list, keyed to z order
            // and the textures, since the bindless IDs are written in z order        

            if (!base.BringToFront(other))                  // if not already at front
            {
                UpdateVertexPositionsTextures(true);        // we changed z order, update, and force the texture rewrite since order has changed
                return false;
            }
            else
                return true;
        }


        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Add(GLBaseControl, bool)"/>
        public override void Add(GLBaseControl child, bool atback = false)
        {
            // On control add, to display, we need to do more work to set textures up and note the bitmap size
            // textures will be updated on invalidatelayout
          
            System.Diagnostics.Debug.Assert(child is GLScrollPanel == false, "GLScrollPanel must not be a child of GLForm");

            textures[child] = items.NewTexture2D(null);                // we make a texture per top level control to render with
            size[child] = child.Size;
            visible[child] = child.Visible;

            child.MakeLevelBitmap(Math.Max(1, child.Width), Math.Max(1, child.Height));    // ensure we make a bitmap

            base.Add(child, atback);
        }

        /// <summary>
        /// Call to add a modal form. Only controls in the modal form are active, other controls are inert
        /// </summary>
        /// <param name="form">The form to make modal</param>
       
        public void AddModalForm(GLForm form)
        {
            Add(form, false);
            modalforms.Add(form);
            //System.Diagnostics.Debug.WriteLine($"Add modal form");
        }

        /// <summary>
        /// Do not normally call this as its automatically called from Form.ForceClose (or via Close) 
        /// Remove from from modal list
        /// </summary>
        /// <param name="form">The form being removed</param>
        public void RemoveModalForm(GLForm form)
        {
            if (modalforms.Count > 0)
            {
                if (modalforms.Last() == form)
                {
                    if (modalforms.Count > 1 && form.IsThisOrChildrenFocused())        // if it has the focus
                    {
                        var focusform = modalforms[modalforms.Count - 2];
                        //System.Diagnostics.Debug.WriteLine($"Modal form {form.Name} has a focus, pass back to {focusform.Name}");
                        focusform.SetFocus();
                    }

                    modalforms.RemoveAt(modalforms.Count - 1);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Tried to remove modal form {form.Name} but not at end - coding error");
                }
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine($"No modal forms active");
            }
        }

        /// <summary>
        /// Is modal forms active?
        /// </summary>
        /// <returns>true if any modal forms are active</returns>
        public bool ModalFormsActive { get { return modalforms.Count > 0; } } 

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.RemoveControl(GLBaseControl, bool, bool)"/>
        internal override void RemoveControl(GLBaseControl child, bool dispose, bool removechildren)
        {
            bool ourchild = ControlsZ.Contains(child);      // record before removecontrol updates controlz list

            base.RemoveControl(child, dispose, removechildren); // remove

            if (ourchild)      // if its our child
            {
                items.Dispose(textures[child]);  // and delete textures and remove from item list
                textures.Remove(child);
                visible.Remove(child);
                size.Remove(child);
                UpdateVertexPositionsTextures(true);        // make sure the list is right, controls list has changed
            }
        }

        // override this to provide translation between form co-ords and viewport/screen coords
        private protected override void SetViewScreenCoord(ref GLMouseEventArgs e)
        {
            e.ViewportLocation = MatrixCalc.AdjustWindowCoordToViewPortCoord(e.WindowLocation);
            e.ScreenCoord = MatrixCalc.AdjustWindowCoordToScreenCoord(e.WindowLocation);
        }

        // update vertexes
        // make a bitmap if the current child size is different to the stored rectangle size 
        // create or update the texture if the texture was never made, or its not the right size
        // write the bindless texture list if we updated the texture OR we are forced to
        // update the vertex buffer with positions
        private void UpdateVertexPositionsTextures(bool forceupdatetextures = false)        
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext() && IsContextCurrent(), "Context incorrect");

            if (ControlsZ.Count > 0)            // may end up with nothing to draw, in which case, don't update anything
            {
                vertexes.AllocateBytes(ControlsZ.Count * sizeof(float) * vertexesperentry * 4);
                vertexes.StartWrite(0, vertexes.Length);

                float z = startz;      // we place it in clip space at a z near
                int controlsvisible = 0;

                List<IGLTexture> tlist = new List<IGLTexture>();
                bool changedtlist = forceupdatetextures;

                foreach (var c in ControlsIZ)       // we paint in IZ order, and we set the Z (bigger is more in the back) from a notional X to 0 so the depth test works
                {
                    if ( c.Visible != visible[c])   // if visible list has changed, then we must rewrite the texture binds list as we don't write vertexes for invisibles
                    {
                        visible[c] = c.Visible;
                        changedtlist = true;
                    }

                    if (c.Visible)  // must be visible to be added to vlist
                    {
                        //System.Diagnostics.Debug.WriteLine($"Update texture {c.Name} with {c.Opacity}");
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
                        controlsvisible++;

                        if (size[c] != c.Size)          // if level bitmap changed size, remake bitmap
                        {
                           // System.Diagnostics.Debug.WriteLine($"Displaycontrol {c.Name} windows size is not texture size {size[c]} remake bitmap");
                            c.MakeLevelBitmap(c.Width, c.Height);
                            size[c] = c.Size;
                        }

                        if (textures[c].Id < 0 || textures[c].Width != c.LevelBitmap.Width || textures[c].Height != c.LevelBitmap.Height)      // if layout changed bitmap
                        {
                           // System.Diagnostics.Debug.WriteLine($"Displaycontrol {c.Name} make new texture of {c.Size} {c.LevelBitmap.Size}");
                            textures[c].CreateOrUpdateTexture(c.Width, c.Height, SizedInternalFormat.Rgba8);   // and make a texture, this will dispose of the old one 
                            changedtlist = true;
                        }

                            //  System.Diagnostics.Debug.WriteLine($"Update tlist for {c.Name} with {c.Opacity}");
                        tlist.Add(textures[c]);     // need to have them in the same order as the client rectangles
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine($"{c.Name} not visible so not adding");
                    }
                }

                vertexes.StopReadWrite();
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);

                if (changedtlist)     // update if anything has changed
                {
                  //  System.Diagnostics.Debug.WriteLine($"Displaycontrol textures changed, write list");
                    texturebinds.WriteHandles(tlist.ToArray()); // write texture handles to the buffer..  written in iz order
                    GLMemoryBarrier.All();
                }

                ri.DrawCount = (controlsvisible>0) ? (controlsvisible * 5 - 1) : 0;    // 4 vertexes per rectangle, 1 restart
            }
            else
                ri.DrawCount = 0;           // and set count to zero.

            RequestRender = true;
        }

        /// <summary> Do not use on control display </summary>
        [Obsolete("Do not use this call on displaycontrol", true)]
        public new void SuspendLayout() { }

        /// <summary> Do not use on control display </summary>
        [Obsolete("Do not use this call on displaycontrol", true)]
        public new void ResumeLayout() {  }

        /// <summary> Do not use on control display </summary>
        [Obsolete("Do not use this call on displaycontrol", true)]
        public new void Layout(ref Rectangle parentarea) {  }

        /// <summary> Do not use on control display </summary>
        [Obsolete("Do not use this call on displaycontrol", true)]
        internal new void SetPos(int left, int top, int width, int height) {  }

        /// <summary> Internal interface do not use </summary>
        public void SetCursor(GLWindowControl.GLCursorType t)
        {
            if (t != lastcursor)
            {
                glwin.SetCursor(t);
                lastcursor = t;
                //System.Diagnostics.Debug.WriteLine($"Cursor to {t}");
            }
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
        private void Gc_Paint(ulong ts)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext() && IsContextCurrent(), "Context incorrect");
            Paint?.Invoke(ts);
        }

        const int vertexesperentry = 4;
        private GLItemsList items;
        private GLWindowControl glwin;
        private GLBuffer vertexes;
        private GLVertexArray vertexarray;
        private Dictionary<GLBaseControl, GLTexture2D> textures;
        private Dictionary<GLBaseControl, Size> size;
        private Dictionary<GLBaseControl, bool> visible;
        private GLBindlessTextureHandleBlock texturebinds;
        private GLRenderableItem ri;
        private IGLProgramShader shader;
        private float startz, deltaz;
        private GLWindowControl.GLCursorType lastcursor = GLWindowControl.GLCursorType.Normal;
        private IntPtr context;

        #endregion

    }
}
