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

using OFC.GL4;
using OpenTK;
using System;
using System.Drawing;

namespace OFC.GL4
{
    // class uses a GLVertexBufferIndirect to hold a vertex buffer and indirect commands, with multiple textures supplied to the shader
    // The object drawn is defined by its objectshader, and its model vertices are in objectbuffer (start of) of objectlength
    // Object shader will get vertex 0 = objectbuffer vector4s, and vertex 1 = worldpositions of items added (instance divided)
    // use with text shader GLShaderPipeline(new GLPLVertexShaderQuadTextureWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexedMulti(0,0,true, texunitspergroup));
    // multiple textures can be bound to carry the text labels, the number given by textures.
    // dependent on the opengl, that gives the number of objects that can be produced (GetMaxTextureDepth())

    public class GLObjectsWithLabels : IDisposable
    {
        private GLVertexBufferIndirect dataindirectbuffer;
        private GLTexture2DArray[] textures;
        private int objectvertexes;
        private GLRenderableItem objectrenderer;
        private GLRenderableItem textrenderer;
        private GLRenderProgramSortedList robjects;
        private int textmapinuse = 0;

        GLItemsList items = new GLItemsList();      // our own item list to hold disposes

        // starsortextures, >0 stars, else -N = textures to use (therefore stars set by max texture depth)

        public GLObjectsWithLabels(string name, GLRenderProgramSortedList robjects,
                                int textures, int maxgroups,
                                IGLProgramShader objectshader, GLBuffer objectbuffer, int objectvertexes ,
                                IGLProgramShader textshader, Size texturesize )
        {
            this.objectvertexes = objectvertexes;
            this.robjects = robjects;

            // find gl parameters
            int maxtexturesbound = GL4Statics.GetMaxFragmentTextures();
            int maxtextper2darray = GL4Statics.GetMaxTextureDepth();
            maxtextper2darray = 4;

            // set up number of textmaps
            int textmaps = Math.Min(textures, maxtexturesbound);

            // which then give us the number of stars we can do
            int stars = textmaps * maxtextper2darray;

            // estimate maximum vert buffer needed, allowing for extra due to the need to align the mat4
            int vertbufsize = stars * (GLBuffer.Vec4size + GLBuffer.Mat4size) + maxgroups * GLBuffer.Mat4size;      

            // create the vertex indirect buffer
            dataindirectbuffer = new GLVertexBufferIndirect(items,vertbufsize, GLBuffer.WriteIndirectArrayStride * maxgroups, true);

            // stars
            GLRenderControl starrc = GLRenderControl.Tri();     // render is triangles, with no depth test so we always appear
            starrc.DepthTest = true;
            starrc.DepthClamp = true;

            objectrenderer = GLRenderableItem.CreateVector4Vector4(items, starrc,
                                                                        objectbuffer, 0, 0,     // binding 0 is shapebuf, offset 0, no draw count yet
                                                                        dataindirectbuffer.Vertex, 0, // binding 1 is vertex's world positions, offset 0
                                                                        null, 0, 1);        // no ic, second divisor 1
            objectrenderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
            objectrenderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

            robjects.Add(objectshader, name + "Stars", objectrenderer);

            // text

            this.textures = new GLTexture2DArray[textmaps];

            for (int i = 0; i < this.textures.Length; i++)
            {
                int n = Math.Min(stars, maxtextper2darray);
                this.textures[i] = new GLTexture2DArray(texturesize.Width, texturesize.Height, n);
                items.Add(this.textures[i]);
                stars -= maxtextper2darray;
            }

            var textrc = GLRenderControl.Quads();
            textrc.DepthTest = true;
            textrc.ClipDistanceEnable = 1;  // we are going to cull primitives which are deleted

            textrenderer = GLRenderableItem.CreateMatrix4(items, textrc,
                                                                dataindirectbuffer.Vertex, 0, 0, //attach buffer with matrices, no draw count
                                                                new GLRenderDataTexture(this.textures,0),        // binding 0..N for textures
                                                                0, 1);     //no ic, and matrix divide so 1 matrix per vertex set
            textrenderer.BaseIndexOffset = 0;     // offset in bytes where commands are stored
            textrenderer.MultiDrawCountStride = GLBuffer.WriteIndirectArrayStride;

            robjects.Add(textshader, name + "text", textrenderer);
        }

        // array/text holds worldpositions and text of each object
        // tag gives a logical name to each group
        // returns position where it stopped, or -1 if all added

        public int AddObjects(Object tag, Vector4[] array, string[] text, 
                                Font fnt, Color fore, Color back, 
                                Vector3 size, Vector3 rot, bool rotatetoviewer, bool rotateelevation,   // see GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix
                                StringFormat fmt, float backscale, Vector3 textoffset)
        {
            int pos = 0;

            do
            {
                if (textmapinuse >= textures.Length)       // out of textures
                    return pos;

                // how many can we take..
                int touse = Math.Min(array.Length - pos, textures[textmapinuse].DepthLeftIndex);

                if ( pos == 0 )     // at pos 0, we can just directly fill
                {
                    if (!dataindirectbuffer.Fill(array, 0, objectvertexes, 0, touse, -1))  // indirect 0 holds object draws, objectvertexes long, touse objects, estimate base instance on position
                        return pos;
                }
                else
                {                   // otherwise, horrible array copy because of lack of opentk interfaces
                    Vector4[] subset = new Vector4[touse];
                    Array.Copy(array, pos, subset,0,touse);
                    if (!dataindirectbuffer.Fill(subset, 0, objectvertexes, 0, touse, -1))
                        return pos;
                }

                dataindirectbuffer.Indirects[0].AddTag(tag);            // indirect draw buffer 0 holds the tags assigned by the user for identity purposes

                Matrix4[] matrix = new Matrix4[touse];      // create the text bitmaps and the matrices
                for (int i = 0; i < touse; i++)
                {
                    int imgpos = textures[textmapinuse].DepthIndex + textmapinuse * 65536;      // bits 16+ has textmap
                    //System.Diagnostics.Debug.WriteLine($"Write Mat {pos} {pos + i}");
                    textures[textmapinuse].DrawText(text[pos+i] + ":" + textmapinuse, fnt, fore,back, -1, fmt, backscale);

                    Vector3 textpos = array[pos + i].Xyz + textoffset;
                    var mat = GLPLVertexShaderQuadTextureWithMatrixTranslation.CreateMatrix(textpos,
                                    size,
                                    rot,
                                    rotatetoviewer, rotateelevation,
                                    imagepos: imgpos);
                    matrix[i] = mat;
                }

                dataindirectbuffer.Vertex.AlignMat4();          // instancing counts in mat4 sizes (mat4 0 @0, mat4 1 @ 64 etc) so align to it
                if ( !dataindirectbuffer.Fill(matrix, 1, 4, 0, touse, -1) )     // indirect 1 holds text draws, 4 vertices per draw, touse objects, estimate base instance on position
                    return pos;

                objectrenderer.DrawCount = dataindirectbuffer.Indirects[0].Positions.Count;       // update draw count
                objectrenderer.IndirectBuffer = dataindirectbuffer.Indirects[0];                  // and buffer

                textrenderer.DrawCount = dataindirectbuffer.Indirects[1].Positions.Count;
                textrenderer.IndirectBuffer = dataindirectbuffer.Indirects[1];

                if (textures[textmapinuse].DepthLeftIndex == 0)                                 // out of bitmap space, next please!
                    textmapinuse++;

                pos += touse;

            } while (pos < array.Length);

            return -1;
        }

        public void Remove(Object tag)
        {
            for( int i = 0; i < dataindirectbuffer.Indirects[0].Tags.Count; i++ )
            {
                if (dataindirectbuffer.Indirects[0].Tags[i] == tag )
                {
                    System.Diagnostics.Debug.WriteLine($"Found tag at {i}");
                    dataindirectbuffer.Remove(i, 0);
                    dataindirectbuffer.Remove(i, 1);
                }
            }
        }

        public void Dispose()
        {
            robjects.Remove(objectrenderer);
            robjects.Remove(textrenderer);
            items.Dispose();
        }

    }
}
