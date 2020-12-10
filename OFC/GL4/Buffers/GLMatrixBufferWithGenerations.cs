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
using System.Collections.Generic;

namespace OFC.GL4
{
    // Class holds a MatrixBuffer, with [0,3] = -1 if deleted
    // You can delete by tag name or clear all
    // You can delete by generation

    public class GLMatrixBufferWithGenerations : IDisposable
    {
        public int Count { get { return entries.Count; } }
        public int Left { get { return Max - (entries.Count - Deleted); } }
        public int Deleted { get; private set; } = 0;
        public int Max { get; private set; } = 0;

        public GLBuffer MatrixBuffer { get; private set; }

        private class EntryInfo
        {
            public Object tag;
            public IDisposable data { get; set; }   // only disposed if non null
            public int generation { get; set; } = int.MaxValue;     // 0 = newest, MaxValue = empty
            public bool IsEmpty { get { return generation == int.MaxValue; } }
        }

        private List<EntryInfo> entries = new List<EntryInfo>();

        public GLMatrixBufferWithGenerations(GLItemsList items, int groupsize)
        {
            MatrixBuffer = new GLBuffer();
            items.Add(MatrixBuffer);
            Max = groupsize;
            MatrixBuffer.AllocateBytes(Max * GLLayoutStandards.Mat4size);
            MatrixBuffer.AddPosition(0);        // CreateMatrix4 needs to have a position
        }

        // return position added as index. If tag == null, you can't find it again
        public int Add(Object tag, IDisposable data, Matrix4 mat)
        {
            var entry = new EntryInfo() { tag = tag, data = data, generation = 0 };

            int pos = Deleted > 0 ? entries.FindIndex(x => x.IsEmpty) : -1;     // find an empty slot if any deleted

            if (pos == -1)
            {
                System.Diagnostics.Debug.Assert(entries.Count < Max);       // the caller should manage not overfilling
                pos = entries.Count;                                // not found, so make a fresh one at end
                entries.Add(entry);
            }
            else
            {
                Deleted--;
                entries[pos] = entry;                               // set empty slot to active
            }

            System.Diagnostics.Debug.WriteLine("Make " + pos);

            //System.Diagnostics.Debug.WriteLine("Pos {0} Matrix {1}", pos, mat);
            mat[0, 3] = pos;     // store pos of image in stack

            MatrixBuffer.StartWrite(GLLayoutStandards.Mat4size * pos, GLLayoutStandards.Mat4size);
            MatrixBuffer.Write(mat);
            MatrixBuffer.StopReadWrite();
            return pos;
        }

        public bool RemoveAt(int i)
        {
            if (i >= 0 && i < entries.Count)
            {
                if (entries[i].data != null)           // owned, bitmap will be valid
                    entries[i].data.Dispose();

                entries[i] = new EntryInfo(); // all will be null/false

                Matrix4 zero = Matrix4.Identity;      // set ctrl 1,3 to -1 to indicate cull matrix
                zero[1, 3] = -1;                      // if it did not work, it would appear at (0,0,0)
                MatrixBuffer.StartWrite(GLLayoutStandards.Mat4size * i, GLLayoutStandards.Mat4size);
                MatrixBuffer.Write(zero);
                MatrixBuffer.StopReadWrite();
                Deleted++;
                return true;
            }
            else
                return false;
        }

        // if keeplist, and its in the list, generation = 0 and kept
        // else increasegeneration, and remove it if >= removegeneration
        public void IncreaseRemoveGeneration(int increasegeneration, int removegeneration,
                                             Dictionary<object, Tuple<GLMatrixBufferWithGenerations, int>> tagtoentries,
                                             HashSet<object> keeplist = null )
        {
            Matrix4 zero = Matrix4.Identity;        // set ctrl 1,3 to -1 to indicate cull matrix
            zero[1, 3] = -1;                        // if it did not work, it would appear at (0,0,0)
            var fm = zero.ToFloatArray();           // writing in float arrays

            bool openedwrite = false;
            
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];

                if (keeplist != null && e.tag != null && keeplist.Contains(e.tag))      // if in keeplist, its gen goes back to zero
                {
                    e.generation = 0;
                }
                else
                {
                    e.generation += increasegeneration;     // increase gen

                    if (e.generation >= removegeneration)    // if older.. note invalid ones get generation = int.max
                    {
                        if (e.data != null)           // owned, bitmap will be valid
                            e.data.Dispose();

                        if (e.tag != null)
                            tagtoentries.Remove(e.tag);

                        entries[i] = new EntryInfo(); // all will be null, generation will be int.max

                        if (!openedwrite)
                        {
                            MatrixBuffer.StartWrite(0, 0, 0);       // map all, keep existing buffer (P3)
                            openedwrite = true;
                        }

                        MatrixBuffer.Write(i * GLLayoutStandards.Mat4size, fm);
                        Deleted++;
                    }
                }
            }

            if (openedwrite)
                MatrixBuffer.StopReadWrite();
        }

        public void Clear()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].data != null)           // owned, bitmap will be valid
                    entries[i].data.Dispose();

                entries[i] = new EntryInfo(); // all will be null
            }

            Matrix4 zero = Matrix4.Identity;
            zero[1, 3] = -1;
            MatrixBuffer.StartWrite(0);
            MatrixBuffer.Write(zero, entries.Count);
            MatrixBuffer.StopReadWrite();
            Deleted = entries.Count;
        }

        public void Dispose()
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].data != null)           // owned, bitmap will be valid
                        entries[i].data.Dispose();
                }

                entries = null;
            }
        }
    }
}

