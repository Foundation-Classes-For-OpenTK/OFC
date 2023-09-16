/*
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

using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Stars
{
    /// <summary>
    /// Star surface shader
    /// </summary>
    public class GLPLStarSurfaceColorFragmentShader : GLShaderPipelineComponentShadersBase
    {
        /// <summary>Time delta for surface, move to make it animate </summary>
        public float TimeDeltaSurface { get; set; } = 0;
        /// <summary>Time delta for spots, move to make it animate </summary>
        public float TimeDeltaSpots { get; set; } = 0;

        /// <summary> Set to make Freq/UnRadius/Scutoff/Blackdeepness/Concentrationequator be updated</summary>
        public bool UpdateControls { get; set; } = true;
        /// <summary> Spots, higher, more but small</summary>
        public float Frequency { get; set; } = 0.00005f;   
        /// <summary> Spots, Lower, more diffused </summary>
        public float UnRadius { get; set; } = 200000;
        /// <summary> Spots, Bar to pass, lower more, higher lots 0.4 lots, 0.6 few</summary>
        public float Scutoff { get; set; } = 0.5f;          
        /// <summary> How dark is each spot</summary>
        public float Blackdeepness { get; set; } = 8;
        /// <summary> Spots, how spread out </summary>
        public float Concentrationequator { get; set; } = 4;

        /// <summary> Constructor
        /// a GLPLVertexShaderModelCoordWorldAutoscale is normally used to drive this
        /// Requires:
        ///      location 1 : vec3 model position - know what fragment we are drawing
        ///      location 3 : vec4 basecolour
        ///      uniform 10 : frequency
        ///      uniform 11 : Radius km
        ///      uniform 12 : Scutoff figure
        ///      uniform 13 : black deepness
        ///      uniform 14 : equator concentration factor
        ///      uniform 15 : time delta to iterate image for surface
        ///      uniform 16 : time delta to iterate image for spots
        /// </summary>
        public GLPLStarSurfaceColorFragmentShader()
        {
            CompileLink(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader, Fragment(), out string unused);
        }

        /// <summary>
        /// Start function for shader to program uniforms
        /// </summary>
        /// <param name="c">Matrix calc</param>
        public override void Start(GLMatrixCalc c)
        {
            if ( UpdateControls )
            {
                GL.ProgramUniform1(Id, 10, Frequency);
                GL.ProgramUniform1(Id, 11, UnRadius);
                GL.ProgramUniform1(Id, 12, Scutoff);
                GL.ProgramUniform1(Id, 13, Blackdeepness);
                GL.ProgramUniform1(Id, 14, Concentrationequator);
                UpdateControls = false;
            }

            GL.ProgramUniform1(Id, 15, TimeDeltaSurface);
            GL.ProgramUniform1(Id, 16, TimeDeltaSpots);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        private string Fragment()
        {
            return
@"
#version 450 core
layout (location = 1) in vec3 modelpos;
layout (location = 3) in vec4 basecolor;
out vec4 color;

layout (location = 10) uniform float frequency;
layout (location = 11) uniform float unRadius;      // km
layout (location = 12) uniform float s;
layout (location = 13) uniform float blackdeepness;
layout (location = 14) uniform float concentrationequator;
layout (location = 15) uniform float unDTsurface;
layout (location = 16) uniform float unDTspots;

#include Shaders.Functions.snoise3.glsl

void main(void)
{
    vec3 position = normalize(modelpos);        // normalise model vectors

    float theta = dot(vec3(0,1,0),position);    // dotp between cur pos and up -1 to +1, 0 at equator
    theta = abs(theta);                         // uniform around equator.

    float clip = s + (theta/concentrationequator);               // clip sets the pass criteria to do the sunspots
    vec3 sPosition = (position + unDTspots) * unRadius;
    float t1 = simplexnoise(sPosition * frequency) -clip;
    float t2 = simplexnoise((sPosition + unRadius) * frequency) -clip;
	float ss = (max(t1, 0.0) * max(t2, 0.0)) * blackdeepness;

    vec3 p1 = vec3(position.x+unDTsurface,position.y,position.z);   // moving the noise across x produces a more realistic look
    float n = (simplexnoise(p1, 4, 40.0, 0.7) + 1.0) * 0.5;      // noise of surface..

    vec3 b = basecolor.xyz;
    b = b - ss - n/4;
    color = vec4(b, basecolor.w);
}
";
        }


    }


    /// <summary>
    /// Shader with subspots and star surface
    /// For a 2D Array texture bound using location 4 to pick base image and control sunspots
    /// Use with GLPLVertexShaderModelWorldTextureAutoScale for instance
    /// discard if alpha is too small 
    /// </summary>

    public class GLPLFragmentShaderTexture2DWSelectorSunspot : GLShaderPipelineComponentShadersBase
    {
        /// <summary>Time delta for surface, move to make it animate </summary>
        public float TimeDeltaSurface { get; set; } = 0;
        /// <summary>Time delta for spots, move to make it animate </summary>
        public float TimeDeltaSpots { get; set; } = 0;

        /// <summary> Set to make Freq/UnRadius/Scutoff/Blackdeepness/Concentrationequator be updated
        /// </summary>
        public bool UpdateControls { get; set; } = true;
        /// <summary> Spots, higher, more but small</summary>
        public float Frequency { get; set; } = 1f;
        /// <summary> Spots, Lower, more diffused, higher, more but smaller. Use this to set the size </summary>
        public float UnRadius { get; set; } = 8;
        /// <summary> Spots, Bar to pass, lower more, higher lots 0.4 lots, 0.6 few</summary>
        public float Scutoff { get; set; } = 0.25f;
        /// <summary> How dark is each spot</summary>
        public float Blackdeepness { get; set; } = 4;
        /// <summary> Spots, how spread out </summary>
        public float Concentrationequator { get; set; } = 4;

        /// <summary>
        /// Constructor.
        /// Requires:
        ///      location 0 : vs_texturecoordinate : vec2 of texture co-ord
        ///      location 3 : float wvalue from world- bits 0..15 select texture.  bit 16 = don't run sun shader
        ///      uniform 10 : frequency
        ///      uniform 11 : Radius km
        ///      uniform 12 : Scutoff figure
        ///      uniform 13 : black deepness
        ///      uniform 14 : equator concentration factor
        ///      uniform 15 : time delta to iterate image for surface
        ///      uniform 16 : time delta to iterate image for spots///      
        ///      tex binding : textureObject : 2D Array texture
        /// </summary>
        /// <param name="binding">Texture binding point</param>

        public GLPLFragmentShaderTexture2DWSelectorSunspot(int binding = 1)
        {
            CompileLink(ShaderType.FragmentShader, Code(), out string unused,
                            new object[] { "texbinding", binding });
        }

        /// <summary>
        /// Start function for shader to program uniforms
        /// </summary>
        /// <param name="c">Matrix calc</param>

        public override void Start(GLMatrixCalc c)
        {
            if (UpdateControls)
            {
                GL.ProgramUniform1(Id, 10, Frequency);
                GL.ProgramUniform1(Id, 11, UnRadius);
                GL.ProgramUniform1(Id, 12, Scutoff);
                GL.ProgramUniform1(Id, 13, Blackdeepness);
                GL.ProgramUniform1(Id, 14, Concentrationequator);
                UpdateControls = false;
            }

            GL.ProgramUniform1(Id, 15, TimeDeltaSurface);
            GL.ProgramUniform1(Id, 16, TimeDeltaSpots);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }


        private string Code()
        {
            return
@"
#version 450 core
layout (location =0) in vec2 vs_textureCoordinate;
layout (location =1) in vec3 vs_modelpos;

const int texbinding = 1;
layout (binding=texbinding) uniform sampler2DArray textureObject2D;

layout (location = 3) in VS_IN
{
    flat float vs_wvalue;
} vs;

layout (location = 10) uniform float frequency;
layout (location = 11) uniform float unRadius;      // km
layout (location = 12) uniform float s;
layout (location = 13) uniform float blackdeepness;
layout (location = 14) uniform float concentrationequator;
layout (location = 15) uniform float unDTsurface;
layout (location = 16) uniform float unDTspots;

#include Shaders.Functions.snoise3.glsl

out vec4 color;

void main(void)
{
    int imageno = int(vs.vs_wvalue);
    bool nosunshader = imageno > 0xffff;
    imageno &= 0xffff;

    vec4 basecolor = texture(textureObject2D, vec3(vs_textureCoordinate,imageno));       // vs_texture coords normalised 0 to 1.0f
    if ( basecolor.w < 0.01)
    {
        discard;
    }
    else
    {
        if ( nosunshader)
        {
            color = basecolor;
        }
        else
        {
            vec3 position = normalize(vs_modelpos);        // normalise model vectors

            float theta = dot(vec3(0,1,0),position);    // dotp between cur pos and up -1 to +1, 0 at equator
            theta = abs(theta);                         // uniform around equator.

            float clip = s + (theta/concentrationequator);               // clip sets the pass criteria to do the sunspots
            vec3 sPosition = (position + unDTspots) * unRadius;
            float t1 = simplexnoise(sPosition * frequency) -clip;
            float t2 = simplexnoise((sPosition + unRadius) * frequency) -clip;
	        float ss = (max(t1, 0.0) * max(t2, 0.0)) * blackdeepness;

            vec3 p1 = vec3(position.x+unDTsurface,position.y,position.z);   // moving the noise across x produces a more realistic look
            float n = (simplexnoise(p1, 4, 40.0, 0.7) + 1.0) * 0.5;      // noise of surface..

            vec3 b = basecolor.xyz;
            b = b - ss - n/4;
            color = vec4(b, basecolor.w);
        }
    }
//color = vec4(1,0.1,0.2,0.3);
}
";
        }

    }


}

