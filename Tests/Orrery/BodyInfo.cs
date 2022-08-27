using GLOFC.GL4;
using GLOFC.Utils;
using System.Collections.Generic;

namespace TestOpenTk
{
    public class BodyInfo
    {
        public StarScan.ScanNode ScanNode { get; set; }
        public StarScan.ScanNode Parent { get; set; }      // or null
        public KeplerOrbitElements KeplerParameters { get; set; }
        public GLRenderDataWorldPositionColor orbitpos { get; set; }    // where the orbit centre is
        public int Index { get; set; }
        public int ParentIndex { get; set; }

        // from subnode, create a bodyinfo, add to bodylist, and then recurse thru its children
        static public void CreateInfoList(List<BodyInfo> bodylist, StarScan.ScanNode sn, StarScan.ScanNode parent, int parentindex, 
                            double parentmasskg, double parentinclination)
        {
            KeplerOrbitElements kepler = null;

            double inclination = parentinclination;

            bool includebody = false;

            if (sn.scandata != null && sn.scandata.nSemiMajorAxis.HasValue)
            {
                double orbitingmass;

                // if we have these, use these to calc mass, more accurate than elite masses due to rounding
                if (sn.scandata.nSemiMajorAxis.HasValue && sn.scandata.nSemiMajorAxis > 0 && sn.scandata.nOrbitalPeriod.HasValue) 
                {
                    orbitingmass = KeplerOrbitElements.CalculateMassKG(sn.scandata.nSemiMajorAxis.Value, sn.scandata.nOrbitalPeriod.Value);
                }
                else
                {
                    orbitingmass = parentmasskg;
                    if (sn.scandata.nMassKG.HasValue)      // Elite seems to use 2 body mass - parent and body, only..
                        orbitingmass += sn.scandata.nMassKG.Value;

                    if (sn.scandata.nSemiMajorAxis < 0)      // if <0, this means we want the orbital period to set the SMA, for debugging purposes only
                    {
                        sn.scandata.nSemiMajorAxis = KeplerOrbitElements.CalculateSMAm(sn.scandata.nOrbitalPeriod.Value, orbitingmass) * 1;
                    }
                }

                // inclunation, according to frontier, is just added thru heirachy, and not affected by axial tilt
                inclination += (sn.scandata.nOrbitalInclination != null ? sn.scandata.nOrbitalInclination.Value : 0);

                kepler = new KeplerOrbitElements(true,
                    sn.scandata.nSemiMajorAxisKM.Value,
                    sn.scandata.nEccentricity != null ? sn.scandata.nEccentricity.Value : 0,                    // protect against missing data
                    inclination,
                    sn.scandata.nAscendingNode != null ? sn.scandata.nAscendingNode.Value: 0,
                    sn.scandata.nPeriapsis != null ? sn.scandata.nPeriapsis.Value : 0,
                    sn.scandata.nMeanAnomaly != null ? (sn.scandata.nMeanAnomaly.Value) : 0,
                    sn.scandata.EventTimeUTC.ToJulianDate()
                );

                kepler.OrbitingMass = orbitingmass;

                System.Diagnostics.Debug.WriteLine(
                    $"BodyInfo {sn.FullName} SMA {kepler.SemiMajorAxism/1000.0} vs {KeplerOrbitElements.CalculateSMAm(sn.scandata.nOrbitalPeriod.Value, orbitingmass) / 1000.0} km " +
                    $" | Period {kepler.OrbitalPeriods} vs CalcOP {KeplerOrbitElements.CalculateOrbitalPeriods(sn.scandata.nSemiMajorAxis.Value, orbitingmass)} s" + 
                    $" | Mass {kepler.OrbitingMass} vs {KeplerOrbitElements.CalculateMassKG(sn.scandata.nSemiMajorAxis.Value, sn.scandata.nOrbitalPeriod.Value)} kg" +
                    $" | Summed Inclination {kepler.Inclinationr.Degrees()} | Axial Eclipic Tilt {(sn.scandata.nAxialTilt.HasValue ? sn.scandata.nAxialTilt.Value.Degrees():-9999)}");

                includebody = true;
            }
            else
            {
                if (sn.BodyID == 0)       // 0, is allowed to have no data
                    includebody = true;
            }

            if (includebody)
            {
                BodyInfo oi = new BodyInfo();
                oi.KeplerParameters = kepler;
                oi.ScanNode = sn;
                oi.Index = bodylist.Count;
                oi.ParentIndex = parentindex;
                oi.orbitpos = new GLRenderDataWorldPositionColor();
                bodylist.Add(oi);

                if (sn.Children != null)
                {
                    foreach (var kvp in sn.Children)
                    {
                        CreateInfoList(bodylist, kvp.Value, sn, oi.Index, sn.scandata?.nMassKG != null ? sn.scandata.nMassKG.Value : 0, inclination);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"{sn.OwnName} does not have kepler info so ignore it and its children");
            }
        }
    }

}
