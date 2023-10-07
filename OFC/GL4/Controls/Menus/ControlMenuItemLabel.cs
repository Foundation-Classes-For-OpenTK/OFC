/*
 * Copyright 2023-2023 Robbyxp1 @ github.com
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

namespace GLOFC.GL4.Controls
{

    /// <summary>
    /// Label menu item
    /// </summary>

    public class GLMenuItemLabel : GLLabel , GLMenuItemBase
    {
        /// <summary>
        /// Construct a menu label item
        /// </summary>
        public GLMenuItemLabel(string name, string text, bool enablethemer = true ): base(name,new Rectangle(0,0,0,0), text)
        {
            EnableThemer = enablethemer;
        }

        /// <summary> To enable the icon area on left. Used for sub menu items</summary>
        public bool IconAreaEnable { get; set; } = false;

        /// <summary> Cannot be selected </summary>
        public bool Selectable { get; set; } = false;


        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);   // let label do its normal autosize
            if (AutoSize)
            {
                GLMenuStrip p = Parent as GLMenuStrip;
                if (p != null)              // we need to add on the icon area width (even if we dont enable it) to make the real width
                    SetNI(size: new Size(Width + p.IconAreaWidth, Height));
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            Rectangle butarea = ClientRectangle;

            GLMenuStrip p = Parent as GLMenuStrip;
            bool ica = IconAreaEnable && p != null;

            if (ica)        // use a margin offset if icon area is on
                LabelPaint(gr, new MarginType(p.IconAreaWidth, 0, 0, 0));
            else
                LabelPaint(gr, new MarginType(0));
        }
    }


}

