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

namespace GLOFC.GL4.Operations
{
    /// <summary>
    /// Clear depth buffer operation
    /// </summary>
    public class GLOperationClearDepthBuffer : GLOperationsBase
    {
        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStatics.ClearDepthBuffer();
        }
    }
    public class GLOperationClearStencilBuffer : GLOperationsBase
    {
        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStatics.ClearStencilBuffer();
        }
    }
    public class GLOperationClearBuffer : GLOperationsBase
    {
        private ClearBufferMask mask;

        /// <summary> Constructor </summary>
        /// <param name="mask">Which buffers to clear</param>
        public GLOperationClearBuffer(ClearBufferMask mask)
        {
            this.mask = mask;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLStatics.ClearBuffer(mask);
        }
    }

    /// <summary>
    /// Null operation, useful just to allow StartAction to be called at this point
    /// </summary>
    public class GLOperationNull: GLOperationsBase
    {
        /// <summary> Constructor </summary>
        public GLOperationNull() : base()
        {
        }
        /// <summary> Constructor taking the start action </summary>
        public GLOperationNull(Action<IGLProgramShader, GLMatrixCalc> sa) : base()
        {
            StartAction = sa;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)        
        {
        }
    }

}

