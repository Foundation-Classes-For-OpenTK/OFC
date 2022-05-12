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

namespace GLOFC.GL4.Shaders.Volumetric
{
    /// <summary>
    /// This namespace contains pipeline volumentic shaders
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Vertex shader for volumetric shading.  
    /// </summary>

    public class GLPLVertexShaderVolumetric : GLShaderPipelineComponentShadersBase
    {
        string vcode =
        @"
#version 450 core

out flat int instance;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

void main(void)
{
    instance = gl_InstanceID;
}
            ";

        /// <summary> Constructor </summary>
        public GLPLVertexShaderVolumetric()
        {
            CompileLink(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader, vcode, out string unused);
        }
    }

    /// <summary>
    /// Geometric shader for volumetric shading
    /// Requires:
    ///     No vertex gl_position is fed in
    ///     Location 0: Instance count from vertex shader is used
    ///     uniform buffer: Volumetric info containing the point block and slice information - see shader
    /// </summary>
    public class GLPLGeometricShaderVolumetric : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bufferbindingpoint">Uniform buffer with volumetric info - see shader</param>
        public GLPLGeometricShaderVolumetric(int bufferbindingpoint)
        {
            CompileLink(ShaderType.GeometryShader, "#include Shaders.Volumetric.volumetricgeoshader.glsl", out string unused, new object[] { "bufferbp", bufferbindingpoint });
        }
    }
}

