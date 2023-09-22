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
    /// This is the base interface which feed thru events from the window driving to consumers
    /// GLWinFormControl is based on it, and uses OpenTk.GLControl as the winforms control to receive all key/mouse events from
    /// GLControlDisplay is a class based on it so that it can dispatch events in the same way
    /// </summary>
    public interface GLWindowControl
    {
        // Gets

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

        /// <summary> GL Profile Type dependant on GL version requested </summary>
        public enum GLProfile
        {
            /// <summary> Compatibility Profile </summary>
            Compatibility,
            /// <summary> Core Profile (3.1+) </summary>
            Core
        };

        /// <summary> GL profile </summary>
        public GLProfile Profile { get ; }

        /// <summary> Is context current to opengl </summary>
        bool IsContextCurrent();

        /// <summary> Resize call back </summary>
        Action<GLWindowControl> Resize { get; set; }

        /// <summary> Paint call back. ulong is elapsed time in ms </summary>
        Action<ulong> Paint { get; set; }
        /// <summary>Mouse down call back </summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseDown { get; set; }
        /// <summary>Mouse up call back </summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseUp { get; set; }
        /// <summary> Mouse move call back</summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseMove { get; set; }
        /// <summary> Mouse enter call back</summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseEnter { get; set; }
        /// <summary>Mouse leave call back </summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseLeave { get; set; }
        /// <summary> Mouse click call back</summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseClick { get; set; }
        /// <summary> Mouse double click call back</summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseDoubleClick { get; set; }
        /// <summary> Mouse wheel call back</summary>
        Action<GLWindowControl, GLMouseEventArgs> MouseWheel { get; set; }
        /// <summary> Key down call back</summary>
        Action<GLWindowControl, GLKeyEventArgs> KeyDown { get; set; }
        /// <summary> Key up call back</summary>
        Action<GLWindowControl, GLKeyEventArgs> KeyUp { get; set; }
        /// <summary> Key press call back</summary>
        Action<GLWindowControl, GLKeyEventArgs> KeyPress { get; set; }
        /// <summary> </summary>


        /// <summary> Make sure current context associated with this instance is selected by opengl </summary>
        void EnsureCurrentContext();
        /// <summary> Invalidate window </summary>
        void Invalidate();

        /// <summary> Cursor type</summary>
        public enum GLCursorType
        {
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

        /// <summary> Set the cursor type</summary>
        void SetCursor(GLCursorType t);
        /// <summary> Get elapsed time of run </summary>
        ulong ElapsedTimems { get; }      
       
    }
}
