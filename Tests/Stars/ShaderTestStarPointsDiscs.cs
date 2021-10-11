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
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestOpenTk
{
    // demonstrates packed structure and sizing of points

    public partial class ShaderTestStarPointsDiscs : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestStarPointsDiscs()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public class GLStarPoints : GLShaderStandard
        {
            public static string StarColours =
    @"
    const ivec3 starcolours[] = ivec3[] (
        ivec3(144,166,255),
        ivec3(148,170,255)
);
";

            string vertpos =
            @"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

"  + StarColours + @"
layout (location = 0) in uvec2 positionpacked;

out flat ivec3 i_color;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

float rand1(float n)
{
return fract(sin(n) * 43758.5453123);
}

void main(void)
{
    uint xcoord = positionpacked.x & 0x1fffff;
    uint ycoord = positionpacked.y & 0x1fffff;
    float x = float(xcoord)/16.0-50000;
    float y = float(ycoord)/16.0-50000;
    uint zcoord = positionpacked.x >> 21;
    zcoord = zcoord | ( ((positionpacked.y >> 21) & 0x7ff) << 11);
    float z = float(zcoord)/16.0-50000;

    vec4 position = vec4( x, y, z, 1.0f);

	gl_Position = mc.ProjectionModelMatrix * position;        // order important

    float distance = 50-pow(distance(mc.EyePosition,vec4(x,y,z,0)),2)/20;

    gl_PointSize = clamp(distance,1.0,63.0);
    i_color = ivec3(128,0,0);
}
";


            string frag =

    @"
#version 450 core

in flat ivec3 i_color;
out vec4 color;

void main(void)
{
    color = vec4( float(i_color.x)/256.0, float(i_color.y)/256.0, float(i_color.z)/256.0,1);
}
";

            public GLStarPoints()      // give the number of images to blend over..
            {
                CompileLink(vertex: vertpos, frag: frag);
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.ZoomDistance = 100F;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, 0), new Vector3(110f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 20.0f;
            };

            {
                items.Add( new GLColorShaderWithWorldCoord(), "COS");
                GLRenderState rl = GLRenderState.Lines(1);

                rObjects.Add(items.Shader("COS"), GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                            GLShapeObjectFactory.CreateBox(400, 200, 40, new Vector3(0, 0, 0), new Vector3(0, 0, 0)),
                            new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green }));
            }

            {
                items.Add(new GLTexturedShaderWithObjectTranslation(), "TEX");

                using (var bmp = BitMapHelpers.DrawTextIntoAutoSizedBitmap("200,100", new Size(200, 100), new Font("Arial", 10.0f), System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.Yellow, Color.Blue))
                {
                    items.Add(new GLTexture2D(bmp, SizedInternalFormat.Rgba8), "200,100");
                }

                using (var bmp = BitMapHelpers.DrawTextIntoAutoSizedBitmap("-200,-100", new Size(200, 100), new Font("Arial", 10.0f), System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, Color.Yellow, Color.Blue))
                {
                    items.Add(new GLTexture2D(bmp, SizedInternalFormat.Rgba8), "-200,-100");
                }

                GLRenderState rq = GLRenderState.Quads();

                rObjects.Add(items.Shader("TEX"), GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                            GLShapeObjectFactory.CreateQuad(20.0f, 20.0f, new Vector3( -90f.Radians(), 0, 0)), GLShapeObjectFactory.TexQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("200,100"), new Vector3(200, 0, 100))));

                rObjects.Add(items.Shader("TEX"), GLRenderableItem.CreateVector4Vector2(items, PrimitiveType.Quads, rq,
                            GLShapeObjectFactory.CreateQuad(20.0f, 20.0f, new Vector3( -90f.Radians(), 0, 0)), GLShapeObjectFactory.TexQuad,
                            new GLRenderDataTranslationRotationTexture(items.Tex("-200,-100"), new Vector3(-200, 0, -100))));
            }

            {
                items.Add(new GLStarPoints(), "STARS");

                Vector3[] stars = GLPointsFactory.RandomStars(10000, -200, 200, -100, 100, 20, -20);

                GLRenderState rp = GLRenderState.PointsByProgram();

                rObjects.Add(items.Shader("STARS"), "Stars", GLRenderableItem.CreateVector3Packed2(items, PrimitiveType.Points, rp,
                                            stars, new Vector3(50000, 50000, 50000), 16));
            }


            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0

            Closed += ShaderTest_Closed;
        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong time)
        {
            float degrees = ((float)time / 5000.0f * 360.0f) % 360f;
            float degreesd2 = ((float)time / 10000.0f * 360.0f) % 360f;

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.Set(gl3dcontroller.MatrixCalc);

            System.Diagnostics.Debug.WriteLine("Draw eye " + gl3dcontroller.MatrixCalc.EyePosition + " to " + gl3dcontroller.PosCamera.Lookat);
            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);
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


