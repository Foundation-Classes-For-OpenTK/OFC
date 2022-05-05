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
using GLOFC.GL4.Textures;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    ///<summary> Frame Buffer object, for drawing into </summary>

    [System.Diagnostics.DebuggerDisplay("Id {Id} {Width} {Height} {ColorTarget}")]
    public class GLFrameBuffer : IDisposable
    {
        ///<summary>  </summary>
        public int Id { get; private set; } = -1;

        ///<summary> Width in pixels. Set when colour texture attaches </summary>
        public int Width { get; protected set; } = 0;
        ///<summary> Height in pixels. Set when colour texture attaches </summary>
        public int Height { get; protected set; } = 1;
        ///<summary> Color Target number </summary>
        public int ColorTarget { get; protected set; } = 0;

        ///<summary> Make a new frame buffer and get ID  </summary>
        public GLFrameBuffer()
        {
            GL.CreateFramebuffers(1, out int id);
            GLStatics.RegisterAllocation(typeof(GLFrameBuffer));
            Id = id;
        }

        ///<summary> Attach a 2D texture to frame buffer on colourtarget and mipmaplevel </summary>
        public void AttachColor(GLTexture2D tex, int colourtarget = 0, int mipmaplevel = 0)
        {
            ColorTarget = colourtarget;
            Width = tex.Width;
            Height = tex.Height;
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, tex.Id, mipmaplevel);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary> Attach a 2D Array texture to frame buffer on colourtarget and mipmaplevel </summary>
        public void AttachColor(GLTexture2DArray tex, int colourtarget = 0, int mipmaplevel = 0)    // not tested.. page 397
        {
            ColorTarget = colourtarget;
            Width = tex.Width;
            Height = tex.Height;
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, tex.Id, mipmaplevel);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary> Attach a 2D texture to frame buffer on colourtarget and mipmaplevel, to a specific layer </summary>
        public void AttachColorLayered(GLTexture2DArray tex, int colourtarget = 0, int mipmaplevel = 0, int layer = 0)    // not tested.. page 401
        {
            ColorTarget = colourtarget;
            Width = tex.Width;
            Height = tex.Height;
            GL.NamedFramebufferTextureLayer(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, tex.Id, mipmaplevel, layer);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary> Attach a 2D texture for the depth buffer, on mipmaplevel </summary>
        public void AttachDepth(GLTexture2D tex, int mipmaplevel = 0)
        {
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.DepthAttachment, tex.Id, mipmaplevel);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary> Attach a 2D texture for the stencil buffer, on mipmaplevel </summary>
        public void AttachStensil(GLTexture2D tex, int mipmaplevel = 0)
        {
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.StencilAttachment, tex.Id, mipmaplevel);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary> Attach a 2D texture for the stencil and depth buffer, on mipmaplevel </summary>
        public void AttachDepthStensil(GLTexture2D tex, int mipmaplevel = 0)
        {
            GL.NamedFramebufferTexture(Id, FramebufferAttachment.DepthStencilAttachment, tex.Id, mipmaplevel);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary>  Attach a render buffer on colourtarget </summary>
        public void AttachColor(GLRenderBuffer r, int colourtarget)
        {
            ColorTarget = colourtarget;
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.ColorAttachment0 + ColorTarget, RenderbufferTarget.Renderbuffer, r.Id);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary>  Attach a render buffer for the depth buffer</summary>
        public void AttachDepth(GLRenderBuffer r)
        {
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, r.Id);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary>  Attach a render buffer for the stencil buffer </summary>
        public void AttachStensil(GLRenderBuffer r)
        {
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, r.Id);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary>  Attach a render buffer for the depth and stencil buffer</summary>
        public void AttachDepthStensil(GLRenderBuffer r)
        {
            GL.NamedFramebufferRenderbuffer(Id, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, r.Id);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }


        ///<summary>Bind a framebuffer for reading to the ReadFrameBuffer target </summary>
        public void BindRead()
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, Id);
        }

        ///<summary> Unbind the read frame buffer </summary>
        public static void UnbindRead()
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        ///<summary> Changes GL target for rendering to this frame buffer, sets viewport, clears to colourback </summary>
        public void BindColor(OpenTK.Graphics.Color4 colourback)
        {
            GL.NamedFramebufferDrawBuffer(Id, DrawBufferMode.ColorAttachment0 + ColorTarget);   // attach the FB to draw buffer target 
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, Id);      // bind the FB to the system
            GL.Viewport(new System.Drawing.Rectangle(0, 0, Width, Height)); // set the viewport
            GL.ClearColor(colourback);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);  // clear the FB
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary> Unbind this framebuffer for rendering </summary>
        public static void UnBind()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }
        ///<summary> Get Framebuffer completion status </summary>
        public FramebufferStatus GetStatus(FramebufferTarget tb = FramebufferTarget.DrawFramebuffer)     // page 405 get status
        {
            return GL.CheckNamedFramebufferStatus(Id, tb);

        }


        ///<summary> Blit from read FB into this, x0/y0/x1/y1 are source area, dx0/dy0/dx1/dy1 are destination area. 
        ///Mask indicates what buffer contents are to be copied: GL_COLOR_BUFFER_BIT, GL_DEPTH_BUFFER_BIT and GL_STENCIL_BUFFER_BIT. 
        ///Filt is the interpolation filter, GL_NEAREST or GL_LINEAR
        ///</summary>
        public void Blit(GLFrameBuffer read, ReadBufferMode src, int x0, int y0, int x1, int y1, int dx0, int dy0, int dx1, int dy1, ClearBufferMask mask, BlitFramebufferFilter filt)
        {
            GL.NamedFramebufferReadBuffer(read.Id, src);
            GL.BlitNamedFramebuffer(read.Id, Id, x0, y0, x1, y1, dx0, dy0, dx1, dy1, mask, filt);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        // 
        ///<summary> Read from bound Read Frame buffer target in this pixel format and type. Bufsize indicates amount of space needed for byte array </summary>
        public void ReadPixels(ReadBufferMode src, int x0, int y0, int x1, int y1, PixelFormat format, PixelType type, int bufsize)
        {
            GL.ReadBuffer(src);
            byte[] array = new byte[bufsize];
            GL.ReadnPixels(x0, y0, x1, y1, format, type, array.Length, array);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        ///<summary> Dispose of the frame buffer </summary>
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