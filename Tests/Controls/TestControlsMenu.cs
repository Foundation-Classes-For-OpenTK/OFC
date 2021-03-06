﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OFC;
using OFC.Controller;
using OFC.GL4;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using OFC.GL4.Controls;
using System.Linq;

// Demonstrate the volumetric calculations needed to compute a plane facing the user inside a bounding box done inside a geo shader
// this one add on tex coord calculation and using a single tight quad shows its working

namespace TestOpenTk
{
    public partial class TestControlsMenu : Form
    {
        private OFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsMenu()
        {
            InitializeComponent();

            glwfc = new OFC.WinForm.GLWinFormControl(glControlContainer);

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

            GLRenderControl rl = GLRenderControl.Lines(1);

            {
                items.Add(new GLFixedShader(System.Drawing.Color.Yellow), "LINEYELLOW");
                rObjects.Add(items.Shader("LINEYELLOW"),
                GLRenderableItem.CreateVector4(items, rl, displaylines));
            }

            float h = 0;
            if ( h != -1)
            {
                items.Add(new GLColorShaderWithWorldCoord(), "COS-1L");

                int dist = 1000;
                Color cr = Color.FromArgb(100, Color.White);
                rObjects.Add(items.Shader("COS-1L"),    // horizontal
                             GLRenderableItem.CreateVector4Color4(items, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(left, h, back), new Vector3(dist, 0, 0), (back - front) / dist + 1),
                                                        new Color4[] { cr })
                                   );

                rObjects.Add(items.Shader("COS-1L"),
                             GLRenderableItem.CreateVector4Color4(items, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(left, h, front), new Vector3(right, h, front), new Vector3(0, 0, dist), (right - left) / dist + 1),
                                                        new Color4[] { cr })
                                   );

            }

            MatrixCalcSpecial mc = new MatrixCalcSpecial();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            if (true)
            {
                bool testform1 = true;

                mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

                displaycontrol = new GLControlDisplay(items, glwfc,mc);       // hook form to the window - its the master, it takes its size fro mc.ScreenCoordMax
                displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
                displaycontrol.Name = "displaycontrol";
                displaycontrol.SuspendLayout();

                if (testform1)
                {
                    GLForm pform = new GLForm("Form1", "GL Menu demonstration", new Rectangle(10, 10, 600, 200));
                    pform.BackColor = Color.FromArgb(200, Color.Red);
                    pform.SuspendLayout();
                    pform.BackColorGradientDir = 90;
                    pform.BackColorGradientAlt = Color.FromArgb(200, Color.Yellow);
                    displaycontrol.Add(pform);

                    if (true)
                    {
                        GLMenuStrip menubar = new GLMenuStrip("Menubar", new Rectangle(0, 0, 500, 24));
                        menubar.Font = new Font("Euro Caps", 12);
                        menubar.Dock = DockingType.Top;

                        GLMenuItem l1 = new GLMenuItem("MI-0A", "MenuA");
                        menubar.Add(l1);

                        GLMenuItem l1a = new GLMenuItem("A-1", "MenuA-1");
                        GLMenuItem l1b = new GLMenuItem("A-2", "MenuA-2");
                        l1b.CheckOnClick = true;
                        l1b.Checked = true;
                        GLMenuItem l1c = new GLMenuItem("A-3", "MenuA-3") { Image = Properties.Resources.GoToHomeSystem };
                        l1c.CheckOnClick = true;
                        l1.SubMenuItems = new List<GLBaseControl>() { l1a, l1b, l1c };

                        GLMenuItem l1a1 = new GLMenuItem("A-1-1", "MenuA-1-1");
                        GLMenuItem l1a2 = new GLMenuItem("A-1-2", "MenuA-1-2");

                        GLMenuItem l1a21 = new GLMenuItem("A-1-2-1", "MenuA-1-2-1");
                        GLMenuItem l1a22 = new GLMenuItem("A-1-2-2", "MenuA-1-2-2");
                        l1a2.SubMenuItems = new List<GLBaseControl>() { l1a21, l1a22 };

                        GLCheckBox l1a3 = new GLCheckBox("A-1-3", new Rectangle(0, 0, 0, 0), "CheckBox A-1-3");
                        l1a3.CheckOnClick = true;
                        l1a3.CheckChanged += (bc) => { menubar.CloseMenus(); };     // need to associate check changed with closing menus - optional

                        GLComboBox l1a4 = new GLComboBox("A-1-4", new Rectangle(0, 0, 0, 0), new List<string>() { "one", "two", "three" });
                        l1a4.SelectedIndexChanged += (c) => { menubar.CloseMenus(); };
                        l1a4.DisableChangeKeys = true;
                        l1a.SubMenuItems = new List<GLBaseControl>() { l1a1, l1a2 ,l1a3, l1a4 };

                        GLMenuItem l2 = new GLMenuItem("MI-0B", "MenuB");
                        menubar.Add(l2);

                        GLMenuItem l2a = new GLMenuItem("B-1", "MenuB-1");
                        l2a.Click += (s) => { System.Diagnostics.Debug.WriteLine("Clicked Menu " + s.Name); };
                        GLMenuItem l2b = new GLMenuItem("B-2", "MenuB-2");
                        l2.SubMenuItems = new List<GLBaseControl>() { l2a, l2b };

                        GLMenuItem l3 = new GLMenuItem("MI-0C", "MenuC");
                        menubar.Add(l3);

                        pform.Add(menubar);
                    }

                    pform.ResumeLayout();
                }

                {
                    GLMenuStrip cm = new GLMenuStrip("CM1");
                    cm.FlowDirection = GLFlowLayoutPanel.ControlFlowDirection.Down;
                    GLMenuItem cm1 = new GLMenuItem("CM1A", "Menu-1");
                    GLMenuItem cm2 = new GLMenuItem("CM1B", "Menu-2");
                    cm2.CheckOnClick = true;
                    GLMenuItem cm3 = new GLMenuItem("CM1C", "Menu-3");

                    GLMenuItem l1a1 = new GLMenuItem("CM1C-1", "Menu-1-1");
                    l1a1.CheckOnClick = true;
                    GLMenuItem l1a2 = new GLMenuItem("CM1C-2", "MenuA-1-2");
                    GLCheckBox l1a3 = new GLCheckBox("CM1C-3", new Rectangle(0, 0, 0, 0), "CheckBox A-1-3");
                    l1a3.CheckOnClick = true;
                    cm3.SubMenuItems = new List<GLBaseControl>() { l1a1, l1a2, l1a3 };

                    cm.Add(cm1);
                    cm.Add(cm2);
                    cm.Add(cm3);

                    displaycontrol.MouseClick += (s, ev) =>
                    {
                        if (ev.Button == GLMouseEventArgs.MouseButtons.Right)
                        {
                            cm.OpenAsContextMenu(displaycontrol,ev.ScreenCoord);
                        }
                    };
                }


                displaycontrol.ResumeLayout();
            }

            gl3dcontroller = new Controller3D();
            gl3dcontroller.ZoomDistance = 5000F;
            gl3dcontroller.EliteMovement = true;
            gl3dcontroller.PaintObjects = Controller3dDraw;

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms * 10.0f;
            };

            gl3dcontroller.MatrixCalc.InPerspectiveMode = true;

            if ( displaycontrol != null )
            {
                gl3dcontroller.Start(mc , displaycontrol, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F);     // HOOK the 3dcontroller to the form so it gets Form events

                displaycontrol.Paint += (o) =>        // subscribing after start means we paint over the scene, letting transparency work
                {                                 
                    displaycontrol.Render(glwfc.RenderState);       // we use the same matrix calc as done in controller 3d draw
                };

            }
            else
                gl3dcontroller.Start(glwfc, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F);     // HOOK the 3dcontroller to the form so it gets Form events

        }

        private void ConfDialog()
        {
            GLFormConfigurable c1 = new GLFormConfigurable();
            c1.Add(new GLFormConfigurable.Entry("Lab1", typeof(GLLabel), "Label 1 ", new Point(10, 10), new Size(200, 24), "TT"));
            c1.Add(new GLFormConfigurable.Entry("But1", typeof(GLButton), "But 1", new Point(10, 40), new Size(200, 24), "TT"));
            c1.Add(new GLFormConfigurable.Entry("Com1", "two", new Point(10, 70), new Size(200, 24), "TT", new List<string>() { "one", "two", "three" }));
            c1.Add(new GLFormConfigurable.Entry("Textb", typeof(GLTextBox), "text box", new Point(10, 100), new Size(200, 24), "TT"));
            c1.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(160, 300), new Size(100, 24), "TT"));
            // c1.Size = new Size(400, 400);
            c1.Init(new Point(200, 200), "Config Form Test");
            c1.Trigger += (cb, en, ctrlname, args) =>
            {
                if (ctrlname == "OK")
                    c1.Close();
            };
            displaycontrol.Add(c1);
        }

        private void MsgDialog()
        {
            GLMessageBox msg = new GLMessageBox( displaycontrol, MsgReturn, "text text\r\nwskwkkw\r\nsksksk\r\nskksks end", "Caption", GLMessageBox.MessageBoxButtons.OKCancel);
        }

        private void MsgReturn(GLMessageBox msg, OFC.GL4.Controls.DialogResult res)
        {
            System.Diagnostics.Debug.WriteLine("!!! Message box " + res);
        }


        private void Controller3dDraw(GLMatrixCalc mc, long time)   // call back by controller to do painting
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).SetFull(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.PosCamera.ZoomFactor;
        }

        private void SystemTick(object sender, EventArgs e)
        {
            OFC.Timers.Timer.ProcessTimers();
            if (displaycontrol != null && displaycontrol.RequestRender)
                glwfc.Invalidate();
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true, Otherkeys);
        }

        private void Otherkeys(KeyboardMonitor h)
        {
            if ( h.HasBeenPressed(Keys.F1))
            {
                displaycontrol.DumpTrees(0,null);
            }

        }
    }
}


