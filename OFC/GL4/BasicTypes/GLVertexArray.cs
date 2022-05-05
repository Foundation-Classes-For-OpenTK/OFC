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
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace GLOFC.GL4
{
    /// <summary>
    /// Vertex Array indicate binding of buffers to draws. Mapping is usually dealt with by GLRenderableItem static setup functions.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Id {Id}")]
    public class GLVertexArray : IGLVertexArray
    {
        /// <summary> GL ID</summary>
        public int Id { get; private set; } = -1;

        private IntPtr context;

        /// <summary> Construct a vertex array </summary>
        public GLVertexArray()
        {
            Id = GL.GenVertexArray();       
            context = GLStatics.GetContext();
            GLStatics.RegisterAllocation(typeof(GLVertexArray));
        }

        /// <summary> Bind vertex array to binding point ready for draw </summary>
        public virtual void Bind()
        {
            System.Diagnostics.Debug.Assert(context == GLStatics.GetContext(), "Context incorrect");     // safety
            GL.BindVertexArray(Id);                  // Bind vertex
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        }

        /// <summary> Dispose of the vertex array </summary>
        public virtual void Dispose()
        {
            if (Id != -1)
            {
                GL.DeleteVertexArray(Id);
                GLStatics.RegisterDeallocation(typeof(GLVertexArray));
                Id = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }

        // floats are being bound

        /// <summary>
        /// Set up a mapping beterrn the binding index, attribindex and indicate components, type, offset and divisor
        /// </summary>
        /// <param name="bindingindex">Binding index to map</param>
        /// <param name="attribindex">Attribute to use in GLSL to access this data</param>
        /// <param name="components">Number of components per</param>
        /// <param name="vat">Type and size of component (Byte,Int,Float etc)</param>
        /// <param name="reloffset">The offset, measured in basic machine units of the first element relative to the start of the vertex buffer binding this attribute fetches from.</param>
        /// <param name="divisor">For instancing, set to >0 for instance dividing of the data</param>
        public void Attribute(int bindingindex, int attribindex, int components, VertexAttribType vat, int reloffset = 0, int divisor = -1)
        {
            GL.VertexArrayAttribFormat(
                Id,
                attribindex,            // attribute index
                components,             // no of components per attribute, 1-4
                vat,                    // type
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                reloffset);             // relative offset, first item

            // ORDER Important, .. found that out

            if (divisor >= 0)            // normally use binding divisor..
                GL.VertexAttribDivisor(attribindex, divisor);

            GL.VertexArrayAttribBinding(Id, attribindex, bindingindex);     // bind atrib to binding    - do this after attrib format
            GL.EnableVertexArrayAttrib(Id, attribindex);

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
           // System.Diagnostics.Debug.WriteLine("ATTR " + attribindex + " to " + bindingindex + " Components " + components + " +" + reloffset + " divisor " + divisor);
        }

        // Integers are being bound

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bindingindex">Binding index to map</param>
        /// <param name="attribindex">Attribute to use in GLSL to access this data</param>
        /// <param name="components">Number of components per</param>
        /// <param name="vat">Type and size of component (Byte,Int,Float etc)</param>
        /// <param name="reloffset">The offset, measured in basic machine units of the first element relative to the start of the vertex buffer binding this attribute fetches from.</param>
        /// <param name="divisor">For instancing, set to >0 for instance dividing of the data</param>
        public void AttributeI(int bindingindex, int attribindex, int components, VertexAttribType vat, int reloffset = 0, int divisor = -1)
        {
            GL.VertexArrayAttribIFormat(
                Id,
                attribindex,            // attribute index
                components,             // no of attribs
                vat,                    // type
                reloffset);             // relative offset, first item

            if (divisor >= 0)            // normally use binding divisor..
                GL.VertexAttribDivisor(attribindex, divisor);               // set up attribute divisor - doing this after doing the binding divisor screws things up

            GL.VertexArrayAttribBinding(Id, attribindex, bindingindex);     // bind atrib to binding 
            GL.EnableVertexArrayAttrib(Id, attribindex);                    // enable attrib

            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
           // System.Diagnostics.Debug.WriteLine("ATTRI " + attribindex + " to " + bindingindex + " Components " + components + " +" + reloffset + " divisor " + divisor);
        }

        /// <summary>
        /// Set up mapping for a matrix4 
        /// </summary>
        /// <param name="bindingindex">Binding index to map</param>
        /// <param name="attribstart">Attribute to use in GLSL to access this data (will by +0 to +3)</param>
        /// <param name="divisor">For instancing, set to >0 for instance dividing of the data</param>
        public void MatrixAttribute(int bindingindex, int attribstart, int divisor = 0)      // bind a matrix..
        {
            for (int i = 0; i < 4; i++)
                Attribute(bindingindex, attribstart + i, 4, VertexAttribType.Float, 16*i, divisor);
        }
    }
}
