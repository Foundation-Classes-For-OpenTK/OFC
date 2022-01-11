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

// Vertex shaders for screen coordinates

namespace GLOFC.GL4
{
    // Pipeline shader, Texture, real screen coords (0-glcontrol.Width,0-glcontrol.height, 0,0 at top left)
    // Requires:
    //      location 0 : position: vec4 vertex array of real screen coords in the x/y/z slots.  w is passed thru on out 3
    //      uniform buffer 0 : GL MatrixCalc with ScreenMatrix set up
    // Out:
    //      gl_Position
    //      location 0 : vec2 vs_textureCoordinate per triangle strip rules
    //      location 1 : worldpos
    //      location 2 : flat out vertexid to tell the frag shader what vertex its on instead of using primitive_ID which does not work with primitive restart (does no reset).
    //      location 3 : flat out w for fragshader.

    public class GLPLVertexShaderTextureScreenCoordWithTriangleStripCoord : GLShaderPipelineComponentShadersBase
    {
        public GLPLVertexShaderTextureScreenCoordWithTriangleStripCoord()
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
layout (location = 3) flat out float wvalue;

void main(void)
{
    wvalue = position.w;
    worldpos = position.xyz;
	gl_Position = mc.ScreenMatrix * vec4(position.xyz,1);        // order important
    vec2 vcoords[4] = {{0,0},{0,1},{1,0},{1,1} };      // these give the coords for the 4 points making up 2 triangles.  Use with the right fragment shader which understands strip co-ords
    vs_textureCoordinate = vcoords[ gl_VertexID % 4];
    vertexid = gl_VertexID;
}
";
        }

    }


}
