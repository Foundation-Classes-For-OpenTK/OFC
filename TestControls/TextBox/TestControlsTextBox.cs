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
using System.Globalization;
using GLOFC.Utils;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Basic;
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.ShapeFactory;
using static GLOFC.GL4.Controls.GLBaseControl;
using static GLOFC.GL4.Controls.GLForm;

namespace TestOpenTk
{
    public partial class TestControlsTextBox : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsTextBox()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer,null,4,6);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
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

            items.Add(new GLMatrixCalcUniformBlock(), "MCUB");     // create a matrix uniform block 

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

            GLMatrixCalc mc = new GLMatrixCalc();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            // a display control

            displaycontrol = new GLControlDisplay(items, glwfc, mc);     // start class but don't hook
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Font = new Font("Times", 8);
            displaycontrol.Paint += (ts) => { System.Diagnostics.Debug.WriteLine("Paint controls"); displaycontrol.Render(glwfc.RenderState, ts); };

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


            GLBaseControl.Themer = Theme;
            GLBaseControl.NoThemer = NoTheme;

            GLForm pform = new GLForm("Form1", "GL Form demonstration", new Rectangle(0, 0, 1000, 800));

            int taborder = 0;

            if (true)
            {
                string l = "Hello there ggg qqq jjj" + Environment.NewLine +"And the ggg next line";

                GLMultiLineTextBox mtb = new GLMultiLineTextBox("mltb", new Rectangle(10, 10, 400, 200), l);
                mtb.Font = new Font("Ms Sans Serif", 24.25f);
                //mtb.Font = new Font("Arial", 25f);
                mtb.LineColor = Color.Green;
               // mtb.EnableVerticalScrollBar = true;
              //  mtb.EnableHorizontalScrollBar = true;
                mtb.SetSelection(16 * 2 + 2, 16 * 3 + 4);
                mtb.TabOrder = taborder++;
                mtb.RightClickMenuFont = new Font("Euro Caps", 14f);
                mtb.FlashingCursor = false;
                mtb.BackColor = Color.Yellow;
                pform.Add(mtb);

            }


            if (false)
            {
                string l = "";
                for (int i = 0; i < 5; i++)
                {
                    string s = string.Format("Line " + i);
                    if (i == 0)
                        s += "And a much much longer Line which should break the width";
                    l += s + "\r\n";
                }
                l += "trail ";
                // l = "";

                GLMultiLineTextBox mtb = new GLMultiLineTextBox("mltb", new Rectangle(0, 400, 400, 200), l);
                mtb.Font = new Font("Ms Sans Serif", 16);
                mtb.LineColor = Color.Green;
                mtb.EnableVerticalScrollBar = true;
                mtb.EnableHorizontalScrollBar = true;
                mtb.SetSelection(16 * 2 + 2, 16 * 3 + 4);
                mtb.TabOrder = taborder++;
                mtb.RightClickMenuFont = new Font("Euro Caps", 14f);
                mtb.BackColor = Color.Blue;
                pform.Add(mtb);
                //mtb.FlashingCursor = false;
                //mtb.ReadOnly = true;

                GLMultiLineTextBox mtb2 = new GLMultiLineTextBox("mltb2", new Rectangle(500, 400, 495, 200), l);
                mtb2.Font = new Font("Ms Sans Serif", 11);
                mtb2.LineColor = Color.Green;
                mtb2.EnableVerticalScrollBar = true;
                mtb2.EnableHorizontalScrollBar = true;
                mtb2.SetSelection(16 * 2 + 2, 16 * 3 + 4);
                mtb2.TabOrder = taborder++;
                mtb2.RightClickMenuFont = new Font("Arial", 14f);
                pform.Add(mtb2);
            }

            if (false)
            {
                GLTextBox tb1 = new GLTextBox("TB1", new Rectangle(0, 300, 350, 40), "Text Box Which is a very long string of very many many characters");
                tb1.Font = new Font("Arial", 12);
                tb1.ReturnPressed += (c1) => { System.Diagnostics.Debug.WriteLine($"Return pressed on text box"); };
                tb1.TabOrder = taborder++;
                tb1.BackColor = Color.AliceBlue;
                tb1.ForeColor = Color.Red;
                pform.Add(tb1);
            }



            displaycontrol.Add(pform);

            systemtimer.Start();
        }

        private void FormDialog()
        {
            GLForm pform2 = new GLForm("Form2", "Popupform", new Rectangle(500, 100, 200, 200));
            pform2.BackColor = Color.FromArgb(200, Color.Green);
            pform2.Font = new Font("Ms sans serif", 10);
            pform2.BackColorGradientDir = 90;
            pform2.BackColorGradientAlt = Color.FromArgb(200, Color.Blue);
            displaycontrol.Add(pform2);
        }


        private void ConfDialog()
        {
            GLFormConfigurable cform = new GLFormConfigurable("test");
            cform.Add(new GLFormConfigurable.Entry("Lab1", typeof(GLLabel), "Label 1 ", new Point(10, 10), new Size(200, 24), "TT"));
            cform.Add(new GLFormConfigurable.Entry("But1", typeof(GLButton), "But 1", new Point(10, 40), new Size(200, 24), "TT"));
            cform.Add(new GLFormConfigurable.Entry("Com1", "two", new Point(10, 70), new Size(200, 24), "TT", new string[] { "one", "two", "three" }));
            cform.Add(new GLFormConfigurable.Entry("Textb", typeof(GLTextBox), "text box", new Point(10, 100), new Size(200, 24), "TT"));
            cform.Add(new GLFormConfigurable.Entry("Double", typeof(GLNumberBoxDouble), "10.2", new Point(10, 100), new Size(200, 24), "TT"));
            cform.Add(new GLFormConfigurable.Entry("OK", typeof(GLButton), "OK", new Point(160, 300), new Size(100, 24), "TT") { Anchor = AnchorType.Right | AnchorType.Bottom });
            cform.InitCentered("Config Form Test");
            cform.Trigger += (cb, en, ctrlname, args) =>
            {
                if (ctrlname == "OK")
                    cform.Close();
            };
            displaycontrol.Add(cform);
        }

        private void ConfDialog2()
        {
            GLMultiLineTextBox tb = new GLMultiLineTextBox("MLT", new Rectangle(10, 10, 1000, 1000), "this is some text\r\nAnd some more");
            var sizer = tb.CalculateTextArea(new Size(50, 24), new Size(400, 400));
            tb.Size = sizer.Item1;
            tb.EnableHorizontalScrollBar = sizer.Item2;
            tb.CursorToEnd();

            GLFormConfigurable cform = new GLFormConfigurable("ConfDialog2");
            cform.AddOK("OK");
            cform.AddCancel("Cancel");
            cform.Add(new GLFormConfigurable.Entry("info", tb));
            cform.InstallStandardTriggers();
            cform.Init(new Point(200, 200), "Config Form Test Long Title and even longer one");
            cform.SetMinimumSizeOnAutoSize = true;
            displaycontrol.Add(cform);      // display and autosize
            cform.AutoSize = false;
            cform.Moveable = true;
            cform.Resizeable = true;
            tb.Width = cform.ClientWidth - 10 * 2;
            System.Diagnostics.Debug.WriteLine($"Autosize {cform.AutoSize}");
        }

        private void FormAutoSize()
        {
            GLForm f = new GLForm("FormAutoTest", "This is a long title with many characters", new Rectangle(100, 100, 50, 50));
            f.AutoSizeToTitle = true;
            f.AutoSize = true;
            displaycontrol.Add(f);
        }

        private void MsgDialog1()
        {
            string t = "";
            for (int i = 0; i < 100; i++)
                t += "Line " + i + " is here" + Environment.NewLine;

            GLMessageBox msg = new GLMessageBox("MB", displaycontrol, new Point(int.MinValue, 500), t, "Caption", GLMessageBox.MessageBoxButtons.OKCancel, callback:MsgReturn);
        }

        private void MsgDialog2()
        {
            string t = "";
            for (int i = 0; i < 30; i++)
                t += "Line " + i + " is here" + " and lets make it very long for an example" +  Environment.NewLine;

            //            GLMessageBox msg = new GLMessageBox("MB", displaycontrol, new Point(300, 500), MsgReturn,"Small message\r\nShorter than\r\nThe other" , "Caption Long here to demonstrate", GLMessageBox.MessageBoxButtons.AbortRetryIgnore);
            //GLMessageBox msg = new GLMessageBox("MB", displaycontrol, new Point(300, 500), MsgReturn, "Longer message message\r\nShorter than\r\nThe other", "Caption Short", GLMessageBox.MessageBoxButtons.AbortRetryIgnore);
            GLMessageBox msg = new GLMessageBox("MB", displaycontrol, new Point(300, 500), t, "Caption Short", GLMessageBox.MessageBoxButtons.AbortRetryIgnore, callback: MsgReturn);
        }

        private void MsgReturn(GLMessageBox msg, DialogResultEnum res)
        {
            System.Diagnostics.Debug.WriteLine("!!! Message box " + res);
        }

        static void Theme(GLBaseControl ctrl)      // run on each control during add, theme it
        {
            System.Diagnostics.Debug.WriteLine($"Theme {ctrl.GetType().Name} {ctrl.Name}");

        }
        static void NoTheme(GLBaseControl ctrl)      // run on each control during add, theme it
        {
            System.Diagnostics.Debug.WriteLine($"No Theme {ctrl.GetType().Name} {ctrl.Name}");
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


