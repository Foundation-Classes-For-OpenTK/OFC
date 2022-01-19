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

namespace GLOFC.GL4.Operations
{
    /// <summary>
    /// Quick stencil setup 
    /// </summary>
    public class GLOperationSetStencil : GLOperationsBase
    {
        /// <summary>
        /// Setup and enable stencil
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilFuncSeparate.xhtml</href>
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilOpSeparate.xhtml</href>
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilMask.xhtml</href>
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        /// <param name="stencilactivebits">Which bits are allowed to change when writing V to stencil buffer</param>
        public GLOperationSetStencil(int refvalue = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack, int stencilactivebits = 0xff)
        {
            this.v = refvalue;
            this.mask = mask;
            this.face = face;
            this.stencilactivebits = stencilactivebits;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.SetStencil(v, mask, face, stencilactivebits);
        }

        private int v;
        private int mask;
        private StencilFace face;
        private int stencilactivebits;
    }

    /// <summary>
    /// Use after drawing, to set up next draw with stencil conditions, full control
    /// </summary>

    public class GLOperationStencilOnlyIf : GLOperationsBase
    {
        /// <summary>
        /// Set up the stencil condition, full control.
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilFuncSeparate.xhtml</href>
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilOpSeparate.xhtml</href>
        /// </summary>
        /// <param name="function">Stencil function to use</param>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        public GLOperationStencilOnlyIf(StencilFunction function, int refvalue = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.f = function;
            this.v = refvalue;
            this.mask = mask;
            this.face = face;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIf(f, v, mask, face);
        }

        private StencilFunction f;
        private int v;
        private int mask;
        private StencilFace face;
    }

    /// <summary>
    /// Use after drawing, to set up next draw with stencil condition greater
    /// </summary>

    public class GLOperationStencilOnlyIfGreater : GLOperationsBase
    {
        /// <summary>
        /// Use after drawing, setup for greater function
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilFuncSeparate.xhtml</href>
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilOpSeparate.xhtml</href>
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        public GLOperationStencilOnlyIfGreater(int refvalue = 2, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.v = refvalue;
            this.mask = mask;
            this.face = face;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIfGreater(v, mask, face);
        }

        private int v;
        private int mask;
        private StencilFace face;
    }

    /// <summary>
    /// Use after drawing, to set up next draw with stencil condition less
    /// </summary>
    public class GLOperationStencilOnlyIfLess : GLOperationsBase
    {
        /// <summary>
        /// Use after drawing, setup for less function 
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilFuncSeparate.xhtml</href>
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilOpSeparate.xhtml</href>
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        public GLOperationStencilOnlyIfLess(int refvalue = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.v = refvalue;
            this.mask = mask;
            this.face = face;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIfLess(v, mask, face);
        }

        private int v;
        private int mask;
        private StencilFace face;
    }

    /// <summary>
    /// Use after drawing, to set up next draw with stencil condition equal
    /// </summary>
    public class GLOperationStencilOnlyIfEqual : GLOperationsBase
    {
        /// <summary>
        /// Use after drawing, setup for equal function
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilFuncSeparate.xhtml</href>
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glStencilOpSeparate.xhtml</href>
        /// </summary>
        /// <param name="refvalue">Reference value</param>
        /// <param name="mask">Stencil test mask (ref AND mask) func (stencil AND mask)</param>
        /// <param name="face">For this face (default front and back) perform stencil operation on</param>
        public GLOperationStencilOnlyIfEqual(int refvalue = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.v = refvalue;
            this.mask = mask;
            this.face = face;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIfEqual(v, mask, face);
        }

        private int v;
        private int mask;
        private StencilFace face;

    }

    /// <summary>
    /// Use to turn stencilling off
    /// </summary>

    public class GLOperationStencilOff : GLOperationsBase
    {
        /// <summary> Constructor </summary>
        public GLOperationStencilOff()
        {
        }
        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.Off();
        }
    }

    /// <summary>
    /// Use to clear stencil
    /// </summary>
    public class GLOperationStencilClear : GLOperationsBase
    {
        /// <summary> Constructor </summary>
        public GLOperationStencilClear()
        {
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.ClearStencilBuffer();
        }
    }
}

