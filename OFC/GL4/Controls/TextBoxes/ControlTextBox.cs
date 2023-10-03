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

using System.Drawing;

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Text Box, a MultiLineTextBox with MultiLineMode turned off
    /// </summary>
    public class GLTextBox : GLMultiLineTextBox
    {
        /// <summary> Constructor with name, bounds and optional text</summary>
        public GLTextBox(string name, Rectangle pos, string text = "", bool enablethemer = true) : base(name, pos, text, enablethemer)
        {
            MultiLineMode = false;
        }

        /// <summary> Constructor with name, bounds, text, colors</summary>
        public GLTextBox(string name, Rectangle pos, string text, Color backcolor, Color forecolor, bool enablethemer = true) : this(name, pos, text, enablethemer)
        {
            BackColor = backcolor;
            ForeColor = forecolor;
        }

        /// <summary> Default constructor </summary>
        public GLTextBox() : this("TB?", DefaultWindowRectangle, "")
        {
        }
    }
}
