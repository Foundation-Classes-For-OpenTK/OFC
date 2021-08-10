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
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Linq;

namespace OFC.GL4
{
    // Class holds a Buffer for a vertex, and an set of indirect buffer, both have a defined size
    // you can add updates to it and mark sections as not drawable

    public class GLVertexBufferIndirect: IDisposable
    {
        public GLBuffer Vertex { get; private set; }
        public List<GLBuffer> Indirects { get; private set; } = new List<GLBuffer>();
        private int indirectsize;
        private BufferUsageHint bufferusage;

        public GLVertexBufferIndirect(int vertsize, int indirectsize, bool std430 = false, BufferUsageHint bh = BufferUsageHint.StaticDraw)
        {
            this.indirectsize = indirectsize;
            this.bufferusage = bh;
            Vertex = new GLBuffer(vertsize, std430, bh);
        }

        // fill vertex buffer with vector4's, and write an indirect to indirectbuffer N
        // vectexcount = <0 use vertices length, else use vertex count
        // baseinstance = <0 use CurrentPos on vertex buffer to estimate instance number, else use this

        public bool Fill(Vector4[] vertices, int indirectbuffer, int vertexcount = -1, int vertexbaseindex = 0, int ic = 1, int baseinstance = -1)
        {
            CreateIndirect(indirectbuffer);

            if (Indirects[indirectbuffer].Left >= GLBuffer.WriteIndirectArrayStride && Vertex.LeftAfterAlign(GLBuffer.Vec4size) >= vertices.Length * GLBuffer.Vec4size)
            {
                Vertex.Fill(vertices);          // creates a position
                System.Diagnostics.Debug.WriteLine($"Vertex buf {Vertex.Positions.Last()} size {vertices.Length * GLBuffer.Vec4size}");
                vertexcount = vertexcount >= 0 ? vertexcount : vertices.Length;
                baseinstance = baseinstance >= 0 ? baseinstance : (Vertex.Positions.Last() / GLBuffer.Vec4size);

                int pos = Indirects[indirectbuffer].Positions.Count * GLBuffer.WriteIndirectArrayStride;
                Indirects[indirectbuffer].AddPosition(pos);
                Indirects[indirectbuffer].StartWrite(pos);
                Indirects[indirectbuffer].WriteIndirectArray(vertexcount, ic, vertexbaseindex, baseinstance);
                Indirects[indirectbuffer].StopReadWrite();
                return true;
            }
            else
                return false;
        }

        // fill vertex buffer with mat4's, and write an indirect to indirectbuffer N

        public bool Fill(Matrix4[] mats, int indirectbuffer, int vertexcount = -1, int vertexbaseindex = 0, int ic = 1, int baseinstance = -1)
        {
            CreateIndirect(indirectbuffer);

            if (Indirects[indirectbuffer].Left >= GLBuffer.WriteIndirectArrayStride && Vertex.LeftAfterAlign(GLBuffer.Vec4size) >= mats.Length * GLBuffer.Mat4size)
            {
                Vertex.Fill(mats);          // creates a position
                System.Diagnostics.Debug.WriteLine($"Vertex buf {Vertex.Positions.Last()} size {mats.Length * GLBuffer.Mat4size}");
                vertexcount = vertexcount >= 0 ? vertexcount : mats.Length;
                baseinstance = baseinstance >= 0 ? baseinstance : (Vertex.Positions.Last() / GLBuffer.Mat4size);

                int pos = Indirects[indirectbuffer].Positions.Count * GLBuffer.WriteIndirectArrayStride;
                Indirects[indirectbuffer].AddPosition(pos);
                Indirects[indirectbuffer].StartWrite(pos);
                Indirects[indirectbuffer].WriteIndirectArray(vertexcount, ic, vertexbaseindex, baseinstance);
                Indirects[indirectbuffer].StopReadWrite();
                return true;
            }
            else
                return false;
        }

        private void CreateIndirect(int indirectbuffer)
        {
            while (Indirects.Count < indirectbuffer + 1)
                Indirects.Add(new GLBuffer(indirectsize, true, bufferusage));
        }

        public void Dispose()
        {
            Vertex.Dispose();
            foreach (var d in Indirects)
                d.Dispose();
        }
    }
}

