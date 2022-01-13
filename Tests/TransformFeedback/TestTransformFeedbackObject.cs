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

using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.Operations;
using GLOFC.GL4.ShapeFactory;

namespace TestOpenTk
{
    public partial class TestTransformFeedbackObject : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        GLOperationQueryTimeStamp ts1, ts2;
        GLBuffer varyingbuffer;
        Vector4[] shape;
        GLTransformFeedback tfobj;

        public TestTransformFeedbackObject()
        {
            InitializeComponent();
            var mode = new OpenTK.Graphics.GraphicsMode(32, 24, 8, 0, 0, 2, false);     // combined 32 max of depth/stencil

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,mode);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 1f;
            gl3dcontroller.MatrixCalc.PerspectiveFarZDistance= 1000f;
            gl3dcontroller.ZoomDistance = 20F;
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(110f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 100.0f;
            };

            items.Add(new GLColorShaderWorld(), "COSW");

            var vs = new GLPLVertexShaderColorModelObjectTranslation(new string[] { "modelpos" },TransformFeedbackMode.InterleavedAttribs);
            var fs = new GLPLFragmentShaderVSColor();
            var cosot = new GLShaderPipeline(vs, fs);
            items.Add(cosot,"COSOT");

            tfobj = new GLTransformFeedback();
            items.Add(tfobj);

            varyingbuffer = new GLBuffer(10000, true, BufferUsageHint.DynamicCopy);

            ts1 = new GLOperationQueryTimeStamp();
            ts2 = new GLOperationQueryTimeStamp();

            rObjects.Add(ts1);

            #region coloured lines

            GLRenderState def = new GLRenderState() { DepthTest = true };      // set up default state for fixed values - no depth test, rely on stencil

            #region Coloured triangles
            if (true)
            {
                GLRenderState rc = GLRenderState.Tri(def);
                rc.CullFace = true;

                var tf = new GLOperationTransformFeedback(TransformFeedbackPrimitiveType.Triangles, tfobj, new GLBuffer[] { varyingbuffer });
                rObjects.Add(cosot, tf);     // must be in render queue, after shader start

                shape = GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f);
                rObjects.Add(cosot, "Tri1",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            shape,
                                            new Color4[] { Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Yellow, Color4.Yellow },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 3, 20))
                            ));

                rObjects.Add(cosot, new GLOperationEndTransformFeedback(tf));     // must be in render queue, after shader start
            }


            for ( int i = 0; i < 1; i++ )
            {
                GLRenderState lines = GLRenderState.Lines(def,5);

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.White, Color.Red, Color.DarkRed, Color.DarkRed })
                                   );

                GLRenderState lines2 = GLRenderState.Lines(def,1);

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines2,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Orange, Color.Blue, Color.DarkRed, Color.DarkRed }));

                rObjects.Add(items.Shader("COSW"), 
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines2,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(-100, 10, 100), new Vector3(10, 0, 0), 21),
                                                             new Color4[] { Color.Yellow, Color.Orange, Color.Yellow, Color.Orange })
                                   );

                rObjects.Add(items.Shader("COSW"), 
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines2,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(100, 10, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Yellow, Color.Orange, Color.Yellow, Color.Orange })
                                   );
            }

            rObjects.Add(ts2);

            #endregion


            #endregion

            #region Matrix Calc Uniform

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0

            #endregion

        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            System.Diagnostics.Debug.WriteLine("Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc, true);

            GLStatics.Check();

            GLStatics.Flush();

            var t1 = ts1.GetCounter();
            var t2 = ts2.GetCounter();

            System.Diagnostics.Debug.WriteLine($"Time Taken {t2-t1} ns");

            GLMemoryBarrier.All();
            Vector3[] values3 = varyingbuffer.ReadVector3sPacked(0, 8);     // varyings seem to ignore the vec3->vec4 packed thingy..

            System.Diagnostics.Debug.Assert(values3[1] == new Vector3(shape[1].X, shape[1].Y, shape[1].Z));
            System.Diagnostics.Debug.Assert(values3[3] == new Vector3(shape[3].X, shape[3].Y, shape[3].Z));

            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.LookAt, true);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.F5, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(0, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F6, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(4, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F7, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(10, 0, -10), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F8, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(50, 0, 50), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F4, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }


            if (kb.HasBeenPressed(Keys.O, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to 90");
                gl3dcontroller.Pan(new Vector2(90, 0), 3);
            }
            if (kb.HasBeenPressed(Keys.P, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to -180");
                gl3dcontroller.Pan(new Vector2(90, 180), 3);
            }

            //System.Diagnostics.Debug.WriteLine("kb check");

        }


        public class GLDirect : GLShaderPipeline
        {
            public GLDirect(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
            {
                AddVertexFragment(new GLPLVertexShaderScreenTexture(), new GLPLFragmentShaderTextureOffset());
            }
        }


    }

}


