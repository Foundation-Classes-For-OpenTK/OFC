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

        // add text
        public Bitmap Add(object tag,
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

            Bitmap bmp = BitMapHelpers.DrawTextIntoFixedSizeBitmapC(text, bitmapsize, f, fore, back, backscale, false, fmt);

            Add(tag, bmp, 1, worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphascale, alphaend, ownbitmap:true);

            return bmp;
        }

        // add a bitmap
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
            System.Diagnostics.Debug.Assert(bmp.Width == bitmapsize.Width && bmp.Height == bitmapsize.Height);

            BitmapGroup g = groups.Find(x => (x.Count-x.Deleted) < maxpergroup);      // find one with space..

            if ( g == null )
            {
                g = new BitmapGroup(items,rc, maxpergroup, MipMapLevel, bitmapsize, maxpergroup);          // no space, make a new one
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
            BitmapGroup g = groups.Find(x => x.FindTag(tag) >= 0);
            return g != null;
        }

        public bool Remove(Object tag)
        {
            BitmapGroup g = groups.Find(x => x.FindTag(tag) >= 0);
            if (g != null)
                return g.RemoveAt(g.FindTag(tag));
            else
                return false;
        }

        public void RemoveGeneration(int generation = 1)
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
            BitmapGroup g = groups.Find(x => x.SetGenerationIfExists(tag, generation));      // find first tag, and mark it
            return g != null;
        }

        public void IncreaseGeneration()     
        {
            foreach (var g in groups)
                g.IncreaseGeneration();
        }

        public void Dispose()           // you can double dispose.
        {
            foreach( BitmapGroup g in groups)
            {
                g.Clear();
                renderlist.Remove(shader,g.RenderableItem);
            }

            items.Dispose();
        }


        #region Bit map group, multiple ones of these exist, entries size is limited by size of GLBuffer holding matrices

        private class BitmapGroup
        {
            public int Count { get { return entries.Count; } }
            public int Deleted { get; set; } = 0;
            public GLRenderableItem RenderableItem { get; set; }

            private class EntryInfo
            {
                public Bitmap bitmap { get; set; }
                public Object tag { get; set; }
                public bool owned { get; set; }      // we own the bitmap
                public int generation { get; set; } = int.MaxValue;     // 0 = newest
                public bool IsEmpty { get { return bitmap == null; } }
            }

            private List<EntryInfo> entries = new List<EntryInfo>();
            private Dictionary<object, int> tagtoentries = new Dictionary<object, int>();
            private GLTexture2DArray texture;
            private GLBuffer matrixbuffer;
            private bool refillmipmaps = false;
            private Size bitmapsize { get; set; }

            public BitmapGroup(GLItemsList items, GLRenderControl rc, int groupsize, int mipmaplevels, Size bitmapsize, int depth)
            {
                matrixbuffer = new GLBuffer();
                items.Add(matrixbuffer);
                matrixbuffer.AllocateBytes(groupsize * GLLayoutStandards.Mat4size);
                matrixbuffer.AddPosition(0);        // CreateMatrix4 needs to have a position

                texture = new GLTexture2DArray();
                items.Add(texture);

                var rd = new RenderData(this);

                RenderableItem = GLRenderableItem.CreateMatrix4(items, rc, matrixbuffer, 4, rd, ic: 0);
                this.bitmapsize = bitmapsize;

                texture.CreateTexture(bitmapsize.Width, bitmapsize.Height, depth, mipmaplevels);
            }

            // return position added as index. If tag == null, you can't find it again
            public int Add(object tag, Bitmap bmp, int bmpmipmaplevels, bool owned, Matrix4 mat)
            {
                var entry = new EntryInfo() { bitmap = bmp, tag = tag , owned = owned, generation = 0};

                int pos = Deleted > 0 ? entries.FindIndex(x => x.IsEmpty) : -1;     // find an empty slot if any deleted

                if (pos == -1)
                {
                    pos = entries.Count;                                // not found, so make a fresh one at end
                    entries.Add(entry);
                }
                else
                {
                    Deleted--;
                    entries[pos] = entry;                               // set empty slot to active
                }

                if ( tag != null )
                    tagtoentries[tag] = pos;

                //System.Diagnostics.Debug.WriteLine("Pos {0} Matrix {1}", pos, mat);
                mat[0, 3] = pos;     // store pos of image in stack

                matrixbuffer.StartWrite(GLLayoutStandards.Mat4size * pos, GLLayoutStandards.Mat4size);
                matrixbuffer.Write(mat);
                matrixbuffer.StopReadWrite();

                texture.LoadBitmap(bmp, pos, false, bmpmipmaplevels);       // texture does not own them, we may do

                if ( bmpmipmaplevels < texture.MipMapLevels)        // if our mipmap is less than ordered, we need an auto gen
                {
                    refillmipmaps = true;
                }

                RenderableItem.InstanceCount = entries.Count;       

                return pos;
            }

            public int FindTag(Object tag)
            {
                return tagtoentries.TryGetValue(tag, out int index) ? index : -1;
            }

            public bool RemoveAt(int i)
            {
                if (i >= 0 && i < entries.Count)
                {
                    if (entries[i].owned)
                        entries[i].bitmap.Dispose();

                    if (entries[i].tag != null)
                        tagtoentries.Remove(entries[i].tag);

                    entries[i] = new EntryInfo(); // all will be null/false

                    Matrix4 zero = Matrix4.Identity;      // set ctrl 1,3 to -1 to indicate cull matrix
                    zero[1, 3] = -1;                      // if it did not work, it would appear at (0,0,0)
                    matrixbuffer.StartWrite(GLLayoutStandards.Mat4size * i, GLLayoutStandards.Mat4size);
                    matrixbuffer.Write(zero);
                    matrixbuffer.StopReadWrite();
                    Deleted++;
                    return true;
                }
                else
                    return false;
            }

            public void RemoveGeneration(int generation = 1)        // all over this, is removed
            {
                Matrix4 zero = Matrix4.Identity;      // set ctrl 1,3 to -1 to indicate cull matrix
                zero[1, 3] = -1;                      // if it did not work, it would appear at (0,0,0)

                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].generation >= generation)    // if older.. note invalid ones get generation = int.max
                    {
                        if (entries[i].owned)
                            entries[i].bitmap.Dispose();

                        if (entries[i].tag != null)
                            tagtoentries.Remove(entries[i].tag);

                        entries[i] = new EntryInfo(); // all will be null, generation will be int.max

                        matrixbuffer.StartWrite(GLLayoutStandards.Mat4size * i, GLLayoutStandards.Mat4size);
                        matrixbuffer.Write(zero);
                        matrixbuffer.StopReadWrite();
                        Deleted++;
                    }
                }
            }

            public void Clear()
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].owned)
                        entries[i].bitmap.Dispose();

                    if (entries[i].tag != null)
                        tagtoentries.Remove(entries[i].tag);

                    entries[i] = new EntryInfo(); // all will be null
                }

                Matrix4 zero = Matrix4.Identity;
                zero[1, 3] = -1;
                matrixbuffer.StartWrite(0);
                matrixbuffer.Write(zero, entries.Count);
                matrixbuffer.StopReadWrite();
                Deleted = entries.Count;
                RenderableItem.InstanceCount = 0;
            }

            // mark if exist, leave otherwise
            public bool SetGenerationIfExists(object tag, int generation = 0)
            {
                if (tagtoentries.TryGetValue(tag, out int index) )
                {
                    entries[index].generation = generation;
                    return true;
                }
                else
                    return false;
            }

            public void IncreaseGeneration()       // increase all generations
            {
                for (int i = 0; i < entries.Count; i++)
                    entries[i].generation++;
            }

            public void Bind()
            {
                if (refillmipmaps )
                {
                    texture.GenMipMapTextures();
                    refillmipmaps = false;
                }

                if (texture.Id >= 0)
                    texture.Bind(1);
            }

        }

        private class RenderData : IGLRenderItemData
        {
            public RenderData(BitmapGroup g)
            {
                group = g;
            }

            public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)     // called per renderable item..
            {
                group.Bind();
            }

            private BitmapGroup group;
        }

        private Size bitmapsize;
        private int maxpergroup;
        private GLItemsList items = new GLItemsList();      // we have our own item list, which is disposed when we dispose
        private List<BitmapGroup> groups = new List<BitmapGroup>();
        private GLRenderProgramSortedList renderlist;
        private GLRenderControl rc;
        private GLShaderPipeline shader { get; set; }

        #endregion
    }
}

