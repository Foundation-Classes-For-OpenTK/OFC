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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    public class GLDataGridViewRow
    {
        public Action<GLDataGridViewRow, int> Changed { get; set; }     // Changed, cell which changed, -1 if general row
        public Action<GLDataGridViewRow, int> SelectionChanged { get; set; }     // Selection changed, of cell, or -1 of row
        public GLDataGridView Parent { get; set; }
        public int Index { get { return rowno; } }
        public int Height { get { return height; } set { if (value != height) { height = Math.Max(value,MinimumHeight); autosizegeneration = 0; Changed?.Invoke(this, -1); } } }
        public int MinimumHeight { get { return minheight; } set { if (value != minheight) { minheight = value; autosizegeneration = 0; Changed?.Invoke(this, -1); } } }
        public bool AutoSize { get { return autosize; } set { if (autosize != value) { autosize = value;  } }  }
        public List<GLDataGridViewCell> Cells { get { return cells; } }
        public GLDataGridViewCellStyle DefaultCellStyle { get { return defaultcellstyle; } }
        public GLDataGridViewCellStyle HeaderStyle { get { return headerstyle; } }

        public GLDataGridViewCell this[int cell] { get { return cell < cells.Count ? cells[cell] : null; } }
        public uint AutoSizeGeneration { get { return autosizegeneration; } set { autosizegeneration = value; } }

        public bool ShowHeaderText { get { return showtext; } set { showtext = value; Changed?.Invoke(this,-1); } }

        public bool Selected { get { return selected; } set { if (value != selected) { selected = value; foreach (var c in cells) c.SelectedNI = value; SelectionChanged(this, -1); } } }
        public bool SelectedNI { get { return selected; } set { selected = value; } }

        public GLDataGridViewRow()
        {
        }

        public void AddCell(GLDataGridViewCell cell)
        {
            int index = cells.Count;
            cell.Parent = this;
            cell.Style.Parent = defaultcellstyle;
            cell.Style.Index = index;
            cell.Index = index;

            // if a cell style has changed
            cell.Style.Changed += (e1) => { autosizegeneration = 0; Changed?.Invoke(this, index); };
            // if a cell content has changed
            cell.Changed += (e1,aus) => { if (aus) autosizegeneration = 0; Changed?.Invoke(this, index); };
            cell.SelectionChanged += (e1) => 
            {
                if ( Selected )      // if row selected, and we are clicked (therefore turning off), then we turn off whole of row
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
                    int celsel = cells.Where(x => x.Selected).Count();
                    selected = celsel == cells.Count;
                    SelectionChanged?.Invoke(this, e1.Index);
                }
            };

            cells.Add(cell);
            Changed?.Invoke(this, index);
        }

        // from the wanted values work out height of row
        // true if we change height.  
        public void SetAutoSizeHeight(uint gen, int h)
        {
            autosizegeneration = gen;
            height = Math.Max(minheight,h);
        }

        public void SetRowNo(int i)
        {
            rowno= i;
        }

        public void RemoveCellAt(int index)
        {
            if ( cells.Count>index)
            {
                cells.RemoveAt(index);
                Changed?.Invoke(this, -1);
            }
        }

        public void Clear()
        {
            cells.Clear();
            Changed?.Invoke(this,-1);
        }

        public void Paint(Graphics gr, Rectangle area)
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
                        gr.DrawString(rowno.ToString(), HeaderStyle.Font, textb, area, fmt);
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
    }
}
