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
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4
{
    // Set of ObjectWithLabels
    // add and remove blocks of tagged objects, will clean up empty OWL when required

    public class GLSetOfObjectsWithLabels : IDisposable
    {
        public Size LabelSize { get { return texturesize; } }

        public int Blocks { get { int t = 0; foreach (var s in set) t += s.Blocks; return t; } }        // how many blocks allocated
        public int BlocksRemoved { get { int t = 0; foreach (var s in set) t += s.BlocksRemoved; return t; } }  // how many have been removed
        public int Objects() { int t = 0; foreach (var s in set) t += s.Objects; return t; }           // total number of objects being drawn

        public int Count { get { return set.Count; } }
        public GLObjectsWithLabels this[int i] { get { return set[i]; } }

        public GLSetOfObjectsWithLabels(string name,        // need a name for the renders
                                        GLRenderProgramSortedList robjects,     // need to give it a render list to add/remove renders to
                                        int textures,       // number of textures to allow per set
                                        int estimateditemspergroup,      // estimated objects per group, this adds on vertext buffer space to allow for mat4 alignment. Smaller means more allowance.
                                        int mingroups,      // minimum number of groups
                                        IGLProgramShader objectshader, GLBuffer objectbuffer, int objectvertexes, GLRenderState objrc, PrimitiveType objpt,   // object shader, buffer, vertexes and its rendercontrol
                                        IGLProgramShader textshader, Size texturesize ,  GLRenderState textrc,   // text shader, text size, and rendercontrol
                                        int debuglimittexturedepth = 0)     // set to limit texture depth per set
        {
            this.name = name;
            this.robjects = robjects;
            this.textures = textures;
            this.estimateditemspergroup = estimateditemspergroup;
            this.mingroups = mingroups;
            this.objectshader = objectshader;
            this.objectbuffer = objectbuffer;
            this.objectvertexescount = objectvertexes;
            this.objrc = objrc;
            this.objpt = objpt;
            this.textshader = textshader;
            this.texturesize = texturesize;
            this.textrc = textrc;
            this.limittexturedepth = debuglimittexturedepth;
        }

        // array holds worldpositions for objects
        // matrix holds pos, orientation, etc for text
        // bitmaps are for each label.  Owned by caller
        // -1 if all added, else can't add from that pos on

        public void Add(Object tag, Object userdata, Vector4[] array, Matrix4[] matrix, Bitmap[] bitmaps)
        {
            if (set.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No sets found, Create 0");
                AddSet();
            }

            int v = set.Last().Add(tag, userdata, array, matrix, bitmaps);

            while ( v >= 0)    // if can't add
            {
                System.Diagnostics.Debug.WriteLine($"Create another set {set.Count} for {v}");
                AddSet();
                v = set.Last().Add(tag, userdata, array, matrix, bitmaps, v);      // add the rest from v
            }
        }

        public bool Remove(object tag)
        {
            bool res = false;
            GLObjectsWithLabels removeit = null;

            foreach (var s in set)
            {
                if (s.Remove(tag))        // if removed something
                {
                    System.Diagnostics.Debug.WriteLine($"remove in set {set.IndexOf(s)}");
                    if (s.Blocks == s.BlocksRemoved)  // if all marked removed
                    {
                        removeit = s;
                    }
                    res = true;
                    break;
                }
            }

            if ( removeit != null )
            { 
                System.Diagnostics.Debug.WriteLine($"Remove set {set.IndexOf(removeit)} with {removeit.Blocks}");
                robjects.Remove(removeit.ObjectRenderer);      // remove renders
                robjects.Remove(removeit.TextRenderer);
                removeit.Dispose();        // then dispose
                set.Remove(removeit);
            }

            return res;
           // System.Diagnostics.Debug.WriteLine($"Total sets remaining {set.Count}");
        }

        public void RemoveOldest(int wanted )
        {
            List<GLObjectsWithLabels> toremove = new List<GLObjectsWithLabels>();
            foreach( var s in set)
            {
                int done = s.RemoveOldest(wanted);
                wanted -= done;
                if (s.Blocks == s.BlocksRemoved)  // if all marked removed
                    toremove.Add(s);
                if (wanted <= 0)
                    break;
            }

            foreach( var removeit in toremove )
            {
                robjects.Remove(removeit.ObjectRenderer);      // remove renders
                robjects.Remove(removeit.TextRenderer);
                removeit.Dispose();        // then dispose
                set.Remove(removeit);
            }
        }

        public void Dispose()
        {
            foreach (var s in set)
            {
                robjects.Remove(s.ObjectRenderer);      // remove renders
                robjects.Remove(s.TextRenderer);
                s.Dispose();            // then dispose
            }
            set.Clear();
        }

        // Return set, render group, and render index in group, or null
        public Tuple<int,int,int> Find(GLShaderPipeline findshader, GLRenderState state, Point pos, Size size)
        {
            var geo = findshader.GetShader<GLPLGeoShaderFindTriangles>(OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader);
            geo.SetScreenCoords(pos, size);
            findshader.Start(null);     // this clears the buffer

            int setno = 0;

            foreach (var s in set)      
            {
                geo.SetGroup(setno << 18);      // set the group marker for 
                s.ObjectRenderer.Execute(findshader, state, discard: true, noshaderstart:true); // execute find over ever set, not clearing the buffer
            }

            findshader.Finish();    // finish shader

            var res = geo.GetResult();
            if (res != null)
            {
                //System.Diagnostics.Debug.WriteLine("Set Found something"); for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
                return new Tuple<int,int, int>(((int)res[0].W)>>18,((int)res[0].W) & 0x3ffff, (int)res[0].Y);
            }
            else
                return null;
        }

        private void AddSet()       // add a new set
        {
            var owl = new GLObjectsWithLabels();
            var ris = owl.Create(textures, estimateditemspergroup, mingroups, objectbuffer, objectvertexescount, objrc, objpt, texturesize, textrc, limittexturedepth);
            robjects.Add(objectshader, name + "O" + (setnumber).ToString(), ris.Item1);
            robjects.Add(textshader, name + "T" + (setnumber++).ToString(), ris.Item2);
            set.Add(owl);
        }

        #region Vars

        private string name;

        private GLRenderProgramSortedList robjects;         // render list

        private int textures;                               // number of textures to ask for 
        private int estimateditemspergroup;                 // estimated items per group
        private int mingroups;                              // minimum groups to ask for

        private IGLProgramShader objectshader;              // object data
        private GLBuffer objectbuffer;
        private int objectvertexescount;
        private GLRenderState objrc;
        private PrimitiveType objpt;

        private IGLProgramShader textshader;                // text data
        private Size texturesize;
        private GLRenderState textrc;

        private int limittexturedepth;                      // debug, limit texture depth

        private int setnumber = 0;                          // for naming

        private List<GLObjectsWithLabels> set = new List<GLObjectsWithLabels>();        // finally the set of OWL

        #endregion

    }
}
