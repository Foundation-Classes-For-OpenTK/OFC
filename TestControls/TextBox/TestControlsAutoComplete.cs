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
    public partial class TestControlsAutoComplete : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsAutoComplete()
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

            GLBaseControl.Themer = Theme;

            GLForm pform = new GLForm("Form1", "GL Form demonstration", new Rectangle(20, 20, 1000, 800));

            int taborder = 0;

            if (true)
            {
                GLPanel pn = new GLPanel("PanelNumberBoxAutocomplete", new Rectangle(10, 10, 400, 100), Color.AliceBlue);

                {
                    GLTextBoxAutoComplete gla = new GLTextBoxAutoComplete("ACTB", new Rectangle(4, 4, 200, 32));
                    gla.TabOrder = taborder++;
                    gla.ShowEndButton = true;
                    gla.Font = new Font("Ms Sans Serif", 12);
                    gla.PerformAutoCompleteInThread += (s, a, set) =>
                    {
                        var r = new List<string>();
                        for (int i = 0; i < 100; i++)
                            r.Add("one" + i);
                        foreach (var x in r)
                        {
                            if (x.StartsWith(s) || s.IsEmpty())
                                set.Add(x);
                        }

                        System.Threading.Thread.Sleep(200);
                    };
                    gla.SelectedEntry += (s) => { System.Diagnostics.Debug.WriteLine($"Autocomplete selected {s.Text}"); };
                    pn.Add(gla);

                    GLButton wbo = new GLButton("wbon", new Rectangle(220, 10, 40, 24), "WB");
                    wbo.Click = (ss, ee) => { gla.ShowEndButton = !gla.ShowEndButton; };
                    pn.Add(wbo);
                }
                {
                    GLTextBoxAutoComplete gla = new GLTextBoxAutoComplete("ACTB", new Rectangle(4, 50, 200, 25));
                    gla.TabOrder = taborder++;
                    gla.ShowEndButton = false;
                    gla.Font = new Font("Ms Sans Serif", 12);
                    gla.PerformAutoCompleteInThread += (s, a, set) =>
                    {
                        var r = new List<string>();
                        for (int i = 0; i < 100; i++)
                            r.Add("two" + i);
                        foreach (var x in r)
                        {
                            if (x.StartsWith(s) || s.IsEmpty())
                                set.Add(x);
                        }

                        System.Threading.Thread.Sleep(200);
                    };

                    gla.SelectedEntry += (s) => { System.Diagnostics.Debug.WriteLine($"Autocomplete selected {s.Text}"); };
                    pn.Add(gla);
                }


                pform.Add(pn);
            }

            displaycontrol.AddModalForm(pform);

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
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true, Otherkeys);
        }

        private void Otherkeys(KeyboardMonitor h)
        {
            if ( h.HasBeenPressed(Keys.F1, KeyboardMonitor.ShiftState.None))
            {
            }

        }

        static void Theme(GLBaseControl ctrl)      // run on each control during add, theme it
        {
            // System.Diagnostics.Debug.WriteLine($"Theme {ctrl.GetType().Name} {ctrl.Name}");

            Color formback = Color.FromArgb(255, 60, 60, 70);
            Color buttonface = Color.FromArgb(255, 128, 128, 128);
            Color texc = Color.Orange;

            var but = ctrl as GLButton;
            if (but != null)
            {
                but.ButtonFaceColor = buttonface;
                but.ForeColor = texc;
                but.BackColor = formback;
                but.BorderColor = Color.FromArgb(255, 90, 90, 90);
            }

            var cb = ctrl as GLCheckBox;
            if (cb != null)
            {
                cb.BackColor = formback;
                cb.ButtonFaceColor = buttonface;
                cb.CheckBoxInnerColor = texc;
                cb.TickBoxReductionRatio = 0.7f;
            }
            var cmb = ctrl as GLComboBox;
            if (cmb != null)
            {
                cmb.BackColor = formback;
                cmb.ForeColor = cmb.DropDownForeColor = texc;
                cmb.FaceColor = cmb.DropDownBackgroundColor = buttonface;
                cmb.BorderColor = formback;
            }

            var dt = ctrl as GLDateTimePicker;
            if (dt != null)
            {
                dt.BackColor = buttonface;
                dt.ForeColor = texc;
                dt.Calendar.ButLeft.ForeColor = dt.Calendar.ButRight.ForeColor = texc;
                dt.SelectedColor = Color.FromArgb(255, 160, 160, 160);
            }

            var fr = ctrl as GLForm;
            if (fr != null)
            {
                fr.BackColor = formback;
                fr.ForeColor = texc;
            }

            var tb = ctrl as GLMultiLineTextBox;    // also autocomplete text box
            if (tb != null)
            {
                tb.BackColor = formback;
                tb.ForeColor = texc;
                tb.TextAreaBackColor = Color.Gray;
                if (!(tb.Parent is GLFormConfigurable))     // this is to stop themeing the box around the warning text
                {
                    tb.BorderColor = Color.Gray;
                    tb.BorderWidth = 1;
                }

              //  tb.LineHeight = 24;
              //  tb.Padding = new PaddingType(0);
            }

            var autoc = ctrl as GLTextBoxAutoComplete;

            if ( autoc!=null)
            {
                autoc.EndButton.BackColor = formback;
                autoc.EndButton.BackColor = Color.Gray;
                autoc.EndButton.ImageFixedSize = new Size(12, 12);
                autoc.EndButton.Size = new Size(20, 20);
                autoc.ListBox.BackColor = Color.Gray;
                autoc.ListBox.ForeColor = Color.White;

                System.Drawing.Imaging.ColorMap colormap1 = new System.Drawing.Imaging.ColorMap();       // any drawn panel with drawn images
                colormap1.OldColor = Color.FromArgb(0, 0, 0);                                      // gray is defined as the forecolour
                colormap1.NewColor = texc;
                //System.Diagnostics.Debug.WriteLine("Theme Image in " + ctrl.Name + " Map " + colormap1.OldColor + " to " + colormap1.NewColor);

                System.Drawing.Imaging.ColorMap colormap2 = new System.Drawing.Imaging.ColorMap();       // any drawn panel with drawn images
                colormap2.OldColor = Color.FromArgb(255, 255, 255);                                      // and white is defined as the forecolour
                colormap2.NewColor = Color.Black;
                //System.Diagnostics.Debug.WriteLine("Theme Image in " + ctrl.Name + " Map " + colormap2.OldColor + " to " + colormap2.NewColor);

                autoc.EndButton.SetDrawnBitmapRemapTable(new System.Drawing.Imaging.ColorMap[] { colormap1, colormap2 });     // used ButtonDisabledScaling note!

            }

            var lb = ctrl as GLLabel;
            if (lb != null)
            {
                lb.ForeColor = texc;
            }

            Color cmbck = Color.FromArgb(255, 128, 128, 128);

            var ms = ctrl as GLMenuStrip;
            if (ms != null)
            {
                ms.BackColor = cmbck;
                ms.IconStripBackColor = cmbck.Multiply(1.2f);
            }
            var mi = ctrl as GLMenuItem;
            if (mi != null)
            {
                mi.BackColor = cmbck;
                mi.ButtonFaceColor = cmbck;
                mi.ForeColor = texc;
                mi.BackDisabledScaling = 1.0f;
            }

            var gb = ctrl as GLGroupBox;
            if (gb != null)
            {
                gb.BackColor = Color.Transparent;
                gb.ForeColor = Color.Orange;
            }

            var flp = ctrl as GLFlowLayoutPanel;
            if (flp != null)
            {
                flp.BackColor = formback;
            }

            var sp = ctrl as GLScrollPanelScrollBar;
            if (sp != null)
            {
                sp.BackColor = formback;
            }

            var p = ctrl as GLPanel;
            if (p != null)
            {
                p.BackColor = formback;
            }


            var trb = ctrl as GLTrackBar;
            if (trb != null)
            {
                trb.BackColor = formback;
                trb.TickColor = texc;
                trb.ForeColor = buttonface;     // of bar
                trb.ButtonFaceColor = Color.FromArgb(255, 190, 190, 190);
                trb.FaceColorScaling = 0.5f;
            }


            //{
            //    float[][] colorMatrixElements = {
            //               new float[] {0.5f,  0,  0,  0, 0},        // red scaling factor of 0.5
            //               new float[] {0,  0.5f,  0,  0, 0},        // green scaling factor of 1
            //               new float[] {0,  0,  0.5f,  0, 0},        // blue scaling factor of 1
            //               new float[] {0,  0,  0,  1, 0},        // alpha scaling factor of 1
            //               new float[] {0.0f, 0.0f, 0.0f, 0, 1}};    // three translations of 

            //    var colormap1 = new System.Drawing.Imaging.ColorMap();
            //    cb.SetDrawnBitmapUnchecked(new System.Drawing.Imaging.ColorMap[] { colormap1 }, colorMatrixElements);
            //}
        }

    }
}


