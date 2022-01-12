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

namespace GLOFC.GL4.Operations
{
    /// <summary>
    /// Operations are actions that can be inserted into the renderlist either as a shader (robjects.Add(new Operation()))
    /// or as a Renderable item (robject.Add(shader, new Operation()) ).  They can execute tasks
    /// such as clearing the depth buffer or controlling stencilling and will be executed in line with the renders
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes
    
    /// <summary>
    /// This is the base class for Operations.
    /// Classes derived from this use Execute(matrixcalc) to perform work at that point in the render
    /// As this inherits from both IGLRenderableItem and IGLProgramShader there many unused interfaces
    /// </summary>

    public abstract class GLOperationsBase : IGLRenderableItem, IGLProgramShader
    {
        /// <summary> Set to enable or disable this operation when inside a queue (same as Enable)</summary>
        public bool Visible { get; set; } = true;                           
        /// <summary> Set to enable or disable this operation when inside a queue (same as Visible)</summary>
        public bool Enable { get => Visible; set => Visible = value; }      

        /// <summary> Called before the operation is about to execute </summary>
        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }

        /// <summary> Called after the operation has executed </summary>
        public Action<IGLProgramShader> FinishAction { get; set; }        // not used

        /// <summary> ID, if applicable to operation </summary>
        public int Id { get; set; } = -1;           

        // inactive when attached to render

        /// <summary> Inherited, Not Applicable to this class </summary>
        public GLRenderState RenderState { get; set; } = null;          // Not used, must be null
        /// <summary> Inherited, Not Applicable to this class </summary>
        public IGLRenderItemData RenderData { get; set; }                   // Not used
        /// <summary> Inherited, Not Applicable to this class </summary>
        public int DrawCount { get; set; } = 0;                             // Not used
        /// <summary> Inherited, Not Applicable to this class </summary>
        public int InstanceCount { get; set; } = 0;                         // Not used

        // inactive when attached to shader

        /// <summary> Name - fixed as Operation </summary>
        public string Name => "Operation";
        /// <summary> Not applicable to this class </summary>
        public IGLShader GetShader(ShaderType t) { throw new NotImplementedException(); }
        /// <summary> Not applicable to this class </summary>
        public T GetShader<T>(OpenTK.Graphics.OpenGL4.ShaderType t) where T : IGLShader { throw new NotImplementedException(); }    // get a subcomponent of type T. Excepts if not present
        /// <summary> Not applicable to this class </summary>
        public T GetShader<T>() where T : IGLShader { throw new NotImplementedException(); }    // get a subcomponent of type T. Excepts if not present
        /// <summary> Not applicable to this class </summary>
        public byte[] GetBinary(out BinaryFormat binformat) { throw new NotImplementedException(); }
        /// <summary> Not applicable to this class </summary>
        public void Load(byte[] bin, BinaryFormat binformat) { throw new NotImplementedException(); }

        /// <summary> Construct an empty operation </summary>
        public GLOperationsBase()
        {
        }

        /// <summary> Defined by specific operation to execute the operation</summary>
        abstract public void Execute(GLMatrixCalc c);       // actual 

        // when attached as a render item

        /// <summary> Called when operation is attached as a renderable item, execute StartAction and operation</summary>
        public void Bind(GLRenderState currentstate, IGLProgramShader shader, GLMatrixCalc c)      
        {
            StartAction?.Invoke(this, c);
            Execute(c);
            FinishAction?.Invoke(this);
        }

        /// <summary> Called when operation is attached as a renderable item, no action</summary>
        public void Render()    
        {
        }

        // When attached as a shader

        /// <summary> Called when operation is attached as a shader</summary>
        public void Start(GLMatrixCalc c)
        {
            StartAction?.Invoke(this, c);
            Execute(c);
            FinishAction?.Invoke(this);
        }

        /// <summary> Called, no action </summary>
        public void Finish()  // no action on finish
        {
        }

        /// <summary> No action in this class </summary>
        public virtual void Dispose()           
        {
        }
    }
}

