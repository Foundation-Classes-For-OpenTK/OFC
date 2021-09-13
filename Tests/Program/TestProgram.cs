/*
 * Copyright 2019 Robbyxp1 @ github.com
 * Part of the EDDiscovery Project
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
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;

// A simpler main for testing

namespace TestOpenTk
{
    public partial class TestProgram : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        GLOperationQueryTimeStamp ts1, ts2;

        public TestProgram()
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

            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (var obj in searcher.Get())
            {
                var bpp = obj.Properties["CurrentBitsPerPixel"];
                if ( bpp != null)
                {
                    var name = obj.Properties["Name"];
                    var ver = obj.Properties["DriverVersion"];
                }
            }


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

            items.Add(new GLColorShaderWithWorldCoord(), "COSW");

            var vs1 = new GLPLVertexShaderColorModelCoordWithObjectTranslation(new string[] { "modelpos" }, TransformFeedbackMode.InterleavedAttribs);
            var vsbin = vs1.GetBinary(out BinaryFormat binformat);      // round trip example thru binary

            var vs = new GLPLVertexShaderColorModelCoordWithObjectTranslation(vsbin,binformat);

            var fs = new GLPLFragmentShaderVSColor(true);
            var cosot = new GLShaderPipeline(vs, fs);

            var pipelinebin = cosot.GetBinary(out BinaryFormat fmt);    // save out a pipeline shader

            var cosotloaded = new GLShaderPipeline(pipelinebin, fmt);       // demo you can load them, but can't be used, since we then don't have all the component classes


            items.Add(cosot,"COSOT");

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


                var shape = GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f);
                rObjects.Add(cosot, "Tri1",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            shape,
                                            new Color4[] { Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Yellow, Color4.Yellow },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 3, 20))
                            ));
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
            mcub.SetText(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc, false);

            GLStatics.Check();

            //GLStatics.Flush();

            var t1 = ts1.GetCounter();
            var t2 = ts2.GetCounter();

            System.Diagnostics.Debug.WriteLine($"Time Taken {t2 - t1} ns");

            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.Lookat, true);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

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
                AddVertexFragment(new GLPLVertexShaderTextureScreenCoordWithTriangleStripCoord(), new GLPLFragmentShaderTextureOffset());
            }
        }


    }

}


