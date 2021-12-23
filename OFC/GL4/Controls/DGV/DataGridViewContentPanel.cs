using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLOFC.GL4.Controls
{

    public class GLDataGridViewContentPanel : GLPanel
    {
        public GLDataGridViewContentPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
        }

        public int FirstDisplayIndex = 0;

        public override void Layout(ref Rectangle parentarea)
        {
            base.Layout(ref parentarea);
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            Rectangle gridbounds = dgv.GridBounds;

            if (dgv.CellBorderWidth > 0)
            {
                using (Brush b = new SolidBrush(dgv.CellBorderColor))
                {
                    using (Pen p = new Pen(b, dgv.CellBorderWidth))
                    {
                        int vpos = 0;
                        if (dgv.ColumnHeaderEnable)
                        {
                            //System.Diagnostics.Debug.WriteLine($"Paint - {vpos} to {gridbounds.Right}");
                            gr.DrawLine(p, 0, vpos, gridbounds.Right-1, vpos);
                            vpos += dgv.ColumnHeaderHeight;
                            vpos += dgv.CellBorderWidth;
                        }

                        for (var rowno = FirstDisplayIndex; rowno < dgv.Rows.Count && vpos < ClientHeight; rowno++)
                        {
                            //System.Diagnostics.Debug.WriteLine($"Paint - {vpos}");
                            gr.DrawLine(p, 0, vpos, gridbounds.Right-1, vpos);
                            vpos += dgv.Rows[rowno].Height;
                            vpos += dgv.CellBorderWidth;
                        }

                        //System.Diagnostics.Debug.WriteLine($"Paint - {vpos}");
                        gr.DrawLine(p, 0, vpos, gridbounds.Right-1, vpos);    // bottom line

                        int hpos = 0;

                        if (dgv.RowHeaderEnable)
                        {
                            gr.DrawLine(p, hpos, 0, hpos, vpos);
                            hpos += dgv.RowHeaderWidth;
                            hpos += dgv.CellBorderWidth;
                        }
                        foreach (var c in dgv.Columns)
                        {
                            //System.Diagnostics.Debug.WriteLine($"Paint | {hpos}");
                            gr.DrawLine(p, hpos, 0, hpos, vpos);
                            hpos += c.Width;
                            hpos += dgv.CellBorderWidth;
                        }

                        //System.Diagnostics.Debug.WriteLine($"Paint | {hpos}");
                        gr.DrawLine(p, hpos, 0, hpos, vpos);
                    }
                }
            }

            if (dgv.ColumnHeaderEnable)
            {
                if (dgv.RowHeaderEnable)
                {
                    dgv.PaintUpperLeft(gr);
                }

                foreach (var c in dgv.Columns)      // paint column headers
                {
                    if (dgv.UserPaintColumnHeaders != null)
                        dgv.UserPaintColumnHeaders(c, gr);
                    else
                        c.Paint(gr);
                }
            }

            for (var rowno = FirstDisplayIndex; rowno < dgv.Rows.Count && dgv.Rows[rowno].HeaderBounds.Top < ClientHeight; rowno++)
            {
                System.Diagnostics.Debug.WriteLine($"Paint row {rowno}");

                var r = dgv.Rows[rowno];
                if (dgv.RowHeaderEnable)
                {
                    if (dgv.UserPaintRowHeaders != null)
                        dgv.UserPaintRowHeaders(r, gr);
                    else
                        r.Paint(gr);
                }
                foreach (var c in r.Cells)
                    c.Paint(gr);
            }
        }
    }
}
