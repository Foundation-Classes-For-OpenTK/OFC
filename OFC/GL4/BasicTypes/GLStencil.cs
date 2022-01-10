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
    /// Stencil Control functions, for use manually or via Operations
    /// To draw, the stuff you want to keep use SetStencil() which fills up those pixels with 1 
    /// To mask the rest, use OnlyIfEqual(0) which will allow painting only if stencil pixels are 0 (the default)
    /// To only draw in the stencil area, use OnlyIfEqual(1)
    /// </summary>
   
    public static class GLStencil
    {
        /// <summary>
        /// Quick setup of all stencil parameters for a StencilFunction.Always and enable
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        /// <param name="stencilactivebits">Which bits are allowed to change when writing V to stencil buffer</param>
        static public void SetStencil(int refvalue = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack, int stencilactivebits = 0xff)
        {
            // stencil passes always, set stencil[pixel] = v
            GL.StencilFuncSeparate(face, StencilFunction.Always, refvalue, mask);
            GL.StencilOpSeparate(face, StencilOp.Keep,          //if it fails, keep stencil value
                                       StencilOp.Keep,          //if depth buffer fails, Set zero
                                       StencilOp.Replace);      //if passed, replace stencil value with ref value
            GL.StencilMask(stencilactivebits);
            GL.Enable(EnableCap.StencilTest);
        }

        /// <summary>
        /// Set up the stencil operation without enable. Full control
        /// </summary>
        /// <param name="function">Stencil function</param>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        static public void OnlyIf(StencilFunction function, int refvalue = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V op stencil[pixel]
            GL.StencilFuncSeparate(face, function, refvalue, mask);
            GL.StencilOpSeparate(face, StencilOp.Keep,          //if it fails, keep stencil value
                                       StencilOp.Keep,          //if depth buffer fails, keep stencil value
                                       StencilOp.Keep);         //if passed, keep stencil value
        }

        /// <summary>
        /// Quick setup for greater function
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        static public void OnlyIfGreater(int refvalue = 2, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V > stencil[pixel]
            OnlyIf(StencilFunction.Greater, refvalue, mask, face);
        }

        /// <summary>
        /// Quick setup for less function
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        static public void OnlyIfLess(int refvalue = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V < stencil[pixel]
            OnlyIf(StencilFunction.Less, refvalue, mask, face);
        }

        /// <summary>
        /// Quick setup for equal function
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        static public void OnlyIfEqual(int refvalue = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            // stencil passes if V == stencil[pixel]
            OnlyIf(StencilFunction.Equal, refvalue, mask, face);
        }

        /// <summary> Disable stencil testing </summary>
        static public void Off()
        {
            GL.Disable(EnableCap.StencilTest);
        }

        /// <summary> Clear the stencil </summary>
        static public void ClearStencilBuffer()       // nicer name
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
        }
    }
}
