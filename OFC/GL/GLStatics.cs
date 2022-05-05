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

using GLOFC.Utils;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GLOFC
{
    /// <summary>
    /// GL Statics functions are generic to GL as a whole (not specifically for GL4).  Most of these are used by the render state so you don't have to control them manually.
    /// Extensions and HasExtensions is useful to know if your GL has a specific GL extension.
    /// Check is used to verify GL is happy and can be sprinkled around your code to make sure nothing is wrong.
    /// </summary>
    public static class GLStatics
    {
        [DllImport("opengl32.dll", EntryPoint = "wglGetCurrentContext")]
        extern static IntPtr wglGetCurrentContext();// DCAl

        /// <summary>
        /// Get the currentopenGL rendering context handle.
        /// </summary>
        public static IntPtr GetContext()
        {
            return wglGetCurrentContext();
        }

        /// <summary>
        /// Call to check that the GL system is okay. GL stores errors in GetError() and you must explicitly check.
        /// Normally wrapped in a Debug.Assert as System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
        /// </summary>
        /// <param name="errmsg">Error message to report, if returns false</param>
        /// <returns>true if okay, false if error</returns>
        public static bool CheckGL(out string errmsg)
        {
            OpenTK.Graphics.OpenGL4.ErrorCode ec;
            errmsg = "";

            while ((ec = OpenTK.Graphics.OpenGL4.GL.GetError()) != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)     // call until no error
            {
                errmsg = errmsg.AppendPrePad($"Error {ec.ToString()}" + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine("GL error " + ec.ToString());
            }

            return !errmsg.HasChars();
        }

        // Allocation tracking, for debug mode, to check we are disposing correctly

        private static Dictionary<Type, int> allocatecounts = new Dictionary<Type, int>();

        /// <summary>
        /// Register an allocation of type T
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void RegisterAllocation(Type t)
        {
            if (!allocatecounts.ContainsKey(t))
                allocatecounts[t] = 0;
            allocatecounts[t]++;
        }
        /// <summary>
        /// Register an deallocation of type T
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void RegisterDeallocation(Type t)
        {
            if ( --allocatecounts[t] < 0 )
                System.Diagnostics.Debug.Assert(false, $"Type {t.Name} over deallocated");
        }

        /// <summary>
        /// Verify all deallocated - call at the end of your program after Items.Dispose()
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void VerifyAllDeallocated()
        {
            foreach (var t in allocatecounts)
            {
                System.Diagnostics.Debug.WriteLine($"OFC Block Type {t.Key.Name} Left {t.Value}");
            }

            foreach (var t in allocatecounts)
            { 
                if ( t.Value > 0)
                    System.Diagnostics.Debug.Assert(false, $"OFC Warning - Block Type {t.Key.Name} not deallocated");
            }
        }

        /// <summary>
        /// Set a EnableCap flag to state
        /// </summary>
        public static void SetEnable(EnableCap c, bool state)
        {
            if (state)
                GL.Enable(c);
            else
                GL.Disable(c);
        }

        /// <summary>
        /// Clear depth buffer
        /// </summary>
        public static void ClearDepthBuffer()    
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        /// <summary>
        /// Clear depth buffer to value
        /// </summary>
        public static void ClearDepthBuffer(int s)       
        {
            GL.ClearDepth(s);
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        /// <summary>
        /// Clear stencil buffer
        /// </summary>
        public static void ClearStencilBuffer()      
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
        }

        /// <summary>
        /// Clear depth buffer to value
        /// </summary>
        public static void ClearStencilBuffer(int s)      
        {
            GL.ClearStencil(s);
            GL.Clear(ClearBufferMask.StencilBufferBit);
        }

        /// <summary>
        /// Clear buffer set using ClearBufferMask
        /// </summary>
        public static void ClearBuffer(ClearBufferMask mask)     
        {
            GL.Clear(mask);
        }

        /// <summary>
        /// Flush GL pipeline - avoid
        /// </summary>
        public static void Flush()
        {
            GL.Flush();
        }

        /// <summary>
        /// Push all GL commands to pipeline
        /// </summary>
        public static void Finish()
        {
            GL.Finish();
        }

        /// <summary>
        /// Set colour masks in use
        /// </summary>
        public static void ColorMasks(bool red, bool green, bool blue, bool alpha)  // enable/disable frame buffer components
        {
            GL.ColorMask(red, green, blue, alpha);
        }

        /// <summary> Return open GL vendor. OpenGL: The format of the VENDOR string is implementation-dependent.</summary>
        public static string GetVendor()
        {
            return GL.GetString(StringName.Vendor);
        }

        /// <summary> Return open GL renderer. OpenGL: The format of the RENDERER string is implementation-dependent </summary>
        public static string GetRenderer()
        {
            return GL.GetString(StringName.Renderer);
        }


        /// <summary> Return open GL shading language string
        /// OpenGL 22.2: The SHADING_LANGUAGE_VERSION string is laid out as follows: version number space vendor-specific information
        /// The version number is either of the form major number.minor number or major number.minor number.release number, where the numbers all have one or more digits.
        /// The minor number for SHADING_LANGUAGE_VERSION is always two digits, matching the OpenGL Shading Language Specification release number.
        /// The release number and vendor specific information are optional.However, if present, then they pertain to the server and their format and contents are implementation-dependent.
        /// </summary>
        public static string GetShadingLanguageVersionString()
        {
            return GL.GetString(StringName.ShadingLanguageVersion);
        }

        /// <summary> Return open GL shader version as a Version. Note since the shader language always has a zero on the end, it will be 4.60, not 4.6
        /// </summary>
        public static Version GetShaderLanguageVersion()
        {
            string s = GL.GetString(StringName.ShadingLanguageVersion);
            int space = s.IndexOf(' ');
            if (space >= 0)
                s = s.Substring(0, space);
            s = s.Trim();
            var v = new Version(s);
            return v;
        }


        /// <summary> Return open GL version
        /// OpenGL 22.2: The VERSION string is laid out as follows: version number space vendor-specific information
        /// The version number is either of the form major number.minor number or major number.minor number.release number, where the numbers all have one or more digits.
        /// The release number and vendor specific information are optional. However, if present, then they pertain to the server and their format and contents are implementation-dependent.
        /// </summary>
        public static string GetVersionString()
        {
            return GL.GetString(StringName.Version);
        }
        /// <summary> Return open GL version as a Version. It will be 4.6, not 4.60 as per the shader language
        /// </summary>
        public static Version GetVersion()
        {
            string s = GL.GetString(StringName.Version);
            int space = s.IndexOf(' ');
            if (space >= 0)
                s = s.Substring(0, space);
            s = s.Trim();
            var v = new Version(s);
            return v;
        }

        /// <summary>
        /// Return list of extensions. Note core removes GlString(Extensions) so has to be done iteratively
        /// </summary>
        public static string[] Extensions()
        {
            int noext = GL.GetInteger(GetPName.NumExtensions);
            string[] ext = new string[noext];
            for (int i = 0; i < noext; i++)
                ext[i] = GL.GetString(StringNameIndexed.Extensions, i);
            System.Diagnostics.Debug.Assert(GLOFC.GLStatics.CheckGL(out string glasserterr), glasserterr);
            return ext;
        }

        // public delegate void DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam);

        /// <summary>
        /// Enable GL debug proc and vector to this function
        /// </summary>
        public static void EnableDebug(DebugProc callback)
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GL.DebugMessageCallback(callback, IntPtr.Zero);
            GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);

            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, -1, "Debug output enabled");
        }
    }
}

