/*
 * Copyright 2019-2011 Robbyxp1 @ github.com
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

namespace OFC.GL4
{
    // operations are actions that can be inserted into the renderlist either as a shader (robjects.Add(new Operation())  )
    // or as a Renderable item (robject.Add(shader, new Operation()) )
    // Classes derived from this use DoOperation(matrixcalc) to perform work at that point in the render

    public abstract class GLOperationsBase : IGLRenderableItem, IGLProgramShader
    {
        // active inherited
        public bool Visible { get; set; } = true;                           // is visible (in this case active)
        public bool Enable { get => Visible; set => Visible = value; }      // use Visible, so same as
        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; } // may be used by derived classes. Check your derived class
        public int Id { get; set; }                                         // may be used by derived classes if its needs an Id

        // inactive when attached to render

        public GLRenderState RenderState { get; set; } = null;          // Not used, must be null
        public IGLRenderItemData RenderData { get; set; }                   // Not used
        public int DrawCount { get; set; } = 0;                             // Not used
        public int InstanceCount { get; set; } = 0;                         // Not used

        // inactive when attached to shader
        public Action<IGLProgramShader> FinishAction { get; set; }        // not used
        public string Name => "Operation";
        public IGLShader Get(ShaderType t) { throw new NotImplementedException(); }

        public GLOperationsBase()
        {
        }

        abstract public void Execute(GLMatrixCalc c);       // actual call to derived classes to do operation

        // when attached as a render item

        public void Bind(GLRenderState currentstate, IGLProgramShader shader, GLMatrixCalc c)      
        {
            Execute(c);
        }

        public void Render()        // no action on render
        {
        }

        // When attached as a shader

        public void Start(GLMatrixCalc c)
        {
            Execute(c);
        }

        public void Finish()  // no action on finish
        {
        }

        public virtual void Dispose()               // no dispose on base
        {
        }
    }
}

