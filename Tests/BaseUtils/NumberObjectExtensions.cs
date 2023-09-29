/*
 * Copyright 2023 Robbyxp1 @ github.com
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

using GLOFC.Utils;
using System.Drawing;
using System.Web.UI.DataVisualization.Charting;

public static class NumberObjectExtensions
{
    /// <summary>
    /// To string, invariant, with separator
    /// </summary>
    /// <param name="v">Value</param>
    /// <param name="separ">Character uses as vector parts delimiter</param>
    /// <returns>Invariant string of values</returns>
    public static string ToStringInvariant(this Point3D v, char separ = ',')
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1}{2}{3}{4}", v.X, separ, v.Y, separ, v.Z);
    }

    /// <summary>
    /// To string, invariant, with separator
    /// </summary>
    /// <param name="v">Value</param>
    /// <param name="separ">Character uses as vector parts delimiter</param>
    /// <returns>Invariant string of values</returns>
    public static string ToStringInvariant(this PointF v, char separ = ',')
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1}{2}", v.X, separ, v.Y);
    }

    /// <summary>
    /// Parse a string for a vector 3 invariant
    /// </summary>
    /// <param name="s">string</param>
    /// <param name="separ">Character uses as vector parts delimiter</param>
    /// <returns>Point3D or null </returns>
    public static Point3D InvariantParsePoint3D(this string s, char separ = ',')
    {
        string[] sl = s.Split(separ);
        if (sl.Length == 3)
        {
            float? x = sl[0].InvariantParseFloatNull();
            float? y = sl[1].InvariantParseFloatNull();
            float? z = sl[2].InvariantParseFloatNull();
            if (x != null && y != null && z != null)
                return new Point3D(x.Value, y.Value, z.Value);
        }

        return null;
    }

    /// <summary>
    /// Parse a string for a vector 2 invariant
    /// </summary>
    /// <param name="s">string</param>
    /// <param name="separ">Character uses as vector parts delimiter</param>
    /// <returns>Point3D or null </returns>
    public static PointF? InvariantParsePointF(this string s, char separ = ',')
    {
        string[] sl = s.Split(separ);
        if (sl.Length == 2)
        {
            float? x = sl[0].InvariantParseFloatNull();
            float? y = sl[1].InvariantParseFloatNull();
            if (x != null && y != null)
                return new PointF(x.Value, y.Value);
        }

        return null;
    }

}

