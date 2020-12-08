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

namespace OFC
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

        static public float[] ToFloatArray(this Matrix4 mat)
        {
            float[] r = new float[] {   mat.Row0.X, mat.Row0.Y, mat.Row0.Z, mat.Row0.W ,        // row major order, 
                                        mat.Row1.X, mat.Row1.Y, mat.Row1.Z, mat.Row1.W ,        // as per https://stackoverflow.com/questions/17717600/confusion-between-c-and-opengl-matrix-order-row-major-vs-column-major
                                        mat.Row2.X, mat.Row2.Y, mat.Row2.Z, mat.Row2.W ,        // which works with vtransform = M.V transforms which is what we uses
                                        mat.Row3.X, mat.Row3.Y, mat.Row3.Z, mat.Row3.W };       // Matrix4 holds it in row order
            return r;
        }
    }
}
