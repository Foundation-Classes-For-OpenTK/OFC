using GLOFC;
using GLOFC.GL4;
using GLOFC.Utils;
using QuickJSON;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOpenTk
{
    public class BodyPhysicalConstants
    {
        // stellar references
        public const double oneSolRadius_m = 695700000; // 695,700km

        // planetary bodies
        public const double oneEarthRadius_m = 6371000;
        public const double oneAtmosphere_Pa = 101325;
        public const double oneGee_m_s2 = 9.80665;
        public const double oneSol_KG = 1.989e30;
        public const double oneEarth_KG = 5.972e24;
        public const double oneMoon_KG = 7.34767309e22;
        public const double oneEarthMoonMassRatio = oneEarth_KG / oneMoon_KG;

        // astrometric
        public const double oneLS_m = 299792458;
        public const double oneAU_m = 149597870700;
        public const double oneAU_LS = oneAU_m / oneLS_m;
        public const double oneDay_s = 86400;
    }

    public class JournalScan
    {
        public bool IsStar { get { return StarType != null; } }
        public bool IsBeltCluster { get { return StarType == null && PlanetClass == null; } }
        public bool IsPlanet { get { return PlanetClass != null; } }

        public string BodyName { get; private set; }                        // direct (meaning no translation)
        public int BodyID { get; private set; }

        public double? nRadius { get; set; }                        // direct (m)

        public string StarType { get; private set; }                        // null if no StarType, direct from journal, K, A, B etc
        public double? nStellarMass { get; private set; }                   // direct

        public double? nSemiMajorAxis { get; set; }                 // direct, m
        public double? nSemiMajorAxisAU { get { if (nSemiMajorAxis.HasValue) return nSemiMajorAxis.Value / BodyPhysicalConstants.oneAU_m; else return null; } }
        public double? nSemiMajorAxisKM { get { if (nSemiMajorAxis.HasValue) return nSemiMajorAxis.Value / 1000.0; else return null; } }
        public string SemiMajorAxisLSKM { get { return nSemiMajorAxis.HasValue ? (nSemiMajorAxis >= BodyPhysicalConstants.oneLS_m / 10 ? ((nSemiMajorAxis.Value / BodyPhysicalConstants.oneLS_m).ToString("N1") + "ls") : ((nSemiMajorAxis.Value / 1000).ToString("N0") + "km")) : ""; } }

        public double? nEccentricity { get; private set; }                  // direct
        public double? nOrbitalInclination { get; private set; }            // direct, degrees
        public double? nPeriapsis { get; private set; }                     // direct, degrees
        public double? nOrbitalPeriod { get; private set; }                 // direct, seconds

        public double? nOrbitalPeriodDays { get { if (nOrbitalPeriod.HasValue) return nOrbitalPeriod.Value / BodyPhysicalConstants.oneDay_s; else return null; } }
        public double? nAscendingNode { get; private set; }                  // odyssey update 7 22/9/21, degrees
        public double? nMeanAnomaly { get; private set; }                    // odyssey update 7 22/9/21, degrees

        public double? nMassEM { get; private set; }                        // direct, not in description of event, mass in EMs

        public double? nMassKG { get { return IsPlanet ? nMassEM * BodyPhysicalConstants.oneEarth_KG : nStellarMass * BodyPhysicalConstants.oneSol_KG; } }

        public double? nAxialTilt { get; private set; }                     // direct, radians
        public double? nAxialTiltDeg { get { if (nAxialTilt.HasValue) return nAxialTilt.Value * 180.0 / Math.PI; else return null; } }
        public bool? nTidalLock { get; private set; }                       // direct
        public double? nRotationPeriod { get; private set; }                // direct, can be negative indi

        public string PlanetClass { get; private set; }                     // planet class, direct. If belt cluster, null. Try to avoid. Not localised

        public DateTime EventTimeUTC;

        public StarPlanetRing[] Rings { get; set; }


        public JournalScan(string name, int bodyid, string sc, string pc, 
            double? mass, double? op, double? axialtilt, double? radius, double? rotperiod,
            double? s, double? e, double? i, double? an, double? p, double? ma, DateTime t
            )
        {
            BodyName = name;
            BodyID = bodyid;
            StarType = sc;
            PlanetClass = pc;

            if (StarType.HasChars())        // convert kg to relevant EM or Sol
                nStellarMass = mass / BodyPhysicalConstants.oneSol_KG;
            else
                nMassEM = mass / BodyPhysicalConstants.oneEarth_KG;
            
            nOrbitalPeriod = op;
            nAxialTilt = axialtilt;
            nRadius = radius;
            nRotationPeriod = rotperiod;

            nSemiMajorAxis = s;
            nEccentricity = e;
            nOrbitalInclination = i;
            nAscendingNode = an;
            nPeriapsis = p;
            nMeanAnomaly = ma;
            EventTimeUTC = t;
        }

        public string DisplayString()
        {
            return "Would display info on system";
        }
    }

    public class StarPlanetRing
    {
        public enum RingClassEnum { Unknown, Rocky, Metalic, Icy, MetalRich }
        public RingClassEnum RingClassID { get; set; }      // Default will be unknown

        public double MassMT { get; set; }
        public double InnerRad { get; set; }
        public double OuterRad { get; set; }
        public double Width { get { return OuterRad - InnerRad; } }
    }

    public partial class StarScan
    {
        public enum ScanNodeType
        {
            star,            // used for top level stars - stars around stars are called a body.
            barycentre,      // only used for top level barycentres (AB)
            body,            // all levels >0 except for below are called a body
            belt,            // used on level 1 under the star : HIP 23759 A Belt Cluster 4 -> elements Main Star,A Belt,Cluster 4
            beltcluster,     // each cluster under it gets this name at level 2
            ring             // rings at the last level : Selkana 9 A Ring : MainStar,9,A Ring
        };

        [System.Diagnostics.DebuggerDisplay("SN {FullName} {NodeType} lv {Level} bid {BodyID}")]
        public partial class ScanNode
        {
            public ScanNodeType NodeType { get; set;}
            public string FullName { get; set;}                 // full name including star system
            public string OwnName { get; set;}                  // own name excluding star system
            public SortedList<string, ScanNode> Children { get; set;}         // kids
            public int Level { get; set;}                       // level within SystemNode
            public int? BodyID { get; set;}
            public bool IsMapped { get; set;}                   // recorded here since the scan data can be replaced by a better version later.
            public bool WasMappedEfficiently { get; set;}

            public JournalScan scandata;            // can be null if no scan, its a place holder, else its a journal scan
        };


        static public ScanNode ReadJSON(JObject jo)
        {
            string time = jo["Epoch"].Str();
            DateTime epoch = time.HasChars() ? DateTime.Parse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal) : DateTime.UtcNow;

            ScanNode n = new ScanNode();

            n.OwnName = jo["Name"].Str();
            n.FullName = jo["FullName"].Str(n.OwnName);

            if (Enum.TryParse<ScanNodeType>(jo["NodeType"].Str("body"), out ScanNodeType ty))
            {
                double? axialtilt = jo["AxialTilt"].DoubleNull(); //degrees
                if (axialtilt != null)
                    axialtilt = axialtilt.Value * (Math.PI / 180.0); // to radians

                double? radius = jo["Radius"].DoubleNull();
                if (radius != null)
                    radius = radius.Value * 1000;   // to m

                double? sma = jo["SemiMajorAxis"].DoubleNull();
                if (sma != null)
                    sma = sma.Value * 1000; // to m

                n.NodeType = ty;
                n.scandata = new JournalScan(n.OwnName, jo["ID"].Int(0),
                                        jo["StarType"].StrNull(),
                                        jo["PlanetClass"].StrNull(), 
                                        jo["Mass"].DoubleNull(),        // kg
                                        jo["OrbitalPeriod"].DoubleNull(),   // sec
                                        axialtilt,
                                        radius,
                                        jo["RotationPeriod"].DoubleNull(),
                                        sma,
                                        jo["Eccentricity"].DoubleNull(),
                                        jo["Inclination"].DoubleNull(),        // deg all
                                        jo["AscendingNode"].DoubleNull(),   // deg
                                        jo["Periapis"].DoubleNull(),    // deg
                                        jo["MeanAnomaly"].DoubleNull(), // deg
                                        epoch);

                JArray rings = jo["Rings"].Array();
                if ( rings != null)
                {
                    n.scandata.Rings = new StarPlanetRing[rings.Count];
                    for(int  i = 0; i < rings.Count; i++)
                    {
                        n.scandata.Rings[i] = new StarPlanetRing() { 
                            RingClassID = rings[i]["Type"].EnumStr<StarPlanetRing.RingClassEnum>(StarPlanetRing.RingClassEnum.Unknown),
                            InnerRad = rings[i]["InnerRad"].Double() * 1000.0,      // in mk in file, m in program
                            OuterRad = rings[i]["OuterRad"].Double() * 1000.0,
                            MassMT = rings[i]["MassMT"].Double(),
                        };
                    }

                }

              //  System.Diagnostics.Debug.WriteLine($"Make scandata {n.FullName} SMA {n.scandata.nSemiMajorAxisKM} km OP {n.scandata.nOrbitalPeriod} s");

                if (jo.Contains("Bodies"))
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
            else
                return null;

        }
    }
    
}
