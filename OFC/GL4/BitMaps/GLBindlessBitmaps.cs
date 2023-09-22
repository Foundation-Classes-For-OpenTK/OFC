/*
 * Copyright 2019-2023 Robbyxp1 @ github.com
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
    /// Class can hold varying number of bitmaps, they can be of different sizes, each can be rotated/sized/lookat individually.
    /// can be alpha blended either by distance in or out. 
    /// </summary>

    public class GLBindlessTextureBitmaps : IDisposable
    {
        /// <summary>Enable or disable this bitmap set</summary>
        public virtual bool Enable { get { return shader.Enable; } set { shader.Enable = value; } }
        /// <summary> Number of tags defined</summary>
        public int TagCount { get { return tagtoentries.Count; } }            // number of tags recorded
        /// <summary> Current generation </summary>
        public uint CurrentGeneration { get; set; } = 0;                       // to be set on write

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of bitmap collection</param>
        /// <param name="rlist">Render list to draw into</param>
        /// <param name="arbblock">Block ID to use for bindless arb parameters</param>
        /// <param name="cullface">True to cull face</param>
        /// <param name="depthtest">True to depth test</param>
        /// <param name="yfixed">Set true to fix Y co-ord externally</param>
        /// <param name="maxpergroup">Maximum number of bitmaps per group</param>
        public GLBindlessTextureBitmaps(string name, GLRenderProgramSortedList rlist, int arbblock = 3,
                                            bool cullface = true, bool depthtest = true, int maxpergroup = int.MaxValue, bool yfixed = false, int maxbitmaps = 1024)
        {
            this.name = name;
            this.context = GLStatics.GetContext();

            int maxdepthpertexture = GL4Statics.GetMaxTextureDepth();     // limits the number of textures per 2darray
            int max = Math.Min(maxdepthpertexture, maxpergroup);        //note RI uses a VertexArray to load the matrix in, so not limited by that (max size of uniform buffer)

            matrixbuffers = new GLMatrixBufferWithGenerations(items, max);

            renderlist = rlist;

            arbtextureblock = new GLBindlessTextureHandleBlock(arbblock);
            arbtextureblock.AllocateBytes(sizeof(long) * 2 * maxbitmaps);   // preallocate
            items.Add(arbtextureblock);

            shader = new GLShaderPipeline(new GLPLVertexShaderMatrixTriStripTexture(yfixed), new GLPLFragmentShaderTextureBindlessIndexed(0, arbblock, alphablend: true, maxbitmaps:maxbitmaps));
            items.Add(shader);

            renderstate = GLRenderState.Tri();      
            renderstate.CullFace = cullface;
            renderstate.DepthTest = depthtest;
            renderstate.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted
        }

        /// <summary>Does tag exist?</summary>
        public bool Exist(object tag)       // does this tag exist?
        {
            return tagtoentries.ContainsKey(tag);
        }
        /// <summary>Remove entry associated with this tag</summary>
        public bool Remove(Object tag)
        {
            if (tagtoentries.TryGetValue(tag, out int pos))
            {
                return matrixbuffers.RemoveAt(pos);
            }
            else
                return false;
        }
        /// <summary>Remove entry</summary>
        public bool Remove(int pos)
        {
            return matrixbuffers.RemoveAt(pos);
        }
        /// <summary>Set visibility and rotation parameters for this tag, true if found</summary>
        public bool SetVisiblityRotation(Object tag, bool onoff, bool rotatetoviewer = false, bool rotateelevation = false)
        {
            if (tagtoentries.TryGetValue(tag, out int pos))
            {
                return matrixbuffers.SetVisibilityRotation(pos, !onoff ? -1 : rotatetoviewer ? (rotateelevation ? 2 : 1) : 0);  // and rotation selection. This is master ctrl, <0 culled, >=0 shown
            }
            else
                return false;
        }
        /// <summary>Get the matrix of the tag. A zero matrix if not there</summary>
        public Matrix4 GetMatrix(Object tag)
        {
            if (tagtoentries.TryGetValue(tag, out int pos))
            {
                return matrixbuffers.GetMatrix(pos);
            }
            else
                return Matrix4.Zero;
        }
        /// <summary>Get the world position of a tag. Zero if not there</summary>
        public Vector3 GetWorldPos(Object tag)              // if not there, get 0,0,0 back. Use exists to chWeck first
        {
            var x = GetMatrix(tag);       // zero if not there
            return new Vector3(x.M11, x.M22, x.M33);
        }

        /// <summary>
        /// Remove generation X from list, excepting these tags which are set to currentgeneration
        /// </summary>
        /// <param name="removegeneration">Remove all generations less or equal to this generation</param>
        /// <param name="currentgeneration">Current generation</param>
        /// <param name="tagtoentries">Tag to entry list to update on removal of each item, this tag is removed from this list on removal</param>
        /// <param name="keeplist">if keeplist is set, and its in the list, the generation is reset to currentgeneration and its kept</param>
        /// <returns>return relative index giving the different between the current gen and the maximum generation found</returns>

        public uint RemoveGeneration(uint removegenerationbelow, Dictionary<object, Tuple<GLMatrixBufferWithGenerations, int>> tagtoentries, 
            HashSet<object> keeplist = null)
        {
            return matrixbuffers.RemoveGeneration(removegenerationbelow, CurrentGeneration, tagtoentries, keeplist);
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
        }

        /// <summary>
        /// Add text, tag may be null (but then you won't be able to find it!)
        /// text is made into a local bitmap, reused per write, so we don't have lots of bitmaps hanging around
        /// </summary>
        /// <param name="tag">Tag for this bitmap, may be null</param>
        /// <param name="data">Disposable data to store, may be null</param>
        /// <param name="text">Text</param>
        /// <param name="font">Font</param>
        /// <param name="forecolor">Fore color</param>
        /// <param name="backcolor">Back color</param>
        /// <param name="worldpos">Position of bitmap in world</param>
        /// <param name="size">Size to draw bitmap in world.</param>
        /// <param name="rotationradians">Rotation of bitmap (ignored if rotates below are on)</param>
        /// <param name="textformat">Text format to use to write text</param>
        /// <param name="backscale">Backscale for background to produce a graduated background</param>
        /// <param name="rotatetoviewer">True to rotate to viewer in azimuth</param>
        /// <param name="rotateelevation">True to rotate to viewer in elevation</param>
        /// <param name="alphafadescalar">Alpha Fade scalar on distance</param>
        /// <param name="alphafadepos">Alpha fade distance. Negative for fade in, positive for fade out </param>
        /// <param name="visible">True if visible on start</param>
        public void Add(object tag, IDisposable data,
                            string text, Font font, Color forecolor, Color backcolor,
                            Size bitmapsize,
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

            var textdrawbitmap = new Bitmap(bitmapsize.Width,bitmapsize.Height);

            GLOFC.Utils.BitMapHelpers.DrawTextIntoFixedSizeBitmap(ref textdrawbitmap, text, font, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, forecolor, backcolor, backscale, false, textformat);

            Add(tag, data, textdrawbitmap, 1, worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, false, visible);
            textdrawbitmap.Dispose();
        }


        /// <summary>
        /// Add a bitmap to the collection.
        /// </summary>
        /// <param name="tag">Tag for this bitmap, may be null</param>
        /// <param name="data">Disposable data to store, may be null</param>
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
        /// <param name="textureformat">Texture format of bitmaps</param>
        public virtual void Add(object tag, IDisposable data,
                            Bitmap bmp, 
                            int bmpmipmaplevels,
                            Vector3 worldpos,
                            Vector3 size,
                            Vector3 rotationradians,
                            bool rotatetoviewer = false, bool rotateelevation = false,
                            float alphafadescalar = 0,
                            float alphafadepos = 0,
                            bool ownbitmap = false,
                            bool visible = true,
                            OpenTK.Graphics.OpenGL4.SizedInternalFormat textureformat = OpenTK.Graphics.OpenGL4.SizedInternalFormat.Rgba8
            )
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Bitmaps detected context incorrect");

            Matrix4 mat = GLPLVertexShaderMatrixTriStripTexture.CreateMatrix(worldpos, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, 0, visible);

            var posi = matrixbuffers.Add(tag, data, mat, CurrentGeneration);     

            var glb = new GLTexture2D(bmp, textureformat, bmpmipmaplevels, bmpmipmaplevels, ownbitmap);
         //   items.Add(glb);

            if (renderableitem == null)
            {
                renderableitem = GLRenderableItem.CreateMatrix4(items, OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, renderstate, matrixbuffers.MatrixBuffer, 0, 4, null, ic: 0);     //drawcount=4 (4 vertexes made up by shader, in tristrip), ic will be set in Add.
                renderlist.Add(shader, name + "Shader", renderableitem);
            }

            //System.Diagnostics.Debug.Write($"Add bitmap into {posi}");
            arbtextureblock.WriteHandle(glb, posi);
            renderableitem.InstanceCount =  matrixbuffers.Count;   // update instance count to items in group
        }

        #region Implementation

        private GLItemsList items = new GLItemsList();      // we have our own item list, which is disposed when we dispose

        private GLMatrixBufferWithGenerations matrixbuffers;
        private GLBindlessTextureHandleBlock arbtextureblock;
        private GLRenderableItem renderableitem;
        private GLRenderProgramSortedList renderlist;
        private GLRenderState renderstate;
        private GLShaderPipeline shader;
        private string name;

        private Dictionary<object, int> tagtoentries = new Dictionary<object, int>();

        private IntPtr context;     // double check for window swapping

        #endregion
    }
}

