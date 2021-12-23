using System;
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    public class GLDataGridViewCellStyle
    {
        public GLDataGridViewCellStyle Parent { get; set; }
        public Action<GLDataGridViewCellStyle> Changed { get; set; }
        public Color BackColor { get { return backcolor.HasValue ? backcolor.Value : Parent.BackColor; } set { if (value != backcolor) { backcolor = value; Changed(this); } } }
        public Color ForeColor { get { return forecolor.HasValue ? forecolor.Value : Parent.ForeColor; } set { if (value != forecolor) { forecolor = value; Changed(this); } } }
        public Color SelectedColor { get { return selectedcolor.HasValue ? selectedcolor.Value : Parent.SelectedColor; } set { if (value != selectedcolor) {  selectedcolor = value; Changed(this); } } }
        public Color HighlightColor { get { return highlightcolor.HasValue ? highlightcolor.Value : Parent.HighlightColor; } set { if (value != highlightcolor) { highlightcolor = value; Changed(this); } } }
        public ContentAlignment ContentAlignment { get { return contentalignment.HasValue ? contentalignment.Value : Parent.ContentAlignment; } set { if (value != contentalignment) { contentalignment = value; Changed(this); } } }
        public Font Font { get { return font != null ? font : Parent.Font; } set { font = value; Changed(this); } }
        public Padding Padding { get { return padding.HasValue ? padding.Value : Parent.Padding; } set { padding = value; Changed(this); } }

        private Color? backcolor;
        private Color? forecolor;
        private Color? selectedcolor;
        private Color? highlightcolor;
        private ContentAlignment? contentalignment;
        private Font font;
        private Padding? padding;
    }
}
