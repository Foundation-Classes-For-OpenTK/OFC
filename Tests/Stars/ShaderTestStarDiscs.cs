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

using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.Shaders.Stars;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.Utils;

namespace TestOpenTk
{
    public partial class ShaderTestStarDiscs : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestStarDiscs()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        // Demonstrate buffer feedback AND geo shader add vertex/dump vertex




        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.ZoomDistance = 20F;
            gl3dcontroller.PosCamera.ZoomMin = 0.1f;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            glwfc.BackColor = Color.FromArgb(0, 0, 60);
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, 0), new Vector3(90f, 0, 0f), 3F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 20.0f;
            };

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0
            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");

            GLTexture2DArray starimagearray = new GLTexture2DArray();
            Bitmap[] starbmps = new Bitmap[] { Properties.Resources.O, Properties.Resources.A, Properties.Resources.F, Properties.Resources.G, Properties.Resources.N };
            Bitmap[] starbmpsreduced = starbmps.CropImages(new RectangleF(16, 16, 68, 68));
            //for (int b = 0; b < starbmpsreduced.Length; b++)  starbmpsreduced[b].Save(@"c:\code\" + $"star{b}.bmp", System.Drawing.Imaging.ImageFormat.Png);
            starimagearray.CreateLoadBitmaps(starbmpsreduced, SizedInternalFormat.Rgba8,ownbmp:true);
            items.Add(starimagearray);
            
            {
                items.Add(new GLColorShaderWorld(), "COS");
                GLRenderState rl = GLRenderState.Lines();

                rObjects.Add(items.Shader("COS"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(-40, 0, 40), new Vector3(10, 0, 0), 9),
                                                        new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );


                rObjects.Add(items.Shader("COS"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, null,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(40, 0, -40), new Vector3(0, 0, 10), 9),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );
            }


            // moon 
            if (false)
            {
                items.Add(new GLTexturedShaderObjectTranslation(), "TEX");

                GLRenderState rt = GLRenderState.Tri();

                var renderableitem = GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                            GLSphereObjectFactory.CreateTexturedSphereFromTriangles(4, 10.0f),
                            new GLRenderDataTranslationRotationTexture(items.Tex("moon"), new Vector3(-30, 0, 0)));

                rObjects.Add(items.Shader("TEX"), "sphere7", renderableitem);
            }

            // moon with autoscaling
            if (true)
            {
                var vertex = new GLPLVertexShaderModelWorldTextureAutoScale(20.0f, 1F, 2F, true);
                var fragment = new GLPLFragmentShaderTexture();
                var shader = new GLShaderPipeline(vertex, fragment);
                items.Add(shader,"MoonTextureShader");

                GLRenderState rt = GLRenderState.Tri();

                var worldpos = new Vector4[] { new Vector4(-10, 0, 0, 2220), new Vector4(-10, 0, 10, 2220), new Vector4(-10, 0, 20, 2220) };

                var renderableitem = GLRenderableItem.CreateVector4Vector2Vector4(items, PrimitiveType.Triangles, rt,
                            GLSphereObjectFactory.CreateTexturedSphereFromTriangles(4, 5.0f),
                            worldpos,
                            new GLRenderDataTexture(items.Tex("moon")), worldpos.Length);

                rObjects.Add(shader, "sphere8", renderableitem);
            }

            // 2d star textures with autoscaling
            if (true)
            {
                var vertex = new GLPLVertexShaderModelWorldTextureAutoScale(20.0f, 1F, 2F, true);
                var fragment = new GLPLFragmentShaderTexture2DWSelectorSunspot();
                var shader = new GLShaderPipeline(vertex, fragment);
                items.Add(shader,"StarTextureShader");

                GLRenderState rt = GLRenderState.Tri();

                var worldpos = new Vector4[] { new Vector4(0, 0, 0, 0 ), new Vector4(1, 0, 10, 1 + 0x10000), new Vector4(2, 0, 20, 2), new Vector4(3, 0, 30, 3) , new Vector4(4, 0, 40, 4 + 0x10000) };

                var renderableitem = GLRenderableItem.CreateVector4Vector2Vector4(items, PrimitiveType.Triangles, rt,
                            GLSphereObjectFactory.CreateTexturedSphereFromTriangles(4, 5.0f),
                            worldpos,
                            new GLRenderDataTexture(starimagearray), worldpos.Length);

                rObjects.Add(shader, "sphere9", renderableitem);
            }

            if ( true )
            {
                var sunvertex = new GLPLVertexShaderModelCoordWorldAutoscale(new Color[] { Color.Green, Color.Red, Color.Blue });
                var sunfragment = new GLPLStarSurfaceFragmentShader();
                var sunshader = new GLShaderPipeline(sunvertex, sunfragment);
                items.Add(sunshader, "StarColorShader");

                var model = GLSphereObjectFactory.CreateSphereFromTriangles(3, 10.0f);
                var positions = new Vector4[] { new Vector4(20, 0, 20, 0), new Vector4(20, 0,0,1) };

                GLRenderState renderstate = GLRenderState.Tri();
                var renderableitem = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.Triangles,
                               renderstate,
                               model,
                               positions, null, positions.Length, 1);

                rObjects.Add(sunshader, "StarColorRender", renderableitem);
            }

            if ( false )
            { 
                items.Add( new GLShaderStarCorona(), "CORONA");

                GLRenderState rq = GLRenderState.Tri();

                rObjects.Add(items.Shader("CORONA"), GLRenderableItem.CreateVector4(items, PrimitiveType.TriangleStrip,
                                        rq,
                                        GLShapeObjectFactory.CreateQuadTriStrip(1f,1f),
                                        new GLRenderDataTranslationRotation(new Vector3(20, 0, 0), new Vector3(0, 0, 0), 20f, calclookat:true)));
            }

            if (false)
            {
                Vector4[] pos = new Vector4[3];
                pos[0] = new Vector4(-20, 0, 10, 0);
                pos[1] = new Vector4(0, 0, 10, 1);
                pos[2] = new Vector4(20, 0, 10, 2);

                var shape = GLSphereObjectFactory.CreateSphereFromTriangles(3, 10.0f);

                GLRenderState rt = GLRenderState.Tri();
                GLRenderableItem ri = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.Triangles, rt, shape, pos, null, pos.Length, 1);

                var vertshader = new GLPLVertexShaderModelCoordWorldAutoscale(new Color[] { Color.Red, Color.Green, Color.Blue });
                var shader = new GLShaderPipeline(vertshader, new GLPLStarSurfaceFragmentShader());
                items.Add(shader, "STAR-M2");
                rObjects.Add(shader, ri);
            }

             System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);

            GL.Enable(EnableCap.DepthClamp);
        }


        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong time)
        {
            // System.Diagnostics.Debug.WriteLine("Draw eye " + gl3dcontroller.MatrixCalc.EyePosition + " to " + gl3dcontroller.Pos.Current);

            float zeroone10000s = ((float)(time % 10000000)) / 10000000.0f;
            float zeroone5000s = ((float)(time % 5000000)) / 5000000.0f;
            float zeroone1000s = ((float)(time % 1000000)) / 1000000.0f;
            float zeroone500s = ((float)(time % 500000)) / 500000.0f;
            float zeroone100s = ((float)(time % 100000)) / 100000.0f;
            float zeroone20s = ((float)(time % 20000)) / 20000.0f;
            float zeroone10s = ((float)(time % 10000)) / 10000.0f;
            float zeroone5s = ((float)(time % 5000)) / 5000.0f;
            float zerotwo5s = ((float)(time % 5000)) / 2500.0f;
            float timediv10s = (float)time / 10000.0f;
            float timediv100s = (float)time / 100000.0f;


            if (items.Contains("StarColorShader"))
            {
                var vertshader = items.Shader("StarColorShader").GetShader<GLPLVertexShaderModelCoordWorldAutoscale>(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                var rot = (float)(-zeroone20s * Math.PI * 2);
                vertshader.ModelTranslation = Matrix4.CreateRotationY(rot);

                var fragshader = items.Shader("StarColorShader").GetShader<GLPLStarSurfaceFragmentShader>(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                fragshader.TimeDeltaSpots = zeroone500s;
                fragshader.TimeDeltaSurface = timediv100s;
            }

            if (items.Contains("StarTextureShader"))
            {
                var vertshader = items.Shader("StarTextureShader").GetShader<GLPLVertexShaderModelWorldTextureAutoScale>(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                var rot = (float)(-zeroone100s * Math.PI * 2);
                vertshader.ModelTranslation = Matrix4.CreateRotationY(rot);

                var fragshader = items.Shader("StarTextureShader").GetShader<GLPLFragmentShaderTexture2DWSelectorSunspot>(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                fragshader.TimeDeltaSpots = zeroone500s;
                fragshader.TimeDeltaSurface = -((float)time / 100000.0f);
            }

            if (items.Contains("MoonTextureShader"))
            {
                var vertshader = items.Shader("MoonTextureShader").GetShader<GLPLVertexShaderModelWorldTextureAutoScale>(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                var rot = (float)(-zeroone10s * Math.PI * 2);
                vertshader.ModelTranslation = Matrix4.CreateRotationY(rot);

            }

            if (items.Contains("CORONA"))
            {
                ((GLShaderStarCorona)items.Shader("CORONA")).TimeDelta = (float)time / 100000f;
            }

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.Set(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState,gl3dcontroller.MatrixCalc);
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);


            this.Text = //"Freq " + frequency.ToString("#.#########") + " unRadius " + unRadius + " scutoff" + scutoff + " BD " + blackdeepness + " CE " + concentrationequator
            "    Looking at " + gl3dcontroller.MatrixCalc.LookAt + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            gl3dcontroller.Redraw();
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
        }
    }
}


