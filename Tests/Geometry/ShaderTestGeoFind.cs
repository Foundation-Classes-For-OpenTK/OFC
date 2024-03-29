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
using GLOFC.GL4.Shaders.Geo;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using GLOFC.GL4.ShapeFactory;

namespace TestOpenTk
{
    public partial class ShaderTestGeoFind : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestGeoFind()
        {
            InitializeComponent();
            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

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
                GLRenderState lines = GLRenderState.Lines();

                items.Add(new GLColorShaderWorld(),"COSW" );

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

            var vert = new GLPLVertexShaderModelCoordWorldAutoscale();
            var frag = new GLPLFragmentShaderFixedColor(Color.Yellow);
            var shader = new GLShaderPipeline(vert, frag);
            items.Add(shader,"TRI");

            var triangles = new Vector4[] { new Vector4(0, 0, 0, 1), new Vector4(10, 0, 0, 1), new Vector4(10, 0, 10, 1) ,
                                    new Vector4(-20, 0, 0, 1), new Vector4(-10, 0, 0, 1), new Vector4(-10, 0, 10, 1)
            };

            var worldpos = new Vector4[] { new Vector4(0, 0, 0, 0), new Vector4(0, 0, 12, 0) };

            GLRenderState rc = GLRenderState.Tri();

            rObjects.Add(items.Shader("TRI"), "scopen", GLRenderableItem.CreateVector4Vector4Buf2(items, PrimitiveType.Triangles, rc, triangles, worldpos, ic:2, seconddivisor:1));

            // demo shared find block, a problem in the past with the previous interface

            GLStorageBlock findblock = new GLStorageBlock(11);

            findshader1 = items.NewShaderPipeline("FS", new GLPLVertexShaderModelCoordWorldAutoscale(), null, null, new GLPLGeoShaderFindTriangles(findblock, 16), null, null, null);
            findrender1 = GLRenderableItem.CreateVector4Vector4Buf2(items, PrimitiveType.Triangles, GLRenderState.Tri(), triangles, worldpos, ic: 2, seconddivisor: 1);

            findshader2 = items.NewShaderPipeline("FS2", new GLPLVertexShaderModelCoordWorldAutoscale(), null, null, new GLPLGeoShaderFindTriangles(findblock, 16), null, null, null);
            findrender2 = GLRenderableItem.CreateVector4Vector4Buf2(items, PrimitiveType.Triangles, GLRenderState.Tri(), triangles, worldpos, ic: 2, seconddivisor: 1);

            Closed += ShaderTest_Closed;
        }

        void mousedown(Object sender, GLMouseEventArgs e)
        {
            //  GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            // mcub.Set(gl3dcontroller.MatrixCalc);

            {
                var geo = findshader1.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);

                geo.SetScreenCoords(e.WindowLocation, glwfc.Size);

                System.Diagnostics.Debug.WriteLine("Run find");
                findrender1.Execute(findshader1, glwfc.RenderState);
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
            {
                var geo = findshader2.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);

                geo.SetScreenCoords(e.WindowLocation, glwfc.Size);

                System.Diagnostics.Debug.WriteLine("Run find 2");
                findrender2.Execute(findshader1, glwfc.RenderState);
                System.Diagnostics.Debug.WriteLine("Finish find 2");

                var res = geo.GetResult();
                if (res != null)
                {
                    for (int i = 0; i < res.Length; i++)
                    {
                        System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
                    }
                }
            }


        }

        GLShaderPipeline findshader1;
        GLShaderPipeline findshader2;
        GLRenderableItem findrender1;
        GLRenderableItem findrender2;

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
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true);
        }

    }
}


