using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLOFC.GL4.Controls
{
    public class GLDataGridViewRow
    {
        public Action<GLDataGridViewRow> Changed { get; set; }
        public GLDataGridView Parent { get; set; }
        public int Index { get { return rowno; } }
        public int Height { get { return height; } set { if (value != height) { height = value; Changed(this); } } }
        public List<GLDataGridViewCell> Cells { get { return cells; } }
        public GLDataGridViewCellStyle DefaultCellStyle { get { return defaultcellstyle; } }
        public GLDataGridViewCellStyle HeaderStyle { get { return headerstyle; } }
        public Rectangle HeaderBounds { get; set; }
        public GLDataGridViewCell this[int cell] { get { return cell < cells.Count ? cells[cell] : null; } }

        public GLDataGridViewRow()
        {
        }
        public void AddTo(List<GLDataGridViewRow> rows)
        {
            rowno = rows.Count;
            rows.Add(this);
        }

        public void AddCell(GLDataGridViewCell cell)
        {
            cell.Parent = this;
            cell.Style.Parent = defaultcellstyle;
            cell.Style.Changed += changedcellstyle;
            cell.Changed += changedcell;
            cells.Add(cell);
            Changed(this);
        }
        public void Clear()
        {
            cells.Clear();
            Changed(this);
        }

        private void changedcellstyle(GLDataGridViewCellStyle c)
        {
            Changed(this);
        }
        private void changedcell(GLDataGridViewCell c)
        {
            Changed(this);
        }

        public void Paint(Graphics gr)
        {
            Rectangle area = new Rectangle(HeaderBounds.Left + HeaderStyle.Padding.Left, HeaderBounds.Top + HeaderStyle.Padding.Top, HeaderBounds.Width - HeaderStyle.Padding.TotalWidth, HeaderBounds.Height - HeaderStyle.Padding.TotalHeight);

            if (HeaderStyle.BackColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(HeaderStyle.BackColor))
                {
                    gr.FillRectangle(b, area);
                }
            }

            using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(HeaderStyle.ContentAlignment))
            {
                //System.Diagnostics.Debug.WriteLine($"Draw {Text} {Enabled} {ForeDisabledScaling}");
                using (Brush textb = new SolidBrush(HeaderStyle.ForeColor))
                {
                    gr.DrawString(rowno.ToString(), HeaderStyle.Font, textb, area, fmt);
                }
            }
        }


        private GLDataGridViewCellStyle defaultcellstyle = new GLDataGridViewCellStyle();
        private GLDataGridViewCellStyle headerstyle = new GLDataGridViewCellStyle();
        private List<GLDataGridViewCell> cells = new List<GLDataGridViewCell>();
        private int height;
        private int rowno;
    }
}
