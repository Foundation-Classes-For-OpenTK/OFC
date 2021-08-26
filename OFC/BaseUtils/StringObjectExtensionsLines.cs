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
    public static class ObjectExtensionsLineStrings
    {
        // outputstart = 1 to N, if less than 1, removes it off count. (so -1 count 3 only produces line 1 (-1,0 removed))
        // count = number of lines to show
        // markline = mark line number N (1..)
        // startlineno = logical number to add onto line number in case its come from a split string
        static public string LineMarking(this string s, int outputstart, int count, string lineformat = null, int markline = -1, int startlinenooffset = 0, string newline = null)
        {
            if (newline == null)
                newline = Environment.NewLine;

            string ret = string.Empty;
            if (outputstart < 1)
                count += outputstart - 1;
            if (count < 1)
                return ret;

            int position = 0, newposition;
            int lineno = 0;

            while ((newposition = s.IndexOf(newline, position)) != -1)
            {
                lineno++;
                if (lineno >= outputstart)
                {
                    if (markline >= 1)
                        ret += (lineno == markline) ? ">>> " : "";
                    if (lineformat != null)
                        ret += (startlinenooffset + lineno).ToStringInvariant(lineformat) + ": ";
                    ret += s.Substring(position, newposition - position) + newline;
                    if (--count <= 0)
                        return ret;
                }

                position = newposition + newline.Length;
            }

            return ret;
        }
    }
}