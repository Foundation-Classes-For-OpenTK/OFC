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
using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestOpenTk
{
    public partial class ShaderTestBlendedShaderMultImages : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestBlendedShaderMultImages()
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
            gl3dcontroller.ZoomDistance = 100F;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            glwfc.BackColor = Color.FromArgb(0, 0, 60);
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, 0), new Vector3(120f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 20.0f;
            };

            IGLTexture array2d = items.Add(new GLTexture2DArray(new Bitmap[] { Properties.Resources.mipmap, Properties.Resources.mipmap2,
                                Properties.Resources.mipmap3, Properties.Resources.mipmap4 }, SizedInternalFormat.Rgba8, 9), "2DArray2");

            if (true)
            {
                items.Add(new GLMultipleTexturedBlended(false, 2), "ShaderPos");
                items.Shader("ShaderPos").StartAction += (s,m) =>
                {
                    array2d.Bind(1);
                };

                Vector4[] instancepositions = new Vector4[4];
                instancepositions[0] = new Vector4(-25, 0, -40, 0);      // last is image index..
                instancepositions[1] = new Vector4(-25, 0, 0, 0);
                instancepositions[2] = new Vector4(-25, 0, 40, 2);
                instancepositions[3] = new Vector4(-25, 0, 80, 2);

                GLRenderState rt = GLRenderState.Tri(cullface: false);
                rObjects.Add(items.Shader("ShaderPos"),
                           GLRenderableItem.CreateVector4Vector2Vector4(items, PrimitiveType.Triangles, rt,
                                   GLSphereObjectFactory.CreateTexturedSphereFromTriangles(3, 20.0f),
                                   instancepositions, ic: 4, separbuf: true
                                   ));
            }

            // Shader MAT

            if (true)
            {
                IGLProgramShader smat = items.Add(new GLMultipleTexturedBlended(true, 2), "ShaderMat");
                smat.StartAction += (s,m) =>
                {
                    array2d.Bind(1);
                };

                Matrix4[] pos2 = new Matrix4[3];
                pos2[0] = Matrix4.CreateRotationY(-80f.Radians());
                pos2[0] *= Matrix4.CreateTranslation(new Vector3(25, 0, -40));
                pos2[0].M44 = 0;        // this is the image number

                pos2[1] = Matrix4.CreateRotationY(-70f.Radians());
                pos2[1] *= Matrix4.CreateTranslation(new Vector3(25, 0, 0));
                pos2[1].M44 = 2;        // this is the image number

                pos2[2] = Matrix4.CreateRotationZ(-60f.Radians());
                pos2[2] *= Matrix4.CreateTranslation(new Vector3(25, 0, 40));
                pos2[2].M44 = 0;        // this is the image number

                GLRenderState rq = GLRenderState.Quads(cullface: false);
                rObjects.Add(items.Shader("ShaderMat"),
                    GLRenderableItem.CreateVector4Vector2Matrix4(items, PrimitiveType.Quads, rq,
                            GLShapeObjectFactory.CreateQuad(20.0f, 20.0f, new Vector3(-90f.Radians(), 0, 0)), GLShapeObjectFactory.TexQuad,
                            pos2, ic: 3, separbuf: false
                            ));
            }

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0
        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong time)
        {
            // System.Diagnostics.Debug.WriteLine("Draw eye " + gl3dcontroller.MatrixCalc.EyePosition + " to " + gl3dcontroller.Pos.Current);

            float zeroone10s = ((float)(time % 10000)) / 10000.0f;
            float zeroone5s = ((float)(time % 5000)) / 5000.0f;
            float zerotwo5s = ((float)(time % 5000)) / 2500.0f;
            float degrees = zeroone10s * 360;
            // matrixbuffer.Write(Matrix4.CreateTranslation(new Vector3(zeroone * 20, 50, 0)),0,true);

            if (items.Contains("ShaderPos"))
            {
                ((GLMultipleTexturedBlended)items.Shader("ShaderPos")).CommonTransform.YRotDegrees = degrees;
                ((GLMultipleTexturedBlended)items.Shader("ShaderPos")).Blend = zerotwo5s;
            }

            if (items.Contains("ShaderMat"))
            {
                ((GLMultipleTexturedBlended)items.Shader("ShaderMat")).CommonTransform.ZRotDegrees = degrees;
                ((GLMultipleTexturedBlended)items.Shader("ShaderMat")).Blend = zerotwo5s;
            }

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.Set(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;

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


