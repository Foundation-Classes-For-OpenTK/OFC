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
using System.Drawing;

namespace OFC.GL4
{
    // Pipeline shader, Matrix Translation, Tex out, image selection out, Quads in, no vertex input, no tex input, Lookat control
    // Requires:
    //      no model vertex input, its auto gen to y=0, x=+/-1, z = +/-1
    //      vertex 4-7 : transform: mat4 array of transforms, one per instance 
    //              [col=3,row=0] is the image index, 
    //              [col=3,row=1] 0 rotate as per matrix, 1 means look at in azimuth, 2 look at in elevation and azimuth, <0 means cull primitive
    //              [col=3,row=2] Fade scaler, 0 = none.  >0 fade out as eye goes in, <0 fade in as eye goes in
    //              [col=3,row=3] Fade End, 0 = none.   for fade out formula is alpha = clamp((EyeDistance-fade end)/Fade scalar,0,1). 
    //                                                  for fade in formula is alpha = clamp((fadeend-EyeDistance)/-Fade scalar,0,1). 
    //      uniform buffer 0 : GL MatrixCalc
    // Out:
    //      location 0 : vs_textureCoordinate
    //      location 2 : image index to use
    //      location 3 : alpha blend to use
    //      gL_Position

    public class GLPLVertexShaderQuadTextureWithMatrixTranslation : GLShaderPipelineShadersBase
    {
        public string Code()
        {
            return
@"
    #version 450 core
    #include UniformStorageBlocks.matrixcalc.glsl
    #include Shaders.Functions.trig.glsl
    #include Shaders.Functions.mat4.glsl

    layout (location = 4) in mat4 transform;

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
    
    vec4 vertex[] = { vec4(-0.5,0,0.5,1), vec4(-0.5,0,-0.5,1), vec4(0.5,0,-0.5,1), vec4(0.5,0,0.5,1)};      // flat on z plane is the default
    vec2 tex[] = { vec2(0,0), vec2(0,1), vec2(1,1), vec2(1,0)};


//layout (binding = 31, std430) buffer Positions      // For debug
//{
//    vec3 wpout;
//    vec3 epout;
//    vec4 dirout;
//};


    void main(void)
    {
        mat4 tx = transform;
        vs.vs_index = int(tx[0][3]);                                // row/col ordering

        vec3 worldposition = vec3(tx[3][0],tx[3][1],tx[3][2]);      // extract world position from row3 columns 0/1/2 

        //wpout = worldposition; epout = mc.EyePosition.xyz; // for debug

        if ( tx[2][3]>0)                                      // fade distance, >0 means fade out as eye goes in
            alpha = clamp((mc.EyeDistance-tx[3][3])/tx[2][3],0,1);  // fade end is 3,3
        else if (tx[2][3]<0)
            alpha = clamp((tx[3][3]-mc.EyeDistance)/-tx[2][3],0,1); // <0 means fade in as eye goes in
        else
            alpha = 1;

        float ctrl = tx[1][3];              // control word for rotate

        if ( ctrl < 0 )                     // -1 cull
        {
            gl_CullDistance[0] = -1;        // all vertex culled
        }
        else 
        {
            gl_CullDistance[0] = +1;        // not culled

            if ( ctrl == 0 )                // if not auto rotate to viewer
            {
                tx[0][3] = tx[1][3] = tx[2][3] = 0;     // use the matrix supplied, correct for flags
                tx[3][3] = 1;
            }
            else
            {
                vec3 scale = vec3(tx[0][0],tx[1][1],tx[2][2]);

                vec2 dir = AzEl(mc.EyePosition.xyz,worldposition);      // x = elevation y = azimuth        eye to world. see GLPLVertexScaleLookat
                tx = mat4ScalethenRotateXthenYthenTranslation(ctrl >= 2 ? -(PI-dir.x) : -PI/2,dir.y,scale,worldposition);

            // dirout = vec4(degrees(dir.x),degrees(dir.y),mc.EyeDistance,alpha); // for debug
            }

            gl_Position = mc.ProjectionModelMatrix * tx * vertex[gl_VertexID];    
        }
            
        vs_textureCoordinate = tex[gl_VertexID];
    }
    ";
        }

        public GLPLVertexShaderQuadTextureWithMatrixTranslation()
        {
            CompileLink(ShaderType.VertexShader, Code(), auxname: GetType().Name);
        }

        // create a matrix for this shader
        static public Matrix4 CreateMatrix(Vector3 worldpos,
                                    Vector3 size,       // Note if Y and Z are zero, then Z is set to same ratio to width as bitmap
                                    Vector3 rotationradians,        // ignored if rotates are on
                                    bool rotatetoviewer = false, bool rotateelevation = false,   // if set, rotationradians not used
                                    float alphafadescalar = 0,
                                    float alphafadeend = 0,
                                    int imagepos = 0,
                                    bool visible = true
            )
        {
            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.Mult(mat, Matrix4.CreateScale(size));
            if (rotatetoviewer == false)                                            // if autorotating, no rotation is allowed. matrix is just scaling/translation
            {
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationX(rotationradians.X));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationY(rotationradians.Y));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationZ(rotationradians.Z));
            }
            mat = Matrix4.Mult(mat, Matrix4.CreateTranslation(worldpos));
            mat[0, 3] = imagepos;
            mat[1, 3] = !visible ? -1 : rotatetoviewer ? (rotateelevation ? 2 : 1) : 0;  // and rotation selection. This is master ctrl, <0 culled, >=0 shown
            mat[2, 3] = alphafadescalar;
            mat[3, 3] = alphafadeend;
            return mat;
        }

        static public Matrix4[] CreateMatrices(Vector4[] worldpos, Vector3 offset,
                                            Vector3 size, Vector3 rot, bool rotatetoviewer, bool rotateelevation,
                                            float alphafadescalar = 0,
                                            float alphafadeend = 0,
                                            int imagepos = 0,
                                            bool visible = true
                                            )
        {
            Matrix4[] mats = new Matrix4[worldpos.Length];
            for (int i = 0; i < worldpos.Length; i++)
                mats[i] = CreateMatrix(worldpos[i].Xyz + offset, size, rot, rotatetoviewer, rotateelevation, alphafadescalar, alphafadeend, imagepos, visible);
            return mats;
        }
    }

}

