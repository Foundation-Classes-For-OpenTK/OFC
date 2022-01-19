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

using GLOFC.Utils;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GLOFC.WaveFront
{
    /// <summary>
    /// Read text for a definition of a waveform object
    /// </summary>

    public class GLWaveformObjReader
    {
        // wavefront object format, not exactly well documented..
        // wavefront data : vertex's are ordered 1+
        // co-ordinate system is right handled - +x to right, +y upwards, +z towards you
        // to compensate for opengl, with +x to right, +y upwards, +z away, z co-ordinates in normals/vertexes are inverted

        /// <summary>
        /// Read objects from file and return list of objects. Null if not read
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <returns>List of wavefront objects or null if fails</returns>
        public List<GLWaveformObject> ReadOBJFile(string path)      // throws exceptions
        {
            string text = null;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception)
            {
                return null;
            }

            return ReadOBJData(text);
        }

        /// <summary>
        /// Read objects from a string.
        /// </summary>
        /// <param name="textdescription">Wavefront definition of objects</param>
        /// <param name="correctzforopengl">Correct for opengl orientation in z axis (true means invert)</param>
        /// <param name="thrownotimplemented">Throw on finding a not implemented descriptor</param>
        /// <returns>List of objects, or null if fails</returns>
        /// <exception cref="System.NotImplementedException">Descriptor not implemented</exception>

        public List<GLWaveformObject> ReadOBJData(string textdescription, bool correctzforopengl = true, bool thrownotimplemented = false)          // throws exceptions
        {
            reader_vertices = new GLMeshVertices();
            reader_objects = new List<GLWaveformObject>();
            reader_current = null;
            reader_matlib = "Not set";

            float zcorr = correctzforopengl ? -1 : 1;

            using (TextReader reader = new StringReader(textdescription))
            {
                string line;
                while( (line = reader.ReadLine()) != null )
                {
                    line = line.Trim();

                    if ( line.HasChars())
                    {
                        List<string> words = new List<string>();
                        foreach( var w in line.Split(' ', '\t'))
                        {
                            if (w.HasChars())
                                words.Add(w);
                        }

                        string type = words[0].ToLower();

                        line = line.Substring(words[0].Length).Trim();

                        words.RemoveAt(0);

                        //System.Diagnostics.Debug.WriteLine("Read " + line);

                        if (type.StartsWith("#"))
                        {
                            // comment
                        }
                        else if (type == "end")     //Rob addition!
                        {
                            break;
                        }
                        else if (type == "v")
                        {
                            if (words.Count >= 3)
                            {
                                reader_vertices.Vertices.Add(new Vector4(   words[0].InvariantParseFloat(0),
                                                                            words[1].InvariantParseFloat(0),
                                                                            zcorr * words[2].InvariantParseFloat(0), 
                                                                            words.Count < 4 ? 1 : words[3].InvariantParseFloat(0)));
                            }
                        }
                        else if (type == "vt")
                        {
                            if (words.Count >= 2)
                            {
                                reader_vertices.TextureVertices.Add(new Vector3(words[0].InvariantParseFloat(0), words[1].InvariantParseFloat(0),
                                                            words.Count < 3 ? 0 : words[2].InvariantParseFloat(0)));
                            }
                        }
                        else if (type == "vn")
                        {
                            if (words.Count >= 2)
                                reader_vertices.Normals.Add(new Vector3(words[0].InvariantParseFloat(0), words[1].InvariantParseFloat(0), zcorr * words[2].InvariantParseFloat(0)));
                        }
                        else if (type == "vp")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }

                        else if (type == "deg")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "bmat")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "step")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "cstype")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }

                        else if (type == "f")
                        {
                            reader_current = Create(reader_current == null || (reader_current.ObjectType != GLWaveformObject.ObjectTypeEnum.Polygon && reader_current.ObjectType != GLWaveformObject.ObjectTypeEnum.Unassigned));
                            reader_current.ObjectType = GLWaveformObject.ObjectTypeEnum.Polygon;

                            foreach (string w in words)
                            {
                                string[] comps = w.Split('/');

                                int ti = comps.Length > 1 ? (comps[1].InvariantParseInt(int.MinValue)) : int.MinValue;

                                if (ti != int.MinValue)
                                {
                                    if (reader_current.Indices.VertexIndices.Count != reader_current.Indices.TextureIndices.Count)
                                        throw new NotImplementedException("New texture index but previous was missing them");

                                    if (ti < 0)
                                        ti = reader_vertices.TextureVertices.Count + ti;
                                    else if (ti >= 1)
                                        ti--;

                                    ti = Math.Min(Math.Max(ti, 0), reader_vertices.TextureVertices.Count - 1);
                                    reader_current.Indices.TextureIndices.Add((uint)ti);
                                }

                                int ni = comps.Length > 2 ? (comps[2].InvariantParseInt(int.MinValue)) : int.MinValue;

                                if ( ni != int.MinValue )
                                {
                                    if (reader_current.Indices.VertexIndices.Count != reader_current.Indices.NormalIndices.Count)
                                        throw new NotImplementedException("New texture index but previous was missing them");

                                    if (ni < 0)
                                        ni = reader_vertices.Normals.Count + ni;
                                    else if (ni >= 1)
                                        ni--;

                                    ni = Math.Min(Math.Max(ni, 0), reader_vertices.Normals.Count - 1);
                                    reader_current.Indices.NormalIndices.Add((uint)ni);
                                }

                                int vi = comps[0].InvariantParseInt(int.MinValue);

                                if (vi != int.MinValue)
                                {
                                    if (vi < 0)
                                        vi = reader_vertices.Vertices.Count + vi;
                                    else if (vi >= 1)
                                        vi--;

                                    vi = Math.Min(Math.Max(vi, 0), reader_vertices.Vertices.Count - 1);
                                    reader_current.Indices.VertexIndices.Add((uint)vi);
                                }

                            }
                        }
                        else if (type == "p")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "l")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "curv")    // curve http://paulbourke.net/dataformats/obj/
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "curv2")   // 2d curve
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "surf")   // surface
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }

                        else if (type == "parm")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "trim")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "hole")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "scrv")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "sp")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "end")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }

                        else if (type == "con")
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }

                        else if (type == "mtllib")
                        {
                            reader_matlib = line;
                            if (reader_current != null)
                                reader_current.MatLibname = reader_matlib;
                        }
                        else if (type == "usemtl")
                        {
                            reader_current = Create(reader_current == null || reader_current.ObjectType != GLWaveformObject.ObjectTypeEnum.Unassigned);
                            reader_current.Material = line;
                        }
                        else if (type == "g")
                        {
                            reader_current = Create(reader_current == null || reader_current.ObjectType != GLWaveformObject.ObjectTypeEnum.Unassigned);
                            reader_current.GroupName = line;
                        }
                        else if (type == "s") // smoothing group
                        {
                            // ignore smoothing groups..
                            //throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "mg") // merging group
                        {
                            throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "o") // object name
                        {
                            reader_current = Create(reader_current == null || reader_current.ObjectType != GLWaveformObject.ObjectTypeEnum.Unassigned);
                            reader_current.ObjectName = line;
                        }

                        else if (type == "bevel") // Bevel interpolation
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "c_interp") //   Color interpolation
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "d_interp") // Dissolve interpolation
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "lod") //   Level of detail
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "shadow_obj") //  Shadow casting
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "trace_obj") //Ray tracing
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "ctech") //  Curve approximation technique
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else if (type == "stech") //  Surface approximation technique
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }
                        else
                        {
                            if (thrownotimplemented)
                                throw new NotImplementedException("Not implemented:" + type);
                        }

                    }
                }
            }

            return reader_objects;
        }

        private GLWaveformObject Create(bool cond)
        {
            if (cond)
            {
                GLWaveformObject cur = new GLWaveformObject(GLWaveformObject.ObjectTypeEnum.Unassigned, reader_vertices, reader_matlib);
                reader_objects.Add(cur);
                return cur;
            }
            else
                return reader_objects.Last();
        }

        private GLMeshVertices reader_vertices;
        private List<GLWaveformObject> reader_objects;
        private GLWaveformObject reader_current;
        private string reader_matlib;

    }
}
