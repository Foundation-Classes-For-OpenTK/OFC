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
using System.Collections.Generic;
using GLOFC.GL4.Controls;
using System.Linq;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using GLOFC.Utils;
using static GLOFC.GL4.Controls.GLBaseControl;

namespace TestOpenTk
{
    public partial class TestControlsToolTip: System.Windows.Forms.Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private System.Windows.Forms.Timer systemtimer = new System.Windows.Forms.Timer();

        public TestControlsToolTip()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);
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

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

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


            {
                items.Add(new GLTexturedShaderObjectTranslation(), "TEX");
                items.Add(new GLTexture2D(TestControls.Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "dotted2");

                GLRenderState rt = GLRenderState.Tri();

                rObjects.Add(items.Shader("TEX"),
                    GLRenderableItem.CreateVector4Vector2(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, rt,
                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(2000f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(-2, 0, 0))
                            ));
            }

            {
                items.Add(new GLFixedColorShaderWorld(Color.FromArgb(150, Color.Green)), "FCS1");
                items.Add(new GLFixedColorShaderWorld(Color.FromArgb(80, Color.Red)), "FCS2");

                GLRenderState rq = GLRenderState.Tri();

                rObjects.Add(items.Shader("FCS1"),
                    GLRenderableItem.CreateVector4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, rq,
                                                GLShapeObjectFactory.CreateQuadTriStrip(1000, 1000, pos: new Vector3(4000, 500, 0))));
                rObjects.Add(items.Shader("FCS2"),
                    GLRenderableItem.CreateVector4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, rq,
                                                GLShapeObjectFactory.CreateQuadTriStrip(1000, 1000, pos: new Vector3(4000, 1000, 0))));
            }

            GLMatrixCalc mc = new GLMatrixCalc();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            // a display control

            displaycontrol = new GLControlDisplay(items, glwfc, mc);     // start class but don't hook
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Font = new Font("Times", 8);
            displaycontrol.Paint += (ts) => { System.Diagnostics.Debug.WriteLine("Paint controls"); displaycontrol.Render(glwfc.RenderState, ts); };

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


            GLForm pform = new GLForm("Form1", "GL Control demonstration", new Rectangle(0, 0, 1400, 850));
            // pform.BackColor = Color.FromArgb(200, Color.Red);
            //pform.BackColorGradientDir = 90;
            //pform.BackColorGradientAlt = Color.FromArgb(200, Color.Yellow);

            displaycontrol.Add(pform);

            GLImage pti1 = new GLImage("PTI1", new Rectangle(10, 10, 24, 24), TestControls.Properties.Resources.dotted);
            pti1.ToolTipText = "This is a tool Tip";
            pform.Add(pti1);

            GLImage pti2 = new GLImage("PTI2", new Rectangle(1350, 10, 24, 24), TestControls.Properties.Resources.dotted);
            pti2.ToolTipText = "This is a tool Tip which is long\nand had lines\nin it";
            pform.Add(pti2);

            GLImage pti3 = new GLImage("PTI3", new Rectangle(1350, 800, 24, 24), TestControls.Properties.Resources.dotted);
            pti3.ToolTipText = "This is a tool Tip which is long\nand had lines\nin it";
            pform.Add(pti3);

            GLToolTip tip = new GLToolTip("ToolTip");
            tip.Font = new Font("Arial", 14);
            displaycontrol.Add(tip);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
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
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true);
        }

    }
}


