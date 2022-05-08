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

using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Vertex
{
    /// <summary>
    /// Shader, Texture, Modelpos, transform
    /// </summary>

    public class GLPLVertexShaderWorldTexture : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Constructor 
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions
        ///      location 1 : vec2 texture co-ords
        ///      uniform buffer 0 :  : GL MatrixCalc
        /// Out:
        ///      gl_Position
        ///      location 0 : vs_textureCoordinate
        ///      location 1 : modelpos
        /// </summary>
        public GLPLVertexShaderWorldTexture()
        {
            CompileLink(ShaderType.VertexShader, Code(), out string unused);
        }

        private string Code()
        {
            return

@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout(location = 1) in vec2 texco;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout(location = 0) out vec2 vs_textureCoordinate;
layout(location = 1) out vec3 modelpos;

void main(void)
{
    modelpos = position.xyz;
	gl_Position = mc.ProjectionModelMatrix * position;        // order important
    vs_textureCoordinate = texco;
}
";
        }

    }

}
