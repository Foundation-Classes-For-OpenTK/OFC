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
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;

namespace TestOpenTk
{
    public partial class ShaderTestVolumetricNoise : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestVolumetricNoise()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

        }


        /// ////////////////////////////////////////////////////////////////////////////////////////////////////

        public class GLFixedShader : GLShaderPipeline
        {
            public GLFixedShader(Color c, Action<IGLProgramShader, GLMatrixCalc> action = null) : base(action)
            {
                AddVertexFragment(new GLPLVertexShaderWorldCoord(), new GLPLFragmentShaderFixedColor(c));
            }
        }

        
        void DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            //string s = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message);
            //System.Diagnostics.Debug.WriteLine("{0} {1} {2} {3} {4} {5}", source, type, id, severity, length, s);
           //  s = null;
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        Vector4[] boundingbox;
        GLStorageBlock dataoutbuffer;
        GLVolumetricUniformBlock volumetricblock;
        GLAtomicBlock atomicbuffer;
        GLRenderableItem noisebox;

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            //GLStatics.EnableDebug(DebugProc);

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 1f;
            gl3dcontroller.MatrixCalc.PerspectiveFarZDistance = 500000f;
            gl3dcontroller.ZoomDistance = 5000F;
            gl3dcontroller.YHoldMovement = true;

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms * 10.0f;
            };

            gl3dcontroller.MatrixCalc.InPerspectiveMode = true;
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, -35000), new Vector3(135f, 0, 0), 0.31622F);


            items.Add(new GLColorShaderWorld(), "COSW");
            GLRenderState rl1 = GLRenderState.Lines();

            float h = -1;
            if ( h != -1 )
            {
                Color cr = Color.FromArgb(60, Color.Gray);
                rObjects.Add(items.Shader("COS-1L"),    // horizontal
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl1,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-35000, h, -35000), new Vector3(-35000, h, 35000), new Vector3(1000, 0, 0), 70),
                                                        new Color4[] { cr })
                                   );

                rObjects.Add(items.Shader("COS-1L"),  
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl1,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-35000, h, -35000), new Vector3(35000, h, -35000), new Vector3(0, 0,1000), 70),
                                                        new Color4[] { cr })
                                   );
            }

            int hsize = 35000, vsize = 2000, zsize = 35000;

            {
                int left = -hsize, right = hsize, bottom = -vsize, top = +vsize, front = -zsize, back = zsize;
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

            // bounding box

            boundingbox = new Vector4[]
            {
                new Vector4(-hsize,-vsize,-zsize,1),
                new Vector4(-hsize,vsize,-zsize,1),
                new Vector4(hsize,vsize,-zsize,1),
                new Vector4(hsize,-vsize,-zsize,1),

                new Vector4(-hsize,-vsize,zsize,1),
                new Vector4(-hsize,vsize,zsize,1),
                new Vector4(hsize,vsize,zsize,1),
                new Vector4(hsize,-vsize,zsize,1),
            };

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            dataoutbuffer = items.NewStorageBlock(5);
            dataoutbuffer.AllocateBytes(sizeof(float) * 4 * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicRead);    // 32 vec4 back

            atomicbuffer = items.NewAtomicBlock(6);
            atomicbuffer.AllocateBytes(sizeof(float) * 32, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicCopy);

            dataoutbuffer = items.NewStorageBlock(5);
            dataoutbuffer.AllocateBytes(sizeof(float) * 4 * 256, OpenTK.Graphics.OpenGL4.BufferUsageHint.DynamicRead);    // 32 vec4 back

            volumetricblock = new GLVolumetricUniformBlock();
            items.Add(volumetricblock, "VB");

            {
                Bitmap[] numbers = new Bitmap[70];
                Matrix4[] numberpos = new Matrix4[numbers.Length];
                Matrix4[] numberpos2 = new Matrix4[numbers.Length];

                Font fnt = new Font("Arial", 20);

                for (int i = 0; i < numbers.Length; i++)
                {
                    int v = -35000 + i * 1000;
                    numbers[i] = new Bitmap(100, 100);
                    GLOFC.Utils.BitMapHelpers.DrawTextCentreIntoBitmap(ref numbers[i], v.ToString(), fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.Red, Color.AliceBlue);

                    numberpos[i] = Matrix4.CreateScale(1);
                    numberpos[i] *= Matrix4.CreateRotationX(-25f.Radians());
                    numberpos[i] *= Matrix4.CreateTranslation(new Vector3(35500, 0, v));
                    numberpos2[i] = Matrix4.CreateScale(1);
                    numberpos2[i] *= Matrix4.CreateRotationX(-25f.Radians());
                    numberpos2[i] *= Matrix4.CreateTranslation(new Vector3(v, 0, -35500));
                }

                GLTexture2DArray array = new GLTexture2DArray(numbers, SizedInternalFormat.Rgba8, ownbmp: true);
                items.Add(array, "Nums");
                items.Add(new GLShaderPipeline(new GLPLVertexShaderModelMatrixTexture(), new GLPLFragmentShaderTexture2DIndexed(0)), "IC-2");

                GLRenderState rq = GLRenderState.Tri(cullface: false);
                GLRenderDataTexture rt = new GLRenderDataTexture(items.Tex("Nums"));

                rObjects.Add(items.Shader("IC-2"), "1-b",
                                        GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.TriangleStrip, rq,
                                                GLShapeObjectFactory.CreateQuadTriStrip(500.0f,500.0f), GLShapeObjectFactory.TexTriStripQuad, numberpos,
                                                rt, numberpos.Length));

                rObjects.Add(items.Shader("IC-2"), "1-b2",
                                        GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.TriangleStrip, rq,
                                                GLShapeObjectFactory.CreateQuadTriStrip(500.0f,500.0f), GLShapeObjectFactory.TexTriStripQuad, numberpos2,
                                                rt, numberpos.Length));
            }

            GLTexture3D noise3d = new GLTexture3D(1024, 64, 1024, OpenTK.Graphics.OpenGL4.SizedInternalFormat.R32f); // red channel only

            //{     // shows program fill
            //    for (int ly = 0; ly < noise3d.Depth; ly++)
            //    {
            //        float[] fd = new float[noise3d.Width * noise3d.Height];
            //        float[] fdi = new float[noise3d.Width * noise3d.Height];
            //        for (int x = 0; x < noise3d.Width; x++)
            //        {
            //            for (int y = 0; y < noise3d.Height; y++)
            //            {
            //                int p = (y * noise3d.Width + x) * 1;

            //                float xv = (float)x / (float)noise3d.Width;
            //                float yv = (float)y / (float)noise3d.Height;

            //                var c = ((Math.Sin(2 * Math.PI * xv) / 2) + 0.5);
            //                c += ((Math.Cos(2 * Math.PI * yv) / 2) + 0.5);

            //                c /= 2;

            //                fd[p + 0] = (float)(c);
            //                fdi[p + 0] = 1.0f-(float)(c);
            //            }
            //        }

            //        noise3d.StoreZPlane(ly, 0, 0, noise3d.Width, noise3d.Height, fd, OpenTK.Graphics.OpenGL4.PixelFormat.Red);        // only a single float per pixel, stored in RED

            //    }
            //}

            ShaderNoiseDisplay ns = new ShaderNoiseDisplay();
            ns.StartAction += (a,m) => { noise3d.Bind(3); };
            items.Add(ns, "NS");
            GLRenderState rv = GLRenderState.Tri();
            noisebox = GLRenderableItem.CreateNullVertex(OpenTK.Graphics.OpenGL4.PrimitiveType.Points, rv);   // no vertexes, all data from bound volumetric uniform, no instances as yet

            rObjects.Add(items.Shader("NS"), noisebox);

            // make a compute shader noise and run it, to fill up noise3d buffer

            ComputeShaderNoise csn = new ComputeShaderNoise(noise3d.Width, noise3d.Height, noise3d.Depth,32,4,32);       // must be a multiple of localgroupsize in csn
            csn.StartAction += (A,m) => { noise3d.BindImage(3); };
            items.Add(csn, "CE1");
            GLComputeShaderList p = new GLComputeShaderList();      // demonstrate a render list holding a compute shader.
            p.Add(csn);
            p.Run();        // run the shader to compute into the noise3d block the noise.

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();

        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            items.Get<GLMatrixCalcUniformBlock>("MCUB").Set(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            if ( noisebox != null )
                noisebox.InstanceCount = volumetricblock.Set(gl3dcontroller.MatrixCalc, boundingbox, 10.0f);        // set up the volumentric uniform

            dataoutbuffer.ZeroBuffer();
            atomicbuffer.ZeroBuffer();

            rObjects.Render(glwfc.RenderState,gl3dcontroller.MatrixCalc);

            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

            Vector4[] databack = dataoutbuffer.ReadVector4s(0, 10);

            //System.Diagnostics.Debug.WriteLine("avg {0} txtavg {1}", databack[0].ToStringVec(), databack[1].ToStringVec());

            for (int i = 0; i < databack.Length; i += 1)
            {
         //       System.Diagnostics.Debug.WriteLine("db " + databack[i].ToStringVec());
            }

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.PosCamera.ZoomFactor;
        }

        public class ComputeShaderNoise: GLShaderCompute
        {
            static int Localgroupsize = 8;

            private string gencode(int w, int h, int d , int wb, int hb, int db)
            {
                return
@"
#version 450 core
#include Shaders.Functions.noise3.glsl
#include Shaders.Functions.random.glsl

layout (local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout (binding=3, r32f ) uniform image3D img;

void main(void)
{
    ivec3 p = ivec3(gl_GlobalInvocationID.xyz);

    float w = " + w.ToStringInvariant() + @";       // grab the constants from caller
    float h = " + h.ToStringInvariant() + @";
    float d = " + d.ToStringInvariant() + @";
    float wb = " + wb.ToStringInvariant() + @";     // these set the granularity of the image..
    float hb = " + hb.ToStringInvariant() + @";
    float db = " + db.ToStringInvariant() + @";

    vec3 np = vec3( float(gl_GlobalInvocationID.x)/w*wb, float(gl_GlobalInvocationID.y)/h*hb,float(gl_GlobalInvocationID.z)/d*db);

    float f = gradientnoiseT1(np);
    vec4 color = vec4(f*0.5+0.5,0,0,1);             // red only

    imageStore( img, p, color);                     // store back the computed noise
}
";
            }

            public ComputeShaderNoise(int width, int height, int depth, int wb, int hb, int db) : base(width/Localgroupsize,height/Localgroupsize,depth/Localgroupsize)
            {
                CompileLink( gencode(width,height,depth,wb,hb,db), out string unused) ;
            }

        }


        public class ShaderNoiseDisplay : GLShaderStandard
        {
            string vcode =
            @"
            #version 450 core

            out int instance;

            void main(void)
            {
                instance = gl_InstanceID;
            }
            ";

            string fcode = @"
#version 450 core
#include Shaders.Functions.noise2.glsl
#include Shaders.Functions.noise3.glsl
#include Shaders.Functions.random.glsl
#include Shaders.Functions.colours.glsl
out vec4 color;

in vec3 vs_texcoord;

layout (binding=3) uniform sampler3D tex;

void main(void)
{   
    //ivec3 texpos = ivec3(vs_texcoord.x*128,vs_texcoord.y*16,vs_texcoord.z*128);   
    //int lod=0;
    //vec4 n2 = texelFetch(tex, texpos,lod);  // pixel samples, need pixel position

    vec3 texpos = vec3(vs_texcoord.x,vs_texcoord.y,vs_texcoord.z);
    vec4 n2 = texture(tex, texpos);         // texture needs 0->1 across whole surface

    color = vec4(n2.x,0,0,1);
}
            ";

            public ShaderNoiseDisplay()
            {
                var an = System.Reflection.Assembly.GetExecutingAssembly().GetName();

                CompileLink(vertex: vcode, frag: fcode, geo: "#include " + an.Name + ".Volumetrics.volumetricgeoNoise.glsl");
            }
        }


        private void SystemTick(object sender, EventArgs e)
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);

        }

        private void OtherKeys(GLOFC.Controller.KeyboardMonitor kb)
        {
            if (kb.HasBeenPressed(Keys.F1, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
            }
        }
    }
}

