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

namespace GLOFC.GL4
{
    // Shader, with tesselation, and Y change in amp using sin, autoscale

    public class GLTesselationShaderSinewaveAutoscaleLookatInstanced : GLShaderPipeline
    {

        public GLTesselationShaderSinewaveAutoscaleLookatInstanced(float tesselation,float amplitude, float repeats, bool rotate = false, bool rotateelevation = true, 
                                                    bool commontransform = false,  float autoscale=0, float autoscalemin = 0.1f, float autoscalemax= 3f)
        {
            var vert = new GLPLVertexScaleLookat(rotate,rotateelevation, commontransform, autoscale, autoscalemin, autoscalemax);
            var tcs = new GLPLTesselationControl(tesselation);
            tes = new GLPLTesselationEvaluateSinewave(amplitude,repeats);
            var frag = new GLPLFragmentShaderTexture2DDiscard();

            AddVertexTCSTESGeoFragment(vert,tcs,tes,null,frag);
        }

        public float Phase { get { return tes.Phase; }  set { tes.Phase = value; } }

        private GLPLTesselationEvaluateSinewave tes;

    }
}

