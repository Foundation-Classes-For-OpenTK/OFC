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
    public class GLDataGridView : GLBaseControl
    {
        public GLDataGridView(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = Color.AliceBlue;
            cellbordercolor = Color.Magenta;

            int sbwidth = 16;
            vertscroll = new GLVerticalScrollBar(name + "_VSB", new Rectangle(0, 0, sbwidth, 10), 0, 100);
            vertscroll.Dock = DockingType.Right;
            vertscroll.Scroll += (sb, se) => { contentpanel.FirstDisplayIndex = se.NewValue; };
            horzscroll = new GLHorizontalScrollBar(name + "_HSB", new Rectangle(0, 0, 10, sbwidth), 0, 100);
            horzscroll.Dock = DockingType.Bottom;
            horzscroll.Scroll += (sb, se) => { colheaderpanel.HorzScroll = contentpanel.HorzScroll = se.NewValue; };
            rowheaderpanel = new GLDataGridViewRowHeaderPanel(name + "_RHP", location);
            rowheaderpanel.Dock = DockingType.Left;
            contentpanel = new GLDataGridViewContentPanel(name + "_CP", rowheaderpanel, location);
            contentpanel.Dock = DockingType.Fill;
            colheaderpanel = new GLDataGridViewColumnHeaderPanel(name + "_CHP", location);
            colheaderpanel.Dock = DockingType.Top;
            topleftpanel = new GLDataGridViewTopLeftHeaderPanel(name + "_TLP", location);
            topleftpanel.Dock = DockingType.LeftTop;
            Add(contentpanel);
            Add(colheaderpanel);
            Add(rowheaderpanel);
            Add(vertscroll);
            Add(horzscroll);
            Add(topleftpanel);

            colheaderstyle.Changed += (e1) => { colheaderpanel.Invalidate(); };
            rowheaderstyle.Changed += (e1) => { ContentInvalidateLayout(); };
            defaultcellstyle.Changed += (e1) => { ContentInvalidateLayout(); };
            upperleftstyle.Changed += (e1) => { colheaderpanel.Invalidate(); };

            upperleftstyle.BackColor = Color.Gray;
            colheaderstyle.BackColor = rowheaderstyle.BackColor = Color.Orange;
            defaultcellstyle.BackColor = Color.White;
            upperleftstyle.ForeColor = colheaderstyle.ForeColor = rowheaderstyle.ForeColor = defaultcellstyle.ForeColor = Color.Black;
            upperleftstyle.SelectedColor = colheaderstyle.SelectedColor = rowheaderstyle.SelectedColor = defaultcellstyle.SelectedColor = Color.Yellow;
            upperleftstyle.HighlightColor = colheaderstyle.HighlightColor = rowheaderstyle.HighlightColor = defaultcellstyle.HighlightColor = Color.Red;
            upperleftstyle.ContentAlignment = colheaderstyle.ContentAlignment = rowheaderstyle.ContentAlignment = defaultcellstyle.ContentAlignment = ContentAlignment.MiddleCenter;
            upperleftstyle.TextFormat = colheaderstyle.TextFormat = rowheaderstyle.TextFormat = defaultcellstyle.TextFormat = 0;
            upperleftstyle.Font = colheaderstyle.Font = rowheaderstyle.Font = defaultcellstyle.Font = Font;
            upperleftstyle.Padding = colheaderstyle.Padding = rowheaderstyle.Padding = defaultcellstyle.Padding = new Padding(0);

            colheaderpanel.MouseClickColumnHeader += (col, e) =>
            {
                //System.Diagnostics.Debug.WriteLine($"Click on {col} {SortColumn} {SortAscending}");
                if (col >= 0)
                    Sort(col, !SortAscending);
            };

            rowheaderpanel.MouseClickRowHeader += (row, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Click on row header {row}");
                MouseClickOnGrid?.Invoke(row, -1, e);
            };
            contentpanel.MouseClickOnGrid += (row, col, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Click on {row} {col}");
                MouseClickOnGrid?.Invoke(row, col, e);
            };

        }
        public int SortColumn { get; set; } = 1;
        public bool SortAscending { get; set; } = true;
        public int ScrollBarWidth { get { return vertscroll.Width; } set { vertscroll.Width = horzscroll.Height = value; } }

        public GLDataGridViewCellStyle UpperLeftStyle { get { return upperleftstyle; } set { upperleftstyle = value; colheaderpanel.Invalidate(); } }
        public GLDataGridViewCellStyle DefaultCellStyle { get { return defaultcellstyle; } set { defaultcellstyle = value; ContentInvalidateLayout(); } }

        public enum ColFillMode { FillWidth, Width };
        public ColFillMode ColumnFillMode { get { return colfillmode; } set { if (value != colfillmode) { colfillmode = value; ContentInvalidateLayout(); } } }
        public GLDataGridViewCellStyle DefaultColumnHeaderStyle { get { return colheaderstyle; } set { colheaderstyle = value; colheaderpanel.Invalidate(); } }
        public bool ColumnHeaderEnable { get { return columnheaderenable; } set { columnheaderenable = value; colheaderpanel.Visible = value; topleftpanel.Visible = colheaderpanel.Visible && rowheaderpanel.Visible; InvalidateLayout(); } }
        public int ColumnHeaderHeight { get { return columnheaderheight; } set { columnheaderheight = value; InvalidateLayout(); } }

        public GLDataGridViewCellStyle DefaultRowHeaderStyle { get { return rowheaderstyle; } set { rowheaderstyle = value; ContentInvalidateLayout(); } }
        public int RowHeaderWidth { get { return rowheaderwidth; } set { rowheaderwidth = value; InvalidateLayout(); } }
        public bool RowHeaderEnable { get { return rowheaderenable; } set { rowheaderenable = value; rowheaderpanel.Visible = value; topleftpanel.Visible = colheaderpanel.Visible && rowheaderpanel.Visible;  ContentInvalidateLayout(); } }

        public List<GLDataGridViewColumn> Columns { get { return columns; } }
        public List<GLDataGridViewRow> Rows { get { return rows; } }

        public Color CellBorderColor { get { return cellbordercolor; } set { cellbordercolor = value; ContentInvalidate(); } }
        public int CellBorderWidth { get { return cellborderwidth; } set { cellborderwidth = value; ContentInvalidateLayout(); } }

        // pixel positions
        public int ColumnPixelLeft(int c) { return columns.Where(x => x.Index < c).Select(y => y.Width).Sum() + cellborderwidth * c; }
        public int ColumnPixelWidth { get { return columns.Select(y=>y.Width).Sum() + cellborderwidth * (columns.Count+1); } }

        public Action<GLDataGridViewColumn, Graphics, Rectangle> UserPaintColumnHeaders { get; set; } = null;
        public Action<GLDataGridViewRow, Graphics, Rectangle> UserPaintRowHeaders { get; set; } = null;
        public Action<Graphics, Rectangle> UserPaintTopLeftHeader { get; set; } = null;

        public Action<int, int, GLMouseEventArgs> MouseClickOnGrid;                // row, col = -1 for row header

        public GLDataGridViewRow CreateRow()
        {
            GLDataGridViewRow row = new GLDataGridViewRow();
            row.Parent = this;
            row.DefaultCellStyle.Parent = defaultcellstyle;
            row.HeaderStyle.Parent = rowheaderstyle;
            row.Height = 24;
            return row;
        }
        public GLDataGridViewColumn CreateColumn()
        {
            GLDataGridViewColumn col = new GLDataGridViewColumn();
            col.HeaderStyle.Parent = colheaderstyle;
            col.Parent = this;
            col.Width = 50;
            col.FillWidth = 100;
            return col;
        }

        public uint AutoSizeGeneration { get; set; } = 1;

        bool ignorecolumncommands = false;
        public void AddColumn(GLDataGridViewColumn col)
        {
            System.Diagnostics.Debug.Assert(col.Parent == this && col.HeaderStyle.Parent != null);      // ensure created by us
            col.HeaderStyle.Changed += (e1) => { colheaderpanel.Invalidate(); };
            col.Changed += (e1, ci) => 
            {
                if (!ignorecolumncommands)
                {
                    if (ci)         // if set, it means width has changed in some way
                    {
                        AutoSizeGeneration++;           // force autosize as we changed width
                        ContentInvalidateLayout();
                    }
                    else
                        colheaderpanel.Invalidate();
                }
            };
            col.SetColNo(columns.Count);
            columns.Add(col);
            AutoSizeGeneration++;           // force autosize as we changed columns
            ContentInvalidateLayout();
        }

        public void RemoveColumn(int index)
        {
            foreach (var r in Rows)
                r.RemoveCellAt(index);      // this will cause lots of row changed cells, causing an Invalidate.

            GLDataGridViewColumn col = columns[index];
            col.Parent = null;
            col.HeaderStyle.Parent = null;
            col.HeaderStyle.Changed = null;
            col.Changed = null;
            columns.RemoveAt(index);

            if (SortColumn == index)
                SortColumn = -1;

            for (int i = 0; i < columns.Count; i++)
                columns[i].SetColNo(i);

            AutoSizeGeneration++;           // force autosize as we changed columns
            ContentInvalidateLayout();
        }

        public void AddRow(GLDataGridViewRow row, int insertat = -1)
        {
            System.Diagnostics.Debug.Assert(row.Parent == this && row.HeaderStyle.Parent != null);      // ensure created by us
            row.HeaderStyle.Changed += (e1) => { ContentInvalidateLayout(); };       // header style changed, need a complete refresh
            row.AutoSizeGeneration = 0;
            row.Changed += (e1, cellno) => 
            {
                contentpanel.RowChanged(row.Index);     // inform CP
                UpdateScrollBar();  // update scroll bar
            };

            if (insertat == -1)
            {
                row.SetRowNo(rows.Count);
                rows.Add(row);
                contentpanel.AddRow(row.Index);       // see if content panel needs redrawing
            }
            else
            {
                rows.Insert(insertat, row);
                for (int i = insertat; i < rows.Count; i++)
                    rows[i].SetRowNo(i);
                contentpanel.InsertRow(row.Index);       // see if content panel needs redrawing
            }

            UpdateScrollBar();
        }

        public void RemoveRow(int index)
        {
            GLDataGridViewRow row = rows[index];
            row.Parent = null;
            row.DefaultCellStyle.Parent = null;
            row.HeaderStyle.Parent = null;
            row.HeaderStyle.Changed = null;
            row.Changed = null;
            contentpanel.RemoveRow(index);
            rows.RemoveAt(index);
            for (int i = index; i < rows.Count; i++)
                rows[i].SetRowNo(i);
            UpdateScrollBar();
        }

        // adjust column width to newwidth in pixels
        public void SetColumnWidth(int index,int newwidth)
        {
           // System.Diagnostics.Debug.WriteLine($"Col {index} delta {newwidth}");
            var col = columns[index];
            if ( colfillmode == ColFillMode.Width)
            {
                col.Width = newwidth;
            }
            else
            {
                // only change this and columns to the right, as per winform DGV

                int cellpixels = columns.Where(x => x.Index >= index).Sum(x => x.Width);       // pixels for columns to resize
                float totalfill = columns.Where(x => x.Index >= index).Sum(x => x.FillWidth);               // fills for columns to resize

                float newfillwidth = newwidth * totalfill / cellpixels;     // compute out fill width from newwidth pixels

                float neededfromothers = newfillwidth - col.FillWidth;      // what we need to nick from the fill of others
                float othertotal = totalfill - col.FillWidth;               // the total fill of the others, ignoring out fill width
                float proportion = 1.0f - neededfromothers / othertotal;    // and the proportion to scale the others

               // System.Diagnostics.Debug.WriteLine($"ColWidth {col.Index} {cellpixels} {totalfill} new fill {newfillwidth} needed {neededfromothers} othertotal = {othertotal} proportion {proportion}");

                col.FillWidth = newfillwidth;

                for( int i = index+1; i < columns.Count; i++)
                {
                    columns[i].FillWidth *= proportion;
                }
            }

        }

        public void Sort(int colno, bool sortascending)
        {
            if (colno < columns.Count)
            {
                System.Diagnostics.Debug.WriteLine($"Sort col {colno} by ascending {sortascending}");
                rows.Sort(delegate (GLDataGridViewRow l, GLDataGridViewRow r) 
                    {
                        if (colno < l.Cells.Count)
                        {
                            if (colno < r.Cells.Count)
                                return l.Cells[colno].CompareTo(r.Cells[colno]) * (sortascending ? +1 : -1);
                            else
                                return 1;
                        }
                        else 
                            return (colno < r.Cells.Count) ? -1 : 0;
                    });

                if (SortColumn >= 0)
                    columns[SortColumn].SortGlyphAscending = null;

                SortColumn = colno;
                SortAscending = sortascending;

                for (int i = 0; i < rows.Count; i++)
                    rows[i].SetRowNo(i);

                columns[colno].SortGlyphAscending = sortascending;

                ContentInvalidateLayout();
            }

        }

        protected override void PerformRecursiveLayout()     
        {
            colheaderpanel.Height = columnheaderheight + cellborderwidth;        // set before children layout
            rowheaderpanel.Width = rowheaderwidth + cellborderwidth;
            rowheaderpanel.DockingMargin = new Margin(0, ColumnHeaderEnable ? colheaderpanel.Height : 0, 0, 0);
            topleftpanel.Size = new Size(rowheaderwidth + cellborderwidth, columnheaderheight + cellborderwidth);
            colheaderpanel.BackColor = BackColor;
            rowheaderpanel.BackColor = BackColor;
            contentpanel.BackColor = BackColor;

            base.PerformRecursiveLayout();      // do layout on children.

            // work out the column layout

            int pixelsforborder = (columns.Count + 1 + (rowheaderenable ? 1 : 0)) * cellborderwidth;

            ignorecolumncommands = true;
            if (colfillmode == ColFillMode.FillWidth)
            {
                int cellpixels = contentpanel.Width - pixelsforborder - (rowheaderenable ? rowheaderwidth : 0);
                float filltotalallcolumns = columns.Select(x => x.FillWidth).Sum();

                bool[] minwidths = new bool[columns.Count];

                float hfillfinaltotal = 0;

                foreach (var col in columns)
                {
                    int width = (int)(cellpixels * col.FillWidth / filltotalallcolumns);         // candidate width for all columns
                    if (width < col.MinimumWidth)                                       // if less than min width, set it to that, take this column out of the fill equation
                    {
                        col.Width = col.MinimumWidth;
                        cellpixels -= col.Width;
                        minwidths[col.Index] = true;
                        //System.Diagnostics.Debug.WriteLine($"{col.Index} less than min width");
                    }
                    else
                        hfillfinaltotal += col.FillWidth;
                }

                //System.Diagnostics.Debug.WriteLine($"{hfillfinaltotal} cell pixels {cellpixels}");
                int pixels = 0;

                foreach (var col in columns)
                {
                    if (!minwidths[col.Index])  // if not min width it, set it to the width, to the column minimum width
                    {
                        col.Width = Math.Max(col.MinimumWidth, (int)(cellpixels * col.FillWidth / hfillfinaltotal)); 
                       // System.Diagnostics.Debug.WriteLine($"{col.Index} auto size to {col.Width}");
                        pixels += col.Width;
                    }
                }

               // System.Diagnostics.Debug.WriteLine($"Cell pixels {cellpixels} total {pixels}");

                int colno = 0;
                while (pixels < cellpixels && columns.Count > 0)      // add 1 pixel to each column in turn until back to count
                {
                    columns[colno].Width += 1;
                   // System.Diagnostics.Debug.WriteLine($"Distribute pixel to {colno}");
                    pixels++;
                    colno = (colno + 1) % columns.Count;
                }
            }
            else
            {
                foreach (var col in columns)
                {
                    if (col.Width < col.MinimumWidth)
                        col.Width = col.MinimumWidth;
                }
            }

            ignorecolumncommands = false;

            UpdateScrollBar();
        }

        private void ContentInvalidateLayout()
        {
            contentpanel.Redraw();
            InvalidateLayout();
        }
        private void ContentInvalidate()
        {
            contentpanel.Redraw();
            Invalidate();
        }

        private void UpdateScrollBar()
        {
            if (LayoutSuspended)
                return;

            // compute the area used in the top, up to height of cp
            var top = ComputeHeight(0, contentpanel.Height);
            if (columns.Count > 0 &&        // need some columns
                top.Item1 < rows.Count - 1)      // if last complete row is less than no of rows, we need a scroll
            {
                var bot = ComputeHeight(-1, contentpanel.Height);   // count from bottom up and see how many rows until we fill the panel
                vertscroll.SetValueMaximumLargeChange(contentpanel.FirstDisplayIndex, rows.Count - 1, rows.Count - bot.Item1);
            }
            else
            {       // no scroll
                vertscroll.SetValueMaximumLargeChange(0, rows.Count - 1, Rows.Count);
            }

            int columnwidth = ColumnPixelWidth;

            if (columnwidth > contentpanel.Width)   // if > contentwidth, then scroll
            {
                horzscroll.SetValueMaximumLargeChange(contentpanel.HorzScroll, columnwidth-1, contentpanel.Width);
            }
            else
            {
                horzscroll.SetValueMaximumLargeChange(0, 99, 100);
            }
        }

        // given a start point : +ve from here, -ve from end (-1 = first end row)
        // and the maximum bit map height to measure, run thru rows till end return lastcompleted row and total height
        private Tuple<int, int> ComputeHeight(int start, int maxbitmapheight)
        {
            int dir = start >= 0 ? 1 : -1;

            if (start < 0)
                start = rows.Count + start;

            int vpos = cellborderwidth;
            int lastcompleterow = -1;

            while (start >= 0 && start < rows.Count && vpos < maxbitmapheight)
            {
                var row = rows[start];

                if (row.AutoSize && row.AutoSizeGeneration != AutoSizeGeneration)
                    PerformAutoSize(row);

                vpos += row.Height + cellborderwidth;
                if (vpos < maxbitmapheight)     // if ending vpos < height, its completely displayed
                    lastcompleterow = start;
                start += dir;
            }

            //  System.Diagnostics.Debug.WriteLine($"Compute Height on {start} maxh {maxbitmapheight} last tow {lastcompleterow} vpos {vpos}");

            return new Tuple<int, int>(lastcompleterow, vpos);
        }

        public void PerformAutoSize(GLDataGridViewRow r)
        {
            //  System.Diagnostics.Debug.WriteLine($"Perform autosize {r.Index}");

            int maxh = 0;

            for (int i = 0; i < columns.Count; i++)     // each column cell gets a chance to autosize against the col width
            {
                if (i < r.Cells.Count)
                {
                    Size s = r.Cells[i].PerformAutoSize(Math.Max(columns[i].Width, columns[i].MinimumWidth));     // max of these just in case we have not performed layout
                    maxh = Math.Max(maxh, s.Height);
                }
            }

            r.SetAutoSizeHeight(AutoSizeGeneration,maxh);
        }

        protected override void OnResize()
        {
            AutoSizeGeneration++;           // we will be doing a content realignment, perform autosize
            base.OnResize();    // do after
        }
        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            colheaderstyle.Font = rowheaderstyle.Font = defaultcellstyle.Font = Font;
            AutoSizeGeneration++;
            ContentInvalidateLayout();
        }

        private List<GLDataGridViewColumn> columns = new List<GLDataGridViewColumn>();
        private List<GLDataGridViewRow> rows = new List<GLDataGridViewRow>();

        private GLDataGridViewCellStyle defaultcellstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle rowheaderstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle colheaderstyle = new GLDataGridViewCellStyle();

        private GLDataGridViewCellStyle upperleftstyle = new GLDataGridViewCellStyle();

        private ColFillMode colfillmode = ColFillMode.Width;
        private int cellborderwidth = 1;
        private int columnheaderheight = 40;
        private bool columnheaderenable = true;

        private int rowheaderwidth = 40;
        private bool rowheaderenable = true;

        private Color cellbordercolor;

        private GLHorizontalScrollBar horzscroll;
        private GLVerticalScrollBar vertscroll;
        private GLDataGridViewContentPanel contentpanel;
        private GLDataGridViewColumnHeaderPanel colheaderpanel;
        private GLDataGridViewRowHeaderPanel rowheaderpanel;
        private GLDataGridViewTopLeftHeaderPanel topleftpanel;
    }

}
