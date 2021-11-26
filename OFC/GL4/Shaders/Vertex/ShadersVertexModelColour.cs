﻿/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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

using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

// Vertex shaders, having a model input, with fixed colour not texture

namespace GLOFC.GL4
{
    // Pipeline shader, Translation, Colour, Modelpos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords. W is ignored
    //      location 1 : vec4 colour
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 array of transforms
    // Out:
    //      gl_Position
    //      location 0 : vs_color
    //      location 1 : modelpos

    public class GLPLVertexShaderColorModelCoordWithObjectTranslation : GLShaderPipelineComponentShadersBase
    {
        public GLPLVertexShaderColorModelCoordWithObjectTranslation(string[] varyings = null, TransformFeedbackMode varymode = TransformFeedbackMode.InterleavedAttribs, bool saveable = false)
        {
            CompileLink(ShaderType.VertexShader, Code(), null, varyings, varymode, auxname: GetType().Name, saveable: saveable);
        }
        public GLPLVertexShaderColorModelCoordWithObjectTranslation(byte[] bin, BinaryFormat bf)
        {
            Load(bin, bf);
        }

        private string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return

@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 color;
layout (location = 22) uniform  mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };


layout (location = 0) out vec4 vs_color;
layout (location = 1) out vec3 modelpos;

void main(void)
{
    modelpos = position.xyz;
	gl_Position = mc.ProjectionModelMatrix * transform * vec4(position.xyz,1);        // order important
	vs_color = color;                                                   // pass to fragment shader
}
";
        }
    }



    // Pipeline shader, Common Model Translation, Seperate World pos, transform, autoscaling of model due to eyedistance
    // colour is given by an array of basecolours, and the world position.W selectes the base colour
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords
    //      location 1 : world-position: vec4 vertex array of world pos for model, instanced.
    //                   W>=0 selects the base colour to present, W <=-1 disables the model at this position
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
    // Out:
    //      gl_Position
    //      location 1 modelpos
    //      location 2 instance id
    //      location 3 basecolor for fragment shader
    //      location 4 drawid (4.6) for multidraws

    public class GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation : GLShaderPipelineComponentShadersBase
    {
        public Matrix4 ModelTranslation { get; set; } = Matrix4.Identity;

        public GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(System.Drawing.Color[] basecolours = null,
                                                                    float autoscale = 0, float autoscalemin = 0.1f, float autoscalemax = 3f, bool useeyedistance = true)
        {
            List<object> values = new List<object> { "autoscale", autoscale, "autoscalemin", autoscalemin, "autoscalemax", autoscalemax, "useeyedistance", useeyedistance };
            if (basecolours != null)
                values.AddRange(new object[] { "colours", basecolours });

            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name, constvalues: values.ToArray()); //, completeoutfile:@"c:\code\code.out");
        }

        public override void Start(GLMatrixCalc c)
        {
            Matrix4 a = ModelTranslation;
            GL.ProgramUniformMatrix4(Id, 22, false, ref a);
            GLOFC.GLStatics.Check();
        }

        private string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 460 core
#include UniformStorageBlocks.matrixcalc.glsl
#include Shaders.Functions.vec4.glsl

layout (location = 0) in vec4 modelposition;
layout (location = 1) in vec4 worldposition;            // instanced, w ignored
layout (location = 22) uniform  mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
        float gl_CullDistance[];
    };

layout (location = 1) out vec3 modelpos;
layout (location = 2) out int instance;
layout (location = 3) out vec4 basecolor;
layout (location = 4) out int drawid;       // 4.6 item

const vec4 colours[] = { vec4(1,1,0,1), vec4(1,1,0,1)};   // for some reason, need two otherwise it barfs

const float autoscale = 0;
const float autoscalemax = 0;
const float autoscalemin = 0;
const bool useeyedistance = true;

void main(void)
{
    if ( worldposition.w <= -1 )
    {
        gl_CullDistance[0] = -1;        // so, if we set it once, we need to set it always, for somereason the compiler if its sees it set and you
    }                                   // don't do it everywhere it can get into an interderminate state per vertex
    else
    {
        gl_CullDistance[0] = 1;
        basecolor = colours[int(worldposition.w)];

        modelpos = modelposition.xyz;

        vec4 pos = modelposition;
        vec4 worldp = vec4(worldposition.xyz,0);

        if ( autoscale>0)
        {
            if ( useeyedistance )
                pos = Scale(pos,clamp(mc.EyeDistance/autoscale,autoscalemin,autoscalemax));
            else
            {
                float d = distance(mc.EyePosition,worldp);            // find distance between eye and world pos
                pos = Scale(pos,clamp(d/autoscale,autoscalemin,autoscalemax));
            }
        }

        vec4 modelrot = transform * pos;
        vec4 wp = modelrot + worldp;
        gl_Position = mc.ProjectionModelMatrix * wp;        // order important
        instance = gl_InstanceID;
        drawid = gl_DrawID;
    }
}
";
        }

    }

    // Pipeline shader, Common Model Translation, Seperate World pos as a matrix, transform of model, common worldpos offset from matrix
    // base colour taken from transform for colour shader
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords. 
    //      vertex 4-7 : transform: mat4 array of transforms, one per instance. Row[3,0-3] = xyz
    //              [col=3,row=1] -1 means cull primitive, else used to lookup in colour array and passed to fragment shader as basecolour
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
    //      uniform 23 : common transform to move/scale objects
    // Out:
    //      gl_Position
    //      location 1 modelpos
    //      location 2 instance id
    //      location 3 basecolour


    public class GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation : GLShaderPipelineComponentShadersBase
    {
        public Matrix4 ModelTranslation { get; set; } = Matrix4.Identity;
        public Vector3 WorldPositionOffset { get; set; } = Vector3.Zero;

        public GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation(System.Drawing.Color[] basecolours = null)
        {
            object[] cvalues = null;
            if (basecolours != null)
                cvalues = new object[] { "colours", basecolours };

            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name, constvalues: cvalues);
        }

        public override void Start(GLMatrixCalc c)
        {
            Matrix4 a = ModelTranslation;
            GL.ProgramUniformMatrix4(Id, 22, false, ref a);
            Vector3 b = WorldPositionOffset;
            GL.ProgramUniform3(Id, 23, ref b);
            GLOFC.GLStatics.Check();
        }

        private string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 modelposition;
layout (location = 4) in mat4 worldpos;
layout (location = 22) uniform  mat4 transform;
layout (location = 23) uniform  vec3 worldoffset;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
        float gl_CullDistance[];
    };

layout (location = 1) out vec3 modelpos;
layout (location = 2) out int instance;
layout (location = 3) out vec4 basecolor;

const vec4 colours[] = { vec4(1,1,0,1), vec4(1,1,0,1)};   // for some reason, need two otherwise it barfs

void main(void)
{
    float ctrl = worldpos[1][3];

    if ( ctrl < 0 )
    {
        gl_CullDistance[0] = -1;        // all vertex culled
    }
    else
    {
        gl_CullDistance[0] = +1;        // not culled
        basecolor = colours[int(ctrl)];

        modelpos = modelposition.xyz;       // passed thru unscaled

        vec4 modelrot = transform * modelposition;
        vec4 worldposition = vec4(worldpos[3][0],worldpos[3][1],worldpos[3][2],0);      // extract world position from row3 columns 0/1/2 (floats 12-14)
        vec4 wp = modelrot + worldposition + vec4(worldoffset,0);
	    gl_Position = mc.ProjectionModelMatrix * wp;        // order important
        instance = gl_InstanceID;
    }
}
";
        }

    }



    // Pipeline shader, Matrix Translation, instanced ID colour
    // Requires:
    //      location 0 : position: vec4 vertex array of positions of model
    //      location 4 : transform: mat4 array of transforms.. 
    //      uniform buffer 0 : GL MatrixCalc
    // Out:
    //      gl_Position
    //      location 0 : vs_color is based on instance ID (this is used mostly for debugging)
    //      location 1 : modelpos

    public class GLPLVertexShaderModelCoordWithMatrixTranslation : GLShaderPipelineComponentShadersBase
    {
        public GLPLVertexShaderModelCoordWithMatrixTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        private string Code()
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl
layout (location = 0) in vec4 position;
layout (location = 4) in mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout (location = 0) out vec4 vs_color;
layout (location = 1) out vec3 modelpos;

void main(void)
{
    modelpos = position.xyz;
    vs_color = vec4(gl_InstanceID*0.2+0.2,gl_InstanceID*0.2+0.2,0.5+gl_VertexID*0.1,1.0);       // colour may be thrown away if required..
	gl_Position = mc.ProjectionModelMatrix * transform * position;        // order important
}
";
        }
    }


    // Pipeline shader, Translation, Color, Common transform, Object transform
    // Requires:
    //      location 0 : position: vec4 vertex array of model positions
    //      location 1 : vec4 colours of vertexs
    //      uniform 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 array of transforms
    //      uniform 23 : commontransform: mat4 array of transforms
    // Out:
    //      location 0 : vs_textureCoordinate
    //      gl_Position

    public class GLPLVertexShaderColorModelCoordWithObjectCommonTranslation : GLShaderPipelineComponentShadersBase
    {
        public GLRenderDataTranslationRotation Transform { get; set; }           // only use this for rotation - position set by object data

        public GLPLVertexShaderColorModelCoordWithObjectCommonTranslation()
        {
            Transform = new GLRenderDataTranslationRotation();
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            base.Start(c);
            Matrix4 t = Transform.Transform;
            GL.ProgramUniformMatrix4(Id, 23, false, ref t);
            GLOFC.GLStatics.Check();
        }

        private string Code()
        {
            return

@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout(location = 1) in vec4 color;

layout (location = 22) uniform  mat4 objecttransform;
layout (location = 23) uniform  mat4 commontransform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout (location= 0) out vec4 vs_color;

void main(void)
{
	gl_Position = mc.ProjectionModelMatrix * objecttransform *  commontransform * position;        // order important
	vs_color = color;                                                   // pass to fragment shader
}
";
        }

    }



    // Pipeline shader, Model co-ords with uniform 22 being set up by render list with world offset
    // colour is given by an array of basecolours, and the world position.W selectes the base colour
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : World position offset, W selects base colour, -1 turns off 
    // Out:
    //      gl_Position
    //      location 1 modelpos
    //      location 2 instance id
    //      location 3 basecolor for fragment shader
    //      location 4 drawid (4.6) for multidraws

    public class GLPLVertexShaderModelCoordWithWorldUniform : GLShaderPipelineComponentShadersBase
    {
        public GLPLVertexShaderModelCoordWithWorldUniform(System.Drawing.Color[] basecolours = null)
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name, constvalues: new object[] { "colours", basecolours }); //, completeoutfile:@"c:\code\code.out");
        }

        private string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 460 core
#include UniformStorageBlocks.matrixcalc.glsl
#include Shaders.Functions.vec4.glsl

layout (location = 0) in vec4 modelposition;
layout (location = 22) uniform vec4 worldposition;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
        float gl_CullDistance[];
    };

layout (location = 0) out vec4 vs_color;

const vec4 colours[] = { vec4(1,1,0,1), vec4(1,1,0,1)};   // for some reason, need two otherwise it barfs

void main(void)
{
    if ( worldposition.w <= -1 )
    {
        gl_CullDistance[0] = -1;        // so, if we set it once, we need to set it always, for somereason the compiler if its sees it set and you
    }                                   // don't do it everywhere it can get into an interderminate state per vertex
    else
    {
        gl_CullDistance[0] = 1;
        vs_color = colours[int(worldposition.w)];

        vec4 pos = vec4(worldposition.xyz,0);
        pos += modelposition;
        gl_Position = mc.ProjectionModelMatrix * pos;        // order important
    }
}
";
        }

    }










}