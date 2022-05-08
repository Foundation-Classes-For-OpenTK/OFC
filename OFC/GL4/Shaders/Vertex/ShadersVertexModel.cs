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

using GLOFC.GL4.Shaders;
using OpenTK.Graphics.OpenGL4;

// Vertex shaders, having a model input

namespace GLOFC.GL4.Shaders.Vertex
{
    /// <summary>
    /// This namespace contains pipeline vertex shaders
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Shader, Translation, Modelpos, transform
    /// </summary>

    public class GLPLVertexShaderModelObjectTranslation : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Constructor 
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions model coords, W is ignored
        ///      uniform buffer 0 : GL MatrixCalc
        ///      uniform 22 : objecttransform: mat4 array of transforms
        /// Out:
        ///      gl_Position
        ///      location 1: modelpos
        /// </summary>
        public GLPLVertexShaderModelObjectTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), out string unused);
        }

        private string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;

layout (location = 22) uniform  mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout (location = 1) out vec3 modelpos;

void main(void)
{
    modelpos = position.xyz;
	gl_Position = mc.ProjectionModelMatrix * transform * vec4(position.xyz,1);        // order important
}
";
        }

    }

}
