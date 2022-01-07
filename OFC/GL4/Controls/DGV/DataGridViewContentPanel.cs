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

using GLOFC.Timers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{

    public class GLDataGridViewContentPanel : GLPanel
    {
        public Action<int, int, GLMouseEventArgs> MouseClickOnGrid;                // row (-1 outside bounds), col = -1 for row header
        public int HorzScroll { get { return ScrollOffset.X; } set { ScrollOffset = new Point(value, ScrollOffset.Y); Invalidate(); } }
        public int DepthMult { get; set; } = 3;
        public int FirstDisplayIndex { get { return firstdisplayindex; } set { MoveTo(value); } }
        public int LastCompleteLine()       // last line on screen completely
        {
            if (gridfirstline != -1)
            {
                int off = FirstDisplayIndex - gridfirstline;
                int topvpos = gridrowoffsets[off];
                while (off < gridrowoffsets.Count-1 && (gridrowoffsets[off+1]-topvpos) < ClientHeight)
                    off++;

                return gridfirstline + off - 1;
            }
            else
                return -1;
        }

        public GLDataGridViewContentPanel(string name, GLDataGridViewRowHeaderPanel rowheaderpanel, Rectangle location) : base(name, location)
        {
            this.rowheaderpanel = rowheaderpanel;
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = Color.Red;// DefaultVerticalScrollPanelBackColor;

            autoscroll.Tick += (t, tick) =>
            {
                GLDataGridView dgv = Parent as GLDataGridView;

                if (lastmousemove.Y > 0)
                {
                    //System.Diagnostics.Debug.WriteLine($"First line {dgv.FirstDisplayIndex} last complete {dgv.LastCompleteLine()}");
                    if (LastCompleteLine() < dgv.Rows.Count - 1)     // and scroll to the end, until all lines are on screen
                        dgv.FirstDisplayIndex++;
                }
                else
                {
                    if (FirstDisplayIndex > 0)
                        FirstDisplayIndex--;
                }
                UpdateSelection(lastmousemove);
            };

        }

        public void Redraw()            // forced redraw, maybe because a new column has been added..
        {
            gridredraw = true;
            Invalidate();
        }
        public void AddRow(int index)
        {
            if (gridlastcompleteline == -1 ||         // not drawn anything
                girddrawndepth < Height ||            // or not drawn a whole screen
                (index >= gridfirstline && index <= gridlastcompleteline + 1))     // or index inside grid
            {
                //System.Diagnostics.Debug.WriteLine($"Content Add row {index} {LevelBitmapfirstline}..{LevelBitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }
        public void InsertRow(int index)
        {
            if (gridlastcompleteline == -1 ||         // not drawn anything
                girddrawndepth < Height ||            // or not drawn a whole screen
                index <= gridlastcompleteline + 1)     // or index before or up to end +1 of grid
            {
               // System.Diagnostics.Debug.WriteLine($"Content Insert row {index} {LevelBitmapfirstline}..{LevelBitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }
        public void RemoveRow(int index)
        {
            if (index >= gridfirstline && index <= gridlastcompleteline + 1)        // if within (incl half painted end row), need a redraw
            {
                //System.Diagnostics.Debug.WriteLine($"Content row remove {index} inside grid {LevelBitmapfirstline}.. {LevelBitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }

        // given row, see if it affects the contents of this panel
        public bool RowChanged(int index)
        {
            if (index >= gridfirstline && index <= gridlastcompleteline + 1)        // if within (incl half painted end row), need a redraw
            {
                //System.Diagnostics.Debug.WriteLine($"Content row changed {index} inside grid {LevelBitmapfirstline}.. {LevelBitmaplastcompleteline + 1} redraw");
                Redraw();
                return true;
            }
            else
                return false;
        }

        private void MoveTo(int fdl)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            firstdisplayindex = dgv.Rows.Count > 0 ? Math.Max(0,Math.Min(fdl, dgv.Rows.Count)) : 0;

            // if FDL is within the drawn range of the bitmap

            if (firstdisplayindex >= gridfirstline && firstdisplayindex <= gridlastcompleteline)
            {
                // offset of lines in the draw
                int lineoffset = firstdisplayindex - gridfirstline;
                // this will be our new scroll position..
                int ystart = gridrowoffsets[lineoffset];
                // what we have left in the bitmap
                int depthleft = girddrawndepth - ystart;

                //                System.Diagnostics.Debug.WriteLine($"Move to {firstdisplayindex} lo {lineoffset} ys {ystart} {depthleft} >= {ClientHeight}");

                // if enough bitmap left.. OR we drew complete the last line, nothing to redraw. Move to pos
                if (depthleft >= ClientHeight || gridlastcompleteline == dgv.Rows.Count - 1)
                {
                    ScrollOffset = new Point(ScrollOffset.X, ystart);      // move the image down to this position
                    Invalidate();                           // invalidate only, no need to redraw
                    return;
                }
            }

            Redraw();
        }

        // we have just been layedout by parent, we know our size
        // set up columns 

        protected override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();

            GLDataGridView dgv = Parent as GLDataGridView;

            if (dgv.ColumnFillMode == GLDataGridView.ColFillMode.FillWidth)
            {
                int pixelsforborder = (dgv.Columns.Count + 1) * dgv.CellBorderWidth;
                int cellpixels = Width - pixelsforborder;
                float filltotalallcolumns = dgv.Columns.Select(x => x.FillWidth).Sum();

                bool[] minwidths = new bool[dgv.Columns.Count];

                float hfillfinaltotal = 0;

                foreach (var col in dgv.Columns)
                {
                    int width = (int)(cellpixels * col.FillWidth / filltotalallcolumns);         // candidate width for all columns
                    if (width < col.MinimumWidth)                                       // if less than min width, set it to that, take this column out of the fill equation
                    {
                        col.WidthNI = col.MinimumWidth;
                        cellpixels -= col.Width;
                        minwidths[col.Index] = true;
                        //System.Diagnostics.Debug.WriteLine($"{col.Index} less than min width");
                    }
                    else
                        hfillfinaltotal += col.FillWidth;
                }

                //System.Diagnostics.Debug.WriteLine($"{hfillfinaltotal} cell pixels {cellpixels}");
                int pixels = 0;

                foreach (var col in dgv.Columns)
                {
                    if (!minwidths[col.Index])  // if not min width it, set it to the width, to the column minimum width
                    {
                        col.WidthNI = Math.Max(col.MinimumWidth, (int)(cellpixels * col.FillWidth / hfillfinaltotal));
                        // System.Diagnostics.Debug.WriteLine($"{col.Index} auto size to {col.Width}");
                        pixels += col.Width;
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"Cell pixels {cellpixels} total {pixels}");

                int colno = 0;
                while (pixels < cellpixels && dgv.Columns.Count > 0)      // add 1 pixel to each column in turn until back to count
                {
                    dgv.Columns[colno].WidthNI += 1;
                    // System.Diagnostics.Debug.WriteLine($"Distribute pixel to {colno}");
                    pixels++;
                    colno = (colno + 1) % dgv.Columns.Count;
                }
            }
            else
            {   // normal width fill ,just make sure not below min width
                foreach (var col in dgv.Columns)
                {
                    if (col.Width < col.MinimumWidth)
                        col.WidthNI = col.MinimumWidth;
                }
            }

            //int hpos = dgv.CellBorderWidth;   foreach (var col in dgv.Columns) { System.Diagnostics.Debug.WriteLine($"Col {col.Index} {hpos} {col.Width}"); hpos += dgv.CellBorderWidth + col.Width; }

            int gridwidth = Math.Max(1, dgv.ColumnPixelWidth);      // width of grid

            if (LevelBitmap == null || LevelBitmap.Width < gridwidth || LevelBitmap.Height < ClientHeight * 2)   // if bitmap not there, or different width needed
            {
                MakeLevelBitmap(Math.Max(gridwidth, 10), Math.Max(Height * DepthMult, 10));
                // System.Diagnostics.Debug.WriteLine($"Make Grid bitmap {LevelBitmap.Width} {LevelBitmap.Height}");
                gridredraw = true;
            }
        }

        protected override void OnResize()
        {
            base.OnResize();
            Redraw();
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            if (gridredraw)    // do we need to redisplay
            {
                DrawTable();
                gridredraw = false;
            }

            // the drawn rectangle is whats left of the bitmap after ScrollOffset..

            Rectangle drawarea = new Rectangle(0, 0, LevelBitmap.Width - ScrollOffset.X, LevelBitmap.Height - ScrollOffset.Y);
            gr.DrawImage(LevelBitmap, drawarea, ScrollOffset.X, ScrollOffset.Y, drawarea.Width, drawarea.Height, GraphicsUnit.Pixel);

            if ( rowheaderpanel.Visible)
                rowheaderpanel.Redraw(ScrollOffset.Y);
        }


        private void DrawTable()
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            //System.Diagnostics.Debug.WriteLine($"Redraw Table");
            int backup = 10;

            while (true)
            {
                using (Graphics gr = Graphics.FromImage(LevelBitmap))
                {
                    gr.Clear(Color.Transparent);

                    gridrowoffsets.Clear();

                    if (dgv.Columns.Count == 0 || dgv.Rows.Count == 0)
                    {
                        ScrollOffset = Point.Empty;
                        rowheaderpanel.DrawEmpty();
                        return;
                    }

                    gridfirstline = Math.Max(0, firstdisplayindex - backup);      // back up a little

                    using (Brush b = new SolidBrush(dgv.CellBorderColor))
                    {
                        using (Pen p = new Pen(b, dgv.CellBorderWidth))
                        {
                            int gridwidth = dgv.ColumnPixelWidth-1;      // width of grid incl cell borders
                            int vpos = 0;

                            for (var rowno = gridfirstline; rowno < dgv.Rows.Count && vpos < LevelBitmap.Height; rowno++)
                            {
                                var row = dgv.Rows[rowno];

                                if ( row.AutoSize && row.AutoSizeGeneration != dgv.AutoSizeGeneration)
                                {
                                    dgv.PerformAutoSize(row);
                                }

                                gridrowoffsets.Add(vpos);       // this row at this border line offset

                                if (dgv.CellBorderWidth > 0)
                                {
                                    gr.DrawLine(p, 0, vpos, gridwidth, vpos);       // draw a line across the top
                                    vpos += dgv.CellBorderWidth;
                                }

                                int hpos = dgv.CellBorderWidth;

                                for (int i = 0; i < dgv.Columns.Count; i++)
                                {
                                    var col = dgv.Columns[i];

                                    if (i < row.CellCount)
                                    {
                                        var cell = row.Cells[i];
                                        Rectangle area = new Rectangle(hpos, vpos, col.Width, row.Height);
                                        cell.Paint(gr, area);
                                    }
                                    else
                                        break;

                                    hpos += col.Width + dgv.CellBorderWidth;
                                }

                                vpos += row.Height;
                                //    System.Diagnostics.Debug.WriteLine($"Row {rowno} Start {gridrowoffsets.Last()}..{vpos} bitmap H {LevelBitmap.Height}");
                                if (vpos < LevelBitmap.Height)
                                    gridlastcompleteline = rowno;
                            }

                            int endpos = Math.Min(vpos + 1, LevelBitmap.Height);  // maximum height (not line) we drew to is vpos or the end of the bitmap
                            gridrowoffsets.Add(endpos);       // add final row vpos for searching purposes
                            girddrawndepth = endpos;

                            if (dgv.CellBorderWidth > 0)
                            {
                                gr.DrawLine(p, 0, vpos, gridwidth, vpos);   // final bottom line

                                int hpos = 0;

                                for (int i = 0; i < dgv.Columns.Count; i++)
                                {
                                  //  System.Diagnostics.Debug.WriteLine($"Cell boundary {hpos}");
                                    gr.DrawLine(p, hpos, 0, hpos, vpos);    // each column one
                                    hpos += dgv.CellBorderWidth + dgv.Columns[i].Width;
                                }

                                gr.DrawLine(p, hpos, 0, hpos, vpos);        // final one
                            }
                        }
                    }

                    if (firstdisplayindex > gridlastcompleteline)     // if not got to first display index, backup too much, decrease
                    {
                        if (backup > 0)
                        {
                            backup /= 2;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"DGV **** Error in drawing {firstdisplayindex} {gridfirstline} {gridlastcompleteline}");
                            return;
                        }
                    }
                    else
                    {
                        int ystart = gridrowoffsets[firstdisplayindex - gridfirstline];
                        //System.Diagnostics.Debug.WriteLine($"Draw grid backed {backup} {LevelBitmapfirstline}..{firstdisplayindex}..{LevelBitmaplastcompleteline} {LevelBitmapdrawndepth}");

                        // if we backed up, and the backup is very large, we may not have enough bitmap to draw into to fill up the client area
                        // this does not apply if we drew to the end
                        // and stop it continuing forever just in case with backup>0
                        if (backup > 0 && gridlastcompleteline != dgv.Rows.Count - 1 &&
                             girddrawndepth < ystart + Height)      // what we have drawn is less than ystart (y=0 on client) + client height
                        {
                            backup /= 2;
                        }
                        else
                        {
                            ScrollOffset = new Point(ScrollOffset.X, ystart);

                            if ( rowheaderpanel.Visible)
                                rowheaderpanel.DrawHeaders(gridfirstline, gridlastcompleteline + 1, girddrawndepth, ystart);

                            break;
                        }
                    }
                }
            }
        }

        GLDataGridViewCell currentcell = null;

        protected override void OnMouseLeave(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (currentcell != null)
            {
                currentcell.OnMouseLeaveCell(e);
                currentcell = null;
            }
        }
        protected override void OnMouseEnter(GLMouseEventArgs e)
        {
            base.OnMouseEnter(e);
            MoveToCell(e);
        }

        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            GLDataGridView dgv = Parent as GLDataGridView;

            if ( selectionstart != null )
            {
                if (!dgv.AllowUserToDragSelectCells)     // disable drag
                    return;

                if (e.Location.Y >= 0 && e.Location.Y <= ClientHeight)      // if within window, no scroll
                    autoscroll.Stop();
                else if ( !autoscroll.Running )
                    autoscroll.Start(50, 200);                              // else autoscroll

                lastmousemove = e.Location;     // record this for autoscroll purposes
                UpdateSelection(e.Location);
            }
            else
            {
                MoveToCell(e);
            }
        }

        protected override void OnMouseDown(GLMouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                GLDataGridView dgv = Parent as GLDataGridView;

                var g = GridRowCol(e.Location);
                if (g != null)
                {
                    var newcell = CellPara(g, e);       // may return null if cell not there
                    newcell?.OnMouseDownCell(e);

                    if (!e.Handled && dgv.AllowUserToSelectCells && g.Column < dgv.Rows[g.Row].CellCount)
                    {
                        lastselectionstart = lastselectionend = selectionstart = g;
                        dgv.ClearSelection();
                        dgv.Rows[g.Row].Cells[g.Column].Selected = true;
                    }
                }
            }
        }

        protected override void OnMouseUp(GLMouseEventArgs e)
        {
            base.OnMouseUp(e);
            autoscroll.Stop();
            lastselectionstart = lastselectionend = selectionstart = null;

            if ( e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                GLDataGridView dgv = Parent as GLDataGridView;

                var g = GridRowCol(e.Location);
                if (g != null)
                {
                    var newcell = CellPara(g, e);       // may return null if cell not there
                    newcell?.OnMouseUpCell(e);
                }
            }
        }

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);

            GLDataGridView dgv = Parent as GLDataGridView;
            var g = GridRowCol(e.Location);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
            {
                if (lastselectionstart == lastselectionend)
                {
                    if (g != null)
                    {
                        var orgbounds = e.Bounds;
                        var orgbloc = e.BoundsLocation;
                        var orgloc = e.Location;

                        var newcell = CellPara(g,e);        // may return null if cell not there
                        newcell?.OnMouseClickCell(e);

                        if (e.Handled == false) // and if it did not, call global click
                        {
                            e.Bounds = orgbounds;
                            e.BoundsLocation = orgbloc;
                            e.Location = orgloc;
                            MouseClickOnGrid(g.Row, g.Column, e);
                        }
                    }
                    else
                        MouseClickOnGrid(-1, -1, e);
                }
            }
            else if ( e.Button == GLMouseEventArgs.MouseButtons.Right)
            {
                if (dgv.ContextPanelContent != null)
                {
                    if (g == null)      // out of cell, return pos of click
                        g = new GLDataGridView.RowColPos() { Column = -1, Row = -1, Location = e.Location };

                    dgv.ContextPanelContent.Show(FindDisplay(), e.ScreenCoord, opentag:g);
                }
            }
        }

        protected override void OnMouseWheel(GLMouseEventArgs e)
        {
            base.OnMouseWheel(e);
            GLDataGridView dgv = Parent as GLDataGridView;
            dgv.Scroll(e.Delta);
        }

        public void MoveToCell(GLMouseEventArgs e)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            var g = GridRowCol(e.Location);
            if (g != null)
            {
                if (g.Column < dgv.Rows[g.Row].CellCount)
                {
                    var newcell = CellPara(g, e);
                    if (newcell == currentcell)
                    {
                        currentcell.OnMouseMoveCell(e);
                    }
                    else
                    {
                        if (currentcell != null)
                            currentcell.OnMouseLeaveCell(e);
                        currentcell = newcell;
                        currentcell.OnMouseEnterCell(e);
                    }
                    return;
                }
            }
            if (currentcell != null)
            {
                currentcell.OnMouseLeaveCell(e);
                currentcell = null;
            }
        }

        private GLDataGridViewCell CellPara(GLDataGridView.RowColPos g, GLMouseEventArgs e)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            if (g.Column < dgv.Rows[g.Row].CellCount)
            {
                var newcell = dgv.Rows[g.Row].Cells[g.Column];
                e.Bounds = new Rectangle(g.CellLocation.X, g.CellLocation.Y, dgv.Columns[g.Column].Width, dgv.Rows[g.Row].Height);
                e.BoundsLocation = g.Location;
                e.Location = new Point(g.Location.X - newcell.Style.Padding.Left, g.Location.Y - newcell.Style.Padding.Top);
                return newcell;
            }
            else
                return null;
        }

        private void UpdateSelection(Point loc)
        {
            var g = GridRowCol(loc);

            if (g != null)
            {
                GLDataGridView dgv = Parent as GLDataGridView;

                // get the min/max ranges of selection, which can be the minimum/max of start/end and current loc

                int minrow = ObjectExtensionsNumbersBool.Min(lastselectionstart.Row, lastselectionend.Row, g.Row);
                int maxrow = ObjectExtensionsNumbersBool.Max(lastselectionstart.Row, lastselectionend.Row, g.Row);
                int mincol = ObjectExtensionsNumbersBool.Min(lastselectionstart.Column, lastselectionend.Column, g.Column);
                int maxcol = ObjectExtensionsNumbersBool.Max(lastselectionstart.Column, lastselectionend.Column, g.Column);

                //System.Diagnostics.Debug.WriteLine($"Cursor {loc} at {g.Row} {g.Column} minr {minrow} maxr {maxrow}");

                for (int row = minrow; row <= maxrow; row++)
                {
                    for (int col = mincol; col <= maxcol && col < dgv.Rows[row].CellCount; col++)
                    {
                        bool selrow = g.Row < selectionstart.Row ? row >= g.Row && row <= selectionstart.Row : row >= selectionstart.Row && row <= g.Row;
                        bool selcol = g.Column < selectionstart.Column ? col >= g.Column && col <= selectionstart.Column : col >= selectionstart.Column && col <= g.Column;
                        //       System.Diagnostics.Debug.WriteLine($"{col} {row} = {selrow} {selcol}");
                        dgv.Rows[row].Cells[col].Selected = selrow && selcol;
                    }
                }

                lastselectionstart = selectionstart;
                lastselectionend = g;
            }
        }

        // returns real row number
        public GLDataGridView.RowColPos GridRowCol(Point p)
        {
            if (gridrowoffsets.Count > 0)
            {
                int y = ScrollOffset.Y + Math.Max(0,Math.Min(p.Y,ClientHeight));      // if mouse if captured, y may be well beyond grid, clip to it

                int gridrow = gridrowoffsets.FindLastIndex(a => a < y);

                if (gridrow >= 0 && gridrow < gridrowoffsets.Count-1)        // last entry is end of grid, either reject or accept
                {
                    GLDataGridView dgv = Parent as GLDataGridView;
                    
                    int xoffset = ScrollOffset.X + Math.Max(0,Math.Min(p.X,ClientWidth));     // clip X

                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        int left = dgv.ColumnPixelLeft(i);
                        if (xoffset >= left && xoffset < left + dgv.Columns[i].Width)
                        {
                            Point off = new Point(xoffset - left, y - gridrowoffsets[gridrow]);
                            Point cellloc = new Point(left, gridrowoffsets[gridrow] + dgv.CellBorderWidth);
                            gridrow += gridfirstline;
                            return new GLDataGridView.RowColPos() { Row = gridrow, Column = i, Location = off, CellLocation = cellloc };
                        }
                    }
                }
            }

            return null;
        }

        private int firstdisplayindex = 0;
        private int gridfirstline = -1;
        private int gridlastcompleteline = -1;
        private int girddrawndepth = -1;
        private bool gridredraw = true;
        private List<int> gridrowoffsets = new List<int>();     // cell boundary pixel upper of cell line on Y

        private GLDataGridViewRowHeaderPanel rowheaderpanel;

        Timer autoscroll = new Timer();
        private Point lastmousemove;
        private GLDataGridView.RowColPos selectionstart;                 // real row numbers
        private GLDataGridView.RowColPos lastselectionstart;
        private GLDataGridView.RowColPos lastselectionend;

    }
}



