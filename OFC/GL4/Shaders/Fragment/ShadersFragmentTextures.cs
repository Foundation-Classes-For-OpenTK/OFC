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

using System.Drawing;
using GLOFC.Utils;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Fragment
{
    /// <summary>
    /// Shader for a 2D texture bound with 2D vertexes
    /// </summary>

    public class GLPLFragmentShaderTexture : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      texture : 2D texture bound on binding point
        /// </summary>
        /// <param name="binding">Binding of texture</param>
        public GLPLFragmentShaderTexture(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), out string unused);
        }

        private string Code(int binding)
        {
            return
@"
#version 450 core
layout (location =0) in vec2 vs_textureCoordinate;
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2D textureObject;

out vec4 color;

void main(void)
{
    color = texture(textureObject, vs_textureCoordinate);       // vs_texture coords normalised 0 to 1.0f
//color = vec4(1,vs_textureCoordinate.x,vs_textureCoordinate.y,1); // test to show tex co-ords come thru
}
";
        }

    }
    /// <summary>
    /// Shader for a 2D array texture with discard
    /// </summary>

    public class GLPLFragmentShaderTexture2DDiscard : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      location 1 : flat in imageno to select, less than 0 discard
        ///      texture : 2D texture bound on binding point
        /// </summary>
        /// <param name="binding">Binding of texture</param>
        /// <param name="alphazerocolour">Allows a default colour to show for low alpha samples</param>
        public GLPLFragmentShaderTexture2DDiscard(int binding = 1, Color? alphazerocolour = null)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding, alphazerocolour != null), out string unused, constvalues: new object[] { "alphacolor", alphazerocolour });
        }

        private string Code(int binding, bool alphacolor)
        {
            return
@"
#version 450 core

layout( location = 0 ) in vec2 vs_textureCoordinate;
layout( location = 1 ) in flat int imageno;      
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
(!alphacolor ? @"discard;" : @"color=alphacolor;")
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

    /// <summary>
    /// Shader for a 2D Array texture bound using instance to pick between them. Use with GLVertexShaderTextureMatrixTransform
    /// discard if alpha is too small or replace with colour
    /// </summary>

    public class GLPLFragmentShaderTexture2DIndexed : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      location 2 : vs_in.vs_instance - instance id to pick texture (added by offset in constructor)E. 
        ///      location 3 : alpha (if alpha blend enabled) float
        ///      tex binding : textureObject : 2D Array texture
        /// </summary>
        /// <param name="offset">Ofsset into texture array as base</param>
        /// <param name="binding">Texture binding point</param>
        /// <param name="alphablend">Allows alpha to be passed from the vertex shader to this</param>
        /// <param name="alphazerocolour">Allows a default colour to show for zero alpha samples</param>

        public GLPLFragmentShaderTexture2DIndexed(int offset, int binding = 1, bool alphablend = false, Color? alphazerocolour = null)
        {
            CompileLink(ShaderType.FragmentShader, Code(alphazerocolour != null), out string unused, new object[] { "enablealphablend", alphablend, "texbinding", binding, "imageoffset", offset, "alphacolor", alphazerocolour });
        }

        private string Code(bool alphacolor)
        {
            return
@"
#version 450 core
layout (location =0) in vec2 vs_textureCoordinate;
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
(!alphacolor ? @"discard;" : @"color=alphacolor;")
+
 @"
    }
    else
        color = cx;

}
";
        }

    }

    /// <summary>
    /// Shader for an array of 2D Array textures. vs_instance input is used to pick array and 2d depth image
    /// Discard if small alpha, use with GLVertexShaderTextureMatrixTransform
    /// </summary>

    public class GLPLFragmentShaderTexture2DIndexMulti : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor. 
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      location 2 : vs_in.vs_instance - instance id/texture offset. 
        ///                  bits 0..15 = image index in texture array, bits 16.. texture array to pick 
        ///      location 3 : alpha (if alpha blend enabled) float
        ///      tex binding [N..] : textureObject : 2D textures, multiple ones. select
        /// </summary>
        /// <param name="offset">Image offset in all texture array to start from</param>
        /// <param name="binding">Texture binding start for first of the 2D Textures bound</param>
        /// <param name="alphablend">Alpha blend enable, allow alpha to be passed from vertex shader to this shader</param>
        /// <param name="maxtextures">No of textures. Note 16 is the opengl minimum textures supported</param>
        public GLPLFragmentShaderTexture2DIndexMulti(int offset, int binding = 1, bool alphablend = false, int maxtextures = 16)  
        {
            CompileLink(ShaderType.FragmentShader, Code(), out string unused, new object[] { "enablealphablend", alphablend, "texbinding", binding, "imageoffset", offset, "texbindinglength", maxtextures });
        }

        private string Code()
        {
            return
@"
#version 450 core
layout (location =0) in vec2 vs_textureCoordinate;

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

    ///<summary>
    /// Shader, 2d texture array (0,1), 2d co-ords, with blend between them via a uniform
    ///</summary>

    public class GLPLFragmentShaderTexture2DBlend : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Blend percent between images </summary>
        public float Blend { get; set; } = 0.0f;

        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      tex binding : textureObject : 2D array texture of two bitmaps, 0 and 1.
        ///      location 30 : uniform float blend between the two texture
        /// </summary>
        /// <param name="binding">Tex binding number</param>
        public GLPLFragmentShaderTexture2DBlend(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), out string unused);
        }

        /// <summary> Start shader </summary>
        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform1(Id, 30, Blend);
        }

        private string Code(int binding)
        {
            return
@"
#version 450 core
layout (location =0) in vec2 vs_textureCoordinate;
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
    /// <summary>
    /// Shader for a 2D texture bound with a variable texture offset
    /// </summary>

    public class GLPLFragmentShaderTextureOffset : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Texture offset 0-1 </summary>
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      tex binding X : textureObject : 2D texture
        /// Pipeline shader, Co-ords are from a triangle strip
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord 
        ///      tex binding 1 : textureObject : 2D array texture of two bitmaps, 0 and 1.
        ///      location 24 : uniform of texture offset (written by start automatically)
        /// </summary>
        /// <param name="binding">Tex binding</param>
        public GLPLFragmentShaderTextureOffset(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(binding), out string unused);
        }

        /// <summary> Start shader</summary>
        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }

        private string Code(int binding)
        {
            return
@"
#version 450 core
layout (location =0) in vec2 vs_textureCoordinate;
layout (binding=" + binding.ToStringInvariant() + @") uniform sampler2D textureObject;
layout (location = 24) uniform  vec2 offset;
out vec4 color;

void main(void)
{
    color = texture(textureObject, vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f
}";
        }
    }

    /// <summary>
    /// Shader for a 2D Array texture bound using location 4 wvalue to pick between them. Use with GLPLVertexShaderModelWorldTextureAutoScale for instance
    /// discard if alpha is too small 
    /// </summary>

    public class GLPLFragmentShaderTexture2DWSelector : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      location 4 : float wvalue from world- bits 0..16 select texture.
        ///      tex binding : textureObject : 2D Array texture
        /// </summary>
        /// <param name="binding">Texture binding point</param>

        public GLPLFragmentShaderTexture2DWSelector(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(), out string unused, 
                            new object[] { "texbinding", binding });
        }

        private string Code()
        {
            return
@"
#version 450 core
layout (location =0) in vec2 vs_textureCoordinate;

const int texbinding = 1;
layout (binding=texbinding) uniform sampler2DArray textureObject2D;

layout (location = 4) in VS_IN
{
    flat float vs_wvalue;
} vs;

out vec4 color;

void main(void)
{
    int imageno = int(vs.vs_wvalue);
    vec4 c = texture(textureObject2D, vec3(vs_textureCoordinate,imageno));       // vs_texture coords normalised 0 to 1.0f
    if ( c.w < 0.01)
    {
        discard;
    }
    else
        color = c;
}
";
        }

    }



}

