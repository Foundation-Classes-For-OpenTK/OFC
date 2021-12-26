using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            contentpanel = new GLDataGridViewContentPanel(name+"_CP",location);
            contentpanel.Dock = DockingType.Fill;
            headerpanel = new GLDataGridViewHeaderPanel(name + "_CP", location);
            headerpanel.Dock = DockingType.Top;
            Add(contentpanel);
            Add(headerpanel);
            Add(vertscroll);
            Add(horzscroll);

            colheaderstyle.Changed += (e1) => { headerpanel.Invalidate(); };
            rowheaderstyle.Changed += (e1) => { ContentInvalidateLayout(); };
            defaultcellstyle.Changed += (e1) => { ContentInvalidateLayout(); };
            upperrightstyle.Changed += (e1) => { headerpanel.Invalidate(); };

            upperrightstyle.BackColor = Color.Gray;
            colheaderstyle.BackColor = rowheaderstyle.BackColor  = Color.Orange;
            defaultcellstyle.BackColor = Color.White;
            upperrightstyle.ForeColor = colheaderstyle.ForeColor = rowheaderstyle.ForeColor = defaultcellstyle.ForeColor = Color.Black;
            upperrightstyle.SelectedColor = colheaderstyle.SelectedColor = rowheaderstyle.SelectedColor = defaultcellstyle.SelectedColor = Color.Yellow;
            upperrightstyle.HighlightColor = colheaderstyle.HighlightColor = rowheaderstyle.HighlightColor = defaultcellstyle.HighlightColor = Color.Red;
            upperrightstyle.ContentAlignment = colheaderstyle.ContentAlignment = rowheaderstyle.ContentAlignment = defaultcellstyle.ContentAlignment = ContentAlignment.MiddleCenter;
            upperrightstyle.Font =colheaderstyle.Font = rowheaderstyle.Font = defaultcellstyle.Font = Font;
            upperrightstyle.Padding = colheaderstyle.Padding = rowheaderstyle.Padding = defaultcellstyle.Padding = new Padding(0);
        }

        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            colheaderstyle.Font = rowheaderstyle.Font = defaultcellstyle.Font = Font;
        }

        public int ScrollBarWidth { get { return vertscroll.Width; } set { vertscroll.Width = horzscroll.Height = value; } }

        public GLDataGridViewCellStyle UpperRightStyle { get { return upperrightstyle; } set { upperrightstyle = value; headerpanel.Invalidate(); } }
        public GLDataGridViewCellStyle DefaultCellStyle { get { return defaultcellstyle; } set { defaultcellstyle = value; ContentInvalidateLayout(); } }

        public enum ColFillMode { Fill, Exact };
        public ColFillMode ColumnFillMode { get { return colfillmode; } set { if (value != colfillmode) { colfillmode = value; ContentInvalidateLayout(); } } }
        public GLDataGridViewCellStyle DefaultColumnHeaderStyle { get { return colheaderstyle; } set { colheaderstyle = value; headerpanel.Invalidate(); } }
        public bool ColumnHeaderEnable { get { return columnheaderenable; } set { columnheaderenable = value; headerpanel.Visible = value; InvalidateLayout(); } }
        public int ColumnHeaderHeight { get { return columnheaderheight; } set { columnheaderheight = value; InvalidateLayout(); } }

        public GLDataGridViewCellStyle DefaultRowHeaderStyle { get { return rowheaderstyle; } set { rowheaderstyle = value; ContentInvalidateLayout(); } }
        public int RowHeaderWidth { get { return rowheaderwidth; } set { rowheaderwidth = value; ContentInvalidateLayout(); } }
        public bool RowHeaderEnable { get { return rowheaderenable; } set { rowheaderenable = value; ContentInvalidateLayout(); } }

        public List<GLDataGridViewColumn> Columns { get { return columns; } }
        public List<GLDataGridViewRow> Rows { get { return rows; } }

        public Color CellBorderColor { get { return cellbordercolor; } set { cellbordercolor = value; ContentInvalidate(); } }
        public int CellBorderWidth { get { return cellborderwidth; } set { cellborderwidth = value; ContentInvalidateLayout(); } }

        public Action<GLDataGridViewColumn, Graphics> UserPaintColumnHeaders { get; set; } = null;
        public Action<GLDataGridViewRow, Graphics, Rectangle> UserPaintRowHeaders { get; set; } = null;

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

        public void AddColumn(GLDataGridViewColumn col)
        {
            System.Diagnostics.Debug.Assert(col.Parent == this && col.HeaderStyle.Parent != null);      // ensure created by us
            col.HeaderStyle.Changed += (e1) => { headerpanel.Invalidate(); };
            col.Changed += (e1, ci) => { if (ci) ContentInvalidateLayout(); else headerpanel.Invalidate(); };
            col.AddTo(columns);
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
            ContentInvalidateLayout();
        }

        public void AddRow(GLDataGridViewRow row)
        {
            System.Diagnostics.Debug.Assert(row.Parent == this && row.HeaderStyle.Parent != null);      // ensure created by us
            row.HeaderStyle.Changed += (e1) => { contentpanel.RowChanged(e1.Index); UpdateScrollBar(); };       // style
            row.Changed += (e1,changedheight) => { contentpanel.RowChanged(e1.Index); if (changedheight) UpdateScrollBar(); };   // b1 = true if height changed
            row.AddTo(rows);    // tell row to add to this collection, and autosize
            contentpanel.AddRow(row.Index);       // see if content panel needs redrawing
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
            UpdateScrollBar();
        }

        public void UpdateScrollBar()
        {
            if (LayoutSuspended)
                return;

            var top = ComputeHeight(0, contentpanel.Height);
            if (top.Item1 < rows.Count - 1)      // if last complete row is less than no of rows, we need a scroll
            {
                var bot = ComputeHeight(-1, contentpanel.Height);
                vertscroll.SetValueMaximumLargeChange(contentpanel.FirstDisplayIndex, rows.Count - 1, rows.Count - bot.Item1);
            }
            else
            {
                vertscroll.SetValueMaximumLargeChange(0, rows.Count - 1, Rows.Count);
            }
        }

        // given a start point : +ve from here, -ve from end (-1 = first end row)
        // and the maximum bit map height to measure, run thru rows till end return lastcompleted row and total height
        public Tuple<int,int> ComputeHeight(int start, int maxbitmapheight)
        {
            int dir = start >= 0 ? 1 : -1;

            if (start < 0)
                start = rows.Count + start;

            int vpos = cellborderwidth;
            int lastcompleterow = -1;

            while (start >= 0 && start < rows.Count && vpos < maxbitmapheight )
            {
                vpos += rows[start].Height + cellborderwidth;
                if (vpos < maxbitmapheight)     // if ending vpos < height, its completely displayed
                    lastcompleterow = start;
                start += dir;
            }

            System.Diagnostics.Debug.WriteLine($"Compute Height on {start} maxh {maxbitmapheight} last tow {lastcompleterow} vpos {vpos}");

            return new Tuple<int, int>(lastcompleterow, vpos);
        }

        protected override void PerformRecursiveLayout()     
        {
            headerpanel.Height = columnheaderheight + cellborderwidth;        // set before children layout
            headerpanel.BackColor = BackColor;
            contentpanel.BackColor = BackColor;

            base.PerformRecursiveLayout();      // do layout on children.

            UpdateScrollBar();
            // work out the column layout

            int pixelsforborder = (columns.Count + 1 + (rowheaderenable ? 1 : 0)) * cellborderwidth;
            int cellpixels = contentpanel.Width - pixelsforborder - (rowheaderenable ? rowheaderwidth :0);

            if ( colfillmode == ColFillMode.Fill )
            {
                float hfilltotal = columns.Select(x => x.FillWidth).Sum();
                int pixels = 0;
                foreach (var col in columns)
                {
                    col.Width = (int)(cellpixels * col.FillWidth / hfilltotal);
                    pixels += col.Width;
                }

                int colno = 0;
                while( pixels < cellpixels && columns.Count>0)      // add 1 pixel to each column in turn until back to count
                {
                    columns[colno].Width += 1;
                    pixels++;
                    colno = (colno + 1) % columns.Count;
                }
            }
            else
            {
                cellpixels = columns.Select(x => x.Width).Sum();
            }

            int vpos = cellborderwidth;
            int hpos = rowheaderenable ? (cellborderwidth*2 + rowheaderwidth) : cellborderwidth;        
            foreach (var c in columns)
            {
                c.HeaderBounds = new Rectangle(hpos, vpos, c.Width, columnheaderenable ? columnheaderheight : 0);
                hpos += cellborderwidth + c.Width;
            }
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

        private List<GLDataGridViewColumn> columns = new List<GLDataGridViewColumn>();
        private List<GLDataGridViewRow> rows = new List<GLDataGridViewRow>();

        private GLDataGridViewCellStyle defaultcellstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle rowheaderstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle colheaderstyle = new GLDataGridViewCellStyle();

        private GLDataGridViewCellStyle upperrightstyle = new GLDataGridViewCellStyle();

        private ColFillMode colfillmode;
        private int cellborderwidth = 1;
        private int columnheaderheight = 40;
        private bool columnheaderenable = true;

        private int rowheaderwidth = 40;
        private bool rowheaderenable = true;

        private Color cellbordercolor;

        private GLHorizontalScrollBar horzscroll;
        private GLVerticalScrollBar vertscroll;
        private GLDataGridViewContentPanel contentpanel;
        private GLDataGridViewHeaderPanel headerpanel;
    }

}
