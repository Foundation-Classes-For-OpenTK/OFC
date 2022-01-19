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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Data Grid View Row
    /// </summary>
    public class GLDataGridViewRow
    {
        /// <summary> Row index </summary>
        public int Index { get { return rowno; } }
        /// <summary> Parent data grid view </summary>
        public GLDataGridView Parent { get; set; }
        /// <summary> Height in pixels</summary>
        public int Height { get { return height; } set { if (value != height) { height = Math.Max(value,MinimumHeight); autosizegeneration = 0; Changed?.Invoke(this); } } }
        /// <summary> Minimum height in pixels</summary>
        public int MinimumHeight { get { return minheight; } set { if (value != minheight) { minheight = value; autosizegeneration = 0; Changed?.Invoke(this); } } }
        /// <summary> Autosize row to content</summary>
        public bool AutoSize { get { return autosize; } set { if (autosize != value) { autosize = value;  } }  }
        /// <summary> Cells assigned to row. Use AddCell to add.</summary>
        public List<GLDataGridViewCell> Cells { get { return cells; } }
        /// <summary> Cell count. Note cell count can be less than number of columns</summary>
        public int CellCount { get { return cells.Count; } }
        /// <summary> Default Cell Style. If not set, the default cell style will be from GLDataGridView.DefaultCellStyle </summary>
        public GLDataGridViewCellStyle DefaultCellStyle { get { return defaultcellstyle; } }
        /// <summary> Header style. If not set, the default header style will be from GLDataGridView.DefaultRowHeaderStyle </summary>
        public GLDataGridViewCellStyle HeaderStyle { get { return headerstyle; } }
        /// <summary> Return cell at column index.  Null if cell does not exist</summary>
        public GLDataGridViewCell this[int index] { get { return index < cells.Count ? cells[index] : null; } }
        /// <summary> Show header text enable </summary>
        public bool ShowHeaderText { get { return showtext; } set { showtext = value; Changed?.Invoke(this); } }
        /// <summary> Whole row selected </summary>
        public bool Selected { get { return selected; } set { if (value != selected) { selected = value; foreach (var c in cells) c.SelectedNI = value; SelectionChanged(this, -1); } } }
        /// <summary> User tag</summary>
        public object Tag { get; set; }

        /// <summary> Default constructor. Note DO NOT construct, use GLDataGridView.CreateRow</summary>
        public GLDataGridViewRow()
        {
        }

        /// <summary> Insert cells(s). Inserted after all previous cells.</summary>
        public void AddCell(params GLDataGridViewCell[] celllist)
        {
            foreach (var cell in celllist)
            {
                int index = cells.Count;
                cell.RowParent = this;
                cell.Style.Parent = defaultcellstyle;
                cell.Index = index;

                // if a cell style has changed
                cell.Style.Changed += (e1) => { autosizegeneration = 0; Changed?.Invoke(this); };
                // if a cell content has changed
                cell.Changed += (e1, aus) => { if (aus) autosizegeneration = 0; Changed?.Invoke(this); };
                cell.SelectionChanged += (e1) =>
                {
                    if (Selected)      // if row selected, and we are clicked (therefore turning off), then we turn off whole of row
                    {
                        selected = false;
                        foreach (var cell in Cells)
                            cell.SelectedNI = false;
                        SelectionChanged?.Invoke(this, -1);
                    }
                    else if (Parent.SelectCellSelectsRow)   // if in whole row select
                    {
                        foreach (var cell in Cells)
                            cell.SelectedNI = e1.Selected;

                        selected = e1.Selected;
                        SelectionChanged?.Invoke(this, -1);
                    }
                    else
                    {
                        int celsel = cells.Where(x => x.Selected || !x.Selectable).Count();     // either selected, or not selectable, counts towards highlight total
                        selected = celsel == cells.Count;
                        SelectionChanged?.Invoke(this, e1.Index);
                    }
                };

                cells.Add(cell);
            }
            Changed?.Invoke(this);
        }

        /// <summary> Remove cell at index. True if a cell existed at that index </summary>
        public bool RemoveCellAt(int index)
        {
            if (cells.Count > index)
            {
                cells.RemoveAt(index);
                Changed?.Invoke(this);
                return true;
            }
            else
                return false;
        }

        /// <summary> Remove all cells on row </summary>
        public void Clear()
        {
            cells.Clear();
            Changed?.Invoke(this);
        }

        #region Implementation

        /// <summary> </summary>
        internal Action<GLDataGridViewRow> Changed { get; set; }     // Changed
        /// <summary> </summary>
        internal Action<GLDataGridViewRow, int> SelectionChanged { get; set; }     // Selection changed, of cell, or -1 of row
        /// <summary> Internal call to set the autosize generation number for this row. Do not use.</summary>
        internal uint AutoSizeGeneration { get { return autosizegeneration; } set { autosizegeneration = value; } }   // for autosize tracking
        /// <summary> Internal call to set selection flag. Do not use. </summary>
        internal bool SelectedNI { get { return selected; } set { selected = value; } }

        internal void SetAutoSizeHeight(uint gen, int h)
        {
            autosizegeneration = gen;
            height = Math.Max(minheight,h);
        }

        internal void SetRowNo(int i, GLDataGridViewCellStyle defcellstyle)
        {
            rowno= i;
            defaultcellstyle.Parent = defcellstyle;
        }

        internal void Paint(Graphics gr, Rectangle area)      
        {
            area = new Rectangle(area.Left + HeaderStyle.Padding.Left, area.Top + HeaderStyle.Padding.Top, area.Width - HeaderStyle.Padding.TotalWidth, area.Height - HeaderStyle.Padding.TotalHeight);

            if ( Selected )
            {
                using (Brush b = new SolidBrush(HeaderStyle.SelectedColor))
                {
                    gr.FillRectangle(b, area);
                }
            }
            else if (HeaderStyle.BackColor != Color.Transparent)
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
                        gr.DrawString((rowno+Parent.RowCountOffset).ToString(), HeaderStyle.Font, textb, area, fmt);
                    }
                }
            }
        }

        private GLDataGridViewCellStyle defaultcellstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle headerstyle = new GLDataGridViewCellStyle();
        private List<GLDataGridViewCell> cells = new List<GLDataGridViewCell>();
        private int height;
        private int minheight = 10;
        private int rowno = -1;
        private bool autosize;
        private uint autosizegeneration = 0;
        private bool showtext = true;
        private bool selected = false;

        #endregion
    }
}
