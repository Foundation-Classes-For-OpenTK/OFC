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
    public partial class TestBitmaps : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public TestBitmaps()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

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
                return (float)ms / 20.0f;
            };

            items.Add( new GLTexturedShaderWithObjectTranslation(),"TEXOT");
            items.Add(new GLTexturedShaderWithObjectTranslation(), "TEXOTNoRot");
            items.Add(new GLColorShaderWithWorldCoord(), "COSW");
            items.Add(new GLColorShaderWithObjectTranslation(), "COSOT");
            items.Add(new GLFixedColorShaderWithObjectTranslation(Color.Goldenrod), "FCOSOT");
            items.Add(new GLTexturedShaderWithObjectCommonTranslation(), "TEXOCT");

            items.Add( new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8)  ,           "dotted"    );
            items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "logo8bpp");
            items.Add(new GLTexture2D(Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "dotted2");
            items.Add(new GLTexture2D(Properties.Resources.wooden, SizedInternalFormat.Rgba8), "wooden");
            items.Add(new GLTexture2D(Properties.Resources.shoppinglist, SizedInternalFormat.Rgba8), "shoppinglist");
            items.Add(new GLTexture2D(Properties.Resources.golden, SizedInternalFormat.Rgba8), "golden");
            items.Add(new GLTexture2D(Properties.Resources.smile5300_256x256x8, SizedInternalFormat.Rgba8), "smile");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");    

            #region coloured lines

            if (true)
            {
                GLRenderState lines = GLRenderState.Lines(1);

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
            if (false)
            {
                GLRenderState lines = GLRenderState.Lines(1);

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
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(2f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(0, 0, 0))
                            ));
                rObjects.Add(items.Shader("COSOT"), "scopen2",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(2f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 10, 10))
                            ));

            }

            #endregion

            #region bitmap Renderer

            if (true)
            {
                using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Size bitmapsize = new Size(128, 40);
                    Vector3 bannersize = new Vector3(20, 0, 0);
                    Font f = new Font("MS sans serif", 8f);

                    tim = new GLBitmaps("bitmap1", rObjects, bitmapsize, 3, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, false, true, 2);      // group 2
                    items.Add(tim);
                    tim.Add("T1", "SingleTest", f, Color.White, Color.Red, new Vector3(10, 10, 10),
                                            bannersize, new Vector3(0,0,0), fmt, alphafadescalar: 10, alphafadepos: 5, rotatetoviewer:true);

                }
            }


            if ( false)
            {
                using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Size bitmapsize = new Size(64, 20);
                    float width = 2.5f;
                    Vector3 bannersize = new Vector3(width, 0, 0);
                    Font f = new Font("MS sans serif", 8f);

                    tim = new GLBitmaps("bitmap1", rObjects, bitmapsize, 3, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, false, true,2);      // group 2
                    items.Add(tim);
                    tim.Add("T1", "MFred", f, Color.White, Color.Red, new Vector3(-10, 5, -10), bannersize, new Vector3(-90F.Radians(), 0, 0), fmt, alphafadescalar: 10, alphafadepos: 5);
                    tim.Add("T2", "MJim", f, Color.White, Color.Red, new Vector3(0, 5, -10), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true);
                    tim.Add("T3", "MGeorge", f, Color.White, Color.Red, new Vector3(10, 5, -10), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true, rotateelevation: true);
                    tim.Remove("T2");
                    tim.Add("T2a", "M2Jim", f, Color.White, Color.Red, new Vector3(0, 5, -10), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true);
                    tim.Remove("T3");       // meaning group 2 should be empty .. test it
                   // tim.Add("T3a", "M2George", f, Color.White, Color.Red, new Vector3(10, 5, -10), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true, rotateelevation: true);

                    tim2 = new GLBitmaps("bitmap2", rObjects, bitmapsize, 3, OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, false, true,25);
                    items.Add(tim2);
                    for (int i = 0; i < 50; i++)
                    {
                        tim2.Add("i" + i.ToString(), "i" + i.ToString() + "!", f, Color.White, Color.Red,
                                        new Vector3((i % 10) * 10 - 50, 5, (i / 10) * 4), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true, rotateelevation: true);

                    }

                    tim2.Clear();

                    for (int i = 0; i < 10000; i++)
                    {
                        tim2.Add("j" + i.ToString(), "j" + i.ToString() + "!", f, Color.White, Color.Red,
                                        new Vector3((i % 10) * 10 - 50, 5, (i / 10) * 4), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true, rotateelevation: true);

                    }

                    //tim.Dispose(); // for test

                }
            }


            #endregion


            #region Matrix Calc Uniform

            items.Add(new GLMatrixCalcUniformBlock(),"MCUB");     // def binding of 0

            #endregion

        }

        GLBitmaps tim;
        GLBitmaps tim2;

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            //System.Diagnostics.Debug.WriteLine("Draw");

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetText(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
            
            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.Lookat, true);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

            //GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            //Vector4[] databack = dataoutbuffer.ReadVector4(0, 4);
            //for (int i = 0; i < databack.Length; i += 1)
            //{
            //   // databack[i] = databack[i] / databack[i].W;
            //   // databack[i].X = databack[i].X * gl3dcontroller.glControl.Width / 2 + gl3dcontroller.glControl.Width/2;
            //   // databack[i].Y = gl3dcontroller.glControl.Height - databack[i].Y * gl3dcontroller.glControl.Height;
            //    System.Diagnostics.Debug.WriteLine("{0}={1}", i, databack[i].ToStringVec(true));
            //}
            //GLStatics.Check();

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            //  gl3dcontroller.Redraw();
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

            if (kb.HasBeenPressed(Keys.F2, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                tim2.SetVisiblityRotation("j1", false);
                gl3dcontroller.Redraw();
            }
            if (kb.HasBeenPressed(Keys.F2, GLOFC.Controller.KeyboardMonitor.ShiftState.Shift))
            {
                tim2.SetVisiblityRotation("j1", true);
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F10, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("F10");
                using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Font f = new Font("MS sans serif", 8f);
                    tim.Add("T2g", "M3George", f, Color.White, Color.Blue, new Vector3(10, 5, -10), new Vector3(2.5f, 0, 0), new Vector3(0, 0, 0), fmt, rotatetoviewer: true);
                    gl3dcontroller.Redraw();
                }
            }

            if (kb.HasBeenPressed(Keys.F11, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("F11");
                tim.Remove("T2g");
                gl3dcontroller.Redraw();
            }

            //tbd why losing focus? why no key

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


