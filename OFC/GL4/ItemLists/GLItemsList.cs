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

using GLOFC.GL4.Shaders;
using GLOFC.Utils;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GLOFC.GL4
{
    // This is a memory class in which you can register GL type items and it will manage them
    // items have names to find them again

    public class GLItemsList : IDisposable
    {
        public static bool StackTrace { get; set; } = false;        // global set for stack trace disposal tracking

        // Get existing items
        public bool Contains(string name )
        {
            return items.ContainsKey(name);
        }

        public GLTextureBase Tex(string name)
        {
            return (GLTextureBase)items[name];
        }

        public IGLProgramShader Shader(string name)
        {
            return (IGLProgramShader)items[name];
        }

        public IGLPipelineComponentShader PLShader(string name)
        {
            return (IGLPipelineComponentShader)items[name];
        }

        public GLVertexArray VA(string name)
        {
            return (GLVertexArray)items[name];
        }

        public GLUniformBlock UB(string name)
        {
            return (GLUniformBlock)items[name];
        }

        public GLStorageBlock SB(string name)
        {
            return (GLStorageBlock)items[name];
        }

        public GLAtomicBlock AB(string name)
        {
            return (GLAtomicBlock)items[name];
        }

        public GLBuffer B(string name)
        {
            return (GLBuffer)items[name];
        }

        public Bitmap Bitmap(string name)
        {
            return (Bitmap)items[name];
        }

        public GLBuffer LastBuffer(int c = 1)
        {
            return (GLBuffer)items.Last(typeof(GLBuffer), c);
        }

        public T Last<T>(int c = 1) where T : class
        {
            return (T)items.Last(typeof(T), c);
        }

        public T Get<T>(string name)
        {
            return (T)items[name];
        }

        // Add existing items. Name can be null and will get a unique name

        public IGLTexture Add(IGLTexture disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public IGLProgramShader Add(IGLProgramShader disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public IGLPipelineComponentShader Add(IGLPipelineComponentShader disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public GLVertexArray Add(GLVertexArray disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public GLUniformBlock Add(GLUniformBlock disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public GLStorageBlock Add(GLStorageBlock disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public GLAtomicBlock Add( GLAtomicBlock disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public GLBuffer Add(GLBuffer disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public Bitmap Add(Bitmap disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public GLBitmaps Add(GLBitmaps disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
            return disp;
        }

        public void Add(IDisposable disp, string name = null)
        {
            System.Diagnostics.Debug.Assert(!items.ContainsValue(disp));
            items.Add(EnsureName(name), disp);
        }

        // New items

        public GLVertexArray NewArray(string name = null)
        {
            GLVertexArray b = new GLVertexArray();
            items[EnsureName(name)] = b;
            return b;
        }

        public GLUniformBlock NewUniformBlock(int bindingindex, string name = null)
        {
            GLUniformBlock sb = new GLUniformBlock(bindingindex);
            items[EnsureName(name)] = sb;
            return sb;
        }

        public GLStorageBlock NewStorageBlock(int bindingindex, bool std430 = false, string name = null)
        {
            GLStorageBlock sb = new GLStorageBlock(bindingindex, std430);
            items[EnsureName(name)] = sb;
            return sb;
        }

        public GLAtomicBlock NewAtomicBlock(int bindingindex, string name = null)
        {
            GLAtomicBlock sb = new GLAtomicBlock(bindingindex);
            items[EnsureName(name)] = sb;
            return sb;
        }

        // a buffer returned
        public GLBuffer NewBuffer(bool std430 = true, string name = null)
        {
            GLBuffer b = new GLBuffer(std430);        
            items[EnsureName(name)] = b;
            return b;
        }
        public GLBuffer NewBuffer(int size, bool std430 = false, BufferUsageHint bh = BufferUsageHint.StaticDraw, string name = null)
        {
            GLBuffer b = new GLBuffer(size,std430,bh);        
            items[EnsureName(name)] = b;
            return b;
        }
        public GLVertexArray NewVertexArray(string name = null)
        {
            var b = new GLVertexArray();        // a standard buffer returned is not for uniforms do not suffer the std140 restrictions
            items[EnsureName(name)] = b;
            return b;
        }

        public GLBindlessTextureHandleBlock NewBindlessTextureHandleBlock(int bindingpoint, string name = null)
        {
            var b = new GLBindlessTextureHandleBlock(bindingpoint);
            items[EnsureName(name)] = b;
            return b;
        }
        public GLBindlessTextureHandleBlock NewBindlessTextureHandleBlock(int bindingpoint, IGLTexture[] textures, string name = null)
        {
            var b = new GLBindlessTextureHandleBlock(bindingpoint,textures);
            items[EnsureName(name)] = b;
            return b;
        }
        public GLShaderPipeline NewShaderPipeline(string name, params Object[] cnst)
        {
            GLShaderPipeline s = (GLShaderPipeline)Activator.CreateInstance(typeof(GLShaderPipeline), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        public GLShaderPipeline NewShaderPipeline(string name, IGLPipelineComponentShader vertex, IGLPipelineComponentShader fragment)
        {
            GLShaderPipeline s = new GLShaderPipeline(vertex, fragment);
            items[EnsureName(name)] = s;
            return s;
        }

        public GLShaderStandard NewShaderStandard(string name, params Object[] cnst)
        {
            GLShaderStandard s = (GLShaderStandard)Activator.CreateInstance(typeof(GLShaderStandard), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }
        public GLShaderCompute NewShaderCompute(string name, params Object[] cnst)
        {
            var s = (GLShaderCompute)Activator.CreateInstance(typeof(GLShaderCompute), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        public GLTexture1D NewTexture1D(string name, params Object[] cnst)
        {
            var s = (GLTexture1D)Activator.CreateInstance(typeof(GLTexture1D), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }
        public GLTexture2D NewTexture2D(string name)
        {
            var s = new GLTexture2D();
            items[EnsureName(name)] = s;
            return s;
        }
        public GLTexture2D NewTexture2D(string name, Bitmap bmp, SizedInternalFormat internalformat, int bitmipmaplevel = 1,
                            int genmipmaplevel = 1, bool ownbitmaps = false, ContentAlignment alignment = ContentAlignment.TopLeft)
        {
            var s = new GLTexture2D(bmp, internalformat, bitmipmaplevel, genmipmaplevel, ownbitmaps, alignment);
            items[EnsureName(name)] = s;
            return s;
        }
        public GLTexture2DArray NewTexture2DArray(string name, params Object[] cnst)
        {
            var s = (GLTexture2DArray)Activator.CreateInstance(typeof(GLTexture2DArray), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }
        public GLTexture3D NewTexture3D(string name, params Object[] cnst)
        {
            var s = (GLTexture3D)Activator.CreateInstance(typeof(GLTexture3D), cnst, null);
            items[EnsureName(name)] = s;
            return s;
        }

        // remove
        public void Dispose(Object obj)     // dispose of this now, and remove from list
        {
            string keytodelete = null;
            foreach (var kvp in items.Keys) //TBD
            {
                if (items[kvp] == obj)
                {
                    (obj as IDisposable).Dispose();
                    keytodelete = kvp;
                    break;
                }
            }

            if (keytodelete != null)
                items.Remove(keytodelete);
        }

        public void Dispose()
        {
            if (StackTrace)
            {
                foreach (var r in items)
                {
                    System.Diagnostics.Debug.WriteLine($"Disposing of {r.Key} {stacktrace[r.Key]}");
                    r.Value.Dispose();
                    System.Diagnostics.Debug.WriteLine($"----");
                }

                items.Clear();
            }
            else
            {
                items.Dispose();
            }
        }
        
        // helpers

        private string EnsureName(string name)
        {
            name = (name == null) ? ("Unnamed_" + (unnamed++)) : name;
            if ( StackTrace )
                stacktrace[name] = Environment.StackTrace;
            return name; 
        }

        private Dictionary<string, string> stacktrace = new Dictionary<string, string>();
        private DisposableDictionary<string, IDisposable> items = new DisposableDictionary<string, IDisposable>();
        private int unnamed = 0;


    }
}
