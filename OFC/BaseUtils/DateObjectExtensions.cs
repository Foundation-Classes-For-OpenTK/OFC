/*
 * Copyright © 2021 Robbyp @ github.org
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

namespace GLOFC
{
    public static class ObjectExtensionsDates
    {
        public static DateTime SafeAddDays(this DateTime v, int n)
        {
            try
            {
                return v.AddDays(n);
            }
            catch
            {
                return v;
            }
        }

        public static DateTime SafeAddMonths(this DateTime v, int n)
        {
            try
            {
                return v.AddMonths(n);
            }
            catch
            {
                return v;
            }
        }

        public static DateTime SafeAddYears(this DateTime v, int n)
        {
            try
            {
                return v.AddYears(n);
            }
            catch
            {
                return v;
            }
        }

        public static DateTime SafeAddSeconds(this DateTime v, int n)
        {
            try
            {
                return v.AddSeconds(n);
            }
            catch
            {
                return v;
            }
        }

        public static DateTime SafeAddHours(this DateTime v, int n)
        {
            try
            {
                return v.AddHours(n);
            }
            catch
            {
                return v;
            }
        }

        public static DateTime SafeAddMinutes(this DateTime v, int n)
        {
            try
            {
                return v.AddMinutes(n);
            }
            catch
            {
                return v;
            }
        }
        public static double ToJulianDate(this DateTime date)       // verified that horizons 2451545.000000000 = A.D. 2000-Jan-01 12:00:00.0000 TDB gives same value for 1/1/2000 12:0:0
        {
            return date.ToOADate() + 2415018.5;
        }
        public static DateTime JulianToDateTime(this double jd)       // verified that horizons 2451545.000000000 = A.D. 2000-Jan-01 12:00:00.0000 TDB gives same value for 1/1/2000 12:0:0
        {
            return DateTime.FromOADate(jd - 2415018.5);
        }

        // left and right can be null or not dates..

        static public int CompareDate(this string left, string right)
        {
            DateTime v1 = DateTime.MinValue, v2 = DateTime.MinValue;

            bool v1hasval = left != null && DateTime.TryParse(left, out v1);
            bool v2hasval = right != null && DateTime.TryParse(right, out v2);

            if (!v1hasval)
            {
                return 1;
            }
            else if (!v2hasval)
            {
                return -1;
            }
            else
            {
                return v1.CompareTo(v2);
            }
        }


    }
}
