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


using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;

namespace GLOFC.GL4
{
    public class GLTexture2D : GLTextureBase          // load a texture into open gl
    {
        public int MultiSample { get; set; } = 0;           // if non zero, multisample texture

        public GLTexture2D()
        {
        }

        public GLTexture2D(Bitmap bmp, SizedInternalFormat internalformat, int bitmipmaplevel = 1,
                            int genmipmaplevel = 1, bool ownbitmaps = false, ContentAlignment alignment = ContentAlignment.TopLeft)
        {
            CreateLoadBitmap(bmp, internalformat, bitmipmaplevel, genmipmaplevel, ownbitmaps, alignment);
        }

        // You can call as many times to create textures. Only creates one if required
        // mipmaplevels does not apply if multisample > 0 
        // Rgba8 is the normal one to pick

        public void CreateOrUpdateTexture(int width, int height, SizedInternalFormat internalformat, int mipmaplevels = 1,
                                                            int multisample = 0, bool fixedmultisampleloc = false)
        {
            // if not there, or changed, we can't just replace it, size is fixed. Delete it

            if (Id == -1 || Width != width || Height != height || mipmaplevels != MipMapLevels || multisample != MultiSample)
            {
                Dispose();

                InternalFormat = internalformat;
                Width = width;
                Height = height;
                MipMapLevels = mipmaplevels;
                MultiSample = multisample;

                GL.CreateTextures(MultiSample > 0 ? TextureTarget.Texture2DMultisample : TextureTarget.Texture2D, 1, out int id);
                GLStatics.Check();
                Id = id;

                if (MultiSample > 0)
                {
                    GL.TextureStorage2DMultisample(
                                Id,
                                MultiSample,
                                InternalFormat,                 // format of texture - 4 floats is the normal, and is given in the constructor
                                Width,                          // width and height of mipmap level 0
                                Height,
                                fixedmultisampleloc);

                }
                else
                {
                    GL.TextureStorage2D(
                                Id,
                                mipmaplevels,                    // levels of mipmapping
                                InternalFormat,                 // format of texture - 4 floats is the normal, and is given in the constructor
                                Width,                          // width and height of mipmap level 0
                                Height);
                }

                SetMinMagFilter();

                GLOFC.GLStatics.Check();
            }
        }

        public void CreateOrUpdateTexturePixelFormat(int width, int height, PixelInternalFormat pi, PixelFormat pf, PixelType pt)   // make with a pixel format..
        {
            if (Id == -1 || Width != width || Height != height)    // if not there, or changed, we can't just replace it, size is fixed. Delete it
            {
                if (Id != -1)
                {
                    Dispose();
                }

                InternalFormat = 0;         // PixelInternalFormat does not fit within this, so zero it
                Width = width;
                Height = height;
                MipMapLevels = 1;

                GL.CreateTextures(TextureTarget.Texture2D, 1, out int id);
                GLStatics.Check();
                Id = id;

                GL.BindTexture(TextureTarget.Texture2D, Id);

                GL.TexImage2D(TextureTarget.Texture2D, 0, pi, width, height, 0, pf, pt, (IntPtr)0);     // we don't actually load data in, so its a null ptr.

                GLOFC.GLStatics.Check();
            }
        }

        public void CreateDepthBuffer(int width, int height)
        {
            CreateOrUpdateTexturePixelFormat(width, height, PixelInternalFormat.DepthComponent32f, PixelFormat.DepthComponent, OpenTK.Graphics.OpenGL4.PixelType.Float);
        }

        public void CreateDepthStencilBuffer(int width, int height)
        {
            CreateOrUpdateTexturePixelFormat(width, height, PixelInternalFormat.Depth32fStencil8, PixelFormat.DepthComponent, OpenTK.Graphics.OpenGL4.PixelType.Float);
        }

        // You can reload the bitmap, it will create a new Texture if required

        public void CreateLoadBitmap(Bitmap bmp, SizedInternalFormat internalformat, int bitmipmaplevels = 1,
                                                int genmipmaplevel = 1, bool ownbitmap = false, ContentAlignment alignment = ContentAlignment.TopLeft)
        {
            int h = MipMapHeight(bmp, bitmipmaplevels);
            int texmipmaps = Math.Max(bitmipmaplevels, genmipmaplevel);

            CreateOrUpdateTexture(bmp.Width, h, internalformat, texmipmaps);

            LoadBitmap(bmp, -1, ownbitmap , bitmipmaplevels, alignment);    // use common load into bitmap, indicating its a 2D texture so use texturesubimage2d

            if (bitmipmaplevels == 1 && genmipmaplevel > 1)     // single level mipmaps with genmipmap levels > 1 get auto gen
                GL.GenerateTextureMipmap(Id);
            
            GLOFC.GLStatics.Check();

           // float[] tex = GetTextureImageAsFloats(end:100);
        }

        // from the bound read framebuffer (from sx/sy) into this texture at x/y
        public void CopyFromReadFramebuffer(int miplevel, int x, int y, int sx, int sy, int width, int height)
        {
            GL.CopyTextureSubImage2D(Id, miplevel, x, y, sx, sy, width, height);
            GLStatics.Check();
        }

        // from any type of ImageTarget into this
        public void CopyFrom(int srcid, ImageTarget srctype, int srcmiplevel, int sx, int sy, int sz, int dmiplevel, int dx, int dy, int width, int height)
        {
            GL.CopyImageSubData(srcid, srctype, srcmiplevel, sx, sy, sz, Id, ImageTarget.Texture2D, dmiplevel, dx, dy, 0, width, height, 1);
            GLStatics.Check();
        }

        // from RenderBuffer
        public void CopyFrom(GLRenderBuffer rb, int sx, int sy, int dmiplevel, int dx, int dy, int width, int height)
        {
            GL.CopyImageSubData(rb.Id, ImageTarget.Renderbuffer, 0, sx, sy, 0, 
                                    Id, ImageTarget.Texture2D, dmiplevel, dx, dy, 0, width, height, 1);
            GLStatics.Check();
        }
    }
}

