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

using OFC.GL4;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OFC.GL4
{
    // class uses a GLVertexBufferIndirect to hold a vertex buffer and indirect commands, with multiple textures supplied to the shader
    // The object drawn is defined by its objectshader, and its model vertices are in objectbuffer (start of) of objectlength
    // Object shader will get vertex 0 = objectbuffer vector4s, and vertex 1 = worldpositions of items added (instance divided)
    // use with text shader GLShaderPipeline(new GLPLVertexShaderQuadTextureWithMatrixTranslation(), new GLPLFragmentShaderTexture2DIndexedMulti(0,0,true, texunitspergroup));
    // multiple textures can be bound to carry the text labels, the number given by textures.
    // dependent on the opengl, that gives the number of objects that can be produced (GetMaxTextureDepth())

    public class GLSetOfObjectsWithLabels : IDisposable
    {
        public Size LabelSize { get { return texturesize; } }

        private string name;
        private GLRenderProgramSortedList robjects;
        private int textures;
        private int maxgroups;
        private IGLProgramShader objectshader;
        private GLBuffer objectbuffer;
        private int objectvertexescount;
        private IGLProgramShader textshader;
        private Size texturesize;
        private int setnumber = 0;      // for debug naming

        private List<GLObjectsWithLabels> set = new List<GLObjectsWithLabels>();

        // starsortextures, >0 stars, else -N = textures to use (therefore stars set by max texture depth)

        public GLSetOfObjectsWithLabels(string name, GLRenderProgramSortedList robjects,
                                        int textures, int maxgroups,
                                        IGLProgramShader objectshader, GLBuffer objectbuffer, int objectvertexes,
                                        IGLProgramShader textshader, Size texturesize)
        {
            this.name = name;
            this.robjects = robjects;
            this.textures = textures;
            this.maxgroups = maxgroups;
            this.objectshader = objectshader;
            this.objectbuffer = objectbuffer;
            this.objectvertexescount = objectvertexes;
            this.textshader = textshader;
            this.texturesize = texturesize;
        }


        public int AddObjects(Object tag, Vector4[] array, Matrix4[] matrix, Bitmap[] bitmaps)
        {
            if (set.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No sets found, Create 0");
                set.Add(new GLObjectsWithLabels(name + (setnumber++).ToString(), robjects, textures, maxgroups, objectshader, objectbuffer, objectvertexescount, textshader, texturesize));
            }

            int v = set.Last().AddObjects(tag, array, matrix, bitmaps);

            if ( v >= 0)    // if can't addc
            {
                System.Diagnostics.Debug.WriteLine($"Create another set {set.Count} for {v}");
                set.Add(new GLObjectsWithLabels(name + (setnumber++).ToString(), robjects, textures, maxgroups, objectshader, objectbuffer, objectvertexescount, textshader, texturesize));
                v = set.Last().AddObjects(tag, array, matrix, bitmaps, v);      // add the rest from v
            }

            return v;
        }

        public void Remove(Predicate<object> test)
        {
            List<GLObjectsWithLabels> tobedisposed = new List<GLObjectsWithLabels>();

            foreach (var s in set)
            {
                if (s.Remove(test))        // if removed something
                {
                    System.Diagnostics.Debug.WriteLine($".. in set {set.IndexOf(s)}");
                    if (s.Number == s.Removed)  // if all marked removed
                        tobedisposed.Add(s);    
                }
            }

            foreach (var s in tobedisposed)
            {
                System.Diagnostics.Debug.WriteLine($"Remove set {set.IndexOf(s)} with {s.Number}");
                s.Dispose();
                set.Remove(s);
            }
            System.Diagnostics.Debug.WriteLine($"Total sets remaining {set.Count}");
        }

        public void Dispose()
        {
            foreach (var s in set)
                s.Dispose();
        }

    }
}
