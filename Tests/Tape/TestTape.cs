/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// A simpler main for demoing

namespace TestOpenTk
{
    public partial class TestTape : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public TestTape()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 1f;
            gl3dcontroller.MatrixCalc.PerspectiveFarZDistance=21000f;
            gl3dcontroller.MouseTranslateAmountAtZoom1PerPixel = 0.5f;
            gl3dcontroller.ZoomDistance = 50F;
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(135f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 100.0f;
            };

            // create stock shaders

            items.Add(new GLColorShaderWorld(), "COSW");
            items.Add(new GLColorShaderObjectTranslation(), "COSOT");
            items.Add( new GLTexturedShaderObjectTranslation(),"TEXOT");

            // create stock textures

            items.Add( new GLTexture2D(Properties.Resources.dotted, SizedInternalFormat.Rgba8)  , "dotted");
            items.Add(new GLTexture2D(Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "dotted2");
            items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "logo8bpp");
            items.Add(new GLTexture2D(Properties.Resources.smile5300_256x256x8, SizedInternalFormat.Rgba8), "smile");

            #region coloured lines

            if (true)
            {
                GLRenderState lines = GLRenderState.Lines(1);

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(-100, -0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed })
                                   );


                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, -0, -100), new Vector3(100, -0, -100), new Vector3(0, 0, 10), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.DarkRed, Color.DarkRed }));
            }

            #endregion

            int ctrl = -1;

            #region Tape

            {
                var pls = new GLShaderPipeline(new GLPLVertexShaderWorldTextureTriStrip(),
                                    new GLPLFragmentShaderTextureTriStripColorReplace(1, Color.FromArgb(255, 206, 0, 0)));
                items.Add(pls, "tapeshader");
            }

            {
                items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "tapelogo");
                items.Tex("tapelogo").SetSamplerMode(OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);

                items.Add(new GLTexture2D(Properties.Resources.Logo8bpp, SizedInternalFormat.Rgba8), "tapelogo2");
                items.Tex("tapelogo2").SetSamplerMode(OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);

                items.Add(new GLTexture2D(Properties.Resources.chevron, SizedInternalFormat.Rgba8), "tapelogo3");
                items.Tex("tapelogo3").SetSamplerMode(OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
            }

            if ((ctrl & (1 << 11)) != 0)
            {
                var p = GLTapeObjectFactory.CreateTape(new Vector3(0, 5, 50), new Vector3(100, 50, 100), 4, 20, 80F.Radians());

                GLRenderState rts = GLRenderState.Tri();
                rts.CullFace = false;

                var ri = GLRenderableItem.CreateVector4(items, PrimitiveType.TriangleStrip, rts, p, new GLRenderDataTexture(items.Tex("tapelogo")));

                rObjects.Add(items.Shader("tapeshader"), "tape1", ri);
            }

            if ((ctrl & (1 << 12)) != 0)
            {
                var p = GLTapeObjectFactory.CreateTape(new Vector3(-0, 5, 50), new Vector3(-100, 50, 100), 4, 20, 80F.Radians());


                GLRenderState rts = GLRenderState.Tri();
                rts.CullFace = false;

                rObjects.Add(items.Shader("tapeshader"), "tape2", GLRenderableItem.CreateVector4(items, PrimitiveType.TriangleStrip, rts, p, new GLRenderDataTexture(items.Tex("tapelogo2"))));
            }

            if ((ctrl & (1 << 13)) != 0)
            {
                // tape goes right to left, demoing a 4 point, 6 point, more tape

                Vector4[] points = new Vector4[] { new Vector4(5, 5, 40, 0), new Vector4(0, 5, 40, 0), new Vector4(-25, 5, 40, 0), new Vector4(-35, 5, 40, 0), new Vector4(-100, 5, 40, 0) };
                Color[] colours = new Color[] { Color.Red, Color.Green, Color.Blue, Color.White };
                var tape = GLTapeObjectFactory.CreateTape(points.ToArray(), colours, 1, 10f, 90F.Radians(), margin: 1f, modulo: 4);

                GLRenderState rts = GLRenderState.Tri(tape.Item3);     // sets primitive restart value to Item3 draw type
                rts.CullFace = false;

                GLRenderableItem ri = GLRenderableItem.CreateVector4(items, PrimitiveType.TriangleStrip, rts, tape.Item1.ToArray(), new GLRenderDataTexture(items.Tex("tapelogo3")));
                ri.CreateElementIndex(items.NewBuffer(), tape.Item2.ToArray(), tape.Item3);

                rObjects.Add(items.Shader("tapeshader"), "tape3", ri);
            }


            #endregion


            #region TapeNormals

            {
                var pls = new GLShaderPipeline(new GLPLVertexShaderWorldTextureTriStripNorm(100,1,10),
                                    new GLPLFragmentShaderTextureTriStripColorReplace(1, Color.FromArgb(255, 206, 0, 0)));
                items.Add(pls, "tapeshadernorm");
            }

            if ((ctrl & (1 << 0)) != 0 )
            {
                var pn = GLTapeNormalObjectFactory.CreateTape(new Vector3(0, 0, 20), new Vector3(60, 0, 20), 10, 45F.Radians());

                GLRenderState rts = GLRenderState.Tri();
                rts.CullFace = false;

                var ri = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.TriangleStrip, rts, pn.Item1,pn.Item2, new GLRenderDataTexture(items.Tex("tapelogo")));

                rObjects.Add(items.Shader("tapeshadernorm"), "tape10", ri);
            }

            if ((ctrl & (1 << 1)) != 0)
            {
                Vector4[] points = new Vector4[] { new Vector4(-80, 5, 10, 0), new Vector4(-50, 5, 10, 0), new Vector4(-20,5,20,0), new Vector4(0,5,30,0)};
                Color[] colours = new Color[] { Color.Red, Color.Green, Color.Blue, Color.White };
                var tape = GLTapeNormalObjectFactory.CreateTape(points.ToArray(), colours, 10, 90F.Radians(), margin: 1f, modulo: 4);

                GLRenderState rts = GLRenderState.Tri(tape.Item4);     // sets primitive restart value to Item3 draw type
                rts.CullFace = false;

                GLRenderableItem ri = GLRenderableItem.CreateVector4Vector4(items, PrimitiveType.TriangleStrip, rts, tape.Item1.ToArray(), tape.Item2.ToArray(), new GLRenderDataTexture(items.Tex("tapelogo3")));
                ri.CreateElementIndex(items.NewBuffer(), tape.Item3.ToArray(), tape.Item4);

                rObjects.Add(items.Shader("tapeshadernorm"), "tape11", ri);
            }

            #endregion


            #region Matrix Calc Uniform

            items.Add(new GLMatrixCalcUniformBlock(),"MCUB");     // def binding of 0

            #endregion

        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong ts)
        {
            //System.Diagnostics.Debug.WriteLine("Draw");

            var mcub = items.Get<GLMatrixCalcUniformBlock>("MCUB");
            mcub.SetFull(gl3dcontroller.MatrixCalc);

            float mtime = (float)(ts % 2000) / 2000.0f;
            float ztoo = mtime < 0.5 ? (mtime * 2) : (1-(mtime-0.5f) * 2);

            var verttape2 = items.GetPLComponent<GLPLVertexShaderWorldTextureTriStripNorm>("tapeshadernorm",ShaderType.VertexShader);
            verttape2.SetWidth(2+4*ztoo);

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
            
            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " from " + gl3dcontroller.MatrixCalc.EyePosition + 
                               " cdir " + gl3dcontroller.PosCamera.CameraDirection + " zoom " + gl3dcontroller.PosCamera.ZoomFactor + 
                                " dist " + gl3dcontroller.MatrixCalc.EyeDistance + " FOV " + gl3dcontroller.MatrixCalc.FovDeg;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            gl3dcontroller.Redraw();
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.F5, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(0, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F6, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(4, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F7, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(10, 0, -10), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F8, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(50, 0, 50), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F4, GLOFC.Controller.KeyboardMonitor.ShiftState.None))           // ! change mode to perspective
            {
                gl3dcontroller.ChangePerspectiveMode(!gl3dcontroller.MatrixCalc.InPerspectiveMode);
            }


            if (kb.HasBeenPressed(Keys.O, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to 90");
                gl3dcontroller.Pan(new Vector2(90, 0), 3);
            }
            if (kb.HasBeenPressed(Keys.P, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                System.Diagnostics.Debug.WriteLine("Order to -180");
                gl3dcontroller.Pan(new Vector2(90, 180), 3);
            }

            //System.Diagnostics.Debug.WriteLine("kb check");

        }

    }

}


