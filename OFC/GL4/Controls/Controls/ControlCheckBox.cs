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
using System.Drawing.Drawing2D;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Check Box control
    /// </summary>
    public class GLCheckBox : GLCheckBoxBase
    {
        /// <summary> Appearance of checkbox </summary>
        public enum CheckBoxAppearance
        {
            /// <summary> Normal (Square)</summary>
            Normal = 0,
            /// <summary> Look like a button </summary>
            Button = 1,
            /// <summary> Radio button (Round)</summary>
            Radio = 2,
        }

        /// <summary> Check box appearance type </summary>
        public CheckBoxAppearance Appearance { get { return appearance; } set { appearance = value; Invalidate(); } }

        // Fore (text), ButtonBack, MouseOverBackColor, MouseDownBackColor from inherited class

        /// <summary> Check Box Alignment </summary>
        public ContentAlignment CheckAlign { get { return checkalign; } set { checkalign = value; Invalidate(); } }     // appearance Normal only
        /// <summary> Size of tick box relative to client area </summary>
        public float TickBoxReductionRatio { get; set; } = 0.75f;       // Normal - size reduction

        /// <summary> Image when unchecked </summary>
        public Image ImageUnchecked { get { return imageUnchecked; } set { imageUnchecked = value; Invalidate(); } }        // apperance normal/button only.  
        /// <summary> Image when indeterminate </summary>
        public Image ImageIndeterminate { get { return imageIndeterminate; } set { imageIndeterminate = value; Invalidate(); } }

        /// <summary>
        /// Set up a remap of color
        /// </summary>
        /// <param name="remap">ColorMap structure for remapping</param>
        /// <param name="colormatrix">Color remap matrix</param>
        /// <param name="disabledscaling">Disabled scaling</param>
        public void SetDrawnBitmapUnchecked(System.Drawing.Imaging.ColorMap[] remap, float[][] colormatrix = null, float disabledscaling = 0.5f)
        {
            //System.Diagnostics.Debug.WriteLine("Apply drawn bitmap scaling to " + Name);
            drawnImageAttributesUnchecked?.Dispose();
            drawnImageAttributesDisabled?.Dispose();
            ControlHelpersStaticFunc.ComputeDrawnPanel(out drawnImageAttributesUnchecked, out drawnImageAttributesDisabled, disabledscaling, remap, colormatrix);
            Invalidate();
        }

        /// <summary> Construct with name, bounds, text </summary>
        public GLCheckBox(string name, Rectangle location, string text) : base(name, location)
        {
            BorderColorNI = Color.Transparent;
            BackColorGradientAltNI = BackColorNI = Color.Transparent;
            TextNI = text;
            CheckOnClick = true;
            Focusable = true;
            InvalidateOnFocusChange = true;
        }

        /// <summary> Construct with name, bounds, image checked, image unchecked </summary>
        public GLCheckBox(string name, Rectangle location, Image chk, Image unchk) : this(name, location,"")
        {
            Image = chk;
            ImageUnchecked = unchk;
            Appearance = CheckBoxAppearance.Button;
        }

        /// <summary> Default Constructor </summary>
        public GLCheckBox() : this("CB?", DefaultWindowRectangle, "")
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);

            if (AutoSize)
                CheckBoxAutoSize();
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            bool hasimages = Image != null;
            float backdisscaling = Enabled ? 1.0f : BackDisabledScaling;
            float foredisscaling = Enabled ? 1.0f : ForeDisabledScaling;

            if (Appearance == CheckBoxAppearance.Button)
            {
                if (Enabled)
                {
                    Rectangle marea = ClientRectangle;
                    marea.Inflate(-2, -2);

                    if (Hover)
                    {
                        using (var b = new LinearGradientBrush(marea, MouseOverColor, MouseOverColor.Multiply(FaceColorScaling), 90))
                            gr.FillRectangle(b, marea);
                    }
                    else if (CheckState == CheckStateType.Checked)
                    {
                        using (var b = new LinearGradientBrush(marea, ButtonFaceColour, ButtonFaceColour.Multiply(FaceColorScaling), 90))
                            gr.FillRectangle(b, marea);
                    }
                }

                if (hasimages)
                    DrawImage(ClientRectangle, gr);

                if (Text.HasChars())
                {
                    using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                        DrawText(ClientRectangle, gr, fmt);
                }
            }
            else if ( Appearance == CheckBoxAppearance.Normal )
            {
                Rectangle tickarea = ClientRectangle;
                Rectangle textarea = ClientRectangle;

                int reduce = (int)(tickarea.Height * TickBoxReductionRatio);
                tickarea.Y += (tickarea.Height - reduce) / 2;
                tickarea.Height = tickarea.Width = reduce;

                if (CheckAlign == ContentAlignment.MiddleRight)
                {
                    tickarea.X = ClientWidth - tickarea.Width;
                    textarea.Width -= tickarea.Width;
                }
                else
                {
                    textarea.X += tickarea.Width;
                    textarea.Width -= tickarea.Width;
                }

                if (!Text.HasChars() && ShowFocusBox)       // normally, text has focus box, but if there are none, surround box
                {
                    if (Focused)
                    {
                        using (Pen p1 = new Pen(CheckBoxBorderColor) { DashStyle = DashStyle.Dash })
                        {
                            gr.DrawRectangle(p1, tickarea);
                        }
                    }

                    tickarea.Inflate(-1, -1);
                }

                if (Text.HasChars())
                {
                    using (StringFormat fmt = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.FitBlackBox })
                        DrawText(textarea, gr, fmt);
                }

                if (!hasimages)
                {
                    Color back = !Enabled ? CheckBoxInnerColor.Multiply(backdisscaling) : (MouseButtonsDown == GLMouseEventArgs.MouseButtons.Left) ? MouseDownColor : Hover ? MouseOverColor : CheckBoxInnerColor;
                    using (Brush inner = new SolidBrush(back))
                        gr.FillRectangle(inner, tickarea);      // fill slightly over size to make sure all pixels are painted

                    using (Pen outer = new Pen(CheckBoxBorderColor))
                        gr.DrawRectangle(outer, tickarea);

                    tickarea.Inflate(-1, -1);

                    Rectangle checkarea = tickarea;
                    checkarea.Width++; checkarea.Height++;          // convert back to area

                    DrawTick(checkarea, Color.FromArgb(200, CheckColor.Multiply(foredisscaling)), CheckState, gr);
                }
                else
                {
                    DrawImage(tickarea, gr);
                }
            }
            else
            {                                                       // RADIO
                Rectangle tickarea = ClientRectangle;

                tickarea.Height -= 6;
                tickarea.Y += 2;
                tickarea.Width = tickarea.Height;

                Rectangle textarea = ClientRectangle;
                textarea.X += tickarea.Width;
                textarea.Width -= tickarea.Width;

                if (!Text.HasChars() && ShowFocusBox)       // normally, text has focus box, but if there are none, surround box
                {
                    if (Focused)
                    {
                        using (Pen p1 = new Pen(MouseDownColor) { DashStyle = DashStyle.Dash })
                        {
                            gr.DrawRectangle(p1, tickarea);
                        }
                    }

                    tickarea.Inflate(-1, -1);
                }

                if (Text.HasChars())
                {
                    using (StringFormat fmt = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                        DrawText(textarea, gr, fmt);
                }

                if (!hasimages)
                {
                    using (Brush outer = new SolidBrush(CheckBoxBorderColor))
                        gr.FillEllipse(outer, tickarea);

                    tickarea.Inflate(-1, -1);

                    Color back = !Enabled ? CheckBoxInnerColor.Multiply(backdisscaling) : (MouseButtonsDown == GLMouseEventArgs.MouseButtons.Left) ? MouseDownColor : Hover ? MouseOverColor : CheckBoxInnerColor;
                    //System.Diagnostics.Debug.WriteLine($"{Name} back {back}");
                    using (Brush second = new SolidBrush(back))
                        gr.FillEllipse(second, tickarea);

                    tickarea.Inflate(-2, -2);

                    if (Checked)
                    {
                        using (Brush second = new SolidBrush(CheckColor.Multiply(foredisscaling)))
                            gr.FillEllipse(second, tickarea);
                    }
                }
                else
                {
                    DrawImage(tickarea, gr);
                }
            }
        }

        /// <summary> Image draw helper </summary>
        protected void DrawImage(Rectangle box, Graphics g)
        {
            if (ImageUnchecked != null)     // if we have an alt image for unchecked
            {
                Image image = CheckState == CheckStateType.Checked ? Image : ((CheckState == CheckStateType.Indeterminate && ImageIndeterminate != null) ? ImageIndeterminate : (ImageUnchecked != null ? ImageUnchecked : Image));
                base.DrawImage(image, box, g, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
            }
            else
            {
               // System.Diagnostics.Debug.WriteLine("Draw {0} e{1} c{2}", Name, Enabled, Checked);
                base.DrawImage(Image, box, g, (Enabled) ? ((Checked) ? drawnImageAttributesEnabled: drawnImageAttributesUnchecked) :drawnImageAttributesDisabled);
            }
        }

        private Font FontToUse;

        /// <summary> Text draw helper </summary>
        protected void DrawText(Rectangle box, Graphics g, StringFormat fmt)
        {
            if (Focused && ShowFocusBox)
            {
                using (Pen p1 = new Pen(DefaultCheckBoxBorderColor) { DashStyle = DashStyle.Dash })
                {
                    Rectangle fr = box;
                    fr.Inflate(-1, -1);
                    g.DrawRectangle(p1, fr);
                }
            }
            if (this.Text.HasChars())
            {
                using (Brush textb = new SolidBrush(Enabled ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
                {
                    if (FontToUse == null || FontToUse.FontFamily != Font.FontFamily || FontToUse.Style != Font.Style || FontToUse.SizeInPoints != Font.SizeInPoints)
                        FontToUse = g.GetFontToFitRectangle(this.Text, Font, box, fmt);
                    //System.Diagnostics.Debug.WriteLine($"Checkbox {Name} Font {Font.ToString()}");
                    g.DrawString(this.Text, FontToUse, textb, box, fmt);
                }
            }
        }

        private CheckBoxAppearance appearance { get; set; } = CheckBoxAppearance.Normal;
        private ContentAlignment checkalign { get; set; } = ContentAlignment.MiddleCenter;

        private Image imageUnchecked { get; set; } = null;               // set if using different images for unchecked
        private Image imageIndeterminate { get; set; } = null;           // optional for intermediate
        private System.Drawing.Imaging.ImageAttributes drawnImageAttributesUnchecked = null;         // if unchecked image does not exist, use this for image scaling


    }
}
