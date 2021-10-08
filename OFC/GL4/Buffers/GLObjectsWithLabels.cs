/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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

using GLOFC.GL4;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4
{
    // class uses a GLVertexBufferIndirect to hold a vertex buffer and indirect commands, with multiple textures supplied to the shader
    // The object drawn is defined by its objectshader, and its model vertices are in objectbuffer (start of) of objectlength
    // Object shader will get vertex 0 = objectbuffer vector4s, and vertex 1 = worldpositions of items added (instance divided)
    // use with text shader GLShaderPipeline(new GLPLVertexShaderQuadTextureWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexedMulti(0,0,true, texunitspergroup));
    // multiple textures can be bound to carry the text labels, the number given by textures, limited by opengl texture limit per fragment shader(GetMaxTextureDepth())
    // that gives the number of objects that can be produced 

    public class GLObjectsWithLabels : IDisposable
    {
        public Size LabelSize { get { return textures[0].Size; } }

        public int Blocks { get { return dataindirectbuffer.Indirects.Count > 0 ? dataindirectbuffer.Indirects[0].Positions.Count : 0; } }      // how many blocks allocated
        public int BlocksRemoved { get; private set; } = 0;     // how many have been removed
        public bool Emptied { get { return Blocks > 0 && BlocksRemoved == Blocks; } }

        private GLVertexBufferIndirect dataindirectbuffer;                  // buffer and its indirect buffers [0] = objects, [1] = labels. [1].tags holds the object tag, [1].Tags holds the count of objects
        public GLRenderableItem ObjectRenderer { get; private set; }
        public GLRenderableItem TextRenderer { get; private set; }
        private GLTexture2DArray[] textures;                                // holds the text labels
        private int objectvertexescount;                                    // vert count for object
        private int textureinuse = 0;                                       // textures in use, up to max textures.

        public class BlockRef                                               // used by adders to pass back a list of block refs
        {
            public GLObjectsWithLabels owl;
            public int blockindex;
            public int count;
            public object tag;
        };

        GLItemsList items = new GLItemsList();      // our own item list to hold disposes

        public Tuple<GLRenderableItem,GLRenderableItem> Create(
                                int textures,       // number of 2D textures to allow maximum (limited by GL)
                                int estimateditemspergroup,      // estimated objects per group, this adds on vertext buffer space to allow for mat4 alignment. Smaller means more allowance.
                                int mingroups,     // minimum groups to have
                                GLBuffer objectbuffer, int objectvertexes , GLRenderState objrc,  PrimitiveType objpt,  // object buffer, vertexes and its rendercontrol
                                Size texturesize, GLRenderState textrc, SizedInternalFormat textureformat, // texturesize and render control
                                int debuglimittexture = 0)  // use to limit texture map depth for debugging
        {
            this.objectvertexescount = objectvertexes;

            // Limit number of 2d textures in a single 2d array
            int maxtextper2darray = GL4Statics.GetMaxTextureDepth();
            if ( debuglimittexture > 0)
                maxtextper2darray = debuglimittexture;

            // set up number of textmaps bound
            int maxtexturesbound = GL4Statics.GetMaxFragmentTextures();
            int textmaps = Math.Min(textures, maxtexturesbound);

            // which then give us the number of stars we can do
            int objectcount = textmaps * maxtextper2darray;
            int groupcount = objectcount / estimateditemspergroup;      
            groupcount = Math.Max(mingroups, groupcount);               // min groups

            System.Diagnostics.Debug.WriteLine($"GLObjectWithLabels oc {objectcount} gc {groupcount}");

            // estimate maximum vert buffer needed, allowing for extra due to the need to align the mat4
            int vertbufsize = objectcount * (GLBuffer.Vec4size + GLBuffer.Mat4size) +       // for a vec4 + mat4 per object
                                groupcount * GLBuffer.Mat4size;         // and for groupcount Mat4 fragmentation per group

            // create the vertex indirect buffer
            dataindirectbuffer = new GLVertexBufferIndirect(items,vertbufsize, GLBuffer.WriteIndirectArrayStride * groupcount, true,BufferUsageHint.DynamicDraw);

            // objects 
            ObjectRenderer = GLRenderableItem.CreateVector4Vector4(items, objpt, objrc,
                                                                        objectbuffer, 0, 0,     // binding 0 is shapebuf, offset 0, no draw count yet
                                                                        dataindirectbuffer.Vertex, 0, // binding 1 is vertex's world positions, offset 0
                                                                        null, 0, 1);        // no ic, second divisor 1
            ObjectRenderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
            ObjectRenderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

            // text

            this.textures = new GLTexture2DArray[textmaps];

            for (int i = 0; i < this.textures.Length; i++)
            {
                int n = Math.Min(objectcount, maxtextper2darray);
                this.textures[i] = new GLTexture2DArray(texturesize.Width, texturesize.Height, n, textureformat, 1);
                items.Add(this.textures[i]);
                objectcount -= maxtextper2darray;
            }

            TextRenderer = GLRenderableItem.CreateMatrix4(items, PrimitiveType.Quads, textrc,
                                                                dataindirectbuffer.Vertex, 0, 0, //attach buffer with matrices, no draw count
                                                                new GLRenderDataTexture(this.textures,0),        // binding 0 assign to our texture 2d
                                                                0, 1);     //no ic, and matrix divide so 1 matrix per vertex set
            TextRenderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
            TextRenderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;


            return new Tuple<GLRenderableItem, GLRenderableItem>(ObjectRenderer, TextRenderer);
        }

        // array/text holds worldpositions and text of each object
        // tag gives a logical name to each group - must be unique
        // returns position where it stopped, or -1 if all added

        public int Add(Vector4[] array, string[] text, 
                                Font fnt, Color fore, Color back, 
                                Vector3 size, Vector3 rot, bool rotatetoviewer, bool rotateelevation,   // see GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix
                                StringFormat fmt, float backscale, Vector3 textoffset, List<BlockRef> blocklist)
        {
            var bmps = BitMapHelpers.DrawTextIntoFixedSizeBitmaps(LabelSize, text, fnt, System.Drawing.Text.TextRenderingHint.ClearTypeGridFit, fore, back, backscale, false, fmt);
            var mats = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrices(array, textoffset, size, rot, rotatetoviewer, rotateelevation,0,0,0,true);
            int v = Add(array, mats, bmps, blocklist);
            BitMapHelpers.Dispose(bmps);
            return v;
        }

        // array holds worldpositions for objects
        // matrix holds pos, orientation, etc for text
        // bitmaps are for each label.  Owned by caller
        // pos = indicates one to start from
        // arraylength = if -1, use length of array, else only go to this entry (so 10 means 0..9 used)
        // -1 all added, else the pos where it failed on
        // block list updated

        public int Add(Vector4[] array, Matrix4[] matrix, Bitmap[] bitmaps, List<BlockRef> blocklist, int pos = 0, int arraylength = -1)
        {
            if (arraylength == -1)      // this means use length of array
                arraylength = array.Length;

            do
            {
                if (textureinuse >= textures.Length)       // out of textures
                    return pos;

                // how many can we take..
                int touse = Math.Min(arraylength - pos, textures[textureinuse].DepthLeftIndex);

                //System.Diagnostics.Debug.WriteLine($"Fill {pos} {touse}");
                // fill in vertex array entries from pos .. pos+touse-1

                if (!dataindirectbuffer.Fill(array, pos, touse, 0, objectvertexescount, 0, touse, -1))  // indirect 0 holds object draws, objectvertexes long, touse objects, estimate base instance on position
                {
                    System.Diagnostics.Debug.WriteLine("GLObjectWithLabels failed to add object indirect");
                    return pos;
                }

                // now fill in the texture by loading bitmaps into each slot, and update the matrix image position
                for (int i = 0; i < touse; i++)
                {
                    int imgpos = textures[textureinuse].DepthIndex + textureinuse * 65536;      // bits 16+ has textmap
                   // System.Diagnostics.Debug.WriteLine($"Obj2 Write Mat {pos} {pos + i} tx {textmapinuse} = {imgpos}");
                    matrix[pos + i][0,3] = imgpos;
                    textures[textureinuse].LoadBitmap(bitmaps[pos + i], textures[textureinuse].DepthIndex++, false, 1);
                }

                // now write in the matrices to the vertex buffers
                dataindirectbuffer.Vertex.AlignMat4();          // instancing counts in mat4 sizes (mat4 0 @0, mat4 1 @ 64 etc) so align to it

                if (!dataindirectbuffer.Fill(matrix, pos, touse, 1, 4, 0, touse, -1))     // indirect 1 holds text draws, 4 vertices per draw, touse objects, estimate base instance on position
                {
                    System.Diagnostics.Debug.WriteLine("GLObjectWithLabels failed to add text indirect");
                    return pos;
                }

                blocklist.Add(new BlockRef() { owl = this, blockindex = dataindirectbuffer.Indirects[0].Positions.Count - 1, count = touse });

                ObjectRenderer.DrawCount = dataindirectbuffer.Indirects[0].Positions.Count;       // update draw count
                ObjectRenderer.IndirectBuffer = dataindirectbuffer.Indirects[0];                  // and buffer

                TextRenderer.DrawCount = dataindirectbuffer.Indirects[1].Positions.Count;
                TextRenderer.IndirectBuffer = dataindirectbuffer.Indirects[1];

                if (textures[textureinuse].DepthLeftIndex == 0)                                 // out of bitmap space, next please!
                    textureinuse++;

                pos += touse;

            } while (pos < arraylength);

            return -1;
        }

        // Find in objects.  Return block list render group and index into it, or null

        public Tuple<int, int> Find(GLShaderPipeline findshader, GLRenderState glstate, Point pos, Size size)
        {
            var geo = findshader.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
            geo.SetScreenCoords(pos, size);
            ObjectRenderer.Execute(findshader, glstate); 
            var res = geo.GetResult();
            if (res != null)
            {
                //System.Diagnostics.Debug.WriteLine("Set Found something");  for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
                return new Tuple<int,int>((int)res[0].W,(int)res[0].Y);
            }
            else
                return null;
        }

        public bool Remove(int i)
        {
            if (dataindirectbuffer.Indirects.Count>0 && i < dataindirectbuffer.Indirects[0].Positions.Count )
            {
                dataindirectbuffer.Remove(i, 0);        // clear draw of both text and object
                dataindirectbuffer.Remove(i, 1);
                BlocksRemoved++;                        // increment blocks removed
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            items.Dispose();
        }

    }
}
