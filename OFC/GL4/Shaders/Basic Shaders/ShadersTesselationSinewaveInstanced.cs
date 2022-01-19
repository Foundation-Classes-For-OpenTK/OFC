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
using GLOFC.GL4.Shaders.Fragment;
using GLOFC.GL4.Shaders.Tesselation;

namespace GLOFC.GL4.Shaders.Basic
{
    /// <summary>
    /// Shader, with tesselation, and Y change in amp using sin, autoscale 
    /// </summary>
    public class GLTesselationShaderSinewaveAutoscale : GLShaderPipeline
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tesselation">Tesselation amount</param>
        /// <param name="amplitude">Amplitude of sinewave</param>
        /// <param name="repeats">Repeats across object</param>
        /// <param name="rotate">To rotate to viewer in azimuth</param>
        /// <param name="rotateelevation">to rotate to viewer in elevation</param>
        /// <param name="commontransformuniform">Common transform uniform. 0 means off</param>
        /// <param name="autoscale">To autoscale distance. Sets the 1.0 scale point.</param>
        /// <param name="autoscalemin">Minimum to scale to</param>
        /// <param name="autoscalemax">Maximum to scale to</param>
        /// 
        public GLTesselationShaderSinewaveAutoscale(float tesselation, float amplitude, float repeats, bool rotate = false, bool rotateelevation = true,
                                                    int commontransformuniform = 0, float autoscale = 0, float autoscalemin = 0.1f, float autoscalemax = 3f)
        {
            var vert = new GLPLVertexScaleLookat(rotate, rotateelevation, commontransformuniform, false, false, autoscale, autoscalemin, autoscalemax);
            var tcs = new GLPLTesselationControl(tesselation);
            tes = new GLPLTesselationEvaluateSinewave(amplitude, repeats);
            var frag = new GLPLFragmentShaderTexture2DDiscard();

            AddVertexTCSTESGeoFragment(vert, tcs, tes, null, frag);
        }

        /// <summary>
        /// Phase of sinewave
        /// </summary>
        public float Phase { get { return tes.Phase; } set { tes.Phase = value; } }

        private GLPLTesselationEvaluateSinewave tes;

    }
}

