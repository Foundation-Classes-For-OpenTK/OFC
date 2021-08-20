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
    // these build on the null shader is used to insert operations in the RenderList pipeline, so it can do some operations
    // outside of the GLRenderControl at the appropriate point
    // you can of course use Shader.StartAction/FinishAction instead, but these may be more useful

    public class GLShaderClearDepthBuffer : GLShaderNull
    {
        public GLShaderClearDepthBuffer()
        {
            StartAction += (s, mc) =>
            {
                GLStatics.ClearDepthBuffer();
                //System.Diagnostics.Debug.WriteLine("Clear Depth Buffer");
            };
        }
    }

    public class GLShaderBeginConditionalRender : GLShaderNull
    {
        public GLShaderBeginConditionalRender(int id, ConditionalRenderType mode)
        {
            StartAction += (s, mc) =>
            {
                GL.BeginConditionalRender(id, mode);
            };
        }
    }

    public class GLShaderEndConditionalRender : GLShaderNull
    {
        public GLShaderEndConditionalRender()
        {
            StartAction += (s, mc) =>
            {
                GL.EndConditionalRender();
            };
        }
    }

    public class GLShaderBeginQuery : GLShaderNull
    {
        public GLShaderBeginQuery(QueryTarget target, int id)
        {
            StartAction += (s, mc) =>
            {
                GL.BeginQuery(target, id);
            };
        }
    }

    public class GLShaderBeginQueryIndexed : GLShaderNull
    {
        public GLShaderBeginQueryIndexed(QueryTarget target, int index, int id)
        {
            StartAction += (s, mc) =>
            {
                GL.BeginQueryIndexed(target, index, id);
            };
        }
    }

    public class GLShaderEndQuery : GLShaderNull
    {
        public GLShaderEndQuery(QueryTarget target)
        {
            StartAction += (s, mc) =>
            {
                GL.EndQuery(target);
            };
        }
    }

    public class GLShaderEndQueryIndexed : GLShaderNull
    {
        public GLShaderEndQueryIndexed(QueryTarget target, int index)
        {
            StartAction += (s, mc) =>
            {
                GL.EndQueryIndexed(target, index);
            };
        }
    }
}
