﻿/*
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
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Linq;

namespace OFC.GL4
{
    // Class holds a buffer for a vertex, and an set of indirect buffers, both have a defined size
    // you can add updates to it and mark sections by tag as not to be drawn

    public class GLVertexBufferIndirect
    {
        public GLBuffer Vertex { get; private set; }
        public List<GLBuffer> Indirects { get; private set; } = new List<GLBuffer>();

        private int indirectsize;
        private BufferUsageHint bufferusage;
        private GLItemsList items;

        public GLVertexBufferIndirect(GLItemsList items, int vertsize, int indirectsize, bool std430 = false, BufferUsageHint bh = BufferUsageHint.StaticDraw)
        {
            this.items = items;
            this.indirectsize = indirectsize;
            this.bufferusage = bh;
            Vertex = new GLBuffer(vertsize, std430, bh);
            items.Add(Vertex);
        }

        public bool EnoughSpaceVertex(int length, int indirectbuffer)
        {
            return Indirects[indirectbuffer].Left >= GLBuffer.WriteIndirectArrayStride && Vertex.LeftAfterAlign(GLBuffer.Vec4size) >= length * GLBuffer.Vec4size;
        }

        public bool EnoughSpaceMatrix(int length, int indirectbuffer)
        {
            return Indirects[indirectbuffer].Left >= GLBuffer.WriteIndirectArrayStride && Vertex.LeftAfterAlign(GLBuffer.Mat4size) >= length * GLBuffer.Mat4size;
        }

        // fill vertex buffer with vector4's, and write an indirect to indirectbuffer N
        
        public bool Fill(Vector4[] vertices, int sourceoffset, int sourcelength, 
                                    int indirectbuffer, 
                                    int vertexcount = -1,           // vectexcount = <0 use vertices length, else use vertex count
                                    int vertexbaseindex = 0,        // normally 0, index into vertex  buffer in vec4
                                    int ic = 1,                     // number of items to instance
                                    int baseinstance = -1)          // baseinstance, <0 use CurrentPos on vertex buffer to estimate instance number, else use this
        {
            CreateIndirect(indirectbuffer);

            if (EnoughSpaceVertex(vertices.Length, indirectbuffer))
            {
                Vertex.Fill(vertices, sourceoffset, sourcelength);          // creates a position
                //Vertex.Fill(vertices);          // creates a position
                //    System.Diagnostics.Debug.WriteLine($"Vertex buf {Vertex.Positions.Last()} size {vertices.Length * GLBuffer.Vec4size}");
                vertexcount = vertexcount >= 0 ? vertexcount : vertices.Length;
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

        // fill vertex buffer with mat4's, and write an indirect to indirectbuffer N

        public bool Fill(Matrix4[] mats, int sourceoffset, int sourcelength, 
                                    int indirectbuffer, 
                                    int vertexcount = -1, int vertexbaseindex = 0, int ic = 1, int baseinstance = -1)
        {
            CreateIndirect(indirectbuffer);

            if (EnoughSpaceMatrix(mats.Length, indirectbuffer))
            {
                Vertex.Fill(mats, sourceoffset, sourcelength);          // creates a position
                // System.Diagnostics.Debug.WriteLine($"Matrix buf {Vertex.Positions.Last()} size {mats.Length * GLBuffer.Mat4size}");
                vertexcount = vertexcount >= 0 ? vertexcount : mats.Length;
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

        // remove draw of indirectnumber in indirectbuffer

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
    }
}
