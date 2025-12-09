/*
 * Copyright 2019-2022 Robbyxp1 @ github.com
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
using System.Collections.Generic;
using System.IO;
using GLOFC.Utils;

namespace GLOFC.GL4
{
    /// <summary>
    /// Shader log - use to control what happens if any shaders reports problems
    /// Accumulates shader reports, and allows control over assert
    /// Shader Source Log allows all shaders to write their shader text to a folder for analysis
    /// </summary>

    public static class GLShaderLog 
    {
        private class Info
        {
            public string Log { get; set; } = "";
            public bool Assert { get; set; } = true;
            public bool Okay { get; set; } = true;
            public string ShaderSourceLog { get; set; } = null;
        }

        static private Dictionary<IntPtr, Info> shaderlog = new Dictionary<IntPtr, Info>();

        /// <summary> Shader log, per context</summary>
        public static string ShaderLog { get {
                IntPtr cx = GLStatics.GetContext();
                return shaderlog.ContainsKey(cx) ? shaderlog[cx].Log : "";
            } }

        /// <summary> To assert on an compiler error or to continue, per context</summary>
        static public bool AssertOnError
        {
            get
            {
                IntPtr cx = GLStatics.GetContext();
                return shaderlog.ContainsKey(cx) ? shaderlog[cx].Assert : true;
            }
            set
            {
                IntPtr cx = GLStatics.GetContext();
                if (!shaderlog.ContainsKey(cx))
                    shaderlog.Add(cx, new Info());
                shaderlog[cx].Assert = value;
            }
        }

        /// <summary> Has all compiling been okay, per context. Setting it once to false keeps it false</summary>
        static public bool Okay
        {
            get
            {
                IntPtr cx = GLStatics.GetContext();
                return shaderlog.ContainsKey(cx) ? shaderlog[cx].Okay : true;
            }
            set
            {
                IntPtr cx = GLStatics.GetContext();
                if (!shaderlog.ContainsKey(cx))
                    shaderlog.Add(cx, new Info());
                shaderlog[cx].Okay &= value;
            }
        }

        /// <summary> On set, set path to log area (do not need trailing slash). On get, returns log area or null </summary>
        static public string ShaderSourceLog
        {
            get
            {
                IntPtr cx = GLStatics.GetContext();
                return shaderlog.ContainsKey(cx) ? shaderlog[cx].ShaderSourceLog : null;
            }
            set
            {
                IntPtr cx = GLStatics.GetContext();
                if (!shaderlog.ContainsKey(cx))
                    shaderlog.Add(cx, new Info());
                shaderlog[cx].ShaderSourceLog = value;
            }
        }

        /// <summary> Add to Shader log</summary>
        public static void Add(string s)
        {
            IntPtr cx = GLStatics.GetContext();
            if (!shaderlog.ContainsKey(cx))
            {
                shaderlog.Add(cx, new Info());
            }

            shaderlog[cx].Log = shaderlog[cx].Log.AppendPrePad(s, Environment.NewLine);
        }

        /// <summary> Reset Shader log</summary>
        public static void Reset()
        {
            IntPtr cx = GLStatics.GetContext();
            shaderlog[cx] = new Info();
        }

        /// <summary>
        /// Given a passedoutpath, and the shader source log setting, pass back a source output file name, or null
        /// </summary>
        /// <param name="passedoutpath">What the caller of the compile wanted, returned if no source log enabled</param>
        /// <param name="rootname">Root name of the shader to call file by</param>
        /// <param name="optname">Optional name to add to the file name</param>
        /// <returns>Log file, or null</returns>
        public static string Outfile(string passedoutpath, string rootname, string optname = "")
        {
            string shadersourcelog = ShaderSourceLog;
            if (shadersourcelog == null || !Directory.Exists(shadersourcelog))
                return passedoutpath;
            else
            {
                int fno = 0;
                while (true)
                {
                    passedoutpath = System.IO.Path.Combine(shadersourcelog,rootname + optname + (fno > 0 ? "-" + fno.ToString() : "") + ".glsl");
                    if (System.IO.File.Exists(passedoutpath))
                        fno++;
                    else
                        return passedoutpath;
                }
            }
        }

    }
}
