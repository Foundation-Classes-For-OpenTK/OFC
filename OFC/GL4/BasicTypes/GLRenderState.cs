/*
 * Copyright 2020 Robbyxp1 @ github.com
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
    // Wraps the openGL main state variables in a class so they get selected correctly for each render.

    public class GLRenderState
    {
        // static creates of a GLRenderControl for the various OpenGL primitives

        static public GLRenderState Tri(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderState Tri(GLRenderState prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        
        static public GLRenderState Tri(uint primitiverestart, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { PrimitiveRestart = primitiverestart, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderState Tri(GLRenderState prev, uint primitiverestart, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { PrimitiveRestart = primitiverestart, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderState Tri(DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderState Tri(GLRenderState prev, DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        // same as Tri, but just kept for naming purposes 
        static public GLRenderState Quads(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        static public GLRenderState Quads(GLRenderState prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        static public GLRenderState Points(float pointsize = 1, bool smooth = true)
        { return new GLRenderState() { PointSize = pointsize, PointSprite = false, PointSmooth = smooth }; }

        static public GLRenderState Points(GLRenderState prev, float pointsize = 1, bool smooth = true)
        { return new GLRenderState(prev) { PointSize = pointsize, PointSprite = false, PointSmooth = smooth }; }

        static public GLRenderState PointsByProgram()
        { return new GLRenderState() { PointSize = 0 }; }

        static public GLRenderState PointSprites()
        { return new GLRenderState() { PointSize = 0, PointSprite = true }; }

        static public GLRenderState PointSprites(GLRenderState prev)
        { return new GLRenderState(prev) { PointSize = 0, PointSprite = true }; }

        static public GLRenderState Patches(int patchsize = 4, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderState() { PatchSize = patchsize, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }

        static public GLRenderState Patches(GLRenderState prev, int patchsize = 4, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderState(prev) { PatchSize = patchsize, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }

        static public GLRenderState Lines(float linewidth = 1, bool smooth = true)        // vertex 0/1 line, 2/3 line
        { return new GLRenderState() { LineWidth = linewidth, LineSmooth = smooth }; }

        static public GLRenderState Lines(GLRenderState prev, float linewidth = 1, bool smooth = true)        // vertex 0/1 line, 2/3 line
        { return new GLRenderState(prev) { LineWidth = linewidth, LineSmooth = smooth }; }

       
        // creators

        public GLRenderState()                                          // fully default 
        {
        }

        public GLRenderState(GLRenderState prev)      // copy previous fixed settings
        {
            PrimitiveRestart = prev.PrimitiveRestart;
            ClipDistanceEnable = prev.ClipDistanceEnable;
            DepthTest = prev.DepthTest;
            DepthFunctionMode = prev.DepthFunctionMode;
            WriteDepthBuffer = prev.WriteDepthBuffer;
            DepthClamp = prev.DepthClamp;
            BlendEnable = prev.BlendEnable;
            BlendSourceRGB = prev.BlendSourceRGB;
            BlendDestRGB = prev.BlendDestRGB;
            BlendSourceA = prev.BlendSourceA;
            BlendDestA = prev.BlendDestA;
            BlendEquationRGB = prev.BlendEquationRGB;
            BlendEquationA = prev.BlendEquationA;
            Discard = prev.Discard;
            ColorMasking = prev.ColorMasking;
        }

        static public GLRenderState Start()
        {
            var startstate = new GLRenderState()        // Set the default state we want to be in at start (some state defaults are at bottom)
            {
                FrontFace = FrontFaceDirection.Ccw,
                CullFace = true,
                PolygonModeFrontAndBack = PolygonMode.Fill,
                PatchSize = 1,
                PointSize = 1,
                PointSprite = false,
                PointSmooth = true,
                PolygonSmooth = false,
                LineWidth = 1,
                LineSmooth = true,
            };

            var curstate = new GLRenderState()        // set to be purposely not default constructor state and the above state 
            {                                                               // to make all get set.
                PrimitiveRestart = 1,
                ClipDistanceEnable = 1,
                DepthTest = false,
                DepthFunctionMode = DepthFunction.Never,
                WriteDepthBuffer = false,
                DepthClamp = true,
                BlendEnable = false,
                BlendSourceRGB = BlendingFactorSrc.ConstantAlpha,
                BlendSourceA = BlendingFactorSrc.ConstantAlpha,
                BlendDestRGB = BlendingFactorDest.ConstantAlpha,
                BlendDestA = BlendingFactorDest.ConstantAlpha,
                BlendEquationA = BlendEquationMode.Min,
                BlendEquationRGB = BlendEquationMode.Min,
                ColorMasking = 0,
                Discard = true,
            };

            curstate.ApplyState(startstate);        // from curstate, apply state

            return startstate;
        }

        public void ApplyState(GLRenderState newstate)      // apply deltas to GL
        {
            // general

            //System.Diagnostics.Debug.WriteLine("Apply " + newstate.PrimitiveType + " Fixed state " + newstate.ApplyFixed);

            if (newstate.PrimitiveRestart != PrimitiveRestart)
            {
                //System.Diagnostics.Debug.WriteLine("Set PR to {0}", newstate.PrimitiveRestart);
                if (newstate.PrimitiveRestart.HasValue)         // is new state has value
                {
                    if (PrimitiveRestart == null)              // if last was off, turn it on
                        GL.Enable(EnableCap.PrimitiveRestart);

                    GL.PrimitiveRestartIndex(newstate.PrimitiveRestart.Value);  // set
                }
                else
                    GL.Disable(EnableCap.PrimitiveRestart);     // else disable

                PrimitiveRestart = newstate.PrimitiveRestart;
            }

            if (ClipDistanceEnable != newstate.ClipDistanceEnable)        // if changed
            {
                if (newstate.ClipDistanceEnable > ClipDistanceEnable)
                {
                    for (int i = ClipDistanceEnable; i < newstate.ClipDistanceEnable; i++)
                        GL.Enable(EnableCap.ClipDistance0 + i);
                }
                else if (newstate.ClipDistanceEnable < ClipDistanceEnable)
                {
                    for (int i = ClipDistanceEnable - 1; i >= newstate.ClipDistanceEnable; i--)
                        GL.Disable(EnableCap.ClipDistance0 + i);
                }

                ClipDistanceEnable = newstate.ClipDistanceEnable;
            }

            if (DepthTest != newstate.DepthTest)
            {
                DepthTest = newstate.DepthTest;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest, DepthTest);
                //  System.Diagnostics.Debug.WriteLine("Depth Test " + DepthTest);
            }

            if (DepthFunctionMode != newstate.DepthFunctionMode)
            {
                DepthFunctionMode = newstate.DepthFunctionMode;
                GL.DepthFunc(DepthFunctionMode);
            }

            if (WriteDepthBuffer != newstate.WriteDepthBuffer)
            {
                WriteDepthBuffer = newstate.WriteDepthBuffer;
                GL.DepthMask(WriteDepthBuffer);
            }

            if (DepthClamp != newstate.DepthClamp)
            {
                DepthClamp = newstate.DepthClamp;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.DepthClamp, DepthClamp);
                // System.Diagnostics.Debug.WriteLine("Depth Clamp" + DepthClamp.Value);
            }

            if (BlendEnable != newstate.BlendEnable)
            {
                BlendEnable = newstate.BlendEnable;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.Blend, BlendEnable);
            }

            // blend on, we have new values for blend equation
            if (BlendEnable == true)
            {
                if (BlendSourceRGB != newstate.BlendSourceRGB || BlendDestRGB != newstate.BlendDestRGB ||
                    BlendSourceA != newstate.BlendSourceA || BlendDestA != newstate.BlendDestA)
                {
                    BlendSourceRGB = newstate.BlendSourceRGB;
                    BlendDestRGB = newstate.BlendDestRGB;
                    BlendSourceA = newstate.BlendSourceA;
                    BlendDestA = newstate.BlendDestA;
                    GL.BlendFuncSeparate(BlendSourceRGB, BlendDestRGB, BlendSourceA, BlendDestA);
                }

                if (BlendEquationRGB != newstate.BlendEquationRGB || BlendEquationA != newstate.BlendEquationA)
                {
                    BlendEquationRGB = newstate.BlendEquationRGB;
                    BlendEquationA = newstate.BlendEquationA;
                    GL.BlendEquationSeparate(BlendEquationRGB, BlendEquationA);
                }
            }

            if (ColorMasking != newstate.ColorMasking)
            {
                ColorMasking = newstate.ColorMasking;

                GL.ColorMask((ColorMasking & ColorMask.Red) == ColorMask.Red, (ColorMasking & ColorMask.Green) == ColorMask.Green,
                                    (ColorMasking & ColorMask.Blue) == ColorMask.Blue, (ColorMasking & ColorMask.Alpha) == ColorMask.Alpha);
            }

            if (newstate.Discard != Discard)
            {
                Discard = newstate.Discard;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.RasterizerDiscard, Discard);
                //System.Diagnostics.Debug.WriteLine("RS Discard " + Discard);
            }

            // ---------------------------------- Below are default null

            // Geo shaders

            if (newstate.PatchSize.HasValue && PatchSize != newstate.PatchSize)
            {
                PatchSize = newstate.PatchSize;
                GL.PatchParameter(PatchParameterInt.PatchVertices, PatchSize.Value);
            }

            // points

            if (newstate.PointSize.HasValue && PointSize != newstate.PointSize)
            {
                if (newstate.PointSize > 0)     // if fixed point size
                {
                    if (PointSize == null || newstate.PointSize != PointSize.Value)
                        GL.PointSize(newstate.PointSize.Value); // set if different

                    if (PointSize == null || PointSize == 0)       // if previous was off
                        GL.Disable(EnableCap.ProgramPointSize);
                }
                else
                    GL.Enable(EnableCap.ProgramPointSize);

                PointSize = newstate.PointSize;
            }

            if (newstate.PointSprite.HasValue && PointSprite != newstate.PointSprite)
            {
                PointSprite = newstate.PointSprite;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PointSprite, PointSprite.Value);
            }

            if (newstate.PointSmooth.HasValue && PointSmooth != newstate.PointSmooth)
            {
                PointSmooth = newstate.PointSmooth;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PointSmooth, PointSmooth.Value);
            }

            // lines

            if (newstate.LineWidth.HasValue && LineWidth != newstate.LineWidth)
            {
                LineWidth = newstate.LineWidth;
                GL.LineWidth(LineWidth.Value);
            }

            if (newstate.LineSmooth.HasValue && LineSmooth != newstate.LineSmooth)
            {
                LineSmooth = newstate.LineSmooth;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.LineSmooth, LineSmooth.Value);
            }

            // triangles

            if (newstate.PolygonModeFrontAndBack.HasValue && PolygonModeFrontAndBack != newstate.PolygonModeFrontAndBack)
            {
                PolygonModeFrontAndBack = newstate.PolygonModeFrontAndBack;
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonModeFrontAndBack.Value);
            }

            if (newstate.PolygonSmooth.HasValue && PolygonSmooth != newstate.PolygonSmooth)
            {
                PolygonSmooth = newstate.PolygonSmooth;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PolygonSmooth, PolygonSmooth.Value);
            }

            if (newstate.CullFace.HasValue && CullFace != newstate.CullFace)
            {
                CullFace = newstate.CullFace;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.CullFace, CullFace.Value);
                // System.Diagnostics.Debug.WriteLine("Cull mode " + CullFace.Value);
            }

            if (newstate.FrontFace.HasValue && FrontFace != newstate.FrontFace)
            {
                FrontFace = newstate.FrontFace;
                GL.FrontFace(FrontFace.Value);
            }

        }

        // these are only set for particular primitive types - so the default construction is don't care.

        public int? PatchSize { get; set; } = null;                 // patches (Geo shaders)
        public float? PointSize { get; set; } = null;               // points
        public bool? PointSprite { get; set; } = null;              // points
        public bool? PointSmooth { get; set; } = null;              // points
        public float? LineWidth { get; set; } = null;               // lines
        public bool? LineSmooth { get; set; } = null;               // lines
        public PolygonMode? PolygonModeFrontAndBack { get; set; } = null;        // triangles/quads
        public bool? PolygonSmooth { get; set; } = null;            // triangles/quads, not normally set
        public bool? CullFace { get; set; } = null;                 // triangles/quads
        public FrontFaceDirection? FrontFace { get; set; } = null;  // triangles/quads

        // Fixed set - these affect all types so are configured to their defaults for normal drawing

        public uint? PrimitiveRestart { get; set; } = null;         // its either null (disabled) or value (enabled). null does not mean don't care.
        public int ClipDistanceEnable { get; set; } = 0;            // set for number of clip/cull distances to enable. 0 means none. 
        public bool DepthTest { get; set; } = true;                 // default on start of Paint draw is ON
        public DepthFunction DepthFunctionMode { get; set; } = DepthFunction.Less;
        public bool WriteDepthBuffer { get; set; } = true;          // default on start of Paint draw is ON
        public bool DepthClamp { get; set; } = false;   
        public bool BlendEnable { get; set; } = true;
        public BlendingFactorSrc BlendSourceRGB { get; set; } = BlendingFactorSrc.SrcAlpha;
        public BlendingFactorDest BlendDestRGB { get; set; } = BlendingFactorDest.OneMinusSrcAlpha;
        public BlendingFactorSrc BlendSourceA { get; set; } = BlendingFactorSrc.SrcAlpha;
        public BlendingFactorDest BlendDestA { get; set; } = BlendingFactorDest.OneMinusSrcAlpha;
        public BlendEquationMode BlendEquationRGB { get; set; } = BlendEquationMode.FuncAdd;
        public BlendEquationMode BlendEquationA { get; set; } = BlendEquationMode.FuncAdd;
        public bool Discard {get;set;} = false;                     // normal not to discard. default on start of draw is OFF

        [System.Flags] public enum ColorMask { Red=1,Green=2,Blue=4,Alpha=8, All=15};
        public ColorMask ColorMasking = ColorMask.Red | ColorMask.Green | ColorMask.Blue | ColorMask.Alpha;

    }

}
