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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GLOFC.GL4.Controls
{
    public class GLGroupBox : GLForeDisplayTextBase
    {
        public const int GBMargins = 2;
        public const int GBPadding = 2;
        public const int GBBorderWidth = 1;
        public const int GBXoffset = 8;
        public const int GBXpad = 2;

        public GLGroupBox(string name, string title, Rectangle location) : base(name, location)
        {
            SetNI(padding: new Padding(GBPadding), margin: new Margin(GBMargins, GroupBoxTextHeight, GBMargins, GBMargins), borderwidth: GBBorderWidth);
            BackColorGradientAltNI = BackColorNI = DefaultGroupBoxBackColor;
            BorderColorNI = DefaultGroupBoxBorderColor;
            foreColor = DefaultGroupBoxForeColor;
            text = title;
        }

        public GLGroupBox(string name, string title, DockingType type, float dockpercent) : this(name, title, DefaultWindowRectangle)
        {
            Dock = type;
            DockPercent = dockpercent;
        }

        public GLGroupBox(string name, string title, Size sizep, DockingType type, float dockpercentage) : this(name, title, new Rectangle(new Point(0,0),sizep))
        {
            Dock = type;
            DockPercent = dockpercentage;
        }

        public GLGroupBox() : this("GB?", "", DefaultWindowRectangle)
        {
        }

        public int GroupBoxTextHeight { get { return (Font?.ScalePixels(20) ?? 20) + GBMargins * 2; } }

        protected override void OnFontChanged()
        {
            base.OnFontChanged();
            SetNI(margin: new Margin(GBMargins, GroupBoxTextHeight, GBMargins, GBMargins));
        }

        protected override void SizeControlPostChild(Size parentsize)
        {
            base.SizeControlPostChild(parentsize);

            if (AutoSize)
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                {
                    var texts = BitMapHelpers.MeasureStringInBitmap(Text, Font, fmt);
                    int textminwidth = (int)texts.Width + GBXoffset;
                    var area = ChildArea();     // all children, find area and set it to it.
                    SetNI(clientsize: new Size(Math.Max(area.Left + area.Right, textminwidth), area.Top + area.Bottom));
                }
            }
        }
        protected override void TextValueChanged()      // called by upper class to say i've changed the text.
        {
            Invalidate();
        }
        protected override void DrawBorder(Graphics gr, Color bc, float bw)      // normal override, you can overdraw border if required.
        {
            int topoffset = this.Text.HasChars() ? (Margin.Top * 3 / 8 ) : GBMargins;
            Rectangle rectarea = new Rectangle(Margin.Left,
                                topoffset,
                                Width - Margin.TotalWidth - 1,
                                Height - Margin.Bottom- topoffset - 1);

            //System.Diagnostics.Debug.WriteLine("Bounds {0} rectarea {1}", bounds, rectarea);

            using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
            {
                // work out the area of the text box, given the text width, and the textalign
                var size = this.Text.HasChars() ? gr.MeasureString(this.Text, this.Font, 10000, fmt) : new SizeF(0, 0);
                int twidth = (int)(size.Width + 0.99f);
                bool alignright = TextAlign == ContentAlignment.MiddleRight || TextAlign == ContentAlignment.TopRight || TextAlign == ContentAlignment.BottomRight;
                Rectangle titlearea = new Rectangle(alignright ? rectarea.Right - twidth - GBXoffset : GBXoffset, 0, twidth, GroupBoxTextHeight);

                using (var p = new Pen(bc, bw))
                {
                    if (this.Text.HasChars())
                    {
                        gr.DrawLine(p, titlearea.Left - GBXpad, rectarea.Top, rectarea.Left, rectarea.Top); // draw around text
                        gr.DrawLine(p, rectarea.Right, rectarea.Top, titlearea.Right + GBXpad, rectarea.Top);
                    }
                    else
                    {
                        gr.DrawLine(p, rectarea.Left, rectarea.Top, rectarea.Right, rectarea.Top);
                    }

                    gr.DrawLine(p, rectarea.Left, rectarea.Top, rectarea.Left, rectarea.Bottom - 1);
                    gr.DrawLine(p, rectarea.Left, rectarea.Bottom - 1, rectarea.Right, rectarea.Bottom - 1);
                    gr.DrawLine(p, rectarea.Right, rectarea.Bottom - 1, rectarea.Right, rectarea.Top);
                    gr.DrawLine(p, rectarea.Right, rectarea.Bottom - 1, rectarea.Right, rectarea.Top);
                }

                if (this.Text.HasChars())
                { 
                    using (Brush textb = new SolidBrush((Enabled) ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
                    {
                        gr.DrawString(this.Text, this.Font, textb, titlearea, fmt);
                    }
                }
            }
        }
    }
}


