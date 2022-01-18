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
    public partial class TestControlsForm : Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsForm()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);

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

            GLMatrixCalc mc = new GLMatrixCalc();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            displaycontrol = new GLControlDisplay(items, glwfc,mc);       // hook form to the window - its the master, it takes its size fro mc.ScreenCoordMax
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Name = "displaycontrol";
            displaycontrol.Font = new Font("Times", 8);

            GLForm pform;

            if (true)
            {
                pform = new GLForm("Form1", "GL Form demonstration", new Rectangle(0, 0, 1000, 800));
                //pform.BackColor = Color.FromArgb(200, Color.Red);
                //pform.Opacity = 0.7f;
                // pform.BackColorGradientDir = 90;
                //  pform.BackColorGradientAlt = Color.FromArgb(200, Color.Yellow);
                //pform.ScaleWindow = new SizeF(0.75f, 0.75f);
                //pform.AlternatePos = new RectangleF(100, 100, 500, 400);
                //pform.AlternatePos = new RectangleF(100, 100, 1200, 1000);
                //  pform.ScaleWindow = new SizeF(0.0f, 0.0f);
                //   pform.Animators.Add(new AnimateScale(100, 1000, true, new SizeF(1, 1),removeafterend:true));
                //  pform.Animators.Add(new AnimateTranslate(glwfc.ElapsedTimems + 100, glwfc.ElapsedTimems + 1000, false, new Point(100, 100), removeafterend: true));
                //  pform.Animators.Add(new AnimateOpacity(glwfc.ElapsedTimems + 100, glwfc.ElapsedTimems + 2000, false, 1.0f,0.0f, removeafterend: true));

                int taborder = 0;

                if (true)
                {
                    GLLabel lab1 = new GLLabel("Lab1", new Rectangle(400, 0, 0, 0), "From Check");
                    pform.Add(lab1);

                    GLButton b1 = new GLButton("B1", new Rectangle(5, 10, 80, 30), "Configuration Dialog");
                    b1.Margin = new MarginType(2);
                    b1.AutoSize = true;
                    b1.TabOrder = taborder++;
                    b1.Padding = new PaddingType(5);
                    b1.Click += (c, ev) => { ConfDialog(); };
                    b1.ToolTipText = "Button 1 tip\r\nLine 2 of it";
                    pform.Add(b1);

                    GLButton b2 = new GLButton("B2", new Rectangle(5, 50, 0, 0), "Msg1");
                    b2.Image = Properties.Resources.ImportSphere;
                    b2.TabOrder = taborder++;
                    b2.ImageAlign = ContentAlignment.MiddleLeft;
                    b2.TextAlign = ContentAlignment.MiddleRight;
                    b2.Click += (c, ev) => { MsgDialog1(); };
                    b2.ToolTipText = "Button 2 tip\r\nLine 2 of it";
                    pform.Add(b2);

                    GLButton b3 = new GLButton("B3", new Rectangle(100, 10, 80, 30), "Font");
                    b3.Margin = new MarginType(2);
                    b3.TabOrder = taborder++;
                    b3.Padding = new PaddingType(5);
                    b3.ToolTipText = "Button 3 tip\r\nLine 2 of it";
                    b3.Click += (c, ev) => {
                        displaycontrol.Font = new Font("Times", 12);
                    };
                    pform.Add(b3);

                    GLButton b4 = new GLButton("B4", new Rectangle(100, 50, 80, 30), "Msg2");
                    b4.TabOrder = taborder++;
                    b4.Padding = new PaddingType(2);
                    b4.ToolTipText = "Button 4 tip\r\nLine 2 of it";
                    b4.Click += (c, ev) => { MsgDialog2(); };
                    pform.Add(b4);

                    GLButton b5 = new GLButton("B5", new Rectangle(200, 10, 80, 30), "Conf2");
                    b5.TabOrder = taborder++;
                    b5.Padding = new PaddingType(2);
                    b5.ToolTipText = "Button 5 tip\r\nLine 2 of it";
                    b5.Click += (c, ev) => { ConfDialog2(); };
                    pform.Add(b5);

                    GLButton b6 = new GLButton("B3", new Rectangle(200, 50, 80, 30), "Disabled");
                    b6.TabOrder = taborder++;
                    b6.ToolTipText = "Button 6 tip\r\nLine 2 of it";
                    b6.Enabled = false;
                    pform.Add(b6);

                }

                if (true)
                {
                    GLComboBox cb1 = new GLComboBox("CB", new Rectangle(0, 100, 0, 0), new List<string>() { "one", "two", "three" });
                    cb1.Margin = new MarginType(16, 8, 16, 8);
                    cb1.TabOrder = taborder++;
                    cb1.ToolTipText = "Combo Box";
                    pform.Add(cb1);

                    GLComboBox cbstars = new GLComboBox("GalaxyStarsNumber", new Rectangle(100, 100, 100, 32));
                    cbstars.ToolTipText = "Control how many stars are shown when zoomes in";
                    cbstars.Items = new List<string>() { "Ultra", "High", "Medium", "Low" };
                    cbstars.TabOrder = taborder++;
                    var list = new List<int>() { 1000000, 500000, 250000, 100000 };
                    int itemno = 1;
                    cbstars.SelectedIndex = itemno >= 0 ? itemno : 1;       // high default
                    pform.Add(cbstars);
                }

                if (true)
                {

                    GLCheckBox chk1 = new GLCheckBox("Checkbox1", new Rectangle(0, 150, 0, 0), "Normal");
                    chk1.Margin = new MarginType(16, 0, 0, 0);
                    chk1.TabOrder = taborder++;
                    pform.Add(chk1);
                    GLCheckBox chk2 = new GLCheckBox("Checkbox2", new Rectangle(100, 150, 0, 0), "Radio");
                    chk2.Appearance = GLCheckBox.CheckBoxAppearance.Radio;
                    chk2.TabOrder = taborder++;
                    chk2.Checked = true;
                    pform.Add(chk2);
                    GLCheckBox chk3 = new GLCheckBox("Checkbox3", new Rectangle(200, 150, 0, 0), "Button");
                    chk3.Appearance = GLCheckBox.CheckBoxAppearance.Button;
                    chk3.TabOrder = taborder++;
                    chk3.BackColor = Color.FromArgb(200, 200, 200);
                    pform.Add(chk3);
                    GLCheckBox chk4 = new GLCheckBox("Checkbox4", new Rectangle(300, 150, 0, 0), "");
                    chk4.TabOrder = taborder++;
                    pform.Add(chk4);
                    GLCheckBox chk5 = new GLCheckBox("Checkbox5", new Rectangle(350, 150, 0, 0), "R1");
                    chk5.Appearance = GLCheckBox.CheckBoxAppearance.Radio;
                    chk5.GroupRadioButton = true;
                    chk5.TabOrder = taborder++;
                    pform.Add(chk5);
                    GLCheckBox chk6 = new GLCheckBox("Checkbox6", new Rectangle(400, 150, 0, 0), "R2");
                    chk6.Appearance = GLCheckBox.CheckBoxAppearance.Radio;
                    chk6.GroupRadioButton = true;
                    chk6.TabOrder = taborder++;
                    pform.Add(chk6);
                    GLCheckBox chk7 = new GLCheckBox("Checkbox7", new Rectangle(0, 175, 0, 0), "Disabled");
                    chk7.TabOrder = taborder++;
                    chk7.Enabled = false;
                    pform.Add(chk7);
                    GLCheckBox chk8 = new GLCheckBox("Checkbox8", new Rectangle(100, 175, 0, 0), "Disabled");
                    chk8.Appearance = GLCheckBox.CheckBoxAppearance.Radio;
                    chk8.TabOrder = taborder++;
                    chk8.Enabled = false;
                    pform.Add(chk8);
                    GLCheckBox chk9 = new GLCheckBox("Checkbox9", new Rectangle(200, 175, 0, 0), "CDisabled");
                    chk9.TabOrder = taborder++;
                    chk9.Enabled = false;
                    chk9.Checked = true;
                    pform.Add(chk9);
                    GLCheckBox chk10 = new GLCheckBox("Checkbox10", new Rectangle(300, 175, 0, 0), "CDisabled");
                    chk10.Appearance = GLCheckBox.CheckBoxAppearance.Radio;
                    chk10.TabOrder = taborder++;
                    chk10.Enabled = false;
                    chk10.Checked = true;
                    pform.Add(chk10);
                }

                if (true)
                {
                    GLDateTimePicker dtp = new GLDateTimePicker("DTP", new Rectangle(0, 210, 500, 30), DateTime.Now);
                    dtp.Culture = System.Globalization.CultureInfo.GetCultureInfo("de-AT");
                    dtp.Format = GLDateTimePicker.DateTimePickerFormat.Long;
                    //dtp.CustomFormat = "'start' dddd 'hello there' MMMM' and here 'yyyy";
                    dtp.Font = new Font("Ms Sans Serif", 11);
                    dtp.ShowCheckBox = true;
                    dtp.ShowCalendar = true;
                    dtp.ShowUpDown = true;
                    dtp.AutoSize = true;
                    //dtp.Culture = CultureInfo.GetCultureInfo("es");
                    dtp.TabOrder = taborder++;
                    pform.Add(dtp);
                }

                if (true)
                {
                    List<string> i1 = new List<string>() { "one two three four five six seven eight", "two", "three", "four", "five", "six", "seven is very long too to check", "eight", "nine", "ten", "eleven", "twelve" };
                    GLListBox lb1 = new GLListBox("LB1", new Rectangle(0, 260, 260, 100), i1);
                    lb1.Font = new Font("Microsoft Sans Serif", 12f);
                    lb1.TabOrder = taborder++;
                    lb1.ShowFocusBox = true;
                    lb1.ScrollBarTheme.SliderColor = Color.AliceBlue;
                    lb1.ScrollBarTheme.ThumbButtonColor = Color.Blue;
                    //lb1.FitToItemsHeight = false;
                    pform.Add(lb1);
                    lb1.SelectedIndexChanged += (s, si) => { System.Diagnostics.Debug.WriteLine("Selected index " + si); };
                }

                if (true)
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

                if (true)
                {
                    GLTextBox tb1 = new GLTextBox("TB1", new Rectangle(0, 500, 150, 40), "Text Data Which is a very long string of very many many characters");
                    tb1.Font = new Font("Arial", 12);
                    tb1.TabOrder = taborder++;
                    pform.Add(tb1);
                }

                if (true)
                {
                    GLUpDownControl upc1 = new GLUpDownControl("UPC1", new Rectangle(0, 600, 26, 46));
                    upc1.TabOrder = taborder++;
                    pform.Add(upc1);
                    upc1.Clicked += (s, upe) => System.Diagnostics.Debug.WriteLine("Up down control {0} {1}", s.Name, upe);
                }

                if (true)
                {
                    GLCalendar cal = new GLCalendar("Cal", new Rectangle(500, 10, 300, 200));
                    cal.TabOrder = taborder++;
                    //cal.Culture = CultureInfo.GetCultureInfo("es");
                    cal.AutoSize = true;
                    cal.Font = new Font("Arial", 10);
                    pform.Add(cal);
                }

                if (true)
                { 
                    GLNumberBoxFloat glf = new GLNumberBoxFloat("FLOAT", new Rectangle(500, 250, 100, 25), 23.4f);
                    glf.BackColor = Color.AliceBlue;
                    glf.TabOrder = taborder++;
                    glf.Font = new Font("Ms Sans Serif", 12);
                    glf.Minimum = -1000;
                    glf.Maximum = 1000;
                    glf.ValueChanged += (a) => { System.Diagnostics.Debug.WriteLine("GLF value changed"); };
                    glf.ValidityChanged += (a,b) => { System.Diagnostics.Debug.WriteLine($"GLF validity changed {b}"); };
                    pform.Add(glf);

                    GLButton glfbut = new GLButton("FLOATBUT", new Rectangle(610, 250, 40, 15), "Value");
                    glfbut.Click += (e1, b1) => { glf.Value = 20.22f; };
                    pform.Add(glfbut);

                    GLTextBoxAutoComplete gla = new GLTextBoxAutoComplete("ACTB", new Rectangle(500, 300, 100, 25));
                    gla.TabOrder = taborder++;
                    gla.Font = new Font("Ms Sans Serif", 12);
                    gla.PerformAutoCompleteInThread += (s, a, set) =>
                    {
                        var r = new List<string>() { "one", "two", "three" };
                        foreach(var x in r)
                        {
                            if (x.StartsWith(s) || s.IsEmpty())
                                set.Add(x);
                        }
                    };
                    gla.SelectedEntry += (s) => { System.Diagnostics.Debug.WriteLine($"Autocomplete selected {s.Text}"); };
                    pform.Add(gla);
                }

                if (true)
                {
                    GLButton b1 = new GLButton("BD1", new Rectangle(5, 10, 80, 30), "Bottom 1");
                    b1.TabOrder = taborder++;
                    b1.Dock = DockingType.Bottom;
                    displaycontrol.Add(b1);
                    GLButton b2 = new GLButton("BD2", new Rectangle(5, 10, 80, 30), "Bottom 2");
                    b2.TabOrder = taborder++;
                    b2.Dock = DockingType.Bottom;
                    displaycontrol.Add(b2);
                }

                displaycontrol.Add(pform);

            }

            if (true)
            {
                GLForm pform2 = new GLForm("Form2", "Form 2 GL Control demonstration", new Rectangle(1100, 0, 400, 400));
                pform2.BackColor = Color.FromArgb(200, Color.Red);
                pform2.Font = new Font("Ms sans serif", 10);
                pform2.BackColorGradientDir = 90;
                pform2.BackColorGradientAlt = Color.FromArgb(200, Color.Blue);
                displaycontrol.Add(pform2);

                GLButton b1 = new GLButton("*********** F2B1", new Rectangle(5, 10, 80, 30), "F2B1");
                pform2.Add(b1);
            }

            if (true)
            {
                GLToolTip tip = new GLToolTip("ToolTip");
                displaycontrol.Add(tip);
            }

            displaycontrol.GlobalMouseDown += (ctrl, ex) =>
            {
                if (ctrl == null || !pform.IsThisOrChildOf(ctrl))
                {
                  //  System.Diagnostics.Debug.WriteLine("Not on form");
                }
                else
                {
                  //  System.Diagnostics.Debug.WriteLine("Click on form");
                }
            };



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
                    displaycontrol.Render(glwfc.RenderState,ts);       // we use the same matrix calc as done in controller 3d draw
                };

            }
            else
                gl3dcontroller.Start(glwfc, new Vector3(0, 0, 10000), new Vector3(140.75f, 0, 0), 0.5F);     // HOOK the 3dcontroller to the form so it gets Form events

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
                displaycontrol.DumpTrees(0,null);
            }

        }
    }
}


