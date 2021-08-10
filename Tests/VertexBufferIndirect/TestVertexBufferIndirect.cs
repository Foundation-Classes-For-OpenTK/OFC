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
    public partial class TestVertexBufferIndirect : Form
    {
        private OFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        GLVertexBufferIndirect vertices;

        public TestVertexBufferIndirect()
        {
            InitializeComponent();

            glwfc = new OFC.WinForm.GLWinFormControl(glControlContainer);

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
                return (float)ms / 40.0f;
            };

            items.Add( new GLTexturedShaderWithObjectTranslation(),"TEXOT");
            items.Add(new GLTexturedShaderWithObjectTranslation(), "TEXOTNoRot");
            items.Add(new GLColorShaderWithWorldCoord(), "COSW");
            items.Add(new GLColorShaderWithObjectTranslation(), "COSOT");
            items.Add(new GLFixedColorShaderWithObjectTranslation(Color.Goldenrod), "FCOSOT");
            items.Add(new GLTexturedShaderWithObjectCommonTranslation(), "TEXOCT");

            items.Add( new GLTexture2D(Properties.Resources.dotted)  ,           "dotted"    );
            items.Add(new GLTexture2D(Properties.Resources.Logo8bpp), "logo8bpp");
            items.Add(new GLTexture2D(Properties.Resources.dotted2), "dotted2");
            items.Add(new GLTexture2D(Properties.Resources.wooden), "wooden");
            items.Add(new GLTexture2D(Properties.Resources.shoppinglist), "shoppinglist");
            items.Add(new GLTexture2D(Properties.Resources.golden), "golden");
            items.Add(new GLTexture2D(Properties.Resources.smile5300_256x256x8), "smile");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k), "moon");    

            #region coloured lines

            if (true)
            {
                GLRenderControl lines = GLRenderControl.Lines(1);

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed })
                                   );


                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed }));
            }
 
            #endregion

            #region Coloured triangles
            if (true)
            {
                GLRenderControl rc = GLRenderControl.Tri();
                rc.CullFace = false;

                rObjects.Add(items.Shader("COSOT"), "scopen",
                            GLRenderableItem.CreateVector4Color4(items, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(5f),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(10, 0, 20))
                            ));

            }

            #endregion

            {

                vertices = new GLVertexBufferIndirect(65536, 1024, true);

                var sunvertex = new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(new Color[] { Color.FromArgb(255, 220, 220, 10), Color.FromArgb(255, 0, 0, 0) });
                items.Add(sunvertex);
                var sunshader = new GLShaderPipeline(sunvertex, new GLPLStarSurfaceFragmentShader());
                items.Add(sunshader);

                var shapebuf = new GLBuffer();
                items.Add(shapebuf);
                var shape = GLSphereObjectFactory.CreateSphereFromTriangles(1, 0.5f);
                shapebuf.AllocateFill(shape);

                int SectorSize = 10;
                int instancestart = 0;

                {
                    Vector3 pos = new Vector3(0, 0, 0);
                    Vector4[] array = new Vector4[2];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    vertices.Fill(array, 0, shape.Length, 0, array.Length, instancestart);
                    instancestart += array.Length;
                }
                {
                    Vector3 pos = new Vector3(-20, 0, 0);
                    Vector4[] array = new Vector4[2];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    vertices.Fill(array, 0, shape.Length, 0, array.Length, 2);
                    instancestart += array.Length;
                }
                {
                    Vector3 pos = new Vector3(-40, 0, 0);
                    Vector4[] array = new Vector4[2];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    vertices.Fill(array, 0, shape.Length, 0, array.Length, instancestart);
                    instancestart += array.Length;
                }


                int[] indirectints = vertices.Indirects[0].ReadInts(0, 12);
                float[] worldpos = vertices.Vertex.ReadFloats(0, 3*2*4);

                GLRenderControl rt = GLRenderControl.Tri();     // render is triangles, with no depth test so we always appear
                rt.DepthTest = true;
                rt.DepthClamp = true;

                var renderer = GLRenderableItem.CreateVector4Vector4(items, rt, 
                                                                            shapebuf, 0, 0,     // binding 0 is shapebuf, offset 0, no draw count 
                                                                            vertices.Vertex, 0, // binding 1 is vertex's world positions, offset 0
                                                                            null, 0, 1);        // no ic, second divisor 1
                renderer.IndirectBuffer = vertices.Indirects[0];
                renderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
                renderer.MultiDrawCount = 3;
                renderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

                rObjects.Add(sunshader, "Sector1", renderer);
            }

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
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true, OtherKeys);
            //  gl3dcontroller.Redraw();
        }

        private void OtherKeys( OFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.F5, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(0, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F6, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(4, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F7, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(10, 0, -10), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F8, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(50, 0, 50), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F4, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }

            if (kb.HasBeenPressed(Keys.F2, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.Redraw();
            }
            if (kb.HasBeenPressed(Keys.F2, OFC.Controller.KeyboardMonitor.ShiftState.Shift))
            {
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F10, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F11, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.O, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to 90");
                gl3dcontroller.Pan(new Vector2(90, 0), 3);
            }
            if (kb.HasBeenPressed(Keys.P, OFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to -180");
                gl3dcontroller.Pan(new Vector2(90, 180), 3);
            }

            //System.Diagnostics.Debug.WriteLine("kb check");

        }

    }

}


