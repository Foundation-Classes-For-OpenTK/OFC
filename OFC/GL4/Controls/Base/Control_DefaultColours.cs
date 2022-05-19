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

// Rules - no winforms in Control land except for Keys

using System;
using System.Drawing;

// Purposely not documented - no need
#pragma warning disable 1591

namespace GLOFC.GL4.Controls
{
    public abstract partial class GLBaseControl : IDisposable
    {
        #region Colours

        // default color schemes and sizes

        static public Color DefaultButtonBackColor = SystemColors.Control;
        static public Color DefaultButtonFaceColor = SystemColors.Control;
        static public Color DefaultButtonBorderColor = SystemColors.ControlText;
        static public Color DefaultButtonForeColor = SystemColors.ControlText;      // text
        static public Color DefaultMouseOverButtonColor = Color.FromArgb(200, 200, 200);
        static public Color DefaultMouseDownButtonColor = Color.FromArgb(230, 230, 230);

        static public Color DefaultListBoxBackColor = SystemColors.Window;
        static public Color DefaultListBoxBorderColor = SystemColors.ControlText;
        static public Color DefaultListBoxForeColor = SystemColors.WindowText;
        static public Color DefaultListBoxLineSeparColor = Color.Green;
        static public Color DefaultListBoxMouseOverColor = Color.FromArgb(200, 200, 200);
        static public Color DefaultListBoxSelectedItemColor = Color.FromArgb(230, 230, 230);

        static public Color DefaultComboBoxBackColor = SystemColors.Window;
        static public Color DefaultComboBoxFaceColor = SystemColors.Window;
        static public Color DefaultComboBoxBorderColor = SystemColors.ControlText;
        static public Color DefaultComboBoxForeColor = SystemColors.ControlText;      // text

        static public Color DefaultScrollbarBackColor = SystemColors.Control;
        static public Color DefaultScrollbarBorderColor = SystemColors.ControlText;
        static public Color DefaultScrollbarSliderColor = Color.FromArgb(200, 200, 200);
        static public Color DefaultScrollbarArrowColor = SystemColors.ControlText;
        static public Color DefaultScrollbarArrowButtonFaceColor = SystemColors.Control;
        static public Color DefaultScrollbarArrowButtonBorderColor = SystemColors.ControlText;
        static public Color DefaultScrollbarMouseOverColor = Color.FromArgb(200, 200, 200);
        static public Color DefaultScrollbarMouseDownColor = Color.FromArgb(230, 230, 230);
        static public Color DefaultScrollbarThumbColor = SystemColors.Control;
        static public Color DefaultScrollbarThumbBorderColor = SystemColors.ControlText;

        static public Color DefaultGroupBoxBackColor = SystemColors.Control;
        static public Color DefaultGroupBoxBorderColor = SystemColors.ControlText;
        static public Color DefaultGroupBoxForeColor = SystemColors.ControlText;

        static public Color DefaultFormBackColor = SystemColors.Control;
        static public Color DefaultFormBorderColor = SystemColors.ControlText;
        static public Color DefaultFormTextColor = SystemColors.ControlText;

        static public Color DefaultPanelBackColor = SystemColors.Control;
        static public Color DefaultPanelBorderColor = SystemColors.ControlText;
        static public Color DefaultTableLayoutBackColor = SystemColors.Control;
        static public Color DefaultTableLayoutBorderColor = SystemColors.ControlText;
        static public Color DefaultFlowLayoutBackColor = SystemColors.Control;
        static public Color DefaultFlowLayoutBorderColor = SystemColors.ControlText;

        static public Color DefaultVerticalScrollPanelBorderColor = SystemColors.ControlText;
        static public Color DefaultVerticalScrollPanelBackColor = SystemColors.Control;

        static public Color DefaultDTPForeColor = SystemColors.WindowText;
        static public Color DefaultDTPBackColor = SystemColors.Window;
        static public Color DefaultDTPSelectedColor = Color.FromArgb(220,220,220);

        static public Color DefaultCalendarForeColor = SystemColors.WindowText;
        static public Color DefaultCalendarBackColor = SystemColors.Window;

        static public Color DefaultCheckBackColor = SystemColors.Control;
        static public Color DefaultCheckForeColor = SystemColors.ControlText;       // text
        static public Color DefaultCheckColor = SystemColors.ControlText;
        static public Color DefaultCheckBoxBorderColor = SystemColors.ControlText;
        static public Color DefaultCheckBoxInnerColor = SystemColors.Window;
        static public Color DefaultCheckMouseOverColor = Color.FromArgb(200, 200, 200);
        static public Color DefaultCheckMouseDownColor = Color.FromArgb(230, 230, 230);

        static public Color DefaultTextBoxErrorColor = Color.OrangeRed;
        static public Color DefaultTextBoxHighlightColor = Color.Red;
        static public Color DefaultTextBoxBackColor = SystemColors.Window;
        static public Color DefaultTextBoxForeColor = SystemColors.WindowText;

        static public Color DefaultTabControlForeColor = SystemColors.ControlText;      // of selected text
        static public Color DefaultTabControlBackColor = SystemColors.Control;
        static public Color DefaultTabControlBorderColor = SystemColors.ControlText;
        static public Color DefaultTabControlSelectedBackColor = SystemColors.Control;
        static public Color DefaultTabControlNotSelectedBackColor = SystemColors.Control;
        static public Color DefaultTabControlNotSelectedForeColor = SystemColors.ControlText;
        static public Color DefaultTabControlMouseOverColor = Color.FromArgb(200, 200, 200);

        static public Color DefaultMenuBackColor = SystemColors.Control;
        static public Color DefaultMenuBorderColor = SystemColors.ControlText;
        static public Color DefaultMenuForeColor = SystemColors.ControlText;
        static public Color DefaultMenuMouseOverColor = Color.FromArgb(200, 200, 200);
        static public Color DefaultMenuIconStripBackColor = Color.FromArgb(220, 220, 220);

        static public Color DefaultToolTipBackColor = SystemColors.Info;       // text
        static public Color DefaultToolTipForeColor = SystemColors.InfoText;       // text

        static public Color DefaultLabelForeColor = SystemColors.WindowText;
        static public Color DefaultLabelBorderColor = SystemColors.ControlText;

        static public Color DefaultDGVBorderColor = SystemColors.ControlText;
        static public Color DefaultDGVBackColor = SystemColors.AppWorkspace;

        static public Color DefaultDGVColumnRowBackColor = SystemColors.Control;
        static public Color DefaultDGVColumnRowForeColor = SystemColors.WindowText;

        static public Color DefaultDGVCellBackColor = SystemColors.Window;
        static public Color DefaultDGVCellForeColor = SystemColors.WindowText;

        static public Color DefaultDGVCellBorderColor = SystemColors.ControlDark;

        static public Color DefaultDGVCellSelectedColor = SystemColors.Highlight;
        static public Color DefaultDGVCellHighlightColor = Color.Red;

        static public Color DefaultTrackBarBarColor = Color.Gray;
        static public Color DefaultTrackBarTickColor = Color.FromArgb(255, 192, 192, 192);
        static public Color DefaultTrackMouseOverColor = Color.FromArgb(255, 210, 210, 210);
        static public Color DefaultTrackMouseDownColor = Color.FromArgb(255, 230, 220, 220);

        #endregion

    }
}
