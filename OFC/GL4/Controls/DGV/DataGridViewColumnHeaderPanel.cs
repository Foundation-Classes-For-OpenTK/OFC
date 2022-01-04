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

using System;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    public class GLDataGridViewColumnHeaderPanel : GLPanel
    {
        public Action<int, GLMouseEventArgs> MouseClickColumnHeader;                // col>=0

        public int HorzScroll { get { return horzscroll; } set { horzscroll = value; Invalidate(); } }

        public GLDataGridViewColumnHeaderPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            if (!dgv.ColumnHeaderEnable || dgv.Columns.Count == 0)
                return;

            int vpos = 0;
            using (Brush b = new SolidBrush(dgv.CellBorderColor))
            {
                using (Pen p = new Pen(b, dgv.CellBorderWidth))
                {
                    int colright = dgv.ColumnPixelWidth-1;

                    gr.DrawLine(p, 0, vpos, colright, vpos);                  // draw across horz top
                    vpos += dgv.ColumnHeaderHeight + dgv.CellBorderWidth;

                    int hpos = -horzscroll;

                    foreach (var c in dgv.Columns)  // horz part, col headers
                    {
                        //System.Diagnostics.Debug.WriteLine($"Paint | {hpos}");
                        gr.DrawLine(p, hpos, 0, hpos, vpos);            // draw verticle
                        hpos += dgv.CellBorderWidth;

                        Rectangle area = new Rectangle(hpos, dgv.CellBorderWidth, c.Width, dgv.ColumnHeaderHeight);
                        c.Paint(gr, area);
                        dgv.UserPaintColumnHeaders?.Invoke(c, gr, area);

                        hpos += c.Width;
                    }

                    gr.DrawLine(p, hpos, 0, hpos, vpos);        // last verticle
                }
            }
        }

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            GLDataGridView dgv = Parent as GLDataGridView;

            int xoff = e.Location.X + HorzScroll;

            if (dragging == 0)
            {
                dgv.RowHeaderWidth = xoff + initialrowheaderwidth;
            }
            else if (dragging == -2)
            {
                dgv.ColumnHeaderHeight = e.Location.Y;
            }
            else if (dragging > 0)
            {
                int colx = xoff - dgv.ColumnPixelLeft(dragging - 1);
                dgv.SetColumnWidth(dragging-1,colx);
            }
            else
            {
                var over = Over(e.Location);
                Cursor = (over != null && over.Item1 == ClickOn.Divider) ? GLCursorType.EW :
                         (over != null && over.Item1 == ClickOn.Height) ? GLCursorType.NS :
                          GLCursorType.Normal;
            }
            return;
        }
        protected override void OnMouseLeave(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);
            dragging = -1;
            Cursor = GLCursorType.Normal;
        }

        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                var over = Over(e.Location);
                if (over != null)
                {
                    GLDataGridView dgv = Parent as GLDataGridView;

                    if (over.Item1 == ClickOn.Divider && dgv.AllowUserToResizeColumns)
                    {
                        dragging = over.Item2;
                        initialrowheaderwidth = dgv.RowHeaderWidth;
                    }
                    else if (over.Item1 == ClickOn.Height && dgv.AllowUserToResizeColumnHeight)
                    {
                        dragging = -2;
                    }
                }
            }
        }

        protected override void OnMouseUp(GLMouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = -1;
        }

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                if (dragging == -1)
                {
                    var over = Over(e.Location);
                    if (over != null && over.Item1 != ClickOn.Divider)
                        MouseClickColumnHeader(over.Item2, e);
                }
            }
            else if (e.Button == GLMouseEventArgs.MouseButtons.Right)
            {
                GLDataGridView dgv = Parent as GLDataGridView;
                if (dgv.ContextPanelColumnHeaders != null)
                {
                    var over = Over(e.Location);

                    if (over != null && over.Item1 == ClickOn.Header)
                    {
                        dgv.ContextPanelColumnHeaders.Show(FindDisplay(), e.ScreenCoord, opentag: new GLDataGridView.RowColPos() { Column = over.Item2, Row = -1, Location = over.Item3 });
                    }
                }
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            GLDataGridView dgv = Parent as GLDataGridView;
            dgv.Scroll(e.Delta);
        }

        private enum ClickOn { Divider, Header, Height }

        private Tuple<ClickOn, int, Point> Over(Point p)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            int xoff = p.X + HorzScroll;

            if (dgv.AllowUserToResizeColumns)
            {
                foreach (var c in dgv.Columns)  // horz part, col headers
                {
                    int hoff = xoff - dgv.ColumnPixelLeft(c.Index);

                    // System.Diagnostics.Debug.WriteLine($"loc {p} col {c.Index} {c.HeaderBounds} {hoff}");
                    if (hoff >= leftmargin && hoff <= rightmargin)
                    {
                        // System.Diagnostics.Debug.WriteLine($"Header mouse over divider {c.Index} {p}");
                        return new Tuple<ClickOn, int, Point>(ClickOn.Divider, c.Index, Point.Empty);
                    }
                }

                if (dgv.Columns.Count > 0 && dgv.ColumnFillMode != GLDataGridView.ColFillMode.FillWidth)
                {
                    int hoff = xoff - dgv.ColumnPixelRight(dgv.Columns.Count - 1);
                    if (hoff >= leftmargin && hoff <= rightmargin)
                    {
                        return new Tuple<ClickOn, int,Point>(ClickOn.Divider, dgv.Columns.Count, new Point(hoff,p.Y));
                    }
                }
            }

            if (p.Y >= Height - bottommargin && dgv.AllowUserToResizeColumnHeight)
                return new Tuple<ClickOn, int,Point>(ClickOn.Height, -1, Point.Empty);

            foreach (var c in dgv.Columns)  // horz part, col headers
            {
                int left = dgv.ColumnPixelLeft(c.Index);
                if (xoff > left && xoff < left+c.Width)
                {
                   // System.Diagnostics.Debug.WriteLine($"Header mouse over {c.Index} {p}");
                    return new Tuple<ClickOn, int,Point>(ClickOn.Header, c.Index, new Point(xoff-left,p.Y));
                }
            }

            return null;
        }

        private int horzscroll = 0;
        private int dragging = -1;
        private int initialrowheaderwidth;
        private const int leftmargin = -4;
        private const int rightmargin = 2;
        private const int bottommargin = 4;

    }
}
