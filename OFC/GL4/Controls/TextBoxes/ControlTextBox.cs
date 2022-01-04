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
    public class GLTextBox : GLMultiLineTextBox
    {
        public GLTextBox(string name, Rectangle pos, string text = "") : base(name, pos, text)
        {
            MultiLineMode = false;
        }

        public GLTextBox() : this("TB?", DefaultWindowRectangle, "")
        {
        }
    }
}
