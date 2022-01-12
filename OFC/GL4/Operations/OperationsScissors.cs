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

using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace GLOFC.GL4.Operations
{
    /// <summary>
    /// Operations on Scissors.
    /// </summary>
    public class GLOperationScissors : GLOperationsBase 
    {
        /// <inheritdoc cref="GLOFC.GL4.GLScissors.Set(int, Rectangle)"/>
        public GLOperationScissors(int viewport, Rectangle rectangle)   
        {
            this.viewport = viewport;
            this.rect = rectangle;
        }

        /// <inheritdoc cref="GLOFC.GL4.GLScissors.Set(int, Rectangle, GLMatrixCalc)"/>
        public GLOperationScissors(int viewport, Rectangle rectangle, GLMatrixCalc matrixcalc)  
        {
            this.viewport = viewport;
            this.rect = new Rectangle(rectangle.Left, matrixcalc.ScreenSize.Height - rectangle.Bottom, rectangle.Width, rectangle.Height);
        }

        /// <inheritdoc cref="GLOFC.GL4.GLScissors.SetToScreenCoords(int, GLMatrixCalc)"/>
        public GLOperationScissors(int viewport, GLMatrixCalc matrixcalc) 
        {
            this.viewport = viewport;
            float leftoffset = matrixcalc.ScreenCoordClipSpaceOffset.X - (-1);
            float topoffset = 1 - matrixcalc.ScreenCoordClipSpaceOffset.Y;
            int left = (int)(leftoffset / 2.0f * matrixcalc.ViewPort.Width) + matrixcalc.ViewPort.Left;
            int top = (int)(topoffset / 2.0f * matrixcalc.ViewPort.Height) + matrixcalc.ViewPort.Top;
            int width = (int)(matrixcalc.ScreenCoordClipSpaceSize.Width / 2.0f * matrixcalc.ViewPort.Width);
            int height = (int)(matrixcalc.ScreenCoordClipSpaceSize.Height / 2.0f * matrixcalc.ViewPort.Height);
            this.rect = new Rectangle(left, top, width, height);
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GL.ScissorIndexed(viewport, rect.Left, rect.Top, rect.Width, rect.Height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }

        private int viewport;
        private Rectangle rect;

    }

    /// <summary>
    /// Turn off scissors
    /// </summary>

    public class GLOperationScissorsOff : GLOperationsBase
    {
        /// <summary> Constructor. Viewport is the number to turn off </summary>
        public GLOperationScissorsOff(int viewport)
        {
            this.viewport = viewport;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLScissors.Disable(viewport);
        }
        
        private int viewport;
    }

}

