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
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestOpenTk
{
    public partial class ShaderTestPointSprites : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestPointSprites()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        public class GLPointSprite : GLShaderStandard
        {
            string vert =
@"
#version 450 core

#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;     // has w=1
layout (location = 1) in vec4 color;       
out vec4 vs_color;
out float calc_size;

void main(void)
{
    vec4 pn = vec4(position.x,position.y,position.z,0);
    float d = distance(mc.EyePosition,pn);
    float sf = 120-d;

    calc_size = gl_PointSize = clamp(sf,1.0,120.0);
    gl_Position = mc.ProjectionModelMatrix * position;        // order important
    vs_color = color;
}
";
            string frag =
@"
#version 450 core

in vec4 vs_color;
layout (binding = 4 ) uniform sampler2D texin;
out vec4 color;
in float calc_size;

void main(void)
{
    if ( calc_size < 2 )
        color = vs_color * 0.5;
    else
    {
        vec4 texcol =texture(texin, gl_PointCoord); 
        float l = texcol.x*texcol.x+texcol.y*texcol.y+texcol.z*texcol.z;
    
        if ( l< 0.1 )
            discard;
        else
            color = texcol * vs_color;
    }
}
";
            public GLPointSprite() : base()
            {
                CompileLink(vert,frag:frag);
            }

           
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            gl3dcontroller.ZoomDistance = 20F;
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, 0), new Vector3(110f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 50.0f;
            };

            //items.Add("lensflarewhite", new GLTexture2D(Properties.Resources.lensflare_white64));
            items.Add(new GLTexture2D(Properties.Resources.StarFlare2, SizedInternalFormat.Rgba8), "lensflare");

            items.Add(new GLColorShaderWithWorldCoord(), "COS");

            #region coloured lines

            {
                GLRenderState rl = GLRenderState.Lines(1);
                rObjects.Add(items.Shader("COS"),    // horizontal
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(-100, 0, 100), new Vector3(10, 0, 0), 21),
                                                        new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );


                rObjects.Add(items.Shader("COS"),    // vertical
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-100, 0, -100), new Vector3(100, 0, -100), new Vector3(0, 0, 10), 21),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );
            }

            var p = GLPointsFactory.RandomStars4(100, -100, 100, 100, -100, 100, -100);
            //p = new Vector4[10];
            //for( int i = 0; i < 10; i++)
            //{
            //    p[i] = new Vector4(i, 6.8f, 0,1);
            //}

            items.Add(new GLPointSprite(), "PS1");

            GLRenderState rp = GLRenderState.PointSprites();     // by program

            GLRenderDataTexture rt = new GLRenderDataTexture(items.Tex("lensflare"),4);

            rObjects.Add(items.Shader("PS1"),
                         GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Points, rp,
                               p, new Color4[] { Color.Red, Color.Yellow, Color.Green },rt));

            #endregion

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

        }

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(Controller3D mc, ulong unused)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).Set(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            rObjects.Render(glwfc.RenderState,gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " dir " + gl3dcontroller.PosCamera.CameraDirection + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;
        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, OtherKeys);
            gl3dcontroller.Redraw();
        }

        private void OtherKeys( GLOFC.Controller.KeyboardMonitor kb )
        {
            if (kb.HasBeenPressed(Keys.F1, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(0, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F2, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(4, 0, 0), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F3, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(10, 0, -10), 1, 2);
            }

            if (kb.HasBeenPressed(Keys.F4, GLOFC.Controller.KeyboardMonitor.ShiftState.None))
            {
                gl3dcontroller.PanZoomTo(new Vector3(50, 0, 50), 1, 2);
            }

        }

    }



}


