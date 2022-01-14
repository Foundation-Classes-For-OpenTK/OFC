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

namespace GLOFC.GL4.Controls
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

    /// <summary> Check state of checkbox </summary>
    public enum CheckState {
        /// <summary> Unchecked </summary>
        Unchecked,
        /// <summary> Checked </summary>
        Checked,
        /// <summary> Indeterminate </summary>
        Indeterminate
    };

    /// <summary>
    /// Base class for check boxes
    /// </summary>
    public abstract class GLCheckBoxBase : GLButtonTextBase
    {
        /// <summary> Callback on check changed </summary>
        public Action<GLBaseControl> CheckChanged { get; set; } = null;
        /// <summary> Callback on click </summary>
        public Action<GLBaseControl> Click { get; set; } = null;

        /// <summary> Current check state. Setting it causes the CheckChanged event </summary>
        public CheckState CheckState { get { return checkstate; } set { SetCheckState(value, true); } }
        /// <summary> Current check state. Setting it does not cause the CheckChanged event </summary>
        public CheckState CheckStateNoChangeEvent { get { return checkstate; } set { SetCheckState(value, false); } }
        /// <summary> Is in Checked state?  Setting it causes the CheckChanged event</summary>
        public bool Checked { get { return checkstate == CheckState.Checked; } set { SetCheckState(value ? CheckState.Checked : CheckState.Unchecked, true); } }
        /// <summary> Is in Checked state?  Setting it does not cause the CheckChanged event</summary>
        /// <summary> </summary>
        public bool CheckedNoChangeEvent { get { return checkstate == CheckState.Checked; } set { SetCheckState(value ? CheckState.Checked : CheckState.Unchecked, false); } }
        /// <summary> Check set on click </summary>
        public bool CheckOnClick { get; set; } = false;            
        /// <summary> Is in a radio button group, grouped by the parent class</summary>
        public bool GroupRadioButton { get; set; } = false;     
        /// <summary> Can only check the control, and cannot uncheck it </summary>
        public bool UserCanOnlyCheck { get; set; } = false;            

        /// <summary> Check box border color around square/round </summary>
        public Color CheckBoxBorderColor { get { return checkBoxBorderColor; } set { checkBoxBorderColor = value; Invalidate(); } }
        /// <summary> Check box color inside the square/round </summary>
        public Color CheckBoxInnerColor { get { return checkBoxInnerColor; } set { checkBoxInnerColor = value; Invalidate(); } }
        /// <summary> Check colour (tick or dot) </summary>
        public Color CheckColor { get { return checkColor; } set { checkColor = value; Invalidate(); } }

        private protected GLCheckBoxBase(string name, Rectangle window) : base(name, window)
        {
            buttonFaceColor = DefaultCheckBackColor;
            foreColor = DefaultCheckForeColor;
            mouseOverColor = DefaultCheckMouseOverColor;
            mouseDownColor = DefaultCheckMouseDownColor;
        }

        private protected void CheckBoxAutoSize()        // autosize for a check box display type
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

        private protected void DrawTick(Rectangle checkarea, Color c1, CheckState chk, Graphics gr)
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

        private protected void SetCheckState(CheckState value, bool firechange)
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

        private protected virtual void OnCheckChanged()
        {
            CheckChanged?.Invoke(this);
        }

        // NOTE: if inherited document had no comment, it does not appear in output! Good!

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)       // clicking on this needs to see if checkonclick is on
        {
            base.OnMouseClick(e);
            if ( !e.Handled && e.Button == GLMouseEventArgs.MouseButtons.Left)
                OnClick();
        }

        /// <summary> Call to perform Click functionality  </summary>
        public virtual void OnClick()
        {
            if ( CheckOnClick && (!UserCanOnlyCheck || CheckState != CheckState.Checked))
            {
                SetCheckState(CheckState == CheckState.Unchecked ? CheckState.Checked : CheckState.Unchecked, true);
            }

            Click?.Invoke(this);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyPress"/>
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
