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

namespace GLOFC.GL4.Controls
{
    // beware using interfaces GLButton exposes - most of them won't work

    public class GLDataGridViewCellButton : GLButton, GLDataGridViewCell
    {
        public GLDataGridViewRow RowParent { get; set; }
        public int Index { get; set; }
        public GLDataGridViewCellStyle Style { get { return style; } }
        public Action<GLDataGridViewCell, bool> Changed { get; set; }        // changed, and it affects the size if bool = true
        public Action<GLDataGridViewCell> SelectionChanged { get; set; }
        public bool Selectable { get; set; } = false;

        public new Padding Padding { get { return Style.Padding; } set { Style.Padding = value; } }     // override padding back to style.padding
        public new Margin Margin { get { return base.Margin; } set { throw new NotImplementedException(); } }     // prevent margin


        public bool Selected { get { return selected; } set { if (value != selected && Selectable) { selected = value; SelectionChanged?.Invoke(this); } } }
        public bool SelectedNI { get { return selected; } set { if ( Selectable) selected = value; } }

        public GLDataGridViewCellButton(Rectangle location, string text) : base("DGVBut",location,text)
        {
        }

        public int CompareTo(GLDataGridViewCell other)
        {
            return -1;
        }

        public void Paint(Graphics gr, Rectangle area)
        {
            PaintBack(gr, area);

            Rectangle drawarea = DrawArea(area);
            gr.SetClip(drawarea);   // set graphics to the clip area
            gr.TranslateTransform(drawarea.X,drawarea.Y);   // move to client 0,0
            Paint(gr);
            gr.ResetClip();
            gr.ResetTransform();
        }

        public Size PerformAutoSize(int width)
        {
            return Size;
        }

        protected void PaintBack(Graphics gr, Rectangle area)
        {
            if (selected)
            {
                using (Brush b = new SolidBrush(style.SelectedColor))
                {
                    gr.FillRectangle(b, area);
                }
            }
            else if (style.BackColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(style.BackColor))
                {
                    gr.FillRectangle(b, area);
                }
            }
        }

        public virtual void OnMouseDownCell(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Mouse down cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            if (IsOver(e))
            {
                MouseButtonsDown = e.Button;
                Changed?.Invoke(this, false);
            }
            e.Handled = true;
        }
        public virtual void OnMouseUpCell(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Mouse up cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            if (MouseButtonsDown != GLMouseEventArgs.MouseButtons.None)
            {
                MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;
                Changed?.Invoke(this, false);
            }
            e.Handled = true;
        }

        public virtual void OnMouseEnterCell(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Enter cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            bool newhover = IsOver(e);
            if (newhover != Hover)
            {
                Hover = newhover;
                Changed?.Invoke(this, false);
            }
        }
        public virtual void OnMouseLeaveCell(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Leave cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            if ( Hover )
            {
                Hover = false;
                Changed?.Invoke(this, false);
            }
        }
        public virtual void OnMouseMoveCell(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Move in cell {RowParent.Index} {Index} {e.WindowLocation} {e.ScreenCoord} {e.Bounds} {e.BoundsLocation} {e.Location}");
            bool newhover = IsOver(e);
            if (newhover != Hover)
            {
                Hover = newhover;
                Changed?.Invoke(this, false);
            }
        }
        public virtual void OnMouseClickCell(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Click in cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");

            if ( IsOver(e))
            {
                e.Handled = true;
                OnMouseClick(e);

            }
        }

        private bool IsOver(GLMouseEventArgs e)
        {
            Rectangle drawarea = DrawArea(e.Bounds);
            Point point = new Point(e.BoundsLocation.X + e.Bounds.X, e.BoundsLocation.Y + e.Bounds.Y);
            bool inside = drawarea.Contains(point);
            //System.Diagnostics.Debug.WriteLine($"..Draw area {point} {drawarea} {inside}");
            return inside;
        }

        private Rectangle DrawArea(Rectangle area)
        {
            area = new Rectangle(area.Left + Style.Padding.Left, area.Top + Style.Padding.Top, area.Width - Style.Padding.TotalWidth, area.Height - Style.Padding.TotalHeight);
            var drawarea = Style.ContentAlignment.ImagePositionFromContentAlignment(area, Size, true, true);
            return drawarea;
        }

        protected GLDataGridViewCellStyle style = new GLDataGridViewCellStyle();
        protected bool selected;
    }
}
