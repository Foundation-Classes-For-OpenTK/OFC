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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OFC.GL4.Controls
{

    public class GLLabel : GLForeDisplayTextBase
    {
        public GLLabel(string name, Rectangle location, string text) : base(name,location)
        {
            this.text = text;
            ForeColor = DefaultLabelForeColor;
            BackColor = Color.Transparent;
        }

        public GLLabel() : this("LB?", DefaultWindowRectangle, "")
        {
        }

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                SizeF size = new Size(0, 0);
                if (Text.HasChars())
                    size = BitMapHelpers.MeasureStringInBitmap(Text, Font, ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign));

                Size s = new Size((int)(size.Width + 0.999) + Margin.TotalWidth + Padding.TotalWidth + BorderWidth + 4,
                                 (int)(size.Height + 0.999) + Margin.TotalHeight + Padding.TotalHeight + BorderWidth + 4);

                SetLocationSizeNI(size: s);
            }
        }

        protected override void Paint(Rectangle area, Graphics gr)
        {
            if (Text.HasChars())
            {
                gr.SmoothingMode = SmoothingMode.AntiAlias;

                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                {
                    using (Brush textb = new SolidBrush((Enabled) ? this.ForeColor : this.ForeColor.Multiply(DisabledScaling)))
                    {
                        gr.DrawString(this.Text, this.Font, textb, area, fmt);
                    }
                }

                gr.SmoothingMode = SmoothingMode.None;
            }
        }
    }
}
