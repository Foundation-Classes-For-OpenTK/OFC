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


using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OFC.GL4
{
    public class GLRIClearDepthBuffer : GLRenderableItemNull
    {
        public GLRIClearDepthBuffer()
        {
            StartAction += (c,s, mc) =>
            {
                GLStatics.ClearDepthBuffer();
                //System.Diagnostics.Debug.WriteLine("Clear Depth Buffer");
            };
        }
    }

    public class GLRIBeginConditionalRender : GLRenderableItemNull
    {
        public GLRIBeginConditionalRender(int id, ConditionalRenderType mode)
        {
            StartAction += (c,s, mc) =>
            {
                GL.BeginConditionalRender(id, mode);
            };
        }
    }

    public class GLRIEndConditionalRender : GLRenderableItemNull
    {
        public GLRIEndConditionalRender()
        {
            StartAction += (c,s, mc) =>
            {
                GL.EndConditionalRender();
            };
        }
    }

    public class GLRIBeginQuery : GLRenderableItemNull
    {
        public GLRIBeginQuery(QueryTarget target, int id)
        {
            StartAction += (c,s, mc) =>
            {
                GL.BeginQuery(target, id);
            };
        }
    }

    public class GLRIBeginQueryIndexed : GLRenderableItemNull
    {
        public GLRIBeginQueryIndexed(QueryTarget target, int index, int id)
        {
            StartAction += (c,s, mc) =>
            {
                GL.BeginQueryIndexed(target, index, id);
            };
        }
    }

    public class GLRIEndQuery : GLRenderableItemNull
    {
        public GLRIEndQuery(QueryTarget target)
        {
            StartAction += (c,s, mc) =>
            {
                GL.EndQuery(target);
            };
        }
    }

    public class GLRIEndQueryIndexed : GLRenderableItemNull
    {
        public GLRIEndQueryIndexed(QueryTarget target, int index)
        {
            StartAction += (c,s, mc) =>
            {
                GL.EndQueryIndexed(target, index);
            };
        }
    }
}

