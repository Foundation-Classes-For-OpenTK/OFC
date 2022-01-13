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
    /// This namespace contains the base GL classes 
    /// * The MatrixCalc, which can transform eye and lookat positions into projection and model transforms, and can handle screen co-ordinated, viewports.
    /// * Classes to handle mouse and keyboard events
    /// * Static classes to assist in using GL items such as vectors and matrices
    /// * Static classes to wrap GL functions in more friendly wrappers
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

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

    /// <summary> Cursor type</summary>
    public enum GLCursorType {
        /// <summary> Normal </summary>
        Normal,
        /// <summary> Wait </summary>
        Wait,
        /// <summary> NS </summary>
        NS,
        /// <summary> EW </summary>
        EW,
        /// <summary> Move</summary>
        Move,
        /// <summary> NWSE</summary>
        NWSE
    };


    /// <summary>
    /// This is the base interface which feed thru events from the window driving to consumers
    /// GLWinFormControl is based on it, and uses OpenTk.GLControl as the winforms control to receive all key/mouse events from
    /// GLControlDisplay is a class based on it so that it can dispatch events in the same way
    /// </summary>
    public interface GLWindowControl
    {
        /// <summary> Resize call back </summary>
        Action<Object> Resize { get; set; }
        /// <summary> Paint call back </summary>
        Action<Object,ulong> Paint { get; set; }
        /// <summary> Mouse down call back </summary>
        Action<Object, GLMouseEventArgs> MouseDown { get; set; }
        /// <summary>Mouse up call back </summary>
        Action<Object, GLMouseEventArgs> MouseUp { get; set; }
        /// <summary> Mouse move call back</summary>
        Action<Object, GLMouseEventArgs> MouseMove { get; set; }
        /// <summary> Mouse enter call back</summary>
        Action<Object, GLMouseEventArgs> MouseEnter { get; set; }
        /// <summary>Mouse leave call back </summary>
        Action<Object, GLMouseEventArgs> MouseLeave { get; set; }
        /// <summary> Mouse click call back</summary>
        Action<Object, GLMouseEventArgs> MouseClick { get; set; }
        /// <summary> Mouse double click call back</summary>
        Action<Object, GLMouseEventArgs> MouseDoubleClick { get; set; }
        /// <summary> Mouse wheel call back</summary>
        Action<Object, GLMouseEventArgs> MouseWheel { get; set; }
        /// <summary> Key down call back</summary>
        Action<Object, GLKeyEventArgs> KeyDown { get; set; }
        /// <summary> Key up call back</summary>
        Action<Object, GLKeyEventArgs> KeyUp { get; set; }
        /// <summary> Key press call back</summary>
        Action<Object, GLKeyEventArgs> KeyPress { get; set; }
        /// <summary> </summary>

        /// <summary> Make sure current context associated with this instance is selected by opengl </summary>
        void EnsureCurrentContext();
        /// <summary> Is context current to opengl </summary>
        bool IsCurrent();
        /// <summary> Invalidate window </summary>
        void Invalidate();
        /// <summary> Get screen rectangle of gl window </summary>
        Rectangle GLWindowControlScreenRectangle { get; }
        /// <summary> Get mouse position in gl window </summary>
        Point MousePosition { get; }
        /// <summary> Get mouse screen position taking into consideration the viewport/scaling etc</summary>
        Point MouseWindowPosition { get; }
        /// <summary> Screen width </summary>
        int Width { get; }
        /// <summary> Screen height</summary>
        int Height { get; }
        /// <summary> Screen size</summary>
        Size Size { get; }
        /// <summary> Is GL window focused? </summary>
        bool Focused { get; }
        /// <summary> Set the cursor type</summary>
        void SetCursor(GLCursorType t);
        /// <summary> Get elapsed time of run </summary>
        ulong ElapsedTimems { get; }      
       
    }
}
