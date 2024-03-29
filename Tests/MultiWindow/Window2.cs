﻿/*
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

// A simpler main for testing

namespace TestOpenTk
{
    public partial class Window2 : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public Window2()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);
            glwfc.EnsureCurrent = true;

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            glwfc.EnsureCurrentContext();       // paranoia check

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
            items.Add(new GLColorShaderObjectTranslation(), "COSOT");
            items.Add( new GLTexturedShaderObjectTranslation(),"TEXOT");

            items.Add( new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8)  , "dotted");
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

            #region Coloured triangles
            if (true)
            {
                GLRenderState rc = GLRenderState.Tri();
                rc.CullFace = false;

                rObjects.Add(items.Shader("COSOT"), "scopen",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 3, 20))
                            ));

                rObjects.Add(items.Shader("COSOT"), "scopen2",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                            new Color4[] { Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Red, Color4.Red },
                                            new GLRenderDataTranslationRotation(new Vector3(-10, -3, -20))
                            ));
            }

            #endregion
            #region textures
            if (true)
            {
                GLRenderState rq = GLRenderState.Tri();

                rObjects.Add(items.Shader("TEXOT"),
                            GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rq,
                            GLShapeObjectFactory.CreateQuadTriStrip(1.0f, 1.0f, new Vector3( -90f.Radians(), 0, 0)), GLShapeObjectFactory.TexTriStripQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(0,0,0))
                            ));

                rObjects.Add(items.Shader("TEXOT"),
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rq,
                            GLShapeObjectFactory.CreateQuadTriStrip(1.0f, 1.0f, new Vector3(0, 0, 0)), GLShapeObjectFactory.TexTriStripQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(2,0,0))
                            ));

                rObjects.Add(items.Shader("TEXOT"),
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rq,
                            GLShapeObjectFactory.CreateQuadTriStrip(1.0f, 1.0f, new Vector3(0, 0, 0)), GLShapeObjectFactory.TexTriStripQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted"), new Vector3(4,0,0))
                            ));

                GLRenderState rqnc = GLRenderState.Tri(cullface: false);

                rObjects.Add(items.Shader("TEXOT"), "EDDFlat",
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rqnc,
                    GLShapeObjectFactory.CreateQuadTriStrip(2.0f, items.Tex("logo8bpp").Width, items.Tex("logo8bpp").Height, new Vector3(-0, 0, 0)), GLShapeObjectFactory.TexTriStripQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("logo8bpp"), new Vector3(6, 0, 0))
                            ));

                rObjects.Add(items.Shader("TEXOT"),
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rqnc,
                            GLShapeObjectFactory.CreateQuadTriStrip(1.5f, 1.5f, new Vector3( 90f.Radians(), 0, 0)), GLShapeObjectFactory.TexTriStripQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("smile"), new Vector3(8, 0, 0))
                           ));

            }

            #endregion


            #region Matrix Calc Uniform

            items.Add(new GLMatrixCalcUniformBlock(),"MCUB");     // def binding of 0

            #endregion

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
            
            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.LookAt, true);

            this.Text = "Window2 Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
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


