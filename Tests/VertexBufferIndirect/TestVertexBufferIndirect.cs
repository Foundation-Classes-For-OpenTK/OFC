/*
 * Copyight 2019-2021 Robbyxp1 @ github.com
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
using GLOFC.GL4.Shaders.Geo;
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.Shaders.Stars;
using GLOFC.GL4.Buffers;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;

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

        GLObjectsWithLabels slc;
        GLShaderPipeline findshaderc;

        GLObjectsWithLabels slt;
        GLShaderPipeline findshadert;

        GLSetOfObjectsWithLabels slset;

        public TestVertexBufferIndirect()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer, null, 4, 6);

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
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.01f;
            gl3dcontroller.MatrixCalc.PerspectiveFarZDistance= 1000f;
            gl3dcontroller.ZoomDistance = 20F;
            gl3dcontroller.PosCamera.ZoomScaling = 1.02F;

            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(110f, 0, 0f), 1F);
            glwfc.MouseClick += GLMouseClick;

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 250.0f;
            };

            items.Add(new GLColorShaderWorld(), "COSW");
            items.Add(new GLColorShaderObjectTranslation(), "COSOT");

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

            #endregion

            #region Coloured triangles
            if (false)
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

            // globe shape
            var shape = GLSphereObjectFactory.CreateTexturedSphereFromTriangles(2, 0.5f);

            // globe vertex
            var starshapebuf = new GLBuffer();
            items.Add(starshapebuf);
            starshapebuf.AllocateFill(shape.Item1);

            // globe tex coord
            var starshapetexbuf = new GLBuffer();
            items.Add(starshapetexbuf);
            starshapetexbuf.AllocateFill(shape.Item2);

            // sunshader for color 
            var sunvertexcolor = new GLPLVertexShaderModelCoordWorldAutoscale(new Color[] { Color.FromArgb(255, 220, 220, 10), Color.FromArgb(255, 0, 0, 0) });
            items.Add(sunvertexcolor);
            var sunshadercolor = new GLShaderPipeline(sunvertexcolor, new GLPLStarSurfaceColorFragmentShader());
            items.Add(sunshadercolor);

            // sunshader for texture
            var sunvertextexture = new GLPLVertexShaderModelWorldTextureAutoScale(20.0f, 1F, 2F, true);
            var sunfragmenttexture = new GLPLFragmentShaderTexture2DWSelectorSunspot();
            var sunshadertexture = new GLShaderPipeline(sunvertextexture, sunfragmenttexture);
            items.Add(sunshadertexture);

            // text shader
            int texunitspergroup = 16;      // opengl minimum texture units per frag shader
            // text shader uses a precomputed vertex shader (no vertex needed) expecting four draw points
            var textshader = new GLShaderPipeline(new GLPLVertexShaderMatrixTriStripTexture(), new GLPLFragmentShaderTexture2DIndexMulti(0, 0, true, texunitspergroup));
            items.Add(textshader);

            // a texture 2d array with various star images
            GLTexture2DArray starimagearray = new GLTexture2DArray();
            Bitmap[] starbmps = new Bitmap[] { Properties.Resources.O, Properties.Resources.A, Properties.Resources.F, Properties.Resources.G, Properties.Resources.N };
            Bitmap[] starbmpsreduced = starbmps.CropImages(new RectangleF(16, 16, 68, 68));
            //for (int b = 0; b < starbmpsreduced.Length; b++)  starbmpsreduced[b].Save(@"c:\code\" + $"star{b}.bmp", System.Drawing.Imaging.ImageFormat.Png);
            starimagearray.CreateLoadBitmaps(starbmpsreduced, SizedInternalFormat.Rgba8, ownbmp: true);
            items.Add(starimagearray);

            // to attach the texture to the render
            GLRenderDataTexture starimagerdi = new GLRenderDataTexture(starimagearray);  // RDI is used to attach the texture

            // find shader for sunvertexcolor
            GLStorageBlock block = new GLStorageBlock(20);
            GLStorageBlock debug = new GLStorageBlock(5);
            debug.AllocateBytes(5000);
            findshaderc = items.NewShaderPipeline(null, sunvertexcolor, null, null, new GLPLGeoShaderFindTriangles(block, 64, obeyculldistance: true, debugbuffer: debug), null, null, null);


            // find shader for sunvertexttexture
            GLStorageBlock blockt = new GLStorageBlock(21);
            GLStorageBlock debugt = new GLStorageBlock(6);
            debugt.AllocateBytes(5000);
            findshadert = items.NewShaderPipeline(null, sunvertextexture, null, null, new GLPLGeoShaderFindTriangles(blockt, 64, obeyculldistance: true, debugbuffer: debugt), null, null, null);

            Font fnt = new Font("MS sans serif", 16f);

            // DEMO Indirect command buffer manually set up

            if ( false )
            {
                int maxstars = 1000;    // this is an aspriation, depends on fragmentation of the system

                dataindirectbuffer = new GLVertexBufferIndirect(items,maxstars * (GLBuffer.Vec4size + GLBuffer.Mat4size), GLBuffer.WriteIndirectArrayStride * 100, true);
                var textarray = new GLTexture2DArray(128, 32, maxstars, SizedInternalFormat.Rgba8);

                int SectorSize = 10;

                if (true)
                {
                    // fill buffer with Vector4, store indirects into 0

                    Vector3 pos = new Vector3(-20, 0, -15);
                    Vector4[] array = new Vector4[10];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    }
                    dataindirectbuffer.Fill(array, 0, array.Length, 0, shape.Item1.Length, 0, array.Length, -1);

                    // fill buffer with Matrix4, store indirects into 1

                    Matrix4[] matrix = new Matrix4[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        int imgpos = textarray.DepthIndex;
                        textarray.DrawText("A" + i, fnt, Color.White, Color.Blue, -1);
                        var mat = GLStaticsMatrix4.CreateMatrix(new Vector3(array[i].X, array[i].Y + 0.6f, array[i].Z),
                                        new Vector3(1, 0, 0.2f),
                                        new Vector3(-90F.Radians()),
                                        imagepos: imgpos);
                        matrix[i] = mat;
                    }

                    dataindirectbuffer.Vertex.AlignMat4();          // instancing counts in mat4 sizes (mat4 0 @0, mat4 1 @ 64 etc) so align to it
                    dataindirectbuffer.Fill(matrix, 0, matrix.Length, 1,    // write to indirect 1
                                            4, 0, array.Length, -1);        // command is to draw 4 vertex, base index, instance repeat of array length
                }

                if (true)
                {
                    // fill in more vector4 into indirect 0
                    Vector3 pos = new Vector3(-30, 0, 0);
                    Vector4[] array = new Vector4[20];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    dataindirectbuffer.Fill(array, 0, array.Length, 0, shape.Item1.Length, 0, array.Length, -1);
                }

                if (true)
                {
                    // add in some more into indirect 0
                    Vector3 pos = new Vector3(-20, 0, 15);
                    Vector4[] array = new Vector4[10];
                    Random rnd = new Random(23);
                    for (int i = 0; i < array.Length; i++)
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);
                    dataindirectbuffer.Fill(array, 0, array.Length, 0, shape.Item1.Length, 0, array.Length, -1);

                    // and 1

                    Matrix4[] matrix = new Matrix4[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        int imgpos = textarray.DepthIndex;
                        textarray.DrawText("C" + i, fnt, Color.White, Color.Red, -1);
                        var mat = GLStaticsMatrix4.CreateMatrix(new Vector3(array[i].X, array[i].Y + 0.6f, array[i].Z),
                                        new Vector3(1, 0, 0.2f),
                                        new Vector3(-90F.Radians()),
                                        imagepos: imgpos);
                        matrix[i] = mat;
                    }

                    dataindirectbuffer.Vertex.AlignMat4();          // instancing countis in mat4 sizes (mat4 0 @0, mat4 1 @ 64 etc) so align to it
                    dataindirectbuffer.Fill(matrix, 0, matrix.Length, 1, 4, 0, array.Length, -1);
                }


                if ( true )
                { 
                    // draw from indirect 0

                    GLRenderState rt = GLRenderState.Tri();     // render is triangles, with no depth test so we always appear
                    rt.DepthTest = true;
                    rt.DepthClamp = true;

                    var renderer = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.Triangles, rt,
                                                                                starshapebuf, 0, 0,     // binding 0 is shapebuf, offset 0, no draw count 
                                                                                dataindirectbuffer.Vertex, 0, // binding 1 is vertex's world positions, offset 0
                                                                                null, 0, 1);        // no ic, second divisor 1
                    renderer.IndirectBuffer = dataindirectbuffer.Indirects[0];
                    renderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
                    renderer.DrawCount = dataindirectbuffer.Indirects[0].Length / 16; // Set no of para sets filled
                    renderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

                    // sunshader requires triangles vertexes
                    rObjects.Add(sunshadercolor, "sunshader", renderer);
                }

                if (true)
                {
                    // draw from buffer 1 using GLPLVertexShaderMatrixTriStripTexture the text labels

                    var rc = GLRenderState.Tri();
                    rc.CullFace = true;     // check we are facing the correct way
                    rc.DepthTest = false;
                    rc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

                    var renderer = GLRenderableItem.CreateMatrix4(items, PrimitiveType.TriangleStrip, rc,
                                                                        dataindirectbuffer.Vertex, 0, 0, //attach buffer with matrices, no draw count
                                                                         new GLRenderDataTexture(textarray, 0),
                                                                         0, 1);     //no ic, and matrix divide so 1 matrix per vertex set
                    renderer.IndirectBuffer = dataindirectbuffer.Indirects[1];
                    renderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
                    renderer.DrawCount = dataindirectbuffer.Indirects[1].Length / 16; // Set no of para sets filled
                    renderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

                    rObjects.Add(textshader, "textshader", renderer);
                }

                //int[] indirectints0 = dataindirectbuffer.Indirects[0].ReadInts(0, 12);        // debug
                //int[] indirectints1 = dataindirectbuffer.Indirects[1].ReadInts(0, 4);
                //float[] worldpos = dataindirectbuffer.Vertex.ReadFloats(0, 3*2*4);

            }

            // DEMO Object with Labels - non textured colour

            if (false)
            {
                GLRenderState starrc = GLRenderState.Tri();     // render is triangles, with no depth test so we always appear
                starrc.DepthTest = true;
                starrc.DepthClamp = true;
                starrc.ClipDistanceEnable = 1;

                var textrc = GLRenderState.Tri();       // text render is triangles are going to cull primitives which are deleted
                textrc.DepthTest = true;
                textrc.ClipDistanceEnable = 1;

                slc = new GLObjectsWithLabels();
                var ris = slc.Create(texunitspergroup, 50, 50, starshapebuf, null, shape.Item1.Length, starrc, PrimitiveType.Triangles, null ,new Size(128, 32), textrc, SizedInternalFormat.Rgba8, 50);
                rObjects.Add(sunshadercolor, "SLsunshadeRO", ris.Item1);
                rObjects.Add(textshader, "SLtextshadeRO", ris.Item2);
                items.Add(slc);

                int SectorSize = 10;

                if (true)
                {
                    Vector3 pos = new Vector3(0, 0, -15);
                    Vector4[] array = new Vector4[5];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (i == 1)
                        {
                            array[i] = new Vector4(0, 0, 0, -1);
                        }
                        else
                            array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), 0);

                        text[i] = "A.r" + i;
                    }

                    var mats = GLStaticsMatrix4.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slc.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    List<GLObjectsWithLabels.BlockRef> bref = new List<GLObjectsWithLabels.BlockRef>();
                    slc.Add(array, mats, bmps, bref);
                    GLOFC.Utils.BitMapHelpers.Dispose(bmps);
                }

                if (false)
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

                    List<GLObjectsWithLabels.BlockRef> bref = new List<GLObjectsWithLabels.BlockRef>();
                    slc.Add(array, text, fnt, Color.White, Color.DarkBlue, new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false, null, 0.5f, new Vector3(0, 0.6f, 0), bref);
                }

                if (false)
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

                    List<GLObjectsWithLabels.BlockRef> bref = new List<GLObjectsWithLabels.BlockRef>();
                    slc.Add(array, text, fnt, Color.White, Color.DarkBlue, new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false, null, 0.5f, new Vector3(0, 0.6f, 0), bref);
                }


                System.Diagnostics.Debug.WriteLine($"Sets {slc.Blocks} Removed {slc.BlocksRemoved}");
            }

            // DEMO Object with Labels - textured

            if (true)
            {
                GLRenderState starrc = GLRenderState.Tri();     // render is triangles, with no depth test so we always appear
                starrc.DepthTest = true;
                starrc.DepthClamp = false;
                starrc.ClipDistanceEnable = 1;

                var textrc = GLRenderState.Tri();       // text render is triangles are going to cull primitives which are deleted
                textrc.DepthTest = true;
                textrc.ClipDistanceEnable = 1;

                slt = new GLObjectsWithLabels();
                var ris = slt.Create(texunitspergroup, 50, 50, starshapebuf, starshapetexbuf, shape.Item1.Length, starrc, PrimitiveType.Triangles, starimagerdi, new Size(128, 32), textrc, SizedInternalFormat.Rgba8, 50);
                rObjects.Add(sunshadertexture, "SLsunshadeT", ris.Item1);
                rObjects.Add(textshader, "SLtextshadeT", ris.Item2);
                items.Add(slt);

                int SectorSize = 10;

                if (true)
                {
                    Vector3 pos = new Vector3(-10, 0, -15);
                    Vector4[] array = new Vector4[2];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), i % 5);

                        text[i] = "TG0.r" + i;
                    }

                    var mats = GLStaticsMatrix4.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slt.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    List<GLObjectsWithLabels.BlockRef> bref = new List<GLObjectsWithLabels.BlockRef>();
                    slt.Add(array, mats, bmps, bref);
                    GLOFC.Utils.BitMapHelpers.Dispose(bmps);
                }
                if (true)
                {
                    Vector3 pos = new Vector3(-12, 0, -15);
                    Vector4[] array = new Vector4[2];
                    string[] text = new string[array.Length];
                    Random rnd = new Random(31);
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = new Vector4(pos.X + rnd.Next(SectorSize), pos.Y + rnd.Next(SectorSize), pos.Z + rnd.Next(SectorSize), i % 5);

                        text[i] = "TG1.r" + i;
                    }

                    var mats = GLStaticsMatrix4.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slt.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    List<GLObjectsWithLabels.BlockRef> bref = new List<GLObjectsWithLabels.BlockRef>();
                    slt.Add(array, mats, bmps, bref);
                    GLOFC.Utils.BitMapHelpers.Dispose(bmps);
                }
            }


            // Sets of..

            if (false)
            {
                GLRenderState starrc = GLRenderState.Tri();     // render is triangles, with no depth test so we always appear
                starrc.DepthTest = true;
                starrc.DepthClamp = true;

                var textrc = GLRenderState.Tri();
                textrc.DepthTest = true;
                textrc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

                slset = new GLSetOfObjectsWithLabels("SLSet", rObjects, true ? 4 : texunitspergroup,
                                                            50, 10,
                                                            sunshadercolor, starshapebuf, null, shape.Item1.Length, starrc, PrimitiveType.Triangles, null,
                                                            textshader, new Size(128, 32), textrc, SizedInternalFormat.Rgba8,
                                                            3);
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

                    var mats = GLStaticsMatrix4.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    slset.Add("GA", text, array, mats, bmps);
                    GLOFC.Utils.BitMapHelpers.Dispose(bmps);
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

                    var mats = GLStaticsMatrix4.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    slset.Add("GB", text, array, mats, bmps);
                    GLOFC.Utils.BitMapHelpers.Dispose(bmps);
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

                    var mats = GLStaticsMatrix4.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                    var bmps = GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkBlue, 0.5f);

                    slset.Add("GC", text, array, mats, bmps);
                    GLOFC.Utils.BitMapHelpers.Dispose(bmps);
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
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
            
            var azel = gl3dcontroller.PosCamera.EyePosition.AzEl(gl3dcontroller.PosCamera.LookAt, true);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
              gl3dcontroller.Redraw();
        }

        static int tagn = 0;

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {

            if (kb.HasBeenPressed(Keys.F5, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GA");
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F6, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GB");
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F7, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GC");
                gl3dcontroller.Redraw();
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
                var mats = GLStaticsMatrix4.CreateMatrices(array, new Vector3(0, 0.6f, 0), new Vector3(2f, 0, 0.4f), new Vector3(-90F.Radians(), 0, 0), true, false);
                var bmps = GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmaps(slset.LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.White, Color.DarkGreen, 0.5f);

                slset.Add("GD" + (tagn++).ToString(), text, array, mats, bmps);
                GLOFC.Utils.BitMapHelpers.Dispose(bmps);
                gl3dcontroller.Redraw();
            }

            if (kb.HasBeenPressed(Keys.F9, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.Remove("GD");
                gl3dcontroller.Redraw();
            }
            if (kb.HasBeenPressed(Keys.F12, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                slset.RemoveOldest(1);
                gl3dcontroller.Redraw();
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
            if (e.Button == GLMouseEventArgs.MouseButtons.Right)
            {
                if (slt != null)
                {
                    var index = slt.Find(findshadert, glwfc.RenderState, e.WindowLocation, gl3dcontroller.MatrixCalc.ViewPort.Size,4);

                    if (index != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"found group {index.Item1} index {index.Item2}");
                        //var namelist = sl.UserTags[index.Item1] as string[];
                        //                   System.Diagnostics.Debug.WriteLine($"... {namelist[index.Item2]}");
                    }
                }

                if (slset != null)
                {
                    var find = slset.FindBlock(findshaderc, glwfc.RenderState, e.WindowLocation, gl3dcontroller.MatrixCalc.ViewPort.Size,4);
                    if (find != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"SLSet {find.Item2} {find.Item3} {find.Item4} {find.Item5}");
                        var userdata = slset.UserData[find.Item1[0].tag] as string[];

                        System.Diagnostics.Debug.WriteLine($"... {userdata[find.Item4]}");
                    }
                }
            }

        }
    }

}


