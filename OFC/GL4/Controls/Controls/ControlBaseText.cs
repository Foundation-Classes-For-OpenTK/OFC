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
    /// <summary>
    ///  Base class for controls with Fore Color
    /// </summary>
    public abstract class GLForeDisplayBase : GLBaseControl
    {
        private protected GLForeDisplayBase(string name, Rectangle location) : base(name, location)
        {
        }

        /// <summary> Fore color </summary>
        public Color ForeColor { get { return foreColor; } set { foreColor = value; Invalidate(); } }       // of text
        /// <summary> Fore color disabled scaling </summary>
        public float ForeDisabledScaling { get { return foreDisabledScaling; } set { if (value != foreDisabledScaling) { foreDisabledScaling = value; Invalidate(); } } }

        private protected Color foreColor { get; set; } = Color.Red;
        private protected float foreDisabledScaling = 0.5F;
    }

    /// <summary>
    /// Base class for controls with Fore Color and Text
    /// </summary>
    public abstract class GLForeDisplayTextBase : GLForeDisplayBase
    {
        private protected GLForeDisplayTextBase(string name, Rectangle location) : base(name, location)
        {
        }

        /// <summary> Text </summary>
        public string Text { get { return text; } set { text = value; TextValueChanged(); } }

        /// <summary> Text Alignment </summary>
        public ContentAlignment TextAlign { get { return textAlign; } set { textAlign = value; Invalidate(); } }

        private protected string text = "";
        private protected ContentAlignment textAlign { get; set; } = ContentAlignment.MiddleLeft;

        private protected abstract void TextValueChanged();                                   
    }
}
