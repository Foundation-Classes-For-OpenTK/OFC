/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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

using GLOFC.GL4.Shaders;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

// Vertex shaders taking world positions

namespace GLOFC.GL4.Shaders.Vertex
{
    /// <summary>
    /// Shader, No extra translation, direct move, but with posibility of using external Y
    /// </summary>

    public class GLPLVertexShaderWorldCoord : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Constructor 
        /// Requires:
        ///      location 0 : vec4 positions (W ignored)
        ///      uniform buffer 0 : standard Matrix uniform block GLMatrixCalcUniformBlock
        ///      uniform 22 : float Y optional
        /// </summary>
        /// <param name="yfromuniform">True to take Y from uniform 22</param>
        public GLPLVertexShaderWorldCoord(bool yfromuniform = false)
        {
            CompileLink(ShaderType.VertexShader, Code(), out string unused, constvalues: new object[] { "yfromuniform", yfromuniform });
        }
        
        /// <summary> Set Y </summary>
        public void SetY(float y)
        {
            GL.ProgramUniform1(Id, 22, y);
        }

        private string Code()
        {
            return
@"
#version 450 core

#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout (location = 22) uniform  float replacementy;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

const bool yfromuniform = false;

void main(void)
{
    if ( yfromuniform )
        gl_Position = mc.ProjectionModelMatrix * vec4(position.x,replacementy,position.z,1);        // order important
    else
	    gl_Position = mc.ProjectionModelMatrix * position;        // order important
}
";
        }

    }

}
