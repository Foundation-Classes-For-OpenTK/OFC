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

namespace GLOFC.GL4
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
            GLStatics.RegisterAllocation(typeof(GLFrameBuffer));
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

        public void AttachColorLayered(GLTexture2DArray tex, int colourtarget = 0, int mipmaplevel = 0, int layer = 0)    // not tested.. page 401
        {
            ColorTarget = colourtarget;
            Width = tex.Width;
            Height = tex.Height;
            GL.NamedFramebufferTextureLayer(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, tex.Id, mipmaplevel, layer);
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

        public void AttachColor(GLRenderBuffer r, int colourtarget)
        {
            ColorTarget = colourtarget;
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, RenderbufferTarget.Renderbuffer, r.Id);
            GLStatics.Check();
        }

        public void AttachDepth(GLRenderBuffer r, int mipmaplevel = 0)
        {
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, r.Id);
            GLStatics.Check();
        }

        public void AttachStensil(GLRenderBuffer r, int mipmaplevel = 0)
        {
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, r.Id);
            GLStatics.Check();
        }

        public void AttachDepthStensil(GLRenderBuffer r, int mipmaplevel = 0)
        {
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, r.Id);
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

        public FramebufferStatus GetStatus(FramebufferTarget tb = FramebufferTarget.DrawFramebuffer)     // page 405 get status
        {
            return GL.CheckNamedFramebufferStatus(Id, tb);

        }

        public static void UnBind()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }

        // Blit from read FB into this
        public void Blit(GLFrameBuffer read, ReadBufferMode src, int x0, int y0, int x1, int y1, int dx0, int dy0, int dx1, int dy1, ClearBufferMask mask, BlitFramebufferFilter filt)
        {
            GL.NamedFramebufferReadBuffer(read.Id, src);
            GL.BlitNamedFramebuffer(read.Id, Id, x0, y0, x1, y1, dx0, dy0, dx1, dy1, mask, filt);
            GLStatics.Check();
        }

        // Read from bound Read Frame buffer target
        public void ReadPixels(ReadBufferMode src, int x0, int y0, int x1, int y1, PixelFormat format, PixelType type, int bufsize)
        {
            GL.ReadBuffer(src);
            byte[] array = new byte[bufsize];
            GL.ReadnPixels(x0, y0, x1, y1, format, type, array.Length, array);
            GLStatics.Check();
        }

        public void Dispose()           // you can double dispose.
        {
            if (Id != -1)
            {
                GL.DeleteFramebuffer(Id);
                GLStatics.RegisterDeallocation(typeof(GLFrameBuffer));
                Id = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }
    }
}