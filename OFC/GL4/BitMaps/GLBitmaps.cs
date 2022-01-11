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

using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Fragment;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4
{
    // Class can hold varying number of bitmaps, all of the same size, each can be rotated/sized/lookat individually.
    // can be alpha blended either by distance in or out. See GLPLVertexShaderQuadTextureWithMatrixTranslation
    // You can delete by tag name or clear all
    // you can add/remove by generation
    // Holds as many text bitmaps as you need, it will grow to fit. It won't shrink, but it will reuse deleted slot.

    public class GLBitmaps : IDisposable
    {
        public virtual bool Enable { get { return shader.Enable; } set { shader.Enable = value; } }

        public string Name { get { return name;  } }

        public int TagCount { get { return matrixbuffers.TagCount; } }              // number of tags recorded

        public uint CurrentGeneration { get { return matrixbuffers.CurrentGeneration; } set { matrixbuffers.CurrentGeneration = value; } }

        public Size BitmapSize {get { return bitmapsize; } }

        public GLBitmaps(string name, GLRenderProgramSortedList rlist, Size bitmapsize, int mipmaplevels = 3, 
                                            OpenTK.Graphics.OpenGL4.SizedInternalFormat textureformat = OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8, 
                                            bool cullface = true, bool depthtest = true, int maxpergroup = int.MaxValue, bool yfixed = false )
        {
            this.name = name;
            this.context = GLStatics.GetContext();

            int maxdepthpertexture = GL4Statics.GetMaxTextureDepth();     // limits the number of textures per 2darray
            int max = Math.Min(maxdepthpertexture, maxpergroup);        //note RI uses a VertexArray to load the matrix in, so not limited by that (max size of uniform buffer)

            matrixbuffers = new GLSetOfMatrixBufferWithGenerations(items, max);

            matrixbuffers.AddedNewGroup += AddedNewGroup;       // hook up call back to say i've made a group

            renderlist = rlist;
            this.bitmapsize = bitmapsize;

            shader = new GLShaderPipeline(new GLPLVertexShaderQuadTextureWithMatrixTranslation(yfixed), new GLPLFragmentShaderTexture2DIndexed(0, alphablend: true));
            items.Add(shader);

            renderstate = GLRenderState.Quads();      
            renderstate.CullFace = cullface;
            renderstate.DepthTest = depthtest;
            renderstate.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

            texmipmaplevels = mipmaplevels;
            this.textureformat = textureformat;
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
                            float alphafadescalar = 0, 
                            float alphafadepos = 0,
                            bool visible = true
                         )
        {
            if (size.Z == 0 && size.Y == 0)
            {
                size.Z = size.X * (float)bitmapsize.Height / (float)bitmapsize.Width;       // autoscale to bitmap ratio
            }

            if (textdrawbitmap == null)
                textdrawbitmap = new Bitmap(bitmapsize.Width, bitmapsize.Height);

            GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmap(ref textdrawbitmap, text, f, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, fore, back, backscale, false, fmt);

            Add(tag, textdrawbitmap, 1, worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, ownbitmap:false, visible);
        }

        // add a bitmap, indicate if owned by class or you.  Gives back group no, position in group, total in group
        public virtual Tuple<int,int,int> Add(object tag,
                            Bitmap bmp,
                            int bmpmipmaplevels,
                            Vector3 worldpos,
                            Vector3 size,       // Note if Y and Z are zero, then Z is set to same ratio to width as bitmap
                            Vector3 rotationradians,        // ignored if rotates are on
                            bool rotatetoviewer = false, bool rotateelevation = false,   // if set, rotationradians not used
                            float alphafadescalar = 0, 
                            float alphafadepos = 0,
                            bool ownbitmap = false,
                            bool visible=  true
                         )
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Bitmaps detected context incorrect");

            Matrix4 mat = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix(worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, 0, visible);

            var gpc = matrixbuffers.Add(tag, ownbitmap ? bmp : null, mat);     // group, pos, total in group
          //  System.Diagnostics.Debug.WriteLine("Make bitmap {0} {1} {2} at {3}", gpc.Item1, gpc.Item2, gpc.Item3 , worldpos);

            grouptextureslist[gpc.Item1].LoadBitmap(bmp, gpc.Item2, false, bmpmipmaplevels);       // texture does not own them, we may do
            grouprenderlist[gpc.Item1].InstanceCount = gpc.Item3;   // update instance count to items in group
            return gpc;
        }

        private void AddedNewGroup( int groupno, GLBuffer matrixbuffer)      // callback due to new group added, we need a texture and a RI
        {   
            var texture = new GLTexture2DArray();
            items.Add(texture);
            texture.CreateTexture(bitmapsize.Width, bitmapsize.Height, matrixbuffers.MaxPerGroup, textureformat, texmipmaplevels);
            grouptextureslist.Add(texture); // need to keep these for later addition

            var rd = new RenderData(texture);
            var renderableItem = GLRenderableItem.CreateMatrix4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.Quads, renderstate, matrixbuffer, 0, 4, rd, ic: 0);     //drawcount=4 (4 vertexes made up by shader), ic will be set in Add.
            renderlist.Add(shader, name + ":" + groupno, renderableItem);
            grouprenderlist.Add(renderableItem);
        }

        private class RenderData : IGLRenderItemData
        {
            public RenderData(GLTexture2DArray tex)
            {
                texture= tex;
            }

            public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)     // called per renderable item..
            {
                GLOFC.GLStatics.Check();

                if (texture.Id >= 0)
                {
                    if (texture.MipMapAutoGenNeeded)
                    {
                        texture.GenMipMapTextures();
                        texture.MipMapAutoGenNeeded = false;
                    }

                    texture.Bind(1);
                    GLOFC.GLStatics.Check();
                }
            }

            private GLTexture2DArray texture;
        }

        public bool Exist(object tag)       // does this tag exist?
        {
            return matrixbuffers.Exist(tag);
        }

        public bool Remove(Object tag)
        {
            return matrixbuffers.Remove(tag);
        }

        public bool SetVisiblityRotation(Object tag, bool onoff , bool rotatetoviewer = false, bool rotateelevation = false)
        {
            return matrixbuffers.SetVisibilityRotation( tag, !onoff ? -1 : rotatetoviewer ? (rotateelevation ? 2 : 1) : 0);  // and rotation selection. This is master ctrl, <0 culled, >=0 shown
        }

        public Matrix4 GetMatrix(Object tag)
        {
            return matrixbuffers.GetMatrix(tag);
        }

        public Vector3 GetWorldPos(Object tag)              // if not there, get 0,0,0 back. Use exists to check first
        {
            var x = matrixbuffers.GetMatrix(tag);       // zero if not there
            return new Vector3(x.M11, x.M22, x.M33);
        }

        public uint RemoveGeneration(uint removegenerationbelow, HashSet<object> keeplist = null)
        {
            return matrixbuffers.RemoveGeneration(removegenerationbelow, keeplist);
        }

        public void Clear()
        {
            matrixbuffers.Clear();
        }

        public void SetY(float y)
        {
            shader.GetShader<GLPLVertexShaderQuadTextureWithMatrixTranslation>(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader).SetY(y);
        }

        public virtual void Dispose()           // you can double dispose.
        {
            matrixbuffers.Dispose();
            items.Dispose();

            if (textdrawbitmap != null)
                textdrawbitmap.Dispose();
        }

        protected GLSetOfMatrixBufferWithGenerations matrixbuffers;

        private Size bitmapsize;
        private int texmipmaplevels;
        private OpenTK.Graphics.OpenGL4.SizedInternalFormat textureformat;
        private GLItemsList items = new GLItemsList();      // we have our own item list, which is disposed when we dispose
        private List<GLTexture2DArray> grouptextureslist = new List<GLTexture2DArray>();
        private List<GLRenderableItem> grouprenderlist = new List<GLRenderableItem>();
        private GLRenderProgramSortedList renderlist;
        private GLRenderState renderstate;
        private GLShaderPipeline shader;
        private Bitmap textdrawbitmap;      // for drawing into alpha text
        private string name;

        private IntPtr context;     // double check for window swapping
    }
}

