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

        public int Objects { get; private set; } = 0;
        public int Count { get { return set.Count; } }

        public List<List<GLObjectsWithLabels.BlockRef>> BlockList { get; private set; } = new List<List<GLObjectsWithLabels.BlockRef>>();     // in add order
        public Dictionary<object, List<GLObjectsWithLabels.BlockRef>> TagsToBlocks { get; private set; } = new Dictionary<object, List<GLObjectsWithLabels.BlockRef>>(); // tags to block list
        public Dictionary<object, object> UserData { get; set; }  = new Dictionary<object, object>();     // tag to user data, optional

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

        // call to reserve a tag, which you later add.  
        public void ReserveTag(object tag)          
        {
            TagsToBlocks[tag] = null;
        }

        // tag should be unique, if not, it won't complain
        // array holds worldpositions for objects
        // matrix holds pos, orientation, etc for text
        // bitmaps are for each label.  Owned by caller
        // -1 if all added, else can't add from that pos on

        public void Add(Object tag, Object usertag, Vector4[] array, Matrix4[] matrix, Bitmap[] bitmaps)
        {
            UserData[tag] = usertag;
            Add(tag, array, matrix, bitmaps);
        }

        public void Add(Object tag, Vector4[] array, Matrix4[] matrix, Bitmap[] bitmaps)
        {
            System.Diagnostics.Debug.Assert(tag != null);

            List<GLObjectsWithLabels.BlockRef> blocklist = new List<GLObjectsWithLabels.BlockRef>();

            if (set.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No sets found, Create 0");
                AddSet();
            }

            int v = set.Last().Add(array, matrix, bitmaps, blocklist);

            while (v >= 0)    // if can't add
            {
                System.Diagnostics.Debug.WriteLine($"Create another set {set.Count} for {v}");
                AddSet();
                v = set.Last().Add(array, matrix, bitmaps, blocklist, v);      // add the rest from v
            }

            blocklist[0].tag = tag;                 // first entry only gets tag
            BlockList.Add(blocklist);               // in order, add block list
            TagsToBlocks[tag] = blocklist;
            Objects += array.Length;
        }

        public bool Remove(object tag)
        {
            List<GLObjectsWithLabels> toremove = new List<GLObjectsWithLabels>();

            if (TagsToBlocks.TryGetValue(tag, out List<GLObjectsWithLabels.BlockRef> blocklist) )
            {
                if (blocklist != null)      // tag may be reserved, not set, so just remove tag.  if set, remove blocklist
                {
                    foreach (var b in blocklist)
                    {
                        b.owl.Remove(b.blockindex);     // in owl, remove block
                        if (b.owl.Emptied)              // if block has gone emptied, add to remove list
                            toremove.Add(b.owl);
                        Objects -= b.count;
                    }

                    foreach (var removeit in toremove)
                    {
                        robjects.Remove(removeit.ObjectRenderer);      // remove renders
                        robjects.Remove(removeit.TextRenderer);
                        removeit.Dispose();        // then dispose
                        set.Remove(removeit);
                    }
                    BlockList.Remove(blocklist);
                }

                TagsToBlocks.Remove(tag);

                return true;
            }
            else
                return false;
        }

        public void RemoveOldest(int n)
        {
            List<GLObjectsWithLabels> toremove = new List<GLObjectsWithLabels>();

            n = Math.Min(BlockList.Count, n);       // limit

            for (int i = 0; i < n; i++)             // for all block list entries
            {
                var blocklist = BlockList[i];

                foreach (var b in blocklist)
                {
                    b.owl.Remove(b.blockindex);     // in owl, remove block
                    if (b.owl.Emptied)              // if block has gone emptied, add to remove list
                        toremove.Add(b.owl);
                    Objects -= b.count;
                }

                System.Diagnostics.Debug.Assert(TagsToBlocks.ContainsKey(blocklist[0].tag));

                UserData.Remove(blocklist[0].tag);      // remove the user data associated with the tag
                TagsToBlocks.Remove(blocklist[0].tag);  // remove the tag associated with the blocklist
            }

            System.Diagnostics.Debug.WriteLine($"Blocklist {BlockList.Count} remove {n} objects {Objects}");
            BlockList.RemoveRange(0, n);            // and empty block list

            foreach (var removeit in toremove)
            {
                robjects.Remove(removeit.ObjectRenderer);      // remove renders
                robjects.Remove(removeit.TextRenderer);
                removeit.Dispose();        // then dispose
                set.Remove(removeit);
            }
        }

        // remove until Objects <= count

        public void RemoveUntil(int count)
        {
            List<GLObjectsWithLabels> toremove = new List<GLObjectsWithLabels>();

            int n = 0;      // number removed
            while( n < BlockList.Count && Objects > count )
            {
                var blocklist = BlockList[n];

                foreach (var b in blocklist)
                {
                    b.owl.Remove(b.blockindex);     // in owl, remove block
                    if (b.owl.Emptied)              // if block has gone emptied, add to remove list
                        toremove.Add(b.owl);
                    Objects -= b.count;
                }

                System.Diagnostics.Debug.Assert(TagsToBlocks.ContainsKey(blocklist[0].tag));

                UserData.Remove(blocklist[0].tag);      // remove the user data associated with the tag
                TagsToBlocks.Remove(blocklist[0].tag);  // remove the tag associated with the blocklist
                n++;
            }

            if (n > 0)      // if removed something
            {
                System.Diagnostics.Debug.WriteLine($"Blocklist {BlockList.Count} remove {n} objects {Objects}");
                BlockList.RemoveRange(0, n);            // and empty block list

                foreach (var removeit in toremove)
                {
                    robjects.Remove(removeit.ObjectRenderer);      // remove renders
                    robjects.Remove(removeit.TextRenderer);
                    removeit.Dispose();        // then dispose
                    set.Remove(removeit);
                }
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
                geo.SetGroup(setno++ << 18);      // set the group marker for this group as a uniform
                s.ObjectRenderer.Execute(findshader, state, discard: true, noshaderstart:true); // execute find over ever set, not clearing the buffer
            }

            findshader.Finish();    // finish shader

            var res = geo.GetResult();
            if (res != null)
            {
                System.Diagnostics.Debug.WriteLine("Set Found something"); for (int i = 0; i < res.Length; i++) System.Diagnostics.Debug.WriteLine(i + " = " + res[i]);
                return new Tuple<int,int, int>(((int)res[0].W) >> 18, ((int)res[0].W) & 0x3ffff, (int)res[0].Y);
            }
            else
                return null;
        }

        // Return Blockref list and count within that list, first entry has tag in it for lookup.  Or null.

        public Tuple<List<GLObjectsWithLabels.BlockRef>,int> FindBlock(GLShaderPipeline findshader, GLRenderState state, Point pos, Size size)
        {
            var ret = Find(findshader, state, pos, size);
            if (ret != null)
            {
                GLObjectsWithLabels s = set[ret.Item1];
                var fb = BlockList.Find(x => x.Find(y => y.owl == s && y.blockindex == ret.Item2) != null);     // find (set,blockindex) in block list
                if ( fb != null )
                {
                    int c = 0;
                    foreach( var br in fb)      // until we get to owl/blockindex, count previous block counts
                    {
                        if (br.owl == s && br.blockindex == ret.Item2)
                            break;
                        c += br.count;      
                    }

                    return new Tuple<List<GLObjectsWithLabels.BlockRef>, int>(fb, c + ret.Item3);       // return block list, and real index into it
                }
            }

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
