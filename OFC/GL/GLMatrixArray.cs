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
using System;

// no xml needed 
#pragma warning disable 1591

namespace GLOFC
{
    // idea is to hold this as a float array
    // not sure if useful, keep for now.
    public class GLMatrixArray
    {
        public GLMatrixArray(int number)
        {
            MatrixArray = new float[number * 16];
            Count = number;
        }

        public Matrix4 this[int i] { get 
            {
                int pos = i * matrixsize;
                return new Matrix4( MatrixArray[pos + 0 * 4 + 0], MatrixArray[pos + 0 * 4 + 1], MatrixArray[pos + 0 * 4 + 2], MatrixArray[pos + 0 * 4 + 3],
                                    MatrixArray[pos + 1 * 4 + 0], MatrixArray[pos + 1 * 4 + 1], MatrixArray[pos + 1 * 4 + 2], MatrixArray[pos + 1 * 4 + 3],
                                    MatrixArray[pos + 2 * 4 + 0], MatrixArray[pos + 2 * 4 + 1], MatrixArray[pos + 2 * 4 + 2], MatrixArray[pos + 2 * 4 + 3],
                                    MatrixArray[pos + 3 * 4 + 0], MatrixArray[pos + 3 * 4 + 1], MatrixArray[pos + 3 * 4 + 2], MatrixArray[pos + 3 * 4 + 3]);
            } set 
            {
                int pos = i * matrixsize;
                MatrixArray[pos + 0 * 4 + 0] = value.M11; MatrixArray[pos + 0 * 4 + 1] = value.M12; MatrixArray[pos + 0 * 4 + 2] = value.M13; MatrixArray[pos + 0 * 4 + 3] = value.M14;
                MatrixArray[pos + 1 * 4 + 0] = value.M21;MatrixArray[pos + 1 * 4 + 1] = value.M22;MatrixArray[pos + 1 * 4 + 2] = value.M23;MatrixArray[pos + 1 * 4 + 3] = value.M24;
                MatrixArray[pos + 2 * 4 + 0] = value.M31;MatrixArray[pos + 2 * 4 + 1] = value.M32;MatrixArray[pos + 2 * 4 + 2] = value.M33;MatrixArray[pos + 2 * 4 + 3] = value.M34;
                MatrixArray[pos + 3 * 4 + 0] = value.M41;MatrixArray[pos + 3 * 4 + 1] = value.M42;MatrixArray[pos + 3 * 4 + 2] = value.M43;MatrixArray[pos + 3 * 4 + 3] = value.M44;
            }
        }

        public float[] MatrixArray { get; private set; }
        public int Count { get; private set; }

        public void Zero()
        {
            Array.Clear(MatrixArray, 0, MatrixArray.Length);
        }
        public void Zero(int start, int length)
        {
            Array.Clear(MatrixArray, start * matrixsize, length * matrixsize);
        }
        public void Identity()
        {
            CopyIn(identity,0,Count);
        }
        public void Identity(int start, int length = 1)
        {
            CopyIn(identity, start, length);
        }

        public void CreateScale(Vector3 scalev, int start, int length = 1)
        {
            scale[0 * 4 + 0] = scalev.X;
            scale[1 * 4 + 1] = scalev.X;
            scale[2 * 4 + 2] = scalev.X;
            CopyIn(scale, start, length);
        }

        public void CreateRotationX(float angle, int start, int length = 1)
        {
            float cos = (float)System.Math.Cos(angle);
            float sin = (float)System.Math.Sin(angle);
            rotx[1 * 4 + 1] = cos;
            rotx[1 * 4 + 2] = sin;
            rotx[2 * 4 + 1] = -sin;
            rotx[2 * 4 + 2] = cos;
            CopyIn(rotx, start, length);
        }
        public void CreateRotationY(float angle, int start, int length = 1)
        {
            float cos = (float)System.Math.Cos(angle);
            float sin = (float)System.Math.Sin(angle);
            roty[0 * 4 + 0] = cos;
            roty[0 * 4 + 2] = -sin;
            roty[2 * 4 + 0] = sin;
            roty[2 * 4 + 2] = cos;
            CopyIn(roty, start, length);
        }

        public void CreateRotationZ(float angle, int start, int length = 1)
        {
            float cos = (float)System.Math.Cos(angle);
            float sin = (float)System.Math.Sin(angle);
            rotz[0 * 4 + 0] = cos;
            rotz[0 * 4 + 1] = sin;
            rotz[1 * 4 + 0] = -sin;
            rotz[1 * 4 + 1] = cos;
            CopyIn(rotz, start, length);
        }

        public void Multiply(int start, Matrix4 right, int length = 1)
        {
            float rM11 = right.Row0.X, rM12 = right.Row0.Y, rM13 = right.Row0.Z, rM14 = right.Row0.W,
                 rM21 = right.Row1.X, rM22 = right.Row1.Y, rM23 = right.Row1.Z, rM24 = right.Row1.W,
                 rM31 = right.Row2.X, rM32 = right.Row2.Y, rM33 = right.Row2.Z, rM34 = right.Row2.W,
                 rM41 = right.Row3.X, rM42 = right.Row3.Y, rM43 = right.Row3.Z, rM44 = right.Row3.W;

            while (length-- > 0)
            {
                int pos = start++ * matrixsize;
                float lM11 = MatrixArray[pos + 4 * 0 + 0], lM12 = MatrixArray[pos + 4 * 0 + 1], lM13 = MatrixArray[pos + 4 * 0 + 2], lM14 = MatrixArray[pos + 4 * 0 + 3],
                     lM21 = MatrixArray[pos + 4 * 1 + 0], lM22 = MatrixArray[pos + 4 * 1 + 1], lM23 = MatrixArray[pos + 4 * 1 + 2], lM24 = MatrixArray[pos + 4 * 1 + 3],
                     lM31 = MatrixArray[pos + 4 * 2 + 0], lM32 = MatrixArray[pos + 4 * 2 + 1], lM33 = MatrixArray[pos + 4 * 2 + 2], lM34 = MatrixArray[pos + 4 * 2 + 3],
                     lM41 = MatrixArray[pos + 4 * 3 + 0], lM42 = MatrixArray[pos + 4 * 3 + 1], lM43 = MatrixArray[pos + 4 * 3 + 2], lM44 = MatrixArray[pos + 4 * 3 + 3];

                MatrixArray[pos + 4 * 0 + 0] = (((lM11 * rM11) + (lM12 * rM21)) + (lM13 * rM31)) + (lM14 * rM41);
                MatrixArray[pos + 4 * 0 + 1] = (((lM11 * rM12) + (lM12 * rM22)) + (lM13 * rM32)) + (lM14 * rM42);
                MatrixArray[pos + 4 * 0 + 2] = (((lM11 * rM13) + (lM12 * rM23)) + (lM13 * rM33)) + (lM14 * rM43);
                MatrixArray[pos + 4 * 0 + 3] = (((lM11 * rM14) + (lM12 * rM24)) + (lM13 * rM34)) + (lM14 * rM44);
                MatrixArray[pos + 4 * 1 + 0] = (((lM21 * rM11) + (lM22 * rM21)) + (lM23 * rM31)) + (lM24 * rM41);
                MatrixArray[pos + 4 * 1 + 1] = (((lM21 * rM12) + (lM22 * rM22)) + (lM23 * rM32)) + (lM24 * rM42);
                MatrixArray[pos + 4 * 1 + 2] = (((lM21 * rM13) + (lM22 * rM23)) + (lM23 * rM33)) + (lM24 * rM43);
                MatrixArray[pos + 4 * 1 + 3] = (((lM21 * rM14) + (lM22 * rM24)) + (lM23 * rM34)) + (lM24 * rM44);
                MatrixArray[pos + 4 * 2 + 0] = (((lM31 * rM11) + (lM32 * rM21)) + (lM33 * rM31)) + (lM34 * rM41);
                MatrixArray[pos + 4 * 2 + 1] = (((lM31 * rM12) + (lM32 * rM22)) + (lM33 * rM32)) + (lM34 * rM42);
                MatrixArray[pos + 4 * 2 + 2] = (((lM31 * rM13) + (lM32 * rM23)) + (lM33 * rM33)) + (lM34 * rM43);
                MatrixArray[pos + 4 * 2 + 3] = (((lM31 * rM14) + (lM32 * rM24)) + (lM33 * rM34)) + (lM34 * rM44);
                MatrixArray[pos + 4 * 3 + 0] = (((lM41 * rM11) + (lM42 * rM21)) + (lM43 * rM31)) + (lM44 * rM41);
                MatrixArray[pos + 4 * 3 + 1] = (((lM41 * rM12) + (lM42 * rM22)) + (lM43 * rM32)) + (lM44 * rM42);
                MatrixArray[pos + 4 * 3 + 2] = (((lM41 * rM13) + (lM42 * rM23)) + (lM43 * rM33)) + (lM44 * rM43);
                MatrixArray[pos + 4 * 3 + 3] = (((lM41 * rM14) + (lM42 * rM24)) + (lM43 * rM34)) + (lM44 * rM44);
            }
        }

        public void Multiply(int left, int right, int result)
        {
            int pos = left * matrixsize;
            float lM11 = MatrixArray[pos + 4 * 0 + 0], lM12 = MatrixArray[pos + 4 * 0 + 1], lM13 = MatrixArray[pos + 4 * 0 + 2], lM14 = MatrixArray[pos + 4 * 0 + 3],
                 lM21 = MatrixArray[pos + 4 * 1 + 0], lM22 = MatrixArray[pos + 4 * 1 + 1], lM23 = MatrixArray[pos + 4 * 1 + 2], lM24 = MatrixArray[pos + 4 * 1 + 3],
                 lM31 = MatrixArray[pos + 4 * 2 + 0], lM32 = MatrixArray[pos + 4 * 2 + 1], lM33 = MatrixArray[pos + 4 * 2 + 2], lM34 = MatrixArray[pos + 4 * 2 + 3],
                 lM41 = MatrixArray[pos + 4 * 3 + 0], lM42 = MatrixArray[pos + 4 * 3 + 1], lM43 = MatrixArray[pos + 4 * 3 + 2], lM44 = MatrixArray[pos + 4 * 3 + 3];

            pos = right * matrixsize;
            float rM11 = MatrixArray[pos + 4 * 0 + 0], rM12 = MatrixArray[pos + 4 * 0 + 1], rM13 = MatrixArray[pos + 4 * 0 + 2], rM14 = MatrixArray[pos + 4 * 0 + 3],
                 rM21 = MatrixArray[pos + 4 * 1 + 0], rM22 = MatrixArray[pos + 4 * 1 + 1], rM23 = MatrixArray[pos + 4 * 1 + 2], rM24 = MatrixArray[pos + 4 * 1 + 3],
                 rM31 = MatrixArray[pos + 4 * 2 + 0], rM32 = MatrixArray[pos + 4 * 2 + 1], rM33 = MatrixArray[pos + 4 * 2 + 2], rM34 = MatrixArray[pos + 4 * 2 + 3],
                 rM41 = MatrixArray[pos + 4 * 3 + 0], rM42 = MatrixArray[pos + 4 * 3 + 1], rM43 = MatrixArray[pos + 4 * 3 + 2], rM44 = MatrixArray[pos + 4 * 3 + 3];

            pos = result * matrixsize;
            MatrixArray[pos + 4 * 0 + 0] = (((lM11 * rM11) + (lM12 * rM21)) + (lM13 * rM31)) + (lM14 * rM41);
            MatrixArray[pos + 4 * 0 + 1] = (((lM11 * rM12) + (lM12 * rM22)) + (lM13 * rM32)) + (lM14 * rM42);
            MatrixArray[pos + 4 * 0 + 2] = (((lM11 * rM13) + (lM12 * rM23)) + (lM13 * rM33)) + (lM14 * rM43);
            MatrixArray[pos + 4 * 0 + 3] = (((lM11 * rM14) + (lM12 * rM24)) + (lM13 * rM34)) + (lM14 * rM44);
            MatrixArray[pos + 4 * 1 + 0] = (((lM21 * rM11) + (lM22 * rM21)) + (lM23 * rM31)) + (lM24 * rM41);
            MatrixArray[pos + 4 * 1 + 1] = (((lM21 * rM12) + (lM22 * rM22)) + (lM23 * rM32)) + (lM24 * rM42);
            MatrixArray[pos + 4 * 1 + 2] = (((lM21 * rM13) + (lM22 * rM23)) + (lM23 * rM33)) + (lM24 * rM43);
            MatrixArray[pos + 4 * 1 + 3] = (((lM21 * rM14) + (lM22 * rM24)) + (lM23 * rM34)) + (lM24 * rM44);
            MatrixArray[pos + 4 * 2 + 0] = (((lM31 * rM11) + (lM32 * rM21)) + (lM33 * rM31)) + (lM34 * rM41);
            MatrixArray[pos + 4 * 2 + 1] = (((lM31 * rM12) + (lM32 * rM22)) + (lM33 * rM32)) + (lM34 * rM42);
            MatrixArray[pos + 4 * 2 + 2] = (((lM31 * rM13) + (lM32 * rM23)) + (lM33 * rM33)) + (lM34 * rM43);
            MatrixArray[pos + 4 * 2 + 3] = (((lM31 * rM14) + (lM32 * rM24)) + (lM33 * rM34)) + (lM34 * rM44);
            MatrixArray[pos + 4 * 3 + 0] = (((lM41 * rM11) + (lM42 * rM21)) + (lM43 * rM31)) + (lM44 * rM41);
            MatrixArray[pos + 4 * 3 + 1] = (((lM41 * rM12) + (lM42 * rM22)) + (lM43 * rM32)) + (lM44 * rM42);
            MatrixArray[pos + 4 * 3 + 2] = (((lM41 * rM13) + (lM42 * rM23)) + (lM43 * rM33)) + (lM44 * rM43);
            MatrixArray[pos + 4 * 3 + 3] = (((lM41 * rM14) + (lM42 * rM24)) + (lM43 * rM34)) + (lM44 * rM44);
        }

        private void CopyIn(float[] input, int start , int length )
        {
            while (length-- > 0)
            {
                Array.Copy(input, 0, MatrixArray, start++ * matrixsize, matrixsize);
            }
        }


        private const int matrixsize = 16;
        private float[] scale = new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        private float[] rotx = new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        private float[] roty = new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        private float[] rotz = new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        private float[] identity = new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };

    }
}
