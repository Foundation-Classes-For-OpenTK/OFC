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

using System.Drawing;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Panel control. Used to encapsulate other controls inside it.
    /// </summary>
    public class GLPanel : GLBaseControl
    {
        /// <summary> Construtor with name, bounds, and optional back color</summary>
        public GLPanel(string name, Rectangle location, Color? backcolour = null) : base(name, location)
        {
            BackColorNI = backcolour.HasValue ? backcolour.Value : DefaultPanelBackColor;
            BorderColorNI = DefaultPanelBorderColor;
        }

        /// <summary> Empty constructor </summary>
        public GLPanel() : this("P?", DefaultWindowRectangle)
        {
        }

        /// <summary> Constructor with name, docking type, docking percent, and optional backcolour</summary>
        public GLPanel(string name, DockingType type, float dockpercent, Color? backcolour = null) : this(name, DefaultWindowRectangle, backcolour)
        {
            Dock = type;
            DockPercent = dockpercent;
        }

        /// <summary> Constructor with name, size, docking type, docking percent, and optional backcolour</summary>
        public GLPanel(string name, Size sizep, DockingType type, float dockpercentage, Color? backcolour = null) : this(name, DefaultWindowRectangle, backcolour)
        {
            Dock = type;
            DockPercent = dockpercentage;
            SetNI(size: sizep);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControlPostChild(Size)"/>
        protected override void SizeControlPostChild(Size parentsize)
        {
            base.SizeControlPostChild(parentsize);

            if (AutoSize)       
            {
                var area = VisibleChildArea();     // all children, find area and set it to it.
                SetNI(clientsize: new Size(area.Left + area.Right, area.Top + area.Bottom));
            }
        }
    }
}


