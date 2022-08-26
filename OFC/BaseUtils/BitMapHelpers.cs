/*
 * Copyright © 2016-2021 Robbyxp1 @ github.com
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


namespace GLOFC.Utils
{
    /// <summary>
    /// Set of utilities for use by OFC and exposed for use by implementors
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Static Class to help draw bitmaps
    /// </summary>
    public static class BitMapHelpers
    {
        /// <summary>
        /// Draw text centre into a bitmap already defined
        /// </summary>
        /// <param name="bitmap">Bitmap to draw into</param>
        /// <param name="text">Text to draw</param>
        /// <param name="font">Font to draw in</param>
        /// <param name="hint">Text rendering hint to use</param>
        /// <param name="forecolor">Fore colour</param>
        /// <param name="backcolor">Back color. If not given, no backcolor is drawn</param>
        public static void DrawTextCentreIntoBitmap(ref Bitmap bitmap, string text, Font font, System.Drawing.Text.TextRenderingHint hint, Color forecolor, Color? backcolor = null)
        {
            using (Graphics bgr = Graphics.FromImage(bitmap))
            {
                bgr.TextRenderingHint = hint;

                if ( backcolor!=null)
                {
                    Rectangle backarea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    using (Brush bb = new SolidBrush(backcolor.Value))
                        bgr.FillRectangle(bb, backarea);
                }

                SizeF sizef = bgr.MeasureString(text, font);

                using (Brush textb = new SolidBrush(forecolor))
                    bgr.DrawString(text, font, textb, bitmap.Width / 2 - (int)((sizef.Width + 1) / 2), bitmap.Height / 2 - (int)((sizef.Height + 1) / 2));
            }
        }

        /// <summary>
        /// Draw text centre into a autosized bitmap 
        /// </summary>
        /// <param name="text">Text to draw</param>
        /// <param name="maxsize">Maximum size of bitmap to make</param>
        /// <param name="font">Font to draw in</param>
        /// <param name="hint">Text rendering hint to use</param>
        /// <param name="forecolor">Fore colour</param>
        /// <param name="backcolor">Back color. If transparent, no back is drawn</param>
        /// <param name="backscale">Gradient of back</param>
        /// <param name="textformat">Text format. Setting it allows you to word wrap etc into the bitmap. No format means a single line across the bitmap unless \n is in there </param>
        public static Bitmap DrawTextIntoAutoSizedBitmap(string text, Size maxsize, Font font, System.Drawing.Text.TextRenderingHint hint, Color forecolor, Color backcolor,
                                            float backscale = 1.0F, StringFormat textformat = null)
        {
            Bitmap t = new Bitmap(1, 1);

            using (Graphics bgr = Graphics.FromImage(t))
            {
                bgr.TextRenderingHint = hint;

                // if frmt, we measure the string within the maxsize bounding box.
                SizeF sizef = (textformat != null) ? bgr.MeasureString(text, font, maxsize, textformat) : bgr.MeasureString(text, font);
                //System.Diagnostics.Debug.WriteLine("Bit map auto size " + sizef);

                int width = Math.Min((int)(sizef.Width + 1), maxsize.Width);
                int height = Math.Min((int)(sizef.Height + 1), maxsize.Height);
                Bitmap img = new Bitmap(width, height);

                using (Graphics dgr = Graphics.FromImage(img))
                {
                    if (!backcolor.IsFullyTransparent() && text.Length > 0)
                    {
                        Rectangle backarea = new Rectangle(0, 0, img.Width, img.Height);
                        using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, backcolor, backcolor.Multiply(backscale), 90))
                            dgr.FillRectangle(bb, backarea);

                        //dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;   // only worth doing this if we have filled it.. if transparent, antialias does not work
                    }

                    using (Brush textb = new SolidBrush(forecolor))
                    {
                        if (textformat != null)
                            dgr.DrawString(text, font, textb, new Rectangle(0, 0, width, height), textformat); // use the draw into rectangle with formatting function
                        else
                            dgr.DrawString(text, font, textb, 0, 0);
                    }

                    return img;
                }
            }
        }

        /// <summary>
        /// Draw text centre into a bitmap 
        /// </summary>
        /// <param name="bitmap">Bitmap to draw into</param>
        /// <param name="text">Text to draw</param>
        /// <param name="font">Font to draw in</param>
        /// <param name="hint">Text rendering hint to use</param>
        /// <param name="forecolor">Fore colour</param>
        /// <param name="backcolor">Back color. If transparent, no back is drawn</param>
        /// <param name="backscale">Gradient of back</param>
        /// <param name="centertext">If true, centre text in box. Ignore textformat</param>
        /// <param name="textformat">Text format. Setting it allows you to word wrap etc into the bitmap. No format means a single line across the bitmap unless \n is in there </param>
        /// <param name="angleback">Angle of gradient on back</param>

        public static Bitmap DrawTextIntoFixedSizeBitmap(ref Bitmap bitmap, string text,Font font, System.Drawing.Text.TextRenderingHint hint, Color forecolor, Color? backcolor,
                                                    float backscale = 1.0F, bool centertext = false, StringFormat textformat = null, int angleback = 90 )
        { 
            using (Graphics dgr = Graphics.FromImage(bitmap))
            {
                dgr.TextRenderingHint = hint;

                if (backcolor != null)           
                {
                    if (backcolor.Value.IsFullyTransparent())       // if transparent colour to paint in, need to fill clear it completely
                    {
                        dgr.Clear(Color.Transparent);
                    }
                    else
                    {
                        Rectangle backarea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                        using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, backcolor.Value, backcolor.Value.Multiply(backscale), angleback))
                            dgr.FillRectangle(bb, backarea);

                        //dgr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; // only if filled
                    }

                }

                using (Brush textb = new SolidBrush(forecolor))
                {
                    if (centertext)
                    {
                        SizeF sizef = dgr.MeasureString(text, font);
                        int w = (int)(sizef.Width + 1);
                        int h = (int)(sizef.Height + 1);
                        dgr.DrawString(text, font, textb, bitmap.Width / 2 - w / 2, bitmap.Height / 2 - h / 2);
                    }
                    else if (textformat != null)
                        dgr.DrawString(text, font, textb, new Rectangle(0, 0, bitmap.Width, bitmap.Height), textformat);
                    else
                        dgr.DrawString(text, font, textb, 0, 0);
                }

                return bitmap;
            }
        }

        /// <summary>
        /// Draw text centre into a array of automatically made bitmaps
        /// </summary>
        /// <param name="size">Size of bitmaps</param>
        /// <param name="text">Array with text to draw</param>
        /// <param name="font">Font to draw in</param>
        /// <param name="hint">Text rendering hint to use</param>
        /// <param name="forecolor">Fore colour</param>
        /// <param name="backcolor">Back color. If transparent, no back is drawn</param>
        /// <param name="backscale">Gradient of back</param>
        /// <param name="centertext">If true, centre text in box. Ignore textformat</param>
        /// <param name="textformat">Text format. Setting it allows you to word wrap etc into the bitmap. No format means a single line across the bitmap unless \n is in there </param>
        /// <param name="angleback">Angle of gradient on back</param>
        /// <param name="pos">Start position in array to start from</param>
        /// <param name="length">Number of items to take in array, or -1 means automatically work it out</param>
        public static Bitmap[] DrawTextIntoFixedSizeBitmaps(Size size, string[] text, 
                                                    Font font, System.Drawing.Text.TextRenderingHint hint, Color forecolor, Color? backcolor,
                                                    float backscale = 1.0F, bool centertext = false, StringFormat textformat = null, int angleback = 90,
                                                    int pos = 0, int length = -1)
        {
            if (length == -1)
                length = text.Length - pos;

            Bitmap[] bmp = new Bitmap[length];
            for( int i = 0; i < length; i++)
            {
                bmp[i] = new Bitmap(size.Width,size.Height);
                DrawTextIntoFixedSizeBitmap(ref bmp[i], text[i+pos], font, hint, forecolor, backcolor, backscale, centertext, textformat, angleback);
            }
            return bmp;
        }

        /// <summary> Dispose of an array of bitmaps </summary>
        public static void Dispose(Bitmap[] array)
        {
            foreach (var x in array)
                x.Dispose();
        }

        /// <summary>
        /// Fill a bitmap with a colour
        /// </summary>
        /// <param name="bitmap">Bitmap to fill</param>
        /// <param name="color">Fore colour</param>
        /// <param name="backscale">Gradient of back</param>
        public static void FillBitmap(Bitmap bitmap, Color color, float backscale = 1.0F)
        {
            using (Graphics dgr = Graphics.FromImage(bitmap))
            {
                Rectangle backarea = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                using (Brush bb = new System.Drawing.Drawing2D.LinearGradientBrush(backarea, color, color.Multiply(backscale), 90))
                    dgr.FillRectangle(bb, backarea);
            }
        }

        /// <summary> Convert BMP to another format and return the bytes of that format </summary> 
        public static byte[] ConvertTo(this Bitmap bmp, System.Drawing.Imaging.ImageFormat fmt)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bmp.Save(ms, fmt);
            Byte[] f = ms.ToArray();
            return f;
        }

        /// <summary> Function select </summary>
        public enum BitmapFunction
        {
            /// <summary> Average bitmap </summary>
            Average,
            /// <summary> Heatmap of bitmap </summary>
            HeatMap,
        };
        /// <summary>
        /// Perform a function and return info on the bitmap
        /// </summary>
        /// <param name="bmp">Bitmap</param>
        /// <param name="granularityx">X granularity</param>
        /// <param name="granularityy">Y granularity</param>
        /// <param name="avggranulatityx">Average X granularity. If avg granulatity set, you can average over a wider area than the granularity for more smoothing</param>
        /// <param name="avggranulatityy">Average Y granularity</param>
        /// <param name="mode">Function mode, see enum</param>
        /// <param name="enablered">Enable red channel</param>
        /// <param name="enablegreen">Enable green channel</param>
        /// <param name="enableblue">Enable bule channel</param>
        /// <param name="flipx">Flip X coord in results</param>
        /// <param name="flipy">Flip y coord in results</param>
        /// <returns>Bitmap with results of function</returns>

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

        /// <summary>
        /// Measure string in bitmap
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="font">Font</param>
        /// <param name="textformat">String format</param>
        /// <param name="measurementbox">If given, maximum size of text box</param>
        /// <returns></returns>
        public static SizeF MeasureStringInBitmap(string text, Font font, StringFormat textformat, Size? measurementbox = null)
        {
            using (Bitmap t = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(t))
                {
                    if (measurementbox == null)
                        measurementbox = new Size(20000, 20000);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    var ret = g.MeasureString(text, font, measurementbox.Value, textformat);
                  //  System.Diagnostics.Debug.WriteLine($"Measure '{text}' in font {font.ToString()} size {ret}");
                    return ret;
                }
            }
        }

        /// <summary>
        /// Measure a string using standard string format (Near/NoWrap)
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="font">Font</param>
        /// <param name="measurementbox"></param>
        /// <returns></returns>
        public static SizeF MeasureStringInBitmap(string text, Font font, Size? measurementbox = null)      // standard format - near/near/nowrap
        {
            using (var pfmt = new StringFormat())
            {
                pfmt.Alignment = StringAlignment.Near;
                pfmt.LineAlignment = StringAlignment.Near;
                pfmt.FormatFlags = StringFormatFlags.NoWrap;
                return MeasureStringInBitmap(text, font, pfmt, measurementbox);
            }
        }

        /// <summary>
        /// Get bitmap into byte array
        /// </summary>
        /// <param name="bmp">Bitmap</param>
        /// <returns>byte array with ARGB bytes for each pixel. Stored in order blue,green,red,alpha</returns>
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

        /// <summary>
        /// Create bitmap from ARGB Bytes
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="argb">byte array with ARGB ordered bytes. Stored in order blue,green,red,alpha</param>
        /// <returns>Bitmap</returns>
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

        /// <summary>
        /// Dump bitmap to debug stream
        /// </summary>
        /// <param name="bmp">Bitmap</param>
        /// <param name="maxy">Maximum Y to go to</param>
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

        /// <summary>
        /// Create a bitmap from the centre part of another bitmap
        /// </summary>
        /// <param name="bmp">Bitmap to clone</param>
        /// <param name="partsize">Size of centre to take, in percentage terms (0.7F = 70% etc)</param>
        /// <returns>New bitmap</returns>
        public static Bitmap CloneCentre(this Bitmap bmp, SizeF partsize)
        {
            var widthtake = (int)(bmp.Width * partsize.Width);
            var widthleft = (bmp.Width - widthtake) / 2;
            var heighttake = (int)(bmp.Height * partsize.Width);
            var heighttop = (bmp.Height - heighttake) / 2;
            var rect = new Rectangle(widthleft, heighttop, widthtake, heighttake);
            var bmppart = bmp.Clone(rect, bmp.PixelFormat);
            return bmppart;
        }
    }
}
