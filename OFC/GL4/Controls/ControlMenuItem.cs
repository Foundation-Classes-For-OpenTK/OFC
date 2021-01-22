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

using System.Collections.Generic;
using System.Drawing;

namespace OFC.GL4.Controls
{
    public class GLMenuItem : GLCheckBoxBase        // its a mash up of a button and a check box
    {
        public GLMenuItem(string name, string text = "") : base(name, new Rectangle(0, 0, 0, 0))        // these are autosized
        {
            SetNI(borderwidth: 0);
            Text = text;
            ShowFocusBox = false;
            ImageStretch = true;        // to make sure that menu items are normally sized by text not by image
            RejectFocus = true;         // MenuStrips always get focus, MI do not
            Focusable = false;
        }

        public Color IconStripBackColor { get { return iconStripBackColor; } set { iconStripBackColor = value; Invalidate(); } }
        public int IconTickAreaWidth { get; set; } = 0;            // zero for off

        public float TickBoxReductionRatio { get; set; } = 0.75f;       // Normal - size reduction

        public bool Highlighted { get { return highlighted; } set { highlighted = value; Invalidate(); } } // if set, lock as highlighted
        public bool DisableHoverHighlight { get { return disablehoverhighlighted; } set { disablehoverhighlighted = value; Invalidate(); } } // if set, lock as highlighted

        public List<GLBaseControl> SubMenuItems { get; set; } = null;

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                ButtonAutoSize(new Size(IconTickAreaWidth,0));
            }
        }

        protected override void Paint(Graphics gr)
        {
            Rectangle butarea = ClientRectangle;

            Color back = PaintButtonBackColor(Highlighted, DisableHoverHighlight);

            if (IconTickAreaWidth > 0)
            {
                butarea.Width -= IconTickAreaWidth;
                butarea.X += IconTickAreaWidth;
                if ( back == BackColor )
                {
                    using (Brush br = new SolidBrush(IconStripBackColor))
                    {
                        gr.FillRectangle(br, new Rectangle(0,0, IconTickAreaWidth, ClientHeight));
                    }

                    base.PaintButtonBack(butarea, gr, back);
                }
                else
                    base.PaintButtonBack(ClientRectangle, gr, back);
            }
            else
            {
                base.PaintButtonBack(ClientRectangle, gr, back);
            }

            //using (Brush inner = new SolidBrush(Color.Red))  gr.FillRectangle(inner, butarea);      // Debug

            base.PaintButton(butarea, gr, false);       // don't paint the image

            if ( IconTickAreaWidth > 0 )
            {
                int reduce = (int)(IconTickAreaWidth * TickBoxReductionRatio);
                Rectangle tickarea = new Rectangle((IconTickAreaWidth - reduce) / 2, (ClientHeight - reduce) / 2, reduce, reduce);
                float discaling = Enabled ? 1.0f : DisabledScaling;

                if (CheckState != CheckState.Unchecked)
                {
                    Color checkboxbordercolour = CheckBoxBorderColor.Multiply(discaling); //(Enabled && Hover) ? MouseOverBackColor : 
                    Color backcolour = (Enabled && Hover) ? MouseOverBackColor : ButtonBackColor.Multiply(discaling);

                    using (Brush inner = new System.Drawing.Drawing2D.LinearGradientBrush(tickarea, CheckBoxInnerColor.Multiply(discaling), backcolour, 225))
                        gr.FillRectangle(inner, tickarea);      // fill slightly over size to make sure all pixels are painted

                    using (Pen outer = new Pen(checkboxbordercolour))     // paint over to ensure good boundary
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

        private GL4.Controls.CheckState checkstate { get; set; } = CheckState.Unchecked;
        private Color iconStripBackColor { get; set; } = DefaultMenuIconStripBackColor;
        private bool highlighted { get; set; } = false;
        private bool disablehoverhighlighted { get; set; } = false;
    }

}

