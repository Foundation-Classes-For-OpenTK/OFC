﻿/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;

// A simpler main for demoing

namespace TestOpenTk
{
    public partial class TestSimple : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public TestSimple()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            {
                System.Diagnostics.Debug.WriteLine($"Version  {GLStatics.GetVersion()}");
                System.Diagnostics.Debug.WriteLine($"Version  {GLStatics.GetVersionString()}");
                System.Diagnostics.Debug.WriteLine($"Vendor  {GLStatics.GetVendor()}");
                System.Diagnostics.Debug.WriteLine($"Shading lang {GLStatics.GetShaderLanguageVersion()}");
                System.Diagnostics.Debug.WriteLine($"Shading lang {GLStatics.GetShadingLanguageVersionString()}");
                var ext = GLStatics.Extensions();
                System.Diagnostics.Debug.WriteLine($"Extension {string.Join(",", ext)}");

                System.Diagnostics.Debug.WriteLine($"UBS={GL4Statics.GetMaxUniformBlockSize()}");
                GL4Statics.GetMaxUniformBuffers(out int v, out int f, out int g, out int tc, out int te);
                System.Diagnostics.Debug.WriteLine($"UB v{v} f{f} g{g} tc{tc} te{te}");
                System.Diagnostics.Debug.WriteLine($"tex layers {GL4Statics.GetMaxTextureDepth()} ");
                System.Diagnostics.Debug.WriteLine($"Vertex attribs {GL4Statics.GetMaxVertexAttribs()} ");
            }


            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 1f;
            gl3dcontroller.MatrixCalc.PerspectiveFarZDistance=21000f;
            gl3dcontroller.MouseTranslateAmountAtZoom1PerPixel = 0.5f;
            gl3dcontroller.ZoomDistance = 50F;
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(135f, 0, 0f), 1F);
             
            bool useopenglcoords = true;        // true for +Z towards viewer. OFC is using mostly +Z away from viewer
            if (useopenglcoords)
            {
                gl3dcontroller.MatrixCalc.ModelAxisPositiveZAwayFromViewer = false;
                gl3dcontroller.SetPositionCamera(new Vector3(0, 0, 0), new Vector3(0, 50, 100), 0f);
                gl3dcontroller.RecalcMatrices();
            }

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 100.0f;
            };

            // create stock shaders

            GLShaderLog.AssertOnError = false;

            items.Add(new GLColorShaderWorld(), "COSW");

#if true
            items.Add(new GLColorShaderObjectTranslation(), "COSOT");
            items.Add(new GLTexturedShaderObjectTranslation(), "TEXOT");

            // create stock textures

            items.Add(new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8), "dotted");
            items.Add(new GLTexture2D(Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "dotted2");
            items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "logo8bpp");
            items.Add(new GLTexture2D(Properties.Resources.smile5300_256x256x8, SizedInternalFormat.Rgba8), "smile");

#region coloured lines

            if (true)
            {
                GLRenderState lines = GLRenderState.Lines();

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed })
                                   );


                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed }));
            }

            if (true)
            {
                GLRenderState lines = GLRenderState.Lines();

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

#endregion

#region Coloured cubes
            if (true)
            {
                GLRenderState rc = GLRenderState.Tri();
                rc.CullFace = false;

                var cube1pos = GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f, new Vector3(10, 2.5f, 10));
                var cube1dtr = new GLRenderDataTranslationRotation(new Vector3(0, 0, 0));

               // these are on positive Z and x

                rObjects.Add(items.Shader("COSOT"), "scopen",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            cube1pos,
                                            new Color4[] { Color4.Green, Color4.Green, Color4.Blue, Color4.Blue, Color4.Cyan, Color4.Cyan },
                                             cube1dtr

                            ));

                var cube2pos = GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f, new Vector3(10, 2.5f, 50));
                rObjects.Add(items.Shader("COSOT"), "scopen1",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            cube2pos,
                                            new Color4[] { Color4.Green, Color4.Green, Color4.Blue, Color4.Blue, Color4.Cyan, Color4.Cyan },
                                             cube1dtr

                            ));

                //this one above cube2 for use in testing ortho mode


               var cube3pos = GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f, new Vector3(10, 10.25f, 50));
               rObjects.Add(items.Shader("COSOT"), "scopen2",
                           GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                           cube3pos,
                                           new Color4[] { Color4.Yellow },
                                            cube1dtr

                           ));

               // negative z and x


               rObjects.Add(items.Shader("COSOT"), "scopen3",
                           GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                           GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                           new Color4[] { Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Red },
                                           new GLRenderDataTranslationRotation(new Vector3(-10, -2.5f, -10))
                           ));

                rObjects.Add(items.Shader("COSOT"), "scopen4",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                            new Color4[] { Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Red },
                                            new GLRenderDataTranslationRotation(new Vector3(-10, -2.5f, -20))
                            ));
            }

#endregion

#region textures
            if (true)
            {
              //  texture facing upwards, culled if viewer below it

                GLRenderState rq = GLRenderState.Tri();

                rObjects.Add(items.Shader("TEXOT"),
                            GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rq,
                            GLShapeObjectFactory.CreateQuadTriStrip(5.0f, 5.0f, new Vector3(-0f.Radians(), 0, 0), ccw:!useopenglcoords), 
                            GLShapeObjectFactory.TexTriStripQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(0, 0, 0))
                            ));
            }

#endregion


            items.Add(new GLMatrixCalcUniformBlock(),"MCUB");     // def binding of 0

#endif
            System.Diagnostics.Debug.WriteLine($"Shader report: {Environment.NewLine}{GLShaderLog.ShaderLog}");

            string shaderlog = GLShaderLog.ShaderLog;
            if (shaderlog.HasChars())
                MessageBox.Show(this, shaderlog, "Shader Log - report to EDD");

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            //System.Diagnostics.Debug.WriteLine("Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
            
            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + 
                               " cdir " + gl3dcontroller.PosCamera.CameraDirection + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + 
                                " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            if ( gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys) )
                gl3dcontroller.Redraw();
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

            if (kb.HasBeenPressed(Keys.F4, GLOFC.Controller.KeyboardMonitor.ShiftState.None))           // ! change mode to perspective
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


