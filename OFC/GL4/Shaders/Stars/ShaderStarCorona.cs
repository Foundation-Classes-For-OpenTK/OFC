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

namespace OFC.GL4
{
    public class GLShaderStarCorona : GLShaderStandard
    {
        const int BindingPoint = 1;

        public string Vertex()
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;

layout (location = 21) uniform  mat4 rotate;
layout (location = 22) uniform  mat4 transform;

layout (location =0) out vec3 fposition;

void main(void)
{
    fposition =vec3(position.xz,0);
    vec4 p1 = rotate * position;
	gl_Position = mc.ProjectionModelMatrix * transform * p1;        // order important
}
";
        }

        public string Fragment()
        {
            return
@"
#version 450 core

#include Shaders.Functions.snoise4.glsl

layout (location =0 ) in vec3 fposition;
out vec4 color;

layout (location = 15) uniform float unDT;

void main(void)
{
	const float brightnessMultiplier = 0.9;   // The higher the number, the brighter the corona will be.
	const float smootheningMultiplier = 0.15; // How smooth the irregular effect is, the higher the smoother.
	const float ringIntesityMultiplier = 2.8; // The higher the number, the smaller the solid ring inside
	const float coronaSizeMultiplier = 2.0;  // The higher the number, the smaller the corona. 2.0
	const float frequency = 1.5;              // The frequency of the irregularities.
	const float fDetail = 0.7;                // The higher the number, the more detail the corona will have. (Might be more GPU intensive when higher, 0.7 seems fine for the normal PC)
	const int iDetail = 10;                   // The higher the number, the more detail the corona will have.
	const float irregularityMultiplier = 4;   // The higher the number, the more irregularities and bigger ones. (Might be more GPU intensive when higher, 4 seems fine for the normal PC)

	/* Don't edit these */

    float t = unDT - length(fposition);

    // Offset normal with noise
    float ox = simplexnoise(vec4(fposition, t) * frequency);
    float oy = simplexnoise(vec4((fposition + (1000.0 * irregularityMultiplier)), t) * frequency);
    float oz = simplexnoise(vec4((fposition + (2000.0 * irregularityMultiplier)), t) * frequency);
	float om = simplexnoise(vec4((fposition + (4000.0 * irregularityMultiplier)), t) * frequency) * simplexnoise(vec4((fposition + (250.0 * irregularityMultiplier)), t) * frequency);
    vec3 offsetVec = vec3(ox * om, oy * om, oz * om) * smootheningMultiplier;

    // Get the distance vector from the center
    vec3 nDistVec = normalize(fposition + offsetVec);

    // Get noise with normalized position to offset the original position
    vec3 position = fposition + simplexnoise(vec4(nDistVec, t), iDetail, 1.5, fDetail) * smootheningMultiplier;

    // Calculate brightness based on distance
    float dist = length(position + offsetVec) * coronaSizeMultiplier;
    float brightness = (1.0 / (dist * dist) - 0.1) * (brightnessMultiplier - 0.4);
	float brightness2 = (1.0 / (dist * dist)) * brightnessMultiplier;

    // Calculate color
    vec3 unColor = vec3(0.9,0.9,0);

    float alpha = clamp(brightness, 0.0, 1.0) * (cos(clamp(brightness, 0.0, 0.5)) / (cos(clamp(brightness2 / ringIntesityMultiplier, 0.0, 1.5)) * 2));
    vec3 starcolor = unColor * brightness;

    alpha = pow(alpha,1.8);             // exp roll of of alpha so it does go to 0, and therefore it does not show box
    if ( alpha < 0.2 )
        discard;
    else
        color = vec4(starcolor, alpha );
}
";
        }

        public GLShaderStarCorona()
        {
            CompileLink(vertex: Vertex(), frag: Fragment());
        }

        public float TimeDelta { get; set; } = 0.00001f * 10;

        public override void Start(GLMatrixCalc c)
        {
            base.Start(c);

            GL.ProgramUniform1(Id, 15, TimeDelta);
            OFC.GLStatics.Check();
        }
    }
}

