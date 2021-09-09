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

using OpenTK;
using OpenTK.Graphics.OpenGL4;

// Vertex shaders taking world positions

namespace GLOFC.GL4
{
    // No extra translation, direct move
    // Requires:
    //      location 0 : vec4 positions
    //      uniform buffer 0 : standard Matrix uniform block GLMatrixCalcUniformBlock

    public class GLPLVertexShaderWorldCoord : GLShaderPipelineShadersBase
    {
        private string Code()
        {
            return
@"
#version 450 core

#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

void main(void)
{
	gl_Position = mc.ProjectionModelMatrix * position;        // order important
}
";
        }

        public GLPLVertexShaderWorldCoord()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }
    }

    // No modelview, just project view. Co-ords are in model view values
    // Requires:
    //      location 0 : vec4 positions
    //      uniform buffer 0 : standard Matrix uniform block GLMatrixCalcUniformBlock

    public class GLPLVertexShaderModelViewCoord: GLShaderPipelineShadersBase
    {
        private string Code()
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl
layout (location = 0) in vec4 position;
out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

void main(void)
{
	gl_Position = mc.ProjectionMatrix * position;        // order important
}
";
        }

        public GLPLVertexShaderModelViewCoord()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }
    }



    // No extra translation, direct move, but with colour
    // Requires:
    //      location 0 : vec4 positions in world space
    //      location 1 : vec4 color components
    //      uniform buffer 0 : standard Matrix uniform block GLMatrixCalcUniformBlock
    // Out:
    //      location 0: vs_color
    //      gl_Position

    public class GLPLVertexShaderColorWorldCoord : GLShaderPipelineShadersBase
    {
        private string Code()
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 color;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };


layout(location = 0) out vec4 vs_color;

void main(void)
{
	gl_Position = mc.ProjectionModelMatrix * position;        // order important
	vs_color = color;                                                   // pass to fragment shader
}
";
        }

        public GLPLVertexShaderColorWorldCoord()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }
    }

    // Pipeline shader, Texture, Modelpos, transform
    // Requires:
    //      location 0 : position: vec4 vertex array of positions
    //      location 1 : vec2 texture co-ords
    //      uniform buffer 0 :  : GL MatrixCalc
    // Out:
    //      gl_Position
    //      location 0 : vs_textureCoordinate
    //      location 1 : modelpos

    public class GLPLVertexShaderTextureWorldCoord : GLShaderPipelineShadersBase
    {
        public GLPLVertexShaderTextureWorldCoord()
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
layout(location = 1) in vec2 texco;

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
	gl_Position = mc.ProjectionModelMatrix * position;        // order important
    vs_textureCoordinate = texco;
}
";
        }

    }


 
    // Pipeline shader, Texture, real screen coords  (0-glcontrol.Width,0-glcontrol.height, 0,0 at top left)
    // Requires:
    //      location 0 : position: vec4 vertex array of world positions, w = colour image index
    //      uniform buffer 0 : GL MatrixCalc with ScreenMatrix set up
    // Out:
    //      location 0: vs_color
    //      gl_Position

    public class GLPLVertexShaderFixedColorPalletWorldCoords : GLShaderPipelineShadersBase
    {
        private string Code()
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

layout(location = 0) out vec4 vs_color;

const vec4[] palette = { };

void main(void)
{
    vec4 pos = vec4(position.xyz,1);
    int colourindex = int(position.w);
	gl_Position = mc.ProjectionModelMatrix * pos;        // order important
    vs_color = palette[colourindex];
}
";
        }

        public GLPLVertexShaderFixedColorPalletWorldCoords(Vector4[] varray)
        {
            CompileLink(ShaderType.VertexShader, Code(), constvalues: new object[] { "palette", varray }, auxname: GetType().Name);
        }

        public GLPLVertexShaderFixedColorPalletWorldCoords(System.Drawing.Color[] cpal) : this( cpal.ToVector4() )
        { 
        }
    }



}
