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
    public class GLPLStarSurfaceFragmentShader : GLShaderPipelineComponentShadersBase
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

        /// <summary> Constructor </summary>
        public GLPLStarSurfaceFragmentShader()
        {
            CompileLink(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader, Fragment(), auxname: GetType().Name);
        }

        /// <summary> Start shader</summary>
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

            GLOFC.GLStatics.Check();
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
}

