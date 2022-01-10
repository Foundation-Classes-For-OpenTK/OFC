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


using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace GLOFC.GL4
{
    /// <summary>
    /// Scissor control - use manually between render lists or use via operations
    /// </summary>

    public static class GLScissors 
    {
        /// <summary> Disable scissor test on this viewport</summary>
        static public void Disable(int viewport)
        {
            GL.Disable(IndexedEnableCap.ScissorTest, viewport);
        }

        /// <summary> Enable scissor test on this viewport with this rectangle (0,0) is lower left</summary>
        static public void Set(int viewport, Rectangle r)             
        {
            GL.ScissorIndexed(viewport, r.Left, r.Top, r.Width, r.Height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }

        /// <summary> Enable scissor test on this viewport with this rectangle (0,0) is top left, GLMatrixCalc gives screen size</summary>
        static public void Set(int viewport, Rectangle r, GLMatrixCalc c)  // 0,0 is top left
        {
            GL.ScissorIndexed(viewport, r.Left, c.ScreenSize.Height - r.Bottom, r.Width, r.Height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }

        /// <summary> Calculate the area to scissor for the MatrixCalc screen coords.. 
        /// Note scissor is defined by the openGL whole window, not its viewport.
        /// </summary>

        static public void SetToScreenCoords(int viewport, GLMatrixCalc c)
        {
            float leftoffset = c.ScreenCoordClipSpaceOffset.X - (-1);
            float topoffset = 1 - c.ScreenCoordClipSpaceOffset.Y;
            int left = (int)(leftoffset / 2.0f * c.ViewPort.Width) + c.ViewPort.Left;
            int top = (int)(topoffset / 2.0f * c.ViewPort.Height) + c.ViewPort.Top;
            int width = (int)(c.ScreenCoordClipSpaceSize.Width / 2.0f * c.ViewPort.Width);
            int height = (int)(c.ScreenCoordClipSpaceSize.Height / 2.0f * c.ViewPort.Height);
            GL.ScissorIndexed(viewport, left, c.ScreenSize.Height - (top+height), width, height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }

    }
}
