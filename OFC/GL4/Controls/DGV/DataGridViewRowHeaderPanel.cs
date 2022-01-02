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

using GLOFC.Timers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
                if (lastselectionend < dgv.Rows.Count - 1)          // more selection down one and select
                {
                    lastselectionend++;
                    dgv.Rows[lastselectionend].Selected = true;
                }
                //System.Diagnostics.Debug.WriteLine($"First line {dgv.FirstDisplayIndex} last complete {dgv.LastCompleteLine()}");
                if ( dgv.LastCompleteLine() < dgv.Rows.Count-1)     // and scroll to the end, until all lines are on screen
                    dgv.FirstDisplayIndex++;
            };
        }

        public void Redraw(int yoffset)
        {
            this.yoffset = yoffset;
            Invalidate();
        }

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
            gridbitmapfirstline = firstline;

            GLDataGridView dgv = Parent as GLDataGridView;

            gridrowoffsets.Clear();

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

                        for (var rowno = gridbitmapfirstline; rowno < dgv.Rows.Count && vpos < gridbitmap.Height; rowno++)
                        {
                            var row = dgv.Rows[rowno];

                            gridrowoffsets.Add(vpos);       // this row at this border line offset

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

                        gridrowoffsets.Add(vpos);       // add final row vpos for searching purposes

                        if (dgv.CellBorderWidth > 0)
                        {
                            gr.DrawLine(p, 0, vpos, headerwidth, vpos);       // draw a line across the bottom
                            gr.DrawLine(p, 0, 0, 0, vpos);      // and one on the left
                        }

                    }
                }
            }
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


        public new Action<int, GLMouseEventArgs> MouseClick;

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            GLDataGridView dgv = Parent as GLDataGridView;

            if (e.Location.Y <= ClientHeight)
                autoscroll.Stop();

            if (dragging >= 0)      // row height
            {
                int y = yoffset + e.Location.Y;
                dgv.Rows[gridbitmapfirstline + dragging].Height = y - gridrowoffsets[dragging];
            }
            else if ( dragging == -2)   // header width
            {
                dgv.RowHeaderWidth = e.Location.X;
            }
            else if ( selectionstart != -1 )        // multi selection of line
            {
                if (!dgv.AllowUserToSelectMultipleRows)     // disable drag
                    return;

                if ( e.Location.Y > ClientHeight)       // if past end, need to autoscroll
                {
                    //System.Diagnostics.Debug.WriteLine($"First line {dgv.FirstDisplayIndex} last complete {dgv.LastCompleteLine()}");
                    autoscroll.Start(50, 200);
                }

                var g = GridRow(e.Location);

                if ( g != null )
                {
                    int row = g.Item1 + gridbitmapfirstline;

                    for (int i = lastselectionstart; i <= lastselectionend; i++)
                        dgv.Rows[i].Selected = row < selectionstart ? i >= row && i <= selectionstart : i >= selectionstart && i <= row;

                    if ( row < selectionstart )
                    {
                        lastselectionstart = row;
                        lastselectionend = selectionstart;
                    }
                    else
                    {
                        lastselectionstart = selectionstart;
                        lastselectionend = row;
                    }

                    //System.Diagnostics.Debug.WriteLine($"Selection {lastselectionstart}..{selectionstart}..{lastselectionend}");
                }
            }
            else
            {
                if (dgv.AllowUserToResizeColumns && e.Location.X >= Width + leftmargin)
                {
                    Cursor = GLCursorType.EW;
                    return;
                }

                Tuple<int, int> g;
                if (dgv.AllowUserToResizeRows && (g= GridRow(e.Location)) != null && g.Item2 < bottommargin)
                {
                    Cursor = dgv.Rows[g.Item1 + gridbitmapfirstline].AutoSize == false ? GLCursorType.NS : GLCursorType.Normal;
                    return;
                }

                Cursor = GLCursorType.Normal;
            }
        }

        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            GLDataGridView dgv = Parent as GLDataGridView;

            if (dgv.AllowUserToResizeColumns && e.Location.X >= Width + leftmargin)
            {
                dragging = -2;
                return;
            }

            var g = GridRow(e.Location);

            if (g != null)
            {
                if (dgv.AllowUserToResizeRows && g.Item2 < bottommargin)
                {
                    dragging = g.Item1;
                    return;
                }
                else if (dgv.AllowUserToSelectRows)
                {
                    //System.Diagnostics.Debug.WriteLine($"Selection start on {g.Item1}");
                    int row = g.Item1 + gridbitmapfirstline;

                    bool itwason = dgv.Rows[row].Selected;

                    dgv.ClearSelection();
                    if (!itwason)
                    {
                        lastselectionstart = lastselectionend = selectionstart = row;
                        dgv.Rows[selectionstart].Selected = true;
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
            if ( dragging == -1 && lastselectionend == -1 )
            {
                var g = GridRow(e.Location);
                System.Diagnostics.Debug.WriteLine($"Click valid  {g.Item1}");

                if (g != null )
                {
                    MouseClickRowHeader(g.Item1 + gridbitmapfirstline, e);
                }
            }
        }

        private Tuple<int,int> GridRow(Point p)
        {
            if (gridrowoffsets.Count > 0)
            {
                int y = yoffset + p.Y;
                int gridrow = gridrowoffsets.FindLastIndex(a => a < y);
                if (gridrow >= 0 && gridrow < gridrowoffsets.Count - 1)       // last entry is end, ignore
                {
                    int off = gridrowoffsets[gridrow + 1] - y;
                    return new Tuple<int, int>(gridrow, off);
                }
            }

            return null;
        }

        Timer autoscroll = new Timer();

        private Bitmap gridbitmap = null;
        private int yoffset = 0;
        private int gridbitmapfirstline;
        private List<int> gridrowoffsets = new List<int>();     // cell boundary pixel upper of cell line on Y

        private int dragging = -1;              // grid nos
        private int selectionstart = -1;        // real row numbers
        private int lastselectionstart = -1;      // real row numbers
        private int lastselectionend = -1;      // real row numbers
        private const int bottommargin = 4;
        private const int leftmargin = -4;
    }
}
