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

using OpenTK;
using System.Collections.Generic;
using System.Linq;

namespace GLOFC
{
    /// <summary>
    /// Class holds a mesh consisting of vertices, texture vertices, Normals
    /// </summary>
    public class GLMeshVertices     // Vertex store for verts, normals, textures
    {
        /// <summary> List of vertices</summary>
        public List<Vector4> Vertices { get; set; }

        /// <summary> List of texture vertices </summary>
        public List<Vector3> TextureVertices { get; set; }
        /// <summary> List of texture vertices Vector2 </summary>
        public List<Vector2> TextureVertices2 { get { return TextureVertices.Select(x => new Vector2(x.X, x.Y)).ToList(); } }
        /// <summary> Array of texture vertices Vector2 </summary>
        public Vector2[] TextureVertices2Array { get { return TextureVertices.Select(x => new Vector2(x.X, x.Y)).ToArray(); } }

        /// <summary> List of normals </summary>
        public List<Vector3> Normals { get; set; }

        /// <summary>
        /// Construct a vertices list from data
        /// </summary>
        /// <param name="verts">List of vertices</param>
        /// <param name="texvert">List of textures Vertexes</param>
        /// <param name="norms">List of normals</param>
        public GLMeshVertices(List<Vector4> verts, List<Vector3> texvert, List<Vector3> norms)
                        
        {
            Vertices = verts;
            TextureVertices = texvert;
            Normals = norms;
        }

        /// <summary> Default Constructor </summary>
        public GLMeshVertices()
        {
            Vertices = new List<Vector4>();
            TextureVertices = new List<Vector3>();
            Normals = new List<Vector3>();
        }
    }

    /// <summary>
    /// Class holds a list of Mesh Indices for Vertex, Textures and Normals
    /// </summary>

    public class GLMeshIndices // indices store for vertex, normals, textures
    {
        /// <summary> List of vertex indices </summary>
        public List<uint> VertexIndices { get; set; }
        /// <summary> Vertex indices as an array </summary>
        public uint[] VertexIndicesArray { get { return VertexIndices.ToArray(); } }
        /// <summary> List of texture indices </summary>
        public List<uint> TextureIndices { get; set; }
        /// <summary> List of normal indices  </summary>
        public List<uint> NormalIndices { get; set; }

        /// <summary>
        /// Construct from a list of indices for vertex, texture and normals
        /// </summary>
        /// <param name="vertindex"></param>
        /// <param name="texindex"></param>
        /// <param name="normindex"></param>
        public GLMeshIndices(List<uint> vertindex, List<uint> texindex, List<uint> normindex)
        {
            VertexIndices = vertindex;
            TextureIndices = texindex;
            NormalIndices = normindex;
        }

        /// <summary> Default Constructor </summary>
        public GLMeshIndices()
        {
            VertexIndices = new List<uint>();
            TextureIndices = new List<uint>();
            NormalIndices = new List<uint>();
        }

        /// <summary> Refactor the indices into triangles, wound either CCW or CW </summary>
        public void RefactorVertexIndicesIntoTriangles(bool ccw = true)
        {
            var newVertexIndices = new List<uint>();
            var newTextureIndices = new List<uint>();
            var newNormalIndices = new List<uint>();

            for (int i = 1; i <= VertexIndices.Count - 2; i++)
            {
                int f = i, s = i + 1;
                if (!ccw)
                {
                    f = i + 1; s = i;
                }

                newVertexIndices.Add(VertexIndices[0]);
                newVertexIndices.Add(VertexIndices[f]);
                newVertexIndices.Add(VertexIndices[s]);

                if (TextureIndices.Count > 0)
                {
                    newTextureIndices.Add(TextureIndices[0]);
                    newTextureIndices.Add(TextureIndices[f]);
                    newTextureIndices.Add(TextureIndices[s]);
                }

                if (NormalIndices.Count > 0)
                {
                    newNormalIndices.Add(NormalIndices[0]);
                    newNormalIndices.Add(NormalIndices[f]);
                    newNormalIndices.Add(NormalIndices[s]);
                }
            }

            VertexIndices = newVertexIndices;
            TextureIndices = newTextureIndices;
            NormalIndices = newNormalIndices;
        }

    }

    /// <summary>
    /// Class holds a Mesh, consisting of vertices and indices
    /// </summary>
    public class GLMesh
    {
        /// <summary> Vertices of the mesh </summary>
        public GLMeshVertices Vertices;
        /// <summary> Indices of the mesh </summary>
        public GLMeshIndices Indices;
    }
}
