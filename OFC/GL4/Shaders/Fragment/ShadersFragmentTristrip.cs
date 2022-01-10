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


using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace GLOFC.GL4
{
    // Pipeline shader, Co-ords are from a triangle strip, with a vertexid which must be modulo 4 aligned for the first primitive. 
    // vertexid allows the shader to adjust to the front/back nature of the auto coords fed to it (00 01 10 11 .. 00 01 10 11)
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord 
    //      location 2 : flat in vertexid, used to work out the primitive.  vertex's id starts must be module 4 aligned
    //      tex binding : textureObject : 2D array texture of two bitmaps, 0 and 1.
    //      location 24 : uniform of texture offset (written by start automatically)

    public class GLPLFragmentShaderTextureTriStrip : GLShaderPipelineComponentShadersBase
    {
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        public GLPLFragmentShaderTextureTriStrip(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }

        public string Code(int binding)
        {
            return
    @"
#version 450 core
layout (location=0) in vec2 vs_textureCoordinate;
layout (location=2) flat in int vertexid;

layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2D textureObject;
layout (location = 24) uniform  vec2 offset;
out vec4 color;

// problem is, vs_textureCoordinate.x is the right way across the texture for P1P2 but backwards for P3P4..
//    01     11     01      11
//    | \P2  | \P4  |  \P6  |
//    |  \   |  \   |   \   |
//    |P1 \  |P3 \  |P5  \  |
//    |    \ |    \ |     \ |
//    00    10     00      10
// P1 = 00 01 10 vs_texturecoord goes forward
// P2 = 01 10 11 vs_texturecoord goes forward
// P3 = 10 11 00 vs_texturecoord goes backwards
// P4 = 11 00 01 vs_texturecoord goes backwards
// P5 = 00 01 10 vs_texturecoord goes forward
// P6 = 01 10 11 vs_texturecoord goes forward
// we need a way to tell if we are on primitive 1+2 vs 3+4.  You can use gl_PrimitiveID, but that does not 
// work for primitive restarts (ID goes not reset to 0 on restart)
// we instead get a flat in vertex id, aligned to a mod 4 boundary on p-restart, and use that

void main(void)
{
    if ( (vertexid & 2) !=0 )   // vertex modulo 0/1 are backwards, 2/3 forwards
    {
        color = texture(textureObject, vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f, texture co-ords are forwards
    }
    else    
    {
        color = texture(textureObject, vec2(1.0-vs_textureCoordinate.x,vs_textureCoordinate.y)+offset);       // texture co-ords backwards in x
    }
}";

        }
    }

    // Pipeline shader, Co-ords are from a triangle strip, with a vertexid which must be modulo 4 aligned for the first primitive. 
    // vertexid allows the shader to adjust to the front/back nature of the auto coords fed to it (00 01 10 11 .. 00 01 10 11)
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord 
    //      location 2 : flat in vertexid, used to work out the primitive.  vertex's id starts must be module 4 aligned
    //      location 3 : colour for the replacement value
    //      tex binding : textureObject : 2D array texture of two bitmaps, 0 and 1.
    //      location 24 : uniform of texture offset (written by start automatically)

    public class GLPLFragmentShaderTextureTriStripColorReplace : GLShaderPipelineComponentShadersBase
    {
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        public GLPLFragmentShaderTextureTriStripColorReplace(int binding, Color replace)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), constvalues:new object[] { "replace", replace }, auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }

        public string Code(int binding) => @"
#version 450 core
layout (location=0) in vec2 vs_textureCoordinate;
layout (location=2) flat in int vertexid;
layout (location=3) flat in vec4 colorin;

layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2D textureObject;
layout (location = 24) uniform  vec2 offset;

const vec4 replace = vec4(0,0,0,0);
out vec4 color;

void main(void)
{
    if ( (vertexid & 2) !=0 )   // vertex modulo 0/1 are backwards, 2/3 forwards
    {
        color = texture(textureObject, vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f, texture co-ords are forwards
    }
    else    
    {
        color = texture(textureObject, vec2(1.0-vs_textureCoordinate.x,vs_textureCoordinate.y)+offset);       // texture co-ords backwards in x
    }

    vec4 deltac = color - replace;     // floats are not precise, find the difference
    if ( deltac.x*deltac.x+deltac.y*deltac.y+deltac.z*deltac.z<0.001)       // if close, square wise (not using length(v) due to speed)
        color = colorin;

}";

    }

}