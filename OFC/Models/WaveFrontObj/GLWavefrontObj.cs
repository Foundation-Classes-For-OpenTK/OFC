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

namespace OFC.WaveFront
{
    // wavefront object, pointing to vertices (may be shared with other objects) 
    // Indicies, and containing meta data like materials

    public class GLWaveformObject
    {
        public enum ObjectType { Unassigned, Polygon };
        public GLMeshVertices Vertices { get; set; }
        public GLMeshIndices Indices { get; set; }

        public string Material { get; set; }
        public string Groupname { get; set; }
        public string Objectname { get; set; }
        public string MatLibname { get; set; }

        public ObjectType Objecttype { get; set; }

        public GLWaveformObject(ObjectType t, GLMeshVertices vert, string matl)
        {
            Objecttype = t;
            Vertices = vert;
            MatLibname = matl;
            Indices = new GLMeshIndices();
        }
    }
}
