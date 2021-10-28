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
    // a button type control

    public class GLButton : GLButtonTextBase
    {
        public Action<GLBaseControl, GLMouseEventArgs> Click { get; set; } = null;
        public Action<GLBaseControl> Return { get; set; } = null;

        public GLButton(string name, Rectangle location) : base(name, location)
        {
            SetNI(padding: new Padding(2), borderwidth: 1);
            BorderColorNI = DefaultButtonBorderColor;
            BackColorNI = DefaultButtonBorderBackColor;
            Focusable = true;
            InvalidateOnFocusChange = true;
        }

        public GLButton(string name, Rectangle location, string text) : this(name, location)
        {
            TextNI = text;
        }

        public GLButton(string name, Rectangle location, Image img, bool stretch) : this(name, location)
        {
            TextNI = "";
            Image = img;
            ImageStretch = stretch;
        }

        public GLButton() : this("But?", DefaultWindowRectangle, "")
        {
        }

        protected override void SizeControl(Size parentsize)
        {
            base.SizeControl(parentsize);
            if (AutoSize)
                ButtonAutoSize(new Size(0,0));
        }

        protected override void Paint(Graphics gr)
        {
            if (ClientWidth < 1 || ClientHeight<1)
                return;
            PaintButtonBack(ClientRectangle, gr, PaintButtonBackColor());
            PaintButton(ClientRectangle, gr,true);
        }

        protected override void OnMouseClick(GLMouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == GLMouseEventArgs.MouseButtons.Left)
                OnClick(e);
        }

        public virtual void OnClick(GLMouseEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        protected override void OnKeyPress(GLKeyEventArgs e)
        {
            if ( e.KeyChar == 13 )
            {
                OnReturn();
            }
        }

        public virtual void OnReturn()
        {
            Return?.Invoke(this);
        }

    }
}
