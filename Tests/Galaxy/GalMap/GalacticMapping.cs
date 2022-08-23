/*
 * Copyright © 2016-2021 EDDiscovery development team
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
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using QuickJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLOFC;
using GLOFC.Utils;

namespace EliteDangerousCore.EDSM
{
    public class GalacticMapping
    {
        public List<GalacticMapObject> galacticMapObjects = null;
        public List<GalMapType> galacticMapTypes = null;

        public GalacticMapObject[] RenderableMapObjects { get { return galacticMapObjects.Where(x => x.galMapType.Image != null ).ToArray(); } }
        public GalMapType[] RenderableMapTypes { get { return galacticMapTypes.Where(x => x.Image != null).ToArray(); } }

        public bool Loaded { get { return galacticMapObjects != null; } }

        public GalacticMapping()
        {
            galacticMapTypes = GalMapType.GetTypes();          // we always have the types.
        }

        public bool ParseFile(string file)
        {
            try
            {
                string json = File.ReadAllText(file);
                return ParseJson(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("GalacticMapping.parsedata exception:" + ex.Message);
            }

            return false;
        }

        public bool ParseJson(string json)
        {
            var gmobjects = new List<GalacticMapObject>();

            try
            {
                if (json.HasChars())
                {
                    //Dictionary<string, int> counts = new Dictionary<string, int>();   foreach (var v in GalMapType.GetTypes())   counts[v.Typeid] = 0;

                    JArray galobjects = (JArray)JArray.Parse(json);
                    foreach (JObject jo in galobjects)
                    {
                        GalacticMapObject galobject = new GalacticMapObject(jo);

                        GalMapType ty = galacticMapTypes.Find(x => x.Typeid.Equals(galobject.type));

                        //System.Diagnostics.Debug.WriteLine($"Type {galobject.type}");
                        //counts[galobject.type]++;

                        if (ty == null)
                        {
                            ty = galacticMapTypes[galacticMapTypes.Count - 1];      // last one is default..
                            System.Diagnostics.Trace.WriteLine("Unknown Gal Map object " + galobject.type);
                        }

                        galobject.galMapType = ty;
                        gmobjects.Add(galobject);
                    }

                    galacticMapObjects = gmobjects;

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("GalacticMapping.parsedata exception:" + ex.Message);
            }

            return false;
        }

        public GalacticMapObject Find(string name, bool contains = false )
        {
            if (galacticMapObjects != null && name.Length>0)
            {
                foreach (GalacticMapObject gmo in galacticMapObjects)
                {
                    if ( gmo.name.Equals(name,StringComparison.InvariantCultureIgnoreCase) || (contains && gmo.name.IndexOf(name,StringComparison.InvariantCultureIgnoreCase)>=0))
                    {
                         return gmo;
                    }
                }
            }

            return null;
        }

        public List<string> GetGMONames()
        {
            List<string> ret = new List<string>();

            if (galacticMapObjects != null)
            {
                foreach (GalacticMapObject gmo in galacticMapObjects)
                {
                    ret.Add(gmo.name);
                }
            }

            return ret;
        }
            
    }
}
