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

namespace GLOFC.WaveFront
{
    /// <summary>
    /// These classes handle reading wavefront objects.
    /// </summary>
    internal static class NamespaceDoc { } // just for documentation purposes


    /// <summary>
    /// Wavefront object, pointing to vertices (may be shared with other objects)  
    /// Indicies, and containing meta data like materials
    /// </summary>
    public class GLWaveformObject
    {
        /// <summary> The object type </summary>
        public enum ObjectTypeEnum {
            /// <summary> Unassigned type</summary>
            Unassigned,
            /// <summary> Polygon type </summary>
            Polygon
        };
        /// <summary> Object vertex mesh of vertex, texture vertex and normals </summary>
        public GLMeshVertices Vertices { get; set; }
        /// <summary> Object indices mesh of vertex, texture vertex and normals</summary>
        public GLMeshIndices Indices { get; set; }

        /// <summary> Object material </summary>
        public string Material { get; set; }
        /// <summary> Object groupname</summary>
        public string GroupName { get; set; }
        /// <summary> Object name</summary>
        public string ObjectName { get; set; }
        /// <summary> Material library name</summary>
        public string MatLibname { get; set; }

        /// <summary> Object type</summary>
        public ObjectTypeEnum ObjectType { get; set; }

        /// <summary>
        /// Construct a object
        /// </summary>
        /// <param name="objecttype">Object type</param>
        /// <param name="meshvertices">Mesh vertices</param>
        /// <param name="material">Object material</param>
        public GLWaveformObject(ObjectTypeEnum objecttype, GLMeshVertices meshvertices, string material)
        {
            ObjectType = objecttype;
            Vertices = meshvertices;
            MatLibname = material;
            Indices = new GLMeshIndices();
        }
    }
}
