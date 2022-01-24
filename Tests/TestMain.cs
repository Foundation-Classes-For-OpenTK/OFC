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

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GLOFC.WaveFront;
using GLOFC.Utils;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.Operations;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using GLOFC.GL4.Wavefront;

namespace TestOpenTk
{
    public partial class TestMain : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLRenderProgramSortedList rObjectscw = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        GLStorageBlock dataoutbuffer;

        public TestMain()
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
            gl3dcontroller.ZoomDistance = 60F;
            gl3dcontroller.PosCamera.ZoomScaling = 1.1f;
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(110f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 30.0f;
            };

            {
                System.Diagnostics.Debug.WriteLine($"UBS={GL4Statics.GetMaxUniformBlockSize()}");
                GL4Statics.GetMaxUniformBuffers(out int v, out int f, out int g, out int tc, out int te);
                System.Diagnostics.Debug.WriteLine($"UB v{v} f{f} g{g} tc{tc} te{te}");
                System.Diagnostics.Debug.WriteLine($"tex layers {GL4Statics.GetMaxTextureDepth()} ");
                System.Diagnostics.Debug.WriteLine($"Vertex attribs {GL4Statics.GetMaxVertexAttribs()} ");
            }

            items.Add( new GLTexturedShaderObjectTranslation(),"TEXOT");
            items.Add(new GLTexturedShaderObjectTranslation(), "TEXOTNoRot");
            items.Add(new GLColorShaderWorld(), "COSW");
            items.Add(new GLColorShaderObjectTranslation(), "COSOT");
            items.Add(new GLFixedColorShaderObjectTranslation(Color.Goldenrod), "FCOSOT");
            items.Add(new GLTexturedShaderObjectCommonTranslation(), "TEXOCT");

            items.Add( new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8)  ,           "dotted"    );
            items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "logo8bpp");
            items.Add(new GLTexture2D(Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "dotted2");
            items.Add(new GLTexture2D(Properties.Resources.wooden, SizedInternalFormat.Rgba8), "wooden");
            items.Add(new GLTexture2D(Properties.Resources.wooden, SizedInternalFormat.Rgba8), "wood");
            items.Add(new GLTexture2D(Properties.Resources.shoppinglist, SizedInternalFormat.Rgba8), "shoppinglist");
            items.Add(new GLTexture2D(Properties.Resources.golden, SizedInternalFormat.Rgba8), "golden");
            items.Add(new GLTexture2D(Properties.Resources.smile5300_256x256x8, SizedInternalFormat.Rgba8), "smile");
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");

            var dot2 = items.Tex("dotted2");
            dot2.ClearSubImage(0, 50, 50, 0,   // x=0,y=0, texture 0 to clear     demo this
                                    50, 50, 1,
                                    1f, 1f, 0, 0.3f);


            ulong ctrl = 0xffffffff;
            ctrl = (1<<13) | (1<<12) | (1<<11);
            ctrl |= 1;
            ctrl |= 0xffffffff;
          //  ctrl = (1 << 25);



            #region coloured lines

            if( (ctrl & (1<<0)) != 0)
            {
                rObjects.Add(new GLOperationClearDepthBuffer());     // demo the operation via the shader interface

                GLRenderState lines = GLRenderState.Lines(1);

                rObjects.Add(items.Shader("COSW"), new GLOperationClearDepthBuffer());     // demo a RI operation inside a particular shader

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(-100, 0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );


                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(100, 0, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green }));
            }
            if( (ctrl & (1<<1)) != 0)
            {
                GLRenderState lines = GLRenderState.Lines(1);

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(-100, 10, 100), new Vector3(10, 0, 0), 21),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );
                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 10, -100), new Vector3(100, 10, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );
            }

            #endregion

            #region Sphere mapping 

            if ((ctrl & (1 << 2)) != 0)
            {
                GLRenderState rc1 = GLRenderState.Tri();
                rObjects.Add(items.Shader("TEXOT"), "sphere7",
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rc1,
                            GLSphereObjectFactory.CreateTexturedSphereFromTriangles(4, 4.0f),
                            new GLRenderDataTranslationRotationTexture(items.Tex("moon"), new Vector3(4, 0, 0))
                            ));

            }

            #endregion

            #region Coloured triangles
            if ( (ctrl & (1<<3)) != 0)
            {
                GLRenderState rc = GLRenderState.Tri();
                rc.CullFace = false;

                rObjects.Add(items.Shader("COSOT"), "scopen",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f, new GLCubeObjectFactory.Sides[] { GLCubeObjectFactory.Sides.Bottom, GLCubeObjectFactory.Sides.Top, GLCubeObjectFactory.Sides.Left, GLCubeObjectFactory.Sides.Right }),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(-6, 0, 0))
                            ));


                rObjects.Add(items.Shader("COSOT"), "scopen-op",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f, new GLCubeObjectFactory.Sides[] { GLCubeObjectFactory.Sides.Bottom, GLCubeObjectFactory.Sides.Top, GLCubeObjectFactory.Sides.Left, GLCubeObjectFactory.Sides.Right }),
                                            new Color4[] { Color4.Red, Color4.Green, Color4.Blue, Color4.White, Color4.Cyan, Color4.Orange },
                                            new GLRenderDataTranslationRotation(new Vector3(-6, 0, -2))
                            ));

                rObjects.Add(items.Shader("COSOT"), "sphere1",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Triangles, rc,
                                        GLSphereObjectFactory.CreateSphereFromTriangles(3, 2.0f),
                                        new Color4[] { Color4.Red, Color4.Green, Color4.Blue, },
                                        new GLRenderDataTranslationRotation(new Vector3(-6, 0, -4))
                            ));
            }

            #endregion


            #region view marker

            if( (ctrl & (1<<4)) != 0)
            {
                GLRenderState rc = GLRenderState.Points(10);

                rObjects.Add(items.Shader("COSOT"), "viewpoint",
                        GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points, rc,
                                       GLCubeObjectFactory.CreateVertexPointCube(1f), new Color4[] { Color.Purple },
                                 new GLRenderDataTranslationRotation(new Vector3(0,10,0))
                                 ));
            }

            #endregion


            #region coloured points
            if( (ctrl & (1<<5)) != 0)
            {
                GLRenderState rc2 = GLRenderState.Points(2);

                rObjects.Add(items.Shader("COSOT"), "pc",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points, rc2,
                                   GLCubeObjectFactory.CreateVertexPointCube(1f), new Color4[] { Color4.Yellow },
                             new GLRenderDataTranslationRotation(new Vector3(-4, 0, 0))
                             ));
                rObjects.Add(items.Shader("COSOT"), "pc2",
                    GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points, rc2,
                                   GLCubeObjectFactory.CreateVertexPointCube(1f), new Color4[] { Color4.Green, Color4.White, Color4.Purple, Color4.Blue },
                             new GLRenderDataTranslationRotation(new Vector3(-4, 0, -2))
                             ));
                rObjects.Add(items.Shader("COSOT"), "cp",
                    GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points,rc2,
                                   GLCubeObjectFactory.CreateVertexPointCube(1f), new Color4[] { Color4.Red },
                             new GLRenderDataTranslationRotation(new Vector3(-4, 0, -4))
                             ));
                rObjects.Add(items.Shader("COSOT"), "dot2-1",
                    GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points,rc2,
                                   GLCubeObjectFactory.CreateVertexPointCube(1f), new Color4[] { Color.Red, Color.Green, Color.Blue, Color.Cyan, Color.Yellow, Color.Yellow, Color.Yellow, Color.Yellow },
                             new GLRenderDataTranslationRotation(new Vector3(-4, 0, -6))
                             ));
                rObjects.Add(items.Shader("COSOT"), "sphere2",
                    GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points,rc2,
                                   GLSphereObjectFactory.CreateSphereFromTriangles(3, 1.0f), new Color4[] { Color4.Red, Color4.Green, Color4.Blue, },
                            new GLRenderDataTranslationRotation(new Vector3(-4, 0, -8))));

                rObjects.Add(items.Shader("COSOT"), "sphere4",
                            GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points, rc2,
                                   GLSphereObjectFactory.CreateSphereFromTriangles(2, 1.0f), new Color4[] { Color4.Red, Color4.Green, Color4.Blue, },
                                new GLRenderDataTranslationRotation(new Vector3(-4, 0, -12))));

                GLRenderState rc10 = GLRenderState.Points(10);

                rObjects.Add(items.Shader("COSOT"), "sphere3",
                    GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points, rc10,
                                   GLSphereObjectFactory.CreateSphereFromTriangles(1, 1.0f), new Color4[] { Color4.Red, Color4.Green, Color4.Blue, },
                            new GLRenderDataTranslationRotation(new Vector3(-4, 0, -10))));

            }

            #endregion


            #region textures
            if( (ctrl & (1<<6)) != 0)
            {
                GLRenderState rt = GLRenderState.Tri();

                rObjects.Add(items.Shader("TEXOT"),
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                                GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                                new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(-2, 30, 0))
                                ));


                rObjects.Add(items.Shader("TEXOT"), "EDDCube",
                            GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("logo8bpp"), new Vector3(-2, 1, -2))
                            ));

                rObjects.Add(items.Shader("TEXOT"), "woodbox",
                            GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("wooden"), new Vector3(-2, 2, -4))
                            ));

                GLRenderState rq = GLRenderState.Quads();

                rObjects.Add(items.Shader("TEXOT"),
                            GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                            GLShapeObjectFactory.CreateQuad(1.0f, 1.0f, new Vector3(0, 0, 0)), GLShapeObjectFactory.TexQuadCW,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(-2, 3, -6))
                            ));

                rObjects.Add(items.Shader("TEXOT"),
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                            GLShapeObjectFactory.CreateQuad(1.0f, 1.0f, new Vector3(0, 0, 0)), GLShapeObjectFactory.TexQuadCW,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(-2, 4, -8))
                            ));

                rObjects.Add(items.Shader("TEXOT"),
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                            GLShapeObjectFactory.CreateQuad(1.0f, 1.0f, new Vector3(0, 0, 0)), GLShapeObjectFactory.TexQuadCW,
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted"), new Vector3(-2, 5, -10))
                            ));

                GLRenderState rqnc = GLRenderState.Quads(cullface: false);

                rObjects.Add(items.Shader("TEXOT"), "EDDFlat",
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rqnc,
                    GLShapeObjectFactory.CreateQuad(2.0f, items.Tex("logo8bpp").Width, items.Tex("logo8bpp").Height, new Vector3(-0, 0, 0)), GLShapeObjectFactory.TexQuadCW,
                            new GLRenderDataTranslationRotationTexture(items.Tex("logo8bpp"), new Vector3(0, 0, 0))
                            ));

                rObjects.Add(items.Shader("TEXOT"),
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rqnc,
                            GLShapeObjectFactory.CreateQuad(1.5f, new Vector3( -90f.Radians(), 0, 0)), GLShapeObjectFactory.TexQuadCW,
                            new GLRenderDataTranslationRotationTexture(items.Tex("smile"), new Vector3(0, 0, -2))
                           ));

                rObjects.Add(items.Shader("TEXOCT"), "woodboxc1",
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("wooden"), new Vector3(0, 0, -4))
                            ));

                rObjects.Add(items.Shader("TEXOCT"), "woodboxc2",
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("wooden"), new Vector3(0, 0, -6))
                           ));

                rObjects.Add(items.Shader("TEXOCT"), "woodboxc3",
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("wooden"), new Vector3(0, 0, -8))
                            ));

                rObjects.Add(items.Shader("TEXOCT"), "sphere5",
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, 1.0f),
                            new GLRenderDataTranslationRotationTexture(items.Tex("wooden"), new Vector3(0, 0, -10))
                            ));

                rObjects.Add(items.Shader("TEXOCT"), "sphere6",
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, 1.5f),
                            new GLRenderDataTranslationRotationTexture(items.Tex("golden"), new Vector3(0, 0, -12))
                            ));

                var cyl = GLCylinderObjectFactory.CreateCylinderFromTriangles(3, 20, 20, 2, caps:true);

                GLRenderState rtri = GLRenderState.Tri();
                rObjects.Add(items.Shader("TEXOTNoRot"), "cylinder1",
                GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rtri, cyl,
                            new GLRenderDataTranslationRotationTexture(items.Tex("logo8bpp"), new Vector3(30, 0, 10))
                            ));

                // for this one, demo indexes and draw with CW to show it works.  Note we are not using primitive restart

                var cyl2 = GLCylinderObjectFactory.CreateCylinderFromTrianglesIndexes(3, 10, 20, 2, caps: true, ccw:false);

                rObjectscw.Add(items.Shader("TEXOTNoRot"), "cylinder2",
                        GLRenderableItem.CreateVector4Vector2Indexed(items, PrimitiveType.Triangles, rtri, cyl2,
                            new GLRenderDataTranslationRotationTexture(items.Tex("logo8bpp"), new Vector3(40, 0, 10))
                            ));


            }

            #endregion

            #region 2dArrays
            if( (ctrl & (1<<7)) != 0)
            {
                items.Add( new GLTexturedShader2DBlendWorld(), "TEX2DA");
                items.Add(new GLTexture2DArray(new Bitmap[] { Properties.Resources.mipmap2, Properties.Resources.mipmap3 }, SizedInternalFormat.Rgba8, 9), "2DArray2");

                GLRenderState rq = GLRenderState.Quads();

                rObjects.Add(items.Shader("TEX2DA"), "2DA",
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                            GLShapeObjectFactory.CreateQuad(2.0f), GLShapeObjectFactory.TexQuadCW,
                            new GLRenderDataTranslationRotationTexture(items.Tex("2DArray2"), new Vector3(-8, 0, 2))
                        ));


                items.Add( new GLTexture2DArray(new Bitmap[] { Properties.Resources.dotted, Properties.Resources.dotted2 }, SizedInternalFormat.Rgba8), "2DArray2-1");

                rObjects.Add(items.Shader("TEX2DA"), "2DA-1",
                    GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                                    GLShapeObjectFactory.CreateQuad(2.0f), GLShapeObjectFactory.TexQuadCW,
                                new GLRenderDataTranslationRotationTexture(items.Tex("2DArray2-1"), new Vector3(-8, 0, -2))
                        ));
            }

            #endregion

            #region Instancing
            if( (ctrl & (1<<8)) != 0)
            {
                items.Add(new GLShaderPipeline(new GLPLVertexShaderModelMatrixColor(), new GLPLFragmentShaderVSColor()),"IC-1");

                Matrix4[] pos1 = new Matrix4[3];
                pos1[0] = Matrix4.CreateTranslation(new Vector3(10, 0, 10));
                pos1[1] = Matrix4.CreateTranslation(new Vector3(10, 5, 10));
                pos1[2] = Matrix4.CreateRotationX(45f.Radians());
                pos1[2] *= Matrix4.CreateTranslation(new Vector3(10, 10, 10));

                GLRenderState rp = GLRenderState.Points(10);

                rObjects.Add(items.Shader("IC-1"), "1-a",
                                        GLRenderableItem.CreateVector4Matrix4(items, PrimitiveType.Points, rp,
                                                GLShapeObjectFactory.CreateQuad(2.0f), pos1,
                                                null, pos1.Length));


                Matrix4[] pos2 = new Matrix4[3];
                pos2[0] = Matrix4.CreateRotationX(-80f.Radians());
                pos2[0] *= Matrix4.CreateTranslation(new Vector3(20, 0, 10));
                pos2[1] = Matrix4.CreateRotationX(-70f.Radians());
                pos2[1] *= Matrix4.CreateTranslation(new Vector3(20, 5, 10));
                pos2[2] = Matrix4.CreateRotationZ(-60f.Radians());
                pos2[2] *= Matrix4.CreateTranslation(new Vector3(20, 10, 10));

                items.Add( new GLShaderPipeline(new GLPLVertexShaderModelMatrixTexture(), new GLPLFragmentShaderTexture()),"IC-2");

                GLRenderState rq = GLRenderState.Quads();
                rq.CullFace = false;

                GLRenderDataTexture rdt = new GLRenderDataTexture(items.Tex("wooden"));

                rObjects.Add(items.Shader("IC-2"), "1-b",
                                        GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.Quads, rq,
                                                GLShapeObjectFactory.CreateQuad(2.0f), GLShapeObjectFactory.TexQuadCW, pos2, 
                                                rdt, pos2.Length));
            }
            #endregion


            #region Tesselation
            if ((ctrl & (1 << 9)) != 0)
            {
                var shdrtesssine = new GLTesselationShaderSinewave(20, 0.5f, 2f);
                items.Add(shdrtesssine, "TESx1");

                GLRenderState rp = GLRenderState.Patches(4);

                rObjects.Add(items.Shader("TESx1"), "O-TES1",
                    GLRenderableItem.CreateVector4(items, PrimitiveType.Patches, rp,
                                        GLShapeObjectFactory.CreateQuadTriStrip(6.0f, 6.0f),
                                        new GLRenderDataTranslationRotationTexture(items.Tex("logo8bpp"), new Vector3(12, 0, 0), new Vector3( -90f.Radians(), 0, 0))
                                        ));
            }

            #endregion


            #region MipMaps
            if( (ctrl & (1<<10)) != 0)
            {
                items.Add( new GLTexture2D(Properties.Resources.mipmap2, SizedInternalFormat.Rgba8, 9), "mipmap1");

                rObjects.Add(items.Shader("TEXOT"), "mipmap1",
                    GLRenderableItem.CreateVector4Vector2(items,PrimitiveType.Triangles, GLRenderState.Tri(),
                                    GLCubeObjectFactory.CreateSolidCubeFromTriangles(1f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                                    new GLRenderDataTranslationRotationTexture(items.Tex("mipmap1"), new Vector3(-10, 0, 0))
                            ));
            }

            #endregion


            #region Screen coords
            // fixed point on screen
            if( (ctrl & (1<<14)) != 0 )
            {
                Vector4[] p = new Vector4[4];

                p[0] = new Vector4(10, 10, 0, 1);       // topleft - correct winding for our system. For dotted, red/blue at top as dots
                p[1] = new Vector4(10, 100, 0, 1);      // bottomleft
                p[2] = new Vector4(50, 10, 0, 1);       // topright
                p[3] = new Vector4(50, 100, 0, 1);      // botright

                items.Add( new GLDirect(), "ds1");

                GLRenderState rts = GLRenderState.Tri();
                GLRenderDataTexture rdt = new GLRenderDataTexture(items.Tex("dotted2"));

                rObjects.Add(items.Shader("ds1"), "ds1", GLRenderableItem.CreateVector4(items, PrimitiveType.TriangleStrip, rts, p , rdt));
            }

            #endregion

            #region Index/instance draw

            // multi element index draw
            if( (ctrl & (1<<15)) != 0)
            {
                float CS = 2, O = -20, OY = 0;
                float[] v = new float[]
                {
                    0,0,0,      // basevertex=1, pad with empties at the start to demo
                    -CS+O, -CS+OY, -CS,
                    -CS+O,  CS+OY, -CS,
                     CS+O, -CS+OY, -CS,
                     CS+O,  CS+OY, -CS,
                     CS+O, -CS+OY,  CS,
                     CS+O,  CS+OY,  CS,
                    -CS+O, -CS+OY,  CS,
                    -CS+O,  CS+OY,  CS,
                };

                byte[] vertex_indices = new byte[]
                {
                    2, 1, 0,
                    3, 1, 2,
                    4, 3, 2,
                    5, 3, 4,
                    6, 5, 4,
                    7, 5, 6,
                    0, 7, 6,
                    1, 7, 0,
                    2, 0, 6,
                    6, 4, 2,
                    3, 5, 7,
                    1, 3, 7
                };

                GLRenderState rt = GLRenderState.Tri();
                GLRenderableItem ri = GLRenderableItem.CreateFloats(items, PrimitiveType.Triangles, rt, v, 3);
                ri.CreateElementIndexByte(items.NewBuffer(), vertex_indices);
                ri.BaseVertex = 1;      // first vertex not used

                items.Add(new GLColourShaderWithWorldCoordXX(), "es1");
                rObjects.Add(items.Shader("es1"), "es1", ri);
            }

            // multi element index draw with primitive restart, draw a triangle strip
            if( (ctrl & (1<<16)) != 0)
            {
                float X = -10, Z = -10;
                float X2 = -8, Z2 = -10;
                float[] v = new float[]
                {
                    1+X,0,1+Z,
                    1+X,0,0+Z,
                    0+X,0,1+Z,
                    0+X,0,0+Z,
                    1+X2,0,1+Z2,
                    1+X2,0,0+Z2,
                    0+X2,0,1+Z2,
                    0+X2,0,0+Z2,
                };

                GLRenderState rts = GLRenderState.Tri(0xff);
                rts.DepthTest = false;
                rts.CullFace = false;

                GLRenderableItem ri = GLRenderableItem.CreateFloats(items,PrimitiveType.TriangleStrip, rts, v, 3);
                ri.CreateRectangleElementIndexByte(items.NewBuffer(), 2,0xff);

                items.Add(new GLColourShaderWithWorldCoordXX(), "es2");

                rObjects.Add(items.Shader("es2"), "es2", ri);
            }

            // indirect multi draw with element index - two red squares in foreground.
            if( (ctrl & (1<<17)) != 0)
            {
                float X = -10, Z = -12;
                float X2 = -8, Z2 = -12;
                float[] v = new float[]
                {
                    1+X,0,1+Z,
                    1+X,0,0+Z,
                    0+X,0,1+Z,
                    0+X,0,0+Z,
                    1+X2,0,1+Z2,
                    1+X2,0,0+Z2,
                    0+X2,0,1+Z2,
                    0+X2,0,0+Z2,
                };

                GLRenderState rts = GLRenderState.Tri(0xff);
                rts.DepthTest = false;
                rts.CullFace = false;

                GLRenderableItem ri = GLRenderableItem.CreateFloats(items, PrimitiveType.TriangleStrip, rts, v, 3);
                ri.CreateRectangleElementIndexByte(items.NewBuffer(), 2);  // put the primitive restart markers in, but we won't use them

                ri.IndirectBuffer = new GLBuffer(std430:true);  // disable alignment to vec4 for arrays for this buffer.
                ri.DrawCount = 2;
                ri.IndirectBuffer.AllocateBytes(ri.MultiDrawCountStride * ri.DrawCount + 4);
                ri.IndirectBuffer.StartWrite(0, ri.IndirectBuffer.Length);
                ri.IndirectBuffer.Write(1.0f);        // dummy float to demo index offset
                ri.BaseIndexOffset = 4;       // and indicate that the base command index is 4
                ri.IndirectBuffer.WriteIndirectElements(4, 1, 0, 0, 0);       // draw indexes 0-3
                ri.IndirectBuffer.WriteIndirectElements(4, 1, 5, 0, 0);       // and 5-8
                ri.IndirectBuffer.StopReadWrite();
                var data = ri.IndirectBuffer.ReadInts(0,10);                            // notice both are red due to primitive ID=1

                items.Add(new GLColourShaderWithWorldCoordXX(), "es3");

                rObjects.Add(items.Shader("es3"), "es3", ri);
            }

            #endregion

            #region Bindless texture
            if ((ctrl & (1 << 18)) != 0)
            {
                IGLTexture[] btextures = new IGLTexture[3];
                btextures[0] = items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "bl1");
                btextures[1] = items.Add(new GLTexture2D(Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "bl2");
                btextures[2] = items.Add(new GLTexture2D(Properties.Resources.golden, SizedInternalFormat.Rgba8), "bl3");

                GLBindlessTextureHandleBlock bl = new GLBindlessTextureHandleBlock(11,btextures);

                GLStatics.Check();

                float X = -10, Z = -14;
                float X2 = -9, Z2 = -15;
                float X3 = -8, Z3 = -16;
                float[] v = new float[]
                {
                    0+X,0,1+Z,
                    0+X,0,0+Z,
                    1+X,0,1+Z,
                    1+X,0,0+Z,

                    0+X2,0,1+Z2,
                    0+X2,0,0+Z2,
                    1+X2,0,1+Z2,
                    1+X2,0,0+Z2,

                    0+X3,0,1+Z3,
                    0+X3,0,0+Z3,
                    1+X3,0,1+Z3,
                    1+X3,0,0+Z3,
                };

                GLRenderState rts = GLRenderState.Tri(0xff);

                GLRenderableItem ri = GLRenderableItem.CreateFloats(items, PrimitiveType.TriangleStrip, rts, v, 3);
                ri.CreateRectangleElementIndexByte(items.NewBuffer(), 3);

                items.Add(new GLBindlessTextureShaderWithWorldCoord(11), "bt1");

                rObjects.Add(items.Shader("bt1"), "bt1-1", ri);
            }


            #endregion

            #region Objects

            if ((ctrl & (1 << 19)) != 0 && false)   // tbd
            {
                GLWaveformObjReader read = new GLWaveformObjReader();
                string s = System.Text.Encoding.UTF8.GetString(Properties.Resources.cubeobj);

                var objlist = read.ReadOBJData(s);

                if (objlist.Count > 0)
                {
                    GLBuffer vert = new GLBuffer();
                    vert.AllocateFill(objlist[0].Vertices.Vertices.ToArray());

                    var shader = new GLUniformColorShaderObjectTranslation();

                    GLRenderState rts = GLRenderState.Tri();

                    foreach (var obj in objlist)
                    {
                        if (obj.Indices.VertexIndices.Count > 0)
                        {
                            obj.Indices.RefactorVertexIndicesIntoTriangles();

                            var ri = GLRenderableItem.CreateVector4(items, PrimitiveType.Triangles, rts, vert, 0, 0, new GLRenderDataTranslationRotationColor(Color.FromName(obj.Material), new Vector3(20, 0, -20), scale: 2f));           // renderable item pointing to vert for vertexes
                            ri.CreateElementIndex(items.NewBuffer(), obj.Indices.VertexIndices.ToArray(), 0);       // using the refactored indexes, create an index table and use

                            rObjects.Add(shader, ri);
                        }
                    }
                }
            }

            if( (ctrl & (1<<20)) != 0)       // waveform object, wood panel, at y=-20,x=15
            {
                GLWaveformObjReader read = new GLWaveformObjReader();
                string s = System.Text.Encoding.UTF8.GetString(Properties.Resources.textobj1);

                var objlist = read.ReadOBJData(s);

                if (objlist.Count > 0)
                {
                    GLBuffer vert = new GLBuffer();
                    vert.AllocateFill(objlist[0].Vertices.Vertices.ToArray(), objlist[0].Vertices.TextureVertices2.ToArray());

                    var shader = new GLTexturedShaderObjectTranslation();

                    GLRenderState rts = GLRenderState.Tri();
                    //rts.CullFace = false;

                    foreach (var obj in objlist)
                    {
                        obj.Indices.RefactorVertexIndicesIntoTriangles();

                        IGLTexture tex = items.Tex(obj.Material);

                        var ri = GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rts, vert, vert.Positions[0], vert.Positions[1], 0, 
                                new GLRenderDataTranslationRotationTexture(tex, new Vector3(15, 0, -20), scale: 2f));           // renderable item pointing to vert for vertexes
                        ri.CreateElementIndex(items.NewBuffer(), obj.Indices.VertexIndices.ToArray(), 0);       // using the refactored indexes, create an index table and use

                        rObjects.Add(shader, ri);
                    }
                }
            }

            if( (ctrl & (1<<21)) != 0)     // wood face at y=-20, coloured cube at y=-20
            {
                GLWaveformObjReader read = new GLWaveformObjReader();
                var objlist = read.ReadOBJData(System.Text.Encoding.UTF8.GetString(Properties.Resources.textobj1));

                GLWavefrontObjCreator oc = new GLWavefrontObjCreator(items, rObjects);

                bool v = oc.Create(objlist, new Vector3(0, 0, -20), new Vector3(0, 0, 0), 2.0f);
                System.Diagnostics.Debug.Assert(v == true);

                objlist = read.ReadOBJData(System.Text.Encoding.UTF8.GetString(Properties.Resources.cubeobj));

                v = oc.Create(objlist, new Vector3(5, 0, -20), new Vector3(0, 0, 0), 2.0f);
                System.Diagnostics.Debug.Assert(v == true);
            }

            if( (ctrl & (1<<22)) != 0) // large sofa
            {
                GLWaveformObjReader read = new GLWaveformObjReader();
                var objlist = read.ReadOBJData(System.Text.Encoding.UTF8.GetString(Properties.Resources.Koltuk));

                GLWavefrontObjCreator oc = new GLWavefrontObjCreator(items, rObjects);
                oc.DefaultColor = Color.Red;

                bool v = oc.Create(objlist, new Vector3(-20, 0, -20), new Vector3(0, 0, 0), 8.0f);
                System.Diagnostics.Debug.Assert(v == true);
            }

              #endregion


            #region Instancing with matrix and lookat
            if( (ctrl & (1<<23)) != 0)
            {
                var texarray = new GLTexture2DArray(new Bitmap[] { Properties.Resources.dotted2, Properties.Resources.planetaryNebula, Properties.Resources.wooden }, SizedInternalFormat.Rgba8);
                items.Add(texarray);

                var shader = new GLShaderPipeline(new GLPLVertexShaderMatrixQuadTexture(), new GLPLFragmentShaderTexture2DIndexed(0));
                items.Add(shader);

                shader.StartAction += (s,m) => { texarray.Bind(1); };

                Matrix4[] pos = new Matrix4[3];
                pos[0] = Matrix4.CreateTranslation(new Vector3(-20, 0, -10));
                pos[0] = Matrix4.CreateScale(5) * pos[0];

                pos[1] = Matrix4.CreateTranslation(new Vector3(-20, 5, -10));
                pos[1] = Matrix4.CreateScale(4) * pos[1];
                pos[1][0, 3] = 1;   // image no
                pos[1][1, 3] = 1;   // lookat control

                pos[2] = Matrix4.CreateRotationX(-45f.Radians());
                pos[2] = Matrix4.CreateScale(4) * pos[2];
                pos[2] *= Matrix4.CreateTranslation(new Vector3(-20, 10, -10));
                pos[2][0, 3] = 2;
                pos[2][1, 3] = 2;   // lookat control
                GLRenderState rp = GLRenderState.Quads();

                rObjects.Add(shader, "1-atex2",
                                        GLRenderableItem.CreateMatrix4(items, PrimitiveType.Quads, rp, pos, 4, ic: 3, matrixdivisor: 1));

            }

            #endregion

            #region Sineway look at

            if( (ctrl & (1<<24)) != 0)   // instanced sinewive with rotate
            {
                var texarray = new GLTexture2DArray(new Bitmap[] { Properties.Resources.Logo8bpp, Properties.Resources.Logo8bpp }, SizedInternalFormat.Rgba8) ;
                items.Add(texarray, "Sinewavetex");
                GLRenderState rp = GLRenderState.Patches(4);

                var shdrtesssine = new GLTesselationShaderSinewaveAutoscale(20, 0.2f, 1f, rotate: true, rotateelevation: false);
                items.Add(shdrtesssine, "TESIx1");

                Vector4[] pos = new Vector4[]       //w = image index
                {
                    new Vector4(40,0,-10,1),
                    new Vector4(40,0,-30,0),            // flat on the xz plane
                };


                var dt = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.Patches, rp,
                                        GLShapeObjectFactory.CreateQuadTriStrip(10.0f, 10.0f, new Vector3(-0f.Radians(), 0, 0)), pos,
                                        new GLRenderDataTexture(texarray),
                                        ic: 2, seconddivisor: 1);

                rObjects.Add(shdrtesssine, "O-TESA1", dt);

                var shdrtesssine2 = new GLTesselationShaderSinewaveAutoscale(20, 0.2f, 1f, rotate: true, rotateelevation: true);
                items.Add(shdrtesssine2, "TESIx2");

                Vector4[] pos2 = new Vector4[]       //w = image index
                {
                    new Vector4(60,0,-10,1),
                    new Vector4(60,0,-30,0),            // flat on the xz plane
                };

                var dt2 = GLRenderableItem.CreateVector4Vector4(items,PrimitiveType.Patches, rp,
                                        GLShapeObjectFactory.CreateQuadTriStrip(10.0f, 10.0f, new Vector3(-0f.Radians(), 0, 0)), pos2,
                                        new GLRenderDataTexture(texarray),
                                        ic: 2, seconddivisor: 1);

                rObjects.Add(shdrtesssine2, "O-TESA2", dt2);
            }

            #endregion

            #region 4.6

            // indirect multi draw with count from a parameter buffer (4.6) static moon at front
            if ((ctrl & (1 << 25)) != 0)
            {
                GLRenderableItem rit;
                GLBuffer ritpara;
                GLRenderState rc1 = GLRenderState.Tri();
                rit = GLRenderableItem.CreateVector4Vector2(items,PrimitiveType.Triangles, rc1,
                            GLSphereObjectFactory.CreateTexturedSphereFromTriangles(2, 5.0f),
                            new GLRenderDataTranslationRotationTexture(items.Tex("moon"), new Vector3(0, 0, -30)));

                rit.IndirectBuffer = new GLBuffer(128,true);    
                rit.IndirectBuffer.StartWrite(0);
                rit.IndirectBuffer.WriteIndirectArray(rit.DrawCount, 1, 0, 0);      // set up an indirect buffer and store the command
                rit.IndirectBuffer.StopReadWrite();
                rit.DrawCount = 1;  // one draw
                var data = rit.IndirectBuffer.ReadInts(0, 4);                            // notice both are red due to primitive ID=1

                ritpara = new GLBuffer(128, true);
                ritpara.StartWrite(0);
                ritpara.Write((int)0);       // dummy to make an offset be needed
                ritpara.Write((int)0);       // dummy to make an offset be needed
                ritpara.Write((int)0);       // dummy to make an offset be needed
                ritpara.Write((int)0);       // dummy to make an offset be needed
                ritpara.Write((int)0);       // dummy to make an offset be needed
                ritpara.Write((int)1);       // count 1
                ritpara.Write((int)0);       // count 
                ritpara.StopReadWrite();
                var data2 = ritpara.ReadInts(0, 1);                            // notice both are red due to primitive ID=1
                rit.ParameterBuffer = ritpara;
                rit.ParameterBufferOffset = 20;  // pick up 
                rit.DrawCount = 8;      // maximum due to 128 buffer size in indirect buffer
                rit.MultiDrawCountStride = 16;

                rObjects.Add(items.Shader("TEXOT"), "ICA", rit);
            }


            #endregion


            #region Matrix Calc Uniform

            var mcb = new GLMatrixCalcUniformBlock();

            items.Add(mcb,"MCUB");     // def binding of 0

            #endregion

            dataoutbuffer = items.NewStorageBlock(5);
            dataoutbuffer.AllocateBytes(sizeof(float) * 4 * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicRead);    // 32 vec4 back

        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong time)
        {
            //System.Diagnostics.Debug.WriteLine("Draw");

            float degrees = ((float)time / 5000.0f * 360.0f) % 360f;
            float degreesd2 = ((float)time / 10000.0f * 360.0f) % 360f;
            float degreesd4 = ((float)time / 20000.0f * 360.0f) % 360f;
            float zeroone = (degrees >= 180) ? (1.0f - (degrees - 180.0f) / 180.0f) : (degrees / 180f);

            if (rObjects.Contains("woodbox"))
            {
                (rObjects["woodbox"].RenderData as GLRenderDataTranslationRotation).XRotDegrees = degrees;
                (rObjects["woodbox"].RenderData as GLRenderDataTranslationRotation).ZRotDegrees = degrees;
            }

            if (rObjects.Contains("EDDCube"))
            {
                (rObjects["EDDCube"].RenderData as GLRenderDataTranslationRotation).YRotDegrees = degrees;
                (rObjects["EDDCube"].RenderData as GLRenderDataTranslationRotation).ZRotDegrees = degreesd2;
            }

            if (rObjects.Contains("sphere3"))
            {
                (rObjects["sphere3"].RenderData as GLRenderDataTranslationRotation).XRotDegrees = -degrees;
                (rObjects["sphere3"].RenderData as GLRenderDataTranslationRotation).YRotDegrees = degrees;
            }
            if (rObjects.Contains("sphere3"))
            {
                (rObjects["sphere4"].RenderData as GLRenderDataTranslationRotation).YRotDegrees = degrees;
                (rObjects["sphere4"].RenderData as GLRenderDataTranslationRotation).ZRotDegrees = -degreesd2;
            }

            if (rObjects.Contains("sphere7"))
                (rObjects["sphere7"].RenderData as GLRenderDataTranslationRotation).YRotDegrees = degreesd4;

            if (items.Contains("TEXOCT"))
                ((GLPLVertexShaderModelTranslationTexture)items.Shader("TEXOCT").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader)).Transform.YRotDegrees = degrees;

            if (items.Contains("TEX2DA"))
                ((GLPLFragmentShaderTexture2DBlend)items.Shader("TEX2DA").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader)).Blend = zeroone;

            if (items.Contains("tapeshader"))
            {
                var s = items.Shader("tapeshader").GetShader<GLPLFragmentShaderTextureTriStripColorReplace>();
                s.TexOffset = new Vector2(-degrees / 360f, 0.0f);
            }

            if (items.Contains("TESx1"))
                ((GLTesselationShaderSinewave)items.Shader("TESx1")).Phase = degrees / 360.0f;
            if (items.Contains("TESIx1"))
                ((GLTesselationShaderSinewaveAutoscale)items.Shader("TESIx1")).Phase = degrees / 360.0f;
            if (items.Contains("TESIx2"))
                ((GLTesselationShaderSinewaveAutoscale)items.Shader("TESIx2")).Phase = degrees / 360.0f;

            GLStatics.Check();
            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            GL.FrontFace(FrontFaceDirection.Cw);
            rObjectscw.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
            GL.FrontFace(FrontFaceDirection.Ccw);

            var azel = gl3dcontroller.PosCamera.LookAt.AzEl(gl3dcontroller.PosCamera.EyePosition, true);


            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + " cdir " + gl3dcontroller.PosCamera.CameraDirection + " azel " + azel + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

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


        }

        static float Fract(float x)
        {
            return x - (float)Math.Floor(x);
        }
    }

    public class GLDirect : GLShaderPipeline
    {
        public GLDirect(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderScreenTexture(), new GLPLFragmentShaderTextureOffset());
        }
    }

    public class GLColourShaderWithWorldCoordXX : GLShaderPipeline
    {
        public GLColourShaderWithWorldCoordXX(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderWorldCoord(), new GLPLFragmentIDShaderColor(2));
        }
    }

    public class GLBindlessTextureShaderWithWorldCoord : GLShaderPipeline
    {
        public GLBindlessTextureShaderWithWorldCoord(int arbbindingpoint, Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderWorldTextureTriStrip(), new GLPLFragmentShaderBindlessTexture(arbbindingpoint));
        }
    }



}


