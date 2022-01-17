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

using GLOFC.Utils;
using System;
using System.Drawing;

// Internal class for DGV, no documentation needed
#pragma warning disable 1591

namespace GLOFC.GL4.Controls
{
    public class GLDataGridViewRowHeaderPanel : GLPanel
    {
        public Action<int, GLMouseEventArgs> MouseClickRowHeader;                // -1 for top left cell

        public GLDataGridViewRowHeaderPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
            autoscroll.Tick += (t, tick) =>
            {
                GLDataGridView dgv = Parent as GLDataGridView;

                if (lastmousemove.Y > 0)
                {
                    if (dgv.LastCompleteLine() < dgv.Rows.Count - 1)     // and scroll to the end, until all lines are on screen
                        dgv.FirstDisplayIndex++;
                }
                else
                {
                    if (dgv.FirstDisplayIndex>0)
                        dgv.FirstDisplayIndex--;
                }

                UpdateSelection(lastmousemove);
            };
        }

        public void Redraw(int yoffset)
        {
            this.yoffset = yoffset;
            Invalidate();
        }

        // we act as a slave of content panel, and do what it tells us
        public void DrawEmpty()
        {
            if (gridbitmap != null)
            {
                using (Graphics gr = Graphics.FromImage(gridbitmap))
                {
                    gr.Clear(Color.Transparent);
                }
                Invalidate();
            }
        }

        public void DrawHeaders(int firstline, int lastline, int drawndepth, int ystart)
        {
            yoffset = ystart;

            GLDataGridView dgv = Parent as GLDataGridView;

            if (!dgv.RowHeaderEnable || dgv.Rows.Count == 0 || dgv.Columns.Count == 0)        // sanity check
                return;

            int headerwidth = dgv.RowHeaderWidth + dgv.CellBorderWidth;

            if (gridbitmap == null ||gridbitmap.Height < drawndepth || gridbitmap.Width < headerwidth )
            {
                gridbitmap?.Dispose();
                gridbitmap = new Bitmap(dgv.RowHeaderWidth + dgv.CellBorderWidth, drawndepth);
            }

            using (Graphics gr = Graphics.FromImage(gridbitmap))
            {
                gr.Clear(Color.Transparent);

                using (Brush b = new SolidBrush(dgv.CellBorderColor))
                { 
                    using (Pen p = new Pen(b, dgv.CellBorderWidth))
                    {
                        int vpos = 0;

                        for (var rowno = firstline; rowno < dgv.Rows.Count && vpos < gridbitmap.Height; rowno++)
                        {
                            var row = dgv.Rows[rowno];

                            if (dgv.CellBorderWidth > 0)
                            {
                                gr.DrawLine(p, 0, vpos, headerwidth, vpos);       // draw a line across the top
                                vpos += dgv.CellBorderWidth;
                            }

                            Rectangle area = new Rectangle(dgv.CellBorderWidth, vpos, dgv.RowHeaderWidth, row.Height);
                            row.Paint(gr, area);
                            dgv.UserPaintRowHeaders?.Invoke(row, gr, area);
  
                            vpos += row.Height;
                        }

                        if (dgv.CellBorderWidth > 0)
                        {
                            gr.DrawLine(p, 0, vpos, headerwidth, vpos);       // draw a line across the bottom
                            gr.DrawLine(p, 0, 0, 0, vpos);      // and one on the left
                        }
                    }
                }
            }

            Invalidate();
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            if (gridbitmap != null)
            {
                //using (Brush b = new SolidBrush(Color.Red))   gr.FillRectangle(b, ClientRectangle);

                Rectangle drawarea = new Rectangle(0, 0, gridbitmap.Width, gridbitmap.Height - yoffset);
                gr.DrawImage(gridbitmap, drawarea, 0, yoffset, drawarea.Width, drawarea.Height, GraphicsUnit.Pixel);
            }
        }

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            GLDataGridView dgv = Parent as GLDataGridView;

            if (dragging >= 0)      // row height
            {
                dgv.Rows[dragging].Height = e.Location.Y - draggingstart;
            }
            else if ( dragging == -2)   // header width
            {
                dgv.RowHeaderWidth = e.Location.X;
            }
            else if ( selectionstart != -1 )        // multi selection of line
            {
                if (!dgv.AllowUserToSelectMultipleRows)     // disable drag
                    return;

                if (e.Location.Y >= 0 && e.Location.Y <= ClientHeight)      // if within window, no scroll
                    autoscroll.Stop();
                else if (!autoscroll.Running)
                    autoscroll.Start(50, 200);                              // else autoscroll

                UpdateSelection(e.Location);
                lastmousemove = e.Location;
            }
            else
            {
                if (dgv.AllowUserToResizeColumns && e.Location.X >= Width + leftmargin)
                {
                    Cursor = GLWindowControl.GLCursorType.EW;
                    return;
                }

                GLDataGridView.RowColPos g;
                if (dgv.AllowUserToResizeRows && (g = contentpanel.GridRowCol(e.Location)) != null && g.Location.Y >= dgv.Rows[g.Row].Height - bottommargin)
                {
                    Cursor = dgv.Rows[g.Row].AutoSize == false ? GLWindowControl.GLCursorType.NS : GLWindowControl.GLCursorType.Normal;
                    return;
                }

                Cursor = GLWindowControl.GLCursorType.Normal;
            }
        }

         protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                GLDataGridView dgv = Parent as GLDataGridView;

                if (dgv.AllowUserToResizeColumns && e.Location.X >= Width + leftmargin)
                {
                    dragging = -2;
                    return;
                }

                var g = contentpanel.GridRowCol(e.Location);

                if (g != null)
                {
                    //System.Diagnostics.Debug.WriteLine($"Grid {g.Row} {g.Column} {g.Location}");
                    if (dgv.AllowUserToResizeRows && g.Location.Y >= dgv.Rows[g.Row].Height - bottommargin)
                    {
                        dragging = g.Row;
                        draggingstart = e.Location.Y - g.Location.Y;       // compute where top line, based on location, would be
                    }
                    else if (dgv.AllowUserToSelectRows)
                    {
                        //System.Diagnostics.Debug.WriteLine($"Selection start on {g.Row}");
                        bool itwason = dgv.Rows[g.Row].Selected;

                        dgv.ClearSelection();

                        if (!itwason)
                        {
                            lastselectionstart = lastselectionend = selectionstart = g.Row;
                            dgv.Rows[selectionstart].Selected = true;
                        }
                    }
                }
            }
        }

        protected override void OnMouseUp(GLMouseEventArgs e)
        {
            base.OnMouseUp(e);
            autoscroll.Stop();
            lastselectionend = selectionstart = lastselectionstart = dragging = -1;
        }

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);

            GLDataGridView dgv = Parent as GLDataGridView;
            var g = contentpanel.GridRowCol(e.Location);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                if (dragging == -1 && lastselectionstart == lastselectionend)      // if not dragging sizing or area
                {
                    if (g != null)
                    {
                        //System.Diagnostics.Debug.WriteLine($"Click valid  {g.Row}");
                        MouseClickRowHeader(g.Row, e);
                    }
                }
            }
            else if ( e.Button == GLMouseEventArgs.MouseButtons.Right)
            {
                if (g != null && dgv.ContextPanelRowHeaders != null)
                {
                    g.Column = -1;
                    dgv.ContextPanelRowHeaders.Show(FindDisplay(), e.ScreenCoord, opentag: g);
                }
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            GLDataGridView dgv = Parent as GLDataGridView;
            dgv.Scroll(e.Delta);
        }

        private void UpdateSelection(Point p)
        {
            var g = contentpanel.GridRowCol(p);

            if (g != null)
            {
                GLDataGridView dgv = Parent as GLDataGridView;

                int minrow = ObjectExtensionsNumbersBool.Min(lastselectionstart, lastselectionend, g.Row);
                int maxrow = ObjectExtensionsNumbersBool.Max(lastselectionstart, lastselectionend, g.Row);

                for (int i = minrow; i <= maxrow; i++)
                    dgv.Rows[i].Selected = g.Row < selectionstart ? i >= g.Row && i <= selectionstart : i >= selectionstart && i <= g.Row;

                lastselectionstart = selectionstart;
                lastselectionend = g.Row;

               // System.Diagnostics.Debug.WriteLine($"Selection {lastselectionstart}..{selectionstart}..{lastselectionend}");
            }
        }

        private Bitmap gridbitmap = null;
        private int yoffset = 0;

        private int dragging = -1;              // grid nos
        private int draggingstart = -1;         // Y start

        PolledTimer autoscroll = new PolledTimer();
        Point lastmousemove;
        private int selectionstart = -1;        // real row numbers
        private int lastselectionstart = -1; 
        private int lastselectionend = -1;   

        private const int bottommargin = 4;
        private const int leftmargin = -4;
        public GLDataGridViewContentPanel contentpanel { get; set; }
    }
}
