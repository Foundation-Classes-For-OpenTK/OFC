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
    // 2d arrays do not interpolate between z pixels, unlike 3d textures

    public class GLTexture2DArray : GLTextureBase          // load a 2D set of textures into open gl
    {
        public int MultiSample { get; set; } = 0;           // if non zero, multisample texture

        public GLTexture2DArray()
        {
        }

        // bitmap 0 gives the common width/height of the image.
        public GLTexture2DArray(Bitmap[] bmps, int mipmaplevel = 1, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f, int genmipmaplevel = 1, bool ownbitmaps = false, Size? bmpsize = null)
        {
            CreateLoadBitmaps(bmps, mipmaplevel, internalformat, genmipmaplevel, ownbitmaps, bmpsize);
        }

        public GLTexture2DArray(int width, int height, int depth, int mipmaplevels = 1, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f)
        {
            CreateTexture(width, height, depth, mipmaplevels, internalformat);
        }

        // You can call as many times to create textures. Only creates one if required
        // mipmaplevels does not apply if multisample > 0 

        public void CreateTexture(int width, int height, int depth, int mipmaplevels = 1, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f,
                                    int multisample = 0, bool fixedmultisampleloc = false)
        {
            if (Id == -1 || Width != width || Height != height || Depth != depth || mipmaplevels != MipMapLevels || MultiSample != multisample )
            {
                Dispose();

                InternalFormat = internalformat;
                Width = width;
                Height = height;
                Depth = depth;
                MipMapLevels = mipmaplevels;
                MultiSample = multisample;

                GL.CreateTextures(MultiSample > 0 ? TextureTarget.Texture2DMultisampleArray : TextureTarget.Texture2DArray, 1, out int id);
                Id = id;

                if (MultiSample > 0)
                {
                    GL.TextureStorage3DMultisample(
                                Id,
                                MultiSample,
                                InternalFormat,                 // format of texture - 4 floats is the normal, and is given in the constructor
                                Width,                          // width and height of mipmap level 0
                                Height,
                                Depth,                          // depth = number of bitmaps depth
                                fixedmultisampleloc);
                }
                else
                {
                    GL.TextureStorage3D(Id,
                        mipmaplevels,        // miplevels.  Either given in the bitmap itself, or generated automatically
                        InternalFormat,         // format of texture - 4 floats is normal, given in constructor
                        Width,
                        Height,
                        Depth);       // depth = number of bitmaps depth
                }

                SetMinMagFilter();

                GLOFC.GLStatics.Check();
            }
        }

        // You can reload the bitmap, it will create a new Texture if required. 
        // Bitmaps array can be sparse will null entries if you don't want to use that level. 
        // texture size is either bmpsize or Level 0 size (which therefore must be there)

        public void CreateLoadBitmaps(Bitmap[] bmps, int bitmapmipmaplevels = 1, SizedInternalFormat internalformat = SizedInternalFormat.Rgba32f, int genmipmaplevel = 1, 
                                               bool ownbitmaps = false, Size? bmpsize = null)
        {
            int width = bmpsize.HasValue ? bmpsize.Value.Width : bmps[0].Width;
            int height = bmpsize.HasValue ? bmpsize.Value.Height : MipMapHeight(bmps[0], bitmapmipmaplevels);        // if bitmap is mipped mapped, work out correct height.
            int texmipmaps = Math.Max(bitmapmipmaplevels, genmipmaplevel);

            CreateTexture(width, height, bmps.Length, texmipmaps, internalformat);

            for (int zorder = 0; zorder < bmps.Length; zorder++)      // for all bitmaps, we load the texture into zoffset of 2darray
            {
                if ( bmps[zorder] != null )       // it can be sparse
                    LoadBitmap(bmps[zorder], zorder, ownbitmaps, bitmapmipmaplevels);   // load into bitmapnumber zoffset level
            }

            if (bitmapmipmaplevels == 1 && genmipmaplevel > 1)     // single level mipmaps with genmipmap levels > 1 get auto gen
                GL.GenerateTextureMipmap(Id);

            GLOFC.GLStatics.Check();
        }

        // from the bound read framebuffer (from sx/sy) into this texture at x/y image z
        public void CopyFromReadFramebuffer(int miplevel, int x, int y, int z, int sx, int sy, int width, int height)
        {
            GL.CopyTextureSubImage3D(Id, miplevel, x, y, z, sx, sy, width, height);
            GLStatics.Check();
        }

        // from the any type of ImageTarget into this
        public void CopyFrom(int srcid, ImageTarget srctype, int srcmiplevel, int sx, int sy, int sz, int dlevel, int dx, int dy, int width, int height)
        {
            GL.CopyImageSubData(srcid, srctype, srcmiplevel, sx, sy, sz, Id, ImageTarget.Texture2DArray, dlevel, dx, dy, 0, width, height, 1);
            GLStatics.Check();
        }


    }
}
