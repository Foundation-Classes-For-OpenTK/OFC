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
    /// <summary>
    /// Data Grid View control
    /// Control does not support editing - viewing only.
    /// </summary>

    public class GLDataGridView : GLBaseControl
    {
        /// <summary> List of columns.</summary>
        public IList<GLDataGridViewColumn> Columns { get { return columns.AsReadOnly(); } }
        /// <summary> List of rows </summary>
        public IList<GLDataGridViewRow> Rows { get { return rows.AsReadOnly(); } }


        /// <summary> Sort column, -1 means not sorted. Note adding new rows causes the sort to be incorrect, but this value will maintain its value </summary>
        public int SortColumn { get; set; } = -1;
        /// <summary> Sort ascending (true) or decending (false)</summary>
        public bool SortAscending { get; set; } = true;

        /// <summary> Scroll bar width </summary>
        public int ScrollBarWidth { get { return vertscroll.Width; } set { vertscroll.Width = horzscroll.Height = value; ContentInvalidateLayout(); } }


        /// <summary> Default cell style. Applied unless overridden by an individual row DefaultCellStyle or by a cell's cell style. </summary>
        public GLDataGridViewCellStyle DefaultCellStyle { get { return defaultcellstyle; } set { defaultcellstyle = value; ContentInvalidateLayout(); } }
        /// <summary> Default alternate row cell style. The DefaultCellStyle will be applied if this is not changed.
        /// Applied unless overridden by an individual row DefaultCellStyle or by a cell's cell style. </summary>
        public GLDataGridViewCellStyle DefaultAltRowCellStyle { get { return defaultaltrowcellstyle; } set { defaultaltrowcellstyle = value; ContentInvalidateLayout(); } }


        /// <summary> Enable Horizontal scroll bar visibility </summary>
        public bool HorizontalScrollVisible { get { return horzscroll.Visible; } set { horzscroll.Visible = value; ContentInvalidateLayout(); } }
        /// <summary> Enable Vertical scroll bar visibility </summary>
        public bool VerticalScrollVisible { get { return vertscroll.Visible; } set { vertscroll.Visible = value; ContentInvalidateLayout(); } }


        /// <summary> Column Fill Mode Types</summary>
        public enum ColFillMode {
            /// <summary> Use the FillWidth weighting value on columns to set the pixel width</summary>
            FillWidth,
            /// <summary> Use the Width value on columns to set the pixel width </summary>
            Width
        };
        /// <summary> Column Fill Mode </summary>
        public ColFillMode ColumnFillMode { get { return colfillmode; } set { if (value != colfillmode) { colfillmode = value; ContentInvalidateLayout(); } } }
        /// <summary> Default style for column headers </summary>
        public GLDataGridViewCellStyle DefaultColumnHeaderStyle { get { return colheaderstyle; } set { colheaderstyle = value; colheaderpanel.Invalidate(); } }
        /// <summary> Column Header show/no show</summary>
        public bool ColumnHeaderEnable { get { return columnheaderenable; } set { columnheaderenable = value; colheaderpanel.Visible = value; topleftpanel.Visible = colheaderpanel.Visible && rowheaderpanel.Visible; InvalidateLayout(); } }
        /// <summary> Column Header height</summary>
        public int ColumnHeaderHeight { get { return columnheaderheight; } set { columnheaderheight = value; InvalidateLayout(); } }
        /// <summary> Allow users to resize columns</summary>
        public bool AllowUserToResizeColumns { get; set; } = true;
        /// <summary> Allow user to resize column height</summary>
        public bool AllowUserToResizeColumnHeight { get; set; } = true;
        /// <summary> Allow user to click on column to sort </summary>
        public bool AllowUserToSortColumns { get; set; } = true;


        /// <summary> Default style for row headers. A Row header can override this with its own HeaderStyle </summary>
        public GLDataGridViewCellStyle DefaultRowHeaderStyle { get { return rowheaderstyle; } set { rowheaderstyle = value; ContentInvalidateLayout(); } }
        /// <summary> Row header show/no show</summary>
        public bool RowHeaderEnable { get { return rowheaderenable; } set { rowheaderenable = value; rowheaderpanel.Visible = value; topleftpanel.Visible = colheaderpanel.Visible && rowheaderpanel.Visible;  ContentInvalidateLayout(); } }
        /// <summary> Row header width </summary>
        public int RowHeaderWidth { get { return rowheaderwidth; } set { rowheaderwidth = value; InvalidateLayout(); } }
        /// <summary> Allow user to resize rows (if row is not autosized) </summary>
        public bool AllowUserToResizeRows { get; set; } = true;
        /// <summary> Allow user to select rows by clicking on header</summary>
        public bool AllowUserToSelectRows { get; set; } = true;
        /// <summary> Allow user to select multiple rows</summary>
        public bool AllowUserToSelectMultipleRows { get; set; } = true;
        /// <summary> Allow user to select cells individually </summary>
        public bool AllowUserToSelectCells { get; set; } = true;
        /// <summary> Allow user to drag cell selection to create a selection area</summary>
        public bool AllowUserToDragSelectCells { get; set; } = true;
        /// <summary> Clicking on a cell in a row selects the whole row </summary>
        public bool SelectCellSelectsRow { get; set; } = false;


        /// <summary> Callback when a row is selected or unselected (bool indicates)</summary>
        public Action<GLDataGridViewRow, bool> SelectedRow { get; set; } = null;
        /// <summary> Callback when a cell is selected or unselected (bool indicates)</summary>
        public Action<GLDataGridViewCell, bool> SelectedCell { get; set; } = null;      
        /// <summary> Callback when selection is cleared</summary>
        public Action SelectionCleared { get; set; } = null;                            

        /// <summary> First row on screen</summary>
        public int FirstDisplayIndex { get { return contentpanel.FirstDisplayIndex; } set { contentpanel.FirstDisplayIndex = value; UpdateScrollBar(); } }
        /// <summary> Last complete line on screen</summary>
        public int LastCompleteLine() {return contentpanel.LastCompleteLine(); }

        /// <summary> Class to hold a row, column, location and celllocation</summary>
        public class RowColPos
        {
            /// <summary> Row</summary>
            public int Row { get; set; }
            /// <summary> Column</summary>
            public int Column { get; set; }
            /// <summary> Location of click within cell </summary>
            public Point Location { get; set; }
            /// <summary> Cell location (top left) on screen </summary>
            public Point CellLocation { get; set; }
        }
        /// <summary> Call to find a cell at position. Returns null if no cell is present at that location</summary>
        public RowColPos FindCellAt(Point position) { return contentpanel.GridRowCol(position); }        

        /// <summary> Set cell border color </summary>
        public Color CellBorderColor { get { return cellbordercolor; } set { cellbordercolor = value; ContentInvalidate(); } }
        /// <summary> Set cell border width</summary>
        public int CellBorderWidth { get { return cellborderwidth; } set { cellborderwidth = value; ContentInvalidateLayout(); } }
        /// <summary> Set upper left cell back color</summary>
        public Color UpperLeftBackColor { get { return upperleftbackcolor; } set { upperleftbackcolor = value; topleftpanel.Invalidate(); } }

        public Color ScrollBar

        // pixel positions
        /// <summary> Find pixel left position of column</summary>
        public int ColumnPixelLeft(int column) { return columns.Where(x => x.Index < column).Select(y => y.Width).Sum() + cellborderwidth * column + cellborderwidth; }
        /// <summary> Find pixel right position of column</summary>
        public int ColumnPixelRight(int column) { return ColumnPixelLeft(column) + columns[column].Width; }
        /// <summary> Find pixel width of all columns, plus borders. Does not include row area. </summary>
        public int ColumnPixelWidth { get { return columns.Select(y=>y.Width).Sum() + cellborderwidth * (columns.Count+1); } }


        /// <summary> Callback, set to user paint column headers. Passed column, graphics and rectangle to draw in</summary>
        public Action<GLDataGridViewColumn, Graphics, Rectangle> UserPaintColumnHeaders { get; set; } = null;
        /// <summary> Callback, set to user paint row headers. Passed row, graphics and rectangle to draw in</summary>
        public Action<GLDataGridViewRow, Graphics, Rectangle> UserPaintRowHeaders { get; set; } = null;
        /// <summary> Callback, set to user paint top left header. Passed graphics and rectangle to draw in</summary>
        public Action<Graphics, Rectangle> UserPaintTopLeftHeader { get; set; } = null;

        /// <summary> Callback, user clicked on grid
        /// * row = -1 for column headers. 
        /// * col = -1 for row headers. 
        /// * -1,-1 for invalid area in content area, 
        /// * -2 -2 for top left  
        /// </summary>
        public Action<int, int, GLMouseEventArgs> MouseClickOnGrid;

        /// <summary> Context menu for content panel.
        /// The context menu Opening callback is fed with a tag with the class RowColPos so it knows what cell and location has been clicked on (row=col=-1 if none)</summary>
        public GLContextMenu ContextPanelContent;
        /// <summary> Context menu for column headers
        /// The context menu Opening callback is fed with a tag with the class RowColPos so it knows what column and location has been clicked on. (col=-1 for top left)</summary>
        public GLContextMenu ContextPanelColumnHeaders;     
        /// <summary> Context menu for row headers.
        /// The context menu Opening callback is fed with a tag with the class RowColPos so it knows what row and location has been clicked on. </summary>
        public GLContextMenu ContextPanelRowHeaders;      

        /// <summary> Construct with name and bounds</summary>
        public GLDataGridView(string name, Rectangle location) : base(name, location)
        {

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
            rowheaderpanel.contentpanel = contentpanel;
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

            BorderColorNI = DefaultDGVBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultDGVBackColor;
            cellbordercolor = DefaultDGVCellBorderColor;
            upperleftbackcolor = colheaderstyle.BackColor = rowheaderstyle.BackColor = DefaultDGVColumnRowBackColor;
            colheaderstyle.ForeColor = rowheaderstyle.ForeColor = DefaultDGVColumnRowForeColor;
            defaultcellstyle.BackColor = DefaultDGVCellBackColor;
            defaultcellstyle.ForeColor = DefaultDGVCellForeColor;
            colheaderstyle.SelectedColor = rowheaderstyle.SelectedColor = defaultcellstyle.SelectedColor = DefaultDGVCellSelectedColor;
            colheaderstyle.HighlightColor = rowheaderstyle.HighlightColor = defaultcellstyle.HighlightColor = DefaultDGVCellHighlightColor;  
            colheaderstyle.ContentAlignment = rowheaderstyle.ContentAlignment = defaultcellstyle.ContentAlignment = ContentAlignment.MiddleCenter;
            colheaderstyle.TextFormat = rowheaderstyle.TextFormat = defaultcellstyle.TextFormat = 0;
            colheaderstyle.Font = rowheaderstyle.Font = defaultcellstyle.Font = Font;
            colheaderstyle.Padding = rowheaderstyle.Padding = defaultcellstyle.Padding = new PaddingType(0);

            defaultaltrowcellstyle.Parent = defaultcellstyle;       // all null, uses cell style until overridden

            colheaderpanel.MouseClickColumnHeader += (col, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Click on {col} {SortColumn} {SortAscending}");
                if (AllowUserToSortColumns)
                    Sort(col, !SortAscending);
                else
                    MouseClickOnGrid?.Invoke(-1, col, e);
            };

            topleftpanel.MouseClickColumnHeader += (e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Click on top left");
                MouseClickOnGrid?.Invoke(-2, -2, e);
            };

            rowheaderpanel.MouseClickRowHeader += (row, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Click on row header {row}");
                MouseClickOnGrid?.Invoke(row, -1, e);
            };
            contentpanel.MouseClickOnGrid += (row, col, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Click on grid {row} {col}");
                MouseClickOnGrid?.Invoke(row, col, e);
            };

        }

        /// <summary> Default constructor </summary>
        public GLDataGridView() : this("DGV",DefaultWindowRectangle)
        {

        }

        /// <summary>  Create a row.  Rows must be created via this call</summary>
        public GLDataGridViewRow CreateRow()
        {
            GLDataGridViewRow row = new GLDataGridViewRow();
            row.Parent = this;
            row.DefaultCellStyle.Parent = defaultcellstyle;
            row.HeaderStyle.Parent = rowheaderstyle;
            row.Height = 24;
            return row;
        }

        /// <summary> Create a column. Columns must be created via this call. </summary>
        /// <param name="width">Column width in pixels</param>
        /// <param name="fillwidth">Column fill width</param>
        /// <param name="minwidth">Column minimum width in pixels</param>
        /// <param name="title">Column title</param>
        public GLDataGridViewColumn CreateColumn(int width = 50, int fillwidth = 100, int minwidth = 10, string title = "")
        {
            GLDataGridViewColumn col = new GLDataGridViewColumn();
            col.HeaderStyle.Parent = colheaderstyle;
            col.Parent = this;
            col.Width = width;
            col.FillWidth = fillwidth;
            col.MinimumWidth = minwidth;
            col.Text = title;
            return col;
        }

        /// <summary> Add a Column (at the end) to the grid </summary>
        public void AddColumn(GLDataGridViewColumn col)
        {
            System.Diagnostics.Debug.Assert(col.Parent == this && col.HeaderStyle.Parent != null);      // ensure created by us
            col.HeaderStyle.Changed += (e1) => { colheaderpanel.Invalidate(); };
            col.Changed += (e1, ci) => 
            {
                if (ci)         // if set, it means width has changed in some way
                {
                    autosizegeneration++;           // force autosize as we changed width
                    ContentInvalidateLayout();
                }
                else
                    colheaderpanel.Invalidate();
            };
            col.SetColNo(columns.Count);
            columns.Add(col);
            autosizegeneration++;           // force autosize as we changed columns
            ContentInvalidateLayout();
        }

        /// <summary> Remove a column</summary>
        public void RemoveColumn(int column)
        {
            foreach (var r in rows)
                r.RemoveCellAt(column);      // this will cause lots of row changed cells, causing an Invalidate.

            GLDataGridViewColumn col = columns[column];
            col.Parent = null;
            col.HeaderStyle.Parent = null;
            col.HeaderStyle.Changed = null;
            col.Changed = null;
            columns.RemoveAt(column);

            if (SortColumn == column)
                SortColumn = -1;

            for (int i = 0; i < columns.Count; i++)
                columns[i].SetColNo(i);

            autosizegeneration++;           // force autosize as we changed columns
            ContentInvalidateLayout();
        }

        /// <summary> Add a row. If insertatrow=-1, add to end. Else inserted before index</summary>
        public void AddRow(GLDataGridViewRow row, int insertatrow = -1)
        {
            System.Diagnostics.Debug.Assert(row.Parent == this && row.HeaderStyle.Parent != null);      // ensure created by us
            row.HeaderStyle.Changed += (e1) => { ContentInvalidateLayout(); };       // header style changed, need a complete refresh
            row.AutoSizeGeneration = 0;
            row.Changed += (e1) => 
            {
                contentpanel.RowChanged(row.Index);     // inform CP
                UpdateScrollBar();  // update scroll bar
            };

            row.SelectionChanged += (rw, cellno) =>
            {
                //System.Diagnostics.Debug.WriteLine($"Selection changed on {rw.Index} {cellno}");

                if (cellno == -1)       // if whole row select
                {
                    if (rw.Selected) // turning on
                    {
                        if (!AllowUserToSelectMultipleRows)     // if not allowed multirow, clear all
                            ClearSelection();

                        if (!selectedcells.ContainsKey(rw.Index))
                            selectedcells[rw.Index] = new HashSet<int>();

                        foreach (var c in rw.Cells)
                            selectedcells[rw.Index].Add(c.Index);

                        SelectedRow?.Invoke(rw, true);
                    }
                    else
                    {   // turning off
                        foreach (var c in rw.Cells)
                            selectedcells[rw.Index].Remove(c.Index);

                        if (selectedcells[rw.Index].Count == 0)
                            selectedcells.Remove(rw.Index);

                        SelectedRow?.Invoke(rw, false);
                    }
                }
                else if (rows[rw.Index].Cells[cellno].Selected)     // individual cell turning on
                {
                    if (!selectedcells.ContainsKey(rw.Index))
                        selectedcells[rw.Index] = new HashSet<int>();
                    selectedcells[rw.Index].Add(cellno);

                    SelectedCell?.Invoke(rows[rw.Index].Cells[cellno], true);
                }
                else
                {
                    selectedcells[rw.Index].Remove(cellno);     // or turning off

                    if (selectedcells[rw.Index].Count == 0)
                        selectedcells.Remove(rw.Index);

                    SelectedCell?.Invoke(rows[rw.Index].Cells[cellno], false);
                }

                contentpanel.RowChanged(row.Index);     // inform CP
            };

            if (insertatrow == -1)
            {
                row.SetRowNo(rows.Count, (rows.Count & 1) != 0 ? DefaultAltRowCellStyle : DefaultCellStyle);
                rows.Add(row);
                contentpanel.AddRow(row.Index);       // see if content panel needs redrawing
            }
            else
            {
                rows.Insert(insertatrow, row);
                for (int i = insertatrow; i < rows.Count; i++)
                    rows[i].SetRowNo(i, (i & 1) != 0 ? DefaultAltRowCellStyle : DefaultCellStyle);
                contentpanel.InsertRow(row.Index);       // see if content panel needs redrawing
            }

            UpdateScrollBar();
        }

        /// <summary> remove a row at row </summary>
        public void RemoveRow(int row)
        {
            GLDataGridViewRow rw = rows[row];
            rw.Parent = null;
            rw.DefaultCellStyle.Parent = null;
            rw.HeaderStyle.Parent = null;
            rw.HeaderStyle.Changed = null;
            rw.Changed = null;
            contentpanel.RemoveRow(row);
            rows.RemoveAt(row);
            for (int i = row; i < rows.Count; i++)
                rows[i].SetRowNo(i, (i & 1) != 0 ? DefaultAltRowCellStyle : DefaultCellStyle);
            UpdateScrollBar();
        }

        /// <summary> Clear the DGV </summary>
        public void Clear()
        {
            rows.Clear();
            ContentInvalidateLayout();
        }

            /// <summary> Adjust the column width to newwidth (in pixels). Will update FillWidth if required. </summary>
        public void SetColumnWidth(int column,int newwidth)
        {
           // System.Diagnostics.Debug.WriteLine($"Col {index} delta {newwidth}");
            var col = columns[column];
            if ( colfillmode == ColFillMode.Width)
            {
                col.Width = newwidth;
            }
            else
            {
                // only change this and columns to the right, as per winform DGV

                int cellpixels = columns.Where(x => x.Index >= column).Sum(x => x.Width);       // pixels for columns to resize
                float totalfill = columns.Where(x => x.Index >= column).Sum(x => x.FillWidth);               // fills for columns to resize

                float newfillwidth = newwidth * totalfill / cellpixels;     // compute out fill width from newwidth pixels

                float neededfromothers = newfillwidth - col.FillWidth;      // what we need to nick from the fill of others
                float othertotal = totalfill - col.FillWidth;               // the total fill of the others, ignoring out fill width
                float proportion = 1.0f - neededfromothers / othertotal;    // and the proportion to scale the others

               // System.Diagnostics.Debug.WriteLine($"ColWidth {col.Index} {cellpixels} {totalfill} new fill {newfillwidth} needed {neededfromothers} othertotal = {othertotal} proportion {proportion}");

                col.FillWidth = newfillwidth;

                for( int i = column+1; i < columns.Count; i++)
                {
                    columns[i].FillWidth *= proportion;
                }
            }
        }

        /// <summary> Sort on column, indicate if sort ascending (true) or decending 
        /// Glyph will be shown on column. SortColumn and SortAscending will be updated
        /// </summary>
        public void Sort(int column, bool sortascending)
        {
            if (column < columns.Count)
            {
                //System.Diagnostics.Debug.WriteLine($"Sort col {colno} by ascending {sortascending}");
                rows.Sort(delegate (GLDataGridViewRow l, GLDataGridViewRow r) 
                    {
                        if (column < l.Cells.Count)
                        {
                            if (column < r.Cells.Count)
                            {
                                if (columns[column].SortCompare != null)
                                    return columns[column].SortCompare(l.Cells[column], r.Cells[column]) * (sortascending ? +1 : -1);      // sort override on a per column basis
                                else
                                    return l.Cells[column].CompareTo(r.Cells[column]) * (sortascending ? +1 : -1);
                            }
                            else
                                return 1;
                        }
                        else 
                            return (column < r.Cells.Count) ? -1 : 0;
                    });

                if (SortColumn >= 0)
                    columns[SortColumn].SortGlyphAscending = null;

                SortColumn = column;
                SortAscending = sortascending;

                for (int i = 0; i < rows.Count; i++)
                    rows[i].SetRowNo(i, (i & 1) != 0 ? DefaultAltRowCellStyle : DefaultCellStyle);

                columns[column].SortGlyphAscending = sortascending;

                ContentInvalidateLayout();
            }
        }

        /// <summary> Clear all selections </summary>
        public void ClearSelection()
        {
            foreach (var kvp in selectedcells)
            {
                foreach (var cell in kvp.Value)
                {
                    rows[kvp.Key].Cells[cell].SelectedNI = false;
                }

                contentpanel.RowChanged(kvp.Key);     // inform CP
                rows[kvp.Key].SelectedNI = false;
            }

            selectedcells.Clear();
            SelectionCleared?.Invoke();
        }

        /// <summary> Get list of selected rows </summary>
        public List<GLDataGridViewRow> GetSelectedRows()
        {
            List<GLDataGridViewRow> set = new List<GLDataGridViewRow>();
            foreach (var kvp in selectedcells)
            {
                if (rows[kvp.Key].Selected)
                    set.Add(rows[kvp.Key]);
            }
            return set;
        }

        /// <summary> Get list of selected cells </summary>
        public List<GLDataGridViewCell> GetSelectedCells()
        {
            List<GLDataGridViewCell> set = new List<GLDataGridViewCell>();
            foreach (var kvp in selectedcells)
            {
                foreach (var c in kvp.Value)
                    set.Add(rows[kvp.Key].Cells[c]);
            }
            return set;
        }

        /// <summary> Scroll the grid up or down one line </summary>
        public void Scroll(int delta)
        {
            if (delta > 0)
                FirstDisplayIndex--;
            else if (LastCompleteLine() < rows.Count - 1)
                FirstDisplayIndex++;
        }

        #region Implementation

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.PerformRecursiveLayout"/>
        protected override void PerformRecursiveLayout()     
        {
            // set before children layout set up some basic parameters of the children

            colheaderpanel.Height = columnheaderheight + cellborderwidth;        
            rowheaderpanel.Width = rowheaderwidth + cellborderwidth;
            rowheaderpanel.DockingMargin = new MarginType(0, ColumnHeaderEnable ? colheaderpanel.Height : 0, 0, 0);
            topleftpanel.Size = new Size(rowheaderwidth + cellborderwidth, columnheaderheight + cellborderwidth);
            colheaderpanel.BackColor = rowheaderpanel.BackColor = topleftpanel.BackColor = contentpanel.BackColor = BackColor;

            base.PerformRecursiveLayout();      // do layout on children.

            
            UpdateScrollBar();                  // update the scroll bar since its affected by sizing
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
                vertscroll.SetValueMaximumLargeChange(0, rows.Count - 1, rows.Count);
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

                if (row.AutoSize)
                    PerformAutoSize(row);

                vpos += row.Height + cellborderwidth;
                if (vpos < maxbitmapheight)     // if ending vpos < height, its completely displayed
                    lastcompleterow = start;
                start += dir;
            }

            //  System.Diagnostics.Debug.WriteLine($"Compute Height on {start} maxh {maxbitmapheight} last tow {lastcompleterow} vpos {vpos}");

            return new Tuple<int, int>(lastcompleterow, vpos);
        }

        internal void PerformAutoSize(GLDataGridViewRow r)
        {
            if (r.AutoSizeGeneration != autosizegeneration)
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

                r.SetAutoSizeHeight(autosizegeneration, maxh);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void OnResize()
        {
            autosizegeneration++;           // we will be doing a content realignment, perform autosize
            base.OnResize();    // do after
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnGlobalFocusChanged(GLBaseControl, GLBaseControl)"/>
        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            colheaderstyle.Font = rowheaderstyle.Font = defaultcellstyle.Font = Font;
            autosizegeneration++;
            ContentInvalidateLayout();
        }

        private List<GLDataGridViewColumn> columns = new List<GLDataGridViewColumn>();
        private List<GLDataGridViewRow> rows = new List<GLDataGridViewRow>();

        private GLDataGridViewCellStyle defaultcellstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle defaultaltrowcellstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle rowheaderstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle colheaderstyle = new GLDataGridViewCellStyle();

        private ColFillMode colfillmode = ColFillMode.Width;
        private int cellborderwidth = 1;
        private int columnheaderheight = 40;
        private bool columnheaderenable = true;

        private int rowheaderwidth = 40;
        private bool rowheaderenable = true;

        private Color cellbordercolor;
        private Color upperleftbackcolor;

        private GLHorizontalScrollBar horzscroll;
        private GLVerticalScrollBar vertscroll;
        private GLDataGridViewContentPanel contentpanel;
        private GLDataGridViewColumnHeaderPanel colheaderpanel;
        private GLDataGridViewRowHeaderPanel rowheaderpanel;
        private GLDataGridViewTopLeftHeaderPanel topleftpanel;

        private Dictionary<int, HashSet<int>> selectedcells = new Dictionary<int, HashSet<int>>();

        private uint autosizegeneration = 1;  

        #endregion
    }

}
