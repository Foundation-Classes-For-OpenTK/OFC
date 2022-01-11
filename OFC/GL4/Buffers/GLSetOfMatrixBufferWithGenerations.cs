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
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Buffers
{
    /// <summary>
    /// Class holds a set of GLMatrixBufferWithGenerations 
    /// You can delete by tag name or clear all
    /// You can delete by generation
    /// </summary>

    public class GLSetOfMatrixBufferWithGenerations : IDisposable
    {
        /// <summary> Callback indicating new group added </summary>
        public Action<int, GLBuffer> AddedNewGroup { get; set; } = null;      // add hook so you know when a new group made, its index and the matrix buffer
        /// <summary> Maximum number of entries per buffer </summary>
        public int Matricesperbuffer { get; private set; }
        /// <summary> Number of tags defined</summary>
        public int TagCount { get { return tagtoentries.Count; } }            // number of tags recorded
        /// <summary> Current generation </summary>
        public uint CurrentGeneration { get; set; } = 0;                       // to be set on write

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="items">Item list to create buffers on</param>
        /// <param name="matricesperbuffer">Number of matrices per buffer</param>
        public GLSetOfMatrixBufferWithGenerations(GLItemsList items, int matricesperbuffer)
        {
            Matricesperbuffer = matricesperbuffer;
            this.items = items;
        }

        /// <summary>
        /// Add an entry to the set
        /// </summary>
        /// <param name="tag">User defined tag, may be null</param>
        /// <param name="data">Disposable data to store, may be null</param>
        /// <param name="mat">Matrix</param>
        /// <returns>Returns tuple with group, pos, total count of group</returns>
        public Tuple<int,int,int> Add(object tag, IDisposable data, Matrix4 mat)    
        {
            var gi = groups.FindIndex(x => x.Left > 0);      // find one with space..

            if (gi == -1)
            {
                gi = groups.Count;
                groups.Add(new GLMatrixBufferWithGenerations(items, Matricesperbuffer));
                AddedNewGroup?.Invoke(gi, groups[gi].MatrixBuffer);
               // System.Diagnostics.Debug.WriteLine("Make group");
            }

            int pos = groups[gi].Add(tag,data, mat,CurrentGeneration);

            if (tag != null)
                tagtoentries[tag] = new Tuple<GLMatrixBufferWithGenerations, int>(groups[gi], pos);

            return new Tuple<int,int,int>(gi,pos,groups[gi].Count);
        }

        /// <summary> Does this tag exist? </summary>
        public bool Exist(object tag)      
        {
            return tagtoentries.ContainsKey(tag);
        }

        /// <summary> Remove this tag, true if done </summary>
        public bool Remove(Object tag)
        {
            if (tagtoentries.TryGetValue(tag, out Tuple<GLMatrixBufferWithGenerations, int> pos))
            {
                pos.Item1.RemoveAt(pos.Item2);
                tagtoentries.Remove(tag);
                return true;
            }
            else
                return false;
        }

        /// <summary> Set visibility and rotation of a tag, true if done </summary>
        public bool SetVisibilityRotation(Object tag, float ctrl)       // ctrl is in the format described by the vertex sharer in use
        {
            if (tagtoentries.TryGetValue(tag, out Tuple<GLMatrixBufferWithGenerations, int> pos))
            {
                pos.Item1.SetVisibilityRotation(pos.Item2,ctrl);
                return true;
            }
            else
                return false;
        }

        /// <summary> Get Matrix. If does not exist, return empty matrix</summary>
        public Matrix4 GetMatrix(Object tag)
        {
            if (tagtoentries.TryGetValue(tag, out Tuple<GLMatrixBufferWithGenerations, int> pos))
            {
                return pos.Item1.GetMatrix(pos.Item2);
            }
            else
                return Matrix4.Zero;
        }

        /// <summary> Remove all entries up and including this generation from the buffer, except for tags in the keeplist </summary>
        public uint RemoveGeneration(uint removegenerationbelow, HashSet<object> keeplist = null)
        {
            uint oldestgenfound = 0;
            foreach (var g in groups)
            {
                uint oldest = g.RemoveGeneration(removegenerationbelow, CurrentGeneration, tagtoentries, keeplist);
                oldestgenfound = Math.Max(oldestgenfound , oldest);
            }

            return oldestgenfound;
               
        }

        /// <summary> Clear all </summary>
        public void Clear()
        {
            foreach (var g in groups)
            {
                g.Clear();
            }

            tagtoentries = new Dictionary<object, Tuple<GLMatrixBufferWithGenerations, int>>(); // clear all tags
        }

        /// <summary> Dispose of this set </summary>
        public void Dispose()           // you can double dispose.
        {
            foreach (var g in groups)
            {
                g.Dispose();
            }

            tagtoentries = null;
        }

        private GLItemsList items;
        private List<GLMatrixBufferWithGenerations> groups = new List<GLMatrixBufferWithGenerations>();
        private Dictionary<object, Tuple<GLMatrixBufferWithGenerations, int>> tagtoentries = new Dictionary<object, Tuple<GLMatrixBufferWithGenerations, int>>();

    }
}

