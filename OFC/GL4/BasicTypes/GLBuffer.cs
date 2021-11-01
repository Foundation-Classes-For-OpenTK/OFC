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
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace GLOFC.GL4
{
    // local data block, writable in Vectors etc, supports std140 and std430
    // you can use Allocate then Fill direct
    // or you can use the GL Mapping function which maps the buffer into memory

    [System.Diagnostics.DebuggerDisplay("Id {Id} Length {Length}")]
    public class GLBuffer : GLLayoutStandards, IDisposable
    {
        public int Id { get; private set; } = -1;

        public GLBuffer(bool std430 = false) : base(std430)
        {
            GL.CreateBuffers(1, out int id);     // this actually makes the buffer, GenBuffer does not - just gets a name
            GLStatics.RegisterAllocation(typeof(GLBuffer));
            GLStatics.Check();
            Id = id;
        }

        public GLBuffer(int allocatesize, bool std430 = false, BufferUsageHint bh = BufferUsageHint.StaticDraw) : this(std430)
        {
            AllocateBytes(allocatesize, bh);
        }

        #region Allocate/Resize/Copy

        public void AllocateBytes(int bytessize, BufferUsageHint uh = BufferUsageHint.StaticDraw)  // call first to set buffer size.. allow for alignment in your size
        {
            if (bytessize > 0)                                               // can call twice - get fresh buffer each time
            {
                Length = bytessize;
                GL.NamedBufferData(Id, Length, (IntPtr)0, uh);               // set buffer size
                var err = GL.GetError();
                System.Diagnostics.Debug.Assert(err == ErrorCode.NoError, $"GL NamedBuffer error {err}");        // check for any errors, always.
                ResetPositions();
            }
        }

        // note this results in a new GL Buffer, so any VertexArrays will need remaking
        // also note the Positions are maintained, you may want to delete those manually.
        public void Resize(int newlength, BufferUsageHint uh = BufferUsageHint.StaticDraw)      // newlength can be zero, meaning discard and go back to start
        {
            if (Length != newlength)
            {
                GL.CreateBuffers(1, out int newid);
                if (newlength > 0)
                {
                    GL.NamedBufferData(newid, newlength, (IntPtr)0, uh);               // set buffer size
                    var err = GL.GetError();
                    System.Diagnostics.Debug.Assert(err == ErrorCode.NoError, $"GL NamedBuffer error {err}");        // check for any errors, always.
                    if (Length > 0)                                                    // if previous buffer had data
                        GL.CopyNamedBufferSubData(Id, newid, (IntPtr)0, (IntPtr)0, Math.Min(Length, newlength));
                }

                GL.DeleteBuffer(Id);        // delete old buffer

                Id = newid;                 // swap to new
                Length = newlength;
            }
        }

        // Other buffer must be Allocated to the size otherpos+length
        public void CopyTo(GLBuffer other, int pos, int otherpos, int length, BufferUsageHint uh = BufferUsageHint.StaticDraw)      // newlength can be zero, meaning discard and go back to start
        {
            int ourend = pos + length;
            int otherend = otherpos + length;
            System.Diagnostics.Debug.Assert(Length >= ourend && other.Length >= otherend);
            GL.CopyNamedBufferSubData(Id, other.Id, (IntPtr)pos, (IntPtr)otherpos, length);
        }

        public void Zero(int pos, int length)
        {
            System.Diagnostics.Debug.Assert(Length != 0 && pos >= 0 && length <= Length && pos + length <= Length);
            GL.ClearNamedBufferSubData(Id, PixelInternalFormat.R32ui, (IntPtr)pos, length, PixelFormat.RedInteger, PixelType.UnsignedInt, (IntPtr)0);
            GLStatics.Check();
        }

        public void ZeroBuffer()    // zero whole of buffer, clear positions
        {
            Zero(0, Length);
            ResetPositions();
        }

        #endregion

        #region Fill - all perform alignment

        // length = -1 use floats.length, else take a subset starting at zero index
        public void Fill(float[] floats, int length = -1)
        {
            length = (length == -1) ? floats.Length : length;
            if (length > 0)
            {
                int datasize = length * sizeof(float);
                int posv = AlignArray(sizeof(float), datasize);
                GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, floats);
                GLStatics.Check();
            }
        }

        public void AllocateFill(float[] vertices)
        {
            AllocateBytes(sizeof(float) * vertices.Length);
            Fill(vertices);
        }

        public void Fill(Vector2[] vertices, int length = -1)
        {
            length = (length == -1) ? vertices.Length : length;
            int datasize = length * Vec2size;
            int posv = AlignArray(Vec2size, datasize);
            if (vertices.Length > 0)
            {
                GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, vertices);
                GLStatics.Check();
            }
        }

        public void AllocateFill(Vector2[] vertices)
        {
            AllocateBytes(Vec2size * vertices.Length);
            Fill(vertices);
        }

        // no Vector3 on purpose, they don't work well with opengl

        public void Fill(Vector4[] vertices, int length = -1)
        {
            length = (length == -1) ? vertices.Length : length;
            int datasize = length * Vec4size;
            int posv = AlignArray(Vec4size, datasize);

            if (vertices.Length > 0)
            {
                GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, vertices);
            }
        }

        // allowing a subset of the vertices to be filled
        public void Fill(Vector4[] vertices, int sourceoffset, int sourcelength)
        {
            if (sourcelength > 0)
            {
                int datasize = sourcelength * Vec4size;
                int posv = AlignArray(Vec4size, datasize);

                if (sourceoffset == 0)        // if from beginning of buffer, then its just copy datasize from the beginning
                {
                    GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, vertices);
                }
                else
                {                             // otherwise, annoyingly, a copy is needed
                    Vector4[] subset = new Vector4[sourcelength];
                    Array.Copy(vertices, sourceoffset, subset, 0, sourcelength);
                    GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, subset);
                }
                GLStatics.Check();
            }
        }

        public void AllocateFill(Vector4[] vertices)
        {
            AllocateBytes(Vec4size * vertices.Length);
            Fill(vertices);
        }

        public void AllocateFill(Vector4[] vertices, Vector2[] tex)
        {
            AllocateBytes(Vec4size * vertices.Length + Vec2size * tex.Length);
            Fill(vertices);
            Fill(tex);
        }

        public void Fill(Matrix4[] mats, int length = -1)
        {
            length = (length == -1) ? mats.Length : length;
            int datasize = length * Mat4size;
            int posv = AlignArray(Vec4size, datasize);
            if (mats.Length > 0)
            {
                GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, mats);
                GLStatics.Check();
            }
        }

        // allowing a subset of the matrices to be filled
        public void Fill(Matrix4[] mats, int sourceoffset, int sourcelength)        
        {
            if (sourcelength > 0)
            {
                int datasize = sourcelength * Mat4size;
                int posv = AlignArray(Vec4size, datasize);

                if (sourceoffset == 0)        // if at start
                {
                    GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, mats);
                }
                else
                {
                    Matrix4[] subset = new Matrix4[sourcelength];
                    Array.Copy(mats, sourceoffset, subset, 0, sourcelength);    // copy seems to be the quickest solution - at least its inside the system
                    GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, subset);
                }
                GLStatics.Check();
            }
        }

        public void AllocateFill(Matrix4[] mats)
        {
            AllocateBytes(Mat4size * mats.Length);
            Fill(mats);
        }

        public void Fill(OpenTK.Graphics.Color4[] colours, int length = -1)        // entries can say repeat colours until filled to entries..
        {
            if (length == -1)
                length = colours.Length;

            int datasize = length * Vec4size;
            int posc = AlignArray(Vec4size, datasize);

            int colstogo = length;
            int colp = posc;

            while (colstogo > 0)   // while more to fill in
            {
                int take = Math.Min(colstogo, colours.Length);      // max of colstogo and length of array
                GL.NamedBufferSubData(Id, (IntPtr)colp, 16 * take, colours);
                colstogo -= take;
                colp += take * 16;
            }
            GLStatics.Check();
        }

        public void Fill(ushort[] words, int length = -1)
        {
            length = (length == -1) ? words.Length : length;
            int datasize = length * sizeof(ushort);
            int posv = AlignArray(sizeof(ushort), datasize);
            if (words.Length > 0)
            {
                GL.NamedBufferSubData(Id, (IntPtr)posv, datasize, words);
                GLStatics.Check();
            }
        }

        public void AllocateFill(ushort[] words)
        {
            AllocateBytes(sizeof(ushort) * words.Length);
            Fill(words);
        }

        public void Fill(uint[] data, int length = -1)
        {
            length = (length == -1) ? data.Length : length;
            int datasize = length * sizeof(uint);
            int pos = AlignArray(sizeof(uint), datasize);
            if (data.Length > 0)
            {
                GL.NamedBufferSubData(Id, (IntPtr)pos, datasize, data);
                GLStatics.Check();
            }
        }

        public void AllocateFill(uint[] data)
        {
            AllocateBytes(sizeof(uint) * data.Length);
            Fill(data);
        }

        public void Fill(byte[] data, int length = -1)      
        {
            length = (length == -1) ? data.Length : length;
            int datasize = length;
            int pos = AlignArray(sizeof(byte), datasize);     
            if (data.Length > 0)
            {
                GL.NamedBufferSubData(Id, (IntPtr)pos, datasize, data);
                GLStatics.Check();
            }
        }

        public void AllocateFill(byte[] data)
        {
            AllocateBytes(data.Length);
            Fill(data);
        }

        public void FillPacked2vec(Vector3[] vertices, Vector3 offsets, float mult)
        {
            int p = 0;                                                                  // probably change to write directly into buffer..
            uint[] packeddata = new uint[vertices.Length * 2];
            for (int i = 0; i < vertices.Length; i++)
            {
                uint z = (uint)((vertices[i].Z + offsets.Z) * mult);
                packeddata[p++] = (uint)((vertices[i].X + offsets.X) * mult) | ((z & 0x7ff) << 21);
                packeddata[p++] = (uint)((vertices[i].Y + offsets.Y) * mult) | (((z >> 11) & 0x7ff) << 21);
            }

            Fill(packeddata);
        }

        public void FillRectangularIndicesBytes(int reccount, uint restartindex = 0xff)        // rectangular indicies with restart
        {
            AllocateBytes(reccount * 5);
            StartWrite(0, Length);
            for (int r = 0; r < reccount; r++)
            {
                byte[] ar = new byte[] { (byte)(r * 4), (byte)(r * 4 + 1), (byte)(r * 4 + 2), (byte)(r * 4 + 3), (byte)restartindex };
                Write(ar);
            }

            StopReadWrite();
        }

        public void FillRectangularIndicesShort(int reccount, uint restartindex = 0xffff)        // rectangular indicies with restart
        {
            AllocateBytes(reccount * 5 * sizeof(short));     // lets use short because we don't have a marshall copy ushort.. ignore the overflow
            StartWrite(0, Length);
            for (int r = 0; r < reccount; r++)
            {
                short[] ar = new short[] { (short)(r * 4), (short)(r * 4 + 1), (short)(r * 4 + 2), (short)(r * 4 + 3), (short)restartindex };
                Write(ar);
            }

            StopReadWrite();
        }

        #endregion

        #region Map Read/Write Common

        private enum MapMode { None, Write, Read};
        private MapMode mapmode = MapMode.None;
        
        // allocate and start write on buffer
        public void AllocateStartWrite(int datasize)        
        {
            AllocateBytes(datasize);
            StartWrite(0,datasize);
        }

        // update the buffer with an area of updated cache on a write.. (datasize=0=all buffer).  
        // Default is bam is to wipe the mapped area. Use 0 in bam not to do this. trap for young players here
        public void StartWrite(int fillpos, int datasize = 0, BufferAccessMask bam = BufferAccessMask.MapInvalidateRangeBit)
        {
            if (datasize == 0)
                datasize = Length - fillpos;

            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && fillpos >= 0 && fillpos + datasize <= Length); // catch double maps

            CurrentPtr = GL.MapNamedBufferRange(Id, (IntPtr)fillpos, datasize, BufferAccessMask.MapWriteBit | bam);

            CurrentPos = fillpos;
            mapmode = MapMode.Write;
            GLStatics.Check();
        }

        public void StartRead(int fillpos, int datasize = 0)        // read the buffer (datasize=0=all buffer)
        {
            if (datasize == 0)
                datasize = Length - fillpos;

            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && fillpos >= 0 && fillpos + datasize <= Length); // catch double maps

            CurrentPtr = GL.MapNamedBufferRange(Id, (IntPtr)fillpos, datasize, BufferAccessMask.MapReadBit);

            CurrentPos = fillpos;
            mapmode = MapMode.Read;
            GLStatics.Check();
        }

        public void StopReadWrite()
        {
            GL.UnmapNamedBuffer(Id);
            mapmode = MapMode.None;
            GLStatics.Check();
        }

        public void Skip(int p)
        {
            System.Diagnostics.Debug.Assert(mapmode != MapMode.None);
            CurrentPtr += p;
            CurrentPos += p;
            System.Diagnostics.Debug.Assert(CurrentPos <= Length);
            GLStatics.Check();
        }

        // move currentpos onto the alignment

        public void AlignFloat()
        {
            AlignArrayPtr(sizeof(float), 0);
        }

        public void AlignVec4()
        {
            AlignArrayPtr(Vec4size, 0);
        }

        // std140 dictates mat4 are aligned on vec4 boundaries.  But if your using instance offsets inside a buffer (where your picking 0+instance*sizeofmat4), you may want to align to a Mat4.
        public void AlignMat4()
        {
            AlignArrayPtr(Mat4size, 0);
        }

        #endregion

        #region Write to map

        public void Write(Matrix4 mat, int repeat = 1)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            var p = AlignArrayPtr(Vec4size, 4);
            var r = mat.ToFloatArray();
            IntPtr ptr = p.Item1;

            while (repeat-- > 0)
            {
                System.Runtime.InteropServices.Marshal.Copy(r, 0, ptr, r.Length);          // number of units, not byte length!
                ptr += Mat4size;
            }
        }

        public void Write(Vector4 v4, int repeat = 1)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            var p = AlignArrayPtr(Vec4size, 1);
            float[] a = new float[] { v4.X, v4.Y, v4.Z, v4.W };
            IntPtr ptr = p.Item1;
            while (repeat-- > 0)
            {
                System.Runtime.InteropServices.Marshal.Copy(a, 0, ptr, a.Length);
                ptr += Vec4size;
            }
        }

        public void Write(System.Drawing.Rectangle r)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            var p = AlignArrayPtr(Vec4size, 1);
            float[] a = new float[] { r.Left, r.Top, r.Right, r.Bottom };
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, a.Length);          
        }

        public void Write(Vector3 mat, float vec4other)      // write vec3 as vec4.
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            var p = AlignArrayPtr(Vec4size, 1);
            float[] a = new float[] { mat.X, mat.Y, mat.Z, vec4other };
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, a.Length);          
        }

        public void Write(float v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            IntPtr pos = AlignScalarPtr(sizeof(float));
            float[] a = new float[] { v };
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        public void Write(float[] a, int length = -1)
        {
            length = (length == -1) ? a.Length : length;

            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            int size = sizeof(float);
            var p = AlignArrayPtr(size, length);
            if (p.Item2 == size)
                System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, length);
            else
            {
                var fa = new float[length * 4];
                for (int i = 0; i < length; i++)      // std140 'orrible
                    fa[i * 4] = a[i];
                System.Runtime.InteropServices.Marshal.Copy(fa, 0, p.Item1, fa.Length);       // number of units, not byte length!
            }
        }

        public void Write(float[] a, int length, int sourceoffset = 0)     // count in floats units, source offset in floats units
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            int size = sizeof(float);
            var p = AlignArrayPtr(size, length);
            if (p.Item2 == size)
                System.Runtime.InteropServices.Marshal.Copy(a, sourceoffset, p.Item1, length);
            else
            {
                var fa = new float[length * 4];
                for (int i = 0; i < length; i++)      // std140 'orrible
                    fa[i * 4] = a[i + sourceoffset];
                System.Runtime.InteropServices.Marshal.Copy(fa, 0, p.Item1, fa.Length);       // number of units, not byte length!
            }
        }

        public void Write(int offset, float[] a)                    // write to an arbitary offset from current pos, in bytes. CurrentPtr not changed
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            IntPtr p = CurrentPtr + offset;
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p, a.Length); // number of units
        }

        public void WriteCont(float[] a, int length = -1)     // without checking for alignment/stride
        {
            length = (length == -1) ? a.Length : length;
            System.Runtime.InteropServices.Marshal.Copy(a, 0, CurrentPtr, length);       // number of units, not byte length!
            CurrentPtr += sizeof(float) * length;
            CurrentPos += sizeof(float) * length;
        }

        public void Write(int v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            int[] a = new int[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(int));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        public void Write(int[] a, int length = -1)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);

            length = (length == -1) ? a.Length : length;
            int size = sizeof(int);
            var p = AlignArrayPtr(size, length);
            if (p.Item2 == size)
                System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, length);       // number of units, not byte length!
            else
            {
                var fa = new int[length * 4];
                for (int i = 0; i < length; i++)      // std140 'orrible
                    fa[i * 4] = a[i];
                System.Runtime.InteropServices.Marshal.Copy(fa, 0, p.Item1, fa.Length);       // number of units, not byte length!
            }
        }

        public void Write(short v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            short[] a = new short[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(short));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        public void Write(short[] a, int length = -1)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            length = (length == -1) ? a.Length : length;
            int size = sizeof(short);
            var p = AlignArrayPtr(size, length);
            if (p.Item2 == size)
                System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, length);       // number of units, not byte length!
            else
            {
                var fa = new short[length * 4];
                for (int i = 0; i < length; i++)      // std140 'orrible
                    fa[i * 4] = a[i];
                System.Runtime.InteropServices.Marshal.Copy(fa, 0, p.Item1, fa.Length);       // number of units, not byte length!
            }
        }

        public void Write(long v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            long[] a = new long[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(long));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        public void Write(long[] a, int length = -1)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            length = (length == -1) ? a.Length : length;
            int size = sizeof(long);
            var p = AlignArrayPtr(size, length);
            if (p.Item2 == size)
                System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, length);       // number of units, not byte length!
            else
            {
                var fa = new long[length * 4];
                for (int i = 0; i < length; i++)      // std140 'orrible
                    fa[i * 4] = a[i];
                System.Runtime.InteropServices.Marshal.Copy(fa, 0, p.Item1, fa.Length);       // number of units, not byte length!
            }
        }

        public void Write(byte v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            byte[] a = new byte[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(byte));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        public void Write(byte[] a, int length = -1 )     // special , not aligned, as not normal glsl type.  Used mostly for element indexes
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            length = (length == -1) ? a.Length : length;
            var p = AlignArrayPtr(sizeof(byte), length);
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, length);
        }

        public const int WriteIndirectArrayStride = 16;

        // write an Indirect Array draw command to the buffer
        // if you use it, MultiDrawCountStride = 16
        public void WriteIndirectArray(int vertexcount, int instancecount = 1, int firstvertex = 0, int baseinstance = 0)
        {
            int[] i = new int[] { vertexcount, instancecount, firstvertex, baseinstance };
           // System.Diagnostics.Debug.WriteLine($"WIA vc {i[0]} ic {i[1]} fv {i[2]} bi {i[3]}");
            Write(i);
        }

        public const int WriteIndirectElementsStride = 16;

        // write an Indirect Element draw command to the buffer
        // if you use it, MultiDrawCountStride = 20
        public void WriteIndirectElements(int vertexcount, int instancecount = 1, int firstindex = 0, int basevertex = 0,int baseinstance = 0)
        {
            int[] i = new int[] { vertexcount, instancecount, firstindex, basevertex, baseinstance };
            Write(i);
        }


        #endregion

        #region Reads - Map into memory and then read

        public byte[] ReadBytes(int size)                                               // read into a byte array. not aligned 
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            byte[] data = new byte[size];
            var p = AlignArrayPtr(sizeof(byte), size);
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, size);
            return data;
        }

        public int[] ReadInts(int count, bool ignorestd130 = false)                    // read into a array, use ignorestd130 array spacing if required.
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            int size = sizeof(int);
            var data = new int[count];
            var p = AlignArrayPtr(size, count);
            if (p.Item2 == size || ignorestd130 )
                System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, data.Length);
            else
            {
                var temp = new int[count * 4];
                System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, temp.Length);
                for (int i = 0; i < data.Length; i++)
                    data[i] = temp[i * 4];
            }
            return data;
        }

        public int ReadInt()
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            int[] data = new int[1];
            IntPtr pos = AlignScalarPtr(sizeof(int));
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 1);
            return data[0];
        }

        public long[] ReadLongs(int count, bool ignorestd130 = false)                    
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            int size = sizeof(long);
            var data = new long[count];
            var p = AlignArrayPtr(size, count);
            if (p.Item2 == size || ignorestd130)
                System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, data.Length);
            else
            {
                var temp = new long[count * 4];
                System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, temp.Length);
                for (int i = 0; i < data.Length; i++)
                    data[i] = temp[i * 4];
            }
            return data;
        }

        public long ReadLong()
        {
            var data = new long[1];
            IntPtr pos = AlignScalarPtr(sizeof(long));
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 1);
            return data[0];
        }

        public float[] ReadFloats(int count, bool ignorestd130 = false)                    
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            int size = sizeof(float);
            var data = new float[count];
            var p = AlignArrayPtr(size, count);
            if (p.Item2 == size || ignorestd130)            
                System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, data.Length); // read tight array
            else
            {                                                   // read sparse array
                var temp = new float[count*4];
                System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, temp.Length);
                for (int i = 0; i < data.Length; i++)
                    data[i] = temp[i * 4];
            }
            return data;
        }

        public float ReadFloat()
        {
            var data = new float[1];
            IntPtr pos = AlignScalarPtr(sizeof(float));
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 1);
            return data[0];
        }

        public Vector2[] ReadVector2s(int count)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            var p = AlignArrayPtr(Vec2size, count);
            float[] fdata = new float[count * 2];       // aligned 2s
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, fdata, 0, count * 2);
            Vector2[] data = new Vector2[count];
            for (int i = 0; i < count; i++)
                data[i] = new Vector2(fdata[i * 2], fdata[i * 2 + 1]);
            return data;
        }

        public Vector3[] ReadVector3s(int count)        // normal alignment of vector3 is on vector4 boundaries
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            var p = AlignArrayPtr(Vec3size, count);
            float[] fdata = new float[count * 4];       // aligned 4s
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, fdata, 0, count * 4);
            Vector3[] data = new Vector3[count];
            for (int i = 0; i < count; i++)
                data[i] = new Vector3(fdata[i * 4], fdata[i * 4 + 1], fdata[i * 4 + 2]);
            return data;
        }

        public Vector3[] ReadVector3sPacked(int count)      // varyings seem to write vector3 tightly packed
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            var p = AlignArrayPtr(1, count);                // no aligned
            float[] fdata = new float[count * 3];   
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, fdata, 0, count * 3);
            Vector3[] data = new Vector3[count];
            for (int i = 0; i < count; i++)
                data[i] = new Vector3(fdata[i * 3], fdata[i * 3 + 1], fdata[i * 3 + 2]);
            return data;
        }

        public Vector4[] ReadVector4s(int count)                    
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            var p = AlignArrayPtr(Vec4size, count);
            float[] fdata = new float[count * 4];
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, fdata, 0, count * 4);
            Vector4[] data = new Vector4[count];
            for (int i = 0; i < count; i++)
                data[i] = new Vector4(fdata[i * 4], fdata[i * 4 + 1], fdata[i * 4 + 2], fdata[i * 4 + 3]);
            return data;
        }

        public Vector4 ReadVector4()
        {
            var data = new float[4];
            IntPtr pos = AlignScalarPtr(Vec4size);
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 4);
            return new Vector4(data[0], data[1], data[2], data[3]);
        }

        public Matrix4 ReadMatrix4()
        {
            var data = new float[16];
            var p = AlignArrayPtr(Vec4size, 4);
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, 16);
            return new Matrix4(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7],
                                data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15]);
        }

        public Matrix4[] ReadMatrix4s(int count)
        {
            var p = AlignArrayPtr(Vec4size, 4);
            var fdata = new float[16*count];
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, fdata, 0, fdata.Length);
            Matrix4[] data = new Matrix4[count];
            for (int i = 0; i < fdata.Length; i += 16)
            {
                data[i/16] = new Matrix4(fdata[i], fdata[i + 1], fdata[i + 2], fdata[i + 3],
                                        fdata[i + 4], fdata[i + 5], fdata[i + 6], fdata[i + 7],
                                        fdata[i + 8], fdata[i + 9], fdata[i + 10], fdata[i + 11],
                                        fdata[i + 12], fdata[i + 13], fdata[i + 14], fdata[i + 15]);
            }

            return data;
        }

        #endregion

        #region Fast Map and Read functions

        public byte[] ReadBuffer(int offset, int len)
        {
            StartRead(offset,len);
            var v = ReadBytes(len);
            StopReadWrite();
            return v;
        }

        public int ReadInt(int offset)
        {
            StartRead(offset);
            int v = ReadInt();
            StopReadWrite();
            return v;
        }

        public int[] ReadInts(int offset, int number, bool ignorestd130 = false)
        {
            StartRead(offset);
            var v = ReadInts(number, ignorestd130);
            StopReadWrite();
            return v;
        }

        public long[] ReadLongs(int offset, int number, bool ignorestd130 = false)
        {
            StartRead(offset);
            var v = ReadLongs(number, ignorestd130);
            StopReadWrite();
            return v;
        }

        public float[] ReadFloats(int offset, int number, bool ignorestd130 = false)
        {
            StartRead(offset);
            var v = ReadFloats(number, ignorestd130);
            StopReadWrite();
            return v;
        }

        public Vector2[] ReadVector2s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadVector2s(number);
            StopReadWrite();
            return v;
        }

        public Vector3[] ReadVector3s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadVector3s(number);
            StopReadWrite();
            return v;
        }

        public Vector3[] ReadVector3sPacked(int offset, int number) // varyings seem to write vector3 packed
        {
            StartRead(offset);
            var v = ReadVector3sPacked(number);
            StopReadWrite();
            return v;
        }

        public Vector4[] ReadVector4s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadVector4s(number);
            StopReadWrite();
            return v;
        }

        public Matrix4[] ReadMatrix4s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadMatrix4s(number);
            StopReadWrite();
            return v;
        }

        #endregion

        #region Binding a buffer to target 

        public void Bind(GLVertexArray va, int bindingindex, int start, int stride, int divisor = 0)      // set buffer binding to a VA
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None);     // catch unmap missing. Since binding to VA can be done before buffer is full, then don't check BufferSize
            va.Bind();
            GL.BindVertexBuffer(bindingindex, Id, (IntPtr)start, stride);      // this buffer to binding index
            GL.VertexBindingDivisor(bindingindex, divisor);
            GLStatics.Check();
            //System.Diagnostics.Debug.WriteLine("BUFBIND " + bindingindex + " To B" + Id + " pos " + start + " stride " + stride + " divisor " + divisor);
        }

        private static int elementbindindex = -1;       // static across renders and programs, like uniform buffers, so no need to keep on rebinding

        public void BindElement()
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            //if (elementbindindex != Id) // removed for testing
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Id);
                GLStatics.Check();
                elementbindindex = Id;
            }
        }

        private static int indirectbindindex = -1;
        public void BindIndirect()
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            //if (indirectbindindex != Id) // removed for testing
            {
                GL.BindBuffer(BufferTarget.DrawIndirectBuffer, Id);
                GLStatics.Check();
                indirectbindindex = Id;
            }
        }

        private static int parameterbindindex = -1;
        public void BindParameter()
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            // if (parameterbindindex != Id) // removed for testing
            {
                GL.BindBuffer((BufferTarget)0x80ee, Id);        // fudge due to ID not being there in 3.3.2
                GLStatics.Check();
                parameterbindindex = Id;
            }
        }

        // Bind to the default (xfb=0) or specific transform feedback buffer object

        public void BindTransformFeedback(int index, int xfb = 0, int offset = 0, int size = -1)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            if ( size == -1 )
                GL.TransformFeedbackBufferBase(xfb, index, Id);
            else
                GL.TransformFeedbackBufferRange(xfb, index, Id, (IntPtr)offset, size);
            GLStatics.Check();
        }

        // unbind from the default (xfb=0) or specific transform feedback buffer object
        static public void UnbindTransformFeedback(int index, int xfb = 0)
        {
            GL.TransformFeedbackBufferBase(xfb, index, 0); // 0 is the unbind value
        }

        private static int querybindindex = -1;
        public void BindQuery()
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            if (querybindindex != Id)
            {
                GL.BindBuffer(BufferTarget.QueryBuffer, Id);
                GLStatics.Check();
                querybindindex = Id;
            }
        }

        static public void UnbindQuery()
        {
            if (querybindindex != -1)
            {
                GL.BindBuffer(BufferTarget.QueryBuffer, 0); // 0 is the unbind value
                GLStatics.Check();
                querybindindex = -1;
            }
        }


        public void Bind(int bindingindex,  BufferRangeTarget tgr)                           // Bind to a arbitary buffer target
        {
            GL.BindBufferBase(tgr, bindingindex, Id);       // binding point set to tgr
        }

        #endregion

        #region Implementation

        public void Dispose()           // you can double dispose.
        {
            if (Id != -1)
            {
                GL.DeleteBuffer(Id);
                GLStatics.RegisterDeallocation(typeof(GLBuffer));
                Id = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }

        #endregion
    }
}