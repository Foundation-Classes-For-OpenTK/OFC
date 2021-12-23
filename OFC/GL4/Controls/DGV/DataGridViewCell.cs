using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLOFC.GL4.Controls
{
    public abstract class GLDataGridViewCell
    {
        public Action<GLDataGridViewCell> Changed { get; set; }
        public GLDataGridViewRow Parent { get; set; }
        public Rectangle CellBounds { get; set; }
        public GLDataGridViewCellStyle Style { get { return style; } }

        public GLDataGridViewCell()
        {
        }

        public abstract void Paint(Graphics gr);
        public abstract Size CalculateSize();

        private GLDataGridViewCellStyle style = new GLDataGridViewCellStyle();
    }

    public class GLDataGridViewCellText : GLDataGridViewCell
    {
        public GLDataGridViewCellText() { }
        public GLDataGridViewCellText(string t) { objvalue = t; }
        public string Value { get { return objvalue; } set { if (value != objvalue) { objvalue = value; Changed(this); } } }

        public override void Paint(Graphics gr)
        {
            Rectangle area = new Rectangle(CellBounds.Left + Style.Padding.Left, CellBounds.Top + Style.Padding.Top, CellBounds.Width - Style.Padding.TotalWidth, CellBounds.Height - Style.Padding.TotalHeight);

            if (Style.BackColor != Color.Transparent)
            {
                using (Brush b = new SolidBrush(Style.BackColor))
                {
                    gr.FillRectangle(b, area);
                }
            }

            using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(Style.ContentAlignment))
            {
                //System.Diagnostics.Debug.WriteLine($"Draw {Text} {Enabled} {ForeDisabledScaling}");
                using (Brush textb = new SolidBrush(Style.ForeColor))
                {
                    gr.DrawString(objvalue, Style.Font, textb, area, fmt);
                }
            }
        }
        public override Size CalculateSize() { return new Size(24, 24); }

        private string objvalue;
    }
}
