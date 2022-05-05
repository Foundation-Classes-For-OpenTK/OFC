﻿/*
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

namespace GLOFC.GL4.Shaders.Vertex
{
    ///<summary>
    /// Shader for Quads (no model position in, autogenerated model and texture co-ords). Compatibility mode only
    /// Matrix per Quad, controlling position, rotation, autoscaling, image selection, auto rotation
    /// </summary>

    public class GLPLVertexShaderMatrixQuadTexture : GLShaderPipelineComponentShadersBase
    {
        /// <summary>
        /// Constructor
        /// Requires:
        ///      vertex 4-7 : transform: mat4 array of transforms, one per instance 
        ///              [col=3,row=0] is the image index, 
        ///              [col=3,row=1] 0 rotate as per matrix, 1 means look at in azimuth, 2 look at in elevation and azimuth, less than 0 means cull primitive
        ///              [col=3,row=2] Fade scaler, 0 = none.  >0 fade out as eye goes in, less than 0 fade in as eye goes in
        ///              [col=3,row=3] Fade Pos, 0 = none.   for fade out formula is alpha = clamp((EyeDistance-fade pos)/Fade scalar,0,1). At EyeDistance less than fadepos, alpha is 0
        ///                                                  for fade in formula is alpha = clamp((fadepos-EyeDistance)/-Fade scalar,0,1). At EyeDistance greater than fadepos, alpha is 0
        ///      uniform buffer 0 : GL MatrixCalc
        ///      uniform 22 : float replacement Y (if enabled)
        /// Out:
        ///      location 0 : vs_textureCoordinate
        ///      location 2 : image index to use
        ///      location 3 : alpha blend to use
        ///      gL_Position
        /// </summary>
        /// <param name="yfromuniform">Take Y value from uniform, instead of transform </param>
        public GLPLVertexShaderMatrixQuadTexture(bool yfromuniform = false)
        {
            CompileLink(ShaderType.VertexShader, Code(), constvalues: new object[] { "yfromuniform", yfromuniform }, auxname: GetType().Name);
        }

        /// <summary> Set Y position for override </summary>
        public void SetY(float y)
        {
            GL.ProgramUniform1(Id, 22, y);
        }

        /// <summary>
        /// Create a Matrix for use with this shader
        /// </summary>
        /// <param name="worldpos">Position</param>
        /// <param name="size">Scale</param>
        /// <param name="rotationradians">Rotation of object</param>
        /// <param name="rotatetoviewer">True to rotate in azimuth to viewer</param>
        /// <param name="rotateelevation">True to rotate in elevation to viewer</param>
        /// <param name="alphafadescalar">Alpha fade scalar with EyeDistance (lookat-eye)</param>
        /// <param name="alphafadepos">Alpha fade distance. Negative for fade in, positive for fade out</param>
        /// <param name="imagepos">Image index into texture, passed to fragement shader</param>
        /// <param name="visible">If visible</param>
        /// <returns></returns>
        static public Matrix4 CreateMatrix(Vector3 worldpos,
                                    Vector3 size,       // Note if Y and Z are zero, then Z is set to same ratio to width as bitmap
                                    Vector3 rotationradians,        // ignored if rotates are on
                                    bool rotatetoviewer = false, bool rotateelevation = false,   // if set, rotationradians not used
                                    float alphafadescalar = 0,
                                    float alphafadepos = 0,
                                    int imagepos = 0,
                                    bool visible = true
            )
        {
            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.Mult(mat, Matrix4.CreateScale(size));
            if (rotatetoviewer == false && rotationradians.LengthSquared>0)   // if autorotating, no rotation is allowed. matrix is just scaling/translation
            {
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationX(rotationradians.X));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationY(rotationradians.Y));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationZ(rotationradians.Z));
            }
            mat = Matrix4.Mult(mat, Matrix4.CreateTranslation(worldpos));
            mat[0, 3] = imagepos;
            mat[1, 3] = !visible ? -1 : rotatetoviewer ? (rotateelevation ? 2 : 1) : 0;  // and rotation selection. This is master ctrl, <0 culled, >=0 shown
            mat[2, 3] = alphafadescalar;
            mat[3, 3] = alphafadepos;
            return mat;
        }

        /// <summary>
        /// Make multiple matrices
        /// </summary>
        /// <param name="worldpos">Positions array</param>
        /// <param name="offset">Offset on each position</param>
        /// <param name="size">Size of each</param>
        /// <param name="rotationradians">Rotation of object</param>
        /// <param name="rotatetoviewer">True to rotate in azimuth to viewer</param>
        /// <param name="rotateelevation">True to rotate in elevation to viewer</param>
        /// <param name="alphafadescalar">Alpha fade scalar with EyeDistance (lookat-eye)</param>
        /// <param name="alphafadepos">Alpha fade distance. Negative for fade in, positive for fade out</param>
        /// <param name="imagepos">Image index into texture, passed to fragement shader</param>
        /// <param name="visible">If visible</param>
        /// <param name="pos">Offset into worldpos array to start at</param>
        /// <param name="length">Number of entries to take from world positions</param>
        /// <returns></returns>

        static public Matrix4[] CreateMatrices(Vector4[] worldpos, Vector3 offset,
                                            Vector3 size, Vector3 rotationradians, 
                                            bool rotatetoviewer, bool rotateelevation,
                                            float alphafadescalar = 0,
                                            float alphafadepos = 0,
                                            int imagepos = 0,
                                            bool visible = true,
                                            int pos = 0, int length = -1        // allowing you to pick out a part of the worldpos array
                                            )
        {
            if (length == -1)
                length = worldpos.Length - pos;

            Matrix4[] mats = new Matrix4[length];
            for (int i = 0; i < length; i++)
                mats[i] = CreateMatrix(worldpos[i+pos].Xyz + offset, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, imagepos, visible);
            return mats;
        }

        private string Code()
        {
            return
@"
    #version 450 core
    #include UniformStorageBlocks.matrixcalc.glsl
    #include Shaders.Functions.trig.glsl
    #include Shaders.Functions.mat4.glsl

    layout (location = 4) in mat4 transform;
    layout (location = 22) uniform  float replacementy;

    out gl_PerVertex {
            vec4 gl_Position;
            float gl_PointSize;
            float gl_ClipDistance[];
            float gl_CullDistance[];
        };

    layout( location = 0) out vec2 vs_textureCoordinate;
    layout (location = 2) out VS_OUT
    {
        flat int vs_index;     
    } vs;
    layout (location = 3) out float alpha;
    
    vec4 vertex[] = { vec4(-0.5,0,0.5,1), vec4(-0.5,0,-0.5,1), vec4(0.5,0,-0.5,1), vec4(0.5,0,0.5,1)};      // flat on xz plane is the default
    vec2 tex[] = { vec2(0,0), vec2(0,1), vec2(1,1), vec2(1,0)};

    layout (binding = 31, std430) buffer Positions      // For debug
    {
        vec4 txout;
    };

    const bool yfromuniform = false;

    void main(void)
    {
        mat4 tx = transform;
        vs.vs_index = int(tx[0][3]);                                // row/col ordering

        if ( yfromuniform)                                          // optional fixed y
            tx[3][1] = replacementy;

        vec3 worldposition = vec3(tx[3][0],tx[3][1],tx[3][2]);      // extract world position from row3 columns 0/1/2 , y possibly fixed

        //wpout = worldposition; epout = mc.EyePosition.xyz; // for debug

        if ( tx[2][3]>0)                                      // fade distance, >0 means fade out as eye goes in
            alpha = clamp((mc.EyeDistance-tx[3][3])/tx[2][3],0,1);  // fade end is 3,3
        else if (tx[2][3]<0)
            alpha = clamp((tx[3][3]-mc.EyeDistance)/-tx[2][3],0,1); // <0 means fade in as eye goes in
        else
            alpha = 1;

        // txout = vec4(mc.EyeDistance,tx[2][3], tx[3][3], alpha); // debugging

        float ctrl = tx[1][3];              // control word for rotate

        if ( ctrl < 0 )                     // -1 cull
        {
            gl_CullDistance[0] = -1;        // all vertex culled
        }
        else 
        {
            gl_CullDistance[0] = +1;        // not culled

            if ( ctrl == 0 )                // if no auto rotate
            {
                tx[0][3] = tx[1][3] = tx[2][3] = 0;     // use the matrix supplied, correct for flags
                tx[3][3] = 1;
            }
            else
            {
                vec3 scale = vec3(tx[0][0],tx[1][1],tx[2][2]);

                vec2 dir = AzEl(mc.EyePosition.xyz,worldposition);      // x = elevation y = azimuth        eye to world. see GLPLVertexScaleLookat
                tx = mat4ScalethenRotateXthenYthenTranslation(ctrl >= 2 ? -(PI-dir.x) : -PI/2,dir.y,scale,worldposition);
            }

            gl_Position = mc.ProjectionModelMatrix * tx * vertex[gl_VertexID];    
        }
            
        vs_textureCoordinate = tex[gl_VertexID];
    }
    ";
        }


    }

}

