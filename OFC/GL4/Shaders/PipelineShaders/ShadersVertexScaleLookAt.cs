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

namespace OFC.GL4
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

const bool rotateelevation = true;
const bool rotate = true;
const bool usetransform = false;
const float autoscale = 0;
const float autoscalemax = 0;
const float autoscalemin = 0;

void main(void)
{
    vec4 pos = vec4(modelposition.xyz,1);       

    if ( autoscale>0)
        pos = Scale(pos,clamp(mc.EyeDistance/autoscale,autoscalemin,autoscalemax));

    if ( usetransform )
    {
        pos = transform * pos;      // use transform to adjust
    }

    if ( rotate )
    {
        

// works 1
        ///vec2 dir = AzEl(worldposition.xyz,mc.EyePosition.xyz);      // From our pos, to the object, what is Az/El
        //float roty = PI-dir.y;
        //pos = mat4rotateXm90thenYm90() * pos;
        //pos = pos * mat4rotateZ(dir.x-PI/2); // rotate around Z to set elevation
        //pos = pos * mat4rotateY(-(PI/2-roty));  // inverse rotate back YZ plane to set azimuth

// works 2:
        //pos = pos * mat4rotateX(PI/2);      // rotate to XY plane
        //pos = pos * mat4rotateY(PI/2);  // rotate back to YZ plane
        //pos = pos * mat4rotateZ(dir.x-PI/2); // rotate around Z to set elevation
        //pos = pos * mat4rotateY(-(PI/2-roty));  // inverse rotate back YZ plane to set azimuth

// works 3:
        //pos = mat4rotateXm90thenYm90() * pos;
        //pos = pos * mat4rotateZ(dir.x-PI/2); // rotate around Z to set elevation
        //pos = pos * mat4rotateY(-(PI/2-roty));  // inverse rotate back YZ plane to set azimuth

// works 4:
        //pos = mat4rotateXm90thenYm90() * pos;
        //pos = pos * mat4rotateZ(dir.x-PI/2); // rotate around Z to set elevation
        //pos = pos * mat4rotateY(-(PI/2-roty));  // inverse rotate back YZ plane to set azimuth

// works 5:
        //vec2 dir = AzEl(mc.EyePosition.xyz,worldposition.xyz);      // From our pos, to the object, what is Az/El
        //pos = mat4rotateX(-(PI-dir.x)) * pos;            // dir.x = inc, 0 upwards to target, 180 downwards to target. So PI-dir.x, and negative to rotate it towards us
        //pos = mat4rotateY(dir.y) * pos;

// works 6:
        vec2 dir = AzEl(mc.EyePosition.xyz,worldposition.xyz);      // From our pos, to the object, what is Az/El.  Az = 0 if forward, 180 if back. El = 0 if up, 90 if level, 180 if down
        if ( rotateelevation)
        {
            pos = mat4rotateX(-(PI-dir.x)) * pos;            // dir.x = inc, 0 upwards to target, 180 downwards to target. So PI-dir.x, and negative to rotate it towards us
            pos = mat4rotateY(dir.y) * pos;
        }
        else
        {
         //   pos = mat4rotateXm90() * pos;
           // pos = mat4rotateY(dir.y) * pos;
            pos = mat4rotateXm90thenY(dir.y) * pos;
        }


// rotate X to dir.x works

       // if (rotateelevation )
         //   tx = mat4rotateXthenY(dir.x,PI-dir.y);              // rotate the flat image to vertical using dir.x (0 = on top, 90 = straight on, etc) then rotate by 180-dir (0 if directly facing from neg z)
        //else
            //tx = mat4rotateXthenY(PI/2,PI-dir.y);
        //tx = mat4rotateXthenY(dir.x,0);

// this seems to work
        //pos = pos * mat4rotateX(PI/2);      // rotate to XY plane
        //pos = pos * mat4rotateY(PI/2);  // rotate back to YZ plane







//        tx = mat4rotateY(PI-dir.y);
  ///      pos = pos * tx;
     //   mat4 t2 = mat4rotateX(dir.x);
       // pos = pos *t2;

        //pos = pos * ty * tx;
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
                                                                    "autoscalemin", autoscalemin, "autoscalemax", autoscalemax });
        }
    }
}
