﻿/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace OFC.GL4.Controls
{
    public class GLTabControl : GLForeDisplayBase
    {
        public int SelectedTab { get { return seltab; } set { if (seltab != value) { seltab = value; ReselectTab(); } } }
        public TabStyleCustom TabStyle { get { return tabstyle; } set { tabstyle = value;InvalidateLayout();; } }

        Color TabSelectedColor { get { return tabSelectedColor; } set { tabSelectedColor = value; Invalidate(); } }
        Color TabNotSelectedColor { get { return tabNotSelectedColor; } set { tabNotSelectedColor = value; Invalidate(); } }
        Color TextNotSelectedColor { get { return textNotSelectedColor; } set { textNotSelectedColor = value; Invalidate(); } }
        Color TabMouseOverColor { get { return tabMouseOverColor; } set { tabMouseOverColor = value; Invalidate(); } }
        Color TabControlBorderColor { get { return tabControlBorderColor; } set { tabControlBorderColor = value; Invalidate(); } }
        float TabColorScaling { get { return tabColorScaling; } set { tabColorScaling = value; Invalidate(); } }

        public GLTabControl(string name, Rectangle location) : base(name, location)
        {
        }

        public GLTabControl() : this("TBC?", DefaultWindowRectangle)
        {
        }

        public override void Add(GLBaseControl other, bool atback = false)
        {
            System.Diagnostics.Debug.Assert(other is GLTabPage);
            other.Dock = DockingType.Fill;
            other.Visible = false;
            base.Add(other,atback);
        }

        public override void Remove(GLBaseControl other)
        {
            System.Diagnostics.Debug.Assert(other is GLTabPage);
            base.Remove(other);
        }

        Rectangle[] tabrectangles;

        const int botmargin = 4;
        const int sidemargin = 8;

        private int CalcRectangles()
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
                foreach( var cb in ControlsOrderAdded)
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


        public override void PerformRecursiveLayout()
        {
            int tabuse = CalcRectangles();

            foreach (var c in ControlsZ)        // all tab controls even if invisible
            {
                Rectangle area = ClientRectangle;       // recalc every time since layout changes it
                area.Y += tabuse;
                area.Height -= tabuse;
                //System.Diagnostics.Debug.WriteLine("Dock tab {0} to {1}", c.Name, area);
                c.Layout(ref area);
                c.PerformRecursiveLayout();
            }
        }

        // called after the background of the panel has been drawn - so it will be clear to write.

        protected override void Paint(Rectangle area, Graphics gr)
        {
            int i = 0;
            foreach( var c in ControlsOrderAdded.OfType<GLTabPage>())
            {
                if (i != seltab)
                {
                    DrawTab(gr, new Rectangle(area.Left + tabrectangles[i].Left, area.Top + tabrectangles[i].Top, tabrectangles[i].Width, tabrectangles[i].Height),
                            c.Text, null, false, mouseover == i);
                }
                i++;
            }

            if (seltab >= 0 && seltab < ControlsOrderAdded.Count)
            {
                var c = ControlsOrderAdded[seltab] as GLTabPage;
                DrawTab(gr, new Rectangle(area.Left + tabrectangles[seltab].Left, area.Top + tabrectangles[seltab].Top, tabrectangles[seltab].Width, tabrectangles[seltab].Height),
                        c.Text, null, true, mouseover == seltab);
            }

        }


        private void DrawTab(Graphics gr, Rectangle area, string text, Image img, bool selected, bool mouseover)
        {
            if (TabStyle == null)
                throw new ArgumentNullException("Custom style not attached");

            Color tabc1 = (Enabled) ? (mouseover ? TabMouseOverColor : (selected ? TabSelectedColor : TabNotSelectedColor)) : TabNotSelectedColor.Multiply(DisabledScaling);
            Color tabc2 = tabc1.Multiply(TabColorScaling);
            Color taboutline = TabControlBorderColor;

            TabStyle.DrawTab(gr, area, selected, tabc1, tabc2, taboutline, TabStyleCustom.TabAlignment.Top);

            Color tabtextc = (Enabled) ? ((selected) ? ForeColor : TextNotSelectedColor) : TextNotSelectedColor.Multiply(DisabledScaling);
            TabStyle.DrawText(gr, area, selected, tabtextc, text, Font, img);
        }

        private void ReselectTab()
        {
            int i = 0;
            foreach (var c in ControlsOrderAdded)     // first is last one entered
            {
                c.VisibleNI = seltab == i;
                i++;
            }

            Invalidate();
        }

        public override void OnMouseMove(GLMouseEventArgs e)
        {
            base.OnMouseMove(e);

            int oldmouseover = mouseover;

            mouseover = -1;
            if (tabrectangles != null)
            {
                for( int i = 0; i < ControlsOrderAdded.Count; i++ )
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

        public override void OnMouseLeave(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);
            int oldmouseover = mouseover;
            mouseover = -1;
            if (mouseover != oldmouseover)
                Invalidate();
        }

        public override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if ( !e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left && mouseover != -1 )
            {
                SelectedTab = mouseover;
            }
        }

        private int seltab = -1;
        private int mouseover = -1;
        private TabStyleCustom tabstyle = new TabStyleSquare();    // change for the shape of tabs.
        private Color tabSelectedColor = DefaultMouseDownButtonColor;
        private Color tabNotSelectedColor = DefaultButtonBackColor;
        private Color textNotSelectedColor = DefaultControlForeColor;
        private Color tabMouseOverColor = DefaultMouseOverButtonColor;
        private Color tabControlBorderColor = DefaultButtonBorderColor;
        private float tabColorScaling = 0.5f;
    }

    /////////////////////////////////////////////////////////

    public class GLTabPage : GLPanel
    {
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                Parent?.Invalidate();
            }
        }

        public GLTabPage(string name, string title) : base(name, DefaultWindowRectangle)
        {
            BackColor = DefaultPanelBackColor;
            text = title;
        }

        public GLTabPage(string name, string title, Color back) : base(name, DefaultWindowRectangle)
        {
            BackColor = back;
            text = title;
        }

        public GLTabPage() : this("TPC?", "")
        {
        }

        private string text = "";
    }
}

