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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OFC.GL4.Controls
{
    public class GLMenuItem : GLButton
    {
        public GLMenuItem(string name, string text = "") : base(name, new Rectangle(0, 0, 0, 0))        // these are autosized
        {
            // note GLButton setup of Padding=2
            BorderWidthNI = 0;      
            Text = text;
            ShowFocusBox = false;
            ImageStretch = true;        // to make sure that menu items are normally sized by text not by image
        }

        public int IconTickAreaWidth { get; set; } = 0;            // zero for off

        public Action<GLBaseControl> CheckChanged { get; set; } = null;    

        public CheckState CheckState { get { return checkstate; } set { SetCheckState(value, true); } }
        public CheckState CheckStateNoChangeEvent { get { return checkstate; } set { SetCheckState(value, false); } }
        public bool Checked { get { return checkstate == CheckState.Checked; } set { SetCheckState(value ? CheckState.Checked : CheckState.Unchecked, true); } }
        public bool CheckedNoChangeEvent { get { return checkstate == CheckState.Checked; } set { SetCheckState(value ? CheckState.Checked : CheckState.Unchecked, false); } }
        public bool CheckOnClick { get; set; } = false;            // if true, autocheck on click

        private Color CheckBoxOuterColor { get { return checkBoxOuterColor; } set { checkBoxOuterColor = value; Invalidate(); } }
        private Color CheckBoxInnerColor { get { return checkBoxInnerColor; } set { checkBoxInnerColor = value; Invalidate(); } }
        private Color CheckColor { get { return checkColor; } set { checkColor = value; Invalidate(); } }

        public float TickBoxReductionRatio { get; set; } = 0.75f;       // Normal - size reduction

        public List<GLBaseControl> SubMenuItems { get; set; } = null;

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize && IconTickAreaWidth>0)
            {
                SetLocationSizeNI(bounds: new Size(Width + IconTickAreaWidth, Height));
                System.Diagnostics.Debug.WriteLine("Menu {0} size {1}", Name, Size);
            }
        }

        protected override void Paint(Rectangle area, Graphics gr)
        {
            Rectangle butarea = area;
            if (IconTickAreaWidth > 0)
            {
                butarea.Width -= IconTickAreaWidth;
                butarea.X += IconTickAreaWidth;
            }

            base.PaintBack(area, gr);

           //using (Brush inner = new SolidBrush(Color.Red))  gr.FillRectangle(inner, butarea);      // Debug

            base.PaintButton(butarea, gr, false);       // don't paint the image

            if ( IconTickAreaWidth > 0 )
            {
                int reduce = (int)(IconTickAreaWidth * TickBoxReductionRatio);
                Rectangle tickarea = new Rectangle(area.X + (IconTickAreaWidth - reduce) / 2, area.Y + (area.Height - reduce) / 2, reduce, reduce);
                float discaling = Enabled ? 1.0f : DisabledScaling;

                if (CheckState != CheckState.Unchecked)
                {
                    Color checkboxbasecolour = CheckBoxOuterColor.Multiply(discaling); //(Enabled && Hover) ? MouseOverBackColor : 

                    using (Brush inner = new SolidBrush(CheckBoxInnerColor))
                        gr.FillRectangle(inner, tickarea);      

                    using (Pen outer = new Pen(checkboxbasecolour))     // paint over to ensure good boundary
                        gr.DrawRectangle(outer, tickarea);
                }

                tickarea.Inflate(-1, -1);       // reduce it around the drawn box above

                if ( Image != null )        // if we have an image, draw it into the tick area
                {
                    base.DrawImage(Image, tickarea, gr, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
                }
                else 
                {
                    base.DrawTick(tickarea, Color.FromArgb(200, CheckColor.Multiply(discaling)), CheckState, gr);
                }

            }
        }

        private void SetCheckState(CheckState value, bool firechange)
        {
            if (checkstate != value)
            {
                checkstate = value;

                if (firechange)
                    OnCheckChanged();   // fire change on us

                Invalidate();
            }
        }

        protected virtual void OnCheckChanged()
        {
            CheckChanged?.Invoke(this);
        }

        public override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == GLMouseEventArgs.MouseButtons.Left && CheckOnClick )
            {
                SetCheckState(checkstate == CheckState.Unchecked ? CheckState.Checked : CheckState.Unchecked, true);
            }
        }

        private GL4.Controls.CheckState checkstate { get; set; } = CheckState.Unchecked;
        private Color checkBoxInnerColor { get; set; } = DefaultMenuItemInnerColor;
        private Color checkBoxOuterColor { get; set; } = DefaultMenuItemOuterColor;
        private Color checkColor { get; set; } = DefaultCheckColor;         // Button - back colour when checked, Normal - check colour
    }

}

