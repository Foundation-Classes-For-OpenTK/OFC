using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLOFC.GL4.Controls
{

    public class GLDataGridViewHeaderPanel : GLPanel
    {
        public GLDataGridViewHeaderPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
        }

        private void DrawColumnHeaders(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;

            if (!dgv.ColumnHeaderEnable)
                return;

            int vpos = 0;
            if (dgv.CellBorderWidth > 0 && dgv.Columns.Count>0) 
            {
                using (Brush b = new SolidBrush(dgv.CellBorderColor))
                {
                    using (Pen p = new Pen(b, dgv.CellBorderWidth))
                    {
                        int colend = dgv.Columns.Last().HeaderBounds.Right;

                        if (dgv.ColumnHeaderEnable)     // line horz across top
                        {
                            //System.Diagnostics.Debug.WriteLine($"Paint - {vpos} to {gridbounds.Right}");
                            gr.DrawLine(p, 0, vpos, colend, vpos);
                            vpos += dgv.ColumnHeaderHeight + dgv.CellBorderWidth;
                        }

                        int hpos = 0;

                        if (dgv.RowHeaderEnable)      // horz part, row header
                        {
                            gr.DrawLine(p, hpos, 0, hpos, vpos);
                            hpos += dgv.RowHeaderWidth;
                            hpos += dgv.CellBorderWidth;
                        }

                        foreach (var c in dgv.Columns)  // horz part, col headers
                        {
                            //System.Diagnostics.Debug.WriteLine($"Paint | {hpos}");
                            gr.DrawLine(p, hpos, 0, hpos, vpos);
                            hpos += c.Width;
                            hpos += dgv.CellBorderWidth;

                            if (dgv.ColumnHeaderEnable)
                            {
                                if (dgv.RowHeaderEnable)
                                {
                                    var upperrightbounds = new Rectangle(dgv.CellBorderWidth, dgv.CellBorderWidth, dgv.RowHeaderWidth, dgv.ColumnHeaderHeight);

                                    if (dgv.UpperRightStyle.BackColor != Color.Transparent)
                                    {
                                        using (Brush b2 = new SolidBrush(dgv.UpperRightStyle.BackColor))
                                        {
                                            gr.FillRectangle(b2, upperrightbounds);
                                        }
                                    }
                                }

                                if (dgv.UserPaintColumnHeaders != null)
                                    dgv.UserPaintColumnHeaders(c, gr);
                                else
                                    c.Paint(gr);
                            }
                        }

                        gr.DrawLine(p, hpos, 0, hpos, vpos);
                        vpos++;
                    }
                }
            }
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            DrawColumnHeaders(gr);
        }
    }
}
