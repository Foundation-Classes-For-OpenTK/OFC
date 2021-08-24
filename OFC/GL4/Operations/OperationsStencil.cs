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

namespace OFC.GL4
{
    public class GLOperationSetStencil : GLOperationsBase
    {
        int v;
        int mask;
        StencilFace face;
        int stencilactivebits;

        public GLOperationSetStencil(int v = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack, int stencilactivebits = 0xff)
        {
            this.v = v;
            this.mask = mask;
            this.face = face;
            this.stencilactivebits = stencilactivebits;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.SetStencil(v, mask, face, stencilactivebits);
        }
    }

    public class GLOperationStencilOnlyIf : GLOperationsBase
    {
        StencilFunction f;
        int v;
        int mask;
        StencilFace face;

        public GLOperationStencilOnlyIf(StencilFunction f, int v = 1, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.f = f;
            this.v = v;
            this.mask = mask;
            this.face = face;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIf(f, v, mask, face);
        }
    }

    public class GLOperationStencilOnlyIfGreater : GLOperationsBase
    {
        int v;
        int mask;
        StencilFace face;

        public GLOperationStencilOnlyIfGreater(int v = 2, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.v = v;
            this.mask = mask;
            this.face = face;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIfGreater(v, mask, face);
        }
    }
    public class GLOperationStencilOnlyIfLess : GLOperationsBase
    {
        int v;
        int mask;
        StencilFace face;

        public GLOperationStencilOnlyIfLess(int v = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.v = v;
            this.mask = mask;
            this.face = face;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIfLess(v, mask, face);
        }
    }
    public class GLOperationStencilOnlyIfEqual : GLOperationsBase
    {
        int v;
        int mask;
        StencilFace face;

        public GLOperationStencilOnlyIfEqual(int v = 0, int mask = 0xff, StencilFace face = StencilFace.FrontAndBack)
        {
            this.v = v;
            this.mask = mask;
            this.face = face;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.OnlyIfEqual(v, mask, face);
        }
    }

    public class GLOperationStencilOff : GLOperationsBase
    {
        public GLOperationStencilOff()
        {
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.Off();
        }
    }
    public class GLOperationStencilClear : GLOperationsBase
    {
        public GLOperationStencilClear()
        {
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLStencil.ClearStencilBuffer();
        }
    }

}

