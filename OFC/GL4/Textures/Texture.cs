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
using System.Runtime.InteropServices;

namespace GLOFC.GL4.Textures
{
    /// <summary>
    /// Base class for all texture types. Abstract class so cannot be instanced itself.
    /// </summary>

    public abstract class GLTextureBase : IGLTexture            // load a texture into open gl
    {
        /// <summary> Constructor </summary>
        protected GLTextureBase()
        {
            context = GLStatics.GetContext();
        }

        /// <summary> GL ID </summary>
        public int Id { get; protected set; } = -1;
        /// <summary> ARB is only acquired after getting it the first time. Use AquireARB. -1 if not acquired </summary>
        public long ArbNumber { get { return arbid; } }
        /// <summary> Width of texture. Width and Height is always the width/height of the first bitmap in z=0. </summary>
        public int Width { get; protected set; } = 0;
        /// <summary> Height of texture </summary>
        public int Height { get; protected set; } = 1;
        /// <summary> Size </summary>
        public Size Size { get { return new Size(Width, Height); } }
        /// <summary> Depth of texture. Depth is no of bitmaps down for 2darray/3d. </summary>
        public int Depth { get; protected set; } = 1;
        /// <summary> Mip map level </summary>
        public int MipMapLevels { get; protected set; } = 1;
        /// <summary> Requires autogen of mip map.  Set if you load a bitmap with mipmaps less than MipMapLevels, you manually clear</summary>
        public bool MipMapAutoGenNeeded { get; set; } = false;  // 
        /// <summary> Internal format of texture</summary>
        public SizedInternalFormat InternalFormat { get; protected set; }
        /// <summary> We keep bitmap records if any are owned, or this is set</summary>
        public bool KeepBitmapList { get; set; } = false;
        /// <summary> If keeping a list, bitmap, even if not owned</summary>
        public Bitmap[] BitMaps { get; private set; }
        /// <summary> If bitmap is owned</summary>
        public bool[] OwnBitMaps { get; private set; }
        /// <summary> Can be used to record what z depth we have got to on bitmap filling </summary>
        public int DepthIndex { get; set; } = 0;
        /// <summary> How many are left from DepthIndex onwards</summary>
        public int DepthLeftIndex { get { return Depth - DepthIndex; } }

        // normal sampler bind - for sampler2D access etc.

        private IntPtr context;     // double check bind is the same as create

        /// <summary>Bind texture to this binding point</summary>
        public void Bind(int bindingpoint)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");
            GL.BindTextureUnit(bindingpoint, Id);
            GLStatics.Check();
        }

        /// <summary> Bind a single image level to specific bindingpoint for the purpose of reading and writing it from shaders
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glBindImageTexture.xhtml</href>
        /// </summary>
        /// <param name="bindingpoint">Binding point to bind to</param>
        /// <param name="level">Level of the texture to bind (z depth in 2dArray/3d bitmaps)</param>
        /// <param name="layered">Specifies whether a layered texture binding is to be established</param>
        /// <param name="layer">If layered is fALSE, specifies the layer of texture to be bound to the image unit. Ignored otherwise.</param>
        /// <param name="tx">Texture access</param>
        /// <param name="sioverride">If set, overrides the internal format</param>
        public void BindImage(int bindingpoint, int level = 0, bool layered = true, int layer = 0, TextureAccess tx = TextureAccess.ReadWrite, SizedInternalFormat? sioverride = null)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");
            GL.BindImageTexture(bindingpoint, Id, level, layered, layer, tx, sioverride != null ? sioverride.Value : InternalFormat);
            GLStatics.Check();
        }

        /// <summary>Bind Image to bindingpoint using level 0</summary>
        public void BindImage(int bindingpoint)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");
            GL.BindImageTexture(bindingpoint, Id, 0, true, 0, TextureAccess.ReadWrite, InternalFormat);
            GLStatics.Check();
        }

        private long arbid = -1;

        /// <summary>Call to ensure this texture has an ARB texture ID</summary>
        public long AcquireArbId()
        {
            if (arbid == -1)
            {
                arbid = OpenTK.Graphics.OpenGL.GL.Arb.GetTextureHandle(Id);
                OpenTK.Graphics.OpenGL.GL.Arb.MakeTextureHandleResident(arbid);     // can't do this twice!
            }
            return arbid;
        }

        /// <summary>Dispose of texture, will free bitmaps if owned</summary>
        public void Dispose()           // you can double dispose.
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");
            if (Id >= 0)
            {
                if (arbid != -1)        // if its been arb'd, de-arb it
                {
                    OpenTK.Graphics.OpenGL.GL.Arb.MakeTextureHandleNonResident(arbid);     // can't do this twice!
                    arbid = -1;
                }

                GL.DeleteTexture(Id);
                GLStatics.RegisterDeallocation(GetType());
                Id = -2;    // -2 means made, then destroyed

                if (BitMaps != null)
                {
                    for (int i = 0; i < BitMaps.Length; i++)
                    {
                        if (OwnBitMaps[i] && BitMaps[i] != null)     // we may have empty spaces in the bitmap list
                            BitMaps[i].Dispose();
                    }

                    BitMaps = null;
                    OwnBitMaps = null;
                }
            }
            else
            {
                if (Id == -2)     // goes -1 -> ID -> -2, never uses stays as -1
                    System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of a texture block in {GetType().FullName}");        // only an warning due to the fact you can create and not use
            }
        }

        /// <summary>
        /// Load a bitmap into a level of the texture.
        /// You must have called CreateTexture before, allows bitmaps to be loaded individually.
        /// This can load into 2d texture, 2d arrays and 3d textures.
        /// </summary>
        /// <param name="bmp">Bitmap to load</param>
        /// <param name="level">Level to store it in. Use level = -1 for 2d Textures.</param>
        /// <param name="ownbmp">Class will own the bitmap</param>
        /// <param name="bmpmipmaplevels">Mip map levels in bitmap</param>
        /// <param name="alignment">Load into texture with this alignment, it bitmap is smaller than texture. Only for mipmaplevel = 0.  Use only for autogenerated mipmaps or no mip maps</param>
        public void LoadBitmap(Bitmap bmp, int level, bool ownbmp = false, int bmpmipmaplevels = 1, ContentAlignment alignment = ContentAlignment.TopLeft)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");
            System.Diagnostics.Debug.Assert(bmpmipmaplevels <= MipMapLevels);

            System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);     // 32 bit words, ARGB format

            MipMapAutoGenNeeded |= bmpmipmaplevels < MipMapLevels;       // ORed in, set flag if you've supplied a bitmap less than mipmaplevels

            IntPtr ptr = bmpdata.Scan0;     // its a byte ptr

            int curwidth = bmp.Width;
            int masterheight = MipMapHeight(bmp, bmpmipmaplevels);

            System.Diagnostics.Debug.Assert(bmp.Width <= Width);
            System.Diagnostics.Debug.Assert(masterheight <= Height);  // bitmap may be shorter

            int curheight = masterheight;

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, bmp.Width);      // indicate the image width, if we take less, then GL will skip pixels to get to next row

            for (int m = 0; m < bmpmipmaplevels; m++)
            {
                int xoff = 0;       // load offset into texture
                int yoff = 0;

                if (m == 0)       // if at mipmap level 0, full scale image..
                {
                    if (curwidth < Width)         // if source width < bitmap width, see if alignement wants us to store the texture in a different part of the bitmap
                    {
                        if (alignment == ContentAlignment.TopRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.BottomRight)
                        {
                            xoff = Width - curwidth;
                        }
                        else if (alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.BottomCenter)
                        {
                            xoff = (Width - curwidth) / 2;
                        }
                    }

                    if (curheight < Height)       // if source height < bitmap height
                    {
                        if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.BottomRight)
                        {
                            yoff = Height - curheight;
                        }
                        else if (alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.MiddleRight)
                        {
                            yoff = (Height - curheight) / 2;
                        }
                    }
                }

                if (level == -1)
                {
                    GL.TextureSubImage2D(Id,
                        m,                  // this is level m
                        xoff,               // x offset inside the target texture..
                        yoff,               // y offset..
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
                        xoff,      // xoff into target
                        yoff,      // yoff into target
                        level,  // zoffset, which is the bitmap depth
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
                if (curheight > 1)          // and the height
                    curheight /= 2;
            }

            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);      // back to off for safety
            bmp.UnlockBits(bmpdata);
            GLStatics.Check();

            if (ownbmp || KeepBitmapList)           // if we own the bmp, or we want to keep records..
            {
                if (BitMaps == null)
                {
                    BitMaps = new Bitmap[Depth];
                    OwnBitMaps = new bool[Depth];
                }

                if (level == -1)      // -1 means use 2d load, so its really the first 0 zorder
                    level = 0;

                BitMaps[level] = bmp;
                OwnBitMaps[level] = ownbmp;
            }
        }

        /// <summary>
        /// Load bitmap into next empty level, determined by DepthIndex
        /// </summary>
        /// <param name="bmp">Bitmap</param>
        /// <param name="ownbmp">Class will own the bitmap</param>
        /// <param name="bmpmipmaplevels">Mip map levels in bitmap</param>
        public void LoadNextBitmap(Bitmap bmp, bool ownbmp = false, int bmpmipmaplevels = 1)
        {
            LoadBitmap(bmp, DepthIndex++, ownbmp, bmpmipmaplevels);
        }

        private Bitmap textdrawbitmap = null;   // until used its null, using no space. then its reused saving recreation

        /// <summary>
        /// Draw text into a texture
        /// </summary>
        /// <param name="text">Text string</param>
        /// <param name="font">Font</param>
        /// <param name="forecolor">Fore color</param>
        /// <param name="backcolor">Back color</param>
        /// <param name="level">Level of the texture to bind (z depth in 2dArray/3d bitmaps). Use -1 for DepthIndex level bitmap</param>
        /// <param name="textformat">Textformat</param>
        /// <param name="backscale">Scaling of backcolor across bitmap for gradient effects</param>
        public void DrawText(string text, Font font, Color forecolor, Color backcolor, int level, StringFormat textformat = null, float backscale = 1.0f)
        {
            if (textdrawbitmap == null)
                textdrawbitmap = new Bitmap(Width, Height);

            if (level < 0)
                level = DepthIndex++;

            Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmap(ref textdrawbitmap, text, font, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, forecolor, backcolor, backscale, false, textformat);

            LoadBitmap(textdrawbitmap, level, true, 1);
        }

        /// <summary>
        /// Clear image to this color
        /// </summary>
        /// <param name="level">Level to clear</param>
        /// <param name="red">Red value, 0-1</param>
        /// <param name="green">Green value, 0-1</param>
        /// <param name="blue">Blue value, 0-1</param>
        /// <param name="alpha">Alpha value, 0-1</param>
        public void ClearImage(int level, float red, float green, float blue, float alpha)  // confirmed
        {
            int size = Marshal.SizeOf<float>() * 4;

            IntPtr pnt = Marshal.AllocHGlobal(size);
            float[] a = new float[] { red, green, blue, alpha };
            Marshal.Copy(a, 0, pnt, a.Length);
            GL.ClearTexImage(Id, level, PixelFormat.Rgba, PixelType.Float, pnt);
            Marshal.FreeHGlobal(pnt);
            GLStatics.Check();
        }

        /// <summary>
        /// Clear part of an image
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glClearTexSubImage.xhtml</href>
        /// </summary>
        /// <param name="level">Level to clear</param>
        /// <param name="x">Left edge</param>
        /// <param name="y">Lower edge</param>
        /// <param name="z">Front of region</param>
        /// <param name="width">Width to clear</param>
        /// <param name="height">Height to clear</param>
        /// <param name="depth">Depth to clear</param>
        /// <param name="red">Red value, 0-1</param>
        /// <param name="green">Green value, 0-1</param>
        /// <param name="blue">Blue value, 0-1</param>
        /// <param name="alpha">Alpha value, 0-1</param>
        public void ClearSubImage(int level, int x, int y, int z, int width, int height, int depth, float red, float green, float blue, float alpha)
        {
            int size = Marshal.SizeOf<float>() * 4;
            IntPtr pnt = Marshal.AllocHGlobal(size);
            float[] a = new float[] { red, green, blue, alpha };
            Marshal.Copy(a, 0, pnt, a.Length);
            GL.ClearTexSubImage(Id, level, x, y, z, width, height, depth, PixelFormat.Rgba, PixelType.Float, pnt);
            Marshal.FreeHGlobal(pnt);
            GLStatics.Check();
        }

        /// <summary>
        /// Get a texture image in a speciic type.  Only floats or bytes are currently supported.
        /// Use inverty to correct for any inversion if your getting the data from a framebuffer texture - it appears to be inverted when written
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pxformatback">Return format.</param>
        /// <param name="level">Image level</param>
        /// <param name="inverty">If to inverty so the image is the standard windows way up (first is top) instead of openGL</param>
        /// <returns>Array of selected type</returns>
        public T[] GetTextureImageAs<T>(PixelFormat pxformatback = PixelFormat.Rgba, int level = 0, bool inverty = false)
        {
            int elementsperpixel = GL4Statics.ElementsPerPixel(pxformatback);

            int totalelements = Width * Height * elementsperpixel;

            int elementsizeT = Marshal.SizeOf(typeof(T));

            int elementstride = elementsperpixel * Width;

            int bufsize = totalelements * elementsizeT;

            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bufsize); // get an unmanaged buffer
            GL.GetTextureImage(Id, level, pxformatback, elementsizeT == 4 ? PixelType.Float : PixelType.UnsignedByte, bufsize, unmanagedPointer);  // fill
            GLStatics.Check();

            if (elementsizeT == 4)
            {
                float[] data = new float[totalelements];

                if (inverty)
                {
                    IntPtr p = unmanagedPointer;
                    for (int y = 0; y < Height; y++)
                    {
                        Marshal.Copy(p, data, (Height - 1 - y) * elementstride, elementstride);      // transfer buffer to floats
                        p += elementstride;
                    }
                }
                else
                {
                    Marshal.Copy(unmanagedPointer, data, 0, totalelements);      // transfer buffer to floats
                }

                Marshal.FreeHGlobal(unmanagedPointer);
                return data as T[];
            }
            else
            {
                byte[] data = new byte[totalelements];

                if (inverty)
                {
                    IntPtr p = unmanagedPointer;
                    for (int y = 0; y < Height; y++)
                    {
                        Marshal.Copy(p, data, (Height - 1 - y) * elementstride, elementstride);      // transfer buffer to floats
                        p += elementstride;
                    }
                }
                else
                {
                    Marshal.Copy(unmanagedPointer, data, 0, totalelements);      // transfer buffer to floats
                }

                Marshal.FreeHGlobal(unmanagedPointer);
                return data as T[];
            }
        }

        /// <summary>
        /// Get a bitmap of a level
        /// </summary>
        /// <param name="level">Level to return</param>
        /// <param name="inverty">If to inverty so the image is the standard windows way up (first is top) instead of openGL</param>
        /// <returns>Bitmap</returns>
        public Bitmap GetBitmap(int level = 0, bool inverty = false)
        {
            byte[] texdatab = GetTextureImageAs<byte>(PixelFormat.Bgra, level, inverty);
            return Utils.BitMapHelpers.CreateBitmapFromARGBBytes(Width, Height, texdatab);
        }

        /// <summary>
        /// Set the sampler mode (GL_CLAMP_TO_EDGE, GL_CLAMP_TO_BORDER, GL_MIRRORED_REPEAT, GL_REPEAT, or GL_MIRROR_CLAMP_TO_EDGE) (Use OpenTK names) 
        /// </summary>
        /// <param name="s">Texture wrap width</param>
        /// <param name="t">Texture wrap height</param>
        /// <param name="r">Texture wrap depth</param>
        public void SetSamplerMode(TextureWrapMode s, TextureWrapMode t, TextureWrapMode r)
        {
            int st = (int)s;
            int tt = (int)t;
            int pt = (int)r;
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapS, ref st);
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapT, ref tt);
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapR, ref pt);

        }

        /// <summary>
        /// Set the sampler mode (GL_CLAMP_TO_EDGE, GL_CLAMP_TO_BORDER, GL_MIRRORED_REPEAT, GL_REPEAT, or GL_MIRROR_CLAMP_TO_EDGE) (Use OpenTK names) 
        /// </summary>
        /// <param name="s">Texture wrap width</param>
        /// <param name="t">Texture wrap height</param>
        public void SetSamplerMode(TextureWrapMode s, TextureWrapMode t)
        {
            int st = (int)s;
            int tt = (int)t;
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapS, ref st);
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapT, ref tt);
        }

        /// <summary>
        /// Set the sampler mode (GL_CLAMP_TO_EDGE, GL_CLAMP_TO_BORDER, GL_MIRRORED_REPEAT, GL_REPEAT, or GL_MIRROR_CLAMP_TO_EDGE) (Use OpenTK names) 
        /// </summary>
        /// <param name="s">Texture wrap width</param>
        public void SetSamplerMode(TextureWrapMode s)
        {
            int st = (int)s;
            GL.TextureParameterI(Id, TextureParameterName.TextureWrapS, ref st);
        }

        /// <summary>
        /// Set Min Mag filter on texture
        /// </summary>
        /// <param name="minfilter">Min filter (default LinearMipmapLinear)</param>
        /// <param name="maxfilter">Max filter (default Linear)</param>
        public void SetMinMagFilter(All minfilter = All.LinearMipmapLinear, All maxfilter = All.Linear)
        {
            var textureMinFilter = (int)minfilter;
            GL.TextureParameterI(Id, TextureParameterName.TextureMinFilter, ref textureMinFilter);
            var textureMagFilter = (int)maxfilter;
            GL.TextureParameterI(Id, TextureParameterName.TextureMagFilter, ref textureMagFilter);
        }

        /// <summary> Set Min Mag filter to linear </summary>
        public void SetMinMagLinear()
        {
            var textureFilter = (int)All.Linear;
            GL.TextureParameterI(Id, TextureParameterName.TextureMinFilter, ref textureFilter);
            GL.TextureParameterI(Id, TextureParameterName.TextureMagFilter, ref textureFilter);
        }

        /// <summary> Generate mip map textures </summary>
        public void GenMipMapTextures()     // only call if mipmaplevels > 1 after you have loaded all z planes. Called automatically for 2d+2darrays
        {
            GL.GenerateTextureMipmap(Id);
        }

        /// <summary> Control mip map level </summary>
        public void MipMapLevel(int basev, int max)
        {
            GL.TextureParameterI(Id, TextureParameterName.TextureBaseLevel, ref basev);
            GL.TextureParameterI(Id, TextureParameterName.TextureMaxLevel, ref max);
        }

        /// <summary> Get mip map height of image</summary>
        static public int MipMapHeight(Bitmap map, int bitmapmipmaplevels)
        {
            return bitmapmipmaplevels == 1 ? map.Height : map.Height / 3 * 2;        // if bitmap is mipped mapped, work out correct height.
        }
    }
}

