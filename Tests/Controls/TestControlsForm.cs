using OpenTK;
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
    public partial class TestControlsForm : Form
    {
        private OFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public TestControlsForm()
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
                    GLForm pform = new GLForm("Form1", "GL Control 2 demonstration", new Rectangle(10, 10, 600, 400));
                    pform.BackColor = Color.FromArgb(200, Color.Red);
                    pform.SuspendLayout();
                    pform.BackColorGradientDir = 90;
                    pform.BackColorGradientAlt = Color.FromArgb(200, Color.Yellow);
                    displaycontrol.Add(pform);

                    GLButton b1 = new GLButton("B1", new Rectangle(5, 5, 80, 40), "Button 1");
                    b1.Margin = new Margin(5);
                    b1.TabOrder = 1;
                    b1.Padding = new OFC.GL4.Controls.Padding(5);
                    b1.Click += (c, ev) => { ConfDialog(); };
                    b1.ToolTipText = "Button 1 tip\r\nLine 2 of it";
                    pform.Add(b1);

                    GLButton b2 = new GLButton("B2", new Rectangle(5, 50, 0, 0), "Button 2");
                    b2.Image = Properties.Resources.ImportSphere;
                    b2.TabOrder = 2;
                    b2.ImageAlign = ContentAlignment.MiddleLeft;
                    b2.TextAlign = ContentAlignment.MiddleRight;
                    b2.Click += (c, ev) => { MsgDialog(); };
                    b2.ToolTipText = "Button 2 tip\r\nLine 2 of it";
                    pform.Add(b2);

                    GLComboBox cb1 = new GLComboBox("CB", new Rectangle(0, 100, 0, 0), new List<string>() { "one", "two", "three" });
                    cb1.Margin = new Margin(16, 0, 16, 0);
                    cb1.TabOrder = 3;

                    pform.Add(cb1);
                    GLCheckBox chk1 = new GLCheckBox("Checkbox", new Rectangle(0, 150, 0, 0), "CheckBox 1");
                    chk1.Margin = new Margin(16, 0, 0, 0);
                    chk1.TabOrder = 4;
                    pform.Add(chk1);
                    GLCheckBox chk2 = new GLCheckBox("Checkbox", new Rectangle(200, 150, 0, 0), "CheckBox 2");
                    chk2.Appearance = CheckBoxAppearance.Radio;
                    chk2.TabOrder = 5;
                    pform.Add(chk2);
                    GLCheckBox chk3 = new GLCheckBox("Checkbox", new Rectangle(400, 150, 0, 0), "CheckBox 2");
                    chk3.Appearance = CheckBoxAppearance.Button;
                    chk3.TabOrder = 6;
                    pform.Add(chk3);

                    pform.ResumeLayout();
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


