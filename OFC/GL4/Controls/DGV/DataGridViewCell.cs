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
    /// <summary>
    /// Data grid view cell interface
    /// </summary>
    public interface GLDataGridViewCell
    {
        /// <summary> Row Parent of cell </summary>
        GLDataGridViewRow RowParent { get; set; }
        /// <summary> Index of cell in row </summary>
        int Index { get; set; }
        /// <summary> Style of cell. Override to set individual style properties, else style comes from row's DefaultCellStyle or data grid view DefaultCellStyle </summary>
        GLDataGridViewCellStyle Style { get; }
        /// <summary> Is Selected</summary>
        bool Selected { get; set; }
        /// <summary> Is Selecable</summary>
        bool Selectable { get; set; }
        /// <summary> Is Selected (special interface for internal use)</summary>
        bool SelectedNI { get; set; }
        /// <summary> User tag</summary>
        object Tag { get; set; }
        /// <summary> Callback to indicate to owner cell has changed. Used internally. </summary>
        Action<GLDataGridViewCell, bool> Changed { get; set; }        
        /// <summary> Callback to indicate to owner that selection has changed. Used internally. </summary>
        Action<GLDataGridViewCell> SelectionChanged { get; set; }
        /// <summary> Call to Paint. Used internally. </summary>
        void Paint(Graphics gr, Rectangle area);
        /// <summary> Call to autosize. Used internally.</summary>
        Size PerformAutoSize(int width);
        /// <summary> Compare cell with another and return -1 less than, 0 equal, +1 greater than other. Used during sort</summary>
        int CompareTo(GLDataGridViewCell other); 

        /// <summary> Cell mouse down. </summary>
        void OnMouseCellDown(GLMouseEventArgs e);
        /// <summary> Cell mouse up </summary>
        void OnMouseCellUp(GLMouseEventArgs e);
        /// <summary> Cell Enter</summary>
        void OnMouseCellEnter(GLMouseEventArgs e);
        /// <summary> Cell Leave </summary>
        void OnMouseCellLeave(GLMouseEventArgs e);
        /// <summary> Move in cell
        /// GLMouseEventArgs will have Bounds set to cell bounds, BoundsLocation set to the top left of the cell, and Location set to the point within the cell.
        /// </summary>
        void OnMouseCellMove(GLMouseEventArgs e);
        /// <summary> Click on cell</summary>
        void OnMouseCellClick(GLMouseEventArgs e);
    }

    /// <summary>
    /// Base class for various cell types
    /// </summary>
    public abstract class GLDataGridViewCellBase
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
        public bool Selectable { get; set; } = true;
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.SelectedNI"/>
        public bool SelectedNI { get { return selected; } set { if ( Selectable) selected = value; } }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Tag"/>
        public object Tag { get; set; }

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
            //System.Diagnostics.Debug.WriteLine($"Enter cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellUp(GLMouseEventArgs)"/>
        public virtual void OnMouseCellUp(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Leave cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellEnter(GLMouseEventArgs)"/>
        public virtual void OnMouseCellEnter(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Enter cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellLeave(GLMouseEventArgs)"/>
        public virtual void OnMouseCellLeave(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"Leave cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellMove(GLMouseEventArgs)"/>
        public virtual void OnMouseCellMove(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Move in cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.OnMouseCellClick(GLMouseEventArgs)"/>
        public virtual void OnMouseCellClick(GLMouseEventArgs e)
        {
           // System.Diagnostics.Debug.WriteLine($"Click in cell {RowParent.Index} {Index} {e.Bounds} {e.BoundsLocation} {e.Location}");
        }

        private protected GLDataGridViewCellStyle style = new GLDataGridViewCellStyle();
        private protected bool selected;
    }

    /// <summary>
    /// Data grid view cell with text content
    /// </summary>
    public class GLDataGridViewCellText : GLDataGridViewCellBase, GLDataGridViewCell
    {
        /// <summary> Text value</summary>
        public string Value { get { return text; } set { if (value != text) { text = value; Changed?.Invoke(this, true); } } }
        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Selected"/>
        public bool Selected { get { return selected; } set { if (value != selected && Selectable) { selected = value; SelectionChanged?.Invoke(this); } } }

        /// <summary> Default constructor</summary>
        public GLDataGridViewCellText() { }
        /// <summary> Constructor with text</summary>
        public GLDataGridViewCellText(string text) { this.text = text; }

        #region Implementation

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Paint(Graphics, Rectangle)"/>

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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.PerformAutoSize(int)"/>
        public Size PerformAutoSize(int width)
        {
            using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(Style.ContentAlignment))
            {
                fmt.FormatFlags = Style.TextFormat;
                var size = GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(text, Style.Font, fmt, new Size(width - Style.Padding.TotalWidth, 20000));
                return new Size((int)(size.Width + 0.99F) + Style.Padding.TotalWidth, (int)(size.Height + 0.99F) + Style.Padding.TotalHeight);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.CompareTo(GLDataGridViewCell)"/>
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
    /// <summary>
    /// Data grid view cell with image content
    /// </summary>
    public class GLDataGridViewCellImage : GLDataGridViewCellBase, GLDataGridViewCell
    {
        /// <summary> Image to display </summary>
        public Image Image { get { return image; } set { if (value != image) { image = value; size = value.Size; Changed?.Invoke(this, true); } } }
        /// <summary> Size of image, allows image scaling </summary>
        public Size Size { get { return size; } set { if (value != size) { size = value; Changed?.Invoke(this, true); } } }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Selected"/>
        public bool Selected { get { return selected; } set { if (value != selected) { selected = value; SelectionChanged?.Invoke(this); } } }

        /// <summary> Default constructor</summary>
        public GLDataGridViewCellImage() { }
        /// <summary> Constructor with Image</summary>
        public GLDataGridViewCellImage(Image t) { image = t; size = image.Size; }

        #region Implementation

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.Paint(Graphics, Rectangle)"/>
        public void Paint(Graphics gr, Rectangle area)
        {
            PaintBack(gr, area);

            area = new Rectangle(area.Left + Style.Padding.Left, area.Top + Style.Padding.Top, area.Width - Style.Padding.TotalWidth, area.Height - Style.Padding.TotalHeight);

            Rectangle drawarea = Style.ContentAlignment.ImagePositionFromContentAlignment(area, size, true, true);
            gr.DrawImage(Image, drawarea, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.PerformAutoSize(int)"/>
        public Size PerformAutoSize(int width)
        {
            return new Size(size.Width + Style.Padding.TotalWidth, size.Height + Style.Padding.TotalHeight);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLDataGridViewCell.CompareTo(GLDataGridViewCell)"/>
        public int CompareTo(GLDataGridViewCell other)
        {
            return -1;
        }

        private Image image;
        private Size size;

        #endregion
    }
}
