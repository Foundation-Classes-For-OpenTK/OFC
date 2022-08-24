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

using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Fragment;
using System;
using OpenTK;

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
    /// </summary>

    public class GLColorShaderObjectTranslation : GLShaderPipeline
    {
        /// <summary> Constructor 
        /// Requires:
        ///      location 0 vec4 positions of model
        ///      location 1 vec4 colours of each vertex
        ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
        ///      uniform 22 matrix4 transform of model->world positions, supply using per object binding
        /// </summary>
        /// <param name="start">Start shader call back</param>
        /// <param name="finish">Finish shader call back</param>
        public GLColorShaderObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelObjectTranslation(), new GLPLFragmentShaderVSColor());
        }
    }

    /// <summary>
    /// Translation shader with vertex colours, fixed colour
    /// </summary>

    public class GLFixedColorShaderObjectTranslation : GLShaderPipeline
    {
        /// <summary> Constructor. Give color 
        /// Requires:
        ///      location 0 vec4 positions of model
        ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
        ///      uniform 22 matrix4 transform of model->world positions, supply using per object binding
        /// </summary>
        /// <param name="color">Color to shade with</param>
        /// <param name="start">Start shader call back</param>
        /// <param name="finish">Finish shader call back</param>
        public GLFixedColorShaderObjectTranslation(System.Drawing.Color color, Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelObjectTranslation(), new GLPLFragmentShaderFixedColor(color));
        }
    }

    /// <summary>
    /// Translation shader with vertex colours, fixed colour
    /// </summary>

    public class GLUniformColorShaderObjectTranslation : GLShaderPipeline
    {
        /// <summary> Constructor 
        /// Requires:
        ///      location 0 vec4 positions of model
        ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
        ///      uniform 22 matrix4 transform of model->world positions, supply using per object binding
        ///      uniform 25 colour of object
        /// </summary>
        /// <param name="start">Start shader call back</param>
        /// <param name="finish">Finish shader call back</param>
        public GLUniformColorShaderObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelObjectTranslation(), new GLPLFragmentShaderUniformColor());
        }
    }

    /// <summary>
    /// Fixed position shader with vertex colours
    /// </summary>

    public class GLColorShaderWorld : GLShaderPipeline
    {
        /// <summary> Constructor 
        /// Requires:
        ///      location 0 vec4 positions of world positions
        ///      location 1 vec4 colours of each vertex
        ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
        /// </summary>
        /// <param name="start">Start shader call back</param>
        /// <param name="finish">Finish shader call back</param>
        /// <param name="worldoffset">True to add a world offset to vertex positions. Use SetOffset</param>
        public GLColorShaderWorld(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null, bool worldoffset = false) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorWorldCoord(worldoffset), new GLPLFragmentShaderVSColor());
        }

        /// <summary>
        /// Set a world offset if enabled
        /// </summary>
        /// <param name="offset">World offset for all vertexes</param>
        public void SetOffset(Vector3 offset)
        {
            this.GetShader<GLPLVertexShaderColorWorldCoord>().SetOffset(offset);
        }
    }

    /// <summary>
    /// Fixed position shader with fixed colour
    /// </summary>

    public class GLFixedColorShaderWorld : GLShaderPipeline
    {
        /// <summary> Constructor. Give color 
        /// Requires:
        ///      location 0 vec4 positions of world positions
        ///      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
        /// </summary>
        /// <param name="color">Color to shade with</param>
        /// <param name="start">Start shader call back</param>
        /// <param name="finish">Finish shader call back</param>
        /// <param name="worldoffset">True to add a world offset to vertex positions. Use SetOffset</param>
        public GLFixedColorShaderWorld(System.Drawing.Color color, Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null, bool worldoffset = false) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderWorldCoord(worldoffset), new GLPLFragmentShaderFixedColor(color));
        }

        /// <summary>
        /// Set a world offset if enabled
        /// </summary>
        /// <param name="offset">World offset for all vertexes</param>
        public void SetOffset(Vector3 offset)
        {
            this.GetShader<GLPLVertexShaderColorWorldCoord>().SetOffset(offset);
        }

    }

}
