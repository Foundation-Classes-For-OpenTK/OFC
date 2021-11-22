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

using OpenTK;
using System;

namespace GLOFC
{
    public static class GLStaticsMatrix4
    {
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

        // opengl matrixes are layed out as
        //  x.x x.y x.z 0       in row order in memory (locations 0,1,2,3 across)
        //  y.x y.y y.z 0       x, y, z are 3-component vectors describing the matrix coordinate system(local coordinate system within relative to the global coordinate system).
        //  z.x z.y z.z 0
        //  p.x p.y p.z 1       p is a 3-component vector describing the origin of matrix coordinate system.
        //  or in memory order: x.x x.y x.z 0 y.x y.y y.z 0 z.x z.y z.z 0 p.x p.y p.z 1

        static public float[] ToFloatArray(this Matrix4 mat)
        {
            float[] r = new float[] {   mat.Row0.X, mat.Row0.Y, mat.Row0.Z, mat.Row0.W ,        // row major order, 
                                        mat.Row1.X, mat.Row1.Y, mat.Row1.Z, mat.Row1.W ,        // as per https://stackoverflow.com/questions/17717600/confusion-between-c-and-opengl-matrix-order-row-major-vs-column-major
                                        mat.Row2.X, mat.Row2.Y, mat.Row2.Z, mat.Row2.W ,        // which works with vtransform = Matrix.Vector order which is what we use
                                        mat.Row3.X, mat.Row3.Y, mat.Row3.Z, mat.Row3.W };       // Matrix4 holds it in row order
            return r;
        }

        static public Matrix4 CreateMatrix(Vector3 worldpos,  Vector3 size,  Vector3 rotationradians)
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

    }
}
