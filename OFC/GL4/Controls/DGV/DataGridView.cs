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
            horzscroll = new GLHorizontalScrollBar(name + "_HSB", new Rectangle(0, 0, 10, sbwidth), 0, 100);
            horzscroll.Dock = DockingType.Bottom;
            contentpanel = new GLDataGridViewContentPanel(name+"_CP",location);
            contentpanel.Dock = DockingType.Fill;
            Add(contentpanel);
            Add(vertscroll);
            Add(horzscroll);



            colheaderstyle.Changed += changedDefaultHeaderStyle;
            rowheaderstyle.Changed += changedDefaultHeaderStyle;
            cellstyle.Changed += changedDefaultHeaderStyle;
            upperrightstyle.Changed += changedDefaultHeaderStyle;

            upperrightstyle.BackColor = Color.Gray;
            colheaderstyle.BackColor = rowheaderstyle.BackColor  = Color.Orange;
            cellstyle.BackColor = Color.White;
            upperrightstyle.ForeColor = colheaderstyle.ForeColor = rowheaderstyle.ForeColor = cellstyle.ForeColor = Color.Black;
            upperrightstyle.SelectedColor = colheaderstyle.SelectedColor = rowheaderstyle.SelectedColor = cellstyle.SelectedColor = Color.Yellow;
            upperrightstyle.HighlightColor = colheaderstyle.HighlightColor = rowheaderstyle.HighlightColor = cellstyle.HighlightColor = Color.Red;
            upperrightstyle.ContentAlignment = colheaderstyle.ContentAlignment = rowheaderstyle.ContentAlignment = cellstyle.ContentAlignment = ContentAlignment.MiddleCenter;
            upperrightstyle.Font =colheaderstyle.Font = rowheaderstyle.Font = cellstyle.Font = Font;
            upperrightstyle.Padding = colheaderstyle.Padding = rowheaderstyle.Padding = cellstyle.Padding = new Padding(0);
        }

        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            colheaderstyle.Font = rowheaderstyle.Font = cellstyle.Font = Font;
        }

        public int ScrollBarWidth { get { return vertscroll.Width; } set { vertscroll.Width = horzscroll.Height = value; } }

        public enum ColFillMode { Fill, Exact };
        public ColFillMode ColumnFillMode { get { return colfillmode; } set { if (value != colfillmode) { colfillmode = value; InvalidateLayout(); } } }

        public bool ColumnHeaderEnable { get { return columnheaderenable; } set { columnheaderenable = value; InvalidateLayout(); } }
        public int ColumnHeaderHeight { get { return columnheaderheight; } set { columnheaderheight = value; InvalidateLayout(); } }
        public int RowHeaderWidth { get { return rowheaderwidth; } set { rowheaderwidth = value; InvalidateLayout(); } }
        public bool RowHeaderEnable { get { return rowheaderenable; } set { rowheaderenable = value; InvalidateLayout(); } }

        public Rectangle GridBounds { get { return gridbounds; } }

        public List<GLDataGridViewColumn> Columns { get { return columns; } }
        public List<GLDataGridViewRow> Rows { get { return rows; } }

        public Color CellBorderColor { get { return cellbordercolor; } set { cellbordercolor = value; Invalidate(); } }
        public int CellBorderWidth { get { return cellborderwidth; } set { cellborderwidth = value; InvalidateLayout(); } }

        public Action<GLDataGridViewColumn, Graphics> UserPaintColumnHeaders { get; set; } = null;
        public Action<GLDataGridViewRow, Graphics> UserPaintRowHeaders { get; set; } = null;

        public GLDataGridViewRow CreateRow()
        {
            GLDataGridViewRow row = new GLDataGridViewRow();
            row.Parent = this;
            row.DefaultCellStyle.Parent = cellstyle;
            row.HeaderStyle.Parent = rowheaderstyle;
            row.HeaderStyle.Changed += changedRowHeaderStyle;
            row.Changed += changedrow;
            row.Height = 24;
            return row;
        }
        public GLDataGridViewColumn CreateColumn()
        {
            GLDataGridViewColumn col = new GLDataGridViewColumn();
            col.Parent = this;
            col.HeaderStyle.Parent = colheaderstyle;
            col.HeaderStyle.Changed += changedColHeaderStyle;
            col.Changed += changedcol;
            col.Width = 50;
            col.FillWidth = 100;
            return col;
        }

        public void AddColumn(GLDataGridViewColumn c)
        {
            c.AddTo(columns);
            InvalidateLayout();
        }
        public void AddRow(GLDataGridViewRow r)
        {
            r.AddTo(rows);
            InvalidateLayout();
        }

        protected override void PerformRecursiveLayout()     
        {
            base.PerformRecursiveLayout();      // do layout on children.

            contentpanel.BackColor = BackColor;

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
            {
                int hpos = rowheaderenable ? (cellborderwidth*2 + rowheaderwidth) : cellborderwidth;        
                foreach (var c in columns)
                {
                    c.HeaderBounds = new Rectangle(hpos, vpos, c.Width, columnheaderenable ? columnheaderheight : 0);
                    hpos += cellborderwidth + c.Width;
                }

                if (columnheaderenable)
                    vpos += columnheaderheight;
            }

            foreach (var r in rows)
            {
                int hpos = cellborderwidth;
                vpos += cellborderwidth;

                r.HeaderBounds = new Rectangle(hpos, vpos, rowheaderenable ? rowheaderwidth:0, r.Height);   // always set bounds so we know

                if ( rowheaderenable)
                    hpos += rowheaderwidth + cellborderwidth;

                for( int colno = 0; colno < columns.Count; colno++)
                {
                    if (colno < r.Cells.Count)     // if within count (row may not be full)
                    {
                        r[colno].CellBounds = new Rectangle(hpos, vpos, columns[colno].Width, r.Height);
                        hpos += columns[colno].Width + cellborderwidth;
                    }
                }

                vpos += r.Height;
            }

            vpos += cellborderwidth;

            if (ColumnHeaderEnable && RowHeaderEnable)
                upperrightbounds = new Rectangle(cellborderwidth, cellborderwidth, rowheaderwidth, columnheaderheight);

            gridbounds = new Rectangle(0, 0, columns.Last().HeaderBounds.Right + cellborderwidth, vpos);
        }

        private void changedcol(GLDataGridViewColumn c)
        {
        }
        private void changedrow(GLDataGridViewRow c)
        {
        }
        private void changedRowHeaderStyle(GLDataGridViewCellStyle e)
        {
        }
        private void changedColHeaderStyle(GLDataGridViewCellStyle e)
        {
        }
        private void changedDefaultHeaderStyle(GLDataGridViewCellStyle e)
        {
        }

        public void PaintUpperLeft(Graphics gr)
        {
            if (upperrightstyle.BackColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(upperrightstyle.BackColor))
                {
                    gr.FillRectangle(b, upperrightbounds);
                }
            }
        }

        private Rectangle gridbounds;

        private List<GLDataGridViewColumn> columns = new List<GLDataGridViewColumn>();
        private List<GLDataGridViewRow> rows = new List<GLDataGridViewRow>();

        private GLDataGridViewCellStyle cellstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle rowheaderstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle colheaderstyle = new GLDataGridViewCellStyle();

        private GLDataGridViewCellStyle upperrightstyle = new GLDataGridViewCellStyle();
        private Rectangle upperrightbounds;

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
    }

}
