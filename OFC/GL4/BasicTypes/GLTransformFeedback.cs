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
using System;

namespace OFC.GL4
{
    // Holds a transform feedback object and has statics for general TF operations

    public class GLTransformFeedbackObject : IDisposable
    {
        public int Id { get; set; } = -1;

        public GLTransformFeedbackObject()
        {
            Id = GL.GenTransformFeedback();
            GLStatics.Check();
        }

        public void Bind()
        {
            GL.BindTransformFeedback(TransformFeedbackTarget.TransformFeedback, Id);    // bind this
        }

        public static void UnBind()
        {
            GL.BindTransformFeedback(TransformFeedbackTarget.TransformFeedback, 0);     // back to default
        }

        public void Dispose()
        {
            if (Id != -1)
            {
                GL.DeleteTransformFeedback(Id);
                GLStatics.Check();
                Id = -1;
            }
        }

        // use to start/end up the bound transform (either default or this one)

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
