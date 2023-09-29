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
        // Fore (text), ButtonBack, MouseOverBackColor, MouseDownBackColor from inherited class

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
        /// <summary> Check Box Alignment, for appearance Normal or Radio 
        /// MiddleLeft (default) or MiddleRight only </summary>
        public ContentAlignment CheckAlign { get { return checkalign; } set { checkalign = value; Invalidate(); } }     
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

        /// <summary> Construct with name, bounds, text. Default is a normal checkbox. Use also for radio buttons </summary>
        public GLCheckBox(string name, Rectangle location, string text, CheckStateType? checkstate = null) : base(name, location)
        {
            BorderColorNI = DefaultButtonBorderColor;
            BackColorGradientAltNI = BackColorNI = Color.Transparent;       // transparent for radio/checkboxes
            TextNI = text;
            CheckOnClick = true;
            Focusable = true;
            InvalidateOnFocusChange = true;
            if (checkstate != null)
                SetCheckStateNI(checkstate.Value);
        }

        /// <summary> Construct with name, bounds, text. Default is a normal checkbox. Use also for radio buttons </summary>
        public GLCheckBox(string name, Rectangle location, string text, bool checkstate) : base(name, location)
        {
            BorderColorNI = DefaultButtonBorderColor;
            BackColorGradientAltNI = BackColorNI = Color.Transparent;       // transparent for radio/checkboxes
            TextNI = text;
            CheckOnClick = true;
            Focusable = true;
            InvalidateOnFocusChange = true;
            SetCheckedNI(checkstate);
        }

        /// <summary> Construct with name, bounds, image checked, image unchecked, background color (default is form background) </summary>
        public GLCheckBox(string name, Rectangle location, Image chk, Image unchk, Color? background = null, CheckStateType? checkstate = null) : base(name,location)
        {
            BorderColorNI = DefaultButtonBorderColor;
            BackColorGradientAltNI = BackColorNI = background ?? DefaultFormBackColor;
            Text = null;
            Image = chk;
            ImageUnchecked = unchk;
            Appearance = CheckBoxAppearance.Button;
            CheckOnClick = true;
            Focusable = true;
            InvalidateOnFocusChange = true;
            if (checkstate != null)
                SetCheckStateNI(checkstate.Value);
        }

        /// <summary> Construct with name, bounds, image checked, image unchecked, background color (default is form background) </summary>
        public GLCheckBox(string name, Rectangle location, Image chk, Image unchk, bool checkstate , Color ? background = null ) : base(name, location)
        {
            BorderColorNI = DefaultButtonBorderColor;
            BackColorGradientAltNI = BackColorNI = background ?? DefaultFormBackColor;
            Text = null;
            Image = chk;
            ImageUnchecked = unchk;
            Appearance = CheckBoxAppearance.Button;
            CheckOnClick = true;
            Focusable = true;
            InvalidateOnFocusChange = true;
            SetCheckedNI(checkstate);
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

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.DrawBack(Rectangle, Graphics, Color, Color, int)"/>
        protected override void DrawBack(Rectangle area, Graphics gr, Color backgroundcolor, Color bcgradientalt, int bcgradientdir)
        {
            if (BackColor == Color.Transparent && Appearance == CheckBoxAppearance.Button)
                System.Diagnostics.Trace.WriteLine($"****OFC WARNING**** CheckBox {Name} is in button mode but background is transparent - this mode requires a back color to paint properly");
            base.DrawBack(area, gr, backgroundcolor, bcgradientalt, bcgradientdir);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            bool hasimages = Image != null;
            float backdisscaling = Enabled ? 1.0f : BackDisabledScaling;
            float foredisscaling = Enabled ? 1.0f : ForeDisabledScaling;

          //  System.Diagnostics.Debug.WriteLine($"Paint CheckBox {Name} : {ClientRectangle} e {Enabled} h {Hover} sfb {ShowFocusBox} f {Focused}");

            if (Appearance == CheckBoxAppearance.Button)
            {
                Rectangle marea = ClientRectangle;

                if (Enabled)
                {
                    if (Hover)
                    {
                        using (var b = new LinearGradientBrush(marea, MouseOverColor, MouseOverColor.Multiply(FaceColorScaling), 90))
                            gr.FillRectangle(b, marea);
                    }
                    else if (CheckState == CheckStateType.Checked)
                    {
                        using (var b = new LinearGradientBrush(marea, ButtonFaceColor, ButtonFaceColor.Multiply(FaceColorScaling), 90))
                            gr.FillRectangle(b, marea);
                    }

                    if (ShowFocusBox)           
                    {
                        marea.Inflate(-1, -1);      // leave 
                        if (Focused)
                        {
                            using (Pen p1 = new Pen(MouseDownColor) { DashStyle = DashStyle.Dash })
                            {
                                gr.DrawRectangleFromArea(p1, marea);
                            }
                        }

                        marea.Inflate(-2, -2);
                    }
                }

                if (hasimages)
                    DrawImage(marea, gr);

                if (Text.HasChars())
                {
                    using (var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                    {
                        DrawText(marea, gr, fmt);
                    }
                }
            }
            else
            {
                Rectangle tickarea = ClientRectangle;

                if (BackColor != Color.Transparent)     // if we are painting the background, give a 1 pixel area around
                    tickarea.Inflate(-1, -1);

                Rectangle textarea = tickarea;

                int reduce = (int)(tickarea.Height * TickBoxReductionRatio);
                tickarea.Y += (tickarea.Height - reduce) / 2;
                tickarea.Height = tickarea.Width = reduce;

                if (Text.HasChars())
                {
                    if (CheckAlign == ContentAlignment.MiddleRight)
                    {
                        tickarea.X = ClientWidth - tickarea.Width + 1;
                        textarea.Width -= tickarea.Width + 1;
                    }
                    else
                    {
                        textarea.X += tickarea.Width + 1;
                        textarea.Width -= tickarea.Width + 1;
                    }
                }

                var drawtextrect = new Rectangle(textarea.Left, textarea.Top, textarea.Width - 1, textarea.Height - 1);
                var drawtickrect = new Rectangle(tickarea.Left, tickarea.Top, tickarea.Width - 1, tickarea.Height - 1);

                if (ShowFocusBox)
                {
                    if (Focused)
                    {
                        using (Pen p1 = new Pen(MouseDownColor) { DashStyle = DashStyle.Dash })
                        {
                            if (Text.HasChars())
                            {
                                //System.Diagnostics.Debug.WriteLine($"Draw focus rect {drawtextrect}");
                                gr.DrawRectangle(p1, drawtextrect);
                            }
                            else
                            {
                                //System.Diagnostics.Debug.WriteLine($"Draw focus rect {drawtickrect}");
                                gr.DrawRectangle(p1, drawtickrect);
                            }
                        }
                    }

                    if (!Text.HasChars())
                    {
                        tickarea.Inflate(-2, -2);
                        drawtickrect.Inflate(-2, -2);
                    }
                }

                if (Text.HasChars())
                {
                    using (StringFormat fmt = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.FitBlackBox })
                        DrawText(textarea, gr, fmt);
                }

                if (!hasimages)
                {
                    //System.Diagnostics.Debug.WriteLine($"Draw tick {tickarea}");

                    Color back = !Enabled ? CheckBoxInnerColor.Multiply(backdisscaling) : 
                                (MouseButtonsDown == GLMouseEventArgs.MouseButtons.Left) ? MouseDownColor : 
                                        Hover ? MouseOverColor : 
                                        CheckBoxInnerColor;

                    if ( Appearance == CheckBoxAppearance.Normal)
                    {
                        using (Brush inner = new SolidBrush(back))
                            gr.FillRectangle(inner, drawtickrect);      // fill slightly over size to make sure all pixels are painted

                        using (Pen outer = new Pen(CheckBoxBorderColor))
                            gr.DrawRectangle(outer, drawtickrect);

                        tickarea.Inflate(-1, -1);

                        DrawTick(tickarea, Color.FromArgb(200, CheckColor.Multiply(foredisscaling)), CheckState, gr);
                    }
                    else
                    {
                        using (Brush outer = new SolidBrush(CheckBoxBorderColor))
                            gr.FillEllipse(outer, tickarea);

                        tickarea.Inflate(-1, -1);

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

        ///<summary> Text draw helper </summary>
        protected void DrawText(Rectangle box, Graphics g, StringFormat fmt)
        {
            using (Brush textb = new SolidBrush(Enabled ? this.ForeColor : this.ForeColor.Multiply(ForeDisabledScaling)))
            {
                if (FontToUse == null || FontToUse.FontFamily != Font.FontFamily || FontToUse.Style != Font.Style || FontToUse.SizeInPoints != Font.SizeInPoints)
                    FontToUse = g.GetFontToFitRectangle(this.Text, Font, box, fmt);
                //System.Diagnostics.Debug.WriteLine($"Checkbox {Name} Font {Font.ToString()}");
                g.DrawString(this.Text, FontToUse, textb, box, fmt);
            }
        }

        private Font FontToUse;
        private CheckBoxAppearance appearance { get; set; } = CheckBoxAppearance.Normal;
        private ContentAlignment checkalign { get; set; } = ContentAlignment.MiddleLeft;
        private Image imageUnchecked { get; set; } = null;               // set if using different images for unchecked
        private Image imageIndeterminate { get; set; } = null;           // optional for intermediate
        private System.Drawing.Imaging.ImageAttributes drawnImageAttributesUnchecked = null;         // if unchecked image does not exist, use this for image scaling


    }
}
