﻿/*
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

using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.ShapeFactory;

// Demonstrate the volumetric calculations needed to compute a plane facing the user inside a bounding box done inside a geo shader

namespace TestOpenTk
{
    public partial class ShaderTestVolumetric2 : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestVolumetric2()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);
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

void main(void)
{
    color = vs_color;
}
";

            public ShaderV2()
            {
                //CompileLink(vertex: vcode, frag: fcode, geo: "#include TestOpenTk.Volumetrics.volumetricgeo2.glsl");
                //                CompileLink(vertex: vcode, frag: fcode, geo: @"#include c:\code\ofc\tests\Volumetrics\volumetricgeo2.glsl");

                //GLShader.includepaths.Add(@"c:\code\ofc\tests");
                //GLShader.includepaths.Add(@"c:\code\ofc\tests\Volumetrics");
                //CompileLink(vertex: vcode, frag: fcode, geo: @"#include volumetricgeo2.glsl");

                GLShader.IncludeModules.Add("TestOpenTk.Volumetrics");
                CompileLink(vertex: vcode, frag: fcode, geo: "#include volumetricgeo2.glsl");


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
                AddVertexFragment(new GLPLVertexShaderViewSpace(), new GLPLFragmentShaderFixedColor(c));
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
            gl3dcontroller.Start(glwfc,new Vector3(30, 0, 0), new Vector3( 135f, 0, 0), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 100.0f;
            };

            items.Add(new GLColorShaderWorld(), "COSW");
            GLRenderState rll = GLRenderState.Lines();

            {

                rObjects.Add(items.Shader("COSW"), "L1",   // horizontal
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rll,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(-100, 0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Gray })
                                   );


                rObjects.Add(items.Shader("COSW"),    // vertical
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rll,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(100, 0, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Gray })
                                   );

            }

            // New geo shader

            Vector4[] points = new Vector4[]
            {
                new Vector4(20,-5,-10,1),       //PT1
                new Vector4(40,+5,+10,1),       //PT7
            };

            items.Add(new ShaderV2(), "V2");

            GLRenderState rltot = GLRenderState.Tri();
            rObjects.Add(items.Shader("V2"), GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rltot, points, ic: 9));        // ic select number of slices

            int left = 20, right = 40, bottom = -5, top = +5, front = -10, back = 10;
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
            rObjects.Add(items.Shader("LINEYELLOW"),  GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rll, lines2));

            dataoutbuffer = items.NewStorageBlock(5);
            dataoutbuffer.AllocateBytes(sizeof(float) * 4 * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicRead);

            atomicbuffer = items.NewAtomicBlock(6);
            atomicbuffer.AllocateBytes(sizeof(float) * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicCopy);

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
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

            Vector4[] databack = dataoutbuffer.ReadVector4s(0, 2);
            for (int i = 0; i < databack.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine(i + " = " + databack[i].ToString());
            }

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " dir " + gl3dcontroller.PosCamera.CameraDirection + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;
        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true);
        }

    }
}

