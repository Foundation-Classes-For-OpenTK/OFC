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

namespace GLOFC.GL4.Controls
{
    /// <summary>
    /// Custom Sorts for data grid view
    /// </summary>
    public static class GLDataGridViewSorts
    {
        /// <summary>
        /// Sort as Numeric
        /// </summary>
        /// <param name="l">Left Cell</param>
        /// <param name="r">Rigth Cell</param>
        /// <returns>-1 left is less than right, 0 equal, 1 left is greater than right</returns>
        public static int SortCompareNumeric(GLDataGridViewCell l, GLDataGridViewCell r)
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

        /// <summary>
        /// Sort as Dates
        /// </summary>
        /// <param name="l">Left Cell</param>
        /// <param name="r">Rigth Cell</param>
        /// <returns>-1 left is less than right, 0 equal, 1 left is greater than right</returns>
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
