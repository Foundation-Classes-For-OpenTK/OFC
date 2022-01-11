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
    ///  Pipeline shader, Texture, Modelpos, transform
    ///  Requires:
    ///       location 0 : position: vec4 vertex array of positions. W contains encoded red, next green, blue in 24 bit quantity
    ///       uniform buffer 0 : GL MatrixCalc
    ///  Out:
    ///       gl_Position
    ///       location 0 : vs_textureCoordinate per triangle strip rules - use a fragment shader which understands the order (GLPLFragmentShaderTextureTriStrip)
    ///       location 1 : worldpos
    ///       location 2 : flat out vertexid to tell the frag shader what vertex its on instead of using primitive_ID which does not work with primitive restart (does no reset).
    ///       location 3 : flat out color carried in vertex as a packed RGB value from input location 0 w co-ord
    /// </summary>

    public class GLPLVertexShaderTextureWorldCoordWithTriangleStripCoordWRGB : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Constructor </summary>
        public GLPLVertexShaderTextureWorldCoordWithTriangleStripCoordWRGB()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        private string Code()
        {
            return

@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout(location = 0) out vec2 vs_textureCoordinate;
layout(location = 1) out vec3 worldpos;
layout(location = 2) flat out int vertexid;
layout(location = 3) flat out vec4 colorout;

void main(void)
{
    vec2 vcoords[4] = {{0,0},{0,1},{1,0},{1,1}};        // these give the coords for the 4 points making up 2 triangles.  Use with the right fragment shader which understands strip co-ords

    worldpos = position.xyz;
    vec4 p = vec4(position.xyz,1);
	gl_Position = mc.ProjectionModelMatrix * p;        // order important
    vs_textureCoordinate = vcoords[ gl_VertexID % 4];  // Very important. gl_vertextid is either an autocounter for non indexed addressing, starting at zero, 
                                                       // for index addressing, its the actual element index given in an element draw
    vertexid = gl_VertexID;

    int cv = int(position.w);
    colorout = vec4( (cv&0xff)/255.0, ((cv>>8)&0xff)/255.0, ((cv>>16)&0xff)/255.0, 1);      // unpack to vec4
}
";
        }

    }

}
