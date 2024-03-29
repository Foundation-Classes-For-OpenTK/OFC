﻿ /*
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

namespace GLOFC.GL4
{
    /// <summary>
    /// Holds a transform feedback object and has statics for general TF operations
    /// </summary>

    public class GLTransformFeedback : IDisposable
    {
        /// <summary> GL ID </summary>
        public int Id { get; set; } = -1;

        /// <summary> Construct a Transform Feedback object </summary>
        public GLTransformFeedback()
        {
            Id = GL.GenTransformFeedback();
            GLStatics.RegisterAllocation(typeof(GLTransformFeedback));
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> Bind this object to the transform feeback binding point
        /// Note you will also need to make a buffer, and bind it using GLBuffer.BindTransformFeedback so there is a buffer ready to receive the feedback
        /// </summary>
        public void Bind()
        {
            GL.BindTransformFeedback(TransformFeedbackTarget.TransformFeedback, Id);    // bind this
        }

        /// <summary> Unbind this object </summary>
        public static void UnBind()
        {
            GL.BindTransformFeedback(TransformFeedbackTarget.TransformFeedback, 0);     // back to default
        }

        /// <summary> Dispose of the object </summary>
        public void Dispose()
        {
            if (Id != -1)
            {
                GL.DeleteTransformFeedback(Id);
                GLStatics.RegisterDeallocation(typeof(GLTransformFeedback));
                System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
                Id = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }

        // use to start/end up the bound transform (either default or this one)

        /// <summary> Start feedback on the primitive type </summary>
        public static void Begin(TransformFeedbackPrimitiveType t)
        {
            GL.BeginTransformFeedback(t);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> End feedback </summary>
        public static void End()
        {
            GL.EndTransformFeedback();
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }
        /// <summary> Pause feedback </summary>
        public static void Pause()
        {
            GL.PauseTransformFeedback();
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }
        /// <summary> Resume from pause </summary>
        public static void Resume()
        {
            GL.ResumeTransformFeedback();
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

    }
}
