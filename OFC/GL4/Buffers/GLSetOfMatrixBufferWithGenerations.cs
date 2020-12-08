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
    // Class holds a MatrixBuffer, with [0,3] = -1 if deleted
    // You can delete by tag name or clear all
    // You can delete by generation

    public class GLSetOfMatrixBufferWithGenerations : IDisposable
    {
        public Action<int, GLBuffer> AddedNewGroup { get; set; } = null;      // add hook so you know when a new group made, its index and the matrix buffer

        public int MaxPerGroup { get; private set; }

        public GLSetOfMatrixBufferWithGenerations(GLItemsList items, int groupsize)
        {
            MaxPerGroup = groupsize;
            this.items = items;
        }

        public Tuple<int,int,int> Add(object tag, IDisposable data, Matrix4 mat)        // returning group, pos, total count of group
        {
            var gi = groups.FindIndex(x => x.Left > 0);      // find one with space..

            if (gi == -1)
            {
                gi = groups.Count;
                groups.Add(new GLMatrixBufferWithGenerations(items, MaxPerGroup));
                AddedNewGroup?.Invoke(gi, groups[gi].MatrixBuffer);
            }

            int pos = groups[gi].Add(tag, data, mat);

            return new Tuple<int,int,int>(gi,pos,groups[gi].Count);
        }

        public bool Exist(object tag)       // does this tag exist?
        {
            var g = groups.Find(x => x.FindTag(tag) >= 0);
            return g != null;
        }

        public bool Remove(Object tag)
        {
            var g = groups.Find(x => x.FindTag(tag) >= 0);
            if (g != null)
                return g.RemoveAt(g.FindTag(tag));
            else
                return false;
        }

        public void RemoveGeneration(int generation = 1)        // all new images get generation 0
        {
            foreach (var g in groups)
                g.RemoveGeneration(generation);
        }

        public void Clear()
        {
            foreach (var g in groups)
            {
                g.Clear();
            }
        }

        public bool SetGenerationIfExist(object tag, int generation = 0)
        {
            var g = groups.Find(x => x.SetGenerationIfExists(tag, generation));      // find first tag, and mark it
            return g != null;
        }

        public void IncreaseGeneration()
        {
            foreach (var g in groups)
                g.IncreaseGeneration();
        }

        public void Dispose()           // you can double dispose.
        {
            foreach (var g in groups)
            {
                g.Dispose();
            }
        }

        private GLItemsList items;
        private List<GLMatrixBufferWithGenerations> groups = new List<GLMatrixBufferWithGenerations>();
    }
}

