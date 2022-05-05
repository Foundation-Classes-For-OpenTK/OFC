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
using GLOFC.GL4.Textures;

namespace TestOpenTk
{
    public partial class TestQueries : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        GLOperationQueryTimeStamp ts1, ts2;

        public TestQueries()
        {
            InitializeComponent();
            var mode = new OpenTK.Graphics.GraphicsMode(32, 24, 8, 0, 0, 2, false);     // combined 32 max of depth/stencil

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,mode,4,6);

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

            items.Add( new GLTexturedShaderObjectTranslation(),"TEXOT");
            items.Add(new GLTexturedShaderObjectTranslation(), "TEXOTNoRot");
            items.Add(new GLColorShaderWorld(), "COSW");
            items.Add(new GLColorShaderObjectTranslation(), "COSOT");
            items.Add(new GLFixedColorShaderObjectTranslation(Color.Goldenrod), "FCOSOT");
            items.Add(new GLTexturedShaderObjectCommonTranslation(), "TEXOCT");

            items.Add(new GLTexture2D(Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "dotted");
            items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "logo8bpp");

            items.Add(new GLTexture2D(Properties.Resources.wooden, SizedInternalFormat.Rgba8), "wooden");
            items.Add(new GLTexture2D(Properties.Resources.shoppinglist, SizedInternalFormat.Rgba8), "shoppinglist");
            items.Add(new GLTexture2D(Properties.Resources.golden, SizedInternalFormat.Rgba8), "golden");
            items.Add(new GLTexture2D(Properties.Resources.smile5300_256x256x8, SizedInternalFormat.Rgba8), "smile");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");


            ts1 = new GLOperationQueryTimeStamp();
            ts1.StartAction = (a, b) => { System.Diagnostics.Debug.WriteLine("Start action on ts1"); };
            ts2 = new GLOperationQueryTimeStamp();

            rObjects.Add(ts1);

            #region coloured lines

            #region Coloured triangles
            if (true)
            {
                GLRenderState rc = GLRenderState.Tri();
                rc.DepthTest = false;
                rc.CullFace = true;

                var q1 = new GLOperationQuery(OpenTK.Graphics.OpenGL4.QueryTarget.PrimitivesGenerated, 0, true);
                q1.FinishAction += (t) => { System.Diagnostics.Debug.WriteLine($"What is Query for Primities Gen? {GLOperationQuery.GetQueryName(OpenTK.Graphics.OpenGL4.QueryTarget.PrimitivesGenerated, 0)}"); };
                items.Add(q1);
                rObjects.Add(q1);
                var q2 = new GLOperationQuery(OpenTK.Graphics.OpenGL4.QueryTarget.SamplesPassed);
                items.Add(q2);
                rObjects.Add(q2);
                var q3 = new GLOperationQuery(OpenTK.Graphics.OpenGL4.QueryTarget.TimeElapsed);
                items.Add(q3);
                rObjects.Add(q3);

                System.Diagnostics.Debug.WriteLine($"Query 1? {GLOperationQuery.IsQuery(q1.Id)}");
                

                rObjects.Add(items.Shader("COSOT"), "Tri",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 3, 20))
                            ));

                rObjects.Add(new GLOperationEndQuery(q3, querycomplete: (t) =>
                {
                    int v = t.GetQuery(OpenTK.Graphics.OpenGL4.GetQueryObjectParam.QueryResult);
                    System.Diagnostics.Debug.WriteLine($"Time {v / 1} ns");
                }));
                rObjects.Add(new GLOperationEndQuery(q1, querycomplete: (t) =>
                {
                    int v = t.GetQuery(OpenTK.Graphics.OpenGL4.GetQueryObjectParam.QueryResult);
                    System.Diagnostics.Debug.WriteLine($"Primitives {v}");
                }));
                rObjects.Add(new GLOperationEndQuery(q2, querycomplete: (t) =>
                {
                    int v = t.GetQuery(OpenTK.Graphics.OpenGL4.GetQueryObjectParam.QueryResult);
                    System.Diagnostics.Debug.WriteLine($"Samples {v}");
                }));
            }



            for( int i = 0; i < 100; i++ )//if (true)
            {
                GLRenderState lines = GLRenderState.Lines();
                lines.DepthTest = false;

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.White, Color.Red, Color.DarkRed, Color.DarkRed })
                                   );

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Orange, Color.Blue, Color.DarkRed, Color.DarkRed }));

                rObjects.Add(items.Shader("COSW"), 
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(-100, 10, 100), new Vector3(10, 0, 0), 21),
                                                             new Color4[] { Color.Yellow, Color.Orange, Color.Yellow, Color.Orange })
                                   );

                rObjects.Add(items.Shader("COSW"), 
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
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

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            GLStatics.Flush();

            var t1 = ts1.GetCounter();
            var t2 = ts2.GetCounter();

            System.Diagnostics.Debug.WriteLine($"Time Taken {t2-t1} ns");

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

    }

}


