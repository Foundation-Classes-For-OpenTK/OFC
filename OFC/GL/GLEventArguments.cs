/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
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
using System.Windows.Forms;

namespace GLOFC
{
    /// <summary>
    /// Mouse Event Args to program
    /// </summary>
    public class GLMouseEventArgs
    {
        /// <summary> Button (ORed together)</summary>
        [System.Flags]
        public enum MouseButtons {
            /// <summary> None </summary>
            None = 0,
            /// <summary> Left </summary>
            Left = 1,
            /// <summary> Middle </summary>
            Middle = 2,
            /// <summary> right </summary>
            Right = 4, };

        /// <summary> Constructor </summary>
        public GLMouseEventArgs(Point l) { Button = MouseButtons.None; WindowLocation = l; Clicks = 0; Delta = 0; Handled = false; Area = AreaType.Client; Alt = Ctrl = Shift = false; }
        /// <summary> Constructor</summary>
        public GLMouseEventArgs(MouseButtons b, Point l, int c, int delta, bool alt, bool ctrl, bool sh) { Button = MouseButtons.None; WindowLocation = l; Clicks = c; Delta = delta; Handled = false; Area = AreaType.Client; Alt = alt; Shift = sh; Ctrl = ctrl; }
        /// <summary> Constructor</summary>
        public GLMouseEventArgs(MouseButtons b, Point l, int c, bool alt, bool ctrl, bool sh) { Button = b; WindowLocation = l; Clicks = c;Delta = 0; Handled = false; Area = AreaType.Client; Alt = alt; Shift = sh; Ctrl = ctrl; }

        // Set by GLWinForm etc

        /// <summary> Button</summary>
        public MouseButtons Button { get; set; }
        /// <summary> Window location </summary>
        public Point WindowLocation { get; set; }       
        /// <summary> Number of clicks </summary>
        public int Clicks { get; set; }
        /// <summary> Delta (used for wheel) </summary>
        public int Delta { get; set; }
        /// <summary> Alt on </summary>
        public bool Alt { get; private set; }
        /// <summary> Shift on </summary>
        public bool Shift { get; private set; }
        /// <summary> Ctrl on </summary>
        public bool Ctrl { get; private set; }
        /// <summary> Has this been handled by someone? </summary>
        public bool Handled { get; set; }               

        // Set by displaycontrol

        /// <summary> Position translated to viewport screen location. Cursor adjusted to left/top of viewport with no scaling </summary>
        public Point ViewportLocation { get; set; }     
        /// <summary> Position translated from viewport to screen location. Takes into account viewport and screen coord scaling. </summary>
        public Point ScreenCoord { get; set; }

        // Set by displaycontrol if over a control.  Tell by Control != null

        /// <summary> DisplayControl - control found </summary>
        public Object Control { get; set; }             // Control type found (GLBaseControl)
        /// <summary> DisplayControl - bounds of control | DGV - Bounds of cell </summary>
        public Rectangle Bounds { get; set; }           // bounds of control (in parent terms)
        /// <summary> DisplayControl - bounds location | DGV - position in cell </summary>
        public Point BoundsLocation { get; set; }       // offset within control bounds
        /// <summary> DisplayControl - location in client area| DGV - position in cell after padding</summary>
        public Point Location { get; set; }             // offset within control ClientRectangle (similar to Location in winforms) in bitmap terms
        /// <summary> Area hit type </summary>
        public enum AreaType {
            /// <summary> Client </summary>
            Client,
            /// <summary> Left of client (in margin/border/padding) </summary>
            Left,
            /// <summary> Top of client (in margin/border/padding) </summary>
            Top,
            /// <summary> Right of client (in margin/border/padding)  </summary>
            Right,
            /// <summary> Bottom of client (in margin/border/padding) </summary>
            Bottom,
            /// <summary> In right bottom corner (in margin/border/padding) </summary>
            NWSE
        };
        /// <summary> DisplayControl - Area of mouse </summary>
        public AreaType Area { get; set; }
        /// <summary> Was it focused at click. Valid for Click only </summary>
        public bool WasFocusedAtClick { get; set; }     
    }

    /// <summary>
    /// Key arguments
    /// </summary>
    public class GLKeyEventArgs     // class so passed by ref
    {
        /// <summary> Alt </summary>
        public bool Alt { get; private set; }
        /// <summary> Control </summary>
        public bool Control { get; private set; }
        /// <summary> Shift </summary>
        public bool Shift { get; private set; }
        /// <summary> KeyCode </summary>
        public Keys KeyCode { get; private set; }
        /// <summary> KeyValue </summary>
        public int KeyValue { get; private set; }
        /// <summary> KeyChar </summary>
        public char KeyChar { get; private set; }      // only on key press, others are not on key press
        /// <summary> Modifier list </summary>
        public Keys Modifiers { get; private set; }
        /// <summary>Has this been handled by someone? </summary>
        public bool Handled { get; set; }

        /// <summary> Constructor </summary>
        public GLKeyEventArgs(bool a, bool c, bool s, Keys kc, int kv, Keys mod)
        {
            Alt = a; Control = c; Shift = s; KeyCode = kc; KeyValue = kv; Modifiers = mod; Handled = false; KeyChar = '\0';
        }
        /// <summary> Constructor </summary>
        public GLKeyEventArgs(char key)
        {
            Alt = false; Control = false; Shift = false; KeyCode = Keys.None; KeyValue = 0; Modifiers = Keys.None; Handled = false; KeyChar = key;
        }
    }

    /// <summary>
    /// Handled args
    /// </summary>
    public class GLHandledArgs     // class so passed by ref
    {
        /// <summary> Has been handled? </summary>
        public bool Handled { get; set; }

        /// <summary> Constructor </summary>
        public GLHandledArgs()
        {
            Handled = false;
        }
    }

}
