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
using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics;
using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GLOFC.Utils;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Basic;

namespace TestOpenTk
{
    public partial class ShaderTestFunctions: Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestFunctions()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();


        // place functions in this compute shader for testing.

        public class ComputeShader : GLShaderCompute
        {
            private string gencode()
            {
                return
    @"
#version 450 core
#include Shaders.Functions.trig.glsl
#include Shaders.Functions.mat4.glsl

vec4 Encode( in float value )
{
    value *= (256.0*256.0*256.0 - 1.0) / (256.0*256.0*256.0);
    vec4 encode = fract( value * vec4(1.0, 256.0, 256.0*256.0, 256.0*256.0*256.0) );
    return vec4( encode.xyz - encode.yzw / 256.0, encode.w ) + 1.0/512.0;
}

float Decode( in vec4 pack )
{
    float value = dot( pack, 1.0 / vec4(1.0, 256.0, 256.0*256.0, 256.0*256.0*256.0) );
    return value * (256.0*256.0*256.0) / (256.0*256.0*256.0 - 1.0);
}

const vec4 bitEnc = vec4(1.,255.,65025.,16581375.);
const vec4 bitDec = 1./bitEnc;
vec4 EncodeFloatRGBA (float v) {
    vec4 enc = bitEnc * v;
    enc = fract(enc);
    enc -= enc.yzww * vec2(1./255., 0.).xxxy;
    return enc;
}
float DecodeFloatRGBA (vec4 v) {
    return dot(v, bitDec);
}


float DecodeFloatRGBA2( vec4 rgba ) 
{
  return dot( rgba, vec4(1.0, 1/255.0, 1/65025.0, 1/16581375.0) );
}

vec4 EncodeFloatRGBA2( float v ) 
{
  vec4 enc = vec4(1.0, 255.0, 65025.0, 16581375.0) * v;
  enc = fract(enc);
  enc -= enc.yzww * vec4(1.0/255.0,1.0/255.0,1.0/255.0,0.0);
  return enc;
}



layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

const int bindingoutdata = 30;
layout (binding = bindingoutdata, std430) buffer Positions      // StorageBlock note - buffer
{
    uint count;     // 0
    vec2 azel1;     // 4,8
    vec2 azel2;     // 12,16

    mat4   matrixout;   // aligned 32
    mat4   identity;
    mat4   rotatex;
    mat4   rotatey;
    mat4   rotatez;
    mat4   rotatexymanu;
    mat4   rotatexyauto;
    mat4 rotateyxmanu;
    mat4 rotateyxauto;

    mat4 translation;
    mat4 rottranslation;
    mat4 transrotscale;
    mat4 transrotscale2;

    vec4 packdecode;
};

void main(void)
{
    count = 10; // test variable
    azel1 = AzEl(vec3(0,0,0),vec3(10,0,0));
    azel2 = vec2(0.5,0.6);

    matrixout = mat4(   1,2,3,4,            // row 1
                        5,6,7,8,
                        9,10,11,12, 
                        13,14,15,16);        // demo memory order. See GLMatrix4Statics. Its layed out in row order.
    matrixout[2][3] = 20000;        // row 2, col 3 is 20000, demo its [row][col]

    identity = mat4identity();
    rotatex = mat4rotateX(0.7853);
    rotatey = mat4rotateY(0.7853);
    rotatez = mat4rotateZ(0.7853);

    // X THEN Y
    mat4 xpi4 = mat4rotateX(0.7853);
    mat4 rotatey05 = mat4rotateY(0.5);
    rotatexymanu = rotatey05 * xpi4;
    rotatexyauto = mat4rotateXthenY(0.7853,0.5);
    rotateyxmanu = xpi4 * rotatey05;
    rotateyxauto = mat4rotateYthenX(0.7853,0.5);

    translation = mat4translation(vec3(10,20,30));

    rottranslation = mat4translation(rotatey05,vec3(10,20,30));

    mat4 trans = mat4translation(vec3(10,20,30));

    mat4 rotscale = mat4ScalethenRotateXthenY(radians(-90),radians(90),vec3(1,2,3));        //    transrotscale = trans * roty * rotx * mscale;
    transrotscale = trans * rotscale;

    transrotscale2 = mat4ScalethenRotateXthenYthenTranslation(radians(-90),radians(90),vec3(1,2,3),vec3(10,20,30));        //    transrotscale = trans * roty * rotx * mscale;

    vec4 colour = vec4(0.2,0.3,0.4,0.1);
    colour = vec4(2,3,4,5);
    //float cv = Decode(colour);
    //packdecode = Encode(cv);

    float vc = DecodeFloatRGBA2(colour);
    packdecode = EncodeFloatRGBA2(vc);

}
";
            }

            public ComputeShader() : base(1,1,1)
            {
                CompileLink(gencode());
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
            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                return (float)ms / 20.0f;
            };

            // this bit is eye candy just to show its working

            items.Add(new GLColorShaderWithWorldCoord(), "COSW");
            GLRenderState rl = GLRenderState.Lines(1);

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
            items.Add(new GLTexturedShaderWithObjectTranslation(), "TEX");

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0

            // Pass vertex data thru a vertex shader which stores into a block

            vecoutbuffer = new GLStorageBlock(30, true);
            vecoutbuffer.AllocateBytes(32000, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicCopy);       // set size of vec buffer

            ComputeShader csn = new ComputeShader();
            csn.Run();
            GLMemoryBarrier.All();

            // test consistency between row/columns of matrix4 of code and glsl
            int count = vecoutbuffer.ReadInt(0);
            System.Diagnostics.Debug.WriteLine("Count is " + count);

            {
                float[] mat4 = vecoutbuffer.ReadFloats(16, 16);
                System.Diagnostics.Debug.WriteLine("Compare mat4 constructor via order of floats:");
                for (int i = 0; i < mat4.Length; i++) System.Diagnostics.Debug.Write(string.Format("{0} = {1}, ", i, mat4[i]));
                System.Diagnostics.Debug.WriteLine("");
            }

            {
                Vector2[] vec2 = vecoutbuffer.ReadVector2s(4, 2);
                System.Diagnostics.Debug.WriteLine($"Vec2a = inc {vec2[0].X.Degrees()} az {vec2[0].Y.Degrees()}");
            }

            Matrix4[] mat4r = vecoutbuffer.ReadMatrix4s(32, 32); // read all matrixes

            System.Diagnostics.Debug.WriteLine("Test mat constructor =" + Environment.NewLine + mat4r[0].ToString());
            System.Diagnostics.Debug.WriteLine("Row0 is " + mat4r[0].Row0.ToString());
            System.Diagnostics.Debug.WriteLine("Should be 20000 = " + mat4r[0][2, 3]);

            int id = 1;
            System.Diagnostics.Debug.WriteLine("Identity matrix = " + Environment.NewLine + mat4r[id++].ToString());

            {
                Matrix4 xrotpi4 = Matrix4.CreateRotationX(0.7853f);     // demo that Matrix4.Create is the same values as mat4rotatex
                System.Diagnostics.Debug.WriteLine("Rotate X Pi/4 =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], xrotpi4) + Environment.NewLine + mat4r[id].ToString());
                id++;
            }

            {
                Matrix4 yrotpi4 = Matrix4.CreateRotationY(0.7853f);
                System.Diagnostics.Debug.WriteLine("Rotate Y Pi/4 =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], yrotpi4) + Environment.NewLine + mat4r[id].ToString());
                id++;
            }

            {
                Matrix4 zrotpi4 = Matrix4.CreateRotationZ(0.7853f);
                System.Diagnostics.Debug.WriteLine("Rotate Z Pi/4 =" + GLStaticsMatrix4.ApproxEquals(mat4r[4], zrotpi4) + Environment.NewLine + mat4r[4].ToString());
                id++;
            }

            {   // test:
                //Matrix4 xrot = Matrix4.CreateRotationX((float)(Math.PI / 2)); 
                //Matrix4 r1 = new Matrix4(0, 0, 0, 0,
                //                         0, 0, 0, 0,
                //                         0, 0, 1, 0,
                //                         0, 0, 0, 1);
                //r1 = new Matrix4(new Matrix3())
                //Matrix4 r2 = r1 * xrot;
                //System.Diagnostics.Debug.WriteLine($"rotate z=+1 around x by 90\n{r1}\n{r2} ");
            }
            // X Y
            {



                Matrix4 xrot= Matrix4.CreateRotationX(0.7853f);     // demo that Matrix4.Create is the same values as mat4rotatex
                Matrix4 yrot = Matrix4.CreateRotationY(0.5f);
                Matrix4 res = Matrix4.Identity;
                res = res * xrot;
                res = res * yrot;
                System.Diagnostics.Debug.WriteLine("Rotate XY Manu =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], res) + Environment.NewLine + mat4r[id].ToString());
                id++;
                System.Diagnostics.Debug.WriteLine("Rotate XY Auto =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], res) + Environment.NewLine + mat4r[id].ToString());
                id++;
                res = Matrix4.Identity;
                res = res * yrot;
                res = res * xrot;
                System.Diagnostics.Debug.WriteLine("Rotate YX Manu =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], res) + Environment.NewLine + mat4r[id].ToString());
                id++;
                System.Diagnostics.Debug.WriteLine("Rotate YX Auto =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], res) + Environment.NewLine + mat4r[id].ToString());
                id++;
            }

            // mat4 translation
            {
                Matrix4 trans = Matrix4.CreateTranslation(10, 20, 30);
                System.Diagnostics.Debug.WriteLine("Translation =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], trans) + Environment.NewLine + mat4r[id].ToString());
                id++;
            }

            // mat4translation with matrix
            {
                Matrix4 yrot05 = Matrix4.CreateRotationY(0.5f);
                Matrix4 trans = Matrix4.CreateTranslation(10, 20, 30);
                Matrix4 rotplustrans = Matrix4.Mult(yrot05, trans);
                System.Diagnostics.Debug.WriteLine("Rot Translation =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], rotplustrans) + Environment.NewLine + mat4r[id].ToString());
                id++;
            }

            {
                Matrix4 trans = Matrix4.CreateTranslation(10, 20, 30);
                Matrix4 rotxm90 = Matrix4.CreateRotationX(-90f.Radians());
                Matrix4 roty90 = Matrix4.CreateRotationY(90f.Radians());
                Matrix4 mscale = Matrix4.CreateScale(1, 2, 3);

                Matrix4 res = Matrix4.Identity;
                res = res * mscale;
                res = res * rotxm90;
                res = res * roty90;
                res = res * trans;
                System.Diagnostics.Debug.WriteLine("Trans rot scale =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], res) + Environment.NewLine + mat4r[id].ToString());
                id++;
                //System.Diagnostics.Debug.WriteLine(transrotscale.ToString());

                System.Diagnostics.Debug.WriteLine("Trans rot scale2 =" + GLStaticsMatrix4.ApproxEquals(mat4r[id], res) + Environment.NewLine + mat4r[id].ToString());
                id++;
            }


            Vector4[] vec4r = vecoutbuffer.ReadVector4s(32+13*64, 2); // read all vec4 (N matrices between)

            {
                System.Diagnostics.Debug.WriteLine($"Vec4 {vec4r[0]}");
            }




            {
                StringMatrix rox = new StringMatrix("1", "0", "0", "0",
                                                    "0", "cx", "sx", "0",
                                                    "0", "-sx", "cx", "0",
                                                    "0", "0", "0", "1");
                StringMatrix roxm90 = new StringMatrix("1", "0", "0", "0",
                                                    "0", "0", "-1", "0",
                                                    "0", "1", "0", "0",
                                                    "0", "0", "0", "1");
                StringMatrix roy = new StringMatrix("cy", "0", "-sy", "0",
                                                    "0", "1", "0", "0",
                                                    "sy", "0", "cy", "0",
                                                    "0", "0", "0", "1");
                StringMatrix res = StringMatrix.Mult(roxm90, roy);
                string r = res.ToString(true);
                System.Diagnostics.Debug.WriteLine($"{r}");
                r = res.ToList();
                System.Diagnostics.Debug.WriteLine($"{r}");

                res = StringMatrix.Mult(rox, roy);
                r = res.ToString(true);
                System.Diagnostics.Debug.WriteLine($"{r}");

                StringVector4 v4a = new StringVector4("a", "-1", "c", "d");
                StringVector4 resv4 = StringMatrix.Mult(rox, v4a);
                r = resv4.ToString(true);
                System.Diagnostics.Debug.WriteLine($"{r}");


            }


            //    System.Di
            //    agnostics.Debug.WriteLine(rotplustransscale.ToString());


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

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true);
        }

    }

 }


