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
using System.Linq;

namespace OFC.GL4.Controls
{
    public class GLCheckBox : GLCheckBoxBase
    {
        public CheckBoxAppearance Appearance { get { return appearance; } set { appearance = value; Invalidate(); } }

        // Fore (text), ButtonBack, MouseOverBackColor, MouseDownBackColor from inherited class

        public ContentAlignment CheckAlign { get { return checkalign; } set { checkalign = value; Invalidate(); } }     // appearance Normal only
        public float TickBoxReductionRatio { get; set; } = 0.75f;       // Normal - size reduction

        public Image ImageUnchecked { get { return imageUnchecked; } set { imageUnchecked = value; Invalidate(); } }        // apperance normal/button only.  
        public Image ImageIndeterminate { get { return imageIndeterminate; } set { imageIndeterminate = value; Invalidate(); } }

        public void SetDrawnBitmapUnchecked(System.Drawing.Imaging.ColorMap[] remap, float[][] colormatrix = null)
        {
            //System.Diagnostics.Debug.WriteLine("Apply drawn bitmap scaling to " + Name);
            drawnImageAttributesUnchecked?.Dispose();
            drawnImageAttributesDisabled?.Dispose();
            ControlHelpersStaticFunc.ComputeDrawnPanel(out drawnImageAttributesUnchecked, out drawnImageAttributesDisabled, DisabledScaling, remap, colormatrix);
            Invalidate();
        }

        public GLCheckBox(string name, Rectangle location, string text) : base(name, location)
        {
            BackColorNI = Color.Transparent;
            TextNI = text;
            CheckOnClick = true;
            Focusable = true;
            InvalidateOnFocusChange = true;
        }

        public GLCheckBox(string name, Rectangle location, Image chk, Image unchk) : this(name, location,"")
        {
            Image = chk;
            ImageUnchecked = unchk;
            Appearance = CheckBoxAppearance.Button;
        }

        public GLCheckBox() : this("CB?", DefaultWindowRectangle, "")
        {
        }

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);

            if (AutoSize)
                CheckBoxAutoSize(parentsize);
        }

        protected override void Paint(Rectangle area, Graphics gr)
        {
            bool hasimages = Image != null;

            if (Appearance == CheckBoxAppearance.Button)
            {
                if (Enabled)
                {
                    Rectangle marea = area;
                    marea.Inflate(-2, -2);

                    if (Hover)
                    {
                        using (var b = new LinearGradientBrush(marea, MouseOverBackColor, MouseOverBackColor.Multiply(BackColorScaling), 90))
                            gr.FillRectangle(b, marea);
                    }
                    else if (CheckState == CheckState.Checked)
                    {
                        using (var b = new LinearGradientBrush(marea, ButtonBackColor, ButtonBackColor.Multiply(BackColorScaling), 90))
                            gr.FillRectangle(b, marea);
                    }
                }

                if (hasimages)
                    DrawImage(area, gr);

                if (Text.HasChars())
                {
                    using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                        DrawText(area, gr, fmt);
                }
            }
            else if ( Appearance == CheckBoxAppearance.Normal )
            {
                Rectangle tickarea = area;
                Rectangle textarea = area;

                int reduce = (int)(tickarea.Height * TickBoxReductionRatio);
                tickarea.Y += (tickarea.Height - reduce) / 2;
                tickarea.Height = tickarea.Width = reduce;

                if (CheckAlign == ContentAlignment.MiddleRight)
                {
                    tickarea.X = area.Width - tickarea.Width;
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
                        using (Pen p1 = new Pen(MouseDownBackColor) { DashStyle = DashStyle.Dash })
                        {
                            gr.DrawRectangle(p1, tickarea);
                        }
                    }

                    tickarea.Inflate(-1, -1);
                }

                float discaling = Enabled ? 1.0f : DisabledScaling;

                Color backcolour = (Enabled && Hover) ? MouseOverBackColor : ButtonBackColor.Multiply(discaling);

                if (!hasimages)      // draw the over box of the checkbox if no images
                {
                    using (Pen outer = new Pen(backcolour))
                        gr.DrawRectangle(outer, tickarea);
                }

                tickarea.Inflate(-1, -1);

                Rectangle checkarea = tickarea;
                checkarea.Width++; checkarea.Height++;          // convert back to area

                //                System.Diagnostics.Debug.WriteLine("Owner draw " + Name + checkarea + rect);

                if (hasimages)
                {
                    if (Enabled && Hover)                // if mouse over, draw a nice box around it
                    {
                        using (Brush mover = new SolidBrush(MouseOverBackColor))
                        {
                            gr.FillRectangle(mover, checkarea);
                        }
                    }
                }
                else
                {                                   // in no image, we draw a set of boxes
                    using (Pen second = new Pen(CheckBoxBorderColor.Multiply(discaling), 1F))
                        gr.DrawRectangle(second, tickarea);

                    tickarea.Inflate(-1, -1);

                    using (Brush inner = new LinearGradientBrush(tickarea, CheckBoxInnerColor.Multiply(discaling), backcolour, 225))
                        gr.FillRectangle(inner, tickarea);      // fill slightly over size to make sure all pixels are painted

                    using (Pen third = new Pen(backcolour.Multiply(discaling), 1F))
                        gr.DrawRectangle(third, tickarea);
                }

                if (Text.HasChars())
                {
                    using (StringFormat fmt = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.FitBlackBox })
                        DrawText(textarea, gr, fmt);
                }

                if (hasimages)
                {
                    DrawImage(checkarea, gr);
                }
                else
                {
                    DrawTick(checkarea, Color.FromArgb(200, CheckColor.Multiply(discaling)), CheckState, gr);
                }
            }
            else
            {                                                       // RADIO
                Rectangle tickarea = area;

                tickarea.Height -= 6;
                tickarea.Y += 2;
                tickarea.Width = tickarea.Height;

                Rectangle textarea = area;
                textarea.X += tickarea.Width;
                textarea.Width -= tickarea.Width;

                if (!Text.HasChars() && ShowFocusBox)       // normally, text has focus box, but if there are none, surround box
                {
                    if (Focused)
                    {
                        using (Pen p1 = new Pen(MouseDownBackColor) { DashStyle = DashStyle.Dash })
                        {
                            gr.DrawRectangle(p1, tickarea);
                        }
                    }

                    tickarea.Inflate(-1, -1);
                }

                Color basecolor = Hover ? MouseOverBackColor : ButtonBackColor;

                using (Brush outer = new SolidBrush(basecolor))
                    gr.FillEllipse(outer, tickarea);

                tickarea.Inflate(-1, -1);

                if (Enabled)
                {
                    using (Brush second = new SolidBrush(CheckBoxInnerColor))
                        gr.FillEllipse(second, tickarea);

                    tickarea.Inflate(-1, -1);

                    using (Brush inner = new LinearGradientBrush(tickarea, CheckBoxInnerColor, basecolor, 225))
                        gr.FillEllipse(inner, tickarea);      // fill slightly over size to make sure all pixels are painted
                }
                else
                {
                    using (Brush disabled = new SolidBrush(CheckBoxInnerColor))
                    {
                        gr.FillEllipse(disabled, tickarea);
                    }
                }

                tickarea.Inflate(-1, -1);

                if (Checked)
                {
                    Color c1 = Color.FromArgb(255, CheckColor);

                    using (Brush inner = new LinearGradientBrush(tickarea, CheckBoxInnerColor, c1, 45))
                        gr.FillEllipse(inner, tickarea);      // fill slightly over size to make sure all pixels are painted

                    using (Pen ring = new Pen(CheckColor))
                        gr.DrawEllipse(ring, tickarea);
                }

                if (Text.HasChars())
                {
                    using (StringFormat fmt = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                        DrawText(textarea, gr, fmt);
                }
            }
        }

        private void DrawImage(Rectangle box, Graphics g)
        {
            if (ImageUnchecked != null)     // if we have an alt image for unchecked
            {
                Image image = CheckState == CheckState.Checked ? Image : ((CheckState == CheckState.Indeterminate && ImageIndeterminate != null) ? ImageIndeterminate : (ImageUnchecked != null ? ImageUnchecked : Image));
                base.DrawImage(image, box, g, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
            }
            else
            {
               // System.Diagnostics.Debug.WriteLine("Draw {0} e{1} c{2}", Name, Enabled, Checked);
                base.DrawImage(Image, box, g, (Enabled) ? ((Checked) ? drawnImageAttributesEnabled: drawnImageAttributesUnchecked) :drawnImageAttributesDisabled);
            }
        }

        private Font FontToUse;

        private void DrawText(Rectangle box, Graphics g, StringFormat fmt)
        {
            if (Focused && ShowFocusBox)
            {
                using (Pen p1 = new Pen(MouseDownBackColor) { DashStyle = DashStyle.Dash })
                {
                    Rectangle fr = box;
                    fr.Inflate(-1, -1);
                    g.DrawRectangle(p1, fr);
                }
            }
            if (this.Text.HasChars())
            {
                using (Brush textb = new SolidBrush(Enabled ? this.ForeColor : this.ForeColor.Multiply(DisabledScaling)))
                {
                    if (FontToUse == null || FontToUse.FontFamily != Font.FontFamily || FontToUse.Style != Font.Style || FontToUse.SizeInPoints != Font.SizeInPoints)
                        FontToUse = g.GetFontToFitRectangle(this.Text, Font, box, fmt);

                    g.DrawString(this.Text, FontToUse, textb, box, fmt);
                }
            }
        }


        private GL4.Controls.CheckBoxAppearance appearance { get; set; } = CheckBoxAppearance.Normal;
        private ContentAlignment checkalign { get; set; } = ContentAlignment.MiddleCenter;

        private Image imageUnchecked { get; set; } = null;               // set if using different images for unchecked
        private Image imageIndeterminate { get; set; } = null;           // optional for intermediate
        private System.Drawing.Imaging.ImageAttributes drawnImageAttributesUnchecked = null;         // if unchecked image does not exist, use this for image scaling


    }
}
