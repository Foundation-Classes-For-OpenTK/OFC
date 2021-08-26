using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using GLOFC;
using GLOFC.Controller;
using GLOFC.GL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using GLOFC.GL4.Controls;
using System.Linq;

// Demonstrate the volumetric calculations needed to compute a plane facing the user inside a bounding box done inside a geo shader
// this one add on tex coord calculation and using a single tight quad shows its working

namespace TestOpenTk
{
    public partial class TestControlsPanels: Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsPanels()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        GLControlDisplay displaycontrol;

        /// ////////////////////////////////////////////////////////////////////////////////////////////////////


        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
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
                items.Add(new GLColorShaderWithWorldCoord(), "COS-1L");

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
                items.Add(new GLTexturedShaderWithObjectTranslation(), "TEX");
                items.Add(new GLTexture2D(Properties.Resources.dotted2), "dotted2");

                GLRenderState rt = GLRenderState.Tri();

                rObjects.Add(items.Shader("TEX"),
                    GLRenderableItem.CreateVector4Vector2(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, rt,
                            GLCubeObjectFactory.CreateSolidCubeFromTriangles(2000f), GLCubeObjectFactory.CreateCubeTexTriangles(),
                            new GLRenderDataTranslationRotationTexture(items.Tex("dotted2"), new Vector3(-2, 0, 0))
                            ));
            }

            {
                items.Add(new GLFixedColorShaderWithWorldCoord(Color.FromArgb(150, Color.Green)), "FCS1");
                items.Add(new GLFixedColorShaderWithWorldCoord(Color.FromArgb(80, Color.Red)), "FCS2");

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

            if (true)
            {
                bool testform1 = true;
                bool testpanel = true;
                bool testgroupbox = true;
                bool testtable = true;
                bool testflow = true;
                bool testscrollbar = true;
                bool testvsp1 = true;
                bool testvsp2 = true;
                bool testtabcontrol = true;
                bool testflowlayout = true;

             //  testpanel = testgroupbox = testtable = testflow = testscrollbar = testvsp1 = testvsp2 = testtabcontrol = false;

                mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

                displaycontrol = new GLControlDisplay(items, glwfc,mc);       // hook form to the window - its the master, it takes its size fro mc.ScreenCoordMax
                displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
                displaycontrol.Name = "displaycontrol";
                displaycontrol.SuspendLayout();

                //                testform1 = false;
                if (testform1)
                {
                    GLForm pform = new GLForm("Form1", "GL Control demonstration", new Rectangle(0, 0, 500, 600));
                    pform.BackColor = Color.FromArgb(200, Color.Red);
                    pform.SuspendLayout();
                    pform.BackColorGradientDir = 90;
                    pform.BackColorGradientAlt = Color.FromArgb(200, Color.Yellow);
                 //   pform.AlternatePos = new RectangleF(100, 10, 1000, 900);
                 //   pform.AlternatePos = new RectangleF(100, 10, 250, 300);

                    //pform.Animators.Add(new AnimateMove(glwfc.ElapsedTimems + 2000, glwfc.ElapsedTimems + 10000, new Point(100, 100)));
                    //   pform.Animators.Add(new AnimateAlternatePos(glwfc.ElapsedTimems + 2000, glwfc.ElapsedTimems + 10000, new RectangleF(100, 100,500,450)));

                    displaycontrol.Add(pform);

                    if (testpanel)
                    {
                        GLPanel p1 = new GLPanel("P3", new Size(150, 150), DockingType.TopLeft, 0);
                        p1.BackColor = Color.Purple;
                        p1.SetMarginBorderWidth(new GLOFC.GL4.Controls.Margin(10), 5, Color.Green, new GLOFC.GL4.Controls.Padding(5));
                        //p1.Animators.Add(new AnimateMove(glwfc.ElapsedTimems + 2000, glwfc.ElapsedTimems + 5000, new Point(500, 500)));
                        //p1.Animators.Add(new AnimateSize(glwfc.ElapsedTimems + 3000, glwfc.ElapsedTimems + 7000, new Size(300, 300)));
                        pform.Add(p1);
                    }

                    if (testtable)
                    {
                        GLTableLayoutPanel ptable = new GLTableLayoutPanel("tablelayout", new Rectangle(5, 200, 190,190));
                        ptable.SuspendLayout();
                        ptable.SetMarginBorderWidth(new Margin(2), 1, Color.Wheat, new GLOFC.GL4.Controls.Padding(2));
                        ptable.Rows = new List<GLTableLayoutPanel.Style> { new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Relative, 50), new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Relative, 50) };
                        ptable.Columns = new List<GLTableLayoutPanel.Style> { new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Relative, 50), new GLTableLayoutPanel.Style(GLTableLayoutPanel.Style.SizeTypeEnum.Relative, 50) };
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
                        ptable.ResumeLayout();
                    }

                    if (testflow)
                    {
                        GLFlowLayoutPanel pflow1 = new GLFlowLayoutPanel("flowlayout", new Rectangle(5, 400, 190,190));
                        pflow1.SuspendLayout();
                        pflow1.SetMarginBorderWidth(new Margin(2), 1, Color.Wheat, new GLOFC.GL4.Controls.Padding(2));
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
                        pflow1.ResumeLayout();
                    }

                    if (testvsp1 && false)
                    {
                        GLPanel p1 = new GLPanel("P3", new Rectangle(200,600,10,190));
                        pform.Add(p1);

                        GLVerticalScrollPanel sp1 = new GLVerticalScrollPanel("VSP1", new Rectangle(5, 600, 190,190));
                        pform.Add(sp1);
                        GLImage sp1i1 = new GLImage("SP1I1", new Rectangle(10, 10, 100, 100), Properties.Resources.dotted);
                        sp1.Add(sp1i1);
                        GLImage sp1i2 = new GLImage("SP1I22", new Rectangle(10, 120, 100, 100), Properties.Resources.dotted);
                        sp1.Add(sp1i2);
                        sp1.ScrollPos = 100;
                    }

                    int col2 = 200;
                    if (testvsp2)
                    {
                        GLVerticalScrollPanelScrollBar spb1 = new GLVerticalScrollPanelScrollBar("CSPan", new Rectangle(col2, 5, 190, 190));
                        pform.Add(spb1);
                        GLImage spb1i1 = new GLImage("SPB1I1", new Rectangle(10, 10, 100, 100), Properties.Resources.dotted);
                        spb1.Add(spb1i1);
                        GLImage spb1i2 = new GLImage("SPB1I2", new Rectangle(10, 120, 100, 100), Properties.Resources.dotted);
                        spb1.Add(spb1i2);
                    }

                    if (testgroupbox)
                    {
                        GLGroupBox p3 = new GLGroupBox("GB1", "Group Box", DockingType.Right, 0.15f);
                        p3.BorderColor = Color.White;
                        p3.ForeColor = Color.Yellow;
                        pform.Add(p3);
                    }

                    if (testtabcontrol)
                    {
                        GLTabControl tc = new GLTabControl("Tabc", new Rectangle(col2, 200, 200, 190));
                        tc.TabStyle = new TabStyleRoundedEdge();
                        tc.TabStyle = new TabStyleSquare();
                        tc.TabStyle = new TabStyleAngled();
                        tc.Font = new Font("Ms Sans Serif",9);

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

                    if (testscrollbar && false)
                    {
                        GLPanel psb = new GLPanel("panelsb", new Rectangle(col2, 400, 190,190));
                        pform.Add(psb);
                        
                        GLScrollBar sb1 = new GLScrollBar("SB1", new Rectangle(0, 0, 20, 100), 0, 100);
                        psb.Add(sb1);

                        GLScrollBar sb2 = new GLScrollBar("SB2", new Rectangle(40, 10, 150, 20), 0, 100);
                        sb2.HorizontalScroll = true;
                        psb.Add(sb2);
                    }

                    if (testflowlayout && false)
                    {
                        GLFlowLayoutPanel pflow2;
                        pflow2 = new GLFlowLayoutPanel("Flowlayout2", new Rectangle(col2,600,300,300));
                        pflow2.AutoSize = true;
                        pflow2.SuspendLayout();
                        pflow2.SetMarginBorderWidth(new Margin(2), 1, Color.Wheat, new GLOFC.GL4.Controls.Padding(2));
                        pflow2.FlowPadding = new GLOFC.GL4.Controls.Padding(10, 5, 0, 5);
                        pform.Add(pflow2);

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
                    }

                    {
                        GLToolTip tip = new GLToolTip("ToolTip");
                        displaycontrol.Add(tip);
                    }

                    pform.ResumeLayout();
                }


                displaycontrol.ResumeLayout();



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
                    System.Diagnostics.Debug.WriteLine(ts + " Render");
                    displaycontrol.Render(glwfc.RenderState,ts);       // we use the same matrix calc as done in controller 3d draw
                };

            }
            else
                gl3dcontroller.Start(glwfc, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F);     // HOOK the 3dcontroller to the form so it gets Form events

        }

        private void Controller3dDraw(Controller3D mc, ulong unused)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).SetText(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.PosCamera.ZoomFactor;
        }

        private void SystemTick(object sender, EventArgs e)
        {
            GLOFC.Timers.Timer.ProcessTimers();
            displaycontrol.Animate(glwfc.ElapsedTimems);
            if (displaycontrol != null && displaycontrol.RequestRender)
                glwfc.Invalidate();
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true);
        }

    }
}


