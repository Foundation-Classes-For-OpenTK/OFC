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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GLOFC.GL4.Controls
{
    public static class GLDataGridViewSorts
    {
        public static int SortCompareDouble(GLDataGridViewCell l, GLDataGridViewCell r)
        {
            var lt = l as GLDataGridViewCellText;
            var rt = r as GLDataGridViewCellText;
            if (lt != null && rt != null)
            {
                return lt.Value.InvariantParseDouble(0).CompareTo(rt.Value.InvariantParseDouble(0));
            }
            else
                return 0;
        }
        public static int SortCompareDate(GLDataGridViewCell l, GLDataGridViewCell r)
        {
            var lt = l as GLDataGridViewCellText;
            var rt = r as GLDataGridViewCellText;
            if (lt != null && rt != null)
            {
                return lt.Value.CompareDate(rt.Value);
            }
            else
                return 0;
        }
    }
}
