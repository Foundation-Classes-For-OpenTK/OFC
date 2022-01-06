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

namespace GLOFC.GL4.Controls
{
    // Scroll panel
    // must not be a child of GLForm as it needs a bitmap to paint into

    public class GLScrollPanel : GLPanel
    {
        public GLScrollPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
        }

        public GLScrollPanel(string name = "SP?") : this(name, DefaultWindowRectangle)
        {
        }

        public new bool AutoSize { get { return false; } set { throw new NotImplementedException(); } }

        public int VertScrollRange { get { return (LevelBitmap != null) ? (LevelBitmap.Height - Height) : 0; } }
        public int VertScrollPos { get { return ScrollOffset.Y; } set { SetScrollPos(ScrollOffset.X,value); } }
        public int HorzScrollRange { get { return (LevelBitmap != null) ? (LevelBitmap.Width - Width) : 0; } }
        public int HorzScrollPos { get { return ScrollOffset.X; } set { SetScrollPos(value,ScrollOffset.Y); } }

        // Width/Height is size of the control without scrolling
        // we layout the children within that area.
        // but if we have areas outside that, the bitmap is expanded to cover it

        protected override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();               // layout the children

            bool needbitmap = false;

            if (ControlsZ.Count > 0)
            {
                Rectangle r = VisibleChildArea();

                int childwidth = r.Left + r.Right;
                int childheight = r.Bottom + r.Top;

                needbitmap = childheight > Height || childwidth > Width;

                if (needbitmap)
                {
                    if ( LevelBitmap == null || childwidth != LevelBitmap.Width || childheight != LevelBitmap.Height) // if height is different, or width is different
                    {
                        //System.Diagnostics.Debug.WriteLine($"RecursiveLayout make SP bitmap {childwidth} {childheight}");
                        MakeLevelBitmap(childwidth, childheight);
                    }
                }
            }

            if ( !needbitmap && LevelBitmap != null)
            {
                MakeLevelBitmap(0,0);       // dispose of bitmap
            }
        }

        // called to paint, with gr set to image to paint into

        protected override void Paint(Graphics gr)
        {
            if ( LevelBitmap != null )
                gr.DrawImage(LevelBitmap, 0,0, new Rectangle(ScrollOffset.X, ScrollOffset.Y, ClientWidth, ClientHeight), GraphicsUnit.Pixel);
        }

        private void SetScrollPos(int hpos, int vpos)
        {
            if (LevelBitmap != null)
            {
                int maxhsp = LevelBitmap.Width - Width;
                int maxvsp = LevelBitmap.Height - Height;
                ScrollOffset = new Point(Math.Max(0, Math.Min(hpos, maxhsp)), Math.Max(0, Math.Min(vpos, maxvsp)));
             //   System.Diagnostics.Debug.WriteLine($"ScrollPanel scrolled to {horzscrollpos} {vertscrollpos} range {maxhsp} {maxvsp}");
                Invalidate();
            }
        }
    }
}

