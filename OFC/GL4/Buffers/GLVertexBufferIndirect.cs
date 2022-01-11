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
 
using OpenTK;
using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Linq;

namespace GLOFC.GL4.Buffers
{
    /// <summary>
    /// Class holds a buffer for a vertex, and an set of indirect buffers, both have a defined size 
    /// you can add updates to it and remove sections by index
    /// </summary>

    public class GLVertexBufferIndirect
    {
        /// <summary> Buffer holding vertex</summary>
        public GLBuffer Vertex { get; private set; }
        /// <summary> List of indirect buffers created</summary>
        public List<GLBuffer> Indirects { get; private set; } = new List<GLBuffer>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="items">Item list to store buffers into</param>
        /// <param name="vertsize">Size of vertex buffer</param>
        /// <param name="indirectsize">Size of indirect buffer</param>
        /// <param name="std430">Std430 layout</param>
        /// <param name="bufferusagehint">Buffer usage hint</param>
        public GLVertexBufferIndirect(GLItemsList items, int vertsize, int indirectsize, bool std430 = false, BufferUsageHint bufferusagehint = BufferUsageHint.StaticDraw)
        {
            this.items = items;
            this.indirectsize = indirectsize;
            this.bufferusage = bufferusagehint;
            this.context = GLStatics.GetContext();
            Vertex = new GLBuffer(vertsize, std430, bufferusagehint);
            items.Add(Vertex);
        }

        /// <summary>
        /// Fill vertex buffer with vector4's, and write an indirect to indirectbuffer N 
        /// </summary>
        /// <param name="vertices">Array of vertices to store</param>
        /// <param name="sourceoffset">Start position in array</param>
        /// <param name="sourcelength">Length of store</param>
        /// <param name="indirectbuffer">Which indirect buffer to store indexes into</param>
        /// <param name="vertexcount">If greater or equal to zero, use this count for indirects, else user source length</param>
        /// <param name="vertexbaseindex">Vertex base index for indirect</param>
        /// <param name="ic">Instance count for indirect</param>
        /// <param name="baseinstance">If greater or equal to zero, use this, else estimate the base instance number based on vertex position. </param>        
        /// <returns>True if filled</returns>
        public bool Fill(Vector4[] vertices, int sourceoffset, int sourcelength, 
                                    int indirectbuffer, 
                                    int vertexcount = -1,          
                                    int vertexbaseindex = 0,      
                                    int ic = 1,                     
                                    int baseinstance = -1)          // baseinstance, <0 use CurrentPos on vertex buffer to estimate instance number, else use this
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");
            CreateIndirect(indirectbuffer);

            if (EnoughSpaceVertex(sourcelength, indirectbuffer))
            {
                Vertex.Fill(vertices, sourceoffset, sourcelength);          // creates a position
                //Vertex.Fill(vertices);          // creates a position
                //    System.Diagnostics.Debug.WriteLine($"Vertex buf {Vertex.Positions.Last()} size {vertices.Length * GLBuffer.Vec4size}");
                vertexcount = vertexcount >= 0 ? vertexcount : sourcelength;
                baseinstance = baseinstance >= 0 ? baseinstance : (Vertex.Positions.Last() / GLBuffer.Vec4size);

                int pos = Indirects[indirectbuffer].Positions.Count * GLBuffer.WriteIndirectArrayStride;
                Indirects[indirectbuffer].AddPosition(pos);
                Indirects[indirectbuffer].StartWrite(pos, GLBuffer.WriteIndirectArrayStride);
                Indirects[indirectbuffer].WriteIndirectArray(vertexcount, ic, vertexbaseindex, baseinstance);
                Indirects[indirectbuffer].StopReadWrite();
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Fill vertex buffer with Matrix4's, and write an indirect to indirectbuffer N 
        /// </summary>
        /// <param name="mats">Array of matrices to store</param>
        /// <param name="sourceoffset">Start position in array</param>
        /// <param name="sourcelength">Length of store</param>
        /// <param name="indirectbuffer">Which indirect buffer to store indexes into</param>
        /// <param name="vertexcount">If greater or equal to zero, use this count for indirects, else user source length</param>
        /// <param name="vertexbaseindex">Vertex base index for indirect</param>
        /// <param name="ic">Instance count for indirect</param>
        /// <param name="baseinstance">If greater or equal to zero, use this, else estimate the base instance number based on vertex position. </param>        
        /// <returns>True if filled</returns>
        public bool Fill(Matrix4[] mats, int sourceoffset, int sourcelength, 
                                    int indirectbuffer, 
                                    int vertexcount = -1, int vertexbaseindex = 0, int ic = 1, int baseinstance = -1)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");
            CreateIndirect(indirectbuffer);

            if (EnoughSpaceMatrix(sourcelength, indirectbuffer))
            {
                Vertex.Fill(mats, sourceoffset, sourcelength);          // creates a position
                // System.Diagnostics.Debug.WriteLine($"Matrix buf {Vertex.Positions.Last()} size {mats.Length * GLBuffer.Mat4size}");
                vertexcount = vertexcount >= 0 ? vertexcount : sourcelength;
                baseinstance = baseinstance >= 0 ? baseinstance : (Vertex.Positions.Last() / GLBuffer.Mat4size);

                int pos = Indirects[indirectbuffer].Positions.Count * GLBuffer.WriteIndirectArrayStride;
                Indirects[indirectbuffer].AddPosition(pos);
                Indirects[indirectbuffer].StartWrite(pos, GLBuffer.WriteIndirectArrayStride);
                Indirects[indirectbuffer].WriteIndirectArray(vertexcount, ic, vertexbaseindex, baseinstance);
                Indirects[indirectbuffer].StopReadWrite();
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Remove this indirect from this indirect buffer
        /// </summary>
        /// <param name="indirectnumber">The indirect number in the buffer</param>
        /// <param name="indirectbuffer">The indirect buffer number</param>
        /// <returns></returns>
        public bool Remove(int indirectnumber, int indirectbuffer)
        {
            if (indirectbuffer < Indirects.Count)
            {
                var ib = Indirects[indirectbuffer];
                if (indirectnumber < ib.Positions.Count)
                {
                    //System.Diagnostics.Debug.WriteLine($"Remove indirect {indirectbuffer} at {indirectnumber} pos {Indirects[indirectbuffer].Positions[indirectnumber]}");
                    Indirects[indirectbuffer].StartWrite(Indirects[indirectbuffer].Positions[indirectnumber], sizeof(int));
                    Indirects[indirectbuffer].Write((int)0);        // zero the vertex count
                    Indirects[indirectbuffer].StopReadWrite();
                    return true;
                }
            }

            return false;
        }

        private void CreateIndirect(int indirectbuffer)
        {
            while (Indirects.Count < indirectbuffer + 1)
            {
                var buf = new GLBuffer(indirectsize, true, bufferusage);
                items.Add(buf);
                Indirects.Add(buf);
            }
        }

        private bool EnoughSpaceVertex(int length, int indirectbuffer)
        {
            return Indirects[indirectbuffer].Left >= GLBuffer.WriteIndirectArrayStride && Vertex.LeftAfterAlign(GLBuffer.Vec4size) >= length * GLBuffer.Vec4size;
        }
        private bool EnoughSpaceMatrix(int length, int indirectbuffer)
        {
            return Indirects[indirectbuffer].Left >= GLBuffer.WriteIndirectArrayStride && Vertex.LeftAfterAlign(GLBuffer.Mat4size) >= length * GLBuffer.Mat4size;
        }

        private int indirectsize;
        private BufferUsageHint bufferusage;
        private GLItemsList items;
        private IntPtr context;
    }
}

