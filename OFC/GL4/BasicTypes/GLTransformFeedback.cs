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
using System.Drawing;

namespace OFC.GL4
{
    // Simple functions to move GL into OFC namespace

    public static class GLTransformFeedback
    {
        public static void BeginTransformFeedback(TransformFeedbackPrimitiveType t)
        {
            GL.BeginTransformFeedback(t);
        }

        public static void EndTransformFeedback()
        {
            GL.EndTransformFeedback();
        }
        public static void PauseTransformFeedback()
        {
            GL.PauseTransformFeedback();
        }
        public static void ResumeTransformFeedback()
        {
            GL.ResumeTransformFeedback();
        }
    }
}
