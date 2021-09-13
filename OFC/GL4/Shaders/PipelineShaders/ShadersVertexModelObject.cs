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

// Vertex shaders, having a model input, objectransform 

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
//modelpos = vec3(gl_VertexID,gl_VertexID*10,gl_VertexID*20);
	gl_Position = mc.ProjectionModelMatrix * transform * vec4(position.xyz,1);        // order important
	vs_color = color;                                                   // pass to fragment shader
}
";
        }

    }

    // Pipeline shader, Translation, Texture, Modelpos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions model coords, w is ignored
    //      location 1 : vec2 texture co-ords
    //      uniform buffer 0 : GL MatrixCalc
    //      uniform 22 : objecttransform: mat4 array of transforms
    // Out:
    //      gl_Position
    //      location 0: vs_textureCoordinate
    //      location 1: modelpos


    public class GLPLVertexShaderTextureModelCoordWithObjectTranslation : GLShaderPipelineComponentShadersBase
    {
        public GLPLVertexShaderTextureModelCoordWithObjectTranslation(bool saveable = false)
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


}
