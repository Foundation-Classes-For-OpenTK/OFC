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

namespace OFC.GL4
{
    public class GLOperationScissors : GLOperationsBase // see GLSciccors.cs
    {
        int viewport;
        Rectangle rect;

        public GLOperationScissors(int viewport, Rectangle r)   // 0,0 is lower left
        {
            this.viewport = viewport;
            this.rect = r;
        }

        public GLOperationScissors(int viewport, Rectangle r, GLMatrixCalc c)   // 0,0 is top left
        {
            this.viewport = viewport;
            this.rect = new Rectangle(r.Left, c.ScreenSize.Height - r.Bottom, r.Width, r.Height);
        }

        public GLOperationScissors(int viewport, GLMatrixCalc c)    // see glscissors.cs
        {
            this.viewport = viewport;
            float leftoffset = c.ScreenCoordClipSpaceOffset.X - (-1);
            float topoffset = 1 - c.ScreenCoordClipSpaceOffset.Y;
            int left = (int)(leftoffset / 2.0f * c.ViewPort.Width) + c.ViewPort.Left;
            int top = (int)(topoffset / 2.0f * c.ViewPort.Height) + c.ViewPort.Top;
            int width = (int)(c.ScreenCoordClipSpaceSize.Width / 2.0f * c.ViewPort.Width);
            int height = (int)(c.ScreenCoordClipSpaceSize.Height / 2.0f * c.ViewPort.Height);
            this.rect = new Rectangle(left, top, width, height);
        }

        public override void Execute(GLMatrixCalc c)
        {
            GL.ScissorIndexed(viewport, rect.Left, rect.Top, rect.Width, rect.Height);
            GL.Enable(IndexedEnableCap.ScissorTest, viewport);
        }
    }

    public class GLOperationScissorsOff : GLOperationsBase
    {
        int viewport;

        public GLOperationScissorsOff(int viewport)
        {
            this.viewport = viewport;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLScissors.Disable(viewport);
        }
    }

}

