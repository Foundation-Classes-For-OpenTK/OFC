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

namespace GLOFC.GL4
{
    // this is a render list, holding a list of Shader programs
    // each shader program is associated with zero or more RenderableItems 
    // The shader calls Start() for each shader, then goes thru the render list (if it has one) , setting up the render control, then Binding and Rendering each item
    // then it Finish() the shader
    // Shaders are executed in the order added, and all renderable items below them are executed in order added to that shader
    // you can decide to force the normal renderable items to be added to the end of the list (creating a duplicate shader at the end if required) instead of the first instance of the shader
    // Compute shaders are always added onto the end the end of the shader list
    // Operations added in a shader slot are always added onto the end the end of the shader list
    // you can add an operation to the render list of a shader as well.

    public class GLRenderProgramSortedList
    {
        private List<Tuple<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>>> renderables;
        private Dictionary<string,IGLRenderableItem> byname;
        private int unnamed = 0;
        private IntPtr context = (IntPtr)0;

        public GLRenderProgramSortedList()
        {
            renderables = new List<Tuple<IGLProgramShader, List<Tuple<string, IGLRenderableItem>>>>();
            byname = new Dictionary<string, IGLRenderableItem>();
        }

        // name can be null if required, which gives it an autoname
        public void Add(IGLProgramShader prog, string name, IGLRenderableItem r, bool atend = false)        // name is the id given to this renderable
        {
            System.Diagnostics.Debug.Assert(r != null);
            name = EnsureName(name, prog, r);
            //System.Diagnostics.Debug.WriteLine($"Add render {prog.Name} {name}");
            AddItem(prog, name, r,atend, true);
            byname.Add(name, r);
        }

        // with autoname
        public void Add(IGLProgramShader prog, IGLRenderableItem r, bool atend = false)
        {
            System.Diagnostics.Debug.Assert(r != null);
            AddItem(prog, EnsureName(null,prog,r), r, atend, true);
        }

        // a compute shader, always added at end
        public void Add(GLShaderCompute cprog)  
        {
            string n = "CS " + cprog.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(cprog, n, null,true,false);     // must be at end, and must not join
        }

        // a operation in a shader slot
        public void Add(GLOperationsBase nprog)
        {
            string n = "OP " + nprog.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(nprog, n, null, true, false);  // must be at end, and must not join
        }

        // a operation in a render item slot
        public void Add(IGLProgramShader shader, GLOperationsBase nprog, bool atend = false)
        {
            string n = "OP-RI " + nprog.GetType().Name + " # " + (unnamed++).ToStringInvariant();
            AddItem(shader, n, nprog, atend, true);  // must be at end, and can join at end
        }

        public bool Remove(IGLRenderableItem r)     
        {
            var f = Find(r);
            return r != null ? Remove(f.Item1, r) : false;
        }

        public void Clear()
        {
            byname.Clear();
            renderables.Clear();
        }

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

        public IGLRenderableItem this[string renderitem] { get { return byname[renderitem]; } }
        public bool Contains(string renderitem) { return byname.ContainsKey(renderitem); }

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

    // use this to just have a compute shader list - same as above, but can only add compute shaders
    public class GLComputeShaderList : GLRenderProgramSortedList        
    {
        public new void Add(IGLProgramShader prog, string name, IGLRenderableItem r, bool atend = false)
        {
            System.Diagnostics.Debug.Assert(false, "Cannot add a normal shader to a compute shader list");
        }

        public new  void Add(IGLProgramShader prog, IGLRenderableItem r, bool atend = false)
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

