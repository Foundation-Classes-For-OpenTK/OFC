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
        public Action<GLDataGridViewRow,bool> Changed { get; set; }     // true if it affect height
        public GLDataGridView Parent { get; set; }
        public int Index { get { return rowno; } }
        public int Height { get { return height; } set { if (value != height) { height = value; Changed?.Invoke(this,true); } } }
        public bool AutoSize { get { return autosize; } set { if (autosize != value) { autosize = value; if (autosize && cells.Count>0) PerformAutoSize(true); } }  }
        public List<GLDataGridViewCell> Cells { get { return cells; } }
        public GLDataGridViewCellStyle DefaultCellStyle { get { return defaultcellstyle; } }
        public GLDataGridViewCellStyle HeaderStyle { get { return headerstyle; } }

        public GLDataGridViewCell this[int cell] { get { return cell < cells.Count ? cells[cell] : null; } }

        public GLDataGridViewRow()
        {
        }
        public void AddTo(List<GLDataGridViewRow> rows)
        {
            rowno = rows.Count;
            PerformAutoSize(false);
            rows.Add(this);
        }

        public void AddCell(GLDataGridViewCell cell)
        {
            int index = cells.Count;
            cell.Parent = this;
            cell.Style.Parent = defaultcellstyle;
            cell.Style.Index = index;
            cell.Index = index;

            // if a cell style has changed, then we perform autosize, then tell DGV, with performautosize=false (don't ask for size)
            cell.Style.Changed += (e1) => { if ( AutoSize) cell.PerformAutoSize(); Changed(this, PerformAutoSize(false)); };
            // if a cell content has changed, then we perform autosize, then tell DGV, with performautosize=false (don't ask for size)
            cell.Changed += (e1) => { if ( AutoSize) cell.PerformAutoSize(); Changed(this, PerformAutoSize(false)); };

            cells.Add(cell);

            if (AutoSize)
            {
                cell.PerformAutoSize();
                Changed?.Invoke(this, PerformAutoSize(false));
            }
            else
                Changed?.Invoke(this, false);
        }


        // if askforsize we ask all columns to set their wanted size
        // true if we change height.  
        public bool PerformAutoSize(bool askforsize)
        {
            if (AutoSize && cells.Count>0)
            {
                System.Diagnostics.Debug.WriteLine($"Row {rowno} Autosize {AutoSize} on all? {askforsize}");
            }
            return false;
        }

        public void RemoveCellAt(int index)
        {
            if ( cells.Count>index)
            {
                cells.RemoveAt(index);
                Changed?.Invoke(this, true);
            }
        }

        public void Clear()
        {
            cells.Clear();
            Changed?.Invoke(this,true);
        }

        public void Paint(Graphics gr, Rectangle area)
        {
            area = new Rectangle(area.Left + HeaderStyle.Padding.Left, area.Top + HeaderStyle.Padding.Top, area.Width - HeaderStyle.Padding.TotalWidth, area.Height - HeaderStyle.Padding.TotalHeight);

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
        private int rowno = -1;
        private bool autosize;
    }
}
