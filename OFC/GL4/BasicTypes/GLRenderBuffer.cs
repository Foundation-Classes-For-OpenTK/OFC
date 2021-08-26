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
    // local data block, writable in Vectors etc, supports std140 and std430
    // you can use Allocate then Fill direct
    // or you can use the GL Mapping function which maps the buffer into memory

    [System.Diagnostics.DebuggerDisplay("Id {Id} Length {Length}")]
    public class GLRenderBuffer : IDisposable
    {
        public int Id { get; private set; } = -1;

        public int Width { get; protected set; } = 0;           // Set when colour texture attaches
        public int Height { get; protected set; } = 1;
        public int Samples { get; protected set; } = 0;

        public GLRenderBuffer() 
        {
            Id = GL.GenRenderbuffer();
        }

        public void Allocate(RenderbufferStorage storage, int width, int height)
        {
            Width = width;
            Height = height;
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Id);
            GL.NamedRenderbufferStorage(Id, storage, Width, Height);
            GLStatics.Check();
        }

        public void AllocateMultisample(RenderbufferStorage storage, int width, int height, int samples)
        {
            Width = width;
            Height = height;
            Samples = Samples;
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Id);
            GL.NamedRenderbufferStorageMultisample(Id,Samples, storage, Width, Height);
            GLStatics.Check();
        }

        public static void UnBind()
        {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        public void Dispose()           // you can double dispose.
        {
            if (Id != -1)
            {
                GL.DeleteRenderbuffer(Id);
                Id = -1;
            }
        }
    }
}