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


using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4.Operations
{
    /// <summary>
    /// Operations for transform feedback
    /// </summary>
    public class GLOperationTransformFeedback : GLOperationsBase       // must be in render queue after shader starts
    {
        /// <summary> Transform feedback Primitive type </summary>
        public TransformFeedbackPrimitiveType PrimitiveType { get; set; }
        /// <summary> Transform feedback instance </summary>
        public GLTransformFeedback TransformFeedback { get; set; }
        /// <summary> Varying buffers, one per bindingindex </summary>
        public GLBuffer[] VaryingBuffers { get; set; }
        /// <summary> List of offsets into the buffer to map. Must be set if size if not null</summary>
        public int[] Offsets { get; set; }
        /// <summary> List of length of buffer area to use. If null, means all of buffer </summary>
        public int[] Sizes { get; set; }

        /// <summary>
        /// Constructor, sets up transform parameters. 
        /// See <href>https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glBeginTransformFeedback.xhtml</href>
        /// </summary>
        /// <param name="primitivetype">Primitive type for transform feedback (Points/Lines/Triangles) </param>
        /// <param name="transformfeedback">The GLTransformfeedback instance, previously created. Created externally as it does not have to be created per render</param>
        /// <param name="buffers">An array of buffers to receive the transform feedback into, starting at index 0. Must be created and allocated with DynamicCopy. See <href>https://www.khronos.org/opengl/wiki/Transform_Feedback</href> for details on how to use multiple buffers</param>
        /// <param name="offset">Offset into buffer if size != -1</param>
        /// <param name="size">Buffer area allocated. If size == -1, all of buffer</param>
        public GLOperationTransformFeedback(TransformFeedbackPrimitiveType primitivetype, GLTransformFeedback transformfeedback, 
                                                 GLBuffer[] buffers, int[] offset = null, int[] size = null)
        {
            this.PrimitiveType = primitivetype;
            this.TransformFeedback = transformfeedback;
            this.Id = TransformFeedback.Id;     // mirror the ID, we are the same
            this.VaryingBuffers = buffers;
            this.Offsets = offset;
            this.Sizes = size;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            TransformFeedback.Bind();  // bind this transformfeedback to target transform feedback
            GLStatics.Check();

            // bind these buffer at offset/set and binding index starting at 0
            for (int i = 0; i < VaryingBuffers.Length; i++)
            {
                //System.Diagnostics.Debug.WriteLine($"TF {TransformFeedback.Id} bp {i} to buf {VaryingBuffers[i].Id}");
                VaryingBuffers[i].BindTransformFeedback(i, TransformFeedback.Id, Offsets == null ? 0 : Offsets[i], Sizes == null ? 0 : Sizes[i]);
            }

            GLStatics.Check();

            GLTransformFeedback.Begin(PrimitiveType);       // and start
            GLStatics.Check();
        }
    }

    /// <summary>
    /// Object to end transform feedback set up by GLOperationTransformFeedback
    /// </summary>
    public class GLOperationEndTransformFeedback : GLOperationsBase       // must be in render queue after object drawn, before shader stops
    {
        /// <summary> Transform feedback operation </summary>
        public GLOperationTransformFeedback TransformFeedbackOperation { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="transformfeedback">The GLTransformfeedback instance, previously created</param>
        public GLOperationEndTransformFeedback(GLOperationTransformFeedback transformfeedback)
        {
            this.TransformFeedbackOperation = transformfeedback;
        }

        /// <summary> Called by render list and executes the operation </summary>
        public override void Execute(GLMatrixCalc c)
        {
            GLTransformFeedback.End();
            for (int i = 0; i < TransformFeedbackOperation.VaryingBuffers.Length; i++)
            {
                //System.Diagnostics.Debug.WriteLine($"TF {TransformFeedbackOperation.Id} bp {i}");
                GLBuffer.UnbindTransformFeedback(i, TransformFeedbackOperation.Id);
            }
            GLStatics.Check();
            GLTransformFeedback.UnBind();
            GLStatics.Check();
        }
    }



}

