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
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;

namespace TestOpenTk
{
    public partial class TestRenderBuffer : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLRenderProgramSortedList rObjectscw = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        GLStorageBlock dataoutbuffer;

        public TestRenderBuffer()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 1f;
            gl3dcontroller.MatrixCalc.PerspectiveFarZDistance = 1000f;
            gl3dcontroller.ZoomDistance = 20F;
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(110f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                return (float)ms / 100.0f;
            };

            items.Add(new GLColorShaderWorld(), "COSW");
            items.Add(new GLColorShaderObjectTranslation(), "COSOT");

            items.Add(new GLTexturedShaderObjectTranslation(), "TEXOT");

            {
                Bitmap bmp = new Bitmap(Properties.Resources.dotted2);      // demo argb copy
                byte[] argbbytes = bmp.GetARGBBytes();
                Bitmap copy = GLOFC.Utils.BitMapHelpers.CreateBitmapFromARGBBytes(bmp.Width, bmp.Height, argbbytes);
                var tex = new GLTexture2D(copy, SizedInternalFormat.Rgba8);
                items.Add(tex, "dotted2");
                Bitmap bmp2 = tex.GetBitmap(inverty: false);
                bmp2.Save(@"c:\code\dotted2.bmp");
            }

            #region coloured lines

            if (true)
            {
                GLRenderState lines = GLRenderState.Lines();

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.White, Color.Red, Color.DarkRed, Color.DarkRed })
                                   );

                GLRenderState lines2 = GLRenderState.Lines();

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines2,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Orange, Color.Blue, Color.DarkRed, Color.DarkRed }));
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
            }

            #endregion

            #region Matrix Calc Uniform

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0

            #endregion

            #region FB

            GLTexture2D ctex = new GLTexture2D();
            items.Add(ctex, "drawnbitmap");

            if (true)
            {
                int width = 1024, height = 768;

                // set up FB
                // NOTE: things end up inverted in Y in the texture, this is because textures are bottom up structures - seems the internet agrees

                GLFrameBuffer fb = new GLFrameBuffer();

                // attach a texture to draw to
                ctex.CreateOrUpdateTexture(width, height, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, 1);
                ctex.SetMinMagLinear();
                fb.AttachColor(ctex, 0, 0);

                GLRenderBuffer rb = new GLRenderBuffer();
                items.Add(rb);
                rb.Allocate(RenderbufferStorage.DepthComponent32f, ctex.Width, ctex.Height);
                fb.AttachDepth(rb);

                // bind Framebuffer to system for it to be the target to draw to, with a default back colour
                fb.BindColor(new OpenTK.Graphics.Color4(40, 40, 40, 255));

                GLMatrixCalc mc = new GLMatrixCalc();
                mc.PerspectiveNearZDistance = 1f;
                mc.PerspectiveFarZDistance = 1000f;
                mc.ResizeViewPort(this, new Size(ctex.Width, ctex.Height));
                Vector3 lookat = new Vector3(0, 0, 0);
                Vector2 camerapos = new Vector2(110f, 0);
                mc.CalculateModelMatrix(lookat, camerapos, 20F, 0);
                mc.CalculateProjectionMatrix();

                ((GLMatrixCalcUniformBlock)items.UB("MCUB")).SetFull(mc);

                var renderState = GLRenderState.Start(glwfc.Profile);

                Vector4[] p = new Vector4[4];

                int size = 64;
                int offset = 10;
                p[0] = new Vector4(offset, offset, 0, 1);       // topleft - correct winding for our system. For dotted, red/blue at top as dots
                p[1] = new Vector4(offset, offset + size, 0, 1);      // bottomleft
                p[2] = new Vector4(offset + size, offset, 0, 1);       // topright
                p[3] = new Vector4(offset + size, offset + size, 0, 1);      // botright

                items.Add(new GLDirect(), "fbds1");

                GLRenderState rts = GLRenderState.Tri();
                GLRenderDataTexture rdt = new GLRenderDataTexture(items.Tex("dotted2"));
                var ri = GLRenderableItem.CreateVector4(items, PrimitiveType.TriangleStrip, rts, p, rdt);
                ri.Execute(items.Shader("fbds1"), renderState, mc);

                GLRenderState lines = GLRenderState.Lines();

                var l1 = GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed });

                l1.Execute(items.Shader("COSW"), renderState, mc);

                var l2 = GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed });

                l2.Execute(items.Shader("COSW"), renderState, mc);

                var l3 = GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(-100, 10, 100), new Vector3(10, 0, 0), 21),
                                                             new Color4[] { Color.Yellow, Color.Orange, Color.Yellow, Color.Orange });

                l3.Execute(items.Shader("COSW"), renderState, mc);
                var l4 = GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(100, 10, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Yellow, Color.Orange, Color.Yellow, Color.Orange });
                l4.Execute(items.Shader("COSW"), renderState, mc);

                GLRenderState rc = GLRenderState.Tri();
                rc.CullFace = false;
                var ri2 = GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 3, 20)));
                ri2.Execute(items.Shader("COSOT"), renderState, mc);

                GLRenderState rq = GLRenderState.Tri();

                var ri3 = GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rq,
                        GLShapeObjectFactory.CreateQuadTriStrip(5f, 5f, new Vector3(-90F.Radians(), 0, 0)), GLShapeObjectFactory.TexTriStripQuad,
                        new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(10, 2, 0)));

                ri3.Execute(items.Shader("TEXOT"), renderState, mc);

                GLFrameBuffer.UnBind();
                gl3dcontroller.MatrixCalc.SetViewPort();        // restore the view port

                byte[] texdatab = ctex.GetTextureImageAs<byte>(OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, 0, true);
                Bitmap bmp = GLOFC.Utils.BitMapHelpers.CreateBitmapFromARGBBytes(ctex.Width, ctex.Height, texdatab);
                bmp.Save(@"c:\code\out.bmp");
            }

            #endregion


            if (true)
            {
                Vector4[] p = new Vector4[4];

                int size = 128;
                int offset = 10;
                p[0] = new Vector4(offset, offset, 0, 1);       // topleft - correct winding for our system. For dotted, red/blue at top as dots
                p[1] = new Vector4(offset, offset + size, 0, 1);      // bottomleft
                p[2] = new Vector4(offset + size, offset, 0, 1);       // topright
                p[3] = new Vector4(offset + size, offset + size, 0, 1);      // botright

                items.Add(new GLDirect(), "ds1");

                GLRenderState rts = GLRenderState.Tri();
                GLRenderDataTexture rdt = new GLRenderDataTexture(items.Tex("dotted2"));

                rObjects.Add(items.Shader("ds1"), "ds1", GLRenderableItem.CreateVector4(items, PrimitiveType.TriangleStrip, rts, p, rdt));
            }

            if (true)
            {
                GLRenderState rq = GLRenderState.Tri();

                float width = 30F;
                float height = 30F / ctex.Width * ctex.Height;

                // TexQuadInv corrects for the inverted FB texture
                rObjects.Add(items.Shader("TEXOT"),
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rq,
                        GLShapeObjectFactory.CreateQuadTriStrip(width, height, new Vector3(-90F.Radians(), 0, 0)), GLShapeObjectFactory.TexTriStripQuadInv,
                        new GLRenderDataTranslationRotationTexture(ctex, new Vector3(-15, 0, 10))
                        ));
            }

            if (true)
            {
                GLRenderState rq = GLRenderState.Tri();
                rObjects.Add(items.Shader("TEXOT"),
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.TriangleStrip, rq,
                        GLShapeObjectFactory.CreateQuadTriStrip(5f, 5f, new Vector3(-90F.Radians(), 0, 0)), GLShapeObjectFactory.TexTriStripQuad,
                        new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(10, 0, 0))
                        ));
            }

            dataoutbuffer = items.NewStorageBlock(5);
            dataoutbuffer.AllocateBytes(sizeof(float) * 4 * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicRead);    // 32 vec4 back

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

            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.LookAt, true);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;
        }

        private void SystemTick(object sender, EventArgs e)
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            gl3dcontroller.Redraw();
        }

        private void OtherKeys(GLOFC.Controller.KeyboardMonitor kb)
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


