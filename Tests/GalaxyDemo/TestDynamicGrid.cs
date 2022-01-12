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
using GLOFC.GL4.Shaders.Fragment;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.ShapeFactory;

namespace TestOpenTk
{
    public partial class TestDynamicGrid : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestDynamicGrid()
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


        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        float lasteyedistance = 100000000;
        int lastgridwidth;

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).Set(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            IGLRenderableItem i = rObjects["DYNGRIDRENDER"];
            DynamicGridVertexShader s = items.PLShader("PLGRIDVertShader") as DynamicGridVertexShader;

            if (Math.Abs(lasteyedistance - gl3dcontroller.MatrixCalc.EyeDistance) > 10)     // a little histerisis
            {
                i.InstanceCount = s.ComputeGridSize(gl3dcontroller.MatrixCalc.EyeDistance, out lastgridwidth);
                lasteyedistance = gl3dcontroller.MatrixCalc.EyeDistance;
            }

            s.SetUniforms(gl3dcontroller.MatrixCalc.LookAt, lastgridwidth, i.InstanceCount);

            DynamicGridCoordVertexShader bs = items.PLShader("PLGRIDBitmapVertShader") as DynamicGridCoordVertexShader;
            bs.ComputeUniforms(lastgridwidth, gl3dcontroller.MatrixCalc, gl3dcontroller.PosCamera.CameraDirection, Color.Yellow, Color.Transparent);

            solmarker.Position = gl3dcontroller.MatrixCalc.LookAt;
            solmarker.Scale = gl3dcontroller.MatrixCalc.EyeDistance / 20;

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.PosCamera.ZoomFactor;
        }



        GLRenderDataTranslationRotationTexture solmarker;
        GLTexture2DArray texcoords;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 1f;
            gl3dcontroller.MatrixCalc.PerspectiveFarZDistance = 500000f;
            gl3dcontroller.ZoomDistance = 5000F;
            gl3dcontroller.YHoldMovement = true;
            gl3dcontroller.PaintObjects = ControllerDraw;

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms * 1.0f * Math.Min(eyedist/1000,10);
            };

            gl3dcontroller.PosCamera.ZoomScaling = 1.1f;

            gl3dcontroller.MatrixCalc.InPerspectiveMode = true;
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(140.75f, 0, 0), 0.5F);

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

            int front = -20000, back = front + 90000, left = -45000, right = left + 90000, vsize = 2000;
 
            Vector4[] displaylines = new Vector4[]
            {
                new Vector4(left,-vsize,front,1),   new Vector4(left,+vsize,front,1),
                new Vector4(left,+vsize,front,1),      new Vector4(right,+vsize,front,1),
                new Vector4(right,+vsize,front,1),     new Vector4(right,-vsize,front,1),
                new Vector4(right,-vsize,front,1),  new Vector4(left,-vsize,front,1),

                new Vector4(left,-vsize,back,1),    new Vector4(left,+vsize,back,1),
                new Vector4(left,+vsize,back,1),       new Vector4(right,+vsize,back,1),
                new Vector4(right,+vsize,back,1),      new Vector4(right,-vsize,back,1),
                new Vector4(right,-vsize,back,1),   new Vector4(left,-vsize,back,1),

                new Vector4(left,-vsize,front,1),   new Vector4(left,-vsize,back,1),
                new Vector4(left,+vsize,front,1),      new Vector4(left,+vsize,back,1),
                new Vector4(right,-vsize,front,1),  new Vector4(right,-vsize,back,1),
                new Vector4(right,+vsize,front,1),     new Vector4(right,+vsize,back,1),
            };

            {
                items.Add( new GLFixedColorShaderWorld(System.Drawing.Color.Yellow), "LINEYELLOW");
                GLRenderState rl = GLRenderState.Lines(1);
                rObjects.Add(items.Shader("LINEYELLOW"), GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rl, displaylines));
            }


            {
                items.Add(new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8),"solmarker");
                items.Add(new GLTexturedShaderObjectTranslation(), "TEX");
                GLRenderState rq = GLRenderState.Quads(cullface: false);
                solmarker = new GLRenderDataTranslationRotationTexture(items.Tex("solmarker"), new Vector3(0, 0, 0));
                rObjects.Add(items.Shader("TEX"),
                             GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                             GLShapeObjectFactory.CreateQuad(1.0f, 1.0f, new Vector3(0, 0, 0)), GLShapeObjectFactory.TexQuad,
                             solmarker
                             ));
            }



            {
                items.Add(new DynamicGridVertexShader(Color.Cyan), "PLGRIDVertShader");
                items.Add(new GLPLFragmentShaderVSColor(), "PLGRIDFragShader");

                GLRenderState rl = GLRenderState.Lines(1);
                rl.DepthTest = false;

                items.Add(new GLShaderPipeline(items.PLShader("PLGRIDVertShader"), items.PLShader("PLGRIDFragShader")), "DYNGRID");
                rObjects.Add(items.Shader("DYNGRID"), "DYNGRIDRENDER", GLRenderableItem.CreateNullVertex(PrimitiveType.Lines, rl,  drawcount: 2));

            }


            {
                items.Add( new DynamicGridCoordVertexShader(), "PLGRIDBitmapVertShader");
                items.Add(new GLPLFragmentShaderTexture2DIndexed(0), "PLGRIDBitmapFragShader");     // binding 1

                GLRenderState rl = GLRenderState.Tri(cullface: false);
                rl.DepthTest = false;


                texcoords = new GLTexture2DArray();
                items.Add( texcoords, "PLGridBitmapTextures");

                GLShaderPipeline sp = new GLShaderPipeline(items.PLShader("PLGRIDBitmapVertShader"), items.PLShader("PLGRIDBitmapFragShader"));

                items.Add(sp, "DYNGRIDBitmap");

                rObjects.Add(items.Shader("DYNGRIDBitmap"), "DYNGRIDBitmapRENDER", GLRenderableItem.CreateNullVertex(PrimitiveType.TriangleStrip, rl, drawcount: 4, instancecount:9));
            }
        }

        private void SystemTick(object sender, EventArgs e)
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            //if (cdmt.AnythingChanged)
            //    gl3dcontroller.Redraw();
        }

        private void OtherKeys(GLOFC.Controller.KeyboardMonitor kb)
        {
        }


    }
}


