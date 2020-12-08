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

namespace OFC.GL4
{
    // Textures are not autoloaded into shaders, you normally should do this by overriding the StartAction of the sampler and call a bind function

    public abstract class GLTextureBase : IGLTexture            // load a texture into open gl
    {
        protected GLTextureBase()
        {
        }

        public int Id { get; protected set; } = -1;
        public long ArbId { get { if (arbid == -1) GetARBID(); return arbid; } }        // ARB is only acquired after getting it the first time

        public int Width { get; protected set; } = 0;           // W/H is always the width/height of the first bitmap in z=0.
        public int Height { get; protected set; } = 1;
        public int Depth { get; protected set; } = 1;           // Depth is no of bitmaps down for 2darray/3d
        public int MipMapLevels { get; protected set; } = 1;    // Mip maps levels of texture
        public bool MipMapAutoGenNeeded { get; set; } = false;  // set if you load a bitmap with mipmaps < MipMapLevels, you manually clear

        public SizedInternalFormat InternalFormat { get; protected set; }       // internal format of stored data in texture unit

        public bool KeepBitmapList { get; set; } = false;       // we keep bitmap records if any are owned, or this is set
        public Bitmap[] BitMaps { get; private set; }           // if keeping a list, bitmap, even if not owned
        public bool[] OwnBitMaps { get; private set; }          // if bitmap is owned

        // normal sampler bind - for sampler2D access etc.

        public void Bind(int bindingpoint)
        {
            GL.BindTextureUnit(bindingpoint, Id);
            OFC.GLStatics.Check();
        }

        // image sampler bindings - you need to specify what access you want.  
        // Also the layout(binding = N, rgba32f) readonly - make the readonly match the access below, and make the rgba32f match the internal format (see page 186 of SuperBible)
        public void BindImage(int bindingpoint, int mipmaplevel = 0, bool allarraylayersavailable = true, int layer = 0, TextureAccess tx = TextureAccess.ReadWrite)
        {
            GL.BindImageTexture(bindingpoint, Id, mipmaplevel, allarraylayersavailable, layer, tx, InternalFormat);
            OFC.GLStatics.Check();
        }

        // this one is special, because you are allowed to use a different sized internal format to texture created type as long as they are in the same class (see page 185 of SuperBible)
        public void BindImage(int bindingpoint, SizedInternalFormat sioverride, int mipmaplevel = 0, bool allarraylayersavailable = true, int layer = 0, TextureAccess tx = TextureAccess.ReadWrite)
        {
            GL.BindImageTexture(bindingpoint, Id, mipmaplevel, allarraylayersavailable, layer, tx, sioverride);
            OFC.GLStatics.Check();
        }

        // and a quick default one
        public void BindImage(int bindingpoint)
        {
            GL.BindImageTexture(bindingpoint, Id, 0, true, 0, TextureAccess.ReadWrite, InternalFormat);
            OFC.GLStatics.Check();
        }

        private long arbid = -1;

        private void GetARBID()     // if you want bindless textures, use ArbId to get the arb handle
        {
            arbid = OpenTK.Graphics.OpenGL.GL.Arb.GetTextureHandle(Id);
            OpenTK.Graphics.OpenGL.GL.Arb.MakeTextureHandleResident(arbid);     // can't do this twice!
        }

        public void Dispose()           // you can double dispose.
        {
            if (Id != -1)
            {
                if (arbid != -1)        // if its been arb'd, de-arb it
                {
                    OpenTK.Graphics.OpenGL.GL.Arb.MakeTextureHandleNonResident(arbid);     // can't do this twice!
                    arbid = -1;
                }

                GL.DeleteTexture(Id);
                Id = -1;

                if (BitMaps != null)
                {
                    for( int  i = 0; i < BitMaps.Length; i++ )
                    {
                        if ( OwnBitMaps[i] && BitMaps[i] != null)     // we may have empty spaces in the bitmap list
                            BitMaps[i].Dispose();
                    }

                    BitMaps = null;
                    OwnBitMaps = null;
                }

            }
        }

        // must have called CreateTexture before, allows bitmaps to be loaded individually
        // load bitmap into texture, allow for mipmapping (mipmaplevels = 1 no mip in current bitmap)
        // bitmap textures go into x/y plane (as per normal graphics).
        // see the derived classes for the actual load function - this is a helper
        // this can load into 2d texture, 2d arrays and 3d textures.
        // if zoffset = -1, load into 2d texture (can't use texture sub image 3d) otherwise its the 2d array or 3d texture at zoffset

        public void LoadBitmap(Bitmap bmp, int zoffset, bool ownbmp = false, int bmpmipmaplevels = 1)
        {
            System.Diagnostics.Debug.Assert(bmpmipmaplevels <= MipMapLevels);

            System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);     // 32 bit words, ARGB format

            MipMapAutoGenNeeded |= bmpmipmaplevels < MipMapLevels;       // ORed in, set flag if you've supplied a bitmap less than mipmaplevels

            IntPtr ptr = bmpdata.Scan0;     // its a byte ptr

            int curwidth = bmp.Width;
            int masterheight = MipMapHeight(bmp, bmpmipmaplevels);

            System.Diagnostics.Debug.Assert(bmp.Width == Width);
            System.Diagnostics.Debug.Assert(masterheight<=Height);  // bitmap may be shorter

            int curheight = masterheight;

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, bmp.Width);      // indicate the image width, if we take less, then GL will skip pixels to get to next row

            for (int m = 0; m < bmpmipmaplevels; m++)
            {
                if (zoffset == -1)
                {
                    GL.TextureSubImage2D(Id,
                        m,                  // this is level m
                        0,                  // x offset inside the target texture..
                        0,                  // y offset..
                        curwidth,           // width to load in the target texture
                        curheight,          // height..
                        PixelFormat.Bgra,       // format of the data we are feeding to it (not the format internally stored)
                        PixelType.UnsignedByte,     // and we asked above for Bgra data as unsigned bytes
                        ptr);
                }
                else
                {
                    GL.TextureSubImage3D(Id,
                        m,      // mipmaplevel
                        0,      // xoff into target
                        0,      // yoff into target
                        zoffset,  // zoffset, which is the bitmap depth
                        curwidth,       // size of image
                        curheight,
                        1,      // depth of the texture, which is 1 pixel
                        PixelFormat.Bgra,   // format of the data we are feeding to it (not the format internally stored)
                        PixelType.UnsignedByte, // unsigned bytes in BGRA.  PixelStore above indicated the stride across 1 row
                        ptr);
                }

                if (m == 0)             // at 0, we jump down the whole first image.  4 is the bytes/pixel
                    ptr += bmp.Width * masterheight * 4;
                else
                    ptr += curwidth * 4;    // else we move across by curwidth.

                if (curwidth > 1)           // scale down size by 2
                    curwidth /= 2;
                if (curheight > 1)
                    curheight /= 2;
            }

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);      // back to off for safety
            bmp.UnlockBits(bmpdata);
            OFC.GLStatics.Check();

            if (ownbmp || KeepBitmapList)           // if we own the bmp, or we want to keep records..
            {
                if (BitMaps == null)
                {
                    BitMaps = new Bitmap[Depth];
                    OwnBitMaps = new bool[Depth];
                }

                if (zoffset == -1)      // -1 means use 2d load, so its really the first 0 zorder
                    zoffset = 0;

                BitMaps[zoffset] = bmp;
                OwnBitMaps[zoffset] = ownbmp;
            }
        }

        // Return texture as a set of floats or byte only (others not supported as of yet)
        // use inverty to correct for any inversion if your getting the data from a framebuffer texture - it appears to be inverted when written

        public T[] GetTextureImageAs<T>(PixelFormat pxformatback = PixelFormat.Rgba, int level = 0, bool inverty = false) 
        {
            int elementsperpixel = GL4Statics.ElementsPerPixel(pxformatback);

            int totalelements = Width * Height * elementsperpixel;

            int elementsizeT = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

            int elementstride = elementsperpixel * Width;

            int bufsize = totalelements * elementsizeT;

            IntPtr unmanagedPointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(bufsize); // get an unmanaged buffer
            GL.GetTextureImage(Id, level, pxformatback, (elementsizeT == 4) ? PixelType.Float : PixelType.UnsignedByte, bufsize, unmanagedPointer);  // fill
            OFC.GLStatics.Check();

            if (elementsizeT == 4)
            {
                float[] data = new float[totalelements];

                if (inverty)
                {
                    IntPtr p = unmanagedPointer;
                    for (int y = 0; y < Height; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(p, data, (Height - 1 - y) * elementstride, elementstride);      // transfer buffer to floats
                        p += elementstride;
                    }
                }
                else
                {
                    System.Runtime.InteropServices.Marshal.Copy(unmanagedPointer, data, 0, totalelements);      // transfer buffer to floats
                }

                System.Runtime.InteropServices.Marshal.FreeHGlobal(unmanagedPointer);
                return data as T[];
            }
            else
            {
                byte[] data = new byte[totalelements];

                if ( inverty )
                {
                    IntPtr p = unmanagedPointer;
                    for (int y = 0; y < Height; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(p, data, (Height - 1 - y) * elementstride, elementstride);      // transfer buffer to floats
                        p += elementstride;
                    }
                }
                else
                {
                    System.Runtime.InteropServices.Marshal.Copy(unmanagedPointer, data, 0, totalelements);      // transfer buffer to floats
                }

                System.Runtime.InteropServices.Marshal.FreeHGlobal(unmanagedPointer);
                return data as T[];
            }
        }

        public Bitmap GetBitmap(int level = 0, bool inverty = false)
        {
            byte[] texdatab = GetTextureImageAs<byte>(OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, level, inverty);
            return BitMapHelpers.CreateBitmapFromARGBBytes(Width, Height, texdatab);
        }

        public void SetSamplerMode(TextureWrapMode s, TextureWrapMode t, TextureWrapMode p)
        {
            int st = (int)s;
            int tt = (int)t;
            int pt = (int)p;
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapS, ref st);
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapT, ref tt);
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapT, ref pt);

        }

        public void SetSamplerMode(TextureWrapMode s, TextureWrapMode t)
        {
            int st = (int)s;
            int tt = (int)t;
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapS, ref st);
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapT, ref tt);
        }

        public void SetSamplerMode(TextureWrapMode s)
        {
            int st = (int)s;
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapS, ref st);
        }

        public void SetMinMagFilter()
        {
            var textureMinFilter = (int)All.LinearMipmapLinear;
            GL.TextureParameterI(Id, TextureParameterName.TextureMinFilter, ref textureMinFilter);
            var textureMagFilter = (int)All.Linear;
            GL.TextureParameterI(Id, TextureParameterName.TextureMagFilter, ref textureMagFilter);
        }

        public void SetMinMagLinear()
        {
            var textureFilter = (int)All.Linear;
            GL.TextureParameterI(Id, TextureParameterName.TextureMinFilter, ref textureFilter);
            GL.TextureParameterI(Id, TextureParameterName.TextureMagFilter, ref textureFilter);
        }

        public void GenMipMapTextures()     // only call if mipmaplevels > 1 after you have loaded all z planes. Called automatically for 2d+2darrays
        {
            GL.GenerateTextureMipmap(Id);
        }

        static public int MipMapHeight(Bitmap map, int bitmapmipmaplevels)
        {
            return (bitmapmipmaplevels == 1) ? map.Height : (map.Height / 3) * 2;        // if bitmap is mipped mapped, work out correct height.
        }


    }
}

