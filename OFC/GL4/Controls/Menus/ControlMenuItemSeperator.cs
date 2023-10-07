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
    /// Menu separator
    /// </summary>
    public class GLMenuItemSeparator :GLBaseControl , GLMenuItemBase
    {
        /// <summary>
        /// Construct a menu item separator. This control takes on parent width less RightDrawMargin after layout
        /// </summary>
        /// <param name="name">control name</param>
        /// <param name="height">height of separator box total</param>
        /// <param name="strokewidth">stroke width of line</param>
        /// <param name="align">alignment, only TopCenter, MiddleCenter or BottomCenter</param>
        /// <param name="forecolor">optional fore colour</param>
        /// <param name="enablethemer">if to enable themeing</param>
        public GLMenuItemSeparator(string name, int height , int strokewidth, ContentAlignment align, Color? forecolor = null, bool enablethemer = true ): base(name,new Rectangle(0,0,0,0))
        {
            BackColor = Color.Transparent;
            this.strokewidth = strokewidth;
            this.contentalign = align;
            this.height = height;
            ForeColor = forecolor ?? Color.Black;
            EnableThemer = enablethemer;
        }

        /// <summary> Fore color </summary>
        public Color ForeColor { get { return foreColor; } set { foreColor = value; Invalidate(); } }
        /// <summary> Stroke width </summary>
        public int StrokeWidth { get { return strokewidth; } set { strokewidth = value; Invalidate(); } }
        /// <summary> Right margin </summary>
        public int RightDrawMargin { get { return right; } set { right = value; InvalidateLayout(); } }
        /// <summary> Content alignment. Only Center versions supported </summary>
        public ContentAlignment ContentAlign { get { return contentalign; } set { contentalign = value; Invalidate(); } }       

        /// <summary> To enable the icon area on left. Used for sub menu items</summary>
        public bool IconAreaEnable { get; set; } = false;

        /// <summary> Cannot be selected </summary>
        public bool Selectable { get; set; } = false;

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
            {
                var sz = new Size(10, height);      // we set the width small at this point to not influence the flow panel size
                //System.Diagnostics.Debug.WriteLine($"Separator size {sz} parent {parentsize}");
                SetNI(size: sz);   // need to make it smaller, otherwise we get into an endless cycle
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Layout(ref Rectangle)"/>

        public override void Layout(ref Rectangle parentarea)
        {
            //System.Diagnostics.Debug.WriteLine($"Separator layout {parentarea}");
            SetNI(size: new Size(parentarea.Width-right, height));       // a little bit smaller. we set the size here, because we know the parent has resized to the maximum of the rest of the items
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            Rectangle butarea = ClientRectangle;

            GLMenuStrip p = Parent as GLMenuStrip;
            bool ica = IconAreaEnable && p != null;

            using (Pen pen = new Pen(ForeColor, strokewidth))
            {
                int ypos = contentalign == ContentAlignment.TopCenter ? 0 : contentalign == ContentAlignment.BottomCenter ? ClientHeight - strokewidth : ClientHeight / 2 - strokewidth / 2;
                gr.DrawLine(pen, new Point(ica ? p.IconAreaWidth : 0, ypos), new Point(ClientWidth, ypos));
               // System.Diagnostics.Debug.WriteLine($"Separator draw {ForeColor} {ypos} {p?.ClientWidth ?? 10}");
            }
        }

        private protected Color foreColor { get; set; }
        private protected int strokewidth { get; set; }
        private protected int height { get; set; }
        private protected int right { get; set; } = 2;

        private protected ContentAlignment contentalign { get; set; }
    }


}

