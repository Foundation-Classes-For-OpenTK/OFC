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
        public GLSetOfMatrixBufferWithGenerations(GLItemsList items, int groupsize)
        {
            maxpergroup = groupsize;
            this.items = items;
        }

        public void Add(object tag, IDisposable data, Matrix4 mat)
        {
            var g = groups.Find(x => x.Left > 0);      // find one with space..

            if (g == null)
            {
                g = new GLMatrixBufferWithGenerations(items,maxpergroup);
                groups.Add(g);
            }

            g.Add(tag, data, mat);
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
            foreach (GLBitmapGroup g in groups)
            {
                g.Dispose();
            }
        }

        private GLItemsList items;
        private List<GLMatrixBufferWithGenerations> groups = new List<GLMatrixBufferWithGenerations>();
        private int maxpergroup;
    }
}

