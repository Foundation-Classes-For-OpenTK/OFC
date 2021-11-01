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
using System.Drawing;
using System.IO;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace GLOFC.GL4
{
    // This is a GL shader object of type ShaderType

    public class GLShader : IDisposable
    {
        public int Id { get; private set; } = -1;
        public bool Compiled { get { return Id != -1; } }

        static public List<string> IncludePaths = new List<string>();               // add to add new include paths for #include
        static public List<string> IncludeModules = new List<string>();             // add to add new resource paths for #include

        private ShaderType type;

        public GLShader( ShaderType t )
        {
            type = t;
        }

        // codelisting is glsl with the following extensions:
        // #include resourcename
        //      resourcename can either be a reference to an OFC glsl file, from the GL4 root, such as Shaders.Volumetric.volumetricgeoshader.glsl
        //      or it can be a fully qualified resource reference: TestOpenTk.Volumetrics.volumetricgeo3.glsl
        //      or it can be a partial resource reference from a include modules path (no . at the end, fully qualified : TestOpenTk.Volumetrics)
        //      or it can be a fully qualified filename (no quotes)
        //      or a partial path from one of the static includepaths
        // const values
        //      constvalues allow you to override definitions in the script for const <type> var = value
        //      declare your variables in glsl like this (const int iterations = 10 for example)
        //      then include in call a list of (string,value) pairs to set the const values to (new object[] {"iterations",20})
        //      glsl types: int, float (passed in as float or double), bool, vec2 (OpenTK.Vector2), vec3 (OpenTK.Vector3), vec4 (as OpenTK.Vector4 or Color)
        //                  vec4[] (OpenTK.Vector4[])

        public string Compile(string codelisting, Object[] constvalues = null, string completeoutfile = null)                // string return gives any errors
        {
            Id = GL.CreateShader(type);
            GLStatics.RegisterAllocation(typeof(GLShader));

            string source = PreprocessShaderCode(codelisting, constvalues, completeoutfile);
            GL.ShaderSource(Id, source);

            GL.CompileShader(Id);

            string CompileReport = GL.GetShaderInfoLog(Id);

            if (CompileReport.HasChars())
            {
                GL.DeleteShader(Id);
                Id = -1;

                int opos = CompileReport.IndexOf("0(");
                if (opos != -1)
                {
                    int opose = CompileReport.IndexOf(")", opos);
                    if (opose != -1)     // lets help ourselves by reporting the source.. since the source can be obscure.
                    {
                        int? lineno = CompileReport.Substring(opos + 2, opose - opos - 2).InvariantParseIntNull();

                        if (lineno.HasValue)
                        {
                            CompileReport = CompileReport + Environment.NewLine + source.LineMarking(lineno.Value - 5, 10, "##0", lineno.Value);
                        }
                    }
                }

                return CompileReport;
            }

            return null;
        }

        public void Dispose()           // you can double dispose.
        {
            if (Id != -1)
            {
                GL.DeleteShader(Id);
                GLStatics.RegisterDeallocation(typeof(GLShader));
                Id = -1;
            }
            else
                System.Diagnostics.Trace.WriteLine($"OFC Warning - double disposing of ${this.GetType().FullName}");
        }


        // take a codelisting, with optional includes, optional variables, and optional output of the code listing
        private static string PreprocessShaderCode(string codelisting, Object[] constvalues = null, string completeoutfile = null)
        {
            LineReader lr = new LineReader();
            lr.OpenString(codelisting);

            string code = "", line;
            List<string> constcode = constvalues != null ? ConstVars(constvalues) : null;       // compute const vars, to be placed after # lines
            List<string> constvars = constcode != null ? constcode.Select((s)=>s.Substring(0,s.IndexOf("=")+1)).ToList() : null;        // without the values for pattern matching

            bool doneversion = false;
            HashSet<string> extensions = new HashSet<string>();

            while ((line = lr.ReadLine()) != null)
            {
                line = line.Trim(); // remove whitespace

                if (line.Length == 0)     // ignore empties
                {
                }
                else if (line.StartsWith("#include", StringComparison.InvariantCultureIgnoreCase) || line.StartsWith("//Include", StringComparison.InvariantCultureIgnoreCase))
                {
                    line = line.Mid(line[0] == '#' ? 8 : 9).Trim();
                    string include = ResourceHelpers.GetResourceAsString(line);

                    if (include == null)       // if not found directly, use the namespace of this function to use as a root path, allowing us to ditch the upper level stuff
                    {
                        var nsofcode = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace;
                        include = ResourceHelpers.GetResourceAsString(nsofcode + "." + line);
                    }

                    if (include == null)      // if not found, see if its in include modules
                    {
                        foreach (string partial in IncludeModules)
                        {
                            include = ResourceHelpers.GetResourceAsString(partial + "." + line);
                            if (include != null)
                                break;
                        }
                    }

                    if (include == null)        // now try files
                    {
                        if (File.Exists(line))
                        {
                            include = GLOFC.FileHelpers.TryReadAllTextFromFile(line);
                        }
                        else
                        {
                            foreach (string partial in IncludePaths)        // try include paths
                            {
                                string path = Path.Combine(partial, line);
                                if (File.Exists(path))
                                {
                                    include = GLOFC.FileHelpers.TryReadAllTextFromFile(path);
                                    break;
                                }
                            }
                        }
                    }

                    System.Diagnostics.Debug.Assert(include != null, "Cannot include " + line);
                    lr.OpenString(include);     // include it
                }
                else if (line.StartsWith("#version", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!doneversion)      // as soon as we have a version, we are set.
                    {
                        code += line + Environment.NewLine;
                        doneversion = true;
                    }
                }
                else if (line.StartsWith("#extension", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!extensions.Contains(line))          // don't repeat these hash lines - if a file has been included, we may get multiple repeats of them
                    {
                        code += line + Environment.NewLine;
                        extensions.Add(line);
                    }
                }
                else
                {
                    if (constcode != null && line.StartsWith("const"))        // if we have constants, see if its a const line
                    {
                        int i = line.ContainsIn(constvars); // check to see if its a constant to be replaced..
                        if (i >= 0)
                            line = constcode[i];        // replace with this
                    }

                    code += line + Environment.NewLine;
                }
            }

            if (completeoutfile != null)
                System.IO.File.WriteAllText(completeoutfile, code);

            return code;
        }

        // list of pairs of (const name = value) where value can be an int, a Color (expressed as a vec4), a Vector2/3/4, a float/double, a bool, Vector4[], Color[]

        private static List<string> ConstVars(params Object[] values)
        {
            List<string> slist = new List<string>();

            for (int i = 0; i < values.Length - 1;)       // pairs of data, so -1 for length
            {
                string name = values[i] as string;
                Object o = values[i + 1];

                if (o is int)
                {
                    slist.Add("const int " + name + " = " + ((int)o).ToStringInvariant() + ";");
                }
                else if (o is Color)
                {
                    System.Drawing.Color c = (System.Drawing.Color)o;
                    string s = "const vec4 " + name + " = vec4(" + ((float)c.R / 255).ToStringInvariant() + "," + ((float)c.G / 255).ToStringInvariant() + "," + ((float)c.B / 255).ToStringInvariant() + "," + ((float)c.A / 255).ToStringInvariant() + ");";
                    slist.Add(s);
                }
                else if (o is OpenTK.Vector4)
                {
                    OpenTK.Vector4 v = (OpenTK.Vector4)o;
                    string s = "const vec4 " + name + " = vec4(" + v.X.ToStringInvariant() + "," + v.Y.ToStringInvariant() + "," + v.Z.ToStringInvariant() + "," + v.W.ToStringInvariant() + ");";
                    slist.Add(s);
                }
                else if (o is OpenTK.Vector2)
                {
                    OpenTK.Vector2 v = (OpenTK.Vector2)o;
                    slist.Add("const vec2 " + name + " = vec2(" + v.X.ToStringInvariant() + "," + v.Y.ToStringInvariant() + ");");
                }
                else if (o is OpenTK.Vector3)
                {
                    OpenTK.Vector3 v = (OpenTK.Vector3)o;
                    slist.Add("const vec3 " + name + " = vec3(" + v.X.ToStringInvariant() + "," + v.Y.ToStringInvariant() + "," + v.Z.ToStringInvariant() + ");");
                }
                else if (o is float)
                {
                    slist.Add("const float " + name + " = " + ((float)o).ToStringInvariant() + ";");
                }
                else if (o is double)
                {
                    slist.Add("const float " + name + " = " + ((double)o).ToStringInvariant() + ";");
                }
                else if (o is bool)
                {
                    slist.Add("const bool " + name + " = " + ((bool)o == false ? "false" : "true") + ";");
                }
                else if (o is OpenTK.Vector4[])
                {
                    Vector4[] p = o as Vector4[];
                    slist.Add("const vec4[] " + name + " = " + p.ToDefinition() + ";");
                }
                else if (o is Color[])
                {
                    Color[] p = o as Color[];
                    string exp = "const vec4 " + name + "[] = {";
                    string vec = "";
                    foreach (Color c in p)
                        vec = vec.AppendPrePad($"vec4({((float)c.R / 255.0f).ToStringInvariant()},{((float)c.G / 255.0f).ToStringInvariant()},{((float)c.B / 255.0f).ToStringInvariant()},{((float)c.A / 255.0f).ToStringInvariant()})", ",");

                    slist.Add(exp + vec + "};");
                }
                else if (o is null)     // null means don't override
                {

                }
                else
                    System.Diagnostics.Debug.Assert(false);

                i += 2;
            }

            return slist;
        }


    }
}
