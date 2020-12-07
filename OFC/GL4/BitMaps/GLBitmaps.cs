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
 
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OFC.GL4
{
    // Class can hold varying number of bitmaps, all of the same size, each can be rotated/sized/lookat individually.
    // can be alpha blended either by distance in or out. See GLPLVertexShaderQuadTextureWithMatrixTranslation
    // You can delete by tag name or clear all
    // Holds as many text bitmaps as you need, it will grow to fit. It won't shrink, but it will reused deleted slot.

    public class GLBitmaps : IDisposable
    {
        public int MipMapLevel { get; set; } = 3;       // set before add..

        public bool Enable { get { return shader.Enable; } set { shader.Enable = value; } }

        public GLBitmaps(GLRenderProgramSortedList rlist, Size bitmapsize, bool cullface = true, bool depthtest = true, int maxpergroup = int.MaxValue )
        {
            int m = Math.Min(GL4Statics.GetValue(OpenTK.Graphics.OpenGL4.GetPName.MaxArrayTextureLayers), GL4Statics.GetMaxUniformBlockSize() / GLLayoutStandards.Mat4size);
            this.maxpergroup = Math.Min(m, maxpergroup);

            renderlist = rlist;
            this.bitmapsize = bitmapsize;

            shader = new GLShaderPipeline(new GLPLVertexShaderQuadTextureWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexed(0, alphablend: true));
            items.Add(shader);

            rc = GLRenderControl.Quads();      
            rc.CullFace = cullface;
            rc.DepthTest = depthtest;
            rc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted
        }


        // add text, tag may be null but then you won't be able to find it
        // text is made into a local bitmap, reused per write, so we don't have lots of bitmaps hanging around

        public void Add(object tag,
                            string text, Font f, Color fore, Color back,
                            Vector3 worldpos,
                            Vector3 size,       // Note if Y and Z are zero, then Z is set to same ratio to width as bitmap
                            Vector3 rotationradians,        // ignored if rotates are on
                            StringFormat fmt = null, float backscale = 1.0f,
                            bool rotatetoviewer = false, bool rotateelevation = false,   // if set, rotationradians not used
                            float alphascale = 0, float alphaend = 0
                         )
        {
            if (size.Z == 0 && size.Y == 0)
            {
                size.Z = size.X * (float)bitmapsize.Height / (float)bitmapsize.Width;       // autoscale to bitmap ratio
            }

            if (textdrawbitmap == null)
                textdrawbitmap = new Bitmap(bitmapsize.Width, bitmapsize.Height);

            BitMapHelpers.DrawTextIntoFixedSizeBitmap(ref textdrawbitmap, text, f, fore, back, backscale, false, fmt);

            Add(tag, textdrawbitmap, 1, worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphascale, alphaend, ownbitmap:false);
        }

        // add a bitmap, indicate if owned by class or you
        public void Add(object tag,
                            Bitmap bmp, 
                            int bmpmipmaplevels,
                            Vector3 worldpos,
                            Vector3 size,       // Note if Y and Z are zero, then Z is set to same ratio to width as bitmap
                            Vector3 rotationradians,        // ignored if rotates are on
                            bool rotatetoviewer = false, bool rotateelevation = false,   // if set, rotationradians not used
                            float alphascale = 0, float alphaend = 0 , 
                            bool ownbitmap = false
                         )
        {
            GLBitmapGroup g = groups.Find(x => x.Left>0);      // find one with space..

            if ( g == null )
            {
                g = new GLBitmapGroup(items,rc, maxpergroup, MipMapLevel, bitmapsize, maxpergroup);          // no space, make a new one
                renderlist.Add(shader, g.RenderableItem);
                groups.Add(g);
            }

            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.Mult(mat, Matrix4.CreateScale(size));
            if (rotatetoviewer == false)                                            // if autorotating, no rotation is allowed. matrix is just scaling/translation
            {
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationX(rotationradians.X));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationY(rotationradians.Y));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationZ(rotationradians.Z));
            }
            mat = Matrix4.Mult(mat, Matrix4.CreateTranslation(worldpos));
            mat[1, 3] = rotatetoviewer ? (rotateelevation ? 2 : 1) : 0;  // and rotation selection
            mat[2, 3] = alphascale;
            mat[3, 3] = alphaend;

            g.Add(tag, bmp, bmpmipmaplevels, ownbitmap, mat);                                        // add entry with matrix
        }

        public bool Exist(object tag)       // does this tag exist?
        {
            GLBitmapGroup g = groups.Find(x => x.FindTag(tag) >= 0);
            return g != null;
        }

        public bool Remove(Object tag)
        {
            GLBitmapGroup g = groups.Find(x => x.FindTag(tag) >= 0);
            if (g != null)
                return g.RemoveAt(g.FindTag(tag));
            else
                return false;
        }

        public void RemoveGeneration(int generation = 1)        // all new images get generation 0
        {
            foreach (var g in groups)
                g.RemoveGeneration(generation);
        }

        public void Clear()
        {
            foreach (var g in groups)
            {
                g.Clear();
            }
        }

        public bool SetGenerationIfExist(object tag, int generation = 0)
        {
            GLBitmapGroup g = groups.Find(x => x.SetGenerationIfExists(tag, generation));      // find first tag, and mark it
            return g != null;
        }

        public void IncreaseGeneration()
        {
            foreach (var g in groups)
                g.IncreaseGeneration();
        }

        public void Dispose()           // you can double dispose.
        {
            foreach( GLBitmapGroup g in groups)
            {
                renderlist.Remove(shader, g.RenderableItem);
                g.Clear();
            }

            items.Dispose();

            if (textdrawbitmap != null)
                textdrawbitmap.Dispose();
        }

        private Size bitmapsize;
        private int maxpergroup;
        private GLItemsList items = new GLItemsList();      // we have our own item list, which is disposed when we dispose
        private List<GLBitmapGroup> groups = new List<GLBitmapGroup>();
        private GLRenderProgramSortedList renderlist;
        private GLRenderControl rc;
        private GLShaderPipeline shader { get; set; }
        private Bitmap textdrawbitmap;      // for drawing into alpha text
    }
}

