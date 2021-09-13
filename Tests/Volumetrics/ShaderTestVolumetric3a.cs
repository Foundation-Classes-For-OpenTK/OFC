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

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using System;
using System.Drawing;
using System.Windows.Forms;

// Demonstrate the volumetric calculations needed to compute a plane facing the user inside a bounding box done inside a geo shader
// this one add on tex coord calculation and using a single tight quad shows its working
// 3a shows the projection transform and the co-ords, demonstrating x = -1 left to +1 right, y = +1 top to -1 bottom, divided by W

namespace TestOpenTk
{
    public partial class ShaderTestVolumetric3a : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestVolumetric3a()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        /// ////////////////////////////////////////////////////////////////////////////////////////////////////

        public class ShaderV2 : GLShaderStandard
        {
            string vcode =
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;

out int instance;

void main(void)
{
	gl_Position = position;       
    instance = gl_InstanceID;
}
";

            string fcode = @"
#version 450 core
out vec4 color;
in vec4 vs_color;
in vec3 vs_texcoord;

void main(void)
{
    color = vs_color;
    //color = vec4(vs_texcoord,0.8);
}
";

            public ShaderV2()
            {
                CompileLink(vertex: vcode, frag: fcode, geo: "#include TestOpenTk.Volumetrics.volumetricgeo3a.glsl");
            }
        }

        public class GLFixedShader : GLShaderPipeline
        {
            public GLFixedShader(Color c, Action<IGLProgramShader, GLMatrixCalc> action = null) : base(action)
            {
                AddVertexFragment(new GLPLVertexShaderWorldCoord(), new GLPLFragmentShaderFixedColor(c));
            }
        }

        public class GLFixedProjectionShader : GLShaderPipeline
        {
            public GLFixedProjectionShader(Color c, Action<IGLProgramShader, GLMatrixCalc> action = null) : base(action)
            {
                AddVertexFragment(new GLPLVertexShaderModelViewCoord(), new GLPLFragmentShaderFixedColor(c));
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            gl3dcontroller.ZoomDistance = 40F;
            gl3dcontroller.MouseRotateAmountPerPixel = 0.05f;
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, 0), new Vector3( 135, 0, 0), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 100.0f;
            };

            items.Add(new GLColorShaderWithWorldCoord(), "COSW");
            GLRenderState rl1 = GLRenderState.Lines(1);

            {

                rObjects.Add(items.Shader("COSW"), "L1",   // horizontal
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl1,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(-100, 0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Gray })
                                   );


                rObjects.Add(items.Shader("COSW"),    // vertical
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl1,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(100, 0, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Gray })
                                   );

            }

            // Number markers using instancing and 2d arrays, each with its own transform
            {
                Bitmap[] numbers = new Bitmap[20];
                Matrix4[] numberpos = new Matrix4[20];

                Font fnt = new Font("Arial", 44);

                for (int i = 0; i < numbers.Length; i++)
                {
                    int v = -100 + i * 10;
                    numbers[i] = new Bitmap(100, 100);
                    BitMapHelpers.DrawTextCentreIntoBitmap(ref numbers[i], v.ToString(), fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.Red, Color.AliceBlue);
                    numberpos[i] = Matrix4.CreateScale(1);
                    numberpos[i] *= Matrix4.CreateRotationX(-80f.Radians());
                    numberpos[i] *= Matrix4.CreateTranslation(new Vector3(20, 0, v));
                }

                GLTexture2DArray array = new GLTexture2DArray(numbers, SizedInternalFormat.Rgba8, ownbitmaps: true);
                items.Add(array, "Nums");
                items.Add(new GLShaderPipeline(new GLPLVertexShaderTextureModelCoordWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexed(0)), "IC-2");

                GLRenderState rq = GLRenderState.Quads(cullface: false);
                GLRenderDataTexture rt = new GLRenderDataTexture(items.Tex("Nums"));

                rObjects.Add(items.Shader("IC-2"), "1-b",
                                        GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.Quads, rq,
                                                GLShapeObjectFactory.CreateQuad(1.0f), GLShapeObjectFactory.TexQuad, numberpos, rt,
                                                numberpos.Length));
            }

            {
                int left = -40, right = 40, bottom = -20, top = +20, front = -40, back = 40;
                Vector4[] lines2 = new Vector4[]
                {
                    new Vector4(left,bottom,front,1),   new Vector4(left,top,front,1),
                    new Vector4(left,top,front,1),      new Vector4(right,top,front,1),
                    new Vector4(right,top,front,1),     new Vector4(right,bottom,front,1),
                    new Vector4(right,bottom,front,1),  new Vector4(left,bottom,front,1),

                    new Vector4(left,bottom,back,1),    new Vector4(left,top,back,1),
                    new Vector4(left,top,back,1),       new Vector4(right,top,back,1),
                    new Vector4(right,top,back,1),      new Vector4(right,bottom,back,1),
                    new Vector4(right,bottom,back,1),   new Vector4(left,bottom,back,1),

                    new Vector4(left,bottom,front,1),   new Vector4(left,bottom,back,1),
                    new Vector4(left,top,front,1),      new Vector4(left,top,back,1),
                    new Vector4(right,bottom,front,1),  new Vector4(right,bottom,back,1),
                    new Vector4(right,top,front,1),     new Vector4(right,top,back,1),

                };

                items.Add(new GLFixedShader(System.Drawing.Color.Yellow), "LINEYELLOW");
                rObjects.Add(items.Shader("LINEYELLOW"),
                            GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rl1, lines2));
            }


            items.Add(new ShaderV2(), "V2");

            Vector4[] points = new Vector4[]
            {
                new Vector4(-40,-20,-40,1),
                new Vector4(+40,+20,+40,1),
            };

            GLRenderState rltot = GLRenderState.Tri();
            rObjects.Add(items.Shader("V2"), GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rltot, points, ic: 1));

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            dataoutbuffer = items.NewStorageBlock(5);
            dataoutbuffer.AllocateBytes(sizeof(float) * 4 * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicRead);    // 32 vec4 back

            atomicbuffer = items.NewAtomicBlock(6);
            atomicbuffer.AllocateBytes(sizeof(float) * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicCopy);

        }

        GLStorageBlock dataoutbuffer;
        GLAtomicBlock atomicbuffer;

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).Set(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            dataoutbuffer.ZeroBuffer();
            atomicbuffer.ZeroBuffer();

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

            Vector4[] databack = dataoutbuffer.ReadVector4s(0, 12);

            for (int i = 0; i < databack.Length; i += 1)
            {
                databack[i] = databack[i] / databack[i].W;      // normalise to screen co-ords in x/y
                System.Diagnostics.Debug.WriteLine("{0} v{1}", i, databack[i].ToStringVec());
            }

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;
        }

        private void SystemTick(object sender, EventArgs e)
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
        }

        private void OtherKeys(GLOFC.Controller.KeyboardMonitor kb)
        {
        }
    }
}

