using GLOFC;
using GLOFC.GL4;
using GLOFC.Utils;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestOpenTk
{
    //SOLVED
    //Response 23/3/22:Re Orbital inclinations - I have checked with our StellarForge guy -
    //the Orbital inclinations are relative to the parent's orbital inclination, not the parent's axis tilt

    public class BodyInfo
    {
        public StarScan.ScanNode ScanNode { get; set; }
        public StarScan.ScanNode Parent { get; set; }      // or null
        public KeplerOrbitElements KeplerParameters { get; set; }
        public GLRenderDataWorldPositionColor orbitpos { get; set; }    // where the orbit centre is
        public GLRenderDataWorldPositionColor bodypos { get; set; } // where the body is
        public int Index { get; set; }
        public int ParentIndex { get; set; }

        static public void CreateInfoTree(StarScan.ScanNode sn, StarScan.ScanNode parent, int parentindex, double prevmasskg, List<BodyInfo> oilist)
        {
            KeplerOrbitElements kepler = null;

            if (sn.scandata != null && sn.scandata.nSemiMajorAxis.HasValue)
            {
                kepler = new KeplerOrbitElements(true,
                    sn.scandata.nSemiMajorAxis.Value,
                    sn.scandata.nEccentricity != null ? sn.scandata.nEccentricity.Value : 0,                    // protect against missing data
                    sn.scandata.nOrbitalInclination != null ? sn.scandata.nOrbitalInclination.Value : 0,
                    sn.scandata.nAscendingNode != null ? sn.scandata.nAscendingNode.Value : 0,
                    sn.scandata.nPeriapsis != null ? sn.scandata.nPeriapsis.Value : 0,
                    sn.scandata.nMeanAnomaly != null ? sn.scandata.nPeriapsis.Value : 0,
                    sn.scandata.EventTimeUTC.ToJulianDate()
                );
            }
            else
            {
                if ( parent != null )
                    System.Diagnostics.Debug.WriteLine($"{sn.OwnName} does not have kepler info");
            }

            BodyInfo oi = new BodyInfo();
            oi.KeplerParameters = kepler;
            oi.ScanNode = sn;
            oi.Index = oilist.Count;
            oi.ParentIndex = parentindex;
            oi.orbitpos = new GLRenderDataWorldPositionColor();
            oi.bodypos = new GLRenderDataWorldPositionColor();
            oilist.Add(oi);

            if (kepler != null)
            {
                if (prevmasskg == 0 && kepler.SemiMajorAxis > 0)
                {
                    kepler.CentralMass = kepler.CalculateMass(sn.scandata.nOrbitalPeriod.Value);
                }
                else
                    kepler.CentralMass = prevmasskg;
            }

            if (sn.Children != null)
            {
                foreach (var kvp in sn.Children)
                {
                    CreateInfoTree(kvp.Value, sn, oi.Index, sn.scandata?.nMassKG != null ? sn.scandata.nMassKG.Value : 0, oilist);
                }
            }
        }
    }

}
