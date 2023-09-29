/*
 * Copyright 2019-2023 Robbyxp1 @ github.com
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

namespace GLOFC.GL4.Shaders.Fragment
{
    /// <summary>
    /// This namespace contains pipeline fragment shaders.
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Pipeline shader, texture shader,
    /// renders an texture ARB dependent either on primitive number/2 (so to be used with a triangle strip) or image id in location 2
    /// With alphablending and discard if transparent
    /// </summary>

    public class GLPLFragmentShaderBindlessTexture : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Offset of texture in object, 0-1 </summary>
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord 
        ///      location 3 : flat in wvalue for opacity control (if enabled by usealphablending)
        ///      location 4 : image id (if useprimidover2=false)
        ///      uniform binding config: ARB bindless texture handles, int 64s
        ///      location 24 : uniform of texture offset (written by start automatically)
        /// </summary>
        /// <param name="arbblock">Binding number of ARB Block</param>
        /// <param name="usealphablending">Alpha blend on/off</param>
        /// <param name="discardiftransparent">Discard if pixel is low intensity</param>
        /// <param name="useprimidover2">Use primitive/2 as texture object number, else use location 4</param>
        public GLPLFragmentShaderBindlessTexture(int arbblock, bool usealphablending = false, bool discardiftransparent = false, bool useprimidover2 = true)
        {
            CompileLink(ShaderType.FragmentShader, Code(arbblock), out string unused,
                        constvalues: new object[] { "usewvalue", usealphablending, "discardiftransparent", discardiftransparent, "useprimidover2", useprimidover2 });
        }

        /// <summary> Shader start </summary>
        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }

        private string Code(int arbblock)
        {
            return
@"
#version 450 core
#extension GL_ARB_bindless_texture : require

layout (location =0) in vec2 vs_textureCoordinate;
layout (location =3) flat in float wvalue;
layout (location = 4) in VS_IN
{
    flat int imageno;
} vs_in;
layout (binding = " + arbblock.ToStringInvariant() + @", std140) uniform TEXTURE_BLOCK
{
    sampler2D tex[256];
};
layout (location = 24) uniform  vec2 offset;

out vec4 color;
const bool usewvalue = false;
const bool discardiftransparent = false;
const bool useprimidover2 = true;

void main(void)
{
    int objno = useprimidover2 ? gl_PrimitiveID/2 : vs_in.imageno;
    color = texture(tex[objno], vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f
//    color = vec4(1,0,0,1);
    if ( discardiftransparent && color.w < 0.0001)
        discard;
    else if ( usewvalue)
        color.w *= wvalue;
}
";
        }
    }


    /// <summary>
    /// Shader for Bindless ARB textures. 
    /// Must have a GLBindlessTextureHandleBlock on the arbbinding point with ARB Texture IDs filled in
    /// Use with GLPLVertexShaderMatrixTriStripTexture for instance
    /// discard if alpha is too small or replace with colour
    /// </summary>

    public class GLPLFragmentShaderTextureBindlessIndexed : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Offset of texture in object, 0-1 </summary>
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      location 2 : vs_in.vs_instance - instance id to pick bindless texture from a block (added by offset in constructor). 
        ///      location 3 : alpha (if alpha blend enabled) float
        ///      location 24 : uniform of texture offset (written by start automatically)
        ///      tex binding : ARB bindless texture block
        /// </summary>
        /// <param name="offset">Ofsset into texture array as base</param>
        /// <param name="arbbinding">Texture binding point</param>
        /// <param name="alphablend">Allows alpha to be passed from the vertex shader to this</param>
        /// <param name="alphazerocolour">Allows a default colour to show for zero alpha samples</param>
        /// <param name="maxbitmaps">Maximum number of bitmaps supported</param>

        public GLPLFragmentShaderTextureBindlessIndexed(int offset, int arbbinding = 1, bool alphablend = false, Color? alphazerocolour = null, int maxbitmaps = 1024)
        {
            CompileLink(ShaderType.FragmentShader, Code(alphazerocolour != null), out string unused, 
                new object[] { "enablealphablend", alphablend, "texbinding", arbbinding, "imageoffset", offset, "alphacolor", alphazerocolour, "maxbitmaps", maxbitmaps });
        }

        /// <summary> Shader start </summary>
        public override void Start(GLMatrixCalc c)
        {
            GL.ProgramUniform2(Id, 24, TexOffset);
        }

        private string Code(bool alphacolor)
        {
            return
@"
#version 460 core
#extension GL_ARB_bindless_texture : require

layout (location =0) in vec2 vs_textureCoordinate;
layout (location = 2) in VS_IN
{
    flat int vs_instanced;      // not sure why structuring is needed..
} vs;
layout (location = 3) in float alpha;
layout (location = 24) uniform  vec2 offset;

const int texbinding = 1;
const int maxbitmaps = 0;
layout (binding = texbinding, std140) uniform TEXTURE_BLOCK
{
    sampler2D tex[maxbitmaps];
};

out vec4 color;

const vec4 alphacolor = vec4(1,1,1,1);
const bool enablealphablend = false;
const int imageoffset = 0;

void main(void)
{
    int objno = vs.vs_instanced+imageoffset;
    vec4 cx;
    cx = texture(tex[objno], vs_textureCoordinate+offset);       // vs_texture coords normalised 0 to 1.0f

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



}

