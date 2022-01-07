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

namespace GLOFC.GL4.Controls
{
    public interface GLDataGridViewCell
    {
        public GLDataGridViewRow RowParent { get; set; }
        public int Index { get; set; }
        public GLDataGridViewCellStyle Style { get; }
        public bool Selected { get; set; }
        public bool Selectable { get; set; }
        public bool SelectedNI { get; set; }
        public Action<GLDataGridViewCell, bool> Changed { get; set; }        // changed, and it affects the size if bool = true
        public Action<GLDataGridViewCell> SelectionChanged { get; set; }
        public void Paint(Graphics gr, Rectangle area);
        public Size PerformAutoSize(int width);
        public int CompareTo(GLDataGridViewCell other); // -1 less than, 0 equal, +1 greater than other
        public void OnMouseDownCell(GLMouseEventArgs e);
        public void OnMouseUpCell(GLMouseEventArgs e);
        public void OnMouseEnterCell(GLMouseEventArgs e);
        public void OnMouseLeaveCell(GLMouseEventArgs e);
        public void OnMouseMoveCell(GLMouseEventArgs e);
        public void OnMouseClickCell(GLMouseEventArgs e);
        public object Tag { get; set; }
    }

    // common stuff useful for cells
    public abstract class GLDataGridViewCellBase
    {
        public GLDataGridViewRow RowParent { get; set; }
        public int Index { get; set; }
        public GLDataGridViewCellStyle Style { get { return style; } }
        public Action<GLDataGridViewCell, bool> Changed { get; set; }        // changed, and it affects the size if bool = true
        public Action<GLDataGridViewCell> SelectionChanged { get; set; }
        public bool Selectable { get; set; } = true;
        public bool SelectedNI { get { return selected; } set { if ( Selectable) selected = value; } }
        public object Tag { get; set; }

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
            //System.Diagnostics.Debug.WriteLine($"Enter cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        public virtual void OnMouseUpCell(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Leave cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        public virtual void OnMouseEnterCell(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Enter cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        public virtual void OnMouseLeaveCell(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Leave cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        public virtual void OnMouseMoveCell(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Move in cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        public virtual void OnMouseClickCell(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Click in cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }

        protected GLDataGridViewCellStyle style = new GLDataGridViewCellStyle();
        protected bool selected;
    }

    public class GLDataGridViewCellText : GLDataGridViewCellBase, GLDataGridViewCell
    {
        public string Value { get { return text; } set { if (value != text) { text = value; Changed?.Invoke(this, true); } } }
        public bool Selected { get { return selected; } set { if (value != selected && Selectable) { selected = value; SelectionChanged?.Invoke(this); } } }
        public GLDataGridViewCellText() { }
        public GLDataGridViewCellText(string t) { text = t; }

        #region Implementation

        public void Paint(Graphics gr, Rectangle area)
        {
            PaintBack(gr, area);

            area = new Rectangle(area.Left + Style.Padding.Left, area.Top + Style.Padding.Top, area.Width - Style.Padding.TotalWidth, area.Height - Style.Padding.TotalHeight);

            using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(Style.ContentAlignment))
            {
                fmt.FormatFlags = Style.TextFormat;
                //System.Diagnostics.Debug.WriteLine($"Draw {Text} {Enabled} {ForeDisabledScaling}");
                using (Brush textb = new SolidBrush(Style.ForeColor))
                {
                    gr.DrawString(text, Style.Font, textb, area, fmt);
                }
            }
        }
        public Size PerformAutoSize(int width)
        {
            using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(Style.ContentAlignment))
            {
                fmt.FormatFlags = Style.TextFormat;
                var size = BitMapHelpers.MeasureStringInBitmap(text, Style.Font, fmt, new Size(width - Style.Padding.TotalWidth, 20000));
                return new Size((int)(size.Width + 0.99F) + Style.Padding.TotalWidth, (int)(size.Height + 0.99F) + Style.Padding.TotalHeight);
            }
        }

        public int CompareTo(GLDataGridViewCell other)
        {
            if (other is GLDataGridViewCellText)
            {
                var otext = ((GLDataGridViewCellText)other).text;
                //System.Diagnostics.Debug.WriteLine($"compare {text} to {otext}");
                return text.CompareTo(otext);
            }
            else
                return -1;
        }

        private string text;

        #endregion
    }
    public class GLDataGridViewCellImage : GLDataGridViewCellBase, GLDataGridViewCell
    {
        public Image Image { get { return image; } set { if (value != image) { image = value; size = value.Size; Changed?.Invoke(this, true); } } }
        public Size Size { get { return size; } set { if (value != size) { size = value; Changed?.Invoke(this, true); } } }
        public bool Selected { get { return selected; } set { if (value != selected) { selected = value; SelectionChanged?.Invoke(this); } } }
        public GLDataGridViewCellImage() { }
        public GLDataGridViewCellImage(Image t) { image = t; size = image.Size; }

        #region Implementation

        public void Paint(Graphics gr, Rectangle area)
        {
            PaintBack(gr, area);

            area = new Rectangle(area.Left + Style.Padding.Left, area.Top + Style.Padding.Top, area.Width - Style.Padding.TotalWidth, area.Height - Style.Padding.TotalHeight);

            Rectangle drawarea = Style.ContentAlignment.ImagePositionFromContentAlignment(area, size, true, true);
            gr.DrawImage(Image, drawarea, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
        }

        public Size PerformAutoSize(int width)
        {
            return new Size(size.Width + Style.Padding.TotalWidth, size.Height + Style.Padding.TotalHeight);
        }

        public int CompareTo(GLDataGridViewCell other)
        {
            return -1;
        }

        private Image image;
        private Size size;

        #endregion
    }
}
