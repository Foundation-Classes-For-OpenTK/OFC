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
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    // Pipeline shader for a 2D texture bound with 2D vertexes
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      tex binding X : textureObject : 2D texture

    public class GLPLFragmentShaderTexture : GLShaderPipelineComponentShadersBase
    {
        public GLPLFragmentShaderTexture(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), auxname: GetType().Name);
        }

        private string Code(int binding)
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

    }

    // Pipeline shader for a 2D array texture, discard if alpha is too small or imageno < 0
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      location 1 : flat in imageno to select, <0 discard

    public class GLPLFragmentShaderTexture2DDiscard : GLShaderPipelineComponentShadersBase
    {
        // alphazerocolor allows a default colour to show for zero alpha samples

        public GLPLFragmentShaderTexture2DDiscard(int binding = 1, Color? alphazerocolour = null)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding, alphazerocolour != null), constvalues: new object[] { "alphacolor", alphazerocolour }, auxname: GetType().Name);
        }

        public string Code(int binding, bool alphacolor)
        {
            return
@"
#version 450 core

layout( location = 0 ) in vec2 vs_textureCoordinate;
layout( location = 1 ) flat in int imageno;      
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2DArray textureObject2D;

out vec4 color;

const vec4 alphacolor = vec4(1,1,1,1);

void main(void)
{
    if ( imageno < 0 )
        discard;
    else
    {   
        vec4 c = texture(textureObject2D, vec3(vs_textureCoordinate,imageno));       // vs_texture coords normalised 0 to 1.0f
        if ( c.w < 0.01)
        {
"
+
((!alphacolor) ? @"discard;" : @"color=alphacolor;")
+
 @"           
        }
        else
            color = c;

    }   
}
";
        }

    }

    // Pipeline shader for a 2D Array texture bound using instance to pick between them. Use with GLVertexShaderTextureMatrixTransform
    // discard if alpha is too small or replace with colour
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      location 2 : vs_in.vs_instance - instance id/texture offset. 
    //      location 3 : alpha (if alpha blend enabled) float
    //      tex binding : textureObject : 2D texture

    public class GLPLFragmentShaderTexture2DIndexed : GLShaderPipelineComponentShadersBase
    {
        // alphablend allows alpha to be passed from the vertex shader to this
        // alphazerocolor allows a default colour to show for zero alpha samples

        public GLPLFragmentShaderTexture2DIndexed(int offset, int binding = 1, bool alphablend = false, Color? alphazerocolour = null)
        {
            CompileLink(ShaderType.FragmentShader, Code(alphazerocolour != null), new object[] { "enablealphablend", alphablend, "texbinding", binding, "imageoffset", offset, "alphacolor", alphazerocolour }, auxname: GetType().Name);
        }

        public string Code(bool alphacolor)
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

const vec4 alphacolor = vec4(1,1,1,1);
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
    {
"
+
((!alphacolor) ? @"discard;" : @"color=alphacolor;")
+
 @"
    }
    else
        color = cx;

}
";
        }

    }

    // Pipeline shader for an array of 2D Array textures.
    // vs_instance input is used to pick array and 2d depth image
    // Discard if small alpha
    // Use with GLVertexShaderTextureMatrixTransform
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      location 2 : vs_in.vs_instance - instance id/texture offset. 
    //                  bits 0..15 = image index in texture, bits 16.. texture index in bindings array
    //      location 3 : alpha (if alpha blend enabled) float
    //      tex binding [N..] : textureObject : 2D textures, multiple ones. select

    public class GLPLFragmentShaderTexture2DIndexedMulti : GLShaderPipelineComponentShadersBase
    {
        // alphablend allows alpha to be passed from the vertex shader to this
        public GLPLFragmentShaderTexture2DIndexedMulti(int offset, int binding = 1, bool alphablend = false, int maxtextures = 16)  // 16 is the opengl minimum textures supported
        {
            CompileLink(ShaderType.FragmentShader, Code(), new object[] { "enablealphablend", alphablend, "texbinding", binding, "imageoffset", offset, "texbindinglength", maxtextures }, auxname: GetType().Name);
        }

        private string Code()
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

    if ( cx.w < 0.01)
    {
        discard;
    }
    else    
        color = cx;
//color = vec4(1,1,1,1);
}
";
        }

    }

    // Pipeline shader, 2d texture array (0,1), 2d o-ords, with blend between them via a uniform
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      tex binding 1 : textureObject : 2D array texture of two bitmaps, 0 and 1.
    //      location 30 : uniform float blend between the two texture

    public class GLPLFragmentShaderTexture2DBlend : GLShaderPipelineComponentShadersBase
    {
        public float Blend { get; set; } = 0.0f;

        public GLPLFragmentShaderTexture2DBlend(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform1(Id, 30, Blend);
        }

        private string Code(int binding)
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

    }

    // Pipeline shader for a 2D texture bound with a variable texture offset
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord
    //      tex binding X : textureObject : 2D texture

    // Pipeline shader, Co-ords are from a triangle strip
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord 
    //      tex binding 1 : textureObject : 2D array texture of two bitmaps, 0 and 1.
    //      location 24 : uniform of texture offset (written by start automatically)

    public class GLPLFragmentShaderTextureOffset : GLShaderPipelineComponentShadersBase
    {
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        // for normal triangle strips, backtobackrect = false.  Only used for special element index draws

        public GLPLFragmentShaderTextureOffset(int binding = 1)
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
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2D textureObject;
layout (location = 24) uniform  vec2 offset;
out vec4 color;

void main(void)
{
    color = texture(textureObject, vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f
}";
        }

    }

    // Pipeline shader, texture shader, renders an texture ARB dependent on primitive number/2, so to be used with a triangle strip
    // 
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord 
    //      location 3 : flat in wvalue for opacity control (if enabled by usealphablending)
    //      uniform binding <config>: ARB bindless texture handles, int 64s
    //      location 24 : uniform of texture offset (written by start automatically)

    public class GLPLBindlessFragmentShaderTextureOffsetArray : GLShaderPipelineComponentShadersBase
    {
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        public GLPLBindlessFragmentShaderTextureOffsetArray(int arbblock, bool usealphablending = false, bool discardiftransparent = false)
        {
            CompileLink(ShaderType.FragmentShader, Code(arbblock), constvalues:new object[] { "usewvalue", usealphablending, "discardiftransparent", discardiftransparent }, auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }

        public string Code(int arbblock)
        {
            return
@"
#version 450 core
#extension GL_ARB_bindless_texture : require

layout (location=0) in vec2 vs_textureCoordinate;
layout (location=3) flat in float wvalue;
layout (binding = " + arbblock.ToStringInvariant() + @", std140) uniform TEXTURE_BLOCK
{
    sampler2D tex[256];
};
layout (location = 24) uniform  vec2 offset;

out vec4 color;
const bool usewvalue = false;
const bool discardiftransparent = false;

void main(void)
{
    int objno = gl_PrimitiveID/2;
    color = texture(tex[objno], vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f

    if ( discardiftransparent && color.w < 0.0001)
        discard;
    else if ( usewvalue)
        color.w *= wvalue;
}
";
        }

    }



}

