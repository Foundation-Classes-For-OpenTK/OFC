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
using OpenTK;
using System;

namespace GLOFC
{
    /// <summary>
    /// Matrix 4 helpers
    /// </summary>
    public static class GLStaticsMatrix4
    {
        /// <summary>
        /// Perform Approx equals on two matrix, with definable maxerror
        /// </summary>
        static public bool ApproxEquals(Matrix4 lm, Matrix4 rm, float maxerr = 0.0001f)
        {
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (!((double)lm[r,c]).ApproxEquals(rm[r, c]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// To Float array
        /// opengl matrixes are layed out as
        ///  x.x x.y x.z 0       in row order in memory (locations 0,1,2,3 across)
        ///  y.x y.y y.z 0       x, y, z are 3-component vectors describing the matrix coordinate system(local coordinate system within relative to the global coordinate system).
        ///  z.x z.y z.z 0
        ///  p.x p.y p.z 1       p is a 3-component vector describing the origin of matrix coordinate system.
        ///  or in memory order: x.x x.y x.z 0 y.x y.y y.z 0 z.x z.y z.z 0 p.x p.y p.z 1
        /// </summary>

        static public float[] ToFloatArray(this Matrix4 mat)
        {
            float[] r = new float[] {   mat.Row0.X, mat.Row0.Y, mat.Row0.Z, mat.Row0.W ,        // row major order, 
                                        mat.Row1.X, mat.Row1.Y, mat.Row1.Z, mat.Row1.W ,        // as per https://stackoverflow.com/questions/17717600/confusion-between-c-and-opengl-matrix-order-row-major-vs-column-major
                                        mat.Row2.X, mat.Row2.Y, mat.Row2.Z, mat.Row2.W ,        // which works with vtransform = Matrix.Vector order which is what we use
                                        mat.Row3.X, mat.Row3.Y, mat.Row3.Z, mat.Row3.W };       // Matrix4 holds it in row order
            return r;
        }

        /// <summary>
        /// Create a matrix from worldpos, size and rotation
        /// </summary>
        static public Matrix4 CreateMatrix(Vector3 worldpos, Vector3 size, Vector3 rotationradians)
        {
            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.Mult(mat, Matrix4.CreateScale(size));
            if (rotationradians.LengthSquared > 0)
            {
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationX(rotationradians.X));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationY(rotationradians.Y));
                mat = Matrix4.Mult(mat, Matrix4.CreateRotationZ(rotationradians.Z));
            }
            mat = Matrix4.Mult(mat, Matrix4.CreateTranslation(worldpos));
            return mat;
        }

        /// <summary>
        /// Create a Planet type rotation matrix
        /// presumes axis is on X plane, positive tips away, and rotation on Y plane, postive values rotate anticlockwise
        /// </summary>
        static public Matrix4 CreateMatrixPlanetRot(Vector3 worldpos, Vector3 size, float axis, float rotation) 
        {
            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.Mult(mat, Matrix4.CreateScale(size));
            mat = Matrix4.Mult(mat, Matrix4.CreateRotationY(-rotation));
            mat = Matrix4.Mult(mat, Matrix4.CreateRotationX(axis));
            mat = Matrix4.Mult(mat, Matrix4.CreateTranslation(worldpos));
            return mat;
        }

        /// <summary>
        /// Create a Viewing and Fading control matrix as used by shaders
        ///              [col=3,row=0] is the image index, 
        ///              [col=3,row=1] 0 rotate as per matrix, 1 means look at in azimuth, 2 look at in elevation and azimuth, less than 0 means cull primitive
        ///              [col=3,row=2] Fade scaler, 0 = none.  
        ///              [col=3,row=3] Fade Pos, 0 = none.   
        ///                     scaler positive : fade out as eye goes in. formula is alpha = clamp((EyeDistance-fade pos)/Fade scalar,0,1). At EyeDistance less than fadepos, alpha is 0
        ///                     scaler negative : fade in as eye goes in. alpha = clamp((fadepos-EyeDistance)/-Fade scalar,0,1). At EyeDistance greater than fadepos, alpha is 0
        ///                     scaler = 0 : pos is absolute fade value in 0-1
        /// </summary>
        /// <param name="worldpos">Position</param>
        /// <param name="size">Scale</param>
        /// <param name="rotationradians">Rotation of object</param>
        /// <param name="rotatetoviewer">True to rotate in azimuth to viewer</param>
        /// <param name="rotateelevation">True to rotate in elevation to viewer</param>
        /// <param name="alphafadescalar">Alpha fade scalar with EyeDistance (lookat-eye) or 0 disabled</param>
        /// <param name="alphafadepos">Alpha fade distance (Negative for fade in, positive for fade out) or alpha fade value. </param>
        /// <param name="imagepos">Image index into texture, passed to fragement shader</param>
        /// <param name="visible">If visible</param>
        /// <returns></returns>
        static public Matrix4 CreateMatrix(Vector3 worldpos,
                                    Vector3 size,       // Note if Y and Z are zero, then Z is set to same ratio to width as bitmap
                                    Vector3 rotationradians,        // ignored if rotates are on
                                    bool rotatetoviewer = false, bool rotateelevation = false,   // if set, rotationradians not used
                                    float alphafadescalar = 0,
                                    float alphafadepos = 1,
                                    int imagepos = 0,
                                    bool visible = true
            )
        {
            Matrix4 mat = Matrix4.Identity;
            mat = Matrix4.Mult(mat, Matrix4.CreateScale(size));
            if (rotatetoviewer == false && rotationradians.LengthSquared > 0)   // if autorotating, no rotation is allowed. matrix is just scaling/translation
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
        /// Make multiple iewing and Fading control matrices
        /// </summary>
        /// <param name="worldpos">Positions array</param>
        /// <param name="offset">Offset on each position</param>
        /// <param name="size">Size of each</param>
        /// <param name="rotationradians">Rotation of object</param>
        /// <param name="rotatetoviewer">True to rotate in azimuth to viewer</param>
        /// <param name="rotateelevation">True to rotate in elevation to viewer</param>
        /// <param name="alphafadescalar">Alpha fade scalar with EyeDistance (lookat-eye) or 0 disabled</param>
        /// <param name="alphafadepos">Alpha fade distance (Negative for fade in, positive for fade out) or alpha fade value. </param>
        /// <param name="imagepos">Image index into texture, passed to fragement shader</param>
        /// <param name="visible">If visible</param>
        /// <param name="pos">Offset into worldpos array to start at</param>
        /// <param name="length">Number of entries to take from world positions</param>
        /// <returns></returns>
        static public Matrix4[] CreateMatrices(Vector4[] worldpos, Vector3 offset,
                                            Vector3 size, Vector3 rotationradians,
                                            bool rotatetoviewer, bool rotateelevation,
                                            float alphafadescalar = 0,
                                            float alphafadepos = 1,
                                            int imagepos = 0,
                                            bool visible = true,
                                            int pos = 0, int length = -1        // allowing you to pick out a part of the worldpos array
                                            )
        {
            if (length == -1)
                length = worldpos.Length - pos;

            Matrix4[] mats = new Matrix4[length];
            for (int i = 0; i < length; i++)
                mats[i] = CreateMatrix(worldpos[i + pos].Xyz + offset, size, rotationradians, rotatetoviewer, rotateelevation, alphafadescalar, alphafadepos, imagepos, visible);
            return mats;
        }

    }
}
