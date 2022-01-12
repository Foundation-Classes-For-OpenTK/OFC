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

        /// <summary>
        /// Setup scissors using open GL co-ords
        /// </summary>
        /// <param name="viewport">Viewport to scissor</param>
        /// <param name="rectangle">Rectangle in GL co-ords, 0,0 is lower left</param>
        static public void Set(int viewport, Rectangle rectangle)             
        {
            GL.ScissorIndexed(viewport, rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }

        /// <summary>
        /// Setup scissors using MatrixCalc screen co-ords.
        /// </summary>
        /// <param name="viewport">Viewport to scissor</param>
        /// <param name="rectangle">Rectangle in screen co-ords, 0,0 is top left</param>
        /// <param name="matrixcalc">MatrixCalc with current screen setup</param>
        static public void Set(int viewport, Rectangle rectangle, GLMatrixCalc matrixcalc)
        {
            GL.ScissorIndexed(viewport, rectangle.Left, matrixcalc.ScreenSize.Height - rectangle.Bottom, rectangle.Width, rectangle.Height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }

        /// <summary>
        /// Setup scissors to screen defined by MatrixCalc
        /// </summary>
        /// <param name="viewport">Viewport to scissor</param>
        /// <param name="matrixcalc">MatrixCalc with current screen setup</param>
        static public void SetToScreenCoords(int viewport, GLMatrixCalc matrixcalc)
        {
            float leftoffset = matrixcalc.ScreenCoordClipSpaceOffset.X - (-1);
            float topoffset = 1 - matrixcalc.ScreenCoordClipSpaceOffset.Y;
            int left = (int)(leftoffset / 2.0f * matrixcalc.ViewPort.Width) + matrixcalc.ViewPort.Left;
            int top = (int)(topoffset / 2.0f * matrixcalc.ViewPort.Height) + matrixcalc.ViewPort.Top;
            int width = (int)(matrixcalc.ScreenCoordClipSpaceSize.Width / 2.0f * matrixcalc.ViewPort.Width);
            int height = (int)(matrixcalc.ScreenCoordClipSpaceSize.Height / 2.0f * matrixcalc.ViewPort.Height);
            GL.ScissorIndexed(viewport, left, matrixcalc.ScreenSize.Height - (top+height), width, height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }

    }
}
