﻿/*
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
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestOpenTk
{
    public partial class ShaderTestStarDiscs : Form
    {
        private OFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestStarDiscs()
        {
            InitializeComponent();

            glwfc = new OFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        // Demonstrate buffer feedback AND geo shader add vertex/dump vertex




        public class GLShaderStarCorona : GLShaderStandard
        {
            const int BindingPoint = 1;

            public string Vertex()
            {
                return
    @"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;

layout (location = 21) uniform  mat4 rotate;
layout (location = 22) uniform  mat4 transform;

layout (location =0) out vec3 fposition;

void main(void)
{
    fposition =vec3(position.xz,0);
    vec4 p1 = rotate * position;
	gl_Position = mc.ProjectionModelMatrix * transform * p1;        // order important
}
";
            }

            public string Fragment()
            {
                return
    @"
#version 450 core

#include Shaders.Functions.snoise4.glsl

layout (location =0 ) in vec3 fposition;
out vec4 color;

layout (location = 15) uniform float unDT;

void main(void)
{
	const float brightnessMultiplier = 0.9;   // The higher the number, the brighter the corona will be.
	const float smootheningMultiplier = 0.15; // How smooth the irregular effect is, the higher the smoother.
	const float ringIntesityMultiplier = 2.8; // The higher the number, the smaller the solid ring inside
	const float coronaSizeMultiplier = 2.0;  // The higher the number, the smaller the corona. 2.0
	const float frequency = 1.5;              // The frequency of the irregularities.
	const float fDetail = 0.7;                // The higher the number, the more detail the corona will have. (Might be more GPU intensive when higher, 0.7 seems fine for the normal PC)
	const int iDetail = 10;                   // The higher the number, the more detail the corona will have.
	const float irregularityMultiplier = 4;   // The higher the number, the more irregularities and bigger ones. (Might be more GPU intensive when higher, 4 seems fine for the normal PC)

	/* Don't edit these */

    float t = unDT - length(fposition);

    // Offset normal with noise
    float ox = simplexnoise(vec4(fposition, t) * frequency);
    float oy = simplexnoise(vec4((fposition + (1000.0 * irregularityMultiplier)), t) * frequency);
    float oz = simplexnoise(vec4((fposition + (2000.0 * irregularityMultiplier)), t) * frequency);
	float om = simplexnoise(vec4((fposition + (4000.0 * irregularityMultiplier)), t) * frequency) * simplexnoise(vec4((fposition + (250.0 * irregularityMultiplier)), t) * frequency);
    vec3 offsetVec = vec3(ox * om, oy * om, oz * om) * smootheningMultiplier;

    // Get the distance vector from the center
    vec3 nDistVec = normalize(fposition + offsetVec);

    // Get noise with normalized position to offset the original position
    vec3 position = fposition + simplexnoise(vec4(nDistVec, t), iDetail, 1.5, fDetail) * smootheningMultiplier;

    // Calculate brightness based on distance
    float dist = length(position + offsetVec) * coronaSizeMultiplier;
    float brightness = (1.0 / (dist * dist) - 0.1) * (brightnessMultiplier - 0.4);
	float brightness2 = (1.0 / (dist * dist)) * brightnessMultiplier;

    // Calculate color
    vec3 unColor = vec3(0.9,0.9,0);

    float alpha = clamp(brightness, 0.0, 1.0) * (cos(clamp(brightness, 0.0, 0.5)) / (cos(clamp(brightness2 / ringIntesityMultiplier, 0.0, 1.5)) * 2));
    vec3 starcolor = unColor * brightness;

    alpha = pow(alpha,1.8);             // exp roll of of alpha so it does go to 0, and therefore it does not show box
    if ( alpha < 0.2 )
        discard;
    else
        color = vec4(starcolor, alpha );
}
";
            }

            public GLShaderStarCorona()
            {
                CompileLink(vertex: Vertex(), frag: Fragment());
            }

            public float TimeDelta{ get; set; } = 0.00001f*10;

            public override void Start(GLMatrixCalc c)
            {
                base.Start(c);

                GL.ProgramUniform1(Id, 15, TimeDelta);
                OFC.GLStatics.Check();
            }
        }

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
                GLRenderControl rl = GLRenderControl.Lines(1);

                rObjects.Add(items.Shader("COS"),
                             GLRenderableItem.CreateVector4Color4(items, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(-40, 0, 40), new Vector3(10, 0, 0), 9),
                                                        new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );


                rObjects.Add(items.Shader("COS"),
                             GLRenderableItem.CreateVector4Color4(items, rl,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(40, 0, -40), new Vector3(0, 0, 10), 9),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );
            }

            {
                items.Add(new GLTexturedShaderWithObjectTranslation(), "TEX");
                items.Add(new GLTexture2D(Properties.Resources.moonmap1k), "moon");

                GLRenderControl rt = GLRenderControl.Tri();

                rObjects.Add(items.Shader("TEX"), "sphere7",
                        GLRenderableItem.CreateVector4Vector2(items, rt,
                            GLSphereObjectFactory.CreateTexturedSphereFromTriangles(4, 10.0f),
                            new GLRenderDataTranslationRotationTexture(items.Tex("moon"), new Vector3(-30, 0, 0))
                        ));
            }

            {
                items.Add(new GLShaderPipeline(new GLPLVertexShaderModelCoordWithObjectTranslation(), new GLPLStarSurfaceFragmentShader()), "STAR");

                GLRenderControl rt = GLRenderControl.Tri();

                rObjects.Add(items.Shader("STAR"), "sun",
                       GLRenderableItem.CreateVector4(items,
                               rt,
                               GLSphereObjectFactory.CreateSphereFromTriangles(3, 10.0f),
                               new GLRenderDataTranslationRotation(new Vector3(20, 0, 0)),
                               ic: 1));

                items.Add( new GLShaderStarCorona(), "CORONA");

                GLRenderControl rq = GLRenderControl.Quads();

                rObjects.Add(items.Shader("CORONA"), GLRenderableItem.CreateVector4(items,
                                        rq,
                                        GLShapeObjectFactory.CreateQuad(1f),
                                        new GLRenderDataTranslationRotation(new Vector3(20, 0, 0), new Vector3(0, 0, 0), 20f, calclookat:true)));
            }

            {
                Vector4[] pos = new Vector4[3];
                pos[0] = new Vector4(-20, 0, 10, 0);
                pos[1] = new Vector4(0, 0, 10, 0);
                pos[2] = new Vector4(20, 0, 10, 0);

                var shape = GLSphereObjectFactory.CreateSphereFromTriangles(3, 10.0f);

                GLRenderControl rt = GLRenderControl.Tri();
                GLRenderableItem ri = GLRenderableItem.CreateVector4Vector4(items, rt, shape, pos, null, pos.Length, 1);

                var shader = new GLShaderPipeline(new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(), new GLPLStarSurfaceFragmentShader());
                items.Add(shader, "STAR-M2");
                rObjects.Add(shader, ri);
            }

            {
                Matrix4[] pos = new Matrix4[3];
                pos[0] = Matrix4.CreateTranslation(-30, 0, 30);
                pos[1] = Matrix4.CreateTranslation(0, 0, 30);
                pos[2] = Matrix4.CreateTranslation(20, 0, 30);

                pos[1][1,3] = -1;       // test clipping of vertex's from pos 1

                var shape = GLSphereObjectFactory.CreateSphereFromTriangles(3, 10.0f);

                GLRenderControl rt = GLRenderControl.Tri();
                rt.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted
                GLRenderableItem ri = GLRenderableItem.CreateVector4Matrix4(items, rt, shape, pos, null, pos.Length, 1);

                var fragshader = new GLPLStarSurfaceFragmentShader();
                fragshader.Scutoff = 0.3f;

                var vertshader = new GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation();
                vertshader.WorldPositionOffset = new Vector3(0, 10, 0);

                var shader = new GLShaderPipeline(vertshader, fragshader);
                items.Add(shader, "STAR-M3");
                rObjects.Add(shader, ri);
            }

            OFC.GLStatics.Check();

            GL.Enable(EnableCap.DepthClamp);
        }


        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(GLMatrixCalc mc, long time)
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
                int vid = items.Shader("STAR").Get(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader).Id;
                ((GLRenderDataTranslationRotation)(rObjects["sun"].RenderData)).RotationDegrees = new Vector3(0, -zeroone100s * 360, 0);
                var stellarsurfaceshader = (GLPLStarSurfaceFragmentShader)items.Shader("STAR").Get(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                stellarsurfaceshader.TimeDeltaSpots = zeroone500s;
                stellarsurfaceshader.TimeDeltaSurface = timediv100s;
            }

            if (items.Contains("STAR-M2"))
            {
                var vid = items.Shader("STAR-M2").Get(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                ((GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation)vid).ModelTranslation = Matrix4.CreateRotationY((float)(-zeroone10s * Math.PI * 2));
                var stellarsurfaceshader = (GLPLStarSurfaceFragmentShader)items.Shader("STAR-M2").Get(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                stellarsurfaceshader.TimeDeltaSpots = zeroone500s;
                stellarsurfaceshader.TimeDeltaSurface = timediv100s;
            }

            if (items.Contains("STAR-M3"))
            {
                var vid = items.Shader("STAR-M3").Get(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                ((GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation)vid).ModelTranslation = Matrix4.CreateRotationY((float)(-zeroone10s * Math.PI * 2));
                var stellarsurfaceshader = (GLPLStarSurfaceFragmentShader)items.Shader("STAR-M3").Get(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
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


            //this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " dir " + gl3dcontroller.Pos.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;
            this.Text = //"Freq " + frequency.ToString("#.#########") + " unRadius " + unRadius + " scutoff" + scutoff + " BD " + blackdeepness + " CE " + concentrationequator
            "    Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true, OtherKeys);
            gl3dcontroller.Redraw();
        }

        private void OtherKeys( OFC.Controller.KeyboardMonitor kb )
        {
            float fact = kb.Shift ? 10 : kb.Alt ? 100 : 1;
            //if (kb.IsCurrentlyPressed(Keys.F1) != null)
            //    frequency -= 0.000001f * fact;
            //if (kb.IsCurrentlyPressed(Keys.F2) != null)
            //    frequency += 0.000001f * fact;
            //if (kb.IsCurrentlyPressed(Keys.F5) != null)
            //    unRadius -= 10 * fact;
            //if (kb.IsCurrentlyPressed(Keys.F6) != null)
            //    unRadius += 10 * fact;
            //if (kb.IsCurrentlyPressed(Keys.F7) != null)
            //    scutoff -= 0.001f * fact;
            //if (kb.IsCurrentlyPressed(Keys.F8) != null)
            //    scutoff += 0.001f * fact;
            //if (kb.IsCurrentlyPressed(Keys.F9) != null)
            //    blackdeepness -= 0.1f * fact;
            //if (kb.IsCurrentlyPressed(Keys.F10) != null)
            //    blackdeepness += 0.1f * fact;
            //if (kb.IsCurrentlyPressed(Keys.F11) != null)
            //    concentrationequator -= 0.1f * fact;
            //if (kb.IsCurrentlyPressed(Keys.F12) != null)
            //    concentrationequator += 0.1f * fact;
        }
    }
}


