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
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{

    // Pipeline shader for a 2D texture bound with 2D vertexes
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      tex binding X : textureObject : 2D texture

    public class GLPLFragmentShaderTexture : GLShaderPipelineShadersBase
    {
        public string Code(int binding)
        {
            return
@"
#version 450 core
layout (location=0) in vec2 vs_textureCoordinate;
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2D textureObject;

out vec4 color;

void main(void)
{
    color = texture(textureObject, vs_textureCoordinate);       // vs_texture coords normalised 0 to 1.0f
}
";
        }

        public GLPLFragmentShaderTexture(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), auxname: GetType().Name);
        }
    }

    // Pipeline shader for a 2D array texture, discard if alpha is too small or imageno < 0
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      location 1 : flat in imageno to select, <0 discard

    public class GLPLFragmentShaderTexture2DDiscard : GLShaderPipelineShadersBase
    {
        public string Code(int binding)
        {
            return
@"
#version 450 core

layout( location = 0 ) in vec2 vs_textureCoordinate;
layout( location = 1 ) flat in int imageno;      
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2DArray textureObject2D;

out vec4 color;

void main(void)
{
    if ( imageno < 0 )
        discard;
    else
    {   
        vec4 c = texture(textureObject2D, vec3(vs_textureCoordinate,imageno));       // vs_texture coords normalised 0 to 1.0f
        if ( c.w < 0.01)
            discard;
        else
            color = c;
    }   
}
";
        }

        public GLPLFragmentShaderTexture2DDiscard(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), auxname: GetType().Name);
        }
    }

    // Pipeline shader for a 2D Array texture bound using instance to pick between them. Use with GLVertexShaderTextureMatrixTransform
    // discard if alpha is too small or imageno < 0
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      location 2 : vs_in.vs_instance - instance id/texture offset. 
    //      location 3 : alpha (if alpha blend enabled) float
    //      tex binding 1 : textureObject : 2D texture

    public class GLPLFragmentShaderTexture2DIndexed : GLShaderPipelineShadersBase
    {
        public string Code()
        {
            return
@"
#version 450 core
layout (location=0) in vec2 vs_textureCoordinate;

// supports up to four texture bindings
const int texbinding = 1;
layout (binding=texbinding) uniform sampler2DArray textureObject2D;

layout (location = 2) in VS_IN
{
    flat int vs_instanced;      // not sure why structuring is needed..
} vs;

layout (location = 3) in float alpha;

out vec4 color;

const bool enablealphablend = false;
const int imageoffset = 0;

void main(void)
{
    int ii = vs.vs_instanced+imageoffset;
    vec4 cx;
    cx = texture(textureObject2D, vec3(vs_textureCoordinate,ii));
    if ( enablealphablend )
        cx.w *= alpha;
    if ( cx.w < 0.01)
        discard;
    else
        color = cx;
}
";
        }

        public GLPLFragmentShaderTexture2DIndexed(int offset, int binding = 1, bool alphablend = false)
        {
            CompileLink(ShaderType.FragmentShader, Code(), new object[] { "enablealphablend", alphablend, "texbinding",binding, "imageoffset",offset }, auxname: GetType().Name);
        }
    }

    // Pipeline shader for a 2D Array texture bound using instance to pick between them. Use with GLVertexShaderTextureMatrixTransform
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      location 2 : vs_in.vs_instance - instance id/texture offset. 
    //                  bits 0..15 = image index in texture, bits 16.. texture index in bindings array
    //      location 3 : alpha (if alpha blend enabled) float
    //      tex binding [N..] : textureObject : 2D textures, multiple ones. select

    public class GLPLFragmentShaderTexture2DIndexedMulti : GLShaderPipelineShadersBase
    {
        public string Code()
        {
            return
@"
#version 450 core
layout (location=0) in vec2 vs_textureCoordinate;

// supports up to N texture bindings
const int texbinding = 1;
const int texbindinglength = 10;
layout (binding=texbinding) uniform sampler2DArray textureObject2D[texbindinglength];       // note use of [], allows you to array as set of bindings

layout (location = 2) in VS_IN
{
    flat int vs_instanced;      // not sure why structuring is needed..
} vs;

layout (location = 3) in float alpha;

out vec4 color;

const bool enablealphablend = false;
const int imageoffset = 0;

void main(void)
{
    int ii = (vs.vs_instanced & 0xffff)+imageoffset;
    int tx = (vs.vs_instanced >> 16);
    vec4 cx = texture(textureObject2D[tx], vec3(vs_textureCoordinate,ii));
    if ( enablealphablend )
        cx.w *= alpha;
    color = cx;
//color = vec4(1,1,1,1);
}
";
        }

        public GLPLFragmentShaderTexture2DIndexedMulti(int offset, int binding = 1, bool alphablend = false, int maxtextures = 16)  // 16 is the opengl minimum textures supported
        {
            CompileLink(ShaderType.FragmentShader, Code(), new object[] { "enablealphablend", alphablend, "texbinding", binding, "imageoffset", offset, "texbindinglength", maxtextures }, auxname: GetType().Name);
        }
    }

    // Pipeline shader, 2d texture array (0,1), 2d o-ords, with blend between them via a uniform
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      tex binding 1 : textureObject : 2D array texture of two bitmaps, 0 and 1.
    //      location 30 : uniform float blend between the two texture

    public class GLPLFragmentShaderTexture2DBlend : GLShaderPipelineShadersBase
    {
        public string Code(int binding)
        {
            return
@"
#version 450 core
layout (location=0) in vec2 vs_textureCoordinate;
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2DArray textureObject;
layout (location = 30) uniform float blend;

out vec4 color;

void main(void)
{
    vec4 col1 = texture(textureObject, vec3(vs_textureCoordinate,0));
    vec4 col2 = texture(textureObject, vec3(vs_textureCoordinate,1));
    color = mix(col1,col2,blend);
}
";
        }

        public float Blend { get; set; } = 0.0f;

        public GLPLFragmentShaderTexture2DBlend(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform1(Id, 30, Blend);
        }
    }

    // Pipeline shader, Co-ords are from a triangle strip
    // either the vertex's are separated by primitive restart, 
    // or they are back to back in which case we need to invert text co-ord x for each other set of triangles
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord - as per triangle strip. Strip must start on a modulo 4 boundary. Must have at least 3 points.
    //      tex binding 1 : textureObject : 2D array texture of two bitmaps, 0 and 1.
    //      location 24 : uniform of texture offset (written by start automatically)

    public class GLPLFragmentShaderTextureTriangleStrip : GLShaderPipelineShadersBase
    {
        public string Code(bool backtoback, int binding)
        {
            return
@"
#version 450 core
layout (location=0) in vec2 vs_textureCoordinate;
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2D textureObject;
layout (location = 24) uniform  vec2 offset;

out vec4 color;

void main(void)
{" +
(backtoback ?
@"
// keep for debugging
//    vec4 col[] = { vec4(0.4,0,0,1), vec4(0,0.4,0,1), vec4(0,0,0.4,1), vec4(0.4,0.4,0,1),vec4(0,0.4,0.4,1), vec4(0.4,0.4,0.4,1),
//                   vec4(0.6,0.2,0,1), vec4(0,0.6,0,1), vec4(0,0,0.6,1), vec4(0.6,0.6,0,1),vec4(0,0.6,0.6,1), vec4(0.6,0.6,0.6,1),
//                   vec4(0.8,0.4,0,1), vec4(0,0.8,0,1), vec4(0,0,0.8,1), vec4(0.8,0.8,0,1),vec4(0,0.8,0.8,1), vec4(0.8,0.8,0.8,1),
//                   vec4(1.0,0.6,0,1), vec4(0,1.0,0,1), vec4(0,0,1.0,1), vec4(1.0,1.0,0,1),vec4(0,1.0,1.0,1), vec4(1.0,1.0,1.0,1),
//};
//if ( gl_PrimitiveID==12)
//color = vec4(1,1,1,1);
//else
//color = col[gl_PrimitiveID];

    if ( gl_PrimitiveID % 4 < 2 )   // first two primitives have coords okay
    {
        color = texture(textureObject, vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f
    }
    else    // next two have them inverted in x due to re-using the previous triangles vertexes
    {
        color = texture(textureObject, vec2(1.0-vs_textureCoordinate.x,vs_textureCoordinate.y)+offset);       // vs_texture coords normalised 0 to 1.0f
    }

" :
@"
        color = texture(textureObject, vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f
") +

"}";
}

        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        public GLPLFragmentShaderTextureTriangleStrip(bool backtobackrect, int binding=1)
        {
            CompileLink(ShaderType.FragmentShader, Code(backtobackrect,binding), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }
    }

    // Pipeline shader, Co-ords are from a triangle strip, we are assuming a primitive restart so we don't need to do the invert thingy
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord - as per triangle strip
    //      uniform binding <config>: ARB bindless texture handles, int 64s
    //      location 24 : uniform of texture offset (written by start automatically)

    public class GLPLBindlessFragmentShaderTextureTriangleStrip : GLShaderPipelineShadersBase
    {
        public string Code(int arbblock)
        {
            return
@"
#version 450 core
#extension GL_ARB_bindless_texture : require

layout (location=0) in vec2 vs_textureCoordinate;
layout (binding = " + arbblock.ToStringInvariant() + @", std140) uniform TEXTURE_BLOCK
{
    sampler2D tex[256];
};
layout (location = 24) uniform  vec2 offset;

out vec4 color;

void main(void)
{
    int objno = gl_PrimitiveID/2;
    color = texture(tex[objno], vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f
}
";
        }

        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        public GLPLBindlessFragmentShaderTextureTriangleStrip(int arbblock)
        {
            CompileLink(ShaderType.FragmentShader, Code(arbblock), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }
    }




}
