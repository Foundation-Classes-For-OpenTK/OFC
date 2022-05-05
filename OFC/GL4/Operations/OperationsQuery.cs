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

namespace GLOFC.GL4.Operations
{
    /// <summary>
    /// Query operation to begin a query
    /// </summary>

    [System.Diagnostics.DebuggerDisplay("Query {Target} {Index}")]
    public class GLOperationQuery : GLOperationsBase, IDisposable       
    {
        /// <summary> Query target, see constructor </summary>
        public QueryTarget Target { get; set; }
        /// <summary> Query index</summary>
        public int Index { get; set; }            // BeginQueryIndexied(target,0,id) == BeginQuery(target,id) GlSpec46. page 46
        /// <summary>Buffer to hold query results. Allocated by caller</summary>
        public GLBuffer QueryBuffer { get; set; }

        /// <summary>
        /// Constructor, set up the query. 
        /// See <href>"https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glBeginQueryIndexed.xhtml"</href>
        /// </summary>
        /// <param name="target">Target may be one of GL_SAMPLES_PASSED, GL_ANY_SAMPLES_PASSED, GL_ANY_SAMPLES_PASSED_CONSERVATIVE, GL_TIME_ELAPSED, GL_TIMESTAMP, GL_PRIMITIVES_GENERATED or GL_TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN. (use openTK IDs)</param>
        /// <param name="index">index of query, normally 0</param>
        /// <param name="createnow">Create query now, or wait until Execute</param>
        /// <param name="querybuffer">Optional, for GLOperationEndQuery, a buffer to store the query data into. Caller must allocate buffer</param>
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

            GLStatics.RegisterAllocation(typeof(GLOperationQuery));

            System.Diagnostics.Debug.Assert(id != 0);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
            this.Id = id;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            if (QueryBuffer != null)
                QueryBuffer.BindQuery();

            GL.BeginQueryIndexed(Target, Index, Id);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> Dispose of the query </summary>

        public override void Dispose()               // when dispose, delete query
        {
            if (Id != -1)
            {
                GL.DeleteQuery(Id);
                GLStatics.RegisterDeallocation(typeof(GLOperationQuery));
                Id = -1;
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }

        /// <summary>
        /// Get the query name. Only valid between begin and end..
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetQueryIndexed.xhtml</href>
        /// </summary>
        /// <param name="target">Target may be one of GL_SAMPLES_PASSED, GL_ANY_SAMPLES_PASSED, GL_ANY_SAMPLES_PASSED_CONSERVATIVE, GL_TIME_ELAPSED, GL_TIMESTAMP, GL_PRIMITIVES_GENERATED or GL_TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN. (use OpenTK names)</param>
        /// <param name="index">Index of query, normally 0</param>
        /// <param name="param">Normall CurrentQuery. Specifies the symbolic name of a query object target parameter. Accepted values are GL_CURRENT_QUERY or GL_QUERY_COUNTER_BITS. (Use OpenTK names)</param>
        /// <returns>Returns query data, nominally query name</returns>
        static public int GetQueryName(QueryTarget target, int index, GetQueryParam param = GetQueryParam.CurrentQuery) 
        {
            GL.GetQueryIndexed(target, index, param, out int res);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
            return res;
        }

        /// <summary> Is this ID a query? </summary>
        static public bool IsQuery(int id)
        {
            return GL.IsQuery(id);
        }
    }

    /// <summary>
    /// End a query operation
    /// </summary>

    [System.Diagnostics.DebuggerDisplay("End Query {Query.Target} {Query.Index}")]
    public class GLOperationEndQuery : GLOperationsBase
    {
        /// <summary> GLOperationQuery instance </summary>
        public GLOperationQuery Query { get; private set; }
        /// <summary> Called on query complete. FinishAction is also called, but this provides the correct class for immediate use.</summary>
        public Action<GLOperationEndQuery> QueryComplete { get; set; }

        /// <summary>
        /// Constructor, set up End Query instance
        /// </summary>
        /// <param name="query">Give the query instance created by GLOperationQuery</param>
        /// <param name="querycomplete">Action on complete</param>
        public GLOperationEndQuery(GLOperationQuery query, Action<GLOperationEndQuery> querycomplete = null)
        {
            this.Id = query.Id; // just for consistency
            this.Query = query;
            this.QueryComplete = querycomplete;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GL.EndQueryIndexed(Query.Target,Query.Index);
            QueryComplete?.Invoke(this);
        }

        /// <summary> Is result available? </summary>
        public bool IsAvailable()
        {
            GL.GetQueryObject(Id, GetQueryObjectParam.QueryResultAvailable, out int p);
            return p != 0;
        }

        /// <summary> Get the query data as an integer.  See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetQueryObject.xhtml</href></summary>
        /// <param name="pname">Query parameter, nominally QueryResult. Also GL_QUERY_RESULT_NO_WAIT or GL_QUERY_RESULT_AVAILABLE. (use OpenTK Names)</param>
        /// <returns>Integer result</returns>
        public int GetQuery(GetQueryObjectParam pname = GetQueryObjectParam.QueryResult)
        {
            GL.GetQueryObject(Query.Id, pname, out int res);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
            return res;
        }

        /// <summary> Get the query data as an long. See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetQueryObject.xhtml</href></summary>
        /// <param name="pname">Query parameter, nominally QueryResult. Also GL_QUERY_RESULT_NO_WAIT or GL_QUERY_RESULT_AVAILABLE. (use OpenTK Names)</param>
        /// <returns>Long result</returns>
        public long GetQueryl(GetQueryObjectParam pname = GetQueryObjectParam.QueryResult)    
        {
            GL.GetQueryObject(Query.Id, pname, out long res);
            return res;
        }

        /// <summary> Get the query data as an int[]. See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetQueryObject.xhtml</href></summary>
        /// <param name="length">Buffer length of query result</param>
        /// <param name="pname">Query parameter, nominally QueryResult. Also GL_QUERY_RESULT_NO_WAIT or GL_QUERY_RESULT_AVAILABLE. (use OpenTK Names)</param>
        /// <returns>int[] result</returns>
        public int[] GetQuerya(int length, GetQueryObjectParam pname = GetQueryObjectParam.QueryResult)    
        {
            int[] array = new int[length];
            GL.GetQueryObject(Query.Id, pname, array);
            return array;
        }

        /// <summary> Get the query data as an long[]. See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetQueryObject.xhtml</href></summary>
        /// <param name="length">Buffer length of query result</param>
        /// <param name="pname">Query parameter, nominally QueryResult. Also GL_QUERY_RESULT_NO_WAIT or GL_QUERY_RESULT_AVAILABLE. (use OpenTK Names)</param>
        /// <returns>long[] result</returns>
        public long[] GetQueryal(int length, GetQueryObjectParam pname = GetQueryObjectParam.QueryResult)  
        {
            long[] array = new long[length];
            GL.GetQueryObject(Query.Id, pname, array);
            return array;
        }

        /// <summary>
        /// Update the buffer given in GLOperationQuery. 
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glGetQueryObject.xhtml</href>
        /// </summary>
        /// <param name="offset">Offset into buffer to write the data to</param>
        /// <param name="pname">Query parameter, nominally QueryResult. Also GL_QUERY_RESULT_NO_WAIT or GL_QUERY_RESULT_AVAILABLE. (use OpenTK Names)</param>
        public void UpdateBuffer(int offset, QueryObjectParameterName pname  = QueryObjectParameterName.QueryResult)     // store in buffer at offset the result
        {
            GL.GetQueryBufferObject(Query.Id, Query.QueryBuffer.Id, pname, (IntPtr)offset);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> Begin a conditional section conditioned on the result of this query 
        /// Use GLOperationEndConditionalRender to finish the condition section.</summary>
        public void BeginConditional(ConditionalRenderType mode)        
        {
            GL.BeginConditionalRender(Query.Id, mode);
        }
    }

    /// <summary>
    /// Operation which unbinds the query buffer. Use if your writing the results of a query to a query buffer.
    /// must be called to clear the query binding after all conditional work has been done.
    /// </summary>
    public class GLOperationEndQueryBuffer : GLOperationsBase
    {
        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLBuffer.UnbindQuery();
        }
    }

    /// <summary>
    /// Query for returning timestamp from openGL
    /// </summary>
    public class GLOperationQueryTimeStamp : GLOperationsBase, IDisposable
    {
        /// <summary> Called on query execution. FinishAction is also called, but this provides the correct class for immediate use </summary>
        public Action<GLOperationQueryTimeStamp> QueryComplete { get; set; }     

        /// <summary> Constructor, create a time query</summary>
        public GLOperationQueryTimeStamp()
        {
            this.Id = GL.GenQuery();
            System.Diagnostics.Debug.Assert(Id != 0);
            GLStatics.RegisterAllocation(typeof(GLOperationQueryTimeStamp));
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GL.QueryCounter(Id, QueryCounterTarget.Timestamp);
            QueryComplete?.Invoke(this);
        }

        /// <summary> Dispose of the query timestamp </summary>
        public override void Dispose()               // when dispose, delete query
        {
            if (Id != -1)
            {
                GL.DeleteQuery(Id);
                Id = -1;
                GLStatics.RegisterDeallocation(typeof(GLOperationQueryTimeStamp));
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }

        /// <summary> Is the timestamp query ready? </summary>
        public bool IsAvailable()
        {
            GL.GetQueryObject(Id, GetQueryObjectParam.QueryResultAvailable, out int p);
            return p != 0;
        }

        /// <summary> Get the timestamp in nanoseconds </summary>
        public long GetCounter(GetQueryObjectParam p = GetQueryObjectParam.QueryResult)
        {
            GL.GetQueryObject(Id, p, out long res);
            return res;
        }
    }

}

