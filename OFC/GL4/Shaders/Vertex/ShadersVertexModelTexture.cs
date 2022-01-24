/*
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

using GLOFC.GL4.Shaders;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

// Vertex shaders, having a model input, texture coords

namespace GLOFC.GL4.Shaders.Vertex
{
    /// <summary>
    /// Shader, Translation, Texture, Modelpos, transform
    /// </summary>

    public class GLPLVertexShaderModelTextureTranslation : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions model coords, w is ignored
        ///      location 1 : vec2 texture co-ords
        ///      uniform buffer 0 : GL MatrixCalc
        ///      uniform 22 : objecttransform: mat4 array of transforms
        /// Out:
        ///      gl_Position
        ///      location 0: vs_textureCoordinate
        ///      location 1: modelpos
        /// </summary>
        /// <param name="saveable">Is shader to be saveable</param>
        public GLPLVertexShaderModelTextureTranslation(bool saveable = false)
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name, saveable: saveable);
        }

        private string Code()       // with transform, object needs to pass in uniform 22 the transform
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
    }

    ///<summary>
    /// Shader, Translation, Texture, autoscale and with matrix controlling rotation, position
    /// image scaling with min/max per object
    /// </summary>

    public class GLPLVertexShaderModelMatrixTexture : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions model coords
        ///      location 1 : vec2 texture co-ordinates
        ///      location 4 : transform: mat4 array of transforms.  position and rotation only, unit size, with:
        ///                   [0][3] = image min size (autoscale>0)
        ///                   [1][3] = image max size (autoscale>0)
        ///                   [2][3] = image scaling (autoscale>0)
        ///                   [3][3] = image no
        ///      uniform buffer 0 : GL MatrixCalc
        /// Out:
        ///      location 0 : vs_textureCoordinate
        ///      location 1 : modelpos
        ///      location 2 : instance count
        ///      location 4 : transform[3][3] image no value as int
        ///      gl_Position
        /// </summary>
        /// <param name="autoscale">To autoscale distance. Sets the 1.0 scale point.</param>
        /// <param name="useeyedistance">Use eye distance to lookat to autoscale, else use distance between object and eye</param>
        public GLPLVertexShaderModelMatrixTexture(float autoscale = 0, bool useeyedistance = true)
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name, constvalues: new object[] {
                                                                    "autoscale", autoscale,
                                                                    "useeyedistance", useeyedistance });
        }

        private string Code()
        {
            return
@"
#version 450 core

#include UniformStorageBlocks.matrixcalc.glsl
#include Shaders.Functions.vec4.glsl

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
layout (location = 4) out VS_OUT2
{
    flat int m33;
} vs_out2;

const float autoscale = 0;
const bool useeyedistance = true;

void main(void)
{
    modelpos = position.xyz;
    vec4 pos= vec4(position.xyz,1);

    mat4 tx = transform;

    if ( autoscale>0)
    {
        float scale;
        if ( useeyedistance )
        {
            scale = mc.EyeDistance/autoscale;
        }
        else
        {
            vec4 worldpos = vec4(transform[3][0],transform[3][1],transform[3][2],0);
            float d = distance(mc.EyePosition,worldpos);            // find distance between eye and world pos
            scale = d/autoscale;
        }

        scale = clamp(scale*transform[2][3],tx[0][3],tx[1][3]);
        tx[0][3]=0;
        tx[1][3]=0;
        tx[2][3]=0;

        pos = Scale(pos,scale);
    }

    vs_out2.m33 = int(transform[3][3]);
    tx[3][3]=1;

	gl_Position = mc.ProjectionModelMatrix * tx * pos;        // order important
    vs_textureCoordinate = texco;
    vs_out.vs_instanced = gl_InstanceID;
}
";
        }

    }


    ///<summary>
    /// Shader, Translation, Texture, Common transform, Object transform, Auto Scale
    /// uniform 22 should be supplied via RenderItemData
    /// </summary>

    public class GLPLVertexShaderModelTranslationTexture : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Rotation to apply 
        /// </summary>
        public GLRenderDataTranslationRotation Transform { get; set; }           // only use this for rotation - position set by object data

        /// <summary>
        ///  Constructor
        /// Requires:
        ///      location 0 : position: vec4 vertex array of model positions
        ///      location 1 : vec2 texture co-ords
        ///      uniform 0 : GL MatrixCalc
        ///      uniform 22 : objecttransform: mat4 array of transforms
        ///      uniform 23 : commontransform: mat4 array of transforms
        /// Out:
        ///      location 0: vs_textureCoordinate
        ///      gl_Position
        /// </summary>
        /// <param name="autoscale">To autoscale distance. Sets the 1.0 scale point.</param>
        /// <param name="autoscalemin">Minimum to scale to</param>
        /// <param name="autoscalemax">Maximum to scale to</param>
        /// <param name="useeyedistance">Use eye distance to lookat to autoscale, else use distance between object and eye</param>
        public GLPLVertexShaderModelTranslationTexture(float autoscale = 0, float autoscalemin = 0.1f, float autoscalemax = 3f, bool useeyedistance = true)
        {
            Transform = new GLRenderDataTranslationRotation();
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name, constvalues: new object[] {
                                                                    "autoscale", autoscale,
                                                                    "autoscalemin", autoscalemin, "autoscalemax", autoscalemax , "useeyedistance", useeyedistance});
        }

        /// <summary> Start shader </summary>
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
#include Shaders.Functions.vec4.glsl

layout (location = 0) in vec4 position;
layout(location = 1) in vec2 texco;
layout (location = 22) uniform  mat4 objecttransform;
layout (location = 23) uniform  mat4 commontransform;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

const float autoscale = 0;
const float autoscalemax = 0;
const float autoscalemin = 0;
const bool useeyedistance = true;

layout(location = 0) out vec2 vs_textureCoordinate;

void main(void)
{
    vec4 pos = position;        // model positions

    if ( autoscale>0)
    {
        if ( useeyedistance )
            pos = Scale(pos,clamp(mc.EyeDistance/autoscale,autoscalemin,autoscalemax));
        else
        {
            vec4 worldpos = vec4(objecttransform[3][0],objecttransform[3][1],objecttransform[3][2],0);
            float d = distance(mc.EyePosition,worldpos);            // find distance between eye and world pos
            pos = Scale(pos,clamp(d/autoscale,autoscalemin,autoscalemax));
        }
    }

	gl_Position = mc.ProjectionModelMatrix * objecttransform *  commontransform * pos;        // order important
    vs_textureCoordinate = texco;
}
";
        }

    }


    /// <summary>
    /// Shader, Common Model Translation, Seperate World pos, transform
    /// </summary>

    public class GLPLVertexShaderModelWorldTexture : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Rotation to apply </summary>
        public Matrix4 ModelTranslation { get; set; } = Matrix4.Identity;

        /// <summary> Constructor 
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions model coords, w is ignored
        ///      location 1 : texco-ords 
        ///      location 2 : world-position: vec4 vertex array of world pos for model, instanced, w ignored
        ///      uniform buffer 0 : GL MatrixCalc
        ///      uniform 22 : objecttransform: mat4 transform of model before world applied (for rotation/scaling)
        /// Out:
        ///      gl_Position
        ///      location 0 : texco
        ///      location 1 : modelpos
        ///      location 2 : instance id
        /// </summary>
        public GLPLVertexShaderModelWorldTexture()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        /// <summary> Start shader </summary>
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

    }


}
