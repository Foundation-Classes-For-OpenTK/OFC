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

    public class GLDataGridViewContentPanel : GLPanel
    {
        public Action<int, int, GLMouseEventArgs> MouseClickOnGrid;                // row, col = -1 for row header
        public int FirstDisplayIndex { get { return firstdisplayindex; } set { MoveTo(value); } }
        public int HorzScroll { get { return gridoffset.X; } set { gridoffset = new Point(value, gridoffset.Y); Invalidate(); } }
        public int DepthMult { get; set; } = 3;

        public GLDataGridViewContentPanel(string name, GLDataGridViewRowHeaderPanel rowheaderpanel, Rectangle location) : base(name, location)
        {
            this.rowheaderpanel = rowheaderpanel;
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = Color.Red;// DefaultVerticalScrollPanelBackColor;
        }

        public void Redraw()            // forced redraw, maybe because a new column has been added..
        {
            gridbitmaplastcompleteline = -1;
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
                System.Diagnostics.Debug.WriteLine($"Content Insert row {index} {gridbitmapfirstline}..{gridbitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }
        public void RemoveRow(int index)
        {
            if (index >= gridbitmapfirstline && index <= gridbitmaplastcompleteline + 1)        // if within (incl half painted end row), need a redraw
            {
                System.Diagnostics.Debug.WriteLine($"Content row remove {index} inside grid {gridbitmapfirstline}.. {gridbitmaplastcompleteline + 1} redraw");
                Redraw();
            }
        }

        // given row, see if it affects the contents of this panel
        public bool RowChanged(int index)
        {
            if (index >= gridbitmapfirstline && index <= gridbitmaplastcompleteline + 1)        // if within (incl half painted end row), need a redraw
            {
                System.Diagnostics.Debug.WriteLine($"Content row remove {index} inside grid {gridbitmapfirstline}.. {gridbitmaplastcompleteline + 1} redraw");
                Redraw();
                return true;
            }
            else
                return false;
        }

        private void MoveTo(int fdl)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            firstdisplayindex = dgv.Rows.Count > 0 ? Math.Min(fdl, dgv.Rows.Count - 1) : 0;

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
                    gridoffset = new Point(gridoffset.X, ystart);      // move the image down to this position
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
                gridbitmaplastcompleteline = -1;
            }

            if (gridbitmaplastcompleteline == -1)    // do we need to redisplay
                DrawTable();

            // the drawn rectangle is whats left of the bitmap after gridoffset..
            Rectangle drawarea = new Rectangle(0, 0, gridbitmap.Width - gridoffset.X, gridbitmap.Height - gridoffset.Y);
            gr.DrawImage(gridbitmap, drawarea, gridoffset.X, gridoffset.Y, drawarea.Width, drawarea.Height, GraphicsUnit.Pixel);

            if ( rowheaderpanel.Visible)
                rowheaderpanel.Redraw(gridoffset.Y);
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
                        gridoffset = Point.Empty;
                        rowheaderpanel.DrawEmpty();
                        return;
                    }

                    gridbitmapfirstline = Math.Max(0, firstdisplayindex - backup);      // tbd how much to back up..

                    using (Brush b = new SolidBrush(dgv.CellBorderColor))
                    {
                        using (Pen p = new Pen(b, dgv.CellBorderWidth))
                        {
                            int gridwidth = dgv.ColumnPixelWidth- 1;      // width of grid
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

                    if (firstdisplayindex > gridbitmaplastcompleteline)
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
                            gridoffset = new Point(gridoffset.X, ystart);

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

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (gridrowoffsets.Count>0)
            {
                int yoffset = gridoffset.Y + e.Location.Y;
                int row = gridrowoffsets.FindLastIndex(a => a < yoffset);
                if (row >= 0)
                {
                    GLDataGridView dgv = Parent as GLDataGridView;
                    int xoffset = gridoffset.X + e.Location.X;
                    for( int i = 0; i < dgv.Columns.Count; i++ )
                    {
                        int left = dgv.ColumnPixelLeft(i);
                        if ( xoffset>=left && xoffset< left+dgv.Columns[i].Width)
                        {
                            row += gridbitmapfirstline;
                            // System.Diagnostics.Debug.WriteLine($"Contentpanel click on row {row} col {col}");
                            MouseClickOnGrid(row, i, e);
                        }
                    }
                }
            }
        }

        private int firstdisplayindex = 0;
        private Bitmap gridbitmap = null;
        private Point gridoffset;       // scroll index, to be replaced
        private int gridbitmapfirstline = -1;
        private int gridbitmaplastcompleteline = -1;
        private int gridbitmapdrawndepth = -1;
        private List<int> gridrowoffsets = new List<int>();     // cell boundary pixel upper of cell line on Y

        private GLDataGridViewRowHeaderPanel rowheaderpanel;
    }
}

