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
using System.Drawing.Drawing2D;

namespace OFC.GL4.Controls
{
    // a button type control

    public class GLButton : GLButtonTextBase
    {
        public Action<GLBaseControl, GLMouseEventArgs> Click { get; set; } = null;
        public Action<GLBaseControl> Return { get; set; } = null;

        public GLButton(string name, Rectangle location) : base(name, location)
        {
            PaddingNI = new Padding(2);       // standard format, a border with a pad of 1
            BorderWidthNI = 1;
            BorderColorNI = DefaultButtonBorderColor;
            BackColorNI = DefaultButtonBorderBackColor;
            Focusable = true;
            InvalidateOnFocusChange = true;
        }

        public GLButton(string name, Rectangle location, string text) : this(name, location)
        {
            TextNI = text;
        }

        public GLButton(string name, Rectangle location, Image img, bool stretch) : this(name, location)
        {
            TextNI = "";
            Image = img;
            ImageStretch = stretch;
        }

        public GLButton() : this("But?", DefaultWindowRectangle, "")
        {
        }

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if ( AutoSize )
            {
                SizeF size = new Size(0, 0);
                if ( Text.HasChars() )
                    size = BitMapHelpers.MeasureStringInBitmap(Text, Font, ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign));

                if (Image != null && ImageStretch == false)     // if we are not stretching the image, we take into account image size
                    size = new SizeF(size.Width+Image.Width, Math.Max(Image.Height,(int)(size.Height+0.999)));

                Size s = new Size((int)(size.Width + 0.999) + ClientWidthMargin + 4,
                                 (int)(size.Height + 0.999) + ClientHeightMargin + 4);

                SetLocationSizeNI(bounds: s);
            }
        }

        protected override void Paint(Rectangle area, Graphics gr)
        {
            if (area.Width < 1 || area.Height < 1)  // and no point drawing any more in the button area if its too small, it will except
                return;
            PaintBack(area, gr);
            PaintButton(area, gr,true);
        }

        protected void PaintBack(Rectangle backarea, Graphics gr)
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

            using (var b = new LinearGradientBrush(new Rectangle(backarea.Left, backarea.Top - 1, backarea.Width, backarea.Height + 1), colBack, colBack.Multiply(BackColorScaling), 90))
                gr.FillRectangle(b, backarea);       // linear grad brushes do not respect smoothing mode, btw
        }

        public override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
                OnClick(e);
        }

        public virtual void OnClick(GLMouseEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        public override void OnKeyPress(GLKeyEventArgs e)
        {
            if ( e.KeyChar == 13 )
            {
                OnReturn();
            }
        }

        public virtual void OnReturn()
        {
            Return?.Invoke(this);
        }

    }
}
