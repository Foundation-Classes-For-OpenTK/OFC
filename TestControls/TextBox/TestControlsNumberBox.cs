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
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GLOFC.GL4.Controls;
using System.Linq;
using System.Globalization;
using GLOFC.Utils;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.ShapeFactory;
using static GLOFC.GL4.Controls.GLBaseControl;
using static GLOFC.GL4.Controls.GLForm;

namespace TestOpenTk
{
    public partial class TestControlsNumberBox : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsNumberBox()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        GLControlDisplay displaycontrol;

        /// ////////////////////////////////////////////////////////////////////////////////////////////////////


        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
            GLStatics.VerifyAllDeallocated();
        }

        public class GLFixedShader : GLShaderPipeline
        {
            public GLFixedShader(Color c, Action<IGLProgramShader, GLMatrixCalc> action = null) : base(action)
            {
                AddVertexFragment(new GLPLVertexShaderWorldCoord(), new GLPLFragmentShaderFixedColor(c));
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            int front = -20000, back = front + 90000, left = -45000, right = left + 90000, vsize = 2000;
  
            Vector4[] displaylines = new Vector4[]
            {
                new Vector4(left,-vsize,front,1),   new Vector4(left,+vsize,front,1),
                new Vector4(left,+vsize,front,1),      new Vector4(right,+vsize,front,1),
                new Vector4(right,+vsize,front,1),     new Vector4(right,-vsize,front,1),
                new Vector4(right,-vsize,front,1),  new Vector4(left,-vsize,front,1),

                new Vector4(left,-vsize,back,1),    new Vector4(left,+vsize,back,1),
                new Vector4(left,+vsize,back,1),       new Vector4(right,+vsize,back,1),
                new Vector4(right,+vsize,back,1),      new Vector4(right,-vsize,back,1),
                new Vector4(right,-vsize,back,1),   new Vector4(left,-vsize,back,1),

                new Vector4(left,-vsize,front,1),   new Vector4(left,-vsize,back,1),
                new Vector4(left,+vsize,front,1),      new Vector4(left,+vsize,back,1),
                new Vector4(right,-vsize,front,1),  new Vector4(right,-vsize,back,1),
                new Vector4(right,+vsize,front,1),     new Vector4(right,+vsize,back,1),
            };

            GLRenderState rl = GLRenderState.Lines();

            {
                items.Add(new GLFixedShader(System.Drawing.Color.Yellow), "LINEYELLOW");
                rObjects.Add(items.Shader("LINEYELLOW"),
                GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rl, displaylines));
            }

            float h = 0;
            if ( h != -1)
            {
                items.Add(new GLColorShaderWorld(), "COS-1L");

                int dist = 1000;
                Color cr = Color.FromArgb(100, Color.White);
                rObjects.Add(items.Shader("COS-1L"),    // horizontal
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(left, h, back), new Vector3(dist, 0, 0), (back - front) / dist + 1),
                                                        new Color4[] { cr })
                                   );

                rObjects.Add(items.Shader("COS-1L"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(right, h, front), new Vector3(0, 0, dist), (right - left) / dist + 1),
                                                        new Color4[] { cr })
                                   );

            }

            GLMatrixCalc mc = new GLMatrixCalc();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            // a display control

            displaycontrol = new GLControlDisplay(items, glwfc, mc);     // start class but don't hook
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Font = new Font("Times", 8);
            displaycontrol.Paint += (ts) => { displaycontrol.Render(glwfc.RenderState, ts); };

            gl3dcontroller = new Controller3D();
            gl3dcontroller.ZoomDistance = 5000F;
            gl3dcontroller.YHoldMovement = true;
            gl3dcontroller.PaintObjects = Controller3dDraw;
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) => { return (float)ms * 10.0f; };
            gl3dcontroller.MatrixCalc.InPerspectiveMode = true;

            // start hooks the glwfc paint function up, first, so it gets to go first
            // No ui events from glwfc.
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F, false, false);
            gl3dcontroller.Hook(displaycontrol, glwfc); // we get 3dcontroller events from displaycontrol, so it will get them when everything else is unselected
            displaycontrol.Hook();  // now we hook up display control to glwin, and paint


            GLForm pform = new GLForm("Form1", "GL Form demonstration", new Rectangle(0, 0, 1000, 800));

            int taborder = 0;

            if (true)
            {
                GLNumberBoxLong nbox = new GLNumberBoxLong("NBL1", new Rectangle(10, 10, 200, 24), 23);
                nbox.Minimum = 1;
                nbox.Maximum = 999;
                nbox.TabOrder = taborder++;
                nbox.ValidityChanged += (s, b) => { System.Diagnostics.Debug.WriteLine($"Validity {s.Name} {b}"); };
                nbox.ValueChanged += (s) => { System.Diagnostics.Debug.WriteLine($"Value {s.Name} {nbox.Value}"); };
                pform.Add(nbox);

            }
            if (true)
            {
                GLNumberBoxFloat nbox = new GLNumberBoxFloat("NBF1", new Rectangle(10, 40, 200, 24), 23);
                nbox.Minimum = -999;
                nbox.Maximum = 999999;
                nbox.TabOrder = taborder++;
                nbox.ValidityChanged += (s, b) => { System.Diagnostics.Debug.WriteLine($"Validity {s.Name} {b}"); };
                nbox.ValueChanged += (s) => { System.Diagnostics.Debug.WriteLine($"Value {s.Name} {nbox.Value:N9}"); };
                pform.Add(nbox);

            }

            if (true)
            {
                GLNumberBoxDouble nbox = new GLNumberBoxDouble("NBD1", new Rectangle(10, 70, 200, 24), 23);
                nbox.Minimum = -999;
                nbox.Maximum = 999;
                nbox.TabOrder = taborder++;
                nbox.ValidityChanged += (s, b) => { System.Diagnostics.Debug.WriteLine($"Validity {s.Name} {b}"); };
                nbox.ValueChanged += (s) => { System.Diagnostics.Debug.WriteLine($"Value {s.Name} {nbox.Value:N9}"); };
                pform.Add(nbox);

            }

            displaycontrol.Add(pform);

            systemtimer.Start();
        }

        private void Controller3dDraw(Controller3D mc, ulong unused)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).SetFull(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.PosCamera.ZoomFactor;
        }

        private void SystemTick(object sender, EventArgs e)
        {
            PolledTimer.ProcessTimers();
            displaycontrol.Animate(glwfc.ElapsedTimems);
            if (displaycontrol != null && displaycontrol.RequestRender)
                glwfc.Invalidate();
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, Otherkeys);
        }

        private void Otherkeys(KeyboardMonitor h)
        {
            if ( h.HasBeenPressed(Keys.F1, KeyboardMonitor.ShiftState.None))
            {
            }

        }
    }
}


