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

using OFC;
using OFC.Controller;
using OFC.GL4;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestOpenTk
{
    public partial class ShaderTestGeoFind : Form
    {
        private OFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestGeoFind()
        {
            InitializeComponent();
            glwfc = new OFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.ZoomDistance = 20F;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            gl3dcontroller.Start(glwfc, new Vector3(0, 0, 0), new Vector3(180f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) =>
            {
                return (float)ms / 50.0f;
            };

            glwfc.MouseDown += mousedown;

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0

            if (true)
            {
                GLRenderState lines = GLRenderState.Lines(1);

                items.Add(new GLColorShaderWithWorldCoord(),"COSW" );

                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines,lines,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(-100, 0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );


                rObjects.Add(items.Shader("COSW"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines,lines,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(100, 0, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green }));
            }

            var vert = new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation();
            var frag = new GLPLFragmentShaderFixedColor(Color.Yellow);
            var shader = new GLShaderPipeline(vert, frag);
            items.Add(shader,"TRI");

            var triangles = new Vector4[] { new Vector4(0, 0, 0, 1), new Vector4(10, 0, 0, 1), new Vector4(10, 0, 10, 1) ,
                                    new Vector4(-20, 0, 0, 1), new Vector4(-10, 0, 0, 1), new Vector4(-10, 0, 10, 1)
            };

            var worldpos = new Vector4[] { new Vector4(0, 0, 0, 0), new Vector4(0, 0, 12, 0) };

            GLRenderState rc = GLRenderState.Tri();

            rObjects.Add(items.Shader("TRI"), "scopen", GLRenderableItem.CreateVector4Vector4Buf2(items, PrimitiveType.Triangles, rc, triangles, worldpos, ic:2, seconddivisor:1));

            findshader = items.NewShaderPipeline("FS", new GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(), null, null, new GLPLGeoShaderFindTriangles(11, 16), null, null, null);
            findrender = GLRenderableItem.CreateVector4Vector4Buf2(items, PrimitiveType.Triangles, GLRenderState.Tri(), triangles, worldpos, ic:2, seconddivisor:1 );

            Closed += ShaderTest_Closed;
        }

        void mousedown(Object sender, GLMouseEventArgs e)
        {
         //  GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
           // mcub.Set(gl3dcontroller.MatrixCalc);

            var geo = findshader.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);

            geo.SetScreenCoords(e.WindowLocation, glwfc.Size);

            System.Diagnostics.Debug.WriteLine("Run find");
            findrender.Execute(findshader, glwfc.RenderState, discard:true);
            System.Diagnostics.Debug.WriteLine("Finish find");

            var res = geo.GetResult();
            if (res != null)
            {
                for (int i = 0; i < res.Length; i++)
                {
                    System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
                }
            }


        }

        GLShaderPipeline findshader;
        GLRenderableItem findrender;

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.Set(gl3dcontroller.MatrixCalc);
            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true);
        }

    }
}


