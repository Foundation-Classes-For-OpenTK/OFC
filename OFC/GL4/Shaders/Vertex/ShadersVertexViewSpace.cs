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

using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    // vertex have already been modelview transformed. Perform projection view. Co-ords are in model view values
    // Requires:
    //      location 0 : vec4 positions
    //      uniform buffer 0 : standard Matrix uniform block GLMatrixCalcUniformBlock

    public class GLPLVertexShaderViewSpaceCoord : GLShaderPipelineComponentShadersBase
    {
        public GLPLVertexShaderViewSpaceCoord()
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
out gl_PerVertex {
        vec4 gl_Position;
        float gl_PointSize;
        float gl_ClipDistance[];
    };

    //      uniform 22 : objecttransform: mat4 array of transforms
void main(void)
{
	gl_Position = mc.ProjectionMatrix * position;        // order important
}
";
        }
    }
}
