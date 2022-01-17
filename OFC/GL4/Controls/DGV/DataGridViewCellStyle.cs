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
    /// <summary>
    /// Class to hold style of cells, either content, row or header cells.
    /// Styles are inherited in this manner:
    /// * Odd rows: GLDataGridView.DefaultCellStyle  - GLDataGridViewRow.DefaultCellStyle - GLDataGridViewCell.Style
    /// * Even rows: GLDataGridView.DefaultCellStyle - GLDataGridView.DefaultAltRowCellStyle  - GLDataGridViewRow.DefaultCellStyle - GLDataGridViewCell.Style
    /// * Column headers: GLDataGridView.DefaultColumnHeaderStyle - GLDataGridViewColumn.HeaderStyle
    /// * Row headers: GLDataGridView.DefaultRowHeaderStyle - GLDataGridViewRow.HeaderStyle
    /// </summary>
    public class GLDataGridViewCellStyle
    {
        /// <summary> Back color</summary>
        public Color BackColor { get { return backcolor.HasValue ? backcolor.Value : Parent.BackColor; } set { if (value != backcolor) { backcolor = value; Changed?.Invoke(this); } } }
        /// <summary> Fore color </summary>
        public Color ForeColor { get { return forecolor.HasValue ? forecolor.Value : Parent.ForeColor; } set { if (value != forecolor) { forecolor = value; Changed?.Invoke(this); } } }
        /// <summary> Selected back color </summary>
        public Color SelectedColor { get { return selectedcolor.HasValue ? selectedcolor.Value : Parent.SelectedColor; } set { if (value != selectedcolor) {  selectedcolor = value; Changed?.Invoke(this); } } }
        /// <summary> Highlight color (not currently used)</summary>
        public Color HighlightColor { get { return highlightcolor.HasValue ? highlightcolor.Value : Parent.HighlightColor; } set { if (value != highlightcolor) { highlightcolor = value; Changed?.Invoke(this); } } }
        /// <summary> Content alignment for text/images in cells/headers</summary>
        public ContentAlignment ContentAlignment { get { return contentalignment.HasValue ? contentalignment.Value : Parent.ContentAlignment; } set { if (value != contentalignment) { contentalignment = value; Changed?.Invoke(this); } } }
        /// <summary> Text format for text in cells</summary>
        public StringFormatFlags TextFormat { get { return textformatflags.HasValue ? textformatflags.Value : Parent.TextFormat; } set { if (value != textformatflags) { textformatflags = value; Changed?.Invoke(this); } } }
        /// <summary> Font for text in cells</summary>
        public Font Font { get { return font != null ? font : Parent.Font; } set { font = value; Changed?.Invoke(this); } }
        /// <summary> Cell padding area </summary>
        public GLBaseControl.PaddingType Padding { get { return padding.HasValue ? padding.Value : Parent.Padding; } set { padding = value; Changed?.Invoke(this); } }

        /// <summary> Parent style </summary>
        public GLDataGridViewCellStyle Parent { get; set; }
        /// <summary> Callback, used internally, has style changed? </summary>
        internal Action<GLDataGridViewCellStyle> Changed { get; set; }

        private Color? backcolor;
        private Color? forecolor;
        private Color? selectedcolor;
        private Color? highlightcolor;
        private ContentAlignment? contentalignment;
        private StringFormatFlags? textformatflags;
        private Font font;
        private GLBaseControl.PaddingType? padding;
    }
}
