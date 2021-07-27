/*
 * Copyright © 2016 EDDiscovery development team
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

using Newtonsoft.Json.Linq;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EliteDangerousCore.EDSM
{
    public class GalacticMapObject
    {
        public int id;
        public string type;
        public string name;
        public string galMapSearch;
        public string galMapUrl;
        public string colour;
        public List<Vector3> points;
        public string description;
 
        public GalMapType galMapType;

        public GalacticMapObject()
        {
            points = new List<Vector3>();
        }

        public void PrintElement(XElement x, int level)
        {
            string pad = "                    ".Substring(0, level);
            System.Diagnostics.Debug.WriteLine(pad+ $"{x.NodeType} {x.Name} {x.HasElements} : {x.Value}");
            //                if (x.NodeType == System.Xml.XmlNodeType.Element)
            if (x.HasAttributes)
            {
                foreach (var y in x.Attributes())
                {
                    System.Diagnostics.Debug.WriteLine(pad + $"  .. {y.NodeType} {y.Name} : {y.Value}");
                }
            }
            if (x.HasElements)
            {
                foreach (XElement y in x.Descendants())
                {
                    PrintElement(y, level + 1);
                }
            }
        }

        public GalacticMapObject(JObject jo)
        {
            id = jo["id"].Int();
            type = jo["type"].Str("Not Set");
            name = jo["name"].Str("No name set");
            galMapSearch = jo["galMapSearch"].Str("");
            galMapUrl = jo["galMapUrl"].Str("");
            colour = jo["color"].Str("Orange");
            description = jo["descriptionMardown"].Str("No description");       // default back up description in case html fails

            var descriptionhtml = jo["descriptionHtml"].StrNull();

            if (descriptionhtml != null)
            {
                string t = "<Body>" + descriptionhtml + "</Body>";

                try
                {
                    XElement xml = XElement.Parse(t);
                    string totaltext = "";
                    foreach (XElement x in xml.Elements())
                    {
                      //  System.Diagnostics.Debug.WriteLine($"Node {x.NodeType} {x.Name} {x.Value}");
                        if (x.Name == "ul")
                        {
                            try
                            {
                                List<XNode> nlist = x.Nodes().ToList();
                                var nlist1 = nlist.Select(x1 => ((XElement)x1).Value).ToList();
                                totaltext += Environment.NewLine + string.Join(Environment.NewLine, nlist1);
                            }
                            catch       // don't care if it fails - trapping due to forced XElement conversion
                            {
                            }
                        }
                        else if (x.Value.HasChars())
                        {
                            string tww = x.Value.WordWrap(120);
                            //System.Diagnostics.Debug.WriteLine($"Line {tww}");
                            totaltext = totaltext.AppendPrePad(tww, Environment.NewLine) + Environment.NewLine;

                            if (x.HasElements)
                            {
                                foreach (var y in x.Elements())
                                {
                                    if (y.Name == "a")
                                    {
                                        var href = y.Attributes().Where(aa => aa.Name == "href").FirstOrDefault();
                                        if (href != null)
                                        {
                                            totaltext += Environment.NewLine + href.Value;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    description = totaltext;
                  //  System.Diagnostics.Debug.WriteLine($"\r\n{name} : {description}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"For {name} xml parse failed {ex}");
                }
            }

            points = new List<Vector3>();

            try
            {
                JArray coords = (JArray)jo["coordinates"];

                if (coords.Count > 0)
                {
                    if (coords[0].Type == JTokenType.Array)
                    {
                        foreach (JArray ja in coords)
                        {
                            float x, y, z;
                            x = ja[0].Value<float>();
                            y = ja[1].Value<float>();
                            z = ja[2].Value<float>();
                            points.Add(new Vector3(x, y, z));
                        }
                    }
                    else
                    {
                        JArray plist = coords;

                        float x, y, z;
                        x = plist[0].Value<float>();
                        y = plist[1].Value<float>();
                        z = plist[2].Value<float>();
                        points.Add(new Vector3(x, y, z));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("GalacticMapObject parse coordinate error: type" + type + " " + ex.Message);
                points = null;
            }
        }

    }
}

