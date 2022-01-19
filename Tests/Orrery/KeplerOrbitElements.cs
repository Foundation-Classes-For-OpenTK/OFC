/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
 * Part of the EDDiscovery Project
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
 */

using GLOFC;
using GLOFC.Utils;
using OpenTK;
using System;
using System.Collections.Generic;

namespace TestOpenTk
{
    public class KeplerOrbitElements
    {
        // keplarian parameters
        public double SemiMajorAxis { get; set; }       // a (meters) the sum of the periapsis and apoapsis distances divided by two.
        public double Eccentricity { get; set; }        // e Eccentricity of orbit
        public double Inclination { get; set; }         // i (radians) Orbital inclination of orbit from the reference plane measured at the ascending node, radians
        public double LongitudeOfAscendingNode { get; set; }    // omega (radians) - angle where the orbit passes upward through the reference plane, normally measured from the reference vernal point of the system, radians
        public double ArgumentOfPeriapsis { get; set; } // w (radians) Argument of periapsis, angle where the periapsis (closest approach) occurs, radians
                                                        // also called the argument of perifocus or argument of pericentre
        public double MeanAnomalyAtT0 { get; set; }     // v (radians) where it is in its orbit, 0 - 2PI, at epoch T0. Mean Anomaly is not a true geometric angle, rather a linear value varying over orbital period
        public double T0 { get; set; }                  // epoch time in days where these values are valid at

        // Other info
        public double CentralMass { get; set; } = 1;    // in KG.  Needed only if your going to use the OrbitPeriod, ToCartesian or Orbit functions
     
        public double OrbitalPeriodS { get { return 2 * Math.PI * Math.Sqrt(SemiMajorAxis * SemiMajorAxis * SemiMajorAxis / GM); } } // seconds, keplers third law 

        public double CalculateMass(double orbitalperiodseconds)        // keplers third law backwards
        {
            return 4 * Math.PI * Math.PI * SemiMajorAxis * SemiMajorAxis * SemiMajorAxis / G / (orbitalperiodseconds * orbitalperiodseconds);
        }

        public Vector3d LastCartensianPosition { get; private set; } // set when ToCartesian is run

        public object Tag { get; set; }                 // for other info

        public double GM { get { return G * CentralMass; } }
        public const double G = 6.67430E-11;                   // in m3.kg-1.s-2, wiki https://en.wikipedia.org/wiki/Gravitational_constant
        public const double J2000 = 2451545.0;                 // equals January 1st 2000 at 12:00 noon (Horizons 2451545.000000000 = A.D. 2000-Jan-01 12:00:00.0000 TDB)

        // in km, degrees and days, as per horizons
        public KeplerOrbitElements(double semimajoraxis, double eccentricity, double inclination, double longitudeofascendingnode, double argumentofperiapsis,
                                    double meananomlalyT0, double T0)
        {
            this.Eccentricity = eccentricity;
            this.SemiMajorAxis = semimajoraxis * 1000;    // to m
            this.Inclination = ((inclination + 360) % 360).Radians();
            this.LongitudeOfAscendingNode = ((longitudeofascendingnode + 360) % 360).Radians();
            this.ArgumentOfPeriapsis = ((argumentofperiapsis + 360) % 360).Radians();
            this.MeanAnomalyAtT0 = ((meananomlalyT0 + 360) % 360).Radians();
            this.T0 = T0;
        }

        // in km, degrees and days, as per horizons. In form used by https://nssdc.gsfc.nasa.gov/planetary/factsheet/earthfact.html
        public KeplerOrbitElements(bool markerunused, double semimajoraxiskm, double eccentricity,
                                    double inclination, double longitudeofascendingnode, double longitudeofperihelion,
                                    double meanlongitude, double T0)
        {
            this.Eccentricity = eccentricity;
            this.SemiMajorAxis = semimajoraxiskm * 1000;    // to m
            this.Inclination = ((inclination + 360) % 360).Radians();

            this.LongitudeOfAscendingNode = ((longitudeofascendingnode + 360) % 360).Radians();

            // https://en.wikipedia.org/wiki/Longitude_of_the_periapsis
            // longitude of perihelion = longitudeofascendingnode + argumentofperiapsis (W' = omega+w)
            // therefore argumentofperiapsis = longitude of perihelion - longitudeofascendingnode

            this.ArgumentOfPeriapsis = ((longitudeofperihelion - longitudeofascendingnode + 720) % 360).Radians();

            // https://en.wikipedia.org/wiki/Mean_longitude
            // mean longitude = longitudeofperiphelion + meananomaly (I = W' + M)
            // verified against horizons orbit viewer from 2000/2020 the positions appear correct

            this.MeanAnomalyAtT0 = ((meanlongitude - longitudeofperihelion + 720 ) % 360).Radians();

            this.T0 = T0;
        }

        public double MeanAnomalyAtT(double tdays)
        {
            System.Diagnostics.Debug.Assert(CentralMass > 1);

            // see https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf

            // 1 Calculate Mean Anomaly at time T. The mean anomaly M is a mathematically convenient fictitious "angle" which varies linearly with time, but which does not correspond to a real geometric angle.

           // System.Diagnostics.Debug.WriteLine($"At {tdays} A={SemiMajorAxis:E8}m EC={Eccentricity.Degrees():E8} deg OP={OrbitalPeriodS / 60 / 60 / 24:0.##}");

            double MAt = MeanAnomalyAtT0;
            if (tdays != T0)     // if not at epoch. T is in days, convert to seconds
            {
                double f = Math.Sqrt(GM / Math.Pow(SemiMajorAxis, 3));
                double d = (tdays - T0) * 60 * 60 * 24;

                MAt = MeanAnomalyAtT0 + d * f;
                MAt = MAt % (Math.PI * 2);
            }

            return MAt;
        }

        // Calculate the Eccentric Anomaly given the mean anomaly
        public double EccentricAnomaly(double MAt)
        {
            //System.Diagnostics.Debug.WriteLine($"    MAt = {MAt.Degrees():E8} deg {MAt} radians");

            // 2 Calculate eccentric anomaly using Newton's method

            double EAt = MAt;       // eccentric anomaly changes the linear MA to take account of the eccentricity of the orbit
            {
                int maxIter = 30;
                double diff = EAt - Eccentricity * Math.Sin(EAt) - MAt;         // difference between current EAt(=MAt) given the eccentricity

                while (Math.Abs(diff) > 0.0000001f && maxIter-- > 0)
                {
                    EAt = EAt - diff / (1 - Eccentricity * Math.Cos(EAt));       // calculate EAnext = E - (E-eSinE-m) / (1-eCosE), 1-eCosE is the differential of the diff eq. above
                    EAt %= Math.PI*2;           // Keep EAt to within 2PI
                    diff = EAt - Eccentricity * Math.Sin(EAt) - MAt;   // recalc diff, until it gets small
                }
            }

            return EAt;
        }

        public double TrueAnomaly(double EAt)
        {
            // 3 Calculate the true anomaly - the true angle around the orbit - verified against Horizons MA->EA

            double TAt = 2 * Math.Atan2(Math.Sqrt(1 + Eccentricity) * Math.Sin(EAt / 2), Math.Sqrt(1 - Eccentricity) * Math.Cos(EAt / 2));

            //System.Diagnostics.Debug.WriteLine($"    TAt = {TAt.Degrees():E8} deg {TAt} radians");  

            return TAt;
        }

        public double DistanceAtT(double tdays)
        {
            double MAt = MeanAnomalyAtT(tdays);
            double EAt = EccentricAnomaly(MAt);
            double rct = SemiMajorAxis * (1 - Eccentricity * Math.Cos(EAt));
            return rct;
        }

        // return position vector with orbit on xy plane in meters
        public Vector3d ToCartesian(double tdays)
        {
            double MAt = MeanAnomalyAtT(tdays);
            double EAt = EccentricAnomaly(MAt);
            double TAt = TrueAnomaly(EAt);

            // 4 Calculate distance to central body

            double rct = SemiMajorAxis * (1 - Eccentricity * Math.Cos(EAt));

            //5 Get position vector (z-axis perpendicular to orbital plane, x-axis pointing to periapsis of the orbit)

            Vector3d ot = new Vector3d(rct * Math.Cos(TAt), rct * Math.Sin(TAt), 0);

            // 6 Transform to the inertial frame in bodycentric.

            double rx = (ot.X * (Math.Cos(ArgumentOfPeriapsis) * Math.Cos(LongitudeOfAscendingNode) - Math.Sin(ArgumentOfPeriapsis) * Math.Cos(Inclination) * Math.Sin(LongitudeOfAscendingNode)) -
                    ot.Y * (Math.Sin(ArgumentOfPeriapsis) * Math.Cos(LongitudeOfAscendingNode) + Math.Cos(ArgumentOfPeriapsis) * Math.Cos(Inclination) * Math.Sin(LongitudeOfAscendingNode)));
            double ry = (ot.X * (Math.Cos(ArgumentOfPeriapsis) * Math.Sin(LongitudeOfAscendingNode) + Math.Sin(ArgumentOfPeriapsis) * Math.Cos(Inclination) * Math.Cos(LongitudeOfAscendingNode)) +
                ot.Y * (Math.Cos(ArgumentOfPeriapsis) * Math.Cos(Inclination) * Math.Cos(LongitudeOfAscendingNode) - Math.Sin(ArgumentOfPeriapsis) * Math.Sin(LongitudeOfAscendingNode)));
            double rz = (ot.X * (Math.Sin(ArgumentOfPeriapsis) * Math.Sin(Inclination)) + ot.Y * (Math.Cos(ArgumentOfPeriapsis) * Math.Sin(Inclination)));

            LastCartensianPosition = new Vector3d(rx, ry, rz); //Position vector in meters
                                                   //  System.Diagnostics.Debug.WriteLine($"Result {r}");

            return LastCartensianPosition;
        }


        // return vector path of orbit, in GL format, on XZ plane, given the day start, day resolution (ie. 2 means every two days), and scaling to GL units
        public Vector4[] Orbit(double tdays, double angleresolutiondeg, double scaling)
        {
            double orbitalperioddays = OrbitalPeriodS / 60 / 60 / 24;
         //   System.Diagnostics.Debug.WriteLine($"Orbit {OrbitalPeriodS} = {orbitalperioddays} days res {angleresolutiondeg} {CentralMass}");

            List<Vector4> ret = new List<Vector4>();
            for ( double a = 0; a <= 360.0;  a = a +angleresolutiondeg)
            {
                double t = tdays + orbitalperioddays / 360.0 * a;
                Vector3d posd = ToCartesian(t);
                ret.Add( new Vector4((float)(posd.X * scaling), (float)(posd.Z * scaling), (float)(posd.Y * scaling), 1) );
            }

            return ret.ToArray();
        }

    }

}
