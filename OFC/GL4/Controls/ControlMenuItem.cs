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

namespace OFC.GL4.Controls
{
    public class GLMenuItem : GLCheckBoxBase        // its a mash up of a button and a check box
    {
        public GLMenuItem(string name, string text = "") : base(name, new Rectangle(0, 0, 0, 0))        // these are autosized
        {
            BorderWidthNI = 0;      
            Text = text;
            ShowFocusBox = false;
            ImageStretch = true;        // to make sure that menu items are normally sized by text not by image
            Focusable = true;
        }

        public int IconTickAreaWidth { get; set; } = 0;            // zero for off

        private Color CheckBoxOuterColor { get { return checkBoxOuterColor; } set { checkBoxOuterColor = value; Invalidate(); } }

        public float TickBoxReductionRatio { get; set; } = 0.75f;       // Normal - size reduction

        public List<GLBaseControl> SubMenuItems { get; set; } = null;

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                ButtonAutoSize(parentsize, new Size(IconTickAreaWidth,0));
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

            base.PaintButtonBack(area, gr);

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

        public Action<GLBaseControl, GLMouseEventArgs> Click { get; set; } = null;

        public override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            OnClick(e);
        }

        public virtual void OnClick(GLMouseEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private GL4.Controls.CheckState checkstate { get; set; } = CheckState.Unchecked;
        private Color checkBoxInnerColor { get; set; } = DefaultMenuItemInnerColor;
        private Color checkBoxOuterColor { get; set; } = DefaultMenuItemOuterColor;
        private Color checkColor { get; set; } = DefaultCheckColor;         // Button - back colour when checked, Normal - check colour
    }

}

