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
    /// Begin condition render on objects in list
    /// </summary>
    public class GLOperationBeginConditionalRender : GLOperationsBase
    {
        private int id;
        private ConditionalRenderType mode;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">Specifies the name of an occlusion query object whose results are used to determine if the rendering commands are discarde</param>
        /// <param name="mode">Specifies how this interprets the results of the occlusion query. </param>
        public GLOperationBeginConditionalRender(int id, ConditionalRenderType mode)
        {
            this.id = id;
            this.mode = mode;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GL.BeginConditionalRender(id, mode);
        }
    }

    /// <summary>
    /// End the condition render, pair with start the conditional render operation.
    /// </summary>
    public class GLOperationEndConditionalRender : GLOperationsBase
    {
        /// <summary> Constructor </summary>
        public GLOperationEndConditionalRender()
        {
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GL.EndConditionalRender();
        }
    }

}
