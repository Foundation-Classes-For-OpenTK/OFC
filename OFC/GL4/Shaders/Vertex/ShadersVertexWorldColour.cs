/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
using OpenTK;
using OpenTK.Graphics.OpenGL4;

// Vertex shaders taking world positions with colour

namespace GLOFC.GL4.Shaders.Vertex
{
    /// <summary>
    /// Shader with colour input
    /// Requires:
    ///      location 0 : vec4 positions in world space (W ignored)
    ///      location 1 : vec4 color components
    ///      uniform buffer 0 : standard Matrix uniform block GLMatrixCalcUniformBlock
    ///      uniform 22 : optional offset
    /// Out:
    ///      location 0: vs_color
    ///      gl_Position
    /// </summary>

    public class GLPLVertexShaderColorWorldCoord : GLShaderPipelineComponentShadersBase
    {
        /// <summary> Constructor </summary>
        /// <param name="useoffset">Use vector 4 offset from uniform 22 and add</param>
        public GLPLVertexShaderColorWorldCoord(bool useoffset = false)
        {
            CompileLink(ShaderType.VertexShader, Code(), constvalues:new object[] { "useoffset", useoffset }, auxname: GetType().Name);
        }

        /// <summary> Set Offset </summary>
        public void SetOffset(Vector4 y)
        {
            GL.ProgramUniform4(Id, 22, y);
        }

        private string Code()
        {
            return
@"
#version 450 core
#include UniformStorageBlocks.matrixcalc.glsl

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 color;

layout (location = 22) uniform vec4 offset;

out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };


layout(location = 0) out vec4 vs_color;

const bool useoffset = false;

void main(void)
{
    vec4 pos = vec4(position.xyz,1);
    if ( useoffset)
        pos += offset;

	gl_Position = mc.ProjectionModelMatrix * pos;        // order important
	vs_color = color;                                                   // pass to fragment shader
}
";
        }
    }

    /// <summary>
    /// Shader Colours
    /// Requires:
    ///      location 0 : position: vec4 vertex array of world positions, w = colour image index,  fixed Y if required
    ///      uniform buffer 0 : GL MatrixCalc with ScreenMatrix set up
    /// Out:
    ///      location 0: vs_color
    ///      gl_Position
    /// </summary>

    public class GLPLVertexShaderFixedColorPalletWorldCoords : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="colourarray">Array of colours as vec4</param>
        /// <param name="yfromuniform">True to take Y from uniform 22</param>
        public GLPLVertexShaderFixedColorPalletWorldCoords(Vector4[] colourarray, bool yfromuniform = false)
        {
            CompileLink(ShaderType.VertexShader, Code(), constvalues: new object[] { "palette", colourarray, "yfromuniform", yfromuniform }, auxname: GetType().Name);
        }

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="colourarray">Array of colours</param>
        /// <param name="yfromuniform">True to take Y from uniform 22</param>
        public GLPLVertexShaderFixedColorPalletWorldCoords(System.Drawing.Color[] colourarray, bool yfromuniform) : this(colourarray.ToVector4(), yfromuniform)
        {
        }

        /// <summary> Set Y </summary>
        public void SetY(float y)
        {
            GL.ProgramUniform1(Id, 22, y);
        }
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
layout (location = 22) uniform  float replacementy;

const vec4[] palette = { };
const bool yfromuniform = false;

void main(void)
{
    vec4 pos;
    if ( yfromuniform )
        pos = vec4(position.x,replacementy,position.z,1);
    else
        pos = vec4(position.xyz,1);

    int colourindex = int(position.w);
	gl_Position = mc.ProjectionModelMatrix * pos;        // order important
    vs_color = palette[colourindex];
}
";
        }

    }



}
