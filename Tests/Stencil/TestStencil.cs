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

using OFC;
using OFC.Controller;
using OFC.GL4;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;
using System.Windows.Forms;

// A simpler main for testing

namespace TestOpenTk
{
    public partial class TestStencil : Form
    {
        private OFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
       // GLRenderProgramSortedList rObjects2 = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public TestStencil()
        {
            InitializeComponent();
            var mode = new OpenTK.Graphics.GraphicsMode(32, 24, 8, 0, 0, 2, false);     // combined 32 max of depth/stencil

            glwfc = new OFC.WinForm.GLWinFormControl(glControlContainer,mode);

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

            items.Add( new GLTexturedShaderWithObjectTranslation(),"TEXOT");
            items.Add(new GLTexturedShaderWithObjectTranslation(), "TEXOTNoRot");
            items.Add(new GLColorShaderWithWorldCoord(), "COSW");
            items.Add(new GLColorShaderWithObjectTranslation(), "COSOT");
            items.Add(new GLFixedColorShaderWithObjectTranslation(Color.Goldenrod), "FCOSOT");
            items.Add(new GLTexturedShaderWithObjectCommonTranslation(), "TEXOCT");

            items.Add(new GLTexture2D(Properties.Resources.dotted2), "dotted");
            items.Add(new GLTexture2D(Properties.Resources.Logo8bpp), "logo8bpp");

            items.Add(new GLTexture2D(Properties.Resources.wooden), "wooden");
            items.Add(new GLTexture2D(Properties.Resources.shoppinglist), "shoppinglist");
            items.Add(new GLTexture2D(Properties.Resources.golden), "golden");
            items.Add(new GLTexture2D(Properties.Resources.smile5300_256x256x8), "smile");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k), "moon");


            #region coloured lines

            GLRenderControl def = new GLRenderControl() { DepthTest = false };      // set up default state for fixed values

            if ( true )
            {
                var sns = new GLShaderNull((sh1, c) => { GLStencil.SetStencil(); });        // add a null shader so at this point we can set the stencil
                rObjects.Add(sns);

                GLRenderControl rq = GLRenderControl.Quads(def);

                IGLProgramShader s = items.Shader("TEXOT");

                rObjects.Add(s, "Quad",
                            GLRenderableItem.CreateVector4Vector2(items, rq,
                            GLShapeObjectFactory.CreateQuad(5.0f, 5.0f, new Vector3(-90F.Radians(), 0, 0)), GLShapeObjectFactory.TexQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted"), new Vector3(-2, 3, -6))
                            ));

                var snf = new GLShaderNull((sh1, c) => { GLStencil.OnlyIfEqual(); });         // now turn it to overwrite mode.
                rObjects.Add(snf);
            }

            #region Coloured triangles
            if (true)
            {
                GLRenderControl rc = GLRenderControl.Tri(def);
                rc.CullFace = false;

                rObjects.Add(items.Shader("COSOT"), "Tri",
                            GLRenderableItem.CreateVector4Color4(items, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 3, 20))
                            ));
            }

            if (true)
            {
                GLRenderControl lines = GLRenderControl.Lines(def,5);

                rObjects.Add(items.Shader("COSW"), "L1",
                             GLRenderableItem.CreateVector4Color4(items, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.White, Color.Red, Color.DarkRed, Color.DarkRed })
                                   );

                GLRenderControl lines2 = GLRenderControl.Lines(def,1);

                rObjects.Add(items.Shader("COSW"), "L2",
                             GLRenderableItem.CreateVector4Color4(items, lines2,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Orange, Color.Blue, Color.DarkRed, Color.DarkRed }));

                rObjects.Add(items.Shader("COSW"), "L3",
                             GLRenderableItem.CreateVector4Color4(items, lines2,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(-100, 10, 100), new Vector3(10, 0, 0), 21),
                                                             new Color4[] { Color.Yellow, Color.Orange, Color.Yellow, Color.Orange })
                                   );

                rObjects.Add(items.Shader("COSW"), "L4",
                             GLRenderableItem.CreateVector4Color4(items, lines2,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(100, 10, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Yellow, Color.Orange, Color.Yellow, Color.Orange })
                                   );
            }

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

        private void ControllerDraw(GLMatrixCalc mc, long time)
        {
            System.Diagnostics.Debug.WriteLine("Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.Lookat, true);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true, OtherKeys);
        }

        private void OtherKeys( OFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.F5, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.CameraLookAt(new Vector3(0, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F6, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.CameraLookAt(new Vector3(4, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F7, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.CameraLookAt(new Vector3(10, 0, -10), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F8, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.CameraLookAt(new Vector3(50, 0, 50), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F4, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }


            if (kb.HasBeenPressed(Keys.O, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to 90");
                gl3dcontroller.CameraPan(new Vector2(90, 0), 3);
            }
            if (kb.HasBeenPressed(Keys.P, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to -180");
                gl3dcontroller.CameraPan(new Vector2(90, 180), 3);
            }

            //System.Diagnostics.Debug.WriteLine("kb check");

        }


        public class GLDirect : GLShaderPipeline
        {
            public GLDirect(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader, GLMatrixCalc> finish = null) : base(start, finish)
            {
                AddVertexFragment(new GLPLVertexShaderTextureScreenCoordWithTriangleStripCoord(), new GLPLFragmentShaderTextureTriangleStrip(false));
            }
        }


    }

}


