﻿/*
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

using GLOFC.GL4.Shaders;
using GLOFC.Utils;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Tesselation
{
    /// <summary>
    /// This namespace contains pipeline tesselation shaders
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Shader, Tesselation , select tess level
    /// Requires:
    ///     gl_in
    ///     1: worldposinstance
    ///     2: instance
    /// Output:
    ///     gl_out
    ///     1: tcs_worldposinstance
    ///     2: tcs_instance 
    /// </summary>

    public class GLPLTesselationControl : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tess">Tesselation level</param>
        public GLPLTesselationControl(float tess)
        {
            CompileLink(ShaderType.TessControlShader, TCS(tess));
        }

        private string TCS(float tesselation)
        {
            return
        @"
#version 450 core

layout (vertices = 4) out;

in gl_PerVertex
{
  vec4 gl_Position;
  float gl_PointSize;
  float gl_ClipDistance[];
} gl_in[];

out gl_PerVertex
{
  vec4 gl_Position;
  float gl_PointSize;
  float gl_ClipDistance[];
} gl_out[];

layout( location = 1 ) in vec4 worldposinstance[];         // pass thru this array. TCS is run one per vertex
layout( location = 2 ) in int instance[];

layout( location = 1 ) out vec4 tcs_worldposinstance[];
layout( location = 2 ) out int tcs_instance[];

void main(void)
{
    float tess = " + tesselation.ToStringInvariant() + @";

    if ( gl_InvocationID == 0 )
    {
        gl_TessLevelInner[0] =  tess;
        gl_TessLevelInner[1] =  tess;
        gl_TessLevelOuter[0] =  tess;
        gl_TessLevelOuter[1] =  tess;
        gl_TessLevelOuter[2] =  tess;
        gl_TessLevelOuter[3] =  tess;
    }

    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
    tcs_worldposinstance[gl_InvocationID] = worldposinstance[gl_InvocationID];
    tcs_instance[gl_InvocationID] = instance[gl_InvocationID];
}
";
        }


    }

}
