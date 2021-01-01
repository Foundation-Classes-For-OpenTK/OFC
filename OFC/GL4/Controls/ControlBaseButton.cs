/*
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
using System.Linq;

namespace OFC.GL4.Controls
{
    public abstract class GLButtonBase : GLImageBase
    {
        public Color ForeColor { get { return foreColor; } set { foreColor = value; Invalidate(); } }       // of text
        public Color ButtonBackColor { get { return buttonBackColor; } set { buttonBackColor = value; Invalidate(); } }
        public Color MouseOverBackColor { get { return mouseOverBackColor; } set { mouseOverBackColor = value; Invalidate(); } }
        public Color MouseDownBackColor { get { return mouseDownBackColor; } set { mouseDownBackColor = value; Invalidate(); } }
        public float BackColorScaling { get { return backColorScaling; } set { backColorScaling = value; Invalidate(); } }

        public GLButtonBase(string name, Rectangle window) : base(name, window)
        {
            InvalidateOnEnterLeave = true;
            InvalidateOnMouseDownUp = true;
        }

        private Color buttonBackColor { get; set; } = DefaultButtonBackColor;
        private Color mouseOverBackColor { get; set; } = DefaultMouseOverButtonColor;
        private Color mouseDownBackColor { get; set; } = DefaultMouseDownButtonColor;
        private Color foreColor { get; set; } = DefaultControlForeColor;
        private float backColorScaling = 0.5F;

    }

    public abstract class GLButtonTextBase : GLButtonBase
    {
        public string Text { get { return text; } set { text = value; Invalidate(); } }
        public ContentAlignment TextAlign { get { return textAlign; } set { textAlign = value; Invalidate(); } }
        public bool ShowFocusBox { get; set; } = true;

        public GLButtonTextBase(string name, Rectangle window) : base(name, window)
        {
        }

        protected string TextNI { set { text = value; } }

        private string text;
        private ContentAlignment textAlign { get; set; } = ContentAlignment.MiddleCenter;

        protected void PaintButtonBack(Rectangle backarea, Graphics gr)
        {
            Color colBack = Color.Empty;

            if (Enabled == false)
            {
                colBack = ButtonBackColor.Multiply(DisabledScaling);
            }
            else if (MouseButtonsDown == GLMouseEventArgs.MouseButtons.Left)
            {
                colBack = MouseDownBackColor;
            }
            else if (Hover)
            {
                colBack = MouseOverBackColor;
            }
            else
            {
                colBack = ButtonBackColor;
            }

            using (var b = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(backarea.Left, backarea.Top - 1, backarea.Width, backarea.Height + 1), colBack, colBack.Multiply(BackColorScaling), 90))
                gr.FillRectangle(b, backarea);       // linear grad brushes do not respect smoothing mode, btw
        }

        protected void PaintButton(Rectangle buttonarea, Graphics gr, bool paintimage)
        {
            if (Focused && ShowFocusBox)
            {
                using (var p = new Pen(MouseDownBackColor))
                {
                    gr.DrawRectangle(p, new Rectangle(buttonarea.Left, buttonarea.Top, buttonarea.Width - 1, buttonarea.Height - 1));
                    buttonarea.Inflate(new Size(-1, -1));
                }
            }

            if (Image != null && paintimage)
            {
                base.DrawImage(Image, buttonarea, gr, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                {
                    using (Brush textb = new SolidBrush((Enabled) ? this.ForeColor : this.ForeColor.Multiply(DisabledScaling)))
                    {
                        gr.DrawString(this.Text, this.Font, textb, buttonarea, fmt);
                    }
                }
            }
        }

        protected void ButtonAutoSize(Size parentsize, Size extra )     // call if autosize as button
        {
            SizeF size = new Size(0, 0);
            if (Text.HasChars())
                size = BitMapHelpers.MeasureStringInBitmap(Text, Font, ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign));

            if (Image != null && ImageStretch == false)     // if we are not stretching the image, we take into account image size
                size = new SizeF(size.Width + Image.Width, Math.Max(Image.Height, (int)(size.Height + 0.999)));

            Size s = new Size((int)(size.Width + 0.999 + extra.Width) + ClientWidthMargin + 4,
                             (int)(size.Height + 0.999 + extra.Height) + ClientHeightMargin + 4);

            SetLocationSizeNI(bounds: s);
        }

    }

}