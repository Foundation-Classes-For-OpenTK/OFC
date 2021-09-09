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

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

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
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            glwfc.BackColor = Color.FromArgb(0, 0, 60);
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, 0), new Vector3(120f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 20.0f;
            };

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0

            {
                items.Add(new GLColorShaderWithWorldCoord(), "COS");
                GLRenderState rl = GLRenderState.Lines(1);

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

            {
                items.Add(new GLTexturedShaderWithObjectTranslation(), "TEX");
                items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");

                GLRenderState rt = GLRenderState.Tri();

                rObjects.Add(items.Shader("TEX"), "sphere7",
                        GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                            GLSphereObjectFactory.CreateTexturedSphereFromTriangles(4, 10.0f),
                            new GLRenderDataTranslationRotationTexture(items.Tex("moon"), new Vector3(-30, 0, 0))
                        ));
            }

            {
                var sunvertex = new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(new Color[] { Color.Yellow, Color.FromArgb(255, 230, 230, 1) });

                items.Add(new GLShaderPipeline(sunvertex, new GLPLStarSurfaceFragmentShader()), "STAR");

                GLRenderState rt = GLRenderState.Tri();

                rObjects.Add(items.Shader("STAR"), "sun",
                       GLRenderableItem.CreateVector4(items, PrimitiveType.Triangles,
                               rt,
                               GLSphereObjectFactory.CreateSphereFromTriangles(3, 10.0f),
                               new GLRenderDataTranslationRotation(new Vector3(20, 0, 0)),
                               ic: 1));

                items.Add( new GLShaderStarCorona(), "CORONA");

                GLRenderState rq = GLRenderState.Quads();

                rObjects.Add(items.Shader("CORONA"), GLRenderableItem.CreateVector4(items, PrimitiveType.Quads,
                                        rq,
                                        GLShapeObjectFactory.CreateQuad(1f),
                                        new GLRenderDataTranslationRotation(new Vector3(20, 0, 0), new Vector3(0, 0, 0), 20f, calclookat:true)));
            }

            if (true)
            {
                Vector4[] pos = new Vector4[3];
                pos[0] = new Vector4(-20, 0, 10, 0);
                pos[1] = new Vector4(0, 0, 10, 0);
                pos[2] = new Vector4(20, 0, 10, 0);

                var shape = GLSphereObjectFactory.CreateSphereFromTriangles(3, 10.0f);

                GLRenderState rt = GLRenderState.Tri();
                GLRenderableItem ri = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.Triangles, rt, shape, pos, null, pos.Length, 1);

                var shader = new GLShaderPipeline(new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(), new GLPLStarSurfaceFragmentShader());
                items.Add(shader, "STAR-M2");
                rObjects.Add(shader, ri);
            }

             GLOFC.GLStatics.Check();

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
            float zeroone10s = ((float)(time % 10000)) / 10000.0f;
            float zeroone5s = ((float)(time % 5000)) / 5000.0f;
            float zerotwo5s = ((float)(time % 5000)) / 2500.0f;
            float timediv10s = (float)time / 10000.0f;
            float timediv100s = (float)time / 100000.0f;


            if (items.Contains("STAR"))
            {
                int vid = items.Shader("STAR").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader).Id;
                ((GLRenderDataTranslationRotation)(rObjects["sun"].RenderData)).RotationDegrees = new Vector3(0, -zeroone100s * 360, 0);
                var stellarsurfaceshader = (GLPLStarSurfaceFragmentShader)items.Shader("STAR").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                stellarsurfaceshader.TimeDeltaSpots = zeroone500s;
                stellarsurfaceshader.TimeDeltaSurface = timediv100s;
            }

            if (items.Contains("STAR-M2"))
            {
                var vid = items.Shader("STAR-M2").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                ((GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation)vid).ModelTranslation = Matrix4.CreateRotationY((float)(-zeroone10s * Math.PI * 2));
                var stellarsurfaceshader = (GLPLStarSurfaceFragmentShader)items.Shader("STAR-M2").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                stellarsurfaceshader.TimeDeltaSpots = zeroone500s;
                stellarsurfaceshader.TimeDeltaSurface = timediv100s;
            }

            if (items.Contains("STAR-M3"))
            {
                var vid = items.Shader("STAR-M3").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                ((GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation)vid).ModelTranslation = Matrix4.CreateRotationY((float)(-zeroone10s * Math.PI * 2));
                var stellarsurfaceshader = (GLPLStarSurfaceFragmentShader)items.Shader("STAR-M3").GetShader(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                stellarsurfaceshader.TimeDeltaSpots = zeroone500s;
                stellarsurfaceshader.TimeDeltaSurface = timediv100s;
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
            "    Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;

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


