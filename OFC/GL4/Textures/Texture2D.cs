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
        public GLTexture2D()
        {
        }

        public GLTexture2D(Bitmap bmp, int bitmipmaplevel = 1, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f, 
                            int genmipmaplevel = 1, bool ownbitmaps = false)
        {
            CreateLoadBitmap(bmp, bitmipmaplevel, internalformat, genmipmaplevel, ownbitmaps);
        }

        // You can call as many times to create textures. Only creates one if required

        public void CreateOrUpdateTexture(int width, int height, int mipmaplevels = 1, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f)
        {
            if (Id == -1 || Width != width || Height != height || mipmaplevels != MipMapLevels)    // if not there, or changed, we can't just replace it, size is fixed. Delete it
            {
                Dispose();

                InternalFormat = internalformat;
                Width = width;
                Height = height;
                MipMapLevels = mipmaplevels;

                GL.CreateTextures(TextureTarget.Texture2D, 1, out int id);
                Id = id;

                GL.TextureStorage2D(
                                Id,
                                mipmaplevels,                    // levels of mipmapping
                                InternalFormat,                 // format of texture - 4 floats is the normal, and is given in the constructor
                                Width,                          // width and height of mipmap level 0
                                Height);

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

        public void CreateLoadBitmap(Bitmap bmp, int bitmipmaplevels = 1, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f, 
                                     int genmipmaplevel = 1, bool ownbitmap = false)
        {
            int h = MipMapHeight(bmp, bitmipmaplevels);
            int texmipmaps = Math.Max(bitmipmaplevels, genmipmaplevel);

            CreateOrUpdateTexture(bmp.Width, h, texmipmaps, internalformat);

            LoadBitmap(bmp, -1, ownbitmap , bitmipmaplevels);    // use common load into bitmap, indicating its a 2D texture so use texturesubimage2d

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

        // from the any type of ImageTarget into this
        public void CopyFrom(int srcid, ImageTarget srctype, int srcmiplevel, int sx, int sy, int sz, int dlevel, int dx, int dy, int width, int height)
        {
            GL.CopyImageSubData(srcid, srctype, srcmiplevel, sx, sy, sz, Id, ImageTarget.Texture2D, dlevel, dx, dy, 0, width, height, 1);
            GLStatics.Check();
        }
    }
}

