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


using System;
using System.Collections.Generic;
using OpenTK;

namespace OFC.GL4
{
    // implements open GL standards on writing data to a GLBuffer.
    
    public abstract class GLLayoutStandards
    {
        public int CurrentPos { get; protected set; } = 0;
        public IntPtr CurrentPtr { get; protected set; } = IntPtr.Zero;

        public int Length { get; protected set; } = 0;      // 0 means not allocated, otherwise allocated to this size.

        public int Left { get { return Length - CurrentPos; } } // not accounting for alignment
        public int LeftAfterAlign(int size) {  int cpos = (CurrentPos + size- 1) & (~(size - 1));  return Length - cpos; }      // align, then return whats left

        public bool IsAllocated { get { return Length > 0; } }
        public bool NotAllocated { get { return Length == 0; } }

        public bool Std430 { get; set; } = false;               // you can change your mind, in case of debugging etc.

        public List<int> Positions { get; set; } = new List<int>();           // at each alignment using AlignArray, a position is stored (GLBuffer Fills).  Not for ptr map alignments.
        public void AddPosition(int pos) { Positions.Add(pos);  }   // special to add positions in outside of normal Align

        public void ResetPositions()   {  CurrentPos = 0; Positions.Clear(); }

        public List<object> Tags { get; set; }                  // user optional, you can assign tags to positions if required
        public void AddTag(object tag) { if (Tags == null) Tags = new List<object>(); Tags.Add(tag); }

        public const int Vec4size = 4 * sizeof(float);
        public const int Vec3size = Vec4size;
        public const int Vec2size = 2 * sizeof(float);
        public const int Mat4size = 4 * 4 * sizeof(float);

        // std140 alignment: scalars are N, 2Vec = 2N, 3Vec = 4N, 4Vec = 4N. 
        //                   array alignment is vec4 for all, stride vec4.  
        //                   mat4 alignment is vec4
        // std430 alignment: scalars are N, 2Vec = 2N, 3Vec = 4N, 4Vec = 4N. 
        //                   array alignment is same as scalar, stride is as per scalar

        public GLLayoutStandards(bool std430 = false)
        {
            CurrentPos = 0;
            Length = 0;
            Std430 = std430;
        }

        protected int AlignArray(int elementsize, int datasize)              // align a vector of element size, move on by datasize
        {
            int arrayalign = Std430 ? elementsize : Vec4size;

            if (arrayalign > 1)
                CurrentPos = (CurrentPos + arrayalign - 1) & (~(arrayalign - 1));

            int pos = CurrentPos;
            CurrentPos += datasize;
            Positions.Add(pos);
            System.Diagnostics.Debug.Assert(CurrentPos <= Length);
            return pos;
        }

        protected IntPtr AlignScalarPtr(int scalarsize)                     // align to scalar size, move the ptr on to align to scalar size
        {
            if (scalarsize > 1)
            {
                int newoffset = (CurrentPos + scalarsize - 1) & (~(scalarsize - 1));
                CurrentPtr += newoffset - CurrentPos;
                CurrentPos = newoffset;
            }

            IntPtr r = CurrentPtr;
            CurrentPtr += scalarsize;
            CurrentPos += scalarsize;
            System.Diagnostics.Debug.Assert(CurrentPos <= Length);
            return r;
        }

        protected Tuple<IntPtr,int> AlignArrayPtr(int elementsize, int count)    // align to elementsize, move data on by count elements
        {
            int arrayalign = Std430 ? elementsize : Vec4size;

            if (arrayalign > 1)
            {
                int newoffset = (CurrentPos + arrayalign - 1) & (~(arrayalign - 1));
                CurrentPtr += newoffset - CurrentPos;
                CurrentPos = newoffset;
            }

            IntPtr r = CurrentPtr;
            CurrentPtr += arrayalign * count;               // arrays are loosely packed in std140, with vec4 between them, so move on by arrayalign
            CurrentPos += arrayalign * count;
            System.Diagnostics.Debug.Assert(CurrentPos <= Length);
            return new Tuple<IntPtr, int>(r, arrayalign);
        }
    }

}