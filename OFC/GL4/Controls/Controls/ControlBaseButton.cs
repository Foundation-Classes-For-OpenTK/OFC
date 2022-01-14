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

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Base class for buttons and checkboxes
    /// </summary>
    public abstract class GLButtonBase : GLImageBase
    {
        /// <summary> Fore color </summary>
        public Color ForeColor { get { return foreColor; } set { foreColor = value; Invalidate(); } }       // of text
        /// <summary> Button face color </summary>
        public Color ButtonFaceColour { get { return buttonFaceColor; } set { buttonFaceColor = value; Invalidate(); } }    // of button
        /// <summary> Mouse over color for face </summary>
        public Color MouseOverColor { get { return mouseOverColor; } set { mouseOverColor = value; Invalidate(); } }
        /// <summary> Mouse down color for face </summary>
        public Color MouseDownColor { get { return mouseDownColor; } set { mouseDownColor = value; Invalidate(); } }
        /// <summary> Face gradient scaling </summary>
        public float FaceColorScaling { get { return faceColorScaling; } set { faceColorScaling = value; Invalidate(); } }
        /// <summary> Show focus box when focused </summary>
        public bool ShowFocusBox { get { return showfocusbox; } set { showfocusbox = value; Invalidate(); } }

        private protected GLButtonBase(string name, Rectangle window) : base(name, window)
        {
            InvalidateOnEnterLeave = true;
            InvalidateOnMouseDownUp = true;
        }

        private protected Color buttonFaceColor { get; set; } = DefaultButtonFaceColor;
        private protected Color mouseOverColor { get; set; } = DefaultMouseOverButtonColor;
        private protected Color mouseDownColor { get; set; } = DefaultMouseDownButtonColor;
        private protected Color foreColor { get; set; } = DefaultButtonForeColor;
        private protected float faceColorScaling = 0.5F;
        private protected bool showfocusbox = true;

    }

    /// <summary>
    /// Base class for text buttons and checkboxes
    /// </summary>

    public abstract class GLButtonTextBase : GLButtonBase
    {
        /// <summary> Text </summary>
        public string Text { get { return text; } set { text = value; Invalidate(); } }
        /// <summary> Alignment of text </summary>
        public ContentAlignment TextAlign { get { return textAlign; } set { textAlign = value; Invalidate(); } }
        /// <summary> Symbol types</summary>
        public enum SymbolType {
            /// <summary> No Symbol</summary>
            None,
            /// <summary> Left triangle</summary>
            LeftTriangle,
            /// <summary> Right triangle</summary>
            RightTriangle
        };
        /// <summary> Symbol to display on button </summary>
        public SymbolType Symbol { get { return buttonsymbol; } set { buttonsymbol = value; Invalidate(); } }
        /// <summary> Symbol size </summary>
        public float SymbolSize { get { return buttonsymbolsize; } set { buttonsymbolsize = value;Invalidate(); } }

        private protected GLButtonTextBase(string name, Rectangle window) : base(name, window)
        {
        }

        private protected string TextNI { set { text = value; } }

        private SymbolType buttonsymbol = SymbolType.None;
        private float buttonsymbolsize = 0.75f;
        private string text;
        private ContentAlignment textAlign { get; set; } = ContentAlignment.MiddleCenter;

        private protected Color PaintButtonFaceColor(bool lockhighlight = false, bool disablehoverhighlight = false)
        {
            Color colBack;

            if (Enabled == false)
                colBack = ButtonFaceColour.Multiply(BackDisabledScaling); 
            else if (MouseButtonsDown == GLMouseEventArgs.MouseButtons.Left)
                colBack = MouseDownColor;
            else if (lockhighlight || (Hover && !disablehoverhighlight))
                colBack = MouseOverColor;
            else
                colBack = ButtonFaceColour;

            return colBack;
        }

        private protected void PaintButtonFace(Rectangle backarea, Graphics gr, Color facecolour)
        {
            using (var b = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(backarea.Left, backarea.Top - 1, backarea.Width, backarea.Height + 1),
                            facecolour, facecolour.Multiply(FaceColorScaling), 90))
            {
                gr.FillRectangle(b, backarea);       // linear grad brushes do not respect smoothing mode, btw
            }
        }

        private protected void PaintButtonTextImageFocus(Rectangle buttonarea, Graphics gr, bool paintimage)
        {
            if (ShowFocusBox)
            {
                if (Focused)
                {
                    using (var p = new Pen(MouseDownColor) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    {
                        gr.DrawRectangle(p, new Rectangle(buttonarea.Left, buttonarea.Top, buttonarea.Width - 1, buttonarea.Height - 1));
                    }
                }
                buttonarea.Inflate(new Size(-1, -1));
            }

            if (Image != null && paintimage)
            {
                base.DrawImage(Image, buttonarea, gr, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                {
                    //System.Diagnostics.Debug.WriteLine($"Draw {Text} {Enabled} {ForeDisabledScaling}");
                    using (Brush textb = new SolidBrush((Enabled) ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
                    {
                        gr.DrawString(this.Text, this.Font, textb, buttonarea, fmt);
                    }
                }
            }

            if ( buttonsymbol != SymbolType.None)
            {
                using (var b = new SolidBrush((Enabled) ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
                {
                    int vcentre = (buttonarea.Top + buttonarea.Bottom) / 2;
                    int hcentre = (buttonarea.Left + buttonarea.Right) / 2;
                    int hright = hcentre + (int)(buttonarea.Width * buttonsymbolsize / 2);
                    int hleft = hcentre - (int)(buttonarea.Width * buttonsymbolsize / 2);
                    int htop = vcentre + (int)(buttonarea.Height * buttonsymbolsize / 2);
                    int hbottom = vcentre - (int)(buttonarea.Height * buttonsymbolsize / 2);

                    if ( buttonsymbol == SymbolType.LeftTriangle )
                        gr.FillPolygon(b, new Point[] { new Point(hleft, vcentre), new Point(hright, htop), new Point(hright, hbottom) });
                    else if ( buttonsymbol == SymbolType.RightTriangle)
                        gr.FillPolygon(b, new Point[] { new Point(hright, vcentre), new Point(hleft, htop), new Point(hleft, hbottom) });
                }
            }
        }

        private protected void ButtonAutoSize(Size extra )     // call if autosize as button
        {
            SizeF size = SizeF.Empty;
            if (Text.HasChars())
            {
                using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                    size = GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(Text, Font, fmt);
            }

            if (Image != null && ImageStretch == false)     // if we are not stretching the image, we take into account image size
                size = new SizeF(size.Width + Image.Width, Math.Max(Image.Height, (int)(size.Height + 0.999)));

            Size s = new Size((int)(size.Width + 0.999 + extra.Width) + 4,
                             (int)(size.Height + 0.999 + extra.Height) + 4);

            SetNI(clientsize: s);
        }

    }

}