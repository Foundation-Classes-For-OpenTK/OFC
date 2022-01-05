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
            if (gridbitmapfirstline != -1)
            {
                int off = FirstDisplayIndex - gridbitmapfirstline;
                int topvpos = gridrowoffsets[off];
                while (off < gridrowoffsets.Count-1 && (gridrowoffsets[off+1]-topvpos) < ClientHeight)
                    off++;

                return gridbitmapfirstline + off - 1;
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
            if (gridbitmaplastcompleteline == -1 ||         // not drawn anything
                gridbitmapdrawndepth < Height ||            // or not drawn a whole screen
                (index >= gridbitmapfirstline && index <= gridbitmaplastcompleteline + 1))     // or index inside grid
            {
                //System.Diagnostics.Debug.WriteLine($"Content Add row {index} {gridbitmapfirstline}..{gridbitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }
        public void InsertRow(int index)
        {
            if (gridbitmaplastcompleteline == -1 ||         // not drawn anything
                gridbitmapdrawndepth < Height ||            // or not drawn a whole screen
                index <= gridbitmaplastcompleteline + 1)     // or index before or up to end +1 of grid
            {
               // System.Diagnostics.Debug.WriteLine($"Content Insert row {index} {gridbitmapfirstline}..{gridbitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }
        public void RemoveRow(int index)
        {
            if (index >= gridbitmapfirstline && index <= gridbitmaplastcompleteline + 1)        // if within (incl half painted end row), need a redraw
            {
                //System.Diagnostics.Debug.WriteLine($"Content row remove {index} inside grid {gridbitmapfirstline}.. {gridbitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }

        // given row, see if it affects the contents of this panel
        public bool RowChanged(int index)
        {
            if (index >= gridbitmapfirstline && index <= gridbitmaplastcompleteline + 1)        // if within (incl half painted end row), need a redraw
            {
                //System.Diagnostics.Debug.WriteLine($"Content row changed {index} inside grid {gridbitmapfirstline}.. {gridbitmaplastcompleteline + 1} redraw");
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

            if (firstdisplayindex >= gridbitmapfirstline && firstdisplayindex <= gridbitmaplastcompleteline)
            {
                // offset of lines in the draw
                int lineoffset = firstdisplayindex - gridbitmapfirstline;
                // this will be our new scroll position..
                int ystart = gridrowoffsets[lineoffset];
                // what we have left in the bitmap
                int depthleft = gridbitmapdrawndepth - ystart;

                //                System.Diagnostics.Debug.WriteLine($"Move to {firstdisplayindex} lo {lineoffset} ys {ystart} {depthleft} >= {ClientHeight}");

                // if enough bitmap left.. OR we drew complete the last line, nothing to redraw. Move to pos
                if (depthleft >= ClientHeight || gridbitmaplastcompleteline == dgv.Rows.Count - 1)
                {
                    ScrollOffset = new Point(ScrollOffset.X, ystart);      // move the image down to this position
                    Invalidate();                           // invalidate only, no need to redraw
                    return;
                }
            }

            Redraw();
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            int gridwidth = Math.Max(1,dgv.ColumnPixelWidth);      // width of grid

            if (gridbitmap == null || gridbitmap.Width < gridwidth)   // if bitmap not there, or different width needed
            {
                gridbitmap?.Dispose();
                gridbitmap = new Bitmap(Math.Max(gridwidth,10), Math.Max(Height * DepthMult,10));
                //System.Diagnostics.Debug.WriteLine($"Grid bitmap {gridbitmap.Width} {gridbitmap.Height}");
                gridredraw = true;
            }

            if (gridredraw)    // do we need to redisplay
            {
                DrawTable();
                gridredraw = false;
            }

            // the drawn rectangle is whats left of the bitmap after ScrollOffset..
            Rectangle drawarea = new Rectangle(0, 0, gridbitmap.Width - ScrollOffset.X, gridbitmap.Height - ScrollOffset.Y);
            gr.DrawImage(gridbitmap, drawarea, ScrollOffset.X, ScrollOffset.Y, drawarea.Width, drawarea.Height, GraphicsUnit.Pixel);

            if ( rowheaderpanel.Visible)
                rowheaderpanel.Redraw(ScrollOffset.Y);
        }


        private void DrawTable()
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            int backup = 10;

            while (true)
            {
                using (Graphics gr = Graphics.FromImage(gridbitmap))
                {
                    gr.Clear(Color.Transparent);

                    gridrowoffsets.Clear();

                    if (dgv.Columns.Count == 0 || dgv.Rows.Count == 0)
                    {
                        ScrollOffset = Point.Empty;
                        rowheaderpanel.DrawEmpty();
                        return;
                    }

                    gridbitmapfirstline = Math.Max(0, firstdisplayindex - backup);      // back up a little

                    using (Brush b = new SolidBrush(dgv.CellBorderColor))
                    {
                        using (Pen p = new Pen(b, dgv.CellBorderWidth))
                        {
                            int gridwidth = dgv.ColumnPixelWidth-1;      // width of grid incl cell borders
                            int vpos = 0;

                            for (var rowno = gridbitmapfirstline; rowno < dgv.Rows.Count && vpos < gridbitmap.Height; rowno++)
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

                                    if (i < row.Cells.Count)
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
                                //    System.Diagnostics.Debug.WriteLine($"Row {rowno} Start {gridrowoffsets.Last()}..{vpos} bitmap H {gridbitmap.Height}");
                                if (vpos < gridbitmap.Height)
                                    gridbitmaplastcompleteline = rowno;
                            }

                            int endpos = Math.Min(vpos + 1, gridbitmap.Height);  // maximum height (not line) we drew to is vpos or the end of the bitmap
                            gridrowoffsets.Add(endpos);       // add final row vpos for searching purposes
                            gridbitmapdrawndepth = endpos;

                            if (dgv.CellBorderWidth > 0)
                            {
                                gr.DrawLine(p, 0, vpos, gridwidth, vpos);   // final bottom line

                                int hpos = 0;

                                for (int i = 0; i < dgv.Columns.Count; i++)
                                {
                                    gr.DrawLine(p, hpos, 0, hpos, vpos);    // each column one
                                    hpos += dgv.CellBorderWidth + dgv.Columns[i].Width;
                                }

                                gr.DrawLine(p, hpos, 0, hpos, vpos);        // final one
                            }
                        }
                    }

                    if (firstdisplayindex > gridbitmaplastcompleteline)     // if not got to first display index, backup too much, decrease
                    {
                        if (backup > 0)
                        {
                            backup /= 2;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"DGV **** Error in drawing {firstdisplayindex} {gridbitmapfirstline} {gridbitmaplastcompleteline}");
                            return;
                        }
                    }
                    else
                    {
                        int ystart = gridrowoffsets[firstdisplayindex - gridbitmapfirstline];
                        //System.Diagnostics.Debug.WriteLine($"Draw grid backed {backup} {gridbitmapfirstline}..{firstdisplayindex}..{gridbitmaplastcompleteline} {gridbitmapdrawndepth}");

                        // if we backed up, and the backup is very large, we may not have enough bitmap to draw into to fill up the client area
                        // this does not apply if we drew to the end
                        // and stop it continuing forever just in case with backup>0
                        if (backup > 0 && gridbitmaplastcompleteline != dgv.Rows.Count - 1 &&
                             gridbitmapdrawndepth < ystart + Height)      // what we have drawn is less than ystart (y=0 on client) + client height
                        {
                            backup /= 2;
                        }
                        else
                        {
                            ScrollOffset = new Point(ScrollOffset.X, ystart);

                            if ( rowheaderpanel.Visible)
                                rowheaderpanel.DrawHeaders(gridbitmapfirstline, gridbitmaplastcompleteline + 1, gridbitmapdrawndepth, ystart);

                            break;
                        }
                    }
                }
            }
        }

        protected override void OnResize()
        {
            base.OnResize();

            // if we made the client big, but the previous bitmap is small, then do a redraw with a new bitmap
            if (gridbitmap != null && gridbitmap.Height < Height * 2)       // less than minumum overlap
            {
                gridbitmap?.Dispose();
                gridbitmap = null;
            }

            Redraw();
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
                    if (dgv.AllowUserToSelectCells && g.Column < dgv.Rows[g.Row].Cells.Count)
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
                        MouseClickOnGrid(g.Row, g.Column, e);
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
                    for (int col = mincol; col <= maxcol && col < dgv.Rows[row].Cells.Count; col++)
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
                            Point off = new Point(xoffset-left, y-gridrowoffsets[gridrow]);
                            gridrow += gridbitmapfirstline;
                            return new GLDataGridView.RowColPos() { Row = gridrow, Column = i, Location = off };
                        }
                    }
                }
            }

            return null;
        }

        private int firstdisplayindex = 0;
        private Bitmap gridbitmap = null;
        private int gridbitmapfirstline = -1;
        private int gridbitmaplastcompleteline = -1;
        private int gridbitmapdrawndepth = -1;
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



