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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// A simpler main for testing

namespace TestOpenTk
{
    public partial class TestVertexBufferIndirect : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        GLVertexBufferIndirect dataindirectbuffer;

        GLObjectsWithLabels sl;
        GLSetOfObjectsWithLabels slset;
        GLShaderPipeline findshader;

        public TestVertexBufferIndirect()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        class TextShader : GLShaderPipeline
        {
            public TextShader(int texunitspergroup)
            {
                AddVertexFragment(new GLPLVertexShaderQuadTextureWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexedMulti(0, 0, true, texunitspergroup));
            }
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
            glwfc.MouseClick += GLMouseClick;

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 40.0f;
            };

            items.Add(new GLColorShaderWithWorldCoord(), "COSW");
            items.Add(new GLColorShaderWithObjectTranslation(), "COSOT");

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
                                            new GLRenderDataTranslationRotation(new Vector3(10, 0, 20))
                            ));

            }

            #endregion

            var sunvertex = new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(new Color[] { Color.FromArgb(255, 220, 220, 10), Color.FromArgb(255, 0, 0, 0) });
            items.Add(sunvertex);
            var sunshader = new GLShaderPipeline(sunvertex, new GLPLStarSurfaceFragmentShader());
            items.Add(sunshader);
            var shapebuf = new GLBuffer();
            items.Add(shapebuf);
            var shape = GLSphereObjectFactory.CreateSphereFromTriangles(1, 0.5f);
            shapebuf.AllocateFill(shape);

            int bufferfindbinding = 1;
            findshader = items.NewShaderPipeline(null, sunvertex, null, null, new GLPLGeoShaderFindTriangles(bufferfindbinding, 16), null, null, null);

            int texunitspergroup = 16;      // opengl minimum texture units per frag shader

            //var textshader = new GLShaderPipeline(new GLPLVertexShaderQuadTextureWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexedMulti(0,0,true, texunitspergroup));
            var textshader = new TextShader(texunitspergroup);
            items.Add(textshader);
            Font fnt = new Font("MS sans serif", 16f);

            if ( true )
            {
                int maxstars = 1000;    // this is an aspriation, depends on fragmentation of the system

                dataindirectbuffer = new GLVertexBufferIndirect(items,maxstars * (GLBuffer.Vec4size + GLBuffer.Mat4size), GLBuffer.WriteIndirectArrayStride * 100, true);
                var textarray = new GLTexture2DArray(128, 32, maxstars);

                int SectorSize = 10;

                {
                    Vector3 pos = new Vector3(-20, 0, -15);
                    Vector4[] array = new Vector4[10];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    dataindirectbuffer.Fill(array, 0, array.Length, 0, shape.Length, 0, array.Length, -1);

                    Matrix4[] matrix = new Matrix4[array.Length];
                    for( int i = 0; i < array.Length; i++ )
                    {
                        int imgpos = textarray.DepthIndex;
                        textarray.DrawText("A" + i, fnt, Color.White, Color.Blue, -1);
                        var mat = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix(new Vector3(array[i].X, array[i].Y + 0.6f, array[i].Z),
                                        new Vector3(1, 0, 0.2f),
                                        new Vector3(-90F.Radians(), 0, 0),
                                        imagepos:imgpos);
                        matrix[i] = mat;
                    }

                    dataindirectbuffer.Vertex.AlignMat4();          // instancing counts in mat4 sizes (mat4 0 @0, mat4 1 @ 64 etc) so align to it
                    dataindirectbuffer.Fill(matrix, 0, matrix.Length, 1, 4, 0, array.Length, -1);
                }

                if (true)
                {
                    Vector3 pos = new Vector3(-20, 0, 0);
                    Vector4[] array = new Vector4[5];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    dataindirectbuffer.Fill(array, 0, array.Length, 0, shape.Length, 0, array.Length, -1);
                }

                if (true)
                {
                    Vector3 pos = new Vector3(-20, 0, 15);
                    Vector4[] array = new Vector4[10];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    dataindirectbuffer.Fill(array, 0, array.Length, 0, shape.Length, 0, array.Length, -1);

                    Matrix4[] matrix = new Matrix4[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        int imgpos = textarray.DepthIndex;
                        textarray.DrawText("C" + i, fnt, Color.White, Color.Red, -1);
                        var mat = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix(new Vector3(array[i].X, array[i].Y + 0.6f, array[i].Z),
                                        new Vector3(1, 0, 0.2f),
                                        new Vector3(-90F.Radians(), 0, 0),
                                        imagepos: imgpos);
                        matrix[i] = mat;
                    }

                    dataindirectbuffer.Vertex.AlignMat4();          // instancing countis in mat4 sizes (mat4 0 @0, mat4 1 @ 64 etc) so align to it
                    dataindirectbuffer.Fill(matrix, 0, matrix.Length, 1, 4, 0, array.Length, -1);
                }


                int[] indirectints0 = dataindirectbuffer.Indirects[0].ReadInts(0, 12);
                int[] indirectints1 = dataindirectbuffer.Indirects[1].ReadInts(0, 4);
                float[] worldpos = dataindirectbuffer.Vertex.ReadFloats(0, 3*2*4);

                if (true)
                {
                    GLRenderState rt = GLRenderState.Tri();     // render is triangles, with no depth test so we always appear
                    rt.DepthTest = true;
                    rt.DepthClamp = true;

                    var renderer = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.Triangles, rt,
                                                                                shapebuf, 0, 0,     // binding 0 is shapebuf, offset 0, no draw count 
                                                                                dataindirectbuffer.Vertex, 0, // binding 1 is vertex's world positions, offset 0
                                                                                null, 0, 1);        // no ic, second divisor 1
                    renderer.IndirectBuffer = dataindirectbuffer.Indirects[0];
                    renderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
                    renderer.DrawCount = 3;
                    renderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

                    rObjects.Add(sunshader, "sunshader", renderer);
                }

                if (true)
                {

                    var rc = GLRenderState.Quads();
                    rc.CullFace = true;
                    rc.DepthTest = true;
                    rc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

                    var renderer = GLRenderableItem.CreateMatrix4(items, PrimitiveType.Quads, rc, 
                                                                        dataindirectbuffer.Vertex, 0, 0, //attach buffer with matrices, no draw count
                                                                         new GLRenderDataTexture(textarray,0), 
                                                                         0,1);     //no ic, and matrix divide so 1 matrix per vertex set
                    renderer.IndirectBuffer = dataindirectbuffer.Indirects[1];
                    renderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
                    renderer.DrawCount = 2;
                    renderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

                    rObjects.Add(textshader, "textshader", renderer);
                }
            }

            if (true)
            {
                GLRenderState starrc = GLRenderState.Tri();     // render is triangles, with no depth test so we always appear
                starrc.DepthTest = true;
                starrc.DepthClamp = true;

                var textrc = GLRenderState.Quads();
                textrc.DepthTest = true;
                textrc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

                sl = new GLObjectsWithLabels();
                var ris = sl.Create(texunitspergroup, 50, 10, shapebuf, shape.Length , starrc, PrimitiveType.Triangles, new Size(128,32), textrc);
                rObjects.Add(sunshader, "SLsunshade", ris.Item1);
                rObjects.Add(textshader, "SLtextshade", ris.Item2);
                items.Add(sl);

                int SectorSize = 10;
                {
                    Vector3 pos = new Vector3(0, 0, -15);
                    Vector4[] array = new Vector4[10];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                        text[i] = "A.r" + i;
                    }

                    var mats = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = BitMapHelpers.DrawTextIntoFixedSizeBitmaps(sl.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    sl.Add("GA",text, array, mats, bmps);
                    BitMapHelpers.Dispose(bmps);
                }
                {
                    Vector3 pos = new Vector3(0, 0, 0);
                    Vector4[] array = new Vector4[20];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                        text[i] = "B." + i;
                    }

                    sl.Add("GB",text, array, text, fnt, Color.White, Color.DarkBlue, new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false, null, 0.5f, new Vector3(0, 0.6f,0));
                }
                {
                    Vector3 pos = new Vector3(0, 0, 15);
                    Vector4[] array = new Vector4[10];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                        text[i] = "C." + i;
                    }

                    sl.Add("GC", text, array, text, fnt, Color.White, Color.DarkBlue, new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false, null, 0.5f, new Vector3(0, 0.6f, 0));
                }

                System.Diagnostics.Debug.WriteLine($"Sets {sl.Blocks} Removed {sl.BlocksRemoved}");
            }

            // Sets of..

            if (true)
            {
                GLRenderState starrc = GLRenderState.Tri();     // render is triangles, with no depth test so we always appear
                starrc.DepthTest = true;
                starrc.DepthClamp = true;

                var textrc = GLRenderState.Quads();
                textrc.DepthTest = true;
                textrc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

                slset = new GLSetOfObjectsWithLabels("SLSet", rObjects, true ? 4 : texunitspergroup, 
                                                            50, 10,
                                                            sunshader, shapebuf, shape.Length, starrc, PrimitiveType.Triangles,
                                                            textshader, new Size(128, 32), textrc,
                                                            10);
                items.Add(slset);

                int SectorSize = 10;
                {
                    Vector3 pos = new Vector3(20, 0, -15);
                    Vector4[] array = new Vector4[10];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                        text[i] = "S.A.r" + i;
                    }

                    var mats = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    slset.Add("GA", text, array, mats, bmps);
                    BitMapHelpers.Dispose(bmps);
                }
                {
                    Vector3 pos = new Vector3(20, 0, 0);
                    Vector4[] array = new Vector4[10];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                        text[i] = "S.B." + i;
                    }

                    var mats = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    slset.Add("GB", text, array, mats, bmps);
                    BitMapHelpers.Dispose(bmps);
                }
                {
                    Vector3 pos = new Vector3(20, 0, 15);
                    Vector4[] array = new Vector4[10];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                        text[i] = "S.C." + i;
                    }

                    var mats = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    slset.Add("GC", text, array, mats, bmps);
                    BitMapHelpers.Dispose(bmps);
                }
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
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true, OtherKeys);
              gl3dcontroller.Redraw();
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {

            if (kb.HasBeenPressed(Keys.F1, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                sl.Remove("GA");
                System.Diagnostics.Debug.WriteLine($"Blocks {sl.Blocks} Removed {sl.BlocksRemoved}");
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F2, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                sl.Remove("GB");
                System.Diagnostics.Debug.WriteLine($"Blocks {sl.Blocks} Removed {sl.BlocksRemoved}");
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F3, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                sl.Remove("GC");
                System.Diagnostics.Debug.WriteLine($"Blocks {sl.Blocks} Removed {sl.BlocksRemoved}");
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F4, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                {
                    int SectorSize = 10;
                    Vector3 pos = new Vector3(0, 0, 30);
                    Vector4[] array = new Vector4[10];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                        text[i] = "D." + i;
                    }

                    Font fnt = new Font("MS sans serif", 16f);
                    sl.Add("GD", text, array, text, fnt, Color.White, Color.DarkBlue, new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false, null, 0.5f, new Vector3(0, 0.6f, 0));
                }
                gl3dcontroller.Redraw();
                System.Diagnostics.Debug.WriteLine($"Sets {sl.Blocks} Removed {sl.BlocksRemoved}");
            }




            if (kb.HasBeenPressed(Keys.F5, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GA");
                gl3dcontroller.Redraw();
                System.Diagnostics.Debug.WriteLine($"Objects {slset.Objects()}");
            }

            if (kb.HasBeenPressed(Keys.F6, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GB");
                gl3dcontroller.Redraw();
                System.Diagnostics.Debug.WriteLine($"Objects {slset.Objects()}");
            }

            if (kb.HasBeenPressed(Keys.F7, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GC");
                gl3dcontroller.Redraw();
                System.Diagnostics.Debug.WriteLine($"Objects {slset.Objects()}");
            }

            if (kb.HasBeenPressed(Keys.F8, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                int SectorSize = 10;
                Vector3 pos = new Vector3(20, 0, 30);
                Vector4[] array = new Vector4[10];
                string[] text = new string[array.Length];
                Random rnd = new Random();
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    text[i] = "S.D." + i;
                }

                Font fnt = new Font("MS sans serif", 16f);
                var mats = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                var bmps = BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkGreen, 0.5f);

                slset.Add("GD", text, array, mats, bmps);
                BitMapHelpers.Dispose(bmps);
                gl3dcontroller.Redraw();

                System.Diagnostics.Debug.WriteLine($"Objects {slset.Objects()} sets {slset.Count}");
            }


            if (kb.HasBeenPressed(Keys.F9, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GD");
                gl3dcontroller.Redraw();
                System.Diagnostics.Debug.WriteLine($"Objects {slset.Objects()} sets {slset.Count}");
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

        protected void GLMouseClick(object v, GLMouseEventArgs e)
        {
            if (sl != null)
            {
                var index = sl.Find(findshader, glwfc.RenderState, e.WindowLocation, gl3dcontroller.MatrixCalc.ViewPort.Size);

                if (index != null)
                {
                    var namelist = sl.UserTags[index.Item1] as string[];
                    System.Diagnostics.Debug.WriteLine($"... {namelist[index.Item2]}");
                }
            }

            if (slset != null)
            {
                var find = slset.Find(findshader, glwfc.RenderState, e.WindowLocation, gl3dcontroller.MatrixCalc.ViewPort.Size);
                if (find != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SLSet {find.Item1} {find.Item2} {find.Item3}");
                    var set = slset[find.Item1];
                    var namelist = set.UserTags[find.Item2] as string[];
                    System.Diagnostics.Debug.WriteLine($"... {namelist[find.Item3]}");
                }
            }

        }
    }

}


