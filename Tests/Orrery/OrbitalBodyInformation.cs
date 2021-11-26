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
    public class BodyInfo
    {
        public KeplerOrbitElements kepler;
        public GLRenderDataWorldPositionColor orbitpos;
        public StarScan.ScanNode node;
        public StarScan.ScanNode parent;
        public int index;
        public int parentindex;

        static public void CreateInfoTree(StarScan.ScanNode sn, StarScan.ScanNode parent, int p, double prevmasskg, List<BodyInfo> oilist)
        {
            KeplerOrbitElements kepler = null;

            if (sn.scandata != null && sn.scandata.nSemiMajorAxis.HasValue)
            {
                kepler = new KeplerOrbitElements(true,
                    sn.scandata.nSemiMajorAxis.Value,
                    sn.scandata.nEccentricity.Value,
                    sn.scandata.nOrbitalInclination.Value,
                    sn.scandata.nAscendingNode.Value,
                    sn.scandata.nPeriapsis.Value,
                    sn.scandata.nMeanAnomaly.Value,
                    sn.scandata.EventTimeUTC.ToJulianDate()
                );
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"{sn.OwnName} does not have kepler info");
            }

            BodyInfo oi = new BodyInfo();
            oi.kepler = kepler;
            oi.node = sn;
            oi.index = oilist.Count;
            oi.parentindex = p;
            oi.orbitpos = new GLRenderDataWorldPositionColor();
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
                    CreateInfoTree(kvp.Value, sn, oi.index, sn.scandata?.nMassKG != null ? sn.scandata.nMassKG.Value : 0, oilist);
                }
            }
        }
    }

}
