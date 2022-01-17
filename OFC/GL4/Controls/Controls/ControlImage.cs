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

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Image Base class
    /// </summary>
    public abstract class GLImageBase : GLBaseControl
    {
        /// <summary> Image to display </summary>
        public Image Image { get { return image; } set { image = value; Invalidate(); } }
        /// <summary> If to stretch the image to the control size </summary>
        public bool ImageStretch { get { return imagestretch; } set { imagestretch = value; Invalidate(); } }
        /// <summary> Image align within control </summary>
        public System.Drawing.ContentAlignment ImageAlign { get { return imagealign; } set { imagealign = value; Invalidate(); } }

        /// <summary> Create an image with this name and bounds </summary>
        public GLImageBase(string name, Rectangle window) : base(name, window)
        {
        }

        /// <summary> What colour brightness scaling to apply to back color if the control is disabled </summary>
        public float BackDisabledScaling { get { return backDisabledScaling; } set { if (backDisabledScaling != value) { backDisabledScaling = value; Invalidate(); } } }
        /// <summary> What colour brightness scaling to apply to fore color if the control is disabled</summary>
        public float ForeDisabledScaling { get { return foreDisabledScaling; } set { if (foreDisabledScaling != value) { foreDisabledScaling = value; Invalidate(); } } }

        /// <summary>
        /// Set up a remap of color
        /// </summary>
        /// <param name="remap">ColorMap structure for remapping</param>
        /// <param name="colormatrix">Color remap matrix</param>
        /// <param name="disabledscaling">Disabled scaling</param>
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

        private protected System.Drawing.Imaging.ImageAttributes drawnImageAttributesEnabled = null;         // Image override (colour etc) for background when using Image while Enabled.
        private protected System.Drawing.Imaging.ImageAttributes drawnImageAttributesDisabled = null;        // Image override (colour etc) for background when using Image while !Enabled.

        private protected void DrawImage(Image image, Rectangle box, Graphics g, System.Drawing.Imaging.ImageAttributes imgattr )
        {
            Size isize = ImageStretch ? box.Size : image.Size;
            Rectangle drawarea = ImageAlign.ImagePositionFromContentAlignment(box, isize, true, true);

            if (imgattr != null)
                g.DrawImage(image, drawarea, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imgattr);
            else
                g.DrawImage(image, drawarea, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
        }

        /// <summary> Dispose of control </summary>
        public override void Dispose()
        {
            base.Dispose();
            drawnImageAttributesEnabled?.Dispose();
            drawnImageAttributesDisabled?.Dispose();
        }
    }

    /// <summary>
    /// A Image control
    /// </summary>
    public class GLImage : GLImageBase
    {
        /// <summary>
        /// Create an image control
        /// </summary>
        /// <param name="name">Control name</param>
        /// <param name="location">Bounds of control</param>
        /// <param name="img">The image</param>
        /// <param name="backcolour">Optional Back colour for control</param>
        public GLImage(string name, Rectangle location, Image img, Color? backcolour = null) : base(name,location)
        {
            BackColorNI = backcolour.HasValue ? backcolour.Value: Color.Transparent;
            Image = img;
        }

        /// <summary> Default Constructor </summary>
        public GLImage() : this("I?",DefaultWindowRectangle,null)
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);

            if (AutoSize)
            {
                SetNI(clientsize: Image.Size);
            }
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            base.DrawImage(Image, ClientRectangle, gr, (Enabled) ? drawnImageAttributesEnabled : drawnImageAttributesDisabled);
        }
    }
}
