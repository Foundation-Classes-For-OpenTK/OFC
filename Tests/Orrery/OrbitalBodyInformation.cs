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
        public GLRenderDataWorldPositionColor rd;
        public StarScan.ScanNode node;
        public StarScan.ScanNode parent;
        public int index;
        public int parentindex;

        static public void CreateInfoTree(StarScan.ScanNode sn, StarScan.ScanNode parent, int p, double prevmasskg, List<BodyInfo> oilist)
        {
            KeplerOrbitElements kepler = new KeplerOrbitElements(true,
                    sn.scandata.nSemiMajorAxis.Value,
                    sn.scandata.nEccentricity.Value,
                    sn.scandata.nOrbitalInclination.Value,
                    sn.scandata.nAscendingNode.Value,
                    sn.scandata.nPeriapsis.Value,
                    sn.scandata.nMeanAnomaly.Value,
                    sn.scandata.EventTimeUTC.ToJulianDate()
                );

            BodyInfo oi = new BodyInfo();
            oi.kepler = kepler;
            oi.node = sn;
            oi.index = oilist.Count;
            oi.parentindex = p;
            oilist.Add(oi);

            if (prevmasskg == 0 && kepler.SemiMajorAxis > 0)
            {
                kepler.CentralMass = kepler.CalculateMass(sn.scandata.nOrbitalPeriod.Value);
            }
            else
                kepler.CentralMass = prevmasskg;

            oi.rd = new GLRenderDataWorldPositionColor();

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
