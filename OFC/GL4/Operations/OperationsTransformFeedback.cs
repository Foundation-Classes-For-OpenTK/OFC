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

namespace GLOFC.GL4
{
    public class GLOperationBeginTransformFeedback : GLOperationsBase       // must be in render queue after shader starts
    {
        public TransformFeedbackPrimitiveType Mode { get; set; }
        public GLTransformFeedbackObject TFObj { get; set; }
        public GLBuffer VaryingBuffer { get; set; }
        public int BindingIndex { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }

        public GLOperationBeginTransformFeedback(TransformFeedbackPrimitiveType mode, GLTransformFeedbackObject obj, GLBuffer buffer, int bindingindex = 0, int offset = 0, int size = -1)
        {
            this.Mode = mode;
            this.TFObj = obj;
            this.VaryingBuffer = buffer;
            this.BindingIndex = bindingindex;
            this.Offset = offset;
            this.Size = size;
        }

        public override void Execute(GLMatrixCalc c)
        {
            TFObj.Bind();
            GLStatics.Check();
            VaryingBuffer.BindTransformFeedback(BindingIndex, TFObj.Id, Offset, Size);
            GLStatics.Check();
            GLTransformFeedbackObject.Begin(Mode);
            GLStatics.Check();
        }
    }

    public class GLOperationEndTransformFeedback : GLOperationsBase       // must be in render queue after object drawn, before shader stops
    {
        public GLTransformFeedbackObject TFObj { get; set; }
        public GLBuffer VaryingBuffer { get; set; }
        public int BindingIndex { get; set; }

        public GLOperationEndTransformFeedback(GLTransformFeedbackObject obj, GLBuffer buffer, int bindingindex = 0)
        {
            this.TFObj = obj;
            this.VaryingBuffer = buffer;
        }

        public override void Execute(GLMatrixCalc c)
        {
            GLTransformFeedbackObject.End();
            GLBuffer.UnbindTransformFeedback(BindingIndex, TFObj.Id);
            GLTransformFeedbackObject.UnBind();
        }
    }



}

