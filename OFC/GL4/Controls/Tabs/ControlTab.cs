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

using GLOFC.Utils;
using System;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Tab Control
    /// </summary>
    public class GLTabControl : GLForeDisplayBase
    {
        /// <summary> The selected tab index. -1 means no page is selected </summary>
        public int SelectedTab { get { return seltab; } set { if (seltab != value) { seltab = value; ReselectTab(); } } }
        /// <summary> The tab style to use. Default is TabStyleSquare. Also available are TabStyleRoundedEdge and TabStyleAngled </summary>
        public TabStyleCustom TabStyle { get { return tabstyle; } set { tabstyle = value;InvalidateLayout();; } }

        /// <summary> Tab selected back color (ForeColor is selected fore color)</summary>
        public Color TabSelectedColor { get { return tabSelectedColor; } set { tabSelectedColor = value; Invalidate(); } }
        /// <summary> Tab not selected back color</summary>
        public Color TabNotSelectedColor { get { return tabNotSelectedColor; } set { tabNotSelectedColor = value; Invalidate(); } }
        /// <summary> Text not selected fore color</summary>
        public Color TextNotSelectedColor { get { return textNotSelectedColor; } set { textNotSelectedColor = value; Invalidate(); } }
        /// <summary> Mouse over tab color </summary>
        public Color TabMouseOverColor { get { return tabMouseOverColor; } set { tabMouseOverColor = value; Invalidate(); } }
        /// <summary> Tab edges border color </summary>
        public Color TabControlBorderColor { get { return tabControlBorderColor; } set { tabControlBorderColor = value; Invalidate(); } }
        /// <summary> Gradient scaling factor to tabs</summary>
        public float TabColorScaling { get { return tabColorScaling; } set { tabColorScaling = value; Invalidate(); } }
        /// <summary> Gradient scaling for background of control (color is BackColor) </summary>
        public float BackDisabledScaling { get { return backDisabledScaling; } set { if (backDisabledScaling != value) { backDisabledScaling = value; Invalidate(); } } }
        /// <summary> Disable autosize, not supported</summary>
        public new bool AutoSize { get { return false; } set { throw new NotImplementedException(); } }

        /// <summary> Construct with name and bounds </summary>
        public GLTabControl(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultTabControlBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultTabControlBackColor;
            foreColor = DefaultTabControlForeColor;
        }

        /// <summary> Default Constructor </summary>
        public GLTabControl() : this("TBC?", DefaultWindowRectangle)
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Add(GLBaseControl, bool)"/>
        public override void Add(GLBaseControl other, bool atback = false)
        {
            System.Diagnostics.Debug.Assert(other is GLTabPage);
            other.Dock = DockingType.Fill;
            other.Visible = false;
            base.Add(other,atback);
        }

        private int CalcRectangles()            // calculate all tab rectangles and populate list, return max height
        {
            if (Font == null)
                return botmargin;

            tabrectangles = new Rectangle[ControlsIZ.Count];

            int tabheight = Font.Height + botmargin;
            int maxheight = tabheight;

            Bitmap t = new Bitmap(1, 1);
            using (Graphics bgr = Graphics.FromImage(t))
            {
                Point p = new Point(0, 0);

                int r = 0;
                foreach( var cb in ControlsIZ)
                {
                    var c = cb as GLTabPage;

                    SizeF sizef = bgr.MeasureString(c.Text, Font);

                    int width = (int)(sizef.Width + sidemargin);
                    if (p.X + width > ClientWidth)
                    {
                        p = new Point(0, p.Y + tabheight-2);
                        maxheight += tabheight;
                    }

                    tabrectangles[r] = new Rectangle(p.X, p.Y, width, tabheight);

                    p = new Point(tabrectangles[r].Right, p.Y);

                    r++;
                }
            }

            return maxheight;
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.PerformRecursiveLayout"/>
        protected override void PerformRecursiveLayout()
        {
            // override the base layout for this control

            int tabuse = CalcRectangles();

            foreach (var c in ControlsZ)                // all tab controls even if invisible
            {
                Rectangle area = ClientRectangle;       // recalc every time since layout changes it
                area.Y += tabuse;
                area.Height -= tabuse;
                //System.Diagnostics.Debug.WriteLine("Dock tab {0} to {1}", c.Name, area);
                c.Layout(ref area);
                c.CallPerformRecursiveLayout();
            }

            ClearLayoutFlags();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            // called after the background of the panel has been drawn - so it will be clear to write.

            int i = 0;
            foreach( var c in ControlsIZ.OfType<GLTabPage>())       // draw all but selected
            {
                if (i != seltab)
                {
                    DrawTab(gr, new Rectangle(tabrectangles[i].Left, tabrectangles[i].Top, tabrectangles[i].Width, tabrectangles[i].Height),
                            c.Text, null, false, mouseover == i);
                }
                i++;
            }

            if (seltab >= 0 && seltab < ControlsIZ.Count)       // and draw selected
            {
                var c = ControlsIZ[seltab] as GLTabPage;
                DrawTab(gr, new Rectangle(tabrectangles[seltab].Left, tabrectangles[seltab].Top, tabrectangles[seltab].Width, tabrectangles[seltab].Height),
                        c.Text, null, true, mouseover == seltab);
            }

        }

        private void DrawTab(Graphics gr, Rectangle area, string text, Image img, bool selected, bool mouseover)
        {
            if (TabStyle == null)
                throw new ArgumentNullException("Custom style not attached");

            Color tabc1 = (Enabled) ? (mouseover ? TabMouseOverColor : (selected ? TabSelectedColor : TabNotSelectedColor)) : TabNotSelectedColor.Multiply(BackDisabledScaling);
            Color tabc2 = tabc1.Multiply(TabColorScaling);
            Color taboutline = TabControlBorderColor;

            TabStyle.DrawTab(gr, area, selected, tabc1, tabc2, taboutline, TabStyleCustom.TabAlignment.Top);

            Color tabtextc = (Enabled) ? ((selected) ? ForeColor : TextNotSelectedColor) : TextNotSelectedColor.Multiply(ForeDisabledScaling);
            TabStyle.DrawText(gr, area, selected, tabtextc, text, Font, img);
        }

        private void ReselectTab()
        {
            SuspendLayout();
            int i = 0;
            foreach (var c in ControlsIZ)     // first is last one entered
            {
                c.Visible = seltab == i;        // this will request parent layout, which is blocked by suspend layout
                i++;
            }

            ResumeLayout();
            Invalidate();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseMove(GLMouseEventArgs)"/>
        protected override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            int oldmouseover = mouseover;

            mouseover = -1;
            if (tabrectangles != null)
            {
                for( int i = 0; i < ControlsIZ.Count; i++ )
                {
                    if (tabrectangles[i].Contains(e.Location))
                    {
                        mouseover = i;
                        break;
                    }
                }
            }

            if (mouseover != oldmouseover)
                Invalidate();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseLeave(GLMouseEventArgs)"/>
        protected override void OnMouseLeave(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);
            int oldmouseover = mouseover;
            mouseover = -1;
            if (mouseover != oldmouseover)
                Invalidate();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if ( !e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left && mouseover != -1 )
            {
                SelectedTab = mouseover;
            }
        }

        private int seltab = -1;
        private int mouseover = -1;
        private Rectangle[] tabrectangles;
        private TabStyleCustom tabstyle = new TabStyleSquare();    // change for the shape of tabs.
        private Color tabSelectedColor = DefaultTabControlSelectedBackColor;
        private Color tabNotSelectedColor = DefaultTabControlNotSelectedBackColor;
        private Color textNotSelectedColor = DefaultTabControlNotSelectedForeColor;
        private Color tabMouseOverColor = DefaultTabControlMouseOverColor;
        private Color tabControlBorderColor = DefaultTabControlBorderColor;
        private float tabColorScaling = 0.5f;
        private float backDisabledScaling = 0.75F;

        const int botmargin = 4;
        const int sidemargin = 8;
    }

    /////////////////////////////////////////////////////////

    /// <summary>
    /// Tab page Control
    /// </summary>
    public class GLTabPage : GLPanel
    {
        /// <summary> Name of Tab </summary>
        public string Text {  get { return text; }   set {  text = value;  Parent?.Invalidate(); } }

        /// <summary> Constructor with name and title </summary>
        public GLTabPage(string name, string title) : base(name, DefaultWindowRectangle)
        {
            BackColorNI = DefaultPanelBackColor;
            text = title;
        }

        /// <summary> Constructor with name, title of tab, and back color</summary>
        public GLTabPage(string name, string title, Color back) : base(name, DefaultWindowRectangle)
        {
            BackColorNI = back;
            text = title;
        }

        /// <summary> Default Constructor </summary>
        public GLTabPage() : this("TPC?", "")
        {
        }

        private string text = "";
    }
}

