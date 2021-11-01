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


using System;

namespace GLOFC.GL4
{
    public interface IGLVertexArray : IDisposable           // to be attached to a renderableitem, vertex arrays need to be based on this
    {
        int Id { get; }
        void Bind();                                        // called just before the item is drawn
    }

    public interface IGLShader : IDisposable                // All shaders inherit from this
    {
        int Id { get; }
        void Start(GLMatrixCalc c);                         // Renders call this when program has just started
        void Finish();                                      // Renders call this when program has ended
    }

    public interface IGLPipelineComponentShader : IGLShader  // All pipeline components must come from this
    {
        int References { get; set; }
        byte[] GetBinary(out OpenTK.Graphics.OpenGL4.BinaryFormat binformat);     // must have linked with wantbinary
        void Load(byte[] bin, OpenTK.Graphics.OpenGL4.BinaryFormat binformat);
    }

    public interface IGLProgramShader : IGLShader           // Shaders suitable for the rendering queue inherit from this - standard and pipeline
    {
        string Name { get; }
        bool Enable { get; set; }

        IGLShader GetShader(OpenTK.Graphics.OpenGL4.ShaderType st);    // get a subcomponent.  if the shader does not have subcomponents, its should return itself.
        T GetShader<T>(OpenTK.Graphics.OpenGL4.ShaderType st) where T : IGLShader;    // get a subcomponent of type T from shader type st. Excepts if not present or no subcomponents
        T GetShader<T>() where T : IGLShader;    // get a subcomponent of type T. Excepts if not present or no subcomponents

        byte[] GetBinary(out OpenTK.Graphics.OpenGL4.BinaryFormat binformat);     // must have linked with wantbinary
        void Load(byte[] bin, OpenTK.Graphics.OpenGL4.BinaryFormat binformat);

        Action<IGLProgramShader, GLMatrixCalc> StartAction { get; set; }      // allow start and finish action events to be added to the shader..
        Action<IGLProgramShader> FinishAction { get; set; }
    }

    public interface IGLTexture : IDisposable               // all textures from this..
    {
        int Id { get; }
        long ArbId { get; }
        int Width { get; }                                  // primary width of mipmap level 0 bitmap on first array entry
        int Height { get; }
        void Bind(int bindingpoint);                        // textures have a chance to bind themselves, called either by instance data (if per object texture) or by shader (either internally or via StartAction)
    }

    public interface IGLRenderableItem                      // a renderable item inherits from this..
    {
        void Bind(GLRenderState rc, IGLProgramShader shader, GLMatrixCalc c);                 // Bind to context
        void Render();                                      // and render - do the Draw.
        GLRenderState RenderState { get; set; }         // must be set.
        IGLRenderItemData RenderData { get; set; }          // may be null - no instance data/Control.  Allows instance data to be modified in the main program
        int DrawCount { get; set; }                         // Draw count
        int InstanceCount { get; set; }                     // Instances
        bool Visible { get; set; }
    }

    public interface IGLRenderItemData                     // to be attached to a rendableitem, instance control/data need to be based on this. Should not need to be disposable..
    {
        void Bind(IGLRenderableItem ri,IGLProgramShader shader, GLMatrixCalc c);  // called just before the item is drawn
    }

}