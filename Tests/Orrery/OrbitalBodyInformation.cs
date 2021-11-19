using GLOFC;
using Newtonsoft.Json.Linq;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOpenTk
{
    public class OrbitalBodyInformation
    {
        public string Name { get; set; }                // for naming
        public string FullName { get; set; }                // for naming
        public string NodeType { get; set; }                // for naming
        public string StarClass { get; set; }                // for naming
        public string PlanetClass { get; set; }                // for naming
        public Vector3d CalculatedPosition { get; set; }    // used during calculation
        public int CentralBodyIndex { get; set; }            // central body reference
        public double Mass { get; set; } = 1;        // in KG.  Not needed for orbital parameters
        public double OrbitalPeriod { get; set; }
        public double AxialTiltDeg { get; set; }
        public double RadiusKm { get; set; }          // in km

        public static KeplerOrbitElements Read(JObject json)
        {
            string time = json["Epoch"].Str();
            DateTime epoch = DateTime.Parse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
     
            KeplerOrbitElements k = new KeplerOrbitElements(true,
                    json["SemiMajorAxis"].Double(0),        // km
                    json["Eccentricity"].Double(0),
                    json["Inclination"].Double(0),
                    json["AscendingNode"].Double(0),
                    json["Periapis"].Double(0),
                    json["MeanAnomaly"].Double(0),
                    epoch.ToJulianDate()
                );
            
            OrbitalBodyInformation ai = new OrbitalBodyInformation()
            {
                Name = json["Name"].Str(),
                FullName = json["FullName"].Str(),
                NodeType = json["NodeType"].Str(),
                StarClass = json["StarClass"].StrNull(),
                PlanetClass = json["PlanetClass"].StrNull(),
                Mass = json["Mass"].Double(0),
                OrbitalPeriod = json["OrbitalPeriod"].Double(0),
                AxialTiltDeg = json["AxialTilt"].Double(0),
                RadiusKm = json["Radius"].Double(0),
            };
            k.Tag = ai;
            return k;
        }

        // jo is the EDD standard body orbital parameter output
        // recurses in to create additional bodies if Bodies is present
        public static void AddToBodyList(JObject jo, List<KeplerOrbitElements> bodylist, double prevmass, int index)
        {
            var kepler = Read(jo);
            OrbitalBodyInformation ai = kepler.Tag as OrbitalBodyInformation;
            if (prevmass == 0 && kepler.SemiMajorAxis > 0)
            {
                kepler.CentralMass = kepler.CalculateMass(ai.OrbitalPeriod);
            }
            else
                kepler.CentralMass = prevmass;

            ai.CentralBodyIndex = index;

            index = bodylist.Count;
            bodylist.Add(kepler);

            if (jo.ContainsKey("Bodies"))
            {
                JArray ja = jo["Bodies"] as JArray;
                foreach (var o in ja)
                {
                    AddToBodyList(o as JObject, bodylist, ai.Mass, index);
                }
            }
        }


    }

}
