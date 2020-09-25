/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
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
    // local data block, writable in Vectors etc, supports std140 and std430
    // you can use Allocate then Fill direct
    // or you can use the GL Mapping function which maps the buffer into memory

    [System.Diagnostics.DebuggerDisplay("Id {Id} Length {Length}")]
    public class GLFrameBuffer : IDisposable
    {
        public int Id { get; private set; } = -1;

        public int Width { get; protected set; } = 0;           // Set when colour texture attaches
        public int Height { get; protected set; } = 1;
        public int ColorTarget { get; protected set; } = 0;

        public GLFrameBuffer() 
        {
            GL.CreateFramebuffers(1, out int id);
            Id = id;
        }

        // attach textures to framebuffer

        public void AttachColor(GLTexture2D tex, int colourtarget = 0, int mipmaplevel = 0)
        {
            ColorTarget = colourtarget;
            Width = tex.Width;
            Height = tex.Height;
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, tex.Id, mipmaplevel);
            GLStatics.Check();
        }

        public void AttachColor(GLTexture2DArray tex, int colourtarget = 0, int mipmaplevel = 0)    // not tested.. page 397
        {
            ColorTarget = colourtarget;
            Width = tex.Width;
            Height = tex.Height;
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, tex.Id, mipmaplevel);
            GLStatics.Check();
        }

        public void AttachDepth(GLTexture2D tex, int mipmaplevel = 0)
        {
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.DepthAttachment, tex.Id, mipmaplevel);
            GLStatics.Check();
        }

        public void AttachStensil(GLTexture2D tex, int mipmaplevel = 0)
        {
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.StencilAttachment, tex.Id, mipmaplevel);
            GLStatics.Check();
        }

        public void AttachDepthStensil(GLTexture2D tex, int mipmaplevel = 0)
        {
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.DepthStencilAttachment, tex.Id, mipmaplevel);
            GLStatics.Check();
        }

        public void BindRead()
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, Id);
        }

        public static void UnbindRead()
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        // Changes GL target for rendering to this FB, sets viewport, clears to colourback
        public void BindColor(OpenTK.Graphics.Color4 colourback)
        {
            GL.NamedFramebufferDrawBuffer(Id, DrawBufferMode.ColorAttachment0 + ColorTarget);   // attach the FB to draw buffer target 
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, Id);      // bind the FB to the system
            GL.Viewport(new System.Drawing.Rectangle(0, 0, Width, Height)); // set the viewport
            GL.ClearColor(colourback);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);  // clear the FB
            GLStatics.Check();
        }

        public FramebufferStatus GetStatus()     // page 405 get status
        {
            return GL.CheckNamedFramebufferStatus(Id, FramebufferTarget.DrawFramebuffer);

        }

        public static void UnBind()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }

        public void Dispose()           // you can double dispose.
        {
            if (Id != -1)
            {
                GL.DeleteFramebuffer(Id);
                Id = -1;
            }
        }
    }
}