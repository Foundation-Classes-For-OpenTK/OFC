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
using System.Collections.Generic;
using GLOFC.GL4.Controls;
using System.Linq;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.ShapeFactory;
using GLOFC.GL4.Textures;
using GLOFC.Utils;

namespace TestOpenTk
{
    public partial class TestControlsPanels: System.Windows.Forms.Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private System.Windows.Forms.Timer systemtimer = new System.Windows.Forms.Timer();

        public TestControlsPanels()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        GLControlDisplay displaycontrol;

        /// ////////////////////////////////////////////////////////////////////////////////////////////////////


        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
            GLStatics.VerifyAllDeallocated();
        }

        public class GLFixedShader : GLShaderPipeline
        {
            public GLFixedShader(Color c, Action<IGLProgramShader, GLMatrixCalc> action = null) : base(action)
            {
                AddVertexFragment(new GLPLVertexShaderWorldCoord(), new GLPLFragmentShaderFixedColor(c));
            }
        }

        class MatrixCalcSpecial : GLMatrixCalc
        {
            public MatrixCalcSpecial()
            {
                ScreenCoordMax = new Size(2000, 1000);
                //ScreenCoordClipSpaceSize = new SizeF(1.8f, 1.8f);
                //ScreenCoordClipSpaceOffset = new PointF(-0.9f, 0.9f);
                //ScreenCoordClipSpaceSize = new SizeF(1f, 1f);
                //ScreenCoordClipSpaceOffset = new PointF(-0.5f, 0.5f);
            }

            public override void ResizeViewPort(object sender, Size newsize)            // override to change view port to a custom one
            {
                if (!(sender is Controller3D))         // ignore from 3dcontroller as it also sends it, but it will be reporting the size of the display window
                {
                    System.Diagnostics.Debug.WriteLine("Set GL Screensize {0}", newsize);
                    ScreenSize = newsize;
                    ScreenCoordMax = newsize;
                    int margin = 0;
                    ViewPort = new Rectangle(new Point(margin, margin), new Size(newsize.Width - margin * 2, newsize.Height - margin * 2));
                    SetViewPort();
                }
            }


        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Closed += ShaderTest_Closed;

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

            GLRenderState rl = GLRenderState.Lines(1);

            {
                items.Add(new GLFixedShader(System.Drawing.Color.Yellow), "LINEYELLOW");
                rObjects.Add(items.Shader("LINEYELLOW"),
                GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rl, displaylines));
            }

            float h = 0;
            if ( h != -1)
            {
                items.Add(new GLColorShaderWorld(), "COS-1L");

                int dist = 1000;
                Color cr = Color.FromArgb(100, Color.White);
                rObjects.Add(items.Shader("COS-1L"),    // horizontal
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(left, h, back), new Vector3(dist, 0, 0), (back - front) / dist + 1),
                                                        new Color4[] { cr })
                                   );

                rObjects.Add(items.Shader("COS-1L"),
                             GLRenderableItem.CreateVector4Color4(items, PrimitiveType.Lines, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(right, h, front), new Vector3(0, 0, dist), (right - left) / dist + 1),
                                                        new Color4[] { cr })
                                   );

            }


            {
                items.Add(new GLTexturedShaderObjectTranslation(), "TEX");
                items.Add(new GLTexture2D(Properties.Resources.dotted2, SizedInternalFormat.Rgba8), "dotted2");

                GLRenderState rt = GLRenderState.Tri();

                rObjects.Add(items.Shader("TEX"),
                    GLRenderableItem.CreateVector4Vector2(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, rt,
                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(2000f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(-2, 0, 0))
                            ));
            }

            {
                items.Add(new GLFixedColorShaderWorld(Color.FromArgb(150, Color.Green)), "FCS1");
                items.Add(new GLFixedColorShaderWorld(Color.FromArgb(80, Color.Red)), "FCS2");

                GLRenderState rq = GLRenderState.Quads();

                rObjects.Add(items.Shader("FCS1"),
                    GLRenderableItem.CreateVector4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Quads, rq,
                                                GLShapeObjectFactory.CreateQuad(1000, pos: new Vector3(4000, 500, 0))));
                rObjects.Add(items.Shader("FCS2"),
                    GLRenderableItem.CreateVector4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Quads, rq,
                                                GLShapeObjectFactory.CreateQuad(1000, pos: new Vector3(4000, 1000, 0))));
            }

            MatrixCalcSpecial mc = new MatrixCalcSpecial();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            displaycontrol = new GLControlDisplay(items, glwfc,mc);       // hook form to the window - its the master, it takes its size fro mc.ScreenCoordMax
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Name = "displaycontrol";

            GLForm pform = new GLForm("Form1", "GL Control demonstration", new Rectangle(0, 0, 1000, 850));
            // pform.BackColor = Color.FromArgb(200, Color.Red);
            //pform.BackColorGradientDir = 90;
            //pform.BackColorGradientAlt = Color.FromArgb(200, Color.Yellow);

            displaycontrol.Add(pform);

            if (true)
            {
                GLPanel p1 = new GLPanel("P3", new Size(150, 150), DockingType.TopLeft, 0);
                p1.SetMarginBorderWidth(new Margin(1), 1, Color.Black, new GLOFC.GL4.Controls.Padding(1));
                pform.Add(p1);
            }

            if (true)
            {
                GLTableLayoutPanel ptable = new GLTableLayoutPanel("tablelayout", new Rectangle(5, 200, 190, 190));
                ptable.Margin = new Margin(2);
                ptable.Padding = new GLOFC.GL4.Controls.Padding(2);
                ptable.BorderWidth = 1;
                ptable.Rows = new List<GLTableLayoutPanel.Style> { new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Weight, 50), new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Weight, 50) };
                ptable.Columns = new List<GLTableLayoutPanel.Style> { new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Weight, 50), new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Weight, 50) };
                pform.Add(ptable);
                GLImage pti1 = new GLImage("PTI1", new Rectangle(0, 0, 24, 24), Properties.Resources.dotted);
                pti1.Column = 0; pti1.Row = 0; pti1.Dock = DockingType.Fill;
                ptable.Add(pti1);
                GLImage pti2 = new GLImage("PTI2", new Rectangle(100, 0, 24, 24), Properties.Resources.dotted2);
                pti2.Column = 1; pti1.Row = 0;
                ptable.Add(pti2);
                GLImage pti3 = new GLImage("PTI3", new Rectangle(100, 0, 48, 48), Properties.Resources.ImportSphere);
                pti3.Column = 0; pti3.Row = 1; pti3.Dock = DockingType.LeftCenter; pti3.ImageStretch = true;
                ptable.Add(pti3);
                GLImage pti4 = new GLImage("PTI4", new Rectangle(100, 0, 64, 64), Properties.Resources.Logo8bpp);
                pti4.Column = 1; pti4.Row = 1; pti4.Dock = DockingType.Center;
                ptable.Add(pti4);
            }

            if (true)
            {
                GLFlowLayoutPanel pflow1 = new GLFlowLayoutPanel("flowlayout", new Rectangle(5, 400, 190, 190));
                pflow1.SetMarginBorderWidth(new Margin(2), 1, Color.Black, new GLOFC.GL4.Controls.Padding(2));
                pflow1.FlowPadding = new GLOFC.GL4.Controls.Padding(10, 5, 0, 0);
                pform.Add(pflow1);
                GLImage pti1 = new GLImage("PTI1", new Rectangle(0, 0, 24, 24), Properties.Resources.dotted);
                pflow1.Add(pti1);
                GLImage pti2 = new GLImage("PTI2", new Rectangle(100, 0, 32, 32), Properties.Resources.dotted2);
                pflow1.Add(pti2);
                GLImage pti3 = new GLImage("PTI3", new Rectangle(100, 0, 48, 48), Properties.Resources.ImportSphere);
                pflow1.Add(pti3);
                GLImage pti4 = new GLImage("PTI4", new Rectangle(100, 0, 64, 64), Properties.Resources.Logo8bpp);
                pflow1.Add(pti4);
            }

            if (true)
            {
                GLScrollPanel sp1 = new GLScrollPanel("VSP1", new Rectangle(5, 600, 200, 200));
                sp1.BorderWidth = 1;
                sp1.BackColor = Color.Yellow;
               // sp1.SetMarginBorderWidth(new Margin(2), 1, Color.Black, new GLOFC.GL4.Controls.Padding(2));
                pform.Add(sp1);
                GLImage sp1i1 = new GLImage("SP1I1", new Rectangle(0, 0, 190, 100), Properties.Resources.dotted);
                sp1.Add(sp1i1);
                GLImage sp1i2 = new GLImage("SP1I22", new Rectangle(10, 150, 100, 100), Properties.Resources.dotted);
                sp1.Add(sp1i2);
                GLImage sp1i3 = new GLImage("SP1I23", new Rectangle(100, 100, 200, 200), Properties.Resources.dotted2);
                sp1.Add(sp1i3);
                sp1.VertScrollPos = 0;
                sp1.HorzScrollPos = 0;

                GLScrollBar sb1 = new GLScrollBar("SB1", new Rectangle(220, 600, 20, 100), 0, 200);
                sb1.Scroll += (c, s1e) => sp1.VertScrollPos = s1e.NewValue;
                pform.Add(sb1);

                GLScrollBar sb2 = new GLScrollBar("SB2", new Rectangle(260, 600, 100, 20), 0, 200);
                sb2.HorizontalScroll = true;
                sb2.Scroll += (c, s1e) => sp1.HorzScrollPos = s1e.NewValue;
                pform.Add(sb2);
            }

            int col2 = 200;
            if (true)
            {
                GLScrollPanelScrollBar spb1 = new GLScrollPanelScrollBar("CSPan", new Rectangle(col2, 5, 190, 190));
                spb1.ScrollBackColor = Color.Yellow;
                spb1.SetMarginBorderWidth(new Margin(2), 1, Color.Black, new GLOFC.GL4.Controls.Padding(2));
                pform.Add(spb1);
                GLImage spb1i1 = new GLImage("SPB1I1", new Rectangle(10, 10, 100, 100), Properties.Resources.dotted);
                spb1.Add(spb1i1);
                GLButton but = new GLButton("SPB1BUT1", new Rectangle(40, 120, 40, 20), "But1");
                but.Click += (en, eb) => { System.Diagnostics.Debug.WriteLine("Click on SP Button"); };
                spb1.Add(but);
                GLImage spb1i2 = new GLImage("SPB1I2", new Rectangle(10, 150, 100, 100), Properties.Resources.dotted);
                spb1.Add(spb1i2);
                spb1.EnableHorzScrolling = false;
            }

            {
                GLButton but = new GLButton("ButExample", new Rectangle(40, 160, 40, 20), "But1");
                but.Click += (en, eb) => { System.Diagnostics.Debug.WriteLine("Click on SP Button"); };
                pform.Add(but);
            }
            int col3 = 400;

            if (true)
            {
                GLScrollPanelScrollBar spb1 = new GLScrollPanelScrollBar("CSPan2", new Rectangle(col3, 5, 190, 190));
                spb1.ScrollBackColor = Color.Blue;
                spb1.SetMarginBorderWidth(new Margin(2), 1, Color.Black, new GLOFC.GL4.Controls.Padding(2));
                pform.Add(spb1);
                GLImage spb1i1 = new GLImage("SPB1I1", new Rectangle(10, 10, 100, 100), Properties.Resources.dotted);
                spb1.Add(spb1i1);
                GLImage spb1i2 = new GLImage("SPB1I2", new Rectangle(10, 120, 100, 100), Properties.Resources.dotted);
                spb1.Add(spb1i2);
                GLImage spb1i3 = new GLImage("SPB1I3", new Rectangle(150, 50, 100, 100), Properties.Resources.dotted2);
                spb1.Add(spb1i3);

            }

            if (true)
            {
                //GLGroupBox p3 = new GLGroupBox("GB1", "Group Box Very Long Title", new Rectangle(col2,200,190,190));
                GLGroupBox p3 = new GLGroupBox("GB1", "Group Box", new Rectangle(col2,200,190,190));
                p3.TextAlign = ContentAlignment.MiddleRight;
                p3.AutoSize = true;
                GLImage spb1i1 = new GLImage("SPB1I1", new Rectangle(10, 10, 100, 100), Properties.Resources.dotted);
                p3.Add(spb1i1);
                pform.Add(p3);
            }

            if (true)
            {
                GLTabControl tc = new GLTabControl("Tabc", new Rectangle(col2, 400, 200, 190));
                tc.TabNotSelectedColor = Color.Yellow;
                tc.TabSelectedColor = Color.Red;
                tc.TabStyle = new TabStyleRoundedEdge();
                tc.TabStyle = new TabStyleSquare();
                tc.TabStyle = new TabStyleAngled();
                tc.Font = new Font("Ms Sans Serif", 9);

                GLTabPage tabp1 = new GLTabPage("tabp1", "TAB 1", Color.Blue);
                tc.Add(tabp1);

                GLButton tabp1b1 = new GLButton("B1", new Rectangle(5, 5, 80, 40), "Button 1");
                tabp1.Add(tabp1b1);
                tabp1b1.Click += (c, ev) => { System.Diagnostics.Debug.WriteLine("On click for " + c.Name + " " + ev.Button); };
                tabp1b1.ToolTipText = "Button 1";

                GLTabPage tabp2 = new GLTabPage("tabp2", "TAB Page 2", Color.Yellow);
                GLButton tabp2b1 = new GLButton("B2-2", new Rectangle(5, 25, 80, 40), "Button 2-2");
                tabp2.Add(tabp2b1);
                tc.Add(tabp2);

                GLTabPage tabp3 = new GLTabPage("tabp3", "TAB Page 3", Color.Green);
                tc.Add(tabp3);
                GLTabPage tabp4 = new GLTabPage("tabp4", "TAB Page 4", Color.Magenta);
                tc.Add(tabp4);

                pform.Add(tc);
                tc.SelectedTab = 0;
            }


            if (true)
            {
                GLPanel pouter = new GLPanel("outerflow", new Rectangle(col3, 200, 1300, 30));      // make it very wide, so the child has all the width it wants to flow into
                pouter.SetMarginBorderWidth(new Margin(5), 1, Color.Blue, new Padding(5));
                pouter.AutoSize = true;

                GLFlowLayoutPanel pflow2;
                pflow2 = new GLFlowLayoutPanel("Flowlayout2", new Rectangle(0,0,10,10));
                pflow2.AutoSize = true;
                pflow2.Margin = new Margin(2);
                pflow2.Padding = new GLOFC.GL4.Controls.Padding(2);
                pflow2.BorderWidth = 1;
                pflow2.FlowPadding = new GLOFC.GL4.Controls.Padding(10, 5, 0, 5);

                GLImage pti1 = new GLImage("PTI1", new Rectangle(0, 0, 24, 24), Properties.Resources.dotted2);
                pflow2.Add(pti1);
                GLImage pti2 = new GLImage("PTI2", new Rectangle(0, 0, 32, 32), Properties.Resources.dotted2);
                pflow2.Add(pti2);
                GLImage pti3 = new GLImage("PTI3", new Rectangle(0, 0, 48, 48), Properties.Resources.ImportSphere);
                pflow2.Add(pti3);

                for (int i = 0; i < 5; i++)
                {
                    GLImage pti4 = new GLImage("PTI00" + i, new Rectangle(0, 0, 64, 64), Properties.Resources.Logo8bpp);
                    pflow2.Add(pti4);
                }

                pouter.Add(pflow2);
                pform.Add(pouter);  
            }

            {
                GLToolTip tip = new GLToolTip("ToolTip");
                displaycontrol.Add(tip);
            }

            gl3dcontroller = new Controller3D();
            gl3dcontroller.ZoomDistance = 5000F;
            gl3dcontroller.YHoldMovement = true;
            gl3dcontroller.PaintObjects = Controller3dDraw;

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms * 10.0f;
            };

            gl3dcontroller.MatrixCalc.InPerspectiveMode = true;

            if ( displaycontrol != null )
            {
                gl3dcontroller.Start(mc , displaycontrol, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F);     // HOOK the 3dcontroller to the form so it gets Form events

                displaycontrol.Paint += (o,ts) =>        // subscribing after start means we paint over the scene, letting transparency work
                {
                    //System.Diagnostics.Debug.WriteLine(ts + " Render");
                    displaycontrol.Render(glwfc.RenderState,ts);       // we use the same matrix calc as done in controller 3d draw
                };

            }
            else
                gl3dcontroller.Start(glwfc, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F);     // HOOK the 3dcontroller to the form so it gets Form events

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        private void Controller3dDraw(Controller3D mc, ulong unused)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).SetFull(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.PosCamera.ZoomFactor;
        }

        private void SystemTick(object sender, EventArgs e)
        {
            PolledTimer.ProcessTimers();
            displaycontrol.Animate(glwfc.ElapsedTimems);
            if (displaycontrol != null && displaycontrol.RequestRender)
                glwfc.Invalidate();
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true);
        }

    }
}


