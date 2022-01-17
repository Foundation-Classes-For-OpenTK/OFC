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
    // 

    /// <summary>
    /// Data grid view cell with a button.
    /// Inherited from GLButton. Note most of the GLBaseControl functionality GLButton inherits is not applicable and should not be used.
    /// The mouse call backs are implemented (MouseClick etc)
    /// </summary>

    public class GLDataGridViewCellButton : GLButton, GLDataGridViewCell
    {
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.RowParent"/>
        public GLDataGridViewRow RowParent { get; set; }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Index"/>
        public int Index { get; set; }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Style"/>
        public GLDataGridViewCellStyle Style { get { return style; } }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Changed"/>
        public Action<GLDataGridViewCell, bool> Changed { get; set; }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.SelectionChanged"/>
        public Action<GLDataGridViewCell> SelectionChanged { get; set; }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Selectable"/>
        public bool Selectable { get; set; } = false;

        /// <summary> Padding, assign to Style.Padding </summary>
        public new PaddingType Padding { get { return Style.Padding; } set { Style.Padding = value; } }     // override padding back to style.padding
        /// <summary> Margin, assign to Style.Margin </summary>
        public new MarginType Margin { get { return base.Margin; } set { throw new NotImplementedException(); } }     // prevent margin

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Selected"/>
        public bool Selected { get { return selected; } set { if (value != selected && Selectable) { selected = value; SelectionChanged?.Invoke(this); } } }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.SelectedNI"/>
        public bool SelectedNI { get { return selected; } set { if ( Selectable) selected = value; } }

        /// <summary> Constructor with location and text </summary>
        public GLDataGridViewCellButton(Rectangle location, string text) : base("DGVBut", location, text)
        {
        }

        /// <summary> Default Constructor</summary>
        public GLDataGridViewCellButton() : base("DGVBut", DefaultWindowRectangle,"")
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.CompareTo(GLDataGridViewCell)"/>
        public int CompareTo(GLDataGridViewCell other)
        {
            return -1;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Paint(Graphics, Rectangle)"/>
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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.PerformAutoSize(int)"/>
        public Size PerformAutoSize(int width)
        {
            return Size;
        }

        private protected void PaintBack(Graphics gr, Rectangle area)
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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellDown(GLMouseEventArgs)"/>
        public virtual void OnMouseCellDown(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Mouse down cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            if (IsOver(e))
            {
                MouseButtonsDown = e.Button;
                Changed?.Invoke(this, false);
                OnMouseDown(e);
            }
            e.Handled = true;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellUp(GLMouseEventArgs)"/>
        public virtual void OnMouseCellUp(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Mouse up cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            if (MouseButtonsDown != GLMouseEventArgs.MouseButtons.None)
            {
                MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;
                Changed?.Invoke(this, false);
                OnMouseUp(e);
            }
            e.Handled = true;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellEnter(GLMouseEventArgs)"/>
        public virtual void OnMouseCellEnter(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Enter cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            bool newhover = IsOver(e);
            if (newhover != Hover)
            {
                Hover = newhover;
                Changed?.Invoke(this, false);
                OnMouseEnter(e);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellLeave(GLMouseEventArgs)"/>
        public virtual void OnMouseCellLeave(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Leave cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
            if ( Hover )
            {
                Hover = false;
                Changed?.Invoke(this, false);
                OnMouseLeave(e);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellMove(GLMouseEventArgs)"/>

        public virtual void OnMouseCellMove(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Move in cell {RowParent.Index} {Index} {e.WindowLocation} {e.ScreenCoord} {e.Bounds} {e.BoundsLocation} {e.Location}");
            bool newhover = IsOver(e);
            if (newhover != Hover)
            {
                Hover = newhover;
                Changed?.Invoke(this, false);
                OnMouseMove(e);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellClick(GLMouseEventArgs)"/>
        public virtual void OnMouseCellClick(GLMouseEventArgs e)
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

        private GLDataGridViewCellStyle style = new GLDataGridViewCellStyle();
        private bool selected;
    }
}
