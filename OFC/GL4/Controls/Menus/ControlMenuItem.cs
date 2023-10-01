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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Menu item for Menu strip control
    /// </summary>
    public class GLMenuItem : GLCheckBoxBase        // its a mash up of a button and a check box
    {
        /// <summary> Constructor with name and optional text </summary>
        public GLMenuItem(string name, string text = "", bool enablethemer = true) : base(name, new Rectangle(0, 0, 0, 0))        // these are autosized
        {
            // don't need to set back colour etc, the menu strip does this on an OnControlAdd
            SetNI(borderwidth: 0);
            FaceColorScaling = 1;       // disable this to give a flat look
            Text = text;
            ShowFocusBox = false;
            ImageStretch = true;        // to make sure that menu items are normally sized by text not by image
            RejectFocus = true;         // MenuStrips always get focus, MI do not
            Focusable = false;
            EnableThemer = enablethemer;
        }

        /// <summary> Default Constructor </summary>
        public GLMenuItem() : this("MI", "")
        {
        }

        /// <summary> To enable the icon area on left. Used for sub menu items</summary>
        public bool IconAreaEnable { get; set; } = false;
        /// <summary> Ratio to reduce the tick relative to client area </summary>
        public float TickBoxReductionRatio { get; set; } = 0.75f;      
        /// <summary> If the item is highlighted </summary>
        public bool Highlighted { get { return highlighted; } set { highlighted = value; Invalidate(); } } 
        /// <summary> If to disable the hover over highlight </summary>
        public bool DisableHoverHighlight { get { return disablehoverhighlighted; } set { disablehoverhighlighted = value; Invalidate(); } } 
        /// <summary> List of sub menu items associated with this this item </summary>
        public List<GLBaseControl> SubMenuItems { get; set; } = null;

        #region Implementation

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                GLMenuStrip p = Parent as GLMenuStrip;
                ButtonAutoSize(p != null ? new Size(p.IconAreaWidth, 0) : Size.Empty);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            Rectangle butarea = ClientRectangle;

            GLMenuStrip p = Parent as GLMenuStrip;
            bool ica = IconAreaEnable && p != null;

            if (ica)
            {
                butarea.Width -= p.IconAreaWidth;
                butarea.X += p.IconAreaWidth;
            }

            if (Enabled && (Highlighted || (Hover && !DisableHoverHighlight)))
            {
                base.PaintButtonFace(ClientRectangle, gr, MouseOverColor);
            }
            else
            {
                if (ica)
                {
                    using (Brush br = new SolidBrush(p.IconStripBackColor))
                    {
                        gr.FillRectangle(br, new Rectangle(0, 0, p.IconAreaWidth, ClientHeight));
                    }
                }

                base.PaintButtonFace(butarea, gr, Enabled ? ButtonFaceColor : ButtonFaceColor.Multiply(BackDisabledScaling));
            }

            //using (Brush inner = new SolidBrush(Color.Red))  gr.FillRectangle(inner, butarea);      // Debug

            base.PaintButtonTextImageFocus(butarea, gr, false);       // don't paint the image

            if (ica)
            {
                int reduce = (int)(p.IconAreaWidth * TickBoxReductionRatio);
                Rectangle tickarea = new Rectangle((p.IconAreaWidth - reduce) / 2, (ClientHeight - reduce) / 2, reduce, reduce);

                if (CheckState != CheckStateType.Unchecked)
                {
                    float discaling = Enabled ? 1.0f : BackDisabledScaling;

                    Color checkboxbordercolour = CheckBoxBorderColor.Multiply(discaling); //(Enabled && Hover) ? MouseOverBackColor : 
                    Color backcolour = (Enabled && Hover) ? MouseOverColor : ButtonFaceColor.Multiply(discaling);

                    using (Brush inner = new System.Drawing.Drawing2D.LinearGradientBrush(tickarea, CheckBoxInnerColor.Multiply(discaling), backcolour, 225))
                        gr.FillRectangle(inner, tickarea);      // fill slightly over size to make sure all pixels are painted

                    using (Pen outer = new Pen(checkboxbordercolour))     // paint over to ensure good boundary
                        gr.DrawRectangle(outer, tickarea);
                }

                tickarea.Inflate(-1, -1);       // reduce it around the drawn box above

                if (Image != null)        // if we have an image, draw it into the tick area
                {
                    base.DrawImage(Image, tickarea, gr, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
                }
                else
                {
                    base.DrawTick(tickarea, Color.FromArgb(200, CheckColor.Multiply(Enabled ? 1.0F : ForeDisabledScaling)), CheckState, gr);
                }

            }
        }

        private bool highlighted { get; set; } = false;
        private bool disablehoverhighlighted { get; set; } = false;

        #endregion
    }

    /// <summary>
    /// Auto check menu item for menu strips
    /// </summary>
    public class GLAutoCheckBoxMenuItem : GLMenuItem
    {
        /// <summary> Constructor with name, text and check state </summary>
        public GLAutoCheckBoxMenuItem(string name, string text, bool checkstate) : base(name, text)        // these are autosized
        {
            Checked = checkstate;
            CheckOnClick = true;
        }
    }

    /// <summary>
    /// Sub Menu item for menu strips
    /// </summary>
    public class GLSubmenuMenuItem: GLMenuItem
    {
        /// <summary> Constructor with name, text and submenu items </summary>
        public GLSubmenuMenuItem(string name, string text, params GLBaseControl[] subitems) : base(name, text) 
        {
            SubMenuItems = subitems.ToList();
        }
    }

}

