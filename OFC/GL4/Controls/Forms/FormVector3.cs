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
 * 
 */

using OpenTK;
using System;
using System.Drawing;
using static GLOFC.GL4.Controls.GLBaseControl;
using static GLOFC.GL4.Controls.GLForm;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Vector form for 2 or 3 vector value
    /// </summary>
    /// 

    public class GLFormVector3: GLForm
    {
        /// <summary>
        /// Value of Vector set by form
        /// </summary>
        public Vector3 Value { get { return new Vector3(x.Value, y.Value, z?.Value??0); } }

        /// <summary>
        /// Set the backcolor, and the colours of text boxes buttons in this form
        /// </summary>
        public new Color BackColor { get { return base.BackColor; } set { x.BackColor = y.BackColor = ok.BackColor = base.BackColor = value; if (z != null) z.BackColor = value; } }

        /// <summary>
        /// Construct Vector3 form
        /// Demonstrates how to code a specific type of form
        /// Use form.DialogResultChanged to pick up the OK button as the dialog result will be set to OK by the button, and the form will then close.
        /// If you need to know if the form has closed, use form.FormClose and form.DialogResult will either be OK or None (close by X)
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="title">title text</param>
        /// <param name="value">value to set</param>
        /// <param name="location">position on screen</param>
        /// <param name="fixedpos">if fixed pos</param>
        /// <param name="margin">margin value</param>
        /// <param name="padding">padding value</param>
        /// <param name="borderwidth">borderwidth value</param>
        /// <param name="vector2">true for vector2 instead of vector3 presentation</param>
        public GLFormVector3(string name, string title, Vector3 value, Rectangle location, bool fixedpos = false, 
                            int margin = 2, int padding = 2, int borderwidth = 1, bool vector2 = false) :
                        base(name, title, location, fixedpos, margin, padding, borderwidth)
        {
            GLLabel xl = new GLLabel("xl", new Rectangle(4, 10, 20, 20), "X");
            Add(xl);
            x = new GLNumberBoxFloat("x", new Rectangle(30, 10, 200, 20), value.X);
            x.TabOrder = 0;
            x.ValidityChanged += OKValid;
            Add(x);
            GLLabel yl = new GLLabel("xl", new Rectangle(4, 40, 20, 20), "Y");
            Add(yl);
            y = new GLNumberBoxFloat("y", new Rectangle(30, 40, 200, 20), value.Y);
            y.TabOrder = 1;
            y.ValidityChanged += OKValid;
            Add(y);
            z = null;
            if (!vector2)
            {
                GLLabel zl = new GLLabel("xl", new Rectangle(4, 70, 20, 20), "Z");
                Add(zl);
                z = new GLNumberBoxFloat("z", new Rectangle(30, 70, 200, 20), value.Z);
                z.TabOrder = 2;
                z.ValidityChanged += OKValid;
                Add(z);
            }

            ok = new GLButton("ok", new Rectangle(150, 100, 80, 20), "OK");
            ok.Click += (s, e) => { DialogResult= DialogResultEnum.OK; Close(); };
            ok.TabOrder = 3;
            Add(ok);

            Resizeable = false;
        }

        private void OKValid(GLBaseControl c, bool value)
        {
            ok.Enabled = x.IsValid && y.IsValid && (z?.IsValid??true);
        }

        private GLNumberBoxFloat x, y, z;
        private GLButton ok;
    }
}

