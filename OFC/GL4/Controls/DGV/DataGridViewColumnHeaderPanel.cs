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
        public Action<int, GLMouseEventArgs> MouseClickColumnHeader;                // -1 for top left cell

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

        public enum ClickOn { Divider, UpperLeft, Header }
        public new Action<ClickOn, GLMouseEventArgs> MouseClick;

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            GLDataGridView dgv = Parent as GLDataGridView;

            int xoff = e.Location.X + HorzScroll;

            if (dragging == 0)
            {
                dgv.RowHeaderWidth = xoff + initialrowheaderwidth;
            }
            else if (dragging > 0)
            {
                int colx = xoff - dgv.ColumnPixelLeft(dragging - 1);
                dgv.SetColumnWidth(dragging-1,colx);
            }
            else
            {
                var over = Over(e.Location);
                if (over != null && over.Item1 == ClickOn.Divider)
                {
                    Cursor = GLCursorType.EW;
                }
                else
                {
                    Cursor = GLOFC.GLCursorType.Normal;
                }
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
            var over = Over(e.Location);
            if (over != null && over.Item1 == ClickOn.Divider)
            {
                GLDataGridView dgv = Parent as GLDataGridView;
                System.Diagnostics.Debug.WriteLine($"Drag start {over.Item2}");
                dragging = over.Item2;
                initialrowheaderwidth = dgv.RowHeaderWidth;
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
            if ( dragging == -1 )
            {
                var over = Over(e.Location);
                if (over != null && over.Item1 != ClickOn.Divider)
                    MouseClickColumnHeader(over.Item2, e);
            }
        }

        private int horzscroll = 0;

        private Tuple<ClickOn, int> Over(Point p)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            int xoff = p.X + HorzScroll;
            foreach (var c in dgv.Columns)  // horz part, col headers
            {
                int hoff = xoff - dgv.ColumnPixelLeft(c.Index);

               // System.Diagnostics.Debug.WriteLine($"loc {p} col {c.Index} {c.HeaderBounds} {hoff}");
                if (hoff >= -4 && hoff <= 2)
                {
                   // System.Diagnostics.Debug.WriteLine($"Header mouse over divider {c.Index} {p}");
                    return new Tuple<ClickOn, int>(ClickOn.Divider, c.Index);
                }
            }

            foreach (var c in dgv.Columns)  // horz part, col headers
            {
                int left = dgv.ColumnPixelLeft(c.Index);
                if (xoff > left && xoff < left+c.Width)
                {
                   // System.Diagnostics.Debug.WriteLine($"Header mouse over {c.Index} {p}");
                    return new Tuple<ClickOn, int>(ClickOn.Header, c.Index);
                }
            }

            if (dgv.RowHeaderEnable && dgv.Columns.Count > 0 && xoff < dgv.RowHeaderWidth)
            {
                //System.Diagnostics.Debug.WriteLine($"Header mouse over upper left {p}");
                return new Tuple<ClickOn, int>(ClickOn.UpperLeft, -1);
            }
            return null;
        }

        private int dragging = -1;
        private int initialrowheaderwidth;

    }
}
