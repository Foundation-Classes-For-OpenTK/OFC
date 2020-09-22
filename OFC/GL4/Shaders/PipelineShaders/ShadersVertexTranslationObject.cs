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

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace OFC.GL4
{
    // Pipeline shader, Translation, Modelpos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords, W is ignored
    //      uniform block 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 array of transforms
    // Out:
    //      gl_Position
    //      location 1: modelpos

    public class GLPLVertexShaderModelCoordWithObjectTranslation : GLShaderPipelineShadersBase
    {
        public string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;

layout (location = 22) uniform  mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout (location = 1) out vec3 modelpos;

void main(void)
{
    modelpos = position.xyz;
	gl_Position = mc.ProjectionModelMatrix * transform * vec4(position.xyz,1);        // order important
}
";
        }

        public GLPLVertexShaderModelCoordWithObjectTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }
    }


    // Pipeline shader, Common Model Translation, Seperate World pos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords
    //      location 1 : world-position: vec4 vertex array of world pos for model, instanced. W is ignored in case its carrying other data
    //      uniform block 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
    // Out:
    //      gl_Position
    //      location 1 modelpos
    //      location 2 instance id

    public class GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation : GLShaderPipelineShadersBase
    {
        public string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 modelposition;
layout (location = 1) in vec4 worldposition;            // instanced, w ignored
layout (location = 22) uniform  mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout (location = 1) out vec3 modelpos;
layout (location = 2) out int instance;

void main(void)
{
    modelpos = modelposition.xyz;
    vec4 modelrot = transform * modelposition;
    vec4 wp = modelrot + vec4(worldposition.xyz,0);
	gl_Position = mc.ProjectionModelMatrix * wp;        // order important
    instance = gl_InstanceID;
}
";
        }

        public Matrix4 ModelTranslation { get; set; } = Matrix4.Identity;

        public GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            Matrix4 a = ModelTranslation;
            GL.ProgramUniformMatrix4(Id, 22, false, ref a);
            OFC.GLStatics.Check();
        }
    }

    // Pipeline shader, Translation, Colour, Modelpos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords. W is ignored
    //      location 1 : vec4 colour
    //      uniform 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 array of transforms
    // Out:
    //      gl_Position
    //      location 0 : vs_color
    //      location 1 : modelpos

    public class GLPLVertexShaderColorModelCoordWithObjectTranslation : GLShaderPipelineShadersBase
    {
        public string Code()       // with transform, object needs to pass in uniform 22 the transform
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

        public GLPLVertexShaderColorModelCoordWithObjectTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }
    }

    // Pipeline shader, Translation, Texture, Modelpos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords, w is ignored
    //      location 1 : vec2 texture co-ords
    //      uniform 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 array of transforms
    // Out:
    //      gl_Position
    //      location 0: vs_textureCoordinate
    //      location 1: modelpos


    public class GLPLVertexShaderTextureModelCoordWithObjectTranslation : GLShaderPipelineShadersBase
    {
        public string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return

@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout(location = 1) in vec2 texco;
layout (location = 22) uniform  mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout(location = 0) out vec2 vs_textureCoordinate;
layout(location = 1) out vec3 modelpos;

void main(void)
{
    modelpos = position.xyz;
	gl_Position = mc.ProjectionModelMatrix * transform * vec4(position.xyz,1);        // order important
    vs_textureCoordinate = texco;
}
";
        }

        public GLPLVertexShaderTextureModelCoordWithObjectTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }
    }


    // Pipeline shader, Common Model Translation, Seperate World pos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords, w is ignored
    //      location 1 : texco-ords 
    //      location 2 : world-position: vec4 vertex array of world pos for model, instanced, w ignored
    //      uniform block 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
    // Out:
    //      gl_Position
    //      location 0 : texco
    //      location 1 : modelpos
    //      location 2 : instance id

    public class GLPLVertexShaderTextureModelCoordWithWorldTranslationCommonModelTranslation : GLShaderPipelineShadersBase
    {
        public string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 modelposition;
layout (location = 1) in vec2 texco;
layout (location = 2) in vec4 worldposition;            // instanced
layout (location = 22) uniform  mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout( location = 0) out vec2 vs_textureCoordinate;
layout (location = 1) out vec3 modelpos;
layout (location = 2) out int instance;

void main(void)
{
    modelpos = modelposition.xyz;
    vec4 modelrot = transform * modelposition;
    vec4 wp = modelrot + vec4(worldposition.xyz,0);
	gl_Position = mc.ProjectionModelMatrix * wp;        // order important
    instance = gl_InstanceID;
    vs_textureCoordinate = texco;
}
";
        }

        public Matrix4 ModelTranslation { get; set; } = Matrix4.Identity;

        public GLPLVertexShaderTextureModelCoordWithWorldTranslationCommonModelTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            Matrix4 a = ModelTranslation;
            GL.ProgramUniformMatrix4(Id, 22, false, ref a);
            OFC.GLStatics.Check();
        }
    }
}
