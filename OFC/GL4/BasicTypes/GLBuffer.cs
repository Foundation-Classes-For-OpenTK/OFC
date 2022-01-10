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
    ///<summary> Local data block, writable in Vectors etc, supports std140 and std430
    /// you can use Allocate then Fill direct
    /// or you can use the GL Mapping function which maps the buffer into memory </summary>

    [System.Diagnostics.DebuggerDisplay("Id {Id} Length {Length}")]
    public class GLBuffer : GLLayoutStandards, IDisposable
    {
        ///<summary>GL ID</summary>
        public int Id { get; private set; } = -1;
        private IntPtr context;

        ///<summary>Create an empty buffer of this standard, default is std130. Standard defines the layout of members of the buffer. See OpenGL</summary>
        public GLBuffer(bool std430 = false) : base(std430)
        {
            GL.CreateBuffers(1, out int id);     // this actually makes the buffer, GenBuffer does not - just gets a name
            GLStatics.RegisterAllocation(typeof(GLBuffer));
            GLStatics.Check();
            Id = id;
            context = GLStatics.GetContext();
        }

        ///<summary>Create a buffer of this size, with this standard and hint</summary>
        public GLBuffer(int allocatesize, bool std430 = false, BufferUsageHint hint = BufferUsageHint.StaticDraw) : this(std430)
        {
            AllocateBytes(allocatesize, hint);
        }

        #region Allocate/Resize/Copy

        ///<summary>Allocate or reallocate buffer size. Call first to set buffer size. Allow for alignment in your size</summary>
        public void AllocateBytes(int bytessize, BufferUsageHint hint = BufferUsageHint.StaticDraw)  
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            if (bytessize > 0)                                               // can call twice - get fresh buffer each time
            {
                Length = bytessize;
                GL.NamedBufferData(Id, Length, (IntPtr)0, hint);               // set buffer size
                var err = GL.GetError();
                System.Diagnostics.Debug.Assert(err == ErrorCode.NoError, $"GL NamedBuffer error {err}");        // check for any errors, always.
                ResetPositions();
            }
        }

        /// <summary>
        /// Resize the buffer. Note this results in a new GL Buffer, so any VertexArrays will need remaking. Also note the Positions are maintained, you may want to delete those manually.
        /// </summary>
        public void Resize(int newlength, BufferUsageHint hint = BufferUsageHint.StaticDraw)      // newlength can be zero, meaning discard and go back to start
        {
            if (Length != newlength)
            {
                GL.CreateBuffers(1, out int newid);
                if (newlength > 0)
                {
                    GL.NamedBufferData(newid, newlength, (IntPtr)0, hint);               // set buffer size
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

        /// <summary>
        /// Copy to another buffer. Other buffer must be Allocated to the size otherpos+length
        /// </summary>
        /// <param name="other">Other buffer</param>
        /// <param name="startpos">Start posiiton in buffer</param>
        /// <param name="otherpos">Position to store it in other buffer</param>
        /// <param name="length">Copy length</param>
        /// <param name="hint"></param>
        public void CopyTo(GLBuffer other, int startpos, int otherpos, int length, BufferUsageHint hint = BufferUsageHint.StaticDraw)      // newlength can be zero, meaning discard and go back to start
        {
            int ourend = startpos + length;
            int otherend = otherpos + length;
            System.Diagnostics.Debug.Assert(Length >= ourend && other.Length >= otherend);
            GL.CopyNamedBufferSubData(Id, other.Id, (IntPtr)startpos, (IntPtr)otherpos, length);
        }

        /// <summary>Zero the buffer from this position and length</summary>
        public void Zero(int pos, int length)
        {
            System.Diagnostics.Debug.Assert(Length != 0 && pos >= 0 && length <= Length && pos + length <= Length);
            GL.ClearNamedBufferSubData(Id, PixelInternalFormat.R32ui, (IntPtr)pos, length, PixelFormat.RedInteger, PixelType.UnsignedInt, (IntPtr)0);
            GLStatics.Check();
        }

        /// <summary>Zero the buffer</summary>
        public void ZeroBuffer()    // zero whole of buffer, clear positions
        {
            Zero(0, Length);
            ResetPositions();
        }

        #endregion

        #region Fill - all perform alignment

        /// <summary>Reset the fill position to this value</summary>
        public void ResetFillPos(int pos = 0)       // call to reset the current fill position to this value, then you can refill
        {
            CurrentPos = pos;
        }

        /// <summary>Fill with floats. length = -1 use floats.length, else take a subset starting at zero index </summary>
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

        /// <summary>Fill with Matrix Array</summary>
        public void Fill(GLMatrixArray a)
        {
            Fill(a.MatrixArray, a.Count * 16);
        }

        /// <summary>Allocate and fill with floats.</summary>
        public void AllocateFill(float[] vertices)
        {
            AllocateBytes(sizeof(float) * vertices.Length);
            Fill(vertices);
        }

        /// <summary>Fill with vector2s, with a definable length.</summary>
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

        /// <summary>Allocate and fill with vector2s.</summary>
        public void AllocateFill(Vector2[] vertices)
        {
            AllocateBytes(Vec2size * vertices.Length);
            Fill(vertices);
        }

        /// <summary>Allocate and fill with vector4s, with a definable length.</summary>
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

        /// <summary>Allocate and fill with Vector4s, allowing a subset of the vertices to be filled</summary>
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

        /// <summary>Allocate and fill with Vector4s</summary>
        public void AllocateFill(Vector4[] vertices)
        {
            AllocateBytes(Vec4size * vertices.Length);
            Fill(vertices);
        }

        /// <summary>Allocate and fill with Vector4s and Vector2 text co-ords</summary>
        public void AllocateFill(Vector4[] vertices, Vector2[] tex)
        {
            AllocateBytes(Vec4size * vertices.Length + Vec2size * tex.Length);
            Fill(vertices);
            Fill(tex);
        }

        /// <summary>AFill with Matrix4s, with a definable length.</summary>
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

        /// <summary>Fill with Matrix4s, allowing a subset of the matrices to be filled</summary>
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

        /// <summary>Allocate and fill with Matrix4s</summary>
        public void AllocateFill(Matrix4[] mats)
        {
            AllocateBytes(Mat4size * mats.Length);
            Fill(mats);
        }

        /// <summary>Fill with colours, with a definable length. Length can be bigger than colours. Entries can say repeat colours until filled to entries..</summary>
        public void Fill(OpenTK.Graphics.Color4[] colours, int length = -1)     
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

        /// <summary>Fill with ushort, with a definable length.</summary>
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

        /// <summary>Allocate and fill with words.</summary>
        public void AllocateFill(ushort[] words)
        {
            AllocateBytes(sizeof(ushort) * words.Length);
            Fill(words);
        }

        /// <summary>Allocate and fill with uints, with a definable length.</summary>
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

        /// <summary>Allocate and fill with uints</summary>
        public void AllocateFill(uint[] data)
        {
            AllocateBytes(sizeof(uint) * data.Length);
            Fill(data);
        }

        /// <summary>Allocate and fill with bytes, with a definable length.</summary>
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

        /// <summary>Allocate and fill with bytes</summary>
        public void AllocateFill(byte[] data)
        {
            AllocateBytes(data.Length);
            Fill(data);
        }

        /// <summary>Fill with packed Vector3s, with an offset and mult to scale them before compressing into 10:11:11 </summary>
        public void FillPacked2vec(Vector3[] vertices, Vector3 offsets, float mult)
        {
            int p = 0;                                                                  
            uint[] packeddata = new uint[vertices.Length * 2];
            for (int i = 0; i < vertices.Length; i++)
            {
                uint z = (uint)((vertices[i].Z + offsets.Z) * mult);
                packeddata[p++] = (uint)((vertices[i].X + offsets.X) * mult) | ((z & 0x7ff) << 21);
                packeddata[p++] = (uint)((vertices[i].Y + offsets.Y) * mult) | (((z >> 11) & 0x7ff) << 21);
            }

            Fill(packeddata);
        }

        /// <summary>Fill with Rectangular byte indexes, reccount number with a restart between each one </summary>
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

        /// <summary>Fill with Rectangular short indexes, reccount number with a restart between each one </summary>
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
        
        /// <summary> Allocate and start write on buffer </summary>
        public void AllocateStartWrite(int datasize)        
        {
            AllocateBytes(datasize);
            StartWrite(0,datasize);
        }

        ///<summary> Begin a write. Select position and size. (datasize=0=all buffer).  
        /// Buffer access mask decides what to keep about the range, default is to wipe the mapped area. Use 0 in bam not to do this. Trap for young players here</summary>
        public void StartWrite(int fillpos, int datasize = 0, BufferAccessMask bufferaccessmask = BufferAccessMask.MapInvalidateRangeBit)
        {
            if (datasize == 0)
                datasize = Length - fillpos;

            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && fillpos >= 0 && fillpos + datasize <= Length); // catch double maps

            CurrentPtr = GL.MapNamedBufferRange(Id, (IntPtr)fillpos, datasize, BufferAccessMask.MapWriteBit | bufferaccessmask);

            CurrentPos = fillpos;
            mapmode = MapMode.Write;
            GLStatics.Check();
        }

        /// <summary>
        /// Start read on definable area
        /// </summary>
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

        /// <summary> Stop a read or write sequence, release buffer back to use </summary>
        public void StopReadWrite()
        {
            GL.UnmapNamedBuffer(Id);
            mapmode = MapMode.None;
            GLStatics.Check();
        }

        /// <summary> Skip pointer forward </summary>
        public void Skip(int p)
        {
            System.Diagnostics.Debug.Assert(mapmode != MapMode.None);
            CurrentPtr += p;
            CurrentPos += p;
            System.Diagnostics.Debug.Assert(CurrentPos <= Length);
            GLStatics.Check();
        }

        /// <summary> Move currentpos onto the alignment </summary>
        public void AlignFloat()
        {
            AlignArrayPtr(sizeof(float), 0);
        }

        /// <summary> Move currentpos onto the alignment </summary>
        public void AlignVec4()
        {
            AlignArrayPtr(Vec4size, 0);
        }

        /// <summary> Move currentpos onto the alignment.
        /// std140 dictates mat4 are aligned on vec4 boundaries.  But if your using instance offsets inside a buffer (where your picking 0+instance*sizeofmat4), you may want to align to a Mat4. </summary>
        public void AlignMat4()
        {
            AlignArrayPtr(Mat4size, 0);
        }

        #endregion

        #region Write to map

        /// <summary> Write to buffer with defined repeat</summary>
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

        /// <summary> Write to buffer with defined repeat</summary>
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

        /// <summary> Write to buffer a rectangle as a Vector4 </summary>
        public void Write(System.Drawing.Rectangle r)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            var p = AlignArrayPtr(Vec4size, 1);
            float[] a = new float[] { r.Left, r.Top, r.Right, r.Bottom };
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, a.Length);          
        }

        /// <summary> Write to buffer a Vector4 with a Vector3 and a float </summary>
        public void Write(Vector3 mat, float vec4other)      // write vec3 as vec4.
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            var p = AlignArrayPtr(Vec4size, 1);
            float[] a = new float[] { mat.X, mat.Y, mat.Z, vec4other };
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, a.Length);          
        }

        /// <summary> Write a float </summary>
        public void Write(float v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            IntPtr pos = AlignScalarPtr(sizeof(float));
            float[] a = new float[] { v };
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        /// <summary> Write to area with defined repeat</summary>
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

        /// <summary> Write a float array of length and offset</summary>
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

        /// <summary>Write to buffer at an arbitary offset in the window a float array. Current position is not changed. </summary>
        public void Write(int offset, float[] a)                    
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            IntPtr p = CurrentPtr + offset;
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p, a.Length); // number of units
        }

        /// <summary> Write to area without checking for alignment/stride a float array of defined length</summary>
        public void WriteCont(float[] a, int length = -1)   
        {
            length = (length == -1) ? a.Length : length;
            System.Runtime.InteropServices.Marshal.Copy(a, 0, CurrentPtr, length);       // number of units, not byte length!
            CurrentPtr += sizeof(float) * length;
            CurrentPos += sizeof(float) * length;
        }

        /// <summary> Write an integer </summary>
        public void Write(int v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            int[] a = new int[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(int));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        /// <summary> Write an integer array with a defined length </summary>
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

        /// <summary> Write a short </summary>
        public void Write(short v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            short[] a = new short[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(short));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        /// <summary>  Write an short array with a defined length </summary>
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

        /// <summary> Wrire a long </summary>
        public void Write(long v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            long[] a = new long[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(long));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        /// <summary> Write an long array with a defined length  </summary>
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

        /// <summary> Write a byte </summary>
        public void Write(byte v)
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            byte[] a = new byte[] { v };
            IntPtr pos = AlignScalarPtr(sizeof(byte));
            System.Runtime.InteropServices.Marshal.Copy(a, 0, pos, a.Length);
        }

        /// <summary>  Write an byte array with a defined length </summary>
        public void Write(byte[] a, int length = -1 )     // special , not aligned, as not normal glsl type.  Used mostly for element indexes
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Write);
            length = (length == -1) ? a.Length : length;
            var p = AlignArrayPtr(sizeof(byte), length);
            System.Runtime.InteropServices.Marshal.Copy(a, 0, p.Item1, length);
        }

        /// <summary>  Indirect array stride </summary>
        public const int WriteIndirectArrayStride = 16;

        /// <summary> Write an Indirect Array draw command to the buffer
        /// if you use it, MultiDrawCountStride = 16</summary>
        public void WriteIndirectArray(int vertexcount, int instancecount = 1, int firstvertex = 0, int baseinstance = 0)
        {
            int[] i = new int[] { vertexcount, instancecount, firstvertex, baseinstance };
           // System.Diagnostics.Debug.WriteLine($"WIA vc {i[0]} ic {i[1]} fv {i[2]} bi {i[3]}");
            Write(i);
        }

        /// <summary>  Indirect Element stride </summary>
        public const int WriteIndirectElementsStride = 16;

        /// <summary> Write an Indirect Element draw command to the buffer
        /// if you use it, MultiDrawCountStride = 20</summary>
        public void WriteIndirectElements(int vertexcount, int instancecount = 1, int firstindex = 0, int basevertex = 0,int baseinstance = 0)
        {
            int[] i = new int[] { vertexcount, instancecount, firstindex, basevertex, baseinstance };
            Write(i);
        }


        #endregion

        #region Reads - Map into memory and then read

        /// <summary> Read number of bytes</summary>
        public byte[] ReadBytes(int size)                                               // read into a byte array. not aligned 
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            byte[] data = new byte[size];
            var p = AlignArrayPtr(sizeof(byte), size);
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, size);
            return data;
        }

        /// <summary> Read number of integers. Optionally ignore std alignment</summary>
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

        /// <summary> Read Int</summary>
        public int ReadInt()
        {
            System.Diagnostics.Debug.Assert(mapmode == MapMode.Read);
            int[] data = new int[1];
            IntPtr pos = AlignScalarPtr(sizeof(int));
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 1);
            return data[0];
        }

        /// <summary> Read number of longs. Optionally ignore std alignment</summary>
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

        /// <summary> Read Long</summary>
        public long ReadLong()
        {
            var data = new long[1];
            IntPtr pos = AlignScalarPtr(sizeof(long));
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 1);
            return data[0];
        }

        /// <summary> Read number of floats. Optionally ignore std alignment</summary>
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

        /// <summary> Read Float</summary>
        public float ReadFloat()
        {
            var data = new float[1];
            IntPtr pos = AlignScalarPtr(sizeof(float));
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 1);
            return data[0];
        }

        /// <summary> Read number of Vector2s</summary>
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

        /// <summary> Read number of Vector3s</summary>
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

        /// <summary> Read number of Vector3s packed into 12 byte strides. Varyings seem to write vector3 tightly packed</summary>
        public Vector3[] ReadVector3sPacked(int count)     
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

        /// <summary> Read number of Vector4s</summary>
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

        /// <summary> Read Vector4</summary>
        public Vector4 ReadVector4()
        {
            var data = new float[4];
            IntPtr pos = AlignScalarPtr(Vec4size);
            System.Runtime.InteropServices.Marshal.Copy(pos, data, 0, 4);
            return new Vector4(data[0], data[1], data[2], data[3]);
        }

        /// <summary> Read Matrix4</summary>
        public Matrix4 ReadMatrix4()
        {
            var data = new float[16];
            var p = AlignArrayPtr(Vec4size, 4);
            System.Runtime.InteropServices.Marshal.Copy(p.Item1, data, 0, 16);
            return new Matrix4(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7],
                                data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15]);
        }

        /// <summary> Read number of Matrix4s</summary>
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

        /// <summary> Read buffer as bytes, at offset and len</summary>
        public byte[] ReadBuffer(int offset, int len)
        {
            StartRead(offset,len);
            var v = ReadBytes(len);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as a int</summary>

        public int ReadInt(int offset)
        {
            StartRead(offset);
            int v = ReadInt();
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as ints, at offset and number, with optionally ignoring alignment</summary>
        public int[] ReadInts(int offset, int number, bool ignorestd130 = false)
        {
            StartRead(offset);
            var v = ReadInts(number, ignorestd130);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as longs, at offset and number, with optionally ignoring alignment</summary>
        public long[] ReadLongs(int offset, int number, bool ignorestd130 = false)
        {
            StartRead(offset);
            var v = ReadLongs(number, ignorestd130);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as floats, at offset and number, with optionally ignoring alignment</summary>
        public float[] ReadFloats(int offset, int number, bool ignorestd130 = false)
        {
            StartRead(offset);
            var v = ReadFloats(number, ignorestd130);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as Vector2 arrays, at offset and number</summary>
        public Vector2[] ReadVector2s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadVector2s(number);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as Vector3 arrays (packed at Vector4 spacing), at offset and number</summary>
        public Vector3[] ReadVector3s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadVector3s(number);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as Vector3 arrays, packed at 12 spacing, at offset and number.  Varyings seem to write vector3 packed</summary>
        public Vector3[] ReadVector3sPacked(int offset, int number)
        {
            StartRead(offset);
            var v = ReadVector3sPacked(number);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as Vector4 arrays, at offset and number</summary>
        public Vector4[] ReadVector4s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadVector4s(number);
            StopReadWrite();
            return v;
        }

        /// <summary> Read buffer as Matrix4 arrays, at offset and number</summary>
        public Matrix4[] ReadMatrix4s(int offset, int number)
        {
            StartRead(offset);
            var v = ReadMatrix4s(number);
            StopReadWrite();
            return v;
        }

        #endregion

        #region Binding a buffer to target 

        /// <summary>Bind buffer to vertext array at this bindingindex, with the start position stride and divisor</summary>
        public void Bind(GLVertexArray va, int bindingindex, int start, int stride, int divisor = 0)      // set buffer binding to a VA
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None);     // catch unmap missing. Since binding to VA can be done before buffer is full, then don't check BufferSize
            va.Bind();
            GL.BindVertexBuffer(bindingindex, Id, (IntPtr)start, stride);      // this buffer to binding index
            GL.VertexBindingDivisor(bindingindex, divisor);
            GLStatics.Check();
            //System.Diagnostics.Debug.WriteLine("BUFBIND " + bindingindex + " To B" + Id + " pos " + start + " stride " + stride + " divisor " + divisor);
        }

        private static int elementbindindex = -1;       // static across renders and programs, like uniform buffers, so no need to keep on rebinding

        /// <summary>Bind buffer to element binding point</summary>
        public void BindElement()
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            //if (elementbindindex != Id) // removed for testing
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Id);
                GLStatics.Check();
                elementbindindex = Id;
            }
        }

        private static int indirectbindindex = -1;

        /// <summary>Bind buffer to indirect binding point</summary>
        public void BindIndirect()
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            //if (indirectbindindex != Id) // removed for testing
            {
                GL.BindBuffer(BufferTarget.DrawIndirectBuffer, Id);
                GLStatics.Check();
                indirectbindindex = Id;
            }
        }

        private static int parameterbindindex = -1;
        /// <summary>Bind buffer to parameter binding point</summary>
        public void BindParameter()
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            // if (parameterbindindex != Id) // removed for testing
            {
                GL.BindBuffer((BufferTarget)0x80ee, Id);        // fudge due to ID not being there in 3.3.2
                GLStatics.Check();
                parameterbindindex = Id;
            }
        }

        ///<summary>Bind buffer to the default (xfb=0) or specific transform feedback buffer object</summary> 
        public void BindTransformFeedback(int index, int xfb = 0, int offset = 0, int size = -1)
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            if ( size == -1 )
                GL.TransformFeedbackBufferBase(xfb, index, Id);
            else
                GL.TransformFeedbackBufferRange(xfb, index, Id, (IntPtr)offset, size);
            GLStatics.Check();
        }

        ///<summary> Unbind from the default (xfb=0) or specific transform feedback buffer object </summary>
        static public void UnbindTransformFeedback(int index, int xfb = 0)
        {
            GL.TransformFeedbackBufferBase(xfb, index, 0); // 0 is the unbind value
        }

        private static int querybindindex = -1;

        ///<summary> Bind to query buffer</summary>
        public void BindQuery()
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            System.Diagnostics.Debug.Assert(mapmode == MapMode.None && Length > 0);     // catch unmap missing or nothing in buffer
            if (querybindindex != Id)
            {
                GL.BindBuffer(BufferTarget.QueryBuffer, Id);
                GLStatics.Check();
                querybindindex = Id;
            }
        }

        ///<summary> Unbinds from query buffer</summary>
        static public void UnbindQuery()
        {
            if (querybindindex != -1)
            {
                GL.BindBuffer(BufferTarget.QueryBuffer, 0); // 0 is the unbind value
                GLStatics.Check();
                querybindindex = -1;
            }
        }

        ///<summary> Bing to buffer range target at this binding index</summary>
        public void Bind(int bindingindex,  BufferRangeTarget tgr)                           
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            GL.BindBufferBase(tgr, bindingindex, Id);       // binding point set to tgr
        }

        #endregion

        #region Implementation

        ///<summary> Dispose buffer on end of use</summary>
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