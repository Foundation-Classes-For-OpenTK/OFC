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
using OpenTK.Graphics.OpenGL4;

namespace OFC.GL4
{
    // attach query to render list or shader list, create first and use to get results

    [System.Diagnostics.DebuggerDisplay("Query {Target} {Index}")]
    public class GLOperationQuery : GLOperationsBase, IDisposable       
    {
        public QueryTarget Target { get; set; }
        public int Index { get; set; }            // BeginQueryIndexied(target,0,id) == BeginQuery(target,id) GlSpec46. page 46
        public GLBuffer QueryBuffer { get; set; }

        public Action<GLOperationQuery> QueryStart { get; set; }      

        public GLOperationQuery(QueryTarget target, int index = 0, bool createnow = false, GLBuffer querybuffer = null)
        {
            this.Target = target;
            this.Index = index;
            this.QueryBuffer = querybuffer;

            int id = 0;
            if (createnow)
            {
                GL.CreateQueries(Target, 1, out id);
            }
            else
            {
                GL.GenQueries(1, out id);
            }

            System.Diagnostics.Debug.Assert(id != 0);
            GLStatics.Check();
            this.Id = id;
        }

        public override void Execute(GLMatrixCalc c)
        {
            if (QueryBuffer != null)
                QueryBuffer.BindQuery();

            GL.BeginQueryIndexed(Target, Index, Id);
            QueryStart?.Invoke(this);
            GLStatics.Check();
        }

        public override void Dispose()               // when dispose, delete query
        {
            GL.DeleteQuery(Id);
            GLStatics.Check();
        }

        static public int GetQueryName(QueryTarget t, int index, GetQueryParam p = GetQueryParam.CurrentQuery) // only valid between begin and end..
        {
            GL.GetQueryIndexed(t, index, p, out int res);
            GLStatics.Check();
            return res;
        }

        static public bool IsQuery(int id)
        {
            return GL.IsQuery(id);
        }
    }

    [System.Diagnostics.DebuggerDisplay("End Query {Query.Target} {Query.Index}")]
    public class GLOperationEndQuery : GLOperationsBase
    {
        public GLOperationQuery Query { get; private set; }
        public Action<GLOperationEndQuery> QueryComplete { get; set; }       // called on EndQuery

        public GLOperationEndQuery(GLOperationQuery query, Action<GLOperationEndQuery> querycomplete = null)
        {
            this.Id = query.Id; // just for consistency
            this.Query = query;
            this.QueryComplete = querycomplete;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GL.EndQueryIndexed(Query.Target,Query.Index);
            QueryComplete?.Invoke(this);
        }

        public bool IsAvailable()
        {
            GL.GetQueryObject(Id, GetQueryObjectParam.QueryResultAvailable, out int p);
            return p != 0;
        }

        public int GetQuery(GetQueryObjectParam p = GetQueryObjectParam.QueryResult)
        {
            GL.GetQueryObject(Query.Id, p, out int res);
            GLStatics.Check();
            return res;
        }

        public long GetQueryl(GetQueryObjectParam p = GetQueryObjectParam.QueryResult)    
        {
            GL.GetQueryObject(Query.Id, p, out long res);
            return res;
        }

        public int[] GetQuerya(int length, GetQueryObjectParam p = GetQueryObjectParam.QueryResult)    
        {
            int[] array = new int[length];
            GL.GetQueryObject(Query.Id, p, array);
            return array;
        }

        public long[] GetQueryal(int length, GetQueryObjectParam p = GetQueryObjectParam.QueryResult)  
        {
            long[] array = new long[length];
            GL.GetQueryObject(Query.Id, p, array);
            return array;
        }

        public void UpdateBuffer(int offset, QueryObjectParameterName p  = QueryObjectParameterName.QueryResult)     // store in buffer at offset the result
        {
            GL.GetQueryBufferObject(Query.Id, Query.QueryBuffer.Id, p, (IntPtr)offset);
            GLStatics.Check();
        }

        public void BeginConditional(ConditionalRenderType mode)        // on this data, conditionally render
        {
            GL.BeginConditionalRender(Query.Id, mode);
        }

    }

    public class GLOperationEndConditional : GLOperationsBase
    {
        public override void Execute(GLMatrixCalc c)
        {
            GL.EndConditionalRender();
        }
        public static void EndConditional()
        {
            GL.EndConditionalRender();
        }
    }

    public class GLOperationEndQueryBuffer : GLOperationsBase
    {
        public override void Execute(GLMatrixCalc c)
        {
            GLBuffer.UnbindQuery();
        }
    }

    public class GLOperationQueryTimeStamp : GLOperationsBase, IDisposable
    {
        public Action<GLOperationQueryTimeStamp> QueryComplete { get; set; }       // called on EndQuery

        public GLOperationQueryTimeStamp()
        {
            this.Id = GL.GenQuery();
            System.Diagnostics.Debug.Assert(Id != 0);
            GLStatics.Check();
        }

        public override void Execute(GLMatrixCalc c)
        {
            GL.QueryCounter(Id, QueryCounterTarget.Timestamp);
            QueryComplete?.Invoke(this);
        }

        public override void Dispose()               // when dispose, delete query
        {
            GL.DeleteQuery(Id);
            GLStatics.Check();
        }

        public bool IsAvailable()
        {
            GL.GetQueryObject(Id, GetQueryObjectParam.QueryResultAvailable, out int p);
            return p != 0;
        }

        public long GetCounter(GetQueryObjectParam p = GetQueryObjectParam.QueryResult)
        {
            GL.GetQueryObject(Id, p, out long res);
            return res;
        }
    }

}

