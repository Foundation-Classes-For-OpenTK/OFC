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

using System.Drawing;

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

        protected void DrawTick(Rectangle checkarea, Color c1, CheckState chk,  Graphics gr)
        {
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (chk == CheckState.Checked)
            {
                Point pt1 = new Point(checkarea.X + 2, checkarea.Y + checkarea.Height / 2 - 1);
                Point pt2 = new Point(checkarea.X + checkarea.Width / 2 - 1, checkarea.Bottom - 2);
                Point pt3 = new Point(checkarea.X + checkarea.Width - 2, checkarea.Y);

                using (Pen pcheck = new Pen(c1, 2.0F))
                {
                    gr.DrawLine(pcheck, pt1, pt2);
                    gr.DrawLine(pcheck, pt2, pt3);
                }
            }
            else if (chk == CheckState.Indeterminate)
            {
                Size cb = new Size(checkarea.Width - 5, checkarea.Height - 5);
                if (cb.Width > 0 && cb.Height > 0)
                {
                    using (Brush br = new SolidBrush(c1))
                    {
                        gr.FillRectangle(br, new Rectangle(new Point(checkarea.X + 2, checkarea.Y + 2), cb));
                    }
                }
            }

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
        }


    }

}
