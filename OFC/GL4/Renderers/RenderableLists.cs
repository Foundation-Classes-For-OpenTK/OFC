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

using GLOFC.GL4.Shaders;
using GLOFC.Utils;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace GLOFC.GL4
{
    /// <summary>
    /// This is a render list, holding a list of Shader programs
    /// Each shader program is associated with zero or more RenderableItems 
    /// The shader calls Start() for each shader, then goes thru the render list (if it has one) , setting up the render control, then Binding and Rendering each item
    /// then it calls Finish() on the shader and moves onto the next one.
    /// Shaders are executed in the order added, and all renderable items below them are executed in order added to that shader (unless overriden by atend flag)
    /// You can decide to force the normal renderable items to be added to the end of the list (creating a duplicate shader at the end if required) instead of the first instance of the shader
    /// Compute shaders are always added onto the end the end of the shader list
    /// Operations added in a shader slot are always added onto the end the end of the shader list
    /// You can add an operation to the render list of a shader as well.
    /// </summary>

    public class GLRenderProgramSortedList
    {
        private List<Tuple<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>>> renderables;
        private Dictionary<string,IGLRenderableItem> byname;
        private int unnamed = 0;
        private IntPtr context = (IntPtr)0;

        /// <summary>Create a render program sorted list</summary>
        public GLRenderProgramSortedList()
        {
            renderables = new List<Tuple<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>>>();
            byname = new Dictionary<string, IGLRenderableItem>();
        }

        /// <summary>Add a shader and renderable item to the list</summary>
        /// <param name="prog">Shader program</param>
        /// <param name="name">Name of renderable item, may be null in which case it will automatically be named</param>
        /// <param name="renderableitem">The render to execute under this shader</param>
        /// <param name="atend">Force the render to be the last in the current queue. If false, and a shader already is in the queue, then its placed at the end of that shader list.</param>
        public void Add(IGLProgramShader prog, string name, IGLRenderableItem renderableitem, bool atend = false)       
        {
            System.Diagnostics.Debug.Assert(renderableitem != null);
            name = EnsureName(name, prog, renderableitem);
            //System.Diagnostics.Debug.WriteLine($"Add render {prog.Name} {name}");
            AddItem(prog, name, renderableitem,atend, true);
            byname.Add(name, renderableitem);
        }

        /// <summary>Add a shader and renderable item to the list</summary>
        /// <param name="prog">Shader program</param>
        /// <param name="renderableitem">The render to execute under this shader</param>
        /// <param name="atend">Force the render to be the last in the current queue. If false, and a shader already is in the queue, then its placed at the end of that shader list.</param>
        public void Add(IGLProgramShader prog, IGLRenderableItem renderableitem, bool atend = false)
        {
            System.Diagnostics.Debug.Assert(renderableitem != null);
            AddItem(prog, EnsureName(null,prog,renderableitem), renderableitem, atend, true);
        }

        /// <summary>Add a compute shader at the end of the list</summary>
        public void Add(GLShaderCompute computeshader)  
        {
            string n = "CS " + computeshader.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(computeshader, n, null,true,false);     // must be at end, and must not join
        }

        /// <summary>Add a operation, always at the end of the current list of renders. Operation is added in the shader slot.</summary>
        public void Add(GLOperationsBase operation)
        {
            string n = "OP " + operation.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(operation, n, null, true, false);  // must be at end, and must not join
        }

        /// <summary>Add an operation at the end of the list of a particular shader. </summary>
        /// <param name="shader">Shader to associate the operation with</param>
        /// <param name="operation">Operation</param>
        /// <param name="atend">Force the operation to be the last in the current queue. If false, and a shader already is in the queue, then its placed at the end of that shader list.</param>
        public void Add(IGLProgramShader shader, GLOperationsBase operation, bool atend = false)
        {
            string n = "OP-RI " + operation.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(shader, n, operation, atend, true);  // must be at end, and can join at end
        }

        /// <summary>Remove the render from the list</summary>
        public bool Remove(IGLRenderableItem r)     
        {
            var f = Find(r);
            return r != null ? Remove(f.Item1, r) : false;
        }

        /// <summary>Clear the render queue</summary>
        public void Clear()
        {
            byname.Clear();
            renderables.Clear();
        }

        /// <summary>Remove the shader/render item</summary>
        public bool Remove(IGLProgramShader prog, IGLRenderableItem r)
        {
            Tuple<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>> found = renderables.Find(x => x.Item1 == prog); // find the shader
            if ( found != null )
            {
                var list = found.Item2;  // list of tuples of <name,RI>
                var i = list.FindIndex(x => Object.ReferenceEquals(x.Item2, r));        // find renderable in list

                if ( i >= 0)
                {
                    byname.Remove(list[i].Item1);   // remove name
                    list.RemoveAt(i);

                    //foreach (var s in renderables[prog]) System.Diagnostics.Debug.WriteLine($"left .. {prog.Name} {s.Item1}");

                    if ( list.Count == 0 )     // if nothing more in shader
                    {
                        //System.Diagnostics.Debug.WriteLine($"remove shader {prog.Name}");
                        renderables.Remove(found);           // remove shader
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>Find the render item by name</summary>
        public IGLRenderableItem this[string renderitem] { get { return byname[renderitem]; } }

        /// <summary>Does the render queue contain this named render</summary>
        public bool Contains(string renderitem) { return byname.ContainsKey(renderitem); }

        /// <summary>Execute the render list, given the render state, matrix calc. Optional verbose debug output mode </summary>
        public void Render(GLRenderState currentstate, GLMatrixCalc c, bool verbose = false)
        {
            if (verbose) System.Diagnostics.Trace.WriteLine("***Begin RList");

            GLStatics.Check();      // ensure clear before start
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context not correct for render list");     // double check context

            GLRenderState lastapplied = null;
            IGLProgramShader curshader = null;

            foreach (var shaderri in renderables)        // kvp of Key=Shader, Value = list of renderables
            {
                // shader must be enabled
                if (shaderri.Item1.Enable)
                {
                    if (shaderri.Item2 == null)                             // indicating its a compute or operation             
                    {
                        if (shaderri.Item1 is GLShaderCompute)              // if compute
                        {
                            if (curshader != null)                          // turn off current shader
                            {
                                curshader.Finish();
                                curshader = null;
                            }

                            if (verbose) System.Diagnostics.Trace.WriteLine("  Compute Shader " + shaderri.Item1.GetType().Name);

                            shaderri.Item1.Start(c);                        // start/finish it
                            shaderri.Item1.Finish();
                        }
                        else
                        {
                            if (verbose) System.Diagnostics.Trace.WriteLine("  Operation " + shaderri.Item1.GetType().Name);
                            shaderri.Item1.Start(c);                        // operations just start, but don't change the current shader
                        }
                    }
                    else if (shaderri.Item2.Find(x=>x.Item2.Visible)!=null)      // must have a list, all must not be null, and some must be visible
                    {
                        if (curshader != shaderri.Item1)                                // if a different shader instance
                        {
                            if (curshader != null)                                      // finish the last one if present
                                curshader.Finish();
                            curshader = shaderri.Item1;
                            curshader.Start(c);                                         // start the program - if compute shader, or operation, this executes the code
                        }
                        //System.Diagnostics.Trace.WriteLine("Shader " + kvp.Item1.GetType().Name);

                        foreach (var g in shaderri.Item2)
                        {
                            GLOFC.GLStatics.Check();

                            if (g.Item2 != null && g.Item2.Visible)                    // Make sure its visible and not empty slot
                            {
                                if (verbose) System.Diagnostics.Trace.WriteLine("  Bind " + g.Item1 + " shader " + shaderri.Item1.GetType().Name);
                                if (g.Item2.RenderState == null)                       // if no render control, do not change last applied.
                                {
                                    g.Item2.Bind(null, shaderri.Item1, c);
                                }
                                else if (object.ReferenceEquals(g.Item2.RenderState, lastapplied))     // no point forcing the test of rendercontrol if its the same as last applied
                                {
                                    g.Item2.Bind(null, shaderri.Item1, c);
                                }
                                else
                                {
                                    g.Item2.Bind(currentstate, shaderri.Item1, c);      // change and remember
                                    lastapplied = g.Item2.RenderState;
                                }

                                if (verbose) System.Diagnostics.Trace.WriteLine("  Render " + g.Item1 + " shader " + shaderri.Item1.GetType().Name);
                                g.Item2.Render();
                                //System.Diagnostics.Trace.WriteLine("....Render Over " + g.Item1);
                            }
                            else
                            {
                                if (verbose) System.Diagnostics.Trace.WriteLine("  Not visible " + g.Item1 + " " + shaderri.Item1.GetType().Name);
                            }

                            GLOFC.GLStatics.Check();
                        }
                    }
                    else
                    {
                        if (verbose) System.Diagnostics.Trace.WriteLine("  No items visible " + shaderri.Item1.GetType().Name);
                    }
                }
                else
                {
                    if (verbose) System.Diagnostics.Trace.WriteLine("  Shader disabled " + shaderri.Item1.GetType().Name + " " + shaderri.Item1.Name);
                }
            }

            if (curshader != null)
                curshader.Finish();

            if (verbose) System.Diagnostics.Trace.WriteLine("***End RList");
            GL.UseProgram(0);           // final clean up
            GL.BindProgramPipeline(0);
        }

        // add a shader, under a name, indicate if at end, and if allowed to end join
        private void AddItem(IGLProgramShader prog, string name, IGLRenderableItem r, bool atend, bool allowendjoin)
        {
            if (context == (IntPtr)0)           // on add, see which context we are in
                context = GLStatics.GetContext();
            else
                System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context not correct for AddItem");     // double check cross windowing

            Tuple<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>> shaderpos = null;
            if ( atend )        // must be at end
            {
                if ( allowendjoin && renderables.Count>0 && renderables[renderables.Count-1].Item1 == prog) // if its the last one, we can add to it, otherwise we must make new
                    shaderpos = renderables[renderables.Count - 1]; 
            }
            else
                shaderpos = renderables.Find(x => x.Item1 == prog); // find the shader, may be null

            if (shaderpos == null)
            {
                renderables.Add(new Tuple<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>>(prog, r == null ? null : new List<Tuple<string, IGLRenderableItem>>()));
                shaderpos = renderables[renderables.Count - 1];
            }

            if ( r != null )
                shaderpos.Item2.Add(new Tuple<string, IGLRenderableItem>(name, r));
        }

        private string EnsureName(string name, IGLProgramShader prog, IGLRenderableItem r)
        {
            return name.HasChars() ? name : (prog.GetType().Name + ":" + r.GetType().Name + " # " + (unnamed++).ToStringInvariant());
        }

        private Tuple<IGLProgramShader, int> Find(IGLRenderableItem r)        // find r in list
        {
            foreach (var kvp in renderables)
            {
                if (kvp.Item2 != null)          // this may be null if its a shader operation or compute shader
                {
                    var i = kvp.Item2.FindIndex(x => Object.ReferenceEquals(x.Item2, r));
                    if (i >= 0)
                        return new Tuple<IGLProgramShader, int>(kvp.Item1, i);
                }
            }
            return null;
        }
    }

    /// <summary>A compute shader list. Use Run() to execute all compute shaders</summary>
    public class GLComputeShaderList : GLRenderProgramSortedList        
    {

        // disallow these adds.
        private new void Add(IGLProgramShader prog, string name, IGLRenderableItem r, bool atend = false)
        {
            System.Diagnostics.Debug.Assert(false, "Cannot add a normal shader to a compute shader list");
        }

        private new  void Add(IGLProgramShader prog, IGLRenderableItem r, bool atend = false)
        {
            System.Diagnostics.Debug.Assert(false, "Cannot add a normal shader to a compute shader list");
        }
        private new void Add(GLOperationsBase nprog)
        {
            System.Diagnostics.Debug.Assert(false, "Cannot add an operation to a compute shader list");
        }

        /// <summary>Execute all compute shaders in the list. Remember to use memory barriers before reading results</summary>
        public void Run()      
        {
            Render(null,null);
        }
    }
}

