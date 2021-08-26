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


using System;

namespace GLOFC.GL4
{
    // Translation shader with vertex colours
    // Requires:
    //      location 0 vec4 positions of model
    //      location 1 vec4 colours of each vertex
    //      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    //      uniform 22 matrix4 transform of model->world positions, supply using per object binding

    public class GLColorShaderWithObjectTranslation : GLShaderPipeline
    {
        public GLColorShaderWithObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelCoordWithObjectTranslation(), new GLPLFragmentShaderVSColor());
        }
    }

    // Translation shader with vertex colours, fixed colour
    // Requires:
    //      location 0 vec4 positions of model
    //      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    //      uniform 22 matrix4 transform of model->world positions, supply using per object binding

    public class GLFixedColorShaderWithObjectTranslation : GLShaderPipeline
    {
        public GLFixedColorShaderWithObjectTranslation(System.Drawing.Color c, Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelCoordWithObjectTranslation(), new GLPLFragmentShaderFixedColor(c));
        }
    }

    // Translation shader with vertex colours, fixed colour
    // Requires:
    //      location 0 vec4 positions of model
    //      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock
    //      uniform 22 matrix4 transform of model->world positions, supply using per object binding
    //      uniform 25 colour of object

    public class GLUniformColorShaderWithObjectTranslation : GLShaderPipeline
    {
        public GLUniformColorShaderWithObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorModelCoordWithObjectTranslation(), new GLPLFragmentShaderUniformColor());
        }
    }

    // Fixed position shader with vertex colours
    // Requires:
    //      location 0 vec4 positions of world positions
    //      location 1 vec4 colours of each vertex
    //      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock

    public class GLColorShaderWithWorldCoord : GLShaderPipeline
    {
        public GLColorShaderWithWorldCoord(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderColorWorldCoord(), new GLPLFragmentShaderVSColor());
        }
    }

    // Fixed position shader with fixed colour
    // Requires:
    //      location 0 vec4 positions of world positions
    //      uniform 0 standard Matrix uniform block GLMatrixCalcUniformBlock

    public class GLFixedColorShaderWithWorldCoord : GLShaderPipeline
    {
        public GLFixedColorShaderWithWorldCoord(System.Drawing.Color c, Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderWorldCoord(), new GLPLFragmentShaderFixedColor(c));
        }
    }

}
