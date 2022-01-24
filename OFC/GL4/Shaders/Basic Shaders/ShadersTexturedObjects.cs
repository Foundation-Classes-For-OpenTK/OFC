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

using System;
using GLOFC.GL4.Shaders;
using GLOFC.GL4.Shaders.Vertex;
using GLOFC.GL4.Shaders.Fragment;

namespace GLOFC.GL4.Shaders.Basic
{
    /// <summary>
    /// Texture, world co-ords
    /// </summary>

    public class GLTexturedShaderWorld : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions world coords
        ///      location 1 : vec2 texture co-ords
        ///      tex binding 1 : textureObject : 2D 
        ///      uniform 0 : GL MatrixCalc
        /// </summary>
        public GLTexturedShaderWorld(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderWorldTexture(), new GLPLFragmentShaderTexture());
        }
    }

    /// <summary>
    /// Texture, translation
    /// </summary>

    public class GLTexturedShaderObjectTranslation : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions
        ///      location 1 : vec2 texture co-ords
        ///      tex binding 1 : textureObject : 2D 
        ///      uniform 0 : GL MatrixCalc
        ///      uniform 22 : objecttransform: mat4 transform
        /// </summary>
        public GLTexturedShaderObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderModelTextureTranslation(), new GLPLFragmentShaderTexture());
        }
    }

    /// <summary>
    /// Texture, translation, common translation
    /// </summary>

    public class GLTexturedShaderObjectCommonTranslation : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions
        ///      location 1 : vec2 texture co-ords
        ///      tex binding 1 : textureObject : 2D 
        ///      uniform block 0 : GL MatrixCalc
        ///      uniform 22 : objecttransform: mat4 transform
        ///      uniform 23 : commontransform: mat4 transform
        /// </summary>
        public GLTexturedShaderObjectCommonTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderModelTranslationTexture(), new GLPLFragmentShaderTexture());
        }
    }

    /// <summary>
    /// Texture, translation, 2d blend
    /// </summary>

    public class GLTexturedShader2DBlendWorld : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// Requires:
        ///      location 0 : position: vec4 vertex array of positions
        ///      location 1 : vec2 texture co-ords
        ///      tex binding 1 : textureObject : 2D array texture of two bitmaps, 0 and 1.
        ///      uniform block 0 : GL MatrixCalc
        ///      uniform 22 : objecttransform: mat4 transform
        ///      uniform 30 : uniform float blend between the two texture
        /// </summary>
        public GLTexturedShader2DBlendWorld(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderModelTextureTranslation(), new GLPLFragmentShaderTexture2DBlend());
        }
    }


}
