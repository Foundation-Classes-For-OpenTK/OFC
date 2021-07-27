﻿/*
 * Copyright © 2016 EDDiscovery development team + Robbyxp1 @ github.com
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
 * 
 * 
 */
using System;
using System.Drawing;

namespace OFC
{
    public static class BitMapHelpers
    {
        public static void DrawTextCentreIntoBitmap(ref Bitmap img, string text, Font dp, System.Drawing.Text.TextRenderingHint hint, Color c, Color? b = null)
        {
            using (Graphics bgr = Graphics.FromImage(img))
            {
                bgr.TextRenderingHint = hint;

                if ( b!=null)
                {
                    Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                    using (Brush bb = new SolidBrush(b.Value))
                        bgr.FillRectangle(bb, backarea);
                }

                SizeF sizef = bgr.MeasureString(text, dp);

                using (Brush textb = new SolidBrush(c))
                    bgr.DrawString(text, dp, textb, img.Width / 2 - (int)((sizef.Width + 1) / 2), img.Height / 2 - (int)((sizef.Height + 1) / 2));
            }
        }

        // if b != Transparent, a back box is drawn.
        // bitmap never bigger than maxsize
        // setting frmt allows you to word wrap etc into a bitmap, maximum of maxsize.  
        // no frmt means a single line across the bitmap unless there are \n in it.

        public static Bitmap DrawTextIntoAutoSizedBitmap(string text, Size maxsize, Font dp, System.Drawing.Text.TextRenderingHint hint, Color c, Color b,
                                            float backscale = 1.0F, StringFormat frmt = null)
        {
            Bitmap t = new Bitmap(1, 1);

            using (Graphics bgr = Graphics.FromImage(t))
            {
                bgr.TextRenderingHint = hint;

                // if frmt, we measure the string within the maxsize bounding box.
                SizeF sizef = (frmt != null) ? bgr.MeasureString(text, dp, maxsize, frmt) : bgr.MeasureString(text, dp);
                //System.Diagnostics.Debug.WriteLine("Bit map auto size " + sizef);

                int width = Math.Min((int)(sizef.Width + 1), maxsize.Width);
                int height = Math.Min((int)(sizef.Height + 1), maxsize.Height);
                Bitmap img = new Bitmap(width, height);

                using (Graphics dgr = Graphics.FromImage(img))
                {
                    if (!b.IsFullyTransparent() && text.Length > 0)
                    {
                        Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                        using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, b, b.Multiply(backscale), 90))
                            dgr.FillRectangle(bb, backarea);

                        //dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;   // only worth doing this if we have filled it.. if transparent, antialias does not work
                    }

                    using (Brush textb = new SolidBrush(c))
                    {
                        if (frmt != null)
                            dgr.DrawString(text, dp, textb, new Rectangle(0, 0, width, height), frmt); // use the draw into rectangle with formatting function
                        else
                            dgr.DrawString(text, dp, textb, 0, 0);
                    }

                    return img;
                }
            }
        }

        // draw into fixed sized bitmap. 
        // centretext overrided frmt and just centres it
        // frmt provides full options and draws text into bitmap

        public static Bitmap DrawTextIntoFixedSizeBitmapC(string text, Size size, Font dp, System.Drawing.Text.TextRenderingHint hint, Color c, Color b,
                                                    float backscale = 1.0F, bool centertext = false, StringFormat frmt = null)
        {
            Bitmap img = new Bitmap(size.Width, size.Height);
            Color? back = null;
            if (!b.IsFullyTransparent())
                back = b;
            return DrawTextIntoFixedSizeBitmap(ref img, text, dp, hint, c, back, backscale, centertext, frmt);
        }

        public static Bitmap DrawTextIntoFixedSizeBitmap(ref Bitmap img, string text,Font dp, System.Drawing.Text.TextRenderingHint hint, Color c, Color? b,
                                                    float backscale = 1.0F, bool centertext = false, StringFormat frmt = null, int angleback = 90 , bool antialias = true)
        { 
            using (Graphics dgr = Graphics.FromImage(img))
            {
                dgr.TextRenderingHint = hint;

                if (b != null)           
                {
                    if (b.Value.IsFullyTransparent())       // if transparent colour to paint in, need to fill clear it completely
                    {
                        dgr.Clear(Color.Transparent);
                    }
                    else
                    {
                        Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                        using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, b.Value, b.Value.Multiply(backscale), angleback))
                            dgr.FillRectangle(bb, backarea);

                        //dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; // only if filled
                    }

                }

                using (Brush textb = new SolidBrush(c))
                {
                    if (centertext)
                    {
                        SizeF sizef = dgr.MeasureString(text, dp);
                        int w = (int)(sizef.Width + 1);
                        int h = (int)(sizef.Height + 1);
                        dgr.DrawString(text, dp, textb, img.Width / 2 - w / 2, img.Height / 2 - h / 2);
                    }
                    else if (frmt != null)
                        dgr.DrawString(text, dp, textb, new Rectangle(0, 0, img.Width, img.Height), frmt);
                    else
                        dgr.DrawString(text, dp, textb, 0, 0);
                }

                return img;
            }
        }

        public static void FillBitmap(Bitmap img, Color c, float backscale = 1.0F)
        {
            using (Graphics dgr = Graphics.FromImage(img))
            {
                Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, c, c.Multiply(backscale), 90))
                    dgr.FillRectangle(bb, backarea);
            }
        }

        // convert BMP to another format and return the bytes of that format

        public static byte[] ConvertTo(this Bitmap bmp, System.Drawing.Imaging.ImageFormat fmt)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bmp.Save(ms, fmt);
            Byte[] f = ms.ToArray();
            return f;
        }

        // not the quickest way in the world, but not supposed to do this at run time
        // can disable a channel, or get a brightness.  If avg granulatity set, you can average over a wider area than the granularity for more smoothing

        public enum BitmapFunction
        {
            Average,
            HeatMap,
        };

        public static Bitmap Function(this Bitmap bmp, int granularityx, int granularityy, int avggranulatityx = 0, int avggranulatityy = 0, BitmapFunction mode = BitmapFunction.Average, 
                        bool enablered = true, bool enablegreen = true, bool enableblue = true, 
                        bool flipx = false, bool flipy = false)
        {
            System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);     // 32 bit words, ARGB format

            IntPtr baseptr = bmpdata.Scan0;     // its a byte ptr

            Bitmap newbmp = new Bitmap(granularityx, granularityy);  // bitmap to match

            if (avggranulatityx == 0)
                avggranulatityx = granularityx;
            if (avggranulatityy == 0)
                avggranulatityy = granularityy;

            int bmpcellsizex = bmp.Width / granularityx;      // no of avg points 
            int bmpcellsizey = bmp.Height / granularityx;
            int avgwidth = bmp.Width / avggranulatityx;
            int avgheight = bmp.Height / avggranulatityx;
            int linestride = bmp.Width * 4;

            for (int gy = 0; gy < granularityy; gy++)
            {
                for (int gx = 0; gx < granularityx; gx++)
                {
                    int x = bmpcellsizex / 2 + bmpcellsizex * gx - avgwidth/2;
                    int mx = x + avgwidth;
                    x = x.Range(0, bmp.Width-1);
                    mx = mx.Range(0, bmp.Width);

                    int y = bmpcellsizey / 2 + bmpcellsizey * gy - avgheight/2;
                    int my = y + avgheight;
                    y = y.Range(0, bmp.Height-1);
                    my = my.Range(0, bmp.Height);   // yes, let it go to height, its the stop value

                  //  System.Diagnostics.Debug.WriteLine("Avg " + x + "->" + mx + ", " + y +"->" + my);

                    uint red=0, green=0, blue = 0,points=0;

                    for (int ay = y; ay < my; ay++)
                    {
                        IntPtr ptr = baseptr + x * 4 + ay * linestride;
                        for (int ax = x; ax < mx; ax++)
                        {
                            int v = System.Runtime.InteropServices.Marshal.ReadInt32(ptr);  // ARBG
                            red += enablered ? (uint)((v >> 16) & 0xff) : 0;
                            blue += enableblue ? (uint)((v >> 8) & 0xff) : 0;
                            green += enablegreen ? (uint)((v >> 0) & 0xff) : 0;
                            ptr += 4;
                            points++;
                            //System.Diagnostics.Debug.WriteLine("Avg " + ax + "," + ay);
                        }
                    }

                    Color res;
                    if (mode == BitmapFunction.HeatMap)
                    {
                        double ir = (double)red * (double)red + (double)green * (double)green + (double)blue * (double)blue;
                        ir = Math.Sqrt(ir) * 255/442;   // scaling is for sqrt(255*255+255*255+255*255) to bring it back to 255 nom
                        ir /= points;
                        res = Color.FromArgb(255, (int)ir, (int)ir, (int)ir);
                    }
                    else
                        res = Color.FromArgb(255, (int)(red / points), (int)(blue / points), (int)(green / points));

                    newbmp.SetPixel(flipx ? (newbmp.Width - 1 - gx) : gx, flipy ? (newbmp.Height - 1 - gy) : gy, res);
                }

            }

            bmp.UnlockBits(bmpdata);
            return newbmp;
        }

        public static SizeF MeasureStringInBitmap(string text, Font f, StringFormat fmt = null )
        {
            using (Bitmap t = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(t))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; 
                    if ( fmt != null )
                        return g.MeasureString(text, f, new Size(10000, 10000), fmt);
                    else
                        return g.MeasureString(text, f, new Size(10000, 10000));
                }
            }
        }

        public static byte[] GetARGBBytes(this Bitmap bmp)
        {
            System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);     // 32 bit words, ARGB format

            int bytecount = bmp.Width * bmp.Height * 4;
            byte[] destbytes = new byte[bytecount];
            System.Runtime.InteropServices.Marshal.Copy(bmpdata.Scan0, destbytes, 0, bytecount);      // stored blue,green,red,alpha

            bmp.UnlockBits(bmpdata);
            return destbytes;
        }

        public static Bitmap CreateBitmapFromARGBBytes(int width , int height, byte[] argb)
        {
            Bitmap b = new Bitmap(width,height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Drawing.Imaging.BitmapData bmpdata = b.LockBits(new Rectangle(0,0,width,height), System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                                        b.PixelFormat);

            IntPtr baseptr = bmpdata.Scan0;     // its a byte ptr
            int bytes = width * height * 4;
            System.Runtime.InteropServices.Marshal.Copy(argb, 0, baseptr, bytes);

            b.UnlockBits(bmpdata);
            return b;
        }

        public static void DumpBitmap(this Bitmap bmp, int maxy = int.MaxValue)
        {
            System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);     // 32 bit words, ARGB format

            IntPtr baseptr = bmpdata.Scan0;     // its a byte ptr

            for (int y = 0; y < Math.Min(bmp.Height,maxy); y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int v = System.Runtime.InteropServices.Marshal.ReadInt32(baseptr);  // ARBG
                    baseptr += 4;

                    uint alpha = (uint)((v >> 24) & 0xff);
                    uint red = (uint)((v >> 16) & 0xff);
                    uint blue = (uint)((v >> 8) & 0xff);
                    uint green = (uint)((v >> 0) & 0xff);
                    System.Diagnostics.Debug.WriteLine("{0} {1} : a{2} r{3} g{4} b{5}", x, y, alpha, red, blue, green);
                }
            }

            bmp.UnlockBits(bmpdata);
        }
    }
}
