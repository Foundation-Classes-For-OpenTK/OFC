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
    ///<summary>Wraps the openGL main state variables in a class so they get selected correctly for each render.
    /// An instance of this class is associated with each GLRenderableItem
    ///</summary> 

    public class GLRenderState
    {
        // static creates of a GLRenderControl for the various OpenGL primitives

        /// <summary> Render setup for primitive Triangles, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Tri(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        /// <summary> Render setup for primitive Triangles, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Tri(GLRenderState prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        /// <summary> Render setup for primitive Triangles with primitive restart, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Tri(uint primitiverestart, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { PrimitiveRestart = primitiverestart, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        /// <summary> Render setup for primitive Triangles from a previous RS and with primitive restart, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Tri(GLRenderState prev, uint primitiverestart, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { PrimitiveRestart = primitiverestart, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        /// <summary> Render setup for primitive Triangles with primitive restart, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Tri(DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        /// <summary> Render setup for primitive Trianges from a previous RS with primitive restart, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Tri(GLRenderState prev, DrawElementsType primitiverestarttype, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { PrimitiveRestart = GL4Statics.DrawElementsRestartValue(primitiverestarttype), FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        /// <summary> Render setup for primitive Quads, with optional control over various parameters of the primitive
        /// Compatibility profile only </summary>
        static public GLRenderState Quads(FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState() { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }

        /// <summary> Render setup for primitive Quads from a previous RS, with optional control over various parameters of the primitive 
        /// Compatibility profile only </summary>
        static public GLRenderState Quads(GLRenderState prev, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true,
                                                                PolygonMode polygonmode = PolygonMode.Fill, bool polysmooth = false)
        { return new GLRenderState(prev) { FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode, PolygonSmooth = polysmooth }; }


        /// <summary> Render setup for primitive Points, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Points(float pointsize = 1, bool smooth = true)
        { return new GLRenderState() { PointSize = pointsize, PointSprite = false, PointSmooth = smooth }; }

        /// <summary> Render setup for primitive Points from a previous RS, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Points(GLRenderState prev, float pointsize = 1, bool smooth = true)
        { return new GLRenderState(prev) { PointSize = pointsize, PointSprite = false, PointSmooth = smooth }; }

        /// <summary> Render setup for primitive Points by Program, with optional control over various parameters of the primitive </summary>
        static public GLRenderState PointsByProgram()
        { return new GLRenderState() { PointSize = 0 }; }

        /// <summary> Render setup for primitive Point sprites in compatibility profile </summary>
        static public GLRenderState PointSpritesCompatibility()
        { return new GLRenderState() { PointSize = 0, PointSprite = true }; }

        /// <summary> Render setup for primitive Point sprites in Core profile</summary>
        static public GLRenderState PointSprites()
        { return new GLRenderState() { PointSize = 0}; }

        /// <summary> Render setup for primitive Patches, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Patches(int patchsize = 4, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderState() { PatchSize = patchsize, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }

        /// <summary> Render setup for primitive Patches from a previous RS, with optional control over various parameters of the primitive </summary>
        static public GLRenderState Patches(GLRenderState prev, int patchsize = 4, FrontFaceDirection frontface = FrontFaceDirection.Ccw, bool cullface = true, PolygonMode polygonmode = PolygonMode.Fill)
        { return new GLRenderState(prev) { PatchSize = patchsize, FrontFace = frontface, CullFace = cullface, PolygonModeFrontAndBack = polygonmode }; }

        /// <summary> Render setup for primitive Lines, with optional control over various parameters of the primitive 
        /// Compatibility profile only </summary>
        static public GLRenderState Lines(float linewidth = 1, bool smooth = true)        // vertex 0/1 line, 2/3 line
        { return new GLRenderState() { LineWidth = linewidth, LineSmooth = smooth }; }

        /// <summary> Render setup for primitive Lines from a previous RS, with optional control over various parameters of the primitive
        /// Compatibility profile only </summary>
        static public GLRenderState Lines(GLRenderState prev, float linewidth = 1, bool smooth = true)        // vertex 0/1 line, 2/3 line
        { return new GLRenderState(prev) { LineWidth = linewidth, LineSmooth = smooth }; }

        /// <summary> Render setup for primitive Lines
        /// Core profile </summary>
        static public GLRenderState Lines()     // do not set linewidth or smooth
        { return new GLRenderState(); }


        // creators

        /// <summary> Create a default render state </summary>
        public GLRenderState()                                          // fully default 
        {
        }

        /// <summary> Copy constructor </summary>
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

        /// <summary> Called by WinFormControl, this sets up the initial render state </summary>
        static public GLRenderState Start(GLControlBase.GLProfile profile)
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

            if ( profile == GLControlBase.GLProfile.Core) //core disables
            {
                startstate.PointSprite = null;      // point sprite control - its always set to enabled
                startstate.PointSmooth = null;      // not available
                startstate.LineSmooth = null;
                startstate.PolygonSmooth = null;
            }

            curstate.ApplyState(startstate);        // from curstate, apply state

            return startstate;
        }

        /// <summary> This sets the GL state variables to the render state this object requires. Called by RenderableItem</summary>
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

            if (newstate.PointSprite.HasValue && PointSprite != newstate.PointSprite)   // Compatibility only, CORE always has it turned on
            {
                PointSprite = newstate.PointSprite;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PointSprite, PointSprite.Value);
            }

            if (newstate.PointSmooth.HasValue && PointSmooth != newstate.PointSmooth)   // Compatibility only
            {
                PointSmooth = newstate.PointSmooth;
                GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PointSmooth, PointSmooth.Value);
            }

            // lines

            if (newstate.LineWidth.HasValue && LineWidth != newstate.LineWidth)         // CORE only allows 1.0
            {
                LineWidth = newstate.LineWidth;
                GL.LineWidth(LineWidth.Value);
            }

            if (newstate.LineSmooth.HasValue && LineSmooth != newstate.LineSmooth)      // Compatibility only
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

            //if (newstate.PolygonSmooth.HasValue && PolygonSmooth != newstate.PolygonSmooth)
            //{
            //    PolygonSmooth = newstate.PolygonSmooth;
            //    GLStatics.SetEnable(OpenTK.Graphics.OpenGL.EnableCap.PolygonSmooth, PolygonSmooth.Value);
            //}

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

        /// <summary> Patch size, number of</summary>
        public int? PatchSize { get; set; } = null;                 // patches (Geo shaders)
        /// <summary> Point size </summary>
        public float? PointSize { get; set; } = null;               // points
        /// <summary> Point sprite on/off. Compatibility Profile only </summary>
        public bool? PointSprite { get; set; } = null;              // points
        /// <summary> Point smooth on/off. Compatibility Profile only</summary>
        public bool? PointSmooth { get; set; } = null;              // points
        /// <summary> Line Width. Values >1 is for Compatibility Profile only</summary>
        public float? LineWidth { get; set; } = null;               // lines
        /// <summary> Line Smooth on/off. Compatibility Profile only</summary>
        public bool? LineSmooth { get; set; } = null;               // lines
        /// <summary> Poly Mode - Point, Line or Fill</summary>
        public PolygonMode? PolygonModeFrontAndBack { get; set; } = null;        // triangles/quads
        /// <summary> Polygon Smooth on/off. Compatibility Profile only</summary>
        public bool? PolygonSmooth { get; set; } = null;            // triangles/quads, not normally set
        /// <summary> Cull Face on/off</summary>
        public bool? CullFace { get; set; } = null;                 // triangles/quads
        /// <summary> Front face direction. Cw or Ccw</summary>
        public FrontFaceDirection? FrontFace { get; set; } = null;  // triangles/quads

        // Fixed set - these affect all types so are configured to their defaults for normal drawing

        /// <summary> Primitive restart value. Null is disabled </summary>
        public uint? PrimitiveRestart { get; set; } = null;         // its either null (disabled) or value (enabled). null does not mean don't care.
        /// <summary> Number of clip distances to consider. 0 off. Default 0 </summary>
        public int ClipDistanceEnable { get; set; } = 0;            // set for number of clip/cull distances to enable. 0 means none. 
        /// <summary> Depth Test on/off</summary>
        public bool DepthTest { get; set; } = true;                 // default on start of Paint draw is ON
        /// <summary> Depth function mode. Default is Less. </summary>
        public DepthFunction DepthFunctionMode { get; set; } = DepthFunction.Less;
        /// <summary> Write Depth buffer on/off. Default is on.</summary>
        public bool WriteDepthBuffer { get; set; } = true;          // default on start of Paint draw is ON
        /// <summary> Depth Clamp on/off. Default is off </summary>
        public bool DepthClamp { get; set; } = false;
        /// <summary> Blend Enable on/off. Default is on</summary>
        public bool BlendEnable { get; set; } = true;
        /// <summary> Blend Source RGB. Default is SrcAlpha </summary>
        public BlendingFactorSrc BlendSourceRGB { get; set; } = BlendingFactorSrc.SrcAlpha;
        /// <summary> Blend Dest RGB. Default is OneMinusSrcAlpha </summary>
        public BlendingFactorDest BlendDestRGB { get; set; } = BlendingFactorDest.OneMinusSrcAlpha;
        /// <summary> Blend Source A. Default is SrcAlpha </summary>
        public BlendingFactorSrc BlendSourceA { get; set; } = BlendingFactorSrc.SrcAlpha;
        /// <summary> Blend Dest A. Default is OneMinusSrcAlpha </summary>
        public BlendingFactorDest BlendDestA { get; set; } = BlendingFactorDest.OneMinusSrcAlpha;
        /// <summary> Blend Equation RGB. Default is FuncAdd </summary>
        public BlendEquationMode BlendEquationRGB { get; set; } = BlendEquationMode.FuncAdd;
        /// <summary> Blend Equation A. Default is FuncAdd </summary>
        public BlendEquationMode BlendEquationA { get; set; } = BlendEquationMode.FuncAdd;
        /// <summary> Discard all render on/off. Default is off</summary>
        public bool Discard {get;set;} = false;                     // normal not to discard. default on start of draw is OFF

        /// <summary> Colour Mask flags </summary>
        [System.Flags] public enum ColorMask {
            /// <summary> Red mask </summary>
            Red = 1,
            /// <summary> Green mask </summary>
            Green = 2,
            /// <summary> Blue mask </summary>
            Blue = 4,
            /// <summary> Alpha mask </summary>
            Alpha = 8,
            /// <summary> All </summary>
            All = 15
        };
        /// <summary> Set Colour Masking. Default is All. </summary>
        public ColorMask ColorMasking = ColorMask.Red | ColorMask.Green | ColorMask.Blue | ColorMask.Alpha;

    }

}
