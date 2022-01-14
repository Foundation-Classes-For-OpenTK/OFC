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

namespace GLOFC.GL4.Controls
{
    /// <summary> Padding for control. Padding is between the border line and the client area. </summary>
    [System.Diagnostics.DebuggerDisplay("{Left} {Top} {Right} {Bottom}")]
    public struct Padding
    {
        /// <summary> Left pad </summary>
        public int Left;
        /// <summary> Top pad</summary>
        public int Top;
        /// <summary> Right pad </summary>
        public int Right;
        /// <summary> Bottom  pad</summary>
        public int Bottom;
        /// <summary> Construct with all four pads</summary>
        public Padding(int left, int top, int right, int bottom) { Left = left; Top = top; Right = right; Bottom = bottom; }
        /// <summary> Construct with the same value for all four pads</summary>
        public Padding(int pad = 0) { Left = pad; Top = pad; Right = pad; Bottom = pad; }
        /// <summary> Give total horizonal pad (left+right)</summary>
        public int TotalWidth { get { return Left + Right; } }
        /// <summary> Give total vertical pad (top+bottom)</summary>
        public int TotalHeight { get { return Top + Bottom; } }

        /// <summary> Test for equality</summary>
        public static bool operator ==(Padding l, Padding r) { return l.Left == r.Left && l.Right == r.Right && l.Top == r.Top && l.Bottom == r.Bottom; }
        /// <summary> Test for inequality </summary>
        public static bool operator !=(Padding l, Padding r) { return !(l.Left == r.Left && l.Right == r.Right && l.Top == r.Top && l.Bottom == r.Bottom); }
        /// <summary> Test if equals </summary>
        public override bool Equals(Object other) { return other is Padding && this == (Padding)other; }
        /// <summary> Get Hash </summary>
        public override int GetHashCode() { return base.GetHashCode(); }
        /// <summary> Get string description</summary>
        public override string ToString() { return $"({Left} {Top} {Right} {Bottom})"; }
    };

    /// <summary> Margin for control. Margin is between the bounds of the control and the border line </summary>

    [System.Diagnostics.DebuggerDisplay("{Left} {Top} {Right} {Bottom}")]
    public struct Margin
    {
        /// <summary> Left margin </summary>
        public int Left;
        /// <summary> Top margin </summary>
        public int Top;
        /// <summary> Right margin </summary>
        public int Right;
        /// <summary> Bottom margin </summary>
        public int Bottom;
        /// <summary> Construct with all four margins </summary>
        public Margin(int left, int top, int right, int bottom) { Left = left; Top = top; Right = right; Bottom = bottom; }
        /// <summary> Construct with same value for all four margins</summary>
        public Margin(int pad = 0) { Left = pad; Top = pad; Right = pad; Bottom = pad; }
        /// <summary> Give total horizonal margin (left+right)</summary>
        public int TotalWidth { get { return Left + Right; } }
        /// <summary> Give total vertical margin (top+bottom)</summary>
        public int TotalHeight { get { return Top + Bottom; } }

        /// <summary> Test for equality</summary>
        public static bool operator ==(Margin l, Margin r) { return l.Left == r.Left && l.Right == r.Right && l.Top == r.Top && l.Bottom == r.Bottom; }
        /// <summary> Test for inequality </summary>
        public static bool operator !=(Margin l, Margin r) { return !(l.Left == r.Left && l.Right == r.Right && l.Top == r.Top && l.Bottom == r.Bottom); }
        /// <summary> Test if equals </summary>
        public override bool Equals(Object other) { return other is Margin && this == (Margin)other; }
        /// <summary> Get Hash </summary>
        public override int GetHashCode() { return base.GetHashCode(); }
        /// <summary> Get string description </summary>
        public override string ToString() { return $"({Left} {Top} {Right} {Bottom})"; }
    }

    /// <summary> Docking type of control to parent </summary>
    public enum DockingType
    {
        // these do not compound
        /// <summary> No dock, place at Bounds </summary>
        None,
        /// <summary> Fill to parent client size </summary>
        Fill,
        /// <summary> Centre in parent with selected width/height (limited to parent client size) </summary>
        Center,             
        /// <summary> Place full width, at given Top and Height </summary>
        Width,              
        /// <summary> Place full height, at given Left and Width </summary>
        Height,

        /// <summary> Full height on Left (offset by dockingmargin). Width determined by either Width (DockPercent=0) or DockPercent of parents width</summary>
        Left,
        /// <summary> Centered on Left (offset by dockingmargin). Height set by Height, Width determined by either Width (DockPercent=0) or DockPercent of parents width.</summary>
        LeftCenter,
        /// <summary> Aligned to Left Top (offset by dockingmargin). Height set by Height, Width determined by either Width (DockPercent=0) or DockPercent of parents width.</summary>
        LeftTop,
        /// <summary> Aligned to Left Bottom (offset by dockingmargin). Height set by Height, Width determined by either Width (DockPercent=0) or DockPercent of parents width.</summary>
        LeftBottom,

        /// <summary> Full height on Right (offset by dockingmargin). Width determined by either Width (DockPercent=0) or DockPercent of parents width</summary>
        Right,
        /// <summary> Centered on Right (offset by dockingmargin). Height set by Height, Width determined by either Width (DockPercent=0) or DockPercent of parents width.</summary>
        RightCenter,
        /// <summary> Aligned to Right Top (offset by dockingmargin). Height set by Height, Width determined by either Width (DockPercent=0) or DockPercent of parents width.</summary>
        RightTop,
        /// <summary> Aligned to Right Bottom (offset by dockingmargin). Height set by Height, Width determined by either Width (DockPercent=0) or DockPercent of parents width.</summary>
        RightBottom,

        /// <summary> Across top (offset by dockingmargin). Height determined by either Height (DockPercent=0) or DockPercent of parents height</summary>
        Top,
        /// <summary> Centered on Top (offset by dockingmargin). Width set by Width, Height determined by either Height (DockPercent=0) or DockPercent of parents height </summary>
        TopCenter,
        /// <summary> Aligned to Top Left (offset by dockingmargin). Width set by Width, Height determined by either Height (DockPercent=0) or DockPercent of parents height </summary>
        TopLeft,
        /// <summary> Aligned to Top Right (offset by dockingmargin). Width set by Width, Height determined by either Height (DockPercent=0) or DockPercent of parents height </summary>
        TopRight,

        /// <summary> Across bottom (offset by dockingmargin). Height determined by either Height (DockPercent=0) or DockPercent of parents height</summary>
        Bottom,
        /// <summary> Centered on Bottom (offset by dockingmargin). Width set by Width, Height determined by either Height (DockPercent=0) or DockPercent of parents height </summary>
        BottomCentre,
        /// <summary> Aligned to Bottom Left (offset by dockingmargin). Width set by Width, Height determined by either Height (DockPercent=0) or DockPercent of parents height </summary>
        BottomLeft,
        /// <summary> Aligned to Bottom Right (offset by dockingmargin). Width set by Width, Height determined by either Height (DockPercent=0) or DockPercent of parents height </summary>
        BottomRight,
    };

    /// <summary> Anchor type of control (Docking must be None)</summary>
    [Flags]
    public enum AnchorType
    {
        /// <summary> No anchor, keep in place </summary>
        None = 0,
        /// <summary> Anchor to right edge </summary>
        Right = 2,
        /// <summary> Anchor to bottom edge </summary>
        Bottom = 8,
        /// <summary> Indicate control is autoplaced. Used by controls on specific types of forms to indicate the form will autoplace it during layout</summary>
        AutoPlacement = 16, 
    };

}
