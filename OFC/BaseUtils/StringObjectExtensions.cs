/*
 * Copyright © 2016-2021 Robbyxp1 @ github.com
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
 * 
 */

using System;

namespace GLOFC
{
    public static class ObjectExtensionsStrings
    {
        static public bool HasChars(this string obj)
        {
            return obj != null && obj.Length > 0;
        }
        static public bool IsEmpty(this string obj)
        {
            return obj == null || obj.Length == 0;
        }

        public static string ToNullSafeString(this object obj)
        {
            return (obj ?? string.Empty).ToString();
        }

        public static string ToNANSafeString(this double obj, string format)
        {
            return (obj != double.NaN) ? obj.ToString(format) : string.Empty;
        }

        public static string ToNANNullSafeString(this double? obj, string format)
        {
            return (obj.HasValue && obj != double.NaN) ? obj.Value.ToString(format) : string.Empty;
        }

        public static string Left(this string obj, int length)      // obj = null, return "".  Length can be > string
        {
            if (obj != null)
            {
                if (length < obj.Length)
                    return obj.Substring(0, length);
                else
                    return obj;
            }
            else
                return string.Empty;
        }

        public static string Left(this string obj, string match, StringComparison cmp = StringComparison.CurrentCulture, bool allifnotthere = false)
        {
            if (obj != null && obj.Length > 0)
            {
                int indexof = obj.IndexOf(match, cmp);
                if (indexof == -1)
                    return allifnotthere ? obj : "";
                else
                    return obj.Substring(0, indexof);
            }
            else
                return string.Empty;
        }

        public static string Mid(this string obj, int start, int length = 999999)      // obj = null, return "".  Mid, start/length can be out of limits
        {
            if (obj != null)
            {
                if (start < obj.Length)        // if in range
                {
                    int left = obj.Length - start;      // what is left..
                    return obj.Substring(start, Math.Min(left, length));    // min of left, length
                }
            }

            return string.Empty;
        }

        public static string Mid(this string obj, string match, StringComparison cmp = StringComparison.CurrentCulture, bool allifnotthere = false)
        {
            if (obj != null && obj.Length > 0)
            {
                int indexof = obj.IndexOf(match, cmp);
                if (indexof == -1)
                    return allifnotthere ? obj : "";
                else
                    return obj.Substring(indexof);
            }
            else
                return string.Empty;
        }

        public static bool Contains(this string data, string comparision, StringComparison c = StringComparison.CurrentCulture)        //extend for case
        {
            return data.IndexOf(comparision, c) >= 0;
        }


        public static string AppendPrePad(this string sb, string other, string prepad = " ")
        {
            if (other != null && other.Length > 0)
            {
                if (sb.Length > 0)
                    sb += prepad;
                sb += other;
            }
            return sb;
        }

        public static string RegExWildCardToRegular(this string value)
        {
            if (value.Contains("*") || value.Contains("?"))
                return "^" + System.Text.RegularExpressions.Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
            else
                return "^" + value + ".*$";
        }

        public static bool WildCardMatch(this string value, string match)
        {
            match = match.RegExWildCardToRegular();
            return System.Text.RegularExpressions.Regex.IsMatch(value, match);
        }

        public static string EscapeControlChars(this string obj)
        {
            string s = obj.Replace(@"\", @"\\");        // order vital
            s = obj.Replace("\r", @"\r");
            return s.Replace("\n", @"\n");
        }
    }
}
