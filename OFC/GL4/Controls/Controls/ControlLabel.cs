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
using System.Drawing;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Label control
    /// </summary>
    public class GLLabel : GLForeDisplayTextBase
    {
        /// <summary> Construct giving name, bounds, text </summary>
        public GLLabel(string name, Rectangle location, string text) : base(name, location)
        {
            this.text = text;
            foreColor = DefaultLabelForeColor;
            BackColorNI = Color.Transparent;
            BorderColorNI = DefaultLabelBorderColor;
        }

        /// <summary> Construct giving name, bounds, text, forecolor, optional backcolor </summary>

        public GLLabel(string name, Rectangle location, string text, Color forecolor, Color? backcolor = null, bool enablethemer = true) : this(name, location,text)
        {
            foreColor = forecolor;
            if ( backcolor.HasValue)
                this.BackColorNI = backcolor.Value;
            EnableThemer = enablethemer;
        }

        /// <summary> Default Constructor </summary>
        public GLLabel() : this("LB?", DefaultWindowRectangle, "")
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                SizeF size = SizeF.Empty;
                if (Text.HasChars())
                {
                    using( var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                        size = GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(Text, Font, fmt);
                }

                Size s = new Size((int)(size.Width + 0.999) + Margin.TotalWidth + Padding.TotalWidth + BorderWidth + 4,
                                 (int)(size.Height + 0.999) + Margin.TotalHeight + Padding.TotalHeight + BorderWidth + 4);

                SetNI(size: s);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            if (Text.HasChars())
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                {
                    using (Brush textb = new SolidBrush((Enabled) ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
                    {
                        gr.DrawString(this.Text, this.Font, textb, ClientRectangle, fmt);
                    }
                }
            }
        }

        private protected override void TextValueChanged()      // called by upper class to say i've changed the text.
        {
            Invalidate();
        }

    }
}
