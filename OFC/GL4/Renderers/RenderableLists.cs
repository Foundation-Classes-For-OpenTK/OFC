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

using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace OFC.GL4
{
    // this is a render list, holding a list of Shader programs
    // each shader program is associated with zero or more RenderableItems 
    // The shader calls Start() for each shader, then goes thru the render list (if it has one) , setting up the render control, then Binding and Rendering each item
    // then it Finish() the program
    // Shaders are executed in the order added
    // Renderable items are ordered by shader, then in the order added.
    // if you add a compute shader or a operation to the shader list, then the renderable items must be null 
    // you can add an operation to the render list of a shader as well. The rendercontrol must be null
    // adding a compute shader in the middle of other renderable items may be useful - but remember to use a memory barrier if required in the shader FinishAction routine

    public class GLRenderProgramSortedList
    {
        private Dictionary<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>> renderables;
        private Dictionary<string,IGLRenderableItem> byname;
        private int unnamed = 0;

        public GLRenderProgramSortedList()
        {
            renderables = new Dictionary<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>>();
            byname = new Dictionary<string, IGLRenderableItem>();
        }

        // name can be null if required, which gives it an autoname
        public void Add(IGLProgramShader prog, string name, IGLRenderableItem r)        // name is the id given to this renderable
        {
            name = EnsureName(name, prog, r);
            //System.Diagnostics.Debug.WriteLine($"Add render {prog.Name} {name}");
            AddItem(prog, name, r);
            byname.Add(name, r);
        }

        // with autoname
        public void Add(IGLProgramShader prog, IGLRenderableItem r)
        {
            AddItem(prog, EnsureName(null,prog,r), r);
        }

        // a compute shader
        public void Add(GLShaderCompute cprog)
        {
            string n = "CS " + cprog.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(cprog, n, null);
        }

        // a operation
        public void Add(GLOperationsBase nprog)
        {
            string n = "OP " + nprog.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(nprog, n, null);
        }

        public bool Remove(IGLRenderableItem r)     
        {
            var f = Find(r);
            return r != null ? Remove(f.Item1, r) : false;
        }

        public bool Remove(IGLProgramShader prog, IGLRenderableItem r)
        {
            if ( renderables.ContainsKey(prog))
            {
                var i = renderables[prog].FindIndex(x => Object.ReferenceEquals(x.Item2, r));
                if ( i >= 0)
                {
                    byname.Remove(renderables[prog][i].Item1);
                    renderables[prog].RemoveAt(i);          // remove renderer

                    //foreach (var s in renderables[prog]) System.Diagnostics.Debug.WriteLine($"left .. {prog.Name} {s.Item1}");

                    if ( renderables[prog].Count == 0 )     // if nothing more in shader
                    {
                        //System.Diagnostics.Debug.WriteLine($"remove shader {prog.Name}");
                        renderables.Remove(prog);           // remove shader
                    }
                    return true;
                }
            }

            return false;
        }

        public IGLRenderableItem this[string renderitem] { get { return byname[renderitem]; } }
        public bool Contains(string renderitem) { return byname.ContainsKey(renderitem); }

        public void Render(GLRenderControl currentstate, GLMatrixCalc c, bool verbose = false)
        {
            GLRenderControl lastapplied = null;

            if (verbose) System.Diagnostics.Debug.WriteLine("***Begin RList");

            foreach (var kvp in renderables)        // kvp of Key=Shader, Value = list of renderables
            {
                // shader must be enabled and at least 1 renderable item visible (or set to null,as a compute/null shader would be)
                if (kvp.Key.Enable && kvp.Value.Find((x)=>x.Item2 == null || x.Item2.Visible)!=null)      
                {
                    //System.Diagnostics.Debug.WriteLine("Shader " + kvp.Key.GetType().Name);
                    kvp.Key.Start(c);                                                  // start the program - if compute shader, this executes the code

                    foreach (var g in kvp.Value)
                    {
                        if (g.Item2 != null && g.Item2.Visible )                    // may have added a null renderable item if its a compute shader.  Make sure its visible.
                        {
                            if (verbose) System.Diagnostics.Debug.WriteLine("  Render " + g.Item1 + " shader " + kvp.Key.GetType().Name);

                            if (g.Item2.RenderControl == null)                      // if no render control, do not change rendercontrol
                            {
                                g.Item2.Bind(null, kvp.Key, c);
                            }
                            else if (object.ReferenceEquals(g.Item2.RenderControl, lastapplied))     // no point forcing the test of rendercontrol if its the same as last applied
                            {
                                g.Item2.Bind(null, kvp.Key, c);
                            }
                            else
                            {
                                g.Item2.Bind(currentstate, kvp.Key, c);             // change and remember
                                lastapplied = g.Item2.RenderControl;
                            }

                            g.Item2.Render();
                            //System.Diagnostics.Debug.WriteLine("....Render Over " + g.Item1);
                        }
                    }

                    kvp.Key.Finish();
                }
            }

            if (verbose) System.Diagnostics.Debug.WriteLine("***End RList");
            GL.UseProgram(0);           // final clean up
            GL.BindProgramPipeline(0);
        }

        private void AddItem(IGLProgramShader prog, string name, IGLRenderableItem r)
        {
            if (!renderables.ContainsKey(prog))
                renderables.Add(prog, new List<Tuple<string, IGLRenderableItem>>());

            var list = renderables[prog];

            renderables[prog].Add(new Tuple<string, IGLRenderableItem>(name, r));
        }

        private string EnsureName(string name, IGLProgramShader prog, IGLRenderableItem r)
        {
            return name.HasChars() ? name : (prog.GetType().Name + ":" + r.GetType().Name + " # " + (unnamed++).ToStringInvariant());
        }

        private Tuple<IGLProgramShader, int> Find(IGLRenderableItem r)        // find r in list
        {
            foreach (var kvp in renderables)
            {
                var i = kvp.Value.FindIndex(x => Object.ReferenceEquals(x.Item2, r));
                if (i >= 0)
                    return new Tuple<IGLProgramShader, int>(kvp.Key, i);
            }
            return null;
        }
    }

    // use this to just have a compute shader list - same as above, but can only add compute shaders
    public class GLComputeShaderList : GLRenderProgramSortedList        
    {
        public new void Add(IGLProgramShader prog, string name, IGLRenderableItem r)
        {
            System.Diagnostics.Debug.Assert(false, "Cannot add a normal shader to a compute shader list");
        }

        public new  void Add(IGLProgramShader prog, IGLRenderableItem r)
        {
            System.Diagnostics.Debug.Assert(false, "Cannot add a normal shader to a compute shader list");
        }
        public new void Add(GLOperationsBase nprog)
        {
            System.Diagnostics.Debug.Assert(false, "Cannot add an operation to a compute shader list");
        }

        public void Run()      
        {
            Render(null,null);
        }
    }
}

