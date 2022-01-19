/*
 * Copyright 2019-2021 Robbyxp1 @ github.com
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
using OpenTK;
using System;

// no xml needed 
#pragma warning disable 1591

namespace GLOFC
{
    //strictly for debugging, a string matrix multiplier so your mind does not get warped when trying to come up with compound matrix functions
    //for glsl

    public class StringVector4
    {
        public string[] Element { get; set; } = new string[4];        // in column order

        public StringVector4() { }
        public StringVector4(params string[] el)     // in row order
        {
            int i = 0;
            foreach (var e in el)
            {
                Element[i++] = e;
            }
        }
        public override string ToString()
        { return ToString(false); }

        public string ToString(bool lf)
        {
            string res = "(";
            for (int i = 0; i < 4; i++)
            {
                res += Element[i];
                if (i < 3)
                {
                    res += ", ";
                    if (lf)
                        res += Environment.NewLine;
                }
            }

            res += ")";
            return res;
        }
    }

    public class StringMatrix
    {
        public string[] Element { get; set; } = new string[16];        // in column order

        public StringMatrix() { }
        public StringMatrix(params string[] el)     // in row order
        {
            int i = 0;
            foreach (var e in el)
            {
                Element[i++] = e;
            }
        }

        static public StringMatrix Mult(StringMatrix l, StringMatrix r)
        {
            StringMatrix res = new StringMatrix();
            for (int i = 0; i < 16; i++)
            {
                int left = (i / 4) * 4;
                int right = i % 4;
                string equation = "";
                for (int j = 0; j < 4; j++)
                {
                    var a = l.Element[left + j];
                    var b = r.Element[right + j * 4];
                    string m = Mult(a, b);
                    equation = equation.AppendPrePad(m, " + ");
                }

                equation = equation != "" ? equation : "0";

                res.Element[i] = equation;
            }
            return res;
        }

        static public string Mult(string a,string b)
        {
            if (a != "0" && b != "0")
            {
                if (a == "1")
                    return b;
                else if (a == "-1")
                    return b.StartsWith("-") ? b.Substring(1) : "-" + b;
                else if (b == "1")
                    return a;
                else if (b == "-1")
                    return a.StartsWith("-") ? a.Substring(1) : "-" + a;
                else
                    return a + "*" + b;
            }
            else
                return null;
        }

        static public StringVector4 Mult(StringMatrix l, StringVector4 r)
        {
            StringVector4 res = new StringVector4();
            for (int i = 0; i < 4; i++)
            {
                int left = i * 4;
                string equation = "";
                for (int j = 0; j < 4; j++)
                {
                    var a = l.Element[left + j];
                    var b = r.Element[i];
                    string m = Mult(a, b);
                    equation = equation.AppendPrePad(m, " + ");
                }

                equation = equation != "" ? equation : "0";
                res.Element[i] = equation;
            }
            return res;
        }

        public override string ToString()
        { return ToString(false); }

        public string ToString(bool lf)
        {
            string res = "(";
            for (int i = 0; i < 16; i++)
            {
                res += Element[i];
                if (i < 15)
                {
                    res += ", ";
                    if (i % 4 == 3 && lf)
                        res += Environment.NewLine;
                }
            }

            res += ")";
            return res;
        }
        public string ToList()
        {
            string res = "";
            for (int i = 0; i < 16; i++)
            {
                string element = $"m{(i / 4)+1}{(i % 4)+1}={Element[i]}" + Environment.NewLine;
                res += element;
            }
            return res;
        }
    }

}
