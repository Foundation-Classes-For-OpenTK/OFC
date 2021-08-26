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

using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    // Autoscale to size on model if required
    //      location 0 : position: vec4 vertex array of positions model coords, w is ignored
    //      location 1 : worldpositions - passed thru to world pos
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
    // Out:
    //      gl_Position
    //      location 1 : worldpos copied
    //      location 2 : instance id

    public class GLPLVertexScaleLookat : GLShaderPipelineShadersBase
    {
        string vert =
        @"
#version 450 core

#include UniformStorageBlocks.matrixcalc.glsl
#include Shaders.Functions.trig.glsl
#include Shaders.Functions.mat4.glsl
#include Shaders.Functions.vec4.glsl

layout (location = 0) in vec4 modelposition;
layout (location = 1) in vec4 worldposition;
layout (location = 22) uniform  mat4 transform;

layout( location = 1) out vec4 worldposinstance;
layout (location = 2) out int instance;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

const bool rotateelevation = false;
const bool rotate = false;
const bool usetransform = false;
const float autoscale = 0;
const float autoscalemax = 0;
const float autoscalemin = 0;

void main(void)
{
    vec4 pos = vec4(modelposition.xyz,1);       

    if ( autoscale>0)
        pos = Scale(pos,clamp(mc.EyeDistance/autoscale,autoscalemin,autoscalemax));

    if ( rotate )       // reverified after much work 21/7/21
    {
        vec2 dir = AzEl(mc.EyePosition.xyz,worldposition.xyz);      // From our pos, to the object, what is Az/El.  Az = 0 if forward, 180 if back. El = 0 if up, 90 if level, 180 if down
        if ( rotateelevation )
        {
            pos = mat4rotateXthenY(-(PI-dir.x), dir.y) * pos;       // dir.x = inclination, 0 upwards to target, 180 downwards to target. Picture is flat on xz plane.
                                                                    // So 180-dir.x (meaning 0 rotate by 180, or 180 no rotate), and negative to rotate it towards us not the other way
                                                                    // dir.y = 0 eye towards target, 180 eye behind target, -90 left +90 right. rotate to eye
        }
        else
        {
            pos = mat4rotateXm90thenY(dir.y) * pos;                 // rotate the bitmap vertical (m90) then rotate to viewer
        }
    }

    if ( usetransform )
    {
        pos = transform * pos;      // use transform to adjust
    }

    gl_Position = pos;
    
    worldposinstance = worldposition;
    instance = gl_InstanceID;
}
";

        public GLPLVertexScaleLookat(bool rotate = false, bool rotateelevation = true, bool commontransform = false,
                                                    float autoscale = 0, float autoscalemin = 0.1f, float autoscalemax = 3f)
        {
            CompileLink(ShaderType.VertexShader, vert, new object[] { "rotate", rotate, "rotateelevation", rotateelevation,
                                                                    "usetransform", commontransform, "autoscale", autoscale,
                                                                    "autoscalemin", autoscalemin, "autoscalemax", autoscalemax },completeoutfile:@"c:\code\shader.txt");
        }
    }
}
