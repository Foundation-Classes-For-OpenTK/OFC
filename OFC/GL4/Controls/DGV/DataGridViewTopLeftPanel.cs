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

// Internal class for DGV, no documentation needed
#pragma warning disable 1591

namespace GLOFC.GL4.Controls
{
    public class GLDataGridViewTopLeftHeaderPanel : GLPanel
    {
        public Action<GLMouseEventArgs> MouseClickColumnHeader;            

        public GLDataGridViewTopLeftHeaderPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultScrollPanelBackColor;
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            if (dgv.UpperLeftBackColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(dgv.UpperLeftBackColor))
                {
                    gr.FillRectangle(b, new Rectangle(0,0,ClientWidth,ClientHeight));
                }
            }

            if (dgv.CellBorderWidth > 0)
            {
                using (Brush b = new SolidBrush(dgv.CellBorderColor))
                {
                    using (Pen p = new Pen(b, dgv.CellBorderWidth))
                    {
                        gr.DrawLine(p, 0, 0, ClientWidth-1, 0);       // draw a line across the top
                        gr.DrawLine(p, 0, 0, 0, ClientHeight-1);
                    }
                }
            }

            if ( dgv.UserPaintTopLeftHeader != null)
            {
                Rectangle area = new Rectangle(dgv.CellBorderWidth, dgv.CellBorderWidth, ClientWidth - 2 * dgv.CellBorderWidth, ClientHeight - 2 * dgv.CellBorderWidth);
                dgv.UserPaintTopLeftHeader(gr, area);
            }
        }

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            GLDataGridView dgv = Parent as GLDataGridView;

            if (dragging == 0)
            {
                dgv.RowHeaderWidth = e.Location.X;
            }
            else if (dragging == 1)
            {
                dgv.ColumnHeaderHeight = e.Location.Y;
            }
            else
            {
                Cursor = (e.Location.X >= Width + leftmargin && dgv.AllowUserToResizeColumns) ? GLWindowControl.GLCursorType.EW :
                         (e.Location.Y >= Height - bottommargin && dgv.AllowUserToResizeColumnHeight) ? GLWindowControl.GLCursorType.NS :
                         GLWindowControl.GLCursorType.Normal;
            }
            return;
        }

        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                GLDataGridView dgv = Parent as GLDataGridView;
                if (e.Location.X >= Width + leftmargin && dgv.AllowUserToResizeColumns)
                {
                    dragging = 0;
                }
                else if (e.Location.Y >= Height - bottommargin && dgv.AllowUserToResizeColumnHeight)
                {
                    dragging = 1;
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
                     MouseClickColumnHeader(e);
                }
            }
            else if (e.Button == GLMouseEventArgs.MouseButtons.Right)
            {
                GLDataGridView dgv = Parent as GLDataGridView;
                if (dgv.ContextMenuColumnHeaders != null)
                {
                    dgv.ContextMenuColumnHeaders.Show(FindDisplay(), e.ScreenCoord, opentag: new GLDataGridView.RowColPos() { Column = -1, Row = -1, Location = e.Location });
                }
            }
        }

        private int dragging = -1;
        private const int leftmargin = -4;
        private const int bottommargin = 4;

    }
}
