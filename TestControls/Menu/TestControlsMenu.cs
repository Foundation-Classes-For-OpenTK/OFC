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
using System.Windows.Forms;
using System.Collections.Generic;
using GLOFC.GL4.Controls;
using System.Linq;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.ShapeFactory;
using GLOFC.Utils;
using static GLOFC.GL4.Controls.GLBaseControl;

namespace TestOpenTk
{
    public partial class TestControlsMenu : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsMenu()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

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

            GLRenderState rl = GLRenderState.Lines();

            {
                items.Add(new GLFixedShader(System.Drawing.Color.Yellow), "LINEYELLOW");
                rObjects.Add(items.Shader("LINEYELLOW"), GLRenderableItem.CreateVector4(items, PrimitiveType.Lines, rl, displaylines));
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

            GLMatrixCalc mc = new GLMatrixCalc();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            if (true)
            {
                mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

                // a display control

                displaycontrol = new GLControlDisplay(items, glwfc, mc);     // start class but don't hook
                displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
                displaycontrol.Font = new Font("Times", 8);
                displaycontrol.Paint += (ts) => { displaycontrol.Render(glwfc.RenderState, ts); };

                gl3dcontroller = new Controller3D();
                gl3dcontroller.ZoomDistance = 5000F;
                gl3dcontroller.YHoldMovement = true;
                gl3dcontroller.PaintObjects = Controller3dDraw;
                gl3dcontroller.KeyboardTravelSpeed = (ms, eyedist) => { return (float)ms * 10.0f; };
                gl3dcontroller.MatrixCalc.InPerspectiveMode = true;

                // start hooks the glwfc paint function up, first, so it gets to go first
                // No ui events from glwfc.
                gl3dcontroller.Start(glwfc, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F, false, false);
                gl3dcontroller.Hook(displaycontrol, glwfc); // we get 3dcontroller events from displaycontrol, so it will get them when everything else is unselected
                displaycontrol.Hook();  // now we hook up display control to glwin, and paint

                if (true)
                {
                    GLForm pform = new GLForm("Form1", "GL Menu demonstration", new Rectangle(10, 10, 600, 200));
                    displaycontrol.AddModalForm(pform);

                    if (true)
                    {
                        GLMenuStrip menubar = new GLMenuStrip("Menubar", new Rectangle(0, 0, 500, 24));
                        menubar.AutoOpenDelay = 1000;
                        menubar.Font = new Font("Euro Caps", 12);
                        menubar.Dock = DockingType.Top;
                        menubar.SubMenuBorderWidth = 1;

                        GLMenuItem l1 = new GLMenuItem("MI-0A", "MenuA");
                        menubar.Add(l1);

                        GLMenuItem l1a = new GLMenuItem("A-1", "MenuA-1");
                        GLMenuItem l1b = new GLMenuItem("A-2", "MenuA-2");
                        l1b.CheckOnClick = true;
                        l1b.Checked = true;
                        GLMenuItem l1c = new GLMenuItem("A-3", "MenuA-3") { Image = TestControls.Properties.Resources.GoToHomeSystem };
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
                        l1a.SubMenuItems = new List<GLBaseControl>() { l1a1, l1a2, l1a3, l1a4 };

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

                }

                if (true)
                {
                    GLContextMenu ctx1, ctx2;

                    GLMenuItem cm1 = new GLMenuItem("CM1A", "Menu-1");
                    cm1.Click += (c1) => {
                        System.Diagnostics.Debug.WriteLine("Click on menu-1");
                    };

                    GLMenuItem cm2 = new GLMenuItem("CM1B", "Menu-2");
                    cm2.CheckOnClick = true;
                    GLMenuItem cm3 = new GLMenuItem("CM1C", "Menu-3");

                    GLMenuItem l1a1 = new GLMenuItem("CM1C-1", "Menu-1-1");
                    l1a1.CheckOnClick = true;
                    GLMenuItem l1a2 = new GLMenuItem("CM1C-2", "MenuA-1-2");
                    GLCheckBox l1a3 = new GLCheckBox("CM1C-3", new Rectangle(0, 0, 0, 0), "CheckBox A-1-3");
                    l1a3.CheckOnClick = true;
                    cm3.SubMenuItems = new List<GLBaseControl>() { l1a1, l1a2, l1a3 };

                    ctx1 = new GLContextMenu("CM1");
                    int count = 0;
                    ctx1.Opening += (c1,c2) =>
                    {
                        bool on = count++ % 2 == 0;
                        System.Diagnostics.Debug.WriteLine($"Set cm2 state {on}");
                        cm2.Visible = on;
                    };
                    ctx1.Add(cm1);
                    ctx1.Add(cm2);
                    ctx1.Add(cm3);

                    Color tc = Color.Orange;
                    ctx2 = new GLContextMenu("CMTX2", true, new GLBaseControl[] {
                        new GLMenuItemLabel("CMTX2L1","Menu Title CTX2"),
                        new GLMenuItemSeparator("S1",10,5,ContentAlignment.MiddleCenter, forecolor:Color.Red),
                        new GLMenuItem("CM1A","Menu Fred") { CheckOnClick = true, ForeColor = tc},
                        new GLMenuItem("CM1B","MenuR2") { CheckOnClick = true, Enabled = false, ForeColor = tc},
                        new GLMenuItem("CM1C","MenuR3") { CheckOnClick = true},
                        new GLMenuItem("CM1C","MenuR4") { CheckOnClick = true },
                        new GLMenuItem("CM1C","MenuR5") { },
                        });

                    ctx2.Font = new Font("Euro caps", 18f);

                    string[] titles = new string[] { "xxxxx one", "xxxxx one and two", "three", "four" };
                    int tno = 0;

                    ctx2.Opening += (c,s) => {
                        ctx2.Size = new Size(10, 10);
                        var ctrl = ctx2["CMTX2L1"];
                        var text = titles[tno++ % titles.Length];
                        System.Diagnostics.Debug.WriteLine($"Text is {text}");
                        ((GLMenuItemLabel)ctrl).Text = text;
                   
                    };


                    displaycontrol.MouseClick += (s, ev) =>
                    {
                        if (ev.Button == GLMouseEventArgs.MouseButtons.Left)
                        {
                            System.Diagnostics.Debug.WriteLine($"*********************** OPEN");
                            ctx1.Show(displaycontrol, ev.ScreenCoord);
                        }
                        else  if (ev.Button == GLMouseEventArgs.MouseButtons.Right)
                        {
                            ctx2.Show(displaycontrol, ev.ScreenCoord);
                        }
                    };
                }
            }

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
            if (displaycontrol != null && displaycontrol.RequestRender)
                glwfc.Invalidate();
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, Otherkeys);
        }

        private void Otherkeys(KeyboardMonitor h)
        {
            if ( h.HasBeenPressed(Keys.F1, KeyboardMonitor.ShiftState.None))
            {
            }

        }
    }
}


