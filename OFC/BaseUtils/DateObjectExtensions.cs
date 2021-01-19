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

namespace BaseUtils
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

    }
}
