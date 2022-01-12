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
using System.Collections.Generic;

namespace GLOFC.GL4.Buffers
{
    /// <summary>
    /// This namespace contains various complex buffer objects which allow the GL buffers to the manipulated efficiently.
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Class holds a Buffer, filled with Matrices. Used normally by GLPLVertexShaderQuadTextureWithMatrixTranslation
    /// [0,3] = image index, [1,3] = ctrl word ( less than 0 not shown, 0++ ctrl as per GLPL)
    /// You can delete by tag name or clear all
    /// You can delete by generation
    /// </summary>

    public class GLMatrixBufferWithGenerations : IDisposable
    {
        /// <summary> Number of entries </summary>
        public int Count { get { return entries.Count; } }
        /// <summary> Entries left</summary>
        public int Left { get { return Max - (entries.Count - Deleted); } }
        /// <summary> Now many are deleted in the list </summary>
        public int Deleted { get; private set; } = 0;
        /// <summary> Maximum size </summary>
        public int Max { get; private set; } = 0;

        /// <summary> The matrix buffer itself </summary>
        public GLBuffer MatrixBuffer { get; private set; }

        /// <summary> Construct, on an item list, a buffer with this size </summary>
        public GLMatrixBufferWithGenerations(GLItemsList items, int maximumsize)
        {
            MatrixBuffer = new GLBuffer();
            items.Add(MatrixBuffer);
            Max = maximumsize;
            MatrixBuffer.AllocateBytes(Max * GLLayoutStandards.Mat4size);
            MatrixBuffer.AddPosition(0);        // CreateMatrix4 needs to have a position
        }

        /// <summary>
        /// Add an entry to the buffer
        /// </summary>
        /// <param name="tag">User tag to identify the entry, may be null</param>
        /// <param name="data">User disposable data to hold for this entry, may be null</param>
        /// <param name="matrix">Matrix to store</param>
        /// <param name="generation">Generation of matrix</param>
        /// <returns>return position added as index.</returns>

        public int Add(Object tag, IDisposable data, Matrix4 matrix, uint generation)
        {
            var entry = new EntryInfo() { tag = tag, data = data, generation = generation, empty = false };

            int pos = Deleted > 0 ? entries.FindIndex(x => x.empty) : -1;     // find an empty slot if any deleted

            if (pos == -1)
            {
                System.Diagnostics.Debug.Assert(Deleted == 0);
                System.Diagnostics.Debug.Assert(entries.Count < Max);       // the caller should manage not overfilling
                pos = entries.Count;                                // not found, so make a fresh one at end
                entries.Add(entry);
            }
            else
            {
                Deleted--;
                entries[pos] = entry;                               // set empty slot to active
            }

            //System.Diagnostics.Debug.WriteLine("Pos {0} Matrix {1}", pos, mat);
            matrix[0, 3] = pos;     // store pos of image in stack

            MatrixBuffer.StartWrite(GLLayoutStandards.Mat4size * pos, GLLayoutStandards.Mat4size);
            MatrixBuffer.Write(matrix);
            MatrixBuffer.StopReadWrite();

            //float[] stored = MatrixBuffer.ReadFloats(GLLayoutStandards.Mat4size * pos, 16, true);
            return pos;
        }

        /// <summary> Remove entry at position (entry is nulled except matrix[1,3] is set to -1)</summary>
        public bool RemoveAt(int i)
        {
            if (i >= 0 && i < entries.Count && entries[i].empty == false )      // if valid to remove
            {
                if (entries[i].data != null)           // owned, bitmap will be valid
                    entries[i].data.Dispose();

                entries[i] = new EntryInfo();           // set to empty as default for this class

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

        /// <summary> Set the visibility and rotation of a matrix</summary>
        public bool SetVisibilityRotation(int i, float ctrl)            // reset the ctrl word at [1,3] of a particular entry
        {
            if (i >= 0 && i < entries.Count && entries[i].empty == false)      // in range and not empty
            {
                MatrixBuffer.StartWrite(GLLayoutStandards.Mat4size * i + 7 * sizeof(float), sizeof(float));
                MatrixBuffer.Write(ctrl);
                MatrixBuffer.StopReadWrite();
                //float[] f= MatrixBuffer.ReadFloats(GLLayoutStandards.Mat4size * i, 16, true);
                return true;
            }
            else
                return false;
        }

        /// <summary> Get Matrix. If does not exist, return empty matrix</summary>
        public Matrix4 GetMatrix(int i)
        {
            if (i >= 0 && i < entries.Count && entries[i].empty == false)      // in range and not empty
            {
                MatrixBuffer.StartRead(GLLayoutStandards.Mat4size * i, GLLayoutStandards.Mat4size);
                Matrix4 mat = MatrixBuffer.ReadMatrix4();
                MatrixBuffer.StopReadWrite();
                return mat;
            }
            else
                return Matrix4.Zero;
        }

        /// <summary>
        /// Remove a generation from the buffer
        /// </summary>
        /// <param name="removegeneration">Remove all generations less or equal to this generation</param>
        /// <param name="currentgeneration">Current generation</param>
        /// <param name="tagtoentries">Tag to entry list to update on removal of each item, this tag is removed from this list on removal</param>
        /// <param name="keeplist">if keeplist is set, and its in the list, the generation is reset to currentgeneration and its kept</param>
        /// <returns>return relative index giving the different between the current gen and the maximum generation found</returns>
        public uint RemoveGeneration(uint removegeneration, uint currentgeneration,
                                             Dictionary<object, Tuple<GLMatrixBufferWithGenerations, int>> tagtoentries,
                                             HashSet<object> keeplist = null )
        {
            Matrix4 zero = Matrix4.Identity;        // set ctrl 1,3 to -1 to indicate cull matrix
            zero[1, 3] = -1;                        // if it did not work, it would appear at (0,0,0)
            var fm = zero.ToFloatArray();           // writing in float arrays

            bool openedwrite = false;
            uint oldestgenfound = 0;

            uint removegenerationbelow = removegeneration + 1;      // the +1 allows the modulo check to work

            //System.Diagnostics.Debug.WriteLine("Remove {0} current {1}", removegeneration, currentgeneration);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (!e.empty)
                {
                    if (keeplist != null && e.tag != null && keeplist.Contains(e.tag))      // if in keeplist, its gen goes back to zero
                    {
                        e.generation = currentgeneration;
                    }
                    else
                    {
                        if (((e.generation - removegenerationbelow) & 0x80000000) != 0)   // if modulo e.generation<removegenerationbelow
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
                        else
                            oldestgenfound = Math.Max(oldestgenfound, currentgeneration - e.generation);        // using modulo to find it
                    }
                }
            }

            if (openedwrite)
                MatrixBuffer.StopReadWrite();

            return oldestgenfound;
        }

        /// <summary> Clear all entries (all entries are nulled except matrix[1,3] is set to -1)</summary>
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

        /// <summary> Dispose of the buffer </summary>
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

        private class EntryInfo
        {
            public Object tag;
            public IDisposable data { get; set; }   // only disposed if non null
            public uint generation { get; set; } = int.MaxValue;     // 0 = newest, MaxValue = empty
            public bool empty { get; set; } = true;
        }

        private List<EntryInfo> entries = new List<EntryInfo>();

    }
}

