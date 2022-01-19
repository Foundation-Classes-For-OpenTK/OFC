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
    /// Data Grid View column
    /// </summary>

    public class GLDataGridViewColumn
    {
        /// <summary> Column Index </summary>
        public int Index { get { return colno; } }
        /// <summary> Parent data grid view </summary>
        public GLDataGridView Parent { get; set; }
        /// <summary> Header Text </summary>
        public string Text { get { return text; } set { text = value; Changed?.Invoke(this,false); } }
        /// <summary> Column width in pixels</summary>
        public int Width { get { return width; } set { if (value != width) { width = Math.Max(minwidth, value); Changed?.Invoke(this, true); } } }
        /// <summary> Column fill weight, for column fill FillWidth mode</summary>
        public float FillWidth { get { return fillwidth; } set { if (value != fillwidth) { fillwidth = value; Changed?.Invoke(this, true); } } }
        /// <summary> Minimum width in pixels </summary>
        public int MinimumWidth { get { return minwidth; } set { if (value != minwidth) { minwidth = value; Changed?.Invoke(this, true); } } }
        /// <summary> Header style. If not set, uses GLDataGridView.DefaultColumnHeaderStyle</summary>
        public GLDataGridViewCellStyle HeaderStyle { get { return headerstyle; } }
        /// <summary> Sort Glyph mode, Ascending (true) or Decending (false)</summary>
        public bool? SortGlyphAscending { get; set; } = null;               
        /// <summary> Show Glyph </summary>
        public bool ShowGlyph { get { return showglyph; } set { showglyph = value; Changed?.Invoke(this, true); } }
        /// <summary> Show Header text enable</summary>
        public bool ShowHeaderText { get { return showtext; } set { showtext = value; Changed?.Invoke(this, true); } }

        /// <summary> Call back to custom sort this column. 
        /// Get the two cells and custom compare, return -1 first less than second, 0 equal, 1 first is greater than second </summary>
        public Func<GLDataGridViewCell, GLDataGridViewCell, int> SortCompare = null;

        /// <summary> Default constructor. Note DO NOT construct, use GLDataGridView.CreateColumn</summary>
        public GLDataGridViewColumn() { }

        #region Implementation
        internal Action<GLDataGridViewColumn, bool> Changed { get; set; }
        internal void SetColNo(int i)
        {
            colno = i;
        }
        internal int WidthNI { get { return width; } set { width = Math.Max(minwidth, value); } }

        internal void Paint(Graphics gr, Rectangle area)
        {
            area = new Rectangle(area.Left + HeaderStyle.Padding.Left, area.Top + HeaderStyle.Padding.Top, area.Width - HeaderStyle.Padding.TotalWidth, area.Height - HeaderStyle.Padding.TotalHeight);

            if (HeaderStyle.BackColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(HeaderStyle.BackColor))
                {
                    gr.FillRectangle(b, area);
                }
            }

            if (ShowHeaderText)
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(HeaderStyle.ContentAlignment))
                {
                    //System.Diagnostics.Debug.WriteLine($"Draw {Text} {Enabled} {ForeDisabledScaling}");
                    using (Brush textb = new SolidBrush(HeaderStyle.ForeColor))
                    {
                        gr.DrawString(text, HeaderStyle.Font, textb, area, fmt);
                    }
                }
            }

            if ( ShowGlyph && SortGlyphAscending != null )
            {
                using (Brush b = new SolidBrush(HeaderStyle.ForeColor))
                {
                    int margin = 2;
                    int size = 10;
                    int hleft = area.Right - size - margin;
                    int hright = hleft + size;
                    int hcentre = (hleft + hright) / 2;
                    int htop = (area.Top+area.Bottom)/2 - size/2;
                    int hbottom = htop + size;
                    if ( SortGlyphAscending == true )
                        gr.FillPolygon(b, new Point[] { new Point(hcentre, htop), new Point(hright, hbottom), new Point(hleft, hbottom) });
                    else
                        gr.FillPolygon(b, new Point[] { new Point(hcentre, hbottom), new Point(hleft, htop), new Point(hright, htop) });
                }

            }
        }

        private GLDataGridViewCellStyle headerstyle = new GLDataGridViewCellStyle();
        private int width;
        private int minwidth = 10;
        private float fillwidth;
        private string text = string.Empty;
        private int colno;
        private bool showtext = true;
        private bool showglyph = true;

        #endregion
    }
}
