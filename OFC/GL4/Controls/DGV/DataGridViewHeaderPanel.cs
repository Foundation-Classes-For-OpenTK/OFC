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

    public class GLDataGridViewHeaderPanel : GLPanel
    {
        public Action<int, GLMouseEventArgs> MouseClickColumnHeader;                // -1 for top left cell

        public int HorzScroll { get { return horzscroll; } set { horzscroll = value; Invalidate(); } }

        public GLDataGridViewHeaderPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
        }

        private void DrawColumnHeaders(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            if (!dgv.ColumnHeaderEnable)
                return;

            int vpos = 0;
            if (dgv.CellBorderWidth > 0 && dgv.Columns.Count>0) 
            {
                using (Brush b = new SolidBrush(dgv.CellBorderColor))
                {
                    using (Pen p = new Pen(b, dgv.CellBorderWidth))
                    {
                        int colend = dgv.Columns.Last().HeaderBounds.Right;

                        if (dgv.ColumnHeaderEnable)     // line horz across top
                        {
                            //System.Diagnostics.Debug.WriteLine($"Paint - {vpos} to {gridbounds.Right}");
                            gr.DrawLine(p, 0, vpos, colend, vpos);
                            vpos += dgv.ColumnHeaderHeight + dgv.CellBorderWidth;
                        }

                        int hpos = -horzscroll;

                        if (dgv.RowHeaderEnable)      // horz part, row header
                        {
                            gr.DrawLine(p, hpos, 0, hpos, vpos);
                            hpos += dgv.RowHeaderWidth;
                            hpos += dgv.CellBorderWidth;
                        }

                        foreach (var c in dgv.Columns)  // horz part, col headers
                        {
                            //System.Diagnostics.Debug.WriteLine($"Paint | {hpos}");
                            gr.DrawLine(p, hpos, 0, hpos, vpos);
                            hpos += c.Width;
                            hpos += dgv.CellBorderWidth;

                            if (dgv.ColumnHeaderEnable)
                            {
                                if (dgv.RowHeaderEnable)
                                {
                                    var upperleftbounds = new Rectangle(dgv.CellBorderWidth-horzscroll, dgv.CellBorderWidth, dgv.RowHeaderWidth, dgv.ColumnHeaderHeight);

                                    if (dgv.UpperLeftStyle.BackColor != Color.Transparent)
                                    {
                                        using (Brush b2 = new SolidBrush(dgv.UpperLeftStyle.BackColor))
                                        {
                                            gr.FillRectangle(b2, upperleftbounds);
                                        }
                                    }
                                }

                                Rectangle area = new Rectangle(c.HeaderBounds.Left - horzscroll, c.HeaderBounds.Top, c.HeaderBounds.Width, c.HeaderBounds.Height);
                                if (dgv.UserPaintColumnHeaders != null)
                                    dgv.UserPaintColumnHeaders(c, gr, area);
                                else
                                    c.Paint(gr, area);


                            }
                        }

                        gr.DrawLine(p, hpos, 0, hpos, vpos);
                        vpos++;
                    }
                }
            }
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            DrawColumnHeaders(gr);
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
                dgv.RowHeaderWidth = xoff;
            }
            else if (dragging > 0)
            {
                dgv.SetColumnWidth(dragging-1,xoff - dgv.Columns[dragging - 1].HeaderBounds.Left);
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
                System.Diagnostics.Debug.WriteLine($"Drag start {over.Item2}");
                dragging = over.Item2;
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
                int hoff = xoff - c.HeaderBounds.Left;

               // System.Diagnostics.Debug.WriteLine($"loc {p} col {c.Index} {c.HeaderBounds} {hoff}");
                if (hoff >= -4 && hoff <= 2)
                {
                   // System.Diagnostics.Debug.WriteLine($"Header mouse over divider {c.Index} {p}");
                    return new Tuple<ClickOn, int>(ClickOn.Divider, c.Index);
                }
            }

            foreach (var c in dgv.Columns)  // horz part, col headers
            {
                if (xoff > c.HeaderBounds.Left && xoff < c.HeaderBounds.Right)
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

    }
}
