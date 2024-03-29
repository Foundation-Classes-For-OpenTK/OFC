﻿/*
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

using GLOFC.GL4.Shaders;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Shaders.Sprites
{
    /// <summary>
    /// This namespace contains sprite shaders
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes

    /// <summary>
    /// Point sprite shader based on eye position vs sprite position.  Needs point sprite on and program point size 
    /// </summary>

    public class GLPointSpriteShader : GLShaderStandard
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tex">Texture to use for sprite</param>
        /// <param name="maxsize">Maximum size of sprite</param>
        /// <param name="scale">Scalar for sprite vs disatance</param>
        public GLPointSpriteShader(IGLTexture tex, float maxsize = 120, float scale = 80) : base()
        {
            StartAction = (a,m) =>
            {
                tex.Bind(4);
            };

            CompileLink(vert, frag: frag, vertexconstvars:new object[] { "maxsize", maxsize, "scale", scale });
        }

        string vert =
@"
        #version 450 core

        const float maxsize = 0;        // replaced by const
        const float scale = 0;          // replaced by const

        #include UniformStorageBlocks.matrixcalc.glsl

        layout (location = 0) in vec4 position;     // has w=1
        layout (location = 1) in vec4 color;
        out vec4 vs_color;
        out float calc_size;

        void main(void)
        {
            vec4 pn = vec4(position.x,position.y,position.z,0);
            float d = distance(mc.EyePosition,pn);
            float sf = maxsize-d/scale;

            calc_size = gl_PointSize = clamp(sf,2.0,maxsize);
            gl_Position = mc.ProjectionModelMatrix * position;        // order important
            vs_color = color;
        }
        ";

        string frag =
@"
        #version 450 core

        in vec4 vs_color;
        layout (binding = 4 ) uniform sampler2D texin;
        out vec4 color;
        in float calc_size;

        void main(void)
        {
            if ( calc_size < 2 )
            {
                discard;
            }
            else
            {
                vec4 texcol =texture(texin, gl_PointCoord);
                float l = texcol.x*texcol.x+texcol.y*texcol.y+texcol.z*texcol.z;

                if ( l< 0.05 || texcol.w <= 0.1)
                    discard;
                else
                    color = texcol * vs_color;
            }
        }
        ";


    }
}

