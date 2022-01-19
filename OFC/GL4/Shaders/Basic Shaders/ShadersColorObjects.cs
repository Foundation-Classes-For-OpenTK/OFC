/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
 * 
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

using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Fragment;
using System;

namespace GLOFC.GL4.Shaders.Basic
{
    /// <summary>
    /// This namespace contains complete basic shaders for:
    /// * color.
    /// * texture.
    /// * model and world translations. 
    /// * Sinewave tesselations.
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Translation shader with vertex colours
    /// Requires:
    ///      location 0 vec4 positions of model
    ///      location 1 vec4 colours of each vertex
    ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    ///      uniform 22 matrix4 transform of model->world positions, supply using per object binding
    /// </summary>

    public class GLColorShaderObjectTranslation : GLShaderPipeline
    {
        /// <summary> Constructor </summary>
        public GLColorShaderObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelObjectTranslation(), new GLPLFragmentShaderVSColor());
        }
    }

    /// <summary>
    /// Translation shader with vertex colours, fixed colour
    /// Requires:
    ///      location 0 vec4 positions of model
    ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    ///      uniform 22 matrix4 transform of model->world positions, supply using per object binding
    /// </summary>

    public class GLFixedColorShaderObjectTranslation : GLShaderPipeline
    {
        /// <summary> Constructor. Give color </summary>
        public GLFixedColorShaderObjectTranslation(System.Drawing.Color c, Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelObjectTranslation(), new GLPLFragmentShaderFixedColor(c));
        }
    }

    /// <summary>
    /// Translation shader with vertex colours, fixed colour
    /// Requires:
    ///      location 0 vec4 positions of model
    ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    ///      uniform 22 matrix4 transform of model->world positions, supply using per object binding
    ///      uniform 25 colour of object
    /// </summary>

    public class GLUniformColorShaderObjectTranslation : GLShaderPipeline
    {
        /// <summary> Constructor </summary>
        public GLUniformColorShaderObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelObjectTranslation(), new GLPLFragmentShaderUniformColor());
        }
    }

    /// <summary>
    /// Fixed position shader with vertex colours
    /// Requires:
    ///      location 0 vec4 positions of world positions
    ///      location 1 vec4 colours of each vertex
    ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    /// </summary>

    public class GLColorShaderWorld : GLShaderPipeline
    {
        /// <summary> Constructor </summary>
        public GLColorShaderWorld(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorWorldCoord(), new GLPLFragmentShaderVSColor());
        }
    }

    /// <summary>
    /// Fixed position shader with fixed colour
    /// Requires:
    ///      location 0 vec4 positions of world positions
    ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    /// </summary>

    public class GLFixedColorShaderWorld : GLShaderPipeline
    {
        /// <summary> Constructor. Give color </summary>
        public GLFixedColorShaderWorld(System.Drawing.Color c, Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderWorldCoord(), new GLPLFragmentShaderFixedColor(c));
        }
    }

}
