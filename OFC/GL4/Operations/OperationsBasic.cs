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
using OpenTK.Graphics.OpenGL;

namespace GLOFC.GL4
{
    public class GLOperationClearDepthBuffer : GLOperationsBase
    {
        public override void Execute(GLMatrixCalc c)
        {
            GLStatics.ClearDepthBuffer();
        }
    }
    public class GLOperationClearStencilBuffer : GLOperationsBase
    {
        public override void Execute(GLMatrixCalc c)
        {
            GLStatics.ClearStencilBuffer();
        }
    }
    public class GLOperationClearBuffer : GLOperationsBase
    {
        private ClearBufferMask mask;
        public GLOperationClearBuffer(ClearBufferMask mask)
        {
            this.mask = mask;
        }
        
        public override void Execute(GLMatrixCalc c)
        {
            GLStatics.ClearBuffer(mask);
        }
    }

    // for this one,set StartAction to action you want to execute
    public class GLOperationAction: GLOperationsBase
    {
        public GLOperationAction() : base()
        {
        }
        public GLOperationAction(Action<IGLProgramShader, GLMatrixCalc> sa) : base()
        {
            StartAction = sa;
        }
        public override void Execute(GLMatrixCalc c)        
        {
            StartAction?.Invoke(this, c);
        }
    }

}

