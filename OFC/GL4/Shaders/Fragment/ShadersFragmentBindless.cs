﻿/*
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
    // Pipeline shader, texture shader,
    // renders an texture ARB dependent either on primitive number/2 (so to be used with a triangle strip) or image id in location 2
    // with alphablending and discard if transparent
    // Requires:
    //      location 0 : vs_texturecoordinate : vec2 of texture co-ord 
    //      location 3 : flat in wvalue for opacity control (if enabled by usealphablending)
    //      location 4 : image id (if useprimidover2=false)
    //      uniform binding <config>: ARB bindless texture handles, int 64s
    //      location 24 : uniform of texture offset (written by start automatically)

    public class GLPLFragmentShaderBindlessTexture : GLShaderPipelineComponentShadersBase
    {
        public Vector2 TexOffset { get; set; } = Vector2.Zero;                   // set to animate.

        public GLPLFragmentShaderBindlessTexture(int arbblock, bool usealphablending = false, bool discardiftransparent = false, bool useprimidover2 = true)
        {
            CompileLink(ShaderType.FragmentShader, Code(arbblock), 
                        constvalues: new object[] { "usewvalue", usealphablending, "discardiftransparent", discardiftransparent, "useprimidover2", useprimidover2 }, 
                        auxname: GetType().Name);
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



}

