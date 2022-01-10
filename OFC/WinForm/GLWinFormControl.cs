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
    // a win form control version of GLWindowControl
    // runs a GLControl from OpenTK, and vectors the GLControl events thru the GLWindowControl standard event interfaces.
    // events from GLControl are translated into GLWindowControl events for dispatch.

    public class GLWinFormControl : GLWindowControl, IDisposable
    {
        public Color BackColor { get; set; } = Color.Black;
        public int Width { get { return glControl.Width; } }
        public int Height { get { return glControl.Height; } }
        public Size Size { get { return glControl.Size; } }
        public bool Focused { get { return glControl.Focused; } }
        public Rectangle GLWindowControlScreenRectangle { get { return new Rectangle(glControl.PointToScreen(new Point(0, 0)), glControl.ClientRectangle.Size); } }        
        public Point MousePosition { get { return Control.MousePosition; } }  // mouse position on all screens
        public Point MouseWindowPosition { get { var mp = Control.MousePosition; var ctop = glControl.PointToScreen(Point.Empty); return new Point(mp.X - ctop.X, mp.Y - ctop.Y); } }
        public GL4.GLRenderState RenderState { get; set; } = null;

        public bool EnsureCurrent { get; set; } = false;         // must be set for multiple opengl windows in one thread

        // what buffers to clear
        public ClearBufferMask ClearBuffers {get;set;} = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

        // use EnsureCurrent to ensure context is selected if running multiple windows in one thread
        public Action<Object, GLMouseEventArgs> MouseDown { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseUp { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseMove { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseEnter { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseLeave { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseClick { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseDoubleClick { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseWheel { get; set; } = null;
        public Action<Object, GLKeyEventArgs> KeyDown { get; set; } = null;
        public Action<Object, GLKeyEventArgs> KeyUp { get; set; } = null;
        public Action<Object, GLKeyEventArgs> KeyPress { get; set; } = null;

        // use EnsureCurrentPaintResize to ensure context is selected if running multiple windows in one thread
        public Action<Object> Resize { get; set; } = null;
        public Action<Object,ulong> Paint { get; set; } = null;     // ulong is elapsed time in ms

        public ulong ElapsedTimems { get { return (ulong)gltime.ElapsedMilliseconds; } }

        private System.Diagnostics.Stopwatch gltime = new System.Diagnostics.Stopwatch();
        private GLControlKeyOverride glControl { get; set; }

        public IntPtr context;

        public GLWinFormControl(Control attachcontrol, OpenTK.Graphics.GraphicsMode mode = null)
        {
            if (mode == null)
                mode = OpenTK.Graphics.GraphicsMode.Default;

            glControl = new GLControlKeyOverride(mode);

            glControl.MakeCurrent();        // make sure GLControl is current context selected, in case operating with multiples

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

        public void Dispose()
        {
            gltime.Stop();
            Control parent = glControl.Parent;
            parent.Controls.Remove(glControl);
            glControl.Dispose();
            glControl = null;
        }

        public void Invalidate()        // repaint
        {
            glControl.Invalidate();
        }

        public void EnsureCurrentContext()
        {
            glControl.MakeCurrent();
        }

        public bool IsCurrent()
        {
            var ctx = GLStatics.GetContext();
            return glControl.Context.IsCurrent && ctx == context;
        }

        public void SetCursor(GLCursorType t)
        {
            if (t == GLCursorType.Wait)
                glControl.Cursor = Cursors.WaitCursor;
            else if (t == GLCursorType.EW)
                glControl.Cursor = Cursors.SizeWE;
            else if (t == GLCursorType.NS)
                glControl.Cursor = Cursors.SizeNS;
            else if (t == GLCursorType.Move)
                glControl.Cursor = Cursors.Hand;
            else if (t == GLCursorType.NWSE)
                glControl.Cursor = Cursors.SizeNWSE;
            else
                glControl.Cursor = Cursors.Default;
        }

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

            Paint?.Invoke(glControl,(ulong)gltime.ElapsedMilliseconds);

            glControl.SwapBuffers();
        }

    }

    public class GLControlKeyOverride : OpenTK.GLControl
    {
        public GLControlKeyOverride(OpenTK.Graphics.GraphicsMode m) : base(m)
        {
        }

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
