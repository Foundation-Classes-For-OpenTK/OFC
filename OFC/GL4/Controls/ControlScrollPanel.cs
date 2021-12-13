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

    public class GLVerticalScrollPanel : GLPanel
    {
        public GLVerticalScrollPanel(string name, Rectangle location) : base(name, location)
        {
            BorderColorNI = DefaultVerticalScrollPanelBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultVerticalScrollPanelBackColor;
        }

        public GLVerticalScrollPanel() : this("VSP?", DefaultWindowRectangle)
        {
        }

        public int ScrollRange { get { return (LevelBitmap != null) ? (LevelBitmap.Height - Height) : 0; } }
        public int ScrollPos { get { return scrollpos; } set { SetScrollPos(value); } }
        private int scrollpos = 0;

        // Width/Height is size of the control without scrolling
        // we layout the children within that area.
        // but if we have areas outside that, the bitmap is expanded to cover it

        protected override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();               // layout the children

            bool needbitmap = false;

            if (ControlsZ.Count > 0)
            {
                Rectangle r = ChildArea();
                int childheight = r.Bottom + r.Top;

                needbitmap = childheight > Height;

                if (needbitmap)
                {
                    if (LevelBitmap == null )
                    {
                       // System.Diagnostics.Debug.WriteLine("Make SP bitmap " + ClientWidth + "," + childheight);
                        MakeLevelBitmap(ClientWidth, childheight);
                    }
                    else if ( childheight != LevelBitmap.Height || LevelBitmap.Width != ClientWidth) // if height is different, or width is different
                    {
                        //   System.Diagnostics.Debug.WriteLine("Change SP bitmap " + ClientWidth + "," + childheight);
                        MakeLevelBitmap(ClientWidth, childheight);
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
                gr.DrawImage(LevelBitmap, 0,0, new Rectangle(0, scrollpos, ClientWidth, ClientHeight), GraphicsUnit.Pixel);
        }

        private void SetScrollPos(int value)
        {
            if (LevelBitmap != null)
            {
                int maxsp = LevelBitmap.Height - Height;
                scrollpos = Math.Max(0, Math.Min(value, maxsp));
                //System.Diagnostics.Debug.WriteLine("ScrollPanel scrolled to " + scrollpos + " maxsp " + maxsp);
                Invalidate();
            }
        }


    }
}

