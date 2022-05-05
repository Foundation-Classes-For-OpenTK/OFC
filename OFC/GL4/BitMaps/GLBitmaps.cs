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
using GLOFC.GL4.Buffers;
using GLOFC.GL4.Textures;

namespace GLOFC.GL4.Bitmaps
{
    /// <summary>
    /// This namespace contains classes which help throw multiple bitmaps efficently onto the screen, and manage their lifecycles
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Class can hold varying number of bitmaps, all of the same size, each can be rotated/sized/lookat individually.
    /// can be alpha blended either by distance in or out. See GLPLVertexShaderQuadTextureWithMatrixTranslation
    /// You can delete by tag name or clear all
    /// you can add/remove by generation
    /// Holds as many text bitmaps as you need, it will grow to fit. It won't shrink, but it will reuse deleted slot.
    /// </summary>

    public class GLBitmaps : IDisposable
    {
        /// <summary>Enable or disable this bitmap set</summary>
        public virtual bool Enable { get { return shader.Enable; } set { shader.Enable = value; } }
        /// <summary>Name of bitmap set</summary>
        public string Name { get { return name;  } }
        /// <summary>Number of tags</summary>
        public int TagCount { get { return matrixbuffers.TagCount; } }              // number of tags recorded
        /// <summary>Current generation of bitmaps</summary>
        public uint CurrentGeneration { get { return matrixbuffers.CurrentGeneration; } set { matrixbuffers.CurrentGeneration = value; } }
        /// <summary>Bit map size</summary>
        public Size BitmapSize {get { return bitmapsize; } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of bitmap collection</param>
        /// <param name="rlist">Render list to draw into</param>
        /// <param name="bitmapsize">Bit map size</param>
        /// <param name="mipmaplevels">Mip map levels</param>
        /// <param name="textureformat">Texture format of bitmaps</param>
        /// <param name="cullface">True to cull face</param>
        /// <param name="depthtest">True to depth test</param>
        /// <param name="maxpergroup">Maximum number of bitmaps per group</param>
        /// <param name="yfixed">Set true to fix Y co-ord externally</param>
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

            shader = new GLShaderPipeline(new GLPLVertexShaderMatrixTriStripTexture(yfixed), new GLPLFragmentShaderTexture2DIndexed(0, alphablend: true));
            items.Add(shader);

            renderstate = GLRenderState.Tri();      
            renderstate.CullFace = cullface;
            renderstate.DepthTest = depthtest;
            renderstate.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

            texmipmaplevels = mipmaplevels;
            this.textureformat = textureformat;
        }

        /// <summary>
        /// Add text, tag may be null (but then you won't be able to find it!)
        /// text is made into a local bitmap, reused per write, so we don't have lots of bitmaps hanging around
        /// </summary>
        /// <param name="tag">Tag for this bitmap, may be null</param>
        /// <param name="text">Text</param>
        /// <param name="font">Font</param>
        /// <param name="forecolor">Fore color</param>
        /// <param name="backcolor">Back color</param>
        /// <param name="worldpos">Position of bitmap in world</param>
        /// <param name="size">Size to draw bitmap in world. Note if Y and Z are zero, then Z is set to same ratio to width as bitmap</param>
        /// <param name="rotationradians">Rotation of bitmap (ignored if rotates below are on)</param>
        /// <param name="textformat">Text format to use to write text</param>
        /// <param name="backscale">Backscale for background to produce a graduated background</param>
        /// <param name="rotatetoviewer">True to rotate to viewer in azimuth</param>
        /// <param name="rotateelevation">True to rotate to viewer in elevation</param>
        /// <param name="alphafadescalar">Alpha Fade scalar on distance</param>
        /// <param name="alphafadepos">Alpha fade distance. Negative for fade in, positive for fade out </param>
        /// <param name="visible">True if visible on start</param>
        public void Add(object tag,
                            string text, Font font, Color forecolor, Color backcolor,
                            Vector3 worldpos,
                            Vector3 size,       
                            Vector3 rotationradians,        
                            StringFormat textformat = null, float backscale = 1.0f,
                            bool rotatetoviewer = false, bool rotateelevation = false,   
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

            GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmap(ref textdrawbitmap, text, font, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, forecolor, backcolor, backscale, false, textformat);

            Add(tag, textdrawbitmap, 1, worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, ownbitmap:false, visible);
        }

        /// <summary>
        /// Add a bitmap to the collection.
        /// </summary>
        /// <param name="tag">Tag for this bitmap, may be null</param>
        /// <param name="bmp">Bitmap</param>
        /// <param name="bmpmipmaplevels">The bitmap mip map levels</param>
        /// <param name="worldpos">Position of bitmap in world</param>
        /// <param name="size">Size to draw bitmap in world.</param>
        /// <param name="rotationradians">Rotation of bitmap (ignored if rotates below are on)</param>
        /// <param name="rotatetoviewer">True to rotate to viewer in azimuth</param>
        /// <param name="rotateelevation">True to rotate to viewer in elevation</param>
        /// <param name="alphafadescalar">Alpha Fade scalar on distance</param>
        /// <param name="alphafadepos">Alpha fade distance. Negative for fade in, positive for fade out </param>
        /// <param name="ownbitmap"></param>
        /// <param name="visible">True if visible on start</param>
        /// <returns></returns>
        // add a bitmap, indicate if owned by class or you.  Gives back group no, position in group, total in group
        public virtual Tuple<int,int,int> Add(object tag,
                            Bitmap bmp,
                            int bmpmipmaplevels,
                            Vector3 worldpos,
                            Vector3 size,      
                            Vector3 rotationradians,        
                            bool rotatetoviewer = false, bool rotateelevation = false,   
                            float alphafadescalar = 0, 
                            float alphafadepos = 0,
                            bool ownbitmap = false,
                            bool visible=  true
                         )
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Bitmaps detected context incorrect");

            Matrix4 mat = GLPLVertexShaderMatrixTriStripTexture.CreateMatrix(worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, 0, visible);

            var gpc = matrixbuffers.Add(tag, ownbitmap ? bmp : null, mat);     // group, pos, total in group
          //  System.Diagnostics.Debug.WriteLine("Make bitmap {0} {1} {2} at {3}", gpc.Item1, gpc.Item2, gpc.Item3 , worldpos);

            grouptextureslist[gpc.Item1].LoadBitmap(bmp, gpc.Item2, false, bmpmipmaplevels);       // texture does not own them, we may do
            grouprenderlist[gpc.Item1].InstanceCount = gpc.Item3;   // update instance count to items in group
            return gpc;
        }

        /// <summary>Does tag exist?</summary>
        public bool Exist(object tag)       // does this tag exist?
        {
            return matrixbuffers.Exist(tag);
        }
        /// <summary>Remove entry associated with this tag</summary>
        public bool Remove(Object tag)
        {
            return matrixbuffers.Remove(tag);
        }
        /// <summary>Set visibility and rotation parameters for this tag, true if found</summary>
        public bool SetVisiblityRotation(Object tag, bool onoff , bool rotatetoviewer = false, bool rotateelevation = false)
        {
            return matrixbuffers.SetVisibilityRotation( tag, !onoff ? -1 : rotatetoviewer ? (rotateelevation ? 2 : 1) : 0);  // and rotation selection. This is master ctrl, <0 culled, >=0 shown
        }
        /// <summary>Get the matrix of the tag. A zero matrix if not there</summary>
        public Matrix4 GetMatrix(Object tag)
        {
            return matrixbuffers.GetMatrix(tag);
        }
        /// <summary>Get the world position of a tag. Zero if not there</summary>
        public Vector3 GetWorldPos(Object tag)              // if not there, get 0,0,0 back. Use exists to check first
        {
            var x = matrixbuffers.GetMatrix(tag);       // zero if not there
            return new Vector3(x.M11, x.M22, x.M33);
        }
        /// <summary>Remove generation X from list, excepting these tags</summary>
        public uint RemoveGeneration(uint removegenerationbelow, HashSet<object> keeplist = null)
        {
            return matrixbuffers.RemoveGeneration(removegenerationbelow, keeplist);
        }
        /// <summary>Clear all bitmaps</summary>
        public void Clear()
        {
            matrixbuffers.Clear();
        }
        /// <summary>Set Y if using Y hold</summary>
        public void SetY(float y)
        {
            shader.GetShader<GLPLVertexShaderMatrixTriStripTexture>(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader).SetY(y);
        }
        /// <summary>Dispose of the bitmaps</summary>
        public virtual void Dispose()           // you can double dispose.
        {
            matrixbuffers.Dispose();
            items.Dispose();

            if (textdrawbitmap != null)
                textdrawbitmap.Dispose();
        }

        #region Implementation
        private void AddedNewGroup(int groupno, GLBuffer matrixbuffer)      // callback due to new group added, we need a texture and a RI
        {
            var texture = new GLTexture2DArray();
            items.Add(texture);
            texture.CreateOrUpdateTexture(bitmapsize.Width, bitmapsize.Height, matrixbuffers.Matricesperbuffer, textureformat, texmipmaplevels);
            grouptextureslist.Add(texture); // need to keep these for later addition

            var rd = new RenderData(texture);
            var renderableItem = GLRenderableItem.CreateMatrix4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, renderstate, matrixbuffer, 0, 4, rd, ic: 0);     //drawcount=4 (4 vertexes made up by shader, in tristrip), ic will be set in Add.
            renderlist.Add(shader, name + ":" + groupno, renderableItem);
            grouprenderlist.Add(renderableItem);
        }

        private class RenderData : IGLRenderItemData
        {
            public RenderData(GLTexture2DArray tex)
            {
                texture = tex;
            }

            public virtual void Bind(IGLRenderableItem ri, IGLProgramShader shader, GLMatrixCalc c)     // called per renderable item..
            {
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);

                if (texture.Id >= 0)
                {
                    if (texture.MipMapAutoGenNeeded)
                    {
                        texture.GenMipMapTextures();
                        texture.MipMapAutoGenNeeded = false;
                    }

                    texture.Bind(1);
                    System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr2), glasserterr2);
                }
            }

            private GLTexture2DArray texture;
        }

        private GLSetOfMatrixBufferWithGenerations matrixbuffers;

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

        #endregion
    }
}

