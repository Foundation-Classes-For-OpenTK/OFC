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


using System;
using OpenTK.Graphics.OpenGL4;

namespace OFC.GL4
{
    // The null shader is used to insert operations in the RenderList pipeline, so it can do some operations
    // outside of the GLRenderControl at the appropriate point (scissoring, stenciling)
    // Use StartAction to hook to supply more start functionality
    // Use can use FinishAction, but it executes after StartAction so there is no real point

    public class GLShaderNull : IGLProgramShader
    {
        public int Id { get { return 0; } }
        public bool Enable { get; set; } = true;                        // if not enabled, no render items below it will be visible

        public IGLShader Get(ShaderType t) { return this; }
        public Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }
        public Action<IGLProgramShader, GLMatrixCalc> FinishAction { get; set; }

        protected GLProgram program;

        public GLShaderNull()
        {
        }

        public GLShaderNull(Action<IGLProgramShader, GLMatrixCalc> sa) : this()
        {
            StartAction = sa;
        }

        public virtual void Start(GLMatrixCalc c)     
        {
            StartAction?.Invoke(this,c);
        }

        public virtual void Finish(GLMatrixCalc c)                 
        {
            FinishAction?.Invoke(this,c);                           // any shader hooks get a chance.
        }

        public virtual void Dispose()
        {
            program.Dispose();
        }

    }
}
