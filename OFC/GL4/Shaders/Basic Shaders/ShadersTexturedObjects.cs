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
using GLOFC.GL4.Shaders.Fragment;

namespace GLOFC.GL4.Shaders.Basic
{
    /// <summary>
    /// Texture, world co-ords
    /// Requires:
    ///      location 0 : position: vec4 vertex array of positions world coords
    ///      location 1 : vec2 texture co-ords
    ///      tex binding 1 : textureObject : 2D 
    ///      uniform 0 : GL MatrixCalc
    /// </summary>

    public class GLTexturedShaderWithWorldCoord : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// </summary>
        public GLTexturedShaderWithWorldCoord(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderTextureWorldCoord(), new GLPLFragmentShaderTexture());
        }
    }

    /// <summary>
    /// Texture, translation
    /// Requires:
    ///      location 0 : position: vec4 vertex array of positions
    ///      location 1 : vec2 texture co-ords
    ///      tex binding 1 : textureObject : 2D 
    ///      uniform 0 : GL MatrixCalc
    ///      uniform 22 : objecttransform: mat4 transform
    /// </summary>

    public class GLTexturedShaderWithObjectTranslation : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// </summary>
        public GLTexturedShaderWithObjectTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderTextureModelCoordWithObjectTranslation(), new GLPLFragmentShaderTexture());
        }
    }

    /// <summary>
    /// Texture, translation, common translation
    /// Requires:
    ///      location 0 : position: vec4 vertex array of positions
    ///      location 1 : vec2 texture co-ords
    ///      tex binding 1 : textureObject : 2D 
    ///      uniform block 0 : GL MatrixCalc
    ///      uniform 22 : objecttransform: mat4 transform
    ///      uniform 23 : commontransform: mat4 transform
    /// </summary>

    public class GLTexturedShaderWithObjectCommonTranslation : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// </summary>
        public GLTexturedShaderWithObjectCommonTranslation(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderTextureModelCoordsWithObjectCommonTranslation(), new GLPLFragmentShaderTexture());
        }
    }

    /// <summary>
    /// Texture, translation, 2d blend
    /// Requires:
    ///      location 0 : position: vec4 vertex array of positions
    ///      location 1 : vec2 texture co-ords
    ///      tex binding 1 : textureObject : 2D array texture of two bitmaps, 0 and 1.
    ///      uniform block 0 : GL MatrixCalc
    ///      uniform 22 : objecttransform: mat4 transform
    ///      uniform 30 : uniform float blend between the two texture
    /// </summary>

    public class GLTexturedShader2DBlendWithWorldCoord : GLShaderPipeline
    {
        /// <summary>
        ///  Constructor
        /// </summary>
        public GLTexturedShader2DBlendWithWorldCoord(Action<IGLProgramShader, GLMatrixCalc> start = null, Action<IGLProgramShader> finish = null) : base(start, finish)
        {
            AddVertexFragment(new GLPLVertexShaderTextureModelCoordWithObjectTranslation(), new GLPLFragmentShaderTexture2DBlend());
        }
    }


}
