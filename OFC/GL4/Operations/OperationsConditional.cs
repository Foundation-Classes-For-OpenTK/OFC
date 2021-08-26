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

namespace GLOFC.GL4
{
    public class GLOperationBeginConditionalRender : GLOperationsBase
    {
        int id;
        ConditionalRenderType mode;

        public GLOperationBeginConditionalRender(int id, ConditionalRenderType mode)
        {
            this.id = id;
            this.mode = mode;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GL.BeginConditionalRender(id, mode);
        }
    }

    public class GLOperationEndConditionalRender : GLOperationsBase
    {
        public GLOperationEndConditionalRender()
        {
        }

        public override void Execute(GLMatrixCalc c)
        {
            GL.EndConditionalRender();
        }
    }

  
  }

