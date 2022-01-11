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
using System.Linq;

#pragma warning disable 1591

namespace GLOFC.GL4.Controls
{
    public enum CheckBoxAppearance
    {
        Normal = 0,
        Button = 1,
        Radio = 2,
    }

    public enum CheckState { Unchecked, Checked, Indeterminate };

    public abstract class GLCheckBoxBase : GLButtonTextBase
    {
        public Action<GLBaseControl> CheckChanged { get; set; } = null;
        public Action<GLBaseControl> Click { get; set; } = null;

        public CheckState CheckState { get { return checkstate; } set { SetCheckState(value, true); } }
        public CheckState CheckStateNoChangeEvent { get { return checkstate; } set { SetCheckState(value, false); } }
        public bool Checked { get { return checkstate == CheckState.Checked; } set { SetCheckState(value ? CheckState.Checked : CheckState.Unchecked, true); } }
        public bool CheckedNoChangeEvent { get { return checkstate == CheckState.Checked; } set { SetCheckState(value ? CheckState.Checked : CheckState.Unchecked, false); } }
        public bool CheckOnClick { get; set; } = false;            // if true, autocheck on click
        public bool GroupRadioButton { get; set; } = false;     // if true, on check, turn off all other CheckBox of parents
        public bool UserCanOnlyCheck { get; set; } = false;            // if true, user can only turn it on

        public Color CheckBoxBorderColor { get { return checkBoxBorderColor; } set { checkBoxBorderColor = value; Invalidate(); } }
        public Color CheckBoxInnerColor { get { return checkBoxInnerColor; } set { checkBoxInnerColor = value; Invalidate(); } }
        public Color CheckColor { get { return checkColor; } set { checkColor = value; Invalidate(); } }

        public GLCheckBoxBase(string name, Rectangle window) : base(name, window)
        {
            buttonFaceColor = DefaultCheckBackColor;
            foreColor = DefaultCheckForeColor;
            mouseOverColor = DefaultCheckMouseOverColor;
            mouseDownColor = DefaultCheckMouseDownColor;
        }

        protected void CheckBoxAutoSize()        // autosize for a check box display type
        {
            SizeF size = SizeF.Empty;
            using( var fmt = ControlHelpersStaticFunc.StringFormatFromContentAlignment(TextAlign))
                size = GLOFC.Utils.BitMapHelpers.MeasureStringInBitmap(Text, Font, fmt);
            size = new SizeF(Math.Max(size.Width, 16), Math.Max(size.Height, 16));
            int h = (int)(size.Height + 0.999);
            Size s = new Size((int)(size.Width + 0.999) + h + 4, h + 4);        // add h to width to account for the tick

            //System.Diagnostics.Debug.WriteLine($"Check box {Name} Autosize to {s}");
            SetNI(clientsize: s);
        }

        protected void DrawTick(Rectangle checkarea, Color c1, CheckState chk, Graphics gr)
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

            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        }

        protected void SetCheckState(CheckState value, bool firechange)
        {
            if (checkstate != value)
            {
                checkstate = value;

                if (GroupRadioButton && Parent != null && checkstate == CheckState.Checked)
                {
                    foreach (GLCheckBoxBase c in Parent.ControlsZ.OfType<GLCheckBoxBase>())
                    {
                        if (c != this && c.GroupRadioButton == true && c.checkstate != CheckState.Unchecked)    // if not us, in a group, and not unchecked
                        {
                            c.checkstate = CheckState.Unchecked;        // set directly
                            if (firechange)
                                c.OnCheckChanged();                         // fire change
                            c.Invalidate();
                        }
                    }
                }

                if (firechange)
                    OnCheckChanged();   // fire change on us

                Invalidate();
            }
        }

        protected virtual void OnCheckChanged()
        {
            CheckChanged?.Invoke(this);
        }

        protected override void OnMouseClick(GLMouseEventArgs e)       // clicking on this needs to see if checkonclick is on
        {
            base.OnMouseClick(e);
            if ( !e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left)
                OnClick();
        }

        public virtual void OnClick()
        {
            if ( CheckOnClick && (!UserCanOnlyCheck || CheckState != CheckState.Checked))
            {
                SetCheckState(CheckState == CheckState.Unchecked ? CheckState.Checked : CheckState.Unchecked, true);
            }

            Click?.Invoke(this);
        }

        protected override void OnKeyPress(GLKeyEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.Handled == false && e.KeyChar == 13)
            {
                OnClick();
            }
        }

        private GL4.Controls.CheckState checkstate { get; set; } = CheckState.Unchecked;
        private Color checkBoxBorderColor { get; set; } = DefaultCheckBoxBorderColor;
        private Color checkBoxInnerColor { get; set; } = DefaultCheckBoxInnerColor;    // Normal only inner colour
        private Color checkColor { get; set; } = DefaultCheckColor;         // Button - back colour when checked, Normal - check colour

    }

}
