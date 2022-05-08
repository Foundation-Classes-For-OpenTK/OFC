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
using System.Collections.Generic;
using GLOFC.Utils;

namespace GLOFC.GL4
{
    /// <summary>
    /// Shader log - any text from compiler/linker ends up here
    /// </summary>

    public static class GLShaderLog 
    {
        private class Info
        {
            public string Log { get; set; } = "";
            public bool Assert { get; set; } = true;
            public bool Okay { get; set; } = true;
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

    }
}
