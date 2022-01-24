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

using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    /// <summary>
    /// Matrix uniform block stored into uniform buffer 0 (fixed). The uniform block is formatted as follows:
    /// </summary>

    public class GLMatrixCalcUniformBlock : GLUniformBlock 
    {
        /// <summary> Size of matrix block </summary>
        public int MatrixCalcUse { get; } = Mat4size * 3 + Vec4size * 2 + sizeof(float) * 4 + Mat4size;

        /// <summary> Binding point </summary>
        public const int BindingPoint = 0;// 0 is the fixed binding block for matrixcal

        /// <summary>
        /// Construct and make the block of this layout:
        /// layout(std140, binding=0) uniform MatrixCalc
        /// {
        /// mat4 ProjectionModelMatrix;
        /// mat4 ProjectionMatrix;
        /// mat4 ModelMatrix;
        /// vec4 TargetPosition;		// vertex position, before ModelMatrix
        /// vec4 EyePosition;			// vertex position, before ModelMatrix
        /// float EyeDistance;          // between eye and target
        /// mat4 ScreenMatrix;			// for co-ordinate transforms between screen coords and display coords
        /// } mc;
        /// 
        /// Include in your project by #include UniformStorageBlocks.matrixcalc.glsl
        /// </summary>
        public GLMatrixCalcUniformBlock() : base(BindingPoint)         
        {
        }

        /// <summary>
        /// Minimal set - ProjectionModelMatrix only
        /// </summary>
        /// <param name="matrixcalc">The current matrix calc to store information from </param>
        public void SetMinimal(GLMatrixCalc matrixcalc)
        {
            if (lastmccount != matrixcalc.CountMatrixCalcs)
            {
                if (NotAllocated)
                    AllocateBytes(MatrixCalcUse, BufferUsageHint.DynamicCopy);

                StartWrite(0, Length);        // the whole schebang
                Write(matrixcalc.ProjectionModelMatrix);
                StopReadWrite();                                // and complete..
                lastmccount = matrixcalc.CountMatrixCalcs;
            }
        }

        /// <summary>
        /// Normal set - All up to EyeDistance inclusive
        /// </summary>
        /// <param name="matrixcalc">The current matrix calc to store information from </param>
        public void Set(GLMatrixCalc matrixcalc)
        {
            if (lastmccount != matrixcalc.CountMatrixCalcs)
            {
                if (NotAllocated)
                    AllocateBytes(MatrixCalcUse, BufferUsageHint.DynamicCopy);

                StartWrite(0, Length);        // the whole schebang
                Write(matrixcalc.ProjectionModelMatrix);     // 0- 63
                Write(matrixcalc.ProjectionMatrix);          // 64-127
                Write(matrixcalc.ModelMatrix);               // 128-191
                Write(matrixcalc.LookAt, 0);         // 192-207
                Write(matrixcalc.EyePosition, 0);            // 208-223
                Write(matrixcalc.EyeDistance);               // 224-239
                StopReadWrite();                                // and complete..
                lastmccount = matrixcalc.CountMatrixCalcs;
            }
        }

        /// <summary>
        /// All set - All fields
        /// </summary>
        /// <param name="matrixcalc">The current matrix calc to store information from</param>
        public void SetFull(GLMatrixCalc matrixcalc) 
        {
            if (lastmccount != matrixcalc.CountMatrixCalcs)
            {
                if (NotAllocated)
                    AllocateBytes(MatrixCalcUse, BufferUsageHint.DynamicCopy);
                StartWrite(0, Length);              // the whole schebang
                Write(matrixcalc.ProjectionModelMatrix);     //0, 64 long
                Write(matrixcalc.ProjectionMatrix);          //64, 64 long
                Write(matrixcalc.ModelMatrix);               //128, 64 long
                Write(matrixcalc.LookAt, 0);         //192, vec4, 16 long
                Write(matrixcalc.EyePosition, 0);            // 208, vec4, 16 long
                Write(matrixcalc.EyeDistance);               // 224, float, 4 long
                Write(matrixcalc.MatrixScreenCoordToClipSpace());                // 240-303, into the project model matrix slot, used for text
                StopReadWrite();   // and complete..
                lastmccount = matrixcalc.CountMatrixCalcs;
            }
        }

        private int lastmccount = int.MinValue;
    }
}

