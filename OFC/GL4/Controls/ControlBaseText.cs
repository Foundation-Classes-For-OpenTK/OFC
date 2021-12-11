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

using System.Drawing;

namespace GLOFC.GL4.Controls
{
    public abstract class GLForeDisplayBase : GLBaseControl
    {
        public GLForeDisplayBase(string name, Rectangle location) : base(name, location)
        {
        }

        public Color ForeColor { get { return foreColor; } set { foreColor = value; Invalidate(); } }       // of text
        public float DisabledScaling { get { return disabledScaling; } set { if (value != disabledScaling) { disabledScaling = value; Invalidate(); } } }

        protected Color foreColor { get; set; } = Color.Red;
        private float disabledScaling = 0.5F;
    }

    public abstract class GLForeDisplayTextBase : GLForeDisplayBase
    {
        public GLForeDisplayTextBase(string name, Rectangle location) : base(name, location)
        {
        }

        public string Text { get { return text; } set { text = value; TextValueChanged(); } }

        public ContentAlignment TextAlign { get { return textAlign; } set { textAlign = value; Invalidate(); } }

        protected string text = "";
        protected ContentAlignment textAlign { get; set; } = ContentAlignment.MiddleLeft;

        protected abstract void TextValueChanged();                                   
    }
}
