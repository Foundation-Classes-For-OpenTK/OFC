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
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Vertex
{
    /// <summary>
    ///  Vertex look at with autoscale, optional uniform common transform, optional generate world pos
    ///  output is either model scaled + worldpos, for use by a tes shader
    ///  or projectionmodelmatrix * (model+worldpos) for use directly by a frag shader
    /// </summary>

    public class GLPLVertexScaleLookat : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        ///  Input
        ///       location 0 : model position: vec4 vertex array of positions model coords, w is ignored. Scaled/autorotated. Optionally used to compute gl_Position
        ///       location 1 : worldpositions - passed thru to world pos, optionally used to compute gl_Position.
        ///       location 2 : texcoords optionally passed thru to 0
        ///       uniform buffer 0 : GL MatrixCalc
        ///       uniform configurable: objecttransform: mat4 transform of model before world applied (for rotation/scaling)
        ///  Out:
        ///       gl_Position: either model position passed thru scaled/rotated, or if generateworldpos = true then projmodelmatrix * (modelpos+worldpos)
        ///       location 0 : optional tex coords generated
        ///       location 1 : worldpos copied
        ///       location 2 : instance id
        /// </summary>
        /// <param name="rotatetoviewer">True to rotate in azimuth to viewer</param>
        /// <param name="rotateelevation">True to rotate in elevation to viewer</param>
        /// <param name="commontransformuniform">0 off, else use this uniform binding point as a common transform</param>
        /// <param name="texcoords">Generate tex coords for frag shader</param>
        /// <param name="generateworldpos">If true, pass out ProjModelMatrix * (modelpos+worldpos), else pass out model position scaled and rotated</param>
        /// <param name="autoscale">To autoscale distance. Sets the 1.0 scale point.</param>
        /// <param name="autoscalemin">Minimum to scale to</param>
        /// <param name="autoscalemax">Maximum to scale to</param>
        /// <param name="useeyedistance">Use eye distance to lookat to autoscale, else use distance between object and eye</param>
        public GLPLVertexScaleLookat(bool rotatetoviewer = false, bool rotateelevation = true, int commontransformuniform = 0, bool texcoords = false, bool generateworldpos = false,
                                                    float autoscale = 0, float autoscalemin = 0.1f, float autoscalemax = 3f, bool useeyedistance = true)
        {
            CompileLink(ShaderType.VertexShader, vert, out string unused, new object[] { "rotate", rotatetoviewer, "rotateelevation", rotateelevation,
                                                                    "usetexcoords", texcoords, "generateworldpos", generateworldpos,
                                                                    "autoscale", autoscale,
                                                                    "autoscalemin", autoscalemin, "autoscalemax", autoscalemax , "useeyedistance", useeyedistance,
                                                                    "transformuniform", commontransformuniform});
        }

        string vert =
        @"
#version 450 core

#include UniformStorageBlocks.matrixcalc.glsl
#include Shaders.Functions.trig.glsl
#include Shaders.Functions.mat4.glsl
#include Shaders.Functions.vec4.glsl

const int transformuniform = 22;

layout (location = 0) in vec4 modelposition;
layout (location = 1) in vec4 worldposition;
layout (location = 2) in vec2 texco;

layout (location = transformuniform) uniform  mat4 transform;

layout (location = 0) out vec2 vs_textureCoordinate;
layout (location = 1) out vec4 worldposinstance;
layout (location = 2) out flat int instance;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

const bool rotateelevation = false;
const bool rotate = false;
const bool usetransform = false;
const bool usetexcoords = false;
const bool generateworldpos = false;
const float autoscale = 0;
const float autoscalemax = 0;
const float autoscalemin = 0;
const bool useeyedistance = true;

void main(void)
{
    vec4 pos = vec4(modelposition.xyz,1);       

    if ( autoscale>0)
    {
        if ( useeyedistance )
            pos = Scale(pos,clamp(mc.EyeDistance/autoscale,autoscalemin,autoscalemax));
        else
        {
            float d = distance(mc.EyePosition,vec4(worldposition.xyz,0));            // find distance between eye and world pos
            pos = Scale(pos,clamp(d/autoscale,autoscalemin,autoscalemax));
        }
    }


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

    if ( transformuniform>0 )
    {
        pos = transform * pos;      // use transform to adjust
    }

    if ( usetexcoords )
        vs_textureCoordinate = texco;

    if ( generateworldpos )
    {
        pos += vec4(worldposition.xyz,0);
	    gl_Position = mc.ProjectionModelMatrix * pos;      
    }
    else
    {
        gl_Position = pos;
    }
    
    worldposinstance = worldposition;
    instance = gl_InstanceID;
}
";

    }
}
