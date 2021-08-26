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
    public static class GLStencil
    {
        // to draw, the stuff you want to keep use SetStencil() which fills up those pixels with 1
        // to mask the rest, use OnlyIfEqual(0) which will allow painting only if stencil pixels are 0 (the default)
        // to only draw in the stencil area, use OnlyIfEqual(1)

        static public void SetStencil(int v = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack, int stencilactivebits = 0xff)
        {
            // stencil passes always, set stencil[pixel] = v
            GL.StencilFuncSeparate(face, StencilFunction.Always, v, mask);
            GL.StencilOpSeparate(face, StencilOp.Keep,          //if it fails, keep stencil value
                                       StencilOp.Keep,          //if depth buffer fails, Set zero
                                       StencilOp.Replace);      //if passed, replace stencil value with ref value
            GL.StencilMask(stencilactivebits);
            GL.Enable(EnableCap.StencilTest);
        }

        static public void OnlyIf(StencilFunction f, int v = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V op stencil[pixel]
            GL.StencilFuncSeparate(face, f, v, mask);
            GL.StencilOpSeparate(face, StencilOp.Keep,          //if it fails, keep stencil value
                                       StencilOp.Keep,          //if depth buffer fails, keep stencil value
                                       StencilOp.Keep);         //if passed, keep stencil value
        }

        static public void OnlyIfGreater(int v = 2, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V > stencil[pixel]
            OnlyIf(StencilFunction.Greater, v, mask, face);
        }

        static public void OnlyIfLess(int v = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V < stencil[pixel]
            OnlyIf(StencilFunction.Less, v, mask, face);
        }

        static public void OnlyIfEqual(int v = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V == stencil[pixel]
            OnlyIf(StencilFunction.Equal, v, mask, face);
        }

        static public void Off()
        {
            GL.Disable(EnableCap.StencilTest);
        }

        static public void ClearStencilBuffer()       // nicer name
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
        }
    }
}
