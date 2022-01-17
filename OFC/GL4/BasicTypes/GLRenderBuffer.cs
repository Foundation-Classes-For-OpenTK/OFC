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

namespace GLOFC.GL4
{
    ///<summary> A render buffer can be used as a target for GLFrameBuffer instead of a texture</summary>

    [System.Diagnostics.DebuggerDisplay("Id {Id} {Width} {Height} {Samples}")]
    public class GLRenderBuffer : IDisposable
    {
        /// <summary>GL ID</summary>
        public int Id { get; private set; } = -1;

        /// <summary> Width of buffer in pixels</summary>
        public int Width { get; protected set; } = 0;           // Set when colour texture attaches
        /// <summary> Height of buffer in pixels </summary>
        public int Height { get; protected set; } = 1;
        /// <summary> Sample depth for multisample buffers</summary>
        public int Samples { get; protected set; } = 0;

        /// <summary> Construct a buffer </summary>
        public GLRenderBuffer() 
        {
            Id = GL.GenRenderbuffer();
            GLStatics.RegisterAllocation(typeof(GLRenderBuffer));
        }

        /// <summary> Allocate a non multisampled buffer of width and height, and bind to target Renderbuffer </summary>
        public void Allocate(RenderbufferStorage storage, int width, int height)
        {
            Width = width;
            Height = height;
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Id);
            GL.NamedRenderbufferStorage(Id, storage, Width, Height);
            GLStatics.Check();
        }

        /// <summary> Allocate a multisample buffer of width, height, and samples depth, and bind to target Renderbuffer </summary>
        public void AllocateMultisample(RenderbufferStorage storage, int width, int height, int samples)
        {
            Width = width;
            Height = height;
            Samples = samples;
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Id);
            GL.NamedRenderbufferStorageMultisample(Id,Samples, storage, Width, Height);
            GLStatics.Check();
        }

        /// <summary> Unbind the buffer from the render buffer target </summary>
        public static void UnBind()
        {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        /// <summary> From the any type of ImageTarget into this</summary>
        public void CopyFrom(int srcid, ImageTarget srctype, int srcmiplevel, int sx, int sy, int sz,       int dx, int dy, int width, int height)
        {
            GL.CopyImageSubData(srcid, srctype, srcmiplevel, sx, sy, sz, Id, ImageTarget.Renderbuffer, 0, dx, dy, 0, width, height, 1);
            GLStatics.Check();
        }

        /// <summary> Dispose of the buffer </summary>
        public void Dispose()           // you can double dispose.
        {
            if (Id != -1)
            {
                GL.DeleteRenderbuffer(Id);
                GLStatics.RegisterDeallocation(typeof(GLRenderBuffer));
                Id = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }
    }
}