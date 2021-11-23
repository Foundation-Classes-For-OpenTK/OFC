using GLOFC;
using GLOFC.GL4;
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
    public class JournalScan
    {
        public bool IsStar { get { return StarType != null; } }
        public bool IsBeltCluster { get { return StarType == null && PlanetClass == null; } }
        public bool IsPlanet { get { return PlanetClass != null; } }

        public string BodyName { get; private set; }                        // direct (meaning no translation)

        public double? nRadius { get; private set; }                        // direct (m)

        public string StarType { get; private set; }                        // null if no StarType, direct from journal, K, A, B etc
        public double? nStellarMass { get; private set; }                   // direct

        public double? nSemiMajorAxis { get; private set; }                 // direct, m
        public double? nSemiMajorAxisAU { get { if (nSemiMajorAxis.HasValue) return nSemiMajorAxis.Value / oneAU_m; else return null; } }
        public string SemiMajorAxisLSKM { get { return nSemiMajorAxis.HasValue ? (nSemiMajorAxis >= oneLS_m / 10 ? ((nSemiMajorAxis.Value / oneLS_m).ToString("N1") + "ls") : ((nSemiMajorAxis.Value / 1000).ToString("N0") + "km")) : ""; } }

        public double? nEccentricity { get; private set; }                  // direct
        public double? nOrbitalInclination { get; private set; }            // direct, degrees
        public double? nPeriapsis { get; private set; }                     // direct, degrees
        public double? nOrbitalPeriod { get; private set; }                 // direct, seconds
        public double? nOrbitalPeriodDays { get { if (nOrbitalPeriod.HasValue) return nOrbitalPeriod.Value / oneDay_s; else return null; } }
        public double? nAscendingNode { get; private set; }                  // odyssey update 7 22/9/21, degrees
        public double? nMeanAnomaly { get; private set; }                    // odyssey update 7 22/9/21, degrees

        public double? nMassEM { get; private set; }                        // direct, not in description of event, mass in EMs

        public double? nMassKG { get { return IsPlanet ? nMassEM * oneEarth_KG : nStellarMass * oneSOL_KG; } }

        public double? nAxialTilt { get; private set; }                     // direct, radians
        public double? nAxialTiltDeg { get { if (nAxialTilt.HasValue) return nAxialTilt.Value * 180.0 / Math.PI; else return null; } }
        public bool? nTidalLock { get; private set; }                       // direct

        public string PlanetClass { get; private set; }                     // planet class, direct. If belt cluster, null. Try to avoid. Not localised

        public const double oneSolRadius_m = 695700000; // 695,700km

        public DateTime EventTimeUTC;
        // planetary bodies
        public const double oneSOLRadius_m = 695700 * 1000;
        public const double oneEarthRadius_m = 6371000;
        public const double oneAtmosphere_Pa = 101325;
        public const double oneGee_m_s2 = 9.80665;
        public const double oneSOL_KG = 1.989e30;
        public const double oneEarth_KG = 5.972e24;
        public const double oneMoon_KG = 7.34767309e22;
        public const double EarthMoonMassRatio = oneEarth_KG / oneMoon_KG;

        // astrometric
        public const double oneLS_m = 299792458;
        public const double oneAU_m = 149597870700;
        public const double oneAU_LS = oneAU_m / oneLS_m;
        public const double oneDay_s = 86400;

        public JournalScan(string name, string sc, string pc, double mass, double op, double axialtiltdeg, double radius,
            double s,double e,double i,double an,double p,double ma, DateTime t
            )
        {
            BodyName = name;
            StarType = sc;
            PlanetClass = pc;
            if (StarType.HasChars())
                nStellarMass = mass / oneSOL_KG;
            else
                nMassEM = mass / oneEarth_KG;
            nOrbitalPeriod = op;
            nAxialTilt = axialtiltdeg;
            nRadius = radius;
            nSemiMajorAxis = s;
            nEccentricity = e;
            nOrbitalInclination = i;
            nAscendingNode = an;
            nPeriapsis = p;
            nMeanAnomaly = ma;
            EventTimeUTC = t;
        }
    }


    public partial class StarScan
    {
        public enum ScanNodeType { star, barycentre, body, belt, beltcluster, ring };
        public partial class ScanNode
        {
            public ScanNodeType NodeType;
            public string FullName;                 // full name including star system
            public string OwnName;                  // own name excluding star system
            public string CustomName;               // if we can extract from the journals a custom name of it, this holds it. Mostly null
            public SortedList<string, ScanNode> Children;         // kids
            public int Level;                       // level within SystemNode
            public int? BodyID;
            public bool IsMapped;                   // recorded here since the scan data can be replaced by a better version later.
            public bool WasMappedEfficiently;

            public string CustomNameOrOwnname { get { return CustomName ?? OwnName; } }

            public JournalScan scandata;            // can be null if no scan, its a place holder, else its a journal scan
        };


        static public ScanNode ReadJSON(JObject jo)
        {
            string time = jo["Epoch"].Str();
            DateTime epoch = time.HasChars() ? DateTime.Parse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal) : DateTime.UtcNow;

            ScanNode n = new ScanNode();

            n.OwnName = jo["Name"].Str();
            n.FullName = jo["FullName"].Str();
            string nt = jo["NodeType"].Str();
            n.NodeType = nt.Equals("barycentre") ? ScanNodeType.barycentre : nt.Equals("body") ? ScanNodeType.body : ScanNodeType.star;

            n.scandata = new JournalScan(n.OwnName, jo["StarClass"].StrNull(), jo["PlanetClass"].StrNull(), jo["Mass"].Double(0),
                                        jo["OrbitalPeriod"].Double(0), jo["AxialTilt"].Double(0), jo["Radius"].Double(0) * 1000,
                jo["SemiMajorAxis"].Double(0),        // km
                jo["Eccentricity"].Double(0),
                jo["Inclination"].Double(0),        // deg all
                jo["AscendingNode"].Double(0),
                jo["Periapis"].Double(0),
                jo["MeanAnomaly"].Double(0),
                epoch
                                            );

            if (jo.ContainsKey("Bodies"))
            {
                n.Children = new SortedList<string, ScanNode>();
                JArray ja = jo["Bodies"] as JArray;
                foreach (JObject o in ja)
                {
                    var cn = ReadJSON(o);
                    n.Children.Add(cn.OwnName, cn);
                }
            }

            return n;
        }
    }
    
}
