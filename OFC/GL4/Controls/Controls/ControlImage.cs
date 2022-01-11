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
using System;
using System.Drawing;

#pragma warning disable 1591

namespace GLOFC.GL4.Controls
{
    public abstract class GLImageBase : GLBaseControl
    {
        public Image Image { get { return image; } set { image = value; Invalidate(); } }
        public bool ImageStretch { get { return imagestretch; } set { imagestretch = value; Invalidate(); } }
        public System.Drawing.ContentAlignment ImageAlign { get { return imagealign; } set { imagealign = value; Invalidate(); } }

        public GLImageBase(string name, Rectangle window) : base(name, window)
        {
        }

        public float BackDisabledScaling { get { return backDisabledScaling; } set { if (backDisabledScaling != value) { backDisabledScaling = value; Invalidate(); } } }
        public float ForeDisabledScaling { get { return foreDisabledScaling; } set { if (foreDisabledScaling != value) { foreDisabledScaling = value; Invalidate(); } } }

        public void SetDrawnBitmapRemapTable(System.Drawing.Imaging.ColorMap[] remap, float[][] colormatrix = null, float disabledscaling = 0.5f)
        {
            if (remap == null)
                throw new ArgumentNullException(nameof(remap));

            drawnImageAttributesEnabled?.Dispose();
            drawnImageAttributesDisabled?.Dispose();

            ControlHelpersStaticFunc.ComputeDrawnPanel(out drawnImageAttributesEnabled, out drawnImageAttributesDisabled, disabledscaling, remap, colormatrix);
            Invalidate();
        }

        private Image image;
        private bool imagestretch = false;
        private System.Drawing.ContentAlignment imagealign = ContentAlignment.MiddleCenter;
        private float backDisabledScaling = 0.75F;
        private float foreDisabledScaling = 0.50F;

        protected System.Drawing.Imaging.ImageAttributes drawnImageAttributesEnabled = null;         // Image override (colour etc) for background when using Image while Enabled.
        protected System.Drawing.Imaging.ImageAttributes drawnImageAttributesDisabled = null;        // Image override (colour etc) for background when using Image while !Enabled.

        protected void DrawImage(Image image, Rectangle box, Graphics g, System.Drawing.Imaging.ImageAttributes imgattr )
        {
            Size isize = ImageStretch ? box.Size : image.Size;
            Rectangle drawarea = ImageAlign.ImagePositionFromContentAlignment(box, isize, true, true);

            if (imgattr != null)
                g.DrawImage(image, drawarea, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imgattr);
            else
                g.DrawImage(image, drawarea, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
        }

        public override void Dispose()
        {
            base.Dispose();
            drawnImageAttributesEnabled?.Dispose();
            drawnImageAttributesDisabled?.Dispose();
        }
    }

    public class GLImage : GLImageBase
    {
        public GLImage(string name, Rectangle location, Bitmap bmp, Color? backcolour = null) : base(name,location)
        {
            BackColor = backcolour.HasValue ? backcolour.Value: Color.Transparent;
            Image = bmp;
        }

        public GLImage() : this("I?",DefaultWindowRectangle,null)
        {
        }

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);

            if (AutoSize)
            {
                SetNI(clientsize: Image.Size);
            }
        }

        protected override void Paint(Graphics gr)
        {
            base.DrawImage(Image, ClientRectangle, gr, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
        }
    }
}
