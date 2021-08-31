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

 using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using EliteDangerousCore;

namespace EliteDangerousCore.EDSM
{
    public enum GalMapTypeEnum
    {
        EDSMUnknown,
        historicalLocation,
        nebula,
        planetaryNebula,
        stellarRemnant,
        blackHole,
        starCluster,
        pulsar,
        minorPOI,
        beacon,
        surfacePOI,
        cometaryBody,
        jumponiumRichSystem,
        planetFeatures,
        deepSpaceOutpost,
        mysteryPOI,
        restrictedSectors,
        independentOutpost,
    }

    public class GalMapType
    {
        public enum GalMapGroup
        {
            Markers = 1,
            Routes,
            Regions,
            Quadrants,
        }

        public string Typeid;
        public string Description;
        public Image Image;
        public GalMapGroup Group;
        public bool Animate;
        public int Index;

        public GalMapType(string id, string desc, GalMapGroup g, Image b, bool animate, int i)
        {
            Typeid = id;
            Description = desc;
            Group = g;
            Image = b;
            Animate = animate;
            Index = i;
        }

        static public List<GalMapType> GetTypes()
        {
            List<GalMapType> type = new List<GalMapType>();

            int index = 0;

            type.Add(new GalMapType("historicalLocation", "η Historical Location", GalMapGroup.Markers, TestOpenTk.Properties.Resources.historicalLocation, false, index++));
            type.Add(new GalMapType("nebula", "α Nebula", GalMapGroup.Markers, TestOpenTk.Properties.Resources.nebula, true, index++));
            type.Add(new GalMapType("planetaryNebula", "β Planetary Nebula", GalMapGroup.Markers, TestOpenTk.Properties.Resources.planetaryNebula, true, index++));
            type.Add(new GalMapType("stellarRemnant", "γ Stellar Features", GalMapGroup.Markers, TestOpenTk.Properties.Resources.stellarRemnant, false, index++));
            type.Add(new GalMapType("blackHole", "δ Black Hole", GalMapGroup.Markers, TestOpenTk.Properties.Resources.blackHole, true, index++));
            type.Add(new GalMapType("starCluster", "σ Star Cluster", GalMapGroup.Markers, TestOpenTk.Properties.Resources.starCluster, true, index++));
            type.Add(new GalMapType("pulsar", "ζ Pulsar", GalMapGroup.Markers , TestOpenTk.Properties.Resources.pulsar, true, index++));
            type.Add(new GalMapType("minorPOI", "★ Minor POI or Star", GalMapGroup.Markers , TestOpenTk.Properties.Resources.minorPOI, true, index++));
            type.Add(new GalMapType("beacon", "⛛ Beacon", GalMapGroup.Markers , TestOpenTk.Properties.Resources.beacon, false, index++));
            type.Add(new GalMapType("surfacePOI", "∅ Surface POI", GalMapGroup.Markers , TestOpenTk.Properties.Resources.surfacePOI, true, index++));
            type.Add(new GalMapType("cometaryBody", "☄ Cometary Body", GalMapGroup.Markers , TestOpenTk.Properties.Resources.cometaryBody, true, index++));
            type.Add(new GalMapType("jumponiumRichSystem", "☢ Jumponium-Rich System", GalMapGroup.Markers, TestOpenTk.Properties.Resources.jumponiumRichSystem, false, index++));
            type.Add(new GalMapType("planetFeatures", "∅ Planetary Features", GalMapGroup.Markers, TestOpenTk.Properties.Resources.planetFeatures, false, index++));
            type.Add(new GalMapType("deepSpaceOutpost", "Deep space outpost", GalMapGroup.Markers, TestOpenTk.Properties.Resources.deepSpaceOutpost, false, index++));
            type.Add(new GalMapType("mysteryPOI", "Mystery POI", GalMapGroup.Markers, TestOpenTk.Properties.Resources.mysteryPOI, true, index++));
            type.Add(new GalMapType("restrictedSectors", "Restricted Sectors", GalMapGroup.Markers, TestOpenTk.Properties.Resources.restrictedSectors, true, index++));
            type.Add(new GalMapType("independentOutpost", "Independent Outpost", GalMapGroup.Markers, TestOpenTk.Properties.Resources.deepSpaceOutpost, false, index++));
            type.Add(new GalMapType("regional", "Regional Marker", GalMapGroup.Markers, TestOpenTk.Properties.Resources.Regional, true, index++));
            type.Add(new GalMapType("geyserPOI", "Geyser", GalMapGroup.Markers, TestOpenTk.Properties.Resources.GeyserPOI, true, index++));
            type.Add(new GalMapType("organicPOI", "Organic Material", GalMapGroup.Markers, TestOpenTk.Properties.Resources.OrganicPOI, true, index++));
            type.Add(new GalMapType("EDSMUnknown", "EDSM other POI type", GalMapGroup.Markers, TestOpenTk.Properties.Resources.EDSMUnknown, true, index++));

            // not visual
            type.Add(new GalMapType("travelRoute", "Travel Route", GalMapGroup.Routes , null, true, index++));
            type.Add(new GalMapType("historicalRoute", "Historical Route", GalMapGroup.Routes , null, true, index++));
            type.Add(new GalMapType("minorRoute", "Minor Route", GalMapGroup.Routes, null, true, index++));
            type.Add(new GalMapType("neutronRoute", "Neutron highway", GalMapGroup.Routes, null, true, index++));
            type.Add(new GalMapType("region", "Region", GalMapGroup.Regions, null, true, index++));
            type.Add(new GalMapType("regionQuadrants", "Galactic Quadrants", GalMapGroup.Quadrants , null, true, index++));


            return type;
        }
    }
}