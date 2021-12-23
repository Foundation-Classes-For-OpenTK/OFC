using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLOFC.GL4.Controls
{

    public class GLDataGridViewColumn
    {
        public GLDataGridView Parent { get; set; }
        public Action<GLDataGridViewColumn> Changed { get; set; }
        public int Index { get { return colno; } }
        public int Width { get { return width; } set { if (value != width) { width = value; Changed(this); } } }
        public float FillWidth { get { return fillwidth; } set { if (value != fillwidth) { fillwidth = value; Changed(this); } } }
        public GLDataGridViewCellStyle HeaderStyle { get { return headerstyle; } }
        public Rectangle HeaderBounds { get; set; }
        public string Text { get { return text; } set { text = value; Changed(this); } }

        public void AddTo(List<GLDataGridViewColumn> cols)
        {
            colno = cols.Count;
            cols.Add(this);
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
                    gr.DrawString(text, HeaderStyle.Font, textb, area, fmt);
                }
            }
        }

        private GLDataGridViewCellStyle headerstyle = new GLDataGridViewCellStyle();
        private int width;
        private float fillwidth;
        private string text = string.Empty;
        private int colno;

    }
}
