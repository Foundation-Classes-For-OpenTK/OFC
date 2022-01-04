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
    public abstract class GLDataGridViewCell
    {
        public GLDataGridViewRow Parent { get; set; }
        public int Index { get; set; }
        public GLDataGridViewCellStyle Style { get { return style; } }

        public bool Selected { get { return selected; } set { if (value != selected) { selected = value; SelectionChanged?.Invoke(this); } } }

        public GLDataGridViewCell()
        {
        }

        #region Implementation

        public Action<GLDataGridViewCell, bool> Changed { get; set; }        // changed, and it affects the size if bool = true
        public Action<GLDataGridViewCell> SelectionChanged { get; set; }
        public bool SelectedNI { get { return selected; } set { selected = value; } }
        public abstract void Paint(Graphics gr, Rectangle area);
        public abstract Size PerformAutoSize(int width);
        public abstract int CompareTo(GLDataGridViewCell other); // -1 less than, 0 equal, +1 greater than other

        private GLDataGridViewCellStyle style = new GLDataGridViewCellStyle();
        private bool selected;
        
        protected void PaintBack(Graphics gr, Rectangle area)
        {
            if (Selected)
            {
                using (Brush b = new SolidBrush(Style.SelectedColor))
                {
                    gr.FillRectangle(b, area);
                }
            }
            else if (Style.BackColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(Style.BackColor))
                {
                    gr.FillRectangle(b, area);
                }
            }
        }
        
        #endregion

    }

    public class GLDataGridViewCellText : GLDataGridViewCell
    {
        public string Value { get { return text; } set { if (value != text) { text = value; Changed?.Invoke(this, true); } } }
        public GLDataGridViewCellText() { }
        public GLDataGridViewCellText(string t) { text = t; }

        #region Implementation

        public override void Paint(Graphics gr, Rectangle area)
        {
            area = new Rectangle(area.Left + Style.Padding.Left, area.Top + Style.Padding.Top, area.Width - Style.Padding.TotalWidth, area.Height - Style.Padding.TotalHeight);

            PaintBack(gr, area);

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
        public override Size PerformAutoSize(int width)
        {
            using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(Style.ContentAlignment))
            {
                fmt.FormatFlags = Style.TextFormat;
                var size = BitMapHelpers.MeasureStringInBitmap(text, Style.Font, fmt, new Size(width - Style.Padding.TotalWidth, 20000));
                return new Size((int)(size.Width + 0.99F), (int)(size.Height + 0.99F));
            }
        }

        public override int CompareTo(GLDataGridViewCell other)
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
    public class GLDataGridViewCellImage : GLDataGridViewCell
    {
        public Image Image { get { return image; } set { if (value != image) { image = value; size = value.Size; Changed?.Invoke(this, true); } } }
        public Size Size { get { return size; }  set { if (value != size) { size = value; Changed?.Invoke(this, true); } } }
        public GLDataGridViewCellImage() { }
        public GLDataGridViewCellImage(Image t) { image = t; size = image.Size; }

        #region Implementation

        public override void Paint(Graphics gr, Rectangle area)
        {
            area = new Rectangle(area.Left + Style.Padding.Left, area.Top + Style.Padding.Top, area.Width - Style.Padding.TotalWidth, area.Height - Style.Padding.TotalHeight);

            PaintBack(gr, area);

            Rectangle drawarea = Style.ContentAlignment.ImagePositionFromContentAlignment(area, size, true, true);
            gr.DrawImage(Image, drawarea, 0,0, image.Width, image.Height, GraphicsUnit.Pixel);
        }

        public override Size PerformAutoSize(int width)
        {
            return size;
        }

        public override int CompareTo(GLDataGridViewCell other)
        {
            return -1;
        }

        private Image image;
        private Size size;

        #endregion
    }
}
