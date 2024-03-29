﻿/*
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
    /// <summary>
    /// Button control
    /// </summary>
    public class GLButton : GLButtonTextBase
    {
        /// <summary> Callback when button is clicked </summary>
        public Action<GLBaseControl, GLMouseEventArgs> Click { get; set; } = null;

        /// <summary> Construct with name and bounds</summary>
        public GLButton(string name, Rectangle location) : base(name, location)
        {
            SetNI(padding: new PaddingType(1), borderwidth: 1);
            BorderColorNI = DefaultButtonBorderColor;
            BackColorGradientAltNI = BackColorNI = DefaultButtonBackColor;
            Focusable = true;
            InvalidateOnFocusChange = true;
        }

        /// <summary> Construct with name, bounds and text </summary>
        public GLButton(string name, Rectangle location, string text) : this(name, location)
        {
            TextNI = text;
        }

        /// <summary> Construct with name, bounds, image and stretch </summary>
        public GLButton(string name, Rectangle location, Image img, bool stretch) : this(name, location)
        {
            TextNI = "";
            Image = img;
            ImageStretch = stretch;
        }

        /// <summary> Default Constructor </summary>
        public GLButton() : this("But?", DefaultWindowRectangle, "")
        {
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.SizeControl(Size)"/>
        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
                ButtonAutoSize(new Size(0,0));
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.Paint(Graphics)"/>
        protected override void Paint(Graphics gr)
        {
            if (ClientWidth < 1 || ClientHeight<1)
                return;
            //System.Diagnostics.Debug.WriteLine($"Paint button {Name} focus {Focused}");

            // we reduce the face area by the focus box reserved area..
            var buttonfacearea = ShowFocusBox ? new Rectangle(2,2, ClientRectangle.Width - 4, ClientRectangle.Height - 4) : ClientRectangle;
            PaintButtonFace(buttonfacearea, gr, PaintButtonFaceColor());

            // we pass the whole client rectangle for text image use
            PaintButtonTextImageFocus(ClientRectangle, gr,true);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnMouseClick(GLMouseEventArgs)"/>
        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            if (SetFocusOnClick)
                SetFocus();

            base.OnMouseClick(e);

            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
                OnClick(e);
        }

        /// <summary> Call to perform Click functionality  </summary>
        protected virtual void OnClick(GLMouseEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        /// <inheritdoc cref="GLOFC.GL4.Controls.GLBaseControl.OnKeyPress(GLKeyEventArgs)"/>
        protected override void OnKeyPress(GLKeyEventArgs e)
        {
            if ( e.KeyChar == 13 )
            {
                OnClick(new GLMouseEventArgs(Point.Empty));
            }
        }


    }
}
