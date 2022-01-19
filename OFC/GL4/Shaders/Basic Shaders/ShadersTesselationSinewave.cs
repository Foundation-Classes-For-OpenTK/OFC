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

using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.Utils;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Basic
{
    /// <summary>
    /// Shader, with tesselation, and Y change in amp using sin 
    /// </summary>
    public class GLTesselationShaderSinewave : GLShaderStandard
    {
        string vert =
        @"
#version 450 core
layout (location = 0) in vec4 position;

void main(void)
{
	gl_Position =position;       
}
";

        string TCS(float tesselation)
        {
            return
        @"
#version 450 core

layout (vertices = 4) out;

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
}
";
        }

        string TES(float amplitude, float repeats)
        {
            return

@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl
layout (quads) in; 

layout (location = 22) uniform  mat4 transform;
layout (location = 26) uniform  float phase;

out vec2 vs_textureCoordinate;

void main(void)
{
    float amp = " + amplitude.ToStringInvariant() + @";
    vs_textureCoordinate = vec2(gl_TessCoord.x,1.0-gl_TessCoord.y);         //1.0-y is due to the project model turning Y upside down so Y points upwards on screen

    vec4 p1 = mix(gl_in[0].gl_Position, gl_in[1].gl_Position, gl_TessCoord.x);  
    vec4 p2 = mix(gl_in[2].gl_Position, gl_in[3].gl_Position, gl_TessCoord.x); 
    vec4 pos = mix(p1, p2, gl_TessCoord.y);                                     

    pos.y += amp*sin((phase+gl_TessCoord.x)*3.142*2*" + repeats.ToStringInvariant() + @");           // .x goes 0-1, phase goes 0-1, convert to radians

    gl_Position = mc.ProjectionModelMatrix * transform * pos;

}
";
        }

        string frag =

@"
#version 450 core
in vec2 vs_textureCoordinate;
out vec4 color;
layout (binding=1) uniform sampler2D textureObject;

void main(void)
{
    color = texture(textureObject, vs_textureCoordinate);       // vs_texture coords normalised 0 to 1.0f
}
";

        /// <summary>
        /// Phase of tesselation, in radians
        /// </summary>
        public float Phase { get; set; } = 0;                   // set to animate.

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tesselation">Tesselation amount</param>
        /// <param name="amplitude">Amplitude value</param>
        /// <param name="repeats">Repeats across object</param>
        public GLTesselationShaderSinewave(float tesselation, float amplitude, float repeats)
        {
            CompileLink(vertex: vert, tcs: TCS(tesselation), tes: TES(amplitude, repeats), frag: frag);
        }

        /// <summary>
        /// Start, bind and program uniforms
        /// </summary>
        public override void Start(GLMatrixCalc c)
        {
            base.Start(c);
            GL.ProgramUniform1(Id, 26, Phase);
            GLStatics.Check();
        }

        /// <summary>
        /// Finish shader
        /// </summary>
        public override void Finish()
        {
            base.Finish();
        }
    }
}
