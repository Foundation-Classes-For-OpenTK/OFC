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

using System;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{

    public class GLDataGridViewTopLeftHeaderPanel : GLPanel
    {
        public GLDataGridViewTopLeftHeaderPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
        }

        protected override void Paint(Graphics gr)
        {
            GLDataGridView dgv = Parent as GLDataGridView;
            if (dgv.CellBorderWidth > 0)
            {
                using (Brush b = new SolidBrush(dgv.CellBorderColor))
                {
                    using (Pen p = new Pen(b, dgv.CellBorderWidth))
                    {
                        gr.DrawLine(p, 0, 0, ClientWidth-1, 0);       // draw a line across the top
                        gr.DrawLine(p, 0, 0, 0, ClientHeight-1);
                    }
                }
            }

            if ( dgv.UserPaintTopLeftHeader != null)
            {
                Rectangle area = new Rectangle(dgv.CellBorderWidth, dgv.CellBorderWidth, ClientWidth - 2 * dgv.CellBorderWidth, ClientHeight - 2 * dgv.CellBorderWidth);
                dgv.UserPaintTopLeftHeader(gr, area);
            }
        }
    }
}
