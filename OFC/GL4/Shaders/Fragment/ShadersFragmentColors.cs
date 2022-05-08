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

using GLOFC.Utils;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Fragment
{
    /// <summary>
    /// Pipeline shader, Fixed Colour fragment shader
    /// </summary>

    public class GLPLFragmentShaderFixedColor : GLShaderPipelineComponentShadersBase
    {
        private OpenTK.Graphics.Color4 col;

        /// <summary>
        /// Construcor for fixed colour shader
        /// Requires: No inputs 
        /// </summary>
        /// <param name="color">Color to paint</param>
        /// <param name="saveable">Make it saveable</param>
        public GLPLFragmentShaderFixedColor(OpenTK.Graphics.Color4 color, bool saveable = false)
        {
            col = color;
            CompileLink(ShaderType.FragmentShader, Code(), out string unused, saveable: saveable);
        }

        private string Code()
        {
            return
@"
#version 450 core
out vec4 color;

void main(void)
{
    color = vec4(" + col.R.ToStringInvariant() + "," + col.G.ToStringInvariant() + "," + col.B.ToStringInvariant() + "," + col.A.ToStringInvariant() + @");
}
";
        }

    }

    /// <summary>
    /// Pipeline shader, uniform decides colour, use GLRenderDataTranslationRotationColour or similar to set the uniform on a per draw basis
    /// </summary>

    public class GLPLFragmentShaderUniformColor : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      uniform : vec4 of colour
        /// </summary>
        /// <param name="uniform">Uniform number to get colour from</param>
        /// <param name="saveable">Make it saveable</param>
        public GLPLFragmentShaderUniformColor(int uniform = 25, bool saveable = false)
        {
            CompileLink(ShaderType.FragmentShader, Code(), out string unused, constvalues: new object[] { "bindingpoint", uniform }, saveable: saveable);
        }

        private string Code()
        {
            return
@"
#version 450 core
out vec4 color;

const int bindingpoint = 25;
layout (location=bindingpoint) uniform vec4 ucol;

void main(void)
{
    color = ucol;
}
";
        }
    }

    /// <summary>
    /// Vertex shader colour pass to it 
    /// </summary>
    public class GLPLFragmentShaderVSColor : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      location 0 : vec4 of colour
        /// </summary>
        /// <param name="saveable">Make it saveable</param>
        public GLPLFragmentShaderVSColor(bool saveable = false)
        {
            CompileLink(ShaderType.FragmentShader, Code(), out string unused, saveable: saveable);
        }

        private string Code()
        {
            return
@"
#version 450 core
layout(location=0) in vec4 vs_color;
out vec4 color;

void main(void)
{
	color = vs_color;
}
";
        }

    }

    /// <summary>
    /// Shader, Fixed shader of one of six colours based on primitive ID, selectable divisor, mostly for testing
    /// </summary>

    public class GLPLFragmentIDShaderColor : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="divisor">Primitive ID divisor</param>
        /// <param name="saveable">Make it saveable</param>
        public GLPLFragmentIDShaderColor(int divisor, bool saveable = false)
        {
            CompileLink(ShaderType.FragmentShader, Code(divisor), out string unused, saveable: saveable);
        }

        private string Code(int divisor)
        {
            return
@"
#version 450 core
out vec4 color;

void main(void)
{
    int side = gl_PrimitiveID/" + divisor.ToStringInvariant() + @";
    vec4 sc[] = { vec4(1,0,0,1),vec4(0,1,0,1),vec4(0,0,1,1),vec4(1,1,0,1),vec4(0,1,1,1),vec4(1,1,1,1)};
	color = sc[side % 6];
}
";
        }
    }
}
