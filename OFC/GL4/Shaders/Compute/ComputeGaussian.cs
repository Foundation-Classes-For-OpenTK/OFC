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


using GLOFC.Utils;

namespace GLOFC.GL4.Shaders.Compute
{
    /// <summary>
    /// Compute shader, 1D gaussian distribution, 8x8x8 multiple 
    /// Requires:
    ///      1D texture to write to, bound on binding point
    /// </summary>

    public class ComputeShaderGaussian : GLShaderCompute
    {
        private static int Localgroupsize = 8;

        private string gencode(int points, float centre, float width, float stddist, int binding)
        {
            return
@"
#version 450 core
#include Shaders.Functions.distribution.glsl

layout (local_size_x = 8, local_size_y = 1, local_size_z = 1) in;

layout (binding=" + binding.ToStringInvariant() + @", r32f ) uniform image1D img;

void main(void)
{
    float points = " + points.ToStringInvariant() + @";       // grab the constants from caller
    float centre = " + centre.ToStringInvariant() + @";       // grab the constants from caller
    float stddist = " + stddist.ToStringInvariant() + @";       // grab the constants from caller
    float width = " + width.ToStringInvariant() + @";       // grab the constants from caller

    float x = (float(gl_GlobalInvocationID.x)/points-0.5)*2*width;      // normalise to -1 to +1, mult by width
    float g = gaussian(x,centre,stddist);
    vec4 color = vec4( g, 0,0,0);
    imageStore( img, int(gl_GlobalInvocationID.x), color);    // store back the computed dist
}
";
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Number of points, must be a multiple of 8 </param>
        /// <param name="centre">Centre noise value (example 2) of guassian distribution</param>
        /// <param name="width">Width noise value (example 2)</param>
        /// <param name="stddist">Standard distribution (example 1.5)</param>
        /// <param name="binding">Binding of texture</param>
        /// <param name="saveable">Is shader to be saveable</param>
        public ComputeShaderGaussian(int points, float centre, float width, float stddist, int binding = 4, bool saveable = false) : base(points / Localgroupsize, 1, 1)
        {
            System.Diagnostics.Debug.Assert(points % Localgroupsize == 0);
            CompileLink(gencode(points, centre, width, stddist, binding), saveable: saveable);
        }
    }
}
