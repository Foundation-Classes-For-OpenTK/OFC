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
using System.Drawing;
using System.Linq;

namespace OFC.GL4
{
    // Class holds a MatrixBuffer
    // You can delete by tag name or clear all
    // You can delete by generation

    public class GLSetOfMatrixBufferWithGenerations : IDisposable
    {
        public Action<int, GLBuffer> AddedNewGroup { get; set; } = null;      // add hook so you know when a new group made, its index and the matrix buffer

        public int MaxPerGroup { get; private set; }

        public int TagCount { get { return tagtoentries.Count; } }            // number of tags recorded

        public uint CurrentGeneration { get; set; } = 0;                       // to be set on write

        public GLSetOfMatrixBufferWithGenerations(GLItemsList items, int groupsize)
        {
            MaxPerGroup = groupsize;
            this.items = items;
        }

        // tag can be null, but then it can't be found
        public Tuple<int,int,int> Add(object tag, IDisposable data, Matrix4 mat)        // returning group, pos, total count of group
        {
            var gi = groups.FindIndex(x => x.Left > 0);      // find one with space..

            if (gi == -1)
            {
                gi = groups.Count;
                groups.Add(new GLMatrixBufferWithGenerations(items, MaxPerGroup));
                AddedNewGroup?.Invoke(gi, groups[gi].MatrixBuffer);
               // System.Diagnostics.Debug.WriteLine("Make group");
            }

            int pos = groups[gi].Add(tag,data, mat,CurrentGeneration);

            if (tag != null)
                tagtoentries[tag] = new Tuple<GLMatrixBufferWithGenerations, int>(groups[gi], pos);

            return new Tuple<int,int,int>(gi,pos,groups[gi].Count);
        }

        public bool Exist(object tag)       // does this tag exist?
        {
            return tagtoentries.ContainsKey(tag);
        }

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

        public void Clear()
        {
            foreach (var g in groups)
            {
                g.Clear();
            }

            tagtoentries = new Dictionary<object, Tuple<GLMatrixBufferWithGenerations, int>>(); // clear all tags
        }

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

