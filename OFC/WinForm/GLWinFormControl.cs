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
using System.Drawing;
using System.Windows.Forms;
using GLOFC.Utils;
using OpenTK.Graphics.OpenGL;

namespace GLOFC.WinForm
{
    /// <summary>
    /// This namespace contains a Winform implementation of OFC
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// A win form control version of GLWindowControl
    /// Runs a GLControl from OpenTK, and vectors the GLControl events thru the GLWindowControl standard event interfaces.
    /// Events from GLControl are translated into GLWindowControl events for dispatch.
    /// </summary>
    public class GLWinFormControl : GLWindowControl, IDisposable
    {
        #region Implement GLWindowControl interface

        /// <summary> Get screen rectangle of gl window </summary>
        public Rectangle GLWindowControlScreenRectangle { get { return new Rectangle(glControl.PointToScreen(new Point(0, 0)), glControl.ClientRectangle.Size); } }
        /// <summary> Get mouse position in gl window </summary>
        public Point MousePosition { get { return Control.MousePosition; } }
        /// <summary> Get mouse screen position taking into consideration the viewport/scaling etc</summary>
        public Point MouseWindowPosition { get { var mp = Control.MousePosition; var ctop = glControl.PointToScreen(Point.Empty); return new Point(mp.X - ctop.X, mp.Y - ctop.Y); } }
        /// <summary> Screen width </summary>
        public int Width { get { return glControl.Width; } }
        /// <summary> Screen height</summary>
        public int Height { get { return glControl.Height; } }
        /// <summary> Screen size</summary>
        public Size Size { get { return glControl.Size; } }
        /// <summary> Is Focused </summary>
        public bool Focused { get { return glControl.Focused; } }
        /// <summary> GL profile </summary>
        public GLControlBase.GLProfile Profile { get { return glControl.Profile; } }

        /// <summary> Is GL context ours? </summary>
        public bool IsCurrent() { var ctx = GLStatics.GetContext(); return glControl.Context.IsCurrent && ctx == context; }

        /// <summary> Resize call back </summary>
        public Action<Object> Resize { get; set; } = null;
        /// <summary> Paint call back. ulong is elapsed time in ms </summary>
        public Action<ulong> Paint { get; set; } = null;  
        /// <summary> Mouse down call back </summary>
        public Action<object, GLMouseEventArgs> MouseDown { get; set; } = null;
        /// <summary>Mouse up call back </summary>
        public Action<object, GLMouseEventArgs> MouseUp { get; set; } = null;
        /// <summary> Mouse move call back</summary>
        public Action<object, GLMouseEventArgs> MouseMove { get; set; } = null;
        /// <summary> Mouse enter call back</summary>
        public Action<object, GLMouseEventArgs> MouseEnter { get; set; } = null;
        /// <summary>Mouse leave call back </summary>
        public Action<object, GLMouseEventArgs> MouseLeave { get; set; } = null;
        /// <summary> Mouse click call back</summary>
        public Action<object, GLMouseEventArgs> MouseClick { get; set; } = null;
        /// <summary> Mouse double click call back</summary>
        public Action<object, GLMouseEventArgs> MouseDoubleClick { get; set; } = null;
        /// <summary> Mouse wheel call back</summary>
        public Action<object, GLMouseEventArgs> MouseWheel { get; set; } = null;
        /// <summary> Key down call back</summary>
        public Action<object, GLKeyEventArgs> KeyDown { get; set; } = null;
        /// <summary> Key up call back</summary>
        public Action<object, GLKeyEventArgs> KeyUp { get; set; } = null;
        /// <summary> Key press call back</summary>
        public Action<object, GLKeyEventArgs> KeyPress { get; set; } = null;

        /// <summary> Ensure this context is current </summary>
        public void EnsureCurrentContext() { glControl.MakeCurrent(); }

        /// <summary> Invalidate and redraw </summary>
        public void Invalidate() { glControl.Invalidate(); }

        /// <summary> Set cursor type </summary>
        public void SetCursor(GLWindowControl.GLCursorType t)
        {
            if (t == GLWindowControl.GLCursorType.Wait)
                glControl.Cursor = Cursors.WaitCursor;
            else if (t == GLWindowControl.GLCursorType.EW)
                glControl.Cursor = Cursors.SizeWE;
            else if (t == GLWindowControl.GLCursorType.NS)
                glControl.Cursor = Cursors.SizeNS;
            else if (t == GLWindowControl.GLCursorType.Move)
                glControl.Cursor = Cursors.Hand;
            else if (t == GLWindowControl.GLCursorType.NWSE)
                glControl.Cursor = Cursors.SizeNWSE;
            else
                glControl.Cursor = Cursors.Default;
        }

        /// <summary> Current elapsed time in milliseconds </summary>
        public ulong ElapsedTimems { get { return (ulong)gltime.ElapsedMilliseconds; } }

        #endregion

        #region WinFormControl specific

        /// <summary> Back Color</summary>
        public Color BackColor { get; set; } = Color.Black;

        /// <summary> Screen rectangle </summary>
        public GL4.GLRenderState RenderState { get; set; } = null;

        /// <summary> Set to make it ensure current when mouse/key/paint is called </summary>
        public bool EnsureCurrent { get; set; } = false;         // must be set for multiple opengl windows in one thread

        /// <summary> What buffer to clear each time </summary>
        public ClearBufferMask ClearBuffers {get;set;} = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

        /// <summary>
        /// Make a GL Control and attach to control.
        /// </summary>
        /// <param name="attachcontrol">Control to attach the GL Control to</param>
        /// <param name="mode">TK graphics mode. If null, GraphicsMode.Default is used</param>
        /// <param name="major">Major GL mode (default 1. 1.0 asks for the latest version compatible with 1.0)</param>
        /// <param name="minor">Minor GL mode (default 0)</param>
        /// <param name="flags">Graphic context flags, default is Default</param>
        public GLWinFormControl(Control attachcontrol, OpenTK.Graphics.GraphicsMode mode = null, int major = 1, int minor = 0, 
                        OpenTK.Graphics.GraphicsContextFlags flags = OpenTK.Graphics.GraphicsContextFlags.Default)
        {
            // See https://www.khronos.org/registry/OpenGL/extensions/ARB/WGL_ARB_create_context.txt for information on major and minor versions

            if (mode == null)
                mode = OpenTK.Graphics.GraphicsMode.Default;


            glControl = new GLControlKeyOverride(mode, major, minor,flags);

            glControl.MakeCurrent();        // make sure GLControl is current context selected, in case operating with multiples

            GLStatics.Check();

            glControl.Dock = DockStyle.Fill;
            glControl.BackColor = System.Drawing.Color.Black;
            glControl.Name = "glControl";
            glControl.TabIndex = 0;
            glControl.VSync = true;
            glControl.PreviewKeyDown += Gl_PreviewKeyDown;

            attachcontrol.Controls.Add(glControl);

            glControl.MouseDown += Gc_MouseDown;
            glControl.MouseUp += Gc_MouseUp;
            glControl.MouseMove += Gc_MouseMove;
            glControl.MouseEnter += Gc_MouseEnter;
            glControl.MouseLeave += Gc_MouseLeave;
            glControl.MouseClick += Gc_MouseClick;
            glControl.MouseDoubleClick += Gc_MouseDoubleClick;
            glControl.MouseWheel += Gc_MouseWheel;
            glControl.KeyDown += Gc_KeyDown;
            glControl.KeyUp += Gc_KeyUp;
            glControl.KeyPress += Gc_KeyPress;
            glControl.Resize += Gc_Resize;
            glControl.Paint += GlControl_Paint;
            context = GLStatics.GetContext();
            System.Diagnostics.Debug.WriteLine($"GL Context {context} created");
            gltime.Start();
            

        }

        /// <summary> Close down </summary>
        public void Dispose()
        {
            gltime.Stop();
            Control parent = glControl.Parent;
            parent.Controls.Remove(glControl);
            glControl.Dispose();
            glControl = null;
        }


        #endregion

        #region Implementation

        private void Gl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)    // all keys are for us
        {
            //System.Diagnostics.Debug.WriteLine("Preview KD " + e.KeyCode);
            if ( e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode ==Keys.Tab )
                e.IsInputKey = true;
        }

        private Point FindCursorFormCoords()
        {
            UnsafeNativeMethods.GetCursorPos(out UnsafeNativeMethods.POINT p);
            Point gcsp = glControl.PointToScreen(new Point(0, 0));
            return new Point(p.X - gcsp.X, p.Y - gcsp.Y);
        }

        private void Gc_MouseEnter(object sender, EventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            Point relcurpos = FindCursorFormCoords();
            var ev = new GLMouseEventArgs(relcurpos);
            MouseEnter?.Invoke(this, ev);
        }

        private void Gc_MouseLeave(object sender, EventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            Point relcurpos = FindCursorFormCoords();
            var ev = new GLMouseEventArgs(relcurpos);
            MouseLeave?.Invoke(this, ev);
        }

        private void Gc_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLMouseEventArgs.MouseButtons b = (((e.Button & System.Windows.Forms.MouseButtons.Left) != 0) ? GLMouseEventArgs.MouseButtons.Left : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Middle) != 0) ? GLMouseEventArgs.MouseButtons.Middle : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Right) != 0) ? GLMouseEventArgs.MouseButtons.Right : 0);

            var ev = new GLMouseEventArgs(b, e.Location, e.Clicks,
                            Control.ModifierKeys.HasFlag(Keys.Alt), Control.ModifierKeys.HasFlag(Keys.Control), Control.ModifierKeys.HasFlag(Keys.Shift));
            MouseUp?.Invoke(this, ev);
        }

        private void Gc_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLMouseEventArgs.MouseButtons b = (((e.Button & System.Windows.Forms.MouseButtons.Left) != 0) ? GLMouseEventArgs.MouseButtons.Left : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Middle) != 0) ? GLMouseEventArgs.MouseButtons.Middle : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Right) != 0) ? GLMouseEventArgs.MouseButtons.Right : 0);

            var ev = new GLMouseEventArgs(b, e.Location, e.Clicks,
                            Control.ModifierKeys.HasFlag(Keys.Alt), Control.ModifierKeys.HasFlag(Keys.Control), Control.ModifierKeys.HasFlag(Keys.Shift));
            MouseDown?.Invoke(this, ev);
        }

        private void Gc_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLMouseEventArgs.MouseButtons b = (((e.Button & System.Windows.Forms.MouseButtons.Left) != 0) ? GLMouseEventArgs.MouseButtons.Left : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Middle) != 0) ? GLMouseEventArgs.MouseButtons.Middle : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Right) != 0) ? GLMouseEventArgs.MouseButtons.Right : 0);
            var ev = new GLMouseEventArgs(b, e.Location, e.Clicks,
                            Control.ModifierKeys.HasFlag(Keys.Alt), Control.ModifierKeys.HasFlag(Keys.Control), Control.ModifierKeys.HasFlag(Keys.Shift));
            MouseClick?.Invoke(this, ev);
        }

        private void Gc_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLMouseEventArgs.MouseButtons b = (((e.Button & System.Windows.Forms.MouseButtons.Left) != 0) ? GLMouseEventArgs.MouseButtons.Left : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Middle) != 0) ? GLMouseEventArgs.MouseButtons.Middle : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Right) != 0) ? GLMouseEventArgs.MouseButtons.Right : 0);
            var ev = new GLMouseEventArgs(b, e.Location, e.Clicks,
                            Control.ModifierKeys.HasFlag(Keys.Alt), Control.ModifierKeys.HasFlag(Keys.Control), Control.ModifierKeys.HasFlag(Keys.Shift));
            MouseDoubleClick?.Invoke(this, ev);
        }

        private void Gc_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLMouseEventArgs.MouseButtons b = (((e.Button & System.Windows.Forms.MouseButtons.Left) != 0) ? GLMouseEventArgs.MouseButtons.Left : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Middle) != 0) ? GLMouseEventArgs.MouseButtons.Middle : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Right) != 0) ? GLMouseEventArgs.MouseButtons.Right : 0);
            var ev = new GLMouseEventArgs(b, e.Location, e.Clicks,
                            Control.ModifierKeys.HasFlag(Keys.Alt), Control.ModifierKeys.HasFlag(Keys.Control), Control.ModifierKeys.HasFlag(Keys.Shift));
            MouseMove?.Invoke(this, ev);
        }

        private void Gc_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLMouseEventArgs.MouseButtons b = (((e.Button & System.Windows.Forms.MouseButtons.Left) != 0) ? GLMouseEventArgs.MouseButtons.Left : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Middle) != 0) ? GLMouseEventArgs.MouseButtons.Middle : 0) |
                (((e.Button & System.Windows.Forms.MouseButtons.Right) != 0) ? GLMouseEventArgs.MouseButtons.Right : 0);
            var ev = new GLMouseEventArgs(b,e.Location, e.Clicks, e.Delta,
                        Control.ModifierKeys.HasFlag(Keys.Alt), Control.ModifierKeys.HasFlag(Keys.Control), Control.ModifierKeys.HasFlag(Keys.Shift));
            MouseWheel?.Invoke(this, ev);
        }

        private void Gc_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)      
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
          //  System.Diagnostics.Debug.WriteLine("GLWIN KD " + e.KeyCode);
            GLKeyEventArgs ka = new GLKeyEventArgs(e.Alt, e.Control, e.Shift, e.KeyCode, e.KeyValue, e.Modifiers);
            KeyDown?.Invoke(this, ka);
        }

        private void Gc_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLKeyEventArgs ka = new GLKeyEventArgs(e.Alt, e.Control, e.Shift, e.KeyCode, e.KeyValue, e.Modifiers);
            KeyUp?.Invoke(this, ka);
        }

        private void Gc_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();
            GLKeyEventArgs ka = new GLKeyEventArgs(e.KeyChar);     
            KeyPress?.Invoke(this, ka);
        }

        private void Gc_Resize(object sender, EventArgs e)
        {
            if (!gltime.IsRunning)          // we can get a resize when detaching, ignore it if timer is not going
                return;
            if (EnsureCurrent)
                glControl.MakeCurrent();            // only needed if running multiple GLs windows in same thread
            Resize?.Invoke(this);
        }

        // called by gl window after invalidate. Set up and call painter of objects

        private void GlControl_Paint(object sender, PaintEventArgs e)
        {
            if (EnsureCurrent)
                glControl.MakeCurrent();            // only needed if running multiple GLs windows in same thread

            System.Diagnostics.Debug.Assert(IsCurrent());

            if (RenderState == null)
            {
                RenderState = GL4.GLRenderState.Start();
                GLStatics.Check();
            }

            // set up initial conditions

            GL.Disable(EnableCap.RasterizerDiscard);    // these need to be set correctly to let clear work
            RenderState.Discard = false;            // indicate its off

            GL.DepthMask(true);                     // must be on
            RenderState.WriteDepthBuffer = true;    // indicate its on

            GL.Disable(EnableCap.ScissorTest);      // Scissors off by default at start of each render - must be done for clear. Not in render state
            GL.Disable(EnableCap.StencilTest);      // and stencil

            GL.ColorMask(true, true, true, true);   // also affects clear colour
            RenderState.ColorMasking = GL4.GLRenderState.ColorMask.All;

            // From opengl: The pixel ownership test, the scissor test, dithering, and the buffer writemasks affect the operation of glClear.
            // The scissor box bounds the cleared region.
            // Alpha function, blend function, logical operation, stenciling, texture mapping, and depth-buffering are ignored by glClear. 

            GL.ClearColor(BackColor);
            GL.Clear(ClearBuffers);                 // Clear - see above how some states

            Paint?.Invoke((ulong)gltime.ElapsedMilliseconds);

            glControl.SwapBuffers();
        }

        private System.Diagnostics.Stopwatch gltime = new System.Diagnostics.Stopwatch();
        private GLControlKeyOverride glControl { get; set; }
        private IntPtr context;

        #endregion
    }

    /// <summary>
    /// GLControl specialised to override key handling
    /// </summary>
    public class GLControlKeyOverride : OpenTK.GLControl, GLControlBase
    {
        /// <summary> Constructor </summary>
        public GLControlKeyOverride(OpenTK.Graphics.GraphicsMode m, int major, int minor, OpenTK.Graphics.GraphicsContextFlags flags) : base(m,major,minor,flags)
        {
            Profile = (major >= 4 || (major >= 3 && minor >= 1)) ? GLControlBase.GLProfile.Core : GLControlBase.GLProfile.Compatibility;
        }

        /// <summary> GL profile </summary>
        public GLControlBase.GLProfile Profile { get; private set; }

        /// <summary> Override key handling </summary>
        protected override bool IsInputKey(Keys keyData)    // disable normal windows control change
        {
            //System.Diagnostics.Debug.WriteLine("Is input key" + keyData);
            if (keyData == Keys.Tab || keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
                return true;
            else
            {
                var iik = base.IsInputKey(keyData);
                //System.Diagnostics.Debug.WriteLine("base Is input key" + keyData + " " + iik);
                return iik;
            }
        }
    }
}

