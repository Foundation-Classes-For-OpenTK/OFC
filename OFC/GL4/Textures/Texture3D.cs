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
    public class GLTexture3D : GLTextureBase         // load a texture into open gl
    {
        public GLTexture3D(int width, int height, int depth, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f, int mipmaplevels = 1)
        {
            CreateOrUpdateTexture(width, height, depth, internalformat, mipmaplevels);
        }

        public void CreateOrUpdateTexture(int width, int height, int depth, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f, int mipmaplevels = 1)
        {
            if (Id == -1 || Width != width || Height != height || Depth != depth || mipmaplevels != MipMapLevels)    // if not there, or changed, we can't just replace it, size is fixed. Delete it
            {
                Dispose();

                InternalFormat = internalformat;
                Width = width; Height = height; Depth = depth;
                MipMapLevels = mipmaplevels;

                GL.CreateTextures(TextureTarget.Texture3D, 1, out int id);
                Id = id;

                GL.TextureStorage3D(Id, mipmaplevels, InternalFormat, Width, Height, Depth);

                SetMinMagFilter();
            }
        }

        // Write to a Z plane the X/Y info.

        // you can use PixelFormat = Red just to store a single float, and then use texture(tex,vec3(x,y,z)) to pick it up - only .x is applicable
        // if you have an rgba, and you store to a single plane using PixelFormat, the other planes are wiped! beware.

        public void StoreZPlane(int zcoord, int xoffset, int yoffset, int width, int height, PixelFormat px, PixelType ty, IntPtr ptr)
        {
            GL.TextureSubImage3D(Id, 0, xoffset, yoffset, zcoord, width, height, 1, px, ty, ptr);
        }

        public void StoreZPlane(int zcoord, int xoffset, int yoffset, int width, int height, Byte[] array, PixelFormat px = PixelFormat.Bgra)    
        {
            GL.TextureSubImage3D(Id, 0, xoffset, yoffset, zcoord, width, height, 1, px, PixelType.UnsignedByte, array);
        }

        public void StoreZPlane(int zcoord, int xoffset, int yoffset, int width, int height, float[] array, PixelFormat px = PixelFormat.Bgra)      
        {
            GL.TextureSubImage3D(Id, 0, xoffset, yoffset, zcoord, width, height, 1, px, PixelType.Float, array);
        }

        // from the bound read framebuffer (from sx/sy) into this texture at x/y image z
        public void CopyFromReadFramebuffer(int miplevel, int x, int y, int z, int sx, int sy, int width, int height)
        {
            GL.CopyTextureSubImage3D(Id, miplevel, x, y, z, sx, sy, width, height);
            GLStatics.Check();
        }

        // from the any type of ImageTarget into this
        public void CopyFrom(int srcid, ImageTarget srctype, int srcmiplevel, int sx, int sy, int sz, int dmiplevel, int dx, int dy, int dz, int width, int height)
        {
            GL.CopyImageSubData(srcid, srctype, srcmiplevel, sx, sy, sz,
                                    Id, ImageTarget.Texture2DArray, dmiplevel, dx, dy, dz, width, height, 1);
            GLStatics.Check();
        }

    }

}

