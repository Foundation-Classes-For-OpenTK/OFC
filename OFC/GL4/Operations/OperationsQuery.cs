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
    public class GLOperationBeginQuery : GLOperationsBase
    {
        QueryTarget target;
        int id;
        public GLOperationBeginQuery(QueryTarget target, int id)
        {
            this.target = target;
            this.id = id;
        }
        public override void DoOperation(GLMatrixCalc c)
        {
            GL.BeginQuery(target, id);
        }
    }

    public class GLOperationBeginQueryIndexed : GLOperationsBase
    {
        QueryTarget target;
        int id,index;
        public GLOperationBeginQueryIndexed(QueryTarget target, int index, int id)
        {
            this.target = target;
            this.index = index;
            this.id = id;
        }

        public override void DoOperation(GLMatrixCalc c)
        {
            GL.BeginQueryIndexed(target, index, id);
        }
    }

    public class GLOperationEndQuery : GLOperationsBase
    {
        QueryTarget target;

        public GLOperationEndQuery(QueryTarget target)
        {
            this.target = target;
        }
        public override void DoOperation(GLMatrixCalc c)
        {
            GL.EndQuery(target);
        }
    }

    public class GLOperationEndQueryIndexed : GLOperationsBase
    {
        QueryTarget target;
        int index;
        public GLOperationEndQueryIndexed(QueryTarget target, int index)
        {
            this.target = target;
            this.index = index;
        }
        public override void DoOperation(GLMatrixCalc c)
        {
            GL.EndQueryIndexed(target, index);
        }
    }

  }

