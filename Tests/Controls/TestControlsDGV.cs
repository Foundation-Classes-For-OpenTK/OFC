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

namespace TestOpenTk
{
    public partial class TestControlsDGV: System.Windows.Forms.Form
    {
        private GLOFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private System.Windows.Forms.Timer systemtimer = new System.Windows.Forms.Timer();

        public TestControlsDGV()
        {
            InitializeComponent();

            glwfc = new GLOFC.WinForm.GLWinFormControl(glControlContainer);
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();
        GLControlDisplay displaycontrol;
        GLDataGridView dgv;
        GLForm pform;
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

            GLMatrixCalc mc = new GLMatrixCalc();
            mc.PerspectiveNearZDistance = 1f;
            mc.PerspectiveFarZDistance = 500000f;

            mc.ResizeViewPort(this, glwfc.Size);          // must establish size before starting

            displaycontrol = new GLControlDisplay(items, glwfc,mc);       // hook form to the window - its the master, it takes its size fro mc.ScreenCoordMax
            displaycontrol.Focusable = true;          // we want to be able to focus and receive key presses.
            displaycontrol.Name = "displaycontrol";

            pform = new GLForm("Form1", "GL Control demonstration", new Rectangle(10, 10, 700, 800));


            displaycontrol.Add(pform);

            if (true)
            {
                dgv = new GLDataGridView("DGV-1", new Rectangle(10, 10, 600, 500));
                dgv.Dock = DockingType.Fill;
               // dgv.ColumnFillMode = GLDataGridView.ColFillMode.FillWidth;
                var col0 = dgv.CreateColumn();
                var col1 = dgv.CreateColumn();
                var col2 = dgv.CreateColumn();
                col0.Width = 20;
                col0.MinimumWidth = 100;
                col0.Text = "Col0";
                col1.Width = 250;
                col1.Text = "Col1";
                col1.MinimumWidth = 25;
                col2.Width = 250;
                col2.Text = "Col2";
                dgv.AddColumn(col0);
                dgv.AddColumn(col1);
                dgv.AddColumn(col2);

                for (int i = 0; i < 10; i++)
                {
                    var row = dgv.CreateRow();
                    if ( i < 2 || i > 5) row.AutoSize = true;
                    string prefix = char.ConvertFromUtf32(i + 65);
                    //row.AddCell(new GLDataGridViewCellText($"{prefix} Text"));
                    row.AddCell(new GLDataGridViewCellText($"{prefix} R{i,2}C0 long bit of text for it to wrap again and again and again"));
                    row.AddCell(new GLDataGridViewCellText($"R{i}C1"));
                    row.AddCell(new GLDataGridViewCellText($"R{i}C2"));
                    dgv.AddRow(row);
                }

                dgv.Rows[1].Height = 40;

              //  dgv.Rows[1].Cells[0].Selected = true;

                pform.Add(dgv);
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

        private void SystemTick(object sender, EventArgs e)
        {
            GLOFC.Timers.Timer.ProcessTimers();
            displaycontrol.Animate(glwfc.ElapsedTimems);
            if (displaycontrol != null && displaycontrol.RequestRender)
                glwfc.Invalidate();
            gl3dcontroller.HandleKeyboardSlewsAndInvalidateIfMoved(true);
        }

        private void Controller3dDraw(Controller3D mc, ulong unused)
        {
            ((GLMatrixCalcUniformBlock)items.UB("MCUB")).SetText(gl3dcontroller.MatrixCalc);        // set the matrix unform block to the controller 3d matrix calc.

            rObjects.Render(glwfc.RenderState, gl3dcontroller.MatrixCalc);

            this.Text = "Looking at " + gl3dcontroller.MatrixCalc.LookAt + " eye@ " + gl3dcontroller.MatrixCalc.EyePosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance + " Zoom " + gl3dcontroller.PosCamera.ZoomFactor;
        }

        private void buttonAddRow_Click(object sender, EventArgs e)
        {
            var row = dgv.CreateRow();
            // row.AutoSize = true;
            int i = dgv.Rows.Count;
            row.AddCell(new GLDataGridViewCellText($"R{i}C0"));
            row.AddCell(new GLDataGridViewCellText($"R{i}C1"));
            dgv.AddRow(row);

        }

        private void buttonInsertRow1_Click(object sender, EventArgs e)
        {
            var row = dgv.CreateRow();
            // row.AutoSize = true;
            int i = dgv.Rows.Count;
            row.AddCell(new GLDataGridViewCellText($"R{i}C0"));
            row.AddCell(new GLDataGridViewCellText($"R{i}C1"));
            dgv.AddRow(row,1);
        }

        private void buttonRemoveRow1_Click(object sender, EventArgs e)
        {
            dgv.RemoveRow(1);

        }

        private void buttonRemoveCol0_Click(object sender, EventArgs e)
        {
            dgv.RemoveColumn(0);
        }

        private void buttonAddCol0_Click(object sender, EventArgs e)
        {
            var col0 = dgv.CreateColumn();
            col0.Width = 200;
            col0.Text = "Col0";
            dgv.AddColumn(col0);

        }

        private void buttonAddCell_Click(object sender, EventArgs e)
        {
            for( int r = 1; r < 10; r++)
            {
                var row = dgv.Rows[r];
                var cell = new GLDataGridViewCellText($"R{r}CX long bit of text for it to wrap again and again and again over and over again until it takes a long number of lines");
                cell.Style.TextFormat = (r % 2 == 0) ? StringFormatFlags.NoWrap : 0;
                row.AddCell(cell);
            }
        }

        private void buttonSizeA_Click(object sender, EventArgs e)
        {
            pform.Size = new Size(800, 600);
        }

        private void buttonSizeB_Click(object sender, EventArgs e)
        {
            pform.Size = new Size(800, 700);
        }

        private void buttonDisableTextCol0_Click(object sender, EventArgs e)
        {
            dgv.Columns[0].ShowHeaderText = !dgv.Columns[0].ShowHeaderText;
        }

        private void buttonDisableTextRow1_Click(object sender, EventArgs e)
        {
            dgv.Rows[1].ShowHeaderText = !dgv.Rows[1].ShowHeaderText;

        }

        private void buttonToggleColumnWidthAdjust_Click(object sender, EventArgs e)
        {
            dgv.AllowUserToResizeColumns = !dgv.AllowUserToResizeColumns;
        }

        private void buttonToggleFillMode_Click(object sender, EventArgs e)
        {
            dgv.ColumnFillMode = dgv.ColumnFillMode == GLDataGridView.ColFillMode.FillWidth ? GLDataGridView.ColFillMode.Width : GLDataGridView.ColFillMode.FillWidth;
        }

        private void buttonColumnHeightAdjust_Click(object sender, EventArgs e)
        {
            dgv.AllowUserToResizeColumnHeight = !dgv.AllowUserToResizeColumnHeight;

        }

        private void buttonSelR1C1_Click(object sender, EventArgs e)
        {
            dgv.Rows[1].Cells[1].Selected = !dgv.Rows[1].Cells[1].Selected;
        }

        private void buttonToggleR1Sel_Click(object sender, EventArgs e)
        {
            dgv.Rows[1].Selected = !dgv.Rows[1].Selected;
        }
    }
}


