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

using OpenTK;
using OpenTK.Graphics.OpenGL4;

// Vertex shaders, having a model input, some with world input, some with a common transform

namespace OFC.GL4
{
    // Pipeline shader, Translation, Modelpos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords, W is ignored
    //      uniform buffer 0 : GL MatrixCalc
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
    //      location 1 : world-position: vec4 vertex array of world pos for model, instanced. W selects the base colour to present
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
    // Out:
    //      gl_Position
    //      location 1 modelpos
    //      location 2 instance id
    //      location 3 basecolor for fragment shader
    //      location 4 drawid (4.6) for multidraws

    public class GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation : GLShaderPipelineShadersBase
    {
        public string Code()       // with transform, object needs to pass in uniform 22 the transform
        {
            return
@"
#version 460 core
#include UniformStorageBlocks.matrixcalc.glsl

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
        vec4 modelrot = transform * modelposition;
        vec4 wp = modelrot + vec4(worldposition.xyz,0);
        gl_Position = mc.ProjectionModelMatrix * wp;        // order important
        instance = gl_InstanceID;
        drawid = gl_DrawID;
    }
}
";
        }

        public Matrix4 ModelTranslation { get; set; } = Matrix4.Identity;

        public GLPLVertexShaderModelCoordWithWorldTranslationCommonModelTranslation(System.Drawing.Color[] basecolours = null)
        {
            object[] cvalues = null;
            if (basecolours != null)
                cvalues = new object[] { "colours", basecolours };

            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name, constvalues:cvalues);
        }

        public override void Start(GLMatrixCalc c)
        {
            Matrix4 a = ModelTranslation;
            GL.ProgramUniformMatrix4(Id, 22, false, ref a);
            OFC.GLStatics.Check();
        }
    }


    // Pipeline shader, Matrix Translation
    // Requires:
    //      location 0 : position: vec4 vertex array of positions of model
    //      location 4 : transform: mat4 array of transforms.. 
    //      uniform buffer 0 : GL MatrixCalc
    // Out:
    //      gl_Position
    //      location 0 : vs_color is based on instance ID mostly for debugging
    //      location 1 : modelpos

    public class GLPLVertexShaderModelCoordWithMatrixTranslation : GLShaderPipelineShadersBase
    {
        public string Code()
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

        public GLPLVertexShaderModelCoordWithMatrixTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }
    }


    // Pipeline shader, Translation, Texture
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords
    //      location 1 : vec2 texture co-ordinates
    //      location 4 : transform: mat4 array of transforms
    //      uniform buffer 0 : GL MatrixCalc
    // Out:
    //      location 0 : vs_textureCoordinate
    //      location 1 : modelpos
    //      location 2 : instance count
    //      gl_Position

    public class GLPLVertexShaderTextureModelCoordWithMatrixTranslation : GLShaderPipelineShadersBase
    {
        public string Code()
        {
            return
@"
#version 450 core

#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout (location = 1) in vec2 texco;
layout (location = 4) in mat4 transform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout(location = 0) out vec2 vs_textureCoordinate;
layout (location = 1) out vec3 modelpos;
layout (location = 2) out VS_OUT
{
    flat int vs_instanced;
} vs_out;


void main(void)
{
    modelpos = position.xyz;
	gl_Position = mc.ProjectionModelMatrix * transform * position;        // order important
    vs_textureCoordinate = texco;
    vs_out.vs_instanced = gl_InstanceID;
}
";
        }

        public GLPLVertexShaderTextureModelCoordWithMatrixTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
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

    public class GLPLVertexShaderColorModelCoordWithObjectCommonTranslation : GLShaderPipelineShadersBase
    {
        public GLRenderDataTranslationRotation Transform { get; set; }           // only use this for rotation - position set by object data

        public string Code()
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
            OFC.GLStatics.Check();
        }
    }


    // Pipeline shader, Translation, Texture, Common transform, Object transform
    // Requires:
    //      location 0 : position: vec4 vertex array of model positions
    //      location 1 : vec2 texture co-ords
    //      uniform 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 array of transforms
    //      uniform 23 : commontransform: mat4 array of transforms
    // Out:
    //      location 0: vs_textureCoordinate
    //      gl_Position

    public class GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation : GLShaderPipelineShadersBase
    {
        public GLRenderDataTranslationRotation Transform { get; set; }           // only use this for rotation - position set by object data

        public string Code()
        {
            return

@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout(location = 1) in vec2 texco;
layout (location = 22) uniform  mat4 objecttransform;
layout (location = 23) uniform  mat4 commontransform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout(location = 0) out vec2 vs_textureCoordinate;

void main(void)
{
	gl_Position = mc.ProjectionModelMatrix * objecttransform *  commontransform * position;        // order important
    vs_textureCoordinate = texco;
}
";
        }

        public GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation()
        {
            Transform = new GLRenderDataTranslationRotation();
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        public override void Start(GLMatrixCalc c)
        {
            base.Start(c);
            Matrix4 t = Transform.Transform;
            GL.ProgramUniformMatrix4(Id, 23, false, ref t);
            OFC.GLStatics.Check();
        }
    }


    // Pipeline shader, Common Model Translation, Seperate World pos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords, w is ignored
    //      location 1 : texco-ords 
    //      location 2 : world-position: vec4 vertex array of world pos for model, instanced, w ignored
    //      uniform buffer 0 : GL MatrixCalc
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


    // Pipeline shader, Common Model Translation, Seperate World pos as a matrix, transform of model, common worldpos offset from matrix
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords. 
    //      vertex 4-7 : transform: mat4 array of transforms, one per instance. Row[3,0-3] = xyz
    //              [col=3,row=1] -1 means cull primitive
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
    //      uniform 23 : common transform to move/scale objects
    // Out:
    //      gl_Position
    //      location 1 modelpos
    //      location 2 instance id
    //      location 3 basecolour


    public class GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation : GLShaderPipelineShadersBase
    {
        public string Code()       // with transform, object needs to pass in uniform 22 the transform
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

        modelpos = modelposition.xyz;
        vec4 modelrot = transform * modelposition;
        vec4 worldposition = vec4(worldpos[3][0],worldpos[3][1],worldpos[3][2],0);      // extract world position from row3 columns 0/1/2 (floats 12-14)
        vec4 wp = modelrot + worldposition + vec4(worldoffset,0);
	    gl_Position = mc.ProjectionModelMatrix * wp;        // order important
        instance = gl_InstanceID;
    }
}
";
        }

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
            OFC.GLStatics.Check();
        }
    }
}
