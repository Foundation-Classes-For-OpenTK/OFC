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
using GLOFC.Utils;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;

namespace TestOpenTk
{
    public partial class ShaderTestVertexCompute : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestVertexCompute()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public class GLVertexShaderCompute : GLShaderPipelineComponentShadersBase
        {
            public string Code()       // Runs the noise function over the vectors and reports state
            {
                return
    @"
#version 450 core
layout (location = 0) in vec4 position;

layout (binding = 1, std430) buffer Positions
{
    int count;
    float noisebuf[];
};

#include Shaders.Functions.snoise3.glsl

void write(float v)
{
    uint ipos = atomicAdd(count,1);
    if ( ipos < 1024 )
        noisebuf[ipos] = v;
}

void write(vec4 v)
{
    uint ipos = atomicAdd(count,4);
    if ( ipos < 1024 )
    {
        noisebuf[ipos] = v.x;
        noisebuf[ipos+1] = v.y;
        noisebuf[ipos+2] = v.z;
        noisebuf[ipos+3] = v.w;
    }
}

void main(void)
{
    vec3 position3 = normalize(position.xyz);

    float theta = dot(vec3(0,1,0),position3);    // angle between cur pos and up, modulo equator.  acos(n) would give radians. As both lengths should be modulo 1, no need to divide by |A||B|

    float unRadius = 1;
    vec3 sPosition = position3 * unRadius;

    float s = 0.36;
    float frequency = 1; //0.00001;
    float t1 = simplexnoise(sPosition * frequency) ;
    float t2 = simplexnoise((sPosition + unRadius) * frequency) ;
	float ss = (max(t1, 0.0) * max(t2, 0.0)) * 2.0;

    write(vec4(position3,theta));

}
";
            }

            public GLVertexShaderCompute()
            {
                CompileLink(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader, Code(), out string unused);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.ZoomDistance = 100F;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            glwfc.BackColor = Color.FromArgb(0, 0, 20);
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(120f, 0, 0f), 1F);
            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 20.0f;
            };

            // this bit is eye candy just to show its working

            items.Add(new GLColorShaderWorld(), "COSW");
            GLRenderState rl = GLRenderState.Lines();

            rObjects.Add(items.Shader("COSW"),
                         GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                    GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(-40, 0, 40), new Vector3(10, 0, 0), 9),
                                                    new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                               );


            rObjects.Add(items.Shader("COSW"),
                         GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                               GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(40, 0, -40), new Vector3(0, 0, 10), 9),
                                                         new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                               );


            items.Add(new GLTexture2D(Properties.Resources.moonmap1k, SizedInternalFormat.Rgba8), "moon");
            items.Add(new GLTexturedShaderObjectTranslation(), "TEX");

            GLRenderState rt = GLRenderState.Tri();
            rObjects.Add(items.Shader("TEX"), "sphere7",
                GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Triangles, rt,
                        GLSphereObjectFactory.CreateTexturedSphereFromTriangles(4, 20.0f),
                        new GLRenderDataTranslationRotationTexture(items.Tex("moon"), new Vector3(0, 0, 0))
                        ));


            // Pass vertex data thru a vertex shader which stores into a block

            items.Add(new GLShaderPipeline(new GLVertexShaderCompute()), "N1");

            vecoutbuffer = new GLStorageBlock(1);           // new storage block on binding index 1 which the vertex shader uses
            vecoutbuffer.AllocateBytes(sizeof(float) * 2048 + sizeof(int), OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicCopy);       // set size of vec buffer

            //Vector4[] data = new Vector4[] {
            //    new Vector4(1, 2, 3, 0),
            //    new Vector4(4, 5, 6, 0)
            //};

            Vector4[] data = GLSphereObjectFactory.CreateSphereFromTriangles(0, 1.0f);

            GLRenderState rp = GLRenderState.PointsByProgram();
            rObjects.Add(items.Shader("N1"), GLRenderableItem.CreateVector4(items, PrimitiveType.Points, rp, data));

            for (double ang = -Math.PI / 2; ang <= Math.PI / 2 + 0.1; ang += 0.1)
            {
                Vector3 pos = new Vector3((float)Math.Cos(ang), (float)Math.Sin(ang), 0);
                Vector3 up = new Vector3(0, 1, 0);
                float dotp = Vector3.Dot(up, pos);
                float lens = (float)(up.Length * pos.Length);
                double computedang = Math.Acos(dotp / lens);
                System.Diagnostics.Debug.WriteLine(ang.Degrees() + " " + pos + "-> dotp" + dotp + " " + computedang.Degrees());
            }

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0
        }

        GLStorageBlock vecoutbuffer;


        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.Set(gl3dcontroller.MatrixCalc);

            vecoutbuffer.ZeroBuffer();
            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            int count = vecoutbuffer.ReadInt(0);
            if (count > 0)
            {
                float[] values = vecoutbuffer.ReadFloats(4, Math.Min(2000, count), true);
                System.Diagnostics.Debug.WriteLine("Count " + count + " min " + values.Min() + " max " + values.Max());
                for (int i = 0; i < count; i = i + 4)
                {
                    Vector3 pos = new Vector3(values[i], values[i + 1], values[i + 2]);
                    System.Diagnostics.Debug.Write("    " + i / 4 + " = " + pos + " : " + values[i + 3]);

                    Vector3 up = new Vector3(0, 1, 0);
                    float value = Vector3.Dot(up, pos);
                    value = 0.0f + value;
                    System.Diagnostics.Debug.WriteLine("        -> dotp" + value);
                }
            }

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            //gl3dcontroller.Redraw();
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
        }
    }
}


